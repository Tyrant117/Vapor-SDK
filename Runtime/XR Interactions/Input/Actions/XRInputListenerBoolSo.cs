using UnityEngine;

namespace VaporXR
{
    [CreateAssetMenu(fileName = "Listener", menuName = "Vapor/XR/Input/Bool Listener", order = 100)]
    public class XRInputListenerBoolSo : XRInputListenerSo
    {
        public bool ReadValue()
        {
            return ReadValue<bool>();
        }
    }
}
