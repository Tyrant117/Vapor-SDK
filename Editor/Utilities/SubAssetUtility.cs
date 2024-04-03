using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VaporEditor
{
    public static class SubAssetUtility
    {
        public static void AddAssetTo(Object mainAsset, Object subAsset, HideFlags hideFlags)
        {
            subAsset.hideFlags = hideFlags;
            AssetDatabase.AddObjectToAsset(subAsset, mainAsset);
            AssetDatabase.SaveAssets();
        }

        public static void RemoveAssetFrom(Object subAsset)
        {
            AssetDatabase.RemoveObjectFromAsset(subAsset);
            AssetDatabase.SaveAssets();
        }

        public static bool HasAssetByType<T>(Object mainAsset)
        {
            var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(mainAsset));
            var countOfType = assets.OfType<T>().Count();
            return countOfType > 0;
        }

        public static T FirstAssetByType<T>(Object mainAsset)
        {
            var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(mainAsset));
            var firstOfType = assets.OfType<T>().FirstOrDefault();
            return firstOfType;
        }

        public static IEnumerable<T> FindAssetsByType<T>(Object mainAsset) where T : Object
        {
            var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(mainAsset));
            return assets.OfType<T>();
        }

        public static T FindAssetByName<T>(Object mainAsset, string name) where T : Object
        {
            var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(mainAsset));
            return (T)assets.FirstOrDefault(subAsset => subAsset.name == name);
        }

        public static T[] CloneAllSubAssets<T>(ScriptableObject mainAsset) where T : ScriptableObject
        {
            var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(mainAsset));
            var result = new T[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                result[i] = (T)CloneSubAsset(assets[i]);
            }
            return result;
        }

        public static T CloneSubAsset<T>(Object subAsset) where T : ScriptableObject
        {
            return (T)CloneSubAsset(subAsset);
        }

        public static ScriptableObject CloneSubAsset(Object subAsset)
        {
            var instance = ScriptableObject.CreateInstance(subAsset.GetType());
            EditorUtility.CopySerialized(subAsset, instance);
            return instance;
        }       
    }
}
