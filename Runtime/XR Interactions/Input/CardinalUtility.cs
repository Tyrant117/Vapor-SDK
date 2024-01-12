using UnityEngine;

namespace VaporXR
{
    /// <summary>
    /// One of the four primary directions.
    /// </summary>
    /// <seealso cref="CardinalUtility"/>
    public enum Cardinal
    {
        /// <summary>
        /// North direction, e.g. forward on a thumbstick.
        /// </summary>
        North,

        /// <summary>
        /// South direction, e.g. back on a thumbstick.
        /// </summary>
        South,

        /// <summary>
        /// East direction, e.g. right on a thumbstick.
        /// </summary>
        East,

        /// <summary>
        /// West direction, e.g. left on a thumbstick.
        /// </summary>
        West,

        /// <summary>
        /// NorthWest direction, e.g. forward-left on a thumbstick.
        /// </summary>
        NorthWest,

        /// <summary>
        /// NorthEast direction, e.g. forward-right on a thumbstick.
        /// </summary>
        NorthEast,

        /// <summary>
        /// SouthEast direction, e.g. back-right on a thumbstick.
        /// </summary>
        SouthEast,

        /// <summary>
        /// SouthWest direction, e.g. back-left on a thumbstick.
        /// </summary>
        SouthWest
    }

    /// <summary>
    /// Utility functions related to <see cref="Cardinal"/> directions.
    /// </summary>
    public static class CardinalUtility
    {
        /// <summary>
        /// Get the nearest cardinal direction for a given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">Input vector, such as from a thumbstick.</param>
        /// <param name="includeCompositeCardinals">Should the composite cardinals be included in the result</param>
        /// <returns>Returns the nearest <see cref="Cardinal"/> direction.</returns>
        /// <remarks>
        /// Arbitrarily biases towards <see cref="Cardinal.North"/> and <see cref="Cardinal.South"/>
        /// to disambiguate when angle is exactly equidistant between directions.
        /// </remarks>
        public static Cardinal GetNearestCardinal(Vector2 value, bool includeCompositeCardinals)
        {
            if (includeCompositeCardinals)
            {
                // Calculate the angle in degrees using Atan2
                var angleInDegrees = Mathf.Atan2(value.x, value.y) * Mathf.Rad2Deg;

                // Ensure the angle is in the range 0-360 degrees
                if (angleInDegrees < 0)
                {
                    angleInDegrees += 360;
                }

                return angleInDegrees switch
                {
                    // Determine the cardinal direction based on the angle
                    >= 337.5f or < 22.5f => Cardinal.North,
                    >= 22.5f and < 67.5f => Cardinal.NorthEast,
                    >= 67.5f and < 112.5f => Cardinal.East,
                    >= 112.5f and < 157.5f => Cardinal.SouthEast,
                    >= 157.5f and < 202.5f => Cardinal.South,
                    >= 202.5f and < 247.5f => Cardinal.SouthWest,
                    >= 247.5f and < 292.5f => Cardinal.West,
                    _ => Cardinal.NorthWest
                };
            }
            else
            {
                // Calculate the angle in degrees using Atan2
                var angleInDegrees = Mathf.Atan2(value.x, value.y) * Mathf.Rad2Deg;

                // Ensure the angle is in the range 0-360 degrees
                if (angleInDegrees < 0)
                {
                    angleInDegrees += 360;
                }

                return angleInDegrees switch
                {
                    // Determine the cardinal direction based on the angle
                    >= 315f or < 45f => Cardinal.North,
                    >= 45f and < 135f => Cardinal.East,
                    >= 135f and < 225f => Cardinal.South,
                    _ => Cardinal.West
                };
            }
        }

        public static float GetNearestAngle(Vector2 value)
        {
            // Calculate the angle in degrees using Atan2
            float angleInDegrees = Mathf.Atan2(value.x, value.y) * Mathf.Rad2Deg;

            // Ensure the angle is in the range 0-360 degrees
            if (angleInDegrees < 0)
            {
                angleInDegrees += 360;
            }
            return angleInDegrees;
        }
    }
}
