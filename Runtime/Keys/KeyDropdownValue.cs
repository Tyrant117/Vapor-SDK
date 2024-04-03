using System;
using System.Diagnostics;
using UnityEngine;
using VaporInspector;

namespace VaporKeys
{
    /// <summary>
    /// The base struct that contains a key. Optionally links to the guid of an object and can be used for remapping key values if that objects key changes.
    /// Has a custom drawer for selecting a key from a dropdown and be decorated with the <see cref="ValueDropdownAttribute"/>
    /// <example>
    /// How to implement the custom dropdown.
    /// <code>
    /// [Serializable, DrawWithVapor]
    /// public class DropdownDrawerExample
    /// {
    ///     [SerializeField, ValueDropdown("@GetCustomKeys")]
    ///     private KeyDropdownValue _exampleDropdown;
    ///
    /// 
    ///     private List&lt;(string, KeyDropdownValue)> GetCustomKeys()
    ///     {
    ///         return new List&lt;(string, KeyDropdownValue)> { "None", new KeyDropdownValue() };
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    [Serializable]
    public struct KeyDropdownValue : IEquatable<KeyDropdownValue>
    {
        public static implicit operator int(KeyDropdownValue kdv) => kdv.Key;

        /// <summary>
        /// The guid of the object linked to this key.
        /// </summary>
        public string Guid;
        /// <summary>
        /// The unique key.
        /// </summary>
        public int Key;

        /// <summary>
        /// If true, this is a "None" key.
        /// </summary>
        public bool IsNone => Key == 0;

        /// <summary>
        /// Creates a new KeyDropdownValue.
        /// </summary>
        /// <param name="guid">The guid of the linked object (can be empty)</param>
        /// <param name="key">the unique key</param>
        public KeyDropdownValue(string guid, int key)
        {
            Guid = guid;
            Key = key;
        }

        /// <summary>
        /// Returns the "None" KeyDropdownValue.
        /// </summary>
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
            return Guid == other.Guid && Key == other.Key;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Guid, Key);
        }
    }
}
