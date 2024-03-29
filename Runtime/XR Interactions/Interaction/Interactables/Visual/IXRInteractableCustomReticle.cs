﻿using VaporXR.Interaction;

namespace VaporXR
{
    /// <summary>
    /// Interface for a component on a <see cref="XRBaseInteractable.customReticle"/> for an interactable.
    /// </summary>
    public interface IXRInteractableCustomReticle
    {
        /// <summary>
        /// Called by the <paramref name="interactable"/> after it instantiates the custom reticle and attaches it
        /// to the <paramref name="reticleProvider"/>.
        /// </summary>
        /// <param name="interactable">The interactable that instantiated the custom reticle.</param>
        /// <param name="reticleProvider">The object to which the custom reticle was attached.</param>
        void OnReticleAttached(Interactable interactable, IXRCustomReticleProvider reticleProvider);

        /// <summary>
        /// Called by the interactable before it detaches the custom reticle and destroys it.
        /// </summary>
        void OnReticleDetaching();
    }
}