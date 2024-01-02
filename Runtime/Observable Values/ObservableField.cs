using System;
using UnityEngine;

namespace VaporObservables
{
    /// <summary>
    /// Container for saved field values.
    /// </summary>
    [Serializable]
    public struct SavedObservableField
    {
        public int ID;
        public ObservableFieldType Type;
        public object Value;

        public SavedObservableField(int id, ObservableFieldType type, object value)
        {
            ID = id;
            Type = type;
            Value = value;
        }
    }

    /// <summary>
    /// The base class for observable data.
    /// </summary>
    public abstract class ObservableField
    {
        /// <summary>
        /// The class this field is part of.
        /// </summary>
        public ObservableClass Class { get; }
        /// <summary>
        /// The Id of the field.
        /// </summary>
        public int FieldID { get; }
        /// <summary>
        /// If true, the this value will be saved.
        /// </summary>
        public bool SaveValue { get; }
        /// <summary>
        /// The type of value this field is.
        /// </summary>
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
            _SetFromObject(field, save.Value);
            return field;

            static ObservableField _AddFieldByType(int fieldID, ObservableFieldType type, bool saveValue)
            {
                return type switch
                {
                    ObservableFieldType.Boolean => new BoolObservable(fieldID, saveValue, false),
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

            static void _SetFromObject(ObservableField field, object value)
            {
                if (value == null) { return; }

                switch (field.Type)
                {
                    case ObservableFieldType.Boolean:
                        ((BoolObservable)field).Value = (bool)value;
                        break;
                    case ObservableFieldType.Int8:
                        ((ByteObservable)field).Value = (byte)value;
                        break;
                    case ObservableFieldType.Int16:
                        ((ShortObservable)field).Value = (short)value;
                        break;
                    case ObservableFieldType.UInt16:
                        ((IntObservable)field).Value = (int)value;
                        break;
                    case ObservableFieldType.Int32:
                        ((IntObservable)field).Value = (int)value;
                        break;
                    case ObservableFieldType.UInt32:
                        ((UIntObservable)field).Value = (uint)value;
                        break;
                    case ObservableFieldType.Single:
                        ((FloatObservable)field).Value = (float)value;
                        break;
                    case ObservableFieldType.Int64:
                        ((LongObservable)field).Value = (long)value;
                        break;
                    case ObservableFieldType.UInt64:
                        ((ULongObservable)field).Value = (ulong)value;
                        break;
                    case ObservableFieldType.Double:
                        ((DoubleObservable)field).Value = (double)value;
                        break;
                    case ObservableFieldType.Vector2:
                        ((Vector2Observable)field).Value = (Vector2)value;
                        break;
                    case ObservableFieldType.Vector2Int:
                        ((Vector2IntObservable)field).Value = (Vector2Int)value;
                        break;
                    case ObservableFieldType.Vector3:
                        ((Vector3Observable)field).Value = (Vector3)value;
                        break;
                    case ObservableFieldType.Vector3Int:
                        ((Vector3IntObservable)field).Value = (Vector3Int)value;
                        break;
                    case ObservableFieldType.Color:
                        ((ColorObservable)field).Value = (Color)value;
                        break;
                    case ObservableFieldType.Quaternion:
                        ((QuaternionObservable)field).Value = (Quaternion)value;
                        break;
                    case ObservableFieldType.String:
                        ((StringObservable)field).Value = (string)value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }        

        public abstract ObservableField Clone();
        #endregion
    }
}
