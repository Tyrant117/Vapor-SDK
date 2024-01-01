using System;

namespace VaporNetcode
{
    [Serializable]
    public class IntField : SyncField
    {
        public static implicit operator int(IntField f) => f.Value;

        public bool IsEqual(IntField other) => Value == other.Value;
        public static bool IsEqual(IntField lhs, IntField rhs) => lhs.Value == rhs.Value;

        public int Value { get; protected set; }
        public bool HasFlag(int flagToCheck) => (Value & flagToCheck) != 0;
        public event Action<IntField, int> ValueChanged;


        public IntField(SyncClass @class, int fieldID, bool saveValue, int value) : base(@class, fieldID, saveValue)
        {
            Type = SyncFieldType.Int;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public IntField(int fieldID, bool saveValue, bool isServer, int value) : base(fieldID, saveValue, isServer)
        {
            Type = SyncFieldType.Int;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetInt(int value)
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

        internal bool ModifyInt(int value, SyncModifyType type)
        {
            return type switch
            {
                SyncModifyType.Set => SetInt(value),
                SyncModifyType.Add => SetInt(Value + value),
                SyncModifyType.Percent => SetInt(Value * value),
                SyncModifyType.PercentAdd => SetInt(Value + Value * value),
                _ => false,
            };
        }

        public void ExternalSet(int value)
        {
            if (SetInt(value))
            {
                if (IsServer)
                {
                    IsServerDirty = true;
                }
                Class?.MarkDirty(this);
            }
        }

        public void ExternalModify(int value, SyncModifyType type)
        {
            if (ModifyInt(value, type))
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
                w.WriteInt(Value);
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
            return base.Deserialize(r) && SetInt(r.ReadInt());
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