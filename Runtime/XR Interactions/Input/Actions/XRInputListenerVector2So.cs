using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    [CreateAssetMenu(fileName = "Listener", menuName = "Vapor/XR/Input/Vector2 Listener", order = 102)]
    public class XRInputListenerVector2So : XRInputListenerSo
    {
        public Vector2 ReadValue()
        {
            return ReadValue<Vector2>();
        }
    }
}
