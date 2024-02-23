using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporInspector;

namespace VaporXR.Interaction
{
    [DisallowMultipleComponent]
    public class GrabInteractorModule : InteractorModule
    {
        [FoldoutGroup("Components"), SerializeField, AutoReference(searchParents: true)]
        private VXRInputDeviceUpdateProvider _updateProvider;

        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("If <lw>true</lw> this interactor can grab objects at a distance and pull them to it.")]
        private bool _distantGrabActive = true;

        [VerticalGroup("Input"), SerializeField]
        private XRInputButton _grabInput;

        public bool DistantGrabActive => _distantGrabActive;


        #region - Initialization -
        protected void OnEnable()
        {
            _grabInput.BindInput().Enable();
            //_grabInput.BindToUpdateEvent(_updateProvider);
            Interactor.SelectActive = OnSelectActiveCheck;
        }

        protected void OnDisable()
        {
            _grabInput.Disable();
            //_grabInput.UnbindUpdateEvent();
            Interactor.SelectActive = null;
        }

        private XRIneractionActiveState OnSelectActiveCheck()
        {
            return new XRIneractionActiveState(_grabInput.IsHeld, _grabInput.State.ActivatedThisFrame, _grabInput.CurrentValue);
        }
        #endregion

        #region - Hovering -
        public override bool CanHover(Interactable interactable)
        {
            return base.CanHover(interactable) && (!Interactor.HasSelection || Interactor.IsSelecting(interactable));
        }
        #endregion

        #region - Selection -
        public override bool CanSelect(Interactable interactable)
        {
            return base.CanSelect(interactable) && (!Interactor.HasSelection || Interactor.IsSelecting(interactable));
        }
        #endregion
    }
}
