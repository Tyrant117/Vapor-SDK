using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporXR.Interaction;

namespace VaporXR.Interaction
{
    /// <summary>
    /// Extension methods for <see cref="IXRHoverInteractor"/>.
    /// </summary>
    /// <seealso cref="IXRHoverInteractor"/>
    public static class HoverInteractorExtensions
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
        public static Interactable GetOldestInteractableHovered(this Interactor interactor) =>
            interactor.InteractablesHovered.Count > 0 ? interactor.InteractablesHovered[0] : null;
    }

    /// <summary>
    /// Extension methods for <see cref="IXRSelectInteractor"/>.
    /// </summary>
    /// <seealso cref="IXRSelectInteractor"/>
    public static class XRSelectInteractorExtensions
    {
        /// <summary>
        /// Gets the oldest Interactable currently selected.
        /// This is a convenience method for when the Interactor does not support selecting multiple interactables at a time.
        /// </summary>
        /// <param name="interactor">The Interactor to operate on.</param>
        /// <returns>Returns the oldest Interactable currently selected.</returns>
        /// <remarks>
        /// Equivalent to <code>interactablesSelected.Count > 0 ? interactablesSelected[0] : null</code>
        /// </remarks>
        /// <seealso cref="IXRSelectInteractor.InteractablesSelected"/>
        public static Interactable GetOldestInteractableSelected(this Interactor interactor) =>
            interactor.InteractablesSelected.Count > 0 ? interactor.InteractablesSelected[0] : null;        
    }
}
