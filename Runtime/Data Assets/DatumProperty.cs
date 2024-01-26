using System;
using UnityEngine;
using VaporInspector;

namespace VaporDataAssets
{
    /// <summary>
    /// Class used as a serialized field in a component for containing a typed value that is either
    /// directly serialized as a constant value or a reference to a <see cref="ScriptableObject"/> container for that data.
    /// </summary>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <typeparam name="TDatum">Datum asset type.</typeparam>
    /// <seealso cref="Datum{T}"/>
    [Serializable, DrawWithVapor]
    public class DatumProperty<TValue, TDatum> where TDatum : Datum<TValue>
    {
        
        /// <summary>
        /// Operator making it easy to treat the container property as the underlying internal value.
        /// </summary>
        /// <param name="datumProperty">Property to get the internal value of.</param>
        /// <returns>Returns internal value represented by the property.</returns>
        public static implicit operator TValue(DatumProperty<TValue, TDatum> datumProperty)
        {
            return datumProperty.Value;
        }
        
        
        [SerializeField]
        [RichTextTooltip("Signifies whether the property is a constant value or a datum asset reference.")]
        private bool _useConstant;

        [SerializeField, ShowIf("%_useConstant")]
        [RichTextTooltip("The constant value used if <c>m_UseConstant</c> is flagged as true.")]
        private TValue _constantValue;

        [SerializeField, HideIf("%_useConstant")]
        [RichTextTooltip("The datum asset reference used if <c>m_UseConstant</c> is flagged as false.")]
        private TDatum _asset;
        
        /// <summary>
        /// Accessor for internal value held by this container.
        /// Getter/Setter uses the constant value if this property is set to "Use Value"
        /// and the associated datum's value is referenced if this property is set to "Use Asset".
        /// </summary>
        public TValue Value
        {
            get => _useConstant ? _constantValue : Asset != null ? Asset.Value : default;
            set
            {
                if (_useConstant)
                {
                    _constantValue = value;
                }
                else
                {
                    Asset.Value = value;
                }
            }
        }
        
        /// <summary>
        /// The current constant value used when this property is set to "Use Value".
        /// </summary>
        public TValue ConstantValue => _constantValue;

        /// <summary>
        /// The current datum asset reference used when this property is set to "Use Asset".
        /// </summary>
        public Datum<TValue> Asset => _asset;

        /// <summary>
        /// Constructor setting initial value for the embedded constant.
        /// </summary>
        /// <param name="value">Initial value.</param>
        protected DatumProperty(TValue value)
        {
            _useConstant = true;
            _constantValue = value;
        }

        /// <summary>
        /// Constructor setting initial datum asset reference.
        /// </summary>
        /// <param name="datum">Datum asset reference.</param>
        protected DatumProperty(TDatum datum)
        {
            _useConstant = false;
            _asset = datum;
        }
    }
}
