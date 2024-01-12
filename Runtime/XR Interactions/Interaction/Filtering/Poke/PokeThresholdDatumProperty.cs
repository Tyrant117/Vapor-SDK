using System;
using Unity.XR.CoreUtils.Datums;

namespace VaporXR
{
    /// <summary>
    /// Serializable container class that holds a poke threshold value or container asset reference.
    /// </summary>
    /// <seealso cref="PokeThresholdDatum"/>
    [Serializable]
    public class PokeThresholdDatumProperty : DatumProperty<PokeThresholdData, PokeThresholdDatum>
    {
        /// <inheritdoc/>
        public PokeThresholdDatumProperty(PokeThresholdData value) : base(value)
        {
        }

        /// <inheritdoc/>
        public PokeThresholdDatumProperty(PokeThresholdDatum datum) : base(datum)
        {
        }
    }
}