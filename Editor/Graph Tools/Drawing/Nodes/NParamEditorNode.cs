using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{
    public class NParamEditorNode<T> : GraphToolsNode<T, NodeSo> where T : ScriptableObject
    {
        private static readonly StyleSheet _portColors = Resources.Load<StyleSheet>("Styles/PortColors");

        private List<Port> _inPorts;
        private List<Port> _outPorts;
        private Port _outPort;

        private List<(NodeParamAttribute portAtr, Type[] portTypes)> _inPortData;
        private List<(NodeResultAttribute portAtr, Type[] portTypes)> _outPortData;
        private readonly Type[] _outPortTypes;

        public NParamEditorNode(GraphEditorView<T> view, NodeSo node, params Type[] outPortTypes)
        {
            View = view;
            Node = node;
            _outPortTypes = outPortTypes;
            FindParams();

            m_CollapseButton.RemoveFromHierarchy();
            CreateTitle(node.Name);

            CreateAdditionalContent(mainContainer.Q("contents"));

            CreateFlowInPort();
            CreateFlowOutPort();

            RefreshExpandedState();
        }

        private void FindParams()
        {
            // Get all fields of the class
            var fields = Node.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var atrs = Node.GetType().GetCustomAttributes<NodeResultAttribute>();


            // Loop through each field and check if it has the MathNodeParamAttribute
            _inPortData = new();
            _outPortData = new();
            foreach (FieldInfo field in fields)
            {
                if (field.IsDefined(typeof(NodeParamAttribute)))
                {
                    var atr = field.GetCustomAttribute<NodeParamAttribute>();
                    _inPortData.Add((atr, atr.PortTypes));
                }
            }

            foreach (var atr in atrs)
            {
                _outPortData.Add((atr, atr.PortTypes));
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
            _inPorts = new(_inPortData.Count);
            foreach (var (portAtr, portTypes) in _inPortData)
            {
                var type = portTypes[0];
                if(portTypes.Length > 1)
                {
                    type = typeof(DynamicValuePort);
                }
                var capacity = portAtr.MultiPort ? Port.Capacity.Multi : Port.Capacity.Single;
                var port = InstantiatePort(Orientation.Horizontal, Direction.Input, capacity, type);
                if (portTypes.Length > 1)
                {
                    var set = new HashSet<Type>();
                    for (int i = 0; i < portTypes.Length; i++)
                    {
                        set.Add(portTypes[i]);
                    }
                    port.userData = set;
                }
                if (!portAtr.Required)
                {
                    port.AddToClassList("optionalPort");
                }
                port.portName = portAtr.ParamName;
                port.tooltip = "The flow input";
                port.styleSheets.Add(_portColors);
                _inPorts.Add(port);
                Ports.Add(port);
                inputContainer.Add(port);
            }
            _inPortData.Clear();
        }

        private void CreateFlowOutPort()
        {
            if (_outPortTypes == null)
            {
                _outPorts = new(_outPortData.Count);
                foreach (var (portAtr, portTypes) in _outPortData)
                {
                    var type = portTypes[0];
                    if (portTypes.Length > 1)
                    {
                        type = typeof(DynamicValuePort);
                    }
                    var capacity = portAtr.MultiPort ? Port.Capacity.Multi : Port.Capacity.Single;
                    var port = InstantiatePort(Orientation.Horizontal, Direction.Output, capacity, type);
                    if (portTypes.Length > 1)
                    {
                        var set = new HashSet<Type>();
                        for (int i = 0; i < portTypes.Length; i++)
                        {
                            set.Add(portTypes[i]);
                        }
                        port.userData = set;
                    }
                    port.portName = portAtr.ParamName;
                    port.tooltip = "The flow output";
                    port.styleSheets.Add(_portColors);
                    _outPorts.Add(port);
                    Ports.Add(port);
                    outputContainer.Add(port);
                }
                _outPortData.Clear();
            }
            else
            {
                var type = _outPortTypes[0];
                if (_outPortTypes.Length > 1)
                {
                    type = typeof(DynamicValuePort);
                }
                _outPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, type);
                if (_outPortTypes.Length > 1)
                {
                    var set = new HashSet<Type>();
                    for (int i = 0; i < _outPortTypes.Length; i++)
                    {
                        set.Add(_outPortTypes[i]);
                    }
                    _outPort.userData = set;
                }
                _outPort.portName = "Out";
                _outPort.tooltip = "The flow output";
                _outPort.styleSheets.Add(_portColors);
                Ports.Add(_outPort);
                outputContainer.Add(_outPort);
            }
                
        }

        protected virtual void CreateAdditionalContent(VisualElement content)
        {

        } 
    }
}
