using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcodeForGo
{
    public static class PeerUpdateOrder
    {
        public enum UpdatePhase
        {
            /// <summary>
            /// Frame-rate independent. Corresponds with the <c>MonoBehaviour.FixedUpdate</c> method.
            /// </summary>
            Fixed,

            /// <summary>
            /// Called every frame. Corresponds with the <c>MonoBehaviour.Update</c> method.
            /// </summary>
            Dynamic,

            /// <summary>
            /// Called at the end of every frame.  Corresponds with the <c>MonoBehaviour.LateUpdate</c> method.
            /// </summary>
            Late,
        }
    }
}
