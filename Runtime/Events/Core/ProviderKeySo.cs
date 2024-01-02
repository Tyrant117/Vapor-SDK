using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporKeys;

namespace VaporEvents
{
    /// <summary>
    /// A scriptable object implementation of the IKey interface that derives its display name from the <see cref="ScriptableObject.name"/> and is used with functionality relating to the <see cref="ProviderBus"/>
    /// </summary>
    [CreateAssetMenu(menuName = "Vapor/Keys/Provider Key",fileName = "ProviderKey", order = 7)]
    public class ProviderKeySo : NamedKeySo
    {
        
    }
}
