using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace VaporXR
{
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Controllers)]
    public class VXRButtonInputProvider : MonoBehaviour, IInputDeviceUpdateProvider
    {
        [SerializeField]
        [Tooltip("The wrapped provider for this MonoBehaviour")]
        private ButtonInputProvider _inputProvider;

        public InputInteractionState CurrentState => _inputProvider.CurrentState;
        public bool IsHeld => _inputProvider.IsHeld;
        
        public event Action InputUpdated;
        public event Action PostInputUpdated;
        
        private void OnEnable()
        {
            _inputProvider.BindToUpdateEvent(this);
        }

        private void OnDisable()
        {
            _inputProvider.UnbindUpdateEvent();
        }
        
        private void Update()
        {
            InputUpdated?.Invoke();
            PostInputUpdated?.Invoke();
        }
        
        public void RegisterForInputUpdate(Action callback) => InputUpdated += callback;
        public void UnRegisterForInputUpdate(Action callback) => InputUpdated -= callback;

        public void RegisterForPostInputUpdate(Action callback) => PostInputUpdated += callback;
        public void UnRegisterForPostInputUpdate(Action callback) => PostInputUpdated -= callback;

        public void AddActivatedListener(Action callback) => _inputProvider.Activated += callback;
        public void RemoveActivatedListener(Action callback) => _inputProvider.Activated -= callback;
        public void AddDeactivatedListener(Action callback) => _inputProvider.Deactivated += callback;
        public void RemoveDeactivatedListener(Action callback) => _inputProvider.Deactivated -= callback;
    }
}
