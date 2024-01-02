using System;
using UnityEngine;

namespace VaporObservables
{
    /// <summary>
    /// The Vector3Int implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="Vector3Int"/>
    /// </summary>
    [Serializable]
    public class Vector3IntObservable : ObservableField
    {
        public static implicit operator Vector3Int(Vector3IntObservable f) => f.Value;

        private Vector3Int _value;

        /// <summary>
        /// The <see cref="Vector2"/> value of the class.
        /// </summary>
        public Vector3Int Value
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
        public event Action<Vector3IntObservable, Vector3Int> ValueChanged;

        public Vector3IntObservable(ObservableClass @class, int fieldID, bool saveValue, Vector3Int value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector3Int;
            Value = value;
        }

        public Vector3IntObservable(int fieldID, bool saveValue, Vector3Int value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector3Int;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(Vector3Int value)
        {
            _value = value;
        }
        #endregion

        #region - Saving -
        public override SavedObservableField Save()
        {
            return new SavedObservableField(FieldID, Type, $"{Value.x},{Value.y},{Value.z}");
        }

        #endregion

        public override string ToString()
        {
            return $"{FieldID} [{Value}]";
        }

        public override ObservableField Clone()
        {
            return new Vector3IntObservable(FieldID, SaveValue, Value);
        }
    }
}
