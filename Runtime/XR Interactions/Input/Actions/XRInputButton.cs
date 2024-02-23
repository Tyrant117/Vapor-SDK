using System;
using UnityEngine;
using VaporInspector;

namespace VaporXR
{
    [Serializable, DrawWithVapor]
    public class XRInputButton : IDisposable
    {
        public enum ButtonReadType
        {
            DirectButton,
            AxisButton,
            SectorButton
        }

#pragma warning disable IDE0051 // Remove unused private members
        private bool IsDirectButton => _buttonType == ButtonReadType.DirectButton;
        private bool IsAxisButton => _buttonType == ButtonReadType.AxisButton;
        private bool IsSectorButton => _buttonType == ButtonReadType.SectorButton;
#pragma warning restore IDE0051 // Remove unused private members

        [SerializeField] 
        private ButtonReadType _buttonType;

        [SerializeField, ShowIf("$IsBoolListener")]
        private XRInputActionButtonSo _button;

        [SerializeField, ShowIf("$IsFloatListener")]
        private XRInputActionAxisButtonSo _axisButton;

        [SerializeField, ShowIf("$IsVector2Listener")]
        private XRInputActionSectorButtonSo _sectorButton;


        private XRInputActionSo _activeButton;
        public InputInteractionState State => _activeButton.CurrentState;
        public bool IsActive => _activeButton.IsActive;
        public bool IsHeld => State.Active;
        public float CurrentValue => State.Value;

        public Action Pressed;
        public Action Released;

        public XRInputActionSo BindInput()
        {
            switch (_buttonType)
            {
                case ButtonReadType.DirectButton:
                    _button.BindAction();
                    _activeButton = _button;
                    break;
                case ButtonReadType.AxisButton:
                    _axisButton.BindAction();
                    _activeButton = _axisButton;
                    break;
                case ButtonReadType.SectorButton:
                    _sectorButton.BindAction();
                    _activeButton = _sectorButton;
                    break;
            }
            _activeButton.Pressed += OnPressed;
            _activeButton.Released += OnReleased;

            return _activeButton;
        }

        public void Enable()
        {
            _activeButton.Enable();
        }

        public void Disable()
        {
            _activeButton.Disable();
        }

        private void OnPressed()
        {
            Pressed?.Invoke();
        }

        private void OnReleased()
        {
            Released?.Invoke();
        }

        public void Dispose()
        {
            Pressed = null;
            Released = null;
        }
    }
}
