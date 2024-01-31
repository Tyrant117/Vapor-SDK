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

        private VXRHand _currentHand;


        private void OnTriggerEnter(Collider other)
        {
            var hand = other.GetComponentInParent<VXRHand>();
            if (hand)
            {
                _currentHand = hand;
                _currentHand.SetHandPose(_pose.Value, duration: _duration);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_currentHand)
            {
                _currentHand.FallbackToIdle(_duration);
                _currentHand = null;
            }
        }
    }
}
