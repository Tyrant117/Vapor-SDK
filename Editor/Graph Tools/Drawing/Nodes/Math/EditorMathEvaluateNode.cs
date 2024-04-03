using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VaporGraphTools;

namespace VaporGraphToolsEditor.Math
{
    public class EditorMathEvaluateNode<T> : GraphToolsTokenNode<T, MathEvaluateNodeSo> where T : ScriptableObject
    {
        private Port _inPort;

        public EditorMathEvaluateNode(GraphEditorView<T> view, MathEvaluateNodeSo node) : base(null, null)
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
            _inPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            _inPort.portName = "In";
            _inPort.tooltip = "The math evaluation";
            _inPort.Q("connector").pickingMode = PickingMode.Position;
            _inPort.Q<Label>().style.display = DisplayStyle.None;
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
