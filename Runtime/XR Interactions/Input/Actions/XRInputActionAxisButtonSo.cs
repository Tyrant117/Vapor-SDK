using System;
using UnityEngine;

namespace VaporXR
{
    [CreateAssetMenu(fileName = "Button", menuName = "Vapor/XR/Input/Axis Button", order = 11)]
    public class XRInputActionAxisButtonSo : XRInputActionSo
    {
        [SerializeField]
        private bool _monitorAxisValues;
        [SerializeField, Range(0.1f, 1f)]
        private float _threshold = 0.1f;
        [SerializeField, Range(0.1f, 1f)]
        private float _releaseThreshold = 0.1f;
        [SerializeField]
        private bool _doubleClick;

        /// <summary>
        /// Invokes (CurrentValue, DeltaValue)
        /// </summary>
        public event Action<float, float> AxisChanged;

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
            return CurrentState.Active ? _currentValue >= _releaseThreshold : _currentValue >= _threshold;
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

            if (!_monitorAxisValues) { return; }
            if (Mathf.Approximately(_lastValue, _currentValue)) { return; }

            AxisChanged?.Invoke(_currentValue, _currentValue - _lastValue);
            _lastValue = _currentValue;
        }        
    }
}
