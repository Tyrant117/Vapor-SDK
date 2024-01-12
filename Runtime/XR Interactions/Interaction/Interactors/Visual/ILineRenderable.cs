using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    /// <summary>
    /// Get line points and hit point info for rendering.
    /// </summary>
    /// <seealso cref="VXRInteractorLineVisual"/>
    /// <seealso cref="XRRayInteractor"/>
    public interface ILineRenderable
    {
        /// <summary>
        /// Gets the polygonal chain represented by a list of endpoints which form line segments to approximate the curve.
        /// Positions are in world space coordinates.
        /// </summary>
        /// <param name="linePoints">When this method returns, contains the sample points if successful.</param>
        /// <param name="numPoints">When this method returns, contains the number of sample points if successful.</param>
        /// <returns>Returns <see langword="true"/> if the sample points form a valid line, such as by having at least two points.
        /// Otherwise, returns <see langword="false"/>.</returns>
        /// <remarks>
        /// Getting line points with <see cref="Vector3"/> array is much less performant than using a native array.
        /// Use <see cref="IAdvancedLineRenderable.GetLinePoints(ref NativeArray{Vector3},out int,Ray?)"/> instead if available.
        /// </remarks>
        bool GetLinePoints(ref Vector3[] linePoints, out int numPoints);

        /// <summary>
        /// Gets the current ray cast hit information, if a hit occurs. It returns the world position and the normal vector
        /// of the hit point, and its position in linePoints.
        /// </summary>
        /// <param name="position">When this method returns, contains the world position of the ray impact point if a hit occurred.</param>
        /// <param name="normal">When this method returns, contains the world normal of the surface the ray hit if a hit occurred.</param>
        /// <param name="positionInLine">When this method returns, contains the index of the sample endpoint within the list of points returned by <see cref="GetLinePoints"/>
        /// where a hit occurred. Otherwise, a value of <c>0</c> if no hit occurred.</param>
        /// <param name="isValidTarget">When this method returns, contains whether both a hit occurred and it is a valid target for interaction.</param>
        /// <returns>Returns <see langword="true"/> if a hit occurs, implying the ray cast hit information is valid. Otherwise, returns <see langword="false"/>.</returns>
        bool TryGetHitInfo(out Vector3 position, out Vector3 normal, out int positionInLine, out bool isValidTarget);
    }
}
