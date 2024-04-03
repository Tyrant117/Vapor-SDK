using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace VaporGraphTools
{
    [SearchableNode("Logic/Chance/Random Chance", "Random Chance")]
    public class RandomChanceNodeSo : LogicNodeSo
    {
        [NodeParam("Chance", 0, true, typeof(float))]
        public NodeSo Chance;

        public int ConnectedPort_Chance;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<float> _chance;

        public override bool Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _chance = (IEvaluatorNode<float>)Chance;
                _hasInit = true;
            }

            return UnityEngine.Random.Range(0, 1f) <= _chance.Evaluate(externalValues, ConnectedPort_Chance);
        }
    }
}
