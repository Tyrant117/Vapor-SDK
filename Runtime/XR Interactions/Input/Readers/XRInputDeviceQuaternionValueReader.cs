using UnityEngine;

namespace VaporXR
{
    /// <summary>
    /// A <see cref="ScriptableObject"/> that provides a <see cref="Quaternion"/> value from a device
    /// from the XR input subsystem as defined by its characteristics and feature usage string.
    /// Intended to be used with an <see cref="XRInputValueReader"/> as its object reference.
    /// </summary>
    [CreateAssetMenu(fileName = "XRInputDeviceQuaternionValueReader", menuName = "Vapor/XR/Input Value Reader/Quaternion")]
    public class XRInputDeviceQuaternionValueReader : XRInputDeviceValueReader<Quaternion>
    {
        /// <inheritdoc />
        public override Quaternion ReadValue() => ReadQuaternionValue();

        public override float ReadValueAsFloat()
        {
            return Quaternion.Angle(ReadQuaternionValue(), Quaternion.identity);
        }

        /// <inheritdoc />
        public override bool TryReadValue(out Quaternion value) => TryReadQuaternionValue(out value);
    }
}
