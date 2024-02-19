#if AR_FOUNDATION_PRESENT || PACKAGE_DOCS_GENERATION
using UnityEngine.XR.Interaction.Toolkit.AR.Inputs;
#endif
using UnityEngine;
using UnityEngine.Serialization;
using VaporXR.Interactors;

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
        public bool CanProcess => isActiveAndEnabled;

        /// <inheritdoc />
        public bool Process(IVXRHoverInteractor interactor, IVXRHoverInteractable interactable)
        {
            return _screenTouchCountInput.ReadValue();
        }
    }
}
