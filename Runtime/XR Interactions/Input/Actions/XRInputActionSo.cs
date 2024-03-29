using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VaporEvents;
using VaporInspector;
using VaporXR.Utilities;

namespace VaporXR
{   
    public class XRInputActionSo : ScriptableObject
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
        public InputInteractionState CurrentState { get; private set; }
        public bool IsActive => BoundAction.enabled;
        public bool IsHeld => CurrentState.Active;
        public float CurrentValue => CurrentState.Value;
        public bool IsDefaultAction { get; set; }


        private readonly UnityObjectReferenceCache<InputActionReference> m_InputActionReferenceCache = new();
        public bool TryGetInputActionReference(out InputActionReference reference) => m_InputActionReferenceCache.TryGet(_actionReference, out reference);

        private InputActionManager _manager;
        protected float _lastValue;
        protected float _currentValue;
        protected bool _wasPressed;
        protected bool _wasReleased;

        public event Action Pressed;
        public event Action Released;

        protected void OnPressed() => Pressed?.Invoke();
        protected void OnReleased() => Released?.Invoke();

        public void BindAction()
        {
            if (!_manager)
            {
                _manager = ProviderBus.GetComponent<InputActionManager>("Input Manager");
                BoundAction = _manager.CreateActionClone(_actionReference.action.id);
                CurrentState = new();
            }
        }

        public void Enable()
        {
            if (!TryGetInputActionReference(out var reference)) { return; }
            BindAction();

            _manager.RegisterForInputUpdate(UpdateInput);
            _manager.RegisterForPostInputUpdate(PostUpdateInput);

            if (!IsDefaultAction)
            {
                _manager.EnableActionOverride(reference.action.id, this);
            }

            BoundAction.Enable();
        }

        public void Disable(bool returnToDefault)
        {
            if (!TryGetInputActionReference(out var reference)) { return; }
            BindAction();

            _manager.UnRegisterForInputUpdate(UpdateInput);
            _manager.UnRegisterForPostInputUpdate(PostUpdateInput);

            BoundAction.Disable();

            if (!IsDefaultAction && returnToDefault)
            {
                _manager.ReturnToDefaultAction(reference.action.id);
            }
        }

        protected virtual void UpdateInput() { }

        protected virtual void PostUpdateInput() { }

        protected virtual bool IsPressed() { return false; }

        protected virtual void FireEvents() { }
    }
}
