using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{
    public static class ValueNodeDrawerHelper
    {
        public static bool AddNodes<GraphArg>(ScriptableObject node, GraphEditorView<GraphArg> editorView, GraphView graphView, List<IGraphToolsNode> editorNodes, List<NodeSo> refNodes) where GraphArg : ScriptableObject
        {
            switch (node)
            {
                case FloatValueNodeSo floatNode:
                    {
                        var editorNode = new EditorFloatPropertyNode<GraphArg>(editorView, floatNode);
                        editorNode.SetPosition(floatNode.Position);
                        graphView.AddElement(editorNode);
                        editorNodes.Add(editorNode);
                        refNodes?.Add(floatNode);
                        return true;
                    }

                case IntValueNodeSo intNode:
                    {
                        var editorNode = new EditorIntPropertyNode<GraphArg>(editorView, intNode);
                        editorNode.SetPosition(intNode.Position);
                        graphView.AddElement(editorNode);
                        editorNodes.Add(editorNode);
                        refNodes?.Add(intNode);
                        return true;
                    }

                case BoolValueNodeSo boolNode:
                    {
                        var editorNode = new EditorBoolPropertyNode<GraphArg>(editorView, boolNode, typeof(bool));
                        editorNode.SetPosition(boolNode.Position);
                        graphView.AddElement(editorNode);
                        editorNodes.Add(editorNode);
                        refNodes?.Add(boolNode);
                        return true;
                    }

                case ExposedPropertyNodeSo propertyNode:
                    {
                        var editorNode = new ExposedPropertyEditorToken<GraphArg>(editorView, propertyNode, default, null);
                        editorNode.SetPosition(propertyNode.Position);
                        graphView.AddElement(editorNode);
                        editorNodes.Add(editorNode);
                        refNodes?.Add(propertyNode);
                        return true;
                    }

                case TimeValueNodeSo timeNode:
                    {
                        var editorNode = new EditorLabelPropertyNode<GraphArg, TimeValueNodeSo, float>(editorView, timeNode, typeof(float));
                        editorNode.SetPosition(timeNode.Position);
                        graphView.AddElement(editorNode);
                        editorNodes.Add(editorNode);
                        refNodes?.Add(timeNode);
                        return true;
                    }

                case DeltaTimeValueNodeSo deltaTimeNode:
                    {
                        var editorNode = new EditorLabelPropertyNode<GraphArg, DeltaTimeValueNodeSo, float>(editorView, deltaTimeNode, typeof(float));
                        editorNode.SetPosition(deltaTimeNode.Position);
                        graphView.AddElement(editorNode);
                        editorNodes.Add(editorNode);
                        refNodes?.Add(deltaTimeNode);
                        return true;
                    }

                case FixedDeltaTimeValueNodeSo fixedDeltaTimeNode:
                    {
                        var editorNode = new EditorLabelPropertyNode<GraphArg, FixedDeltaTimeValueNodeSo, float>(editorView, fixedDeltaTimeNode, typeof(float));
                        editorNode.SetPosition(fixedDeltaTimeNode.Position);
                        graphView.AddElement(editorNode);
                        editorNodes.Add(editorNode);
                        refNodes?.Add(fixedDeltaTimeNode);
                        return true;
                    }
            }
            return false;
        }
    }
}
