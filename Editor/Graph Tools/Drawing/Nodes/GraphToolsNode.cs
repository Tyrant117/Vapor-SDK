using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{
    public abstract class GraphToolsNode<T, U> : Node, IGraphToolsNode where T : ScriptableObject where U : NodeSo
    {
        private U _node;
        public U Node { get => _node; protected set => _node = value; }

        private List<Port> _ports = new();
        public List<Port> Ports => _ports;

        public GraphEditorView<T> View { get; protected set; }

        public NodeSo GetNode() => Node;
    }
}
