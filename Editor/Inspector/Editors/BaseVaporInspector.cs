using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VaporInspector;

namespace VaporInspectorEditor
{
    public abstract class BaseVaporInspector : Editor
    {
        public static readonly Func<FieldInfo, bool> FieldSearchPredicate = f => !f.IsDefined(typeof(HideInInspector))
                                                                                 && !f.IsDefined(typeof(NonSerializedAttribute))
                                                                                 && (f.IsPublic || f.IsDefined(typeof(SerializeField)));
        public static readonly Func<MethodInfo, bool> MethodSearchPredicate = f => f.IsDefined(typeof(ButtonAttribute));
        public static readonly Func<PropertyInfo, bool> PropertySearchPredicate = f => f.IsDefined(typeof(ShowInInspectorAttribute));

        protected static readonly Dictionary<string, VaporGroupNode> NodeBag = new();

        protected readonly List<VaporDrawerInfo> SerializedDrawerInfo = new();
        // protected readonly Dictionary<string, VaporDrawerInfo> PropertyPathToDrawerMap = new();
        protected VaporGroupNode RootNode;
        protected string UnmanagedGroupName;

        public override VisualElement CreateInspectorGUI()
        {
            var inspector = new VisualElement();
            inspector.Add(DrawScript());
            GetSerializedDrawerInfo();
            DrawVaporInspector(inspector);

            return inspector;
        }

        protected VisualElement DrawScript()
        {
            var script = new PropertyField(serializedObject.FindProperty("m_Script"));
            var hide = target.GetType().IsDefined(typeof(HideMonoScriptAttribute));
            script.style.display = hide ? DisplayStyle.None : DisplayStyle.Flex;
            script.SetEnabled(false);
            return script;
        }

        #region - Vapor Inspector -
        protected void DrawVaporInspector(VisualElement container)
        {
            NodeBag.Clear();
            UnmanagedGroupName = $"{target.GetType().Name}-Unmanaged";
            RootNode = new VaporGroupNode(null, null, container);

            BuildGroups();
            BuildNodeGraph();
        }

        protected void BuildGroups()
        {
            var unmanagedNode = _DrawUnmanagedGroupNode(RootNode);

            foreach (var drawer in SerializedDrawerInfo)
            {
                if (!drawer.IsUnmanagedGroup)
                {
                    var node = RootNode;
                    foreach (var attribute in drawer.Groups)
                    {
                        switch (attribute.Type)
                        {
                            case UIGroupType.Horizontal:
                                if (attribute is HorizontalGroupAttribute horizontalAttribute)
                                {
                                    node = _DrawHorizontalGroupNode(horizontalAttribute, node);
                                }
                                break;
                            case UIGroupType.Vertical:
                                if (attribute is VerticalGroupAttribute verticalAttribute)
                                {
                                    node = _DrawVerticalGroupNode(verticalAttribute, node);
                                }
                                break;
                            case UIGroupType.Foldout:
                                if (attribute is FoldoutGroupAttribute foldoutAttribute)
                                {
                                    node = _DrawFoldoutGroupNode(foldoutAttribute, node);
                                }
                                break;
                            case UIGroupType.Box:
                                if (attribute is BoxGroupAttribute boxAttribute)
                                {
                                    node = _DrawBoxGroupNode(boxAttribute, node);
                                }
                                break;
                            case UIGroupType.Tab:
                                if (attribute is TabGroupAttribute tabAttribute)
                                {
                                    node = _DrawTabGroupNode(tabAttribute, node);
                                }
                                break;
                            case UIGroupType.Title:
                                if (attribute is TitleGroupAttribute titleAttribute)
                                {
                                    node = _DrawTitleGroupNode(titleAttribute, node);
                                }
                                break;
                        }
                    }
                    if (drawer.IsDrawnWithVapor)
                    {
                        drawer.BuildGroups("", node, NodeBag);
                    }
                    else
                    {
                        // node.AddContent(drawer);
                        node.AddChild(drawer);
                    }
                }
                else
                {
                    if (drawer.IsDrawnWithVapor)
                    {
                        drawer.BuildGroups(UnmanagedGroupName, unmanagedNode, NodeBag);
                    }
                    else
                    {
                        // unmanagedNode.AddContent(drawer);
                        unmanagedNode.AddChild(drawer);
                    }
                }
            }

            VaporGroupNode _DrawUnmanagedGroupNode(VaporGroupNode parentNode)
            {
                if (!NodeBag.TryGetValue(UnmanagedGroupName, out var node))
                {
                    var atr = target.GetType().GetCustomAttribute<UnManagedGroupAttribute>() ?? new UnManagedGroupAttribute();
                    VisualElement ve = atr.UnmanagedGroupType switch
                    {
                        UIGroupType.Horizontal => new StyledHorizontalGroup()
                        {
                            name = UnmanagedGroupName
                        },
                        UIGroupType.Vertical => new StyledVerticalGroup()
                        {
                            name = UnmanagedGroupName
                        },
                        UIGroupType.Foldout => new StyledFoldout(atr.UnmanagedGroupHeader)
                        {
                            name = UnmanagedGroupName
                        },
                        UIGroupType.Box => new StyledHeaderBox(atr.UnmanagedGroupHeader)
                        {
                            name = UnmanagedGroupName
                        },
                        UIGroupType.Tab => new StyledTabGroup()
                        {
                            name = UnmanagedGroupName
                        },
                        UIGroupType.Title => new StyledTitleGroup(new TitleGroupAttribute(UnmanagedGroupName, atr.UnmanagedGroupHeader))
                        {
                            name = UnmanagedGroupName
                        },
                        _ => new StyledVerticalGroup()
                        {
                            name = UnmanagedGroupName
                        },
                    };
                    parentNode.AddChild(atr.UnmanagedGroupType, UnmanagedGroupName, atr.UnmanagedGroupOrder, ve);
                    var added = parentNode.Children[^1];
                    NodeBag.Add(UnmanagedGroupName, added);
                    return added;
                }
                else
                {
                    return node;
                }
            }

            VaporGroupNode _DrawHorizontalGroupNode(HorizontalGroupAttribute attribute, VaporGroupNode parentNode)
            {
                if (!NodeBag.TryGetValue(attribute.GroupName, out var node))
                {
                    var horizontal = new StyledHorizontalGroup
                    {
                        name = attribute.GroupName
                    };
                    parentNode.AddChild(attribute, horizontal);
                    var added = parentNode.Children[^1];
                    NodeBag.Add(attribute.GroupName, added);
                    return added;
                }
                else
                {
                    return node;
                }
            }

            VaporGroupNode _DrawVerticalGroupNode(VerticalGroupAttribute attribute, VaporGroupNode parentNode)
            {
                if (!NodeBag.TryGetValue(attribute.GroupName, out var node))
                {
                    var vertical = new StyledVerticalGroup
                    {
                        name = attribute.GroupName
                    };
                    parentNode.AddChild(attribute, vertical);
                    var added = parentNode.Children[^1];
                    NodeBag.Add(attribute.GroupName, added);
                    return added;
                }
                else
                {
                    return node;
                }
            }

            VaporGroupNode _DrawFoldoutGroupNode(FoldoutGroupAttribute attribute, VaporGroupNode parentNode)
            {
                if (!NodeBag.TryGetValue(attribute.GroupName, out var node))
                {
                    var foldout = new StyledFoldout(attribute.Header)
                    {
                        name = attribute.GroupName
                    };
                    parentNode.AddChild(attribute, foldout);
                    var added = parentNode.Children[^1];
                    NodeBag.Add(attribute.GroupName, added);
                    return added;
                }
                else
                {
                    return node;
                }
            }

            VaporGroupNode _DrawBoxGroupNode(BoxGroupAttribute attribute, VaporGroupNode parentNode)
            {
                if (!NodeBag.TryGetValue(attribute.GroupName, out var node))
                {
                    var box = new StyledHeaderBox(attribute.Header)
                    {
                        name = attribute.GroupName
                    };
                    parentNode.AddChild(attribute, box);
                    var added = parentNode.Children[^1];
                    NodeBag.Add(attribute.GroupName, added);
                    return added;
                }
                else
                {
                    return node;
                }
            }

            VaporGroupNode _DrawTabGroupNode(TabGroupAttribute attribute, VaporGroupNode parentNode)
            {
                if (!NodeBag.TryGetValue(attribute.GroupName, out var node))
                {
                    var tabs = new StyledTabGroup()
                    {
                        name = attribute.GroupName
                    };
                    parentNode.AddChild(attribute, tabs);
                    var added = parentNode.Children[^1];
                    NodeBag.Add(attribute.GroupName, added);
                    return added;
                }
                else
                {
                    return node;
                }
            }

            VaporGroupNode _DrawTitleGroupNode(TitleGroupAttribute attribute, VaporGroupNode parentNode)
            {
                if (!NodeBag.TryGetValue(attribute.GroupName, out var node))
                {
                    var title = new StyledTitleGroup(attribute)
                    {
                        name = attribute.GroupName
                    };
                    parentNode.AddChild(attribute, title);
                    var added = parentNode.Children[^1];
                    NodeBag.Add(attribute.GroupName, added);
                    return added;
                }
                else
                {
                    return node;
                }
            }
        }
        protected void BuildNodeGraph()
        {
            foreach (var childNode in RootNode.Children.OrderBy(n => n.GroupOrder).ThenBy(n => n.AddOrder))
            {
                if (!childNode.ShouldDraw) continue;

                // Debug.Log($"Node: {childNode.Container.name} - {childNode.GroupOrder}");
                // childNode.BuildContent();
                RootNode.Container.Add(childNode.Container);
                _TraverseNodes(childNode);
            }

            static void _TraverseNodes(VaporGroupNode parentNode)
            {
                foreach (var childNode in parentNode.Children.OrderBy(n => n.GroupOrder).ThenBy(n => n.AddOrder))
                {
                    if (!childNode.ShouldDraw) continue;
                    if (parentNode.GroupType == UIGroupType.Horizontal)
                    {
                        childNode.Container.style.flexGrow = 1;
                    }
                    
                    // Debug.Log($"<b>Node</b>: {childNode.Container.name} - {childNode.GroupOrder}");
                    // childNode.BuildContent();
                    parentNode.Container.Add(childNode.Container);
                    _TraverseNodes(childNode);
                }
            }
        }
        #endregion        

        #region - Helpers -
        protected void GetSerializedDrawerInfo()
        {
            SerializedDrawerInfo.Clear();
            // PropertyPathToDrawerMap.Clear();
            List<FieldInfo> fieldInfo = new();
            List<PropertyInfo> propertyInfo = new();
            List<MethodInfo> methodInfo = new();
            var targetType = target.GetType();
            Stack<Type> typeStack = new();
            while (targetType != null)
            {
                typeStack.Push(targetType);
                targetType = targetType.BaseType;
            }

            while (typeStack.TryPop(out var type))
            {
                fieldInfo.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
                propertyInfo.AddRange(type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
                methodInfo.AddRange(type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
            }

            foreach (var field in fieldInfo.Where(FieldSearchPredicate))
            {
                var property = serializedObject.FindProperty(field.Name);
                if (property == null)
                {
                    continue;
                }

                var info = new VaporDrawerInfo(property.propertyPath, field, property, target, null);
                // var info = new VaporDrawerInfo(property.propertyPath, field, property, target, null, PropertyPathToDrawerMap);
                SerializedDrawerInfo.Add(info);
                // PropertyPathToDrawerMap.Add(property.propertyPath, info);
            }

            foreach (var property in propertyInfo.Where(PropertySearchPredicate))
            {
                var info = new VaporDrawerInfo($"{target.name}_p_{property.Name}", property, target, null);
                SerializedDrawerInfo.Add(info);
            }
            foreach (var method in methodInfo.Where(MethodSearchPredicate))
            {
                var info = new VaporDrawerInfo(method.Name, method, target, null);
                SerializedDrawerInfo.Add(info);
            }

            // foreach (var sf in _serializedDrawerInfo)
            // {
            //     //Debug.Log($"{sf.FieldInfo.Name} - {sf.FieldInfo.FieldType} - {sf.Path}");
            // }
        }
        #endregion
    }
}
