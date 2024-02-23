using System;
using System.Collections.Generic;
using VaporEvents;
using VaporInspector;
using VaporKeys;

namespace VaporXR
{
    [Serializable, DrawWithVapor(UIGroupType.Vertical)]
    public struct InteractionLayerKey
    {
        [ValueDropdown("@GetAllProviderKeyValues"), IgnoreCustomDrawer]
        public KeyDropdownValue Layer;

        public static List<(string, KeyDropdownValue)> GetAllProviderKeyValues()
        {
            return KeyUtility.GetAllKeysOfNamedType("InteractionLayerKeyKeys");
        }
    }
}
