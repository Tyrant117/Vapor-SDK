using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporGraphTools
{
    [SearchableNode("Value/Exposed Property", "Exposed Property", "values"), NodeIsToken, NodeResult("", 0, typeof(bool), typeof(int), typeof(float))]
    public class ExposedPropertyNodeSo : NodeSo, IEvaluatorNode<bool>, IEvaluatorNode<int>, IEvaluatorNode<float>
    {
        [SerializeField]
        private string _valueName;
        public string ValueName { get => _valueName; set => _valueName = value; }

        bool IEvaluatorNode<bool>.GetValue(int portIndex)
        {
            throw new System.NotImplementedException();
        }
        int IEvaluatorNode<int>.GetValue(int portIndex)
        {
            throw new System.NotImplementedException();
        }
        float IEvaluatorNode<float>.GetValue(int portIndex)
        {
            throw new System.NotImplementedException();
        }


        bool IEvaluatorNode<bool>.Evaluate(IExternalValueGetter getter, int portIndex)
        {
            var val = getter.GetExposedValue(GetGuid(), typeof(bool));
            return val.BoolValue;
        }
        int IEvaluatorNode<int>.Evaluate(IExternalValueGetter getter, int portIndex)
        {
            var val = getter.GetExposedValue(GetGuid(), typeof(int));
            return val.IntValue;
        }
        float IEvaluatorNode<float>.Evaluate(IExternalValueGetter getter, int portIndex)
        {
            var val = getter.GetExposedValue(GetGuid(), typeof(float));
            return val.FloatValue;
        }

        public override void LinkNodeData(List<NodeSo> nodesToLink, Action<NodeSo> callback)
        {
            base.LinkNodeData(nodesToLink, callback);

            callback?.Invoke(this);
        }
    }
}
