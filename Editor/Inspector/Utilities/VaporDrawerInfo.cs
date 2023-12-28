using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VaporInspector;
using Object = UnityEngine.Object;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace VaporInspectorEditor
{
    public enum DrawerInfoType
    {
        Field,
        Property,
        Method
    }

    public class VaporDrawerInfo
    {
        private static Func<VaporGroupAttribute, int> ShortestToLongestName => group => group.GroupName.Length;

        public string Path { get; }
        public DrawerInfoType InfoType { get; }
        public FieldInfo FieldInfo { get; }
        public MethodInfo MethodInfo { get; }
        public PropertyInfo PropertyInfo { get; }
        public SerializedProperty Property { get; private set; }
        public object Target { get; private set; }

        public VaporDrawerInfo Parent { get; }
        public bool HasParent { get; }
        public List<VaporDrawerInfo> Children { get; }
        public bool HasChildren { get; }
        public bool HasUnmanagedChildren { get; }
        public bool IsDrawnWithVapor { get; }
        public bool IsUnityObject { get; }

        public List<VaporGroupAttribute> Groups { get; } = new();
        public VaporGroupAttribute ContainingGroup { get; }
        public bool IsUnmanagedGroup { get; }

        public string UpdatedGroupName { get; set; }
        public int UpdatedOrder { get; }
        public bool HasUpdatedOrder { get; }

        private readonly DrawWithVaporAttribute _drawWithVaporAttribute;
        private readonly UnManagedGroupAttribute _unmanagedGroupAttribute;

        public VaporDrawerInfo(string path, FieldInfo fieldInfo, SerializedProperty property, object target, VaporDrawerInfo parentDrawer/*, Dictionary<string, VaporDrawerInfo> pathToDrawerMap*/)
        {
            Path = path;
            FieldInfo = fieldInfo;
            InfoType = DrawerInfoType.Field;
            Property = property;
            Parent = parentDrawer;
            HasParent = parentDrawer != null;
            IsUnityObject = FieldInfo.FieldType.IsSubclassOf(typeof(Object));
            IsDrawnWithVapor = FieldInfo.FieldType.IsDefined(typeof(DrawWithVaporAttribute)) && !IsUnityObject;
            Target = target;

            if (TryGetAttributes<VaporGroupAttribute>(out var attributes))
            {
                if (attributes.Length > 1)
                {
                    Groups = attributes.OrderBy(ShortestToLongestName).ToList();
                }
                else
                {
                    Groups.Add(attributes[0]);
                }
                ContainingGroup = Groups[^1];
            }
            IsUnmanagedGroup = ContainingGroup == null || string.IsNullOrEmpty(ContainingGroup.GroupName);
            if (TryGetAttribute<PropertyOrderAttribute>(out var propOrder))
            {
                UpdatedOrder = propOrder.Order;
                HasUpdatedOrder = true;
            }

            if (!IsDrawnWithVapor) return;
            
            Children = new List<VaporDrawerInfo>();
            HasChildren = true;
            _drawWithVaporAttribute = FieldInfo.FieldType.GetCustomAttribute<DrawWithVaporAttribute>();
            _unmanagedGroupAttribute = FieldInfo.FieldType.GetCustomAttribute<UnManagedGroupAttribute>() ?? new UnManagedGroupAttribute();
            var targetType = FieldInfo.FieldType;
            var subTarget = FieldInfo.GetValue(Target);
            Stack<Type> typeStack = new();
            while (targetType != null)
            {
                typeStack.Push(targetType);
                targetType = targetType.BaseType;
            }
            List<FieldInfo> fieldInfoList = new();
            List<PropertyInfo> propertyInfoList = new();
            List<MethodInfo> methodInfoList = new();
            while (typeStack.TryPop(out var type))
            {
                fieldInfoList.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
                propertyInfoList.AddRange(type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
                methodInfoList.AddRange(type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
            }

            foreach (var field in fieldInfoList.Where(BaseVaporInspector.FieldSearchPredicate))
            {
                var relativeProperty = Property.FindPropertyRelative(field.Name);
                var relTarget = Property.boxedValue;// SerializedPropertyUtility.GetTargetObjectWithProperty(relativeProperty);
                var info = new VaporDrawerInfo(relativeProperty.propertyPath, field, relativeProperty, relTarget, this);
                if (info.IsUnmanagedGroup)
                {
                    HasUnmanagedChildren = true;
                }
                Children.Add(info);
            }
                
            foreach (var childProperty in propertyInfoList.Where(BaseVaporInspector.PropertySearchPredicate))
            {
                var info = new VaporDrawerInfo($"{Property.propertyPath}_p_{childProperty.Name}", childProperty, subTarget, this);
                if (info.IsUnmanagedGroup)
                {
                    HasUnmanagedChildren = true;
                }
                Children.Add(info);
            }
            foreach (var method in methodInfoList.Where(BaseVaporInspector.MethodSearchPredicate))
            {                    
                var info = new VaporDrawerInfo($"{Property.propertyPath}_m_{method.Name}", method, subTarget, this);
                if (info.IsUnmanagedGroup)
                {
                    HasUnmanagedChildren = true;
                }
                Children.Add(info);
            }
        }

        public VaporDrawerInfo(string path, PropertyInfo propertyInfo, object target, VaporDrawerInfo parentDrawer)
        {
            Path = path;
            PropertyInfo = propertyInfo;
            Target = target;

            InfoType = DrawerInfoType.Property;
            Parent = parentDrawer;
            HasParent = parentDrawer != null;

            if (TryGetAttributes<VaporGroupAttribute>(out var attributes))
            {
                if (attributes.Length > 1)
                {
                    Groups = attributes.OrderBy(ShortestToLongestName).ToList();
                }
                else
                {
                    Groups.Add(attributes[0]);
                }
                ContainingGroup = Groups[^1];
            }
            IsUnmanagedGroup = ContainingGroup == null || string.IsNullOrEmpty(ContainingGroup.GroupName);
            if (!TryGetAttribute<PropertyOrderAttribute>(out var propOrder)) return;
            
            UpdatedOrder = propOrder.Order;
            HasUpdatedOrder = true;
        }

        public VaporDrawerInfo(string path, MethodInfo methodInfo, object target, VaporDrawerInfo parentDrawer)
        {
            Path = path;
            MethodInfo = methodInfo;
            InfoType = DrawerInfoType.Method;
            Target = target;
            Parent = parentDrawer;
            HasParent = parentDrawer != null;

            if (TryGetAttributes<VaporGroupAttribute>(out var attributes))
            {
                if (attributes.Length > 1)
                {
                    Groups = attributes.OrderBy(ShortestToLongestName).ToList();
                }
                else
                {
                    Groups.Add(attributes[0]);
                }
                ContainingGroup = Groups[^1];
            }
            IsUnmanagedGroup = ContainingGroup == null || string.IsNullOrEmpty(ContainingGroup.GroupName);
            if (!TryGetAttribute<PropertyOrderAttribute>(out var propOrder)) return;
            
            UpdatedOrder = propOrder.Order;
            HasUpdatedOrder = true;
        }



        #region - Drawing -
        public void BuildGroups(string rootName, VaporGroupNode node, Dictionary<string, VaporGroupNode> nodeBag)
        {
            //Debug.Log($"Draw Groups of {Path} with RootName: {rootName}");
            if (IsUnmanagedGroup)
            {
                UpdatedGroupName = string.IsNullOrEmpty(rootName) ? $"_{FieldInfo.Name}" : $"{rootName}/_{FieldInfo.Name}";
                node = _DrawInlineGroup(node, ObjectNames.NicifyVariableName(FieldInfo.Name), UpdatedGroupName, HasUpdatedOrder ? UpdatedOrder : 0/*int.MaxValue*/);
            }
            else
            {
                // var parentName = string.IsNullOrEmpty(rootName) ? $"{ContainingGroup.GroupName}" : $"{rootName}/{ContainingGroup.GroupName}";
                UpdatedGroupName = string.IsNullOrEmpty(rootName) ? $"{ContainingGroup.GroupName}/_{FieldInfo.Name}" : $"{rootName}/{ContainingGroup.GroupName}/_{FieldInfo.Name}";
                //node = _DrawVerticalGroup(node, UpdatedGroupName, HasUpdatedOrder ? UpdatedOrder : ContainingGroup.Order);
                node = _DrawInlineGroup(node, ObjectNames.NicifyVariableName(FieldInfo.Name), UpdatedGroupName, HasUpdatedOrder ? UpdatedOrder : ContainingGroup.Order);
            }
            var rootNode = node;
            VaporGroupNode unmanagedNode = null;
            if (HasUnmanagedChildren)
            {
                unmanagedNode = _DrawUnmanagedGroup(rootNode, $"{UpdatedGroupName}-Unmanaged");
            }
            foreach (var child in Children)
            {
                if (!child.IsUnmanagedGroup)
                {
                    child.UpdatedGroupName = $"{UpdatedGroupName}/{child.ContainingGroup.GroupName}";
                    foreach (var attribute in child.Groups)
                    {
                        // string parentName = attribute.ParentName != string.Empty ? $"{UpdatedGroupName}/{attribute.ParentName}" : UpdatedGroupName;
                        var groupName = $"{UpdatedGroupName}/{attribute.GroupName}";
                        switch (attribute.Type)
                        {
                            case UIGroupType.Horizontal:
                                if (attribute is HorizontalGroupAttribute)
                                {
                                    node = _DrawHorizontalGroup(node, groupName, child.HasUpdatedOrder ? child.UpdatedOrder : attribute.Order);
                                }
                                break;
                            case UIGroupType.Vertical:
                                if (attribute is VerticalGroupAttribute)
                                {
                                    node = _DrawVerticalGroup(node, groupName, child.HasUpdatedOrder ? child.UpdatedOrder : attribute.Order);
                                }
                                break;
                            case UIGroupType.Foldout:
                                if (attribute is FoldoutGroupAttribute foldoutAttribute)
                                {
                                    node = _DrawFoldoutGroup(node, groupName, foldoutAttribute.Header, child.HasUpdatedOrder ? child.UpdatedOrder : attribute.Order);
                                }
                                break;
                            case UIGroupType.Box:
                                if (attribute is BoxGroupAttribute boxAttribute)
                                {
                                    node = _DrawBoxGroup(node, groupName, boxAttribute.Header, child.HasUpdatedOrder ? child.UpdatedOrder : attribute.Order);
                                }
                                break;
                            case UIGroupType.Tab:
                                if (attribute is TabGroupAttribute)
                                {
                                    node = _DrawTabGroup(node, groupName, child.HasUpdatedOrder ? child.UpdatedOrder : attribute.Order);
                                }
                                break;
                            case UIGroupType.Title:
                                if (attribute is TitleGroupAttribute titleAttribute)
                                {
                                    node = _DrawTitleGroup(node, groupName, titleAttribute, child.HasUpdatedOrder ? child.UpdatedOrder : attribute.Order);
                                }
                                break;
                        }
                    }
                    if (child.IsDrawnWithVapor)
                    {
                        child.BuildGroups(/*inspector, */UpdatedGroupName, node, nodeBag);
                    }
                    else
                    {
                        node.AddContent(/*inspector, */child);
                    }
                }
                else
                {
                    child.UpdatedGroupName = UpdatedGroupName;
                    if (child.IsDrawnWithVapor)
                    {
                        child.BuildGroups(/*inspector, */UpdatedGroupName, rootNode, nodeBag);
                    }
                    else
                    {
                        unmanagedNode.AddContent(/*inspector, */child);
                    }
                }
            }

            VaporGroupNode _DrawUnmanagedGroup(VaporGroupNode parentNode, string groupName)
            {
                if (!nodeBag.TryGetValue(groupName, out var foundNode))
                {
                    var order = _unmanagedGroupAttribute.UnmanagedGroupOrder;
                    VisualElement ve = _unmanagedGroupAttribute.UnmanagedGroupType switch
                    {
                        UIGroupType.Horizontal => new StyledHorizontalGroup()
                        {
                            name = groupName
                        },
                        UIGroupType.Vertical => new StyledVerticalGroup()
                        {
                            name = groupName
                        },
                        UIGroupType.Foldout => new StyledFoldout(_unmanagedGroupAttribute.UnmanagedGroupHeader)
                        {
                            name = groupName
                        },
                        UIGroupType.Box => new StyledHeaderBox(_unmanagedGroupAttribute.UnmanagedGroupHeader)
                        {
                            name = groupName
                        },
                        UIGroupType.Tab => new StyledTabGroup()
                        {
                            name = groupName
                        },
                        UIGroupType.Title => new StyledTitleGroup(new TitleGroupAttribute(groupName, _unmanagedGroupAttribute.UnmanagedGroupHeader))
                        {
                            name = groupName
                        },
                        _ => new StyledVerticalGroup()
                        {
                            name = groupName
                        },
                    };

                    //var vertical = new StyledVerticalGroup
                    //{
                    //    name = groupName
                    //};
                    parentNode.AddChild(_unmanagedGroupAttribute.UnmanagedGroupType, groupName, order, ve);
                    var added = parentNode.Children[^1];
                    nodeBag.Add(groupName, added);
                    return added;
                }
                else
                {
                    return foundNode;
                }
            }

            VaporGroupNode _DrawHorizontalGroup(VaporGroupNode parentNode, string groupName, int order)
            {
                if (!nodeBag.TryGetValue(groupName, out var foundNode))
                {
                    var horizontal = new StyledHorizontalGroup
                    {
                        name = groupName
                    };
                    parentNode.AddChild(UIGroupType.Horizontal, groupName, order, horizontal);
                    var added = parentNode.Children[^1];
                    nodeBag.Add(groupName, added);
                    return added;
                }
                else
                {
                    return foundNode;
                }
            }

            VaporGroupNode _DrawVerticalGroup(VaporGroupNode parentNode, string groupName, int order)
            {
                if (!nodeBag.TryGetValue(groupName, out var foundNode))
                {
                    var vertical = new StyledVerticalGroup
                    {
                        name = groupName
                    };
                    parentNode.AddChild(UIGroupType.Vertical, groupName, order, vertical);
                    var added = parentNode.Children[^1];
                    nodeBag.Add(groupName, added);
                    return added;
                }
                else
                {
                    return foundNode;
                }
            }

            VaporGroupNode _DrawFoldoutGroup(VaporGroupNode parentNode, string groupName, string header, int order)
            {
                if (!nodeBag.TryGetValue(groupName, out var foundNode))
                {
                    var foldout = new StyledFoldout(header)
                    {
                        name = groupName
                    };
                    parentNode.AddChild(UIGroupType.Foldout, groupName, order, foldout);
                    var added = parentNode.Children[^1];
                    nodeBag.Add(groupName, added);
                    return added;
                }
                else
                {
                    return foundNode;
                }
            }

            VaporGroupNode _DrawBoxGroup(VaporGroupNode parentNode, string groupName, string header, int order)
            {
                if (!nodeBag.TryGetValue(groupName, out var foundNode))
                {
                    var box = new StyledHeaderBox(header)
                    {
                        name = groupName
                    };
                    parentNode.AddChild(UIGroupType.Box, groupName, order, box);
                    var added = parentNode.Children[^1];
                    nodeBag.Add(groupName, added);
                    return added;
                }
                else
                {
                    return foundNode;
                }
            }

            VaporGroupNode _DrawTabGroup(VaporGroupNode parentNode, string groupName, int order)
            {
                if (!nodeBag.TryGetValue(groupName, out var foundNode))
                {
                    var tabs = new StyledTabGroup()
                    {
                        name = groupName
                    };
                    parentNode.AddChild(UIGroupType.Tab, groupName, order, tabs);
                    var added = parentNode.Children[^1];
                    nodeBag.Add(groupName, added);
                    return added;
                }
                else
                {
                    return foundNode;
                }
            }

            VaporGroupNode _DrawTitleGroup(VaporGroupNode parentNode, string groupName, TitleGroupAttribute attribute, int order)
            {
                if (!nodeBag.TryGetValue(groupName, out var foundNode))
                {
                    var title = new StyledTitleGroup(attribute)
                    {
                        name = groupName
                    };
                    parentNode.AddChild(UIGroupType.Title, groupName, order, title);
                    var added = parentNode.Children[^1];
                    nodeBag.Add(groupName, added);
                    return added;
                }
                else
                {
                    return foundNode;
                }
            }

            VaporGroupNode _DrawInlineGroup(VaporGroupNode parentNode, string header, string groupName, int order)
            {
                return _drawWithVaporAttribute.InlinedGroupType switch
                {
                    UIGroupType.Horizontal => _DrawHorizontalGroup(parentNode, groupName, order),
                    UIGroupType.Vertical => _DrawVerticalGroup(parentNode, groupName, order),
                    UIGroupType.Foldout => _DrawFoldoutGroup(parentNode, groupName, header, order),
                    UIGroupType.Box => _DrawBoxGroup(parentNode, groupName, header, order),
                    UIGroupType.Tab => _DrawVerticalGroup(parentNode, groupName, order),
                    UIGroupType.Title => _DrawTitleGroup(parentNode, groupName, new TitleGroupAttribute(groupName, header), order),
                    _ => _DrawVerticalGroup(parentNode, groupName, order),
                };
            }
        }

        public void Rebind(SerializedProperty property)
        {
            var onlyTarget = InfoType switch
            {
                DrawerInfoType.Field => false,
                DrawerInfoType.Property => true,
                DrawerInfoType.Method => true,
                _ => true
            };

            if (onlyTarget)
            {
                Property = property;
                Target = property.boxedValue;
            }
            else
            {
                Property = property.FindPropertyRelative(FieldInfo.Name);
                Target = property.boxedValue;
            }

            if (!HasChildren)
            {
                return;
            }

            foreach (var child in Children)
            {
                child.Rebind(Property);
            }
        }

        #endregion

        #region - Methods-

        public void InvokeMethod()
        {
            MethodInfo.Invoke(Property != null ? Property.boxedValue : Target, null);
        }

        #endregion

        #region - Attributes -
        public bool HasAttribute<T>() where T : Attribute
        {
            return InfoType switch
            {
                DrawerInfoType.Field => FieldInfo.IsDefined(typeof(T), true),
                DrawerInfoType.Property => PropertyInfo.IsDefined(typeof(T), true),
                DrawerInfoType.Method => MethodInfo.IsDefined(typeof(T), true),
                _ => false,
            };
        }

        public bool TryGetAttribute<T>(out T attribute) where T : Attribute
        {
            bool result;
            switch (InfoType)
            {
                case DrawerInfoType.Field:
                    result = FieldInfo.IsDefined(typeof(T), true);
                    attribute = result ? FieldInfo.GetCustomAttribute<T>(true) : null;
                    return result;
                case DrawerInfoType.Property:
                    result = PropertyInfo.IsDefined(typeof(T), true);
                    attribute = result ? PropertyInfo.GetCustomAttribute<T>(true) : null;
                    return result;
                case DrawerInfoType.Method:
                    result = MethodInfo.IsDefined(typeof(T), true);
                    attribute = result ? MethodInfo.GetCustomAttribute<T>(true) : null;
                    return result;
                default:
                    attribute = null;
                    return false;
            }
        }

        public bool TryGetAttributes<T>(out T[] attribute) where T : Attribute
        {
            bool result;
            switch (InfoType)
            {
                case DrawerInfoType.Field:
                    result = FieldInfo.IsDefined(typeof(T), true);
                    attribute = (T[])(result ? FieldInfo.GetCustomAttributes<T>(true) : null);
                    return result;
                case DrawerInfoType.Property:
                    result = PropertyInfo.IsDefined(typeof(T), true);
                    attribute = (T[])(result ? PropertyInfo.GetCustomAttributes<T>(true) : null);
                    return result;
                case DrawerInfoType.Method:
                    result = MethodInfo.IsDefined(typeof(T), true);
                    attribute = (T[])(result ? MethodInfo.GetCustomAttributes<T>(true) : null);
                    return result;
                default:
                    attribute = null;
                    return false;
            }
        }
        #endregion
    }
}
