using Unity.XR.CoreUtils.Datums;
using UnityEngine;

namespace VaporXR
{
    /// <summary>
    /// <see cref="ScriptableObject"/> container class that holds a <see cref="PokeThresholdData"/> value.
    /// </summary>
    [CreateAssetMenu(fileName = "PokeThresholdDatum", menuName = "Vapor/XR/Value Datums/Poke Threshold Datum", order = 0)]
    public class PokeThresholdDatum : Datum<PokeThresholdData>
    {

    }
}