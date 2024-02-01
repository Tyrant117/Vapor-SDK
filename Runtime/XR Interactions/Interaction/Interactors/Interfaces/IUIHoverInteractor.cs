using System;
using VaporXR.UI;

namespace VaporXR
{
    /// <summary>
    /// Matches the UI Model to the state of the Interactor with support for hover events.
    /// </summary>
    public interface IUIHoverInteractor : IUIInteractor
    {
        /// <summary>
        /// The event that is called when the Interactor begins hovering over a UI element.
        /// </summary>
        /// <remarks>
        /// The <see cref="UIHoverEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        event Action<UIHoverEventArgs> UiHoverEntered;

        /// <summary>
        /// The event that is called when this Interactor ends hovering over a UI element.
        /// </summary>
        /// <remarks>
        /// The <see cref="UIHoverEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        event Action<UIHoverEventArgs> UiHoverExited;

        /// <summary>
        /// The <see cref="XRUIInputModule"/> calls this method when the Interactor begins hovering over a UI element.
        /// </summary>
        /// <param name="args">Event data containing the UI element that is being hovered over.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnUIHoverExited(UIHoverEventArgs)"/>
        void OnUIHoverEntered(UIHoverEventArgs args);

        /// <summary>
        /// The <see cref="XRUIInputModule"/> calls this method when the Interactor ends hovering over a UI element.
        /// </summary>
        /// <param name="args">Event data containing the UI element that is no longer hovered over.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnUIHoverEntered(UIHoverEventArgs)"/>
        void OnUIHoverExited(UIHoverEventArgs args);
    }
}
