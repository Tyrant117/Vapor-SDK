using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{
    public class EditorBoolPropertyNode<T> : GraphToolsTokenPropertyNode<T, BoolValueNodeSo, bool> where T : ScriptableObject
    {
        public EditorBoolPropertyNode(GraphEditorView<T> view, BoolValueNodeSo node, Type outPortType) : base(node, outPortType)
        {
            View = view;
        }

        protected override VisualElement CreatePropertyDrawer()
        {
            var field = new Toggle("X")
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    marginBottom = 6,
                    marginLeft = 6,
                    marginRight = 6,
                    marginTop = 6,
                    maxWidth = 60,
                },
                pickingMode = PickingMode.Ignore
            };
            field.RemoveFromClassList("unity-base-field__aligned");
            var label = field.Q<Label>();
            label.pickingMode = PickingMode.Ignore;
            label.style.maxWidth = 30;
            label.style.minWidth = 30;
            field.SetValueWithoutNotify(Node.Value);
            field.RegisterValueChangedCallback(OnValueChanged);
            return field;
        }

        private void OnValueChanged(ChangeEvent<bool> evt)
        {
            Node.Value = evt.newValue;
        }
    }
}
