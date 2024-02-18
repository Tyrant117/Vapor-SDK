using UnityEngine;

namespace VaporXR
{
    /// <summary>
    /// A <see cref="ScriptableObject"/> that provides a <see cref="bool"/> value from a device
    /// from the XR input subsystem as defined by its characteristics and feature usage string.
    /// Intended to be used with an <see cref="XRInputValueReader"/> as its object reference
    /// or as part of an <see cref="XRInputDeviceButtonReader"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "XRInputDeviceBoolValueReader", menuName = "XR/Input Value Reader/bool")]
    public class XRInputDeviceBoolValueReader : XRInputDeviceValueReader<bool>
    {
        /// <inheritdoc />
        public override bool ReadValue() => ReadBoolValue();

        /// <summary>
        /// Reads the <see cref="ReadValue"/> converted to a float. 1f or 0f.
        /// </summary>
        /// <returns></returns>
        public override float ReadValueAsFloat() => ReadBoolValue() ? 1f : 0f;

        /// <inheritdoc />
        public override bool TryReadValue(out bool value) => TryReadBoolValue(out value);
    }
}
