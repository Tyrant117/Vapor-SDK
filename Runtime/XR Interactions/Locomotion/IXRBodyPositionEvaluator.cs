using Unity.XR.CoreUtils;
using UnityEngine;

namespace VaporXR.Locomotion
{
    /// <summary>
    /// Interface for an object that determines the position of the user's body for a given <see cref="XROrigin"/>.
    /// This is used by implementations of <see cref="IXRBodyTransformation"/> to transform the XR Origin using the
    /// user's body, rather than the tracking origin itself, as the frame of reference.
    /// </summary>
    public interface IXRBodyPositionEvaluator
    {
        /// <summary>
        /// Gets the position of where the user's body is grounded (e.g. their feet), in the local space of the <see cref="XROrigin.Origin"/>.
        /// </summary>
        /// <param name="xrOrigin">The XR Origin whose body position to get.</param>
        /// <returns>Returns the position of where the user's body is grounded, in the local space of the <see cref="XROrigin.Origin"/>.</returns>
        Vector3 GetBodyGroundLocalPosition(VXROrigin xrOrigin);
    }

    /// <summary>
    /// Extension methods for <see cref="IXRBodyPositionEvaluator"/>.
    /// </summary>
    public static class XRBodyPositionEvaluatorExtensions
    {
        /// <summary>
        /// Gets the world position of where the user's body is grounded (e.g. their feet).
        /// </summary>
        /// <param name="evaluator">The evaluator that determines the body position.</param>
        /// <param name="xrOrigin">The XR Origin whose body position to get.</param>
        /// <returns>Returns the world position of where the user's body is grounded.</returns>
        public static Vector3 GetBodyGroundWorldPosition(this IXRBodyPositionEvaluator evaluator, VXROrigin xrOrigin)
        {
            return xrOrigin.Origin.transform.TransformPoint(evaluator.GetBodyGroundLocalPosition(xrOrigin));
        }
    }
}