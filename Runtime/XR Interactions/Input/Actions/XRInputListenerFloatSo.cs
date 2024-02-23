using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    [CreateAssetMenu(fileName = "Listener", menuName = "Vapor/XR/Input/Float Listener", order = 101)]
    public class XRInputListenerFloatSo : XRInputListenerSo
    {
        public float ReadValue()
        {
            return ReadValue<float>();
        }
    }
}
