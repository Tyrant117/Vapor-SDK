using System;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Interactors;
using VaporXR.Utilities;

namespace VaporXR
{
    public abstract class VXRSorter : MonoBehaviour, IAttachPoint
    {
        #region Inspectors              
        [FoldoutGroup("Components"), SerializeField]
        protected Transform _attachPoint;        
        #endregion

        #region Properties
        /// <summary>
        /// The nearest <see cref="IVXRInteractable"/> object hit by the ray that was inserted into the valid targets
        /// list when not selecting anything.
        /// </summary>
        /// <remarks>
        /// Updated during <see cref="PreprocessInteractor"/>.
        /// </remarks>
        public IVXRInteractable CurrentNearestValidTarget { get; protected set; }

        /// <summary>
        /// The set of Interactables that this Interactor could possibly interact with this frame.
        /// This list is not sorted by priority.
        /// </summary>
        /// <seealso cref="IXRInteractor.GetValidTargets"/>
        public List<IVXRInteractable> PossibleTargets { get; } = new();

        public Transform AttachPoint { get => _attachPoint; set => _attachPoint = value; }
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
        
        protected readonly List<IVXRInteractable> _sortedValidTargets = new();
        protected readonly List<IVXRInteractable> _frameValidTargets = new();        
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
            _contactMonitor.ResolveUnassociatedColliders(args.interactableObject);
            if (_contactMonitor.IsContacting(args.interactableObject) && !PossibleTargets.Contains(args.interactableObject))
            {
                PossibleTargets.Add(args.interactableObject);
            }
        }

        public void OnInteractableUnregistered(InteractableUnregisteredEventArgs args)
        {
            PossibleTargets.Remove(args.interactableObject);
        }
        #endregion

        #region - Interaction -
        public void ProcessContacts(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
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

        public abstract IVXRInteractable ProcessSorter(IVXRInteractor interactor, IXRTargetFilter filter = null);

        public abstract void GetValidTargets(IVXRInteractor interactor, List<IVXRInteractable> targets, IXRTargetFilter filter = null);        

        public Transform GetAttachTransform(IVXRInteractable interactable)
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
        /// <seealso cref="IVXRInteractor.InteractionLayers"/>
        /// <seealso cref="IVXRInteractable.InteractionLayers"/>
        protected static bool HasInteractionLayerOverlap(IVXRInteractor interactor, IVXRInteractable interactable)
        {
            return (interactor.InteractionLayers & interactable.InteractionLayers) != 0;
        }
        #endregion

        #region - Contacts -
        protected abstract void EvaluateContacts();       

        protected abstract void OnContactAdded(IVXRInteractable interactable);
        protected abstract void OnContactRemoved(IVXRInteractable interactable);

        public virtual void ManualAddTarget(IVXRInteractable interactable)
        {
            if (PossibleTargets.Contains(interactable))
            {
                return;
            }

            PossibleTargets.Add(interactable);
        }

        public virtual bool ManualRemoveTarget(IVXRInteractable interactable)
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
