using UnityEngine;

namespace Vapor.Tweening
{
    public class ResizeUIAnimation : BaseUIAnimation
    {
        protected readonly RectTransform _target;
        protected Vector2 _initialSize;
        protected Vector2 _finalSize;
        protected Vector2 _velocity;

        public ResizeUIAnimation(RectTransform target, float duration, int iterations, Vector2 finalSize, Easing.Ease ease = Easing.Ease.Linear) : base(duration, iterations, ease)
        {
            _target = target;
            _finalSize = finalSize;
        }

        protected override void CacheInitialData()
        {
            _initialSize = _target.sizeDelta;
        }

        protected override void CacheVelocity()
        {
            _velocity = (_finalSize - _initialSize) / _duration;
        }

        protected override void OnReset()
        {
            _target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _initialSize.x);
            _target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _initialSize.y);
        }

        protected override void OnSetFinalState()
        {
            _target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _finalSize.x);
            _target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _finalSize.y);
        }

        protected override void UpdateTargetState(double fraction)
        {
            Vector2 dXY = Vector3.Lerp(_initialSize, _finalSize, _easingFunction((float)fraction)); // _target.sizeDelta + _velocity * Time.deltaTime;
            _target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, dXY.x);
            _target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, dXY.y);
        }
    }
}
