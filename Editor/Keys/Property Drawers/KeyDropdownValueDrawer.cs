using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.Utilities;
using VaporInspector;
using VaporInspectorEditor;
using VaporKeys;

namespace VaporKeysEditor
{
    [CustomPropertyDrawer(typeof(KeyDropdownValue))]
    public class KeyDropdownValueDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            List<string> keys = new();
            List<KeyDropdownValue> values = new();
            var atr = fieldInfo.GetCustomAttribute<ValueDropdownAttribute>();
            if (atr == null)
            {
                return new Label($"{property.displayName} must implement {TooltipMarkup.ClassMarkup(nameof(ValueDropdownAttribute))}");
            }
            ConvertToTupleList(keys, values, GetKeysField(atr.AssemblyQualifiedType, atr.Resolver[1..]));
            var foldout = new StyledPropertyFoldout(property.displayName);
            var tooltip = "";
            if (fieldInfo.IsDefined(typeof(RichTextTooltipAttribute), true))
            {
                var rtAtr = fieldInfo.GetCustomAttribute<RichTextTooltipAttribute>();
                tooltip = rtAtr.Tooltip;
            }
            if (atr.Searchable)
            {
                var indexOfCurrent = values.IndexOf((KeyDropdownValue)property.boxedValue);
                var dropdown = new SearchableDropdown<string>("", keys[indexOfCurrent])
                {
                    name = fieldInfo.Name,
                    userData = (property, values),
                    tooltip = tooltip,
                    style =
                    {
                        flexGrow = 1
                    }
                };
                dropdown.SetChoices(keys);
                dropdown.ValueChanged += OnSearchableDropdownChanged; 
                foldout.SetHeaderProperty(dropdown);
            }
            else
            {
                Debug.Log((KeyDropdownValue)property.boxedValue);
                var indexOfCurrent = values.IndexOf((KeyDropdownValue)property.boxedValue);
                var dropdown = new DropdownField("", keys, indexOfCurrent)
                {
                    name = fieldInfo.Name,
                    tooltip = tooltip,
                    userData = (property, values),
                    style =
                    {
                        flexGrow = 1
                    }
                };
                dropdown.AddToClassList("unity-base-field__aligned");
                dropdown.RegisterValueChangedCallback(OnDropdownChanged);
                foldout.SetHeaderProperty(dropdown);
            }

            var guidBox = new StyledHorizontalGroup();
            var guidField = new PropertyField(property.FindPropertyRelative("Guid"))
            {
                style = { flexGrow = 1}
            };
            guidBox.Add(guidField);
            guidField.SetEnabled(false);
            guidBox.Add(new Button(() => OnSelectClicked(property))
            {
                text = "Select"
            });

            var keyBox = new StyledHorizontalGroup();
            var keyField = new PropertyField(property.FindPropertyRelative("Key"))
            {
                style = { flexGrow = 1}
            };
            keyField.SetEnabled(false);
            keyBox.Add(keyField);
            keyBox.Add(new Button(() => OnRemapClicked(property))
            {
                text = "Re-Map",
            });

            foldout.Add(guidBox);
            foldout.Add(keyBox);
            return foldout;
        }
        
        private static void OnSelectClicked(SerializedProperty property)
        {
            if (property is { boxedValue: KeyDropdownValue key })
            {
                key.Select();
            }
        }

        private static void OnRemapClicked(SerializedProperty property)
        {
            if (property is { boxedValue: KeyDropdownValue key })
            {
                key.Remap();
            }
        }

        private static void OnSearchableDropdownChanged(VisualElement visualElement, string oldValue, string newValue)
        {
            if (visualElement is SearchableDropdown<string> dropdown)
            {
                var tuple = ((SerializedProperty, List<KeyDropdownValue>))dropdown.userData;
                var newVal = tuple.Item2[dropdown.Index];
                Debug.Log("Applied " + newVal);
                tuple.Item1.boxedValue = newVal;
                tuple.Item1.serializedObject.ApplyModifiedProperties();
            }
        }

        private static void OnDropdownChanged(ChangeEvent<string> evt)
        {
            if (evt.target is DropdownField dropdown)
            {
                var tuple = ((SerializedProperty, List<KeyDropdownValue>))dropdown.userData;
                var newVal = tuple.Item2[dropdown.index];
                Debug.Log("Applied " + newVal);
                tuple.Item1.boxedValue = newVal;
                tuple.Item1.serializedObject.ApplyModifiedProperties();
            }
        }

        private static void ConvertToTupleList(List<string> keys, List<KeyDropdownValue> values, IList convert)
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
                values.Add((KeyDropdownValue)item2);
            }
        }

        private static IList GetKeysField(Type type, string valuesName)
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
}
