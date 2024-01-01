using System;
using UnityEngine;

namespace VaporObservables
{
    [Serializable]
    public class Vector2Observable : ObservableField
    {
        public static implicit operator Vector2(Vector2Observable f) => f.Value;

        public Vector2 Value { get; protected set; }
        public event Action<Vector2Observable, Vector2> ValueChanged;

        public Vector2Observable(ObservableClass @class, int fieldID, bool saveValue, Vector2 value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector2;
            Value = value;
        }

        public Vector2Observable(int fieldID, bool saveValue, Vector2 value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector2;
            Value = value;
        }

        #region - Setters -
        internal bool InternalSet(Vector2 value)
        {
            if (Value != value)
            {
                var old = Value;
                Value = value;
                ValueChanged?.Invoke(this, Value - old);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal bool InternalModify(Vector2 value, ObservableModifyType type) => type switch
        {
            ObservableModifyType.Set => InternalSet(value),
            ObservableModifyType.Add => InternalSet(Value + value),
            ObservableModifyType.Multiplier => InternalSet(Value * value),
            ObservableModifyType.PercentAdd => InternalSet(Value + Value * value),
            _ => false,
        };

        public void SetWithoutNotify(Vector2 value)
        {
            Value = value;
        }

        public void Set(Vector2 value)
        {
            if (InternalSet(value))
            {
                Class?.MarkDirty(this);
            }
        }

        public void Modify(float multiplier)
        {
            if (InternalSet(Value * multiplier))
            {
                Class?.MarkDirty(this);
            }
        }

        public void Modify(Vector2 value, ObservableModifyType type)
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
            return new SavedObservableField(FieldID, Type, $"{Value.x},{Value.y}");
        }

        public override ObservableField Clone()
        {
            return new Vector2Observable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}