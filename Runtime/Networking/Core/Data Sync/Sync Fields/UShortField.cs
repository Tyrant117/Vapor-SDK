using System;

namespace VaporNetcode
{
    public class UShortField : SyncField
    {
        public static implicit operator ushort(UShortField f) => f.Value;

        public ushort Value { get; protected set; }
        public event Action<UShortField> ValueChanged;

        public UShortField(SyncClass @class, int fieldID, bool saveValue, ushort value) : base(@class, fieldID, saveValue)
        {
            Type = SyncFieldType.UShort;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public UShortField(int fieldID, bool saveValue, bool isServer, ushort value) : base(fieldID, saveValue, isServer)
        {
            Type = SyncFieldType.UShort;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetShort(ushort value)
        {
            if (Type == SyncFieldType.UShort)
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

        public void ExternalSet(ushort value)
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
        #endregion

        #region - Serialization -
        public override bool Serialize(NetworkWriter w, bool clearDirtyFlag = true)
        {
            if (base.Serialize(w, clearDirtyFlag))
            {
                w.WriteUShort(Value);
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
            return base.Deserialize(r) && SetShort(r.ReadUShort());
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
