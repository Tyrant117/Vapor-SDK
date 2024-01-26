#if ENABLE_VR || UNITY_GAMECORE
#define XR_MODULE_AVAILABLE
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR;
using VaporInspector;

namespace VaporXR
{
    public class VXROrigin : MonoBehaviour
    {
        /// <summary>
        /// Sets which Tracking Origin Mode to use when initializing the input device.
        /// </summary>
        /// <seealso cref="RequestedTrackingOriginMode"/>
        /// <seealso cref="TrackingOriginModeFlags"/>
        /// <seealso cref="XRInputSubsystem.TrySetTrackingOriginMode"/>
        public enum TrackingOriginMode
        {
            /// <summary>
            /// Uses the default Tracking Origin Mode of the input device.
            /// </summary>
            /// <remarks>
            /// When changing to this value after startup, the Tracking Origin Mode will not be changed.
            /// </remarks>
            NotSpecified,

            /// <summary>
            /// Sets the Tracking Origin Mode to <see cref="TrackingOriginModeFlags.Device"/>.
            /// Input devices will be tracked relative to the first known location.
            /// </summary>
            /// <remarks>
            /// Represents a device-relative tracking origin. A device-relative tracking origin defines a local origin
            /// at the position of the device in space at some previous point in time, usually at a recenter event,
            /// power-on, or AR/VR session start. Pose data provided by the device will be in this space relative to
            /// the local origin. This means that poses returned in this mode will not include the user height (for VR)
            /// or the device height (for AR) and any camera tracking from the XR device will need to be manually offset accordingly.
            /// </remarks>
            /// <seealso cref="TrackingOriginModeFlags.Device"/>
            Device,

            /// <summary>
            /// Sets the Tracking Origin Mode to <see cref="TrackingOriginModeFlags.Floor"/>.
            /// Input devices will be tracked relative to a location on the floor.
            /// </summary>
            /// <remarks>
            /// Represents the tracking origin whereby (0, 0, 0) is on the "floor" or other surface determined by the
            /// XR device being used. The pose values reported by an XR device in this mode will include the height
            /// of the XR device above this surface, removing the need to offset the position of the camera tracking
            /// the XR device by the height of the user (VR) or the height of the device above the floor (AR).
            /// </remarks>
            /// <seealso cref="TrackingOriginModeFlags.Floor"/>
            Floor,
        }
        
        //This is the average seated height in meters (which equals 44 inches).
        private const float DefaultCameraYOffset = 1.1176f;
        
#if XR_MODULE_AVAILABLE
        /// <summary>
        /// Used to cache the input subsystems without creating additional GC allocations.
        /// </summary>
        private static readonly List<XRInputSubsystem> s_InputSubsystems = new List<XRInputSubsystem>();
#endif
        
        #region Inspector
        [SerializeField]
        [RichTextTooltip("The \"Origin\" <cls>GameObject</cls> is used to refer to the base of the XR Origin, by default it is this <cls>GameObject</cls>.\n" +
                         "This is the <cls>GameObject</cls> that will be manipulated via locomotion.")]
        private GameObject _originBaseGameObject;
        [SerializeField] 
        [RichTextTooltip("The Camera to associate with the XR device.")]
        private Camera _camera;
        [SerializeField] 
        [RichTextTooltip("The <cls>GameObject</cls> to move to desired height off the floor (defaults to this object if none provided).\n" +
                         "This is used to transform the XR device from camera space to XR Origin space.")]
        private GameObject _cameraFloorOffsetObject;
        [SerializeField] 
        [RichTextTooltip("The type of tracking origin to use for this VXROrigin. Tracking origins identify where (0, 0, 0) is in the world of tracking. Not all devices support all tracking origin modes.")]
        private TrackingOriginMode _requestedTrackingOriginMode = TrackingOriginMode.NotSpecified;
        [SerializeField]
        [RichTextTooltip("Camera height to be used when in Device Tracking Origin Mode to define the height of the user from the floor.\n" +
                         "This is the amount that the camera is offset from the floor when moving the <mth>CameraFloorOffsetObject</mth>")]
        private float _cameraYOffset = DefaultCameraYOffset;
        #endregion

        #region Properties
        /// <summary>
        /// The <see cref="Camera"/> used to render the scene from the point of view of the XR device. Must be a child of
        /// the <see cref="GameObject"/> containing this <c>XROrigin</c> component.
        /// </summary>
        /// <remarks>
        /// You can add a <see cref="UnityEngine.InputSystem.XR.TrackedPoseDriver"/> component to the <see cref="Camera"/> 
        /// GameObject to update its position and rotation using tracking data from the XR device.
        /// You must update the <see cref="Camera"/> position and rotation using tracking data from the XR device.
        /// </remarks>
        public Camera Camera { get => _camera; set => _camera = value; }

        /// <summary>
        /// The parent <c>Transform</c> for all "trackables" (for example, planes and feature points).
        /// </summary>
        /// <remarks>
        /// See [Trackables](xref:arfoundation-managers) for more information.
        /// </remarks>
        public Transform TrackablesParent { get; private set; }

        /// <summary>
        /// The "Origin" <see cref="GameObject"/> is used to refer to the base of the XR Origin, by default it is this <see cref="GameObject"/>.
        /// This is the <see cref="GameObject"/> that will be manipulated via locomotion.
        /// </summary>
        public GameObject Origin { get => _originBaseGameObject; set => _originBaseGameObject = value; }

        /// <summary>
        /// The <see cref="GameObject"/> to move to desired height off the floor (defaults to this object if none provided).
        /// This is used to transform the XR device from camera space to XR Origin space.
        /// </summary>
        public GameObject CameraFloorOffsetObject
        {
            get => _cameraFloorOffsetObject;
            set
            {
                _cameraFloorOffsetObject = value;
                MoveOffsetHeight();
            }
        }

        /// <summary>
        /// The type of tracking origin to use for this XROrigin. Tracking origins identify where (0, 0, 0) is in the world
        /// of tracking. Not all devices support all tracking origin modes.
        /// </summary>
        /// <seealso cref="TrackingOriginMode"/>
        public TrackingOriginMode RequestedTrackingOriginMode
        {
            get => _requestedTrackingOriginMode;
            set
            {
                _requestedTrackingOriginMode = value;
                TryInitializeCamera();
            }
        }

        /// <summary>
        /// Camera height to be used when in <c>Device</c> Tracking Origin Mode to define the height of the user from the floor.
        /// This is the amount that the camera is offset from the floor when moving the <see cref="CameraFloorOffsetObject"/>.
        /// </summary>
        public float CameraYOffset
        {
            get => _cameraYOffset;
            set
            {
                _cameraYOffset = value;
                MoveOffsetHeight();
            }
        }

#if XR_MODULE_AVAILABLE || PACKAGE_DOCS_GENERATION
        /// <summary>
        /// (Read Only) The Tracking Origin Mode of this XR Origin.
        /// </summary>
        /// <seealso cref="RequestedTrackingOriginMode"/>
        public TrackingOriginModeFlags CurrentTrackingOriginMode { get; private set; }
#endif

        /// <summary>
        /// (Read Only) The origin's local position in camera space.
        /// </summary>
        public Vector3 OriginInCameraSpacePos => _camera.transform.InverseTransformPoint(_originBaseGameObject.transform.position);

        /// <summary>
        /// (Read Only) The camera's local position in origin space.
        /// </summary>
        public Vector3 CameraInOriginSpacePos => _originBaseGameObject.transform.InverseTransformPoint(_camera.transform.position);

        /// <summary>
        /// (Read Only) The camera's height relative to the origin.
        /// </summary>
        public float CameraInOriginSpaceHeight => CameraInOriginSpacePos.y;
        #endregion

        #region Fields
        // Bookkeeping to track lazy initialization of the tracking origin mode type.
        private bool _cameraInitialized;
        private bool _cameraInitializing;
        #endregion

        #region Events
        /// <summary>
        /// Invoked during
        /// [Application.onBeforeRender](xref:UnityEngine.Application.onBeforeRender(UnityEngine.Events.UnityAction))
        /// whenever the <see cref="TrackablesParent"/> [transform](xref:UnityEngine.Transform) changes.
        /// </summary>
        public event Action<ARTrackablesParentTransformChangedEventArgs> TrackablesParentTransformChanged;
        #endregion

        #region - Initialization -
        protected void OnValidate()
        {
            if (_originBaseGameObject == null)
                _originBaseGameObject = gameObject;

            if (Application.isPlaying && isActiveAndEnabled)
            {
                // Respond to the mode changing by re-initializing the camera,
                // or just update the offset height in order to avoid recentering.
                if (_IsModeStale())
                {
                    TryInitializeCamera();
                }
                else
                {
                    MoveOffsetHeight();
                }
            }

            bool _IsModeStale()
            {
#if XR_MODULE_AVAILABLE
                if (s_InputSubsystems.Count > 0)
                {
                    foreach (var inputSubsystem in s_InputSubsystems)
                    {
                        // Convert from the request enum to the flags enum that is used by the subsystem
                        TrackingOriginModeFlags equivalentFlagsMode;
                        switch (_requestedTrackingOriginMode)
                        {
                            case TrackingOriginMode.NotSpecified:
                                // Don't need to initialize the camera since we don't set the mode when NotSpecified (we just keep the current value)
                                return false;
                            case TrackingOriginMode.Device:
                                equivalentFlagsMode = TrackingOriginModeFlags.Device;
                                break;
                            case TrackingOriginMode.Floor:
                                equivalentFlagsMode = TrackingOriginModeFlags.Floor;
                                break;
                            default:
                                Assert.IsTrue(false, $"Unhandled {nameof(TrackingOriginMode)}={_requestedTrackingOriginMode}");
                                return false;
                        }

                        if (inputSubsystem != null && inputSubsystem.GetTrackingOriginMode() != equivalentFlagsMode)
                        {
                            return true;
                        }
                    }
                }
#endif
                return false;
            }
        }

        protected void Awake()
        {
            if (_cameraFloorOffsetObject == null)
            {
                Debug.LogWarning("No Camera Floor Offset GameObject specified for XR Origin, using attached GameObject.", this);
                _cameraFloorOffsetObject = gameObject;
            }

            if (_camera == null)
            {
                var mainCamera = Camera.main;
                if (mainCamera != null)
                    _camera = mainCamera;
                else
                    Debug.LogWarning("No Main Camera is found for XR Origin, please assign the Camera field manually.", this);
            }

            // This will be the parent GameObject for any trackables (such as planes) for which
            // we want a corresponding GameObject.
            TrackablesParent = new GameObject("Trackables").transform;
            TrackablesParent.SetParent(transform, false);
            TrackablesParent.SetLocalPose(Pose.identity);
            TrackablesParent.localScale = Vector3.one;

            if (_camera)
            {
#if INCLUDE_INPUT_SYSTEM && INCLUDE_LEGACY_INPUT_HELPERS
                var trackedPoseDriver = m_Camera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                var trackedPoseDriverOld = m_Camera.GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
                if (trackedPoseDriver == null && trackedPoseDriverOld == null)
                {
                    Debug.LogWarning(
                        $"Camera \"{m_Camera.name}\" does not use a Tracked Pose Driver (Input System), " +
                        "so its transform will not be updated by an XR device.  In order for this to be " +
                        "updated, please add a Tracked Pose Driver (Input System) with bindings for position and rotation of the center eye.", this);
                }
#elif !INCLUDE_INPUT_SYSTEM && INCLUDE_LEGACY_INPUT_HELPERS
                var trackedPoseDriverOld = m_Camera.GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
                if (trackedPoseDriverOld == null)
                {
                    Debug.LogWarning(
                        $"Camera \"{m_Camera.name}\" does not use a Tracked Pose Driver, and com.unity.xr.legacyinputhelpers is installed. " +
                        "Although the Tracked Pose Driver from Legacy Input Helpers can be used, it is recommended to " +
                        "install com.unity.inputsystem instead and add a Tracked Pose Driver (Input System) with bindings for position and rotation of the center eye.", this);
                }
#elif INCLUDE_INPUT_SYSTEM && !INCLUDE_LEGACY_INPUT_HELPERS
                var trackedPoseDriver = m_Camera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                if (trackedPoseDriver == null)
                {
                    Debug.LogWarning(
                        $"Camera \"{m_Camera.name}\" does not use a Tracked Pose Driver (Input System), " +
                        "so its transform will not be updated by an XR device.  In order for this to be " +
                        "updated, please add a Tracked Pose Driver (Input System) with bindings for position and rotation of the center eye.", this);
                }
#elif !INCLUDE_INPUT_SYSTEM && !INCLUDE_LEGACY_INPUT_HELPERS
                Debug.LogWarning(
                    $"Camera \"{_camera.name}\" does not use a Tracked Pose Driver and com.unity.inputsystem is not installed, " +
                    "so its transform will not be updated by an XR device.  In order for this to be " +
                    "updated, please install com.unity.inputsystem and add a Tracked Pose Driver (Input System) with bindings for position and rotation of the center eye.", this);
#endif
            }
        }
        
        protected void OnEnable() => Application.onBeforeRender += OnBeforeRender;

        protected void OnDisable() => Application.onBeforeRender -= OnBeforeRender;

        protected void Start()
        {
            TryInitializeCamera();
        }
        
        protected void OnDestroy()
        {
#if XR_MODULE_AVAILABLE
            foreach (var inputSubsystem in s_InputSubsystems)
            {
                if (inputSubsystem != null)
                    inputSubsystem.trackingOriginUpdated -= OnInputSubsystemTrackingOriginUpdated;
            }
#endif
        }
        
        /// <summary>
        /// Repeatedly attempt to initialize the camera.
        /// </summary>
        private void TryInitializeCamera()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            _cameraInitialized = SetupCamera();
            if (!_cameraInitialized & !_cameraInitializing)
            {
                StartCoroutine(RepeatInitializeCamera());
            }
        }

        private IEnumerator RepeatInitializeCamera()
        {
            _cameraInitializing = true;
            while (!_cameraInitialized)
            {
                yield return null;
                if (!_cameraInitialized)
                {
                    _cameraInitialized = SetupCamera();
                }
            }

            _cameraInitializing = false;
        }
        
        /// <summary>
        /// Handles re-centering and off-setting the camera in space depending on which tracking origin mode it is setup in.
        /// </summary>
        private bool SetupCamera()
        {
            var initialized = true;

#if XR_MODULE_AVAILABLE
            SubsystemManager.GetInstances(s_InputSubsystems);
            if (s_InputSubsystems.Count > 0)
            {
                foreach (var inputSubsystem in s_InputSubsystems)
                {
                    if (SetupCamera(inputSubsystem))
                    {
                        // It is possible this could happen more than
                        // once so unregister the callback first just in case.
                        inputSubsystem.trackingOriginUpdated -= OnInputSubsystemTrackingOriginUpdated;
                        inputSubsystem.trackingOriginUpdated += OnInputSubsystemTrackingOriginUpdated;
                    }
                    else
                    {
                        initialized = false;
                    }
                }
            }
#endif

            return initialized;
        }

#if XR_MODULE_AVAILABLE
        private bool SetupCamera(XRInputSubsystem inputSubsystem)
        {
            if (inputSubsystem == null)
                return false;

            var successful = true;

            switch (_requestedTrackingOriginMode)
            {
                case TrackingOriginMode.NotSpecified:
                    CurrentTrackingOriginMode = inputSubsystem.GetTrackingOriginMode();
                    break;
                case TrackingOriginMode.Device:
                case TrackingOriginMode.Floor:
                {
                    var supportedModes = inputSubsystem.GetSupportedTrackingOriginModes();

                    // We need to check for Unknown because we may not be in a state where we can read this data yet.
                    if (supportedModes == TrackingOriginModeFlags.Unknown)
                        return false;

                    // Convert from the request enum to the flags enum that is used by the subsystem
                    var equivalentFlagsMode = _requestedTrackingOriginMode == TrackingOriginMode.Device
                        ? TrackingOriginModeFlags.Device
                        : TrackingOriginModeFlags.Floor;

                    // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags -- Treated like Flags enum when querying supported modes
                    if ((supportedModes & equivalentFlagsMode) == 0)
                    {
                        _requestedTrackingOriginMode = TrackingOriginMode.NotSpecified;
                        CurrentTrackingOriginMode = inputSubsystem.GetTrackingOriginMode();
                        Debug.LogWarning($"Attempting to set the tracking origin mode to {equivalentFlagsMode}, but that is not supported by the SDK." +
                            $" Supported types: {supportedModes:F}. Using the current mode of {CurrentTrackingOriginMode} instead.", this);
                    }
                    else
                    {
                        successful = inputSubsystem.TrySetTrackingOriginMode(equivalentFlagsMode);
                    }
                }
                break;
                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(TrackingOriginMode)}={_requestedTrackingOriginMode}");
                    return false;
            }

            if (successful)
                MoveOffsetHeight();

            if (CurrentTrackingOriginMode == TrackingOriginModeFlags.Device || _requestedTrackingOriginMode == TrackingOriginMode.Device)
                successful = inputSubsystem.TryRecenter();

            return successful;
        }

        private void OnInputSubsystemTrackingOriginUpdated(XRInputSubsystem inputSubsystem)
        {
            CurrentTrackingOriginMode = inputSubsystem.GetTrackingOriginMode();
            MoveOffsetHeight();
        }
#endif
        #endregion

        #region - Update -
        private void OnBeforeRender()
        {
            if (_camera)
            {
                var pose = GetCameraOriginPose();
                TrackablesParent.position = pose.position;
                TrackablesParent.rotation = pose.rotation;
            }

            if (TrackablesParent.hasChanged)
            {
                TrackablesParentTransformChanged?.Invoke(new ARTrackablesParentTransformChangedEventArgs(this, TrackablesParent));
                TrackablesParent.hasChanged = false;
            }
        }
        #endregion

        #region - Posing -
        private Pose GetCameraOriginPose()
        {
            var localOriginPose = Pose.identity;
            var parent = _camera.transform.parent;

            return parent
                ? parent.TransformPose(localOriginPose)
                : localOriginPose;
        }
        #endregion

        #region - Helpers -
        /// <summary>
        /// Sets the height of the camera based on the current tracking origin mode by updating the <see cref="CameraFloorOffsetObject"/>.
        /// </summary>
        private void MoveOffsetHeight()
        {
#if XR_MODULE_AVAILABLE
            if (!Application.isPlaying)
                return;

            switch (CurrentTrackingOriginMode)
            {
                case TrackingOriginModeFlags.Floor:
                    MoveOffsetHeight(0f);
                    break;
                case TrackingOriginModeFlags.Device:
                    MoveOffsetHeight(_cameraYOffset);
                    break;
                default:
                    return;
            }
#endif
        }
        
        /// <summary>
        /// Sets the height of the camera to the given <paramref name="y"/> value by updating the <see cref="CameraFloorOffsetObject"/>.
        /// </summary>
        /// <param name="y">The local y-position to set.</param>
        private void MoveOffsetHeight(float y)
        {
            if (_cameraFloorOffsetObject != null)
            {
                var offsetTransform = _cameraFloorOffsetObject.transform;
                var desiredPosition = offsetTransform.localPosition;
                desiredPosition.y = y;
                offsetTransform.localPosition = desiredPosition;
            }
        }
        
        /// <summary>
        /// Rotates the XR origin object around the camera object by the provided <paramref name="angleDegrees"/>.
        /// This rotation only occurs around the origin's Up vector
        /// </summary>
        /// <param name="angleDegrees">The amount of rotation in degrees.</param>
        /// <returns>Returns <see langword="true"/> if the rotation is performed. Otherwise, returns <see langword="false"/>.</returns>
        public bool RotateAroundCameraUsingOriginUp(float angleDegrees)
        {
            return RotateAroundCameraPosition(_originBaseGameObject.transform.up, angleDegrees);
        }

        /// <summary>
        /// Rotates the XR origin object around the camera object's position in world space using the provided <paramref name="vector"/>
        /// as the rotation axis. The XR Origin object is rotated by the amount of degrees provided in <paramref name="angleDegrees"/>.
        /// </summary>
        /// <param name="vector">The axis of the rotation.</param>
        /// <param name="angleDegrees">The amount of rotation in degrees.</param>
        /// <returns>Returns <see langword="true"/> if the rotation is performed. Otherwise, returns <see langword="false"/>.</returns>
        public bool RotateAroundCameraPosition(Vector3 vector, float angleDegrees)
        {
            if (_camera == null || _originBaseGameObject == null)
            {
                return false;
            }

            // Rotate around the camera position
            _originBaseGameObject.transform.RotateAround(_camera.transform.position, vector, angleDegrees);

            return true;
        }

        /// <summary>
        /// This function will rotate the XR Origin object such that the XR Origin's up vector will match the provided vector.
        /// </summary>
        /// <param name="destinationUp">the vector to which the XR Origin object's up vector will be matched.</param>
        /// <returns>Returns <see langword="true"/> if the rotation is performed or the vectors have already been matched.
        /// Otherwise, returns <see langword="false"/>.</returns>
        public bool MatchOriginUp(Vector3 destinationUp)
        {
            if (_originBaseGameObject == null)
            {
                return false;
            }

            if (_originBaseGameObject.transform.up == destinationUp)
                return true;

            var rigUp = Quaternion.FromToRotation(_originBaseGameObject.transform.up, destinationUp);
            _originBaseGameObject.transform.rotation = rigUp * transform.rotation;

            return true;
        }

        /// <summary>
        /// This function will rotate the XR Origin object around the camera object using the <paramref name="destinationUp"/> vector such that:
        /// <list type="bullet">
        /// <item>
        /// <description>The camera will look at the area in the direction of the <paramref name="destinationForward"/></description>
        /// </item>
        /// <item>
        /// <description>The projection of camera's forward vector on the plane with the normal <paramref name="destinationUp"/> will be in the direction of <paramref name="destinationForward"/></description>
        /// </item>
        /// <item>
        /// <description>The up vector of the XR Origin object will match the provided <paramref name="destinationUp"/> vector (note that the camera's Up vector can not be manipulated)</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="destinationUp">The up vector that the origin's up vector will be matched to.</param>
        /// <param name="destinationForward">The forward vector that will be matched to the projection of the camera's forward vector on the plane with the normal <paramref name="destinationUp"/>.</param>
        /// <returns>Returns <see langword="true"/> if the rotation is performed. Otherwise, returns <see langword="false"/>.</returns>
        public bool MatchOriginUpCameraForward(Vector3 destinationUp, Vector3 destinationForward)
        {
            if (_camera != null && MatchOriginUp(destinationUp))
            {
                // Project current camera's forward vector on the destination plane, whose normal vector is destinationUp.
                var projectedCamForward = Vector3.ProjectOnPlane(_camera.transform.forward, destinationUp).normalized;

                // The angle that we want the XROrigin to rotate is the signed angle between projectedCamForward and destinationForward, after the up vectors are matched.
                var signedAngle = Vector3.SignedAngle(projectedCamForward, destinationForward, destinationUp);

                RotateAroundCameraPosition(destinationUp, signedAngle);

                return true;
            }

            return false;
        }

        /// <summary>
        /// This function will rotate the XR Origin object around the camera object using the <paramref name="destinationUp"/> vector such that:
        /// <list type="bullet">
        /// <item>
        /// <description>The forward vector of the XR Origin object, which is the direction the player moves in Unity when walking forward in the physical world, will match the provided <paramref name="destinationUp"/> vector</description>
        /// </item>
        /// <item>
        /// <description>The up vector of the XR Origin object will match the provided <paramref name="destinationUp"/> vector</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="destinationUp">The up vector that the origin's up vector will be matched to.</param>
        /// <param name="destinationForward">The forward vector that will be matched to the forward vector of the XR Origin object,
        /// which is the direction the player moves in Unity when walking forward in the physical world.</param>
        /// <returns>Returns <see langword="true"/> if the rotation is performed. Otherwise, returns <see langword="false"/>.</returns>
        public bool MatchOriginUpOriginForward(Vector3 destinationUp, Vector3 destinationForward)
        {
            if (_originBaseGameObject != null && MatchOriginUp(destinationUp))
            {
                // The angle that we want the XR Origin to rotate is the signed angle between the origin's forward and destinationForward, after the up vectors are matched.
                var signedAngle = Vector3.SignedAngle(_originBaseGameObject.transform.forward, destinationForward, destinationUp);

                RotateAroundCameraPosition(destinationUp, signedAngle);

                return true;
            }

            return false;
        }

        /// <summary>
        /// This function moves the camera to the world location provided by <paramref name="desiredWorldLocation"/>.
        /// It does this by moving the XR Origin object so that the camera's world location matches the desiredWorldLocation
        /// </summary>
        /// <param name="desiredWorldLocation">the position in world space that the camera should be moved to</param>
        /// <returns>Returns <see langword="true"/> if the move is performed. Otherwise, returns <see langword="false"/>.</returns>
        public bool MoveCameraToWorldLocation(Vector3 desiredWorldLocation)
        {
            if (_camera == null)
            {
                return false;
            }

            var rot = Matrix4x4.Rotate(_camera.transform.rotation);
            var delta = rot.MultiplyPoint3x4(OriginInCameraSpacePos);
            _originBaseGameObject.transform.position = delta + desiredWorldLocation;

            return true;
        }
        #endregion
        
    }
}
