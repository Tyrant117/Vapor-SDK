using System;
using System.Diagnostics;
using UnityEngine;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class SuffixAttribute : PropertyAttribute
    {
        public string Suffix { get; }

        public SuffixAttribute(string suffix)
        {
            Suffix = suffix;
        }
    }
}
