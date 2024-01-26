using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace VaporXR
{
    /// <summary>
    /// <see cref="InputInteractionState"/> type to hold current state for a given interaction.
    /// </summary>
    public class InputInteractionState
    {
        private float _value;
        /// <summary>
        /// The value of the interaction in this frame.
        /// </summary>
        public float Value
        {
            get => _value;
            set => _value = value;
        }

        private bool _active;
        /// <summary>
        /// Whether it is currently on.
        /// </summary>
        public bool Active
        {
            get => _active;
            set => _active = value;
        }

        private bool _activatedThisFrame;

        /// <summary>
        /// Whether the interaction state activated this frame.
        /// </summary>
        public bool ActivatedThisFrame
        {
            get => _activatedThisFrame;
            set => _activatedThisFrame = value;
        }

        private bool _deactivatedThisFrame;

        /// <summary>
        /// Whether the interaction state deactivated this frame.
        /// </summary>
        public bool DeactivatedThisFrame
        {
            get => _deactivatedThisFrame;
            set => _deactivatedThisFrame = value;
        }

        private int _clickCount;
        public int ClickCount
        {
            get => _clickCount;
            set => _clickCount = value;
        }
        private float _lastActivated;
        public float LastActivated
        {
            get => _lastActivated;
            set => _lastActivated = value;
        }

        /// <summary>
        /// Sets the interaction state for this frame. This method should only be called once per frame.
        /// </summary>
        /// <param name="isActive">Whether the state is active (in other words, pressed).</param>
        public void SetFrameState(bool isActive)
        {
            SetFrameState(isActive, isActive ? 1f : 0f);
        }

        /// <summary>
        /// Sets the interaction state for this frame. This method should only be called once per frame.
        /// </summary>
        /// <param name="isActive">Whether the state is active (in other words, pressed).</param>
        /// <param name="newValue">The interaction value.</param>
        public void SetFrameState(bool isActive, float newValue)
        {
            Value = newValue;
            ActivatedThisFrame = !Active && isActive;
            DeactivatedThisFrame = Active && !isActive;
            Active = isActive;

            // Increment Click Count
            if (Time.time - LastActivated >= 0.5f)
            {
                ClickCount = 0;
            }

            if (ActivatedThisFrame)
            {
                ClickCount++;
                LastActivated = Time.time;
            }
        }

        /// <summary>
        /// Sets the interaction state that are based on whether they occurred "this frame".
        /// </summary>
        /// <param name="wasActive">Whether the previous state is active (in other words, pressed).</param>
        public void SetFrameDependent(bool wasActive)
        {
            ActivatedThisFrame = !wasActive && Active;
            DeactivatedThisFrame = wasActive && !Active;
        }

        /// <summary>
        /// Resets the interaction states that are based on whether they occurred "this frame".
        /// </summary>
        /// <seealso cref="ActivatedThisFrame"/>
        /// <seealso cref="DeactivatedThisFrame"/>
        public void ResetFrameDependent()
        {
            ActivatedThisFrame = false;
            DeactivatedThisFrame = false;
        }

        public void ResetClickCount()
        {
            ClickCount = 0;
        }
    }
}
