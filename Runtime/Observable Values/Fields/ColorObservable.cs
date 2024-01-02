using System;
using UnityEngine;

namespace VaporObservables
{
    /// <summary>
    /// The color implementation of an <see cref="ObservableField"/>. Can be implicitly cast to a <see cref="Color"/>
    /// </summary>
    [Serializable]
    public class ColorObservable : ObservableField
    {
        public static implicit operator Color(ColorObservable f) => f.Value;

        private Color _value;
        /// <summary>
        /// The <see cref="Color"/> value of the class.
        /// </summary>
        public Color Value
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
        public event Action<ColorObservable, Color> ValueChanged;

        public ColorObservable(ObservableClass @class, int fieldID, bool saveValue, Color value) : base(@class, fieldID, saveValue)
        {
            Type = ObservableFieldType.Color;
            Value = value;
        }

        public ColorObservable(int fieldID, bool saveValue, Color value) : base(fieldID, saveValue)
        {
            Type = ObservableFieldType.Color;
            Value = value;
        }

        #region - Setters -
        public void SetWithoutNotify(Color value)
        {
            _value = value;
        }
        #endregion

        #region - Saving -
        public override SavedObservableField Save()
        {
            return new SavedObservableField(FieldID, Type, $"{Value.r},{Value.g},{Value.b},{Value.a}");
        }

        public override ObservableField Clone()
        {
            return new ColorObservable(FieldID, SaveValue, Value);
        }
        #endregion
    }
}
