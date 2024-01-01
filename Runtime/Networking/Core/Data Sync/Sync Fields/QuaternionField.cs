using System;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public class QuaternionField : SyncField
    {
        public static implicit operator Quaternion(QuaternionField f) => f.Value;

        public Quaternion Value { get; protected set; }
        public event Action<QuaternionField> ValueChanged;

        public QuaternionField(SyncClass @class, int fieldID, bool saveValue, Quaternion value) : base(@class, fieldID, saveValue)
        {
            Type = SyncFieldType.Quaternion;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public QuaternionField(int fieldID, bool saveValue, bool isServer, Quaternion value) : base(fieldID, saveValue, isServer)
        {
            Type = SyncFieldType.Quaternion;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetQuaternion(Quaternion value)
        {
            if (Type == SyncFieldType.Quaternion)
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

        public void ExternalSet(Quaternion value)
        {
            if (SetQuaternion(value))
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
                w.WriteQuaternion(Value);
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
            return base.Deserialize(r) && SetQuaternion(r.ReadQuaternion());
        }
        #endregion

        #region - Saving -
        public override SavedSyncField Save()
        {
            return new SavedSyncField(FieldID, Type, $"{Value.x},{Value.y},{Value.z},{Value.w}");
        }
        #endregion
    }
}
