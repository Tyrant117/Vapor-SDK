using System;
using UnityEngine;

namespace VaporObservables
{
    /// <summary>
    /// The Vector2Int implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="Vector2Int"/>
    /// </summary>
    [Serializable]
    public class Vector2IntObservable : ObservableField
    {
        public static implicit operator Vector2Int(Vector2IntObservable f) => f.Value;
        
        private Vector2Int _value;
        /// <summary>
        /// The <see cref="Vector2Int"/> value of the class.
        /// </summary>
        public Vector2Int Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                
                var oldValue = _value;
                _value = value;
                ValueChanged?.Invoke(this, oldValue);
                Class?.MarkDirty(this);
            }
        }
        /// <summary>
        /// Invoked on value change. Parameters are the new and old values. New -> Old
        /// </summary>
        public event Action<Vector2IntObservable, Vector2Int> ValueChanged;

        public Vector2IntObservable(ObservableClass @class, int fieldID, bool saveValue, Vector2Int value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector2Int;
            Value = value;
        }

        public Vector2IntObservable(int fieldID, bool saveValue, Vector2Int value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector2Int;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(Vector2Int value)
        {
            _value = value;
        }
        #endregion

        #region - Saving -
        public override SavedObservableField Save()
        {
            return new SavedObservableField(FieldID, Type, $"{Value.x},{Value.y}");
        }

        public override ObservableField Clone()
        {
            return new Vector2IntObservable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}