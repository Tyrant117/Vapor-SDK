using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VaporEditor;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{
    public static class NodeUtility
    {
        public static void SaveNodeGraphRepresentation(ScriptableObject mainGraphAsset, ScriptableObject newGraphData, List<NodeSo> newNodeData, Action<NodeSo> traverseCallback)
        {
            var nodesToLink = new List<NodeSo>();
            var assets = SubAssetUtility.FindAssetsByType<NodeSo>(mainGraphAsset);
            foreach (var asset in assets)
            {
                var match = newNodeData.FirstOrDefault(newNode => newNode.GetGuid() == asset.GetGuid());
                if (match != null)
                {
                    EditorUtility.CopySerializedIfDifferent(match, asset);
                    EditorUtility.SetDirty(asset);
                    nodesToLink.Add(asset);
                }
                else
                {
                    Debug.Log($"Removed: {asset.name}");
                    AssetDatabase.RemoveObjectFromAsset(asset);
                }
            }

            foreach (var newNode in newNodeData)
            {
                var match = assets.Any(asset => asset.GetGuid() == newNode.GetGuid());
                if (!match)
                {
                    var cloned = SubAssetUtility.CloneSubAsset(newNode);
                    cloned.name = string.IsNullOrEmpty(newNode.name) ? newNode.GetType().Name : newNode.name;
                    AssetDatabase.AddObjectToAsset(cloned, mainGraphAsset);
                    nodesToLink.Add((NodeSo)cloned);
                }
            }

            foreach (var node in nodesToLink)
            {
                node.LinkNodeData(nodesToLink, traverseCallback);
            }

            var name = mainGraphAsset.name;
            EditorUtility.CopySerializedIfDifferent(newGraphData, mainGraphAsset);
            mainGraphAsset.name = name;
            EditorUtility.SetDirty(mainGraphAsset);
            AssetDatabase.SaveAssets();
        }
    }
}
