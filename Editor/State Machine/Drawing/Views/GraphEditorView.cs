using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VaporGraphTools;
using VaporGraphToolsEditor;
using VaporStateMachine;

namespace VaporStateMachineEditor
{
    public class GraphEditorView : GraphEditorView<StateMachineGraphControllerSo>
    {
        private SearchProvider _searchProvider;

        public List<StateMachineNode> EditorNodes { get; } = new();

        public GraphEditorView(GraphToolsEditWindow<StateMachineGraphControllerSo> editWindow, StateMachineGraphControllerSo graphObject, List<NodeSo> nodeObjects, string graphName) : base(editWindow, graphObject, nodeObjects, graphName)
        {
            
        }

        protected override void CreateView()
        {
            var content = new VisualElement { name = "content" };
            {
                GraphView = new StateMachineGraphView(this, Graph)
                { name = "GraphView", viewDataKey = $"{nameof(StateMachineGraphView)}" };
                GraphView.SetupZoom(0.05f, 8);
                GraphView.AddManipulator(new ContentDragger());
                GraphView.AddManipulator(new SelectionDragger());
                GraphView.AddManipulator(new RectangleSelector());
                GraphView.AddManipulator(new ClickSelector());
                //_graphView.RegisterCallback<KeyDownEvent>(OnKeyDown);
                // Bugfix 1312222. Running 'ResetSelectedBlockNodes' on all mouse up interactions will break selection
                // after changing tabs. This was originally added to fix a bug with middle-mouse clicking while dragging a block node.
                //_graphView.RegisterCallback<MouseUpEvent>(evt => { if (evt.button == (int)MouseButton.MiddleMouse) _graphView.ResetSelectedBlockNodes(); });
                // This takes care of when a property is dragged from BB and then the drag is ended by the Escape key, hides the scroll boundary regions and drag indicator if so
                //_graphView.RegisterCallback<DragExitedEvent>(evt =>
                //{
                //    blackboardController.blackboard.OnDragExitedEvent(evt);
                //    blackboardController.blackboard.hideDragIndicatorAction?.Invoke();
                //});

                //RegisterGraphViewCallbacks();
                content.Add(GraphView);

                //string serializedWindowLayout = EditorUserSettings.GetConfigValue(k_FloatingWindowsLayoutKey);
                //if (!string.IsNullOrEmpty(serializedWindowLayout))
                //{
                //    m_FloatingWindowsLayout = JsonUtility.FromJson<FloatingWindowsLayout>(serializedWindowLayout);
                //}

                //CreateInspector();
                //CreateBlackboard();

                //UpdateSubWindowsVisibility();

                GraphView.graphViewChanged = GraphViewChanged;

                //RegisterCallback<GeometryChangedEvent>(ApplySerializedWindowLayouts);
            }

            _searchProvider = ScriptableObject.CreateInstance<SearchProvider>();
            _searchProvider.View = this;
            GraphView.nodeCreationRequest = NodeCreationRequest;

            AddNodes(Nodes);
            AddEdges();
            Add(content);
        }

        private void NodeCreationRequest(NodeCreationContext context)
        {
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchProvider);
        }

        private void AddNodes(List<NodeSo> nodes)
        {
            Debug.Log($"Adding Nodes: {nodes.Count}");
            foreach (var node in nodes)
            {
                if (node is StateNodeSo stateNode)
                {
                    var editorNode = new StateMachineNode(this, stateNode);
                    editorNode.SetPosition(stateNode.Position);
                    GraphView.AddElement(editorNode);
                    EditorNodes.Add(editorNode);
                }
            }
        }

        public override void AddNode(ScriptableObject node)
        {
            Undo.RegisterCreatedObjectUndo(node, "Create Scriptable Object Node");
            if (node is StateNodeSo stateNode)
            {
                Debug.Log($"Adding Node: {stateNode}");
                var editorNode = new StateMachineNode(this, stateNode);
                editorNode.SetPosition(stateNode.Position);
                GraphView.AddElement(editorNode);
                Nodes.Add(stateNode);
                EditorNodes.Add(editorNode);
            }
        }

        private void AddEdges()
        {

        }

        private void AddEdge(Edge edge)
        {
            var inNode = (StateMachineNode)edge.input.node;
            int inPortIndex = inNode.Ports.IndexOf(edge.input);

            var outNode = (StateMachineNode)edge.output.node;
            int outPortIndex = outNode.Ports.IndexOf(edge.output);

            //var conn = new EffectGraphConnection(inNode.Node.Id, inPortIndex, outNode.Node.Id, outPortIndex);

            //Window.CurrentGraph.Connections.Add(conn);

            //ConnectionMap[edge] = conn;
        }        

        private GraphViewChange GraphViewChanged(GraphViewChange graphViewChange)
        {
            _CreateEdges(graphViewChange);
            _MoveElements(graphViewChange);
            _RemoveElements(graphViewChange);            

            //UpdateEdgeColors(nodesToUpdate);

            Window.MarkDirty();
            return graphViewChange;

            void _CreateEdges(GraphViewChange graphViewChange)
            {
                if (graphViewChange.edgesToCreate != null)
                {
                    foreach (var edge in graphViewChange.edgesToCreate)
                    {
                        AddEdge(edge);
                    }
                    //graphViewChange.edgesToCreate.Clear();
                }
            }

            void _MoveElements(GraphViewChange graphViewChange)
            {
                if (graphViewChange.movedElements != null)
                {
                    foreach (var element in graphViewChange.movedElements)
                    {
                        if (element is StateMachineNode node)
                        {
                            node.Node.Position = element.parent.ChangeCoordinatesTo(GraphView.contentViewContainer, element.GetPosition());
                        }

                        if (element is StickyNote stickyNote)
                        {
                            //SetStickyNotePosition(stickyNote);
                        }
                    }
                }
            }

            static void _RemoveElements(GraphViewChange graphViewChange)
            {
                if (graphViewChange.elementsToRemove != null)
                {
                    foreach (var edge in graphViewChange.elementsToRemove.OfType<Edge>())
                    {
                        if (edge.input != null)
                        {

                        }
                        if (edge.output != null)
                        {

                        }
                    }
                }
            }
        }
    }
}
