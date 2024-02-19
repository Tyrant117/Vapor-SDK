using System;
using System.Globalization;

namespace VaporObservables
{
    /// <summary>
    /// The float implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="float"/>
    /// </summary>
    [Serializable]
    public class FloatObservable : ObservableField
    {
        public static implicit operator float(FloatObservable f) => f.Value;

        private float _value;
        /// <summary>
        /// The <see cref="float"/> value of the class.
        /// </summary>
        public float Value
        {
            get => _value;
            set
            {
                if (_value.Equals(value)) return;
                
                var oldValue = _value;
                _value = value;
                ValueChanged?.Invoke(this, oldValue);
                Class?.MarkDirty(this);
            }
        }
        public event Action<FloatObservable, float> ValueChanged;

        public FloatObservable(ObservableClassOld @class, int fieldID, bool saveValue, float value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Single;
            Value = value;
        }

        public FloatObservable(int fieldID, bool saveValue, float value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Single;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(float value)
        {
            _value = value;
        }
        #endregion

        #region - Saving -
        public override SavedObservableField Save()
        {
            return new SavedObservableField(FieldID, Type, Value.ToString(CultureInfo.InvariantCulture));
        }
        #endregion

        public override string ToString()
        {
            return $"{FieldID} [{Value}]";
        }

        public override ObservableField Clone()
        {
            return new FloatObservable(FieldID, SaveValue, Value);
        }
    }
}
