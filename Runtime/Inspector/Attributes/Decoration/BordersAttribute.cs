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
        public int Left { get; }
        public int Right { get; }
        public int Top { get; }
        public int Bottom { get; }
        public StyleColor Color { get; }

        public BordersAttribute(bool rounded = true, int left = 1, int right = 1, int top = 1, int bottom = 1, string borderColor = "")
        {
            Rounded = rounded;
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
            Color = ResolverUtility.GetColor(borderColor, ContainerStyles.BorderColor.value, out var resolverType);
        }
    }
}
