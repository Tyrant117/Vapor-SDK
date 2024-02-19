using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Utilities;

namespace VaporXR.Interactables
{
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Interactables)]
    public class VXRInteractable : MonoBehaviour, IVXRInteractable
    {
        #region Inspector
        [FoldoutGroup("Components"), SerializeField]
        [RichTextTooltip("")]
        private VXRCompositeInteractable _composite;
        [FoldoutGroup("Components"), SerializeField]
        [RichTextTooltip("Colliders to use for interaction with this Interactable (if empty, will use any child Colliders).")]
        private List<Collider> _colliders = new();

        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("Allows interaction with Interactors whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.")]
        private InteractionLayerMask _interactionLayers = 1;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("Specifies how this Interactable calculates its distance to a location, either using its Transform position, Collider position or Collider volume.")]
        private DistanceCalculationModeType _distanceCalculationMode = DistanceCalculationModeType.InteractionPointPosition;
        #endregion

        #region Properties
        private VXRInteractionManager _interactionManager;
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> that this Interactable will communicate with (will find one if <see langword="null"/>).
        /// </summary>
        public VXRInteractionManager InteractionManager
        {
            get => _interactionManager;
            set
            {
                _interactionManager = value;
                if (Application.isPlaying && isActiveAndEnabled)
                {
                    RegisterWithInteractionManager();
                }
            }
        }

        public VXRCompositeInteractable Composite { get => _composite; protected set => _composite = value; }

        public List<Collider> Colliders => _colliders;

        /// <summary>
        /// Allows interaction with Interactors whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.
        /// </summary>
        /// <seealso cref="VXRBaseInteractor.InteractionLayers"/>
        /// <seealso cref="IsHoverableBy(IVXRHoverInteractor)"/>
        /// <seealso cref="IsSelectableBy(IVXRSelectInteractor)"/>
        /// <inheritdoc />
        public InteractionLayerMask InteractionLayers { get => _interactionLayers; set => _interactionLayers = value; }
        #endregion

        #region Fields        
        private VXRInteractionManager _registeredInteractionManager;
        #endregion

        #region Events        
        public event Action<InteractableRegisteredEventArgs> Registered;        
        public event Action<InteractableUnregisteredEventArgs> Unregistered;
        public Func<IVXRInteractable, Vector3, DistanceInfo> GetDistanceOverride { get; set; }
        #endregion

        #region - Initialization -
        protected virtual void Awake()
        {
            // If no colliders were set, populate with children colliders
            if (_colliders.Count == 0)
            {
                GetComponentsInChildren(_colliders);
                // Skip any that are trigger colliders since these are usually associated with snap volumes.
                // If a user wants to use a trigger collider, they must serialize the reference manually.
                _colliders.RemoveAll(col => col.isTrigger);
            }

            // Setup Interaction Manager
            FindCreateInteractionManager();
        }

        protected virtual void OnEnable()
        {
            FindCreateInteractionManager();
            RegisterWithInteractionManager();
        }

        protected virtual void OnDisable()
        {
            UnregisterWithInteractionManager();
        }

        private void FindCreateInteractionManager()
        {
            if (_interactionManager != null)
            {
                return;
            }

            _interactionManager = ComponentLocatorUtility<VXRInteractionManager>.FindOrCreateComponent();
        }

        private void RegisterWithInteractionManager()
        {
            if (_registeredInteractionManager == _interactionManager)
            {
                return;
            }

            UnregisterWithInteractionManager();

            if (_interactionManager != null)
            {
                _interactionManager.RegisterInteractable(this);
                _registeredInteractionManager = _interactionManager;
            }
        }

        private void UnregisterWithInteractionManager()
        {
            if (_registeredInteractionManager != null)
            {
                _registeredInteractionManager.UnregisterInteractable(this);
                _registeredInteractionManager = null;
            }
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when this Interactable is registered with it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that registered this Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.RegisterInteractable(IVXRInteractable)"/>
        public virtual void OnRegistered(InteractableRegisteredEventArgs args)
        {
            if (args.manager != _interactionManager)
            {
                Debug.LogWarning($"An Interactable was registered with an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{_interactionManager}\" but was registered with \"{args.manager}\".", this);
            }

            Registered?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when this Interactable is unregistered from it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that unregistered this Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.UnregisterInteractable(IVXRInteractable)"/>
        public virtual void OnUnregistered(InteractableUnregisteredEventArgs args)
        {
            if (args.manager != _registeredInteractionManager)
            {
                Debug.LogWarning($"An Interactable was unregistered from an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{_registeredInteractionManager}\" but was unregistered from \"{args.manager}\".", this);
            }

            Unregistered?.Invoke(args);
        }
        #endregion

        #region - Processing -
        public virtual void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {

        }
        #endregion

        #region - Interaction -
        public virtual Transform GetAttachTransform(IAttachPoint attachPoint)
        {
            return transform;
        }
        #endregion

        #region - Helper -        
        public virtual float GetDistanceSqrToInteractor(IAttachPoint attachPoint)
        {
            if (attachPoint == null)
            {
                return float.MaxValue;
            }

            var interactorAttachTransform = attachPoint.GetAttachTransform(this);
            if (interactorAttachTransform == null)
            {
                return float.MaxValue;
            }

            var interactorPosition = interactorAttachTransform.position;
            var distanceInfo = GetDistance(interactorPosition);
            return distanceInfo.distanceSqr;
        }
        
        public virtual DistanceInfo GetDistance(Vector3 position)
        {
            if (GetDistanceOverride != null)
            {
                return GetDistanceOverride(this, position);
            }

            switch (_distanceCalculationMode)
            {
                case DistanceCalculationModeType.TransformPosition:
                    var thisObjectPosition = transform.position;
                    var offset = thisObjectPosition - position;
                    var distanceInfo = new DistanceInfo
                    {
                        point = thisObjectPosition,
                        distanceSqr = offset.sqrMagnitude
                    };
                    return distanceInfo;

                case DistanceCalculationModeType.InteractionPointPosition:
                    XRInteractableUtility.TryGetClosestInteractionPoint(this, position, out distanceInfo);
                    return distanceInfo;

                case DistanceCalculationModeType.ColliderPosition:
                    XRInteractableUtility.TryGetClosestCollider(this, position, out distanceInfo);
                    return distanceInfo;

                case DistanceCalculationModeType.ColliderVolume:
                    XRInteractableUtility.TryGetClosestPointOnCollider(this, position, out distanceInfo);
                    return distanceInfo;
                
                default:
                    Debug.Assert(false, $"Unhandled {nameof(DistanceCalculationModeType)}={_distanceCalculationMode}.", this);
                    goto case DistanceCalculationModeType.TransformPosition;
            }
        }
        #endregion
    }
}
