using System;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    /// <summary>
    /// An interface that represents an Interactable component that controls how a GameObject
    /// interacts with an Interactor component. An example is a Grab Interactable which
    /// can be picked up and moved by an Interactor.
    /// </summary>
    /// <remarks>
    /// When scripting, you can typically write custom behaviors that derive from <see cref="XRBaseInteractable"/>
    /// or one of its derived classes rather than implementing this interface directly.
    /// </remarks>
    /// <seealso cref="XRBaseInteractable"/>
    /// <seealso cref="IXRActivateInteractable"/>
    /// <seealso cref="IXRHoverInteractable"/>
    /// <seealso cref="IXRSelectInteractable"/>
    /// <seealso cref="VXRBaseInteractor"/>
    public interface IXRInteractable
    {
        /// <summary>
        /// Calls the methods in its invocation list when this Interactable is registered with an <see cref="VXRInteractionManager"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractableRegisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.interactableRegistered"/>
        event Action<InteractableRegisteredEventArgs> Registered;

        /// <summary>
        /// Calls the methods in its invocation list when this Interactable is unregistered from an <see cref="VXRInteractionManager"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractableUnregisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.interactableUnregistered"/>
        event Action<InteractableUnregisteredEventArgs> Unregistered;

        /// <summary>
        /// (Read Only) Allows interaction with Interactors whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.
        /// </summary>
        /// <seealso cref="VXRBaseInteractor.InteractionLayers"/>
        InteractionLayerMask InteractionLayers { get; }

        /// <summary>
        /// (Read Only) Colliders to use for interaction with this Interactable.
        /// </summary>
        List<Collider> Colliders { get; }

        /// <summary>
        /// (Read Only) The <see cref="Transform"/> associated with the Interactable.
        /// </summary>
        /// <remarks>
        /// When this Interactable is a component, this property is the Transform of the GameObject the component is attached to.
        /// </remarks>
#pragma warning disable IDE1006 // Naming Styles
        Transform transform { get; }
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Gets the <see cref="Transform"/> that serves as the attachment point for a given Interactor.
        /// </summary>
        /// <param name="interactor">The specific Interactor as context to get the attachment point for.</param>
        /// <returns>Returns the attachment point <see cref="Transform"/>.</returns>
        /// <seealso cref="VXRBaseInteractor.GetAttachTransform"/>
        /// <remarks>
        /// This should typically return the Transform of a child GameObject or the <see cref="transform"/> itself.
        /// </remarks>
        Transform GetAttachTransform(VXRBaseInteractor interactor);

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when this Interactable is registered with it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that registered this Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.RegisterInteractable(IXRInteractable)"/>
        void OnRegistered(InteractableRegisteredEventArgs args);

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when this Interactable is unregistered from it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that unregistered this Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.UnregisterInteractable(IXRInteractable)"/>
        void OnUnregistered(InteractableUnregisteredEventArgs args);

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method to update the Interactable.
        /// </summary>
        /// <param name="updatePhase">The update phase this is called during.</param>
        /// <remarks>
        /// Please see the <see cref="VXRInteractionManager"/> and <see cref="XRInteractionUpdateOrder.UpdatePhase"/> documentation for more
        /// details on update order.
        /// </remarks>
        /// <seealso cref="XRInteractionUpdateOrder.UpdatePhase"/>
        /// <seealso cref="VXRBaseInteractor.ProcessInteractor"/>
        void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase);

        /// <summary>
        /// Calculates squared distance to an Interactor (based on colliders).
        /// </summary>
        /// <param name="interactor">Interactor to calculate distance against.</param>
        /// <returns>Returns the minimum squared distance between the Interactor and this Interactable's colliders.</returns>
        float GetDistanceSqrToInteractor(VXRBaseInteractor interactor);
    }
}