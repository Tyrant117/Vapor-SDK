using System;
using UnityEngine;

namespace VaporNetcode
{
    public class Vector2IntField : SyncField
    {
        public static implicit operator Vector2Int(Vector2IntField f) => f.Value;
        public Vector2Int Value { get; protected set; }
        public event Action<Vector2IntField, Vector2Int> ValueChanged;

        public Vector2IntField(SyncClass @class, int fieldID, bool saveValue, Vector2Int value) : base(@class, fieldID, saveValue)
        {
            Type = SyncFieldType.Vector2Int;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public Vector2IntField(int fieldID, bool saveValue, bool isServer, Vector2Int value) : base(fieldID, saveValue, isServer)
        {
            Type = SyncFieldType.Vector2Int;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetVector2Int(Vector2Int value)
        {
            if (Type == SyncFieldType.Vector2Int)
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

        public void ExternalSet(Vector2Int value)
        {
            if (SetVector2Int(value))
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
                w.WriteVector2Int(Value);
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
            return base.Deserialize(r) && SetVector2Int(r.ReadVector2Int());
         }
        #endregion

        #region - Saving -
        public override SavedSyncField Save()
        {
            return new SavedSyncField(FieldID, Type, $"{Value.x},{Value.y}");
        }
        #endregion
    }
}