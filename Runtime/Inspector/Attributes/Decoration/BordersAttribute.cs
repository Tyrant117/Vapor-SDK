using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class BordersAttribute : PropertyAttribute
    {
        public bool Rounded { get; }
        public float Left { get; }
        public float Right { get; }
        public float Top { get; }
        public float Bottom { get; }
        public StyleColor Color { get; }

        public BordersAttribute(bool rounded = true, float left = float.MinValue, float right = float.MinValue, float top = float.MinValue, float bottom = float.MinValue)
        {
            Rounded = rounded;
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
            Color = ContainerStyles.BorderColor;
        }

        public BordersAttribute(bool rounded = true, float left = float.MinValue, float right = float.MinValue, float top = float.MinValue, float bottom = float.MinValue, float r = 0.132f, float g = 0.132f, float b = 0.132f)
        {
            Rounded = rounded;
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
            Color = new(new Color(r, g, b));
        }
    }
}
