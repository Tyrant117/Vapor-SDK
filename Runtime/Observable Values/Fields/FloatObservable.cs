using System;

namespace VaporObservables
{
    [Serializable]
    public class FloatObservable : ObservableField
    {
        public static implicit operator float(FloatObservable f) => f.Value;

        public float Value { get; protected set; }
        public event Action<FloatObservable, float> ValueChanged;

        public FloatObservable(ObservableClass @class, int fieldID, bool saveValue, float value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Single;
            Value = value;
        }

        public FloatObservable(int fieldID, bool saveValue, float value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Single;
            Value = value;
        }

        #region - Setters -
        internal bool InternalSet(float value)
        {
            if (Value != value)
            {
                float oldValue = Value;
                Value = value;
                ValueChanged?.Invoke(this, Value - oldValue);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal bool InternalModify(float value, ObservableModifyType type) => type switch
        {
            ObservableModifyType.Set => InternalSet(value),
            ObservableModifyType.Add => InternalSet(Value + value),
            ObservableModifyType.Multiplier => InternalSet(Value * value),
            ObservableModifyType.PercentAdd => InternalSet(Value + Value * value),
            _ => false,
        };

        public void SetWithoutNotify(float value)
        {
            Value = value;
        }

        public void Set(float value)
        {
            if (InternalSet(value))
            {
                Class?.MarkDirty(this);
            }
        }

        public void Modify(float value, ObservableModifyType type)
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
            return new FloatObservable(FieldID, SaveValue, Value);
        }
    }
}
