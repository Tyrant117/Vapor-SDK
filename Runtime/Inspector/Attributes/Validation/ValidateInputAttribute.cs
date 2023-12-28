using System;
using System.Diagnostics;
using UnityEngine;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class ValidateInputAttribute : PropertyAttribute
    {
        public string MethodName { get; } = "";

        public ValidateInputAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}
