using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace VaporGraphTools
{
    public class NodeSo : ScriptableObject, IGuidNode, IGraphPosition
    {
        [SerializeField]
        private string _guid;
        [SerializeField]
        private string _linkingGuid;
        public string LinkingGuid { get => _linkingGuid; set => _linkingGuid = value; }

        [SerializeField]
        private Rect _position;
        public Rect Position { get => _position; set => _position = value; }

        [SerializeField]
        private string _name;        
        public string Name { get => _name; set => _name = value; }

        [SerializeField]
        private List<EdgeConnection> _edges = new();
        public List<EdgeConnection> Edges { get => _edges; set => _edges = value; }

        
        public string GetGuid() => _guid;
        public void SetGuid(string guid) => _guid = guid;

        [Conditional("UNITY_EDITOR")]
        public virtual void LinkNodeData(List<NodeSo> nodesToLink, Action<NodeSo> callback)
        {
            Edges.Sort((l, r) => l.PortIndex.CompareTo(r.PortIndex));

            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            FindNodeAndConnectedPort(fields, nodesToLink);
        }

        [Conditional("UNITY_EDITOR")]
        private void FindNodeAndConnectedPort(FieldInfo[] fields, List<NodeSo> nodesToLink)
        {
            var ins = fields.Where(x => Attribute.IsDefined(x, typeof(NodeParamAttribute)));
            foreach (var port in ins)
            {
                var atr = port.GetCustomAttribute<NodeParamAttribute>();
                var edge = Edges.FirstOrDefault(e => e.PortIndex == atr.PortIndex);
                if (edge != null)
                {
                    var node = nodesToLink.First(x => edge == x.GetGuid());
                    port.SetValue(this, node);
                    var portIndex = GetType().GetField($"ConnectedPort_{port.Name}");
                    Assert.IsNotNull(portIndex, $"There is no public field with the name ConnectedPort_{port.Name}, all fields marked with a NodeParamAttribute," +
                        $" must have an integer field with the name punlic int ConnectedPort_<NodeParamAttribute.FieldName>");
                    portIndex.SetValue(this, edge.ConnectedPortIndex);
                }
                else
                {
                    Assert.IsFalse(atr.Required, $"Node:{name} of type:{GetType()} has a NodeParamAttribute with the name {atr.ParamName} that is a required node," +
                        $" but there is no edge connecting to it in the graph.");
                }
            }
        }
    }
}
