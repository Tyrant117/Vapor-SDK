using System.Runtime.CompilerServices;
using VaporKeys;

namespace VaporGraphTools
{
    public class NamedKeyValueNodeSo : ValueNodeSo<NamedKeySo>
    {
        public virtual int FromID { get; } = -1;
    }
}
