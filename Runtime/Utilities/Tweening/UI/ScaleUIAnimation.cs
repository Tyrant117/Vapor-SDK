using UnityEngine;

namespace Vapor.Tweening
{
    public class ScaleUIAnimation : BaseUIAnimation
    {
        protected readonly RectTransform _target;
        protected Vector3 _initialLocalScale;
        protected Vector3 _finalScale;
        protected Vector3 _velocity;
        protected Vector3 _delta;
        protected Vector3 _lastDelta;

        public ScaleUIAnimation(RectTransform target, float duration, int iterations, Vector3 finalScale, Easing.Ease ease = Easing.Ease.Linear) : base(duration, iterations, ease)
        {
            _target = target;
            _finalScale = finalScale;
        }

        protected override void CacheInitialData()
        {
            _initialLocalScale = _target.localScale;
        }

        protected override void CacheVelocity()
        {
            _delta = _finalScale - _initialLocalScale;
            _velocity = _delta / _duration;
        }

        protected override void OnReset()
        {
            _target.localScale = _initialLocalScale;
            _lastDelta = Vector3.zero;
        }

        protected override void OnSetFinalState()
        {
            _target.localScale = _finalScale;
        }

        protected override void UpdateTargetState(double fraction)
        {
            var scale = _delta * _easingFunction((float)fraction);
            var delta = scale - _lastDelta;

            _target.localScale += delta;
            _lastDelta = scale;
        }
    }
}
