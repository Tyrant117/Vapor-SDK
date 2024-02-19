using System;

namespace VaporObservables
{
    /// <summary>
    /// The uint implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="uint"/>
    /// </summary>
    [Serializable]
    public class UIntObservable : ObservableField
    {
        public static implicit operator uint(UIntObservable f) => f.Value;

        private uint _value;
        /// <summary>
        /// The <see cref="uint"/> value of the class.
        /// </summary>
        public uint Value
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
        public event Action<UIntObservable, uint> ValueChanged;

        public UIntObservable(ObservableClassOld @class, int fieldID, bool saveValue, uint value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.UInt32;
            Value = value;
        }

        public UIntObservable(int fieldID, bool saveValue, uint value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.UInt32;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(uint value)
        {
            _value = value;
        }
        #endregion

        #region - Saving -
        public override SavedObservableField Save()
        {
            return new SavedObservableField(FieldID, Type, Value.ToString());
        }

        public override ObservableField Clone()
        {
            return new UIntObservable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}
