using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporXR
{
    [System.Serializable]
    public class CompositeButtonInputProvider
    {
        [SerializeField] private List<ButtonInputProvider> _buttonInputProviders;
        
        public InputInteractionState CurrentState { get; private set; }
        public bool IsHeld => CurrentState.Active;
        
        private IInputDeviceUpdateProvider _updateProvider;
        
        public event Action Activated;
        public event Action Deactivated;

        public void BindUpdateSource(IInputDeviceUpdateProvider sourceUpdate)
        {
            _updateProvider = sourceUpdate;
            _updateProvider.RegisterForInputUpdate(UpdateInput);
            _updateProvider.RegisterForPostInputUpdate(PostUpdateInput);
        }

        public void UnbindUpdateSource()
        {
            foreach (var button in _buttonInputProviders)
            {
                button.Setup();
            }

            _updateProvider.UnRegisterForInputUpdate(UpdateInput);
            _updateProvider.UnRegisterForPostInputUpdate(PostUpdateInput);
            _updateProvider = null;
        }

        private void UpdateInput()
        {
            foreach (var button in _buttonInputProviders)
            {
                button.UpdateInput();
            }
        }

        private void PostUpdateInput()
        {
                CurrentState.ResetFrameDependent();

                var active = true;
                foreach (var button in _buttonInputProviders)
                {
                    if (button.CurrentState.Active) continue;
                    
                    active = false;
                    break;
                }

                CurrentState.SetFrameState(active);

                FireEvents();
        }
        
        private void FireEvents()
        {
            if (CurrentState.ActivatedThisFrame)
            {
                Activated?.Invoke();
            }
            else if (CurrentState.DeactivatedThisFrame)
            {
                Deactivated?.Invoke();
            }
        }
    }
}
