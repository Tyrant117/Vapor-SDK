using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporInspector;

namespace VaporXR
{
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Controllers)]
    public class VXRAxis2DInputProvider : MonoBehaviour, IInputDeviceUpdateProvider
    {
        [SerializeField]
        [RichTextTooltip("The wrapped provider for this MonoBehaviour")]
        private Axis2DInputProvider _provider;
        
        public event Action InputUpdated;
        public event Action PostInputUpdated;

        private void OnEnable()
        {
            _provider.BindToUpdateEvent(this);
        }

        private void OnDisable()
        {
            _provider.UnbindUpdateEvent();
        }
        
        private void Update()
        {
            InputUpdated?.Invoke();
            PostInputUpdated?.Invoke();
        }
        
        public void RegisterForInputUpdate(Action callback)
        {
            InputUpdated += callback;
        }

        public void UnRegisterForInputUpdate(Action callback)
        {
            InputUpdated -= callback;
        }

        public void RegisterForPostInputUpdate(Action callback)
        {
            PostInputUpdated += callback;
        }

        public void UnRegisterForPostInputUpdate(Action callback)
        {
            PostInputUpdated -= callback;
        }
    }
}
