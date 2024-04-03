using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VaporGraphTools;
using VaporGraphTools.Math;

namespace VaporGraphToolsEditor.Math
{
    public class PortTypes
    {
        public class FloatPort { }
    }

    public class OneParamMathNode<T> : GraphToolsNode<T, MathNodeSo> where T : ScriptableObject
    {
        private Port _inPort;
        private Port _outPort;

        private string _inPortName;

        public OneParamMathNode(GraphEditorView<T> view, MathNodeSo node)
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
            foreach (FieldInfo field in fields)
            {
                if (field.IsDefined(typeof(MathNodeParamAttribute)))
                {
                    _inPortName = field.GetCustomAttribute<MathNodeParamAttribute>().ParamName;
                    break;
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
            _inPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            _inPort.portName = _inPortName != null ? _inPortName : "A (1)";
            _inPort.tooltip = "The flow input";
            //_inPort.Q<Label>().style.display = DisplayStyle.None;
            Ports.Add(_inPort);
            inputContainer.Add(_inPort);
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
