using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using VaporInspector;

namespace VaporXR
{
    /// <summary>
    /// Data needed to describe a haptic impulse.
    /// </summary>
    [Serializable, DrawWithVapor(UIGroupType.Vertical)]
    public class HapticImpulseData
    {
        #region - Inspector -
        [SerializeField, Range(0f, 1f)] 
        [RichTextTooltip("The desired motor amplitude that should be within a [0-1] range.")]
        private float _amplitude;
        [SerializeField, Suffix("s")]
        [RichTextTooltip("The desired duration of the impulse in seconds.")]
        private float _duration;
        [SerializeField, Suffix("Hz")]
        [RichTextTooltip("The desired frequency of the impulse in Hz.\nThe default value of 0 means to use the default frequency of the device.\nNot all devices or XR Plug-ins support specifying a frequency.")]
        private float _frequency;
        #endregion

        #region - Properties -
        /// <summary>
        /// The desired motor amplitude that should be within a [0-1] range.
        /// </summary>
        public float Amplitude { get => _amplitude; set => _amplitude = value; }

        /// <summary>
        /// The desired duration of the impulse in seconds.
        /// </summary>
        public float Duration { get => _duration; set => _duration = value; }

        /// <summary>
        /// The desired frequency of the impulse in Hz.
        /// The default value of 0 means to use the default frequency of the device.
        /// Not all devices or XR Plug-ins support specifying a frequency.
        /// </summary>
        public float Frequency { get => _frequency; set => _frequency = value; }
        #endregion
    }
}
