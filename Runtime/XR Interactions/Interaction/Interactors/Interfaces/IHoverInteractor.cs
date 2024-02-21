using System;
using System.Collections.Generic;

namespace VaporXR.Interaction
{
    public interface IHoverInteractor
    {
        bool IsHoverActive { get; }
        List<Interactable> InteractablesHovered { get; }
        IXRFilterList<IXRHoverFilter> HoverFilters { get; }
        Func<bool> HoverActive { get; set; }

        event Action<HoverEnterEventArgs> HoverEntering;
        event Action<HoverEnterEventArgs> HoverEntered;
        event Action<HoverExitEventArgs> HoverExiting;
        event Action<HoverExitEventArgs> HoverExited;

        bool CanHover(Interactable interactable);
        bool IsHovering(Interactable interactable);
        void OnHoverEntered(HoverEnterEventArgs args);
        void OnHoverEntering(HoverEnterEventArgs args);
        void OnHoverExited(HoverExitEventArgs args);
        void OnHoverExiting(HoverExitEventArgs args);
    }
}
