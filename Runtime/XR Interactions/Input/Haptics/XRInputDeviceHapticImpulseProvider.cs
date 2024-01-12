using UnityEngine;
using UnityEngine.XR;

namespace VaporXR
{
    /// <summary>
    /// A <see cref="ScriptableObject"/> that provides a group of haptic impulse channels for a device
    /// from the XR input subsystem as defined by its characteristics
    /// </summary>
    [CreateAssetMenu(fileName = "XRInputDeviceHapticImpulseProvider", menuName = "Vapor/XR/Input Device Haptic Impulse Provider")]
    public class XRInputDeviceHapticImpulseProvider : ScriptableObject, IXRHapticImpulseProvider
    {
        // Inspector
        public InputDeviceCharacteristics Characteristics;

        private XRInputDeviceHapticImpulseChannelGroup _channelGroup;
        private InputDevice _inputDevice;

        /// <inheritdoc />
        public IXRHapticImpulseChannelGroup GetChannelGroup()
        {
            RefreshInputDeviceIfNeeded();
            _channelGroup ??= new XRInputDeviceHapticImpulseChannelGroup();
            _channelGroup.Initialize(_inputDevice);

            return _channelGroup;
        }

        private void RefreshInputDeviceIfNeeded()
        {
            if (!_inputDevice.isValid)
            {
                XRInputTrackingAggregator.TryGetDeviceWithExactCharacteristics(Characteristics, out _inputDevice);
            }
        }
    }
}
