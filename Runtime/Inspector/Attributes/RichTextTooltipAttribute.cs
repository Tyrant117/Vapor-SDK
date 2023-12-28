using System;
using System.Diagnostics;
using UnityEngine;
using Vapor.Utilities;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class RichTextTooltipAttribute : PropertyAttribute
    {
        public string Tooltip { get; }

        /// <summary>
        /// Converts a custom markup string using the <see cref="TooltipMarkup.FormatMarkupString"/> to a tooltip.
        /// </summary>
        /// <param name="tooltip">The tooltip to convert</param>
        public RichTextTooltipAttribute(string tooltip)
        {
            Tooltip = TooltipMarkup.FormatMarkupString(tooltip);
        }
    }
}
