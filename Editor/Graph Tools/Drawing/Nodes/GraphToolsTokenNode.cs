using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{
    public class GraphToolsTokenNode<T, U> : TokenNode, IGraphToolsNode where T : ScriptableObject where U : NodeSo
    {
        private U _node;
        public U Node { get => _node; protected set => _node = value; }

        private List<Port> _ports = new();        

        public List<Port> Ports => _ports;

        public GraphEditorView<T> View { get; protected set; }

        public GraphToolsTokenNode(Port input, Port output) : base(input, output)
        {
        }

        public NodeSo GetNode() => Node;
    }
}
