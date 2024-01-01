using System;
using UnityEngine;

namespace VaporNetcode
{
    public class CompressedQuaternionField : SyncField
    {
        public static implicit operator Quaternion(CompressedQuaternionField f) => f.Value;

        public Quaternion Value { get; protected set; }
        public event Action<CompressedQuaternionField> ValueChanged;

        public CompressedQuaternionField(SyncClass @class, int fieldID, bool saveValue, Quaternion value) : base(@class, fieldID, saveValue)
        {
            Type = SyncFieldType.CompressedQuaternion;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public CompressedQuaternionField(int fieldID, bool saveValue, bool isServer, Quaternion value) : base(fieldID, saveValue, isServer)
        {
            Type = SyncFieldType.CompressedQuaternion;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetQuaternion(Quaternion value)
        {
            if (Type == SyncFieldType.CompressedQuaternion)
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
                w.WriteUInt(Compression.CompressQuaternion(Value));
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
            return base.Deserialize(r) && SetQuaternion(Compression.DecompressQuaternion(r.ReadUInt()));
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
