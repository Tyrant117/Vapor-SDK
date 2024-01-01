using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Vapor.ObjectLogging;

namespace VaporEditor
{
    [CustomPropertyDrawer(typeof(RichStringLog))]
    public class RichStringLogDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                }
            };

            var type = property.FindPropertyRelative("Type");
            var content = property.FindPropertyRelative("Content");
            property.FindPropertyRelative("StackTrace");
            property.FindPropertyRelative("TimeStamp");

            var texture = type.intValue switch
            {
                0 => EditorGUIUtility.IconContent("console.infoicon.sml@2x").image,
                1 => EditorGUIUtility.IconContent("console.warnicon.sml@2x").image,
                2 => EditorGUIUtility.IconContent("console.erroricon.sml@2x").image,
                _ => null
            };
            
            var image = new Image
            {
                image = texture,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
                style =
                {
                    marginLeft = 3,
                    marginRight = 3,
                    marginTop = 1,
                    marginBottom = 1,
                    flexShrink = 0,
                }
            };
            var label = new Label(content.stringValue)
            {
                style =
                {
                    paddingTop = 6,
                    paddingBottom = 6,
                    marginRight = 3,
                    flexGrow = 1,
                    flexShrink = 1,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    whiteSpace = WhiteSpace.Normal,
                }
            };
            row.Add(image);
            row.Add(label);
            return row;
        }
    }
}
