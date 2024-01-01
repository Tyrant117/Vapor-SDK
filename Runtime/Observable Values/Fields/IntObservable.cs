using System;

namespace VaporObservables
{
    [Serializable]
    public class IntObservable : ObservableField
    {
        public static implicit operator int(IntObservable f) => f.Value;

        public int Value { get; protected set; }
        public bool HasFlag(int flagToCheck) => (Value & flagToCheck) != 0;
        public event Action<IntObservable, int> ValueChanged; // Value and Delta

        public IntObservable(ObservableClass @class, int fieldID, bool saveValue, int value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Int32;
            Value = value;
        }

        public IntObservable(int fieldID, bool saveValue, int value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Int32;
            Value = value;
        }

        #region - Setters -
        internal bool InternalSet(int value)
        {
            if (Value != value)
            {
                int oldValue = Value;
                Value = value;
                ValueChanged?.Invoke(this, Value - oldValue);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal bool InternalModify(int value, ObservableModifyType type) => type switch
        {
            ObservableModifyType.Set => InternalSet(value),
            ObservableModifyType.Add => InternalSet(Value + value),
            ObservableModifyType.Multiplier => InternalSet(Value * value),
            ObservableModifyType.PercentAdd => InternalSet(Value + Value * value),
            _ => false,
        };

        public void SetWithoutNotify(int value)
        {
            Value = value;
        }

        public void Set(int value)
        {
            if (InternalSet(value))
            {
                Class?.MarkDirty(this);
            }
        }

        public void Modify(int value, ObservableModifyType type)
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
        #endregion

        public override string ToString()
        {
            return $"{FieldID} [{Value}]";
        }

        public override ObservableField Clone()
        {
            return new IntObservable(FieldID, SaveValue, Value);
        }
    }
}
