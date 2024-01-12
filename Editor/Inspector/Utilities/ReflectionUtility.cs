using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace VaporInspectorEditor
{
    public static class ReflectionUtility
    {
        private static readonly List<Type> s_TypeCache = new();
        private static readonly List<FieldInfo> s_FieldCache = new();
        private static readonly List<PropertyInfo> s_PropertyCache = new();
        private static readonly List<MethodInfo> s_MethodCache = new();
        
        

        public static IEnumerable<FieldInfo> GetAllFields(object target, Func<FieldInfo, bool> predicate)
        {
            if (target == null)
            {
                Debug.LogError("The target object is null. Check for missing scripts.");
                return null;
            }

            s_FieldCache.Clear();
            var types = GetSelfAndBaseTypes(target);

            for (var i = types.Count - 1; i >= 0; i--)
            {
                foreach (var fieldInfo in types[i]
                    .GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(predicate))
                {
                    s_FieldCache.Add(fieldInfo);
                }
            }
            return s_FieldCache;
        }

        public static IEnumerable<PropertyInfo> GetAllProperties(object target, Func<PropertyInfo, bool> predicate)
        {
            if (target == null)
            {
                Debug.LogError("The target object is null. Check for missing scripts.");
                return null;
            }

            s_PropertyCache.Clear();
            var types = GetSelfAndBaseTypes(target);

            for (var i = types.Count - 1; i >= 0; i--)
            {

                foreach (var propertyInfo in types[i]
                    .GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(predicate))
                {
                    s_PropertyCache.Add(propertyInfo);
                }
            }
            return s_PropertyCache;
        }

        public static IEnumerable<MethodInfo> GetAllMethods(object target, Func<MethodInfo, bool> predicate)
        {
            if (target == null)
            {
                Debug.LogError("The target object is null. Check for missing scripts.");
                return null;
            }

            s_MethodCache.Clear();
            var types = GetSelfAndBaseTypes(target);
            for (var i = types.Count - 1; i >= 0; i--)
            {
                foreach (var methodInfo in types[i]
                    .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(predicate))
                {
                    s_MethodCache.Add(methodInfo);
                }
            }
            return s_MethodCache;
        }

        public static FieldInfo GetField(object target, string fieldName)
        {
            return GetAllFields(target, f => f.Name.Equals(fieldName, StringComparison.Ordinal)).FirstOrDefault();
        }

        public static FieldInfo GetField(IEnumerable<FieldInfo> fields, string fieldName) => fields.FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.Ordinal));

        public static PropertyInfo GetProperty(object target, string propertyName)
        {
            return GetAllProperties(target, p => p.Name.Equals(propertyName, StringComparison.Ordinal)).FirstOrDefault();
        }
        
        public static PropertyInfo GetProperty(IEnumerable<PropertyInfo> properties, string propertyName) => properties.FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.Ordinal));

        public static MethodInfo GetMethod(object target, string methodName)
        {
            return GetAllMethods(target, m => m.Name.Equals(methodName, StringComparison.Ordinal)).FirstOrDefault();
        }
        
        public static MethodInfo GetMethod(IEnumerable<MethodInfo> methods, string fieldName) => methods.FirstOrDefault(m => m.Name.Equals(fieldName, StringComparison.Ordinal));

        public static Type GetListElementType(Type listType)
        {
            return listType.IsGenericType ? listType.GetGenericArguments()[0] : listType.GetElementType();
        }

        /// <summary>
        ///		Get type and all base types of target, sorted as following:
        ///		<para />[target's type, base type, base's base type, ...]
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static List<Type> GetSelfAndBaseTypes(object target)
        {
            s_TypeCache.Clear();
            s_TypeCache.Add(target.GetType());
            while (s_TypeCache[^1].BaseType != null)
            {
                s_TypeCache.Add(s_TypeCache[^1].BaseType);
            }

            return s_TypeCache;
        }

        /// <summary>
        ///		Get type and all base types of target, sorted as following:
        ///		<para />[target's type, base type, base's base type, ...]
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static List<Type> GetSelfAndBaseTypes(Type target)
        {
            s_TypeCache.Clear();
            s_TypeCache.Add(target);
            while (s_TypeCache[^1].BaseType != null)
            {
                s_TypeCache.Add(s_TypeCache[^1].BaseType);
            }

            return s_TypeCache;
        }
    }
}
