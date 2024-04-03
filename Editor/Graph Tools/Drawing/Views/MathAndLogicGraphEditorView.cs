using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VaporGraphTools;
using VaporGraphTools.Math;
using VaporGraphToolsEditor;
using VaporGraphToolsEditor.Math;
using VaporKeys;

namespace VaporGraphToolsEditor
{
    public class MathAndLogicGraphEditorView<SearchProviderArg, GraphViewArg, GraphArg> : GraphEditorView<GraphArg> where SearchProviderArg : GraphToolsSearchProvider<GraphArg> where GraphViewArg : GraphToolsGraphView<GraphArg> where GraphArg : ScriptableObject
    {
        private SearchProviderArg _searchProvider;

        public MathAndLogicGraphEditorView(GraphToolsEditWindow<GraphArg> editWindow, GraphArg graphObject, List<NodeSo> nodeObjects, string graphName) : base(editWindow, graphObject, nodeObjects, graphName)
        {
        }

        protected override void CreateView()
        {
            var content = new VisualElement { name = "content" };
            {
                // Parameters for the constructor
                object[] constructorParams = { this, Graph };

                // Create an instance using Activator.CreateInstance
                object viewInstance = Activator.CreateInstance(typeof(GraphViewArg), constructorParams);

                // Cast the instance to MathGraphView
                GraphView = (GraphViewArg)viewInstance;
                GraphView.name = "GraphView";
                GraphView.viewDataKey = $"{nameof(GraphViewArg)}";
                GraphView.SetupZoom(0.05f, 8);
                GraphView.AddManipulator(new ContentDragger());
                GraphView.AddManipulator(new SelectionDragger());
                GraphView.AddManipulator(new RectangleSelector());
                GraphView.AddManipulator(new ClickSelector());

                content.Add(GraphView);

                GraphView.graphViewChanged = GraphViewChanged;
            }

            _searchProvider = ScriptableObject.CreateInstance<SearchProviderArg>();
            _searchProvider.SetupIncludes();
            _searchProvider.IncludeFlags.Add("math");
            _searchProvider.IncludeFlags.Add("logic");
            _searchProvider.IncludeFlags.Add("values");
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
                if (AddAdditionalNodes(node))
                    continue;

                if (ValueNodeDrawerHelper.AddNodes(node, this, GraphView, EditorNodes, null))
                    continue;
                if (MathNodeDrawerHelper.AddNodes(node, this, GraphView, EditorNodes, null))
                    continue;
                if (LogicNodeDrawerHelper.AddNodes(node, this, GraphView, EditorNodes, null))
                    continue;
                if (NParamNodeDrawerHelper.AddNodes(node, this, GraphView, EditorNodes, null))
                    continue;
            }
        }

        protected virtual bool AddAdditionalNodes(NodeSo node)
        {
            return false;
        }

        public override void AddNode(ScriptableObject node)
        {
            Undo.RegisterCreatedObjectUndo(node, "Create Scriptable Object Node");

            if (AddSpecialNode(node))
                return;

            if (ValueNodeDrawerHelper.AddNodes(node, this, GraphView, EditorNodes, Nodes))
                return;
            if (MathNodeDrawerHelper.AddNodes(node, this, GraphView, EditorNodes, Nodes))
                return;
            if (LogicNodeDrawerHelper.AddNodes(node, this, GraphView, EditorNodes, Nodes))
                return;
            if (NParamNodeDrawerHelper.AddNodes(node, this, GraphView, EditorNodes, Nodes))
                return;
        }

        protected virtual bool AddSpecialNode(ScriptableObject node)
        {
            return false;
        }

        private void AddEdges()
        {
            foreach (var root in EditorNodes)
            {
                var edges = root.GetNode().Edges;
                foreach (var edge in edges)
                {
                    var child = EditorNodes.FirstOrDefault(x => edge == x.GetNode().GetGuid());
                    if (child != null)
                    {
                        var e = child.Ports[edge.ConnectedPortIndex].ConnectTo(root.Ports[edge.PortIndex]);
                        GraphView.AddElement(e);
                    }
                }
            }
        }

        private void AddEdge(Edge edge)
        {
            var inNode = (IGraphToolsNode)edge.input.node;
            int inPortIndex = inNode.Ports.IndexOf(edge.input);

            var outNode = (IGraphToolsNode)edge.output.node;
            int outPortIndex = outNode.Ports.IndexOf(edge.output);

            if (inNode != null && outNode != null)
            {
                var root = inNode.GetNode();
                var child = outNode.GetNode();
                root.Edges.Add(new EdgeConnection(inPortIndex, outPortIndex, child.GetGuid()));
            }

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
                        if (element is IGraphToolsNode node)
                        {
                            node.GetNode().Position = element.parent.ChangeCoordinatesTo(GraphView.contentViewContainer, element.GetPosition());
                        }

                        if (element is StickyNote stickyNote)
                        {
                            //SetStickyNotePosition(stickyNote);
                        }
                    }
                }
            }

            void _RemoveElements(GraphViewChange graphViewChange)
            {
                if (graphViewChange.elementsToRemove != null)
                {
                    foreach (var edge in graphViewChange.elementsToRemove.OfType<Edge>())
                    {
                        if (edge.input != null && edge.output != null && edge.input.node is IGraphToolsNode inN && edge.output.node is IGraphToolsNode outN)
                        {
                            if (inN.GetNode() is NodeSo root && outN.GetNode() is NodeSo child)
                            {
                                var idx = root.Edges.FindIndex(x => x == child.GetGuid());
                                if (idx != -1)
                                {
                                    root.Edges.RemoveAt(idx);
                                }
                            }
                        }
                    }

                    foreach (var node in graphViewChange.elementsToRemove.OfType<IGraphToolsNode>())
                    {
                        Debug.Log("Removing Node" + node);
                        EditorNodes.Remove(node);
                        Nodes.Remove(node.GetNode());
                    }
                }
            }
        }
    }
}
