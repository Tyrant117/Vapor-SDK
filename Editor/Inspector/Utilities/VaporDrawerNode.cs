using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using VaporInspector;
using Object = UnityEngine.Object;

namespace VaporInspectorEditor
{
    public enum NodeInfoType
    {
        Field,
        Property,
        Method,
        Group,
    }

    public class VaporDrawerNode
    {
        private static Func<VaporGroupAttribute, int> ShortestToLongestName => group => group.GroupName.Length;
        
        public string Path { get; }
        public NodeInfoType NodeType { get; }
        public VaporDrawerNode Parent { get; }
        public List<VaporDrawerNode> Children { get; } = new();

        public bool HasParent { get; }
        public bool HasChildren { get; }
        public int DrawOrder { get; }

        public object Target { get; }
        public SerializedProperty Property { get; }
        public FieldInfo FieldInfo { get; }
        public MethodInfo MethodInfo { get; }
        public PropertyInfo PropertyInfo { get; }
        
        // Groups
        public List<VaporGroupAttribute> Groups { get; } = new();
        public VaporGroupAttribute ContainingGroup { get; }
        public bool IsUnmanagedGroup { get; }
        
        // Field
        public bool IsDrawnWithVapor { get; }
        public bool IsUnityObject { get; }

        public VaporDrawerNode(string path, FieldInfo fieldInfo, SerializedProperty property, object target, VaporDrawerNode parentNode)
        {
            
        }
        
        #region - Attributes -
        public bool HasAttribute<T>() where T : Attribute
        {
            return NodeType switch
            {
                NodeInfoType.Field => FieldInfo.IsDefined(typeof(T), true),
                NodeInfoType.Property => PropertyInfo.IsDefined(typeof(T), true),
                NodeInfoType.Method => MethodInfo.IsDefined(typeof(T), true),
                _ => false,
            };
        }

        public bool TryGetAttribute<T>(out T attribute) where T : Attribute
        {
            bool result;
            switch (NodeType)
            {
                case NodeInfoType.Field:
                    result = FieldInfo.IsDefined(typeof(T), true);
                    attribute = result ? FieldInfo.GetCustomAttribute<T>(true) : null;
                    return result;
                case NodeInfoType.Property:
                    result = PropertyInfo.IsDefined(typeof(T), true);
                    attribute = result ? PropertyInfo.GetCustomAttribute<T>(true) : null;
                    return result;
                case NodeInfoType.Method:
                    result = MethodInfo.IsDefined(typeof(T), true);
                    attribute = result ? MethodInfo.GetCustomAttribute<T>(true) : null;
                    return result;
                case NodeInfoType.Group:
                default:
                    attribute = null;
                    return false;
            }
        }

        public bool TryGetAttributes<T>(out T[] attribute) where T : Attribute
        {
            bool result;
            switch (NodeType)
            {
                case NodeInfoType.Field:
                    result = FieldInfo.IsDefined(typeof(T), true);
                    attribute = (T[])(result ? FieldInfo.GetCustomAttributes<T>(true) : null);
                    return result;
                case NodeInfoType.Property:
                    result = PropertyInfo.IsDefined(typeof(T), true);
                    attribute = (T[])(result ? PropertyInfo.GetCustomAttributes<T>(true) : null);
                    return result;
                case NodeInfoType.Method:
                    result = MethodInfo.IsDefined(typeof(T), true);
                    attribute = (T[])(result ? MethodInfo.GetCustomAttributes<T>(true) : null);
                    return result;
                case NodeInfoType.Group:
                default:
                    attribute = null;
                    return false;
            }
        }
        #endregion
    }
}
