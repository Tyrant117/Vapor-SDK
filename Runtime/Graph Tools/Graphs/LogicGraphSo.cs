using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporGraphTools
{
    public class LogicGraphSo : ScriptableObject
    {
        [SerializeField]
        private LogicEvaluateNodeSo _root;
        public LogicEvaluateNodeSo Root { get => _root; set => _root = value; }

        [SerializeField]
        private List<ExposedPropertyNodeSo> _exposedProperties = new();
        public List<ExposedPropertyNodeSo> ExposedProperties => _exposedProperties;

        public bool EvaluateGraph(IExternalValueGetter getter)
        {
            return Root.Evaluate(getter, -1);
        }
    }
}
