using UnityEngine.UIElements;

namespace VaporInspector
{
    public class StyledVerticalGroup : VisualElement
    {
        public StyledVerticalGroup(float top = 1, float bottom = 1, bool overrideLabelPositions = false)
        {
            StyleContent(top, bottom);
            if (overrideLabelPositions)
            {
                AddToClassList("unity-inspector-element");
                AddToClassList("unity-inspector-main-container");
            }
        }

        protected virtual void StyleContent(float top, float bottom)
        {
            name = "styled-vertical-group";
            style.marginTop = top;
            style.marginBottom = bottom;
        }
    }
}
