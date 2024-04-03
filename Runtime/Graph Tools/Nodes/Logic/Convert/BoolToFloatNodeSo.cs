using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace VaporGraphTools
{
    [SearchableNode("Logic/Convert/Bool To Float", "Bool to Float")]
    public class BoolToFloatNodeSo : MathNodeSo
    {
        [NodeParam("Bool", 0, true, typeof(bool))]
        public NodeSo Bool;

        public int ConnectedPort_Bool;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<bool> _a;

        public override float Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<bool>)Bool;
                _hasInit = true;
            }

            return Convert.ToSingle(_a.Evaluate(externalValues, 0));
        }

        //public override void LinkNodeData(List<NodeSo> nodesToLink, Action<NodeSo> callback)
        //{
        //    Assert.IsTrue(Edges.Count == 1, $"Node:{name} of type:{nameof(BoolToFloatNodeSo)} must have a 1 edges. Edge Count:{Edges.Count}");

        //    _bool = nodesToLink.First(x => Edges[0] == x.GetGuid());
        //}
    }
}
