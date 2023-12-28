using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace VaporInspector
{
    public class StyledTabGroup : Box
    {
        public Toolbar Toolbar { get; private set; }
        public ToolbarToggle CurrentToggle { get; private set; }
        public Dictionary<string, VisualElement> TabContent { get; } = new();

        public StyledTabGroup()
        {
            StyleBox();
            StyleToolbar();
            Add(Toolbar);
        }

        protected virtual void StyleBox()
        {
            name = "styled-header-box";
            style.borderBottomColor = ContainerStyles.BorderColor;
            style.borderTopColor = ContainerStyles.BorderColor;
            style.borderRightColor = ContainerStyles.BorderColor;
            style.borderLeftColor = ContainerStyles.BorderColor;
            style.borderBottomLeftRadius = 3;
            style.borderBottomRightRadius = 3;
            style.borderTopLeftRadius = 3;
            style.borderTopRightRadius = 3;
            style.marginTop = 3;
            style.marginBottom = 3;
            style.paddingLeft = 3;
            style.paddingBottom = 3;
            style.paddingRight = 4;
            style.backgroundColor = ContainerStyles.BackgroundColor;
        }

        protected virtual void StyleToolbar()
        {
            Toolbar = new Toolbar();
            Toolbar.style.marginLeft = -3;
            Toolbar.style.marginRight = -4;
            Toolbar.style.paddingLeft = 3;
            Toolbar.style.paddingRight = 4;
            var tog1 = new EditorToolbarToggle("Tab 1");
            var tog2 = new EditorToolbarToggle("Tab 2");
            var tog3 = new ToolbarToggle()
            {
                text = "Tab 3"
            };
            var tog4 = new ToolbarToggle()
            {
                text = "Tab 4"
            };
            tog1.RegisterValueChangedCallback(ToggleChanged);
            tog2.RegisterValueChangedCallback(ToggleChanged);
            tog3.RegisterValueChangedCallback(ToggleChanged);
            tog4.RegisterValueChangedCallback(ToggleChanged);
            Toolbar.Add(tog1);
            Toolbar.Add(tog2);
            Toolbar.Add(tog3);
            Toolbar.Add(tog4);
        }

        private void ToggleChanged(ChangeEvent<bool> evt)
        {
            var toggle = evt.currentTarget as ToolbarToggle;
            if (CurrentToggle != null && CurrentToggle != toggle)
            {
                CurrentToggle.SetValueWithoutNotify(false);
                CurrentToggle.style.marginBottom = 0;
            }
            CurrentToggle = toggle;
            if (!CurrentToggle.value)
            {
                CurrentToggle.SetValueWithoutNotify(true);
            }
            CurrentToggle.style.marginBottom = -1;
        }
    }
}
