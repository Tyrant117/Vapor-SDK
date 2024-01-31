using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    public class TriggerPoseZone : MonoBehaviour
    {
        [SerializeField]
        private HandPoseDatum _pose;
        [SerializeField]
        private float _duration;

        public HandPose Pose => _pose.Value;
        public float Duration => _duration;
    }
}
