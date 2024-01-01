using System;

namespace VaporObservables
{
    [Serializable]
    public class ShortObservable : ObservableField
    {
        public static implicit operator short(ShortObservable f) => f.Value;

        public short Value { get; protected set; }
        public event Action<ShortObservable, int> ValueChanged;

        public ShortObservable(ObservableClass @class, int fieldID, bool saveValue, short value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Int16;
            Value = value;
        }

        public ShortObservable(int fieldID, bool saveValue, short value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Int16;
            Value = value;
        }

        #region - Setters -
        internal bool InternalSet(short value)
        {
            if (Value != value)
            {
                var oldValue = Value;
                Value = value;
                ValueChanged?.Invoke(this, Value - oldValue);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal bool InternalModify(short value, ObservableModifyType type)
        {
            return type switch
            {
                ObservableModifyType.Set => InternalSet(value),
                ObservableModifyType.Add => InternalSet((short)(Value + value)),
                ObservableModifyType.Multiplier => InternalSet((short)(Value * value)),
                ObservableModifyType.PercentAdd => InternalSet((short)(Value + Value * value)),
                _ => false,
            };
        }

        public void SetWithoutNotify(short value)
        {
            Value = value;
        }

        public void Set(short value)
        {
            if (InternalSet(value))
            {
                Class?.MarkDirty(this);
            }
        }

        public void Modify(short value, ObservableModifyType type)
        {
            if (InternalModify(value, type))
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

        public override ObservableField Clone()
        {
            return new ShortObservable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}