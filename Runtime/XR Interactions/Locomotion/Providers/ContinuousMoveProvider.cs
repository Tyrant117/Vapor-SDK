using UnityEngine;
using VaporInspector;

namespace VaporXR.Locomotion
{
    /// <summary>
    /// Locomotion provider that allows the user to smoothly move their rig continuously over time
    /// based on read input values, such as from the controller thumbstick.
    /// </summary>
    /// <seealso cref="LocomotionProvider"/>
    /// <seealso cref="ContinuousTurnProvider"/>
    public class ContinuousMoveProvider : LocomotionProvider
    {
        #region Inspector
        [SerializeField, FoldoutGroup("Continuous Move Settings")] 
        [RichTextTooltip("The speed, in units per second, to move forward.")]
        private float _moveSpeed = 1f;
        [SerializeField, FoldoutGroup("Continuous Move Settings")] 
        [RichTextTooltip("Controls whether to enable strafing (sideways movement).")]
        private bool _enableStrafe = true;
        [SerializeField, FoldoutGroup("Continuous Move Settings")] 
        [RichTextTooltip("Controls whether to enable flying (unconstrained movement). This overrides the use of gravity.")]
        private bool _enableFly;
        [SerializeField, FoldoutGroup("Continuous Move Settings")] 
        [RichTextTooltip("Controls whether gravity affects this provider when a Character Controller is used and flying is disabled.")]
        private bool _useGravity = true;
        [SerializeField, FoldoutGroup("Continuous Move Settings")] 
        [RichTextTooltip("The source Transform to define the forward direction.")]
        private Transform _forwardSource;

        [SerializeField, FoldoutGroup("Input", order: 10)]
        [RichTextTooltip("The update provider used for polling the input mapped to LeftHandTurnInput and RightHandTurnInput")]
        private VXRInputDeviceUpdateProvider _inputDeviceUpdateProvider;
        [SerializeField, FoldoutGroup("Input")]
        [RichTextTooltip("Reads input data from the left hand controller. Input Action must be a Value action type (Vector 2).")]
        private Axis2DInputProvider _leftHandMoveInput;
        [SerializeField, FoldoutGroup("Input")]
        [RichTextTooltip("Reads input data from the right hand controller. Input Action must be a Value action type (Vector 2).")]
        private Axis2DInputProvider _rightHandMoveInput;
        #endregion

        #region Properties
        /// <summary>
        /// The speed, in units per second, to move forward.
        /// </summary>
        public float MoveSpeed { get => _moveSpeed; set => _moveSpeed = value; }

        /// <summary>
        /// Controls whether to enable strafing (sideways movement).
        /// </summary>
        public bool EnableStrafe { get => _enableStrafe; set => _enableStrafe = value; }

        /// <summary>
        /// Controls whether to enable flying (unconstrained movement). This overrides <see cref="UseGravity"/>.
        /// </summary>
        public bool EnableFly { get => _enableFly; set => _enableFly = value; }

        /// <summary>
        /// Controls whether gravity affects this provider when a <see cref="CharacterController"/> is used.
        /// This only applies when <see cref="EnableFly"/> is <see langword="false"/>.
        /// </summary>
        public bool UseGravity { get => _useGravity; set => _useGravity = value; }

        /// <summary>
        /// The source <see cref="Transform"/> that defines the forward direction.
        /// </summary>
        public Transform ForwardSource { get => _forwardSource; set => _forwardSource = value; }

        /// <summary>
        /// The transformation that is used by this component to apply translation movement.
        /// </summary>
        public XROriginMovement Transformation { get; set; } = new();

        /// <summary>
        /// Reads input data from the left hand controller. Input Action must be a Value action type (Vector 2).
        /// </summary>
        public Axis2DInputProvider LeftHandMoveInput { get => _leftHandMoveInput; set => _leftHandMoveInput = value; }

        /// <summary>
        /// Reads input data from the right hand controller. Input Action must be a Value action type (Vector 2).
        /// </summary>
        public Axis2DInputProvider RightHandMoveInput { get => _rightHandMoveInput; set => _rightHandMoveInput = value; }
        #endregion

        #region Fields
        private CharacterController _characterController;

        private bool _attemptedGetCharacterController;

        private bool _isMovingXROrigin;

        private Vector3 _verticalVelocity;
        #endregion

        protected void OnEnable()
        {
            _leftHandMoveInput.BindToUpdateEvent(_inputDeviceUpdateProvider);
            _rightHandMoveInput.BindToUpdateEvent(_inputDeviceUpdateProvider);
        }

        protected void OnDisable()
        {
            _leftHandMoveInput.UnbindUpdateEvent();
            _rightHandMoveInput.UnbindUpdateEvent();
        }

        protected void Update()
        {
            _isMovingXROrigin = false;

            var xrOrigin = Mediator.xrOrigin?.Origin;
            if (xrOrigin == null)
                return;

            var input = ReadInput();
            var translationInWorldSpace = ComputeDesiredMove(input);

            if (input != Vector2.zero || _verticalVelocity != Vector3.zero)
                MoveRig(translationInWorldSpace);

            if (!_isMovingXROrigin)
                TryEndLocomotion();
        }


        private Vector2 ReadInput()
        {
            var leftHandValue = _leftHandMoveInput.CurrentValue;
            var rightHandValue = _rightHandMoveInput.CurrentValue;

            return leftHandValue + rightHandValue;
        }

        /// <summary>
        /// Determines how much to slide the rig due to <paramref name="input"/> vector.
        /// </summary>
        /// <param name="input">Input vector, such as from a thumbstick.</param>
        /// <returns>Returns the translation amount in world space to move the rig.</returns>
        protected virtual Vector3 ComputeDesiredMove(Vector2 input)
        {
            if (input == Vector2.zero)
                return Vector3.zero;

            var xrOrigin = Mediator.xrOrigin;
            if (xrOrigin == null)
                return Vector3.zero;

            // Assumes that the input axes are in the range [-1, 1].
            // Clamps the magnitude of the input direction to prevent faster speed when moving diagonally,
            // while still allowing for analog input to move slower (which would be lost if simply normalizing).
            var inputMove = Vector3.ClampMagnitude(new Vector3(_enableStrafe ? input.x : 0f, 0f, input.y), 1f);

            // Determine frame of reference for what the input direction is relative to
            var forwardSourceTransform = _forwardSource == null ? xrOrigin.Camera.transform : _forwardSource;
            var inputForwardInWorldSpace = forwardSourceTransform.forward;

            var originTransform = xrOrigin.Origin.transform;
            var speedFactor = _moveSpeed * Time.deltaTime * originTransform.localScale.x; // Adjust speed with user scale

            // If flying, just compute move directly from input and forward source
            if (_enableFly)
            {
                var inputRightInWorldSpace = forwardSourceTransform.right;
                var combinedMove = inputMove.x * inputRightInWorldSpace + inputMove.z * inputForwardInWorldSpace;
                return combinedMove * speedFactor;
            }

            var originUp = originTransform.up;

            if (Mathf.Approximately(Mathf.Abs(Vector3.Dot(inputForwardInWorldSpace, originUp)), 1f))
            {
                // When the input forward direction is parallel with the rig normal,
                // it will probably feel better for the player to move along the same direction
                // as if they tilted forward or up some rather than moving in the rig forward direction.
                // It also will probably be a better experience to at least move in a direction
                // rather than stopping if the head/controller is oriented such that it is perpendicular with the rig.
                inputForwardInWorldSpace = -forwardSourceTransform.up;
            }

            var inputForwardProjectedInWorldSpace = Vector3.ProjectOnPlane(inputForwardInWorldSpace, originUp);
            var forwardRotation = Quaternion.FromToRotation(originTransform.forward, inputForwardProjectedInWorldSpace);

            var translationInRigSpace = forwardRotation * inputMove * speedFactor;
            var translationInWorldSpace = originTransform.TransformDirection(translationInRigSpace);

            return translationInWorldSpace;
        }

        /// <summary>
        /// Creates a locomotion event to move the rig by <paramref name="translationInWorldSpace"/>,
        /// and optionally applies gravity.
        /// </summary>
        /// <param name="translationInWorldSpace">The translation amount in world space to move the rig (pre-gravity).</param>
        protected virtual void MoveRig(Vector3 translationInWorldSpace)
        {
            var xrOrigin = Mediator.xrOrigin?.Origin;
            if (xrOrigin == null)
                return;

            FindCharacterController();

            var motion = translationInWorldSpace;

            if (_characterController != null && _characterController.enabled)
            {
                // Step vertical velocity from gravity
                if (_characterController.isGrounded || !_useGravity || _enableFly)
                {
                    _verticalVelocity = Vector3.zero;
                }
                else
                {
                    _verticalVelocity += Physics.gravity * Time.deltaTime;
                }

                motion += _verticalVelocity * Time.deltaTime;
            }

            TryStartLocomotionImmediately();

            if (LocomotionState != LocomotionState.Moving)
                return;

            // Note that calling Move even with Vector3.zero will have an effect by causing isGrounded to update
            _isMovingXROrigin = true;
            Transformation.motion = motion;
            TryQueueTransformation(Transformation);
        }

        private void FindCharacterController()
        {
            var xrOrigin = Mediator.xrOrigin?.Origin;
            if (xrOrigin == null)
                return;

            // Save a reference to the optional CharacterController on the rig GameObject
            // that will be used to move instead of modifying the Transform directly.
            if (_characterController == null && !_attemptedGetCharacterController)
            {
                // Try on the Origin GameObject first, and then fallback to the XR Origin GameObject (if different)
                if (!xrOrigin.TryGetComponent(out _characterController) && xrOrigin != Mediator.xrOrigin.gameObject)
                    Mediator.xrOrigin.TryGetComponent(out _characterController);

                _attemptedGetCharacterController = true;
            }
        }
    }
}
