using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    /// <summary>
    /// Compares ray cast hits by distance, to sort in ascending order.
    /// </summary>
    public sealed class RaycastHitComparer : IComparer<RaycastHit>
    {
        /// <summary>
        /// Compares ray cast hits by distance in ascending order.
        /// </summary>
        /// <param name="a">The first ray cast hit to compare.</param>
        /// <param name="b">The second ray cast hit to compare.</param>
        /// <returns>Returns less than 0 if a is closer than b. 0 if a and b are equal. Greater than 0 if b is closer than a.</returns>
        public int Compare(RaycastHit a, RaycastHit b)
        {
            var aDistance = a.collider != null ? a.distance : float.MaxValue;
            var bDistance = b.collider != null ? b.distance : float.MaxValue;
            return aDistance.CompareTo(bDistance);
        }
    }
}
