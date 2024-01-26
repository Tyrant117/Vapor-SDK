using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;
using VaporInspector;

namespace VaporXR
{
    /// <summary>
    /// The <see cref="VXRTrackedPoseDriver"/> component applies the current pose value of a tracked device
    /// to the <see cref="Transform"/> of the <see cref="GameObject"/>.
    /// <see cref="VXRTrackedPoseDriver"/> can track multiple types of devices including XR HMDs, controllers, and remotes.
    /// </summary>
    /// <remarks>
    /// For <see cref="positionInput"/> and <see cref="rotationInput"/>, if an action is directly defined
    /// in the <see cref="InputActionProperty"/>, as opposed to a reference to an action externally defined
    /// in an <see cref="InputActionAsset"/>, the action will automatically be enabled and disabled by this
    /// behavior during <see cref="OnEnable"/> and <see cref="OnDisable"/>. The enabled state for actions
    /// externally defined must be managed externally from this behavior.
    /// </remarks>
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Controllers)]
    [DisallowMultipleComponent]
    // ReSharper disable once InconsistentNaming
    public class VXRTrackedPoseDriver : MonoBehaviour
    {
        /// <summary>
        /// Options for which <see cref="Transform"/> properties to update.
        /// </summary>
        /// <seealso cref="trackingType"/>
        public enum TrackingType
        {
            /// <summary>
            /// Update both rotation and position.
            /// </summary>
            RotationAndPosition,

            /// <summary>
            /// Update rotation only.
            /// </summary>
            RotationOnly,

            /// <summary>
            /// Update position only.
            /// </summary>
            PositionOnly,
        }
        
        public enum ControllerType
        {
            None,
            Oculus,
            WMR,
            Vive,
            Knuckles,
            Cosmos,
            ReverbG2
        }

        /// <summary>
        /// These bit flags correspond with <c>UnityEngine.XR.InputTrackingState</c>
        /// but that enum is not used to avoid adding a dependency to the XR module.
        /// Only the Position and Rotation flags are used by this class, so velocity and acceleration flags are not duplicated here.
        /// </summary>
        [Flags]
        public enum TrackingStates
        {
            /// <summary>
            /// Position and rotation are not valid.
            /// </summary>
            None,

            /// <summary>
            /// Position is valid.
            /// See <c>InputTrackingState.Position</c>.
            /// </summary>
            Position = 1 << 0,

            /// <summary>
            /// Rotation is valid.
            /// See <c>InputTrackingState.Rotation</c>.
            /// </summary>
            Rotation = 1 << 1,
        }
        
        /// <summary>
        /// Options for which phases of the player loop will update <see cref="Transform"/> properties.
        /// </summary>
        /// <seealso cref="updateType"/>
        public enum UpdateType
        {
            /// <summary>
            /// Update after the Input System has completed an update and right before rendering.
            /// This is the recommended and default option to minimize lag for XR tracked devices.
            /// </summary>
            UpdateAndBeforeRender,

            /// <summary>
            /// Update after the Input System has completed an update except right before rendering.
            /// </summary>
            /// <remarks>
            /// This may be dynamic update, fixed update, or a manual update depending on the Update Mode
            /// project setting for Input System.
            /// </remarks>
            Update,

            /// <summary>
            /// Update after the Input System has completed an update right before rendering.
            /// </summary>
            /// <remarks>
            /// Note that this update mode may not trigger if there are no XR devices added which use before render timing.
            /// </remarks>
            BeforeRender,
            
            /// <summary>
            /// Sample input corresponding to <see cref="FixedUpdate"/>.
            /// </summary>
            Fixed
        }

        [SerializeField]
        private bool _enableTracking = true;

        [SerializeField] private bool _isHmd;
        [FormerlySerializedAs("m_TrackingType")] [SerializeField, Tooltip("Which Transform properties to update.")]
        private TrackingType _trackingType;
        [FormerlySerializedAs("m_UpdateType")] [SerializeField, Tooltip("Updates the Transform properties after these phases of Input System event processing.")]
        private UpdateType _updateType = UpdateType.UpdateAndBeforeRender;
        [FormerlySerializedAs("m_IgnoreTrackingState")] [SerializeField, Tooltip("Ignore Tracking State and always treat the input pose as valid.")]
        private bool _ignoreTrackingState;

        [SerializeField, Tooltip("The input action to read the position value of a tracked device. Must be a Vector 3 control type.")]
        private XRInputDeviceBoolValueReader _isTrackedInput;
        [SerializeField, Tooltip("The input action to read the position value of a tracked device. Must be a Vector 3 control type.")]
        private XRInputDeviceVector3ValueReader _positionInput;
        [SerializeField, Tooltip("The input action to read the rotation value of a tracked device. Must be a Quaternion control type.")]
        private XRInputDeviceQuaternionValueReader _rotationInput;
        [SerializeField, Tooltip("The input action to read the tracking state value of a tracked device. Identifies if position and rotation have valid data. Must be an Integer control type.")]
        private XRInputDeviceInputTrackingStateValueReader _trackingStateInput;
        [SerializeField]
        private Transform _trackingOffset;

        [Title("Debug")]
        [SerializeField, ReadOnly] 
        private string _deviceName;
        [SerializeField, ReadOnly] 
        private string _deviceManufacturer;
        [SerializeField, ReadOnly] 
        private InputDeviceCharacteristics _deviceCharacteristics;

        public ControllerType Controller { get; private set; }
        public Transform TrackingOffset => _trackingOffset;

        private Vector3 _controllerPositionOffset;
        private Quaternion _controllerRotationOffset;

        private bool _isTracked;
        private Vector3 _currentPosition = Vector3.zero;
        private Quaternion _currentRotation = Quaternion.identity;
        private InputTrackingState _currentTrackingState = InputTrackingState.Position | InputTrackingState.Rotation;

        private bool _deviceDebugSetup;

        #region  - Initialization -
        protected void Awake()
        {
#if UNITY_INPUT_SYSTEM_ENABLE_VR && ENABLE_VR
            if (HasStereoCamera(out var cameraComponent))
            {
                UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(cameraComponent, true);
            }
#endif
        }

        protected void OnEnable()
        {
            Application.onBeforeRender += OnBeforeRender;
        }

        protected void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
        }

        protected void OnDestroy()
        {
#if UNITY_INPUT_SYSTEM_ENABLE_VR && ENABLE_VR
            if (HasStereoCamera(out var cameraComponent))
            {
                UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(cameraComponent, false);
            }
#endif
        }
        #endregion

        #region - Update -
        protected void Update()
        {
            if (_enableTracking && _updateType is UpdateType.Update or UpdateType.UpdateAndBeforeRender)
            {
                UpdateTrackingInput();
            }
        }

        /// <summary>
        /// This method is automatically called for "Just Before Render" input updates for VR devices.
        /// </summary>
        /// <seealso cref="Application.onBeforeRender"/>
        protected void OnBeforeRender()
        {
            if (_enableTracking && _updateType is UpdateType.BeforeRender or UpdateType.UpdateAndBeforeRender)
            {
                UpdateTrackingInput();
            }
        }
        
        /// <summary>
        /// Corresponds to <see cref="FixedUpdate"/>. It has the frequency of the physics system and is called
        /// every fixed framerate frame.
        /// </summary>
        protected void FixedUpdate()
        {
            if (_enableTracking && _updateType == UpdateType.Fixed)
            {
                UpdateTrackingInput();
            }
        }

        /// <summary>
        /// Updates the pose values in the given controller state based on the current tracking input of the controller device.
        /// Unity calls this automatically from <see cref="FixedUpdate"/>, <see cref="OnBeforeRender"/>, and <see cref="UpdateController"/> so explicit calls
        /// to this function are not required.
        /// </summary>
        protected void UpdateTrackingInput()
        {
            _isTracked = _isTrackedInput.ReadValue();
            _currentTrackingState = _trackingStateInput.ReadValue();
            _currentPosition = _positionInput.ReadValue();// + _controllerPositionOffset;
            _currentRotation = _rotationInput.ReadValue();// * _controllerRotationOffset;

            if (!_deviceDebugSetup && _currentTrackingState != InputTrackingState.None)
            {
                _deviceName = _trackingStateInput.InputDevice.name;
                _deviceManufacturer = _trackingStateInput.InputDevice.manufacturer;
                _deviceCharacteristics = _trackingStateInput.InputDevice.characteristics;
                if (!_isHmd)
                {
                    Controller = GetController(_deviceManufacturer, _deviceName);
                    var offset = ControllerHardwareOffsets.GetDeviceOffset(Controller);
                    _controllerPositionOffset = offset.position;
                    _controllerRotationOffset = offset.rotation;
                    TrackingOffset.SetLocalPositionAndRotation(_controllerPositionOffset, _controllerRotationOffset);
                }

                _deviceDebugSetup = true;
            }

            SetLocalTransform(_currentPosition, _currentRotation);
        }
        #endregion

        /// <summary>
        /// Updates <see cref="Transform"/> properties, constrained by tracking type and tracking state.
        /// </summary>
        /// <param name="newPosition">The new local position to possibly set.</param>
        /// <param name="newRotation">The new local rotation to possibly set.</param>
        protected void SetLocalTransform(Vector3 newPosition, Quaternion newRotation)
        {
            var positionValid = _ignoreTrackingState || (_currentTrackingState & InputTrackingState.Position) != 0;
            var rotationValid = _ignoreTrackingState || (_currentTrackingState & InputTrackingState.Rotation) != 0;

            if (_trackingType == TrackingType.RotationAndPosition && rotationValid && positionValid)
            {
                transform.SetLocalPositionAndRotation(newPosition, newRotation);
                return;
            }

            if (rotationValid && _trackingType is TrackingType.RotationAndPosition or TrackingType.RotationOnly)
            {
                transform.localRotation = newRotation;
            }

            if (positionValid && _trackingType is TrackingType.RotationAndPosition or TrackingType.PositionOnly)
            {
                transform.localPosition = newPosition;
            }
        }

        private bool HasStereoCamera(out Camera cameraComponent)
        {
            return TryGetComponent(out cameraComponent) && cameraComponent.stereoEnabled;
        }
        
        private static ControllerType GetController(string deviceManufacturer, string deviceName)
        {
            const string windowsMr = "windowsmr";
            const string windowsMrOpenXR = "windows mr controller";
            const string vive = "vive";
            const string cosmos = "cosmos";
            const string oculus = "oculus";
            const string knuckles = "knuckles";
            const string knucklesOpenXR = "index controller";
            const string wmrController = "spatial";
            const string htc = "htc";
            const string reverb = "reverb";
            const string g2 = "g2";
            const string openXrG2 = "hp reverb g2 controller";
            const string reverbG2 = "0x045E/0x066A";
            
            if (string.IsNullOrWhiteSpace(deviceManufacturer) && string.IsNullOrWhiteSpace(deviceName))
            {
                return ControllerType.None;
            }

            deviceManufacturer ??= "";
            deviceName ??= "";
            deviceManufacturer = deviceManufacturer.ToLower();
            deviceName = deviceName.ToLower();

            if (deviceName.Contains(knuckles) || deviceName.Contains(knucklesOpenXR.ToLower()))
            {
                return ControllerType.Knuckles;
            }

            if (deviceManufacturer.Contains(oculus))
            {
                return ControllerType.Oculus;
            }

            if (deviceName.Contains(cosmos))
            {
                return ControllerType.Cosmos;
            }

            if (deviceManufacturer.Contains(htc) || deviceName.Contains(vive))
            {
                return ControllerType.Vive;
            }

            var isWindowsMixedReality = deviceManufacturer.Contains(windowsMr) || deviceName.Contains(wmrController) || deviceName.Contains(windowsMrOpenXR.ToLower()) ||
                                        deviceName.Contains(openXrG2);
            if (isWindowsMixedReality)
            {
                if (deviceName.Contains(reverbG2.ToLower()) || deviceName.Contains(openXrG2.ToLower()))
                {
                    return ControllerType.ReverbG2;
                }
                return ControllerType.WMR;
            }

            return ControllerType.None;
        }
    }
}
