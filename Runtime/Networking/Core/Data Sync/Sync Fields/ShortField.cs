using System;

namespace VaporNetcode
{
    [Serializable]
    public class ShortField : SyncField
    {
        public static implicit operator short(ShortField f) => f.Value;

        public short Value { get; protected set; }
        public event Action<ShortField, int> ValueChanged;

        public ShortField(SyncClass @class, int fieldID, bool saveValue, short value) : base(@class, fieldID, saveValue)
        {
            Type = SyncFieldType.Short;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public ShortField(int fieldID, bool saveValue, bool isServer, short value) : base(fieldID, saveValue, isServer)
        {
            Type = SyncFieldType.Short;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetShort(short value)
        {
            if (Type == SyncFieldType.Short)
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

        internal bool ModifyShort(short value, SyncModifyType type)
        {
            return type switch
            {
                SyncModifyType.Set => SetShort(value),
                SyncModifyType.Add => SetShort((short)(Value + value)),
                SyncModifyType.Percent => SetShort((short)(Value * value)),
                SyncModifyType.PercentAdd => SetShort((short)(Value + Value * value)),
                _ => false,
            };
        }

        public void ExternalSet(short value)
        {
            if (SetShort(value))
            {
                if (IsServer)
                {
                    IsServerDirty = true;
                }
                Class?.MarkDirty(this);
            }
        }

        public void ExternalModify(short value, SyncModifyType type)
        {
            if (ModifyShort(value, type))
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
                w.WriteShort(Value);
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
            return base.Deserialize(r) && SetShort(r.ReadShort());
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