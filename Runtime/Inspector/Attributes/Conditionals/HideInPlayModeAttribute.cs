using System;
using UnityEngine;
using System.Diagnostics;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class HideInPlayModeAttribute : PropertyAttribute
    {
        public HideInPlayModeAttribute()
        {
        }
    }
}
