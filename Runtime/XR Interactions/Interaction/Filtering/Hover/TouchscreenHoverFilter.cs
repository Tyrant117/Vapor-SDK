#if AR_FOUNDATION_PRESENT || PACKAGE_DOCS_GENERATION
using UnityEngine.XR.Interaction.Toolkit.AR.Inputs;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace VaporXR
{
    /// <summary>
    /// Hover filter that checks if the screen is being touched.
    /// Can be used with the ray interactor to prevent hover interactions when the screen is not being touched.
    /// </summary>
    public class TouchscreenHoverFilter : MonoBehaviour, IXRHoverFilter
    {
        [SerializeField] 
        private XRInputDeviceBoolValueReader _screenTouchCountInput;

        /// <summary>
        /// The input used to read the screen touch count value.
        /// </summary>
        /// <seealso cref="TouchscreenGestureInputController.fingerCount"/>
        public XRInputDeviceBoolValueReader ScreenTouchCountInput
        {
            get => _screenTouchCountInput;
            set => _screenTouchCountInput = value;
        }

        /// <inheritdoc />
        public bool canProcess => isActiveAndEnabled;

        /// <inheritdoc />
        public bool Process(VXRBaseInteractor interactor, IXRHoverInteractable interactable)
        {
            return _screenTouchCountInput.ReadValue();
        }
    }
}
