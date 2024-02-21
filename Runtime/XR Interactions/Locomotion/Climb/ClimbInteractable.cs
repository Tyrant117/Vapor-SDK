using UnityEngine;
using Vapor.Utilities;
using VaporXR.Interaction;
using VaporXR.Interaction;
using VaporXR.Locomotion.Teleportation;

namespace VaporXR.Locomotion
{
    /// <summary>
    /// Interactable that can be climbed while selected.
    /// </summary>
    /// <seealso cref="ClimbProvider"/>
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class ClimbInteractable : InteractableModule
    {
        const float k_DefaultMaxInteractionDistance = 0.1f;

        [SerializeField]
        [Tooltip("The climb provider that performs locomotion while this interactable is selected. " +
                 "If no climb provider is configured, will attempt to find one.")]
        ClimbProvider m_ClimbProvider;

        /// <summary>
        /// The climb provider that performs locomotion while this interactable is selected.
        /// If no climb provider is configured, will attempt to find one.
        /// </summary>
        public ClimbProvider climbProvider
        {
            get => m_ClimbProvider;
            set => m_ClimbProvider = value;
        }

        [SerializeField]
        [Tooltip("Transform that defines the coordinate space for climb locomotion. " +
                 "Will use this GameObject's Transform by default.")]
        Transform m_ClimbTransform;

        /// <summary>
        /// Transform that defines the coordinate space for climb locomotion. Will use this GameObject's Transform by default.
        /// </summary>
        public Transform ClimbTransform
        {
            get
            {
                if (m_ClimbTransform == null)
                    m_ClimbTransform = transform;
                return m_ClimbTransform;
            }
            set => m_ClimbTransform = value;
        }

        [SerializeField]
        [Tooltip("Controls whether to apply a distance check when validating hover and select interaction.")]
        bool m_FilterInteractionByDistance = true;

        /// <summary>
        /// Controls whether to apply a distance check when validating hover and select interaction.
        /// </summary>
        /// <seealso cref="MaxInteractionDistance"/>
        /// <seealso cref="XRBaseInteractable.distanceCalculationMode"/>
        public bool FilterInteractionByDistance
        {
            get => m_FilterInteractionByDistance;
            set => m_FilterInteractionByDistance = value;
        }

        [SerializeField]
        [Tooltip("The maximum distance that an interactor can be from this interactable to begin hover or select.")]
        float m_MaxInteractionDistance = k_DefaultMaxInteractionDistance;

        /// <summary>
        /// The maximum distance that an interactor can be from this interactable to begin hover or select.
        /// Only applies when <see cref="FilterInteractionByDistance"/> is <see langword="true"/>.
        /// </summary>
        /// <seealso cref="FilterInteractionByDistance"/>
        /// <seealso cref="XRBaseInteractable.distanceCalculationMode"/>
        public float MaxInteractionDistance
        {
            get => m_MaxInteractionDistance;
            set => m_MaxInteractionDistance = value;
        }

        [SerializeField]
        [Tooltip("The teleport volume used to assist with movement to a specific destination after ending a climb " +
            "(optional, may be None). Only used if there is a Climb Teleport Interactor in the scene.")]
        TeleportationMultiAnchorVolume m_ClimbAssistanceTeleportVolume;

        /// <summary>
        /// The teleport volume used to assist with movement to a specific destination after ending a climb (optional,
        /// may be <see langword="null"/>). If there is a <see cref="ClimbTeleportInteractorModule"/> in the scene that
        /// references the same <see cref="ClimbProvider"/> as this interactable, it will interact with the volume while
        /// this interactable is being climbed.
        /// </summary>
        public TeleportationMultiAnchorVolume ClimbAssistanceTeleportVolume
        {
            get => m_ClimbAssistanceTeleportVolume;
            set => m_ClimbAssistanceTeleportVolume = value;
        }

        [SerializeField]
        [Tooltip("Optional override of locomotion settings specified in the climb provider. " +
                 "Only applies as an override if set to Use Value or if the asset reference is set.")]
        ClimbSettingsDatumProperty m_ClimbSettingsOverride;

        /// <summary>
        /// Optional override of climb locomotion settings specified in the climb provider. Only applies as
        /// an override if <see cref="Unity.XR.CoreUtils.Datums.DatumProperty{TValue, TDatum}.Value"/> is not <see langword="null"/>.
        /// </summary>
        public ClimbSettingsDatumProperty ClimbSettingsOverride
        {
            get => m_ClimbSettingsOverride;
            set => m_ClimbSettingsOverride = value;
        }

        protected virtual void OnValidate()
        {
            if (m_ClimbTransform == null)
            {
                m_ClimbTransform = transform;
            }
        }

        protected virtual void Reset()
        {
            Interactable.SelectMode = InteractableSelectMode.Multiple;
            m_ClimbTransform = transform;
        }

        protected override void Awake()
        {
            base.Awake();
            if (m_ClimbProvider == null)
            {
                ComponentLocatorUtility<ClimbProvider>.TryFindComponent(out m_ClimbProvider);
            }
        }

        protected virtual void OnEnable()
        {
            Interactable.SelectEntered += OnSelectEntered;
            Interactable.SelectExited += OnSelectExited;
        }

        protected virtual void OnDisable()
        {
            Interactable.SelectEntered -= OnSelectEntered;
            Interactable.SelectExited -= OnSelectExited;
        }

        public override bool IsHoverableBy(Interactor interactor)
        {
            return base.IsHoverableBy(interactor) && (!m_FilterInteractionByDistance ||
                Interactable.GetDistanceSqrToInteractor(interactor) <= m_MaxInteractionDistance * m_MaxInteractionDistance);
        }

        public override bool IsSelectableBy(Interactor interactor)
        {
            return base.IsSelectableBy(interactor) && (Interactable.IsSelectedBy(interactor) || !m_FilterInteractionByDistance ||
                Interactable.GetDistanceSqrToInteractor(interactor) <= m_MaxInteractionDistance * m_MaxInteractionDistance);
        }

        public void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (m_ClimbProvider != null || ComponentLocatorUtility<ClimbProvider>.TryFindComponent(out m_ClimbProvider))
            {
                m_ClimbProvider.StartClimbGrab(this, args.InteractorObject);
            }
        }

        public void OnSelectExited(SelectExitEventArgs args)
        {
            if (m_ClimbProvider != null)
            {
                m_ClimbProvider.FinishClimbGrab(args.InteractorObject);
            }
        }
    }
}