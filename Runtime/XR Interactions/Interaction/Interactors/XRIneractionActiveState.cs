using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    public struct XRIneractionActiveState
    {
        public bool Performed;
        public bool WasPerformedThisFrame;
        public float Value;

        public XRIneractionActiveState(bool performed, bool wasPerformedThisFrame, float value)
        {
            Performed = performed;
            WasPerformedThisFrame = wasPerformedThisFrame;
            Value = value;
        }
    }
}
