using System;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using UnityEngine.XR;
using VaporInspector;
#if XR_HANDS_1_3_OR_NEWER
using UnityEngine.XR.Hands;
#endif

namespace VaporXR
{
    /// <summary>
    /// Manages swapping between hands and controllers at runtime based on whether hands and controllers are tracked.
    /// </summary>
    /// <remarks>
    /// This component uses the following logic for determining which modality is active:
    /// If a hand begin tracking, this component will switch to the hand group of interactors.
    /// If the player wakes the motion controllers by grabbing them, this component will switch to the motion controller group of interactors
    /// once they become tracked. While waiting to activate the controller GameObject while not tracked, both groups will be deactivated.
    /// <br />
    /// This component is useful even when a project does not use hand tracking. By assigning the motion controller set of GameObjects,
    /// this component will keep them deactivated until the controllers become tracked to avoid showing the controllers at the default
    /// origin position.
    /// </remarks>
    public class VXRInputModalityManager : MonoBehaviour
    {
        /// <summary>
        /// The mode of an individual hand.
        /// </summary>
        public enum InputMode
        {
            /// <summary>
            /// Neither mode. This is also the mode when waiting for the motion controller to be tracked.
            /// Toggle off both sets of GameObjects.
            /// </summary>
            None,

            /// <summary>
            /// The user is using hand tracking for their hand input.
            /// Swap to the Hand Tracking GameObject for the hand.
            /// </summary>
            TrackedHand,

            /// <summary>
            /// The user is using a motion controller for their hand input.
            /// Swap to the Motion Controllers GameObject for the hand.
            /// </summary>
            MotionController,
        }

        private static readonly BindableEnum<InputMode> _currentInputMode = new BindableEnum<InputMode>(InputMode.None);

        /// <summary>
        /// Static bindable variable used to track the current input mode.
        /// </summary>
        public static IReadOnlyBindableVariable<InputMode> CurrentInputMode => _currentInputMode;

        #region Inspector
#if XR_HANDS_1_3_OR_NEWER
        [FoldoutGroup("HandTracking", "Hand Tracking")]
#else
        [HideInInspector]
#endif
        [SerializeField]
        [RichTextTooltip("GameObject representing the left hand group of interactors. Will toggle on when using hand tracking and off when using motion controllers.")]
        private GameObject _mLeftHand;
#if XR_HANDS_1_3_OR_NEWER
        [FoldoutGroup("HandTracking")]
#else
        [HideInInspector]
#endif
        [SerializeField]
        [RichTextTooltip("GameObject representing the right hand group of interactors. Will toggle on when using hand tracking and off when using motion controllers.")]
        private GameObject _rightHand;

        [SerializeField, FoldoutGroup("Motion Controllers")]
        [RichTextTooltip("<cls>GameObject</cls> representing the left motion controller group of interactors. Will toggle on when using motion controllers and off when using hand tracking.")]
        private GameObject _leftController;

        [SerializeField, FoldoutGroup("MotionControllers")]
        [RichTextTooltip("<cls>GameObject</cls> representing the left motion controller group of interactors. Will toggle on when using motion controllers and off when using hand tracking.")]
        private GameObject _rightController;
        #endregion

        #region Properties
        /// <summary>
        /// GameObject representing the left hand group of interactors. Will toggle on when using hand tracking and off when using motion controllers.
        /// </summary>
        public GameObject LeftHand
        {
            get => _mLeftHand;
            set => _mLeftHand = value;
        }

        /// <summary>
        /// GameObject representing the right hand group of interactors. Will toggle on when using hand tracking and off when using motion controllers.
        /// </summary>
        public GameObject RightHand
        {
            get => _rightHand;
            set => _rightHand = value;
        }

        /// <summary>
        /// GameObject representing the left motion controller group of interactors. Will toggle on when using motion controllers and off when using hand tracking.
        /// </summary>
        public GameObject LeftController
        {
            get => _leftController;
            set => _leftController = value;
        }

        /// <summary>
        /// GameObject representing the left motion controller group of interactors. Will toggle on when using motion controllers and off when using hand tracking.
        /// </summary>
        public GameObject RightController
        {
            get => _rightController;
            set => _rightController = value;
        }
        #endregion

        #region Fields
#if XR_HANDS_1_3_OR_NEWER
        XRHandSubsystem m_HandSubsystem;
        bool m_LoggedMissingHandSubsystem;
#endif

        /// <summary>
        /// Monitor used for waiting until a controller device from the XR module becomes tracked.
        /// </summary>
        private readonly InputDeviceMonitor _inputDeviceMonitor = new InputDeviceMonitor();

        private InputMode _leftInputMode;
        private InputMode _rightInputMode;
        #endregion

        #region Events
        /// <summary>
        /// Calls the methods in its invocation list when hand tracking mode is started.
        /// </summary>
        /// <remarks>
        /// This event does not fire again for the other hand if the first already started this mode.
        /// </remarks>
        public event Action TrackedHandModeStarted;
        
        /// <summary>
        /// Calls the methods in its invocation list when both hands have stopped hand tracking mode.
        /// </summary>
        public event Action TrackedHandModeEnded;
        
        /// <summary>
        /// Calls the methods in its invocation list when motion controller mode is started.
        /// </summary>
        /// <remarks>
        /// This event does not fire again for the other hand if the first already started this mode.
        /// </remarks>
        public event Action MotionControllerModeStarted;

        /// <summary>
        /// Calls the methods in its invocation list when both hands have stopped motion controller mode.
        /// </summary>
        public event Action MotionControllerModeEnded;

        protected virtual void OnTrackedHandModeStarted()
        {
            TrackedHandModeStarted?.Invoke();
        }
        
        protected virtual void OnTrackedHandModeEnded()
        {
            TrackedHandModeEnded?.Invoke();
        }

        protected virtual void OnMotionControllerModeEnded()
        {
            MotionControllerModeEnded?.Invoke();
        }

        protected virtual void OnMotionControllerModeStarted()
        {
            MotionControllerModeStarted?.Invoke();
        }
        #endregion

        #region - Unity Events -
        protected void OnEnable()
        {
#if XR_HANDS_1_3_OR_NEWER
            if (m_HandSubsystem == null || !m_HandSubsystem.running)
            {
                // We don't log here if the hand subsystem is missing because the subsystem may not yet be added
                // if manually done by other behaviors during the first frame's Awake/OnEnable/Start.
                XRInputTrackingAggregator.TryGetHandSubsystem(out m_HandSubsystem);
            }
#else
            if (_mLeftHand != null || _rightHand != null)
            {
                Debug.LogWarning(
                    "Script requires XR Hands (com.unity.xr.hands) package to switch to hand tracking groups. Install using Window > Package Manager or click Fix on the related issue in Edit > Project Settings > XR Plug-in Management > Project Validation.",
                    this);
            }
#endif

            SubscribeHandSubsystem();
            InputDevices.deviceConnected += OnDeviceConnected;
            InputDevices.deviceDisconnected += OnDeviceDisconnected;
            InputDevices.deviceConfigChanged += OnDeviceConfigChanged;
            _inputDeviceMonitor.TrackingAcquired += OnControllerTrackingAcquired;

            UpdateLeftMode();
            UpdateRightMode();
        }

        protected void OnDisable()
        {
            UnsubscribeHandSubsystem();
            InputDevices.deviceConnected -= OnDeviceConnected;
            InputDevices.deviceDisconnected -= OnDeviceDisconnected;
            InputDevices.deviceConfigChanged -= OnDeviceConfigChanged;

            if (_inputDeviceMonitor == null) return;
            
            _inputDeviceMonitor.TrackingAcquired -= OnControllerTrackingAcquired;
            _inputDeviceMonitor.ClearAllDevices();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Update()
        {
#if XR_HANDS_1_3_OR_NEWER
            // Retry finding the running hand subsystem if necessary.
            // Only bother to try if hand tracking GameObjects are used.
            if ((m_HandSubsystem == null || !m_HandSubsystem.running) && (m_LeftHand != null || m_RightHand != null))
            {
                if (XRInputTrackingAggregator.TryGetHandSubsystem(out var runningHandSubsystem))
                {
                    if (runningHandSubsystem != m_HandSubsystem)
                    {
                        UnsubscribeHandSubsystem();
                        m_HandSubsystem = runningHandSubsystem;
                        SubscribeHandSubsystem();

                        UpdateLeftMode();
                        UpdateRightMode();
                    }
                }
                // Don't warn if there was some hand subsystem obtained at one time.
                // Without this check, the warning would be logged when exiting play mode.
                else if (m_HandSubsystem == null)
                {
                    LogMissingHandSubsystem();
                }
            }
#endif
        }
        #endregion

        #region - Mode -
        private void SetLeftMode(InputMode inputMode)
        {
            SafeSetActive(_mLeftHand, inputMode == InputMode.TrackedHand);
            SafeSetActive(_leftController, inputMode == InputMode.MotionController);
            var oldMode = _leftInputMode;
            _leftInputMode = inputMode;

            OnModeChanged(oldMode, inputMode, _rightInputMode);
        }

        private void SetRightMode(InputMode inputMode)
        {
            SafeSetActive(_rightHand, inputMode == InputMode.TrackedHand);
            SafeSetActive(_rightController, inputMode == InputMode.MotionController);
            var oldMode = _rightInputMode;
            _rightInputMode = inputMode;

            OnModeChanged(oldMode, inputMode, _leftInputMode);
        }

        private void OnModeChanged(InputMode oldInputMode, InputMode newInputMode, InputMode otherHandInputMode)
        {
            if (oldInputMode == newInputMode)
            {
                return;
            }

            // Invoke the events for the overall input modality.
            // "Started" when the first device changes to it, "Ended" when the last remaining device changes away from it.
            // Invoke the "Ended" event before the "Started" event for intuitive ordering.
            if (otherHandInputMode != InputMode.TrackedHand && oldInputMode == InputMode.TrackedHand)
            {
                OnTrackedHandModeEnded();
            }
            else if (otherHandInputMode != InputMode.MotionController && oldInputMode == InputMode.MotionController)
            {
                OnMotionControllerModeEnded();
            }

            if (otherHandInputMode != InputMode.TrackedHand && newInputMode == InputMode.TrackedHand)
            {
                OnTrackedHandModeStarted();
            }
            else if (otherHandInputMode != InputMode.MotionController && newInputMode == InputMode.MotionController)
            {
                OnMotionControllerModeStarted();
            }

            _currentInputMode.Value = newInputMode;
        }

        private void UpdateLeftMode()
        {
            if (GetLeftHandIsTracked())
            {
                SetLeftMode(InputMode.TrackedHand);
                return;
            }

            if (XRInputTrackingAggregator.TryGetDeviceWithExactCharacteristics(XRInputTrackingAggregator.Characteristics.LeftController, out var xrInputDevice))
            {
                UpdateMode(xrInputDevice, SetLeftMode);
                return;
            }

            SetLeftMode(InputMode.None);
        }

        private void UpdateRightMode()
        {
            if (GetRightHandIsTracked())
            {
                SetRightMode(InputMode.TrackedHand);
                return;
            }

            if (XRInputTrackingAggregator.TryGetDeviceWithExactCharacteristics(XRInputTrackingAggregator.Characteristics.RightController, out var xrInputDevice))
            {
                UpdateMode(xrInputDevice, SetRightMode);
                return;
            }

            SetRightMode(InputMode.None);
        }

        private void UpdateMode(InputDevice controllerDevice, Action<InputMode> setModeMethod)
        {
            if (!controllerDevice.isValid)
            {
                setModeMethod(InputMode.None);
                return;
            }

            if (controllerDevice.TryGetFeatureValue(CommonUsages.isTracked, out var isTracked) && isTracked)
            {
                setModeMethod(InputMode.MotionController);
            }
            else
            {
                // Start monitoring for when the controller is tracked, see OnControllerTrackingAcquired
                setModeMethod(InputMode.None);
                _inputDeviceMonitor.AddDevice(controllerDevice);
            }
        }

        #endregion

        #region - Hands -
        private void SubscribeHandSubsystem()
        {
#if XR_HANDS_1_3_OR_NEWER
            if (m_HandSubsystem != null)
                m_HandSubsystem.trackingAcquired += OnHandTrackingAcquired;
#endif
        }

        private void UnsubscribeHandSubsystem()
        {
#if XR_HANDS_1_3_OR_NEWER
            if (m_HandSubsystem != null)
                m_HandSubsystem.trackingAcquired -= OnHandTrackingAcquired;
#endif
        }

        private bool GetLeftHandIsTracked()
        {
#if XR_HANDS_1_3_OR_NEWER
            return m_HandSubsystem != null && m_HandSubsystem.leftHand.isTracked;
#else
            return false;
#endif
        }

        private bool GetRightHandIsTracked()
        {
#if XR_HANDS_1_3_OR_NEWER
            return m_HandSubsystem != null && m_HandSubsystem.rightHand.isTracked;
#else
            return false;
#endif
        }

#if XR_HANDS_1_3_OR_NEWER
        private void OnHandTrackingAcquired(XRHand hand)
        {
            switch (hand.handedness)
            {
                case Handedness.Left:
                    SetLeftMode(InputMode.TrackedHand);
                    break;

                case Handedness.Right:
                    SetRightMode(InputMode.TrackedHand);
                    break;
            }
        }
#endif

        private void LogMissingHandSubsystem()
        {
#if XR_HANDS_1_3_OR_NEWER
            if (m_LoggedMissingHandSubsystem)
                return;

            // If the hand subsystem couldn't be found and Initialize XR on Startup is enabled, warn about enabling Hand Tracking Subsystem.
            // If a user turns off that project setting, don't warn to console since the subsystem wouldn't have been created yet.
            // This warning should allow most users to fix a misconfiguration when they have either of the hand tracking GameObjects set.
            if (m_LeftHand != null || m_RightHand != null)
            {
                var instance = XRGeneralSettings.Instance;
                if (instance != null && instance.InitManagerOnStart)
                {
                    Debug.LogWarning("Hand Tracking Subsystem not found or not running, can't subscribe to hand tracking status." +
                        " Enable that feature in the OpenXR project settings and ensure OpenXR is enabled as the plug-in provider." +
                        " This component will reattempt getting a reference to the subsystem each frame.", this);
                }
            }

            m_LoggedMissingHandSubsystem = true;
#endif
        }
        #endregion

        #region - Connection Events -
        private void OnDeviceConnected(InputDevice device)
        {
            // Swap to controller
            var characteristics = device.characteristics;
            if (characteristics == XRInputTrackingAggregator.Characteristics.LeftController)
            {
                UpdateMode(device, SetLeftMode);
            }
            else if (characteristics == XRInputTrackingAggregator.Characteristics.RightController)
            {
                UpdateMode(device, SetRightMode);
            }
        }

        private void OnDeviceDisconnected(InputDevice device)
        {
            _inputDeviceMonitor.RemoveDevice(device);

            // Swap to hand tracking if tracked or turn off the controller
            var characteristics = device.characteristics;
            if (characteristics == XRInputTrackingAggregator.Characteristics.LeftController)
            {
                var mode = GetLeftHandIsTracked() ? InputMode.TrackedHand : InputMode.None;
                SetLeftMode(mode);
            }
            else if (characteristics == XRInputTrackingAggregator.Characteristics.RightController)
            {
                var mode = GetRightHandIsTracked() ? InputMode.TrackedHand : InputMode.None;
                SetRightMode(mode);
            }
        }

        private void OnDeviceConfigChanged(InputDevice device)
        {
            // Do the same as if the device was added
            OnDeviceConnected(device);
        }

        private void OnControllerTrackingAcquired(InputDevice device)
        {
            var characteristics = device.characteristics;
            if (_leftInputMode == InputMode.None && characteristics == XRInputTrackingAggregator.Characteristics.LeftController)
            {
                SetLeftMode(InputMode.MotionController);
            }
            else if (_rightInputMode == InputMode.None && characteristics == XRInputTrackingAggregator.Characteristics.RightController)
            {
                SetRightMode(InputMode.MotionController);
            }
        }
        #endregion


        #region - Helpers -
        private static void SafeSetActive(GameObject gameObject, bool active)
        {
            if (gameObject != null && gameObject.activeSelf != active)
            {
                gameObject.SetActive(active);
            }
        }
        #endregion
    }
}