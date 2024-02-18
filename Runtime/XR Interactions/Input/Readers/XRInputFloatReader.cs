using UnityEngine;
using UnityEngine.InputSystem;
using VaporInspector;
using VaporXR.Utilities;

namespace VaporXR
{
    [System.Serializable, DrawWithVapor(UIGroupType.Vertical)]
    public class XRInputFloatReader : IXRInputValueReader<float>
    {
#pragma warning disable IDE0051 // Remove unused private members
        private bool UseLegacyInput => _inputType == InputSourceType.Legacy;
        private bool UseNewInput => _inputType == InputSourceType.ActionReference;
#pragma warning restore IDE0051 // Remove unused private members
        // Inspector
        [SerializeField]
        private InputSourceType _inputType = InputSourceType.ActionReference;
        [SerializeField, ShowIf("$UseLegacyInput")]
        private XRInputDeviceFloatValueReader _reader;
        [SerializeField, ShowIf("$UseNewInput")]
        private InputActionReference _actionReference;

        readonly UnityObjectReferenceCache<InputActionReference> m_InputActionReferenceCache = new();
        private protected bool TryGetInputActionReference(out InputActionReference reference) =>
            m_InputActionReferenceCache.TryGet(_actionReference, out reference);

        public bool IsValid
        {
            get
            {
                return _inputType switch
                {
                    InputSourceType.None => false,
                    InputSourceType.Legacy => _reader != null,
                    InputSourceType.ActionReference => _actionReference != null,
                    _ => false,
                };
            }
        }

        public float ReadValue()
        {
            return _inputType switch
            {
                InputSourceType.None => 0,
                InputSourceType.Legacy => _reader.ReadValue(),
                InputSourceType.ActionReference => TryGetInputActionReference(out var actionReference) ? actionReference.action.ReadValue<float>() : 0f,
                _ => 0,
            };
        }

        public float ReadValueAsFloat()
        {
            return _inputType switch
            {
                InputSourceType.None => 0,
                InputSourceType.Legacy => _reader.ReadValue(),
                InputSourceType.ActionReference => TryGetInputActionReference(out var actionReference) ? actionReference.action.ReadValue<float>() : 0f,
                _ => 0,
            };
        }

        public bool TryReadValue(out float value)
        {
            value = 0;
            switch (_inputType)
            {
                case InputSourceType.None:
                    return false;
                case InputSourceType.Legacy:
                    return _reader.TryReadValue(out value);
                case InputSourceType.ActionReference:
                    value = TryGetInputActionReference(out var actionReference) ? actionReference.action.ReadValue<float>() : 0f;
                    return true;
                default:
                    return false;
            }
        }

        public override string ToString()
        {
            return _reader != null ? _reader.name : _actionReference != null ? _actionReference.name : "";
        }
    }
}
