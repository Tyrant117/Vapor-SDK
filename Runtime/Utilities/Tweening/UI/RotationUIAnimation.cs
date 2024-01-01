using UnityEngine;

namespace Vapor.Tweening
{
    public class RotationUIAnimation : BaseUIAnimation
    {
        protected readonly RectTransform _target;
        protected Quaternion _initialLocalRotation;
        protected Vector3 _delta;
        protected Vector3 _velocity;
        protected Vector3 _lastDelta;

        public RotationUIAnimation(RectTransform target, float duration, int iterations, Vector3 delta, Easing.Ease ease = Easing.Ease.Linear) : base(duration, iterations, ease)
        {
            _target = target;
            _delta = delta;
        }

        protected override void CacheInitialData()
        {
            _initialLocalRotation = _target.localRotation;
        }

        protected override void CacheVelocity()
        {
            _velocity = _delta / _duration;
        }

        protected override void OnReset()
        {
            _target.localRotation = _initialLocalRotation;
            _lastDelta = Vector3.zero;
        }

        protected override void OnSetFinalState()
        {
            _target.localRotation = _initialLocalRotation * Quaternion.Euler(_delta);
        }

        protected override void UpdateTargetState(double fraction)
        {
            var rot = _delta * _easingFunction((float)fraction);
            var delta = rot - _lastDelta;

            _target.localRotation *= Quaternion.Euler(delta);
            _lastDelta = rot;
        }
    }
}
