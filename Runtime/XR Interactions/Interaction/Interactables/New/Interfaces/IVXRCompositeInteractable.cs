using UnityEngine;
using VaporXR.Interactors;

namespace VaporXR.Interactables
{
    public interface IVXRCompositeInteractable
    {

#pragma warning disable IDE1006 // Naming Styles
        Transform transform { get; }
#pragma warning restore IDE1006 // Naming Styles

        IVXRHoverInteractable Hover { get; }
        IVXRSelectInteractable Select { get; }
        bool IsHovered { get; }
        bool IsSelected { get; }
        bool IsFocused { get; }

        bool IsHoverableBy(IVXRHoverInteractor interactor);
        bool IsHoveredBy(IVXRHoverInteractor interactor);
        bool IsSelectableBy(IVXRSelectInteractor interactor);
        bool IsSelectedBy(IVXRSelectInteractor interactor);
    }
}
