using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
#if XR_HANDS_1_3_OR_NEWER
using UnityEngine.XR.Hands;
#endif

namespace VaporXR
{
    /// <summary>
    /// Tracking status of the device in a unified format.
    /// </summary>
    /// <seealso cref="XRInputTrackingAggregator"/>
    public struct TrackingStatus
    {
        /// <summary>
        /// Whether the device is available.
        /// </summary>
        /// <seealso cref="InputDevice.added"/>
        /// <seealso cref="InputDevice.isValid"/>
        /// <seealso cref="XRHandSubsystem"/>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Whether the device is tracked.
        /// </summary>
        /// <seealso cref="TrackedDevice.isTracked"/>
        /// <seealso cref="EyeGazeInteraction.EyeGazeDevice.pose"/>
        /// <seealso cref="PoseControl.isTracked"/>
        /// <seealso cref="CommonUsages.isTracked"/>
        /// <seealso cref="XRHand.isTracked"/>
        public bool IsTracked { get; set; }

        /// <summary>
        /// Whether the device tracking values are valid.
        /// </summary>
        /// <seealso cref="TrackedDevice.trackingState"/>
        /// <seealso cref="EyeGazeInteraction.EyeGazeDevice.pose"/>
        /// <seealso cref="PoseControl.trackingState"/>
        /// <seealso cref="CommonUsages.trackingState"/>
        /// <seealso cref="XRHand.isTracked"/>
        public InputTrackingState TrackingState { get; set; }
    }

    /// <summary>
    /// Provides methods for obtaining the tracking status of XR devices registered with Unity
    /// without needing to know the input system it is sourced from.
    /// </summary>
    /// <remarks>
    /// XR devices may be added to Unity through different mechanisms, such as native XR devices registered
    /// with the XR module, real or simulated devices registered with the Input System package, or the
    /// Hand Tracking Subsystem of OpenXR.
    /// </remarks>
    public static class XRInputTrackingAggregator
    {
        /// <summary>
        /// Provides shortcut properties for describing XR module input device characteristics for common XR devices.
        /// </summary>
        public static class Characteristics
        {
            /// <summary>
            /// HMD characteristics.
            /// <see cref="InputDeviceCharacteristics.HeadMounted"/> <c>|</c> <see cref="InputDeviceCharacteristics.TrackedDevice"/>
            /// </summary>
            public static InputDeviceCharacteristics Hmd => InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice;

            /// <summary>
            /// Eye gaze characteristics.
            /// <see cref="InputDeviceCharacteristics.HeadMounted"/> <c>|</c> <see cref="InputDeviceCharacteristics.EyeTracking"/> <c>|</c> <see cref="InputDeviceCharacteristics.TrackedDevice"/>
            /// </summary>
            public static InputDeviceCharacteristics EyeGaze => InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.EyeTracking | InputDeviceCharacteristics.TrackedDevice;

            /// <summary>
            /// Left controller characteristics.
            /// <see cref="InputDeviceCharacteristics.HeldInHand"/> <c>|</c> <see cref="InputDeviceCharacteristics.TrackedDevice"/> <c>|</c> <see cref="InputDeviceCharacteristics.Controller"/> <c>|</c> <see cref="InputDeviceCharacteristics.Left"/>
            /// </summary>
            public static InputDeviceCharacteristics LeftController => InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller |
                                                                       InputDeviceCharacteristics.Left;

            /// <summary>
            /// Right controller characteristics.
            /// <see cref="InputDeviceCharacteristics.HeldInHand"/> <c>|</c> <see cref="InputDeviceCharacteristics.TrackedDevice"/> <c>|</c> <see cref="InputDeviceCharacteristics.Controller"/> <c>|</c> <see cref="InputDeviceCharacteristics.Right"/>
            /// </summary>
            public static InputDeviceCharacteristics RightController => InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller |
                                                                        InputDeviceCharacteristics.Right;

            /// <summary>
            /// Left tracked hand characteristics.
            /// <see cref="InputDeviceCharacteristics.HandTracking"/> <c>|</c> <see cref="InputDeviceCharacteristics.TrackedDevice"/> <c>|</c> <see cref="InputDeviceCharacteristics.Left"/>
            /// </summary>
            public static InputDeviceCharacteristics LeftTrackedHand => InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Left;

            /// <summary>
            /// Right tracked hand characteristics.
            /// <see cref="InputDeviceCharacteristics.HandTracking"/> <c>|</c> <see cref="InputDeviceCharacteristics.TrackedDevice"/> <c>|</c> <see cref="InputDeviceCharacteristics.Right"/>
            /// </summary>
            public static InputDeviceCharacteristics RightTrackedHand => InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Right;
        }

        /// <summary>
        /// Temporary list used when getting the XR module devices.
        /// </summary>
        private static List<InputDevice> s_XRInputDevices;

#if XR_HANDS_1_3_OR_NEWER
        /// <summary>
        /// Temporary list used when getting the hand subsystems.
        /// </summary>
        static List<XRHandSubsystem> s_HandSubsystems;
#endif

        /// <summary>
        /// Gets the tracking status of the HMD device for this frame.
        /// </summary>
        /// <returns>Returns a snapshot of the tracking status for this frame.</returns>
        public static TrackingStatus GetHmdStatus()
        {
            return !Application.isPlaying ? default :
                // Try XR module devices
                TryGetDeviceWithExactCharacteristics(Characteristics.Hmd, out var xrInputDevice) ? GetTrackingStatus(xrInputDevice) : default;
        }

        /// <summary>
        /// Gets the tracking status of the eye gaze device for this frame.
        /// </summary>
        /// <returns>Returns a snapshot of the tracking status for this frame.</returns>
        public static TrackingStatus GetEyeGazeStatus()
        {
            return !Application.isPlaying ? default :
                // Try XR module devices
                TryGetDeviceWithExactCharacteristics(Characteristics.EyeGaze, out var xrInputDevice) ? GetTrackingStatus(xrInputDevice) : default;
        }

        /// <summary>
        /// Gets the tracking status of the left motion controller device for this frame.
        /// </summary>
        /// <returns>Returns a snapshot of the tracking status for this frame.</returns>
        public static TrackingStatus GetLeftControllerStatus()
        {
            return !Application.isPlaying ? default :
                // Try XR module devices
                TryGetDeviceWithExactCharacteristics(Characteristics.LeftController, out var xrInputDevice) ? GetTrackingStatus(xrInputDevice) : default;
        }

        /// <summary>
        /// Gets the tracking status of the right motion controller device for this frame.
        /// </summary>
        /// <returns>Returns a snapshot of the tracking status for this frame.</returns>
        public static TrackingStatus GetRightControllerStatus()
        {
            return !Application.isPlaying ? default :
                // Try XR module devices
                TryGetDeviceWithExactCharacteristics(Characteristics.RightController, out var xrInputDevice) ? GetTrackingStatus(xrInputDevice) : default;
        }

        /// <summary>
        /// Gets the tracking status of the left tracked hand device for this frame.
        /// </summary>
        /// <returns>Returns a snapshot of the tracking status for this frame.</returns>
        public static TrackingStatus GetLeftTrackedHandStatus()
        {
            if (!Application.isPlaying)
            {
                return default;
            }

#if XR_HANDS_1_3_OR_NEWER
            // Try XR Hand Subsystem devices
            if (TryGetHandSubsystem(out var handSubsystem))
                return GetTrackingStatus(handSubsystem.leftHand);
#endif

            // Try XR module devices
            return TryGetDeviceWithExactCharacteristics(Characteristics.LeftTrackedHand, out var xrInputDevice) ? GetTrackingStatus(xrInputDevice) : default;
        }

        /// <summary>
        /// Gets the tracking status of the right tracked hand device for this frame.
        /// </summary>
        /// <returns>Returns a snapshot of the tracking status for this frame.</returns>
        public static TrackingStatus GetRightTrackedHandStatus()
        {
            if (!Application.isPlaying)
            {
                return default;
            }

#if XR_HANDS_1_3_OR_NEWER
            // Try XR Hand Subsystem devices
            if (TryGetHandSubsystem(out var handSubsystem))
                return GetTrackingStatus(handSubsystem.rightHand);
#endif

            // Try XR module devices
            return TryGetDeviceWithExactCharacteristics(Characteristics.RightTrackedHand, out var xrInputDevice) ? GetTrackingStatus(xrInputDevice) : default;
        }

        /// <summary>
        /// Gets the tracking status of the left Meta Hand Tracking Aim device for this frame.
        /// </summary>
        /// <returns>Returns a snapshot of the tracking status for this frame.</returns>
        public static TrackingStatus GetLeftMetaAimHandStatus()
        {
            if (!Application.isPlaying)
            {
                return default;
            }

#if XR_HANDS_1_3_OR_NEWER
            // Try Input System devices
            // var currentDevice = InputSystem.InputSystem.GetDevice<MetaAimHand>(InputSystem.CommonUsages.LeftHand);
            // if (currentDevice != null)
            //     return GetTrackingStatus(currentDevice);
#endif

            return default;
        }

        /// <summary>
        /// Gets the tracking status of the right Meta Hand Tracking Aim device for this frame.
        /// </summary>
        /// <returns>Returns a snapshot of the tracking status for this frame.</returns>
        public static TrackingStatus GetRightMetaAimHandStatus()
        {
            if (!Application.isPlaying)
            {
                return default;
            }

#if XR_HANDS_1_3_OR_NEWER
            // Try Input System devices
            // var currentDevice = InputSystem.InputSystem.GetDevice<MetaAimHand>(InputSystem.CommonUsages.RightHand);
            // if (currentDevice != null)
            //     return GetTrackingStatus(currentDevice);
#endif

            return default;
        }

#if XR_HANDS_1_3_OR_NEWER
        /// <summary>
        /// Gets the first hand subsystem. If there are multiple, returns the first running subsystem.
        /// </summary>
        /// <param name="handSubsystem">When this method returns, contains the hand subsystem if found.</param>
        /// <returns>Returns <see langword="true"/> if a hand subsystem was found. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="SubsystemManager.GetSubsystems{T}"/>
        public static bool TryGetHandSubsystem(out XRHandSubsystem handSubsystem)
        {
            s_HandSubsystems ??= new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(s_HandSubsystems);
            if (s_HandSubsystems.Count == 0)
            {
                handSubsystem = default;
                return false;
            }

            if (s_HandSubsystems.Count > 1)
            {
                for (var i = 0; i < s_HandSubsystems.Count; ++i)
                {
                    handSubsystem = s_HandSubsystems[i];
                    if (handSubsystem.running)
                        return true;
                }
            }

            handSubsystem = s_HandSubsystems[0];
            return true;
        }
#endif

        /// <summary>
        /// Gets the first active XR input device that matches the specified <see cref="InputDeviceCharacteristics"/> exactly.
        /// </summary>
        /// <param name="desiredCharacteristics">A bitwise combination of the exact characteristics you are looking for.</param>
        /// <param name="inputDevice">When this method returns, contains the input device if a match was found.</param>
        /// <returns>Returns <see langword="true"/> if a match was found. Otherwise, returns <see langword="false"/>.</returns>
        /// <remarks>
        /// This function finds any input devices available to the XR Subsystem that match the specified <see cref="InputDeviceCharacteristics"/>
        /// bitmask exactly. The function does not include devices that only provide some of the desired characteristics or capabilities.
        /// </remarks>
        /// <seealso cref="InputDevices.GetDevicesWithCharacteristics"/>
        public static bool TryGetDeviceWithExactCharacteristics(InputDeviceCharacteristics desiredCharacteristics, out InputDevice inputDevice)
        {
            s_XRInputDevices ??= new List<InputDevice>();
            // The InputDevices.GetDevicesWithCharacteristics method does a bitwise comparison, not an equal check,
            // so it may return devices that have additional characteristic flags (HMD characteristics is a subset
            // of Eye Gaze characteristics, so this additional filtering ensures the correct device is returned if both are added).
            // Instead, get all devices and use equals to make sure the characteristics matches exactly.
            InputDevices.GetDevices(s_XRInputDevices);
            foreach (var inputDev in s_XRInputDevices)
            {
                inputDevice = inputDev;
                if (inputDevice.characteristics == desiredCharacteristics)
                {
                    return true;
                }
            }

            inputDevice = default;
            return false;
        }

#if OPENXR_1_6_OR_NEWER
        private static TrackingStatus GetTrackingStatus(EyeGazeInteraction.EyeGazeDevice device)
        {
            if (device == null)
                return default;

            return new TrackingStatus
            {
                isConnected = device.added,
                isTracked = device.pose.isTracked.isPressed,
                trackingState = (InputTrackingState)device.pose.trackingState.value,
            };
        }
#endif

        private static TrackingStatus GetTrackingStatus(InputDevice device)
        {
            return new TrackingStatus
            {
                IsConnected = device.isValid,
                IsTracked = device.TryGetFeatureValue(CommonUsages.isTracked, out var isTracked) && isTracked,
                TrackingState = device.TryGetFeatureValue(CommonUsages.trackingState, out var trackingState) ? trackingState : InputTrackingState.None,
            };
        }

#if XR_HANDS_1_3_OR_NEWER
        private static TrackingStatus GetTrackingStatus(XRHand hand)
        {
            return new TrackingStatus
            {
                isConnected = true,
                isTracked = hand.isTracked,
                trackingState = hand.isTracked ? InputTrackingState.Position | InputTrackingState.Rotation : InputTrackingState.None,
            };
        }
#endif
    }
}
