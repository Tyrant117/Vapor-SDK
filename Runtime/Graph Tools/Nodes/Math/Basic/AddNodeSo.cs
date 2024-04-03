using System;

namespace VaporGraphTools.Math
{
    [SearchableNode("Math/Basic/Add", "Add"), NodeResult("Out", 2, typeof(float))]
    public class AddNodeSo : MathNodeSo
    {
        [NodeParam("A", 0, true, typeof(float))]
        public NodeSo A;
        [NodeParam("B", 1, true, typeof(float))]
        public NodeSo B;

        public int ConnectedPort_A;
        public int ConnectedPort_B;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<float> _a;
        [NonSerialized]
        private IEvaluatorNode<float> _b;

        public override float Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<float>)A;
                _b = (IEvaluatorNode<float>)B;
                _hasInit = true;
            }

            return _a.Evaluate(externalValues, ConnectedPort_A) + _b.Evaluate(externalValues, ConnectedPort_B);
        }
    }
}
