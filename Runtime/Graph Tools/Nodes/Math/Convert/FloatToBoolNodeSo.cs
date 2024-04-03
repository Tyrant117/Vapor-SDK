using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace VaporGraphTools
{
    [SearchableNode("Math/Convert/Float To Bool", "Float to Bool")]
    public class FloatToBoolNodeSo : LogicNodeSo
    {
        [NodeParam("Float", 0, true, typeof(float))]
        public NodeSo Float;

        public int ConnectedPort_Float;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<float> _a;

        public override bool Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<float>)Float;
                _hasInit = true;
            }

            return Convert.ToBoolean(_a.Evaluate(externalValues, ConnectedPort_Float));
        }
    }
}
