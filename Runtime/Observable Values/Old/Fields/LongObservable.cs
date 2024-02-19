using System;

namespace VaporObservables
{
    /// <summary>
    /// The long implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="long"/>
    /// </summary>
    [Serializable]
    public class LongObservable : ObservableField
    {
        public static implicit operator long(LongObservable f) => f.Value;

        private long _value;
        /// <summary>
        /// The <see cref="long"/> value of the class.
        /// </summary>
        public long Value
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
        public event Action<LongObservable, long> ValueChanged;

        public LongObservable(ObservableClassOld @class, int fieldID, bool saveValue, long value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Int64;
            Value = value;
        }

        public LongObservable(int fieldID, bool saveValue, long value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Int64;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(long value)
        {
            _value = value;
        }
        #endregion

        #region - Saving -
        public override SavedObservableField Save()
        {
            return new SavedObservableField(FieldID, Type, Value.ToString());
        }

        public override ObservableField Clone()
        {
            return new LongObservable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}