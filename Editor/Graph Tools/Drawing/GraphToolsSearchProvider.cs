using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VaporEditor;
using VaporGraphTools;
using VaporKeys;

namespace VaporGraphToolsEditor
{
    public struct SearchContextElement
    {
        public Type NodeType { get; private set; }
        public string MenuName { get; private set; }
        public object UserData { get; private set; }


        public SearchContextElement(Type nodeType, string title, object userData = null)
        {
            NodeType = nodeType;
            MenuName = title;
            UserData = userData;
        }
    }

    public abstract class GraphToolsSearchProvider<T> : ScriptableObject, ISearchWindowProvider where T : ScriptableObject
    {
        public GraphEditorView<T> View;
        public List<string> IncludeFlags = new();

        public static List<SearchContextElement> Elements = new();

        public virtual void SetupIncludes() { }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            Elements.Clear();

            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Nodes"), 0)
            };

            var types = TypeCache.GetTypesWithAttribute<SearchableNodeAttribute>();
            foreach (var type in types)
            {
                var atr = type.GetCustomAttribute<SearchableNodeAttribute>();
                FilterNewNode(type, atr);
            }

            if (IncludeFlags.Contains("math"))
            {
                var mathGraphs = AssetDatabaseUtility.FindAssetsByType<MathGraphSo>();
                foreach (var graph in mathGraphs)
                {
                    if (View.Graph.name == graph.name) { continue; }

                    AssetNodeContainer containerTarget = new(graph.name, AssetDatabase.GetAssetPath(graph), graph.GetType());
                    Elements.Add(new SearchContextElement(typeof(MathGraphNodeSo), $"Graphs/Math/{graph.name}", containerTarget));
                }
            }

            if (IncludeFlags.Contains("logic"))
            {
                var logicGraphs = AssetDatabaseUtility.FindAssetsByType<LogicGraphSo>();
                foreach (var graph in logicGraphs)
                {
                    if (View.Graph.name == graph.name) { continue; }

                    AssetNodeContainer containerTarget = new(graph.name, AssetDatabase.GetAssetPath(graph), graph.GetType());
                    Elements.Add(new SearchContextElement(typeof(LogicGraphNodeSo), $"Graphs/Logic/{graph.name}", containerTarget));
                }
            }

            AddAdditionalNodes();

            SortElements();

            var groups = new List<string>();
            foreach (var e in Elements)
            {
                var split = e.MenuName.Split('/');
                var groupName = new StringBuilder();
                for (int i = 0; i < split.Length - 1; i++)
                {
                    groupName.Append(split[i]);
                    if (!groups.Contains(groupName.ToString()))
                    {
                        tree.Add(new SearchTreeGroupEntry(new GUIContent(split[i]), i + 1));
                        groups.Add(groupName.ToString());
                    }
                    groupName.Append("/");
                }

                var entry = new SearchTreeEntry(new GUIContent(split[^1]))
                {
                    level = split.Length,
                    userData = e,
                };
                tree.Add(entry);
            }

            return tree;
        }

        /// <summary>
        /// Filters a newly created node. This method should add be used to add new <see cref="SearchContextElement"/> to <see cref="Elements"/>
        /// </summary>
        /// <param name="node"></param>
        protected virtual void FilterNewNode(Type nodeType, SearchableNodeAttribute attribute)
        {
            if (string.IsNullOrEmpty(attribute.MenuName)) { return; }
            if (nodeType.IsSubclassOf(typeof(NodeSo)))
            {
                if (attribute.TypeCollection != null)
                {
                    if (attribute.IncludeFlags.Length != 0)
                    {
                        foreach (var flag in attribute.IncludeFlags)
                        {
                            if (IncludeFlags.Contains(flag))
                            {
                                var collection = AssetDatabaseUtility.FindAssetsByType(attribute.TypeCollection);
                                foreach (var item in collection)
                                {
                                    AssetNodeContainer containerTarget = new(item.name, AssetDatabase.GetAssetPath(item), item.GetType());
                                    Elements.Add(new SearchContextElement(nodeType, $"{attribute.MenuName}/{item.name}", containerTarget));
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        var collection = AssetDatabaseUtility.FindAssetsByType(attribute.TypeCollection);
                        foreach (var item in collection)
                        {
                            AssetNodeContainer containerTarget = new(item.name, AssetDatabase.GetAssetPath(item), item.GetType());
                            Elements.Add(new SearchContextElement(nodeType, $"{attribute.MenuName}/{item.name}", containerTarget));
                        }
                    }
                }
                else
                {
                    if (attribute.IncludeFlags.Length != 0)
                    {
                        foreach (var flag in attribute.IncludeFlags)
                        {
                            if (IncludeFlags.Contains(flag))
                            {
                                Elements.Add(new SearchContextElement(nodeType, attribute.MenuName));
                                break;
                            }
                        }
                    }
                    else
                    {
                        Elements.Add(new SearchContextElement(nodeType, attribute.MenuName));
                    }
                }
            }
        }

        protected virtual void AddAdditionalNodes() { }

        protected virtual void SortElements()
        {
            // Sort
            Elements.Sort((e1, e2) =>
            {
                var s1 = e1.MenuName.Split('/');
                var s2 = e2.MenuName.Split('/');
                for (int i = 0; i < s1.Length; i++)
                {
                    if (i >= s2.Length)
                    {
                        return 1;
                    }
                    else
                    {
                        var value = s1[i].CompareTo(s2[i]);
                        if (value != 0)
                        {
                            if (s1.Length != s2.Length && (i == s1.Length - 1 || i == s2.Length - 1))
                            {
                                return s1.Length < s2.Length ? 1 : -1;
                            }
                            else
                            {
                                return value;
                            }
                        }
                    }
                }

                return 0;
            });
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            var mousePos = View.ChangeCoordinatesTo(View, context.screenMousePosition - View.Window.position.position);
            var graphMousePos = View.GraphView.contentViewContainer.WorldToLocal(mousePos);

            var element = (SearchContextElement)SearchTreeEntry.userData;

            if (element.NodeType.IsSubclassOf(typeof(NodeSo)))
            {
                var n = ScriptableObject.CreateInstance(element.NodeType);
                if (n is NodeSo node)
                {
                    node.Position = new Rect(graphMousePos, Vector2.zero);
                    if (node is IGuidNode guidNode)
                    {
                        var guid = guidNode.CreateGuid();
                        node.SetGuid(guid);
                    }
                    if (element.UserData != null)
                    {
                        if (element.UserData is AssetNodeContainer assetContainer)
                        {
                            Debug.Log(assetContainer.AssetType);
                            if (assetContainer.AssetType.IsSubclassOf(typeof(NamedKeySo)))
                            {
                                var asset = (NamedKeySo)AssetDatabase.LoadAssetAtPath(assetContainer.AssetPath, assetContainer.AssetType);
                                var namedKeyNode = (NamedKeyValueNodeSo)node;
                                namedKeyNode.Value = asset;
                                namedKeyNode.name = asset.name;
                                namedKeyNode.Name = asset.name;
                            }
                            if (assetContainer.AssetType.IsSubclassOf(typeof(IntegerKeySo)))
                            {
                                var asset = (IntegerKeySo)AssetDatabase.LoadAssetAtPath(assetContainer.AssetPath, assetContainer.AssetType);
                                var integerKeyNode = (IntegerKeyValueNodeSo)node;
                                integerKeyNode.Value = asset;
                                integerKeyNode.name = asset.name;
                                integerKeyNode.Name = asset.name;
                            }

                            if (assetContainer.AssetType.IsSubclassOf(typeof(MathGraphSo)))
                            {
                                Debug.Log("Math Graph Built");
                                var asset = (MathGraphSo)AssetDatabase.LoadAssetAtPath(assetContainer.AssetPath, assetContainer.AssetType);
                                var mathGraphNode = (MathGraphNodeSo)node;
                                mathGraphNode.Graph = asset;
                                mathGraphNode.name = asset.name;
                                mathGraphNode.Name = asset.name;
                            }

                            if (assetContainer.AssetType.IsSubclassOf(typeof(LogicGraphSo)))
                            {
                                var asset = (LogicGraphSo)AssetDatabase.LoadAssetAtPath(assetContainer.AssetPath, assetContainer.AssetType);
                                var logicGraphNode = (LogicGraphNodeSo)node;
                                logicGraphNode.Graph = asset;
                                logicGraphNode.name = asset.name;
                                logicGraphNode.Name = asset.name;
                            }
                        }
                    }
                    else
                    {
                        var searchableAtr = element.NodeType.GetCustomAttribute<SearchableNodeAttribute>();
                        if (searchableAtr != null)
                        {
                            node.Name = searchableAtr.NodeName;
                        }
                        else
                        {
                            node.Name = element.NodeType.Name;
                        }
                        node.name = node.Name;
                    }
                    AddNodeEntry(node);
                    View.AddNode(node);
                }
            }
            else
            {
                if (element.UserData is AssetNodeContainer container)
                {
                    if (container.AssetType == typeof(MathGraphSo))
                    {
                        var graph = (MathGraphSo)AssetDatabase.LoadAssetAtPath(container.AssetPath, container.AssetType);
                        var node = CreateInstance<MathGraphNodeSo>();
                        node.Position = new Rect(graphMousePos, Vector2.zero);
                        if (node is IGuidNode guidNode)
                        {
                            var guid = guidNode.CreateGuid();
                            node.SetGuid(guid);
                        }
                        node.Graph = graph;
                        node.name = graph.name;
                        node.Name = graph.name;
                        AddNodeEntry(node);
                        View.AddNode(node);
                    }

                    if (container.AssetType == typeof(LogicGraphSo))
                    {
                        var graph = (LogicGraphSo)AssetDatabase.LoadAssetAtPath(container.AssetPath, container.AssetType);
                        var node = CreateInstance<LogicGraphNodeSo>();
                        node.Position = new Rect(graphMousePos, Vector2.zero);
                        if (node is IGuidNode guidNode)
                        {
                            var guid = guidNode.CreateGuid();
                            node.SetGuid(guid);
                        }
                        node.Graph = graph;
                        node.name = graph.name;
                        node.Name = graph.name;
                        AddNodeEntry(node);
                        View.AddNode(node);
                    }
                }

                AddTypeEntry(graphMousePos, element);
            }

            return true;
        }

        protected virtual void AddNodeEntry(NodeSo node) { }

        protected virtual void AddTypeEntry(Vector2 graphMousePos, SearchContextElement context) { }
    }
}
