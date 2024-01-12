using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Controllers)]
    public class VXRInputDeviceUpdateProvider : MonoBehaviour, IInputDeviceUpdateProvider
    {
        public event Action InputUpdated;
        public event Action PostInputUpdated;
        
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
