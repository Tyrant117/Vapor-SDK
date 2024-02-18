using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    public enum InputSourceType
    {
        /// <summary>
        /// No input will be used
        /// </summary>
        None,
        /// <summary>
        /// Legacy input will be used
        /// </summary>
        Legacy,
        /// <summary>
        /// Input Action References will be used.
        /// </summary>
        ActionReference,
    }

    /// <summary>
    /// One of the 8 carindal directions.
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
}
