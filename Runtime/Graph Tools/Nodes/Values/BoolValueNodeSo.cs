using System.Runtime.CompilerServices;

namespace VaporGraphTools
{
    [SearchableNode("Value/Bool Value", "Bool")]
    public class BoolValueNodeSo : ValueNodeSo<bool>, IEvaluatorNode<float>, IEvaluatorNode<bool>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IEvaluatorNode<bool>.GetValue(int portIndex) => Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float IEvaluatorNode<float>.GetValue(int portIndex) => Value ? 1 : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float IEvaluatorNode<float>.Evaluate(IExternalValueGetter externalValues, int portIndex) => Value ? 1 : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IEvaluatorNode<bool>.Evaluate(IExternalValueGetter getter, int portIndex) => Value;   
    }
}
