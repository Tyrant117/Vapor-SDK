using System;
using System.Diagnostics;
using UnityEngine;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class TitleAttribute : PropertyAttribute
    {
        public string Title { get; }
        public string Subtitle { get; }
        public bool Underline { get; }

        public TitleAttribute(string title, string subtitle = "", bool underline = true)
        {
            Title = title;
            Subtitle = subtitle;
            Underline = underline;
        }        
    }
}
