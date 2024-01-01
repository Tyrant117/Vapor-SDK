using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using Vapor.ObjectLogging;

namespace VaporEditor
{
    [CustomPropertyDrawer(typeof(ObjectLogger))]
    public class ObjectLoggerDrawer : PropertyDrawer
    {
       
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var logProps = property.FindPropertyRelative("_logs");
            
            var box = new VisualElement();
            var toolbar = new Toolbar()
            {
                style =
                {
                    borderTopWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    paddingLeft = 3,
                    justifyContent = Justify.FlexEnd
                }
            };
            toolbar.Add(new Label("Logs")
            {
                style =
                {
                    flexGrow = 1,
                    unityTextAlign = TextAnchor.MiddleLeft,
                }
            });
            var clearButton = new ToolbarButton()
            {
                text = "Clear",
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };
            toolbar.Add(clearButton);
            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(new ToolbarSearchField()
            {
                style =
                {
                    maxWidth = 300,
                }
            });
            var infoImage = new Image
            {
                image = EditorGUIUtility.IconContent("console.infoicon.sml").image,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            var warningImage = new Image
            {
                image = EditorGUIUtility.IconContent("console.warnicon.sml").image,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            var errorImage = new Image
            {
                image = EditorGUIUtility.IconContent("console.erroricon.sml").image,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            var infoToggle = new Toggle
            {
                focusable = false
            };
            infoToggle.RemoveFromClassList(Toggle.ussClassName);
            infoToggle.AddToClassList(ToolbarToggle.ussClassName);
            infoToggle.TrackPropertyValue(property.FindPropertyRelative("_infoCount"), x => OnInfoCountChanged(x, infoToggle));
            infoToggle.Add(infoImage);
            infoToggle.Add(new TextElement()
            {
                text = property.FindPropertyRelative("_infoCount").intValue.ToString(),
                style =
                {
                    marginLeft = 2,
                    marginRight = 2,
                    justifyContent = Justify.SpaceAround,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            });
            var warningToggle = new ToolbarToggle();
            warningToggle.Add(warningImage);
            var errorToggle = new ToolbarToggle();
            errorToggle.Add(errorImage);
            toolbar.Add(infoToggle);
            toolbar.Add(warningToggle);
            toolbar.Add(errorToggle);
            box.Add(toolbar);
            
            // var sv = new ScrollView(ScrollViewMode.Vertical);
            var lv = new ListView(null)
            {
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                showAddRemoveFooter = false,
                showBorder = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                reorderable = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.Single,
                style =
                {
                    maxHeight = 252,
                }
            };
            lv.BindProperty(logProps);

            box.Add(lv);

            var stackTraceView = new ScrollView();
            var stLabel = new Label("")
            {
                focusable = true,
            };
            stLabel.RegisterCallback<PointerDownLinkTagEvent>(OnLinkClicked);
            clearButton.clickable = new Clickable(() => Clear(property, lv));
            // var imguiLabel = new IMGUIContainer(() => OnDrawImGuiLabel(property, lv));
            
            stackTraceView.Add(stLabel);
            // stackTraceView.Add(imguiLabel);
            
            box.Add(stackTraceView);
            
            lv.selectionChanged += x => OnListSelectionChanged(stLabel, x);
            return box;
        }

        private static void OnInfoCountChanged(SerializedProperty property, Toggle infoToggle)
        {
            int newValue = property.intValue;
            infoToggle.Q<TextElement>().text = newValue.ToString();
        }

        private static void OnLinkClicked(PointerDownLinkTagEvent evt)
        {
            Debug.Log(evt.linkID);
            var split = evt.linkID.Split('%');
            var path = split[0];
            if (!int.TryParse(split[1], out var line))
            {
                line = 1;
            }
            if (!int.TryParse(split[2], out var column))
            {
                column = 0;
            }

            var packageName = "";
            if (split.Length == 4)
            {
                packageName = split[3];
            }
            
            var formatFfp = path.Replace('\\', '/');
            if (formatFfp.StartsWith(Application.dataPath))
            {
                var relPath = "Assets" + path[Application.dataPath.Length..];
                var csPath = AssetDatabase.LoadMainAssetAtPath(relPath);
                if (!AssetDatabase.OpenAsset(csPath, line, column))
                {
                    Debug.Log($"Could Not Open File: {relPath}");
                }
            }
            else if (formatFfp.StartsWith("./Packages"))
            {
                var rootDirPath = formatFfp[11..];
                var firstDirIndex = rootDirPath.IndexOf('/');
                var relPath = $"Packages/{packageName}{rootDirPath[firstDirIndex..]}";
                var csPath = AssetDatabase.LoadMainAssetAtPath(relPath);
                if (!AssetDatabase.OpenAsset(csPath, line, column))
                {
                    Debug.Log($"Could Not Open File: {relPath}");
                }
            }
            else
            {
                Debug.Log($"Could Find File: {formatFfp}");
            }
        }
        
        private static void OnListSelectionChanged(TextElement label, IEnumerable<object> obj)
        {
            var enumerable = obj.ToList();
            if (enumerable.Count == 0)
            {
                label.text = "";
            }
            
            foreach (var o in enumerable)
            {
                if (o is SerializedProperty { boxedValue: RichStringLog log })
                {
                    label.text = log.StackTrace;
                }
            }
        }

        private static void Clear(SerializedProperty property, BaseVerticalCollectionView lv)
        {
            property.FindPropertyRelative("_logs").arraySize = 0;
            property.serializedObject.ApplyModifiedProperties();
            lv.ClearSelection();
        }
    }
}
