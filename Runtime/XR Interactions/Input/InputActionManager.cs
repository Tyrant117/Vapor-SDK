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

        private readonly Dictionary<Guid, InputAction> _actionOverrideMap = new();

        public event Action InputUpdated;
        public event Action PostInputUpdated;

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

            //foreach (var actionAsset in m_ActionAssets)
            //{
            //    if (actionAsset != null)
            //    {
            //        actionAsset.Enable();
            //    }
            //}
            _actionAsset.Enable();
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

            //foreach (var actionAsset in m_ActionAssets)
            //{
            //    if (actionAsset != null)
            //    {
            //        actionAsset.Disable();
            //    }
            //}
            _actionAsset.Disable();
        }

        

        public InputAction CreateActionClone(Guid actionGuid)
        {
            var actionToClone = _actionAsset.FindAction(actionGuid);
            var clone = actionToClone.Clone();
            return clone;
        }

        public void EnableActionOverride(Guid guid, InputAction overrideAction)
        {
            var actionToOverride = _actionAsset.FindAction(guid);
            if(_actionOverrideMap.TryGetValue(guid, out var currentOverride))
            {
                // If its already overriden by another action.
                // Disable that action
                currentOverride.Disable();
            }
            actionToOverride.Disable();
            _actionOverrideMap[guid] = overrideAction;
            overrideAction.Enable();
        }

        public void DisableActionOverride(Guid guid)
        {
            var actionToOverride = _actionAsset.FindAction(guid);
            if (_actionOverrideMap.TryGetValue(guid, out var currentOverride))
            {
                // If its already overriden by another action.
                // Disable that action
                currentOverride.Disable();
            }
            actionToOverride.Enable();
        }
    }
}
