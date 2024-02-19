using System;
using System.Collections.Generic;
using VaporXR.Interactors;

namespace VaporXR
{
    /// <summary>
    /// An interface that represents an Interactor component that can hover over
    /// an Interactable component.
    /// </summary>
    /// <seealso cref="IVXRHoverInteractable"/>
    //public interface IXRHoverInteractor : IXRInteractor
    //{
    //    /// <summary>
    //    /// The event that is called when this Interactor begins hovering over an Interactable.
    //    /// </summary>
    //    /// <remarks>
    //    /// The <see cref="HoverEnterEventArgs"/> passed to each listener is only valid while the event is invoked,
    //    /// do not hold a reference to it.
    //    /// </remarks>
    //    /// <seealso cref="HoverExited"/>
    //    event Action<HoverEnterEventArgs> HoverEntered;

    //    /// <summary>
    //    /// The event that is called when this Interactor ends hovering over an Interactable.
    //    /// </summary>
    //    /// <remarks>
    //    /// The <see cref="HoverExitEventArgs"/> passed to each listener is only valid while the event is invoked,
    //    /// do not hold a reference to it.
    //    /// </remarks>
    //    /// <seealso cref="HoverEntered"/>
    //    event Action<HoverExitEventArgs> HoverExited;

    //    /// <summary>
    //    /// (Read Only) The list of Interactables that are currently being hovered over (may by empty).
    //    /// </summary>
    //    /// <remarks>
    //    /// You should treat this as a read only view of the list and should not modify it.
    //    /// It is exposed as a <see cref="List{T}"/> rather than an <see cref="IReadOnlyList{T}"/> to avoid GC Allocations
    //    /// when enumerating the list.
    //    /// </remarks>
    //    /// <seealso cref="HasHover"/>
    //    /// <seealso cref="IXRHoverInteractable.InteractorsHovering"/>
    //    List<IXRHoverInteractable> InteractablesHovered { get; }

    //    /// <summary>
    //    /// (Read Only) Indicates whether this Interactor is currently hovering an Interactable.
    //    /// </summary>
    //    /// <remarks>
    //    /// In other words, returns whether <see cref="InteractablesHovered"/> contains any Interactables.
    //    /// <example>
    //    /// <code>interactablesHovered.Count > 0</code>
    //    /// </example>
    //    /// </remarks>
    //    /// <seealso cref="InteractablesHovered"/>
    //    /// <seealso cref="IXRHoverInteractable.IsHovered"/>
    //    bool HasHover { get; }

    //    /// <summary>
    //    /// (Read Only) Indicates whether this Interactor is in a state where it could hover.
    //    /// </summary>
    //    bool IsHoverActive { get; }

    //    /// <summary>
    //    /// Determines if the Interactable is valid for hover this frame.
    //    /// </summary>
    //    /// <param name="interactable">Interactable to check.</param>
    //    /// <returns>Returns <see langword="true"/> if the interactable can be hovered over this frame.</returns>
    //    /// <seealso cref="IXRHoverInteractable.IsHoverableBy"/>
    //    bool CanHover(IXRHoverInteractable interactable);

    //    /// <summary>
    //    /// Determines whether this Interactor is currently hovering the Interactable.
    //    /// </summary>
    //    /// <param name="interactable">Interactable to check.</param>
    //    /// <returns>Returns <see langword="true"/> if this Interactor is currently hovering the Interactable.
    //    /// Otherwise, returns <seealso langword="false"/>.</returns>
    //    /// <remarks>
    //    /// In other words, returns whether <see cref="InteractablesHovered"/> contains <paramref name="interactable"/>.
    //    /// </remarks>
    //    /// <seealso cref="InteractablesHovered"/>
    //    bool IsHovering(IXRHoverInteractable interactable);

    //    /// <summary>
    //    /// The <see cref="VXRInteractionManager"/> calls this method
    //    /// right before the Interactor first initiates hovering over an Interactable
    //    /// in a first pass.
    //    /// </summary>
    //    /// <param name="args">Event data containing the Interactable that is being hovered over.</param>
    //    /// <remarks>
    //    /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
    //    /// </remarks>
    //    /// <seealso cref="OnHoverEntered(HoverEnterEventArgs)"/>
    //    void OnHoverEntering(HoverEnterEventArgs args);

    //    /// <summary>
    //    /// The <see cref="VXRInteractionManager"/> calls this method
    //    /// when the Interactor first initiates hovering over an Interactable
    //    /// in a second pass.
    //    /// </summary>
    //    /// <param name="args">Event data containing the Interactable that is being hovered over.</param>
    //    /// <remarks>
    //    /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
    //    /// </remarks>
    //    /// <seealso cref="OnHoverExited(HoverExitEventArgs)"/>
    //    void OnHoverEntered(HoverEnterEventArgs args);

    //    /// <summary>
    //    /// The <see cref="VXRInteractionManager"/> calls this method
    //    /// right before the Interactor ends hovering over an Interactable
    //    /// in a first pass.
    //    /// </summary>
    //    /// <param name="args">Event data containing the Interactable that is no longer hovered over.</param>
    //    /// <remarks>
    //    /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
    //    /// </remarks>
    //    /// <seealso cref="OnHoverExited(HoverExitEventArgs)"/>
    //    void OnHoverExiting(HoverExitEventArgs args);

    //    /// <summary>
    //    /// The <see cref="VXRInteractionManager"/> calls this method
    //    /// when the Interactor ends hovering over an Interactable
    //    /// in a second pass.
    //    /// </summary>
    //    /// <param name="args">Event data containing the Interactable that is no longer hovered over.</param>
    //    /// <remarks>
    //    /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
    //    /// </remarks>
    //    /// <seealso cref="OnHoverEntered(HoverEnterEventArgs)"/>
    //    void OnHoverExited(HoverExitEventArgs args);
    //}

    /// <summary>
    /// Extension methods for <see cref="IXRHoverInteractor"/>.
    /// </summary>
    /// <seealso cref="IXRHoverInteractor"/>
    public static class XRHoverInteractorExtensions
    {
        /// <summary>
        /// Gets the oldest interactable currently being hovered over.
        /// This is a convenience method for when the interactor does not support hovering multiple interactables at a time.
        /// </summary>
        /// <param name="interactor">The interactor to operate on.</param>
        /// <returns>Returns the oldest interactable currently being hovered over.</returns>
        /// <remarks>
        /// Equivalent to <code>interactablesHovered.Count > 0 ? interactablesHovered[0] : null</code>
        /// </remarks>
        /// <seealso cref="IXRHoverInteractor.InteractablesHovered"/>
        public static IVXRHoverInteractable GetOldestInteractableHovered(this VXRBaseInteractor interactor) =>
            interactor.InteractablesHovered.Count > 0 ? interactor.InteractablesHovered[0] : null;

        /// <summary>
        /// Gets the oldest interactable currently being hovered over.
        /// This is a convenience method for when the interactor does not support hovering multiple interactables at a time.
        /// </summary>
        /// <param name="interactor">The interactor to operate on.</param>
        /// <returns>Returns the oldest interactable currently being hovered over.</returns>
        /// <remarks>
        /// Equivalent to <code>interactablesHovered.Count > 0 ? interactablesHovered[0] : null</code>
        /// </remarks>
        /// <seealso cref="IXRHoverInteractor.InteractablesHovered"/>
        public static IVXRHoverInteractable GetOldestInteractableHovered(this VXRHoverInteractor interactor) =>
            interactor.InteractablesHovered.Count > 0 ? interactor.InteractablesHovered[0] : null;
    }
}