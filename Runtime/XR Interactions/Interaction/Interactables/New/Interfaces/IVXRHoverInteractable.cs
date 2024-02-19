using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporXR.Interactors;

namespace VaporXR
{
    public interface IVXRHoverInteractable : IVXRInteractable
    {
        bool CanBeHovered { get; }
        bool IsHovered { get; }
        List<IVXRHoverInteractor> InteractorsHovering { get; }
        /// <summary>
        /// The list of hover filters in this object.
        /// Used as additional hover validations for this Interactable.
        /// </summary>
        /// <remarks>
        /// While processing hover filters, all changes to this list don't have an immediate effect. These changes are
        /// buffered and applied when the processing is finished.
        /// Calling <see cref="IXRFilterList{T}.MoveTo"/> in this list will throw an exception when this list is being processed.
        /// </remarks>
        /// <seealso cref="ProcessHoverFilters"/>
        IXRFilterList<IXRHoverFilter> HoverFilters { get; }

        event Action<HoverEnterEventArgs> FirstHoverEntered;
        event Action<HoverEnterEventArgs> HoverEntering;
        event Action<HoverEnterEventArgs> HoverEntered;
        event Action<HoverExitEventArgs> HoverExiting;
        event Action<HoverExitEventArgs> HoverExited;
        event Action<HoverExitEventArgs> LastHoverExited;

        /// <summary>
        /// Determines if a given Interactor can hover over this Interactable.
        /// </summary>
        /// <param name="interactor">Interactor to check for a valid hover state with.</param>
        /// <returns>Returns <see langword="true"/> if hovering is valid this frame. Returns <see langword="false"/> if not.</returns>
        /// <seealso cref="VXRBaseInteractor.CanHover"/>
        bool IsHoverableBy(IVXRHoverInteractor interactor);
        /// <summary>
        /// Determines whether this Interactable is currently being hovered by the Interactor.
        /// </summary>
        /// <param name="interactor">Interactor to check.</param>
        /// <returns>Returns <see langword="true"/> if this Interactable is currently being hovered by the Interactor.
        /// Otherwise, returns <seealso langword="false"/>.</returns>
        /// <remarks>
        /// In other words, returns whether <see cref="InteractorsHovering"/> contains <paramref name="interactor"/>.
        /// </remarks>
        /// <seealso cref="InteractorsHovering"/>
        bool IsHoveredBy(IVXRHoverInteractor interactor);

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// right before the Interactor first initiates hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is initiating the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverEntered(HoverEnterEventArgs)"/>
        void OnHoverEntering(HoverEnterEventArgs args);

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interactor first initiates hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is initiating the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverExited(HoverExitEventArgs)"/>        
        void OnHoverEntered(HoverEnterEventArgs args);

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// right before the Interactor ends hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is ending the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverExited(HoverExitEventArgs)"/>
        void OnHoverExiting(HoverExitEventArgs args);

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interactor ends hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is ending the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverEntered(HoverEnterEventArgs)"/>
        void OnHoverExited(HoverExitEventArgs args);
    }
}
