using UnityEngine;
using VaporInspector;

namespace VaporKeys
{
    public abstract class KeySo : ScriptableObject, IKey
    {
        [FoldoutGroup("Key", "Key Data"), SerializeField, ReadOnly]
        private int _key;
        [SerializeField]
        [FoldoutGroup("Key"), RichTextTooltip("If <lw>TRUE</lw>, this key will be ignored by KeyGenerator.GenerateKeys().")]
        protected bool _deprecated;
        public int Key => _key;
        public void ForceRefreshKey() { _key = name.GetKeyHashCode(); }
        public abstract string DisplayName { get; }
        public bool IsDeprecated => _deprecated;


        public virtual void GenerateKeys()
        {
            var scriptName = GetType().Name;
            scriptName = scriptName.Replace("Scriptable", "");
            scriptName = scriptName.Replace("SO", "");
            KeyGenerator.GenerateKeys(GetType(), $"{scriptName}Keys", true);
        }
    }
}
