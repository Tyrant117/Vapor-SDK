using Unity.Burst;
using Unity.Mathematics;
using Unity.XR.CoreUtils;
using UnityEngine;
using Vapor.Utilities;
using VaporInspector;

namespace VaporXR
{
    /// <summary>
    /// Provides low-latency stabilization for XR pose inputs, especially useful on rays.
    /// </summary>
    [BurstCompile]
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_TransformStabilizer)]
    public class VXRTransformStabilizier : MonoBehaviour
    {
        private const float K90FPS = 1f / 90f;

        #region Inspector
        [SerializeField, BoxGroup("Configuration")]
        [RichTextTooltip("The Transform component whose position and rotation will be matched and stabilized.")]
        private Transform _target;
        [SerializeField, BoxGroup("Configuration")]
        [RequireInterface(typeof(IXRRayProvider))]
        [RichTextTooltip("Optional - When provided a ray, the stabilizer will calculate the rotation that keeps a ray's endpoint stable.")]
        private Object _aimTargetObject;
        [SerializeField, BoxGroup("Configuration")]
        [RichTextTooltip("If enabled, will read the target and apply stabilization in local space. Otherwise, in world space.")]
        private bool _useLocalSpace;
        
        [SerializeField, BoxGroup("Stabilization Parameters"), Suffix("°")]
        [RichTextTooltip("Maximum distance (in degrees) that stabilization will be applied.")]
        private float _angleStabilization = 20f;
        [SerializeField, BoxGroup("Stabilization Parameters"), Suffix("m")]
        [RichTextTooltip("Maximum distance (in meters) that stabilization will be applied.")]
        private float _positionStabilization = 0.25f;
        #endregion

        #region Properties
        /// <summary>
        /// The <see cref="Transform"/> component whose position and rotation will be matched and stabilized.
        /// </summary>
        public Transform TargetTransform
        {
            get => _target;
            set => _target = value;
        }

        private IXRRayProvider _aimTarget;
        /// <summary>
        /// When provided a ray, the stabilizer will calculate the rotation that keeps a ray's endpoint stable. 
        /// When stabilizing rotation, it uses whatever value is most optimal - either the last rotation (minimizing rotation), 
        /// or the rotation that keeps the endpoint in place.
        /// </summary>
        public IXRRayProvider AimTarget
        {
            get => _aimTarget;
            set
            {
                _aimTarget = value;
                _aimTargetObject = value as Object;
            }
        }
        
        /// <summary>
        /// If enabled, will read the target and apply stabilization in local space. Otherwise, in world space.
        /// </summary>
        public bool UseLocalSpace
        {
            get => _useLocalSpace;
            set => _useLocalSpace = value;
        }

        /// <summary>
        /// Maximum distance (in degrees) that stabilization will be applied.
        /// </summary>
        public float AngleStabilization
        {
            get => _angleStabilization;
            set => _angleStabilization = value;
        }

        /// <summary>
        /// Maximum distance (in meters) that stabilization will be applied.
        /// </summary>
        public float PositionStabilization
        {
            get => _positionStabilization;
            set => _positionStabilization = value;
        }
        #endregion

        #region Fields
        private Transform _cachedTransform;
        #endregion

        #region - Unity Methods -
        protected void Awake()
        {
            _cachedTransform = transform;
            _aimTarget ??= _aimTargetObject as IXRRayProvider;
        }

        protected void OnEnable()
        {
            _aimTarget ??= _aimTargetObject as IXRRayProvider;

            if (_useLocalSpace)
            {
                _cachedTransform.SetLocalPose(_target.GetLocalPose());
            }
            else
            {
                _cachedTransform.SetWorldPose(_target.GetWorldPose());
            }
        }

        protected void Update()
        {
            ApplyStabilization(ref _cachedTransform, _target, _aimTarget, _positionStabilization, _angleStabilization, Time.deltaTime, _useLocalSpace);
        }
        #endregion

        #region - Stabilization -
        /// <summary>
        /// Stabilizes the position and rotation of a Transform relative to a target Transform.
        /// </summary>
        /// <param name="toStabilize">The Transform to be stabilized.</param>
        /// <param name="target">The target Transform to stabilize against.</param>
        /// <param name="aimTarget">Provides the ray endpoint for rotation calculations (optional).</param>
        /// <param name="positionStabilization">Factor for stabilizing position (larger values result in quicker stabilization).</param>
        /// <param name="angleStabilization">Factor for stabilizing angle (larger values result in quicker stabilization).</param>
        /// <param name="deltaTime">The time interval to use for stabilization calculations.</param>
        /// <param name="useLocalSpace">Whether to use local space for position and rotation calculations. Defaults to false.</param>
        /// <remarks>
        /// This method adjusts the position and rotation of 'toStabilize' Transform to make it gradually align with the 'target' Transform. 
        /// If 'aimTarget' is provided, it also considers the endpoint of the ray for more precise rotation stabilization.
        /// The 'positionStabilization' and 'angleStabilization' parameters control the speed of stabilization.
        /// If 'useLocalSpace' is true, the method operates in the local space of the 'toStabilize' Transform.
        /// </remarks>
        public static void ApplyStabilization(ref Transform toStabilize, in Transform target, in IXRRayProvider aimTarget, float positionStabilization, float angleStabilization, float deltaTime, bool useLocalSpace = false)
        {
            var currentPose = useLocalSpace ? toStabilize.GetLocalPose() : toStabilize.GetWorldPose();
            var targetPose = useLocalSpace ? target.GetLocalPose() : target.GetWorldPose();
            var currentPosition = (float3)currentPose.position;
            var currentRotation = (quaternion)currentPose.rotation;
            var targetPosition = (float3)targetPose.position;
            var targetRotation = (quaternion)targetPose.rotation;

            // Processing in local space means we want to scale the position stabilization to keep it normalized
            var localScale = useLocalSpace ? toStabilize.lossyScale.x : 1f;
            localScale = Mathf.Abs(localScale) < 0.01f ? 0.01f : localScale;
            var invScale = 1f / localScale;

            float3 resultPosition;
            quaternion resultRotation;
            
            if (aimTarget == null)
            {
                StabilizeTransform(currentPosition, currentRotation, targetPosition, targetRotation, deltaTime, positionStabilization * localScale, angleStabilization,
                    out resultPosition, out resultRotation);
            }
            else
            {
                // Calculate the stabilized position
                StabilizePosition(currentPosition, targetPosition, deltaTime, positionStabilization * localScale, out resultPosition);

                // Use that to come up with the rotation that would put the endpoint of the ray at it's last position
                // Stabilize rotation to whatever value is closer - keeping the endpoint stable or the ray itself stable
                CalculateRotationParams(currentPosition, resultPosition, toStabilize.forward, toStabilize.up, aimTarget.RayEndPoint, invScale, angleStabilization, 
                    out var antiRotation, out var scaleFactor, out var targetAngleScale);

                StabilizeOptimalRotation(currentRotation, targetRotation, antiRotation, deltaTime, angleStabilization, targetAngleScale, scaleFactor, out resultRotation);
            }

            var resultPose = new Pose(resultPosition, resultRotation);
            if (useLocalSpace)
                toStabilize.SetLocalPose(resultPose);
            else
                toStabilize.SetWorldPose(resultPose);
        }
        
        [BurstCompile]
        private static void StabilizeTransform(in float3 startPos, in quaternion startRot, in float3 targetPos, in quaternion targetRot, float deltaTime, float positionStabilization, float angleStabilization, out float3 resultPos, out quaternion resultRot)
        {
            // Calculate the stabilized position
            var positionOffset = targetPos - startPos;
            var positionDistance = math.length(positionOffset);
            var positionLerp = CalculateStabilizedLerp(positionDistance / positionStabilization, deltaTime);

            // Calculate the stabilized rotation
            BurstMathUtility.Angle(targetRot, startRot, out var rotationOffset);
            var rotationLerp = CalculateStabilizedLerp(rotationOffset / angleStabilization, deltaTime);

            resultPos = math.lerp(startPos, targetPos, positionLerp);
            resultRot = math.slerp(startRot, targetRot, rotationLerp);
        }

        [BurstCompile]
        private static void StabilizePosition(in float3 startPos,in float3 targetPos, float deltaTime, float positionStabilization, out float3 resultPos)
        {
            // Calculate the stabilized position
            var positionOffset = targetPos - startPos;
            var positionDistance = math.length(positionOffset);
            var positionLerp = CalculateStabilizedLerp(positionDistance / positionStabilization, deltaTime);
            
            resultPos = math.lerp(startPos, targetPos, positionLerp);
        }

        [BurstCompile]
        private static void StabilizeOptimalRotation(in quaternion startRot, in quaternion targetRot, in quaternion alternateStartRot, float deltaTime, float angleStabilization, float alternateStabilization, float scaleFactor, out quaternion resultRot)
        {
            // Calculate the stabilized rotation
            BurstMathUtility.Angle(targetRot, startRot, out var rotationOffset);
            var rotationLerp = rotationOffset / angleStabilization;

            BurstMathUtility.Angle(targetRot, alternateStartRot, out var alternateRotationOffset);
            var alternateRotationLerp = alternateRotationOffset / alternateStabilization;

            if (alternateRotationLerp < rotationLerp)
            {
                alternateRotationLerp = CalculateStabilizedLerp(alternateRotationLerp, deltaTime * scaleFactor);
                resultRot = math.slerp(alternateStartRot, targetRot, alternateRotationLerp);
            }
            else
            {
                rotationLerp = CalculateStabilizedLerp(rotationLerp, deltaTime * scaleFactor);
                resultRot = math.slerp(startRot, targetRot, rotationLerp);
            }
        }

        /// <summary>
        /// Calculates a lerp value for stabilizing between a historic and current value based on their distance.
        /// The historic value is weighted more heavily the closer the distance is to 0.
        /// At a distance greater than 1, the current value is used.
        /// This filters out jitter when input is trying to be held still or moved slowly while preserving low latency for large movement.
        /// </summary>
        /// <param name="distance">The distance between a historic and current value of motion or input.</param>
        /// <param name="timeSlice">How much time has passed between when these values were recorded.</param>
        /// <returns>Returns the stabilized lerp value.</returns>
        [BurstCompile]
        private static float CalculateStabilizedLerp(float distance, float timeSlice)
        {
            // The original angle stabilization code just used distance directly
            // This feels great in VR but is frame-dependent on experiences running at 90 fps
            //return Mathf.Clamp01(distance);

            // We can estimate a time-independent analog
            var originalLerp = distance;

            // If the distance has moved far enough, just use the current value for low latency movement
            if (originalLerp >= 1f)
                return 1f;

            // If the values haven't changed, then it doesn't matter what the value is so we'll just use the historic one
            if (originalLerp <= 0f)
                return 0f;

            // For fps higher than 90 fps, we scale this value
            // For fps lower than 90 fps, we take advantage of the fact that each time this algorithm
            // runs with the same values, the remaining lerp distance squares itself
            // We estimate this up to 3 time slices.  At that point the numbers just get too small to be useful
            // (and any VR experience running at 30 fps is going to be pretty rough, even with re-projection)
            var doubleFrameLerp = originalLerp - originalLerp * originalLerp;
            var tripleFrameLerp = doubleFrameLerp * doubleFrameLerp;

            var localTimeSlice = timeSlice / K90FPS;

            var firstSlice = math.clamp(localTimeSlice, 0f, 1f);
            var secondSlice = math.clamp(localTimeSlice - 1f, 0f, 1f);
            var thirdSlice = math.clamp(localTimeSlice - 2f, 0f, 1f);

            return originalLerp * firstSlice + doubleFrameLerp * secondSlice + tripleFrameLerp * thirdSlice;
        }

        /// <summary>
        /// Helper function that calculates the rotation values needed for <see cref="StabilizeOptimalRotation"/>.
        /// </summary>
        /// <param name="currentPosition">The pre-stabilized position of the ray.</param>
        /// <param name="resultPosition">The stabilized position of the ray.</param>
        /// <param name="forward">The pre-stabilized ray forward.</param>
        /// <param name="up">The pre-stabilized ray up.</param>
        /// <param name="rayEnd">The calculated ray endpoint of the last frame.</param>
        /// <param name="invScale">The scalar that preserves local scaling.</param>
        /// <param name="angleStabilization">Maximum range (in degrees) that angle stabilization is applied.</param>
        /// <param name="antiRotation">The rotation that will make the stabilized ray point to the previous endpoint.</param>
        /// <param name="scaleFactor">Scalar to apply additional stabilization over the default calculation.</param>
        /// <param name="targetAngleScale">Maximum range (in degrees) that angle stabilization is applied, for returning the stabilized ray to the previous endpoint.</param>
        [BurstCompile]
        private static void CalculateRotationParams(in float3 currentPosition, in float3 resultPosition, in float3 forward, in float3 up, in float3 rayEnd, float invScale, float angleStabilization,
                                                out quaternion antiRotation, out float scaleFactor, out float targetAngleScale)
        {
            var rayLength = math.length(rayEnd - currentPosition);
            var linearRayEnd = currentPosition + forward * rayLength;

            antiRotation = quaternion.LookRotationSafe(linearRayEnd - resultPosition, up);
            scaleFactor = 1f + math.log(math.max(rayLength * invScale, 1f));
            targetAngleScale = angleStabilization * math.clamp(scaleFactor, 1f, 3f);
        }
        #endregion
    }
}
