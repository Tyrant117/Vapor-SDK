using System;
using System.Diagnostics;
using UnityEngine;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ShowInInspectorAttribute : PropertyAttribute
    {
        public bool Dynamic { get; }
        public int DynamicInterval { get; }

        /// <summary>
        /// Marks a property to be visible in the inspector. The property will be readonly and the data is not serialized.
        /// </summary>
        /// <param name="dynamic">If True, the property will update its values on an interval. Can be used when the property expression should be evaulated continously.</param>
        /// <param name="dynamicInterval">The interval in milliseconds that the property will evualate.</param>
        public ShowInInspectorAttribute(bool dynamic = false, int dynamicInterval = 1000)
        {
            Dynamic = dynamic;
            DynamicInterval = dynamicInterval;
        }
    }
}
