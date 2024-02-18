using UnityEngine;
using UnityEngine.Assertions;

namespace VaporXR
{
    /// <summary>
    /// Interpreted input from an input reader. Represents the logical state of an interaction input,
    /// such as the select input, which may not be the same as the physical state of the input.
    /// </summary>
    /// <seealso cref="InputTriggerType"/>
    public class LogicalInputState
    {
        /// <summary>
        /// Whether the logical input state is currently active.
        /// </summary>
        public bool Active { get; private set; }

        private InputTriggerType _mode;
        /// <summary>
        /// The type of input
        /// </summary>
        public InputTriggerType Mode
        {
            get => _mode;
            set
            {
                if (_mode == value) return;

                _mode = value;
                Refresh();
            }
        }

        /// <summary>
        /// Read whether the button is currently performed, which typically means whether the button is being pressed.
        /// This is typically true for multiple frames.
        /// </summary>
        /// <seealso cref="IXRInputButtonReader.IsPressed"/>
        public bool IsPerformed { get; private set; }

        /// <summary>
        /// Read whether the button performed this frame, which typically means whether the button started being pressed during this frame.
        /// This is typically only true for one single frame.
        /// </summary>
        /// <seealso cref="IXRInputButtonReader.ReadWasPerformedThisFrame"/>
        public bool WasPerformedThisFrame { get; private set; }

        /// <summary>
        /// Read whether the button stopped performing this frame, which typically means whether the button stopped being pressed during this frame.
        /// This is typically only true for one single frame.
        /// </summary>
        public bool WasUnperformedThisFrame { get; private set; }

        /// <summary>
        /// The current value of the input from a value of 0 to 1 inclusive.
        /// </summary>
        public float CurrentValue { get; private set; }

        private bool _hasSelection;

        private float _timeAtPerformed;
        private float _timeAtUnperformed;

        private bool _toggleActive;
        private bool _toggleDeactivatedThisFrame;
        private bool _waitingForDeactivate;

        // Add wasUnperformedThisFrame when Input System 1.8.0 is available since this method may not be called every frame.
        // Kept internal until then to avoid breaking changes.
        // See https://github.com/Unity-Technologies/InputSystem/pull/1795
        public void UpdateInput(bool performed, bool performedThisFrame, bool hasSelection, float currentValue) =>
            UpdateInput(performed, performedThisFrame, hasSelection, currentValue, Time.realtimeSinceStartup);

        private void UpdateInput(bool performed, bool performedThisFrame, bool hasSelection, float currentValue, float realtime)
        {
            var prevPerformed = IsPerformed;

            IsPerformed = performed;
            WasPerformedThisFrame = performedThisFrame;
            WasUnperformedThisFrame = prevPerformed && !performed;
            CurrentValue = currentValue;
            _hasSelection = hasSelection;

            if (WasPerformedThisFrame)
                _timeAtPerformed = realtime;

            if (WasUnperformedThisFrame)
                _timeAtUnperformed = realtime;

            _toggleDeactivatedThisFrame = false;
            if (Mode == InputTriggerType.Toggle || Mode == InputTriggerType.Sticky)
            {
                if (_toggleActive && performedThisFrame)
                {
                    _toggleActive = false;
                    _toggleDeactivatedThisFrame = true;
                    _waitingForDeactivate = true;
                }

                if (WasUnperformedThisFrame)
                    _waitingForDeactivate = false;
            }

            Refresh();
        }

        public void UpdateHasSelection(bool hasSelection)
        {
            if (_hasSelection == hasSelection)
                return;

            // Reset toggle values when no longer selecting
            // (can happen by another Interactor taking the Interactable or through method calls).
            _hasSelection = hasSelection;
            _toggleActive = hasSelection;
            _waitingForDeactivate = false;

            Refresh();
        }

        private void Refresh()
        {
            switch (Mode)
            {
                case InputTriggerType.State:
                    Active = IsPerformed;
                    break;

                case InputTriggerType.StateChange:
                    Active = WasPerformedThisFrame || (_hasSelection && !WasUnperformedThisFrame);
                    break;

                case InputTriggerType.Toggle:
                    Active = _toggleActive || (WasPerformedThisFrame && !_toggleDeactivatedThisFrame);
                    break;

                case InputTriggerType.Sticky:
                    Active = _toggleActive || _waitingForDeactivate || WasPerformedThisFrame;
                    break;

                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(InputTriggerType)}={Mode}");
                    break;
            }
        }
    }
}
