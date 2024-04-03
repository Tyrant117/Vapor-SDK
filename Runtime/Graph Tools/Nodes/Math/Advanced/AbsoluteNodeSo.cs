using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace VaporGraphTools.Math
{
    [SearchableNode("Math/Advanced/Absolute", "Absolute")]
    public class AbsoluteNodeSo : MathNodeSo
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

            return Mathf.Abs(_a.Evaluate(externalValues, ConnectedPort_A));
        }
    }
}
