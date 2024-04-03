using System;
using UnityEngine;

namespace VaporGraphTools.Math
{
    [SearchableNode("Math/Basic/Square Root", "Square Root")]
    public class SquareRootNodeSo : MathNodeSo
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

            return Mathf.Sqrt(_a.Evaluate(externalValues, ConnectedPort_A));
        }
    }
}
