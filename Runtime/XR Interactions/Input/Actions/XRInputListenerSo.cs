using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VaporEvents;
using VaporInspector;
using VaporXR.Utilities;

namespace VaporXR
{   
    public class XRInputListenerSo : ScriptableObject
    {
        [TitleGroup("Reference"), SerializeField, OnValueChanged("ReferenceUpdated")]
        private InputActionReference _actionReference;
        [TitleGroup("Reference"), SerializeField]
        private string _actionReferenceId;
        [TitleGroup("Reference"), Button]
#pragma warning disable IDE0051 // Remove unused private members
        private void Validate()
        {
            if (_actionReference)
            {
                if (!_actionReferenceId.Equals(_actionReference.action.id))
                {
                    _actionReferenceId = _actionReference.action.id.ToString();
                }
            }
            else
            {
#if UNITY_EDITOR
                if (string.IsNullOrEmpty(_actionReferenceId))
                {
                    return;
                }
                var assets = UnityEditor.AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/Vapor/XR/Readers/VXR Default Input Actions.inputactions");
                var guid = new Guid(_actionReferenceId);
                foreach (var sub in assets)
                {
                    if (sub is InputActionReference iar && iar.action.id == guid)
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
                _actionReferenceId = _actionReference.action.id.ToString();
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
