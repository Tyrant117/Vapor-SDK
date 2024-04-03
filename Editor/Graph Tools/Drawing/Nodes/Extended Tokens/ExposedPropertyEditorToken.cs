using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{
    public class ExposedPropertyEditorToken<GraphArg> : NParamEditorToken<GraphArg> where GraphArg : ScriptableObject
    {
        private readonly ExposedPropertyNodeSo _node;

        public ExposedPropertyEditorToken(GraphEditorView<GraphArg> view, ExposedPropertyNodeSo node, EditorLabelVisualData visualData, params Type[] outPortTypes) : base(view, node, visualData, outPortTypes)
        {
            _node = node;
            var contents = mainContainer.Q("contents");
            var ve = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };
            var label = new Label("Name:")
            {
                style =
                {
                    marginRight = 4
                }
            };
            var text = new TextField();
            text.style.marginTop = 4;
            text.style.marginBottom = 2;
            text.style.flexGrow = 1;
            text.SetValueWithoutNotify(_node.ValueName);
            text.RegisterValueChangedCallback(OnNameChanged);
            ve.Add(label);
            ve.Add(text);
            contents.Add(ve);
        }

        private void OnNameChanged(ChangeEvent<string> evt)
        {
            _node.ValueName = evt.newValue;
        }
    }
}
