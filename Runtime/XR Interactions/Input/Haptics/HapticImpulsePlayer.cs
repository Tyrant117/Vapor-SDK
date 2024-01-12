using UnityEngine;
using UnityEngine.Serialization;

namespace VaporXR
{
    /// <summary>
    /// Component that allows for sending haptic impulses to a device.
    /// </summary>
    public class HapticImpulsePlayer : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Specifies the output haptic control or controller that haptic impulses will be sent to.")]
        private XRInputDeviceHapticImpulseProvider _hapticOutput;
        /// <summary>
        /// Specifies the output haptic control or controller that haptic impulses will be sent to.
        /// </summary>
        public XRInputDeviceHapticImpulseProvider HapticOutput
        {
            get => _hapticOutput;
            set => _hapticOutput = value;
        }

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Amplitude multiplier which can be used to dampen the haptic impulses sent by this component.")]
        private float _amplitudeMultiplier = 1f;

        /// <summary>
        /// Amplitude multiplier which can be used to dampen the haptic impulses sent by this component.
        /// </summary>
        public float AmplitudeMultiplier
        {
            get => _amplitudeMultiplier;
            set => _amplitudeMultiplier = value;
        }

        /// <summary>
        /// Sends a haptic impulse on the configured channel or default channel of the configured device.
        /// </summary>
        /// <param name="amplitude">The desired motor amplitude that should be within a [0-1] range.</param>
        /// <param name="duration">The desired duration of the impulse in seconds.</param>
        /// <returns>Returns <see langword="true"/> if successful. Otherwise, returns <see langword="false"/>.</returns>
        /// <remarks>
        /// This method considers sending the haptic impulse a success (and thus returns <see langword="true"/>)
        /// if the haptic impulse was successfully sent to the device even if frequency is ignored or not supported by the device.
        /// <br />
        /// Uses the default frequency of the device.
        /// </remarks>
        public bool SendHapticImpulse(float amplitude, float duration) => SendHapticImpulse(amplitude, duration, 0f);

        /// <summary>
        /// Sends a haptic impulse on the configured channel or default channel of the configured device.
        /// </summary>
        /// <param name="amplitude">The desired motor amplitude that should be within a [0-1] range.</param>
        /// <param name="duration">The desired duration of the impulse in seconds.</param>
        /// <param name="frequency">The desired frequency of the impulse in Hz. A value of 0 means to use the default frequency of the device.</param>
        /// <returns>Returns <see langword="true"/> if successful. Otherwise, returns <see langword="false"/>.</returns>
        /// <remarks>
        /// This method considers sending the haptic impulse a success (and thus returns <see langword="true"/>)
        /// if the haptic impulse was successfully sent to the device even if frequency is ignored or not supported by the device.
        /// <br />
        /// Frequency is currently only functional when the OpenXR Plugin (com.unity.xr.openxr) package is installed
        /// and the input action is using an input binding to a Haptic Control.
        /// </remarks>
        public bool SendHapticImpulse(float amplitude, float duration, float frequency)
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            return _hapticOutput.GetChannelGroup()?.GetChannel()?.SendHapticImpulse(amplitude * _amplitudeMultiplier, duration, frequency) ?? false;
        }

        public static HapticImpulsePlayer GetOrCreateInHierarchy(GameObject gameObject)
        {
            var hapticImpulsePlayer = gameObject.GetComponentInParent<HapticImpulsePlayer>(true);
            if (hapticImpulsePlayer != null) return hapticImpulsePlayer;
            
            // Try to add the component in the hierarchy where it can be found and shared by other interactors.
            // Otherwise, just add it to this GameObject.
            var impulseProvider = gameObject.GetComponentInParent<IXRHapticImpulseProvider>(true);
            var impulseProviderComponent = impulseProvider as Component;
            hapticImpulsePlayer = impulseProviderComponent != null
                ? impulseProviderComponent.gameObject.AddComponent<HapticImpulsePlayer>()
                : gameObject.AddComponent<HapticImpulsePlayer>();

            return hapticImpulsePlayer;
        }
    }
}
