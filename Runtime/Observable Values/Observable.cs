using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace VaporObservables
{
    [Serializable]
    public struct SavedObservable
    {
        public string Name;
        public string ValueType;
        public object Value;

        public SavedObservable(string name, Type valueType, object value)
        {
            Name = name;
            ValueType = valueType.AssemblyQualifiedName;
            Value = value;
        }
    }

    [Serializable]
    public abstract class Observable
    {
        // ***** Properties ******
        /// <summary>
        /// The Id of the field.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// If true, the this value will be saved.
        /// </summary>
        public bool SaveValue { get; set; }

        // ***** Events ******
        public event Action<Observable> Dirtied;

        protected Observable(string name, bool saveValue)
        {
            Name = name;
            SaveValue = saveValue;
        }

        #region - Value -
        public abstract object GetValueBoxed();
        public abstract void SetValueBoxed(object value);
        #endregion

        #region - Events -
        internal Observable WithDirtied(Action<Observable> callback)
        {
            if (callback != null)
            {
                Dirtied += callback;
            }
            return this;
        }

        protected void OnDirtied()
        {
            Dirtied?.Invoke(this);
        }

        internal virtual void ClearCallbacks()
        {
            Dirtied = null;
        }
        #endregion

        #region - Saving and Loading -
        public abstract string SaveAsJson();
        public abstract SavedObservable Save();

        public static Observable Load(string json)
        {
            var load = JsonConvert.DeserializeObject<SavedObservable>(json);
            return Load(load);
        }

        public static Observable Load(SavedObservable load)
        {
            var valueType = Type.GetType(load.ValueType);
            Type loadType = typeof(Observable<>).MakeGenericType(valueType);
            var result = Activator.CreateInstance(loadType, new object[] { load.Name, true }) as Observable;
            //result.Name = load.Name;
            //result.SaveValue = true;
            if (IsPrimitiveOrDecimal(valueType))
            {
                object numeric = valueType switch
                {
                    var t when t == typeof(bool) => Convert.ToBoolean(load.Value),
                    var t when t == typeof(byte) => Convert.ToByte(load.Value),
                    var t when t == typeof(sbyte) => Convert.ToSByte(load.Value),
                    var t when t == typeof(short) => Convert.ToInt16(load.Value),
                    var t when t == typeof(ushort) => Convert.ToUInt16(load.Value),
                    var t when t == typeof(int) => Convert.ToInt32(load.Value),
                    var t when t == typeof(uint) => Convert.ToUInt32(load.Value),
                    var t when t == typeof(float) => Convert.ToSingle(load.Value),
                    var t when t == typeof(long) => Convert.ToInt64(load.Value),
                    var t when t == typeof(ulong) => Convert.ToUInt64(load.Value),
                    var t when t == typeof(double) => Convert.ToDouble(load.Value),
                    var t when t == typeof(decimal) => Convert.ToDecimal(load.Value),
                    var t when t == typeof(char) => Convert.ToChar(load.Value),
                    _ => load.Value
                };
                result.SetValueBoxed(numeric);
            }
            else
            {
                if (load.Value is JObject jobj)
                {
                    object wrapper = valueType switch
                    {
                        var t when t == typeof(Vector2) => jobj.ToObject<Vector2Wrapper>().ToObject(),
                        var t when t == typeof(Vector2Int) => jobj.ToObject<Vector2IntWrapper>().ToObject(),
                        var t when t == typeof(Vector3) => jobj.ToObject<Vector3Wrapper>().ToObject(),
                        var t when t == typeof(Vector3Int) => jobj.ToObject<Vector3IntWrapper>().ToObject(),
                        var t when t == typeof(Color) => jobj.ToObject<ColorWrapper>().ToObject(),
                        var t when t == typeof(Quaternion) => jobj.ToObject<QuaternionWrapper>().ToObject(),
                        _ => _ConvertFallBackToType(jobj.ToObject<FallbackObjectWrapper>())
                    };
                    //Debug.Log($"Setting Wrapped Value: {wrapper}");
                    result.SetValueBoxed(wrapper);
                }
                else
                {
                    Debug.LogWarning($"Unhandled Type {load.Value.GetType()}");
                    result.SetValueBoxed(load.Value);
                }
            }
            return result;

            static object _ConvertFallBackToType(FallbackObjectWrapper wrapper)
            {
                if (wrapper.ToObject() is JObject jobj)
                {
                    return jobj.ToObject(wrapper.ToType());
                }
                return null;
            }
        }
        
        protected static bool IsPrimitiveOrDecimal(Type type)
        {
            return type.IsPrimitive || type == typeof(decimal);
        }
        #endregion        
    }

    [Serializable]
    public class Observable<T> : Observable, IEquatable<Observable<T>> where T : struct
    {
        public static implicit operator T(Observable<T> f) => f.Value;
        public static bool operator ==(Observable<T> left, Observable<T> right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return left.Value.Equals(right.Value);
        }
        public static bool operator !=(Observable<T> left, Observable<T> right)
        {
            return !(left == right);
        }

        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                if (_value.Equals(value))
                {
                    return;
                }

                var oldValue = _value;
                _value = value;
                ValueChanged?.Invoke(this, oldValue);
                OnDirtied();
            }
        }

        public event Action<Observable<T>, T> ValueChanged; // Value and Old Value
        

        public Observable(string name, bool saveValue) : base(name, saveValue)
        {
            Value = default;
        }

        public Observable(string name, bool saveValue, T value) : base(name, saveValue)
        {
            Value = value;
        }

        #region - Value -
        public void SetWithoutNotify(T value)
        {
            _value = value;               
        }

        public override object GetValueBoxed()
        {
            return Value;
        }

        public override void SetValueBoxed(object value)
        {
            Assert.IsTrue(value is T, $"Value [{value}] is not correct type: {value.GetType()} | Expecting: {typeof(T)}");
            Value = (T)value;
        }
        #endregion

        #region - Events -
        public Observable<T> WithChanged(Action<Observable<T>, T> callback)
        {
            if (callback != null)
            {
                ValueChanged += callback;
            }
            return this;
        }

        internal override void ClearCallbacks()
        {
            base.ClearCallbacks();
            ValueChanged = null;
        }
        #endregion

        #region - Saving and Loading -
        public override string SaveAsJson()
        {
            var save = Save();
            return JsonConvert.SerializeObject(save);
        }

        public override SavedObservable Save()
        {
            if (IsPrimitiveOrDecimal(typeof(T)))
            {
                return new SavedObservable(Name, typeof(T), _value);
            }
            else
            {
                return new SavedObservable(Name, typeof(T), SupportedTypes.ToWrapper(_value));
            }
        }
        #endregion

        #region - Helpers -
        public Observable<T> Clone()
        {
            return new Observable<T>(Name, true, Value);
        }

        public override bool Equals(object other)
        {
            return other is Observable<T> val && Equals(val);
        }

        public bool Equals(Observable<T> other)
        {
            return _value.Equals(other._value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, SaveValue, _value);
        }

        public override string ToString()
        {
            return $"{Name} [{Value}]";
        }               
        #endregion
    }
}
