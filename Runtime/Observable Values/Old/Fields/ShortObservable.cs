using System;

namespace VaporObservables
{
    /// <summary>
    /// The short implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="short"/>
    /// </summary>
    [Serializable]
    public class ShortObservable : ObservableField
    {
        public static implicit operator short(ShortObservable f) => f.Value;

        private short _value;
        /// <summary>
        /// The <see cref="short"/> value of the class.
        /// </summary>
        public short Value
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
        public event Action<ShortObservable, int> ValueChanged;

        public ShortObservable(ObservableClassOld @class, int fieldID, bool saveValue, short value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Int16;
            Value = value;
        }

        public ShortObservable(int fieldID, bool saveValue, short value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Int16;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(short value)
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
            return new ShortObservable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}