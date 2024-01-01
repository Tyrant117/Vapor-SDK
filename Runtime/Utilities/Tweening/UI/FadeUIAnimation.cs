using UnityEngine;

namespace Vapor.Tweening
{
    public class FadeUIAnimation : BaseUIAnimation
    {
        protected readonly CanvasGroup _target;
        protected float _initialAlpha;
        protected float _finalAlpha;
        protected float _velocity;

        public FadeUIAnimation(CanvasGroup target, float duration, int iterations, float finalAlpha, Easing.Ease ease = Easing.Ease.Linear) : base(duration, iterations, ease)
        {
            _target = target;
            _finalAlpha = finalAlpha;
        }

        protected override void CacheInitialData()
        {
            _initialAlpha = _target.alpha;
        }

        protected override void CacheVelocity()
        {
            _velocity = (_finalAlpha - _initialAlpha) / _duration;
        }

        protected override void OnReset()
        {
            _target.alpha = _initialAlpha;
        }

        protected override void OnSetFinalState()
        {
            _target.alpha = _finalAlpha;
        }

        protected override void UpdateTargetState(double fraction)
        {
            _target.alpha = Mathf.Lerp(_initialAlpha, _finalAlpha, _easingFunction((float)fraction));
        }
    }
}
