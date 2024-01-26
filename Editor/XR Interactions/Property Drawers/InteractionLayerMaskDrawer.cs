using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using VaporXR;
using MaskField = UnityEditor.UIElements.MaskField;

namespace VaporXREditor
{
    /// <summary>
    /// Class used to draw an <see cref="InteractionLayerMask"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(InteractionLayerMask))]
    public class InteractionLayerMaskDrawer : PropertyDrawer
    {
        private static readonly List<string> s_DisplayOptions = new();
        private static readonly List<int> s_ValueOptions = new();

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var maskProperty = property.FindPropertyRelative("_bits");
            s_DisplayOptions.Clear();
            s_ValueOptions.Clear();
            
            
            InteractionLayerSettings.Instance.GetLayerNamesAndValues(s_DisplayOptions, s_ValueOptions);
            var ve = new MaskField("Interaction Layers", s_DisplayOptions, (int)maskProperty.uintValue)
            {
                userData = maskProperty
            };
            ve.AddToClassList(BaseField<MaskField>.alignedFieldUssClassName);
            ve.RegisterValueChangedCallback(OnMaskChanged);
            return ve;
        }

        private static void OnMaskChanged(ChangeEvent<int> evt)
        {
            var mask = (MaskField)evt.target;
            if (mask.userData is not SerializedProperty maskProp) return;
            
            maskProp.uintValue = (uint)evt.newValue;
            maskProp.serializedObject.ApplyModifiedProperties();
        }
    }
}
