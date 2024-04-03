using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VaporGraphToolsEditor.Math
{
    [GraphToolsMathNode("Math/Basic/Add")]
    public class AddNode : AbstractMathNode
    {
        public AddNode()
        {
            Name = "Add";
        }

        public override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Add", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Add(
            [GraphToolsMathParameter(0)] float A,
            [GraphToolsMathParameter(1)] float B,
            [GraphToolsMathParameter(2)] float Out)
        {
            return
@"
{
    Out = A + B;
}
";
        }
    }
}
