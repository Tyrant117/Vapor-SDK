using System;

namespace VaporNetcode
{
    [Serializable]
    public class ULongField : SyncField
    {
        public static implicit operator ulong(ULongField f) => f.Value;

        public ulong Value { get; protected set; }
        public event Action<ULongField> ValueChanged;

        public ULongField(SyncClass @class, int fieldID, bool saveValue, ulong value) : base(@class, fieldID, saveValue)
        {
            Type = SyncFieldType.ULong;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public ULongField(int fieldID, bool saveValue, bool isServer, ulong value) : base(fieldID, saveValue, isServer)
        {
            Type = SyncFieldType.ULong;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetULong(ulong value)
        {
            if (Type == SyncFieldType.ULong)
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
            else
            {
                return false;
            }
        }

        internal bool ModifyULong(ulong value, SyncModifyType type)
        {
            return type switch
            {
                SyncModifyType.Set => SetULong(value),
                SyncModifyType.Add => SetULong(Value + value),
                SyncModifyType.Percent => SetULong(Value * value),
                SyncModifyType.PercentAdd => SetULong(Value + Value * value),
                _ => false,
            };
        }

        public void ExternalSet(ulong value)
        {
            if (SetULong(value))
            {
                if (IsServer)
                {
                    IsServerDirty = true;
                }
                Class?.MarkDirty(this);
            }
        }

        public void ExternalModify(ulong value, SyncModifyType type)
        {
            if (ModifyULong(value, type))
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
                Compression.CompressVarUInt(w, Value);
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
            return base.Deserialize(r) && SetULong(Compression.DecompressVarUInt(r));
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