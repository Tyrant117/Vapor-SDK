using UnityEngine;
using UnityEngine.XR;

namespace VaporXR
{
    /// <summary>
    /// A <see cref="ScriptableObject"/> that provides a <see cref="InputTrackingState"/> value from a device
    /// from the XR input subsystem as defined by its characteristics and feature usage string.
    /// Intended to be used with an <see cref="XRInputValueReader"/> as its object reference.
    /// </summary>
    [CreateAssetMenu(fileName = "XRInputDeviceInputTrackingStateValueReader", menuName = "Vapor/XR/Input Value Reader/InputTrackingState")]
    public class XRInputDeviceInputTrackingStateValueReader : XRInputDeviceValueReader<InputTrackingState>
    {
        /// <inheritdoc />
        public override InputTrackingState ReadValue() => ReadInputTrackingStateValue();

        public override float ReadValueAsFloat() => (int)ReadInputTrackingStateValue();

        /// <inheritdoc />
        public override bool TryReadValue(out InputTrackingState value) => TryReadInputTrackingStateValue(out value);
    }
}
