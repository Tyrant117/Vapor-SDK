using System;

namespace VaporObservables
{
    /// <summary>
    /// The boolean implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="bool"/>
    /// </summary>
    [Serializable]
    public class BoolObservable : ObservableField
    {
        public static implicit operator bool(BoolObservable f) => f.Value;

        private bool _value;
        /// <summary>
        /// The <see cref="bool"/> value of the class.
        /// </summary>
        public bool Value
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
        /// The event that is fired when the <see cref="Value"/> changes. First new value and then the old value.
        /// </summary>
        public event Action<BoolObservable, bool> ValueChanged; // Value and Delta

        public BoolObservable(ObservableClassOld @class, int fieldID, bool saveValue, bool value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Boolean;
            Value = value;
        }

        public BoolObservable(int fieldID, bool saveValue, bool value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Boolean;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(bool value)
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
            return new BoolObservable(FieldID, SaveValue, Value);
        }
    }
}
