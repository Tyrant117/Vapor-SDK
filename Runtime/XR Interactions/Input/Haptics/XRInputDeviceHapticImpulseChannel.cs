using UnityEngine.XR;

namespace VaporXR
{
    /// <summary>
    /// Allows for sending haptic impulses to a channel on a device from the XR input subsystem.
    /// </summary>
    /// <seealso cref="IXRHapticImpulseChannel"/>
    /// <seealso cref="InputDevice.SendHapticImpulse"/>
    public class XRInputDeviceHapticImpulseChannel : IXRHapticImpulseChannel
    {
        /// <summary>
        /// The channel to receive the impulse.
        /// </summary>
        public int MotorChannel { get; set; }

        /// <summary>
        /// The input device to send the impulse to.
        /// </summary>
        public InputDevice Device { get; set; }

        /// <inheritdoc />
        public bool SendHapticImpulse(float amplitude, float duration, float frequency)
        {
            // InputDevice does not support sending frequency.
            return Device.SendHapticImpulse((uint)MotorChannel, amplitude, duration);
        }
    }
}
