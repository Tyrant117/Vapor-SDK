using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{
    public class LogicEvaluateEditorNode<T> : GraphToolsTokenNode<T, LogicEvaluateNodeSo> where T : ScriptableObject
    {
        private static readonly StyleSheet _portColors = Resources.Load<StyleSheet>("Styles/PortColors");

        private Port _inPort;

        public LogicEvaluateEditorNode(GraphEditorView<T> view, LogicEvaluateNodeSo node) : base(null, null)
        {
            View = view;
            Node = node;

            name = "PropertyTokenView";
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/GraphToolsTokenView"));

            CreateTitle("Evaluate()");

            CreateInOutPort();

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

        private void CreateInOutPort()
        {
            _inPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
            //_inPort.portName = "In";
            _inPort.tooltip = "The logic evaluation";
            _inPort.Q("connector").pickingMode = PickingMode.Position;
            _inPort.Q<Label>().style.display = DisplayStyle.None;
            _inPort.styleSheets.Add(_portColors);
            Ports.Add(_inPort);
            inputContainer.Add(_inPort);

            if (_inPort != null)
            {
                var pill = this.Q<Pill>("pill");
                pill.left = _inPort;
            }
        }
    }
}
