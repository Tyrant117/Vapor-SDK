using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using Vapor.Utilities;
using VaporXR.Interaction;

namespace VaporXR.Locomotion.Teleportation
{
    /// <summary>
    /// This is intended to be the base class for all Teleportation Interactables. This abstracts the teleport request process for specializations of this class.
    /// </summary>
    public abstract class BaseTeleportationInteractable : InteractableModule, IXRReticleDirectionProvider
    {
        /// <summary>
        /// Indicates when the teleportation action happens.
        /// </summary>
        public enum TeleportTriggerType
        {
            /// <summary>
            /// Teleportation occurs once selection is released without being canceled.
            /// </summary>
            OnSelectExited,

            /// <summary>
            /// Teleportation occurs right when area is selected.
            /// </summary>
            OnSelectEntered,

            /// <summary>
            /// Teleportation occurs on activate.
            /// </summary>
            OnActivated,

            /// <summary>
            /// Teleportation occurs on deactivate.
            /// </summary>
            OnDeactivated,
        }

        const float k_DefaultNormalToleranceDegrees = 30f;

        [SerializeField]
        [Tooltip("The teleportation provider that this teleportation interactable will communicate teleport requests to." +
            " If no teleportation provider is configured, will attempt to find a teleportation provider.")]
        TeleportationProvider _teleportationProvider;

        /// <summary>
        /// The teleportation provider that this teleportation interactable communicates teleport requests to.
        /// If no teleportation provider is configured, will attempt to find a teleportation provider.
        /// </summary>
        public TeleportationProvider TeleportationProvider
        {
            get => _teleportationProvider;
            set => _teleportationProvider = value;
        }

        [SerializeField]
        [Tooltip("How to orient the rig after teleportation." +
            "\nSet to:" +
            "\n\nWorld Space Up to stay oriented according to the world space up vector." +
            "\n\nSet to Target Up to orient according to the target BaseTeleportationInteractable Transform's up vector." +
            "\n\nSet to Target Up And Forward to orient according to the target BaseTeleportationInteractable Transform's rotation." +
            "\n\nSet to None to maintain the same orientation before and after teleporting.")]
        private MatchOrientation _matchOrientation = MatchOrientation.WorldSpaceUp;

        /// <summary>
        /// How to orient the rig after teleportation.
        /// </summary>
        /// <remarks>
        /// Set to:
        /// <list type="bullet">
        /// <item>
        /// <term><see cref="MatchOrientation.WorldSpaceUp"/></term>
        /// <description> to stay oriented according to the world space up vector.</description>
        /// </item>
        /// <item>
        /// <term><see cref="MatchOrientation.TargetUp"/></term>
        /// <description> to orient according to the target <see cref="BaseTeleportationInteractable"/> Transform's up vector.</description>
        /// </item>
        /// <item>
        /// <term><see cref="MatchOrientation.TargetUpAndForward"/></term>
        /// <description> to orient according to the target <see cref="BaseTeleportationInteractable"/> Transform's rotation.</description>
        /// </item>
        /// <item>
        /// <term><see cref="MatchOrientation.None"/></term>
        /// <description> to maintain the same orientation before and after teleporting.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public MatchOrientation MatchOrientation { get => _matchOrientation; set => _matchOrientation = value; }

        [SerializeField]
        [Tooltip("Whether or not to rotate the rig to match the forward direction of the attach transform of the selecting interactor.")]
        bool _matchDirectionalInput;

        /// <summary>
        /// Whether or not to rotate the rig to match the forward direction of the attach transform of the selecting interactor.
        /// This only applies when <see cref="MatchOrientation"/> is set to <see cref="MatchOrientation.WorldSpaceUp"/> or <see cref="MatchOrientation.TargetUp"/>.
        /// </summary>
        public bool MatchDirectionalInput { get => _matchDirectionalInput; set => _matchDirectionalInput = value; }

        [SerializeField]
        [Tooltip("Specify when the teleportation will be triggered. Options map to when the trigger is pressed or when it is released.")]
        TeleportTriggerType _teleportTrigger = TeleportTriggerType.OnSelectExited;

        /// <summary>
        /// Specifies when the teleportation triggers.
        /// </summary>
        public TeleportTriggerType TeleportTrigger { get => _teleportTrigger; set => _teleportTrigger = value; }

        [SerializeField]
        [Tooltip("When enabled, this teleportation interactable will only be selectable by a ray interactor if its current " +
                 "hit normal is aligned with this object's up vector.")]
        bool m_FilterSelectionByHitNormal;

        /// <summary>
        /// When set to <see langword="true"/>, this teleportation interactable will only be selectable by a ray interactor if its current
        /// hit normal is aligned with this object's up vector.
        /// </summary>
        /// <seealso cref="upNormalToleranceDegrees"/>
        public bool filterSelectionByHitNormal
        {
            get => m_FilterSelectionByHitNormal;
            set => m_FilterSelectionByHitNormal = value;
        }

        [SerializeField]
        [Tooltip("Sets the tolerance in degrees from this object's up vector for a hit normal to be considered aligned with the up vector.")]
        float m_UpNormalToleranceDegrees = k_DefaultNormalToleranceDegrees;

        /// <summary>
        /// The tolerance in degrees from this object's up vector for a hit normal to be considered aligned with the up vector.
        /// </summary>
        /// <seealso cref="filterSelectionByHitNormal"/>
        public float upNormalToleranceDegrees
        {
            get => m_UpNormalToleranceDegrees;
            set => m_UpNormalToleranceDegrees = value;
        }

        [SerializeField]
        TeleportingEvent m_Teleporting = new TeleportingEvent();

        /// <summary>
        /// Gets or sets the event that Unity calls when queuing to teleport via
        /// the <see cref="Teleportation.TeleportationProvider"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="TeleportingEventArgs"/> passed to each listener is only valid
        /// while the event is invoked, do not hold a reference to it.
        /// </remarks>
        public TeleportingEvent teleporting
        {
            get => m_Teleporting;
            set => m_Teleporting = value;
        }

        // Reusable event args
        readonly LinkedPool<TeleportingEventArgs> m_TeleportingEventArgs = new(() => new TeleportingEventArgs(), collectionCheck: false);

        readonly Dictionary<Interactor, Vector3> m_TeleportForwardPerInteractor = new();

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            if (_teleportationProvider == null)
            {
                ComponentLocatorUtility<TeleportationProvider>.TryFindComponent(out _teleportationProvider);
            }
        }

        protected virtual void OnEnable()
        {
            Interactable.SelectEntered += OnSelectEntered;
            Interactable.SelectExited += OnSelectExited;
        }

        protected virtual void OnDisable()
        {
            Interactable.SelectEntered -= OnSelectEntered;
            Interactable.SelectExited -= OnSelectExited;
        }

        protected virtual void Reset()
        {
            Interactable.SelectMode = InteractableSelectMode.Multiple;
        }

        /// <summary>
        /// Automatically called upon the teleport trigger event occurring to generate the teleport request.
        /// The teleportation destination pose should be filled out.
        /// </summary>
        /// <param name="interactor">The interactor that initiated the teleport trigger.</param>
        /// <param name="raycastHit">The ray cast hit information from the interactor.</param>
        /// <param name="teleportRequest">The teleport request that should be filled out during this method call.</param>
        /// <returns>Returns <see langword="true"/> if the teleport request was successfully updated and should be queued. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="TeleportationProvider.QueueTeleportRequest"/>
        protected virtual bool GenerateTeleportRequest(Interactor interactor, RaycastHit raycastHit, ref TeleportRequest teleportRequest) => false;

        private void SendTeleportRequest(Interactor interactor)
        {
            Debug.Log($"Sending Teleport Request From {name}. Triggered By Interactor {interactor.name}");
            if (interactor == null)
            {
                return;
            }

            if (_teleportationProvider == null && !ComponentLocatorUtility<TeleportationProvider>.TryFindComponent(out _teleportationProvider))
            {
                return;
            }

            RaycastHit raycastHit = default;
            if (interactor.TryGetModule<RayInteractorModule>(out var rayInteractor))
            {
                // Are we still selecting this object and within the tolerated normal threshold?
                if (!rayInteractor.TryGetCurrent3DRaycastHit(out raycastHit) ||
                    !InteractionManager.TryGetInteractableForCollider(raycastHit.collider, out var hitInteractable) ||
                    hitInteractable != Interactable ||
                    (m_FilterSelectionByHitNormal && Vector3.Angle(transform.up, raycastHit.normal) > m_UpNormalToleranceDegrees))
                {
                    return;
                }
            }

            var teleportRequest = new TeleportRequest
            {
                matchOrientation = _matchOrientation,
                requestTime = Time.time,
            };

            var success = GenerateTeleportRequest(interactor, raycastHit, ref teleportRequest);

            if (success)
            {
                UpdateTeleportRequestRotation(interactor, ref teleportRequest);
                success = _teleportationProvider.QueueTeleportRequest(teleportRequest);

                if (success && m_Teleporting != null)
                {
                    using (m_TeleportingEventArgs.Get(out var args))
                    {
                        args.InteractorObject = interactor;
                        args.InteractableObject = Interactable;
                        args.TeleportRequest = teleportRequest;
                        m_Teleporting.Invoke(args);
                    }
                }
            }
        }

        void UpdateTeleportRequestRotation(Interactor interactor, ref TeleportRequest teleportRequest)
        {
            if (!_matchDirectionalInput || !m_TeleportForwardPerInteractor.TryGetValue(interactor, out var forward))
                return;

            switch (teleportRequest.matchOrientation)
            {
                case MatchOrientation.WorldSpaceUp:
                    teleportRequest.destinationRotation = Quaternion.LookRotation(forward, Vector3.up);

                    // Change the match orientation value to request that the teleportation provider should apply the destination rotation with the directional input.
                    teleportRequest.matchOrientation = MatchOrientation.TargetUpAndForward;
                    break;

                case MatchOrientation.TargetUp:
                    teleportRequest.destinationRotation = Quaternion.LookRotation(forward, transform.up);

                    // Change the match orientation value to request that the teleportation provider should apply the destination rotation with the directional input.
                    teleportRequest.matchOrientation = MatchOrientation.TargetUpAndForward;
                    break;
            }
        }

        public override void PreProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreProcessInteractable(updatePhase);

            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic || !_matchDirectionalInput)
                return;

            // Update the reticle direction for each interactor that is hovering or selecting this interactable.
            for (int index = 0, count = Interactable.InteractorsHovering.Count; index < count; ++index)
            {
                var interactorHovering = Interactable.InteractorsHovering[index];
                CalculateTeleportForward(interactorHovering);
            }

            for (int index = 0, count = Interactable.InteractorsSelecting.Count; index < count; ++index)
            {
                var interactorSelecting = Interactable.InteractorsSelecting[index];
                // Skip if also hovered by the interactor since it would have already been computed above.
                if (Interactable.IsHoveredBy(interactorSelecting))
                {
                    continue;
                }

                CalculateTeleportForward(interactorSelecting);
            }

            void CalculateTeleportForward(Interactor interactor)
            {
                var attachTransform = interactor.GetAttachTransform(Interactable);
                switch (MatchOrientation)
                {
                    case MatchOrientation.WorldSpaceUp:
                        m_TeleportForwardPerInteractor[interactor] = Vector3.ProjectOnPlane(attachTransform.forward, Vector3.up).normalized;
                        break;

                    case MatchOrientation.TargetUp:
                        m_TeleportForwardPerInteractor[interactor] = Vector3.ProjectOnPlane(attachTransform.forward, transform.up).normalized;
                        break;
                }
            }
        }

        public virtual void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (_teleportTrigger == TeleportTriggerType.OnSelectEntered)
            {
                SendTeleportRequest(args.InteractorObject);
            }
        }

        public virtual void OnSelectExited(SelectExitEventArgs args)
        {
            if (_teleportTrigger == TeleportTriggerType.OnSelectExited && !args.IsCanceled)
            {
                SendTeleportRequest(args.InteractorObject);
            }
        }

        public virtual void OnActivated(ActivateEventArgs args)
        {
            if (_teleportTrigger == TeleportTriggerType.OnActivated)
            {
                SendTeleportRequest(args.InteractorObject);
            }
        }

        public virtual void OnDeactivated(DeactivateEventArgs args)
        {
            if (_teleportTrigger == TeleportTriggerType.OnDeactivated)
            {
                SendTeleportRequest(args.InteractorObject);
            }
        }

        public override bool IsSelectableBy(Interactor interactor)
        {
            var isSelectable = base.IsSelectableBy(interactor);
            if (isSelectable && m_FilterSelectionByHitNormal &&
                interactor.TryGetModule<RayInteractorModule>(out var rayInteractor) && 
                rayInteractor.TryGetCurrent3DRaycastHit(out var raycastHit) &&
                InteractionManager.TryGetInteractableForCollider(raycastHit.collider, out var hitInteractable) &&
                hitInteractable == Interactable)
            {
                // The ray interactor should only be able to select if its current hit is this interactable
                // and the hit normal is within the tolerated threshold.
                isSelectable &= Vector3.Angle(transform.up, raycastHit.normal) <= m_UpNormalToleranceDegrees;
            }

            return isSelectable;
        }

        public void GetReticleDirection(Interactor interactor, Vector3 hitNormal, out Vector3 reticleUp, out Vector3? optionalReticleForward)
        {
            optionalReticleForward = null;
            reticleUp = hitNormal;
            Vector3 reticleForward;
            var xrOrigin = TeleportationProvider.Mediator.XROrigin;
            switch (MatchOrientation)
            {
                case MatchOrientation.WorldSpaceUp:
                    reticleUp = Vector3.up;
                    if (_matchDirectionalInput && m_TeleportForwardPerInteractor.TryGetValue(interactor, out reticleForward))
                        optionalReticleForward = reticleForward;
                    else if (xrOrigin != null)
                        optionalReticleForward = xrOrigin.Camera.transform.forward;
                    break;

                case MatchOrientation.TargetUp:
                    reticleUp = transform.up;
                    if (_matchDirectionalInput && m_TeleportForwardPerInteractor.TryGetValue(interactor, out reticleForward))
                        optionalReticleForward = reticleForward;
                    else if (xrOrigin != null)
                        optionalReticleForward = xrOrigin.Camera.transform.forward;
                    break;

                case MatchOrientation.TargetUpAndForward:
                    reticleUp = transform.up;
                    optionalReticleForward = transform.forward;
                    break;

                case MatchOrientation.None:
                    if (xrOrigin != null)
                    {
                        reticleUp = xrOrigin.Origin.transform.up;
                        optionalReticleForward = xrOrigin.Camera.transform.forward;
                    }
                    break;

                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(Teleportation.MatchOrientation)}={MatchOrientation}.");
                    break;
            }
        }
    }
}
