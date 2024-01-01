using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VaporNetcode
{
    public class SyncBatcher
    {
        public bool IsDirty { get; private set; }
        public bool IsFirstUnbatch { get; private set; } = true;

        public Dictionary<Vector2Int, SyncClass> classMap = new(50);
        public Dictionary<int, SyncField> fieldMap = new(50);

        public Dictionary<Vector2Int, SyncClass> dirtyClasses = new(50);
        public Dictionary<int, SyncField> dirtyFields = new(50);

        public event Action<SyncClass> ClassCreated;
        public event Action<SyncField> FieldCreated;
        public event Action<int, int> FirstUnbatch;
        public event Action<int, int> Unbatched;

        private readonly NetworkWriter w;

        public SyncBatcher()
        {
            IsFirstUnbatch = true;
            w = new();
        }

        #region - Getters -
        public bool TryGetClass<T>(int uniqueID, out T sync) where T : SyncClass
        {
            var key = new Vector2Int(SyncClassID<T>.ID, uniqueID);
            if (classMap.TryGetValue(key, out var @class))
            {
                sync = @class as T;
                return true;
            }
            else
            {
                if (NetLogFilter.LogError)
                {
                    Debug.Log($"Class: {typeof(T)} with id {uniqueID} not found.");
                }
                sync = default;
                return false;
            }
        }

        public List<T> GetAllClassesOfType<T>() where T : SyncClass
        {
            int type = SyncClassID<T>.ID;
            List<T> result = new(classMap.Count);
            result.AddRange(from @class in classMap.Values
                            where @class.Type == type
                            select (T)@class);
            return result;
        }

        public bool TryGetField<T>(int fieldKey, out T sync) where T : SyncField
        {
            if (fieldMap.TryGetValue(fieldKey, out var field))
            {
                sync = field as T;
                return true;
            }
            else
            {
                if (NetLogFilter.LogError)
                {
                    Debug.Log($"Field: {typeof(T)} with id {fieldKey} not found.");
                }
                sync = default;
                return false;
            }
        }
        #endregion

        #region - Registration -
        public void RegisterSyncClass(SyncClass observableClass)
        {
            Vector2Int key = new(observableClass.Type, observableClass.ID);
            if (classMap.ContainsKey(key))
            {
                if (NetLogFilter.LogInfo && NetLogFilter.SyncVars) { Debug.Log($"Overwriting SyncClass at Key: {key}"); }
            }

            if (!dirtyClasses.ContainsKey(key))
            {
                dirtyClasses.Add(key, observableClass);
            }
            classMap[key] = observableClass;
            observableClass.Dirtied += SyncClass_Dirtied;
            IsDirty = true;
        }

        public void RegisterSyncField(SyncField observableField)
        {
            int key = observableField.FieldID;
            if (fieldMap.ContainsKey(key))
            {
                if (NetLogFilter.LogInfo && NetLogFilter.SyncVars) { Debug.Log($"Overwriting SyncField at Key: {key}"); }
            }

            if (!dirtyFields.ContainsKey(key))
            {
                dirtyFields.Add(key, observableField);
            }

            fieldMap[key] = observableField;
            observableField.Dirtied += SyncField_Dirtied;
            IsDirty = true;
        }
        #endregion

        #region - Dirty -
        private void SyncClass_Dirtied(SyncClass observableClass)
        {
            Vector2Int key = new(observableClass.Type, observableClass.ID);
            if (!dirtyClasses.ContainsKey(key))
            {
                dirtyClasses.Add(key, observableClass);
            }
            IsDirty = true;
        }

        private void SyncField_Dirtied(SyncField observableField)
        {
            if (!dirtyFields.ContainsKey(observableField.FieldID))
            {
                dirtyFields.Add(observableField.FieldID, observableField);
            }
            IsDirty = true;
        }
        #endregion

        #region - Batching -
        public SyncDataMessage Batch(bool full)
        {
            w.Reset();
            if (full)
            {
                _Full();
            }
            else
            {
                _Partial();
            }
            dirtyClasses.Clear();
            dirtyFields.Clear();
            IsDirty = false;


            var packet = new SyncDataMessage
            {
                data = w.ToArraySegment()
            };

            return packet;

            void _Full()
            {
                w.WriteInt(classMap.Count);
                foreach (var oc in classMap.Values)
                {
                    oc.SerializeInFull(w);
                }
                w.WriteInt(fieldMap.Count);
                foreach (var of in fieldMap.Values)
                {
                    of.SerializeInFull(w);
                }

                if (NetLogFilter.LogDebug && NetLogFilter.SyncVars && NetLogFilter.Spew)
                {
                    Debug.Log($"Fully Batching | Classes: {classMap.Count} Fields: {fieldMap.Count}");
                }
            }

            void _Partial()
            {
                w.WriteInt(dirtyClasses.Count);
                foreach (var oc in dirtyClasses.Values)
                {
                    oc.Serialize(w);
                }

                w.WriteInt(dirtyFields.Count);
                foreach (var of in dirtyFields.Values)
                {
                    of.Serialize(w);
                }
            }
        }

        public void Unbatch(SyncDataMessage packet)
        {
            using var r = NetworkReaderPool.Get(packet.data);
            int classCount = r.ReadInt();
            if (NetLogFilter.LogDebug && NetLogFilter.SyncVars && NetLogFilter.Spew)
            {
                Debug.Log($"Unbatching Classes: {classCount}");
            }
            for (int i = 0; i < classCount; i++)
            {
                SyncClass.StartDeserialize(r, out int type, out int id);
                var key = new Vector2Int(type, id);
                if (classMap.TryGetValue(key, out var @class))
                {
                    @class.Deserialize(r);
                }
                else
                {
                    if (SyncFieldFactory.TryCreateSyncClass(type, id, false, out SyncClass newClass))
                    {
                        classMap[key] = newClass;
                        if (NetLogFilter.LogDebug && NetLogFilter.SyncVars) { Debug.Log($"Unbatch and Create Class | {newClass.GetType().Name} [{id}]"); }
                        newClass.Deserialize(r);
                        ClassCreated?.Invoke(newClass);
                    }
                    else
                    {
                        if (NetLogFilter.LogError)
                        {
                            Debug.LogError($"SyncFieldFactory Does Not Implement Func To Create Class [{type}]");
                        }
                    }
                }
            }

            int fieldCount = r.ReadInt();
            if (NetLogFilter.LogDebug && NetLogFilter.SyncVars && NetLogFilter.Spew)
            {
                Debug.Log($"Unbatching Fields: {fieldCount}");
            }
            for (int i = 0; i < fieldCount; i++)
            {
                SyncField.StartDeserialize(r, out int id, out var type);
                if (fieldMap.TryGetValue(id, out var field))
                {
                    field.Deserialize(r);
                }
                else
                {
                    var newField = SyncField.GetFieldByType(id, type, false, false);
                    fieldMap[id] = newField;
                    if (NetLogFilter.LogDebug && NetLogFilter.SyncVars) { Debug.Log($"Unbatch and Create Field | {type} [{id}]"); }
                    newField.Deserialize(r);
                    FieldCreated?.Invoke(newField);
                }
            }
            if (IsFirstUnbatch)
            {
                IsFirstUnbatch = false;
                FirstUnbatch?.Invoke(classCount, fieldCount);
            }
            else
            {
                Unbatched?.Invoke(classCount, fieldCount);
            }
        }
        #endregion

        #region - Saving and Loading -
        public void Save(out List<SavedSyncClass> classes, out List<SavedSyncField> fields)
        {
            classes = new(classMap.Values.Count);
            fields = new(fieldMap.Values.Count);

            foreach (var @class in classMap.Values)
            {
                classes.Add(@class.Save());
            }

            foreach (var field in fieldMap.Values)
            {
                if (field.SaveValue)
                {
                    fields.Add(field.Save());
                }
            }
        }

        public void Load(List<SavedSyncClass> classes, List<SavedSyncField> fields)
        {
            foreach (var @class in classes)
            {
                Vector2Int key = new(@class.Type, @class.ID);
                if (classMap.TryGetValue(key, out var observable))
                {
                    observable.Load(@class);
                }
            }

            foreach (var field in fields)
            {
                if (fieldMap.TryGetValue(field.ID, out var observable))
                {
                    observable.Load(field);
                }
            }
        }

        public void Reload(List<SavedSyncClass> classes)
        {
            foreach (var @class in classes)
            {
                Vector2Int key = new(@class.Type, @class.ID);
                if (classMap.TryGetValue(key, out var observable))
                {
                    observable.Load(@class, forceReload: true);
                }
            }
        }
        #endregion
    }
}
