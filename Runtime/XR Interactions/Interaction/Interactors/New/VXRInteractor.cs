using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Utilities;
using VaporInspector;
using Object = UnityEngine.Object;

namespace VaporXR.Interactors
{
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Interactors)]
    public abstract class VXRInteractor : MonoBehaviour, IVXRInteractor
    {
    
        [FoldoutGroup("Components"), SerializeField]
        private VXRCompositeInteractor _composite;
        [FoldoutGroup("Components"), SerializeField]
        private List<VXRSorter> _sorters;

        [FoldoutGroup("Interaction"), SerializeField]
        private InteractionLayerMask _interactionLayers = -1;
        [FoldoutGroup("Interaction"), SerializeField]
        private InteractorHandedness _handedness;
        [FoldoutGroup("Interaction"), SerializeField]
        private Transform _attachPoint;

        [SerializeField, FoldoutGroup("Filters", order: 90)]
        private XRBaseTargetFilter _startingTargetFilter;

        #region Properties
        private VXRInteractionManager _interactionManager;
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> that this Interactor will communicate with (will find one if <see langword="null"/>).
        /// </summary>
        public VXRInteractionManager InteractionManager
        {
            get => _interactionManager;
            set
            {
                _interactionManager = value;
                if (Application.isPlaying && isActiveAndEnabled)
                    RegisterWithInteractionManager();
            }
        }

        public VXRCompositeInteractor Composite => _composite;

        public List<VXRSorter> Sorters => _sorters;

        public VXRSorter OverrideSorter => _overrideSorter;

        /// <summary>
        /// (Read Only) Allows interaction with Interactables whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.
        /// </summary>
        /// <seealso cref="IVXRInteractable.InteractionLayers"/>
        public InteractionLayerMask InteractionLayers => _interactionLayers;

        /// <summary>
        /// (Read Only) Represents which hand or controller the interactor is associated with.
        /// </summary>
        public InteractorHandedness Handedness => _handedness;

        /// <summary>
        /// (Read Only) The <see cref="Transform"/> that is used as the attach point for Interactables.
        /// </summary>
        /// <remarks>
        /// Automatically instantiated and set in <see cref="Awake"/> if <see langword="null"/>.
        /// </remarks>
        public Transform AttachPoint => _attachPoint;

        private IXRTargetFilter _targetFilter;
        /// <summary>
        /// The Target Filter that this Interactor is linked to.
        /// </summary>
        /// <seealso cref="StartingTargetFilter"/>
        public IXRTargetFilter TargetFilter
        {
            get
            {
                return _targetFilter is Object unityObj && unityObj == null ? null : _targetFilter;
            }
            set
            {
                if (Application.isPlaying)
                {
                    TargetFilter?.Unlink(this);
                    _targetFilter = value;
                    TargetFilter?.Link(this);
                }
                else
                {
                    _targetFilter = value;
                }
            }
        }
        #endregion

        #region Fields
        private VXRInteractionManager _registeredInteractionManager;

        private Transform _vxrOriginTransform;
        private bool _hasVXROrigin;
        private bool _failedToFindVXROrigin;

        private VXRSorter _overrideSorter;
        private bool _useOverrideSorter;
        #endregion

        #region Events
        /// <summary>
        /// Calls the methods in its invocation list when this Interactor is registered with an Interaction Manager.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractorRegisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.interactorRegistered"/>
        public event Action<InteractorRegisteredEventArgs> Registered;

        /// <summary>
        /// Calls the methods in its invocation list when this Interactor is unregistered from an Interaction Manager.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractorUnregisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.interactorUnregistered"/>
        public event Action<InteractorUnregisteredEventArgs> Unregistered;
        #endregion

        #region - Initialization -
        protected virtual void Awake()
        {
            // Create empty attach transform if none specified
            CreateAttachTransform();

            // Setup the starting filters
            if (_startingTargetFilter != null)
            {
                TargetFilter = _startingTargetFilter;
            }

            // Setup Interaction Manager
            FindCreateInteractionManager();
        }

        protected virtual void OnEnable()
        {
            FindCreateInteractionManager();
            RegisterWithInteractionManager();
        }

        private void FindCreateInteractionManager()
        {
            if (_interactionManager == null)
            {
                _interactionManager = ComponentLocatorUtility<VXRInteractionManager>.FindOrCreateComponent();
            }
        }

        private void RegisterWithInteractionManager()
        {
            if (_registeredInteractionManager == _interactionManager)
            {
                return;
            }

            UnregisterWithInteractionManager();

            if (_interactionManager == null)
            {
                return;
            }

            _interactionManager.RegisterInteractor(this);
            _registeredInteractionManager = _interactionManager;
        }

        private void UnregisterWithInteractionManager()
        {
            if (_registeredInteractionManager == null)
            {
                return;
            }

            _registeredInteractionManager.UnregisterInteractor(this);
            _registeredInteractionManager = null;
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when this Interactor is registered with it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that registered this Interactor.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.RegisterInteractor(IXRInteractor)"/>
        public virtual void OnRegistered(InteractorRegisteredEventArgs args)
        {
            if (args.manager != _interactionManager)
            {
                Debug.LogWarning($"An Interactor was registered with an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{_interactionManager}\" but was registered with \"{args.manager}\".", this);
            }

            Registered?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when this Interactor is unregistered from it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that unregistered this Interactor.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.UnregisterInteractor(IXRInteractor)"/>
        public virtual void OnUnregistered(InteractorUnregisteredEventArgs args)
        {
            if (args.manager != _registeredInteractionManager)
            {
                Debug.LogWarning($"An Interactor was unregistered from an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{_registeredInteractionManager}\" but was unregistered from \"{args.manager}\".", this);
            }

            Unregistered?.Invoke(args);
        }

        protected virtual void OnDisable()
        {
            UnregisterWithInteractionManager();
        }

        protected virtual void OnDestroy()
        {
            // Unlink this Interactor from the Target Filter
            TargetFilter?.Unlink(this);
        }
        #endregion

        #region - Processing -
        public virtual void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (_useOverrideSorter)
            {
                _overrideSorter.ProcessContacts(updatePhase);
            }
            else
            {
                foreach (var sorter in _sorters)
                {
                    sorter.ProcessContacts(updatePhase);
                }
            }
        }

        public virtual void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Late)
            {
                if (_useOverrideSorter)
                {
                    _overrideSorter.ProcessContacts(updatePhase);
                }
                else
                {
                    foreach (var sorter in _sorters)
                    {
                        sorter.ProcessContacts(updatePhase);
                    }
                }
            }
        }

        public void SetOverrideSorter(VXRSorter sorter)
        {
            _overrideSorter = sorter;
            _useOverrideSorter = _overrideSorter != null;
        }

        public virtual void GetValidTargets(List<IVXRInteractable> targets)
        {
            if (_useOverrideSorter)
            {
                _overrideSorter.GetValidTargets(this, targets, TargetFilter);
            }
            else
            {
                foreach (var sorter in _sorters)
                {
                    sorter.GetValidTargets(this, targets, TargetFilter);
                }
            }
        }
        #endregion

        #region - Attaching -
        /// <summary>
        /// Create a new child GameObject to use as the attach transform if one is not set.
        /// </summary>
        /// <seealso cref="AttachTransform"/>
        protected void CreateAttachTransform()
        {
            if (_attachPoint == null)
            {
                _attachPoint = new GameObject($"[{gameObject.name}] Attach").transform;
                _attachPoint.SetParent(transform, false);
                _attachPoint.localPosition = Vector3.zero;
                _attachPoint.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Gets the <see cref="Transform"/> that is used as the attachment point for a given Interactable.
        /// </summary>
        /// <param name="interactable">The specific Interactable as context to get the attachment point for.</param>
        /// <returns>Returns the attachment point <see cref="Transform"/>.</returns>
        /// <seealso cref="IVXRInteractable.GetAttachTransform"/>
        /// <remarks>
        /// This should typically return the Transform of a child GameObject or the <see cref="transform"/> itself.
        /// </remarks>
        public virtual Transform GetAttachTransform(IVXRInteractable interactable)
        {
            return _attachPoint;
        }
        #endregion

        #region - Helpers -
        /// <summary>
        /// Attempts to locate and return the XR Origin reference frame for the interactor.
        /// </summary>
        /// <seealso cref="XROrigin"/>
        public bool TryGetXROrigin(out Transform origin)
        {
            if (_hasVXROrigin)
            {
                origin = _vxrOriginTransform;
                return true;
            }

            if (!_failedToFindVXROrigin)
            {
                var xrOrigin = GetComponentInParent<VXROrigin>();
                if (xrOrigin != null)
                {
                    var originGo = xrOrigin.Origin;
                    if (originGo != null)
                    {
                        _vxrOriginTransform = originGo.transform;
                        _hasVXROrigin = true;
                        origin = _vxrOriginTransform;
                        return true;
                    }
                }
                _failedToFindVXROrigin = true;
            }
            origin = null;
            return false;
        }

        public bool TryGetSelectInteractor(out IVXRSelectInteractor interactor)
        {
            interactor = null;
            if (Composite != null)
            {
                interactor = Composite.Select;
                return interactor != null;
            }
            else
            {
                return false;
            }
        }

        public bool TryGetHoverInteractor(out IVXRHoverInteractor interactor)
        {
            interactor = null;
            if (Composite != null)
            {
                interactor = Composite.Hover;
                return interactor != null;
            }
            else
            {
                return false;
            }
        }
        #endregion
    }
}
