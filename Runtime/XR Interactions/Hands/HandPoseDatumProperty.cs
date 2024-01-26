

using VaporDataAssets;

namespace VaporXR
{
    /// <summary>
    /// Serializable container class that holds a <see cref="HandPose"/> value or a container asset reference.
    /// </summary>
    /// <seealso cref="HandPoseDatum"/>
    [System.Serializable]
    public class HandPoseDatumProperty : DatumProperty<HandPose, HandPoseDatum>
    {
        /// <inheritdoc/>
        public HandPoseDatumProperty(HandPose value) : base(value)
        {
        }

        /// <inheritdoc/>
        public HandPoseDatumProperty(HandPoseDatum datum) : base(datum)
        {
        }
    }
}
