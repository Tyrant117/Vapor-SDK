using UnityEngine;
using UnityEngine.Serialization;
using VaporXR.Utilities;

namespace VaporXR
{
    /// <summary>
    /// An adapter component that provides a <see cref="bool"/> and <see cref="float"/> value from a device
    /// from the XR input subsystem as defined by its characteristics and feature usage string.
    /// </summary>
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_XRInputDeviceButtonReader)]
    public sealed class XRInputDeviceButtonReader : MonoBehaviour, IXRInputButtonReader
    {
        [SerializeField]
        [Tooltip("The value that is read to determine whether the button is down.")]
        private XRInputDeviceBoolValueReader _boolValueReader;
        [SerializeField]
        [Tooltip("The value that is read to determine the scalar value that varies from 0 to 1.")]
        private XRInputDeviceFloatValueReader _floatValueReader;
        
        /// <summary>
        /// The value that is read to determine whether the button is down.
        /// </summary>
        public XRInputDeviceBoolValueReader BoolValueReader
        {
            get => _boolValueReader;
            set => _boolValueReader = value;
        }
        
        /// <summary>
        /// The value that is read to determine the scalar value that varies from 0 to 1.
        /// </summary>
        public XRInputDeviceFloatValueReader FloatValueReader
        {
            get => _floatValueReader;
            set => _floatValueReader = value;
        }

        private bool _isPerformed;
        private bool _wasPerformedThisFrame;

        private readonly UnityObjectReferenceCache<XRInputDeviceBoolValueReader> _boolValueReaderCache = new();
        private readonly UnityObjectReferenceCache<XRInputDeviceFloatValueReader> _floatValueReaderCache = new();

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        private void Awake()
        {
            if (_boolValueReader == null)
            {
                Debug.LogError("No bool value reader set for XRInputDeviceButtonReader.", this);
            }

            if (_floatValueReader == null)
            {
                Debug.LogError("No float value reader set for XRInputDeviceButtonReader.", this);
            }
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        private void Update()
        {
            var prevPerformed = _isPerformed;
            _isPerformed = TryGetBoolValueReader(out var reference) && reference.ReadValue();
            _wasPerformedThisFrame = !prevPerformed && _isPerformed;
        }

        /// <inheritdoc />
        public bool ReadIsPerformed()
        {
            return _isPerformed;
        }

        /// <inheritdoc />
        public bool ReadWasPerformedThisFrame()
        {
            return _wasPerformedThisFrame;
        }

        /// <inheritdoc />
        public float ReadValue()
        {
            return TryGetFloatValueReader(out var reference) ? reference.ReadValue() : default;
        }

        /// <inheritdoc />
        public bool TryReadValue(out float value)
        {
            if (TryGetFloatValueReader(out var reference))
            {
                return reference.TryReadValue(out value);
            }

            value = default;
            return false;
        }

        private bool TryGetBoolValueReader(out XRInputDeviceBoolValueReader reference) => _boolValueReaderCache.TryGet(_boolValueReader, out reference);

        private bool TryGetFloatValueReader(out XRInputDeviceFloatValueReader reference) => _floatValueReaderCache.TryGet(_floatValueReader, out reference);
    }
}
