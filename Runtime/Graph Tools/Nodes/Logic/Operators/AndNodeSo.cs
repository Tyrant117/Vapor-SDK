using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace VaporGraphTools
{
    [SearchableNode("Logic/Conditional/Logical And", "A && B")]
    public class AndNodeSo : LogicNodeSo
    {
        [NodeParam("A", 0, true, typeof(bool))]
        public NodeSo A;
        [NodeParam("B", 1, true, typeof(bool))]
        public NodeSo B;

        public int ConnectedPort_A;
        public int ConnectedPort_B;

        [NonSerialized]
        private bool _hasInit;
        [NonSerialized]
        private IEvaluatorNode<bool> _a;
        [NonSerialized]
        private IEvaluatorNode<bool> _b;

        public override bool Evaluate(IExternalValueGetter externalValues, int portIndex)
        {
            if (!_hasInit)
            {
                _a = (IEvaluatorNode<bool>)A;
                _b = (IEvaluatorNode<bool>)B;
                _hasInit = true;
            }

            return _a.Evaluate(externalValues, ConnectedPort_A) && _b.Evaluate(externalValues, ConnectedPort_B);
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
