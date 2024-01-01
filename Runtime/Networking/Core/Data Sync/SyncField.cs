using System;
using UnityEngine;

namespace VaporNetcode
{
    [Serializable]
    public struct SavedSyncField
    {
        public int ID;
        public SyncFieldType Type;
        public string Value;

        public SavedSyncField(int id, SyncFieldType type, string value)
        {
            ID = id;
            Type = type;
            Value = value;
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteInt(ID);
            w.WriteInt((int)Type);
            w.WriteString(Value);
        }

        public static SavedSyncField Deserialize(NetworkReader r)
        {
            return new SavedSyncField(r.ReadInt(), (SyncFieldType)r.ReadInt(), r.ReadString());
        }
    }

    public abstract class SyncField
    {
        public SyncClass Class { get; }
        public int FieldID { get; }
        public SyncFieldType Type { get; protected set; }
        public bool SaveValue { get; }
        public bool IsServer { get; }

        private bool isServerDirty;
        public bool IsServerDirty
        {
            get => isServerDirty;
            protected set
            {
                if (value && value != isServerDirty)
                {
                    Dirtied?.Invoke(this);
                }
                isServerDirty = value;
            }
        }

        public event Action<SyncField> Dirtied;

        public SyncField(SyncClass @class, int fieldID, bool saveValue)
        {
            Class = @class;
            FieldID = fieldID;
            SaveValue = saveValue;
            IsServer = @class.IsServer;
        }

        public SyncField(int fieldID, bool saveValue, bool isServer)
        {
            Class = null;
            FieldID = fieldID;
            SaveValue = saveValue;
            IsServer = isServer;
        }        

        #region - Serialization -
        public virtual bool Serialize(NetworkWriter w, bool clearDirtyFlag = true)
        {
            if (IsServer && (IsServerDirty || !clearDirtyFlag))
            {
                w.WriteInt(FieldID);
                w.WriteByte((byte)Type);
                return true;
            }
            return false;
        }

        public virtual bool SerializeInFull(NetworkWriter w, bool clearDirtyFlag = true)
        {
            if (IsServer)
            {
                w.WriteInt(FieldID);
                w.WriteByte((byte)Type);
                return true;
            }
            return false;
        }

        public virtual bool Deserialize(NetworkReader r)
        {
            return !IsServer;
        }

        public static void StartDeserialize(NetworkReader r, out int id, out SyncFieldType type)
        {
            id = r.ReadInt();
            type = (SyncFieldType)r.ReadByte();
        }
        #endregion

        #region - Saving & Loading -
        public abstract SavedSyncField Save();

        public void Load(SavedSyncField observable)
        {
            if (observable.Value is null or "") { return; }

            switch (observable.Type)
            {
                case SyncFieldType.Byte:
                    if (this is ByteField bf)
                    {
                        bf.ExternalSet(byte.Parse(observable.Value));
                    }
                    break;
                case SyncFieldType.Short:
                    if (this is ShortField sf)
                    {
                        sf.ExternalSet(short.Parse(observable.Value));
                    }
                    break;
                case SyncFieldType.UShort:
                    if (this is UShortField usf)
                    {
                        usf.ExternalSet(ushort.Parse(observable.Value));
                    }
                    break;
                case SyncFieldType.Int:
                    if (this is IntField intF)
                    {
                        intF.ExternalSet(int.Parse(observable.Value));
                    }
                    break;
                case SyncFieldType.UInt:
                    if (this is UIntField uintF)
                    {
                        uintF.ExternalSet(uint.Parse(observable.Value));
                    }
                    break;
                case SyncFieldType.Float:
                    if (this is FloatField ff)
                    {
                        ff.ExternalSet(float.Parse(observable.Value));
                    }
                    break;
                case SyncFieldType.Long:
                    if (this is LongField lf)
                    {
                        lf.ExternalSet(long.Parse(observable.Value));
                    }
                    break;
                case SyncFieldType.ULong:
                    if (this is ULongField ulf)
                    {
                        ulf.ExternalSet(ulong.Parse(observable.Value));
                    }
                    break;
                case SyncFieldType.Double:
                    if (this is DoubleField df)
                    {
                        df.ExternalSet(double.Parse(observable.Value));
                    }
                    break;
                case SyncFieldType.Vector2:
                    if (this is Vector2Field v2f)
                    {
                        string[] split2 = observable.Value.Split(new char[] { ',' });
                        v2f.ExternalSet(new Vector2(float.Parse(split2[0]), float.Parse(split2[1])));
                    }
                    break;
                case SyncFieldType.Vector2Int:
                    if (this is Vector2IntField v2if)
                    {
                        string[] split2i = observable.Value.Split(new char[] { ',' });
                        v2if.ExternalSet(new Vector2Int(int.Parse(split2i[0]), int.Parse(split2i[1])));
                    }
                    break;
                case SyncFieldType.Vector3:
                    if (this is Vector3Field v3f)
                    {
                        string[] split3 = observable.Value.Split(new char[] { ',' });
                        v3f.ExternalSet(new Vector3(float.Parse(split3[0]), float.Parse(split3[1]), float.Parse(split3[2])));
                    }
                    break;
                case SyncFieldType.Vector3Int:
                    if (this is Vector3IntField v3if)
                    {
                        string[] split3i = observable.Value.Split(new char[] { ',' });
                        v3if.ExternalSet(new Vector3Int(int.Parse(split3i[0]), int.Parse(split3i[1]), int.Parse(split3i[2])));
                    }
                    break;
                case SyncFieldType.Vector3DeltaCompressed:
                    if (this is Vector3DeltaCompressedField v3df)
                    {
                        string[] split3 = observable.Value.Split(new char[] { ',' });
                        v3df.ExternalSet(new Vector3(float.Parse(split3[0]), float.Parse(split3[1]), float.Parse(split3[2])));
                    }
                    break;
                case SyncFieldType.Vector4:
                    if (this is Vector4Field v4f)
                    {
                        string[] split4 = observable.Value.Split(new char[] { ',' });
                        v4f.ExternalSet(new Vector4(float.Parse(split4[0]), float.Parse(split4[1]), float.Parse(split4[2]), float.Parse(split4[3])));
                    }
                    break;
                case SyncFieldType.Color:
                    if (this is ColorField colorf)
                    {
                        string[] color = observable.Value.Split(new char[] { ',' });
                        colorf.ExternalSet(new Color(float.Parse(color[0]), float.Parse(color[1]), float.Parse(color[2]), float.Parse(color[3])));
                    }
                    break;
                case SyncFieldType.Quaternion:
                    if (this is QuaternionField qf)
                    {
                        string[] quat = observable.Value.Split(new char[] { ',' });
                        qf.ExternalSet(new Quaternion(float.Parse(quat[0]), float.Parse(quat[1]), float.Parse(quat[2]), float.Parse(quat[3])));
                    }
                    break;
                case SyncFieldType.CompressedQuaternion:
                    if (this is CompressedQuaternionField cqf)
                    {
                        string[] cquat = observable.Value.Split(new char[] { ',' });
                        cqf.ExternalSet(new Quaternion(float.Parse(cquat[0]), float.Parse(cquat[1]), float.Parse(cquat[2]), float.Parse(cquat[3])));
                    }
                    break;
                case SyncFieldType.String:
                    if (this is StringField stf)
                    {
                        stf.ExternalSet(observable.Value);
                    }
                    break;                               
            }
        }
        
        public static SyncField CreateAndLoad(SavedSyncField save)
        {
            var field = _AddFieldByType(save.ID, save.Type, true);
            _SetFromString(field, save.Value);
            return field;

            static SyncField _AddFieldByType(int fieldID, SyncFieldType type, bool saveValue)
            {
                return type switch
                {
                    SyncFieldType.Byte => new ByteField(fieldID, saveValue, true, 0),
                    SyncFieldType.Short => new ShortField(fieldID, saveValue, true, 0),
                    SyncFieldType.UShort => new UShortField(fieldID, saveValue, true, 0),
                    SyncFieldType.Int => new IntField(fieldID, saveValue, true, 0),
                    SyncFieldType.UInt => new UIntField(fieldID, saveValue, true, 0),
                    SyncFieldType.Float => new FloatField(fieldID, saveValue, true, 0),
                    SyncFieldType.Long => new LongField(fieldID, saveValue, true, 0),
                    SyncFieldType.ULong => new ULongField(fieldID, saveValue, true, 0),
                    SyncFieldType.Double => new DoubleField(fieldID, saveValue, true, 0),
                    SyncFieldType.Vector2 => new Vector2Field(fieldID, saveValue, true, Vector2.zero),
                    SyncFieldType.Vector2Int => new Vector2IntField(fieldID, saveValue, true, Vector2Int.zero),
                    SyncFieldType.Vector3 => new Vector3Field(fieldID, saveValue, true, Vector3.zero),
                    SyncFieldType.Vector3Int => new Vector3IntField(fieldID, saveValue, true, Vector3Int.zero),
                    SyncFieldType.Color => new ColorField(fieldID, saveValue, true, Color.white),
                    SyncFieldType.Quaternion => new QuaternionField(fieldID, saveValue, true, Quaternion.identity),
                    SyncFieldType.String => new StringField(fieldID, saveValue, true, ""),
                    _ => null,
                };
            }

            static void _SetFromString(SyncField field, string value)
            {
                if (value is null or "") { return; }

                switch (field.Type)
                {
                    case SyncFieldType.Byte:
                        ((ByteField)field).ExternalSet(byte.Parse(value));
                        break;
                    case SyncFieldType.Short:
                        ((ShortField)field).ExternalSet(short.Parse(value));
                        break;
                    case SyncFieldType.UShort:
                        ((IntField)field).ExternalSet(ushort.Parse(value));
                        break;
                    case SyncFieldType.Int:
                        ((IntField)field).ExternalSet(int.Parse(value));
                        break;
                    case SyncFieldType.UInt:
                        ((UIntField)field).ExternalSet(uint.Parse(value));
                        break;
                    case SyncFieldType.Float:
                        ((FloatField)field).ExternalSet(float.Parse(value));
                        break;
                    case SyncFieldType.Long:
                        ((LongField)field).ExternalSet(long.Parse(value));
                        break;
                    case SyncFieldType.ULong:
                        ((ULongField)field).ExternalSet(ulong.Parse(value));
                        break;
                    case SyncFieldType.Double:
                        ((DoubleField)field).ExternalSet(double.Parse(value));
                        break;
                    case SyncFieldType.Vector2:
                        string[] split2 = value.Split(new char[] { ',' });
                        ((Vector2Field)field).ExternalSet(new Vector2(float.Parse(split2[0]), float.Parse(split2[1])));
                        break;
                    case SyncFieldType.Vector2Int:
                        string[] split2i = value.Split(new char[] { ',' });
                        ((Vector2IntField)field).ExternalSet(new Vector2Int(int.Parse(split2i[0]), int.Parse(split2i[1])));
                        break;
                    case SyncFieldType.Vector3:
                        string[] split3 = value.Split(new char[] { ',' });
                        ((Vector3Field)field).ExternalSet(new Vector3(float.Parse(split3[0]), float.Parse(split3[1]), float.Parse(split3[2])));
                        break;
                    case SyncFieldType.Vector3Int:
                        string[] split3i = value.Split(new char[] { ',' });
                        ((Vector3IntField)field).ExternalSet(new Vector3Int(int.Parse(split3i[0]), int.Parse(split3i[1]), int.Parse(split3i[2])));
                        break;
                    case SyncFieldType.Color:
                        string[] color = value.Split(new char[] { ',' });
                        ((ColorField)field).ExternalSet(new Color(float.Parse(color[0]), float.Parse(color[1]), float.Parse(color[2]), float.Parse(color[3])));
                        break;
                    case SyncFieldType.Quaternion:
                        string[] quat = value.Split(new char[] { ',' });
                        ((QuaternionField)field).ExternalSet(new Quaternion(float.Parse(quat[0]), float.Parse(quat[1]), float.Parse(quat[2]), float.Parse(quat[3])));
                        break;
                    case SyncFieldType.String:
                        ((StringField)field).ExternalSet(value);
                        break;
                }
            }
        }
        #endregion

        #region - Statics -
        public static SyncField GetFieldByType(int fieldID, SyncFieldType type, bool saveValue, bool isServer)
        {
            return type switch
            {
                SyncFieldType.Byte => new ByteField(fieldID, saveValue, isServer, 0),
                SyncFieldType.Short => new ShortField(fieldID, saveValue, isServer, 0),
                SyncFieldType.UShort => new UShortField(fieldID, saveValue, isServer, 0),
                SyncFieldType.Int => new IntField(fieldID, saveValue, isServer, 0),
                SyncFieldType.UInt => new UIntField(fieldID, saveValue, isServer, 0),
                SyncFieldType.Float => new FloatField(fieldID, saveValue, isServer, 0),
                SyncFieldType.Long => new LongField(fieldID, saveValue, isServer, 0),
                SyncFieldType.ULong => new ULongField(fieldID, saveValue, isServer, 0),
                SyncFieldType.Double => new DoubleField(fieldID, saveValue, isServer, 0),
                SyncFieldType.Vector2 => new Vector2Field(fieldID, saveValue, isServer, Vector2.zero),
                SyncFieldType.Vector2Int => new Vector2IntField(fieldID, saveValue, isServer, Vector2Int.zero),
                SyncFieldType.Vector3 => new Vector3Field(fieldID, saveValue, isServer, Vector3.zero),
                SyncFieldType.Vector3Int => new Vector3IntField(fieldID, saveValue, isServer, Vector3Int.zero),
                SyncFieldType.Vector3DeltaCompressed => new Vector3DeltaCompressedField(fieldID, saveValue, isServer, Vector3.zero),
                SyncFieldType.Vector4 => new Vector4Field(fieldID, saveValue, isServer, Vector4.zero),
                SyncFieldType.Color => new ColorField(fieldID, saveValue, isServer, Color.white),
                SyncFieldType.Quaternion => new QuaternionField(fieldID, saveValue, isServer, Quaternion.identity),
                SyncFieldType.CompressedQuaternion => new CompressedQuaternionField(fieldID, saveValue, isServer, Quaternion.identity),
                SyncFieldType.String => new StringField(fieldID, saveValue, isServer, ""),
                _ => null,
            };
        }
        #endregion
    }
}
