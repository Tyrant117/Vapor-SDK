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

        private Coroutine _waitingToPose;
        private readonly WaitForSeconds _wfs = new(0.3f);
        private int _uiCount;

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

        #region - Posing -
        protected virtual void OnUIHoverPoseEntered(UIHoverEventArgs args)
        {
            if (_uiHoverPoseEnabled)
            {
                _uiCount++;
                if (_uiCount == 1 && _waitingToPose != null)
                {
                    _waitingToPose = StartCoroutine(PoseAfterDelay());
                }
            }
        }

        protected virtual void OnUIHoverPoseExited(UIHoverEventArgs args)
        {
            if (_uiHoverPoseEnabled)
            {
                _uiCount--;
                if (_uiCount == 0)
                {
                    if (_waitingToPose != null)
                    {
                        StopCoroutine(_waitingToPose);
                        _waitingToPose = null;
                    }
                    _hand.RequestReturnToIdle(Interactor, _uiHoverPoseDuration);
                }
            }
        }

        private IEnumerator PoseAfterDelay()
        {
            yield return _wfs;
            _hand.RequestHandPose(HandPoseType.Hover, Interactor, _uiHoverPose.Value, duration: _uiHoverPoseDuration);
            _waitingToPose = null;
        }
        #endregion
    }
}
