using System;
using UnityEditor;
using UnityEngine;
using VaporEditor;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{
    //public class GraphToolsMathAndLogicSearchProvider<T> : GraphToolsSearchProvider<T> where T: MathGraphSo
    //{
    //    protected override void AddAdditionalNodes()
    //    {
    //        var mathGraphs = AssetDatabaseUtility.FindAssetsByType<MathGraphSo>();
    //        foreach (var graph in mathGraphs)
    //        {
    //            if (View.Graph.name == graph.name) { continue; }

    //            AssetNodeContainer containerTarget = new(graph.name, AssetDatabase.GetAssetPath(graph), graph.GetType());
    //            Elements.Add(new SearchContextElement(containerTarget.GetType(), $"Graphs/Math/{graph.name}", containerTarget));
    //        }

    //        var logicGraphs = AssetDatabaseUtility.FindAssetsByType<LogicGraphSo>();
    //        foreach (var graph in logicGraphs)
    //        {
    //            if (View.Graph.name == graph.name) { continue; }

    //            AssetNodeContainer containerTarget = new(graph.name, AssetDatabase.GetAssetPath(graph), graph.GetType());
    //            Elements.Add(new SearchContextElement(containerTarget.GetType(), $"Graphs/Logic/{graph.name}", containerTarget));
    //        }
    //    }

    //    protected override void AddTypeEntry(Vector2 graphMousePos, SearchContextElement context)
    //    {
    //        if (context.UserData is AssetNodeContainer container)
    //        {
    //            if (container.AssetType == typeof(MathGraphSo))
    //            {
    //                var graph = (MathGraphSo)AssetDatabase.LoadAssetAtPath(container.AssetPath, container.AssetType);
    //                var node = CreateInstance<MathGraphNodeSo>();
    //                node.Position = new Rect(graphMousePos, Vector2.zero);
    //                if (node is IGuidNode guidNode)
    //                {
    //                    var guid = guidNode.CreateGuid();
    //                    node.SetGuid(guid);
    //                }
    //                node.Graph = graph;
    //                node.name = graph.name;
    //                node.Name = graph.name;
    //                AddNodeEntry(node);
    //                View.AddNode(node);
    //            }

    //            if (container.AssetType == typeof(LogicGraphSo))
    //            {
    //                var graph = (LogicGraphSo)AssetDatabase.LoadAssetAtPath(container.AssetPath, container.AssetType);
    //                var node = CreateInstance<LogicGraphNodeSo>();
    //                node.Position = new Rect(graphMousePos, Vector2.zero);
    //                if (node is IGuidNode guidNode)
    //                {
    //                    var guid = guidNode.CreateGuid();
    //                    node.SetGuid(guid);
    //                }
    //                node.Graph = graph;
    //                node.name = graph.name;
    //                node.Name = graph.name;
    //                AddNodeEntry(node);
    //                View.AddNode(node);
    //            }
    //        }
    //    }
    //}
}
