using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VaporInspector;

namespace VaporInspectorEditor
{
    public class SerializedReferenceSelector : VisualElement
    {
        public SerializedReferenceSelector(string label, string indexName, List<Type> types, Action<Type> setterCallback)
        {
            var keys = new List<string>();
            foreach (var type in types)
            {
                keys.Add(type.Name);
            }
            var index = keys.IndexOf(indexName);
            if (index == -1)
            {
                index = 0;
            }
            var dropdown = new SearchableDropdown<string>(label, keys[index])
            {
                userData = (setterCallback, types),
                style =
                    {
                        flexGrow = 1
                    }
            };
            dropdown.AddToClassList("unity-base-field__aligned");
            dropdown.SetChoices(keys);
            dropdown.ValueChanged += OnSearchableDropdownChanged;

            Add(dropdown);
        }

        private static void OnSearchableDropdownChanged(VisualElement visualElement, string oldValue, string newValue)
        {
            if (visualElement is SearchableDropdown<string> dropdown)
            {
                var tuple = ((Action<Type>, List<Type>))dropdown.userData;
                var newVal = tuple.Item2[dropdown.Index];
                Debug.Log("Applied " + newVal);
                tuple.Item1.Invoke(newVal);
            }
        }
    }
}
