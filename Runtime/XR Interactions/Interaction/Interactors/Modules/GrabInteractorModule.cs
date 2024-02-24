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
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("The minimum distance an interactable must be from this interactor to make it a distant grab")]
        private float _minimumDistanceForDistantGrab = 0.1f;

        [VerticalGroup("Input"), SerializeField]
        private XRInputButton _grabInput;

        private float _minDistanceSqr;


        #region - Initialization -
        protected override void Awake()
        {
            base.Awake();
            _minDistanceSqr = _minimumDistanceForDistantGrab * _minimumDistanceForDistantGrab;
        }

        protected void OnEnable()
        {
            _grabInput.Enable();
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

        #region - Helpers -
        public bool IsDistantGrab(Interactable interactable)
        {
            return _distantGrabActive && (VXRSorter.Type)interactable.LastSorterType == VXRSorter.Type.Raycast;
            //var distance = interactable.GetDistanceSqrToInteractor(Interactor);

            //return _distantGrabActive && distance >= _minDistanceSqr;
        }
        #endregion
    }
}
