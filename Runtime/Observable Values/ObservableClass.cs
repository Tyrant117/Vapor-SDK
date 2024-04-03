using Newtonsoft.Json;
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
        public string Name;
        public string ClassType;
        public SavedObservable[] SavedFields;

        public SavedObservableClass(string name, Type type, List<SavedObservable> fields)
        {
            Name = name;
            ClassType = type.AssemblyQualifiedName;
            SavedFields = fields.ToArray();
        }
    }

    public interface IObservedClass
    {
        void SetupFields(ObservableClass @class)
        {

        }
    }

    /// <summary>
    /// An abstract implementation of an observable class that can keep track of a collection of Observables.
    /// When a value is changed inside the monitored collection the entire class will be marked dirty.
    /// This class also facillitates serializing and deserializing the class into a json format.
    /// The one requirement of this class is it must implement a constructor that only implements the default string named arguments.
    /// <code>
    /// public class ChildObservableClass
    /// {
    ///     public ChildObservableClass(string className) : base(className) { }
    ///     
    ///     protected override SetupFields()
    ///     {
    ///         // Field initialization should be done here, not in the constructor.
    ///     }
    /// }
    /// </code>
    /// </summary>
    public abstract class ObservableClass
    {
        /// <summary>
        /// A unique name for this instance of the class.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets a field based on the ID and casts it to a type that inherits from <see cref="ObservableField"/>. There is no checking, will throw errors on invalid id.
        /// </summary>
        /// <param name="fieldID">The id of the field to retrieve</param>
        /// <typeparam name="T">The type to cast the field to</typeparam>
        /// <returns>The <see cref="ObservableField"/> of type T</returns>
        public T GetField<T>(string fieldName) where T : Observable => (T)Fields[fieldName];

        public T GetFieldValue<T>(string fieldName) where T : struct => GetField<Observable<T>>(fieldName).Value;

        public void SetFieldValue<T>(string fieldName, T value) where T : struct => GetField<Observable<T>>(fieldName).Value = value;

        protected readonly Dictionary<string, Observable> Fields = new();
        protected bool IsLoaded = false;

        /// <summary>
        /// This event is fired when the <see cref="ObservableField"/>s of the class change.
        /// </summary>
        public event Action<ObservableClass, Observable> Dirtied;

        protected ObservableClass(string className)
        {
            Name = className;
            SetupFields();
        }

        #region - Fields -
        /// <summary>
        /// This method should add all the default fields to the derived class.
        /// </summary>
        protected abstract void SetupFields();

        public Observable<T> AddField<T>(string fieldName, bool saveValue, T value, Action<Observable<T>, T> callback = null) where T : struct
        {
            if (!Fields.ContainsKey(fieldName))
            {
                var field = new Observable<T>(fieldName, saveValue, value).WithChanged(callback);
                field.WithDirtied(MarkDirty);
                Fields.Add(fieldName, field);
                MarkDirty(Fields[fieldName]);
                return field;
            }
            else
            {
                Debug.LogError($"Field [{fieldName}] already added to class {Name}");
                return (Observable<T>)Fields[fieldName];
            }
        }

        public void AddField(Observable field)
        {
            if (Fields.TryAdd(field.Name, field))
            {
                field.WithDirtied(MarkDirty);
                MarkDirty(field);
            }
            else
            {
                Debug.LogError($"Field [{field.Name}] already added to class {Name}");
            }
        }

        public void RemoveField(string fieldName)
        {
            if (Fields.TryGetValue(fieldName, out var field))
            {
                field.ClearCallbacks();
                Fields.Remove(fieldName);
            }
        }

        internal virtual void MarkDirty(Observable field)
        {
            Dirtied?.Invoke(this, field);
        }
        #endregion

        #region - Saving and Loading -
        public string SaveAsJson()
        {
            var save = Save();
            return JsonConvert.SerializeObject(save);
        }

        public SavedObservableClass Save()
        {
            List<SavedObservable> holder = new();
            foreach (var field in Fields.Values)
            {
                if (field.SaveValue)
                {
                    holder.Add(field.Save());
                }
            }
            return new SavedObservableClass(Name, GetType(), holder);
        }

        public static SavedObservableClass Load(string json)
        {
            return JsonConvert.DeserializeObject<SavedObservableClass>(json);
        }

        public void Load(SavedObservableClass load)
        {
            if (!IsLoaded)
            {
                //var valueType = Type.GetType(load.ClassType);
                //var result = Activator.CreateInstance(valueType, new object[] { load.Name }) as ObservableClass;
                if (load.SavedFields != null)
                {
                    foreach (var field in load.SavedFields)
                    {
                        var obs = Observable.Load(field);
                        if (Fields.ContainsKey(field.Name))
                        {
                            Fields[field.Name].SetValueBoxed(obs.GetValueBoxed());
                        }
                        else
                        {
                            AddField(obs);
                        }
                    }
                }
                IsLoaded = true;
            }
        }
        #endregion
    }

    public class ObservableClass<T> : ObservableClass where T : IObservedClass
    {
        public T ObservedClass { get; }

        public ObservableClass(string className, T observedClass) : base(className)
        {
            ObservedClass = observedClass;
            ObservedClass.SetupFields(this);
        }

        protected override void SetupFields()
        {

        }
    }
}
