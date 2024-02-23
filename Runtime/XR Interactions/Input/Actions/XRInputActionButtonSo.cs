using UnityEngine;

namespace VaporXR
{
    [CreateAssetMenu(fileName = "Button", menuName = "Vapor/XR/Input/Button", order = 10)]
    public class XRInputActionButtonSo : XRInputActionSo
    {

        [SerializeField]
        private bool _doubleClick;

        protected override void UpdateInput()
        {
            _lastValue = _currentValue;
            CurrentState.ResetFrameDependent();
            _wasPressed = IsPressed();
            CurrentState.SetFrameState(_wasPressed, _currentValue);
            _wasReleased = CurrentState.DeactivatedThisFrame;

            FireEvents();
        }

        protected override bool IsPressed()
        {
            _currentValue = BoundAction.ReadValue<float>();
            return _currentValue >= 0.1f;
        }

        protected override void FireEvents()
        {
            if (CurrentState.ActivatedThisFrame)
            {
                if (_doubleClick)
                {
                    if (CurrentState.ClickCount >= 2)
                    {
                        OnPressed();
                        CurrentState.ResetClickCount();
                    }
                }
                else
                {
                    OnPressed();
                }
            }
            else if (CurrentState.DeactivatedThisFrame)
            {
                OnReleased();
            }
        }
    }
}
