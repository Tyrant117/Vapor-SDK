using System;

namespace VaporGraphTools
{
    [SearchableNode("Math/Logic/If Else", "If Else")]
    public class IfElseNodeSo : MathNodeSo
    {
        [NodeParam("If", 0, true, typeof(bool))]
        public NodeSo If;
        [NodeParam("True", 1, true, typeof(float))]
        public NodeSo True;
        [NodeParam("False", 2, true, typeof(float))]
        public NodeSo False;

        public int ConnectedPort_If;
        public int ConnectedPort_True;
        public int ConnectedPort_False;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<bool> _if;
        [NonSerialized]
        private IEvaluatorNode<float> _true;
        [NonSerialized]
        private IEvaluatorNode<float> _false;

        public override float Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _if = (IEvaluatorNode<bool>)If;
                _true = (IEvaluatorNode<float>)True;
                _false = (IEvaluatorNode<float>)False;
                _hasInit = true;
            }

            return _if.Evaluate(externalValues, ConnectedPort_If) ? _true.Evaluate(externalValues, ConnectedPort_True) : _false.Evaluate(externalValues, ConnectedPort_False);
        }
    }
}
