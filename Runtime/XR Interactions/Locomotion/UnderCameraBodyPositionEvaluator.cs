using Unity.XR.CoreUtils;
using UnityEngine;

namespace VaporXR.Locomotion
{
    /// <summary>
    /// Scriptable object that estimates the user's body position by projecting the position of the camera onto the
    /// XZ plane of the <see cref="XROrigin"/>.
    /// </summary>
    /// <remarks>
    /// This is the default <see cref="VXRBodyTransformer.BodyPositionEvaluator"/> for an <see cref="VXRBodyTransformer"/>.
    /// </remarks>
    [CreateAssetMenu(fileName = "UnderCameraBodyPositionEvaluator", menuName = "XR/Locomotion/Under Camera Body Position Evaluator")]
    public class UnderCameraBodyPositionEvaluator : ScriptableObject, IXRBodyPositionEvaluator
    {
        /// <inheritdoc/>
        public Vector3 GetBodyGroundLocalPosition(XROrigin xrOrigin)
        {
            var bodyPosition = xrOrigin.CameraInOriginSpacePos;
            bodyPosition.y = 0f;
            return bodyPosition;
        }
    }
}