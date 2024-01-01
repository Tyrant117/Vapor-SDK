using System;

namespace VaporObservables
{
    [Serializable]
    public class ByteObservable : ObservableField
    {
        public static implicit operator byte(ByteObservable f) => f.Value;

        public byte Value { get; protected set; }
        public bool Bool => Value != 0;
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
        internal bool InternalSet(byte value)
        {
            if (Value != value)
            {
                byte oldValue = Value;
                Value = value;
                ValueChanged?.Invoke(this, (byte)(Value - oldValue));
                return true;
            }
            else
            {
                return false;
            }
        }

        internal bool InternalModify(byte value, ObservableModifyType type) => type switch
        {
            ObservableModifyType.Set => InternalSet(value),
            ObservableModifyType.Add => InternalSet((byte)(Value + value)),
            ObservableModifyType.Multiplier => InternalSet((byte)(Value * value)),
            ObservableModifyType.PercentAdd => InternalSet((byte)(Value + Value * value)),
            _ => false,
        };

        public void SetWithoutNotify(byte value)
        {
            Value = value;
        }

        public void Set(byte value)
        {
            if (InternalSet(value))
            {
                Class?.MarkDirty(this);
            }
        }

        public void Modify(byte value, ObservableModifyType type)
        {
            if(InternalModify(value, type))
            {
                Class?.MarkDirty(this);
            }
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
