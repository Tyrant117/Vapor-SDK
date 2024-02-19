using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR.Interactors
{
    public interface IVXRHoverInteractor : IVXRInteractor
    {
        bool IsHoverActive { get; }
        List<IVXRHoverInteractable> InteractablesHovered { get; }
        IXRFilterList<IXRHoverFilter> HoverFilters { get; }
        Func<bool> HoverActive { get; set; }

        event Action<HoverEnterEventArgs> HoverEntering;
        event Action<HoverEnterEventArgs> HoverEntered;
        event Action<HoverExitEventArgs> HoverExiting;
        event Action<HoverExitEventArgs> HoverExited;

        bool CanHover(IVXRHoverInteractable interactable);
        bool IsHovering(IVXRHoverInteractable interactable);
        void OnHoverEntered(HoverEnterEventArgs args);
        void OnHoverEntering(HoverEnterEventArgs args);
        void OnHoverExited(HoverExitEventArgs args);
        void OnHoverExiting(HoverExitEventArgs args);
    }
}
