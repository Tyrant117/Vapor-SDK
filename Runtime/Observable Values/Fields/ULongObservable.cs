using System;

namespace VaporObservables
{
    [Serializable]
    public class ULongObservable : ObservableField
    {
        public static implicit operator ulong(ULongObservable f) => f.Value;

        public ulong Value { get; protected set; }
        public event Action<ULongObservable> ValueChanged;

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
        internal bool InternalSet(ulong value)
        {
            if (Value != value)
            {
                Value = value;
                ValueChanged?.Invoke(this);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void SetWithoutNotify(ulong value)
        {
            Value = value;
        }

        public void Set(ulong value)
        {
            if (InternalSet(value))
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
            return new ULongObservable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}