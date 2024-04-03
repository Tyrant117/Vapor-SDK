using System.Runtime.CompilerServices;
using VaporGraphTools.Math;
using VaporKeys;

namespace VaporGraphTools
{
    public class KeyValueNodeSo : ValueNodeSo<KeyDropdownValue>, IEvaluatorNode<int>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetValue(int portIndex) => Value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Evaluate(IExternalValueGetter getter, int portIndex) => Value;
    }
}
