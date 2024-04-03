using System;

namespace VaporGraphTools.Math
{
    [SearchableNode("Math/Advanced/Reciprocal", "Reciprocal")]
    public class ReciprocalNodeSo : MathNodeSo
    {
        [NodeParam("A", 0, true, typeof(float))]
        public NodeSo A;

        public int ConnectedPort_A;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<float> _a;

        public override float Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<float>)A;
                _hasInit = true;
            }

            return 1 / _a.Evaluate(externalValues, ConnectedPort_A);
        }
    }
}
