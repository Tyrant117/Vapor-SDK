using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace VaporGraphTools
{
    [SearchableNode("Logic/Operators/Equal To", "A == B")]
    public class EqualToNodeSo : LogicNodeSo
    {
        [NodeParam("A", 0, true, typeof(int))]
        public NodeSo A;
        [NodeParam("B", 1, true, typeof(int))]
        public NodeSo B;

        public int ConnectedPort_A;
        public int ConnectedPort_B;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<int> _a;
        [NonSerialized]
        private IEvaluatorNode<int> _b;

        public override bool Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<int>)A;
                _b = (IEvaluatorNode<int>)B;
                _hasInit = true;
            }

            return _a.Evaluate(externalValues, ConnectedPort_A) == _b.Evaluate(externalValues, ConnectedPort_B);
        }
    }
}
