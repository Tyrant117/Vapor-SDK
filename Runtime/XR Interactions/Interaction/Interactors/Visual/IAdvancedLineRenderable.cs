using Unity.Collections;
using UnityEngine;

namespace VaporXR
{
    /// <summary>
    /// An advanced interface for providing line data for rendering with additional functionality.
    /// </summary>
    /// <seealso cref="VXRInteractorLineVisual"/>
    /// <seealso cref="VXRRayInteractor"/>
    public interface IAdvancedLineRenderable : ILineRenderable
    {
        /// <summary>
        /// Gets the polygonal chain represented by a list of endpoints which form line segments to approximate the curve.
        /// Positions are in world space coordinates.
        /// </summary>
        /// <param name="linePoints">When this method returns, contains the sample points if successful.</param>
        /// <param name="numPoints">When this method returns, contains the number of sample points if successful.</param>
        /// <param name="rayOriginOverride">Optional ray origin override used when re-computing the line.</param>
        /// <returns>Returns <see langword="true"/> if the sample points form a valid line, such as by having at least two points.
        /// Otherwise, returns <see langword="false"/>.</returns>
        bool GetLinePoints(ref NativeArray<Vector3> linePoints, out int numPoints, Ray? rayOriginOverride = null);

        /// <summary>
        /// Gets the line origin and direction.
        /// Origin and Direction are in world space coordinates.
        /// </summary>
        /// <param name="origin">Point in space where the line originates from.</param>
        /// <param name="direction">Direction vector used to draw line.</param>
        void GetLineOriginAndDirection(out Vector3 origin, out Vector3 direction);
    }
}
