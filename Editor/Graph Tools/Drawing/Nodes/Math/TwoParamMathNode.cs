using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VaporGraphTools;
using VaporGraphTools.Math;

namespace VaporGraphToolsEditor.Math
{
    public class TwoParamMathNode<T> : GraphToolsNode<T, MathNodeSo> where T : ScriptableObject
    {
        private Port _in1Port;
        private Port _in2Port;
        private Port _outPort;
        private List<string> _inPortNames;

        public TwoParamMathNode(GraphEditorView<T> view, MathNodeSo node)
        {
            View = view;
            Node = node;
            FindParams();

            m_CollapseButton.RemoveFromHierarchy();
            CreateTitle(node.Name);

            CreateFlowInPort();
            CreateFlowOutPort();

            RefreshExpandedState();
        }

        private void FindParams()
        {
            // Get all fields of the class
            var fields = Node.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // Loop through each field and check if it has the MathNodeParamAttribute
            _inPortNames = new();
            foreach (FieldInfo field in fields)
            {
                if (field.IsDefined(typeof(MathNodeParamAttribute)))
                {
                    _inPortNames.Add(field.GetCustomAttribute<MathNodeParamAttribute>().ParamName);
                }
            }
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
            _in1Port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            _in1Port.portName = _inPortNames.Count > 0 ? _inPortNames[0] : "A (1)";
            _in1Port.tooltip = "The flow input";
            Ports.Add(_in1Port);
            inputContainer.Add(_in1Port);

            _in2Port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            _in2Port.portName = _inPortNames.Count > 1 ? _inPortNames[1] : "B (1)";
            _in2Port.tooltip = "The flow input";
            Ports.Add(_in2Port);
            inputContainer.Add(_in2Port);
        }

        private void CreateFlowOutPort()
        {
            _outPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            _outPort.portName = "Out (1)";
            _outPort.tooltip = "The flow output";
            //_inPort.Q<Label>().style.display = DisplayStyle.None;
            Ports.Add(_outPort);
            outputContainer.Add(_outPort);
        }
    }
}
