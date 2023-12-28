using UnityEngine.UIElements;

namespace VaporInspector
{
    public class StyledHorizontalGroup : VisualElement
    {
        public Label Label { get; private set; }
        public VisualElement Content { get; private set; }

        public override VisualElement contentContainer => Content;

        public StyledHorizontalGroup() : base()
        {
            StyleGroup();
            //if (label != string.Empty)
            //{
            //    StyleLabel(label);
            //}
            StyleContent();

            //if (label != string.Empty)
            //{
            //    hierarchy.Add(Label);
            //}
            hierarchy.Add(Content);
            //Content.AddToClassList("unity-inspector-element");
            //Content.AddToClassList("unity-inspector-main-container");
        }

        protected virtual void StyleGroup()
        {
            name = "styled-horizontal-group";
            style.flexDirection = FlexDirection.Row;
            style.marginTop = 1;
            style.marginBottom = 1;
            style.marginLeft = 0;
            style.marginRight = 0;
        }

        protected virtual void StyleLabel(string label)
        {
            Label = new Label(label)
            {
                name = "styled-horizontal-group-label"
            };
            Label.AddToClassList("unity-property-field__label");

        }

        protected virtual void StyleContent()
        {
            Content = new VisualElement()
            {
                name = "styled-horizontal-group-content"
            };
            Content.style.flexDirection = FlexDirection.Row;
            Content.style.flexGrow = 1f;
        }
    }
}
