using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using VaporInspector;

namespace VaporXR.Locomotion
{
    /// <summary>
    /// Locomotion provider that allows the user to rotate their rig in fixed angle increments
    /// based on read input values, such as from the controller thumbstick.
    /// </summary>
    /// <seealso cref="LocomotionProvider"/>
    /// <seealso cref="ContinuousTurnProvider"/>
    public class SnapTurnProvider : LocomotionProvider
    {
        #region Inspector
        [SerializeField, FoldoutGroup("Snap Turn Settings"), Suffix("°")] 
        [RichTextTooltip("The number of degrees clockwise to rotate when snap turning clockwise.")]
        private float _turnAmount = 45f;
        [SerializeField, FoldoutGroup("Snap Turn Settings"), Suffix("s")] 
        [RichTextTooltip("The amount of time (in seconds) that the system will wait before starting another snap turn.")]
        private float _debounceTime = 0.5f;
        [SerializeField, FoldoutGroup("Snap Turn Settings")] 
        [RichTextTooltip("Controls whether to enable left & right snap turns.")]
        private bool _enableTurnLeftRight = true;
        [SerializeField, FoldoutGroup("Snap Turn Settings")] 
        [RichTextTooltip("Controls whether to enable 180° snap turns.")]
        private bool _enableTurnAround = true;
        [SerializeField, FoldoutGroup("Snap Turn Settings"), Suffix("s")] 
        [RichTextTooltip("The time (in seconds) to delay the first turn after receiving initial input for the turn.")]
        private float _delayTime;
        
        [SerializeField, FoldoutGroup("Input"), AutoReference(searchParents: true)]
        [RichTextTooltip("The update provider used for polling the input mapped to LeftHandTurnInput and RightHandTurnInput")]
        private VXRInputDeviceUpdateProvider _inputDeviceUpdateProvider;
        [SerializeField, FoldoutGroup("Input")] 
        [RichTextTooltip("Reads input data from the left hand controller. Input Action must be a <str>Vector2</str>.")]
        private Axis2DInputProvider _leftHandTurnInput;
        [SerializeField, FoldoutGroup("Input")] 
        [RichTextTooltip("Reads input data from the right hand controller. Input Action must be a <str>Vector2</str>.")]
        private Axis2DInputProvider _rightHandTurnInput;
        #endregion

        #region Properties
        /// <summary>
        /// The number of degrees clockwise Unity rotates the rig when snap turning clockwise.
        /// </summary>
        public float TurnAmount { get => _turnAmount; set => _turnAmount = value; }

        /// <summary>
        /// The amount of time that Unity waits before starting another snap turn.
        /// </summary>
        public float DebounceTime { get => _debounceTime; set => _debounceTime = value; }

        /// <summary>
        /// Controls whether to enable left and right snap turns.
        /// </summary>
        /// <seealso cref="EnableTurnAround"/>
        public bool EnableTurnLeftRight { get => _enableTurnLeftRight; set => _enableTurnLeftRight = value; }
        
        /// <summary>
        /// Controls whether to enable 180° snap turns.
        /// </summary>
        /// <seealso cref="EnableTurnLeftRight"/>
        public bool EnableTurnAround { get => _enableTurnAround; set => _enableTurnAround = value; }

        /// <summary>
        /// The time (in seconds) to delay the first turn after receiving initial input for the turn.
        /// Subsequent turns while holding down input are delayed by the <see cref="DebounceTime"/>, not the delay time.
        /// This delay can be used, for example, as time to set a tunneling vignette effect as a VR comfort option.
        /// </summary>
        public float DelayTime { get => _delayTime; set => _delayTime = value; }

        /// <inheritdoc/>
        public override bool CanStartMoving => _delayTime <= 0f || Time.time - _delayStartTime >= _delayTime;

        /// <summary>
        /// The transformation that is used by this component to apply turn movement.
        /// </summary>
        public XRBodyYawRotation Transformation { get; set; } = new XRBodyYawRotation();

        /// <summary>
        /// Reads input data from the left hand controller. Input Action must be a Value action type (Vector 2).
        /// </summary>
        public Axis2DInputProvider LeftHandTurnInput { get => _leftHandTurnInput; set => _leftHandTurnInput = value; }

        /// <summary>
        /// Reads input data from the right hand controller. Input Action must be a Value action type (Vector 2).
        /// </summary>
        public Axis2DInputProvider RightHandTurnInput { get => _rightHandTurnInput; set => _rightHandTurnInput = value; }
        #endregion

        #region Fields
        private float _currentTurnAmount;
        private float _timeStarted;
        private float _delayStartTime;
        #endregion

        protected void OnEnable()
        {
            _leftHandTurnInput.BindToUpdateEvent(_inputDeviceUpdateProvider);
            _rightHandTurnInput.BindToUpdateEvent(_inputDeviceUpdateProvider);
        }

        protected void OnDisable()
        {
            _leftHandTurnInput.UnbindUpdateEvent();
            _rightHandTurnInput.UnbindUpdateEvent();
        }
        
        protected void Update()
        {
            // Wait for a certain amount of time before allowing another turn.
            if (_timeStarted > 0f && (_timeStarted + _debounceTime < Time.time))
            {
                _timeStarted = 0f;
                return;
            }

            var input = ReadInput();
            var amount = GetTurnAmount(input);
            if (Mathf.Abs(amount) > 0f)
            {
                StartTurn(amount);
            }
            else if (Mathf.Approximately(_currentTurnAmount, 0f) && LocomotionState == LocomotionState.Moving)
            {
                TryEndLocomotion();
            }

            if (LocomotionState != LocomotionState.Moving || !(math.abs(_currentTurnAmount) > 0f))
            {
                return;
            }

            _timeStarted = Time.time;
            Transformation.angleDelta = _currentTurnAmount;
            TryQueueTransformation(Transformation);
            _currentTurnAmount = 0f;

            if (Mathf.Approximately(amount, 0f))
            {
                TryEndLocomotion();
            }
        }

        private Vector2 ReadInput()
        {
            var leftHandValue = _leftHandTurnInput.CurrentValue;
            var rightHandValue = _rightHandTurnInput.CurrentValue;

            return leftHandValue + rightHandValue;
        }

        /// <summary>
        /// Determines the turn amount in degrees for the given <paramref name="input"/> vector.
        /// </summary>
        /// <param name="input">Input vector, such as from a thumbstick.</param>
        /// <returns>Returns the turn amount in degrees for the given <paramref name="input"/> vector.</returns>
        protected virtual float GetTurnAmount(Vector2 input)
        {
            if (input == Vector2.zero)
            {
                return 0f;
            }

            var cardinal = CardinalUtility.GetNearestCardinal(input, false);
            switch (cardinal)
            {
                case Cardinal.North:
                    break;
                case Cardinal.South:
                    if (_enableTurnAround)
                        return 180f;
                    break;
                case Cardinal.East:
                    if (_enableTurnLeftRight)
                        return _turnAmount;
                    break;
                case Cardinal.West:
                    if (_enableTurnLeftRight)
                        return -_turnAmount;
                    break;
                case Cardinal.NorthWest:
                case Cardinal.NorthEast:
                case Cardinal.SouthEast:
                case Cardinal.SouthWest:
                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(Cardinal)}={cardinal}");
                    break;
            }

            return 0f;
        }

        /// <summary>
        /// Begins turning locomotion.
        /// </summary>
        /// <param name="amount">Amount to turn.</param>
        protected void StartTurn(float amount)
        {
            if (_timeStarted > 0f)
            {
                return;
            }

            if (LocomotionState == LocomotionState.Idle)
            {
                if (_delayTime > 0f)
                {
                    if (TryPrepareLocomotion())
                    {
                        _delayStartTime = Time.time;
                    }
                }
                else
                {
                    TryStartLocomotionImmediately();
                }
            }

            // We set the m_CurrentTurnAmount here so we can still trigger the turn
            // in the case where the input is released before the delay timeout happens.
            if (math.abs(amount) > 0f)
            {
                _currentTurnAmount = amount;
            }
        }
    }
}
