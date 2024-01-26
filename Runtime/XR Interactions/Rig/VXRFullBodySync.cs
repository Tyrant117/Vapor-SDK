using Unity.XR.CoreUtils;
using UnityEngine;

namespace VaporXR
{
    public class VXRFullBodySync : MonoBehaviour
    {
        [SerializeField] private Transform _deviceHmd;
        [SerializeField] private Transform _deviceHmdLookTarget;
        [SerializeField] private Transform _ikHead;
        
        
        [SerializeField] private Transform _deviceLeftHand;
        [SerializeField] private Transform _ikLeftHand;
        
        [SerializeField] private Transform _deviceRightHand;
        [SerializeField] private Transform _ikRightHand;
        
        private void LateUpdate()
        {
            _ikHead.SetWorldPose(_deviceHmdLookTarget.GetWorldPose());
            _ikLeftHand.SetWorldPose(_deviceLeftHand.GetWorldPose());
            _ikRightHand.SetWorldPose(_deviceRightHand.GetWorldPose());

            transform.position = _deviceHmd.position;
            var yRot = _deviceHmd.rotation.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0, yRot, 0);
        }
    }
}
