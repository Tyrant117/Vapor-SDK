using System.Collections.Generic;
using UnityEngine;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Interaction;
using VaporXR.Utilities;

namespace VaporXR
{
    public abstract class VXRSorter : MonoBehaviour, IAttachPoint
    {
        #region Inspectors              
        [FoldoutGroup("Components"), SerializeField]
        protected Transform _attachPoint;

        [FoldoutGroup("Properties"), SerializeField]
        private bool _isActive = true;
        #endregion

        #region Properties
        /// <summary>
        /// The nearest <see cref="Interactable"/> object hit by the ray that was inserted into the valid targets
        /// list when not selecting anything.
        /// </summary>
        /// <remarks>
        /// Updated during <see cref="PreprocessInteractor"/>.
        /// </remarks>
        public Interactable CurrentNearestValidTarget { get; protected set; }

        /// <summary>
        /// The set of Interactables that this Interactor could possibly interact with this frame.
        /// This list is not sorted by priority.
        /// </summary>
        /// <seealso cref="IXRInteractor.GetValidTargets"/>
        public List<Interactable> PossibleTargets { get; } = new();

        public Transform AttachPoint { get => _attachPoint; set => _attachPoint = value; }
        
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    if (value)
                    {
                        _contactMonitor.ResolveUnassociatedColliders();
                    }

                    ResetCollidersAndValidTargets();
                }
                _isActive = value;
            }
        }
        #endregion

        #region Fields
        protected VXRInteractionManager _interactionManager;
        /// <summary>
        /// The set of Colliders that stayed in touch with this Interactor.
        /// </summary>
        protected readonly HashSet<Collider> _stayedColliders = new();

        protected readonly TriggerContactMonitor _contactMonitor = new();

        protected PhysicsScene _localPhysicsScene;
        protected bool _frameContactsEvaulated;
        
        protected readonly List<Interactable> _sortedValidTargets = new();
        protected readonly List<Interactable> _frameValidTargets = new();        
        #endregion

        #region - Initialization -
        protected virtual void Awake()
        {
            _localPhysicsScene = gameObject.scene.GetPhysicsScene();
            if (!_attachPoint)
            {
                _attachPoint = transform;
            }
            _FindCreateInteractionManager();

            _contactMonitor.interactionManager = _interactionManager;

            void _FindCreateInteractionManager()
            {
                if (_interactionManager == null)
                {
                    _interactionManager = ComponentLocatorUtility<VXRInteractionManager>.FindOrCreateComponent();
                }
            }
        }

        protected virtual void OnEnable()
        {
            _contactMonitor.contactAdded += OnContactAdded;
            _contactMonitor.contactRemoved += OnContactRemoved;

            _contactMonitor.ResolveUnassociatedColliders();
            ResetCollidersAndValidTargets();
        }

        protected virtual void OnDisable()
        {
            _contactMonitor.contactAdded -= OnContactAdded;
            _contactMonitor.contactRemoved -= OnContactRemoved;

            ResetCollidersAndValidTargets();
        }

        public void OnInteractableRegistered(InteractableRegisteredEventArgs args)
        {
            _contactMonitor.ResolveUnassociatedColliders(args.InteractableObject);
            if (_contactMonitor.IsContacting(args.InteractableObject) && !PossibleTargets.Contains(args.InteractableObject))
            {
                PossibleTargets.Add(args.InteractableObject);
            }
        }

        public void OnInteractableUnregistered(InteractableUnregisteredEventArgs args)
        {
            PossibleTargets.Remove(args.InteractableObject);
        }
        #endregion

        #region - Interaction -
        public void ProcessContacts(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (!IsActive) { return; }

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                if (_frameContactsEvaulated)
                {
                    return;
                }

                EvaluateContacts();
                _frameContactsEvaulated = true;
            }
            else if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Late)
            {
                _frameContactsEvaulated = false;
            }
        }

        public abstract Interactable ProcessSorter(Interactor interactor, IXRTargetFilter filter = null);

        public abstract void GetValidTargets(Interactor interactor, List<Interactable> targets, IXRTargetFilter filter = null);

        public Transform GetAttachTransform(Interactable interactable)
        {
            return _attachPoint;
        }

        /// <summary>
        /// Determines whether the Interactor and Interactable share at least one interaction layer
        /// between their Interaction Layer Masks.
        /// </summary>
        /// <param name="interactor">The Interactor to check.</param>
        /// <param name="interactable">The Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if the Interactor and Interactable share at least one interaction layer. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="Interactor.InteractionLayers"/>
        /// <seealso cref="Interactable.InteractionLayers"/>
        protected static bool HasInteractionLayerOverlap(Interactor interactor, Interactable interactable)
        {
            foreach (var layer in interactable.InteractionLayers)
            {
                if (interactor.InteractionLayers.Contains(layer))
                {
                    return true;
                }
            }
            return false;

            //return (interactor.InteractionLayers & interactable.InteractionLayers) != 0;
        }
        #endregion

        #region - Contacts -
        protected abstract void EvaluateContacts();       

        protected abstract void OnContactAdded(Interactable interactable);
        protected abstract void OnContactRemoved(Interactable interactable);

        public virtual void ManualAddTarget(Interactable interactable)
        {
            if (PossibleTargets.Contains(interactable))
            {
                return;
            }

            PossibleTargets.Add(interactable);
        }

        public virtual bool ManualRemoveTarget(Interactable interactable)
        {
            if (PossibleTargets.Remove(interactable))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears current valid targets and stayed colliders.
        /// </summary>
        protected abstract void ResetCollidersAndValidTargets();                
        #endregion
    }
}
