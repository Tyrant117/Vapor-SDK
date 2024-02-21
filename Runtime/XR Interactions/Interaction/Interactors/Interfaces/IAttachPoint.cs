using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporXR.Interaction;

namespace VaporXR
{
    public interface IAttachPoint
    {
#pragma warning disable IDE1006 // Naming Styles
        Transform transform { get; }
#pragma warning restore IDE1006 // Naming Styles

        Transform AttachPoint { get; }

        /// <summary>
        /// Gets the <see cref="Transform"/> that is used as the attachment point for a given Interactable.
        /// </summary>
        /// <param name="interactable">The specific Interactable as context to get the attachment point for.</param>
        /// <returns>Returns the attachment point <see cref="Transform"/>.</returns>
        /// <seealso cref="Interactable.GetAttachTransform"/>
        /// <remarks>
        /// This should typically return the Transform of a child GameObject or the <see cref="transform"/> itself.
        /// </remarks>
        Transform GetAttachTransform(Interactable interactable);
    }
}
