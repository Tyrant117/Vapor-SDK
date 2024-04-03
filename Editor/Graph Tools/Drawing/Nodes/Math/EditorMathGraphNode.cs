using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{
    public class EditorMathGraphNode<T> : GraphToolsNode<T, MathGraphNodeSo> where T : ScriptableObject
    {
        private static readonly StyleSheet _portColors = Resources.Load<StyleSheet>("Styles/PortColors");

        private List<Port> _inPorts;
        private Port _outPort;

        public EditorMathGraphNode(GraphEditorView<T> view, MathGraphNodeSo node)
        {
            View = view;
            Node = node;

            m_CollapseButton.RemoveFromHierarchy();
            CreateTitle(node.Name);

            CreateFlowInPort();
            CreateFlowOutPort();

            RefreshExpandedState();
        }

        private void CreateTitle(string title)
        {
            this.title = title;
            var label = titleContainer.Q<Label>();
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginRight = 6;
            label.style.marginLeft = 6;
        }

        private void CreateFlowInPort()
        {
            _inPorts = new(Node.Graph.ExposedProperties.Count);
            foreach (var exposedProp in Node.Graph.ExposedProperties)
            {
                var port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(DynamicValuePort));
                var set = new HashSet<Type>
                {
                    typeof(bool),
                    typeof(int),
                    typeof(float)
                };
                port.userData = set;
                port.portName = exposedProp.ValueName;
                port.tooltip = "The flow input";
                port.styleSheets.Add(_portColors);
                _inPorts.Add(port);
                Ports.Add(port);
                inputContainer.Add(port);
            }
        }

        private void CreateFlowOutPort()
        {
            _outPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            _outPort.portName = "Out";
            _outPort.tooltip = "The graph evaluation";
            //_inPort.Q<Label>().style.display = DisplayStyle.None;
            Ports.Add(_outPort);
            outputContainer.Add(_outPort);
        }
    }
}
