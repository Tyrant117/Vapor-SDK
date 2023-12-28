using System;
using System.Diagnostics;
using UnityEngine;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class OnValueChangedAttribute : PropertyAttribute
    {
        public string MethodName { get; } = "";

        public OnValueChangedAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}
