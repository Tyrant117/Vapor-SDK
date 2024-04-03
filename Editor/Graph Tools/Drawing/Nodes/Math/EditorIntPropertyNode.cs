using UnityEngine;
using UnityEngine.UIElements;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{ 
    public class EditorIntPropertyNode<GraphArg> : GraphToolsTokenPropertyNode<GraphArg, IntValueNodeSo, int> where GraphArg : ScriptableObject
    {
        public EditorIntPropertyNode(GraphEditorView<GraphArg> view, IntValueNodeSo node) : base(node, typeof(int))
        {
            View = view;
        }

        protected override VisualElement CreatePropertyDrawer()
        {
            var field = new IntegerField("X")
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    marginBottom = 6,
                    marginLeft = 6,
                    marginRight = 6,
                    marginTop = 6,
                    maxWidth = 130,
                }
            };
            field.RemoveFromClassList("unity-base-field__aligned");
            var label = field.Q<Label>();
            label.pickingMode = PickingMode.Ignore;
            label.style.maxWidth = 30;
            label.style.minWidth = 30;
            field.Q<VisualElement>("unity-text-input").style.maxWidth = 100;
            field.SetValueWithoutNotify(Node.Value);
            field.RegisterValueChangedCallback(OnValueChanged);
            return field;
        }

        private void OnValueChanged(ChangeEvent<int> evt)
        {
            Node.Value = evt.newValue;
        }
    }
}
