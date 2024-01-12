using System;
using System.Collections.Generic;
using UnityEngine.XR;

namespace VaporXR
{
    /// <summary>
    /// Helper class to monitor input devices from the XR module and invoke an event
    /// when the device is tracked. Used in the behavior to keep a GameObject deactivated
    /// until the device becomes tracked, at which point the callback method can activate it.
    /// </summary>
    /// <seealso cref="TrackedDeviceMonitor"/>
    public class InputDeviceMonitor
    {
        /// <summary>
        /// Event that is invoked one time when the device is tracked.
        /// </summary>
        /// <seealso cref="AddDevice"/>
        /// <seealso cref="CommonUsages.isTracked"/>
        /// <seealso cref="InputTracking.trackingAcquired"/>
        public event Action<InputDevice> TrackingAcquired;

        private readonly List<InputDevice> _monitoredDevices = new List<InputDevice>();
        private bool _subscribed;

        /// <summary>
        /// Add an input device to monitor and invoke <see cref="TrackingAcquired"/>
        /// one time when the device is tracked. The device is automatically removed
        /// from being monitored when tracking is acquired.
        /// </summary>
        /// <param name="device">The input device to start monitoring.</param>
        /// <remarks>
        /// Waits until the next global tracking acquired event to read if the device is tracked.
        /// </remarks>
        public void AddDevice(InputDevice device)
        {
            // Start subscribing if necessary
            if (_monitoredDevices.Contains(device)) return;
            
            _monitoredDevices.Add(device);
            Subscribe();
        }

        /// <summary>
        /// Stop monitoring the device for its tracked status.
        /// </summary>
        /// <param name="device">The input device to stop monitoring</param>
        public void RemoveDevice(InputDevice device)
        {
            // Stop subscribing if there are no devices left to monitor
            if (_monitoredDevices.Remove(device) && _monitoredDevices.Count == 0)
            {
                Unsubscribe();
            }
        }

        /// <summary>
        /// Stop monitoring all devices for their tracked status.
        /// </summary>
        public void ClearAllDevices()
        {
            if (_monitoredDevices.Count <= 0) return;
            
            _monitoredDevices.Clear();
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || _monitoredDevices.Count <= 0) return;
            
            InputTracking.trackingAcquired += OnTrackingAcquired;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            
            InputTracking.trackingAcquired -= OnTrackingAcquired;
            _subscribed = false;
        }

        private void OnTrackingAcquired(XRNodeState nodeState)
        {
            // The XRNodeState argument is ignored since there can be overlap of different input devices
            // at that XRNode, so instead each monitored device is read for its IsTracked state.
            // If the InputDevice constructor is ever made public instead of internal, we could instead just
            // get the InputDevice from the XRNodeState.uniqueID since that corresponds to the InputDevice.deviceId.
            // For the typically small number of devices monitored, an extra read call is not too expensive.

            for (var index = 0; index < _monitoredDevices.Count; ++index)
            {
                var device = _monitoredDevices[index];
                if (!(device.TryGetFeatureValue(CommonUsages.isTracked, out var isTracked) && isTracked))
                {
                    continue;
                }

                // Stop monitoring and invoke event
                _monitoredDevices.RemoveAt(index);
                --index;

                TrackingAcquired?.Invoke(device);
            }

            // Once all monitored devices have been tracked, unsubscribe from the global event
            if (_monitoredDevices.Count != 0)
            {
                return;
            }

            Unsubscribe();
        }
    }
}
