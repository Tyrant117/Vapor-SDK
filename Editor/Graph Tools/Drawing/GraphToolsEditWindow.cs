using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VaporEditor;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{
    public abstract class GraphToolsEditWindow<T> : EditorWindow where T : ScriptableObject
    {
        public static void Open<W>(string assetGuid) where W : GraphToolsEditWindow<T>
        {
            var window = GetWindow<W>("", true, typeof(SceneView));
            window.Initialize(assetGuid);
        }

        [SerializeField]
        private string _selectedGuid;
        public string SelectedGuid { get => _selectedGuid; set => _selectedGuid = value; }

        [SerializeField]
        private GraphEditorView<T> _graphEditorView;
        public GraphEditorView<T> GraphEditorView
        {
            get => _graphEditorView;
            protected set
            {
                if (_graphEditorView != null)
                {
                    _graphEditorView.RemoveFromHierarchy();
                    _graphEditorView.Dispose();
                }

                _graphEditorView = value;

                if (_graphEditorView != null)
                {
                    _graphEditorView.SaveRequested += () => SaveAsset();
                    _graphEditorView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                    _frameAllAfterLayout = true;
                    rootVisualElement.Add(_graphEditorView);
                }
            }
        }

        [SerializeField]
        private T _graphObject;
        public T GraphObject
        {
            get => _graphObject;
            set
            {
                if (_graphObject != null)
                {
                    DestroyImmediate(_graphObject);
                }

                _graphObject = value;
            }
        }

        public List<NodeSo> _nodeObjects;
        public List<NodeSo> NodeObjects => _nodeObjects;

        public string AssetName
        {
            get { return titleContent.text; }
        }

        [NonSerialized]
        private bool _frameAllAfterLayout;

        public void Initialize(string assetGuid)
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(assetGuid));
            if (asset == null)
            {
                Debug.Log("Initialized: Asset null");
                return;
            }

            if (!EditorUtility.IsPersistent(asset))
            {
                Debug.Log("Initialized: Asset not peristant");
                return;
            }

            if (SelectedGuid == assetGuid)
            {
                Debug.Log($"Initialized: Already initialized: {asset.name}");
                return;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            SelectedGuid = assetGuid;
            string graphName = Path.GetFileNameWithoutExtension(path);

            GraphObject = CreateInstance<T>();
            GraphObject.hideFlags = HideFlags.HideAndDontSave;
            EditorUtility.CopySerialized(asset, GraphObject);
            _nodeObjects = new(SubAssetUtility.CloneAllSubAssets<NodeSo>(asset));

            CreateEditorView(asset, graphName);

            UpdateTitle();

            Repaint();
        }

        protected abstract void CreateEditorView(T mainAsset, string graphName);

        private void CreateGUI()
        {
            if (GraphObject == null && SelectedGuid != null)
            {
                var guid = SelectedGuid;
                SelectedGuid = null;
                Initialize(guid);
            }
        }

        private void OnEnable()
        {
            this.SetAntiAliasing(4);
        }

        private void OnDisable()
        {
            _graphEditorView?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _graphEditorView?.Dispose();

            _graphEditorView = null;
            _graphObject = null;

            Resources.UnloadUnusedAssets();
        }

        public void UpdateTitle()
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(SelectedGuid);
            string assetName = Path.GetFileNameWithoutExtension(assetPath);

            // update blackboard title (before we add suffixes)
            if (GraphEditorView != null)
            {
                GraphEditorView.AssetName = assetName;
            }

            // build the window title (with suffixes)
            string title = assetName;
            if (EditorUtility.IsDirty(GraphObject))
            {
                hasUnsavedChanges = true;
                // This is the message EditorWindow will show when prompting to close while dirty
                saveChangesMessage = GetSaveChangesMessage();
            }
            else
            {
                hasUnsavedChanges = false;
                saveChangesMessage = "";
            }
            if (!AssetFileExists())
            {
                title = title + " (deleted)";
            }

            // get window icon
            Texture2D icon;
            {
                string theme = EditorGUIUtility.isProSkin ? "_dark" : "_light";
                icon = Resources.Load<Texture2D>("Icons/sg_graph_icon_gray" + theme);
            }

            titleContent = new GUIContent(title, icon);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (GraphEditorView == null)
            {
                return;
            }

            // this callback is only so we can run post-layout behaviors after the graph loads for the first time
            // we immediately unregister it so it doesn't get called again
            GraphEditorView.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            if (_frameAllAfterLayout)
            {
                GraphEditorView?.GraphView?.FrameAll();
            }

            _frameAllAfterLayout = false;
        }

        public bool SaveAsset()
        {
            bool saved = false;
            if (SelectedGuid != null && GraphObject != null)
            {
                Debug.Log("Save Called");
                var mainAsset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(SelectedGuid));
                OnBeforeSave(mainAsset);
                NodeUtility.SaveNodeGraphRepresentation(mainAsset, GraphObject, GraphEditorView.Nodes, x => OnTraverse(GraphObject, x));
                OnAfterSave(mainAsset);

                EditorUtility.ClearDirty(GraphObject);
                hasUnsavedChanges = false;
                saved = true;
            }

            UpdateTitle();

            return saved;
        }

        protected virtual void OnBeforeSave(T mainAsset) { }
        protected virtual void OnTraverse(T mainAsset, NodeSo node) { }
        protected virtual void OnAfterSave(T mainAsset) { }

        #region - Helpers -
        private bool AssetFileExists()
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(SelectedGuid);
            return File.Exists(assetPath);
        }

        private string GetSaveChangesMessage()
        {
            return "Do you want to save the changes you made in the Graph?\n\n" +
                AssetDatabase.GUIDToAssetPath(SelectedGuid) +
                "\n\nYour changes will be lost if you don't save them.";
        }

        public void MarkDirty()
        {
            EditorUtility.SetDirty(GraphObject);
            hasUnsavedChanges = true;
        }
        #endregion
    }
}
