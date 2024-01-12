using System;
using System.Diagnostics;
using UnityEngine;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class MarginsAttribute : PropertyAttribute
    {
        public int? Left { get; }
        public int? Right { get; }
        public int? Top { get; }
        public int? Bottom { get; }

        /// <summary>
        /// Use this attribute to change the margins of a property.
        /// Int.MinValue are the default margins.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        public MarginsAttribute(int left = int.MinValue, int right = int.MinValue, int top = int.MinValue, int bottom = int.MinValue)
        {
            Left = left == int.MinValue ? null : left;
            Right = right == int.MinValue ? null : right;
            Top = top == int.MinValue ? null : top;
            Bottom = bottom == int.MinValue ? null : bottom;
        }        
    }
}
