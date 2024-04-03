using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using VaporGraphTools.Math;

namespace VaporGraphTools
{
    [SearchableNode("Value/Float Value", "Float")]
    public class FloatValueNodeSo : ValueNodeSo<float>, IEvaluatorNode<float>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetValue(int portIndex) => Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(IExternalValueGetter externalValues, int portIndex) => Value;        
    }
}
