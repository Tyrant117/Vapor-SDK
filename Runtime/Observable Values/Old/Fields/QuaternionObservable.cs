using System;
using UnityEngine;

namespace VaporObservables
{
    /// <summary>
    /// The Quaternion implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="Quaternion"/>
    /// </summary>
    [Serializable]
    public class QuaternionObservable : ObservableField
    {
        public static implicit operator Quaternion(QuaternionObservable f) => f.Value;

        private Quaternion _value;
        /// <summary>
        /// The <see cref="Quaternion"/> value of the class.
        /// </summary>
        public Quaternion Value
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
        public event Action<QuaternionObservable, Quaternion> ValueChanged;

        public QuaternionObservable(ObservableClassOld @class, int fieldID, bool saveValue, Quaternion value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Quaternion;
            Value = value;
        }

        public QuaternionObservable(int fieldID, bool saveValue, Quaternion value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Quaternion;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(Quaternion value)
        {
            _value = value;
        }
        #endregion

        #region - Saving -
        public override SavedObservableField Save()
        {
            return new SavedObservableField(FieldID, Type, $"{Value.x},{Value.y},{Value.z},{Value.w}");
        }

        public override ObservableField Clone()
        {
            return new QuaternionObservable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}
