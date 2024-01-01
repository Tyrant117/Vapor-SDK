using System;

namespace VaporNetcode
{
    [Serializable]
    public class LongField : SyncField
    {
        public static implicit operator long(LongField f) => f.Value;

        public long Value { get; protected set; }
        public event Action<LongField, long> ValueChanged;

        public LongField(SyncClass @class, int fieldID, bool saveValue, long value) : base(@class, fieldID, saveValue)
        {
            Type = SyncFieldType.Long;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public LongField(int fieldID, bool saveValue, bool isServer, long value) : base(fieldID, saveValue, isServer)
        {
            Type = SyncFieldType.Long;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetLong(long value)
        {
            if (Type == SyncFieldType.Long)
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

        internal bool ModifyLong(long value, SyncModifyType type)
        {
            return type switch
            {
                SyncModifyType.Set => SetLong(value),
                SyncModifyType.Add => SetLong(Value + value),
                SyncModifyType.Percent => SetLong(Value * value),
                SyncModifyType.PercentAdd => SetLong(Value + Value * value),
                _ => false,
            };
        }

        public void ExternalSet(long value)
        {
            if (SetLong(value))
            {
                if (IsServer)
                {
                    IsServerDirty = true;
                }
                Class?.MarkDirty(this);
            }
        }

        public void ExternalModify(long value, SyncModifyType type)
        {
            if (ModifyLong(value, type))
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
                w.WriteLong(Value);
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
            return base.Deserialize(r) && SetLong(r.ReadLong());
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