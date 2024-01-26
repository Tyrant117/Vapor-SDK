using UnityEngine;
using VaporDataAssets;

namespace VaporXR
{
    /// <summary>
    /// <see cref="ScriptableObject"/> container class that holds a <see cref="HandPose"/> value.
    /// </summary>
    [CreateAssetMenu(fileName = "HandPose", menuName = "Vapor/XR/Value Datums/Hand Pose")]
    public class HandPoseDatum : Datum<HandPose>
    {
        
    }
}
