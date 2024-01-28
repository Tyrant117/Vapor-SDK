using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VaporInspector;
using FilePathAttribute = VaporInspector.FilePathAttribute;
using Object = UnityEngine.Object;

namespace VaporInspectorEditor
{
    public static class DrawerUtility
    {

        #region - Node Based Drawers -
        public static VisualElement DrawVaporFieldWithVerticalLayout(VaporInspectorNode node)
        {
            var vertical = new StyledVerticalGroup(0, 0, true);
            var field = DrawVaporField(node);
            vertical.Add(field);
            return vertical;
        }

        public static VisualElement DrawVaporField(VaporInspectorNode node)
        {
            if (HasCustomPropertyDrawer(node.FieldInfo.FieldType) && !node.HasAttribute<IgnoreCustomDrawerAttribute>())
            {
                var field = new PropertyField(node.Property)
                {
                    name = node.Path,
                    userData = node,
                };
                return field;
            }

            if (node.TryGetAttribute<ValueDropdownAttribute>(out var dropdownAtr))
            {
                return DrawNodeVaporValueDropdown(node, dropdownAtr);
            }
            
            if (node.Property.isArray && node.Property.propertyType != SerializedPropertyType.String && !node.HasAttribute<DrawWithUnityAttribute>())
            {
                return DrawNodeVaporList(node);
            }

            return _DrawVaporField();

            VisualElement _DrawVaporField()
            {
                var field = new PropertyField(node.Property)
                {
                    name = node.Path,
                    userData = node,
                };
                if (node.IsUnityObject)
                {
                    field.AddManipulator(new ContextualMenuManipulator(evt =>
                    {
                        evt.menu.AppendAction("Set To Null", _ =>
                        {
                            node.Property.boxedValue = null;
                            node.Property.serializedObject.ApplyModifiedProperties();
                        });
                        evt.menu.AppendSeparator();
                        evt.menu.AppendAction("Copy", _ => { ClipboardUtility.WriteToBuffer(node); });
                        evt.menu.AppendAction("Paste", _ => { ClipboardUtility.ReadFromBuffer(node); }, _ =>
                        {
                            var read = ClipboardUtility.CanReadFromBuffer(node);
                            return read ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                        }, node);
                        evt.menu.AppendSeparator();
                        evt.menu.AppendAction("Copy Property Path", _ => { EditorGUIUtility.systemCopyBuffer = node.FieldInfo.Name; });
                        evt.menu.AppendAction("Open Inspector", _ => OpenNewInspector(node.Property.objectReferenceValue));
                    }));
                }
                else
                {
                    field.AddManipulator(new ContextualMenuManipulator(evt =>
                    {
                        evt.menu.AppendAction("Reset", _ =>
                        {
                            var clonedTarget = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(node.Target.GetType());
                            var val = node.FieldInfo.GetValue(clonedTarget);
                            node.Property.boxedValue = val;
                            node.Property.serializedObject.ApplyModifiedProperties();
                        });
                        evt.menu.AppendSeparator();
                        evt.menu.AppendAction("Copy", _ => { ClipboardUtility.WriteToBuffer(node); });
                        evt.menu.AppendAction("Paste", _ => { ClipboardUtility.ReadFromBuffer(node); }, _ =>
                        {
                            var read = ClipboardUtility.CanReadFromBuffer(node);
                            return read ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                        }, node);
                        evt.menu.AppendSeparator();
                        evt.menu.AppendAction("Copy Property Path", _ => { EditorGUIUtility.systemCopyBuffer = node.FieldInfo.Name; });
                    }));
                }

                field.RegisterCallback<GeometryChangedEvent>(OnNodePropertyBuilt);
                return field;
            }
        }

        public static VisualElement DrawVaporProperty(VaporInspectorNode node)
        {
            var clonedTarget = node.Target;
            clonedTarget = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(clonedTarget.GetType());
            var val = node.PropertyInfo.GetValue(clonedTarget).ToString();
            if (node.FieldInfo != null)
            {
                val = node.FieldInfo.GetValue(clonedTarget).ToString();
            }

            var tooltip = "";
            if (node.TryGetAttribute<RichTextTooltipAttribute>(out var rtAtr))
            {
                tooltip = rtAtr.Tooltip;
            }

            var prop = new TextField(node.Path[(node.Path.IndexOf("p_", StringComparison.Ordinal) + 2)..])
            {
                name = node.Path,
            };
            var label = prop.Q<Label>();
            label.tooltip = tooltip;
            label.AddToClassList("unity-base-field__label");
            prop.SetValueWithoutNotify(val);
            prop.SetEnabled(false);
            
            if (node.TryGetAttribute<ShowInInspectorAttribute>(out var showAtr))
            {
                if (showAtr.Dynamic)
                {
                    prop.schedule.Execute(() => OnNodeDynamicPropertyShow(node, prop)).Every(showAtr.DynamicInterval);
                }
            }

            return prop;
        }
        
        public static VisualElement DrawVaporMethod(VaporInspectorNode node)
        {
            var atr = node.MethodInfo.GetCustomAttribute<ButtonAttribute>();
            var label = atr.Label;
            if (string.IsNullOrEmpty(label))
            {
                label = ObjectNames.NicifyVariableName(node.MethodInfo.Name);
            }

            var tooltip = "";
            if (node.TryGetAttribute<RichTextTooltipAttribute>(out var rtAtr))
            {
                tooltip = rtAtr.Tooltip;
            }

            var button = new StyledButton(atr.Size)
            {
                tooltip = tooltip,
                name = node.Path,
                text = label,
                userData = node
            };
            button.RegisterCallback<GeometryChangedEvent>(OnNodeMethodBuilt);
            return button;
        }

        public static VisualElement DrawGroupElement(VaporInspectorNode node, VaporGroupAttribute groupAttribute)
        {
            var ve = groupAttribute.Type switch
            {
                UIGroupType.Horizontal => _DrawHorizontalGroupNode((HorizontalGroupAttribute)groupAttribute),
                UIGroupType.Vertical => _DrawVerticalGroupNode((VerticalGroupAttribute)groupAttribute),
                UIGroupType.Foldout => _DrawFoldoutGroupNode((FoldoutGroupAttribute)groupAttribute),
                UIGroupType.Box => _DrawBoxGroupNode((BoxGroupAttribute)groupAttribute),
                UIGroupType.Tab => _DrawTabGroupNode((TabGroupAttribute)groupAttribute),
                UIGroupType.Title => _DrawTitleGroupNode((TitleGroupAttribute)groupAttribute),
                _ => throw new ArgumentOutOfRangeException()
            };
            ve.userData = node;
            ve.RegisterCallback<GeometryChangedEvent>(OnNodeGroupBuilt);
            return ve;
            
            VisualElement _DrawHorizontalGroupNode(HorizontalGroupAttribute attribute)
            {
                var horizontal = new StyledHorizontalGroup
                {
                    name = attribute.GroupName
                };
                return horizontal;
            }

            VisualElement _DrawVerticalGroupNode(VerticalGroupAttribute attribute)
            {
                var vertical = new StyledVerticalGroup
                {
                    name = attribute.GroupName
                };
                return vertical;
            }

            VisualElement _DrawFoldoutGroupNode(FoldoutGroupAttribute attribute)
            {
                var foldout = new StyledFoldout(attribute.Header)
                {
                    name = attribute.GroupName
                };
                return foldout;
            }

            VisualElement _DrawBoxGroupNode(BoxGroupAttribute attribute)
            {
                var box = new StyledHeaderBox(attribute.Header)
                {
                    name = attribute.GroupName
                };
                return box;
            }

            VisualElement _DrawTabGroupNode(TabGroupAttribute attribute)
            {
                var tabs = new StyledTabGroup()
                {
                    name = attribute.GroupName
                };
                return tabs;
            }

            VisualElement _DrawTitleGroupNode(TitleGroupAttribute attribute)
            {
                var title = new StyledTitleGroup(attribute)
                {
                    name = attribute.GroupName
                };
                return title;
            }
        }
        
        private static VisualElement DrawNodeVaporValueDropdown(VaporInspectorNode node, ValueDropdownAttribute dropdownAtr)
        {
            List<string> keys = new();
            List<object> values = new();
            switch (dropdownAtr.ResolverType)
            {
                case ResolverType.None:
                    break;
                case ResolverType.Property:
                    if (dropdownAtr.AssemblyQualifiedType != null)
                    {
                        _ConvertToTupleList(keys, values, _GetKeysProperty(dropdownAtr.AssemblyQualifiedType, dropdownAtr.Resolver[1..]));
                    }
                    else
                    {
                        var pi = ReflectionUtility.GetProperty(node.Target, dropdownAtr.Resolver[1..]);
                        _ConvertToTupleList(keys, values, (IList)pi.GetValue(node.Target));
                    }

                    break;
                case ResolverType.Method:
                    if (dropdownAtr.AssemblyQualifiedType != null)
                    {
                        _ConvertToTupleList(keys, values, _GetKeysMethod(dropdownAtr.AssemblyQualifiedType, dropdownAtr.Resolver[1..]));
                    }
                    else
                    {
                        var mi = ReflectionUtility.GetMethod(node.Target, dropdownAtr.Resolver[1..]);
                        _ConvertToTupleList(keys, values, (IList)mi.Invoke(node.Target, null));
                    }

                    break;
                case ResolverType.Field:
                    if (dropdownAtr.AssemblyQualifiedType != null)
                    {
                        _ConvertToTupleList(keys, values, _GetKeysField(dropdownAtr.AssemblyQualifiedType, dropdownAtr.Resolver[1..]));
                    }
                    else
                    {
                        var fi = ReflectionUtility.GetField(node.Target, dropdownAtr.Resolver[1..]);
                        _ConvertToTupleList(keys, values, (IList)fi.GetValue(node.Target));
                    }

                    break;
            }
            
            var tooltip = "";
            if (node.TryGetAttribute<RichTextTooltipAttribute>(out var rtAtr))
            {
                tooltip = rtAtr.Tooltip;
            }

            if (dropdownAtr.Searchable)
            {
                var indexOfCurrent = values.IndexOf(node.Property.boxedValue);
                var currentNameValue = indexOfCurrent >= 0 ? keys[indexOfCurrent] : "null";
                var field = new SearchableDropdown<string>(node.Property.displayName, currentNameValue)
                {
                    name = node.Path,
                    userData = (node, values),
                    tooltip = tooltip,
                };
                field.AddToClassList("unity-base-field__aligned");
                field.SetChoices(keys);
                field.ValueChanged += OnNodeSearchableDropdownChanged;
                return field;
            }
            else
            {
                var container = new VisualElement()
                {
                    name = node.Path,
                    userData = node
                };
                var index = values.IndexOf(node.Property.boxedValue);
                var field = new DropdownField(node.Property.displayName, keys, index)
                {
                    tooltip = tooltip,
                    userData = values
                };
                field.AddToClassList("unity-base-field__aligned");
                field.RegisterValueChangedCallback(OnNodeDropdownChanged);
                container.Add(field);
                return container;
            }

            static void _ConvertToTupleList(List<string> keys, List<object> values, IList convert)
            {
                if (convert == null)
                {
                    return;
                }
                
                foreach (var obj in convert)
                {
                    var item1 = (string)obj.GetType().GetField("Item1", BindingFlags.Instance | BindingFlags.Public)
                        ?.GetValue(obj);
                    var item2 = obj.GetType().GetField("Item2", BindingFlags.Instance | BindingFlags.Public)
                        ?.GetValue(obj);
                    if (item1 == null || item2 == null)
                    {
                        continue;
                    }

                    keys.Add(item1);
                    values.Add(item2);
                }
            }

            static IList _GetKeysProperty(Type type, string valuesName)
            {
                var propertyInfo = type.GetProperty(valuesName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (propertyInfo == null)
                {
                    var allTypes = ReflectionUtility.GetSelfAndBaseTypes(type);
                    foreach (var t in allTypes)
                    {
                        propertyInfo = t.GetProperty(valuesName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        if (propertyInfo != null)
                        {
                            break;
                        }
                    }
                }

                if (propertyInfo == null) return null;

                var keys = propertyInfo.GetValue(null);
                if (keys is IList keyList)
                {
                    return keyList;
                }

                return null;
            }

            static IList _GetKeysMethod(Type type, string valuesName)
            {
                var methodInfo = type.GetMethod(valuesName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (methodInfo == null)
                {
                    var allTypes = ReflectionUtility.GetSelfAndBaseTypes(type);
                    foreach (var t in allTypes)
                    {
                        methodInfo = t.GetMethod(valuesName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        if (methodInfo != null)
                        {
                            break;
                        }
                    }
                }

                if (methodInfo == null) return null;

                var keys = methodInfo.Invoke(null, null);
                if (keys is IList keyList)
                {
                    return keyList;
                }

                return null;
            }

            static IList _GetKeysField(Type type, string valuesName)
            {
                var fieldInfo = type.GetField(valuesName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (fieldInfo == null)
                {
                    var allTypes = ReflectionUtility.GetSelfAndBaseTypes(type);
                    foreach (var t in allTypes)
                    {
                        fieldInfo = t.GetField(valuesName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        if (fieldInfo != null)
                        {
                            break;
                        }
                    }
                }

                if (fieldInfo == null) return null;

                var keys = fieldInfo.GetValue(null);
                if (keys is IList keyList)
                {
                    return keyList;
                }

                return null;
            }
        }

        private static VisualElement DrawNodeVaporList(VaporInspectorNode node)
        {
            var list = new StyledNodeList(node)
            {
                name = node.Path,
            };
            return list;
        }

        private static void OnNodeDropdownChanged(ChangeEvent<string> evt)
        {
            if (evt.target is DropdownField dropdown && dropdown.parent.userData is VaporInspectorNode node && dropdown.userData is List<object> values)
            {
                var newVal = values[dropdown.index];
                node.Property.boxedValue = newVal;
                node.Property.serializedObject.ApplyModifiedProperties();
            }
        }
        
        private static void OnNodeSearchableDropdownChanged(VisualElement visualElement, string oldValue, string newValue)
        {
            if (visualElement is SearchableDropdown<string> dropdown)
            {
                var tuple = ((VaporInspectorNode, List<object>))dropdown.userData;
                var newVal = tuple.Item2[dropdown.Index];
                Debug.Log("Applied " + newVal);
                tuple.Item1.Property.boxedValue = newVal;
                tuple.Item1.Property.serializedObject.ApplyModifiedProperties();
            }
        }

        private static void OnNodeDynamicPropertyShow(VaporInspectorNode node, TextField field)
        {
            var clonedTarget = node.Target;
            var cleanupImmediate = false;
            if (node.Target.GetType().IsSubclassOf(typeof(Component)))
            {
                clonedTarget = Object.Instantiate((Component)node.Target);
                cleanupImmediate = true;
            }
            else
            {
                clonedTarget = Activator.CreateInstance(clonedTarget.GetType());
            }

            var val = node.PropertyInfo.GetValue(clonedTarget).ToString();
            if (node.FieldInfo != null)
            {
                val = node.FieldInfo.GetValue(clonedTarget).ToString();
            }

            field.SetValueWithoutNotify(val);
            if (!cleanupImmediate) return;

            var obj = (Component)clonedTarget;
            Object.DestroyImmediate(obj.gameObject);
        }
        #endregion
        
        #region Property Drawers

        public static VisualElement DrawVaporElementWithVerticalLayout(VaporDrawerInfo drawer, string drawerName)
        {
            var vertical = new StyledVerticalGroup(0, 0, true);
            var field = DrawVaporElement(drawer, drawerName);
            vertical.Add(field);
            return vertical;
        }

        public static VisualElement DrawVaporElement(VaporDrawerInfo drawer, string drawerName)
        {
            switch (drawer.InfoType)
            {
                case DrawerInfoType.Field:
                    if (HasCustomPropertyDrawer(drawer.FieldInfo.FieldType) && !drawer.HasAttribute<IgnoreCustomDrawerAttribute>())
                    {
                        var field = new PropertyField(drawer.Property)
                        {
                            name = drawerName,
                            userData = drawer,
                        };
                        return field;
                    }
                    
                    if (drawer.TryGetAttribute<ValueDropdownAttribute>(out var dropdownAtr))
                    {
                        return DrawVaporValueDropdown(drawer, drawerName, dropdownAtr);
                    }

                    if (drawer.Property.isArray && drawer.Property.propertyType != SerializedPropertyType.String && !drawer.HasAttribute<DrawWithUnityAttribute>())
                    {
                        return DrawVaporList(drawer, drawerName);
                    }

                    return DrawVaporField(drawer, drawerName);
                case DrawerInfoType.Property:
                    return DrawVaporProperty(drawer, drawerName);
                case DrawerInfoType.Method:
                    return DrawVaporMethod(drawer);
                default:
                    return null;
            }
        }

        private static VisualElement DrawVaporValueDropdown(VaporDrawerInfo drawer, string drawerName, ValueDropdownAttribute dropdownAtr)
        {
            List<string> keys = new();
            List<object> values = new();
            switch (dropdownAtr.ResolverType)
            {
                case ResolverType.None:
                    break;
                case ResolverType.Property:
                    if (dropdownAtr.AssemblyQualifiedType != null)
                    {
                        _ConvertToTupleList(keys, values, _GetKeysProperty(dropdownAtr.AssemblyQualifiedType, dropdownAtr.Resolver[1..]));
                    }
                    else
                    {
                        var pi = ReflectionUtility.GetProperty(drawer.Target, dropdownAtr.Resolver[1..]);
                        _ConvertToTupleList(keys, values, (IList)pi.GetValue(drawer.Target));
                    }

                    break;
                case ResolverType.Method:
                    if (dropdownAtr.AssemblyQualifiedType != null)
                    {
                        _ConvertToTupleList(keys, values, _GetKeysMethod(dropdownAtr.AssemblyQualifiedType, dropdownAtr.Resolver[1..]));
                    }
                    else
                    {
                        var mi = ReflectionUtility.GetMethod(drawer.Target, dropdownAtr.Resolver[1..]);
                        _ConvertToTupleList(keys, values, (IList)mi.Invoke(drawer.Target, null));
                    }

                    break;
                case ResolverType.Field:
                    if (dropdownAtr.AssemblyQualifiedType != null)
                    {
                        _ConvertToTupleList(keys, values, _GetKeysField(dropdownAtr.AssemblyQualifiedType, dropdownAtr.Resolver[1..]));
                    }
                    else
                    {
                        var fi = ReflectionUtility.GetField(drawer.Target, dropdownAtr.Resolver[1..]);
                        _ConvertToTupleList(keys, values, (IList)fi.GetValue(drawer.Target));
                    }

                    break;
            }
            
            var tooltip = "";
            if (drawer.TryGetAttribute<RichTextTooltipAttribute>(out var rtAtr))
            {
                tooltip = rtAtr.Tooltip;
            }

            if (dropdownAtr.Searchable)
            {
                var indexOfCurrent = values.IndexOf(drawer.Property.boxedValue);
                var currentNameValue = indexOfCurrent >= 0 ? keys[indexOfCurrent] : "null";
                var field = new SearchableDropdown<string>(drawer.Property.displayName, currentNameValue)
                {
                    name = drawerName,
                    userData = (drawer, values),
                    tooltip = tooltip,
                };
                field.AddToClassList("unity-base-field__aligned");
                field.SetChoices(keys);
                field.ValueChanged += OnSearchableDropdownChanged;
                return field;
            }
            else
            {
                var container = new VisualElement()
                {
                    name = drawerName,
                    userData = drawer
                };
                var index = values.IndexOf(drawer.Property.boxedValue);
                var field = new DropdownField(drawer.Property.displayName, keys, index)
                {
                    tooltip = tooltip,
                    userData = values
                };
                field.AddToClassList("unity-base-field__aligned");
                field.RegisterValueChangedCallback(OnDropdownChanged);
                container.Add(field);
                return container;
            }

            static void _ConvertToTupleList(List<string> keys, List<object> values, IList convert)
            {
                if (convert == null)
                {
                    return;
                }
                
                foreach (var obj in convert)
                {
                    var item1 = (string)obj.GetType().GetField("Item1", BindingFlags.Instance | BindingFlags.Public)
                        ?.GetValue(obj);
                    var item2 = obj.GetType().GetField("Item2", BindingFlags.Instance | BindingFlags.Public)
                        ?.GetValue(obj);
                    if (item1 == null || item2 == null)
                    {
                        continue;
                    }

                    keys.Add(item1);
                    values.Add(item2);
                }
            }

            static IList _GetKeysProperty(Type type, string valuesName)
            {
                var propertyInfo = type.GetProperty(valuesName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (propertyInfo == null)
                {
                    var allTypes = ReflectionUtility.GetSelfAndBaseTypes(type);
                    foreach (var t in allTypes)
                    {
                        propertyInfo = t.GetProperty(valuesName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        if (propertyInfo != null)
                        {
                            break;
                        }
                    }
                }

                if (propertyInfo == null) return null;

                var keys = propertyInfo.GetValue(null);
                if (keys is IList keyList)
                {
                    return keyList;
                }

                return null;
            }

            static IList _GetKeysMethod(Type type, string valuesName)
            {
                var methodInfo = type.GetMethod(valuesName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (methodInfo == null)
                {
                    var allTypes = ReflectionUtility.GetSelfAndBaseTypes(type);
                    foreach (var t in allTypes)
                    {
                        methodInfo = t.GetMethod(valuesName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        if (methodInfo != null)
                        {
                            break;
                        }
                    }
                }

                if (methodInfo == null) return null;

                var keys = methodInfo.Invoke(null, null);
                if (keys is IList keyList)
                {
                    return keyList;
                }

                return null;
            }

            static IList _GetKeysField(Type type, string valuesName)
            {
                var fieldInfo = type.GetField(valuesName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (fieldInfo == null)
                {
                    var allTypes = ReflectionUtility.GetSelfAndBaseTypes(type);
                    foreach (var t in allTypes)
                    {
                        fieldInfo = t.GetField(valuesName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        if (fieldInfo != null)
                        {
                            break;
                        }
                    }
                }

                if (fieldInfo == null) return null;

                var keys = fieldInfo.GetValue(null);
                if (keys is IList keyList)
                {
                    return keyList;
                }

                return null;
            }
        }

        private static VisualElement DrawVaporList(VaporDrawerInfo drawer, string drawerName)
        {
            var list = new StyledList(drawer)
            {
                name = drawerName,
                userData = drawer
            };
            return list;
        }

        private static VisualElement DrawVaporField(VaporDrawerInfo drawer, string drawerName)
        {
            var field = new PropertyField(drawer.Property)
            {
                name = drawerName,
                userData = drawer,
            };
            if (drawer.IsUnityObject)
            {
                field.AddManipulator(new ContextualMenuManipulator(evt =>
                {
                    evt.menu.AppendAction("Set To Null", ca =>
                    {
                        drawer.Property.boxedValue = null;
                        drawer.Property.serializedObject.ApplyModifiedProperties();
                    });
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Copy", _ => { ClipboardUtility.WriteToBuffer(drawer); });
                    evt.menu.AppendAction("Paste", _ => { ClipboardUtility.ReadFromBuffer(drawer); }, _ =>
                    {
                        var read = ClipboardUtility.CanReadFromBuffer(drawer);
                        return read ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                    }, drawer);
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Copy Property Path", _ => { EditorGUIUtility.systemCopyBuffer = drawer.FieldInfo.Name; });
                }));
            }
            else
            {
                field.AddManipulator(new ContextualMenuManipulator(evt =>
                {
                    evt.menu.AppendAction("Reset", ca =>
                    {
                        var clonedTarget = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(drawer.Target.GetType());
                        var val = drawer.FieldInfo.GetValue(clonedTarget);
                        drawer.Property.boxedValue = val;
                        drawer.Property.serializedObject.ApplyModifiedProperties();
                    });
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Copy", _ => { ClipboardUtility.WriteToBuffer(drawer); });
                    evt.menu.AppendAction("Paste", _ => { ClipboardUtility.ReadFromBuffer(drawer); }, _ =>
                    {
                        var read = ClipboardUtility.CanReadFromBuffer(drawer);
                        return read ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                    }, drawer);
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Copy Property Path", _ => { EditorGUIUtility.systemCopyBuffer = drawer.FieldInfo.Name; });
                }));
            }

            field.RegisterCallback<GeometryChangedEvent>(OnPropertyBuilt);
            return field;
        }

        private static VisualElement DrawVaporProperty(VaporDrawerInfo drawer, string drawerName)
        {
            var clonedTarget = drawer.Target;
            // var cleanupImmediate = false;
            clonedTarget = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(clonedTarget.GetType());
            // if (drawer.Target.GetType().IsSubclassOf(typeof(Component)))
            // {
            //     // clonedTarget = Object.Instantiate((Component)drawer.Target);
            //     // cleanupImmediate = true;
            // }
            // else
            // {
            //     clonedTarget = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(clonedTarget.GetType());
            //     // clonedTarget = Activator.CreateInstance(clonedTarget.GetType());
            // }
            var val = drawer.PropertyInfo.GetValue(clonedTarget).ToString();
            if (drawer.FieldInfo != null)
            {
                val = drawer.FieldInfo.GetValue(clonedTarget).ToString();
            }

            var tooltip = "";
            if (drawer.TryGetAttribute<RichTextTooltipAttribute>(out var rtAtr))
            {
                tooltip = rtAtr.Tooltip;
            }

            var prop = new TextField(drawer.Path[(drawer.Path.IndexOf("p_", StringComparison.Ordinal) + 2)..])
            {
                name = drawerName,
            };
            var label = prop.Q<Label>();
            label.tooltip = tooltip;
            label.AddToClassList("unity-base-field__label");
            prop.SetValueWithoutNotify(val);
            prop.SetEnabled(false);
            // if (cleanupImmediate)
            // {
            //     var obj = (Component)clonedTarget;
            //     Object.DestroyImmediate(obj.gameObject);
            // }
            if (drawer.TryGetAttribute<ShowInInspectorAttribute>(out var showAtr))
            {
                if (showAtr.Dynamic)
                {
                    prop.schedule.Execute(() => OnDynamicPropertyShow(drawer, prop)).Every(showAtr.DynamicInterval);
                }
            }

            return prop;
        }

        private static VisualElement DrawVaporMethod(VaporDrawerInfo drawer)
        {
            var atr = drawer.MethodInfo.GetCustomAttribute<ButtonAttribute>();
            var label = atr.Label;
            if (string.IsNullOrEmpty(label))
            {
                label = ObjectNames.NicifyVariableName(drawer.MethodInfo.Name);
            }

            var tooltip = "";
            if (drawer.TryGetAttribute<RichTextTooltipAttribute>(out var rtAtr))
            {
                tooltip = rtAtr.Tooltip;
            }

            var button = new StyledButton(atr.Size)
            {
                tooltip = tooltip,
                name = drawer.Path,
                text = label,
                userData = drawer
            };
            button.RegisterCallback<GeometryChangedEvent>(OnMethodBuilt);
            return button;
        }

        private static void OnDropdownChanged(ChangeEvent<string> evt)
        {
            if (evt.target is DropdownField dropdown && dropdown.parent.userData is VaporDrawerInfo drawer && dropdown.userData is List<object> values)
            {
                var newVal = values[dropdown.index];
                drawer.Property.boxedValue = newVal;
                drawer.Property.serializedObject.ApplyModifiedProperties();
            }
        }
        
        private static void OnSearchableDropdownChanged(VisualElement visualElement, string oldValue, string newValue)
        {
            if (visualElement is SearchableDropdown<string> dropdown)
            {
                var tuple = ((VaporDrawerInfo, List<object>))dropdown.userData;
                var newVal = tuple.Item2[dropdown.Index];
                Debug.Log("Applied " + newVal);
                tuple.Item1.Property.boxedValue = newVal;
                tuple.Item1.Property.serializedObject.ApplyModifiedProperties();
            }
        }

        private static void OnDynamicPropertyShow(VaporDrawerInfo drawer, TextField field)
        {
            var clonedTarget = drawer.Target;
            var cleanupImmediate = false;
            if (drawer.Target.GetType().IsSubclassOf(typeof(Component)))
            {
                clonedTarget = Object.Instantiate((Component)drawer.Target);
                cleanupImmediate = true;
            }
            else
            {
                clonedTarget = Activator.CreateInstance(clonedTarget.GetType());
            }

            var val = drawer.PropertyInfo.GetValue(clonedTarget).ToString();
            if (drawer.FieldInfo != null)
            {
                val = drawer.FieldInfo.GetValue(clonedTarget).ToString();
            }

            field.SetValueWithoutNotify(val);
            if (!cleanupImmediate) return;

            var obj = (Component)clonedTarget;
            Object.DestroyImmediate(obj.gameObject);
        }

        #endregion

        private static void OnNodePropertyBuilt(GeometryChangedEvent evt)
        {
            var field = (PropertyField)evt.target;
            if (field is not { childCount: > 0 }) return;

            field.UnregisterCallback<GeometryChangedEvent>(OnNodePropertyBuilt);
            OnNodePropertyBuilt(field);
        }
        
        private static void OnNodeMethodBuilt(GeometryChangedEvent evt)
        {
            var button = (StyledButton)evt.target;
            if (button == null) return;

            button.UnregisterCallback<GeometryChangedEvent>(OnMethodBuilt);
            OnNodeMethodBuilt(button);
        }

        public static void OnNodePropertyBuilt(PropertyField field)
        {
            var list = field.Q<ListView>();
            if (list != null)
            {
                list.Q<Toggle>().style.marginLeft = 3;
            }

            if (field.userData is not VaporInspectorNode node)
            {
                return;
            }

            var prop = node.Property;
            if (prop.propertyType == SerializedPropertyType.Generic && !node.IsDrawnWithVapor)
            {
                if (node.HasAttribute<InlineEditorAttribute>())
                {
                    field.Q<Toggle>().RemoveFromHierarchy();
                    var inlineContent = field.Q<VisualElement>("unity-content");
                    inlineContent.style.display = DisplayStyle.Flex;
                    inlineContent.style.marginLeft = 0;
                }
                else
                {
                    field.Q<Toggle>().style.marginLeft = 0;
                }
            }

            if (prop.propertyType == SerializedPropertyType.Boolean)
            {
                field.Q<Toggle>().pickingMode = PickingMode.Ignore;
                field.Q<Label>().RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
            }

            DrawDecorators(field, node);
            DrawLabel(field, node);
            DrawLabelWidth(field, node);
            DrawHideLabel(field, node);
            DrawRichTooltip(field, node);
            DrawConditionals(field, node);
            DrawReadOnly(field, node);
            DrawAutoReference(field, node);
            DrawTitle(node);
            DrawPathSelection(field, node);
            DrawInlineButtons(field, node);
            DrawSuffix(field, node);

            // Validation
            DrawRequireInterface(field, prop, node);
            DrawValidation(field, node);
        }

        private static void OnNodeGroupBuilt(GeometryChangedEvent evt)
        {
            var element = (VisualElement)evt.target;
            element.UnregisterCallback<GeometryChangedEvent>(OnNodeGroupBuilt);
            OnNodeGroupBuilt(element);
        }

        private static void OnNodeGroupBuilt(VisualElement element)
        {
            if (element.userData is not VaporInspectorNode node)
            {
                return;
            }
            List<Action> resolvers = new();
            
            DrawDecorators(element, node);
            DrawConditionals(element, node);
            
            if (resolvers.Count > 0)
            {
                element.schedule.Execute(() => Resolve(resolvers)).Every(VaporInspectorsSettingsProvider.VaporInspectorResolverUpdateRate);
            }
        }
        
        public static void OnNodeListPropertyBuilt(PropertyField field, SerializedProperty prop, VaporInspectorNode node)
        {
            var list = field.Q<ListView>();
            if (list != null)
            {
                list.Q<Toggle>().style.marginLeft = 3;
            }

            List<Action> resolvers = new();

            if (node is null)
            {
                return;
            }

            if (prop.propertyType == SerializedPropertyType.Generic && !node.IsDrawnWithVapor)
            {
                if (node.HasAttribute<InlineEditorAttribute>())
                {
                    field.Q<Toggle>().RemoveFromHierarchy();
                    var inlineContent = field.Q<VisualElement>("unity-content");
                    inlineContent.style.display = DisplayStyle.Flex;
                    inlineContent.style.marginLeft = 0;
                }
                else
                {
                    field.Q<Toggle>().style.marginLeft = 0;
                }
            }
            
            if (prop.propertyType == SerializedPropertyType.Boolean)
            {
                field.Q<Toggle>().pickingMode = PickingMode.Ignore;
                field.Q<Label>().RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
            }

            DrawDecorators(field, node);
            DrawLabel(field, node);
            DrawLabelWidth(field, node);
            DrawHideLabel(field, node);
            DrawRichTooltip(field, node);
            DrawConditionals(field, node);
            DrawReadOnly(field, node);
            DrawAutoReference(field, node);
            DrawTitle(node);
            DrawPathSelection(field, node);
            DrawInlineButtons(field, node);
            DrawSuffix(field, node);

            // Validation
            DrawRequireInterface(field, prop, node);
            DrawValidation(field, node);

            if (resolvers.Count > 0)
            {
                field.schedule.Execute(() => Resolve(resolvers)).Every(VaporInspectorsSettingsProvider.VaporInspectorResolverUpdateRate);
            }
        }
        
        public static void OnNodeMethodBuilt(StyledButton button)
        {
            if (button.userData is not VaporInspectorNode node)
            {
                return;
            }

            button.clickable = new Clickable(() => node.InvokeMethod());
        }
        
        private static void OnPropertyBuilt(GeometryChangedEvent evt)
        {
            var field = (PropertyField)evt.target;
            if (field is not { childCount: > 0 }) return;

            field.UnregisterCallback<GeometryChangedEvent>(OnPropertyBuilt);
            OnPropertyBuilt(field);
        }

        private static void OnMethodBuilt(GeometryChangedEvent evt)
        {
            var button = (StyledButton)evt.target;
            if (button == null) return;

            button.UnregisterCallback<GeometryChangedEvent>(OnMethodBuilt);
            OnMethodBuilt(button);
        }

        public static void OnPropertyBuilt(PropertyField field)
        {
            var list = field.Q<ListView>();
            if (list != null)
            {
                list.Q<Toggle>().style.marginLeft = 3;
            }

            List<Action> resolvers = new();

            if (field.userData is not VaporDrawerInfo drawer)
            {
                return;
            }

            var prop = drawer.Property;
            if (prop.propertyType == SerializedPropertyType.Generic && !drawer.IsDrawnWithVapor)
            {
                if (drawer.HasAttribute<InlineEditorAttribute>())
                {
                    field.Q<Toggle>().RemoveFromHierarchy();
                    var inlineContent = field.Q<VisualElement>("unity-content");
                    inlineContent.style.display = DisplayStyle.Flex;
                    inlineContent.style.marginLeft = 0;
                }
                else
                {
                    field.Q<Toggle>().style.marginLeft = 0;
                }
            }

            if (prop.propertyType == SerializedPropertyType.Boolean)
            {
                field.Q<Toggle>().pickingMode = PickingMode.Ignore;
                field.Q<Label>().RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
            }

            DrawDecorators(field, drawer);
            DrawLabel(field, drawer, resolvers);
            DrawLabelWidth(field, drawer);
            DrawHideLabel(field, drawer);
            DrawRichTooltip(field, drawer);
            DrawConditionals(field, drawer, resolvers);
            DrawReadOnly(field, drawer);
            DrawAutoReference(field, drawer);
            DrawTitle(field, drawer);
            DrawPathSelection(field, drawer);
            DrawInlineButtons(field, drawer, resolvers);
            DrawSuffix(field, drawer);

            // Validation
            DrawRequireInterface(field, prop, drawer);
            DrawValidation(field, drawer, resolvers);

            if (resolvers.Count > 0)
            {
                field.schedule.Execute(() => Resolve(resolvers)).Every(1000);
            }
        }

        public static void OnListPropertyBuilt(PropertyField field, SerializedProperty prop, VaporDrawerInfo listDrawer)
        {
            var list = field.Q<ListView>();
            if (list != null)
            {
                list.Q<Toggle>().style.marginLeft = 3;
            }

            List<Action> resolvers = new();

            if (listDrawer is null)
            {
                return;
            }

            if (prop.propertyType == SerializedPropertyType.Generic && !listDrawer.IsDrawnWithVapor)
            {
                if (listDrawer.HasAttribute<InlineEditorAttribute>())
                {
                    field.Q<Toggle>().RemoveFromHierarchy();
                    var inlineContent = field.Q<VisualElement>("unity-content");
                    inlineContent.style.display = DisplayStyle.Flex;
                    inlineContent.style.marginLeft = 0;
                }
                else
                {
                    field.Q<Toggle>().style.marginLeft = 0;
                }
            }
            
            if (prop.propertyType == SerializedPropertyType.Boolean)
            {
                field.Q<Toggle>().pickingMode = PickingMode.Ignore;
                field.Q<Label>().RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
            }

            DrawDecorators(field, listDrawer);
            DrawLabel(field, listDrawer, resolvers);
            DrawLabelWidth(field, listDrawer);
            DrawHideLabel(field, listDrawer);
            DrawRichTooltip(field, listDrawer);
            DrawConditionals(field, listDrawer, resolvers);
            DrawReadOnly(field, listDrawer);
            DrawAutoReference(field, listDrawer);
            DrawTitle(field, listDrawer);
            DrawPathSelection(field, listDrawer);
            DrawInlineButtons(field, listDrawer, resolvers);
            DrawSuffix(field, listDrawer);

            // Validation
            DrawRequireInterface(field, prop, listDrawer);
            DrawValidation(field, listDrawer, resolvers);

            if (resolvers.Count > 0)
            {
                field.schedule.Execute(() => Resolve(resolvers)).Every(1000);
            }
        }

        public static void OnMethodBuilt(StyledButton button)
        {
            if (button.userData is not VaporDrawerInfo drawer)
            {
                return;
            }

            button.clickable = new Clickable(() => drawer.InvokeMethod());
        }

        private static void Resolve(List<Action> resolvers)
        {
            foreach (var item in resolvers)
            {
                item.Invoke();
            }
        }

        #region Attribute Drawers
        public static void DrawLabel(PropertyField field, VaporDrawerInfo drawer, List<Action> resolvers)
        {
            if (drawer.TryGetAttribute<LabelAttribute>(out var atr))
            {
                var label = field.Q<Label>();
                switch (atr.LabelResolverType)
                {
                    case ResolverType.None:
                        label.text = atr.Label;
                        break;
                    case ResolverType.Property:
                        PropertyInfo pi = ReflectionUtility.GetProperty(drawer.Target, atr.LabelResolver[1..]);
                        label.text = (string)pi.GetValue(drawer.Target);
                        resolvers.Add(() => label.text = (string)pi.GetValue(drawer.Target));
                        break;
                    case ResolverType.Method:
                        MethodInfo mi = ReflectionUtility.GetMethod(drawer.Target, atr.LabelResolver[1..]);
                        label.text = (string)mi.Invoke(drawer.Target, null);
                        resolvers.Add(() => label.text = (string)mi.Invoke(drawer.Target, null));
                        break;
                }

                switch (atr.LabelColorResolverType)
                {
                    case ResolverType.None:
                        label.style.color = atr.LabelColor;
                        break;
                    case ResolverType.Property:
                        PropertyInfo pi = ReflectionUtility.GetProperty(drawer.Target, atr.LabelColorResolver[1..]);
                        label.style.color = (Color)pi.GetValue(drawer.Target);
                        resolvers.Add(() => label.style.color = (Color)pi.GetValue(drawer.Target));
                        break;
                    case ResolverType.Method:
                        MethodInfo mi = ReflectionUtility.GetMethod(drawer.Target, atr.LabelColorResolver[1..]);
                        label.style.color = (Color)mi.Invoke(drawer.Target, null);
                        resolvers.Add(() => label.style.color = (Color)mi.Invoke(drawer.Target, null));
                        break;
                }

                if (atr.HasIcon)
                {
                    var image = new Image
                    {
                        image = EditorGUIUtility.IconContent(atr.Icon).image,
                        scaleMode = ScaleMode.ScaleToFit,
                        pickingMode = PickingMode.Ignore
                    };
                    image.style.alignSelf = Align.FlexEnd;
                    switch (atr.IconColorResolverType)
                    {
                        case ResolverType.None:
                            image.tintColor = atr.IconColor.value;
                            break;
                        case ResolverType.Property:
                            PropertyInfo pi = ReflectionUtility.GetProperty(drawer.Target, atr.IconColorResolver[1..]);
                            image.tintColor = (Color)pi.GetValue(drawer.Target);
                            resolvers.Add(() => image.tintColor = (Color)pi.GetValue(drawer.Target));
                            break;
                        case ResolverType.Method:
                            MethodInfo mi = ReflectionUtility.GetMethod(drawer.Target, atr.IconColorResolver[1..]);
                            image.tintColor = (Color)mi.Invoke(drawer.Target, null);
                            resolvers.Add(() => image.tintColor = (Color)mi.Invoke(drawer.Target, null));
                            break;
                    }

                    label.Add(image);
                }
            }
        }

        public static void DrawLabelWidth(PropertyField field, VaporDrawerInfo drawer)
        {
            if (!drawer.TryGetAttribute<LabelWidthAttribute>(out var atr)) return;
            
            field[0].RemoveFromClassList("unity-base-field__aligned");
            var label = field.Q<Label>();
            if (atr.UseAutoWidth)
            {
                label.style.minWidth = new StyleLength(StyleKeyword.Auto);
                label.style.width = new StyleLength(StyleKeyword.Auto);
            }
            else
            {
                var minWidth = Mathf.Min(label.resolvedStyle.minWidth.value, atr.Width);
                label.style.minWidth = minWidth;
                label.style.width = atr.Width;
            }
        }

        public static void DrawHideLabel(PropertyField field, VaporDrawerInfo drawer)
        {
            if (drawer.HasAttribute<HideLabelAttribute>())
            {
                var label = field.Q<Label>();
                label.style.display = DisplayStyle.None;
            }
        }

        public static void DrawRichTooltip(PropertyField field, VaporDrawerInfo drawer)
        {
            if (!drawer.TryGetAttribute<RichTextTooltipAttribute>(out var rtAtr)) return;

            var label = field.Q<Label>();
            label.tooltip = rtAtr.Tooltip;
        }

        public static void DrawDecorators(PropertyField field, VaporDrawerInfo drawer)
        {
            if (drawer.TryGetAttribute<BackgroundColorAttribute>(out var backgroundColor))
            {
                field.style.backgroundColor = backgroundColor.BackgroundColor;
            }

            if (drawer.TryGetAttribute<MarginsAttribute>(out var margins))
            {
                if (margins.Bottom != null)
                {
                    field.style.marginBottom = margins.Bottom.Value;
                }

                if (margins.Top != null)
                {
                    field.style.marginTop = margins.Top.Value;
                }

                if (margins.Left != null)
                {
                    field.style.marginLeft = margins.Left.Value;
                }

                if (margins.Right != null)
                {
                    field.style.marginRight = margins.Right.Value;
                }
            }

            if (drawer.TryGetAttribute<PaddingAttribute>(out var padding))
            {
                if (padding.Bottom != null)
                {
                    field.style.paddingBottom = padding.Bottom.Value;
                }

                if (padding.Top != null)
                {
                    field.style.paddingTop = padding.Top.Value;
                }

                if (padding.Left != null)
                {
                    field.style.paddingLeft = padding.Left.Value;
                }

                if (padding.Right != null)
                {
                    field.style.paddingRight = padding.Right.Value;
                }
            }

            if (drawer.TryGetAttribute<BordersAttribute>(out var borders))
            {
                field.style.borderBottomWidth = borders.Bottom;
                field.style.borderBottomColor = borders.Color;

                field.style.borderTopWidth = borders.Top;
                field.style.borderTopColor = borders.Color;

                field.style.borderLeftWidth = borders.Left;
                field.style.borderLeftColor = borders.Color;

                field.style.borderRightWidth = borders.Right;
                field.style.borderRightColor = borders.Color;

                if (borders.Rounded)
                {
                    field.style.borderBottomLeftRadius = 3;
                    field.style.borderBottomRightRadius = 3;
                    field.style.borderTopLeftRadius = 3;
                    field.style.borderTopRightRadius = 3;
                }
            }
        }
        
        public static void DrawConditionals(PropertyField field, VaporDrawerInfo drawer, List<Action> resolvers)
        {
            if (drawer.TryGetAttribute<ShowIfAttribute>(out var showIf))
            {
                switch (showIf.ResolverType)
                {
                    case ResolverType.None:
                        break;
                    case ResolverType.Property:
                        var pi = ReflectionUtility.GetProperty(drawer.Target, showIf.Resolver[1..]);
                        field.style.display = (bool)pi.GetValue(drawer.Target) ? DisplayStyle.Flex : DisplayStyle.None;
                        resolvers.Add(() => field.style.display = (bool)pi.GetValue(drawer.Target) ? DisplayStyle.Flex : DisplayStyle.None);
                        break;
                    case ResolverType.Method:
                        var mi = ReflectionUtility.GetMethod(drawer.Target, showIf.Resolver[1..]);
                        field.style.display = (bool)mi.Invoke(drawer.Target, null) ? DisplayStyle.Flex : DisplayStyle.None;
                        resolvers.Add(() => field.style.display = (bool)mi.Invoke(drawer.Target, null) ? DisplayStyle.Flex : DisplayStyle.None);
                        break;
                    case ResolverType.Field:
                        //var test = ReflectionUtility.GetField(drawer.Target, showIf.Resolver[1..]);
                        //var resolverContainer = new ResolverContainerStruct<bool>(drawer.Target, test, b => field.style.display = b ? DisplayStyle.Flex : DisplayStyle.None);
                        //resolvers.Add(() => resolverContainer.Resolve());
                        
                        // var fi = ReflectionUtility.GetField(drawer.Target, showIf.Resolver[1..]);
                        // field.style.display = (bool)fi.GetValue(drawer.Target) ? DisplayStyle.Flex : DisplayStyle.None;
                        // resolvers.Add(() => field.style.display = (bool)fi.GetValue(drawer.Target) ? DisplayStyle.Flex : DisplayStyle.None);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (drawer.TryGetAttribute<HideIfAttribute>(out var hideIf))
            {
                switch (hideIf.ResolverType)
                {
                    case ResolverType.None:
                        break;
                    case ResolverType.Property:
                        var pi = ReflectionUtility.GetProperty(drawer.Target, hideIf.Resolver[1..]);
                        field.style.display = (bool)pi.GetValue(drawer.Target) ? DisplayStyle.None : DisplayStyle.Flex;
                        resolvers.Add(() => field.style.display = (bool)pi.GetValue(drawer.Target) ? DisplayStyle.None : DisplayStyle.Flex);
                        break;
                    case ResolverType.Method:
                        var mi = ReflectionUtility.GetMethod(drawer.Target, hideIf.Resolver[1..]);
                        field.style.display = (bool)mi.Invoke(drawer.Target, null) ? DisplayStyle.None : DisplayStyle.Flex;
                        resolvers.Add(() => field.style.display = (bool)mi.Invoke(drawer.Target, null) ? DisplayStyle.None : DisplayStyle.Flex);
                        break;
                    case ResolverType.Field:
                        var fi = ReflectionUtility.GetField(drawer.Target, hideIf.Resolver[1..]);
                        field.style.display = (bool)fi.GetValue(drawer.Target) ? DisplayStyle.None : DisplayStyle.Flex;
                        resolvers.Add(() => field.style.display = (bool)fi.GetValue(drawer.Target) ? DisplayStyle.None : DisplayStyle.Flex);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (drawer.TryGetAttribute<DisableIfAttribute>(out var disableIf))
            {
                switch (disableIf.ResolverType)
                {
                    case ResolverType.None:
                        break;
                    case ResolverType.Property:
                        var pi = ReflectionUtility.GetProperty(drawer.Target, disableIf.Resolver[1..]);
                        field.SetEnabled(!(bool)pi.GetValue(drawer.Target));
                        resolvers.Add(() => field.SetEnabled(!(bool)pi.GetValue(drawer.Target)));
                        break;
                    case ResolverType.Method:
                        var mi = ReflectionUtility.GetMethod(drawer.Target, disableIf.Resolver[1..]);
                        field.SetEnabled(!(bool)mi.Invoke(drawer.Target, null));
                        resolvers.Add(() => field.SetEnabled(!(bool)mi.Invoke(drawer.Target, null)));
                        break;
                    case ResolverType.Field:
                        var fi = ReflectionUtility.GetProperty(drawer.Target, disableIf.Resolver[1..]);
                        field.SetEnabled(!(bool)fi.GetValue(drawer.Target));
                        resolvers.Add(() => field.SetEnabled(!(bool)fi.GetValue(drawer.Target)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (drawer.TryGetAttribute<EnableIfAttribute>(out var enableIf))
            {
                switch (enableIf.ResolverType)
                {
                    case ResolverType.None:
                        break;
                    case ResolverType.Property:
                        var pi = ReflectionUtility.GetProperty(drawer.Target, enableIf.Resolver[1..]);
                        field.SetEnabled((bool)pi.GetValue(drawer.Target));
                        resolvers.Add(() => field.SetEnabled((bool)pi.GetValue(drawer.Target)));
                        break;
                    case ResolverType.Method:
                        var mi = ReflectionUtility.GetMethod(drawer.Target, enableIf.Resolver[1..]);
                        field.SetEnabled((bool)mi.Invoke(drawer.Target, null));
                        resolvers.Add(() => field.SetEnabled((bool)mi.Invoke(drawer.Target, null)));
                        break;
                    case ResolverType.Field:
                        var fi = ReflectionUtility.GetField(drawer.Target, enableIf.Resolver[1..]);
                        field.SetEnabled((bool)fi.GetValue(drawer.Target));
                        resolvers.Add(() => field.SetEnabled((bool)fi.GetValue(drawer.Target)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (drawer.HasAttribute<HideInEditorModeAttribute>())
            {
                field.style.display = EditorApplication.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
                resolvers.Add(() => field.style.display = EditorApplication.isPlaying ? DisplayStyle.Flex : DisplayStyle.None);
            }

            if (drawer.HasAttribute<HideInPlayModeAttribute>())
            {
                field.style.display = EditorApplication.isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
                resolvers.Add(() => field.style.display = EditorApplication.isPlaying ? DisplayStyle.None : DisplayStyle.Flex);
            }

            if (drawer.HasAttribute<DisableInEditorModeAttribute>())
            {
                field.SetEnabled(EditorApplication.isPlaying);
                resolvers.Add(() => field.SetEnabled(EditorApplication.isPlaying));
            }

            if (drawer.HasAttribute<DisableInPlayModeAttribute>())
            {
                field.SetEnabled(!EditorApplication.isPlaying);
                resolvers.Add(() => field.SetEnabled(!EditorApplication.isPlaying));
            }
        }

        public static void DrawPathSelection(PropertyField field, VaporDrawerInfo drawer)
        {
            if (drawer.TryGetAttribute<FilePathAttribute>(out var fileAtr) && field[0] is TextField filePathTextField)
            {
                var inlineButton = new Button(() => filePathTextField.value = _FormatFilePath(fileAtr.AbsolutePath, fileAtr.FileExtension))
                {
                    text = "",
                };
                var image = new Image
                {
                    image = EditorGUIUtility.IconContent("d_FolderOpened Icon").image,
                    scaleMode = ScaleMode.ScaleToFit
                };
                filePathTextField.style.width = 0;
                inlineButton.style.paddingLeft = 3;
                inlineButton.style.paddingRight = 3;
                inlineButton.style.backgroundColor = new Color(0, 0, 0, 0);
                image.style.width = 16;
                image.style.height = 16;
                inlineButton.Add(image);
                field.Add(inlineButton);
                field.style.flexDirection = FlexDirection.Row;
                field[0].style.flexGrow = 1f;
            }

            if (drawer.TryGetAttribute<FolderPathAttribute>(out var folderAtr) && field[0] is TextField folderPathTextField)
            {
                var inlineButton = new Button(() => folderPathTextField.value = _FormatFolderPath(folderAtr.AbsolutePath))
                {
                    text = "",
                };
                var image = new Image
                {
                    image = EditorGUIUtility.IconContent("d_FolderOpened Icon").image,
                    scaleMode = ScaleMode.ScaleToFit
                };
                folderPathTextField.style.width = 0;
                inlineButton.style.paddingLeft = 3;
                inlineButton.style.paddingRight = 3;
                inlineButton.style.backgroundColor = new Color(0, 0, 0, 0);
                image.style.width = 16;
                image.style.height = 16;
                inlineButton.Add(image);
                field.Add(inlineButton);
                field.style.flexDirection = FlexDirection.Row;
                field[0].style.flexGrow = 1f;
            }

            string _FormatFilePath(bool absolutePath, string fileExtension)
            {
                if (!absolutePath)
                {
                    var path = EditorUtility.OpenFilePanel("File Path", "Assets", fileExtension);
                    var start = path.IndexOf("Assets", StringComparison.Ordinal);
                    return path[start..];
                }
                else
                {
                    return EditorUtility.OpenFilePanel("File Path", "Assets", fileExtension);
                }
            }

            string _FormatFolderPath(bool absolutePath)
            {
                if (!absolutePath)
                {
                    var path = EditorUtility.OpenFolderPanel("Folder Path", "Assets", "");
                    var start = path.IndexOf("Assets", StringComparison.Ordinal);
                    return path[start..];
                }
                else
                {
                    return EditorUtility.OpenFolderPanel("Folder Path", "Assets", "");
                }
            }
        }

        public static void DrawReadOnly(PropertyField field, VaporDrawerInfo drawer)
        {
            if (drawer.HasAttribute<ReadOnlyAttribute>())
            {
                field.SetEnabled(false);
            }
        }

        public static void DrawTitle(PropertyField field, VaporDrawerInfo drawer)
        {
            if (drawer.TryGetAttribute<TitleAttribute>(out var atr))
            {
                string labelText = $"<b>{atr.Title}</b>";
                if (atr.Subtitle != string.Empty)
                {
                    labelText = $"<b>{atr.Title}</b>\n<color=#9E9E9E><i><size=10>{atr.Subtitle}</size></i></color>";
                }

                var title = new Label(labelText);
                title.style.borderBottomWidth = atr.Underline ? 1 : 0;
                title.style.paddingBottom = 2;
                title.style.borderBottomColor = ContainerStyles.TextDefault;
                title.style.marginBottom = 1f;
                int index = field.parent.IndexOf(field);
                field.parent.Insert(index, title);
            }
        }

        public static void DrawInlineButtons(PropertyField field, VaporDrawerInfo drawer, List<Action> resolvers)
        {
            if (drawer.TryGetAttributes<InlineButtonAttribute>(out var atrs))
            {
                foreach (var atr in atrs)
                {
                    var methodInfo = ReflectionUtility.GetMethod(drawer.Target, atr.MethodName);
                    if (methodInfo != null)
                    {
                        var inlineButton = new Button(() => methodInfo.Invoke(drawer.Target, null))
                        {
                            text = atr.Label,
                        };
                        inlineButton.style.paddingLeft = 3;
                        inlineButton.style.paddingRight = 3;
                        if (atr.Icon != string.Empty)
                        {
                            var image = new Image
                            {
                                image = EditorGUIUtility.IconContent(atr.Icon).image,
                                scaleMode = ScaleMode.ScaleToFit,
                                tintColor = atr.Tint
                            };
                            if (atr.TintResolverType != ResolverType.None)
                            {
                                switch (atr.TintResolverType)
                                {
                                    case ResolverType.None:
                                        break;
                                    case ResolverType.Property:
                                        var pi = ReflectionUtility.GetProperty(drawer.Target, atr.TintResolver[1..]);
                                        image.tintColor = (Color)pi.GetValue(drawer.Target);
                                        resolvers.Add(() => image.tintColor = (Color)pi.GetValue(drawer.Target));
                                        break;
                                    case ResolverType.Method:
                                        var mi = ReflectionUtility.GetMethod(drawer.Target, atr.TintResolver[1..]);
                                        image.tintColor = (Color)mi.Invoke(drawer.Target, null);
                                        resolvers.Add(() => image.tintColor = (Color)mi.Invoke(drawer.Target, null));
                                        break;
                                }
                            }

                            inlineButton.Add(image);
                        }

                        field.Add(inlineButton);
                        field.style.flexDirection = FlexDirection.Row;
                        field[0].style.flexGrow = 1f;
                    }
                }
            }
        }

        public static void DrawSuffix(PropertyField field, VaporDrawerInfo drawer)
        {
            if (drawer.TryGetAttribute<SuffixAttribute>(out var atr))
            {
                var suffix = new Label(atr.Suffix);
                suffix.style.color = new Color(0.5f, 0.5f, 0.5f, 1);
                suffix.style.alignSelf = Align.Center;
                suffix.style.marginLeft = 3;
                suffix.style.paddingLeft = 3;
                field.Add(suffix);
                field.style.flexDirection = FlexDirection.Row;
                field[0].style.flexGrow = 1f;
            }
        }

        public static void DrawAutoReference(PropertyField field, VaporDrawerInfo drawer)
        {
            if (drawer.TryGetAttribute<AutoReferenceAttribute>(out var atr)
                && drawer.Property.propertyType == SerializedPropertyType.ObjectReference
                && !drawer.Property.objectReferenceValue
                && drawer.Property.serializedObject.targetObject is Component component)
            {
                var comp = component.GetComponent(drawer.FieldInfo.FieldType);
                if (!comp && atr.SearchChildren)
                {
                    comp = component.GetComponentInChildren(drawer.FieldInfo.FieldType, true);
                }

                if (!comp && atr.SearchParents)
                {
                    comp = component.GetComponentInParent(drawer.FieldInfo.FieldType, true);
                }

                drawer.Property.objectReferenceValue = comp;
                drawer.Property.serializedObject.ApplyModifiedProperties();
            }
        }

        public static void DrawValidation(PropertyField field, VaporDrawerInfo drawer, List<Action> resolvers)
        {
            if (drawer.TryGetAttribute<OnValueChangedAttribute>(out var ovcatr))
            {
                var methodInfo = ReflectionUtility.GetMethod(drawer.Target, ovcatr.MethodName);
                if (methodInfo != null)
                {
                    field.RegisterValueChangeCallback(x => methodInfo.Invoke(drawer.Target, null));
                }
            }

            if (drawer.TryGetAttribute<ValidateInputAttribute>(out var viatr))
            {
                var label = field.Q<Label>();
                var image = new Image
                {
                    name = "image-error",
                    image = EditorGUIUtility.IconContent("Error").image,
                    scaleMode = ScaleMode.ScaleToFit,
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        alignSelf = Align.FlexEnd
                    }
                    //tintColor = ContainerStyles.ErrorText.value,
                };
                label.Add(image);

                var methodInfo = ReflectionUtility.GetMethod(drawer.Target, viatr.MethodName);
                if (methodInfo != null)
                {
                    var validated = _OnValidateInput(drawer.Property, methodInfo, drawer.Target);
                    image.style.display = validated ? DisplayStyle.None : DisplayStyle.Flex;
                    field.RegisterValueChangeCallback(x => _ValidateInput(x, methodInfo, drawer.Target));
                }
            }

            static void _ValidateInput(SerializedPropertyChangeEvent evt, MethodInfo mi, object target)
            {
                var validated = _OnValidateInput(evt.changedProperty, mi, target);
                var field = evt.target as PropertyField;
                var image = field.Q<Image>("image-error");
                image.style.display = validated ? DisplayStyle.None : DisplayStyle.Flex;
            }

            static bool _OnValidateInput(SerializedProperty sp, MethodInfo mi, object target)
            {
                return sp.propertyType switch
                {
                    SerializedPropertyType.Generic => (bool)mi.Invoke(target, new[] { sp.boxedValue }),
                    SerializedPropertyType.Integer => (bool)mi.Invoke(target, new object[] { sp.intValue }),
                    SerializedPropertyType.Boolean => (bool)mi.Invoke(target, new object[] { sp.boolValue }),
                    SerializedPropertyType.Float => (bool)mi.Invoke(target, new object[] { sp.floatValue }),
                    SerializedPropertyType.String => (bool)mi.Invoke(target, new object[] { sp.stringValue }),
                    SerializedPropertyType.Color => (bool)mi.Invoke(target, new object[] { sp.colorValue }),
                    SerializedPropertyType.ObjectReference => (bool)mi.Invoke(target, new object[] { sp.objectReferenceValue }),
                    SerializedPropertyType.LayerMask => (bool)mi.Invoke(target, new object[] { sp.intValue }),
                    SerializedPropertyType.Enum => (bool)mi.Invoke(target, new object[] { sp.enumValueIndex }),
                    SerializedPropertyType.Vector2 => (bool)mi.Invoke(target, new object[] { sp.vector2Value }),
                    SerializedPropertyType.Vector3 => (bool)mi.Invoke(target, new object[] { sp.vector3Value }),
                    SerializedPropertyType.Vector4 => (bool)mi.Invoke(target, new object[] { sp.vector4Value }),
                    SerializedPropertyType.Rect => (bool)mi.Invoke(target, new object[] { sp.rectValue }),
                    SerializedPropertyType.ArraySize => (bool)mi.Invoke(target, new object[] { sp.arraySize }),
                    SerializedPropertyType.Character => (bool)mi.Invoke(target, new object[] { sp.stringValue }),
                    SerializedPropertyType.AnimationCurve => (bool)mi.Invoke(target, new object[] { sp.animationCurveValue }),
                    SerializedPropertyType.Bounds => (bool)mi.Invoke(target, new object[] { sp.boundsValue }),
                    SerializedPropertyType.Gradient => (bool)mi.Invoke(target, new object[] { sp.gradientValue }),
                    SerializedPropertyType.Quaternion => (bool)mi.Invoke(target, new object[] { sp.quaternionValue }),
                    SerializedPropertyType.ExposedReference => (bool)mi.Invoke(target, new object[] { sp.exposedReferenceValue }),
                    SerializedPropertyType.FixedBufferSize => (bool)mi.Invoke(target, new object[] { sp.fixedBufferSize }),
                    SerializedPropertyType.Vector2Int => (bool)mi.Invoke(target, new object[] { sp.vector2IntValue }),
                    SerializedPropertyType.Vector3Int => (bool)mi.Invoke(target, new object[] { sp.vector3IntValue }),
                    SerializedPropertyType.RectInt => (bool)mi.Invoke(target, new object[] { sp.rectIntValue }),
                    SerializedPropertyType.BoundsInt => (bool)mi.Invoke(target, new object[] { sp.boundsIntValue }),
                    SerializedPropertyType.ManagedReference => (bool)mi.Invoke(target, new[] { sp.managedReferenceValue }),
                    SerializedPropertyType.Hash128 => (bool)mi.Invoke(target, new object[] { sp.hash128Value }),
                    _ => false,
                };
            }
        }

        public static void DrawRequireInterface(PropertyField field, SerializedProperty prop, VaporDrawerInfo drawer)
        {
            if (!drawer.TryGetAttribute<RequireInterfaceAttribute>(out var reqIntAtr)) return;
            
            var objDrawer = field.Q<ObjectField>();
            var guiContent = EditorGUIUtility.ObjectContent(objDrawer.value, reqIntAtr.InterfaceType);
            objDrawer.hierarchy[1].Q<Image>().image = guiContent.image;
            objDrawer.hierarchy[1].Q<Label>().text = guiContent.text;
            field.RegisterValueChangeCallback(x => _ValidateInput(x, reqIntAtr));

            var picker = objDrawer.hierarchy[1][1];
            picker.style.display = DisplayStyle.None;
            if (objDrawer.hierarchy[1].childCount == 2)
            {
                var pickerClone = new VisualElement();
                pickerClone.AddToClassList(ObjectField.selectorUssClassName);
                objDrawer.hierarchy[1].Add(pickerClone);
                pickerClone.RegisterCallback<MouseDownEvent>(x => _PickerSelect(x, reqIntAtr.InterfaceType, prop), TrickleDown.TrickleDown);
            }

            // objDrawer.hierarchy[1][1].RegisterCallback<MouseDownEvent>(_PickerSelect, TrickleDown.TrickleDown);

            void _PickerSelect(MouseDownEvent evt, Type pickType, SerializedProperty property)
            {
                var filter = ShowObjectPickerUtility.GetSearchFilter(typeof(Object), pickType);
                ShowObjectPickerUtility.ShowObjectPicker(typeof(Object), null, obj => _OnPickValue(obj, property), property.objectReferenceValue, ShowObjectPickerUtility.ObjectPickerSources.AssetsAndScene, filter);
                evt.StopPropagation();
            }


            static void _OnPickValue(Object o, SerializedProperty property)
            {
                property.objectReferenceValue = o;
                property.serializedObject.ApplyModifiedProperties();
            }

            static void _ValidateInput(SerializedPropertyChangeEvent evt, RequireInterfaceAttribute reqIntAtr)
            {
                if (evt.changedProperty.objectReferenceValue != null)
                {
                    if (evt.changedProperty.objectReferenceValue is GameObject go)
                    {
                        var comp = go.GetComponent(reqIntAtr.InterfaceType);
                        evt.changedProperty.objectReferenceValue = comp;
                        evt.changedProperty.serializedObject.ApplyModifiedProperties();
                    }
                    else
                    {
                        if (!reqIntAtr.InterfaceType.IsInstanceOfType(evt.changedProperty.objectReferenceValue))
                        {
                            evt.changedProperty.objectReferenceValue = null;
                            evt.changedProperty.serializedObject.ApplyModifiedProperties();
                            return;
                        }
                    }
                }

                var field = evt.target as PropertyField;
                var objDrawer = field.Q<ObjectField>();
                var guiContent = EditorGUIUtility.ObjectContent(evt.changedProperty.objectReferenceValue, reqIntAtr.InterfaceType);
                objDrawer.hierarchy[1].Q<Image>().image = guiContent.image;
                objDrawer.hierarchy[1].Q<Label>().text = guiContent.text;
            }
        }
        #endregion

        #region - Node Attribute Drawers -
        public static void DrawLabel(PropertyField field, VaporInspectorNode drawer)
        {
            if (drawer.TryGetAttribute<LabelAttribute>(out var atr))
            {
                var label = field.Q<Label>();
                switch (atr.LabelResolverType)
                {
                    case ResolverType.None:
                        label.text = atr.Label;
                        break;
                    case ResolverType.Property:
                        var pi = ReflectionUtility.GetProperty(drawer.Target, atr.LabelResolver[1..]);
                        var resolverContainerProp = new ResolverContainerClass<string>(drawer, pi, s => label.text = s);
                        drawer.VisualNode.AddResolver(resolverContainerProp);

                        //PropertyInfo pi = ReflectionUtility.GetProperty(drawer.Target, atr.LabelResolver[1..]);
                        //label.text = (string)pi.GetValue(drawer.Target);
                        //resolvers.Add(() => label.text = (string)pi.GetValue(drawer.Target));
                        break;
                    case ResolverType.Method:
                        var mi = ReflectionUtility.GetMethod(drawer.Target, atr.LabelResolver[1..]);
                        var resolverContainerMethod = new ResolverContainerClass<string>(drawer, mi, s => label.text = s);
                        drawer.VisualNode.AddResolver(resolverContainerMethod);

                        //MethodInfo mi = ReflectionUtility.GetMethod(drawer.Target, atr.LabelResolver[1..]);
                        //label.text = (string)mi.Invoke(drawer.Target, null);
                        //resolvers.Add(() => label.text = (string)mi.Invoke(drawer.Target, null));
                        break;
                    case ResolverType.Field:
                        var fi = ReflectionUtility.GetField(drawer.Target, atr.LabelResolver[1..]);
                        var resolverContainerField = new ResolverContainerClass<string>(drawer, fi, s => label.text = s);
                        drawer.VisualNode.AddResolver(resolverContainerField);
                        break;
                }

                switch (atr.LabelColorResolverType)
                {
                    case ResolverType.None:
                        label.style.color = atr.LabelColor;
                        break;
                    case ResolverType.Property:
                        var pi = ReflectionUtility.GetProperty(drawer.Target, atr.LabelColorResolver[1..]);
                        var resolverContainerProp = new ResolverContainerStruct<Color>(drawer, pi, c => label.style.color = c);
                        drawer.VisualNode.AddResolver(resolverContainerProp);

                        //PropertyInfo pi = ReflectionUtility.GetProperty(drawer.Target, atr.LabelColorResolver[1..]);
                        //label.style.color = (Color)pi.GetValue(drawer.Target);
                        //resolvers.Add(() => label.style.color = (Color)pi.GetValue(drawer.Target));
                        break;
                    case ResolverType.Method:
                        var mi = ReflectionUtility.GetMethod(drawer.Target, atr.LabelColorResolver[1..]);
                        var resolverContainerMethod = new ResolverContainerStruct<Color>(drawer, mi, c => label.style.color = c);
                        drawer.VisualNode.AddResolver(resolverContainerMethod);

                        //MethodInfo mi = ReflectionUtility.GetMethod(drawer.Target, atr.LabelColorResolver[1..]);
                        //label.style.color = (Color)mi.Invoke(drawer.Target, null);
                        //resolvers.Add(() => label.style.color = (Color)mi.Invoke(drawer.Target, null));
                        break;
                    case ResolverType.Field:
                        var fi = ReflectionUtility.GetField(drawer.Target, atr.LabelColorResolver[1..]);
                        var resolverContainerField = new ResolverContainerStruct<Color>(drawer, fi, c => label.style.color = c);
                        drawer.VisualNode.AddResolver(resolverContainerField);
                        break;
                }

                if (atr.HasIcon)
                {
                    var image = new Image
                    {
                        image = EditorGUIUtility.IconContent(atr.Icon).image,
                        scaleMode = ScaleMode.ScaleToFit,
                        pickingMode = PickingMode.Ignore
                    };
                    image.style.alignSelf = Align.FlexEnd;
                    switch (atr.IconColorResolverType)
                    {
                        case ResolverType.None:
                            image.tintColor = atr.IconColor.value;
                            break;
                        case ResolverType.Property:
                            var pi = ReflectionUtility.GetProperty(drawer.Target, atr.IconColorResolver[1..]);
                            var resolverContainerProp = new ResolverContainerStruct<Color>(drawer, pi, c => image.tintColor = c);
                            drawer.VisualNode.AddResolver(resolverContainerProp);

                            //PropertyInfo pi = ReflectionUtility.GetProperty(drawer.Target, atr.IconColorResolver[1..]);
                            //image.tintColor = (Color)pi.GetValue(drawer.Target);
                            //resolvers.Add(() => image.tintColor = (Color)pi.GetValue(drawer.Target));
                            break;
                        case ResolverType.Method:
                            var mi = ReflectionUtility.GetMethod(drawer.Target, atr.IconColorResolver[1..]);
                            var resolverContainerMethod = new ResolverContainerStruct<Color>(drawer, mi, c => image.tintColor = c);
                            drawer.VisualNode.AddResolver(resolverContainerMethod);

                            //MethodInfo mi = ReflectionUtility.GetMethod(drawer.Target, atr.IconColorResolver[1..]);
                            //image.tintColor = (Color)mi.Invoke(drawer.Target, null);
                            //resolvers.Add(() => image.tintColor = (Color)mi.Invoke(drawer.Target, null));
                            break;
                        case ResolverType.Field:
                            var fi = ReflectionUtility.GetField(drawer.Target, atr.IconColorResolver[1..]);
                            var resolverContainerField = new ResolverContainerStruct<Color>(drawer, fi, c => image.tintColor = c);
                            drawer.VisualNode.AddResolver(resolverContainerField);
                            break;
                    }

                    label.Add(image);
                }
            }
        }

        public static void DrawLabelWidth(PropertyField field, VaporInspectorNode drawer)
        {
            if (!drawer.TryGetAttribute<LabelWidthAttribute>(out var atr)) return;
            
            field[0].RemoveFromClassList("unity-base-field__aligned");
            var label = field.Q<Label>();
            if (atr.UseAutoWidth)
            {
                label.style.minWidth = new StyleLength(StyleKeyword.Auto);
                label.style.width = new StyleLength(StyleKeyword.Auto);
            }
            else
            {
                var minWidth = Mathf.Min(label.resolvedStyle.minWidth.value, atr.Width);
                label.style.minWidth = minWidth;
                label.style.width = atr.Width;
            }
        }

        public static void DrawHideLabel(PropertyField field, VaporInspectorNode drawer)
        {
            if (drawer.HasAttribute<HideLabelAttribute>())
            {
                var label = field.Q<Label>();
                label.style.display = DisplayStyle.None;
            }
        }

        public static void DrawRichTooltip(PropertyField field, VaporInspectorNode drawer)
        {
            if (!drawer.TryGetAttribute<RichTextTooltipAttribute>(out var rtAtr)) return;

            var label = field.Q<Label>();
            label.tooltip = rtAtr.Tooltip;
        }

        public static void DrawDecorators(VisualElement field, VaporInspectorNode drawer)
        {
            if (drawer.TryGetAttribute<BackgroundColorAttribute>(out var backgroundColor))
            {
                field.style.backgroundColor = backgroundColor.BackgroundColor;
            }

            if (drawer.TryGetAttribute<MarginsAttribute>(out var margins))
            {
                if (margins.Bottom != null)
                {
                    field.style.marginBottom = margins.Bottom.Value;
                }

                if (margins.Top != null)
                {
                    field.style.marginTop = margins.Top.Value;
                }

                if (margins.Left != null)
                {
                    field.style.marginLeft = margins.Left.Value;
                }

                if (margins.Right != null)
                {
                    field.style.marginRight = margins.Right.Value;
                }
            }

            if (drawer.TryGetAttribute<PaddingAttribute>(out var padding))
            {
                if (padding.Bottom != null)
                {
                    field.style.paddingBottom = padding.Bottom.Value;
                }

                if (padding.Top != null)
                {
                    field.style.paddingTop = padding.Top.Value;
                }

                if (padding.Left != null)
                {
                    field.style.paddingLeft = padding.Left.Value;
                }

                if (padding.Right != null)
                {
                    field.style.paddingRight = padding.Right.Value;
                }
            }

            if (drawer.TryGetAttribute<BordersAttribute>(out var borders))
            {
                field.style.borderBottomWidth = borders.Bottom;
                field.style.borderBottomColor = borders.Color;

                field.style.borderTopWidth = borders.Top;
                field.style.borderTopColor = borders.Color;

                field.style.borderLeftWidth = borders.Left;
                field.style.borderLeftColor = borders.Color;

                field.style.borderRightWidth = borders.Right;
                field.style.borderRightColor = borders.Color;

                if (borders.Rounded)
                {
                    field.style.borderBottomLeftRadius = 3;
                    field.style.borderBottomRightRadius = 3;
                    field.style.borderTopLeftRadius = 3;
                    field.style.borderTopRightRadius = 3;
                }
            }
        }
        
        public static void DrawConditionals(VisualElement field, VaporInspectorNode drawer)
        {
            if (drawer.TryGetAttribute<ShowIfAttribute>(out var showIf))
            {
                switch (showIf.ResolverType)
                {
                    case ResolverType.None:
                        break;
                    case ResolverType.Property:
                        var pi = ReflectionUtility.GetProperty(drawer.Target, showIf.Resolver[1..]);
                        var resolverContainerProp = new ResolverContainerStruct<bool>(drawer, pi, b => field.style.display = b ? DisplayStyle.Flex : DisplayStyle.None);
                        drawer.VisualNode.AddResolver(resolverContainerProp);
                        //resolvers.Add(() => resolverContainerProp.Resolve());

                        //var pi = ReflectionUtility.GetProperty(drawer.Target, showIf.Resolver[1..]);
                        //field.style.display = (bool)pi.GetValue(drawer.Target) ? DisplayStyle.Flex : DisplayStyle.None;
                        //resolvers.Add(() => field.style.display = (bool)pi.GetValue(drawer.Target) ? DisplayStyle.Flex : DisplayStyle.None);
                        break;
                    case ResolverType.Method:
                        var mi = ReflectionUtility.GetMethod(drawer.Target, showIf.Resolver[1..]);
                        var resolverContainerMethod = new ResolverContainerStruct<bool>(drawer, mi, b => field.style.display = b ? DisplayStyle.Flex : DisplayStyle.None);
                        drawer.VisualNode.AddResolver(resolverContainerMethod);

                        //var mi = ReflectionUtility.GetMethod(drawer.Target, showIf.Resolver[1..]);
                        //field.style.display = (bool)mi.Invoke(drawer.Target, null) ? DisplayStyle.Flex : DisplayStyle.None;
                        //resolvers.Add(() => field.style.display = (bool)mi.Invoke(drawer.Target, null) ? DisplayStyle.Flex : DisplayStyle.None);
                        break;
                    case ResolverType.Field:
                        var fi = ReflectionUtility.GetField(drawer.Target, showIf.Resolver[1..]);
                        var resolverContainerField = new ResolverContainerStruct<bool>(drawer, fi, b => field.style.display = b ? DisplayStyle.Flex : DisplayStyle.None);
                        drawer.VisualNode.AddResolver(resolverContainerField);
                        //resolvers.Add(() => resolverContainer.Resolve());

                        // var fi = ReflectionUtility.GetField(drawer.Target, showIf.Resolver[1..]);
                        // field.style.display = (bool)fi.GetValue(drawer.Target) ? DisplayStyle.Flex : DisplayStyle.None;
                        // resolvers.Add(() => field.style.display = (bool)fi.GetValue(drawer.Target) ? DisplayStyle.Flex : DisplayStyle.None);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (drawer.TryGetAttribute<HideIfAttribute>(out var hideIf))
            {
                switch (hideIf.ResolverType)
                {
                    case ResolverType.None:
                        break;
                    case ResolverType.Property:
                        var pi = ReflectionUtility.GetProperty(drawer.Target, hideIf.Resolver[1..]);
                        var resolverContainerProp = new ResolverContainerStruct<bool>(drawer, pi, b => field.style.display = b ? DisplayStyle.None : DisplayStyle.Flex);
                        drawer.VisualNode.AddResolver(resolverContainerProp);

                        //var pi = ReflectionUtility.GetProperty(drawer.Target, hideIf.Resolver[1..]);
                        //field.style.display = (bool)pi.GetValue(drawer.Target) ? DisplayStyle.None : DisplayStyle.Flex;
                        //resolvers.Add(() => field.style.display = (bool)pi.GetValue(drawer.Target) ? DisplayStyle.None : DisplayStyle.Flex);
                        break;
                    case ResolverType.Method:
                        var mi = ReflectionUtility.GetMethod(drawer.Target, hideIf.Resolver[1..]);
                        var resolverContainerMethod = new ResolverContainerStruct<bool>(drawer, mi, b => field.style.display = b ? DisplayStyle.None : DisplayStyle.Flex);
                        drawer.VisualNode.AddResolver(resolverContainerMethod);

                        //var mi = ReflectionUtility.GetMethod(drawer.Target, hideIf.Resolver[1..]);
                        //field.style.display = (bool)mi.Invoke(drawer.Target, null) ? DisplayStyle.None : DisplayStyle.Flex;
                        //resolvers.Add(() => field.style.display = (bool)mi.Invoke(drawer.Target, null) ? DisplayStyle.None : DisplayStyle.Flex);
                        break;
                    case ResolverType.Field:
                        var fi = ReflectionUtility.GetField(drawer.Target, hideIf.Resolver[1..]);
                        var resolverContainerField = new ResolverContainerStruct<bool>(drawer, fi, b => field.style.display = b ? DisplayStyle.None : DisplayStyle.Flex);
                        drawer.VisualNode.AddResolver(resolverContainerField);

                        //var fi = ReflectionUtility.GetField(drawer.Target, hideIf.Resolver[1..]);
                        //field.style.display = (bool)fi.GetValue(drawer.Target) ? DisplayStyle.None : DisplayStyle.Flex;
                        //resolvers.Add(() => field.style.display = (bool)fi.GetValue(drawer.Target) ? DisplayStyle.None : DisplayStyle.Flex);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (drawer.TryGetAttribute<DisableIfAttribute>(out var disableIf))
            {
                switch (disableIf.ResolverType)
                {
                    case ResolverType.None:
                        break;
                    case ResolverType.Property:
                        var pi = ReflectionUtility.GetProperty(drawer.Target, disableIf.Resolver[1..]);
                        var resolverContainerProp = new ResolverContainerStruct<bool>(drawer, pi, b => field.SetEnabled(!b));
                        drawer.VisualNode.AddResolver(resolverContainerProp);

                        //var pi = ReflectionUtility.GetProperty(drawer.Target, disableIf.Resolver[1..]);
                        //field.SetEnabled(!(bool)pi.GetValue(drawer.Target));
                        //resolvers.Add(() => field.SetEnabled(!(bool)pi.GetValue(drawer.Target)));
                        break;
                    case ResolverType.Method:
                        var mi = ReflectionUtility.GetMethod(drawer.Target, disableIf.Resolver[1..]);
                        var resolverContainerMethod = new ResolverContainerStruct<bool>(drawer, mi, b => field.SetEnabled(!b));
                        drawer.VisualNode.AddResolver(resolverContainerMethod);

                        //var mi = ReflectionUtility.GetMethod(drawer.Target, disableIf.Resolver[1..]);
                        //field.SetEnabled(!(bool)mi.Invoke(drawer.Target, null));
                        //resolvers.Add(() => field.SetEnabled(!(bool)mi.Invoke(drawer.Target, null)));
                        break;
                    case ResolverType.Field:
                        var fi = ReflectionUtility.GetField(drawer.Target, disableIf.Resolver[1..]);
                        var resolverContainerField = new ResolverContainerStruct<bool>(drawer, fi, b => field.SetEnabled(!b));
                        drawer.VisualNode.AddResolver(resolverContainerField);

                        //var fi = ReflectionUtility.GetProperty(drawer.Target, disableIf.Resolver[1..]);
                        //field.SetEnabled(!(bool)fi.GetValue(drawer.Target));
                        //resolvers.Add(() => field.SetEnabled(!(bool)fi.GetValue(drawer.Target)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (drawer.TryGetAttribute<EnableIfAttribute>(out var enableIf))
            {
                switch (enableIf.ResolverType)
                {
                    case ResolverType.None:
                        break;
                    case ResolverType.Property:
                        var pi = ReflectionUtility.GetProperty(drawer.Target, enableIf.Resolver[1..]);
                        var resolverContainerProp = new ResolverContainerStruct<bool>(drawer, pi, b => field.SetEnabled(b));
                        drawer.VisualNode.AddResolver(resolverContainerProp);

                        //var pi = ReflectionUtility.GetProperty(drawer.Target, enableIf.Resolver[1..]);
                        //field.SetEnabled((bool)pi.GetValue(drawer.Target));
                        //resolvers.Add(() => field.SetEnabled((bool)pi.GetValue(drawer.Target)));
                        break;
                    case ResolverType.Method:
                        var mi = ReflectionUtility.GetMethod(drawer.Target, enableIf.Resolver[1..]);
                        var resolverContainerMethod = new ResolverContainerStruct<bool>(drawer, mi, b => field.SetEnabled(b));
                        drawer.VisualNode.AddResolver(resolverContainerMethod);

                        //var mi = ReflectionUtility.GetMethod(drawer.Target, enableIf.Resolver[1..]);
                        //field.SetEnabled((bool)mi.Invoke(drawer.Target, null));
                        //resolvers.Add(() => field.SetEnabled((bool)mi.Invoke(drawer.Target, null)));
                        break;
                    case ResolverType.Field:
                        var fi = ReflectionUtility.GetField(drawer.Target, enableIf.Resolver[1..]);
                        var resolverContainerField = new ResolverContainerStruct<bool>(drawer, fi, b => field.SetEnabled(b));
                        drawer.VisualNode.AddResolver(resolverContainerField);

                        //var fi = ReflectionUtility.GetField(drawer.Target, enableIf.Resolver[1..]);
                        //field.SetEnabled((bool)fi.GetValue(drawer.Target));
                        //resolvers.Add(() => field.SetEnabled((bool)fi.GetValue(drawer.Target)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (drawer.HasAttribute<HideInEditorModeAttribute>())
            {
                var resolverContainerFunc = new ResolverContainerActionStruct<bool>(
                    () => EditorApplication.isPlaying, 
                    b => field.style.display = b ? DisplayStyle.Flex : DisplayStyle.None);
                drawer.VisualNode.AddResolver(resolverContainerFunc);
            }

            if (drawer.HasAttribute<HideInPlayModeAttribute>())
            {
                var resolverContainerFunc = new ResolverContainerActionStruct<bool>(
                    () => EditorApplication.isPlaying,
                    b => field.style.display = b ? DisplayStyle.None : DisplayStyle.Flex);
                drawer.VisualNode.AddResolver(resolverContainerFunc);
            }

            if (drawer.HasAttribute<DisableInEditorModeAttribute>())
            {
                var resolverContainerFunc = new ResolverContainerActionStruct<bool>(
                    () => EditorApplication.isPlaying,
                    b => field.SetEnabled(b));
                drawer.VisualNode.AddResolver(resolverContainerFunc);
            }

            if (drawer.HasAttribute<DisableInPlayModeAttribute>())
            {
                var resolverContainerFunc = new ResolverContainerActionStruct<bool>(
                    () => EditorApplication.isPlaying,
                    b => field.SetEnabled(!b));
                drawer.VisualNode.AddResolver(resolverContainerFunc);
            }
        }

        public static void DrawPathSelection(PropertyField field, VaporInspectorNode drawer)
        {
            if (drawer.TryGetAttribute<FilePathAttribute>(out var fileAtr) && field[0] is TextField filePathTextField)
            {
                var inlineButton = new Button(() => filePathTextField.value = _FormatFilePath(fileAtr.AbsolutePath, fileAtr.FileExtension))
                {
                    text = "",
                };
                var image = new Image
                {
                    image = EditorGUIUtility.IconContent("d_FolderOpened Icon").image,
                    scaleMode = ScaleMode.ScaleToFit
                };
                filePathTextField.style.width = 0;
                inlineButton.style.paddingLeft = 3;
                inlineButton.style.paddingRight = 3;
                inlineButton.style.backgroundColor = new Color(0, 0, 0, 0);
                image.style.width = 16;
                image.style.height = 16;
                inlineButton.Add(image);
                field.Add(inlineButton);
                field.style.flexDirection = FlexDirection.Row;
                field[0].style.flexGrow = 1f;
            }

            if (drawer.TryGetAttribute<FolderPathAttribute>(out var folderAtr) && field[0] is TextField folderPathTextField)
            {
                var inlineButton = new Button(() => folderPathTextField.value = _FormatFolderPath(folderAtr.AbsolutePath))
                {
                    text = "",
                };
                var image = new Image
                {
                    image = EditorGUIUtility.IconContent("d_FolderOpened Icon").image,
                    scaleMode = ScaleMode.ScaleToFit
                };
                folderPathTextField.style.width = 0;
                inlineButton.style.paddingLeft = 3;
                inlineButton.style.paddingRight = 3;
                inlineButton.style.backgroundColor = new Color(0, 0, 0, 0);
                image.style.width = 16;
                image.style.height = 16;
                inlineButton.Add(image);
                field.Add(inlineButton);
                field.style.flexDirection = FlexDirection.Row;
                field[0].style.flexGrow = 1f;
            }

            string _FormatFilePath(bool absolutePath, string fileExtension)
            {
                if (!absolutePath)
                {
                    var path = EditorUtility.OpenFilePanel("File Path", "Assets", fileExtension);
                    var start = path.IndexOf("Assets", StringComparison.Ordinal);
                    return path[start..];
                }
                else
                {
                    return EditorUtility.OpenFilePanel("File Path", "Assets", fileExtension);
                }
            }

            string _FormatFolderPath(bool absolutePath)
            {
                if (!absolutePath)
                {
                    var path = EditorUtility.OpenFolderPanel("Folder Path", "Assets", "");
                    if (string.IsNullOrEmpty(path))
                    {
                        return "";
                    }

                    var start = path.IndexOf("Assets", StringComparison.Ordinal);
                    return path[start..];
                }
                else
                {
                    return EditorUtility.OpenFolderPanel("Folder Path", "Assets", "");
                }
            }
        }

        public static void DrawReadOnly(PropertyField field, VaporInspectorNode drawer)
        {
            if (drawer.HasAttribute<ReadOnlyAttribute>())
            {
                field.SetEnabled(false);
            }
        }

        public static void DrawTitle(VaporInspectorNode node)
        {
            if (!node.TryGetAttribute<TitleAttribute>(out var atr)) return;
            
            var labelText = $"<b>{atr.Title}</b>";
            if (atr.Subtitle != string.Empty)
            {
                labelText = $"<b>{atr.Title}</b>\n<color=#9E9E9E><i><size=10>{atr.Subtitle}</size></i></color>";
            }

            var title = new Label(labelText)
            {
                style =
                {
                    borderBottomWidth = atr.Underline ? 1 : 0,
                    paddingBottom = 2,
                    borderBottomColor = ContainerStyles.TextDefault,
                    marginBottom = 1f
                }
            };
            // Has to insert at hierarchy because .Insert uses the content container which is the PropertyField.
            node.VisualNode.hierarchy.Insert(0, title);
        }

        public static void DrawInlineButtons(PropertyField field, VaporInspectorNode drawer)
        {
            if (drawer.TryGetAttributes<InlineButtonAttribute>(out var atrs))
            {
                foreach (var atr in atrs)
                {
                    var methodInfo = ReflectionUtility.GetMethod(drawer.Target, atr.MethodName);
                    if (methodInfo != null)
                    {
                        var inlineButton = new Button(() => methodInfo.Invoke(drawer.Target, null))
                        {
                            text = atr.Label,
                        };
                        inlineButton.style.paddingLeft = 3;
                        inlineButton.style.paddingRight = 3;
                        if (atr.Icon != string.Empty)
                        {
                            var image = new Image
                            {
                                image = EditorGUIUtility.IconContent(atr.Icon).image,
                                scaleMode = ScaleMode.ScaleToFit,
                                tintColor = atr.Tint
                            };
                            if (atr.TintResolverType != ResolverType.None)
                            {
                                switch (atr.TintResolverType)
                                {
                                    case ResolverType.None:
                                        break;
                                    case ResolverType.Property:
                                        var pi = ReflectionUtility.GetProperty(drawer.Target, atr.TintResolver[1..]);
                                        var resolverContainerProp = new ResolverContainerStruct<Color>(drawer, pi, c => image.tintColor = c);
                                        drawer.VisualNode.AddResolver(resolverContainerProp);

                                        //var pi = ReflectionUtility.GetProperty(drawer.Target, atr.TintResolver[1..]);
                                        //image.tintColor = (Color)pi.GetValue(drawer.Target);
                                        //resolvers.Add(() => image.tintColor = (Color)pi.GetValue(drawer.Target));
                                        break;
                                    case ResolverType.Method:
                                        var mi = ReflectionUtility.GetMethod(drawer.Target, atr.TintResolver[1..]);
                                        var resolverContainerMethod = new ResolverContainerStruct<Color>(drawer, mi, c => image.tintColor = c);
                                        drawer.VisualNode.AddResolver(resolverContainerMethod);

                                        //var mi = ReflectionUtility.GetMethod(drawer.Target, atr.TintResolver[1..]);
                                        //image.tintColor = (Color)mi.Invoke(drawer.Target, null);
                                        //resolvers.Add(() => image.tintColor = (Color)mi.Invoke(drawer.Target, null));
                                        break;
                                    case ResolverType.Field:
                                        var fi = ReflectionUtility.GetField(drawer.Target, atr.TintResolver[1..]);
                                        var resolverContainerField = new ResolverContainerStruct<Color>(drawer, fi, c => image.tintColor = c);
                                        drawer.VisualNode.AddResolver(resolverContainerField);
                                        break;
                                }
                            }

                            inlineButton.Add(image);
                        }

                        field.Add(inlineButton);
                        field.style.flexDirection = FlexDirection.Row;
                        field[0].style.flexGrow = 1f;
                    }
                }
            }
        }

        public static void DrawSuffix(PropertyField field, VaporInspectorNode drawer)
        {
            if (drawer.TryGetAttribute<SuffixAttribute>(out var atr))
            {
                var suffix = new Label(atr.Suffix);
                suffix.style.color = new Color(0.5f, 0.5f, 0.5f, 1);
                suffix.style.alignSelf = Align.Center;
                suffix.style.marginLeft = 3;
                suffix.style.paddingLeft = 3;
                field.Add(suffix);
                field.style.flexDirection = FlexDirection.Row;
                field[0].style.flexGrow = 1f;
            }
        }

        public static void DrawAutoReference(PropertyField field, VaporInspectorNode drawer)
        {
            if (drawer.TryGetAttribute<AutoReferenceAttribute>(out var atr)
                && drawer.Property.propertyType == SerializedPropertyType.ObjectReference
                && !drawer.Property.objectReferenceValue
                && drawer.Property.serializedObject.targetObject is Component component)
            {
                var comp = component.GetComponent(drawer.FieldInfo.FieldType);
                if (!comp && atr.SearchChildren)
                {
                    comp = component.GetComponentInChildren(drawer.FieldInfo.FieldType, true);
                }

                if (!comp && atr.SearchParents)
                {
                    comp = component.GetComponentInParent(drawer.FieldInfo.FieldType, true);
                }

                drawer.Property.objectReferenceValue = comp;
                drawer.Property.serializedObject.ApplyModifiedProperties();
            }
        }

        public static void DrawValidation(PropertyField field, VaporInspectorNode drawer)
        {
            if (drawer.TryGetAttribute<OnValueChangedAttribute>(out var ovcatr))
            {
                var methodInfo = ReflectionUtility.GetMethod(drawer.Target, ovcatr.MethodName);
                if (methodInfo != null)
                {
                    field.RegisterValueChangeCallback(x => methodInfo.Invoke(drawer.Target, null));
                }
            }

            if (drawer.TryGetAttribute<ValidateInputAttribute>(out var viatr))
            {
                var label = field.Q<Label>();
                var image = new Image
                {
                    name = "image-error",
                    image = EditorGUIUtility.IconContent("Error").image,
                    scaleMode = ScaleMode.ScaleToFit,
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        alignSelf = Align.FlexEnd
                    }
                    //tintColor = ContainerStyles.ErrorText.value,
                };
                label.Add(image);

                var methodInfo = ReflectionUtility.GetMethod(drawer.Target, viatr.MethodName);
                if (methodInfo != null)
                {
                    var validated = _OnValidateInput(drawer.Property, methodInfo, drawer.Target);
                    image.style.display = validated ? DisplayStyle.None : DisplayStyle.Flex;
                    field.RegisterValueChangeCallback(x => _ValidateInput(x, methodInfo, drawer.Target));
                }
            }

            if (drawer.TryGetAttribute<RangeAttribute>(out var rangeAttribute))
            {
                _ClampRangeValues(drawer.Property, rangeAttribute.min, rangeAttribute.max);
            }

            static void _ValidateInput(SerializedPropertyChangeEvent evt, MethodInfo mi, object target)
            {
                var validated = _OnValidateInput(evt.changedProperty, mi, target);
                var field = evt.target as PropertyField;
                var image = field.Q<Image>("image-error");
                image.style.display = validated ? DisplayStyle.None : DisplayStyle.Flex;
            }

            static bool _OnValidateInput(SerializedProperty sp, MethodInfo mi, object target)
            {
                return sp.propertyType switch
                {
                    SerializedPropertyType.Generic => (bool)mi.Invoke(target, new[] { sp.boxedValue }),
                    SerializedPropertyType.Integer => (bool)mi.Invoke(target, new object[] { sp.intValue }),
                    SerializedPropertyType.Boolean => (bool)mi.Invoke(target, new object[] { sp.boolValue }),
                    SerializedPropertyType.Float => (bool)mi.Invoke(target, new object[] { sp.floatValue }),
                    SerializedPropertyType.String => (bool)mi.Invoke(target, new object[] { sp.stringValue }),
                    SerializedPropertyType.Color => (bool)mi.Invoke(target, new object[] { sp.colorValue }),
                    SerializedPropertyType.ObjectReference => (bool)mi.Invoke(target, new object[] { sp.objectReferenceValue }),
                    SerializedPropertyType.LayerMask => (bool)mi.Invoke(target, new object[] { sp.intValue }),
                    SerializedPropertyType.Enum => (bool)mi.Invoke(target, new object[] { sp.enumValueIndex }),
                    SerializedPropertyType.Vector2 => (bool)mi.Invoke(target, new object[] { sp.vector2Value }),
                    SerializedPropertyType.Vector3 => (bool)mi.Invoke(target, new object[] { sp.vector3Value }),
                    SerializedPropertyType.Vector4 => (bool)mi.Invoke(target, new object[] { sp.vector4Value }),
                    SerializedPropertyType.Rect => (bool)mi.Invoke(target, new object[] { sp.rectValue }),
                    SerializedPropertyType.ArraySize => (bool)mi.Invoke(target, new object[] { sp.arraySize }),
                    SerializedPropertyType.Character => (bool)mi.Invoke(target, new object[] { sp.stringValue }),
                    SerializedPropertyType.AnimationCurve => (bool)mi.Invoke(target, new object[] { sp.animationCurveValue }),
                    SerializedPropertyType.Bounds => (bool)mi.Invoke(target, new object[] { sp.boundsValue }),
                    SerializedPropertyType.Gradient => (bool)mi.Invoke(target, new object[] { sp.gradientValue }),
                    SerializedPropertyType.Quaternion => (bool)mi.Invoke(target, new object[] { sp.quaternionValue }),
                    SerializedPropertyType.ExposedReference => (bool)mi.Invoke(target, new object[] { sp.exposedReferenceValue }),
                    SerializedPropertyType.FixedBufferSize => (bool)mi.Invoke(target, new object[] { sp.fixedBufferSize }),
                    SerializedPropertyType.Vector2Int => (bool)mi.Invoke(target, new object[] { sp.vector2IntValue }),
                    SerializedPropertyType.Vector3Int => (bool)mi.Invoke(target, new object[] { sp.vector3IntValue }),
                    SerializedPropertyType.RectInt => (bool)mi.Invoke(target, new object[] { sp.rectIntValue }),
                    SerializedPropertyType.BoundsInt => (bool)mi.Invoke(target, new object[] { sp.boundsIntValue }),
                    SerializedPropertyType.ManagedReference => (bool)mi.Invoke(target, new[] { sp.managedReferenceValue }),
                    SerializedPropertyType.Hash128 => (bool)mi.Invoke(target, new object[] { sp.hash128Value }),
                    _ => false,
                };
            }

            static void _ClampRangeValues(SerializedProperty sp, float min, float max)
            {
                switch (sp.numericType)
                {
                    case SerializedPropertyNumericType.Int32:
                        int vInt32 = sp.intValue;
                        vInt32 = (int)Mathf.Clamp(vInt32, min, max);
                        sp.intValue = vInt32;
                        sp.serializedObject.ApplyModifiedProperties();
                        break;
                    case SerializedPropertyNumericType.Float:
                        float f = sp.floatValue;
                        f = Mathf.Clamp(f, min, max);
                        sp.floatValue = f;
                        sp.serializedObject.ApplyModifiedProperties();
                        break;
                    case SerializedPropertyNumericType.Double:
                        double d = sp.floatValue;
                        d = Math.Clamp(d, min, max);
                        sp.doubleValue = d;
                        sp.serializedObject.ApplyModifiedProperties();
                        break;
                    case SerializedPropertyNumericType.Unknown:
                    case SerializedPropertyNumericType.Int8:
                    case SerializedPropertyNumericType.UInt8:
                    case SerializedPropertyNumericType.Int16:
                    case SerializedPropertyNumericType.UInt16:
                    case SerializedPropertyNumericType.UInt32:
                    case SerializedPropertyNumericType.Int64:
                    case SerializedPropertyNumericType.UInt64:
                    default:
                        Debug.LogError($"Range Attribute For Type {sp.numericType} is not supported.");
                        break;
                }
            }
        }

        public static void DrawRequireInterface(PropertyField field, SerializedProperty prop, VaporInspectorNode drawer)
        {
            if (!drawer.TryGetAttribute<RequireInterfaceAttribute>(out var reqIntAtr)) return;
            
            var objDrawer = field.Q<ObjectField>();
            var guiContent = EditorGUIUtility.ObjectContent(objDrawer.value, reqIntAtr.InterfaceType);
            objDrawer.hierarchy[1].Q<Image>().image = guiContent.image;
            objDrawer.hierarchy[1].Q<Label>().text = guiContent.text;
            field.RegisterValueChangeCallback(x => _ValidateInput(x, reqIntAtr));

            var picker = objDrawer.hierarchy[1][1];
            picker.style.display = DisplayStyle.None;
            if (objDrawer.hierarchy[1].childCount == 2)
            {
                var pickerClone = new VisualElement();
                pickerClone.AddToClassList(ObjectField.selectorUssClassName);
                objDrawer.hierarchy[1].Add(pickerClone);
                pickerClone.RegisterCallback<MouseDownEvent>(x => _PickerSelect(x, reqIntAtr.InterfaceType, prop), TrickleDown.TrickleDown);
            }

            // objDrawer.hierarchy[1][1].RegisterCallback<MouseDownEvent>(_PickerSelect, TrickleDown.TrickleDown);

            void _PickerSelect(MouseDownEvent evt, Type pickType, SerializedProperty property)
            {
                var filter = ShowObjectPickerUtility.GetSearchFilter(typeof(Object), pickType);
                ShowObjectPickerUtility.ShowObjectPicker(typeof(Object), null, obj => _OnPickValue(obj, property), property.objectReferenceValue, ShowObjectPickerUtility.ObjectPickerSources.AssetsAndScene, filter);
                evt.StopPropagation();
            }


            static void _OnPickValue(Object o, SerializedProperty property)
            {
                property.objectReferenceValue = o;
                property.serializedObject.ApplyModifiedProperties();
            }

            static void _ValidateInput(SerializedPropertyChangeEvent evt, RequireInterfaceAttribute reqIntAtr)
            {
                if (evt.changedProperty.objectReferenceValue != null)
                {
                    if (evt.changedProperty.objectReferenceValue is GameObject go)
                    {
                        var comp = go.GetComponent(reqIntAtr.InterfaceType);
                        evt.changedProperty.objectReferenceValue = comp;
                        evt.changedProperty.serializedObject.ApplyModifiedProperties();
                    }
                    else
                    {
                        if (!reqIntAtr.InterfaceType.IsInstanceOfType(evt.changedProperty.objectReferenceValue))
                        {
                            evt.changedProperty.objectReferenceValue = null;
                            evt.changedProperty.serializedObject.ApplyModifiedProperties();
                            return;
                        }
                    }
                }

                var field = evt.target as PropertyField;
                var objDrawer = field.Q<ObjectField>();
                var guiContent = EditorGUIUtility.ObjectContent(evt.changedProperty.objectReferenceValue, reqIntAtr.InterfaceType);
                objDrawer.hierarchy[1].Q<Image>().image = guiContent.image;
                objDrawer.hierarchy[1].Q<Label>().text = guiContent.text;
            }
        }
        #endregion

        #region Helpers

        private static MethodInfo s_GetDrawerTypeForTypeInfo;

        private static bool HasCustomPropertyDrawer(Type type)
        {
            if (s_GetDrawerTypeForTypeInfo == null)
            {
                // Cache the method info
                var assembly = typeof(Editor).Assembly;
                var scriptAttributeUtilityType = assembly.GetType("UnityEditor.ScriptAttributeUtility");
                s_GetDrawerTypeForTypeInfo = scriptAttributeUtilityType.GetMethod("GetDrawerTypeForType", BindingFlags.NonPublic | BindingFlags.Static);
            }

            // ReSharper disable once PossibleNullReferenceException
            var drawerType = (Type)s_GetDrawerTypeForTypeInfo.Invoke(null, new object[] { type });
            return drawerType != null;
        }

        private static void OpenNewInspector(Object obj)
        {
            if(obj == null) { return; }

            Type inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor");
            //EditorWindow inspectorInstance = EditorWindow.GetWindow(inspectorType);

            var popup = EditorWindow.CreateInstance(inspectorType) as EditorWindow;
            //inspectorType.GetProperty("isLocked", BindingFlags.Public | BindingFlags.Instance).SetValue(popup, true);
            SetInspectorTarget(popup, obj);
            popup.Show();           
            Debug.Log(popup);

            //inspectorInstance.ShowModalUtility();

            // Set the target object for the Inspector window

            static void SetInspectorTarget(EditorWindow inspector, Object target)
            {
                //Type inspectorType = inspector.GetType().BaseType;
                string assetPath = AssetDatabase.GetAssetPath(target);
                if (string.IsNullOrEmpty(assetPath)) 
                {
                    if (target is Component comp)
                    {
                        inspector.GetType().GetMethod("SetObjectsLocked", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(inspector, new object[] { new List<Object>() { comp.gameObject } });
                    }
                }
                else
                {
                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    inspector.GetType().GetMethod("SetObjectsLocked", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(inspector, new object[] { new List<Object>() { asset } });
                }

                
                //var inspectedObjectField = inspectorType.GetField("m_InspectedObject", BindingFlags.NonPublic | BindingFlags.Instance);
                //if (inspectedObjectField != null)
                //{
                //    inspectedObjectField.SetValue(inspector, asset);
                //    inspector.Repaint();
                //}
                //else
                //{
                //    Debug.LogError("Failed to access the Inspector's internal fields.");
                //}
            }
        }
        #endregion

        #region - Resolvers -
        public abstract class ResolverContainer
        {
            public abstract void Resolve();

            protected object NearestTarget(VaporInspectorNode parent)
            {
                var target = parent.Property?.boxedValue;
                if (target == null)
                {
                    if (parent.IsRootNode)
                    {
                        target = parent.Root.targetObject;
                    }
                    else
                    {
                        return NearestTarget(parent.Parent);
                    }
                }
                return target;
            }
        }

        public class ResolverContainerActionStruct<T> : ResolverContainer where T : struct
        {
            private readonly Func<T> _checkForChanged;
            private readonly Action<T> _onValueChanged;

            private T _currentValue;

            public ResolverContainerActionStruct(Func<T> checkForChanged, Action<T> onValueChanged)
            {
                _checkForChanged = checkForChanged;
                _onValueChanged = onValueChanged;

                _currentValue = _checkForChanged.Invoke();
                _onValueChanged.Invoke(_currentValue);
            }

            public override void Resolve()
            {
                var val = _checkForChanged.Invoke();
                if (_currentValue.Equals(val))
                {
                    return;
                }

                _currentValue = val;
                _onValueChanged.Invoke(_currentValue);
            }
        }

        public class ResolverContainerActionClass<T> : ResolverContainer where T : class
        {
            private readonly Func<T> _checkForChanged;
            private readonly Action<T> _onValueChanged;

            private T _currentValue;

            public ResolverContainerActionClass(Func<T> checkForChanged, Action<T> onValueChanged)
            {
                _checkForChanged = checkForChanged;
                _onValueChanged = onValueChanged;

                _currentValue = _checkForChanged.Invoke();
                _onValueChanged.Invoke(_currentValue);
            }

            public override void Resolve()
            {
                var val = _checkForChanged.Invoke();
                if (_currentValue == val)
                {
                    return;
                }

                _currentValue = val;
                _onValueChanged.Invoke(_currentValue);
            }
        }

        public class ResolverContainerStruct<T> : ResolverContainer where T : struct
        {
            private ResolverType type;
            private readonly VaporInspectorNode _node;
            private readonly FieldInfo _fieldInfo;
            private readonly PropertyInfo _propertyInfo;
            private readonly MethodInfo _methodInfo;
            private readonly Action<T> _onValueChanged;
            
            private T _currentValue;

            public ResolverContainerStruct(VaporInspectorNode node, FieldInfo fieldInfo, Action<T> onValueChanged)
            {
                type = ResolverType.Field;
                _node = node;
                _fieldInfo = fieldInfo;
                _onValueChanged = onValueChanged;

                if (_node.Parent.Property == null && !(_node.Parent.ElementType is VaporInspectorNode.NodeElementType.Root or VaporInspectorNode.NodeElementType.Group))
                {
                    Debug.LogError($"Node parent is missing property {_node.Parent.VisualNode.name}");
                    return;
                }
                _currentValue = (T)_fieldInfo.GetValue(NearestTarget(_node.Parent));
                _onValueChanged.Invoke(_currentValue);
            }

            public ResolverContainerStruct(VaporInspectorNode node, PropertyInfo propertyInfo, Action<T> onValueChanged)
            {
                type = ResolverType.Property;
                _node = node;
                _propertyInfo = propertyInfo;
                _onValueChanged = onValueChanged;

                if (_node.Parent.Property == null && !(_node.Parent.ElementType is VaporInspectorNode.NodeElementType.Root or VaporInspectorNode.NodeElementType.Group))
                {
                    Debug.LogError($"Node parent is missing property {_node.Parent.VisualNode.name}");
                    return;
                }
                _currentValue = (T)_propertyInfo.GetValue(NearestTarget(_node.Parent));
                _onValueChanged.Invoke(_currentValue);
            }

            public ResolverContainerStruct(VaporInspectorNode node, MethodInfo methodInfo, Action<T> onValueChanged)
            {
                type = ResolverType.Method;
                _node = node;
                _methodInfo = methodInfo;
                _onValueChanged = onValueChanged;

                if (_node.Parent.Property == null && !(_node.Parent.ElementType is VaporInspectorNode.NodeElementType.Root or VaporInspectorNode.NodeElementType.Group))
                {
                    Debug.LogError($"Node parent is missing property {_node.Parent.VisualNode.name}");
                    return;
                }
                _currentValue = (T)_methodInfo.Invoke(NearestTarget(_node.Parent), null);
                _onValueChanged.Invoke(_currentValue);
            }

            public override void Resolve()
            {
                //if (_node.Parent.Property == null && !_node.Parent.IsRootNode) { Debug.LogError($"Node parent is missing property {_node.Parent.VisualNode.name}"); return; }

                var target = NearestTarget(_node.Parent);

                var val = type switch
                {
                    ResolverType.None => default,
                    ResolverType.Property => (T)_propertyInfo.GetValue(target),
                    ResolverType.Method => (T)_methodInfo.Invoke(target, null),
                    ResolverType.Field => (T)_fieldInfo.GetValue(target),
                    _ => default
                };
                if (_currentValue.Equals(val))
                {
                    return;
                }

                //var t = _node.Parent.Property.boxedValue.GetType();
                //var pi = t.GetProperty($"{_propertyInfo.Name}", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                //val = (T)pi.GetValue(_node.Parent.Property.boxedValue);
                //Debug.Log($"Resolving: {_node.Target} - {_propertyInfo.Name} - {_currentValue} == {val}");
                _currentValue = val;
                _onValueChanged.Invoke(_currentValue);
            }
        }

        public class ResolverContainerClass<T> : ResolverContainer where T : class
        {
            private readonly ResolverType type;
            private readonly VaporInspectorNode _node;
            private readonly FieldInfo _fieldInfo;
            private readonly PropertyInfo _propertyInfo;
            private readonly MethodInfo _methodInfo;
            private readonly Action<T> _onValueChanged;

            private T _currentValue;

            public ResolverContainerClass(VaporInspectorNode node, FieldInfo fieldInfo, Action<T> onValueChanged)
            {
                type = ResolverType.Field;
                _node = node;
                _fieldInfo = fieldInfo;
                _onValueChanged = onValueChanged;

                if (_node.Parent.Property == null && !(_node.Parent.ElementType is VaporInspectorNode.NodeElementType.Root or VaporInspectorNode.NodeElementType.Group))
                {
                    Debug.LogError($"Node parent is missing property {_node.Parent.VisualNode.name}");
                    return;
                }
                _currentValue = (T)_fieldInfo.GetValue(NearestTarget(_node.Parent));
                _onValueChanged.Invoke(_currentValue);
            }

            public ResolverContainerClass(VaporInspectorNode node, PropertyInfo propertyInfo, Action<T> onValueChanged)
            {
                type = ResolverType.Property;
                _node = node;
                _propertyInfo = propertyInfo;
                _onValueChanged = onValueChanged;

                if (_node.Parent.Property == null && !(_node.Parent.ElementType is VaporInspectorNode.NodeElementType.Root or VaporInspectorNode.NodeElementType.Group))
                {
                    Debug.LogError($"Node parent is missing property {_node.Parent.VisualNode.name}");
                    return;
                }
                _currentValue = (T)_propertyInfo.GetValue(NearestTarget(_node.Parent));
                _onValueChanged.Invoke(_currentValue);
            }

            public ResolverContainerClass(VaporInspectorNode node, MethodInfo methodInfo, Action<T> onValueChanged)
            {
                type = ResolverType.Method;
                _node = node;
                _methodInfo = methodInfo;
                _onValueChanged = onValueChanged;

                if (_node.Parent.Property == null && !(_node.Parent.ElementType is VaporInspectorNode.NodeElementType.Root or VaporInspectorNode.NodeElementType.Group))
                {
                    Debug.LogError($"Node parent is missing property {_node.Parent.VisualNode.name}");
                    return;
                }
                _currentValue = (T)_methodInfo.Invoke(NearestTarget(_node.Parent), null);
                _onValueChanged.Invoke(_currentValue);
            }

            public override void Resolve()
            {
                var target = NearestTarget(_node.Parent);

                var val = type switch
                {
                    ResolverType.None => default,
                    ResolverType.Property => (T)_propertyInfo.GetValue(target),
                    ResolverType.Method => (T)_methodInfo.Invoke(target, null),
                    ResolverType.Field => (T)_fieldInfo.GetValue(target),
                    _ => default
                };
                if (_currentValue.Equals(val))
                {
                    return;
                }

                _currentValue = val;
                _onValueChanged.Invoke(_currentValue);
            }
        }
        #endregion
    }
}
