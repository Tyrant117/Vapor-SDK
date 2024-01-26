using System;
using UnityEngine;
using VaporInspector;

namespace VaporXR
{
    [Serializable, DrawWithVapor]
    public class Axis1DInputProvider
    {
        // Inspector
        [SerializeField]
        private XRInputDeviceFloatValueReader _reader;
        
        // Properties
        /// <summary>
        /// The current value of the assigned axis.
        /// </summary>
        public float CurrentValue { get; protected set; }

        private float _lastValue;
        private IInputDeviceUpdateProvider _updateProvider;

        /// <summary>
        /// The event fired when the axis value changes by a small tolerance.
        /// </summary>
        public event Action AxisChanged;

        public void BindToUpdateEvent(IInputDeviceUpdateProvider sourceUpdate)
        {
            if (_reader == null)
            {
                return;
            }
            
            _updateProvider = sourceUpdate;
            _updateProvider.RegisterForInputUpdate(UpdateInput);
        }

        public void UnbindUpdateEvent()
        {
            if (_reader == null)
            {
                return;
            }
            
            if (_updateProvider == null)
            {
                return;
            }
            
            _updateProvider.UnRegisterForInputUpdate(UpdateInput);
            _updateProvider = null;
        }

        private void UpdateInput()
        {
            _lastValue = CurrentValue;
            CurrentValue = _reader.ReadValue();

            FireEvents();
        }
        
        public void FireEvents()
        {
            if (!Mathf.Approximately(_lastValue, CurrentValue))
            {
                AxisChanged?.Invoke();
            }
        }
    }
}
