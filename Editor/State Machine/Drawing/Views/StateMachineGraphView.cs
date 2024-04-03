using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VaporStateMachine;
using Object = UnityEngine.Object;

namespace VaporStateMachineEditor
{
    public class StateMachineGraphView : GraphView
    {
        private readonly MethodInfo m_UndoRedoPerformedMethodInfo;

        public Vector2 CachedMousePosition { get; private set; }
        public GraphEditorView View { get; }
        public StateMachineGraphControllerSo Graph { get; private set; }

        public StateMachineGraphView()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/StateMachineGraphView"));
            serializeGraphElements = SerializeGraphElementsImplementation;
            canPasteSerializedData = CanPasteSerializedDataImplementation;
            unserializeAndPaste = UnserializeAndPasteImplementation;
            deleteSelection = DeleteSelectionImplementation;
            elementsInsertedToStackNode = ElementsInsertedToStackNode;
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);

            this.viewTransformChanged += OnTransformChanged;

            // Get reference to GraphView assembly
            Assembly graphViewAssembly = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var assemblyName = assembly.GetName().ToString();
                if (assemblyName.Contains("GraphView"))
                {
                    graphViewAssembly = assembly;
                }
            }

            Type graphViewType = graphViewAssembly?.GetType("UnityEditor.Experimental.GraphView.GraphView");
            // Cache the method info for this function to be used through application lifetime
            m_UndoRedoPerformedMethodInfo = graphViewType?.GetMethod("UndoRedoPerformed",
                BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new Type[] { },
                null);
        }

        public StateMachineGraphView(GraphEditorView graphView, StateMachineGraphControllerSo graph) : this()
        {
            View = graphView;
            Graph = graph;
        }

        // GraphView has a bug where the viewTransform will be reset to default when swapping between two
        // GraphViewEditor windows of the same type. This is a hack to prevent that from happening w/as little
        // halo as possible.
        Vector3 lkgPosition;
        Vector3 lkgScale;
        private void OnTransformChanged(GraphView graphView)
        {
            if (!graphView.viewTransform.position.Equals(Vector3.zero))
            {
                lkgPosition = graphView.viewTransform.position;
                lkgScale = graphView.viewTransform.scale;
            }
            else if (!lkgPosition.Equals(Vector3.zero))
            {
                graphView.UpdateViewTransform(lkgPosition, lkgScale);
            }
        }

        #region - Nodes -
        private void ElementsInsertedToStackNode(StackNode stackNode, int insertIndex, IEnumerable<GraphElement> elements)
        {
            //var contextView = stackNode as ContextView;
            //contextView.InsertElements(insertIndex, elements);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var allPorts = new List<Port>();
            var ports = new List<Port>();

            foreach (var node in View.EditorNodes)
            {
                allPorts.AddRange(node.Ports);
            }

            foreach (var p in allPorts)
            {
                if (p == startPort) { continue; }
                if (p.node == startPort.node) { continue; }
                if (p.direction == startPort.direction) { continue; }
                if (p.portType == startPort.portType)
                {
                    ports.Add(p);
                }

            }

            return ports;
        }
        #endregion

        #region - Drag and Drop -
        private void OnMouseMoveEvent(MouseMoveEvent evt)
        {
            CachedMousePosition = evt.mousePosition;
        }

        private bool ValidateObjectForDrop(Object obj)
        {
            return EditorUtility.IsPersistent(obj) && (
                obj is Texture2D ||
                obj is Cubemap ||
                //obj is SubGraphAsset asset && !asset.descendents.Contains(graph.assetGuid) && asset.assetGuid != graph.assetGuid ||
                obj is Texture2DArray ||
                obj is Texture3D);
        }

        private void OnDragUpdatedEvent(DragUpdatedEvent e)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
            bool dragging = false;
            if (selection != null)
            {
                //var anyCategoriesInSelection = selection.OfType<SGBlackboardCategory>();
                //if (!anyCategoriesInSelection.Any())
                //{
                //    // Blackboard items
                //    bool validFields = false;
                //    foreach (SGBlackboardField propertyView in selection.OfType<SGBlackboardField>())
                //    {
                //        if (!(propertyView.userData is MultiJsonInternal.UnknownShaderPropertyType))
                //            validFields = true;
                //    }

                //    dragging = validFields;
                //}
                //else
                //{
                //    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                //}
            }
            else
            {
                // Handle unity objects
                var objects = DragAndDrop.objectReferences;
                foreach (Object obj in objects)
                {
                    if (ValidateObjectForDrop(obj))
                    {
                        dragging = true;
                        break;
                    }
                }
            }

            if (dragging)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            }
        }

        // Contrary to the name this actually handles when the drop operation is performed
        private void OnDragPerformEvent(DragPerformEvent e)
        {
            Vector2 localPos = (e.currentTarget as VisualElement).ChangeCoordinatesTo(contentViewContainer, e.localMousePosition);

            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
            if (selection != null)
            {
                // Blackboard
                //if (selection.OfType<SGBlackboardField>().Any())
                //{
                //    IEnumerable<SGBlackboardField> fields = selection.OfType<SGBlackboardField>();
                //    foreach (SGBlackboardField field in fields)
                //    {
                //        CreateNode(field, localPos);
                //    }

                //    // Call this delegate so blackboard can respond to blackboard field being dropped
                //    blackboardFieldDropDelegate?.Invoke();
                //}
            }
            else
            {
                // Handle unity objects
                var objects = DragAndDrop.objectReferences;
                foreach (Object obj in objects)
                {
                    if (ValidateObjectForDrop(obj))
                    {
                        //CreateNode(obj, localPos);
                    }
                }
            }
        }
        #endregion

        #region - Context Actions -
        private string SerializeGraphElementsImplementation(IEnumerable<GraphElement> elements)
        {
            return string.Empty;
        }

        private bool CanPasteSerializedDataImplementation(string data)
        {
            return false;
        }

        private void UnserializeAndPasteImplementation(string operationName, string data)
        {

        }

        private void DeleteSelectionImplementation(string operationName, AskUser askUser)
        {

        }
        #endregion
    }
}
