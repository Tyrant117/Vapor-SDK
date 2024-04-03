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
    public class LabelEditorToken<T> : GraphToolsTokenNode<T, NodeSo> where T : ScriptableObject
    {
        private static readonly Texture2D exposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");
        protected static readonly StyleSheet _portColors = Resources.Load<StyleSheet>("Styles/PortColors");

        protected List<Port> _outPorts;

        private List<(NodeResultAttribute portAtr, Type[] portTypes)> _outPortData;
        private readonly Type[] _outPortTypes;

        public LabelEditorToken(GraphEditorView<T> view, NodeSo node, EditorLabelVisualData visualData, params Type[] outPortTypes) : base(null, null)
        {
            View = view;
            Node = node;
            _outPortTypes = outPortTypes;
            FindParams();

            name = "PropertyTokenView";
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/GraphToolsTokenView"));
            if (!string.IsNullOrEmpty(visualData.StyleSheet))
            {
                Debug.Log($"Adding StyleSheet: {visualData.StyleSheet}");
                styleSheets.Add(Resources.Load<StyleSheet>(visualData.StyleSheet));
                var border = this.Q<VisualElement>("node-border");
                border.name = visualData.BorderName;
                border.AddToClassList(visualData.ClassName);
            }

            if (string.IsNullOrEmpty(visualData.IconPath))
            {
                icon = exposedIcon;
            }
            else
            {
                icon = Resources.Load<Texture2D>(visualData.IconPath);
                var ico = this.Q<Image>("icon");
                ico.style.width = 24;
                ico.style.height = 24;
                ico.style.marginTop = 4;
                ico.style.marginBottom = 4;
            }

            CreateTitle(node.Name);

            CreateFlowOutPort();

            RefreshExpandedState();
        }

        private void FindParams()
        {
            // Get all fields of the class
            var atrs = Node.GetType().GetCustomAttributes<NodeResultAttribute>();

            // Loop through each field and check if it has the MathNodeParamAttribute
            _outPortData = new();
            foreach (var atr in atrs)
            {
                _outPortData.Add((atr, atr.PortTypes));
            }
        }

        private void CreateTitle(string title)
        {
            this.title = title;
            var titleLabel = this.Q<Label>("title-label");
            titleLabel.style.marginTop = 6;
            titleLabel.style.marginBottom = 6;
            titleLabel.style.marginLeft = 6;
            titleLabel.style.marginRight = 6;
        }

        private void CreateFlowOutPort()
        {
            if (_outPortTypes == null)
            {
                _outPorts = new(_outPortData.Count);
                VisualElement outVe = outputContainer;
                bool inGroup = false;
                if (_outPortData.Count > 1)
                {
                    outVe = new();
                    inGroup = true;
                }

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
                    outVe.Add(port);
                }
                if (inGroup)
                {
                    outputContainer.Add(outVe);
                    if (_outPorts[0] != null)
                    {
                        var pill = this.Q<Pill>("pill");
                        pill.right = outVe;
                    }
                }
                else
                {
                    if (_outPorts[0] != null)
                    {
                        var pill = this.Q<Pill>("pill");
                        pill.right = _outPorts[0];
                    }
                }
                _outPortData.Clear();
            }
            else
            {
                AddCustomOutPorts();
            }
        }

        protected virtual void AddCustomOutPorts()
        {
            _outPorts = new(1);
            var type = _outPortTypes[0];
            if (_outPortTypes.Length > 1)
            {
                type = typeof(DynamicValuePort);
            }
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, type);
            if (_outPortTypes.Length > 1)
            {
                var set = new HashSet<Type>();
                for (int i = 0; i < _outPortTypes.Length; i++)
                {
                    set.Add(_outPortTypes[i]);
                }
                port.userData = set;
            }
            port.portName = "";
            port.styleSheets.Add(_portColors);
            _outPorts.Add(port);
            Ports.Add(port);
            outputContainer.Add(port);

            if (_outPorts[0] != null)
            {
                var pill = this.Q<Pill>("pill");
                pill.right = _outPorts[0];
            }
        }
    }
}
