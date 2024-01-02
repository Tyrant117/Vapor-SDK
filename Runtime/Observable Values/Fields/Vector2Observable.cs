using System;
using UnityEngine;

namespace VaporObservables
{
    /// <summary>
    /// The Vector2 implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="Vector2"/>
    /// </summary>
    [Serializable]
    public class Vector2Observable : ObservableField
    {
        public static implicit operator Vector2(Vector2Observable f) => f.Value;

        private Vector2 _value;
        /// <summary>
        /// The <see cref="Vector2"/> value of the class.
        /// </summary>
        public Vector2 Value
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
        public event Action<Vector2Observable, Vector2> ValueChanged;

        public Vector2Observable(ObservableClass @class, int fieldID, bool saveValue, Vector2 value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector2;
            Value = value;
        }

        public Vector2Observable(int fieldID, bool saveValue, Vector2 value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector2;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(Vector2 value)
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
            return new Vector2Observable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}