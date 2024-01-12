using System;
using UnityEngine;

namespace VaporXR
{
    /// <summary>
    /// <see cref="InputInteractionState"/> type to hold current state for a given interaction.
    /// </summary>
    [Serializable]
    public struct InputInteractionState
    {
        [Range(0f, 1f)]
        [SerializeField]
        private float m_Value;

        /// <summary>
        /// The value of the interaction in this frame.
        /// </summary>
        public float Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        [SerializeField]
        private bool m_Active;

        /// <summary>
        /// Whether it is currently on.
        /// </summary>
        public bool Active
        {
            get => m_Active;
            set => m_Active = value;
        }

        private bool m_ActivatedThisFrame;

        /// <summary>
        /// Whether the interaction state activated this frame.
        /// </summary>
        public bool ActivatedThisFrame
        {
            get => m_ActivatedThisFrame;
            set => m_ActivatedThisFrame = value;
        }

        private bool m_DeactivatedThisFrame;

        /// <summary>
        /// Whether the interaction state deactivated this frame.
        /// </summary>
        public bool DeactivatedThisFrame
        {
            get => m_DeactivatedThisFrame;
            set => m_DeactivatedThisFrame = value;
        }

        private int clickCount;
        public int ClickCount
        {
            get => clickCount;
            set => clickCount = value;
        }
        private float lastActivated;
        public float LastActivated
        {
            get => lastActivated;
            set => lastActivated = value;
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
