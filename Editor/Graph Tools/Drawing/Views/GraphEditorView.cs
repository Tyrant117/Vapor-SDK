using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VaporGraphTools;
using Object = UnityEngine.Object;

namespace VaporGraphToolsEditor
{
    public abstract class GraphEditorView<T> : VisualElement, IDisposable where T : ScriptableObject
    {
        public GraphToolsEditWindow<T> Window { get; set; }

        public GraphView GraphView { get; set; }

        public T Graph { get; set; }
        public List<NodeSo> Nodes { get; set; }

        public string AssetName { get; set; }

        public Action SaveRequested { get; set; }
        public Action ShowInProjectRequested { get; set; }
        public List<IGraphToolsNode> EditorNodes { get; } = new();

        public GraphEditorView(GraphToolsEditWindow<T> editWindow, T graphObject, List<NodeSo> nodeObjects, string graphName)
        {
            Debug.Log("Nodes Set");
            Window = editWindow;
            Graph = graphObject;
            Nodes = nodeObjects;
            AssetName = graphName;

            name = "GraphEditorView";
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/GraphToolsEditorView"));

            CreateToolbar();
            CreateView();
        }

        private void CreateToolbar()
        {
            var toolbar = new IMGUIContainer(() =>
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                if (GUILayout.Button("Save Asset", EditorStyles.toolbarButton))
                {
                    SaveRequested?.Invoke();
                }
                if (GUILayout.Button("Show In Project", EditorStyles.toolbarButton))
                {
                    ShowInProjectRequested?.Invoke();
                }

                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();
                //m_UserViewSettings.isBlackboardVisible = GUILayout.Toggle(m_UserViewSettings.isBlackboardVisible, "Blackboard", EditorStyles.toolbarButton);

                //m_UserViewSettings.isInspectorVisible = GUILayout.Toggle(m_UserViewSettings.isInspectorVisible, "Graph Inspector", EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                {
                    //UserViewSettingsChangeCheck(newColorIndex);
                }
                GUILayout.EndHorizontal();
            });
            Add(toolbar);
        }

        protected abstract void CreateView();

        public virtual void Dispose()
        {
            if (GraphView != null)
            {
                SaveRequested = null;
                ShowInProjectRequested = null;
                foreach (var edge in GraphView.Query<Edge>().ToList())
                {
                    edge.output = null;
                    edge.input = null;
                }

                GraphView.nodeCreationRequest = null;
                GraphView = null;
                Debug.Log("Nodes Disposed");
                Nodes = null;
            }
        }

        public abstract void AddNode(ScriptableObject node);
    }
}
