using System;
using UnityEngine;

namespace VaporObservables
{
    public class Vector2IntObservable : ObservableField
    {
        public static implicit operator Vector2Int(Vector2IntObservable f) => f.Value;
        public Vector2Int Value { get; protected set; }
        public event Action<Vector2IntObservable, Vector2Int> ValueChanged;

        public Vector2IntObservable(ObservableClass @class, int fieldID, bool saveValue, Vector2Int value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector2Int;
            Value = value;
        }

        public Vector2IntObservable(int fieldID, bool saveValue, Vector2Int value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector2Int;
            Value = value;
        }

        #region - Setters -
        internal bool InternalSet(Vector2Int value)
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

        internal bool InternalModify(Vector2Int value, ObservableModifyType type) => type switch
        {
            ObservableModifyType.Set => InternalSet(value),
            ObservableModifyType.Add => InternalSet(Value + value),
            ObservableModifyType.Multiplier => InternalSet(Value * value),
            ObservableModifyType.PercentAdd => InternalSet(Value + Value * value),
            _ => false,
        };

        public void SetWithoutNotify(Vector2Int value)
        {
            Value = value;
        }

        public void Set(Vector2Int value)
        {
            if (InternalSet(value))
            {
                Class?.MarkDirty(this);
            }
        }

        public void Modify(int multiplier)
        {
            if (InternalSet(Value * multiplier))
            {
                Class?.MarkDirty(this);
            }
        }

        public void Modify(Vector2Int value, ObservableModifyType type)
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
            return new Vector2IntObservable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}