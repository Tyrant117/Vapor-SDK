using System;
using System.Collections.Generic;
using UnityEngine;

namespace VaporObservables
{
    /// <summary>
    /// Container for a collection of saved fields.
    /// </summary>
    [Serializable]
    public struct SavedObservableClass
    {
        public int Type;
        public int ID;
        public SavedObservableField[] SavedFields;

        public SavedObservableClass(int type, int id, List<SavedObservableField> fields)
        {
            Type = type;
            ID = id;
            SavedFields = fields.ToArray();
        }
    }

    /// <summary>
    /// Base class for a collection of observable fields.
    /// </summary>
    public abstract class ObservableClass
    {
        /// <summary>
        /// A type id that is unique to the inherited class
        /// </summary>
        public int Type { get; protected set; }
        /// <summary>
        /// A unique id for this instance of the class.
        /// </summary>
        public int ID { get; protected set; }
        /// <summary>
        /// Gets an <see cref="ObservableField"/> based on the ID. There is no checking, will throw errors on invalid id.
        /// </summary>
        /// <param name="fieldID">The id of the field to retrieve</param>
        /// <returns>The <see cref="ObservableField"/></returns>
        public ObservableField GetField(int fieldID) => Fields[fieldID];
        /// <summary>
        /// Gets a field based on the ID and casts it to a type that inherits from <see cref="ObservableField"/>. There is no checking, will throw errors on invalid id.
        /// </summary>
        /// <param name="fieldID">The id of the field to retrieve</param>
        /// <typeparam name="T">The type to cast the field to</typeparam>
        /// <returns>The <see cref="ObservableField"/> of type T</returns>
        public T GetField<T>(int fieldID) where T : ObservableField => (T)Fields[fieldID];

        protected readonly Dictionary<int, ObservableField> Fields = new();
        protected bool IsLoaded;

        /// <summary>
        /// This event is fired when the <see cref="ObservableField"/>s of the class change.
        /// </summary>
        public event Action<ObservableClass> Dirtied;

        protected ObservableClass(int uniqueId)
        {
            ID = uniqueId;
            IsLoaded = false;
        }

        protected ObservableClass(int containerType, int uniqueId)
        {
            Type = containerType;
            ID = uniqueId;
            IsLoaded = false;
        }

        #region - Field Management -
        public void AddField(int fieldID, ObservableFieldType type, bool saveValue, object value = null)
        {
            var field = value == null ? AddFieldByType(fieldID, type, saveValue) : AddFieldByType(fieldID, type, saveValue, value);
            if (field != null)
            {
                Fields[fieldID] = field;
                MarkDirty(Fields[fieldID]);
            }
            else
            {
                Debug.Log($"Class {Type} - {ID} Failed To Add Field: {type} {fieldID}");
            }
        }

        public void AddField(ObservableField field)
        {
            Fields[field.FieldID] = field;
            MarkDirty(field);
        }

        protected ObservableField AddFieldByType(int fieldID, ObservableFieldType type, bool saveValue, object value)
        {
            return type switch
            {
                ObservableFieldType.Boolean => new BoolObservable(this, fieldID, saveValue, Convert.ToBoolean(value)),
                ObservableFieldType.Int8 => new ByteObservable(this, fieldID, saveValue, Convert.ToByte(value)),
                ObservableFieldType.Int16 => new ShortObservable(this, fieldID, saveValue, Convert.ToInt16(value)),
                ObservableFieldType.UInt16 => new UShortObservable(this, fieldID, saveValue, Convert.ToUInt16(value)),
                ObservableFieldType.Int32 => new IntObservable(this, fieldID, saveValue, Convert.ToInt32(value)),
                ObservableFieldType.UInt32 => new UIntObservable(this, fieldID, saveValue, Convert.ToUInt32(value)),
                ObservableFieldType.Single => new FloatObservable(this, fieldID, saveValue, Convert.ToSingle(value)),
                ObservableFieldType.Int64 => new LongObservable(this, fieldID, saveValue, Convert.ToInt64(value)),
                ObservableFieldType.UInt64 => new ULongObservable(this, fieldID, saveValue, Convert.ToUInt64(value)),
                ObservableFieldType.Double => new DoubleObservable(this, fieldID, saveValue, Convert.ToDouble(value)),
                ObservableFieldType.Vector2 => new Vector2Observable(this, fieldID, saveValue, (Vector2)value),
                ObservableFieldType.Vector2Int => new Vector2IntObservable(this, fieldID, saveValue, (Vector2Int)value),
                ObservableFieldType.Vector3 => new Vector3Observable(this, fieldID, saveValue, (Vector3)value),
                ObservableFieldType.Vector3Int => new Vector3IntObservable(this, fieldID, saveValue, (Vector3Int)value),
                ObservableFieldType.Color => new ColorObservable(this, fieldID, saveValue, (Color)value),
                ObservableFieldType.Quaternion => new QuaternionObservable(this, fieldID, saveValue, (Quaternion)value),
                ObservableFieldType.String => new StringObservable(this, fieldID, saveValue, Convert.ToString(value)),
                _ => null,
            };
        }

        protected ObservableField AddFieldByType(int fieldID, ObservableFieldType type, bool saveValue)
        {

            return type switch
            {
                ObservableFieldType.Boolean => new BoolObservable(this, fieldID, saveValue, false),
                ObservableFieldType.Int8 => new ByteObservable(this, fieldID, saveValue, 0),
                ObservableFieldType.Int16 => new ShortObservable(this, fieldID, saveValue, 0),
                ObservableFieldType.UInt16 => new UShortObservable(this, fieldID, saveValue, 0),
                ObservableFieldType.Int32 => new IntObservable(this, fieldID, saveValue, 0),
                ObservableFieldType.UInt32 => new UIntObservable(this, fieldID, saveValue, 0),
                ObservableFieldType.Single => new FloatObservable(this, fieldID, saveValue, 0),
                ObservableFieldType.Int64 => new LongObservable(this, fieldID, saveValue, 0),
                ObservableFieldType.UInt64 => new ULongObservable(this, fieldID, saveValue, 0),
                ObservableFieldType.Double => new DoubleObservable(this, fieldID, saveValue, 0),
                ObservableFieldType.Vector2 => new Vector2Observable(this, fieldID, saveValue, Vector2.zero),
                ObservableFieldType.Vector2Int => new Vector2IntObservable(this, fieldID, saveValue, Vector2Int.zero),
                ObservableFieldType.Vector3 => new Vector3Observable(this, fieldID, saveValue, Vector3.zero),
                ObservableFieldType.Vector3Int => new Vector3IntObservable(this, fieldID, saveValue, Vector3Int.zero),
                ObservableFieldType.Color => new ColorObservable(this, fieldID, saveValue, Color.white),
                ObservableFieldType.Quaternion => new QuaternionObservable(this, fieldID, saveValue, Quaternion.identity),
                ObservableFieldType.String => new StringObservable(this, fieldID, saveValue, ""),
                _ => null,
            };
        }

        internal virtual void MarkDirty(ObservableField field)
        {
            Dirtied?.Invoke(this);
        }
        #endregion

        #region - Saving & Loading -
        public SavedObservableClass Save()
        {
            List<SavedObservableField> holder = new();
            foreach (var field in Fields.Values)
            {
                if (field.SaveValue)
                {
                    holder.Add(field.Save());
                }
            }
            return new SavedObservableClass(Type, ID, holder);
        }

        public void Load(SavedObservableClass save, bool createMissingFields = true, bool forceReload = false)
        {
            if (IsLoaded && !forceReload) { return; }

            if(save.SavedFields != null)
            {
                foreach (var field in save.SavedFields)
                {
                    if (Fields.ContainsKey(field.ID))
                    {
                        SetFromObject(field.ID, field.Value);
                    }
                    else
                    {
                        if (!createMissingFields) { continue; }
                        AddField(field.ID, field.Type, true);
                        SetFromObject(field.ID, field.Value);
                    }
                }
            }
            IsLoaded = true;
        }

        protected void SetFromObject(int fieldID, object value)
        {
            if (value == null) { return; }
            if (!Fields.ContainsKey(fieldID)) { return; }

            switch (Fields[fieldID].Type)
            {
                case ObservableFieldType.Boolean:
                    GetField<BoolObservable>(fieldID).Value = (bool)value;
                    break;
                case ObservableFieldType.Int8:
                    GetField<ByteObservable>(fieldID).Value = (byte)value;
                    break;
                case ObservableFieldType.Int16:
                    GetField<ShortObservable>(fieldID).Value = (short)value;
                    break;
                case ObservableFieldType.UInt16:
                    GetField<UShortObservable>(fieldID).Value = (ushort)value;
                    break;
                case ObservableFieldType.Int32:
                    GetField<IntObservable>(fieldID).Value = (int)value;
                    break;
                case ObservableFieldType.UInt32:
                    GetField<UIntObservable>(fieldID).Value = (uint)value;
                    break;
                case ObservableFieldType.Single:
                    GetField<FloatObservable>(fieldID).Value = (float)value;
                    break;
                case ObservableFieldType.Int64:
                    GetField<LongObservable>(fieldID).Value = (long)value;
                    break;
                case ObservableFieldType.UInt64:
                    GetField<ULongObservable>(fieldID).Value = (ulong)value;
                    break;
                case ObservableFieldType.Double:
                    GetField<DoubleObservable>(fieldID).Value = (double)value;
                    break;
                case ObservableFieldType.Vector2:
                    GetField<Vector2Observable>(fieldID).Value = (Vector2)value;
                    break;
                case ObservableFieldType.Vector2Int:
                    GetField<Vector2IntObservable>(fieldID).Value = (Vector2Int)value;
                    break;
                case ObservableFieldType.Vector3:
                    GetField<Vector3Observable>(fieldID).Value = (Vector3)value;
                    break;
                case ObservableFieldType.Vector3Int:
                    GetField<Vector3IntObservable>(fieldID).Value = (Vector3Int)value;
                    break;
                case ObservableFieldType.Color:
                    GetField<ColorObservable>(fieldID).Value = (Color)value;
                    break;
                case ObservableFieldType.Quaternion:
                    GetField<QuaternionObservable>(fieldID).Value = (Quaternion)value;
                    break;
                case ObservableFieldType.String:
                    GetField<StringObservable>(fieldID).Value = (string)value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion
    }
}
