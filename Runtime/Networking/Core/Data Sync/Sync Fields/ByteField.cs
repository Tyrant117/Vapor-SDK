using System;

namespace VaporNetcode
{
    [Serializable]
    public class ByteField : SyncField
    {
        public static implicit operator byte(ByteField f) => f.Value;
        public static implicit operator bool(ByteField f) => f.Bool;

        public byte Value { get; protected set; }
        public bool Bool => Value != 0;
        public event Action<ByteField, int> ValueChanged;

        public ByteField(SyncClass @class, int fieldID, bool saveValue, byte value) : base(@class, fieldID, saveValue)
        {
            Type = SyncFieldType.Byte;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public ByteField(int fieldID, bool saveValue, bool isServer, byte value) : base(fieldID, saveValue, isServer)
        {
            Type = SyncFieldType.Byte;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetByte(byte value)
        {
            if (Type == SyncFieldType.Byte)
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

        internal bool ModifyByte(byte value, SyncModifyType type)
        {
            return type switch
            {
                SyncModifyType.Set => SetByte(value),
                SyncModifyType.Add => SetByte((byte)(Value + value)),
                SyncModifyType.Percent => SetByte((byte)(Value * value)),
                SyncModifyType.PercentAdd => SetByte((byte)(Value + Value * value)),
                _ => false,
            };
        }

        public void ExternalSet(byte value)
        {
            if (SetByte(value))
            {
                if(IsServer)
                {
                    IsServerDirty = true;
                }
                Class?.MarkDirty(this);
            }
        }

        public void ExternalModify(byte value, SyncModifyType type)
        {
            if(ModifyByte(value, type))
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
                w.WriteByte(Value);
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
            return base.Deserialize(r) && SetByte(r.ReadByte());
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