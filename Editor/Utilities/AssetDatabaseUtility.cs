using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VaporEditor
{
    public static class AssetDatabaseUtility 
    {
        // Example method to find assets of a specific type
        public static List<T> FindAssetsByType<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
            return guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>).Where(asset => asset != null).ToList();
        }

        public static List<Object> FindAssetsByType(Type type)
        {
            var guids = AssetDatabase.FindAssets($"t:{type}");
            return guids.Select(AssetDatabase.GUIDToAssetPath).Select(p => AssetDatabase.LoadAssetAtPath(p, type)).Where(asset => asset != null).ToList();
        }

        public static string AssetToGuid(Object asset)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }
    }
}
