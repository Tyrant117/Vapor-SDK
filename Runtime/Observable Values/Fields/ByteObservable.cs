using System;

namespace VaporObservables
{
    /// <summary>
    /// The byte implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="byte"/>
    /// </summary>
    [Serializable]
    public class ByteObservable : ObservableField
    {
        public static implicit operator byte(ByteObservable f) => f.Value;

        private byte _value;
        /// <summary>
        /// The <see cref="byte"/> value of the class.
        /// </summary>
        public byte Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                
                var oldValue = _value;
                _value = value;
                ValueChanged?.Invoke(this, (byte)(_value - oldValue));
                Class?.MarkDirty(this);
            }
        }
        /// <summary>
        /// If the class is treated as a <see cref="bool"/> this returns the result.
        /// </summary>
        public bool Bool => Value != 0;
        /// <summary>
        /// The event that is fired when the <see cref="Value"/> changes. First the value and then the Delta.
        /// </summary>
        public event Action<ByteObservable, byte> ValueChanged; // Value and Delta

        public ByteObservable(ObservableClass @class, int fieldID, bool saveValue, byte value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Int8;
            Value = value;
        }

        public ByteObservable(int fieldID, bool saveValue, byte value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Int8;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(byte value)
        {
            _value = value;
        }
        #endregion

        #region - Saving -
        public override SavedObservableField Save()
        {
            return new SavedObservableField(FieldID, Type, Value.ToString());
        }
        #endregion

        public override string ToString()
        {
            return $"{FieldID} [{Value}]";
        }

        public override ObservableField Clone()
        {
            return new ByteObservable(FieldID, SaveValue, Value);
        }
    }
}
