using System;
using UnityEngine;
using Vapor;
using VaporInspector;

namespace VaporKeys
{
    /// <summary>
    /// A scriptable object implementation of the IKey interface.
    /// </summary>
    public abstract class KeySo : ScriptableObject, IKey
    {
        [FoldoutGroup("Key", "Key Data"), SerializeField, ReadOnly, RichTextTooltip("The unique for this object.")]
        private int _key;
        [SerializeField]
        [FoldoutGroup("Key"), RichTextTooltip("If <lw>TRUE</lw>, this key will be ignored by KeyGenerator.GenerateKeys().")]
        protected bool _deprecated;
        public int Key => _key;
        public void ForceRefreshKey() { _key = name.GetKeyHashCode(); }
        public abstract string DisplayName { get; }
        public bool IsDeprecated => _deprecated;


        [FoldoutGroup("Key"), Button, RichTextTooltip("Forces Generation of the keys for this Type")]
        public void GenerateKeys()
        {
            var scriptName = GetType().Name;
            scriptName = scriptName.Replace("Scriptable", "");
            scriptName = scriptName.Replace("SO", "");
            scriptName = scriptName.Replace("So", "");
            KeyGenerator.GenerateKeys(GetType(), $"{scriptName}Keys", true);
        }

        public static void GenerateKeysOfType<T>() where T : KeySo
        {
            var scriptName = typeof(T).Name;
            scriptName = scriptName.Replace("Scriptable", "");
            scriptName = scriptName.Replace("SO", "");
            scriptName = scriptName.Replace("So", "");
            KeyGenerator.GenerateKeys(typeof(T), $"{scriptName}Keys", true);
        }
    }
}
