using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VaporGraphTools;
using VaporGraphTools.Math;
using VaporInspectorEditor;

namespace VaporGraphToolsEditor
{

    public readonly struct EditorLabelVisualData
    {
        public readonly string IconPath;
        public readonly string StyleSheet;
        public readonly string BorderName;
        public readonly string ClassName;

        public EditorLabelVisualData(string iconPath, string styleSheet, string borderName, string className)
        {
            IconPath = iconPath;
            StyleSheet = styleSheet;
            BorderName = borderName;
            ClassName = className;
        }
    }

    public class EditorLabelPropertyNode<T, U, Z> : GraphToolsTokenNode<T, U> where T : ScriptableObject where U : ValueNodeSo<Z>
    {
        static readonly Texture2D exposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");
        private Port _outPort;
        private readonly Type _outPortType;

        public EditorLabelPropertyNode(GraphEditorView<T> view, U node, Type outPortType, EditorLabelVisualData visualData = default) : base(null, null)
        {
            View = view;
            Node = node;
            _outPortType = outPortType;

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
            _outPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, _outPortType);
            _outPort.portName = "Out";
            _outPort.tooltip = "The flow output";
            _outPort.Q<Label>().style.display = DisplayStyle.None;
            Ports.Add(_outPort);
            outputContainer.Add(_outPort);

            if (_outPort != null)
            {
                var pill = this.Q<Pill>("pill");
                pill.right = _outPort;
            }
        }
    }
}
