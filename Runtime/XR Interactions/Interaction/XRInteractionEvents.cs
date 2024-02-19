﻿using System;
using UnityEngine;
using UnityEngine.Events;
using VaporXR.Interactors;
using VaporXR.Locomotion.Teleportation;

namespace VaporXR
{
    /// <summary>
    /// Event data associated with an interaction event between an Interactor and Interactable.
    /// </summary>
    public abstract class BaseInteractionEventArgs
    {
        /// <summary>
        /// The Interactor associated with the interaction event.
        /// </summary>
        public IVXRInteractor InteractorObject { get; set; }

        /// <summary>
        /// The Interactable associated with the interaction event.
        /// </summary>
        public IVXRInteractable InteractableObject { get; set; }
    }

    #region Teleport

    /// <summary>
    /// <see cref="UnityEvent"/> that Unity invokes when queuing to teleport via
    /// a <see cref="TeleportationProvider"/>.
    /// </summary>
    [Serializable]
    public sealed class TeleportingEvent : UnityEvent<TeleportingEventArgs>
    {
    }

    /// <summary>
    /// Event data associated with the event that Unity invokes during a selection or
    /// activation event between an Interactable and an Interactor, according to the
    /// timing defined by <see cref="BaseTeleportationInteractable.TeleportTrigger"/>.
    /// </summary>
    public class TeleportingEventArgs : BaseInteractionEventArgs
    {
        /// <summary>
        /// The <see cref="TeleportRequest"/> that is being queued, but has not been acted on yet.
        /// </summary>
        public TeleportRequest teleportRequest { get; set; }
    }

    #endregion

    #region Hover

    /// <summary>
    /// <see cref="UnityEvent"/> that Unity invokes when an Interactor initiates hovering over an Interactable.
    /// </summary>
    [Serializable]
    public sealed class HoverEnterEvent : UnityEvent<HoverEnterEventArgs>
    {
    }

    /// <summary>
    /// Event data associated with the event when an Interactor initiates hovering over an Interactable.
    /// </summary>
    public class HoverEnterEventArgs : BaseInteractionEventArgs
    {
        /// <summary>
        /// The Interactor associated with the interaction event.
        /// </summary>
        public new IVXRHoverInteractor interactorObject
        {
            get => (IVXRHoverInteractor)base.InteractorObject;
            set => base.InteractorObject = value;
        }

        /// <summary>
        /// The Interactable associated with the interaction event.
        /// </summary>
        public new IVXRHoverInteractable interactableObject
        {
            get => (IVXRHoverInteractable)base.InteractableObject;
            set => base.InteractableObject = value;
        }

        /// <summary>
        /// The Interaction Manager associated with the interaction event.
        /// </summary>
        public VXRInteractionManager manager { get; set; }
    }

    /// <summary>
    /// <see cref="UnityEvent"/> that Unity invokes when an Interactor ends hovering over an Interactable.
    /// </summary>
    [Serializable]
    public sealed class HoverExitEvent : UnityEvent<HoverExitEventArgs>
    {
    }

    /// <summary>
    /// Event data associated with the event when an Interactor ends hovering over an Interactable.
    /// </summary>
    public class HoverExitEventArgs : BaseInteractionEventArgs
    {
        /// <summary>
        /// The Interactor associated with the interaction event.
        /// </summary>
        public new IVXRHoverInteractor interactorObject
        {
            get => (IVXRHoverInteractor)base.InteractorObject;
            set => base.InteractorObject = value;
        }

        /// <summary>
        /// The Interactable associated with the interaction event.
        /// </summary>
        public new IVXRHoverInteractable interactableObject
        {
            get => (IVXRHoverInteractable)base.InteractableObject;
            set => base.InteractableObject = value;
        }

        /// <summary>
        /// The Interaction Manager associated with the interaction event.
        /// </summary>
        public VXRInteractionManager manager { get; set; }

        /// <summary>
        /// Whether the hover was ended due to being canceled, such as from
        /// either the Interactor or Interactable being unregistered due to being
        /// disabled or destroyed.
        /// </summary>
        public bool isCanceled { get; set; }
    }

    #endregion

    #region Select

    /// <summary>
    /// <see cref="UnityEvent"/> that Unity invokes when an Interactor initiates selecting an Interactable.
    /// </summary>
    [Serializable]
    public sealed class SelectEnterEvent : UnityEvent<SelectEnterEventArgs>
    {
    }

    /// <summary>
    /// Event data associated with the event when an Interactor initiates selecting an Interactable.
    /// </summary>
    public class SelectEnterEventArgs : BaseInteractionEventArgs
    {
        /// <summary>
        /// The Interactor associated with the interaction event.
        /// </summary>
        public new IVXRSelectInteractor InteractorObject
        {
            get => (IVXRSelectInteractor)base.InteractorObject;
            set => base.InteractorObject = value;
        }

        /// <summary>
        /// The Interactable associated with the interaction event.
        /// </summary>
        public new IVXRSelectInteractable InteractableObject
        {
            get => (IVXRSelectInteractable)base.InteractableObject;
            set => base.InteractableObject = value;
        }

        /// <summary>
        /// The Interaction Manager associated with the interaction event.
        /// </summary>
        public VXRInteractionManager Manager { get; set; }
    }

    /// <summary>
    /// <see cref="UnityEvent"/> that Unity invokes when an Interactor ends selecting an Interactable.
    /// </summary>
    [Serializable]
    public sealed class SelectExitEvent : UnityEvent<SelectExitEventArgs>
    {
    }

    /// <summary>
    /// Event data associated with the event when an Interactor ends selecting an Interactable.
    /// </summary>
    public class SelectExitEventArgs : BaseInteractionEventArgs
    {
        /// <summary>
        /// The Interactor associated with the interaction event.
        /// </summary>
        public new IVXRSelectInteractor InteractorObject
        {
            get => (IVXRSelectInteractor)base.InteractorObject;
            set => base.InteractorObject = value;
        }

        /// <summary>
        /// The Interactable associated with the interaction event.
        /// </summary>
        public IVXRSelectInteractable GetinteractableObject()
        {
            return (IVXRSelectInteractable)base.InteractableObject;
        }

        /// <summary>
        /// The Interactable associated with the interaction event.
        /// </summary>
        public void SetinteractableObject(IVXRSelectInteractable value)
        {
            base.InteractableObject = value;
        }

        /// <summary>
        /// The Interaction Manager associated with the interaction event.
        /// </summary>
        public VXRInteractionManager Manager { get; set; }

        /// <summary>
        /// Whether the selection was ended due to being canceled, such as from
        /// either the Interactor or Interactable being unregistered due to being
        /// disabled or destroyed.
        /// </summary>
        public bool IsCanceled { get; set; }
    }

    #endregion

    #region Focus

    /// <summary>
    /// <see cref="UnityEvent"/> that Unity invokes when an Interaction group initiates focusing an Interactable.
    /// </summary>
    [Serializable]
    public sealed class FocusEnterEvent : UnityEvent<FocusEnterEventArgs>
    {
    }

    /// <summary>
    /// Event data associated with the event when an Interaction group gains focus of an Interactable.
    /// </summary>
    public class FocusEnterEventArgs : BaseInteractionEventArgs
    {
        /// <summary>
        /// The interaction group associated with the interaction event.
        /// </summary>
        public IXRInteractionGroup interactionGroup { get; set; }

        /// <summary>
        /// The Interactable associated with the interaction event.
        /// </summary>
        public new IXRFocusInteractable interactableObject
        {
            get => (IXRFocusInteractable)base.InteractableObject;
            set => base.InteractableObject = value;
        }

        /// <summary>
        /// The Interaction Manager associated with the interaction event.
        /// </summary>
        public VXRInteractionManager manager { get; set; }
    }

    /// <summary>
    /// <see cref="UnityEvent"/> that Unity invokes when an Interaction group ends focusing an Interactable.
    /// </summary>
    [Serializable]
    public sealed class FocusExitEvent : UnityEvent<FocusExitEventArgs>
    {
    }

    /// <summary>
    /// Event data associated with the event when an Interaction group ends focusing an Interactable.
    /// </summary>
    public class FocusExitEventArgs : BaseInteractionEventArgs
    {
        /// <summary>
        /// The interaction group associated with the interaction event.
        /// </summary>
        public IXRInteractionGroup interactionGroup { get; set; }

        /// <summary>
        /// The Interactable associated with the interaction event.
        /// </summary>
        public new IXRFocusInteractable interactableObject
        {
            get => (IXRFocusInteractable)base.InteractableObject;
            set => base.InteractableObject = value;
        }

        /// <summary>
        /// The Interaction Manager associated with the interaction event.
        /// </summary>
        public VXRInteractionManager manager { get; set; }

        /// <summary>
        /// Whether the focus was lost due to being canceled, such as from
        /// either the Interaction group or Interactable being unregistered due to being
        /// disabled or destroyed.
        /// </summary>
        public bool isCanceled { get; set; }
    }

    #endregion

    #region Activate

    /// <summary>
    /// <see cref="UnityEvent"/> that Unity invokes when the selecting Interactor activates an Interactable.
    /// </summary>
    /// <remarks>
    /// Not to be confused with activating or deactivating a <see cref="GameObject"/> with <see cref="GameObject.SetActive"/>.
    /// This is a generic event when an Interactor wants to activate its selected Interactable,
    /// such as from a trigger pull on a controller.
    /// </remarks>
    [Serializable]
    public sealed class ActivateEvent : UnityEvent<ActivateEventArgs>
    {
    }

    /// <summary>
    /// Event data associated with the event when the selecting Interactor activates an Interactable.
    /// </summary>
    public class ActivateEventArgs : BaseInteractionEventArgs
    {
        /// <summary>
        /// The Interactor associated with the interaction event.
        /// </summary>
        public new IVXRInteractor interactorObject
        {
            get => (IVXRInteractor)base.InteractorObject;
            set => base.InteractorObject = value;
        }

        /// <summary>
        /// The Interactable associated with the interaction event.
        /// </summary>
        public new IXRActivateInteractable interactableObject
        {
            get => (IXRActivateInteractable)base.InteractableObject;
            set => base.InteractableObject = value;
        }
    }

    /// <summary>
    /// <see cref="UnityEvent"/> that Unity invokes when the selecting Interactor deactivates an Interactable.
    /// </summary>
    /// <remarks>
    /// Not to be confused with activating or deactivating a <see cref="GameObject"/> with <see cref="GameObject.SetActive"/>.
    /// This is a generic event when an Interactor wants to deactivate its selected Interactable,
    /// such as from a trigger pull on a controller.
    /// </remarks>
    [Serializable]
    public sealed class DeactivateEvent : UnityEvent<DeactivateEventArgs>
    {
    }

    /// <summary>
    /// Event data associated with the event when the selecting Interactor deactivates an Interactable.
    /// </summary>
    public class DeactivateEventArgs : BaseInteractionEventArgs
    {
        /// <summary>
        /// The Interactor associated with the interaction event.
        /// </summary>
        public new IVXRInteractor interactorObject
        {
            get => (IVXRInteractor)base.InteractorObject;
            set => base.InteractorObject = value;
        }

        /// <summary>
        /// The Interactable associated with the interaction event.
        /// </summary>
        public new IXRActivateInteractable interactableObject
        {
            get => (IXRActivateInteractable)base.InteractableObject;
            set => base.InteractableObject = value;
        }
    }

    #endregion

    #region Registration

    /// <summary>
    /// Event data associated with a registration event with an <see cref="VXRInteractionManager"/>.
    /// </summary>
    public abstract class BaseRegistrationEventArgs
    {
        /// <summary>
        /// The Interaction Manager associated with the registration event.
        /// </summary>
        public VXRInteractionManager manager { get; set; }
    }

    /// <summary>
    /// Event data associated with the event when an <see cref="IXRInteractionGroup"/> is registered with an <see cref="VXRInteractionManager"/>.
    /// </summary>
    public class InteractionGroupRegisteredEventArgs : BaseRegistrationEventArgs
    {
        /// <summary>
        /// The Interaction Group that was registered.
        /// </summary>
        public IXRInteractionGroup interactionGroupObject { get; set; }

        /// <summary>
        /// The Interaction Group that contains the registered Group. Will be <see langword="null"/> if there is no containing Group.
        /// </summary>
        public IXRInteractionGroup containingGroupObject { get; set; }
    }

    /// <summary>
    /// Event data associated with the event when an Interactor is registered with an <see cref="VXRInteractionManager"/>.
    /// </summary>
    public partial class InteractorRegisteredEventArgs : BaseRegistrationEventArgs
    {
        /// <summary>
        /// The Interactor that was registered.
        /// </summary>
        public IVXRInteractor interactorObject { get; set; }

        /// <summary>
        /// The Interaction Group that contains the registered Interactor. Will be <see langword="null"/> if there is no containing Group.
        /// </summary>
        public IXRInteractionGroup containingGroupObject { get; set; }
    }

    /// <summary>
    /// Event data associated with the event when an Interactable is registered with an <see cref="VXRInteractionManager"/>.
    /// </summary>
    public partial class InteractableRegisteredEventArgs : BaseRegistrationEventArgs
    {
        /// <summary>
        /// The Interactable that was registered.
        /// </summary>
        public IVXRInteractable interactableObject { get; set; }
    }

    /// <summary>
    /// Event data associated with the event when an Interaction Group is unregistered from an <see cref="VXRInteractionManager"/>.
    /// </summary>
    public class InteractionGroupUnregisteredEventArgs : BaseRegistrationEventArgs
    {
        /// <summary>
        /// The Interaction Group that was unregistered.
        /// </summary>
        public IXRInteractionGroup interactionGroupObject { get; set; }
    }

    /// <summary>
    /// Event data associated with the event when an Interactor is unregistered from an <see cref="VXRInteractionManager"/>.
    /// </summary>
    public partial class InteractorUnregisteredEventArgs : BaseRegistrationEventArgs
    {
        /// <summary>
        /// The Interactor that was unregistered.
        /// </summary>
        public IVXRInteractor interactorObject { get; set; }
    }

    /// <summary>
    /// Event data associated with the event when an Interactable is unregistered from an <see cref="VXRInteractionManager"/>.
    /// </summary>
    public partial class InteractableUnregisteredEventArgs : BaseRegistrationEventArgs
    {
        /// <summary>
        /// The Interactable that was unregistered.
        /// </summary>
        public IVXRInteractable interactableObject { get; set; }
    }

    #endregion
}
