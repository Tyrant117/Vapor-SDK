using System;
using System.Diagnostics;
using UnityEngine;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class FolderPathAttribute : PropertyAttribute
    {
        public bool AbsolutePath { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="absolutePath">Should the absolute path be returned</param>
        public FolderPathAttribute(bool absolutePath = false)
        {
            AbsolutePath = absolutePath;
        }
    }
}
