using UnityEngine;
using VaporInspector;

namespace VaporXR
{
    public abstract class PoseSource : MonoBehaviour, IPoseSource
    {
        [FoldoutGroup("Posing"), SerializeField]
        private VXRHand _hand;
        [FoldoutGroup("Posing"), SerializeField]
        private HandPoseDatum _pose;
        [FoldoutGroup("Posing"), SerializeField]
        private float _poseDuration;

        protected void EnablePose()
        {
            if (_pose != null)
            {
                _hand.RequestHandPose(HandPoseType.Hover, this, _pose.Value, duration: _poseDuration);
            }
        }

        protected void DisablePose()
        {
            _hand.RequestReturnToIdle(this, _poseDuration);
        }
    }
}
