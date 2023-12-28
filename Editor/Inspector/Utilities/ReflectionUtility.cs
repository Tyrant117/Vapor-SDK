using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace VaporInspectorEditor
{
    public static class ReflectionUtility
    {
        private static readonly List<Type> _typeCache = new();
        private static readonly List<FieldInfo> _fieldCache = new();
        private static readonly List<PropertyInfo> _propertyCache = new();
        private static readonly List<MethodInfo> _methodCache = new();

        public static List<FieldInfo> GetAllFields(object target, Func<FieldInfo, bool> predicate)
        {
            if (target == null)
            {
                Debug.LogError("The target object is null. Check for missing scripts.");
                return null;
            }

            _fieldCache.Clear();
            List<Type> types = GetSelfAndBaseTypes(target);

            for (int i = types.Count - 1; i >= 0; i--)
            {
                foreach (var fieldInfo in types[i]
                    .GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(predicate))
                {
                    _fieldCache.Add(fieldInfo);
                }
            }
            return _fieldCache;
        }

        public static List<PropertyInfo> GetAllProperties(object target, Func<PropertyInfo, bool> predicate)
        {
            if (target == null)
            {
                Debug.LogError("The target object is null. Check for missing scripts.");
                return null;
            }

            _propertyCache.Clear();
            List<Type> types = GetSelfAndBaseTypes(target);

            for (int i = types.Count - 1; i >= 0; i--)
            {

                foreach (var propertyInfo in types[i]
                    .GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(predicate))
                {
                    _propertyCache.Add(propertyInfo);
                }
            }
            return _propertyCache;
        }

        public static List<MethodInfo> GetAllMethods(object target, Func<MethodInfo, bool> predicate)
        {
            if (target == null)
            {
                Debug.LogError("The target object is null. Check for missing scripts.");
                return null;
            }

            _methodCache.Clear();
            List<Type> types = GetSelfAndBaseTypes(target);

            for (int i = types.Count - 1; i >= 0; i--)
            {

                foreach (var methodInfo in types[i]
                    .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(predicate))
                {
                    _methodCache.Add(methodInfo);
                }
            }
            return _methodCache;
        }

        public static FieldInfo GetField(object target, string fieldName)
        {
            return GetAllFields(target, f => f.Name.Equals(fieldName, StringComparison.Ordinal)).FirstOrDefault();
        }

        public static PropertyInfo GetProperty(object target, string propertyName)
        {
            return GetAllProperties(target, p => p.Name.Equals(propertyName, StringComparison.Ordinal)).FirstOrDefault();
        }

        public static MethodInfo GetMethod(object target, string methodName)
        {
            return GetAllMethods(target, m => m.Name.Equals(methodName, StringComparison.Ordinal)).FirstOrDefault();
        }

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
            _typeCache.Clear();
            _typeCache.Add(target.GetType());
            while (_typeCache[^1].BaseType != null)
            {
                _typeCache.Add(_typeCache[^1].BaseType);
            }

            return _typeCache;
        }

        /// <summary>
        ///		Get type and all base types of target, sorted as following:
        ///		<para />[target's type, base type, base's base type, ...]
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static List<Type> GetSelfAndBaseTypes(Type target)
        {
            _typeCache.Clear();
            _typeCache.Add(target);
            while (_typeCache[^1].BaseType != null)
            {
                _typeCache.Add(_typeCache[^1].BaseType);
            }

            return _typeCache;
        }
    }
}
