using System;
using UnityEngine;

namespace VaporNetcode
{
    [Serializable]
    public class Vector3Field : SyncField
    {
        public static implicit operator Vector3(Vector3Field f) => f.Value;

        public Vector3 Value { get; protected set; }
        public event Action<Vector3Field, Vector3> ValueChanged;

        public Vector3Field(SyncClass @class, int fieldID, bool saveValue, Vector3 value) : base(@class, fieldID, saveValue)
        {
            Type = SyncFieldType.Vector3;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public Vector3Field(int fieldID, bool saveValue, bool isServer, Vector3 value) : base(fieldID, saveValue, isServer)
        {
            Type = SyncFieldType.Vector3;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetVector3(Vector3 value)
        {
            if (Type == SyncFieldType.Vector3)
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

        public void ExternalSet(Vector3 value)
        {
            if (SetVector3(value))
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
                w.WriteVector3(Value);
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
            return base.Deserialize(r) && SetVector3(r.ReadVector3());
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