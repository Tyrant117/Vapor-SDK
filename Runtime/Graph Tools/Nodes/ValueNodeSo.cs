using System.Runtime.CompilerServices;
using UnityEngine;
using VaporKeys;

namespace VaporGraphTools
{
    public abstract class ValueNodeSo<T> : PropertyNodeSo
    {
        [SerializeField]
        private T _value;
        public virtual T Value { get => _value; set => _value = value; }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public override object GetBoxedValue(IExternalValueGetter getter) => Value;
    }
}
