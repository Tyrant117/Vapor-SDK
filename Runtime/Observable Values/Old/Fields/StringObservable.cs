using System;

namespace VaporObservables
{
    /// <summary>
    /// The string implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="string"/>
    /// </summary>
    [Serializable]
    public class StringObservable : ObservableField
    {
        public static implicit operator string(StringObservable f) => f.Value;

        private string _value;
        /// <summary>
        /// The <see cref="string"/> value of the class.
        /// </summary>
        public string Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                
                var oldValue = _value;
                _value = value;
                ValueChanged?.Invoke(this, oldValue);
                Class?.MarkDirty(this);
            }
        }
        /// <summary>
        /// Returns the new and old values of the changed string. Value -> Old
        /// </summary>
        public event Action<StringObservable, string> ValueChanged;

        public StringObservable(ObservableClassOld @class, int fieldID, bool saveValue, string value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.String;
            Value = value;
        }

        public StringObservable(int fieldID, bool saveValue, string value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.String;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(string value)
        {
            _value = value;
        }
        #endregion

        #region - Saving -
        public override SavedObservableField Save()
        {
            return new SavedObservableField(FieldID, Type, Value.ToString());
        }
        #endregion

        public override string ToString()
        {
            return $"{FieldID} [{Value}]";
        }

        public override ObservableField Clone()
        {
            return new StringObservable(FieldID, SaveValue, Value);
        }
    }
}
