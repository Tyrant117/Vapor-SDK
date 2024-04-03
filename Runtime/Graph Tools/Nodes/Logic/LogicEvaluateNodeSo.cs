using System;

namespace VaporGraphTools
{
    public class LogicEvaluateNodeSo : NodeSo, IEvaluatorNode<bool>
    {
        [NodeParam("Start", 0, true, typeof(bool))]
        public NodeSo Start;

        public int ConnectedPort_Start;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<bool> _eval;

        public bool GetValue(int portIndex)
        {
            if (!_hasInit)
            {
                _eval = (IEvaluatorNode<bool>)Start;
                _hasInit = true;
            }

            return _eval.GetValue(portIndex);
        }

        public bool Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _eval = (IEvaluatorNode<bool>)Start;
                _hasInit = true;
            }

            return _eval.Evaluate(externalValues, ConnectedPort_Start);
        }
    }
}
