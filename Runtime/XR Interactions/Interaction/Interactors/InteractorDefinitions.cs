using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    /// <summary>
    /// Enum used to determine how the socket should scale the interactable.
    /// </summary>
    /// <seealso cref="VXRSocketInteractor.SocketScaleMode"/>
    public enum SocketScaleMode
    {
        /// <summary>
        /// The interactable will not be scaled when attached to the socket.
        /// </summary>
        None,

        /// <summary>
        /// The interactable will be scaled to a fixed size when attached to the socket.
        /// The actual size is defined by the <see cref="XRSocketInteractor.fixedScale"/> value.
        /// </summary>
        Fixed,

        /// <summary>
        /// The interactable will be scaled to fit the size of the socket when attached.
        /// The scaling is dynamic, computed using the interactable's bounds, with a target size defined by <see cref="XRSocketInteractor.targetBoundsSize"/>.
        /// </summary>
        StretchedToFitSize,
    }
}
