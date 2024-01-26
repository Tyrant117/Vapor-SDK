using UnityEngine;

namespace VaporDataAssets
{
    /// <summary>
    /// <see cref="ScriptableObject"/> container class that holds a typed value.
    /// Can be referenced by multiple components in order to share the same set of data.
    /// </summary>
    /// <typeparam name="T">Value type held by this container.</typeparam>
    /// <seealso cref="DatumProperty{TValue,TDatum}"/>
    public abstract class Datum<T> : ScriptableObject
    {
        [SerializeField] private T _value;
        
        public T Value { get => _value; set => _value = value; }
    }
}
