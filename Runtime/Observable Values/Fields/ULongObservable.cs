using System;

namespace VaporObservables
{
    /// <summary>
    /// The ulong implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="ulong"/>
    /// </summary>
    [Serializable]
    public class ULongObservable : ObservableField
    {
        public static implicit operator ulong(ULongObservable f) => f.Value;

        private ulong _value;
        /// <summary>
        /// The <see cref="ulong"/> value of the class.
        /// </summary>
        public ulong Value
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
        public event Action<ULongObservable, ulong> ValueChanged;

        public ULongObservable(ObservableClass @class, int fieldID, bool saveValue, ulong value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.UInt64;
            Value = value;
        }

        public ULongObservable(int fieldID, bool saveValue, ulong value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.UInt64;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(ulong value)
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
            return new ULongObservable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}