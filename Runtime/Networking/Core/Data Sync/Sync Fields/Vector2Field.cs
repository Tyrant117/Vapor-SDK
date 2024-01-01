using System;
using UnityEngine;

namespace VaporNetcode
{
    [Serializable]
    public class Vector2Field : SyncField
    {
        public static implicit operator Vector2(Vector2Field f) => f.Value;

        public Vector2 Value { get; protected set; }
        public event Action<Vector2Field, Vector2> ValueChanged;

        public Vector2Field(SyncClass @class, int fieldID, bool saveValue, Vector2 value) : base(@class, fieldID, saveValue)
        {
            Type = SyncFieldType.Vector2;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public Vector2Field(int fieldID, bool saveValue, bool isServer, Vector2 value) : base(fieldID, saveValue, isServer)
        {
            Type = SyncFieldType.Vector2;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetVector2(Vector2 value)
        {
            if (Type == SyncFieldType.Vector2)
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

        public void ExternalSet(Vector2 value)
        {
            if (SetVector2(value))
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
                w.WriteVector2(Value);
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
            return base.Deserialize(r) && SetVector2(r.ReadVector2());
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