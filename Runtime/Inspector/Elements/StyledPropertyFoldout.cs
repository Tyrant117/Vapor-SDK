using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace VaporInspector
{
    public class StyledPropertyFoldout : StyledFoldout
    {
        public VisualElement Header { get; }

        public StyledPropertyFoldout(string header) : base(header)
        {
            Header = Label.parent;
            Header.style.marginTop = 1;
            Header.style.marginBottom = 1;
            Label.style.marginLeft = 0;
        }

        public void SetHeaderProperty(VisualElement headerProperty)
        {
            if (headerProperty is PropertyField prop)
            {
                prop.label = "";
                prop.style.flexGrow = 1;
                prop.style.marginRight = 3;
                prop.style.marginLeft = 25;
                prop.RegisterCallback<GeometryChangedEvent>(OnPropertyBuilt);
            }
            Header.Add(headerProperty);
        }

        private void OnPropertyBuilt(GeometryChangedEvent evt)
        {
            if (evt.target is PropertyField propertyField && propertyField.childCount > 0)
            {
                propertyField.UnregisterCallback<GeometryChangedEvent>(OnPropertyBuilt);
                // Debug.Log(propertyField.Q<TextElement>());
                propertyField.Q<TextElement>().style.marginLeft = 0;
            }
        }

        protected override void StyleBox()
        {
            base.StyleBox();
            style.marginTop = 0;
            style.marginBottom = 0;
            style.backgroundColor = ContainerStyles.DarkInspectorBackgroundColor;
        }

        protected override void StyleFoldout(string header)
        {
            base.StyleFoldout(header);
            var togStyle = Foldout.Q<Toggle>().style;
            togStyle.backgroundColor = ContainerStyles.InspectorBackgroundColor;
            Label.AddToClassList("unity-base-field__label");
            // Label.AddToClassList("unity-property-field__label");
        }
    }
}
