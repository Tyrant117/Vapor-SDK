using System;

namespace VaporNetcode
{
    [Serializable]
    public class StringField : SyncField
    {
        public static implicit operator string(StringField f) => f.Value;

        public string Value { get; protected set; }
        /// <summary>
        /// Returns the new and old values of the changed string. New -> Old
        /// </summary>
        public event Action<StringField, string> ValueChanged;

        public StringField(SyncClass @class, int fieldID, bool saveValue, string value) : base(@class, fieldID, saveValue)
        {
            Type = SyncFieldType.String;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        public StringField(int fieldID, bool saveValue, bool isServer, string value) : base(fieldID, saveValue, isServer)
        {
            Type = SyncFieldType.String;
            Value = value;
            if (IsServer)
            {
                IsServerDirty = true;
            }
        }

        #region - Setters -
        internal bool SetString(string value)
        {
            if (Type == SyncFieldType.String)
            {
                if (Value != value)
                {
                    var oldValue = Value;
                    Value = value;
                    ValueChanged?.Invoke(this, oldValue);
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

        public void ExternalSet(string value)
        {
            if (SetString(value))
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
                w.WriteString(Value);
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
            return base.Deserialize(r) && SetString(r.ReadString());
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