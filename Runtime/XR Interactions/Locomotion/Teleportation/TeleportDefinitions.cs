using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR.Locomotion.Teleportation
{
    /// <summary>
    /// The option of which object's orientation in the rig Unity matches with the destination after teleporting.
    /// </summary>
    public enum MatchOrientation
    {
        /// <summary>
        /// After teleporting the XR Origin will be positioned such that its up vector matches world space up.
        /// </summary>
        WorldSpaceUp,

        /// <summary>
        /// After teleporting the XR Origin will be positioned such that its up vector matches target up.
        /// </summary>
        TargetUp,

        /// <summary>
        /// After teleporting the XR Origin will be positioned such that its up and forward vectors match target up and forward, respectively.
        /// </summary>
        TargetUpAndForward,

        /// <summary>
        /// After teleporting the XR Origin will not attempt to match any orientation.
        /// </summary>
        None,
    }
}
