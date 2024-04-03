using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using VaporGraphTools;
using VaporGraphToolsEditor.Math;

namespace VaporGraphToolsEditor
{
    public static class MathNodeDrawerHelper
    {
        public static bool AddNodes<GraphArg>(ScriptableObject node, GraphEditorView<GraphArg> editorView, GraphView graphView, List<IGraphToolsNode> editorNodes, List<NodeSo> refNodes) where GraphArg : ScriptableObject
        {
            if (node is MathEvaluateNodeSo resultNode)
            {
                var editorNode = new EditorMathEvaluateNode<GraphArg>(editorView, resultNode);
                editorNode.SetPosition(resultNode.Position);
                graphView.AddElement(editorNode);
                editorNodes.Add(editorNode);
                refNodes?.Add(resultNode);
                return true;
            }

            if (node is MathGraphNodeSo mathGraphNode)
            {
                var editorNode = new EditorMathGraphNode<GraphArg>(editorView, mathGraphNode);
                editorNode.SetPosition(mathGraphNode.Position);
                graphView.AddElement(editorNode);
                editorNodes.Add(editorNode);
                refNodes?.Add(mathGraphNode);
                return true;
            }

            if (node is MathNodeSo mathNode)
            {
                var editorNode = new NParamEditorNode<GraphArg>(editorView, mathNode, typeof(float));
                editorNode.SetPosition(mathNode.Position);
                graphView.AddElement(editorNode);
                editorNodes.Add(editorNode);
                refNodes?.Add(mathNode);
                return true;
            }
            return false;
        }
    }
}
