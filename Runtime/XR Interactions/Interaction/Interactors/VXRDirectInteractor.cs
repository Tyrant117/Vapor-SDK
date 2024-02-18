using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Utilities;

namespace VaporXR
{
    /// <summary>
    /// Interactor used for directly interacting with interactables that are touching. This is handled via trigger volumes
    /// that update the current set of valid targets for this interactor. This component must have a collision volume that is
    /// set to be a trigger to work.
    /// </summary>
    public class VXRDirectInteractor : VXRInputInteractor
    {
        #region Inspector
        [SerializeField, FoldoutGroup("Interaction")]
        private bool _improveAccuracyWithSphereCollider;
        [SerializeField, FoldoutGroup("Interaction")]
        private LayerMask _physicsLayerMask = 1; // Default
        [SerializeField, FoldoutGroup("Interaction")]
        private QueryTriggerInteraction _physicsTriggerInteraction = QueryTriggerInteraction.Ignore;
        #endregion

        #region Properties
        /// <summary>
        /// When a Sphere Collider component is the only collider on this interactor, and no Rigidbody component is attached,
        /// the interactor will use Burst compiler optimizations and sphere casts instead of relying on physics trigger events
        /// to evaluate direct interactions when this property is enabled. This also improves inter-frame accuracy and reliability.
        /// </summary>
        /// <remarks>
        /// Cannot change this value at runtime after <c>Awake</c>.
        /// Enabling this property can improve inter-frame reliability during fast motion when the requirements for optimization are met
        /// by running on each Update instead of Fixed Update and using a sphere cast to determine valid targets.
        /// Disable to force the use of trigger events, such as the <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnTriggerStay.html"><c>OnTriggerStay</c></a>
        /// and <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html"><c>FixedUpdate</c></a> methods.
        /// </remarks>
        /// <seealso cref="UsingSphereColliderAccuracyImprovement"/>
        public bool ImproveAccuracyWithSphereCollider
        {
            get => _improveAccuracyWithSphereCollider;
            set => _improveAccuracyWithSphereCollider = value;
        }
        
        /// <summary>
        /// Whether the requirements were successfully met to use the alternate improved collider accuracy code path.
        /// </summary>
        /// <remarks>
        /// The requirements are a single Sphere Collider component and no Rigidbody component on this GameObject.
        /// </remarks>
        /// <seealso cref="ImproveAccuracyWithSphereCollider"/>
        public bool UsingSphereColliderAccuracyImprovement { get; private set; }

        /// <summary>
        /// Physics layer mask used for limiting direct interactor overlaps when using the <seealso cref="ImproveAccuracyWithSphereCollider"/> option.
        /// </summary>
        public LayerMask PhysicsLayerMask
        {
            get => _physicsLayerMask;
            set => _physicsLayerMask = value;
        }
        
        /// <summary>
        /// Determines whether the direct interactor sphere overlap will hit triggers when using the <seealso cref="ImproveAccuracyWithSphereCollider"/> option.
        /// </summary>
        public QueryTriggerInteraction PhysicsTriggerInteraction
        {
            get => _physicsTriggerInteraction;
            set => _physicsTriggerInteraction = value;
        }
        
        /// <summary>
        /// The set of Interactables that this Interactor could possibly interact with this frame.
        /// This list is not sorted by priority.
        /// </summary>
        /// <seealso cref="IXRInteractor.GetValidTargets"/>
        protected List<IXRInteractable> UnsortedValidTargets { get; } = new List<IXRInteractable>();
        #endregion

        #region Fields
        /// <summary>
        /// Reusable value of <see cref="WaitForFixedUpdate"/> to reduce allocations.
        /// </summary>
        private static readonly WaitForFixedUpdate s_WaitForFixedUpdate = new();
        
        /// <summary>
        /// The set of Colliders that stayed in touch with this Interactor on fixed updated.
        /// This list will be populated by colliders in <c>OnTriggerStay</c>.
        /// </summary>
        private readonly HashSet<Collider> _stayedColliders = new();

        private readonly TriggerContactMonitor _triggerContactMonitor = new();
        
        /// <summary>
        /// Reference to Coroutine that updates the trigger contact monitor with the current
        /// stayed colliders.
        /// </summary>
        private IEnumerator _updateCollidersAfterTriggerStay;

        private SphereCollider _sphereCollider;
        private PhysicsScene _localPhysicsScene;
        private Vector3 _lastSphereCastOrigin = Vector3.zero;
        private readonly Collider[] _overlapSphereHits = new Collider[25];
        private readonly RaycastHit[] _sphereCastHits = new RaycastHit[25];
        private bool _firstFrame = true;
        private bool _contactsSortedThisFrame;
        private readonly List<IXRInteractable> _sortedValidTargets = new();
        #endregion

        #region - Initialization -
        protected override void Awake()
        {
            base.Awake();
            _localPhysicsScene = gameObject.scene.GetPhysicsScene();
            _triggerContactMonitor.interactionManager = InteractionManager;
            _updateCollidersAfterTriggerStay = UpdateCollidersAfterOnTriggerStay();
            ValidateColliderConfiguration();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _triggerContactMonitor.contactAdded += OnContactAdded;
            _triggerContactMonitor.contactRemoved += OnContactRemoved;
            ResetCollidersAndValidTargets();

            if (!UsingSphereColliderAccuracyImprovement)
                StartCoroutine(_updateCollidersAfterTriggerStay);
        }
        
        protected override void OnDisable()
        {
            base.OnDisable();
            _triggerContactMonitor.contactAdded -= OnContactAdded;
            _triggerContactMonitor.contactRemoved -= OnContactRemoved;
            ResetCollidersAndValidTargets();

            if (!UsingSphereColliderAccuracyImprovement)
                StopCoroutine(_updateCollidersAfterTriggerStay);
        }
        
        public override void OnRegistered(InteractorRegisteredEventArgs args)
        {
            base.OnRegistered(args);
            args.manager.interactableRegistered += OnInteractableRegistered;
            args.manager.interactableUnregistered += OnInteractableUnregistered;
            _triggerContactMonitor.interactionManager = args.manager;

            if (!UsingSphereColliderAccuracyImprovement)
            {
                // Attempt to resolve any colliders that entered this trigger while this was not subscribed,
                // and filter out any targets that were unregistered while this was not subscribed.
                _triggerContactMonitor.ResolveUnassociatedColliders();
                VXRInteractionManager.RemoveAllUnregistered(args.manager, UnsortedValidTargets);
            }
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
            _triggerContactMonitor.ResolveUnassociatedColliders(interactable);
            if (_triggerContactMonitor.IsContacting(interactable))
                OnContactAdded(interactable);
        }

        private void OnInteractableUnregistered(InteractableUnregisteredEventArgs args)
        {
            OnContactRemoved(args.interactableObject);
        }
        #endregion

        #region - Processing -
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);
            if (UsingSphereColliderAccuracyImprovement && updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
                EvaluateSphereOverlap();
        }

        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractor(updatePhase);

            if (!UsingSphereColliderAccuracyImprovement && updatePhase == XRInteractionUpdateOrder.UpdatePhase.Fixed)
            {
                // Clear stayed Colliders at the beginning of the physics cycle before
                // the OnTriggerStay method populates this list.
                // Then the UpdateCollidersAfterOnTriggerStay coroutine will use this list to remove Colliders
                // that no longer stay in this frame after previously entered and add any stayed Colliders
                // that are not currently tracked by the TriggerContactMonitor.
                _stayedColliders.Clear();
            }
        }
        #endregion

        #region - Contacts -
        protected void OnTriggerEnter(Collider other)
        {
            if (UsingSphereColliderAccuracyImprovement)
                return;

            _triggerContactMonitor.AddCollider(other);
        }

        protected void OnTriggerStay(Collider other)
        {
            if (UsingSphereColliderAccuracyImprovement)
                return;

            _stayedColliders.Add(other);
        }

        protected void OnTriggerExit(Collider other)
        {
            if (UsingSphereColliderAccuracyImprovement)
                return;

            _triggerContactMonitor.RemoveCollider(other);
        }
        
        /// <summary>
        /// This coroutine functions like a LateFixedUpdate method that executes after OnTriggerXXX.
        /// </summary>
        /// <returns>Returns enumerator for coroutine.</returns>
        private IEnumerator UpdateCollidersAfterOnTriggerStay()
        {
            while (true)
            {
                // Wait until the end of the physics cycle so that OnTriggerXXX can get called.
                // See https://docs.unity3d.com/Manual/ExecutionOrder.html
                yield return s_WaitForFixedUpdate;

                _triggerContactMonitor.UpdateStayedColliders(_stayedColliders);
            }
            // ReSharper disable once IteratorNeverReturns -- stopped when behavior is destroyed.
        }

        private void EvaluateSphereOverlap()
        {
            _contactsSortedThisFrame = false;
            _stayedColliders.Clear();

            Transform directAttachTransform = GetAttachTransform(null);
            // Hover Check
            Vector3 interactorPosition = directAttachTransform.TransformPoint(_sphereCollider.center);
            Vector3 overlapStart = _lastSphereCastOrigin;
            Vector3 interFrameEnd = interactorPosition;
            float grabRadius = _sphereCollider.radius * _sphereCollider.transform.lossyScale.x;

            BurstPhysicsUtils.GetSphereOverlapParameters(overlapStart, interFrameEnd, out var normalizedOverlapVector, out var overlapSqrMagnitude, out var overlapDistance);

            // If no movement is recorded.
            // Check if sphere cast size is sufficient for proper cast, or if first frame since last frame poke position will be invalid.
            if (_firstFrame || overlapSqrMagnitude < 0.001f)
            {
                var numberOfOverlaps = _localPhysicsScene.OverlapSphere(interFrameEnd, grabRadius, _overlapSphereHits,
                    _physicsLayerMask, _physicsTriggerInteraction);

                for (var i = 0; i < numberOfOverlaps; ++i)
                {
                    _stayedColliders.Add(_overlapSphereHits[i]);
                }
            }
            else
            {
                var numberOfOverlaps = _localPhysicsScene.SphereCast(
                    overlapStart,
                    grabRadius,
                    normalizedOverlapVector,
                    _sphereCastHits,
                    overlapDistance,
                    _physicsLayerMask,
                    _physicsTriggerInteraction);

                for (var i = 0; i < numberOfOverlaps; ++i)
                {
                    _stayedColliders.Add(_overlapSphereHits[i]);
                }
            }

            _triggerContactMonitor.UpdateStayedColliders(_stayedColliders);

            _lastSphereCastOrigin = interactorPosition;
            _firstFrame = false;
        }

        private void ValidateColliderConfiguration()
        {
            // If there isn't a Rigidbody on the same GameObject, a Trigger Collider has to be on this GameObject
            // for OnTriggerEnter, OnTriggerStay, and OnTriggerExit to be called by Unity. When this has a Rigidbody, Colliders can be
            // on child GameObjects and they don't necessarily have to be Trigger Colliders.
            // See Collision action matrix https://docs.unity3d.com/Manual/CollidersOverview.html
            if (!TryGetComponent(out Rigidbody _))
            {
                var colliders = GetComponents<Collider>();

                // If we don't have a Rigidbody and we only have 1 collider that is a Sphere Collider, we can use that to optimize the direct interactor.
                if (_improveAccuracyWithSphereCollider &&
                    colliders.Length == 1 && colliders[0] is SphereCollider sphereCollider)
                {
                    _sphereCollider = sphereCollider;

                    // Disable collider as only its radius is used.
                    _sphereCollider.enabled = false;
                    UsingSphereColliderAccuracyImprovement = true;
                    return;
                }

                var hasTriggerCollider = false;
                foreach (var col in colliders)
                {
                    if (col.isTrigger)
                    {
                        hasTriggerCollider = true;
                        break;
                    }
                }

                if (!hasTriggerCollider)
                    Debug.LogWarning("Direct Interactor does not have required Collider set as a trigger.", this);
            }
        }
        
        private void OnContactAdded(IXRInteractable interactable)
        {
            if (UnsortedValidTargets.Contains(interactable))
                return;

            UnsortedValidTargets.Add(interactable);
            _contactsSortedThisFrame = false;
        }

        private void OnContactRemoved(IXRInteractable interactable)
        {
            if (UnsortedValidTargets.Remove(interactable))
                _contactsSortedThisFrame = false;
        }

        /// <summary>
        /// Clears current valid targets and stayed colliders.
        /// </summary>
        private void ResetCollidersAndValidTargets()
        {
            UnsortedValidTargets.Clear();
            _sortedValidTargets.Clear();
            _contactsSortedThisFrame = false;
            _firstFrame = true;
            _stayedColliders.Clear();
            _triggerContactMonitor.UpdateStayedColliders(_stayedColliders);
        }
        #endregion

        #region - Hovering -
        public override bool CanHover(IXRHoverInteractable interactable)
        {
            return base.CanHover(interactable) && (!HasSelection || IsSelecting(interactable));
        }
        #endregion

        #region - Selection -
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return base.CanSelect(interactable) && (!HasSelection || IsSelecting(interactable));
        }
        #endregion

        #region - Helpers
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            targets.Clear();

            if (!isActiveAndEnabled)
                return;

            var filter = TargetFilter;
            if (filter != null && filter.CanProcess)
                filter.Process(this, UnsortedValidTargets, targets);
            else
            {
                // If not using the filter, we can cache the sorting of valid targets until the next time PreprocessInteractor is executed.
                if (_contactsSortedThisFrame)
                {
                    targets.AddRange(_sortedValidTargets);
                    return;
                }

                // Sort valid targets
                SortingHelpers.SortByDistanceToInteractor(this, UnsortedValidTargets, _sortedValidTargets);

                targets.AddRange(_sortedValidTargets);
                _contactsSortedThisFrame = true;
            }
        }
        #endregion
        
    }
}
