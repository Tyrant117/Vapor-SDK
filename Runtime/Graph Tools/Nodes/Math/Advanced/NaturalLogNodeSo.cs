using System;
using UnityEngine;

namespace VaporGraphTools.Math
{
    [SearchableNode("Math/Advanced/Natural Log", "Natural Log")]
    public class NaturalLogNodeSo : MathNodeSo
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

            return Mathf.Log(_a.Evaluate(externalValues, ConnectedPort_A), 2);
        }
    }
}
