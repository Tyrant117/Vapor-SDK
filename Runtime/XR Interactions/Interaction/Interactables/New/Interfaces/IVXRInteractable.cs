using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporXR.Interactables;

namespace VaporXR
{
    public interface IVXRInteractable
    {
#pragma warning disable IDE1006 // Naming Styles
        public Transform transform { get; }
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Overriding callback of this object's distance calculation.
        /// Use this to change the calculation performed in <see cref="GetDistance"/> without needing to create a derived class.
        /// <br />
        /// When a callback is assigned to this property, the <see cref="GetDistance"/> execution calls it to perform the
        /// distance calculation instead of using its default calculation (specified by <see cref="DistanceCalculationMode"/> in this base class).
        /// Assign <see langword="null"/> to this property to restore the default calculation.
        /// </summary>
        /// <remarks>
        /// The assigned callback will be invoked to calculate and return the distance information of the point on this
        /// Interactable (the first parameter) closest to the given location (the second parameter).
        /// The given location and returned distance information are in world space.
        /// </remarks>
        /// <seealso cref="GetDistance"/>
        /// <seealso cref="DistanceInfo"/>
        Func<IVXRInteractable, Vector3, DistanceInfo> GetDistanceOverride { get; set; }

        /// <summary>
        /// (Read Only) Colliders to use for interaction with this Interactable (if empty, will use any child Colliders).
        /// </summary>
        List<Collider> Colliders { get; }
        InteractionLayerMask InteractionLayers { get; set; }
        VXRCompositeInteractable Composite { get; }

        /// <summary>
        /// Calls the methods in its invocation list when this Interactable is registered with an Interaction Manager.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractableRegisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.interactableRegistered"/>
        event Action<InteractableRegisteredEventArgs> Registered;

        /// <summary>
        /// Calls the methods in its invocation list when this Interactable is unregistered from an Interaction Manager.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractableUnregisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.interactableUnregistered"/>
        event Action<InteractableUnregisteredEventArgs> Unregistered;

        void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase);

        /// <summary>
        /// Gets the <see cref="Transform"/> that serves as the attachment point for a given Interactor.
        /// </summary>
        /// <param name="attachPoint">The specific attachment point for.</param>
        /// <returns>Returns the attachment point <see cref="Transform"/>.</returns>
        /// <seealso cref="VXRBaseInteractor.GetAttachTransform"/>
        /// <remarks>
        /// This should typically return the Transform of a child GameObject or the <see cref="transform"/> itself.
        /// </remarks>
        Transform GetAttachTransform(IAttachPoint attachPoint);

        /// <remarks>
        /// This method calls the <see cref="GetDistance"/> method to perform the distance calculation.
        /// </remarks>
        float GetDistanceSqrToInteractor(IAttachPoint attachPoint);
        /// <summary>
        /// Gets the distance from this Interactable to the given location.
        /// This method uses the calculation mode configured in <see cref="DistanceCalculationMode"/>.
        /// <br />
        /// This method can be overridden (without needing to subclass) by assigning a callback to <see cref="GetDistanceOverride"/>.
        /// To restore the previous calculation mode configuration, assign <see langword="null"/> to <see cref="GetDistanceOverride"/>.
        /// </summary>
        /// <param name="position">Location in world space to calculate the distance to.</param>
        /// <returns>Returns the distance information (in world space) from this Interactable to the given location.</returns>
        /// <remarks>
        /// This method is used by other methods and systems to calculate this Interactable distance to other objects and
        /// locations (<see cref="GetDistanceSqrToInteractor(VXRBaseInteractor)"/>).
        /// </remarks>
        DistanceInfo GetDistance(Vector3 position);
        void OnRegistered(InteractableRegisteredEventArgs args);
        void OnUnregistered(InteractableUnregisteredEventArgs args);
    }
}
