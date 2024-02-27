using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporInspector;
using VaporXR.UI;

namespace VaporXR.Interaction
{
    [DisallowMultipleComponent]
    public class TouchScreenInteractorModule : InteractorModule
    {
        [FoldoutGroup("Components"), SerializeField, AutoReference]
        private GraphicInteractorModule _graphicInteractor;

        [FoldoutGroup("Posing"), SerializeField]
        private bool _uiHoverPoseEnabled = true;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_uiHoverPoseEnabled")]
        private VXRHand _hand;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_uiHoverPoseEnabled")]
        private HandPoseDatum _uiHoverPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_uiHoverPoseEnabled")]
        private float _uiHoverPoseDuration;


        public GraphicInteractorModule Graphic => _graphicInteractor;

        private int _uiCount;
        private float _pendingTimer;
        private bool _hasPosed;

        private void OnEnable()
        {
            _graphicInteractor.UiHoverEntered += OnUIHoverPoseEntered;
            _graphicInteractor.UiHoverExited += OnUIHoverPoseExited;
            _uiCount = 0;
        }

        private void OnDisable()
        {
            _graphicInteractor.UiHoverEntered -= OnUIHoverPoseEntered;
            _graphicInteractor.UiHoverExited -= OnUIHoverPoseExited;
        }

        public override void PostProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) { return; }

            if (_uiCount > 0 && !_hasPosed && Time.time > _pendingTimer)
            {
                _hand.RequestHandPose(HandPoseType.Hover, Interactor, _uiHoverPose.Value, duration: _uiHoverPoseDuration);
                _hasPosed = true;
            }
        }

        #region - Posing -
        protected virtual void OnUIHoverPoseEntered(UIHoverEventArgs args)
        {
            if (_uiHoverPoseEnabled)
            {
                _uiCount++;
                if (_uiCount == 0)
                {
                    _pendingTimer = Time.time + 0.3f;
                }
            }
        }

        protected virtual void OnUIHoverPoseExited(UIHoverEventArgs args)
        {
            if (_uiHoverPoseEnabled)
            {
                _uiCount--;
                if(_uiCount == 0)
                {
                    _hasPosed = false;
                }
            }
        }
        #endregion
    }
}
