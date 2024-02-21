using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR.Interaction
{
    /// <summary>
    /// Options for the selection policy of an Interactable.
    /// </summary>
    /// <seealso cref="IVXRSelectInteractable.SelectMode"/>
    public enum InteractableSelectMode
    {
        /// <summary>
        /// Allows the Interactable to only be selected by a single Interactor at a time
        /// and allows other Interactors to take selection by automatically deselecting.
        /// </summary>
        Single,

        /// <summary>
        /// Allows for multiple Interactors at a time to select the Interactable.
        /// </summary>
        /// <remarks>
        /// This option can be disabled in the Inspector window by adding the <see cref="CanSelectMultipleAttribute"/>
        /// with a value of <see langword="false"/> to a derived class of <see cref="XRBaseInteractable"/>.
        /// </remarks>
        Multiple,
    }

    /// <summary>
    /// Options for the focus policy of an Interactable.
    /// </summary>
    /// <seealso cref="IXRFocusInteractable.FocusMode"/>
    public enum InteractableFocusMode
    {
        /// <summary>
        /// Focus not supported for this interactable.
        /// </summary>
        None,

        /// <summary>
        /// Allows the Interactable to only be focused by a single Interaction group at a time
        /// and allows other Interaction groups to take focus by automatically losing focus.
        /// </summary>
        Single,

        /// <summary>
        /// Allows for multiple Interaction groups at a time to focus the Interactable.
        /// </summary>
        /// <remarks>
        /// This option can be disabled in the Inspector window by adding the <see cref="CanFocusMultipleAttribute"/>
        /// with a value of <see langword="false"/> to a derived class of <see cref="XRBaseInteractable"/>.
        /// </remarks>
        Multiple,
    }
}
