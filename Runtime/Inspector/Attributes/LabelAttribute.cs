using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class LabelAttribute : PropertyAttribute
    {
        public string Label { get; }
        public bool HasLabel { get; }
        public ResolverType LabelResolverType { get; }
        public string LabelResolver { get; }

        public StyleColor LabelColor { get; }
        public ResolverType LabelColorResolverType { get; }
        public string LabelColorResolver { get; }

        public string Icon { get; }
        public bool HasIcon { get; }
        public StyleColor IconColor { get; }
        public ResolverType IconColorResolverType { get; }
        public string IconColorResolver { get; }

        public LabelAttribute(string label = "", string labelColor = "", string icon = "", string iconColor = "")
        {
            Label = label;
            HasLabel = label != string.Empty;
            ResolverUtility.HasResolver(label, out var labelResolver);
            LabelResolver = label;
            LabelResolverType = labelResolver;

            LabelColor = ResolverUtility.GetColor(labelColor, ContainerStyles.LabelDefault.value, out var labelColorResolverType);
            LabelColorResolverType = labelColorResolverType;
            LabelColorResolver = labelColor;

            Icon = icon;
            HasIcon = icon != string.Empty;
            IconColor = ResolverUtility.GetColor(iconColor, Color.white, out var iconColorResolverType);
            IconColorResolverType = iconColorResolverType;
            IconColorResolver = iconColor;
        }
    }
}
