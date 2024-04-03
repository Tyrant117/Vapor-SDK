using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VaporGraphToolsEditor.Math
{
    public abstract class AbstractMathNode
    {
        public string Name { get; set; }

        public abstract MethodInfo GetFunctionToConvert();
    }
}
