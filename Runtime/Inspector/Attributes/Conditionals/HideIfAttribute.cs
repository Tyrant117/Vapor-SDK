using System;
using System.Diagnostics;
using UnityEngine;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class HideIfAttribute : PropertyAttribute
    {
        public ResolverType ResolverType { get; }
        public string Resolver { get; } = "";

        public HideIfAttribute(string resolver)
        {
            if (ResolverUtility.HasResolver(resolver, out var type))
            {
                ResolverType = type;
                Resolver = resolver;
            }
        }
    }
}
