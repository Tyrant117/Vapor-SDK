using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR.Locomotion.Teleportation
{
    /// <summary>
    /// The Teleport Request that describes the result of the teleportation action. Each Teleportation Interactable must fill out a Teleport Request
    /// for each teleport action.
    /// </summary>
    public struct TeleportRequest
    {
        /// <summary>
        /// The position in world space of the Teleportation Destination.
        /// </summary>
        public Vector3 destinationPosition;
        /// <summary>
        /// The rotation in world space of the Teleportation Destination. This is used primarily for matching world rotations directly.
        /// </summary>
        public Quaternion destinationRotation;
        /// <summary>
        ///  The Time (in unix epoch) of the request.
        /// </summary>
        public float requestTime;
        /// <summary>
        /// The option of how to orient the rig after teleportation.
        /// </summary>
        public MatchOrientation matchOrientation;
    }
}
