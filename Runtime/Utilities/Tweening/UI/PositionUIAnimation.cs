using UnityEngine;

namespace Vapor.Tweening
{
    public class PositionUIAnimation : BaseUIAnimation
    {
        protected readonly RectTransform _target;
        protected Vector3 _initialLocalPosition;
        protected Vector3 _delta;
        protected Vector3 _velocity;
        protected Vector3 _lastDelta;

        public PositionUIAnimation(RectTransform target, float duration, int iterations, Vector3 delta, Easing.Ease ease = Easing.Ease.Linear) : base(duration, iterations, ease)
        {
            _target = target;
            _delta = delta;
        }

        protected override void CacheInitialData()
        {
            _initialLocalPosition = _target.localPosition;
        }

        protected override void CacheVelocity()
        {
            _velocity = _delta / _duration;
        }

        protected override void OnReset()
        {
            _target.localPosition = _initialLocalPosition;
            _lastDelta = Vector3.zero;
        }

        protected override void OnSetFinalState()
        {
            _target.localPosition = _initialLocalPosition + _delta;
        }

        protected override void UpdateTargetState(double fraction)
        {
            var pos = _delta * _easingFunction((float)fraction);
            var delta = pos - _lastDelta;

            _target.localPosition += delta;
            _lastDelta = pos;
        }
    }
}
