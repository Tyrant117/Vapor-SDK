using System;

namespace VaporNetcode
{
    [Serializable]
    public class DoubleField : SyncField
    {
        public static implicit operator double(DoubleField f) => f.Value;

        public double Value { get; protected set; }
        public event Action<DoubleField, double> ValueChanged;

        public DoubleField(SyncClass @class, int fieldID, bool saveValue, double value) : base(@class, fieldID, saveValue)
        {
            Type = SyncFieldType.Double;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public DoubleField(int fieldID, bool saveValue, bool isServer, double value) : base(fieldID, saveValue, isServer)
        {
            Type = SyncFieldType.Double;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }


        #region - Setters -
        internal bool SetDouble(double value)
        {
            if (Type == SyncFieldType.Double)
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
            else
            {
                return false;
            }
        }
        internal bool ModifyDouble(double value, SyncModifyType type)
        {
            return type switch
            {
                SyncModifyType.Set => SetDouble(value),
                SyncModifyType.Add => SetDouble(Value + value),
                SyncModifyType.Percent => SetDouble(Value * value),
                SyncModifyType.PercentAdd => SetDouble(Value + Value * value),
                _ => false,
            };
        }

        public void ExternalSet(double value)
        {
            if (SetDouble(value))
            {
                if (IsServer)
                {
                    IsServerDirty = true;
                }
                Class?.MarkDirty(this);
            }
        }

        public void ExternalModify(double value, SyncModifyType type)
        {
            if (ModifyDouble(value, type))
            {
                if (IsServer)
                {
                    IsServerDirty = true;
                }
                Class?.MarkDirty(this);
            }
        }
        #endregion

        #region - Serialization -
        public override bool Serialize(NetworkWriter w, bool clearDirtyFlag = true)
        {
            if (base.Serialize(w, clearDirtyFlag))
            {
                w.WriteDouble(Value);
                if (clearDirtyFlag)
                {
                    IsServerDirty = false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool SerializeInFull(NetworkWriter w, bool clearDirtyFlag = true)
        {
            if (IsServer && clearDirtyFlag)
            {
                IsServerDirty = false;
            }
            return Serialize(w, false);
        }

        public override bool Deserialize(NetworkReader r)
        {
            return base.Deserialize(r) && SetDouble(r.ReadDouble());
        }
        #endregion

        #region - Saving -
        public override SavedSyncField Save()
        {
            return new SavedSyncField(FieldID, Type, Value.ToString());
        }
        #endregion
    }
}