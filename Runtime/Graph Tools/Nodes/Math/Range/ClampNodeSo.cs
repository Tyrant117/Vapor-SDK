using System;
using UnityEngine;

namespace VaporGraphTools.Math
{
    [SearchableNode("Math/Range/Clamp", "Clamp")]
    public class ClampNodeSo : MathNodeSo
    {
        [NodeParam("Min", 0, true, typeof(float))]
        public NodeSo A;
        [NodeParam("Max", 1, true, typeof(float))]
        public NodeSo B;
        [NodeParam("Value", 2, true, typeof(float))]
        public NodeSo C;

        public int ConnectedPort_A;
        public int ConnectedPort_B;
        public int ConnectedPort_C;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<float> _a;
        [NonSerialized]
        private IEvaluatorNode<float> _b;
        [NonSerialized]
        private IEvaluatorNode<float> _c;

        public override float Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<float>)A;
                _b = (IEvaluatorNode<float>)B;
                _c = (IEvaluatorNode<float>)C;
                _hasInit = true;
            }

            return Mathf.Clamp(_c.Evaluate(externalValues, ConnectedPort_C), _a.Evaluate(externalValues, ConnectedPort_A), _b.Evaluate(externalValues, ConnectedPort_B));
        }
    }
}
