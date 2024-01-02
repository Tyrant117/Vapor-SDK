using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporKeys;

namespace VaporEvents
{
    /// <summary>
    /// A scriptable object implementation of the IKey interface that derives its display name from the <see cref="ScriptableObject.name"/> and is used with functionality relating to the <see cref="EventBus"/>
    /// </summary>
    [CreateAssetMenu(menuName = "Vapor/Keys/Event Key",fileName = "EventKey", order = 6)]
    public class EventKeySo : NamedKeySo
    {

    }
}
