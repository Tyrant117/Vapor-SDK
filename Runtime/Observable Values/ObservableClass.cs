using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporObservables
{
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

    public abstract class ObservableClass
    {
        public int Type { get; protected set; }
        public int ID { get; protected set; }
        public ObservableField GetField(int fieldID) => _fields[fieldID];
        public T GetField<T>(int fieldID) where T : ObservableField => (T)_fields[fieldID];

        protected Dictionary<int, ObservableField> _fields = new();
        protected bool _isLoaded;

        public event Action<ObservableClass> Dirtied;

        protected ObservableClass(int unqiueID)
        {
            ID = unqiueID;
            _isLoaded = false;
        }

        protected ObservableClass(int containerType, int unqiueID)
        {
            Type = containerType;
            ID = unqiueID;
            _isLoaded = false;
        }

        #region - Field Management -
        public void AddField(int fieldID, ObservableFieldType type, bool saveValue, object value = null)
        {
            ObservableField field = value == null ? AddFieldByType(fieldID, type, saveValue) : AddFieldByType(fieldID, type, saveValue, value);
            if (field != null)
            {
                _fields[fieldID] = field;
                MarkDirty(_fields[fieldID]);
            }
            else
            {
                Debug.Log($"Class {Type} - {ID} Failed To Add Field: {type} {fieldID}");
            }
        }

        public void AddField(ObservableField field)
        {
            _fields[field.FieldID] = field;
            MarkDirty(field);
        }

        protected ObservableField AddFieldByType(int fieldID, ObservableFieldType type, bool saveValue, object value)
        {
            return type switch
            {
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
            foreach (var field in _fields.Values)
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
            if (_isLoaded && !forceReload) { return; }

            if(save.SavedFields != null)
            {
                foreach (var field in save.SavedFields)
                {
                    if (_fields.ContainsKey(field.ID))
                    {
                        SetFromString(field.ID, field.Value);
                    }
                    else
                    {
                        if (!createMissingFields) { continue; }
                        AddField(field.ID, field.Type, true);
                        SetFromString(field.ID, field.Value);
                    }
                }
            }
            _isLoaded = true;
        }

        protected void SetFromString(int fieldID, string value)
        {
            if (value is null or "") { return; }
            if (!_fields.ContainsKey(fieldID)) { return; }

            switch (_fields[fieldID].Type)
            {
                case ObservableFieldType.Int8:
                    GetField<ByteObservable>(fieldID).Set(byte.Parse(value));
                    break;
                case ObservableFieldType.Int16:
                    GetField<ShortObservable>(fieldID).Set(short.Parse(value));
                    break;
                case ObservableFieldType.UInt16:
                    GetField<UShortObservable>(fieldID).Set(ushort.Parse(value));
                    break;
                case ObservableFieldType.Int32:
                    GetField<IntObservable>(fieldID).Set(int.Parse(value));
                    break;
                case ObservableFieldType.UInt32:
                    GetField<UIntObservable>(fieldID).Set(uint.Parse(value));
                    break;
                case ObservableFieldType.Single:
                    GetField<FloatObservable>(fieldID).Set(float.Parse(value));
                    break;
                case ObservableFieldType.Int64:
                    GetField<LongObservable>(fieldID).Set(long.Parse(value));
                    break;
                case ObservableFieldType.UInt64:
                    GetField<ULongObservable>(fieldID).Set(ulong.Parse(value));
                    break;
                case ObservableFieldType.Double:
                    GetField<DoubleObservable>(fieldID).Set(double.Parse(value));
                    break;
                case ObservableFieldType.Vector2:
                    string[] split2 = value.Split(new char[] { ',' });
                    GetField<Vector2Observable>(fieldID).Set(new Vector2(float.Parse(split2[0]), float.Parse(split2[1])));
                    break;
                case ObservableFieldType.Vector2Int:
                    string[] split2i = value.Split(new char[] { ',' });
                    GetField<Vector2IntObservable>(fieldID).Set(new Vector2Int(int.Parse(split2i[0]), int.Parse(split2i[1])));
                    break;
                case ObservableFieldType.Vector3:
                    string[] split3 = value.Split(new char[] { ',' });
                    GetField<Vector3Observable>(fieldID).Set(new Vector3(float.Parse(split3[0]), float.Parse(split3[1]), float.Parse(split3[2])));
                    break;
                case ObservableFieldType.Vector3Int:
                    string[] split3i = value.Split(new char[] { ',' });
                    GetField<Vector3IntObservable>(fieldID).Set(new Vector3Int(int.Parse(split3i[0]), int.Parse(split3i[1]), int.Parse(split3i[2])));
                    break;
                case ObservableFieldType.Color:
                    string[] color = value.Split(new char[] { ',' });
                    GetField<ColorObservable>(fieldID).Set(new Color(float.Parse(color[0]), float.Parse(color[1]), float.Parse(color[2]), float.Parse(color[3])));
                    break;
                case ObservableFieldType.Quaternion:
                    string[] quat = value.Split(new char[] { ',' });
                    GetField<QuaternionObservable>(fieldID).Set(new Quaternion(float.Parse(quat[0]), float.Parse(quat[1]), float.Parse(quat[2]), float.Parse(quat[3])));
                    break;
                case ObservableFieldType.String:
                    GetField<StringObservable>(fieldID).Set(value);
                    break;
            }
        }
        #endregion
    }
}
