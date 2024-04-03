using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace VaporGraphTools
{
    [SearchableNode("Value/Int Value", "Int")]
    public class IntValueNodeSo : ValueNodeSo<int>, IEvaluatorNode<int>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetValue(int portIndex) => Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Evaluate(IExternalValueGetter externalValues, int portIndex) => Value;
    }
}
