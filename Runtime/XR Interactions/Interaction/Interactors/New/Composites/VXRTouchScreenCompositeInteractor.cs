using UnityEngine;
using VaporInspector;
using VaporXR.UI;

namespace VaporXR.Interactors
{
    [RequireComponent(typeof(VXRGraphicInteractor))]
    public class VXRTouchScreenCompositeInteractor : VXRCompositeInteractor, IPoseSource
    {
        [BoxGroup("Components"), SerializeField, AutoReference]
        private VXRGraphicInteractor _graphicInteractor;

        [FoldoutGroup("Posing"), SerializeField]
        private bool _uiHoverPoseEnabled = true;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_uiHoverPoseEnabled")]
        private VXRHand _hand;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_uiHoverPoseEnabled")]
        private HandPoseDatum _uiHoverPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_uiHoverPoseEnabled")]
        private float _uiHoverPoseDuration;


        public VXRGraphicInteractor Graphic => _graphicInteractor;

        private void OnEnable()
        {
            _graphicInteractor.UiHoverEntered += OnUIHoverPoseEntered;
            _graphicInteractor.UiHoverExited += OnUIHoverPoseExited;
        }

        private void OnDisable()
        {
            _graphicInteractor.UiHoverEntered -= OnUIHoverPoseEntered;
            _graphicInteractor.UiHoverExited -= OnUIHoverPoseExited;
        }

        #region - Posing -
        protected virtual void OnUIHoverPoseEntered(UIHoverEventArgs args)
        {
            if (_uiHoverPoseEnabled)
            {
                _hand.RequestHandPose(HandPoseType.Hover, this, _uiHoverPose.Value, duration: _uiHoverPoseDuration);
            }
        }

        protected virtual void OnUIHoverPoseExited(UIHoverEventArgs args)
        {
            if (_uiHoverPoseEnabled)
            {
                _hand.RequestReturnToIdle(this, _uiHoverPoseDuration);
            }
        }
        #endregion
    }
}
