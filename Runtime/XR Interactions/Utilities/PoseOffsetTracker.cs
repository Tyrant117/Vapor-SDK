using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    /// <summary>
    /// A simple script that tracks two transforms and averages the position and rotation offsets
    /// from one to the other.
    /// </summary>
    public class PoseOffsetTracker : MonoBehaviour
    {
        public Transform From;
        public Transform To;

        public Vector3 PositionOffset;
        public Vector3 RotationOffset;


        private void Update()
        {
            var pOff = From.InverseTransformPoint(To.position);
            var rOff = Quaternion.Inverse(From.rotation) * To.rotation;// Quaternion.FromToRotation(From.forward, To.forward).eulerAngles;
                       

            PositionOffset = pOff;
            RotationOffset = rOff.eulerAngles;

            //Vector3(0,-0.03,-0.095)
            //Vector3(-0.046329204,0.00984701794,0.087644957)
            //Vector3(46.9339638,27.8914871,41.7441025)
            //Vector3(0,-0.067272447,0.0734807253)
        }
    }
}
