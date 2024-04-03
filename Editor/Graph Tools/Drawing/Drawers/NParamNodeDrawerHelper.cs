using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{
    public static class NParamNodeDrawerHelper
    {
        public static bool AddNodes<GraphArg>(ScriptableObject node, GraphEditorView<GraphArg> editorView, GraphView graphView, List<IGraphToolsNode> editorNodes, List<NodeSo> refNodes) where GraphArg : ScriptableObject
        {
            if (node is NodeSo nodeSo)
            {
                if (nodeSo.GetType().IsDefined(typeof(NodeIsTokenAttribute), true))
                {
                    var token = new NParamEditorToken<GraphArg>(editorView, nodeSo, default, null);
                    token.SetPosition(nodeSo.Position);
                    graphView.AddElement(token);
                    editorNodes.Add(token);
                    refNodes?.Add(nodeSo);
                }
                else
                {
                    var editorNode = new NParamEditorNode<GraphArg>(editorView, nodeSo, null);
                    editorNode.SetPosition(nodeSo.Position);
                    graphView.AddElement(editorNode);
                    editorNodes.Add(editorNode);
                    refNodes?.Add(nodeSo);
                }
                return true;
            }
            return false;
        }
    }
}
