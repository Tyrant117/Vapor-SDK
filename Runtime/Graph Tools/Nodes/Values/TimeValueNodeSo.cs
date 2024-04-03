using System.Runtime.CompilerServices;
using UnityEngine;

namespace VaporGraphTools
{
    [SearchableNode("Value/Time Value", "Time")]
    public class TimeValueNodeSo : ValueNodeSo<float>, IEvaluatorNode<float>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetValue(int portIndex) => Time.time;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(IExternalValueGetter externalValues, int portIndex) => Time.time;        
    }
}
