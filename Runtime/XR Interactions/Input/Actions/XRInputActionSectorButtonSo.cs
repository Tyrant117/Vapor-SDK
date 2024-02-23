using System;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using VaporInspector;

namespace VaporXR
{
    [CreateAssetMenu(fileName = "Button", menuName = "Vapor/XR/Input/Sector Button", order = 12)]
    public class XRInputActionSectorButtonSo : XRInputActionSo
    {
        /// <summary>
        /// Sets which state this sector button is in.
        /// </summary>
        /// <seealso cref="_state"/>
        public enum State
        {
            /// <summary>
            /// Input control is in the center deadzone region.
            /// </summary>
            Centered,

            /// <summary>
            /// The initial latched direction was one of the configured valid directions.
            /// </summary>
            StartedValidDirection,

            /// <summary>
            /// The initial latched direction was not one of the configured valid directions.
            /// </summary>
            StartedInvalidDirection,
        }

        [SerializeField]
        private bool _monitorAxisValues;
        [SerializeField, Range(0.1f, 1f)]
        private float _threshold = 0.1f;
        [SerializeField, Range(0.1f, 1f)]
        private float _releaseThreshold = 0.1f;
        [SerializeField, Range(0f, 360f)]
        private float _minAngle;
        [SerializeField, Range(0f, 360f)]
        private float _maxAngle;

        [FoldoutGroup("Cardinal Setup"), SerializeField, InlineButton("SetupCardinals","Set")]
        private Cardinal _cardinal;
        [FoldoutGroup("Cardinal Setup"), SerializeField]
        private bool _eightDirectional;
        private void SetupCardinals()
        {
            if (_eightDirectional)
            {
                (float, float) tuple = _cardinal switch
                {
                    // Determine the cardinal direction based on the angle
                    Cardinal.North => (337.5f, 22.5f),
                    Cardinal.NorthEast => (22.5f, 67.5f),
                    Cardinal.East => (67.5f, 112.5f),
                    Cardinal.SouthEast => (112.5f, 157.5f),
                    Cardinal.South => (157.5f, 202.5f),
                    Cardinal.SouthWest => (202.5f, 247.5f),
                    Cardinal.West => (247.5f, 292.5f),
                    Cardinal.NorthWest => (292.5f, 337.5f),
                    _ => (0, 0)
                };
                _minAngle = tuple.Item1;
                _maxAngle = tuple.Item2;
            }
            else
            {
                (float, float) tuple = _cardinal switch
                {
                    // Determine the cardinal direction based on the angle
                    Cardinal.North => (315f, 45f),
                    Cardinal.East => (45f, 135f),
                    Cardinal.South => (135f, 225f),
                    Cardinal.West => (225f, 315f),

                    Cardinal.NorthEast => (0f, 90f),
                    Cardinal.SouthEast => (90f, 180f),
                    Cardinal.SouthWest => (180f, 270f),
                    Cardinal.NorthWest => (270f, 360f),
                    _ => (0, 0)
                };
                _minAngle = tuple.Item1;
                _maxAngle = tuple.Item2;
            }
        }

        public Vector2 Joystick { get; private set; }

        private bool _releaseWasValid;
        private State _state;

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
            Joystick = BoundAction.ReadValue<Vector2>();
            _currentValue = Joystick.magnitude;

            if (CurrentState.Active)
            {
                if (_currentValue < _releaseThreshold)
                {
                    _releaseWasValid = IsValidDirection();
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (_currentValue >= _threshold)
                {
                    if (_state == State.Centered)
                    {
                        var valid = IsValidDirection();
                        _state = valid ? State.StartedValidDirection : State.StartedInvalidDirection;
                        return valid;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    _state = State.Centered;
                    return false;
                }
            }
        }

        private bool IsValidDirection()
        {
            return ValidDirection(Joystick, _minAngle, _maxAngle);
        }

        [BurstCompile]
        private static bool ValidDirection(float2 joystick, float minAngle, float maxAngle)
        {
            // Calculate the angle in degrees using Atan2
            var angleInDegrees = math.degrees(math.atan2(joystick.x, joystick.y));

            // Ensure the angle is in the range 0-360 degrees
            if (angleInDegrees < 0)
            {
                angleInDegrees += 360;
            }

            return minAngle > maxAngle
                ? angleInDegrees >= minAngle || angleInDegrees < maxAngle
                : angleInDegrees >= minAngle && angleInDegrees < maxAngle;
        }

        protected override void FireEvents()
        {
            if (CurrentState.ActivatedThisFrame)
            {
                OnPressed();
            }
            else if (CurrentState.DeactivatedThisFrame && _releaseWasValid)
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
