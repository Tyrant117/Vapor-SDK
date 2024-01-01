using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public class Vector3DeltaCompressedField : SyncField
    {
        public static implicit operator Vector3(Vector3DeltaCompressedField f) => f.Value;

        public Vector3 Value { get; protected set; }
        public float Precision { get; set; } = 0.01f;
        public event Action<Vector3DeltaCompressedField, Vector3> ValueChanged;

        private Vector3Long _lastSerializedValue;
        private Vector3Long _lastDeserializedValue;

        public Vector3DeltaCompressedField(SyncClass @class, int fieldID, bool saveValue, Vector3 value) : base(@class, fieldID, saveValue)
        {
            Type = SyncFieldType.Vector3DeltaCompressed;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public Vector3DeltaCompressedField(int fieldID, bool saveValue, bool isServer, Vector3 value) : base(fieldID, saveValue, isServer)
        {
            Type = SyncFieldType.Vector3DeltaCompressed;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetVector3(Vector3 value)
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
                Compression.ScaleToLong(Value, Precision, out Vector3Long quantized);
                var deltaPos = quantized - _lastSerializedValue;
                //Debug.Log($"Serializing Delta: {deltaPos}");

                w.WriteBool(true); // is delta
                Compression.CompressVarInt(w, deltaPos.x);
                Compression.CompressVarInt(w, deltaPos.y);
                Compression.CompressVarInt(w, deltaPos.z);
                if (clearDirtyFlag)
                {
                    Compression.ScaleToLong(Value, Precision, out _lastSerializedValue);
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
            if (base.SerializeInFull(w, clearDirtyFlag))
            {
                w.WriteBool(false);
                w.WriteVector3(Value);
                Compression.ScaleToLong(Value, Precision, out _lastSerializedValue);
                if (clearDirtyFlag)
                {
                    IsServerDirty = false;
                }
                //Debug.Log($"V3D Serialize in Full {FieldID} {Type} {false} {Value}");
                return true;
            }
            else
            {
                //Debug.Log($"V3D Cannot Serialize in Full {IsServer} {IsServerDirty}");
                return false;
            }
        }

        public override bool Deserialize(NetworkReader r)
        {
            if (!base.Deserialize(r)) { return false; }
            bool delta = r.ReadBool();
            bool set;
            if (delta)
            {
                Vector3Long deltaValue = new(Compression.DecompressVarInt(r), Compression.DecompressVarInt(r), Compression.DecompressVarInt(r));
                //Debug.Log($"Deserializing Delta: {deltaValue}");
                Vector3Long quantized = _lastDeserializedValue + deltaValue;
                set = SetVector3(Compression.ScaleToFloat(quantized, Precision));
                Compression.ScaleToLong(Value, Precision, out _lastDeserializedValue);
            }
            else
            {
                set = SetVector3(r.ReadVector3());
                Compression.ScaleToLong(Value, Precision, out _lastDeserializedValue);
            }
            return set;
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
