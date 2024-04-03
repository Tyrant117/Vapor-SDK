using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporKeys;

namespace VaporGraphTools
{
    public class IntegerKeyValueNodeSo : ValueNodeSo<IntegerKeySo>
    {
        public virtual int FromID { get; } = -1;
    }
}
