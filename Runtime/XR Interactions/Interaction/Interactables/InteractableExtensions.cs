using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporXR.Interaction;

namespace VaporXR.Interaction
{
    /// <summary>
    /// Extension methods for <see cref="IVXRHoverInteractable"/>.
    /// </summary>
    /// <seealso cref="IVXRHoverInteractable"/>
    public static class HoverInteractableExtensions
    {
        /// <summary>
        /// Gets the oldest interactor currently hovering on this interactable.
        /// This is a convenience method for when the interactable does not support being hovered by multiple interactors at a time.
        /// </summary>
        /// <param name="interactable">The interactable to operate on.</param>
        /// <returns>Returns the oldest interactor currently hovering on this interactable.</returns>
        /// <remarks>
        /// Equivalent to <code>interactorsHovering.Count > 0 ? interactorsHovering[0] : null</code>
        /// </remarks>
        /// <seealso cref="IVXRHoverInteractable.InteractorsHovering"/>
        public static Interactor GetOldestInteractorHovering(this Interactable interactable) =>
            interactable?.InteractorsHovering.Count > 0 ? interactable.InteractorsHovering[0] : null;

        /// <summary>
        /// Gets whether the interactable is currently being hovered by an interactor associated with the left hand or controller.
        /// </summary>
        /// <param name="interactable">The interactable to operate on.</param>
        /// <returns>Returns <see langword="true"/> if any interactor currently hovering this interactable has <see cref="IXRInteractor.Handedness"/> of <see cref="InteractorHandedness.Left"/>.</returns>
        /// <remarks>
        /// This method will return <see langword="true"/> even if it is not exclusively being hovered by the left hand or controller.
        /// In other words, it will still return <see langword="true"/> if the interactable is also being hovered by
        /// an interactor associated with the right hand or controller.
        /// </remarks>
        /// <seealso cref="IsHoveredByRight"/>
        /// <seealso cref="IXRInteractor.Handedness"/>
        public static bool IsHoveredByLeft(this Interactable interactable) =>
            IsHoveredBy(interactable, InteractorHandedness.Left);

        /// <summary>
        /// Gets whether the interactable is currently being hovered by an interactor associated with the right hand or controller.
        /// </summary>
        /// <param name="interactable">The interactable to operate on.</param>
        /// <returns>Returns <see langword="true"/> if any interactor currently hovering this interactable has <see cref="IXRInteractor.Handedness"/> of <see cref="InteractorHandedness.Right"/>.</returns>
        /// <remarks>
        /// This method will return <see langword="true"/> even if it is not exclusively being hovered by the right hand or controller.
        /// In other words, it will still return <see langword="true"/> if the interactable is also being hovered by
        /// an interactor associated with the left hand or controller.
        /// </remarks>
        /// <seealso cref="IsHoveredByLeft"/>
        /// <seealso cref="IXRInteractor.Handedness"/>
        public static bool IsHoveredByRight(this Interactable interactable) =>
            IsHoveredBy(interactable, InteractorHandedness.Right);

        private static bool IsHoveredBy(Interactable interactable, InteractorHandedness handedness)
        {
            var interactorsHovering = interactable.InteractorsHovering;
            for (var i = 0; i < interactorsHovering.Count; ++i)
            {
                if (interactorsHovering[i].Handedness == handedness)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Extension methods for <see cref="IVXRSelectInteractable"/>.
    /// </summary>
    /// <seealso cref="IVXRSelectInteractable"/>
    public static class SelectInteractableExtensions
    {
        /// <summary>
        /// Gets the oldest interactor currently selecting this interactable.
        /// This is a convenience method for when the interactable does not support being selected by multiple interactors at a time.
        /// </summary>
        /// <param name="interactable">The interactable to operate on.</param>
        /// <returns>Returns the oldest interactor currently selecting this interactable.</returns>
        /// <remarks>
        /// Equivalent to <code>interactorsSelecting.Count > 0 ? interactorsSelecting[0] : null</code>
        /// </remarks>
        /// <seealso cref="IVXRSelectInteractable.InteractorsSelecting"/>
        public static Interactor GetOldestInteractorSelecting(this Interactable interactable) =>
            interactable != null && interactable.InteractorsSelecting.Count > 0 ? interactable.InteractorsSelecting[0] : null;

        /// <summary>
        /// Gets whether the interactable is currently being selected by an interactor associated with the left hand or controller.
        /// </summary>
        /// <param name="interactable">The interactable to operate on.</param>
        /// <returns>Returns <see langword="true"/> if any interactor currently selecting this interactable has <see cref="IXRInteractor.Handedness"/> of <see cref="InteractorHandedness.Left"/>.</returns>
        /// <remarks>
        /// This method will return <see langword="true"/> even if it is not exclusively being selected by the left hand or controller.
        /// In other words, it will still return <see langword="true"/> if the interactable is also being selected by
        /// an interactor associated with the right hand or controller.
        /// </remarks>
        /// <seealso cref="IsSelectedByRight"/>
        /// <seealso cref="IXRInteractor.Handedness"/>
        public static bool IsSelectedByLeft(this Interactable interactable) =>
            IsSelectedBy(interactable, InteractorHandedness.Left);

        /// <summary>
        /// Gets whether the interactable is currently being selected by an interactor associated with the right hand or controller.
        /// </summary>
        /// <param name="interactable">The interactable to operate on.</param>
        /// <returns>Returns <see langword="true"/> if any interactor currently selecting this interactable has <see cref="IXRInteractor.Handedness"/> of <see cref="InteractorHandedness.Right"/>.</returns>
        /// <remarks>
        /// This method will return <see langword="true"/> even if it is not exclusively being selected by the right hand or controller.
        /// In other words, it will still return <see langword="true"/> if the interactable is also being selected by
        /// an interactor associated with the left hand or controller.
        /// </remarks>
        /// <seealso cref="IsSelectedByLeft"/>
        /// <seealso cref="IXRInteractor.Handedness"/>
        public static bool IsSelectedByRight(this Interactable interactable) =>
            IsSelectedBy(interactable, InteractorHandedness.Right);

        private static bool IsSelectedBy(Interactable interactable, InteractorHandedness handedness)
        {
            var interactorsSelecting = interactable.InteractorsSelecting;
            for (var i = 0; i < interactorsSelecting.Count; ++i)
            {
                if (interactorsSelecting[i].Handedness == handedness)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Extension methods for <see cref="IXRFocusInteractable"/>.
    /// </summary>
    /// <seealso cref="IXRFocusInteractable"/>
    public static class FocusInteractableExtensions
    {
        /// <summary>
        /// Gets the oldest interaction group currently focusing on this interactable.
        /// This is a convenience method for when the interactable does not support being focused by multiple interaction groups at a time.
        /// </summary>
        /// <param name="interactable">The interactable to operate on.</param>
        /// <returns>Returns the oldest interaction group currently focusing this interactable.</returns>
        /// <remarks>
        /// Equivalent to <code>interactionGroupsFocusing.Count > 0 ? interactionGroupsFocusing[0] : null</code>
        /// </remarks>
        public static IXRInteractionGroup GetOldestInteractorFocusing(this Interactable interactable) =>
            interactable != null && interactable.InteractionGroupsFocusing.Count > 0 ? interactable.InteractionGroupsFocusing[0] : null;
    }
}
