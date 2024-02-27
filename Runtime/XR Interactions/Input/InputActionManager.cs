using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VaporEvents;

namespace VaporXR
{
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_XRInputDeviceButtonReader)]
    public class InputActionManager : ProvidedMonoBehaviour, IInputDeviceUpdateProvider
    {
        [SerializeField]
        [Tooltip("Input action assets to affect when inputs are enabled or disabled.")]
        private InputActionAsset _actionAsset;

        [SerializeField]
        [Tooltip("A list of the default button actions that will be the fallback actions if the button is not being overriden.\n" +
            "These should be your main control layout actions like move, jump, grab, etc. If a mapped button action does not have a default it will use the action in the ActionAsset")]
        private List<XRInputActionSo> _defaultButtonActions;

        private readonly Dictionary<Guid, XRInputActionSo> _actionOverrideMap = new();
        private readonly Dictionary<Guid, XRInputActionSo> _defaultActionMap = new();

        public event Action InputUpdated;
        public event Action PostInputUpdated;

        private void Awake()
        {
            foreach (var action in _defaultButtonActions)
            {
                if (action.TryGetInputActionReference(out var reference))
                {
                    action.IsDefaultAction = true;
                    _defaultActionMap.TryAdd(reference.action.id, action);
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnableInput();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            DisableInput();
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

        /// <summary>
        /// Enable all actions referenced by this component.
        /// </summary>
        /// <remarks>
        /// Unity will automatically call this function when this <see cref="InputActionManager"/> component is enabled.
        /// However, this method can be called to enable input manually, such as after disabling it with <see cref="DisableInput"/>.
        /// <br />
        /// Enabling inputs only enables the action maps contained within the referenced
        /// action map assets (see <see cref="ActionAssets"/>).
        /// </remarks>
        /// <seealso cref="DisableInput"/>
        public void EnableInput()
        {
            if (_actionAsset == null)
            {
                return;
            }

            _actionAsset.Enable();

            foreach (var kvp in _defaultActionMap)
            {
                var actionToOverride = _actionAsset.FindAction(kvp.Key);
                actionToOverride.Disable();

                kvp.Value.Enable();
            }
        }

        /// <summary>
        /// Disable all actions referenced by this component.
        /// </summary>
        /// <remarks>
        /// This function will automatically be called when this <see cref="InputActionManager"/> component is disabled.
        /// However, this method can be called to disable input manually, such as after enabling it with <see cref="EnableInput"/>.
        /// <br />
        /// Disabling inputs only disables the action maps contained within the referenced
        /// action map assets (see <see cref="ActionAssets"/>).
        /// </remarks>
        /// <seealso cref="EnableInput"/>
        public void DisableInput()
        {
            if (_actionAsset == null)
            {
                return;
            }

            _actionAsset.Disable();

            foreach (var kvp in _defaultActionMap)
            {
                kvp.Value.Disable(false);
            }
        }        

        public InputAction CreateActionClone(Guid actionGuid)
        {
            var actionToClone = _actionAsset.FindAction(actionGuid);
            var clone = actionToClone.Clone();
            return clone;
        }

        public void EnableActionOverride(Guid guid, XRInputActionSo overrideAction)
        {
            if (_actionOverrideMap.TryGetValue(guid, out var currentOverride) && currentOverride != overrideAction)
            {
                // If its already overriden by another action.
                // Disable that action
                currentOverride.Disable(false);
            }

            if (_defaultActionMap.TryGetValue(guid, out var defaultAction))
            {
                defaultAction.Disable(false);
            }
            else
            {
                var actionToOverride = _actionAsset.FindAction(guid);
                actionToOverride.Disable();
            }            

            _actionOverrideMap[guid] = overrideAction;
        }

        public void ReturnToDefaultAction(Guid guid)
        {
            if (_defaultActionMap.TryGetValue(guid, out var defaultAction))
            {
                defaultAction.Enable();
            }
            else
            {
                var actionToOverride = _actionAsset.FindAction(guid);
                actionToOverride.Enable();
            }
        }
    }
}
