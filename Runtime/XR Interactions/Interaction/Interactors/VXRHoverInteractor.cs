using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Utilities;

namespace VaporXR
{
    /// <summary>
    /// Interactor used for interacting with interactables through hovering.
    /// This interactor can only fire hover events.
    /// </summary>
    /// <seealso cref="VXRHoverPoseFilter"/>
    public class VXRHoverInteractor : VXRBaseInteractor, IPoseSource
    {
        /// <summary>
        /// Sets which shape of physics cast to use for the cast when detecting collisions.
        /// </summary>
        public enum OverlapDetectionModeType
        {
            /// <summary>
            /// Uses <see cref="OverlapSphereCommand"/> to detect collisions.
            /// </summary>
            Sphere,

            /// <summary>
            /// Uses <see cref="OverlapBoxCommand"/> to detect collisions.
            /// </summary>
            Box,
        }

        #region Inspector
        private bool IsOverlapModeSphere => _overlapMode == OverlapDetectionModeType.Sphere;
        private bool IsOverlapModeBox => _overlapMode == OverlapDetectionModeType.Box;

        [BoxGroup("Components"), SerializeField]
        private VXRHand _hand;

        [FoldoutGroup("Overlap"), SerializeField]
        [RichTextTooltip("The starting origin of overlap checks.")]
        private Transform _overlapAnchor;
        [FoldoutGroup("Overlap"), SerializeField]
        private OverlapDetectionModeType _overlapMode;
        [FoldoutGroup("Overlap"), SerializeField, ShowIf("$IsOverlapModeSphere")]
        private float _overlapSphereRadius = 0.1f;
        [FoldoutGroup("Overlap"), SerializeField, ShowIf("$IsOverlapModeBox")]
        private Vector3 _overlapBoxSize = new(0.1f, 0.05f, 0.1f);

        [FoldoutGroup("Overlap"), SerializeField]
        private LayerMask _overlapMask = 1; // Default
        [FoldoutGroup("Overlap"), SerializeField]
        [RichTextTooltip("The type of interaction with trigger colliders when overlapping.")]
        private QueryTriggerInteraction _overlapTriggerInteraction = QueryTriggerInteraction.Ignore;

        [FoldoutGroup("Posing"), SerializeField]
        private bool _hoverPoseEnabled = false;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_hoverPoseEnabled")]
        private HandPoseDatum _hoverPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_hoverPoseEnabled")]
        private float _hoverPoseDuration;
        #endregion

        #region Properties
        /// <summary>
        /// The nearest <see cref="IXRInteractable"/> object hit by the ray that was inserted into the valid targets
        /// list when not selecting anything.
        /// </summary>
        /// <remarks>
        /// Updated during <see cref="PreprocessInteractor"/>.
        /// </remarks>
        protected IXRInteractable CurrentNearestValidTarget { get; private set; }

        /// <summary>
        /// The set of Interactables that this Interactor could possibly interact with this frame.
        /// This list is not sorted by priority.
        /// </summary>
        /// <seealso cref="IXRInteractor.GetValidTargets"/>
        protected List<IXRInteractable> UnsortedOverlapTargets { get; } = new();

        protected List<IXRInteractable> SortedRaycastTargets { get; } = new();
        #endregion

        #region Fields
        /// <summary>
        /// The set of Colliders that stayed in touch with this Interactor.
        /// </summary>
        private readonly HashSet<Collider> _stayedOverlapColliders = new();

        private readonly TriggerContactMonitor _overlapContactMonitor = new();

        private PhysicsScene _localPhysicsScene;
        private Vector3 _lastCastOrigin = Vector3.zero;
        private readonly Collider[] _overlapHits = new Collider[25];
        private readonly RaycastHit[] _castHits = new RaycastHit[25];
        private bool _firstFrame = true;
        private bool _overlapContactsSortedThisFrame;
        private readonly List<IXRInteractable> _sortedValidTargets = new();
        private readonly List<IXRInteractable> _frameValidTargets = new();

        /// <summary>
        /// Reusable list of optimal raycast hits, for lookup during cone casting.
        /// </summary>
        private static readonly HashSet<Collider> s_OptimalHits = new();
        #endregion

        #region - Initialization -
        protected override void Awake()
        {
            base.Awake();
            AllowSelect = false;
            _localPhysicsScene = gameObject.scene.GetPhysicsScene();
            _overlapContactMonitor.interactionManager = InteractionManager;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _overlapContactMonitor.contactAdded += OnOverlapContactAdded;
            _overlapContactMonitor.contactRemoved += OnOverlapContactRemoved;
            ResetCollidersAndValidTargets();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _overlapContactMonitor.contactAdded -= OnOverlapContactAdded;
            _overlapContactMonitor.contactRemoved -= OnOverlapContactRemoved;
            ResetCollidersAndValidTargets();
        }

        public override void OnRegistered(InteractorRegisteredEventArgs args)
        {
            base.OnRegistered(args);
            args.manager.interactableRegistered += OnInteractableRegistered;
            args.manager.interactableUnregistered += OnInteractableUnregistered;
            _overlapContactMonitor.interactionManager = args.manager;
        }

        public override void OnUnregistered(InteractorUnregisteredEventArgs args)
        {
            base.OnUnregistered(args);
            args.manager.interactableRegistered -= OnInteractableRegistered;
            args.manager.interactableUnregistered -= OnInteractableUnregistered;
        }

        private void OnInteractableRegistered(InteractableRegisteredEventArgs args)
        {
            var interactable = args.interactableObject;
            _overlapContactMonitor.ResolveUnassociatedColliders(interactable);
            if (_overlapContactMonitor.IsContacting(interactable))
            {
                OnOverlapContactAdded(interactable);
            }
        }

        private void OnInteractableUnregistered(InteractableUnregisteredEventArgs args)
        {
            OnOverlapContactRemoved(args.interactableObject);
        }
        #endregion

        #region - Processing -
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                if (_overlapMode == OverlapDetectionModeType.Sphere)
                {
                    EvaluateSphereOverlap();
                }
                else
                {
                    EvaluateBoxOverlap();
                }

                // Determine the Interactables that this Interactor could possibly interact with this frame
                GetValidTargets(_frameValidTargets);
                var nearestObject = (_frameValidTargets.Count > 0) ? _frameValidTargets[0] : null;
                if (nearestObject != CurrentNearestValidTarget && !HasSelection)
                {
                    CurrentNearestValidTarget = nearestObject;
                }
            }
        }
        #endregion

        #region - Hovering -

        public override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);
            OnHoverPoseEntered(args);
        }

        public override void OnHoverExited(HoverExitEventArgs args)
        {
            base.OnHoverExited(args);
            OnHoverPoseExited(args);
        }

        private void OnHoverPoseEntered(HoverEnterEventArgs args)
        {
            if (_hoverPoseEnabled)
            {
                if (args.interactableObject is VXRHoverInteractable interactable && interactable.TryGetOverrideHoverPose(out var pose, out var duration))
                {
                    _hand.RequestHandPose(HandPoseType.Hover, this, pose.Value, duration: duration);
                }
                else
                {
                    _hand.RequestHandPose(HandPoseType.Hover, this, _hoverPose.Value, duration: _hoverPoseDuration);
                }
            }
        }

        private void OnHoverPoseExited(HoverExitEventArgs args)
        {
            if (_hoverPoseEnabled)
            {
                _hand.RequestReturnToIdle(this, _hoverPoseDuration);
            }
        }
        #endregion

        #region - Contacts -
        private void EvaluateSphereOverlap()
        {
            _overlapContactsSortedThisFrame = false;
            _stayedOverlapColliders.Clear();

            //Transform directAttachTransform = GetAttachTransform(null);
            // Hover Check
            Vector3 interactorPosition = _overlapAnchor.position;// directAttachTransform.TransformPoint(_overlapAnchor.position);
            Vector3 overlapStart = _lastCastOrigin;
            Vector3 interFrameEnd = interactorPosition;
            float grabRadius = _overlapSphereRadius;

            BurstPhysicsUtils.GetSphereOverlapParameters(overlapStart, interFrameEnd, out var normalizedOverlapVector, out var overlapSqrMagnitude, out var overlapDistance);

            // If no movement is recorded.
            // Check if sphere cast size is sufficient for proper cast, or if first frame since last frame poke position will be invalid.
            if (_firstFrame || overlapSqrMagnitude < 0.001f)
            {
                var numberOfOverlaps = _localPhysicsScene.OverlapSphere(interFrameEnd, grabRadius, _overlapHits,
                    _overlapMask, _overlapTriggerInteraction);

                for (var i = 0; i < numberOfOverlaps; ++i)
                {
                    _stayedOverlapColliders.Add(_overlapHits[i]);
                }
            }
            else
            {
                var numberOfOverlaps = _localPhysicsScene.SphereCast(
                    overlapStart,
                    grabRadius,
                    normalizedOverlapVector,
                    _castHits,
                    overlapDistance,
                    _overlapMask,
                    _overlapTriggerInteraction);

                for (var i = 0; i < numberOfOverlaps; ++i)
                {
                    _stayedOverlapColliders.Add(_castHits[i].collider);
                }
            }

            _overlapContactMonitor.UpdateStayedColliders(_stayedOverlapColliders);

            _lastCastOrigin = interactorPosition;
            _firstFrame = false;
        }

        private void EvaluateBoxOverlap()
        {
            _overlapContactsSortedThisFrame = false;
            _stayedOverlapColliders.Clear();

            //Transform directAttachTransform = GetAttachTransform(null);
            // Hover Check
            Vector3 interactorPosition = _overlapAnchor.position;//directAttachTransform.TransformPoint(_overlapAnchor.position);
            Vector3 overlapStart = _lastCastOrigin;
            Vector3 interFrameEnd = interactorPosition;
            var boxSize = _overlapBoxSize / 2f;

            BurstPhysicsUtils.GetSphereOverlapParameters(overlapStart, interFrameEnd, out var normalizedOverlapVector, out var overlapSqrMagnitude, out var overlapDistance);

            // If no movement is recorded.
            // Check if sphere cast size is sufficient for proper cast, or if first frame since last frame poke position will be invalid.
            if (_firstFrame || overlapSqrMagnitude < 0.001f)
            {
                var numberOfOverlaps = _localPhysicsScene.OverlapBox(interFrameEnd, boxSize, _overlapHits, _overlapAnchor.rotation,
                    _overlapMask, _overlapTriggerInteraction);

                for (var i = 0; i < numberOfOverlaps; ++i)
                {
                    _stayedOverlapColliders.Add(_overlapHits[i]);
                }
            }
            else
            {
                var numberOfOverlaps = _localPhysicsScene.BoxCast(
                    overlapStart,
                    boxSize,
                    normalizedOverlapVector,
                    _castHits,
                    _overlapAnchor.rotation,
                    overlapDistance,
                    _overlapMask,
                    _overlapTriggerInteraction);

                for (var i = 0; i < numberOfOverlaps; ++i)
                {
                    _stayedOverlapColliders.Add(_castHits[i].collider);
                }
            }

            _overlapContactMonitor.UpdateStayedColliders(_stayedOverlapColliders);

            _lastCastOrigin = interactorPosition;
            _firstFrame = false;
        }

        private void OnOverlapContactAdded(IXRInteractable interactable)
        {
            if (UnsortedOverlapTargets.Contains(interactable))
            {
                return;
            }

            UnsortedOverlapTargets.Add(interactable);
            _overlapContactsSortedThisFrame = false;
        }

        private void OnOverlapContactRemoved(IXRInteractable interactable)
        {
            if (UnsortedOverlapTargets.Remove(interactable))
            {
                _overlapContactsSortedThisFrame = false;
            }
        }

        /// <summary>
        /// Clears current valid targets and stayed colliders.
        /// </summary>
        private void ResetCollidersAndValidTargets()
        {
            UnsortedOverlapTargets.Clear();
            SortedRaycastTargets.Clear();
            _sortedValidTargets.Clear();
            _overlapContactsSortedThisFrame = false;
            _firstFrame = true;
            _stayedOverlapColliders.Clear();
            _overlapContactMonitor.UpdateStayedColliders(_stayedOverlapColliders);
        }
        #endregion

        #region - Helpers -
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            targets.Clear();

            if (!isActiveAndEnabled)
            {
                return;
            }

            var filter = TargetFilter;
            if (filter != null && filter.canProcess)
            {
                if (UnsortedOverlapTargets.Count > 0)
                {
                    filter.Process(this, UnsortedOverlapTargets, targets);
                }
            }
            else
            {
                if (UnsortedOverlapTargets.Count > 0)
                {
                    // If not using the filter, we can cache the sorting of valid targets until the next time PreprocessInteractor is executed.
                    if (_overlapContactsSortedThisFrame)
                    {
                        targets.AddRange(_sortedValidTargets);
                        return;
                    }

                    // Sort valid targets
                    SortingHelpers.SortByDistanceToInteractor(this, UnsortedOverlapTargets, _sortedValidTargets);

                    targets.AddRange(_sortedValidTargets);
                    _overlapContactsSortedThisFrame = true;
                }
            }
        }
        #endregion

        private void OnDrawGizmosSelected()
        {
            if (_overlapAnchor)
            {
                Gizmos.color = Color.white;
                switch (_overlapMode)
                {
                    case OverlapDetectionModeType.Sphere:
                        Gizmos.DrawWireSphere(_overlapAnchor.position, _overlapSphereRadius);
                        break;
                    case OverlapDetectionModeType.Box:
                        Gizmos.DrawWireCube(_overlapAnchor.position, _overlapBoxSize);
                        break;
                }
            }
        }
    }
}
