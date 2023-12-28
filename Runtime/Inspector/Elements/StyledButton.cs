using System;
using UnityEngine.UIElements;

namespace VaporInspector
{
    public class StyledButton : Button
    {
        public StyledButton(ButtonSize size = ButtonSize.Small) : base()
        {
            Style(size);
        }

        public StyledButton(Action clickEvent, ButtonSize size = ButtonSize.Small) : base(clickEvent)
        {
            Style(size);
        }

        private void Style(ButtonSize size)
        {
            style.marginTop = 2;
            style.marginBottom = 2;
            style.height = (int)size + 4;
        }
    }
}
