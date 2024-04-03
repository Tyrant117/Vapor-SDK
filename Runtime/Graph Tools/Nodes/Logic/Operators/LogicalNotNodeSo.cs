using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporGraphTools
{
    [SearchableNode("Logic/Operator/Logical Not", "!A")]
    public class LogicalNotNodeSo : LogicNodeSo
    {
        [NodeParam("A", 0, true, typeof(bool))]
        public NodeSo A;

        public int ConnectedPort_A;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<bool> _a;

        public override bool Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<bool>)A;
                _hasInit = true;
            }

            return !_a.Evaluate(externalValues, ConnectedPort_A);
        }

        //public override void LinkNodeData(List<NodeSo> nodesToLink, Action<NodeSo> callback)
        //{
        //    Assert.IsTrue(Edges.Count == 2, $"Node:{name} of type:{nameof(AndNodeSo)} must have a 2 edges. Edge Count:{Edges.Count}");

        //    base.LinkNodeData(nodesToLink, callback);
        //    A = nodesToLink.First(x => Edges[0] == x.GetGuid());
        //    B = nodesToLink.First(x => Edges[1] == x.GetGuid());
        //}
    }
}
