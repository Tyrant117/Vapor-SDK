using System;
using UnityEngine;

namespace VaporNetcode
{
    public class Vector3IntField : SyncField
    {
        public static implicit operator Vector3Int(Vector3IntField f) => f.Value;

        public Vector3Int Value { get; protected set; }
        public event Action<Vector3IntField, Vector3Int> ValueChanged;

        public Vector3IntField(SyncClass @class, int fieldID, bool saveValue, Vector3Int value) : base(@class, fieldID, saveValue)
        {
            Type = SyncFieldType.Vector3Int;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public Vector3IntField(int fieldID, bool saveValue, bool isServer, Vector3Int value) : base(fieldID, saveValue, isServer)
        {
            Type = SyncFieldType.Vector3Int;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetVector3Int(Vector3Int value)
        {
            if (Type == SyncFieldType.Vector3Int)
            {
                if (Value != value)
                {
                    var old = Value;
                    Value = value;
                    ValueChanged?.Invoke(this, Value - old);
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

        public void ExternalSet(Vector3Int value)
        {
            if (SetVector3Int(value))
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
                w.WriteVector3Int(Value);
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
            return base.Deserialize(r) && SetVector3Int(r.ReadVector3Int());
        }
        #endregion

        #region - Saving -
        public override SavedSyncField Save()
        {
            return new SavedSyncField(FieldID, Type, $"{Value.x},{Value.y},{Value.z}");
        }
        #endregion
    }
}