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
                    if (HasCustomPropertyDrawer(drawer.FieldInfo.FieldType))
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

                    if (drawer.Property.isArray && !drawer.HasAttribute<DrawWithUnityAttribute>())
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
                var field = new SearchableDropdown<object>(drawer.Property.displayName, drawer.Property.boxedValue)
                {
                    name = drawerName,
                    userData = drawer,
                    tooltip = tooltip,
                };
                field.SetChoices(values);
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

            // Debug.Log(field.name);
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

            // field.Q<Label>().AddManipulator(new ContextualMenuManipulator(@event =>
            // {
            //     Debug.Log("Fired");
            //     @event.menu.AppendAction("Set To Null", ca =>
            //     {
            //         drawer.Property.boxedValue = null;
            //         drawer.Property.serializedObject.ApplyModifiedProperties();
            //     });
            // }));
            // field.Q<Label>().RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            // {
            //     Debug.Log("Fired");
            //     evt.menu.AppendAction("Set To Null", ca =>
            //     {
            //         drawer.Property.boxedValue = null;
            //         drawer.Property.serializedObject.ApplyModifiedProperties();
            //     });
            // }, TrickleDown.TrickleDown);

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
            DrawValidation(field, drawer, resolvers);

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
            if (drawer.TryGetAttribute<LabelWidthAttribute>(out var atr))
            {
                field[0].RemoveFromClassList("unity-base-field__aligned");
                var label = field.Q<Label>();
                if (atr.UseAutoWidth)
                {
                    label.style.minWidth = new StyleLength(StyleKeyword.Auto);
                    label.style.width = new StyleLength(StyleKeyword.Auto);
                }
                else
                {
                    float minWidth = Mathf.Min(label.resolvedStyle.minWidth.value, atr.Width);
                    label.style.minWidth = minWidth;
                    label.style.width = atr.Width;
                }
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
            if (drawer.TryGetAttribute<BackgroundColorAttribute>(out var bcatr))
            {
                field.style.backgroundColor = bcatr.BackgroundColor;
            }

            if (drawer.TryGetAttribute<MarginsAttribute>(out var matr))
            {
                if (matr.Bottom != float.MinValue)
                {
                    field.style.marginBottom = matr.Bottom;
                }

                if (matr.Top != float.MinValue)
                {
                    field.style.marginTop = matr.Top;
                }

                if (matr.Left != float.MinValue)
                {
                    field.style.marginLeft = matr.Left;
                }

                if (matr.Right != float.MinValue)
                {
                    field.style.marginRight = matr.Right;
                }
            }

            if (drawer.TryGetAttribute<PaddingAttribute>(out var patr))
            {
                if (patr.Bottom != float.MinValue)
                {
                    field.style.paddingBottom = patr.Bottom;
                }

                if (patr.Top != float.MinValue)
                {
                    field.style.paddingTop = patr.Top;
                }

                if (patr.Left != float.MinValue)
                {
                    field.style.paddingLeft = patr.Left;
                }

                if (patr.Right != float.MinValue)
                {
                    field.style.paddingRight = patr.Right;
                }
            }

            if (drawer.TryGetAttribute<BordersAttribute>(out var batr))
            {
                if (batr.Bottom != float.MinValue)
                {
                    field.style.borderBottomWidth = batr.Bottom;
                    field.style.borderBottomColor = batr.Color;
                }

                if (batr.Top != float.MinValue)
                {
                    field.style.borderTopWidth = batr.Top;
                    field.style.borderTopColor = batr.Color;
                }

                if (batr.Left != float.MinValue)
                {
                    field.style.borderLeftWidth = batr.Left;
                    field.style.borderLeftColor = batr.Color;
                }

                if (batr.Right != float.MinValue)
                {
                    field.style.borderRightWidth = batr.Right;
                    field.style.borderRightColor = batr.Color;
                }

                if (batr.Rounded)
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
            if (drawer.TryGetAttribute<ShowIfAttribute>(out var siatr))
            {
                switch (siatr.ResolverType)
                {
                    case ResolverType.None:
                        break;
                    case ResolverType.Property:
                        PropertyInfo pi = ReflectionUtility.GetProperty(drawer.Target, siatr.Resolver[1..]);
                        field.style.display = (bool)pi.GetValue(drawer.Target) ? DisplayStyle.Flex : DisplayStyle.None;
                        resolvers.Add(() => field.style.display = (bool)pi.GetValue(drawer.Target) ? DisplayStyle.Flex : DisplayStyle.None);
                        break;
                    case ResolverType.Method:
                        MethodInfo mi = ReflectionUtility.GetMethod(drawer.Target, siatr.Resolver[1..]);
                        field.style.display = (bool)mi.Invoke(drawer.Target, null) ? DisplayStyle.Flex : DisplayStyle.None;
                        resolvers.Add(() => field.style.display = (bool)mi.Invoke(drawer.Target, null) ? DisplayStyle.Flex : DisplayStyle.None);
                        break;
                }
            }

            if (drawer.TryGetAttribute<ShowIfAttribute>(out var hiatr))
            {
                switch (hiatr.ResolverType)
                {
                    case ResolverType.None:
                        break;
                    case ResolverType.Property:
                        PropertyInfo pi = ReflectionUtility.GetProperty(drawer.Target, hiatr.Resolver[1..]);
                        field.style.display = (bool)pi.GetValue(drawer.Target) ? DisplayStyle.None : DisplayStyle.Flex;
                        resolvers.Add(() => field.style.display = (bool)pi.GetValue(drawer.Target) ? DisplayStyle.None : DisplayStyle.Flex);
                        break;
                    case ResolverType.Method:
                        MethodInfo mi = ReflectionUtility.GetMethod(drawer.Target, hiatr.Resolver[1..]);
                        field.style.display = (bool)mi.Invoke(drawer.Target, null) ? DisplayStyle.None : DisplayStyle.Flex;
                        resolvers.Add(() => field.style.display = (bool)mi.Invoke(drawer.Target, null) ? DisplayStyle.None : DisplayStyle.Flex);
                        break;
                }
            }

            if (drawer.TryGetAttribute<ShowIfAttribute>(out var diatr))
            {
                switch (diatr.ResolverType)
                {
                    case ResolverType.None:
                        break;
                    case ResolverType.Property:
                        PropertyInfo pi = ReflectionUtility.GetProperty(drawer.Target, diatr.Resolver[1..]);
                        field.SetEnabled(!(bool)pi.GetValue(drawer.Target));
                        resolvers.Add(() => field.SetEnabled(!(bool)pi.GetValue(drawer.Target)));
                        break;
                    case ResolverType.Method:
                        MethodInfo mi = ReflectionUtility.GetMethod(drawer.Target, diatr.Resolver[1..]);
                        field.SetEnabled(!(bool)mi.Invoke(drawer.Target, null));
                        resolvers.Add(() => field.SetEnabled(!(bool)mi.Invoke(drawer.Target, null)));
                        break;
                }
            }

            if (drawer.TryGetAttribute<ShowIfAttribute>(out var eiatr))
            {
                switch (eiatr.ResolverType)
                {
                    case ResolverType.None:
                        break;
                    case ResolverType.Property:
                        PropertyInfo pi = ReflectionUtility.GetProperty(drawer.Target, eiatr.Resolver[1..]);
                        field.SetEnabled((bool)pi.GetValue(drawer.Target));
                        resolvers.Add(() => field.SetEnabled((bool)pi.GetValue(drawer.Target)));
                        break;
                    case ResolverType.Method:
                        MethodInfo mi = ReflectionUtility.GetMethod(drawer.Target, eiatr.Resolver[1..]);
                        field.SetEnabled((bool)mi.Invoke(drawer.Target, null));
                        resolvers.Add(() => field.SetEnabled((bool)mi.Invoke(drawer.Target, null)));
                        break;
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
                    string path = EditorUtility.OpenFilePanel("File Path", "Assets", fileExtension);
                    int start = path.IndexOf("Assets");
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
                    string path = EditorUtility.OpenFolderPanel("Folder Path", "Assets", "");
                    int start = path.IndexOf("Assets");
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
                                        PropertyInfo pi = ReflectionUtility.GetProperty(drawer.Target, atr.TintResolver[1..]);
                                        image.tintColor = (Color)pi.GetValue(drawer.Target);
                                        resolvers.Add(() => image.tintColor = (Color)pi.GetValue(drawer.Target));
                                        break;
                                    case ResolverType.Method:
                                        MethodInfo mi = ReflectionUtility.GetMethod(drawer.Target, atr.TintResolver[1..]);
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
                    //tintColor = ContainerStyles.ErrorText.value,
                };
                image.style.alignSelf = Align.FlexEnd;
                label.Add(image);

                var methodInfo = ReflectionUtility.GetMethod(drawer.Target, viatr.MethodName);
                if (methodInfo != null)
                {
                    bool validated = _OnValidateInput(drawer.Property, methodInfo, drawer.Target);
                    image.style.display = validated ? DisplayStyle.None : DisplayStyle.Flex;
                    field.RegisterValueChangeCallback(x => _ValidateInput(x, methodInfo, drawer.Target));
                }
            }

            static void _ValidateInput(SerializedPropertyChangeEvent evt, MethodInfo mi, object target)
            {
                bool validated = _OnValidateInput(evt.changedProperty, mi, target);
                var field = evt.target as PropertyField;
                var image = field.Q<Image>("image-error");
                image.style.display = validated ? DisplayStyle.None : DisplayStyle.Flex;
            }

            static bool _OnValidateInput(SerializedProperty sp, MethodInfo mi, object target)
            {
                return sp.propertyType switch
                {
                    SerializedPropertyType.Generic => (bool)mi.Invoke(target, new object[] { sp.boxedValue }),
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
                    SerializedPropertyType.ManagedReference => (bool)mi.Invoke(target, new object[] { sp.managedReferenceValue }),
                    SerializedPropertyType.Hash128 => (bool)mi.Invoke(target, new object[] { sp.hash128Value }),
                    _ => false,
                };
            }
        }

        #endregion

        #region Helpers

        private static MethodInfo _getDrawerTypeForTypeInfo;

        private static bool HasCustomPropertyDrawer(Type type)
        {
            if (_getDrawerTypeForTypeInfo == null)
            {
                // Cache the method info
                var assembly = typeof(Editor).Assembly;
                var scriptAttributeUtilityType = assembly.GetType("UnityEditor.ScriptAttributeUtility");
                _getDrawerTypeForTypeInfo = scriptAttributeUtilityType.GetMethod("GetDrawerTypeForType", BindingFlags.NonPublic | BindingFlags.Static);
            }

            // ReSharper disable once PossibleNullReferenceException
            var drawerType = (Type)_getDrawerTypeForTypeInfo.Invoke(null, new object[] { type });
            return drawerType != null;
        }

        #endregion
    }
}
