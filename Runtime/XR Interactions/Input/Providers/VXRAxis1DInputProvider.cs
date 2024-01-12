using System;
using UnityEngine;
using VaporInspector;

namespace VaporXR
{
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Controllers)]
    public class VXRAxis1DInputProvider : MonoBehaviour, IInputDeviceUpdateProvider
    {
        [SerializeField] 
        [RichTextTooltip("The wrapped provider for this MonoBehaviour")]
        private Axis1DInputProvider _inputProvider;

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
