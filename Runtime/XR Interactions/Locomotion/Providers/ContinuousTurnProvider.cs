using UnityEngine;
using UnityEngine.Assertions;
using VaporInspector;

namespace VaporXR.Locomotion
{
    /// <summary>
    /// Locomotion provider that allows the user to smoothly rotate their rig continuously over time
    /// based on read input values, such as from the controller thumbstick.
    /// </summary>
    /// <seealso cref="LocomotionProvider"/>
    /// <seealso cref="ContinuousMoveProvider"/>
    /// <seealso cref="SnapTurnProvider"/>
    public class ContinuousTurnProvider : LocomotionProvider
    {
        #region Inspector
        [SerializeField, FoldoutGroup("Continuous Turn Settings"), Suffix("°/s")]
        [RichTextTooltip("The number of degrees/second clockwise to rotate when turning.")]
        private float _turnSpeed = 60f;
        
        [SerializeField, FoldoutGroup("Input")]
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
        /// The number of degrees/second clockwise to rotate when turning clockwise.
        /// </summary>
        public float TurnSpeed
        {
            get => _turnSpeed;
            set => _turnSpeed = value;
        }

        /// <summary>
        /// Reads input data from the left hand controller. Input Action must be a Value action type (Vector 2).
        /// </summary>
        public Axis2DInputProvider LeftHandTurnInput
        {
            get => _leftHandTurnInput;
            set => _leftHandTurnInput = value;
        }

        /// <summary>
        /// Reads input data from the right hand controller. Input Action must be a Value action type (Vector 2).
        /// </summary>
        public Axis2DInputProvider RightHandTurnInput
        {
            get => _rightHandTurnInput;
            set => _rightHandTurnInput = value;
        }

        /// <summary>
        /// The transformation that is used by this component to apply turn movement.
        /// </summary>
        public XRBodyYawRotation Transformation { get; set; } = new XRBodyYawRotation();
        #endregion

        #region Fields
        private bool _isTurningXROrigin;
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
            _isTurningXROrigin = false;

            // Use the input amount to scale the turn speed.
            var input = ReadInput();
            var turnAmount = GetTurnAmount(input);

            TurnRig(turnAmount);

            if (!_isTurningXROrigin)
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
                case Cardinal.South:
                    break;
                case Cardinal.East:
                case Cardinal.West:
                    return input.magnitude * (Mathf.Sign(input.x) * _turnSpeed * Time.deltaTime);
                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(Cardinal)}={cardinal}");
                    break;
            }

            return 0f;
        }

        /// <summary>
        /// Rotates the rig by <paramref name="turnAmount"/> degrees.
        /// </summary>
        /// <param name="turnAmount">The amount of rotation in degrees.</param>
        protected void TurnRig(float turnAmount)
        {
            if (Mathf.Approximately(turnAmount, 0f))
            {
                return;
            }

            TryStartLocomotionImmediately();

            if (LocomotionState != LocomotionState.Moving)
            {
                return;
            }

            _isTurningXROrigin = true;
            Transformation.angleDelta = turnAmount;
            TryQueueTransformation(Transformation);
        }
    }
}
