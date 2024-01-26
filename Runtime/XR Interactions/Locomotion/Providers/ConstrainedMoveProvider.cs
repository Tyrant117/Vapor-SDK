using UnityEngine;

namespace VaporXR.Locomotion
{
    /// <summary>
    /// Base class for a locomotion provider that allows for constrained movement with a <see cref="CharacterController"/>.
    /// </summary>
    /// <seealso cref="LocomotionProvider"/>
    public abstract class ConstrainedMoveProvider : LocomotionProvider
    {
        [SerializeField]
        [Tooltip("Controls whether to enable unconstrained movement along the x-axis.")]
        bool m_EnableFreeXMovement = true;
        /// <summary>
        /// Controls whether to enable unconstrained movement along the x-axis.
        /// </summary>
        public bool enableFreeXMovement
        {
            get => m_EnableFreeXMovement;
            set => m_EnableFreeXMovement = value;
        }

        [SerializeField]
        [Tooltip("Controls whether to enable unconstrained movement along the y-axis.")]
        bool m_EnableFreeYMovement;
        /// <summary>
        /// Controls whether to enable unconstrained movement along the y-axis.
        /// </summary>
        public bool enableFreeYMovement
        {
            get => m_EnableFreeYMovement;
            set => m_EnableFreeYMovement = value;
        }

        [SerializeField]
        [Tooltip("Controls whether to enable unconstrained movement along the z-axis.")]
        bool m_EnableFreeZMovement = true;
        /// <summary>
        /// Controls whether to enable unconstrained movement along the z-axis.
        /// </summary>
        public bool enableFreeZMovement
        {
            get => m_EnableFreeZMovement;
            set => m_EnableFreeZMovement = value;
        }

        [SerializeField]
        [Tooltip("Controls whether gravity applies to constrained axes when a Character Controller is used.")]
        bool m_UseGravity = true;
        /// <summary>
        /// Controls whether gravity applies to constrained axes when a <see cref="CharacterController"/> is used.
        /// </summary>
        public bool useGravity
        {
            get => m_UseGravity;
            set => m_UseGravity = value;
        }

        /// <summary>
        /// The transformation that is used by this component to apply translation movement.
        /// </summary>
        public XROriginMovement transformation { get; set; } = new XROriginMovement();

        CharacterController m_CharacterController;
        bool m_AttemptedGetCharacterController;
        bool m_IsMovingXROrigin;
        Vector3 m_GravityDrivenVelocity;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Update()
        {
            m_IsMovingXROrigin = false;

            var xrOrigin = Mediator.XROrigin?.Origin;
            if (xrOrigin == null)
                return;

            if (!m_IsMovingXROrigin)
                TryEndLocomotion();
        }

        /// <summary>
        /// Determines how much to move the rig.
        /// </summary>
        /// <param name="attemptingMove">Whether the provider is attempting to move.</param>
        /// <returns>Returns the translation amount in world space to move the rig.</returns>
        protected abstract Vector3 ComputeDesiredMove(out bool attemptingMove);

        /// <summary>
        /// Creates a locomotion event to move the rig by <paramref name="translationInWorldSpace"/>,
        /// and optionally restricts movement along each axis and applies gravity.
        /// </summary>
        /// <param name="translationInWorldSpace">The translation amount in world space to move the rig
        /// (before restricting movement along each axis and applying gravity).</param>
        protected virtual void MoveRig(Vector3 translationInWorldSpace)
        {
            FindCharacterController();

            var motion = translationInWorldSpace;
            if (!m_EnableFreeXMovement)
                motion.x = 0f;
            if (!m_EnableFreeYMovement)
                motion.y = 0f;
            if (!m_EnableFreeZMovement)
                motion.z = 0f;

            if (m_CharacterController != null && m_CharacterController.enabled)
            {
                // Step vertical velocity from gravity
                if (m_CharacterController.isGrounded || !m_UseGravity)
                {
                    m_GravityDrivenVelocity = Vector3.zero;
                }
                else
                {
                    m_GravityDrivenVelocity += Physics.gravity * Time.deltaTime;
                    if (m_EnableFreeXMovement)
                        m_GravityDrivenVelocity.x = 0f;
                    if (m_EnableFreeYMovement)
                        m_GravityDrivenVelocity.y = 0f;
                    if (m_EnableFreeZMovement)
                        m_GravityDrivenVelocity.z = 0f;
                }

                motion += m_GravityDrivenVelocity * Time.deltaTime;
            }

            TryStartLocomotionImmediately();

            if (LocomotionState != LocomotionState.Moving)
                return;

            // Note that calling Move even with Vector3.zero will have an effect by causing isGrounded to update
            m_IsMovingXROrigin = true;
            transformation.motion = motion;
            TryQueueTransformation(transformation);
        }

        void FindCharacterController()
        {
            var xrOrigin = Mediator.XROrigin?.Origin;
            if (xrOrigin == null)
                return;

            // Save a reference to the optional CharacterController on the rig GameObject
            // that will be used to move instead of modifying the Transform directly.
            if (m_CharacterController == null && !m_AttemptedGetCharacterController)
            {
                // Try on the Origin GameObject first, and then fallback to the XR Origin GameObject (if different)
                if (!xrOrigin.TryGetComponent(out m_CharacterController) && xrOrigin != Mediator.XROrigin.gameObject)
                    Mediator.XROrigin.TryGetComponent(out m_CharacterController);

                m_AttemptedGetCharacterController = true;
            }
        }
    }
}