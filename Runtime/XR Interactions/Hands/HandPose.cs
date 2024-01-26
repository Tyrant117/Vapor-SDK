using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    [System.Serializable]
    public class HandPose
    {
        [SerializeField]
        private Pose _poseOffset;
        [SerializeField]
        private List<Pose> _fingerPoses = new();
        
        public Pose PoseOffset { get => _poseOffset; set => _poseOffset = value; }
        public List<Pose> FingerPoses => _fingerPoses;

        public static HandPose Lerp(HandPose from, HandPose to, float fraction)
        {
            var interpolatedPose = new HandPose
            {
                _poseOffset = new Pose(Vector3.Lerp(from._poseOffset.position, to._poseOffset.position, fraction), Quaternion.Slerp(from._poseOffset.rotation, to._poseOffset.rotation, fraction))
            };
            
            var fingerCount = from._fingerPoses.Count;
            if (fingerCount > to._fingerPoses.Count)
            {
                fingerCount = to._fingerPoses.Count;
            }
            for (var i = 0; i < fingerCount; i++)
            {
                interpolatedPose._fingerPoses.Add(new Pose(Vector3.Lerp(from._fingerPoses[i].position, to._fingerPoses[i].position, fraction), Quaternion.Slerp(from._fingerPoses[i].rotation, to._fingerPoses[i].rotation, fraction)));
            }
            
            return interpolatedPose;
        }

        public static void Lerp(HandPose poseToSet, HandPose from, HandPose to, float fraction)
        {
            poseToSet._poseOffset = new Pose(Vector3.Lerp(from._poseOffset.position, to._poseOffset.position, fraction),
                Quaternion.Slerp(from._poseOffset.rotation, to._poseOffset.rotation, fraction));

            var fingerCount = Mathf.Min(poseToSet._fingerPoses.Count, from._fingerPoses.Count, to._fingerPoses.Count);
            for (var i = 0; i < fingerCount; i++)
            {
                poseToSet._fingerPoses[i] = new Pose(Vector3.Lerp(from._fingerPoses[i].position, to._fingerPoses[i].position, fraction),
                    Quaternion.Slerp(from._fingerPoses[i].rotation, to._fingerPoses[i].rotation, fraction));
            }
        }

        public HandPose Copy()
        {
            var copy = new HandPose()
            {
                _poseOffset = _poseOffset
            };
            copy._fingerPoses.AddRange(_fingerPoses);
            return copy;
        }
    }
}
