using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporGraphToolsEditor.Math
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class GraphToolsMathParameterAttribute : Attribute
    {
        public int SlotId { get; }

        public GraphToolsMathParameterAttribute(int slotId)
        {
            SlotId = slotId;
        }        
    }
}
