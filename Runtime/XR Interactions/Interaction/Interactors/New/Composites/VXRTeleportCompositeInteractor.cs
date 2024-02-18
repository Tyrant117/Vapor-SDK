using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporInspector;
using VaporXR.Interactors;

namespace VaporXR
{
    [RequireComponent(typeof(VXRInteractorLineVisual))]
    public class VXRTeleportCompositeInteractor : VXRRayCompositeInteractor
    {
        protected override bool RequiresSelectInteractor => true;
        [FoldoutGroup("Components"), SerializeField, AutoReference(searchParents: true)]
        private VXRInputDeviceUpdateProvider _updateProvider;

        [VerticalGroup("Input"), SerializeField]
        private ButtonInputProvider _teleportDrawInput;
        [VerticalGroup("Input"), SerializeField]
        private ButtonInputProvider _teleportActivateInput;

        public override bool ShouldDrawLine
        {
            get
            {
                return _teleportDrawInput.IsHeld;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            Select.SetOverrideSorter(_curveSorter);
            Select.SelectActionTrigger = InputTriggerType.State; // This must be state for the selection to function.
        }

        protected void OnEnable()
        {
            _teleportDrawInput.BindToUpdateEvent(_updateProvider);
            _teleportActivateInput.BindToUpdateEvent(_updateProvider);

            Select.SelectActive = OnSelectActiveCheck;
        }        

        protected void OnDisable()
        {
            _teleportDrawInput.UnbindUpdateEvent();
            _teleportActivateInput.UnbindUpdateEvent();

            Select.SelectActive = null;
        }

        private XRIneractionActiveState OnSelectActiveCheck()
        {
            return new XRIneractionActiveState(_teleportActivateInput.IsHeld, _teleportActivateInput.CurrentState.ActivatedThisFrame, _teleportActivateInput.CurrentValue);
        }
    }
}
