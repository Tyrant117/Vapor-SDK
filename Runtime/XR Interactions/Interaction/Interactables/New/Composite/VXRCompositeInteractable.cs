using UnityEngine;
using VaporInspector;
using VaporXR.Interactors;

namespace VaporXR.Interactables
{
    public abstract class VXRCompositeInteractable : VXRInteractable, IVXRCompositeInteractable
    {
        protected virtual bool RequiresHoverInteractable => false;
        protected virtual bool RequiresSelectInteractable => false;


        [FoldoutGroup("Components"), SerializeField, ShowIf("$RequiresHoverInteractable"), InlineButton("AddHoverInteractable", label: "+")]
        private VXRHoverInteractable _hover;
        [FoldoutGroup("Components"), SerializeField, ShowIf("$RequiresSelectInteractable"), InlineButton("AddSelectInteractable", label: "+")]
        private VXRSelectInteractable _select;

#pragma warning disable IDE0051 // Remove unused private members
        private void AddHoverInteractable() { if (!_hover) _hover = gameObject.AddComponent<VXRHoverInteractable>(); }
        private void AddSelectInteractable() { if (!_select) _select = gameObject.AddComponent<VXRSelectInteractable>(); }
#pragma warning restore IDE0051 // Remove unused private members

        public IVXRHoverInteractable Hover => _hover;
        public bool IsHovered => Hover != null && Hover.IsHovered;
        public IVXRSelectInteractable Select => _select;
        public bool IsSelected => Select != null && Select.IsSelected;
        public bool IsFocused => Select != null && Select.IsFocused;

        protected override void Awake()
        {
            base.Awake();
            Composite = this;
        }

        public virtual bool IsHoverableBy(IVXRHoverInteractor interactor)
        {
            return true;
        }

        public bool IsHoveredBy(IVXRHoverInteractor interactor)
        {
            return _hover != null && _hover.IsHoveredBy(interactor);
        }

        public virtual bool IsSelectableBy(IVXRSelectInteractor interactor)
        {
            return true;
        }

        public bool IsSelectedBy(IVXRSelectInteractor interactor)
        {
            return _select != null && _select.IsSelectedBy(interactor);
        }
    }
}
