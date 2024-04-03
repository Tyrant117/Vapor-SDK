using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace VaporGraphToolsEditor
{
    public class GraphToolsGraphView<T> : GraphView where T : ScriptableObject
    {
        public GraphEditorView<T> View { get; }
        public T Graph { get; private set; }

        public GraphToolsGraphView()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/GraphToolsGraphView"));
        }

        public GraphToolsGraphView(GraphEditorView<T> view, T graph) : this()
        {
            View = view;
            Graph = graph;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var allPorts = new List<Port>();
            var ports = new List<Port>();

            foreach (var node in View.EditorNodes)
            {
                allPorts.AddRange(node.Ports);
            }

            foreach (var p in allPorts)
            {
                if (p == startPort) { continue; }
                if (p.node == startPort.node) { continue; }
                if (p.direction == startPort.direction) { continue; }
                if (p.portType == startPort.portType)
                {
                    ports.Add(p);
                }
                else if (p.userData is HashSet<Type> dynamicPorts && dynamicPorts.Contains(startPort.portType))
                {
                    ports.Add(p);
                }
                else if (startPort.portType == typeof(DynamicValuePort) && startPort.userData is HashSet<Type> dynamicStartPort && dynamicStartPort.Contains(p.portType))
                {
                    ports.Add(p);
                }
            }

            return ports;
        }
    }
}
