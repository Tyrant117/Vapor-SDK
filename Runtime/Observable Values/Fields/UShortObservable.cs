using System;

namespace VaporObservables
{
    public class UShortObservable : ObservableField
    {
        public static implicit operator ushort(UShortObservable f) => f.Value;

        public ushort Value { get; protected set; }
        public event Action<UShortObservable> ValueChanged;

        public UShortObservable(ObservableClass @class, int fieldID, bool saveValue, ushort value) : base(@class, fieldID, saveValue)
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
        internal bool InternalSet(ushort value)
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

        public void SetWithoutNotify(ushort value)
        {
            Value = value;
        }

        public void Set(ushort value)
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
            return new UShortObservable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}
