using System;
using System.Diagnostics;
using UnityEngine;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public ResolverType ResolverType { get; }
        public string Resolver { get; } = "";

        public ShowIfAttribute(string resolver)
        {
            if (!ResolverUtility.HasResolver(resolver, out var type)) return;
            
            ResolverType = type;
            Resolver = resolver;
        }
    }
}
