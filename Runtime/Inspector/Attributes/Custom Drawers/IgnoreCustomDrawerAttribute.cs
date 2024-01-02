using System;
using System.Diagnostics;
using UnityEngine;

namespace VaporInspector
{
    /// <summary>
    /// Ignores the PropertyDrawer for this field if it exists.
    /// </summary>
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field)]
    public class IgnoreCustomDrawerAttribute : PropertyAttribute
    {
        
    }
}
