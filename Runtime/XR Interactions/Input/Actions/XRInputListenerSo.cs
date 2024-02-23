using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VaporEvents;
using VaporInspector;

namespace VaporXR
{
    public class XRInputListenerSo : ScriptableObject
    {
        [TitleGroup("Reference"), SerializeField, OnValueChanged("ReferenceUpdated")]
        private InputActionReference _actionReference = null;
        [TitleGroup("Reference"), SerializeField]
        private string _actionReferenceName = "";
        [TitleGroup("Reference"), Button]
#pragma warning disable IDE0051 // Remove unused private members
        private void Validate()
        {
            if (_actionReference)
            {
                if (_actionReferenceName != _actionReference.name)
                {
                    _actionReferenceName = _actionReference.name;
                }
            }
            else
            {
#if UNITY_EDITOR
                if (string.IsNullOrEmpty(_actionReferenceName))
                {
                    return;
                }
                var assets = UnityEditor.AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/Vapor/XR/Readers/VXR Default Input Actions.inputactions");
                foreach (var sub in assets)
                {
                    if (sub is InputActionReference iar && iar.name == _actionReferenceName)
                    {
                        _actionReference = iar;
                        break;
                    }
                }
#endif
            }
        }
#pragma warning disable IDE0051 // Remove unused private members
        private void ReferenceUpdated()
        {
            if (_actionReference)
            {
                _actionReferenceName = _actionReference.name;
            }
        }
#pragma warning restore IDE0051 // Remove unused private members

        public InputAction BoundAction { get; private set; }


        private InputActionManager _manager;

        protected void BindAction()
        {
            if (!_manager)
            {
                _manager = ProviderBus.GetComponent<InputActionManager>("Input Manager");
                BoundAction = _manager.CreateActionClone(_actionReference.action.id);
            }
        }

        public void Enable()
        {
            BindAction();

            BoundAction.Enable();
        }

        public void Disable()
        {
            BindAction();

            BoundAction.Disable();
        }

        protected T ReadValue<T>() where T : struct
        {
            return BoundAction.ReadValue<T>();
        }
    }
}
