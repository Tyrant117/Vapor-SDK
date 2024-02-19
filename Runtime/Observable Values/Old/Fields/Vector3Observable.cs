using System;
using UnityEngine;

namespace VaporObservables
{
    /// <summary>
    /// The Vector3 implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="Vector3"/>
    /// </summary>
    [Serializable]
    public class Vector3Observable : ObservableField
    {
        public static implicit operator Vector3(Vector3Observable f) => f.Value;

        private Vector3 _value;
        /// <summary>
        /// The <see cref="Vector3"/> value of the class.
        /// </summary>
        public Vector3 Value
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
        public event Action<Vector3Observable, Vector3> ValueChanged;

        public Vector3Observable(ObservableClassOld @class, int fieldID, bool saveValue, Vector3 value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector3;
            Value = value;
        }

        public Vector3Observable(int fieldID, bool saveValue, Vector3 value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector3;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(Vector3 value)
        {
            _value = value;
        }
        #endregion

        #region - Saving -
        public override SavedObservableField Save()
        {
            return new SavedObservableField(FieldID, Type, $"{Value.x},{Value.y},{Value.z}");
        }

        public override ObservableField Clone()
        {
            return new Vector3Observable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}