using System;

namespace VaporObservables
{
    [Serializable]
    public class DoubleObservable : ObservableField
    {
        public static implicit operator double(DoubleObservable f) => f.Value;

        public double Value { get; protected set; }
        public event Action<DoubleObservable, double> ValueChanged;

        public DoubleObservable(ObservableClass @class, int fieldID, bool saveValue, double value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Double;
            Value = value;
        }

        public DoubleObservable(int fieldID, bool saveValue, double value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Double;
            Value = value;
        }

        #region - Setters -
        internal bool InternalSet(double value)
        {
            if (Value != value)
            {
                double oldValue = Value;
                Value = value;
                ValueChanged?.Invoke(this, Value - oldValue);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal bool InternalModify(double value, ObservableModifyType type) => type switch
        {
            ObservableModifyType.Set => InternalSet(value),
            ObservableModifyType.Add => InternalSet(Value + value),
            ObservableModifyType.Multiplier => InternalSet(Value * value),
            ObservableModifyType.PercentAdd => InternalSet(Value + Value * value),
            _ => false,
        };

        public void SetWithoutNotify(double value)
        {
            Value = value;
        }

        public bool Set(double value)
        {
            if (InternalSet(value))
            {
                Class?.MarkDirty(this);
                return true;
            }
            return false;
        }

        public bool Modify(double value, ObservableModifyType type)
        {
            if (InternalModify(value, type))
            {
                Class?.MarkDirty(this);
                return true;
            }
            return false;
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
            return new DoubleObservable(FieldID, SaveValue, Value);
        }
    }
}
