using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Assertions;
using VaporInspector;

namespace VaporXR.Locomotion
{
    /// <summary>
    /// A version of continuous movement that automatically controls the frame of reference that
    /// determines the forward direction of movement based on user preference for each hand.
    /// For example, can configure to use head relative movement for the left hand and controller relative movement for the right hand.
    /// </summary>
    public class RelativeContinuousMoveProvider : ContinuousMoveProvider
    {
        /// <summary>
        /// Defines which transform the XR Origin's movement direction is relative to.
        /// </summary>
        /// <seealso cref="RelativeContinuousMoveProvider.LeftHandMovementDirection"/>
        /// <seealso cref="RelativeContinuousMoveProvider.RightHandMovementDirection"/>
        public enum MovementDirection
        {
            /// <summary>
            /// Use the forward direction of the head (camera) as the forward direction of the XR Origin's movement.
            /// </summary>
            HeadRelative,

            /// <summary>
            /// Use the forward direction of the hand (controller) as the forward direction of the XR Origin's movement.
            /// </summary>
            HandRelative,
        }

        #region Inspector
        [SerializeField, FoldoutGroup("Relative Movement Settings", order: 1)] 
        [RichTextTooltip("Directs the XR Origin's movement when using the head-relative mode. If not set, will automatically find and use the XR Origin Camera.")]
        private Transform _headTransform;

        [SerializeField, FoldoutGroup("Relative Movement Settings")] 
        [RichTextTooltip("Directs the XR Origin's movement when using the hand-relative mode with the left hand.")]
        private Transform _leftControllerTransform;

        [SerializeField, FoldoutGroup("Relative Movement Settings")] 
        [RichTextTooltip("Directs the XR Origin's movement when using the hand-relative mode with the right hand.")]
        private Transform _rightControllerTransform;

        [SerializeField, FoldoutGroup("Relative Movement Settings")]
        [RichTextTooltip("Whether to use the specified head transform or left controller transform to direct the XR Origin's movement for the left hand.")]
        private MovementDirection _leftHandMovementDirection;

        [SerializeField, FoldoutGroup("Relative Movement Settings")]
        [RichTextTooltip("Whether to use the specified head transform or right controller transform to direct the XR Origin's movement for the right hand.")]
        private MovementDirection _rightHandMovementDirection;
        #endregion

        #region Properties
        /// <summary>
        /// Directs the XR Origin's movement when using the head-relative mode. If not set, will automatically find and use the XR Origin Camera.
        /// </summary>
        public Transform HeadTransform { get => _headTransform; set => _headTransform = value; }

        /// <summary>
        /// Directs the XR Origin's movement when using the hand-relative mode with the left hand.
        /// </summary>
        public Transform LeftControllerTransform { get => _leftControllerTransform; set => _leftControllerTransform = value; }

        public Transform RightControllerTransform { get => _rightControllerTransform; set => _rightControllerTransform = value; }

        /// <summary>
        /// Whether to use the specified head transform or controller transform to direct the XR Origin's movement for the left hand.
        /// </summary>
        /// <seealso cref="MovementDirection"/>
        public MovementDirection LeftHandMovementDirection { get => _leftHandMovementDirection; set => _leftHandMovementDirection = value; }

        /// <summary>
        /// Whether to use the specified head transform or controller transform to direct the XR Origin's movement for the right hand.
        /// </summary>
        /// <seealso cref="MovementDirection"/>
        public MovementDirection RightHandMovementDirection { get => _rightHandMovementDirection; set => _rightHandMovementDirection = value; }
        #endregion

        #region Fields
        private Transform _combinedTransform;
        private Pose _leftMovementPose = Pose.identity;
        private Pose _rightMovementPose = Pose.identity;
        #endregion


        protected override void Awake()
        {
            base.Awake();

            _combinedTransform = new GameObject("[Relative Move Provider] Combined Forward Source").transform;
            _combinedTransform.SetParent(transform, false);
            _combinedTransform.localPosition = Vector3.zero;
            _combinedTransform.localRotation = Quaternion.identity;

            ForwardSource = _combinedTransform;
        }

        protected override Vector3 ComputeDesiredMove(Vector2 input)
        {
            // Don't need to do anything if the total input is zero.
            // This is the same check as the base method.
            if (input == Vector2.zero)
                return Vector3.zero;

            // Initialize the Head Transform if necessary, getting the Camera from XR Origin
            if (_headTransform == null)
            {
                var xrOrigin = Mediator.XROrigin;
                if (xrOrigin != null)
                {
                    var xrCamera = xrOrigin.Camera;
                    if (xrCamera != null)
                        _headTransform = xrCamera.transform;
                }
            }

            // Get the forward source for the left hand input
            switch (_leftHandMovementDirection)
            {
                case MovementDirection.HeadRelative:
                    if (_headTransform != null)
                        _leftMovementPose = _headTransform.GetWorldPose();

                    break;

                case MovementDirection.HandRelative:
                    if (_leftControllerTransform != null)
                        _leftMovementPose = _leftControllerTransform.GetWorldPose();

                    break;

                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(MovementDirection)}={_leftHandMovementDirection}");
                    break;
            }

            // Get the forward source for the right hand input
            switch (_rightHandMovementDirection)
            {
                case MovementDirection.HeadRelative:
                    if (_headTransform != null)
                        _rightMovementPose = _headTransform.GetWorldPose();

                    break;

                case MovementDirection.HandRelative:
                    if (_rightControllerTransform != null)
                        _rightMovementPose = _rightControllerTransform.GetWorldPose();

                    break;

                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(MovementDirection)}={_rightHandMovementDirection}");
                    break;
            }

            // Combine the two poses into the forward source based on the magnitude of input
            var leftHandValue = LeftHandMoveInput.ReadValue();
            var rightHandValue = RightHandMoveInput.ReadValue();

            var totalSqrMagnitude = leftHandValue.sqrMagnitude + rightHandValue.sqrMagnitude;
            var leftHandBlend = 0.5f;
            if (totalSqrMagnitude > Mathf.Epsilon)
                leftHandBlend = leftHandValue.sqrMagnitude / totalSqrMagnitude;

            var combinedPosition = Vector3.Lerp(_rightMovementPose.position, _leftMovementPose.position, leftHandBlend);
            var combinedRotation = Quaternion.Slerp(_rightMovementPose.rotation, _leftMovementPose.rotation, leftHandBlend);
            _combinedTransform.SetPositionAndRotation(combinedPosition, combinedRotation);

            return base.ComputeDesiredMove(input);
        }
    }
}
