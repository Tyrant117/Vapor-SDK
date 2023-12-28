using System;
using System.Diagnostics;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ButtonAttribute : Attribute
    {
        public string Label { get; }
        public ButtonSize Size { get; }

        public ButtonAttribute(ButtonSize size = ButtonSize.Small)
        {
            Label = null;
            Size = size;
        }

        public ButtonAttribute(string label, ButtonSize size = ButtonSize.Small)
        {
            Label = label;
            Size = size;
        }
    }
}
