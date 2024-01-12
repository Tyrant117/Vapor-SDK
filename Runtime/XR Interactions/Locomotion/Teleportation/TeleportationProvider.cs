using UnityEngine;
using UnityEngine.Assertions;
using VaporInspector;

namespace VaporXR.Locomotion.Teleportation
{
    /// <summary>
    /// The <see cref="TeleportationProvider"/> is responsible for moving the XR Origin
    /// to the desired location on the user's request.
    /// </summary>
    public class TeleportationProvider : LocomotionProvider
    {
        #region Inspector
        [SerializeField, Suffix("s"), FoldoutGroup("Teleportation Settings")] 
        [RichTextTooltip("The time (in seconds) to delay the teleportation once it is activated.")]
        private float _delayTime;
        #endregion

        #region Properties
        /// <summary>
        /// The current teleportation request.
        /// </summary>
        protected TeleportRequest CurrentRequest { get; set; }

        /// <summary>
        /// Whether the current teleportation request is valid.
        /// </summary>
        protected bool ValidRequest { get; set; }

        /// <summary>
        /// The time (in seconds) to delay the teleportation once it is activated.
        /// This delay can be used, for example, as time to set a tunneling vignette effect as a VR comfort option.
        /// </summary>
        public float DelayTime { get => _delayTime; set => _delayTime = value; }

        /// <inheritdoc/>
        public override bool CanStartMoving => _delayTime <= 0f || Time.time - _delayStartTime >= _delayTime;
        
        /// <summary>
        /// The transformation that is used by this component to apply up vector orientation.
        /// </summary>
        /// <seealso cref="MatchOrientation.WorldSpaceUp"/>
        /// <seealso cref="MatchOrientation.TargetUp"/>
        public XROriginUpAlignment UpTransformation { get; set; } = new();

        /// <summary>
        /// The transformation that is used by this component to apply forward vector orientation.
        /// </summary>
        /// <seealso cref="MatchOrientation.TargetUpAndForward"/>
        public XRCameraForwardXZAlignment ForwardTransformation { get; set; } = new();

        /// <summary>
        /// The transformation that is used by this component to apply teleport positioning movement.
        /// </summary>
        public XRBodyGroundPosition PositionTransformation { get; set; } = new();
        #endregion

        #region Fields
        private float _delayStartTime;
        #endregion
        
        protected virtual void Update()
        {
            if (!ValidRequest)
                return;

            if (LocomotionState == LocomotionState.Idle)
            {
                if (_delayTime > 0f)
                {
                    if (TryPrepareLocomotion())
                        _delayStartTime = Time.time;
                }
                else
                {
                    TryStartLocomotionImmediately();
                }
            }

            if (LocomotionState == LocomotionState.Moving)
            {
                switch (CurrentRequest.matchOrientation)
                {
                    case MatchOrientation.WorldSpaceUp:
                        UpTransformation.targetUp = Vector3.up;
                        TryQueueTransformation(UpTransformation);
                        break;
                    case MatchOrientation.TargetUp:
                        UpTransformation.targetUp = CurrentRequest.destinationRotation * Vector3.up;
                        TryQueueTransformation(UpTransformation);
                        break;
                    case MatchOrientation.TargetUpAndForward:
                        UpTransformation.targetUp = CurrentRequest.destinationRotation * Vector3.up;
                        TryQueueTransformation(UpTransformation);
                        ForwardTransformation.targetDirection = CurrentRequest.destinationRotation * Vector3.forward;
                        TryQueueTransformation(ForwardTransformation);
                        break;
                    case MatchOrientation.None:
                        // Change nothing. Maintain current origin rotation.
                        break;
                    default:
                        Assert.IsTrue(false, $"Unhandled {nameof(MatchOrientation)}={CurrentRequest.matchOrientation}.");
                        break;
                }

                PositionTransformation.targetPosition = CurrentRequest.destinationPosition;
                TryQueueTransformation(PositionTransformation);

                TryEndLocomotion();
                ValidRequest = false;
            }
        }
        
        /// <summary>
        /// This function will queue a teleportation request within the provider.
        /// </summary>
        /// <param name="teleportRequest">The teleportation request to queue.</param>
        /// <returns>Returns <see langword="true"/> if successfully queued. Otherwise, returns <see langword="false"/>.</returns>
        public virtual bool QueueTeleportRequest(TeleportRequest teleportRequest)
        {
            CurrentRequest = teleportRequest;
            ValidRequest = true;
            return true;
        }
    }
}
