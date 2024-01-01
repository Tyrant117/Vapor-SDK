using System;
using UnityEngine;

namespace VaporObservables
{
    [Serializable]
    public struct SavedObservableField
    {
        public int ID;
        public ObservableFieldType Type;
        public string Value;

        public SavedObservableField(int id, ObservableFieldType type, string value)
        {
            ID = id;
            Type = type;
            Value = value;
        }
    }

    public abstract class ObservableField
    {
        public ObservableClass Class { get; }
        public int FieldID { get; }
        public bool SaveValue { get; }
        public ObservableFieldType Type { get; protected set; }

        protected ObservableField(ObservableClass @class, int fieldID, bool saveValue)
        {
            Class = @class;
            FieldID = fieldID;
            SaveValue = saveValue;
        }

        protected ObservableField(int fieldID, bool saveValue)
        {
            FieldID = fieldID;
            SaveValue = saveValue;
        }

        #region - Saving & Loading -
        public abstract SavedObservableField Save();

        public static ObservableField Load(SavedObservableField save)
        {
            var field = _AddFieldByType(save.ID, save.Type, true);
            _SetFromString(field, save.Value);
            return field;

            static ObservableField _AddFieldByType(int fieldID, ObservableFieldType type, bool saveValue)
            {
                return type switch
                {
                    ObservableFieldType.Int8 => new ByteObservable(fieldID, saveValue, 0),
                    ObservableFieldType.Int16 => new ShortObservable(fieldID, saveValue, 0),
                    ObservableFieldType.UInt16 => new UShortObservable(fieldID, saveValue, 0),
                    ObservableFieldType.Int32 => new IntObservable(fieldID, saveValue, 0),
                    ObservableFieldType.UInt32 => new UIntObservable(fieldID, saveValue, 0),
                    ObservableFieldType.Single => new FloatObservable(fieldID, saveValue, 0),
                    ObservableFieldType.Int64 => new LongObservable(fieldID, saveValue, 0),
                    ObservableFieldType.UInt64 => new ULongObservable(fieldID, saveValue, 0),
                    ObservableFieldType.Double => new DoubleObservable(fieldID, saveValue, 0),
                    ObservableFieldType.Vector2 => new Vector2Observable(fieldID, saveValue, Vector2.zero),
                    ObservableFieldType.Vector2Int => new Vector2IntObservable(fieldID, saveValue, Vector2Int.zero),
                    ObservableFieldType.Vector3 => new Vector3Observable(fieldID, saveValue, Vector3.zero),
                    ObservableFieldType.Vector3Int => new Vector3IntObservable(fieldID, saveValue, Vector3Int.zero),
                    ObservableFieldType.Color => new ColorObservable(fieldID, saveValue, Color.white),
                    ObservableFieldType.Quaternion => new QuaternionObservable(fieldID, saveValue, Quaternion.identity),
                    ObservableFieldType.String => new StringObservable(fieldID, saveValue, ""),
                    _ => null,
                };
            }

            static void _SetFromString(ObservableField field, string value)
            {
                if (value is null or "") { return; }

                switch (field.Type)
                {
                    case ObservableFieldType.Int8:
                        ((ByteObservable)field).Set(byte.Parse(value));
                        break;
                    case ObservableFieldType.Int16:
                        ((ShortObservable)field).Set(short.Parse(value));
                        break;
                    case ObservableFieldType.UInt16:
                        ((IntObservable)field).Set(ushort.Parse(value));
                        break;
                    case ObservableFieldType.Int32:
                        ((IntObservable)field).Set(int.Parse(value));
                        break;
                    case ObservableFieldType.UInt32:
                        ((UIntObservable)field).Set(uint.Parse(value));
                        break;
                    case ObservableFieldType.Single:
                        ((FloatObservable)field).Set(float.Parse(value));
                        break;
                    case ObservableFieldType.Int64:
                        ((LongObservable)field).Set(long.Parse(value));
                        break;
                    case ObservableFieldType.UInt64:
                        ((ULongObservable)field).Set(ulong.Parse(value));
                        break;
                    case ObservableFieldType.Double:
                        ((DoubleObservable)field).Set(double.Parse(value));
                        break;
                    case ObservableFieldType.Vector2:
                        string[] split2 = value.Split(new char[] { ',' });
                        ((Vector2Observable)field).Set(new Vector2(float.Parse(split2[0]), float.Parse(split2[1])));
                        break;
                    case ObservableFieldType.Vector2Int:
                        string[] split2i = value.Split(new char[] { ',' });
                        ((Vector2IntObservable)field).Set(new Vector2Int(int.Parse(split2i[0]), int.Parse(split2i[1])));
                        break;
                    case ObservableFieldType.Vector3:
                        string[] split3 = value.Split(new char[] { ',' });
                        ((Vector3Observable)field).Set(new Vector3(float.Parse(split3[0]), float.Parse(split3[1]), float.Parse(split3[2])));
                        break;
                    case ObservableFieldType.Vector3Int:
                        string[] split3i = value.Split(new char[] { ',' });
                        ((Vector3IntObservable)field).Set(new Vector3Int(int.Parse(split3i[0]), int.Parse(split3i[1]), int.Parse(split3i[2])));
                        break;
                    case ObservableFieldType.Color:
                        string[] color = value.Split(new char[] { ',' });
                        ((ColorObservable)field).Set(new Color(float.Parse(color[0]), float.Parse(color[1]), float.Parse(color[2]), float.Parse(color[3])));
                        break;
                    case ObservableFieldType.Quaternion:
                        string[] quat = value.Split(new char[] { ',' });
                        ((QuaternionObservable)field).Set(new Quaternion(float.Parse(quat[0]), float.Parse(quat[1]), float.Parse(quat[2]), float.Parse(quat[3])));
                        break;
                    case ObservableFieldType.String:
                        ((StringObservable)field).Set(value);
                        break;
                }
            }
        }        

        public abstract ObservableField Clone();
        #endregion
    }
}
