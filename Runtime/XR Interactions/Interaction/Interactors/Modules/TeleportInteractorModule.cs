using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporInspector;

namespace VaporXR.Interaction
{
    [DisallowMultipleComponent]
    public class TeleportInteractorModule : RayInteractorModule
    {
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
            Interactor.SetOverrideSorter(_curveSorter);
            Interactor.SelectActionTrigger = InputTriggerType.State; // This must be state for the selection to function.
        }

        protected void OnEnable()
        {
            _teleportDrawInput.BindToUpdateEvent(_updateProvider);
            _teleportActivateInput.BindToUpdateEvent(_updateProvider);

            Interactor.SelectActive = OnSelectActiveCheck;
        }

        protected void OnDisable()
        {
            _teleportDrawInput.UnbindUpdateEvent();
            _teleportActivateInput.UnbindUpdateEvent();

            Interactor.SelectActive = null;
        }

        private XRIneractionActiveState OnSelectActiveCheck()
        {
            return new XRIneractionActiveState(_teleportActivateInput.IsHeld, _teleportActivateInput.CurrentState.ActivatedThisFrame, _teleportActivateInput.CurrentValue);
        }
    }
}
