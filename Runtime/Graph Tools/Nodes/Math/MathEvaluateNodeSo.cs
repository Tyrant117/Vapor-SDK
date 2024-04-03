using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using VaporGraphTools.Math;

namespace VaporGraphTools
{
    public class MathEvaluateNodeSo : NodeSo, IEvaluatorNode<float>
    {
        public string NodeName = "Evaluate()";

        [NodeParam("Start", 0, true, typeof(float))]
        public NodeSo Start;

        public int ConnectedPort_Start;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<float> _eval;

        public float GetValue(int portIndex)
        {
            if (!_hasInit)
            {
                _eval = (IEvaluatorNode<float>)Start;
                _hasInit = true;
            }

            return _eval.GetValue(portIndex);
        }

        public float Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _eval = (IEvaluatorNode<float>)Start;
                _hasInit = true;
            }

            return _eval.Evaluate(externalValues, ConnectedPort_Start);
        }        
    }
}
