using System;
using VaporGraphTools;
using VaporKeys;

namespace VaporGraphToolsEditor
{
    public abstract class KeyNodeContainer
    {
        public string Name { get; }
        public KeyDropdownValue Key { get; }

        public KeyNodeContainer((string, KeyDropdownValue) stat)
        {
            Name = stat.Item1;
            Key = stat.Item2;
        }

        public abstract Type GetNodeType();
    }

    public class KeyNodeContainer<T> : KeyNodeContainer where T : NodeSo
    {
        public KeyNodeContainer((string, KeyDropdownValue) stat) : base(stat)
        {
        }

        public override Type GetNodeType()
        {
            return typeof(T);
        }
    }
}
