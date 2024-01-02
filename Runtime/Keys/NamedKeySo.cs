using UnityEngine;

namespace VaporKeys
{
    /// <summary>
    /// A scriptable object implementation of the IKey interface that derives its display name from the <see cref="ScriptableObject.name"/>
    /// </summary>
    [CreateAssetMenu(menuName = "Vapor/Keys/Named Key",fileName = "NamedKey", order = 5)]
    public class NamedKeySo : KeySo
    {
        public override string DisplayName => name;
    }
}
