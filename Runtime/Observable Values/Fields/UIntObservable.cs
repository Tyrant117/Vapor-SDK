using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporObservables
{
    public class UIntObservable : ObservableField
    {
        public static implicit operator uint(UIntObservable f) => f.Value;

        public uint Value { get; protected set; }
        public event Action<UIntObservable> ValueChanged;

        public UIntObservable(ObservableClass @class, int fieldID, bool saveValue, uint value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.UInt32;
            Value = value;
        }

        public UIntObservable(int fieldID, bool saveValue, uint value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.UInt32;
            Value = value;
        }

        #region - Setters -
        internal bool InternalSet(uint value)
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

        public void SetWithoutNotify(uint value)
        {
            Value = value;
        }

        public void Set(uint value)
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
            return new UIntObservable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}
