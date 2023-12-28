using System;
using System.Diagnostics;
using UnityEngine;

namespace VaporInspector
{
    [Conditional("VAPOR_INSPECTOR")]
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class ValueDropdownAttribute : PropertyAttribute
    {
        public ResolverType ResolverType { get; }
        public string Resolver { get; } = "";
        public bool Searchable { get; }
        public Type AssemblyQualifiedType { get; }

        public ValueDropdownAttribute(string resolver, string assemblyQualifiedType = null, bool searchable = false)
        {
            if (!ResolverUtility.HasResolver(resolver, out var type)) return;

            ResolverType = type;
            Resolver = resolver;
            Searchable = searchable;
            if (string.IsNullOrEmpty(assemblyQualifiedType))
            {
                return;
            }

            AssemblyQualifiedType = Type.GetType(assemblyQualifiedType);
        }
    }
}
