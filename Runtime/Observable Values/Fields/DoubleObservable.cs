using System;
using System.Globalization;

namespace VaporObservables
{
    /// <summary>
    /// The double implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="double"/>
    /// </summary>
    [Serializable]
    public class DoubleObservable : ObservableField
    {
        public static implicit operator double(DoubleObservable f) => f.Value;

        private double _value;
        /// <summary>
        /// The <see cref="double"/> value of the class.
        /// </summary>
        public double Value
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
        public event Action<DoubleObservable, double> ValueChanged;

        public DoubleObservable(ObservableClass @class, int fieldID, bool saveValue, double value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Double;
            Value = value;
        }

        public DoubleObservable(int fieldID, bool saveValue, double value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Double;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(double value)
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
            return new DoubleObservable(FieldID, SaveValue, Value);
        }
    }
}
