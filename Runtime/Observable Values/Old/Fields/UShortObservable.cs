using System;

namespace VaporObservables
{
    /// <summary>
    /// The ushort implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="ushort"/>
    /// </summary>
    [Serializable]
    public class UShortObservable : ObservableField
    {
        public static implicit operator ushort(UShortObservable f) => f.Value;

        private ushort _value;
        /// <summary>
        /// The <see cref="ushort"/> value of the class.
        /// </summary>
        public ushort Value
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
        public event Action<UShortObservable, ushort> ValueChanged;

        public UShortObservable(ObservableClassOld @class, int fieldID, bool saveValue, ushort value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.UInt16;
            Value = value;
        }

        public UShortObservable(int fieldID, bool saveValue, ushort value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.UInt16;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(ushort value)
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
            return new UShortObservable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}
