using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if  UNITY_EDITOR
using UnityEditor;
#endif

namespace Vapor
{
    /// <summary>
    /// A static class to access the AssetDatabase functionality. Should be used carefully as it will only function in the editor not build.
    /// </summary>
    public static class RuntimeAssetDatabaseUtility
    {
        public static List<T> FindAssetsByType<T>() where T : Object
        {
#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
            return guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>).Where(asset => asset != null).ToList();
#else
            return null;
#endif
        }
    }
}
