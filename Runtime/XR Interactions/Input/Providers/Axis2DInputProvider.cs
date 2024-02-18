using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporInspector;

namespace VaporXR
{
    [Serializable, DrawWithVapor]
    public class Axis2DInputProvider
    {
        // Inspector
        [SerializeField]
        private XRInputVector2Reader _reader;
        
        // Properties
        /// <summary>
        /// The current value of the assigned axis.
        /// </summary>
        public Vector2 CurrentValue { get; protected set; }

        private Vector2 _lastValue;
        private IInputDeviceUpdateProvider _updateProvider;

        /// <summary>
        /// The event fired when the axis value changes by a small tolerance.
        /// </summary>
        public event Action<Vector2> AxisChanged;
        
        public void BindToUpdateEvent(IInputDeviceUpdateProvider sourceUpdate)
        {
            if (!_reader.IsValid)
            {
                return;
            }
            
            _updateProvider = sourceUpdate;
            _updateProvider.RegisterForInputUpdate(UpdateInput);
        }

        public void UnbindUpdateEvent()
        {
            if (!_reader.IsValid)
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
            if (!CurrentValue.Equals(_lastValue))
            {
                AxisChanged?.Invoke(CurrentValue);
            }
        }
        
        public Cardinal GetCardinal(bool includecCompositeCardinals)
        {
            return CardinalUtility.GetNearestCardinal(CurrentValue, includecCompositeCardinals);
        }

        public float GetAngle()
        {
            return CardinalUtility.GetNearestAngle(CurrentValue);
        }
    }
}
