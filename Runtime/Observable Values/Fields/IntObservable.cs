using System;

namespace VaporObservables
{
    /// <summary>
    /// The int implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="int"/>
    /// </summary>
    [Serializable]
    public class IntObservable : ObservableField
    {
        public static implicit operator int(IntObservable f) => f.Value;

        private int _value;
        /// <summary>
        /// The <see cref="int"/> value of the class.
        /// </summary>
        public int Value
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
        /// If this integer is being treated as a <see cref="FlagsAttribute"/>, check if a flag has been set.
        /// </summary>
        /// <param name="flagToCheck">The flag to check</param>
        /// <returns>True if the integer has the flag</returns>
        public bool HasFlag(int flagToCheck) => (Value & flagToCheck) != 0;
        public event Action<IntObservable, int> ValueChanged; // Value and Old Value

        public IntObservable(ObservableClass @class, int fieldID, bool saveValue, int value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Int32;
            Value = value;
        }

        public IntObservable(int fieldID, bool saveValue, int value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Int32;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(int value)
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
            return new IntObservable(FieldID, SaveValue, Value);
        }
    }
}
