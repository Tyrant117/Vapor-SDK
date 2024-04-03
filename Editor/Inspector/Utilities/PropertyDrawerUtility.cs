using System;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace VaporInspectorEditor
{
    public static class PropertyDrawerUtility
    {
        public static VisualElement CreatePropertyDrawerFromObject<T>(Object obj, string propertyPath) where T : PropertyDrawer
        {
            var so = new SerializedObject(obj);
            var prop = so.FindProperty(propertyPath);
            return CreatePropertyDrawerFromSerializedProperty<T>(prop);
        }

        public static VisualElement CreatePropertyDrawerFromSerializedProperty<T>(SerializedProperty serializedProperty) where T : PropertyDrawer
        {
            var drawer = Activator.CreateInstance<T>();
            return drawer.CreatePropertyGUI(serializedProperty);
        }
    }
}
