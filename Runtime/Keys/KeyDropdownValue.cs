using System;
using System.Diagnostics;
using UnityEngine;

namespace VaporKeys
{
    [Serializable]
    public struct KeyDropdownValue : IEquatable<KeyDropdownValue>
    {
        public static implicit operator int(KeyDropdownValue kdv) => kdv.Key;

        public string Guid;
        public int Key;

        public bool IsNone => Key == 0;

        public KeyDropdownValue(string guid, int key)
        {
            Guid = guid;
            Key = key;
        }

        public static KeyDropdownValue None => new (string.Empty, 0);

        [Conditional("UNITY_EDITOR")]
        public void Select()
        {
#if UNITY_EDITOR
            if (Guid == string.Empty) return;
            
            var refVal = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(Guid));
            UnityEditor.Selection.activeObject = refVal;
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public void Remap()
        {
#if UNITY_EDITOR
            if (Guid == string.Empty) return;
            var refVal = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(Guid));
            
            if (refVal is not IKey rfk) return;
            rfk.ForceRefreshKey();
            Key = rfk.Key;
            UnityEditor.EditorUtility.SetDirty(refVal);
#endif
        }

        public override string ToString()
        {
            return $"Key: {Key} belonging to {Guid}";
        }

        public override bool Equals(object obj)
        {
            return obj is KeyDropdownValue other && Equals(other);
        }

        public bool Equals(KeyDropdownValue other)
        {
            return Guid.Equals(other.Guid) && Key.Equals(other.Key);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Guid, Key);
        }
    }
}
