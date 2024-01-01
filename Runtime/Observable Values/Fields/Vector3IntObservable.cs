using System;
using UnityEngine;

namespace VaporObservables
{
    [Serializable]
    public class Vector3IntObservable : ObservableField
    {
        public static implicit operator Vector3Int(Vector3IntObservable f) => f.Value;

        public Vector3Int Value { get; protected set; }
        public event Action<Vector3IntObservable, Vector3Int> ValueChanged;

        public Vector3IntObservable(ObservableClass @class, int fieldID, bool saveValue, Vector3Int value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector3Int;
            Value = value;
        }

        public Vector3IntObservable(int fieldID, bool saveValue, Vector3Int value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Vector3Int;
            Value = value;
        }

        #region - Setters -
        internal bool InternalSet(Vector3Int value)
        {
            if (Value != value)
            {
                Vector3Int oldValue = Value;
                Value = value;
                ValueChanged?.Invoke(this, Value - oldValue);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal bool InternalModify(Vector3Int value, ObservableModifyType type) => type switch
        {
            ObservableModifyType.Set => InternalSet(value),
            ObservableModifyType.Add => InternalSet(Value + value),
            ObservableModifyType.Multiplier => InternalSet(Value * value),
            ObservableModifyType.PercentAdd => InternalSet(Value + Value * value),
            _ => false,
        };

        public void SetWithoutNotify(Vector3Int value)
        {
            Value = value;
        }

        public void Set(Vector3Int value)
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

        public void Modify(Vector3Int value, ObservableModifyType type)
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
            return new SavedObservableField(FieldID, Type, string.Format("{0},{1},{2}", Value.x, Value.y, Value.z));
        }
        #endregion

        public override string ToString()
        {
            return $"{FieldID} [{Value}]";
        }

        public override ObservableField Clone()
        {
            return new Vector3IntObservable(FieldID, SaveValue, Value);
        }
    }
}
