using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Utilities;

namespace VaporXR
{
    /// <summary>
    /// Interactor used for directly interacting with interactables. This interaction will prioritize objects that are touching the interacting overlap volume
    /// and optionally fallback to the raycast volume.
    /// </summary>
    public class VXRGrabInteractor : VXRInputInteractor
    {              
        #region Inspector
        private bool IsOverlapModeSphere => _overlapMode == OverlapDetectionModeType.Sphere;
        private bool IsOverlapModeBox => _overlapMode == OverlapDetectionModeType.Box;

        private bool IsHitModeRaycast => _hitDetectionType == HitDetectionModeType.Raycast;
        private bool IsHitModeSphere => _hitDetectionType == HitDetectionModeType.SphereCast;
        private bool IsHitModeCone => _hitDetectionType == HitDetectionModeType.ConeCast;

        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("If <lw>true</lw> this interactor can grab objects at a distance and pull them to it.")]
        private bool _distantGrabActive = true;

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

        [FoldoutGroup("Raycast"), SerializeField]
        [RichTextTooltip("The starting position and direction of any ray casts.")]
        private Transform _rayOriginTransform;
        [FoldoutGroup("Raycast"), SerializeField]
        [RichTextTooltip("The type of hit detection to use for the ray cast.\n" +
                         "<mth>Raycast:</mth> Uses <cls>Physics</cls> RayCast to detect collisions.\n" +
                         "<mth>SphereCast:</mth> Uses <cls>Physics</cls> SphereCast to detect collisions.\n" +
                         "<mth>ConeCast:</mth> Uses cone casting to detect collisions.")]
        private HitDetectionModeType _hitDetectionType = HitDetectionModeType.Raycast;
        [FoldoutGroup("Raycast"), SerializeField, Range(0.01f, 0.25f), ShowIf("$IsHitModeSphere")]
        [RichTextTooltip("The radius used for sphere casting.")]
        private float _sphereCastRadius = 0.1f;
        [FoldoutGroup("Raycast"), SerializeField, Range(0f, 180f), ShowIf("$IsHitModeCone")]
        [RichTextTooltip("The angle in degrees of the cone used for cone casting. Will use regular ray casting if set to 0.")]
        private float _coneCastAngle = 6f;
        [FoldoutGroup("Raycast"), SerializeField]
        [RichTextTooltip("The layer mask used for limiting ray cast targets.")]
        private LayerMask _raycastMask = 1;
        [FoldoutGroup("Raycast"), SerializeField]
        [RichTextTooltip("The type of interaction with trigger colliders via ray cast.")]
        private QueryTriggerInteraction _raycastTriggerInteraction = QueryTriggerInteraction.Ignore;
        [FoldoutGroup("Raycast"), SerializeField]
        [RichTextTooltip("Whether ray cast should include or ignore hits on trigger colliders that are snap volume colliders, even if the ray cast is set to ignore triggers.\n" +
                         "If you are not using gaze assistance or XR Interactable Snap Volume components, you should set this property to <itf>QuerySnapVolumeInteraction</itf>.<mth>Ignore</mth> to avoid the performance cost.")]
        private QuerySnapVolumeInteraction _raycastSnapVolumeInteraction = QuerySnapVolumeInteraction.Collide;
        [FoldoutGroup("Raycast"), SerializeField]
        [RichTextTooltip("Whether Unity considers only the closest Interactable as a valid target for interaction.")]
        private bool _hitClosestOnly;
        [FoldoutGroup("Raycast"), SerializeField]
        [RichTextTooltip("Gets or sets the max distance of ray cast when the line type is a straight line. Increasing this value will make the line reach further.")]
        private float _maxRaycastDistance = 30f;
        #endregion

        #region Properties
        /// <summary>
        /// The nearest <see cref="IVXRInteractable"/> object hit by the ray that was inserted into the valid targets
        /// list when not selecting anything.
        /// </summary>
        /// <remarks>
        /// Updated during <see cref="PreprocessInteractor"/>.
        /// </remarks>
        protected IVXRInteractable CurrentNearestValidTarget { get; private set; }

        /// <summary>
        /// The set of Interactables that this Interactor could possibly interact with this frame.
        /// This list is not sorted by priority.
        /// </summary>
        /// <seealso cref="IXRInteractor.GetValidTargets"/>
        protected List<IVXRInteractable> UnsortedOverlapTargets { get; } = new ();

        protected List<IVXRInteractable> SortedRaycastTargets { get; } = new();
        #endregion

        #region Fields
        private const int MaxRaycastHits = 10;

        /// <summary>
        /// The set of Colliders that stayed in touch with this Interactor.
        /// </summary>
        private readonly HashSet<Collider> _stayedOverlapColliders = new();
        /// <summary>
        /// The set of Colliders that stayed in distant grab with this Interactor.
        private readonly HashSet<Collider> _stayedRaycastColliders = new();

        private readonly TriggerContactMonitor _overlapContactMonitor = new();
        private readonly TriggerContactMonitor _raycastContactMonitor = new();

        private PhysicsScene _localPhysicsScene;
        private Vector3 _lastCastOrigin = Vector3.zero;
        private readonly Collider[] _overlapHits = new Collider[25];
        private readonly RaycastHit[] _castHits = new RaycastHit[25];
        private bool _firstFrame = true;
        private bool _overlapContactsSortedThisFrame;
        private readonly List<IVXRInteractable> _sortedValidTargets = new();
        private readonly List<IVXRInteractable> _frameValidTargets = new();


        private readonly RaycastHit[] _raycastHits = new RaycastHit[MaxRaycastHits];
        private int _raycastHitsCount;
        private readonly RaycastHitComparer _raycastHitComparer = new();

        private static readonly RaycastHit[] s_ConeCastScratch = new RaycastHit[10];
        /// <summary>
        /// Reusable list of optimal raycast hits, for lookup during cone casting.
        /// </summary>
        private static readonly HashSet<Collider> s_OptimalHits = new();
        #endregion

        #region - Initialization -
        protected override void Awake()
        {
            base.Awake();
            _localPhysicsScene = gameObject.scene.GetPhysicsScene();
            _overlapContactMonitor.interactionManager = InteractionManager;
            _raycastContactMonitor.interactionManager = InteractionManager;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _overlapContactMonitor.contactAdded += OnOverlapContactAdded;
            _overlapContactMonitor.contactRemoved += OnOverlapContactRemoved;
            _raycastContactMonitor.contactAdded += OnRaycastContactAdded;
            _raycastContactMonitor.contactRemoved += OnRaycastContactRemoved;
            ResetCollidersAndValidTargets();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _overlapContactMonitor.contactAdded -= OnOverlapContactAdded;
            _overlapContactMonitor.contactRemoved -= OnOverlapContactRemoved;
            _raycastContactMonitor.contactAdded -= OnRaycastContactAdded;
            _raycastContactMonitor.contactRemoved -= OnRaycastContactRemoved;
            ResetCollidersAndValidTargets();
        }

        public override void OnRegistered(InteractorRegisteredEventArgs args)
        {
            base.OnRegistered(args);
            args.manager.interactableRegistered += OnInteractableRegistered;
            args.manager.interactableUnregistered += OnInteractableUnregistered;
            _overlapContactMonitor.interactionManager = args.manager;
            _raycastContactMonitor.interactionManager = args.manager;
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
            _raycastContactMonitor.ResolveUnassociatedColliders(interactable);
            if (_overlapContactMonitor.IsContacting(interactable))
            {
                OnOverlapContactAdded(interactable);
            }
            if (_raycastContactMonitor.IsContacting(interactable))
            {
                OnRaycastContactAdded(interactable);
            }
        }

        private void OnInteractableUnregistered(InteractableUnregisteredEventArgs args)
        {
            OnOverlapContactRemoved(args.interactableObject);
            OnRaycastContactRemoved(args.interactableObject);
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

                if (_distantGrabActive)
                {
                    EvaluateRaycasts();
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

        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractor(updatePhase);
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

        private void EvaluateRaycasts()
        {
            _stayedRaycastColliders.Clear();
            var queryTriggerInteraction = _raycastSnapVolumeInteraction == QuerySnapVolumeInteraction.Collide
                ? QueryTriggerInteraction.Collide
                : _raycastTriggerInteraction;

            switch (_hitDetectionType)
            {
                case HitDetectionModeType.Raycast:
                    _raycastHitsCount = _localPhysicsScene.Raycast(_rayOriginTransform.position, _rayOriginTransform.forward,
                        _raycastHits, _maxRaycastDistance, _raycastMask, queryTriggerInteraction);
                    break;

                case HitDetectionModeType.SphereCast:
                    _raycastHitsCount = _localPhysicsScene.SphereCast(_rayOriginTransform.position, _sphereCastRadius, _rayOriginTransform.forward,
                        _raycastHits, _maxRaycastDistance, _raycastMask, queryTriggerInteraction);
                    break;

                case HitDetectionModeType.ConeCast:
                        _raycastHitsCount = FilteredConeCast(_rayOriginTransform.position, _coneCastAngle, _rayOriginTransform.forward, _rayOriginTransform.position,
                            _raycastHits, _maxRaycastDistance, _raycastMask, queryTriggerInteraction);
                    break;
            }

            if (_raycastHitsCount > 0)
            {
                var baseQueryHitsTriggers = _raycastTriggerInteraction == QueryTriggerInteraction.Collide ||
                    (_raycastTriggerInteraction == QueryTriggerInteraction.UseGlobal && Physics.queriesHitTriggers);

                if (_raycastSnapVolumeInteraction == QuerySnapVolumeInteraction.Ignore && baseQueryHitsTriggers)
                {
                    // Filter out Snap Volume trigger collider hits
                    _raycastHitsCount = FilterTriggerColliders(InteractionManager, _raycastHits, _raycastHitsCount, snapVolume => snapVolume != null);
                }
                else if (_raycastSnapVolumeInteraction == QuerySnapVolumeInteraction.Collide && !baseQueryHitsTriggers)
                {
                    // Filter out trigger collider hits that are not Snap Volume snap colliders
                    _raycastHitsCount = FilterTriggerColliders(InteractionManager, _raycastHits, _raycastHitsCount, snapVolume => snapVolume == null);
                }

                // Sort all the hits by distance along the curve since the results of the 3D ray cast are not ordered.
                // Sorting is done after filtering above for performance.
                SortingHelpers.Sort(_raycastHits, _raycastHitComparer, _raycastHitsCount);
            }

            if (_raycastHitsCount > 0)
            {
                for (var i = 0; i < _raycastHitsCount; ++i)
                {
                    var raycastHit = _raycastHits[i];
                    _stayedRaycastColliders.Add(raycastHit.collider);

                    // Stop after the first if enabled
                    if (_hitClosestOnly)
                    {
                        break;
                    }
                }
            }

            _raycastContactMonitor.UpdateStayedColliders(_stayedRaycastColliders);

            static int FilterTriggerColliders(VXRInteractionManager interactionManager, RaycastHit[] raycastHits, int count, Func<VXRInteractableSnapVolume, bool> removeRule)
            {
                for (var index = 0; index < count; ++index)
                {
                    var hitCollider = raycastHits[index].collider;
                    if (hitCollider.isTrigger)
                    {
                        interactionManager.TryGetInteractableForCollider(hitCollider, out _, out var snapVolume);
                        if (removeRule(snapVolume))
                        {
                            RemoveAt(raycastHits, index, count);
                            --count;
                            --index;
                        }
                    }
                }

                return count;
            }

            static void RemoveAt<T>(T[] array, int index, int count) where T : struct
            {
                Array.Copy(array, index + 1, array, index, count - index - 1);
                Array.Clear(array, count - 1, 1);
            }
        }

        [BurstCompile]
        private int FilteredConeCast(in Vector3 from, float coneCastAngleDegrees, in Vector3 direction, in Vector3 origin,
            RaycastHit[] results, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            s_OptimalHits.Clear();

            // Set up all the sphere casts
            var obstructionDistance = math.min(maxDistance, 1000f);

            // Raycast looking for obstructions and any optimal targets
            var hitCounter = 0;
            var optimalHits = _localPhysicsScene.Raycast(origin, direction, s_ConeCastScratch, obstructionDistance, layerMask, queryTriggerInteraction);
            if (optimalHits > 0)
            {
                for (var i = 0; i < optimalHits; ++i)
                {
                    var hitInfo = s_ConeCastScratch[i];
                    if (hitInfo.distance > obstructionDistance)
                    {
                        continue;
                    }

                    // If an obstruction is found, then reject anything behind it
                    if (!InteractionManager.TryGetInteractableForCollider(hitInfo.collider, out _))
                    {
                        obstructionDistance = math.min(hitInfo.distance, obstructionDistance);

                        // Since we are rejecting anything past the obstruction, we push its distance back to allow for objects in the periphery to be selected first
                        hitInfo.distance += 1.5f;
                    }

                    results[hitCounter] = hitInfo;
                    s_OptimalHits.Add(hitInfo.collider);
                    hitCounter++;
                }
            }

            // Now do a series of sphere casts that increase in size.
            // We don't process obstructions here
            // We don't do ultra-fine cone rejection instead add horizontal distance to the spherecast depth
            var angleRadius = math.tan(math.radians(coneCastAngleDegrees) * 0.5f);
            var currentOffset = (origin - from).magnitude;
            while (currentOffset < obstructionDistance)
            {
                BurstPhysicsUtils.GetConecastParameters(angleRadius, currentOffset, obstructionDistance, direction, out var originOffset, out var endRadius, out var castMax);

                // Spherecast
                var initialResults = _localPhysicsScene.SphereCast(origin + originOffset, endRadius, direction, s_ConeCastScratch, castMax, layerMask, queryTriggerInteraction);

                // Go through each result
                for (var i = 0; (i < initialResults && hitCounter < results.Length); i++)
                {
                    var hit = s_ConeCastScratch[i];

                    // Range check
                    if (hit.distance > obstructionDistance)
                        continue;

                    // If it's an optimal hit, then skip it
                    if (s_OptimalHits.Contains(hit.collider))
                        continue;

                    // It must have an interactable
                    if (!InteractionManager.TryGetInteractableForCollider(hit.collider, out _))
                        continue;

                    if (Mathf.Approximately(hit.distance, 0f) && hit.point == Vector3.zero)
                    {
                        // Sphere cast can return hits where point is (0, 0, 0) in error.
                        continue;
                    }

                    // Adjust distance by distance from ray center for default sorting
                    BurstPhysicsUtils.GetConecastOffset(origin, hit.point, direction, out var hitToRayDist);

                    // We penalize these off-center hits by a meter + whatever horizontal offset they have
                    hit.distance += currentOffset + 1f + (hitToRayDist);
                    results[hitCounter] = hit;
                    hitCounter++;
                }
                currentOffset += castMax;
            }

            s_OptimalHits.Clear();
            Array.Clear(s_ConeCastScratch, 0, 10);
            return hitCounter;
        }

        private void OnOverlapContactAdded(IVXRInteractable interactable)
        {
            if (UnsortedOverlapTargets.Contains(interactable))
            {
                return;
            }

            UnsortedOverlapTargets.Add(interactable);
            _overlapContactsSortedThisFrame = false;
        }

        private void OnRaycastContactAdded(IVXRInteractable interactable)
        {
            if (SortedRaycastTargets.Contains(interactable))
            {
                return;
            }

            SortedRaycastTargets.Add(interactable);
        }

        private void OnOverlapContactRemoved(IVXRInteractable interactable)
        {
            if (UnsortedOverlapTargets.Remove(interactable))
            {
                _overlapContactsSortedThisFrame = false;
            }
        }

        private void OnRaycastContactRemoved(IVXRInteractable interactable)
        {
            SortedRaycastTargets.Remove(interactable);
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
            _stayedRaycastColliders.Clear();
            _overlapContactMonitor.UpdateStayedColliders(_stayedOverlapColliders);
            _raycastContactMonitor.UpdateStayedColliders(_stayedRaycastColliders);
        }
        #endregion

        #region - Hovering -
        public override bool CanHover(IVXRHoverInteractable interactable)
        {
            return base.CanHover(interactable) && (!HasSelection || IsSelecting(interactable));
        }
        #endregion

        #region - Selection -
        public override bool CanSelect(IVXRSelectInteractable interactable)
        {
            return base.CanSelect(interactable) && (!HasSelection || IsSelecting(interactable));
        }
        #endregion

        #region - Helpers -
        public override void GetValidTargets(List<IVXRInteractable> targets)
        {
            targets.Clear();

            if (!isActiveAndEnabled)
            {
                return;
            }

            var filter = TargetFilter;
            if (filter != null && filter.CanProcess)
            {
                if (UnsortedOverlapTargets.Count > 0)
                {
                    filter.Process(this, UnsortedOverlapTargets, targets);
                }
                else if (_distantGrabActive)
                {
                    filter.Process(this, SortedRaycastTargets, targets);
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
                else if (_distantGrabActive)
                {
                    targets.AddRange(SortedRaycastTargets);
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

            if (_rayOriginTransform)
            {
                var transformData = _rayOriginTransform != null ? _rayOriginTransform : transform;
                var gizmoStart = transformData.position;
                var gizmoEnd = gizmoStart + (transformData.forward * _maxRaycastDistance);
                Gizmos.color = new Color(58 / 255f, 122 / 255f, 248 / 255f, 237 / 255f);

                switch (_hitDetectionType)
                {
                    case HitDetectionModeType.Raycast:
                        // Draw the raycast line
                        Gizmos.DrawLine(gizmoStart, gizmoEnd);
                        break;

                    case HitDetectionModeType.SphereCast:
                        {
                            var gizmoUp = transformData.up * _sphereCastRadius;
                            var gizmoSide = transformData.right * _sphereCastRadius;
                            Gizmos.DrawWireSphere(gizmoStart, _sphereCastRadius);
                            Gizmos.DrawLine(gizmoStart + gizmoSide, gizmoEnd + gizmoSide);
                            Gizmos.DrawLine(gizmoStart - gizmoSide, gizmoEnd - gizmoSide);
                            Gizmos.DrawLine(gizmoStart + gizmoUp, gizmoEnd + gizmoUp);
                            Gizmos.DrawLine(gizmoStart - gizmoUp, gizmoEnd - gizmoUp);
                            Gizmos.DrawWireSphere(gizmoEnd, _sphereCastRadius);
                            break;
                        }

                    case HitDetectionModeType.ConeCast:
                        {
                            var coneRadius = Mathf.Tan(_coneCastAngle * Mathf.Deg2Rad * 0.5f) * _maxRaycastDistance;
                            var gizmoUp = transformData.up * coneRadius;
                            var gizmoSide = transformData.right * coneRadius;
                            Gizmos.DrawLine(gizmoStart, gizmoEnd);
                            Gizmos.DrawLine(gizmoStart, gizmoEnd + gizmoSide);
                            Gizmos.DrawLine(gizmoStart, gizmoEnd - gizmoSide);
                            Gizmos.DrawLine(gizmoStart, gizmoEnd + gizmoUp);
                            Gizmos.DrawLine(gizmoStart, gizmoEnd - gizmoUp);
                            Gizmos.DrawWireSphere(gizmoEnd, coneRadius);
                            break;
                        }
                }
            }
        }
    }
}
