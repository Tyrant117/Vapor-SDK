using Unity.XR.CoreUtils.Datums;
using UnityEngine;

namespace VaporXR.Locomotion
{
    /// <summary>
    /// <see cref="ScriptableObject"/> container class that holds a <see cref="ClimbSettings"/> value.
    /// </summary>
    [CreateAssetMenu(fileName = "ClimbSettings", menuName = "VXR/Locomotion/Climb Settings")]
    public class ClimbSettingsDatum : Datum<ClimbSettings>
    {
        
    }
}