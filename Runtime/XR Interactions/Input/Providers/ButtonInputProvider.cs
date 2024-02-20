using System;
using UnityEngine;
using VaporInspector;

namespace VaporXR
{
    [Serializable, DrawWithVapor]
    public class ButtonInputProvider
    {
        /// <summary>
        /// Sets which strategy to use when sweeping the stick around the cardinal directions
        /// without returning to center for whether the action should perform or cancel.
        /// </summary>
        public enum ThresholdBehavior
        {
            /// <summary>
            /// Perform if initially actuated in a configured valid direction, and will remain so
            /// (i.e. even if no longer actuating in a valid direction)
            /// until returning to center, at which point it will cancel.
            /// </summary>
            Locked,

            /// <summary>
            /// Perform if initially actuated in a configured valid direction. Cancels when
            /// no longer actuating in a valid direction, and performs again when re-entering a valid sector
            /// even when not returning to center.
            /// </summary>
            AllowReentry,

            /// <summary>
            /// Perform if initially actuated in a configured valid direction. Cancels when
            /// no longer actuating in a valid direction, and remains so when re-entering a valid sector
            /// without returning to center.
            /// </summary>
            DisallowReentry,

            /// <summary>
            /// Perform if actuated in a configured valid direction, no matter the initial actuation direction.
            /// Cancels when not actuated in a valid direction.
            /// </summary>
            HistoryIndependent,
        }

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

        [Flags]
        public enum Directions
        {
            /// <summary>
            /// Do not include any direction, e.g. the center deadzone region of a thumbstick.
            /// The action will never perform.
            /// </summary>
            None = 0,

            /// <summary>
            /// Include North direction, e.g. forward on a thumbstick.
            /// </summary>
            North = 1 << 0,

            /// <summary>
            /// Include South direction, e.g. back on a thumbstick.
            /// </summary>
            South = 1 << 1,

            /// <summary>
            /// Include East direction, e.g. right on a thumbstick.
            /// </summary>
            East = 1 << 2,

            /// <summary>
            /// Include West direction, e.g. left on a thumbstick.
            /// </summary>
            West = 1 << 3,

            /// <summary>
            /// Include NorthWest direction, e.g. forward-left on a thumbstick.
            /// </summary>
            NorthWest = 1 << 4,

            /// <summary>
            /// Include NorthEast direction, e.g. forward-right on a thumbstick.
            /// </summary>
            NorthEast = 1 << 5,

            /// <summary>
            /// Include SouthEast direction, e.g. back-left on a thumbstick.
            /// </summary>
            SouthEast = 1 << 6,

            /// <summary>
            /// Include SouthWest direction, e.g. back-right on a thumbstick.
            /// </summary>
            SouthWest = 1 << 7,
        }
        
        public enum ButtonReadType
        {
            None,
            DirectButton,
            AxisButton,
            SectorButton
        }

        protected const float MinimumThreshold = 0.1f;

        [SerializeField] private ButtonReadType _buttonType;
        
        [SerializeField, ShowIf("$IsDirectButton")]
        private XRInputBoolReader _buttonDirectReader;
        
        [SerializeField, ShowIf("$IsAxisButton")]
        private XRInputFloatReader _buttonAxis1DReader;
        
        [SerializeField, ShowIf("$IsSectorButton")] 
        private XRInputVector2Reader _buttonAxis2DReader;
        [SerializeField, ShowIf("$IsSectorButton")]
        private XRInputBoolReader _buttonAxis2DButton;
        [SerializeField, ShowIf("$IsSectorButton")]
        private Directions _directionalButton;
        [SerializeField, ShowIf("$IsSectorButton")] 
        private bool _includeCompositeDirections;
        
        
        [SerializeField]
        private bool _doubleClick;
        [SerializeField, Range(0.1f, 1f)]
        private float _threshold = 0.1f;
        
        [SerializeField, Range(0.1f, 1f), ShowIf("$IsAxisButton")]
        private float _releaseThreshold = 0.1f;
        [SerializeField, HideIf("$IsDirectButton")]
        private ThresholdBehavior _thresholdBehavior = ThresholdBehavior.AllowReentry;
        [SerializeField, ShowIf("$IsAxisButton")]
        private bool _monitorAxisValues;

        public InputInteractionState CurrentState { get; private set; } = new();
        public bool IsHeld => CurrentState.Active;
        public float CurrentValue => _currentValue;
        public Vector2 CurrentDirectionVector { get; private set; }
        public Directions CurrentDirection { get; private set; }
        public float Threshold { get => _threshold; set => _threshold = value; }

#if UNITY_EDITOR
        // ReSharper disable once UnusedMember.Local
#pragma warning disable IDE0051 // Remove unused private members
        private bool IsNone => _buttonType == ButtonReadType.None;
                               // ReSharper disable once UnusedMember.Local
        private bool IsDirectButton => _buttonType == ButtonReadType.DirectButton;
        // ReSharper disable once UnusedMember.Local
        private bool IsAxisButton => _buttonType == ButtonReadType.AxisButton;
        // ReSharper disable once UnusedMember.Local
        private bool IsSectorButton => _buttonType == ButtonReadType.SectorButton;        
#pragma warning restore IDE0051 // Remove unused private members
#endif
        
        private IInputDeviceUpdateProvider _updateProvider;
        private State _state;
        private bool _isAxisSetup;
        private bool _wasValidDirection;
        private float _lastValue;
        private float _currentValue;
        private bool _wasPressed; 
        private bool _wasReleased;
        private bool _wasFullyReleased = true;
        private bool _waitingForFullRelease;
        
        public event Action Activated;
        public event Action Deactivated;
        /// <summary>
        /// Invokes (CurrentValue, DeltaValue)
        /// </summary>
        public event Action<float, float> AxisChanged;

        public void Setup()
        {
            if (_isAxisSetup)
            {
                return;
            }

            if (_buttonType == ButtonReadType.None)
            {
                return;
            }

            _isAxisSetup = true;

            // If not a button exit binding setup. This case is for when you might not want a button bound, but its part of another class.
            // Like a Motion Provide or part of a composite that always returns its default value.
        }
        
        public void BindToUpdateEvent(IInputDeviceUpdateProvider sourceUpdate)
        {
            Setup();
            if (!_isAxisSetup)
            {
                return;
            }
            
            _updateProvider = sourceUpdate;
            _updateProvider.RegisterForInputUpdate(UpdateInput);
        }

        public void UnbindUpdateEvent()
        {
            if (!_isAxisSetup)
            {
                return;
            }
            
            if (_updateProvider == null)
            {
                return;
            }
            _updateProvider.UnRegisterForInputUpdate(UpdateInput);
            _updateProvider = null;
        }

        public void UpdateInput()
        {
            if (!_isAxisSetup)
            {
                return;
            }

            _lastValue = _currentValue;
            CurrentState.ResetFrameDependent();
            _wasPressed = IsPressed();
            CurrentState.SetFrameState(_wasPressed, _currentValue);
            _wasReleased = CurrentState.DeactivatedThisFrame;
            if (_waitingForFullRelease)
            {
                _waitingForFullRelease = WaitingForFullRelease();
            }

            FireEvents();
        }

        protected bool IsPressed()
        {
            switch (_buttonType)
            {
                case ButtonReadType.DirectButton:
                    _currentValue = _buttonDirectReader.ReadValueAsFloat();
                    return _currentValue >= _threshold;
                case ButtonReadType.AxisButton:
                    return _IsAxis1DPressed();
                case ButtonReadType.SectorButton:
                    return _IsAxis2DPressed();
                default:
                    throw new ArgumentOutOfRangeException();
            }

            bool _IsAxis1DPressed()
            {
                _currentValue = _buttonAxis1DReader.ReadValue();
                switch (_thresholdBehavior)
                {
                    case ThresholdBehavior.Locked:
                        return CurrentState.Active ? _currentValue >= MinimumThreshold : _currentValue >= _threshold;
                    case ThresholdBehavior.AllowReentry:
                        return CurrentState.Active ? _currentValue >= _releaseThreshold : _currentValue >= _threshold;
                    case ThresholdBehavior.DisallowReentry:
                        if (CurrentState.Active)
                        {
                            var held = _currentValue >= _releaseThreshold;
                            _waitingForFullRelease = !held;
                            return held;
                        }
                        if (_waitingForFullRelease)
                        {
                            return false;
                        }
                        else
                        {
                            return _currentValue >= _threshold;
                        }
                    case ThresholdBehavior.HistoryIndependent:
                    default:
                        return _currentValue >= _threshold;
                }
            }

            bool _IsAxis2DPressed()
            {
                var isActuated = !_buttonAxis2DButton.IsValid || _buttonAxis2DButton.ReadValue();
                CurrentDirectionVector = isActuated ? _buttonAxis2DReader.ReadValue() : Vector2.zero;
                _currentValue = isActuated ? CurrentDirectionVector.magnitude : 0;
                if (!isActuated || CurrentValue < Threshold)
                {
                    //_wasReleased = false;
                    _waitingForFullRelease = false;
                    CurrentDirection = Directions.None;
                    _state = State.Centered;
                    return false;
                }

                var isValidDirection = IsValidDirection();
                if (_state == State.Centered)
                {
                    _state = isValidDirection ? State.StartedValidDirection : State.StartedInvalidDirection;
                    _wasValidDirection = isValidDirection;
                    return isValidDirection;
                }
                bool isStillPressed;
                switch (_thresholdBehavior)
                {
                    case ThresholdBehavior.Locked:
                        isStillPressed = _state == State.StartedValidDirection;
                        break;
                    case ThresholdBehavior.AllowReentry:
                        if (_wasValidDirection && !isValidDirection && _state == State.StartedValidDirection)
                        {
                            // Was Valid  Now Invalid (Release)
                            isStillPressed = false;
                        }
                        else if (!_wasValidDirection && isValidDirection && _state == State.StartedValidDirection)
                        {
                            // Was Invalid Now Valid (Press)
                            isStillPressed = true;
                        }
                        else
                        {
                            // Either All Invalid or All Valid
                            isStillPressed = _wasValidDirection && isValidDirection && _state == State.StartedValidDirection;
                        }

                        break;
                    case ThresholdBehavior.DisallowReentry:
                        if (_waitingForFullRelease)
                        {
                            isStillPressed = false;
                        }
                        else
                        {
                            isStillPressed = _wasValidDirection && isValidDirection && _state == State.StartedValidDirection;
                            if (!isStillPressed)
                            {
                                _waitingForFullRelease = true;
                            }
                        }

                        break;
                    case ThresholdBehavior.HistoryIndependent:
                        isStillPressed = isValidDirection;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                _wasValidDirection = isValidDirection;
                return isStillPressed;
            }
        }

        protected bool WaitingForFullRelease()
        {
            return _buttonType switch
            {
                ButtonReadType.AxisButton => _thresholdBehavior switch
                {
                    ThresholdBehavior.DisallowReentry => _currentValue >= MinimumThreshold,
                    _ => false,
                },
                ButtonReadType.SectorButton => _thresholdBehavior switch
                {
                    ThresholdBehavior.DisallowReentry => _currentValue >= Threshold,
                    _ => false,
                },
                _ => false,
            };
        }

        public void FireEvents()
        {
            if (CurrentState.ActivatedThisFrame)
            {
                if (_doubleClick)
                {
                    if (CurrentState.ClickCount >= 2)
                    {
                        Activated?.Invoke();
                        CurrentState.ResetClickCount();
                    }
                }
                else
                {
                    Activated?.Invoke();
                }
            }
            else if (CurrentState.DeactivatedThisFrame)
            {
                Deactivated?.Invoke();
            }

            if (_buttonType != ButtonReadType.AxisButton || !_monitorAxisValues) return;
            if (Mathf.Approximately(_lastValue, _currentValue)) return;
                
            AxisChanged?.Invoke(_currentValue, _currentValue - _lastValue);
            _lastValue = _currentValue;
        }

        private bool IsValidDirection()
        {
            var cardinal = CardinalUtility.GetNearestCardinal(CurrentDirectionVector, _includeCompositeDirections);
            CurrentDirection = GetNearestDirection(cardinal);
            return (CurrentDirection & _directionalButton) != 0;
        }

        private static Directions GetNearestDirection(Cardinal value)
        {
            return value switch
            {
                Cardinal.North => Directions.North,
                Cardinal.South => Directions.South,
                Cardinal.East => Directions.East,
                Cardinal.West => Directions.West,
                Cardinal.NorthWest => Directions.NorthWest,
                Cardinal.NorthEast => Directions.NorthEast,
                Cardinal.SouthEast => Directions.SouthEast,
                Cardinal.SouthWest => Directions.SouthWest,
                _ => Directions.None,
            };
        }

        public override string ToString()
        {
            var s1 = _buttonDirectReader.IsValid ? _buttonDirectReader.ToString() : "";
            var s2 = _buttonAxis1DReader.IsValid ? _buttonAxis1DReader.ToString() : "";
            var s3 = _buttonAxis2DReader.IsValid ? _buttonAxis2DReader.ToString() : "";
            return $"Button Reader: {s1} {s2} {s3}";
        }

    }
}
