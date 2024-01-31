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
        [SerializeField]
        private Collider _collider;
        [SerializeField]
        private List<Collider> _ignoreColliders;

        private VXRHand _currentHand;

        private void Awake()
        {
            foreach(var col in _ignoreColliders)
            {
                Physics.IgnoreCollision(_collider, col, true);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var hand = other.GetComponentInParent<VXRHand>();
            if (hand)
            {
                if (_currentHand)
                {
                    _currentHand.FallbackToIdle(_duration);
                }
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
