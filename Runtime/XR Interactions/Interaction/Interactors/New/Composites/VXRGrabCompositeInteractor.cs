using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporInspector;

namespace VaporXR
{
    public class VXRGrabCompositeInteractor : VXRCompositeInteractor
    {
        protected override bool RequiresHoverInteractor => true;
        protected override bool RequiresSelectInteractor => true;

        [FoldoutGroup("Components"), SerializeField, AutoReference(searchParents: true)]
        private VXRInputDeviceUpdateProvider _updateProvider;        

        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("If <lw>true</lw> this interactor can grab objects at a distance and pull them to it.")]
        private bool _distantGrabActive = true;

        [VerticalGroup("Input"), SerializeField]
        private ButtonInputProvider _grabInput;

        public bool DistantGrabActive => _distantGrabActive;


        #region - Initialization -
        protected void OnEnable()
        {
            _grabInput.BindToUpdateEvent(_updateProvider);
            Select.SelectActive = OnSelectActiveCheck;
        }

        protected void OnDisable()
        {
            _grabInput.UnbindUpdateEvent();
            Select.SelectActive = null;
        }

        private XRIneractionActiveState OnSelectActiveCheck()
        {
            return new XRIneractionActiveState(_grabInput.IsHeld, _grabInput.CurrentState.ActivatedThisFrame, _grabInput.CurrentValue);
        }
        #endregion

        #region - Hovering -
        public override bool CanHover(IVXRHoverInteractable interactable)
        {
            return base.CanHover(interactable) && (!HasSelection || IsSelecting(interactable.Composite.Select));
        }
        #endregion

        #region - Selection -
        public override bool CanSelect(IVXRSelectInteractable interactable)
        {
            return base.CanSelect(interactable) && (!HasSelection || IsSelecting(interactable));
        }
        #endregion
    }
}
