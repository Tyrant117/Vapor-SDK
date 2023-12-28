using System;
using System.Diagnostics;
using UnityEngine;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class InlineButtonAttribute : PropertyAttribute
    {
        public string MethodName { get; }
        public string Label { get; }
        public string Icon { get; }

        public Color Tint { get; }
        public ResolverType TintResolverType { get; }
        public string TintResolver { get; } = "";

        public InlineButtonAttribute(string methodName, string label = "", string icon = "", string iconTint = "")
        {
            MethodName = methodName;
            Label = label;
            Icon = icon;

            if (iconTint == string.Empty)
            {
                Tint = Color.white;
            }
            else
            {
                char first = iconTint[0];
                if (first.Equals('#'))
                {
                    Tint = ColorUtility.TryParseHtmlString(iconTint, out var htmlColor) ? htmlColor : Color.white;
                }
                else if (ResolverUtility.HasResolver(iconTint, out var type))
                {
                    TintResolverType = type;
                    TintResolver = iconTint;
                }
                else
                {
                    var split = iconTint.Split(',');
                    if (split.Length == 4)
                    {
                        Tint = float.TryParse(split[0], out var r) && float.TryParse(split[1], out var g) && float.TryParse(split[2], out var b) && float.TryParse(split[3], out var a)
                            ? new Color(r, g, b, a)
                            : Color.white;
                    }
                    else if (split.Length == 3)
                    {
                        Tint = float.TryParse(split[0], out var r) && float.TryParse(split[1], out var g) && float.TryParse(split[2], out var b)
                            ? new Color(r, g, b)
                            : Color.white;
                    }
                    else
                    {
                        Tint = Color.white;
                    }
                }
            }
        }
    }
}
