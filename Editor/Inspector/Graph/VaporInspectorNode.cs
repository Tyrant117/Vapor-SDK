using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VaporInspector;
using Object = UnityEngine.Object;

namespace VaporInspectorEditor
{
    public class VaporInspectorNode
    {
        public enum NodeElementType
        {
            Root,
            Field,
            Property,
            Method,
            Group
        }
        
        public static readonly Func<FieldInfo, bool> FieldSearchPredicate = f => !f.IsDefined(typeof(HideInInspector))
                                                                                 && !f.IsDefined(typeof(NonSerializedAttribute))
                                                                                 && (f.IsPublic || f.IsDefined(typeof(SerializeField)));
        public static readonly Func<MethodInfo, bool> MethodSearchPredicate = f => f.IsDefined(typeof(ButtonAttribute));
        public static readonly Func<PropertyInfo, bool> PropertySearchPredicate = f => f.IsDefined(typeof(ShowInInspectorAttribute));
        private static Func<VaporGroupAttribute, int> ShortestToLongestName => group => group.GroupName.Length;
        
        // Node Properties
        public SerializedObject Root { get; }
        public bool IsRootNode { get; }
        public string Path { get; private set; }
        public SerializedProperty Property { get; private set; }
        public Type Type { get; }
        public object Target { get; private set; }
        public NodeElementType ElementType { get; }
        public VaporGroupAttribute Group { get; private set; }
        public List<VaporGroupAttribute> Groups { get; private set; }
        public int DrawOrder { get; private set; }
        public bool OverrideGroupTypeWithDrawn { get; set; }

        // Hierarchy
        public VaporInspectorNode Parent { get; set; }
        public List<VaporInspectorNode> Children { get; private set; } = new();
        
        // Type Specifics
        public FieldInfo FieldInfo { get; }
        public PropertyInfo PropertyInfo { get; }
        public MethodInfo MethodInfo { get; }
        public bool IsUnityObject { get; private set; }
        public bool IsDrawnWithVapor { get; private set; }
        public UIGroupType DrawnWithGroup { get; private set; }

        public NodeElement VisualNode { get; private set; }


        public VaporInspectorNode(SerializedObject root, object target)
        {
            Root = root;
            Path = "";
            Target = target;
            Type = Target.GetType();
            IsRootNode = true;
            ElementType = NodeElementType.Root;

            var children = BuildChildren();
            BuildGroupNodes(children);

            _OrderNodes(this);

            static void _OrderNodes(VaporInspectorNode node)
            {
                node.Children = node.Children.OrderBy(n => n.DrawOrder).ToList();
                foreach (var child in node.Children)
                {
                    _OrderNodes(child);
                }
            }
        }

        public VaporInspectorNode(Type listElementType, SerializedProperty listElementProperty)
        {
            Root = listElementProperty.serializedObject;
            Property = listElementProperty;
            Path = listElementProperty.propertyPath;
            Target = listElementProperty.boxedValue;
            Type = listElementType;
            IsRootNode = true;
            ElementType = NodeElementType.Root;
            
            var children = BuildChildren();
            BuildGroupNodes(children);

            _OrderNodes(this);

            static void _OrderNodes(VaporInspectorNode node)
            {
                foreach (var child in node.Children.OrderBy(n => n.DrawOrder))
                {
                    _OrderNodes(child);
                }
            }
        }

        public VaporInspectorNode(VaporInspectorNode parent, FieldInfo fieldInfo, SerializedProperty property, object target)
        {
            FieldInfo = fieldInfo;

            Root = parent.Root;
            Parent = parent;
            Property = property;
            Path = property.propertyPath;
            Target = target;
            Type = FieldInfo.FieldType;
            IsRootNode = false;
            ElementType = NodeElementType.Field;
            
            FindGroupsAndDrawOrder();
            var children = BuildChildren();
            BuildGroupNodes(children);
        }

        public VaporInspectorNode(VaporInspectorNode parent, string path, PropertyInfo propertyInfo, object target)
        {
            PropertyInfo = propertyInfo;
            
            Root = parent.Root;
            Path = path;
            Parent = parent;
            Property = parent.Property;
            Target = target;
            Type = PropertyInfo.PropertyType;
            IsRootNode = false;
            ElementType = NodeElementType.Property;
            
            FindGroupsAndDrawOrder();
            var children = BuildChildren();
            BuildGroupNodes(children);
        }
        
        public VaporInspectorNode(VaporInspectorNode parent, string path, MethodInfo methodInfo, object target)
        {
            MethodInfo = methodInfo;
            
            Root = parent.Root;
            Path = path;
            Parent = parent;
            Property = parent.Property;
            Target = target;
            IsRootNode = false;
            ElementType = NodeElementType.Method;
            
            FindGroupsAndDrawOrder();
        }

        public VaporInspectorNode(VaporInspectorNode parent, VaporGroupAttribute groupAttribute)
        {
            Parent = parent;
            Property = parent.Property;
            Group = groupAttribute;
            DrawOrder = groupAttribute.Order;
            ElementType = NodeElementType.Group;
        }

        private void FindGroupsAndDrawOrder()
        {
            if (TryGetAttribute<PropertyOrderAttribute>(out var propOrder))
            {
                DrawOrder = propOrder.Order;
            }
            
            if (!TryGetAttributes<VaporGroupAttribute>(out var attributes))
            {
                return;
            }

            Groups = new List<VaporGroupAttribute>();
            if (attributes.Length > 1)
            {
                Groups = attributes.OrderBy(ShortestToLongestName).ToList();
            }
            else
            {
                Groups.Add(attributes[0]);
            }

            Group = Groups[^1];
        }

        protected List<VaporInspectorNode> BuildChildren()
        {
            var children = new List<VaporInspectorNode>();
            IsUnityObject = Type.IsSubclassOf(typeof(Object));
            IsDrawnWithVapor = Type.IsDefined(typeof(DrawWithVaporAttribute)) && !IsUnityObject;
            if (!IsRootNode && !IsDrawnWithVapor)
            {
                return children;
            }

            if (IsDrawnWithVapor)
            {
                DrawnWithGroup = Type.GetCustomAttribute<DrawWithVaporAttribute>().InlinedGroupType;
            }

            List<FieldInfo> fieldInfo = new();
            List<PropertyInfo> propertyInfo = new();
            List<MethodInfo> methodInfo = new();
            var targetType = Type;
            Stack<Type> typeStack = new();
            while (targetType != null)
            {
                typeStack.Push(targetType);
                targetType = targetType.BaseType;
            }

            var subTarget = IsRootNode ? Target : ElementType == NodeElementType.Field ? FieldInfo.GetValue(Target) : PropertyInfo.GetValue(Target);

            while (typeStack.TryPop(out var type))
            {
                fieldInfo.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
                propertyInfo.AddRange(type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
                methodInfo.AddRange(type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
            }

            foreach (var field in fieldInfo.Where(FieldSearchPredicate))
            {
                var property = Property != null ? Property.FindPropertyRelative(field.Name) : Root.FindProperty(field.Name);
                if (property == null)
                {
                    continue;
                }

                var node = new VaporInspectorNode(this, field, property, subTarget);
                children.Add(node);
            }

            foreach (var property in propertyInfo.Where(PropertySearchPredicate))
            {
                var path = Property != null ? $"{Property.propertyPath}_p_{property.Name}" : $"p_{property.Name}";
                var node = new VaporInspectorNode(this, path , property, subTarget);
                children.Add(node);
            }

            foreach (var method in methodInfo.Where(MethodSearchPredicate))
            {
                var path = Property != null ? $"{Property.propertyPath}_m_{method.Name}" : $"m_{method.Name}";
                var node = new VaporInspectorNode(this, path, method, subTarget);
                children.Add(node);
            }

            return children;
        }

        private void BuildGroupNodes(List<VaporInspectorNode> children)
        {
            if (children.Count == 0)
            {
                return;
            }

            VaporInspectorNode unmanagedNode = null;
            var nodeBag = new Dictionary<string, VaporInspectorNode>();
            var rootNodeList = new List<VaporInspectorNode>();
            foreach (var child in children)
            {
                if (child.Group == null)
                {
                    var atr = Target.GetType().GetCustomAttribute<UnManagedGroupAttribute>() ?? new UnManagedGroupAttribute();
                    unmanagedNode = new VaporInspectorNode(this, atr.ToGroupAttribute());
                }
                else
                {
                    foreach (var group in child.Groups)
                    {
                        if (!nodeBag.ContainsKey(group.GroupName))
                        {
                            nodeBag.Add(group.GroupName, new VaporInspectorNode(this, group));
                        }
                    }
                }
            }

            foreach (var groupNode in nodeBag.Values)
            {
                if (groupNode.Group.ParentName == string.Empty)
                {
                    rootNodeList.Add(groupNode);
                }
                else
                {
                    if (nodeBag.TryGetValue(groupNode.Group.ParentName, out var parentGroupNode))
                    {
                        parentGroupNode.Add(groupNode);
                    }
                }
            }

            foreach (var child in children)
            {
                if (child.Group == null)
                {
                    if (unmanagedNode == null) continue;

                    unmanagedNode.Add(child);
                }
                else
                {
                    if (nodeBag.TryGetValue(child.Group.GroupName, out var node))
                    {
                        node.Add(child);
                    }
                }
            }

            foreach (var rootNode in rootNodeList)
            {
                Add(rootNode);
            }

            if (unmanagedNode != null)
            {
                Add(unmanagedNode);
            }
        }

        public void Add(VaporInspectorNode child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public void Draw(VisualElement root)
        {
            var nodeElement = new NodeElement(this);
            VisualNode = nodeElement;
            root.Add(nodeElement);
        }
        
        public void Rebind(SerializedProperty property)
        {
            var onlyTarget = ElementType switch
            {
                NodeElementType.Root => 1,
                NodeElementType.Field => 0,
                NodeElementType.Property => 1,
                NodeElementType.Method => 1,
                NodeElementType.Group => 1,
                _ => 2
            };

            switch (onlyTarget)
            {
                case 1:
                    Property = property;
                    if (ElementType is NodeElementType.Root)
                    {
                        Path = Property.propertyPath;
                        VisualNode.Rename();
                    }
                    break;
                case 0:
                    Property = property.FindPropertyRelative(FieldInfo.Name);
                    Path = Property.propertyPath;
                    VisualNode.Rename();
                    break;
            }

            if (Children.Count == 0)
            {
                return;
            }

            foreach (var child in Children)
            {
                child.Rebind(Property);
            }
        }
        
        #region - Methods-
        public void InvokeMethod()
        {
            MethodInfo.Invoke(Property != null ? Property.boxedValue : Target, null);
        }
        #endregion

        #region - Attributes -
        public bool HasAttribute<T>() where T : Attribute
        {
            return ElementType switch
            {
                NodeElementType.Field => FieldInfo.IsDefined(typeof(T), true),
                NodeElementType.Property => PropertyInfo.IsDefined(typeof(T), true),
                NodeElementType.Method => MethodInfo.IsDefined(typeof(T), true),
                NodeElementType.Root => Type.IsDefined(typeof(T), true),
                _ => false,
            };
        }

        public bool TryGetAttribute<T>(out T attribute) where T : Attribute
        {
            bool result;
            switch (ElementType)
            {
                case NodeElementType.Field:
                    result = FieldInfo.IsDefined(typeof(T), true);
                    attribute = result ? FieldInfo.GetCustomAttribute<T>(true) : null;
                    return result;
                case NodeElementType.Property:
                    result = PropertyInfo.IsDefined(typeof(T), true);
                    attribute = result ? PropertyInfo.GetCustomAttribute<T>(true) : null;
                    return result;
                case NodeElementType.Method:
                    result = MethodInfo.IsDefined(typeof(T), true);
                    attribute = result ? MethodInfo.GetCustomAttribute<T>(true) : null;
                    return result;
                case NodeElementType.Root:
                    result = Type.IsDefined(typeof(T), true);
                    attribute = result ? Type.GetCustomAttribute<T>(true) : null;
                    return result;
                default:
                    attribute = null;
                    return false;
            }
        }

        public bool TryGetAttributes<T>(out T[] attribute) where T : Attribute
        {
            bool result;
            switch (ElementType)
            {
                case NodeElementType.Field:
                    result = FieldInfo.IsDefined(typeof(T), true);
                    attribute = (T[])(result ? FieldInfo.GetCustomAttributes<T>(true) : null);
                    return result;
                case NodeElementType.Property:
                    result = PropertyInfo.IsDefined(typeof(T), true);
                    attribute = (T[])(result ? PropertyInfo.GetCustomAttributes<T>(true) : null);
                    return result;
                case NodeElementType.Method:
                    result = MethodInfo.IsDefined(typeof(T), true);
                    attribute = (T[])(result ? MethodInfo.GetCustomAttributes<T>(true) : null);
                    return result;
                case NodeElementType.Root:
                    result = Type.IsDefined(typeof(T), true);
                    attribute = (T[])(result ? Type.GetCustomAttributes<T>(true) : null);
                    return result;
                default:
                    attribute = null;
                    return false;
            }
        }

        public void CleanupResolvers(bool includeChildren)
        {
            VisualNode.ClearResolvers();
            if (includeChildren)
            {
                foreach(var child in Children)
                {
                    child.CleanupResolvers(true);
                }
            }
        }

        public void RestartResolvers(bool includeChildren)
        {
            VisualNode.StartResolvers();
            if (includeChildren)
            {
                foreach (var child in Children)
                {
                    child.RestartResolvers(true);
                }
            }
        }

        public void CleanupResolverWithProperty(SerializedProperty propToRemove)
        {
            if(Property.propertyPath == propToRemove.propertyPath)
            {
                Debug.Log("Found Matching Prop To Cleanup");
                CleanupResolvers(true);
            }
            else
            {
                Debug.Log($"Skipping {VisualNode.name}");
                foreach (var child in Children)
                {
                    child.CleanupResolverWithProperty(propToRemove);
                }
            }
        }
        #endregion
    }
}
