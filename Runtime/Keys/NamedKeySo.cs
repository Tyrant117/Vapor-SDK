using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporKeys
{
    [CreateAssetMenu(menuName = "Vapor/Keys/Named Key",fileName = "NamedKey", order = 5)]
    public class NamedKeySo : KeySo
    {
        public override string DisplayName => name;
    }
}
