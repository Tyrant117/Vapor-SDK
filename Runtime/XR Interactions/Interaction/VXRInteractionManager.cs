using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Pool;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Utilities;
using Object = UnityEngine.Object;
using VaporXR.Interaction;
using VaporXR.Interaction;


#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER
using UnityEditor.Search;
using VaporEvents;
#endif
#if AR_FOUNDATION_PRESENT
using UnityEngine.XR.Interaction.Toolkit.AR;
#endif

namespace VaporXR
{
    /// <summary>
    /// The Interaction Manager acts as an intermediary between Interactors and Interactables.
    /// It is possible to have multiple Interaction Managers, each with their own valid set of Interactors and Interactables.
    /// Upon being enabled, both Interactors and Interactables register themselves with a valid Interaction Manager
    /// (if a specific one has not already been assigned in the inspector). The loaded scenes must have at least one Interaction Manager
    /// for Interactors and Interactables to be able to communicate.
    /// </summary>
    /// <remarks>
    /// Many of the methods on the Interactors and Interactables are designed to be called by this Interaction Manager
    /// rather than being called directly in order to maintain consistency between both targets of an interaction event.
    /// </remarks>
    /// <seealso cref="Interactor"/>
    /// <seealso cref="Interactable"/>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_InteractionManager)]
    // ReSharper disable once InconsistentNaming
    public class VXRInteractionManager : MonoBehaviour
    {
        /// <summary>
        /// Calls the methods in its invocation list when an <see cref="IXRInteractionGroup"/> is registered.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractionGroupRegisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="RegisterInteractionGroup(IXRInteractionGroup)"/>
        /// <seealso cref="IXRInteractionGroup.registered"/>
        public event Action<InteractionGroupRegisteredEventArgs> interactionGroupRegistered;

        /// <summary>
        /// Calls the methods in its invocation list when an <see cref="IXRInteractionGroup"/> is unregistered.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractionGroupUnregisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="UnregisterInteractionGroup(IXRInteractionGroup)"/>
        /// <seealso cref="IXRInteractionGroup.unregistered"/>
        public event Action<InteractionGroupUnregisteredEventArgs> interactionGroupUnregistered;

        /// <summary>
        /// Calls the methods in its invocation list when an <see cref="Interactor"/> is registered.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractorRegisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="RegisterInteractor(Interactor)"/>
        /// <seealso cref="Interactor.Registered"/>
        public event Action<InteractorRegisteredEventArgs> interactorRegistered;

        /// <summary>
        /// Calls the methods in its invocation list when an <see cref="Interactor"/> is unregistered.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractorUnregisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="UnregisterInteractor(Interactor)"/>
        /// <seealso cref="Interactor.Unregistered"/>
        public event Action<InteractorUnregisteredEventArgs> interactorUnregistered;

        /// <summary>
        /// Calls the methods in its invocation list when an <see cref="Interactable"/> is registered.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractableRegisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="RegisterInteractable(Interactable)"/>
        /// <seealso cref="Interactable.Registered"/>
        public event Action<InteractableRegisteredEventArgs> interactableRegistered;

        /// <summary>
        /// Calls the methods in its invocation list when an <see cref="Interactable"/> is unregistered.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractableUnregisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="UnregisterInteractable(Interactable)"/>
        /// <seealso cref="Interactable.Unregistered"/>
        public event Action<InteractableUnregisteredEventArgs> interactableUnregistered;


        /// <summary>
        /// Calls this method in its invocation list when an <see cref="IXRInteractionGroup"/> gains focus.
        /// </summary>
        public event Action<FocusEnterEventArgs> focusGained;

        /// <summary>
        /// Calls this method in its invocation list when an <see cref="IXRInteractionGroup"/> loses focus.
        /// </summary>
        public event Action<FocusExitEventArgs> focusLost;

        [SerializeField]
        [RequireInterface(typeof(IXRHoverFilter))]
        List<Object> m_StartingHoverFilters = new List<Object>();

        /// <summary>
        /// The hover filters that this object uses to automatically populate the <see cref="hoverFilters"/> List at
        /// startup (optional, may be empty).
        /// All objects in this list should implement the <see cref="IXRHoverFilter"/> interface.
        /// </summary>
        /// <remarks>
        /// To access and modify the hover filters used after startup, the <see cref="hoverFilters"/> List should
        /// be used instead.
        /// </remarks>
        /// <seealso cref="hoverFilters"/>
        public List<Object> startingHoverFilters
        {
            get => m_StartingHoverFilters;
            set => m_StartingHoverFilters = value;
        }

        readonly ExposedRegistrationList<IXRHoverFilter> m_HoverFilters = new ExposedRegistrationList<IXRHoverFilter> { BufferChanges = false };

        /// <summary>
        /// The list of global hover filters in this object.
        /// Used as additional hover validations for this manager.
        /// </summary>
        /// <remarks>
        /// While processing hover filters, all changes to this list don't have an immediate effect. These changes are
        /// buffered and applied when the processing is finished.
        /// Calling <see cref="IXRFilterList{T}.MoveTo"/> in this list will throw an exception when this list is being processed.
        /// </remarks>
        /// <seealso cref="ProcessHoverFilters"/>
        public IXRFilterList<IXRHoverFilter> hoverFilters => m_HoverFilters;

        [SerializeField]
        [RequireInterface(typeof(IXRSelectFilter))]
        List<Object> m_StartingSelectFilters = new List<Object>();

        /// <summary>
        /// The select filters that this object uses to automatically populate the <see cref="selectFilters"/> List at
        /// startup (optional, may be empty).
        /// All objects in this list should implement the <see cref="IXRSelectFilter"/> interface.
        /// </summary>
        /// <remarks>
        /// To access and modify the select filters used after startup, the <see cref="selectFilters"/> List should
        /// be used instead.
        /// </remarks>
        /// <seealso cref="selectFilters"/>
        public List<Object> startingSelectFilters
        {
            get => m_StartingSelectFilters;
            set => m_StartingSelectFilters = value;
        }

        readonly ExposedRegistrationList<IXRSelectFilter> m_SelectFilters = new ExposedRegistrationList<IXRSelectFilter> { BufferChanges = false };

        /// <summary>
        /// The list of global select filters in this object.
        /// Used as additional select validations for this manager.
        /// </summary>
        /// <remarks>
        /// While processing select filters, all changes to this list don't have an immediate effect. Theses changes are
        /// buffered and applied when the processing is finished.
        /// Calling <see cref="IXRFilterList{T}.MoveTo"/> in this list will throw an exception when this list is being processed.
        /// </remarks>
        /// <seealso cref="ProcessSelectFilters"/>
        public IXRFilterList<IXRSelectFilter> selectFilters => m_SelectFilters;

        /// <summary>
        /// (Read Only) The last <see cref="Interactable"/> that was focused by
        /// any <see cref="Interactor"/>.
        /// </summary>
        public Interactable lastFocused { get; protected set; }

        /// <summary>
        /// (Read Only) List of enabled Interaction Manager instances.
        /// </summary>
        /// <remarks>
        /// Intended to be used by XR Interaction Debugger.
        /// </remarks>
        internal static List<VXRInteractionManager> activeInteractionManagers { get; } = new();

        /// <summary>
        /// Map of all registered objects to test for colliding.
        /// </summary>
        readonly Dictionary<Collider, Interactable> m_ColliderToInteractableMap = new();

        /// <summary>
        /// Map of colliders and their associated <see cref="XRInteractableSnapVolume"/>.
        /// </summary>
        readonly Dictionary<Collider, VXRInteractableSnapVolume> m_ColliderToSnapVolumes = new();

        /// <summary>
        /// List of registered Interactors.
        /// </summary>
        readonly RegistrationList<Interactor> m_Interactors = new();

        /// <summary>
        /// List of registered Interaction Groups.
        /// </summary>
        readonly RegistrationList<IXRInteractionGroup> m_InteractionGroups = new();

        /// <summary>
        /// List of registered Interactables.
        /// </summary>
        readonly RegistrationList<Interactable> m_Interactables = new();

        /// <summary>
        /// Reusable list of Interactables for retrieving the current hovered Interactables of an Interactor.
        /// </summary>
        readonly List<Interactable> m_CurrentHovered = new();

        /// <summary>
        /// Reusable list of Interactables for retrieving the current selected Interactables of an Interactor.
        /// </summary>
        readonly List<Interactable> m_CurrentSelected = new();

        /// <summary>
        /// Map of Interactables that have the highest priority for selection in a frame.
        /// </summary>
        readonly Dictionary<Interactable, List<IXRTargetPriorityInteractor>> m_HighestPriorityTargetMap = new();

        /// <summary>
        /// Pool of Target Priority Interactor lists. Used by m_HighestPriorityTargetMap.
        /// </summary>
        static readonly LinkedPool<List<IXRTargetPriorityInteractor>> s_TargetPriorityInteractorListPool = new(() => new List<IXRTargetPriorityInteractor>(), actionOnRelease: list => list.Clear(), collectionCheck: false);

        /// <summary>
        /// Reusable list of valid targets for an Interactor.
        /// </summary>
        readonly List<Interactable> m_ValidTargets = new();

        /// <summary>
        /// Reusable set of valid targets for an Interactor.
        /// </summary>
        readonly HashSet<Interactable> m_UnorderedValidTargets = new();

        /// <summary>
        /// Set of all Interactors that are in an Interaction Group.
        /// </summary>
        readonly HashSet<Interactor> m_InteractorsInGroup = new();

        /// <summary>
        /// Set of all Interaction Groups that are in an Interaction Group.
        /// </summary>
        readonly HashSet<IXRInteractionGroup> m_GroupsInGroup = new();

        readonly List<IXRInteractionGroup> m_ScratchInteractionGroups = new();
        readonly List<Interactor> m_ScratchInteractors = new();

        // Reusable event args
        readonly LinkedPool<FocusEnterEventArgs> m_FocusEnterEventArgs = new(() => new FocusEnterEventArgs(), collectionCheck: false);
        readonly LinkedPool<FocusExitEventArgs> m_FocusExitEventArgs = new(() => new FocusExitEventArgs(), collectionCheck: false);
        readonly LinkedPool<SelectEnterEventArgs> m_SelectEnterEventArgs = new(() => new SelectEnterEventArgs(), collectionCheck: false);
        readonly LinkedPool<SelectExitEventArgs> m_SelectExitEventArgs = new(() => new SelectExitEventArgs(), collectionCheck: false);
        readonly LinkedPool<HoverEnterEventArgs> m_HoverEnterEventArgs = new(() => new HoverEnterEventArgs(), collectionCheck: false);
        readonly LinkedPool<HoverExitEventArgs> m_HoverExitEventArgs = new(() => new HoverExitEventArgs(), collectionCheck: false);
        readonly LinkedPool<InteractionGroupRegisteredEventArgs> m_InteractionGroupRegisteredEventArgs = new(() => new InteractionGroupRegisteredEventArgs(), collectionCheck: false);
        readonly LinkedPool<InteractionGroupUnregisteredEventArgs> m_InteractionGroupUnregisteredEventArgs = new(() => new InteractionGroupUnregisteredEventArgs(), collectionCheck: false);
        readonly LinkedPool<InteractorRegisteredEventArgs> m_InteractorRegisteredEventArgs = new(() => new InteractorRegisteredEventArgs(), collectionCheck: false);
        readonly LinkedPool<InteractorUnregisteredEventArgs> m_InteractorUnregisteredEventArgs = new(() => new InteractorUnregisteredEventArgs(), collectionCheck: false);
        readonly LinkedPool<InteractableRegisteredEventArgs> m_InteractableRegisteredEventArgs = new(() => new InteractableRegisteredEventArgs(), collectionCheck: false);
        readonly LinkedPool<InteractableUnregisteredEventArgs> m_InteractableUnregisteredEventArgs = new(() => new InteractableUnregisteredEventArgs(), collectionCheck: false);

        static readonly ProfilerMarker s_PreprocessInteractorsMarker = new ProfilerMarker("XRI.PreprocessInteractors");
        static readonly ProfilerMarker s_ProcessInteractionStrengthMarker = new ProfilerMarker("XRI.ProcessInteractionStrength");
        static readonly ProfilerMarker s_ProcessInteractorsMarker = new ProfilerMarker("XRI.ProcessInteractors");
        static readonly ProfilerMarker s_ProcessInteractablesMarker = new ProfilerMarker("XRI.ProcessInteractables");
        static readonly ProfilerMarker s_UpdateGroupMemberInteractionsMarker = new ProfilerMarker("XRI.UpdateGroupMemberInteractions");
        internal static readonly ProfilerMarker s_GetValidTargetsMarker = new ProfilerMarker("XRI.GetValidTargets");
        static readonly ProfilerMarker s_FilterRegisteredValidTargetsMarker = new ProfilerMarker("XRI.FilterRegisteredValidTargets");
        internal static readonly ProfilerMarker s_EvaluateInvalidFocusMarker = new ProfilerMarker("XRI.EvaluateInvalidFocus");
        internal static readonly ProfilerMarker s_EvaluateInvalidSelectionsMarker = new ProfilerMarker("XRI.EvaluateInvalidSelections");
        internal static readonly ProfilerMarker s_EvaluateInvalidHoversMarker = new ProfilerMarker("XRI.EvaluateInvalidHovers");
        internal static readonly ProfilerMarker s_EvaluateValidSelectionsMarker = new ProfilerMarker("XRI.EvaluateValidSelections");
        internal static readonly ProfilerMarker s_EvaluateValidHoversMarker = new ProfilerMarker("XRI.EvaluateValidHovers");
        static readonly ProfilerMarker s_FocusEnterMarker = new ProfilerMarker("XRI.FocusEnter");
        static readonly ProfilerMarker s_FocusExitMarker = new ProfilerMarker("XRI.FocusExit");
        static readonly ProfilerMarker s_SelectEnterMarker = new ProfilerMarker("XRI.SelectEnter");
        static readonly ProfilerMarker s_SelectExitMarker = new ProfilerMarker("XRI.SelectExit");
        static readonly ProfilerMarker s_HoverEnterMarker = new ProfilerMarker("XRI.HoverEnter");
        static readonly ProfilerMarker s_HoverExitMarker = new ProfilerMarker("XRI.HoverExit");

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void Awake()
        {
            // Setup the starting filters
            m_HoverFilters.RegisterReferences(m_StartingHoverFilters, this);
            m_SelectFilters.RegisterReferences(m_StartingSelectFilters, this);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (activeInteractionManagers.Count > 0)
            {
                var message = "There are multiple active and enabled XR Interaction Manager components in the loaded scenes." +
                    " This is supported, but may not be intended since interactors and interactables are not able to interact with those registered to a different manager." +
                    " You can use the <b>Window</b> > <b>Analysis</b> > <b>XR Interaction Debugger</b> window to verify the interactors and interactables registered with each.";
#if UNITY_EDITOR
                    message += " The default manager that interactors and interactables automatically register with when None is: " +
                               GetHierarchyPath(ComponentLocatorUtility<VXRInteractionManager>.FindOrCreateComponent().gameObject);
#endif

                Debug.LogWarning(message, this);
            }

            activeInteractionManagers.Add(this);
            Application.onBeforeRender += OnBeforeRender;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
            activeInteractionManagers.Remove(this);
            ClearPriorityForSelectionMap();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        // ReSharper disable PossiblyImpureMethodCallOnReadonlyVariable -- ProfilerMarker.Begin with context object does not have Pure attribute
        protected virtual void Update()
        {
            ClearPriorityForSelectionMap();
            FlushRegistration();

            using (s_PreprocessInteractorsMarker.Auto())
                PreprocessInteractors(XRInteractionUpdateOrder.UpdatePhase.Dynamic);

            foreach (var interactionGroup in m_InteractionGroups.RegisteredSnapshot)
            {
                if (!m_InteractionGroups.IsStillRegistered(interactionGroup) || m_GroupsInGroup.Contains(interactionGroup))
                    continue;

                using (s_EvaluateInvalidFocusMarker.Auto())
                    ClearInteractionGroupFocus(interactionGroup);

                using (s_UpdateGroupMemberInteractionsMarker.Auto())
                    interactionGroup.UpdateGroupMemberInteractions();
            }

            foreach (var interactor in m_Interactors.RegisteredSnapshot)
            {
                if (!m_Interactors.IsStillRegistered(interactor) || m_InteractorsInGroup.Contains(interactor))
                    continue;

                using (s_GetValidTargetsMarker.Auto())
                    GetValidTargets(interactor, m_ValidTargets);

                using (s_EvaluateInvalidSelectionsMarker.Auto())
                    ClearInteractorSelection(interactor, m_ValidTargets);

                using (s_EvaluateInvalidHoversMarker.Auto())
                    ClearInteractorHover(interactor, m_ValidTargets);

                using (s_EvaluateValidSelectionsMarker.Auto())
                    InteractorSelectValidTargets(interactor, m_ValidTargets);

                using (s_EvaluateValidHoversMarker.Auto())
                    InteractorHoverValidTargets(interactor, m_ValidTargets);
            }

            using (s_ProcessInteractionStrengthMarker.Auto())
                ProcessInteractionStrength(XRInteractionUpdateOrder.UpdatePhase.Dynamic);

            using (s_ProcessInteractorsMarker.Auto())
                ProcessInteractors(XRInteractionUpdateOrder.UpdatePhase.Dynamic);
            using (s_ProcessInteractablesMarker.Auto())
                ProcessInteractables(XRInteractionUpdateOrder.UpdatePhase.Dynamic);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void LateUpdate()
        {
            FlushRegistration();

            using (s_ProcessInteractorsMarker.Auto())
                ProcessInteractors(XRInteractionUpdateOrder.UpdatePhase.Late);
            using (s_ProcessInteractablesMarker.Auto())
                ProcessInteractables(XRInteractionUpdateOrder.UpdatePhase.Late);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void FixedUpdate()
        {
            FlushRegistration();

            using (s_ProcessInteractorsMarker.Auto())
                ProcessInteractors(XRInteractionUpdateOrder.UpdatePhase.Fixed);
            using (s_ProcessInteractablesMarker.Auto())
                ProcessInteractables(XRInteractionUpdateOrder.UpdatePhase.Fixed);
        }

        /// <summary>
        /// Delegate method used to register for "Just Before Render" input updates for VR devices.
        /// </summary>
        /// <seealso cref="Application"/>
        [BeforeRenderOrder(XRInteractionUpdateOrder.k_BeforeRenderOrder)]
        protected virtual void OnBeforeRender()
        {
            FlushRegistration();

            using (s_ProcessInteractorsMarker.Auto())
                ProcessInteractors(XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender);
            using (s_ProcessInteractablesMarker.Auto())
                ProcessInteractables(XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender);
        }
        // ReSharper restore PossiblyImpureMethodCallOnReadonlyVariable

        /// <summary>
        /// Automatically called each frame to preprocess all interactors registered with this manager.
        /// </summary>
        /// <param name="updatePhase">The update phase.</param>
        /// <remarks>
        /// Please see the <see cref="XRInteractionUpdateOrder.UpdatePhase"/> documentation for more details on update order.
        /// </remarks>
        /// <seealso cref="Interactor.PreprocessInteractor"/>
        /// <seealso cref="XRInteractionUpdateOrder.UpdatePhase"/>
        protected virtual void PreprocessInteractors(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            foreach (var interactionGroup in m_InteractionGroups.RegisteredSnapshot)
            {
                if (!m_InteractionGroups.IsStillRegistered(interactionGroup) || m_GroupsInGroup.Contains(interactionGroup))
                    continue;

                interactionGroup.PreprocessGroupMembers(updatePhase);
            }

            foreach (var interactor in m_Interactors.RegisteredSnapshot)
            {
                if (!m_Interactors.IsStillRegistered(interactor) || m_InteractorsInGroup.Contains(interactor))
                    continue;

                interactor.PreprocessInteractor(updatePhase);
            }
        }

        /// <summary>
        /// Automatically called each frame to process all interactors registered with this manager.
        /// </summary>
        /// <param name="updatePhase">The update phase.</param>
        /// <remarks>
        /// Please see the <see cref="XRInteractionUpdateOrder.UpdatePhase"/> documentation for more details on update order.
        /// </remarks>
        /// <seealso cref="Interactor.PreprocessInteractor"/>
        /// <seealso cref="XRInteractionUpdateOrder.UpdatePhase"/>
        protected virtual void ProcessInteractors(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            foreach (var interactionGroup in m_InteractionGroups.RegisteredSnapshot)
            {
                if (!m_InteractionGroups.IsStillRegistered(interactionGroup) || m_GroupsInGroup.Contains(interactionGroup))
                    continue;

                interactionGroup.ProcessGroupMembers(updatePhase);
            }

            foreach (var interactor in m_Interactors.RegisteredSnapshot)
            {
                if (!m_Interactors.IsStillRegistered(interactor) || m_InteractorsInGroup.Contains(interactor))
                    continue;

                interactor.ProcessInteractor(updatePhase);
            }
        }

        /// <summary>
        /// Automatically called each frame to process all interactables registered with this manager.
        /// </summary>
        /// <param name="updatePhase">The update phase.</param>
        /// <remarks>
        /// Please see the <see cref="XRInteractionUpdateOrder.UpdatePhase"/> documentation for more details on update order.
        /// </remarks>
        /// <seealso cref="Interactable.ProcessInteractable"/>
        /// <seealso cref="XRInteractionUpdateOrder.UpdatePhase"/>
        protected virtual void ProcessInteractables(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            foreach (var interactable in m_Interactables.RegisteredSnapshot)
            {
                if (!m_Interactables.IsStillRegistered(interactable))
                    continue;

                interactable.ProcessInteractable(updatePhase);
            }
        }

        /// <summary>
        /// Automatically called each frame to process interaction strength of interactables and interactors registered with this manager.
        /// </summary>
        /// <param name="updatePhase">The update phase.</param>
        /// <seealso cref="IXRInteractionStrengthInteractable.ProcessInteractionStrength"/>
        /// <seealso cref="IXRInteractionStrengthInteractor.ProcessInteractionStrength"/>
        /// <seealso cref="XRInteractionUpdateOrder.UpdatePhase"/>
        protected virtual void ProcessInteractionStrength(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            // Unlike other processing, with interaction strength, interactables are processed before interactors
            // since interactables with the ability to be poked dictate the interaction strength. After the
            // interaction strength of interactables are computed for this frame, they are gathered into
            // the interactor for use in affordances or within the process step.
            foreach (var interactable in m_Interactables.RegisteredSnapshot)
            {
                if (!m_Interactables.IsStillRegistered(interactable))
                    continue;

                interactable.ProcessInteractionStrength(updatePhase);
            }

            foreach (var interactor in m_Interactors.RegisteredSnapshot)
            {
                if (!m_Interactors.IsStillRegistered(interactor))
                    continue;

                if (interactor is IXRInteractionStrengthInteractor interactionStrengthInteractor)
                    interactionStrengthInteractor.ProcessInteractionStrength(updatePhase);
            }
        }

        /// <summary>
        /// Whether the given Interactor can hover the given Interactable.
        /// You can extend this method to add global hover validations by code.
        /// </summary>
        /// <param name="interactor">The Interactor to check.</param>
        /// <param name="interactable">The Interactable to check.</param>
        /// <returns>Returns whether the given Interactor can hover the given Interactable.</returns>
        /// <remarks>
        /// You can also extend the global hover validations without needing to create a derived class by adding hover
        /// filters to this object (see <see cref="startingHoverFilters"/> and <see cref="hoverFilters"/>).
        /// </remarks>
        /// <seealso cref="IsHoverPossible"/>
        public virtual bool CanHover(Interactor interactor, Interactable interactable)
        {
            return interactor.IsHoverActive && IsHoverPossible(interactor, interactable);
        }

        /// <summary>
        /// Whether the given Interactor would be able to hover the given Interactable if the Interactor were in a state where it could hover.
        /// </summary>
        /// <param name="interactor">The Interactor to check.</param>
        /// <param name="interactable">The Interactable to check.</param>
        /// <returns>Returns whether the given Interactor would be able to hover the given Interactable if the Interactor were in a state where it could hover.</returns>
        /// <seealso cref="CanHover"/>
        public bool IsHoverPossible(Interactor interactor, Interactable interactable)
        {
            return /*HasInteractionLayerOverlap(interactor, interactable) && */ProcessHoverFilters(interactor, interactable) &&
                interactor.CanHover(interactable) && interactable.IsHoverableBy(interactor);
        }

        /// <summary>
        /// Whether the given Interactor can select the given Interactable.
        /// You can extend this method to add global select validations by code.
        /// </summary>
        /// <param name="interactor">The Interactor to check.</param>
        /// <param name="interactable">The Interactable to check.</param>
        /// <returns>Returns whether the given Interactor can select the given Interactable.</returns>
        /// <remarks>
        /// You can also extend the global select validations without needing to create a derived class by adding select
        /// filters to this object (see <see cref="startingSelectFilters"/> and <see cref="selectFilters"/>).
        /// </remarks>
        /// <seealso cref="IsSelectPossible"/>
        public virtual bool CanSelect(Interactor interactor, Interactable interactable)
        {
            return interactor.IsSelectActive && IsSelectPossible(interactor, interactable);
        }

        /// <summary>
        /// Whether the given Interactor would be able to select the given Interactable if the Interactor were in a state where it could select.
        /// </summary>
        /// <param name="interactor">The Interactor to check.</param>
        /// <param name="interactable">The Interactable to check.</param>
        /// <returns>Returns whether the given Interactor would be able to select the given Interactable if the Interactor were in a state where it could select.</returns>
        /// <seealso cref="CanSelect"/>
        public bool IsSelectPossible(Interactor interactor, Interactable interactable)
        {
            return /*HasInteractionLayerOverlap(interactor, interactable) && */ProcessSelectFilters(interactor, interactable) &&
                interactor.CanSelect(interactable) && interactable.IsSelectableBy(interactor);
        }

        /// <summary>
        /// Whether the given Interactor can gain focus of the given Interactable.
        /// You can extend this method to add global focus validations by code.
        /// </summary>
        /// <param name="interactor">The Interactor to check.</param>
        /// <param name="interactable">The Interactable to check.</param>
        /// <returns>Returns whether the given Interactor can gain focus of the given Interactable.</returns>
        /// <seealso cref="IsFocusPossible"/>
        public virtual bool CanFocus(Interactor interactor, Interactable interactable)
        {
            return IsFocusPossible(interactor, interactable);
        }

        /// <summary>
        /// Whether the given Interactor would be able gain focus of the given Interactable if the Interactor were in a state where it could focus.
        /// </summary>
        /// <param name="interactor">The Interactor to check.</param>
        /// <param name="interactable">The Interactable to check.</param>
        /// <returns>Returns whether the given Interactor would be able to gain focus of the given Interactable if the Interactor were in a state where it could focus.</returns>
        /// <seealso cref="CanSelect"/>
        public bool IsFocusPossible(Interactor interactor, Interactable interactable)
        {
            return interactable.CanBeFocused /*&& HasInteractionLayerOverlap(interactor, interactable)*/;
        }

        /// <summary>
        /// Registers a new Interaction Group to be processed.
        /// </summary>
        /// <param name="interactionGroup">The Interaction Group to be registered.</param>
        public virtual void RegisterInteractionGroup(IXRInteractionGroup interactionGroup)
        {
            IXRInteractionGroup containingGroup = null;
            if (interactionGroup is IXRGroupMember groupMember)
                containingGroup = groupMember.ContainingGroup;

            if (containingGroup != null && !IsRegistered(containingGroup))
            {
                Debug.LogError($"Cannot register {interactionGroup} with Interaction Manager before its containing " +
                               "Interaction Group is registered.", this);
                return;
            }

            if (m_InteractionGroups.Register(interactionGroup))
            {
                if (containingGroup != null)
                    m_GroupsInGroup.Add(interactionGroup);

                using (m_InteractionGroupRegisteredEventArgs.Get(out var args))
                {
                    args.Manager = this;
                    args.InteractionGroupObject = interactionGroup;
                    args.ContainingGroupObject = containingGroup;
                    OnRegistered(args);
                }
            }
        }

        /// <summary>
        /// Automatically called when an Interaction Group is registered with this Interaction Manager.
        /// Notifies the Interaction Group, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="args">Event data containing the registered Interaction Group.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="RegisterInteractionGroup(IXRInteractionGroup)"/>
        protected virtual void OnRegistered(InteractionGroupRegisteredEventArgs args)
        {
            Debug.Assert(args.Manager == this, this);

            args.InteractionGroupObject.OnRegistered(args);
            interactionGroupRegistered?.Invoke(args);
        }

        /// <summary>
        /// Unregister an Interaction Group so it is no longer processed.
        /// </summary>
        /// <param name="interactionGroup">The Interaction Group to be unregistered.</param>
        public virtual void UnregisterInteractionGroup(IXRInteractionGroup interactionGroup)
        {
            if (!IsRegistered(interactionGroup))
                return;

            interactionGroup.OnBeforeUnregistered();

            // Make sure no registered interactors or groups still reference this group
            if (m_InteractionGroups.FlushedCount > 0)
            {
                m_InteractionGroups.GetRegisteredItems(m_ScratchInteractionGroups);
                foreach (var group in m_ScratchInteractionGroups)
                {
                    if (group is IXRGroupMember groupMember && groupMember.ContainingGroup == interactionGroup)
                    {
                        Debug.LogError($"Cannot unregister {interactionGroup} with Interaction Manager before its " +
                            "Group Members have been re-registered as not part of the Group.", this);
                        return;
                    }
                }
            }

            if (m_Interactors.FlushedCount > 0)
            {
                m_Interactors.GetRegisteredItems(m_ScratchInteractors);
                foreach (var interactor in m_ScratchInteractors)
                {
                    if (interactor is IXRGroupMember groupMember && groupMember.ContainingGroup == interactionGroup)
                    {
                        Debug.LogError($"Cannot unregister {interactionGroup} with Interaction Manager before its " +
                            "Group Members have been re-registered as not part of the Group.", this);
                        return;
                    }
                }
            }

            if (m_InteractionGroups.Unregister(interactionGroup))
            {
                m_GroupsInGroup.Remove(interactionGroup);
                using (m_InteractionGroupUnregisteredEventArgs.Get(out var args))
                {
                    args.Manager = this;
                    args.InteractionGroupObject = interactionGroup;
                    OnUnregistered(args);
                }
            }
        }

        /// <summary>
        /// Automatically called when an Interaction Group is unregistered from this Interaction Manager.
        /// Notifies the Interaction Group, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="args">Event data containing the unregistered Interaction Group.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="UnregisterInteractionGroup(IXRInteractionGroup)"/>
        protected virtual void OnUnregistered(InteractionGroupUnregisteredEventArgs args)
        {
            Debug.Assert(args.Manager == this, this);

            args.InteractionGroupObject.OnUnregistered(args);
            interactionGroupUnregistered?.Invoke(args);
        }

        /// <summary>
        /// Gets all currently registered Interaction groups
        /// </summary>
        /// <param name="interactionGroups">The list that will filled with all of the registered interaction groups</param>
        public void GetInteractionGroups(List<IXRInteractionGroup> interactionGroups)
        {
            m_InteractionGroups.GetRegisteredItems(interactionGroups);
        }

        /// <summary>
        /// Gets the registered Interaction Group with the given name.
        /// </summary>
        /// <param name="groupName">The name of the interaction group to retrieve.</param>
        /// <returns>Returns the interaction group with matching name, or null if none were found.</returns>
        /// <seealso cref="IXRInteractionGroup.GroupName"/>
        public IXRInteractionGroup GetInteractionGroup(string groupName)
        {
            foreach (var interactionGroup in m_InteractionGroups.RegisteredSnapshot)
            {
                if (interactionGroup.GroupName == groupName)
                    return interactionGroup;
            }

            return null;
        }

        /// <summary>
        /// Registers a new Interactor to be processed.
        /// </summary>
        /// <param name="interactor">The Interactor to be registered.</param>
        public virtual void RegisterInteractor(Interactor interactor)
        {
            IXRInteractionGroup containingGroup = null;
            if (interactor is IXRGroupMember groupMember)
                containingGroup = groupMember.ContainingGroup;

            if (containingGroup != null && !IsRegistered(containingGroup))
            {
                Debug.LogError($"Cannot register {interactor} with Interaction Manager before its containing " +
                               "Interaction Group is registered.", this);
                return;
            }

            if (m_Interactors.Register(interactor))
            {
                if (containingGroup != null)
                {
                    m_InteractorsInGroup.Add(interactor);
                }

                using (m_InteractorRegisteredEventArgs.Get(out var args))
                {
                    args.Manager = this;
                    args.InteractorObject = interactor;
                    args.ContainingGroupObject = containingGroup;
                    OnRegistered(args);
                }
            }
        }

        /// <summary>
        /// Automatically called when an Interactor is registered with this Interaction Manager.
        /// Notifies the Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="args">Event data containing the registered Interactor.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="RegisterInteractor(Interactor)"/>
        protected virtual void OnRegistered(InteractorRegisteredEventArgs args)
        {
            Debug.Assert(args.Manager == this, this);

            args.InteractorObject.OnRegistered(args);
            interactorRegistered?.Invoke(args);
        }

        /// <summary>
        /// Unregister an Interactor so it is no longer processed.
        /// </summary>
        /// <param name="interactor">The Interactor to be unregistered.</param>
        public virtual void UnregisterInteractor(Interactor interactor)
        {
            if (!IsRegistered(interactor))
                return;

            var interactorTransform = interactor.transform;

            // We suppress canceling focus for inactive interactors vs. destroyed interactors as that is used as a method of mediation
            if (interactorTransform == null || interactorTransform.gameObject.activeSelf)
            {
                CancelInteractorFocus(interactor);
            }

            if (interactor is Interactor selectInteractor)
            {
                CancelInteractorSelection(selectInteractor);
            }

            if (interactor is Interactor hoverInteractor)
            {
                CancelInteractorHover(hoverInteractor);
            }

            if (m_Interactors.Unregister(interactor))
            {
                m_InteractorsInGroup.Remove(interactor);
                using (m_InteractorUnregisteredEventArgs.Get(out var args))
                {
                    args.Manager = this;
                    args.InteractorObject = interactor;
                    OnUnregistered(args);
                }
            }
        }

        /// <summary>
        /// Automatically called when an Interactor is unregistered from this Interaction Manager.
        /// Notifies the Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="args">Event data containing the unregistered Interactor.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="UnregisterInteractor(Interactor)"/>
        protected virtual void OnUnregistered(InteractorUnregisteredEventArgs args)
        {
            Debug.Assert(args.Manager == this, this);

            args.InteractorObject.OnUnregistered(args);
            interactorUnregistered?.Invoke(args);
        }

        /// <summary>
        /// Registers a new Interactable to be processed.
        /// </summary>
        /// <param name="interactable">The Interactable to be registered.</param>
        public virtual void RegisterInteractable(Interactable interactable)
        {
            if (m_Interactables.Register(interactable))
            {
                foreach (var interactableCollider in interactable.Colliders)
                {
                    if (interactableCollider == null)
                        continue;

                    // Add the association for a fast lookup which maps from Collider to Interactable.
                    // Warn if the same Collider is already used by another registered Interactable
                    // since the lookup will only return the earliest registered rather than a list of all.
                    // The warning is suppressed in the case of gesture interactables since it's common
                    // to compose multiple on the same GameObject.
                    if (!m_ColliderToInteractableMap.TryGetValue(interactableCollider, out var associatedInteractable))
                    {
                        m_ColliderToInteractableMap.Add(interactableCollider, interactable);
                    }
#if AR_FOUNDATION_PRESENT 
#pragma warning disable 618
                    else if (!(interactable is ARBaseGestureInteractable && associatedInteractable is ARBaseGestureInteractable))
#pragma warning restore 618
#else
                    else
#endif
                    {
                        Debug.LogWarning("A collider used by an Interactable object is already registered with another Interactable object." +
                            $" The {interactableCollider} will remain associated with {associatedInteractable}, which was registered before {interactable}." +
                            " The value returned by XRInteractionManager.TryGetInteractableForCollider will be the first association.",
                            interactable as Object);
                    }
                }

                using (m_InteractableRegisteredEventArgs.Get(out var args))
                {
                    args.Manager = this;
                    args.InteractableObject = interactable;
                    OnRegistered(args);
                }
            }
        }

        /// <summary>
        /// Automatically called when an Interactable is registered with this Interaction Manager.
        /// Notifies the Interactable, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="args">Event data containing the registered Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="RegisterInteractable(Interactable)"/>
        protected virtual void OnRegistered(InteractableRegisteredEventArgs args)
        {
            Debug.Assert(args.Manager == this, this);

            args.InteractableObject.OnRegistered(args);
            interactableRegistered?.Invoke(args);
        }

        /// <summary>
        /// Unregister an Interactable so it is no longer processed.
        /// </summary>
        /// <param name="interactable">The Interactable to be unregistered.</param>
        public virtual void UnregisterInteractable(Interactable interactable)
        {
            if (!IsRegistered(interactable))
                return;

            if (interactable is Interactable focusable)
                CancelInteractableFocus(focusable);

            if (interactable is Interactable selectable)
                CancelInteractableSelection(selectable);

            if (interactable is Interactable hoverable)
                CancelInteractableHover(hoverable);

            if (m_Interactables.Unregister(interactable))
            {
                // This makes the assumption that the list of Colliders has not been changed after
                // the Interactable is registered. If any were removed afterward, those would remain
                // in the dictionary.
                foreach (var interactableCollider in interactable.Colliders)
                {
                    if (interactableCollider == null)
                        continue;

                    if (m_ColliderToInteractableMap.TryGetValue(interactableCollider, out var associatedInteractable) && associatedInteractable == interactable)
                        m_ColliderToInteractableMap.Remove(interactableCollider);
                }

                using (m_InteractableUnregisteredEventArgs.Get(out var args))
                {
                    args.Manager = this;
                    args.InteractableObject = interactable;
                    OnUnregistered(args);
                }
            }
        }

        /// <summary>
        /// Automatically called when an Interactable is unregistered from this Interaction Manager.
        /// Notifies the Interactable, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="args">Event data containing the unregistered Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="UnregisterInteractable(Interactable)"/>
        protected virtual void OnUnregistered(InteractableUnregisteredEventArgs args)
        {
            Debug.Assert(args.Manager == this, this);

            args.InteractableObject.OnUnregistered(args);
            interactableUnregistered?.Invoke(args);
        }

        /// <summary>
        /// Registers a new snap volume to associate the snap collider and interactable.
        /// </summary>
        /// <param name="snapVolume">The snap volume to be registered.</param>
        /// <seealso cref="UnregisterSnapVolume"/>
        public void RegisterSnapVolume(VXRInteractableSnapVolume snapVolume)
        {
            if (snapVolume == null)
                return;

            var snapCollider = snapVolume.snapCollider;
            if (snapCollider == null)
                return;

            if (!m_ColliderToSnapVolumes.TryGetValue(snapCollider, out var associatedSnapVolume))
            {
                m_ColliderToSnapVolumes.Add(snapCollider, snapVolume);
            }
            else
            {
                Debug.LogWarning("A collider used by a snap volume component is already registered with another snap volume component." +
                    $" The {snapCollider} will remain associated with {associatedSnapVolume}, which was registered before {snapVolume}." +
                    " The value returned by XRInteractionManager.TryGetInteractableForCollider will be the first association.",
                    snapVolume);
            }
        }

        /// <summary>
        /// Unregister the snap volume so it is no longer associated with the snap collider or interactable.
        /// </summary>
        /// <param name="snapVolume">The snap volume to be unregistered.</param>
        /// <seealso cref="RegisterSnapVolume"/>
        public void UnregisterSnapVolume(VXRInteractableSnapVolume snapVolume)
        {
            if (snapVolume == null)
                return;

            // This makes the assumption that the snap collider has not been changed after
            // the snap volume is registered.
            var snapCollider = snapVolume.snapCollider;
            if (snapCollider == null)
                return;

            if (m_ColliderToSnapVolumes.TryGetValue(snapCollider, out var associatedSnapVolume) && associatedSnapVolume == snapVolume)
                m_ColliderToSnapVolumes.Remove(snapCollider);
        }

        /// <summary>
        /// Returns all registered Interaction Groups into List <paramref name="results"/>.
        /// </summary>
        /// <param name="results">List to receive registered Interaction Groups.</param>
        /// <remarks>
        /// This method populates the list with the registered Interaction Groups at the time the
        /// method is called. It is not a live view, meaning Interaction Groups
        /// registered or unregistered afterward will not be reflected in the
        /// results of this method.
        /// Clears <paramref name="results"/> before adding to it.
        /// </remarks>
        public void GetRegisteredInteractionGroups(List<IXRInteractionGroup> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            m_InteractionGroups.GetRegisteredItems(results);
        }

        /// <summary>
        /// Returns all registered Interactors into List <paramref name="results"/>.
        /// </summary>
        /// <param name="results">List to receive registered Interactors.</param>
        /// <remarks>
        /// This method populates the list with the registered Interactors at the time the
        /// method is called. It is not a live view, meaning Interactors
        /// registered or unregistered afterward will not be reflected in the
        /// results of this method.
        /// Clears <paramref name="results"/> before adding to it.
        /// </remarks>
        /// <seealso cref="GetRegisteredInteractables(List{Interactable})"/>
        public void GetRegisteredInteractors(List<Interactor> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            m_Interactors.GetRegisteredItems(results);
        }

        /// <summary>
        /// Returns all registered Interactables into List <paramref name="results"/>.
        /// </summary>
        /// <param name="results">List to receive registered Interactables.</param>
        /// <remarks>
        /// This method populates the list with the registered Interactables at the time the
        /// method is called. It is not a live view, meaning Interactables
        /// registered or unregistered afterward will not be reflected in the
        /// results of this method.
        /// Clears <paramref name="results"/> before adding to it.
        /// </remarks>
        /// <seealso cref="GetRegisteredInteractors(List{Interactor})"/>
        public void GetRegisteredInteractables(List<Interactable> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            m_Interactables.GetRegisteredItems(results);
        }

        /// <summary>
        /// Checks whether the <paramref name="interactionGroup"/> is registered with this Interaction Manager.
        /// </summary>
        /// <param name="interactionGroup">The Interaction Group to check.</param>
        /// <returns>Returns <see langword="true"/> if registered. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="RegisterInteractionGroup(IXRInteractionGroup)"/>
        public bool IsRegistered(IXRInteractionGroup interactionGroup)
        {
            return m_InteractionGroups.IsRegistered(interactionGroup);
        }

        /// <summary>
        /// Checks whether the <paramref name="interactor"/> is registered with this Interaction Manager.
        /// </summary>
        /// <param name="interactor">The Interactor to check.</param>
        /// <returns>Returns <see langword="true"/> if registered. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="RegisterInteractor(Interactor)"/>
        public bool IsRegistered(Interactor interactor)
        {
            return m_Interactors.IsRegistered(interactor);
        }

        /// <summary>
        /// Checks whether the <paramref name="interactable"/> is registered with this Interaction Manager.
        /// </summary>
        /// <param name="interactable">The Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if registered. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="RegisterInteractable(Interactable)"/>
        public bool IsRegistered(Interactable interactable)
        {
            return m_Interactables.IsRegistered(interactable);
        }

        /// <summary>
        /// Gets the Interactable a specific <see cref="Collider"/> is attached to.
        /// </summary>
        /// <param name="interactableCollider">The collider of the Interactable to retrieve.</param>
        /// <param name="interactable">The returned Interactable associated with the collider.</param>
        /// <returns>Returns <see langword="true"/> if an Interactable was associated with the collider. Otherwise, returns <see langword="false"/>.</returns>
        public bool TryGetInteractableForCollider(Collider interactableCollider, out Interactable interactable)
        {
            interactable = null;
            if (interactableCollider == null)
                return false;

            // Try direct association, and then fallback to snap volume association
            var hasDirectAssociation = m_ColliderToInteractableMap.TryGetValue(interactableCollider, out interactable);
            if (!hasDirectAssociation)
            {
                if (m_ColliderToSnapVolumes.TryGetValue(interactableCollider, out var snapVolume) && snapVolume != null)
                    interactable = snapVolume.interactable;
            }

            return interactable != null && (!(interactable is Object unityObject) || unityObject != null);
        }

        /// <summary>
        /// Gets the Interactable a specific <see cref="Collider"/> is attached to.
        /// </summary>
        /// <param name="interactableCollider">The collider of the Interactable to retrieve.</param>
        /// <param name="interactable">The returned Interactable associated with the collider.</param>
        /// <param name="snapVolume">The returned snap volume associated with the collider.</param>
        /// <returns>Returns <see langword="true"/> if an Interactable was associated with the collider. Otherwise, returns <see langword="false"/>.</returns>
        public bool TryGetInteractableForCollider(Collider interactableCollider, out Interactable interactable, out VXRInteractableSnapVolume snapVolume)
        {
            interactable = null;
            snapVolume = null;
            if (interactableCollider == null)
                return false;

            // Populate both out params
            var hasDirectAssociation = m_ColliderToInteractableMap.TryGetValue(interactableCollider, out interactable);
            if (m_ColliderToSnapVolumes.TryGetValue(interactableCollider, out snapVolume) && snapVolume != null)
            {
                if (hasDirectAssociation)
                {
                    // Detect mismatch, ignore the snap volume
                    if (snapVolume.interactable != interactable)
                        snapVolume = null;
                }
                else
                {
                    interactable = snapVolume.interactable;
                }
            }

            return interactable != null && (!(interactable is Object unityObject) || unityObject != null);
        }

        /// <summary>
        /// Checks if a given collider is paired with an interactable registered with the interaction mananger, or if it is paired to a snap volume.
        /// </summary>
        /// <param name="colliderToCheck">Collider to lookup</param>
        /// <returns>True if collider is paired to either an interactable directly, or indirectly via a snap volume.</returns>
        public bool IsColliderRegisteredToInteractable(in Collider colliderToCheck)
        {
            return m_ColliderToInteractableMap.ContainsKey(colliderToCheck) || m_ColliderToSnapVolumes.ContainsKey(colliderToCheck);
        }

        /// <summary>
        /// Checks if the given <paramref name="potentialSnapVolumeCollider"/> is registered with a snap volume.
        /// </summary>
        /// <param name="potentialSnapVolumeCollider">Collider to check.</param>
        /// <returns>True if the collider is paired to a snap volume.</returns>
        public bool IsColliderRegisteredSnapVolume(in Collider potentialSnapVolumeCollider)
        {
            return m_ColliderToSnapVolumes.ContainsKey(potentialSnapVolumeCollider);
        }

        /// <summary>
        /// Gets whether the given Interactable is the highest priority candidate for selection in this frame, useful for
        /// custom feedback.
        /// Only <see cref="IXRTargetPriorityInteractor"/>s that are configured to monitor Targets will be considered.
        /// </summary>
        /// <param name="target">The Interactable to check if it's the highest priority candidate for selection.</param>
        /// <param name="interactors">(Optional) Returns the list of Interactors where the given Interactable has the highest priority for selection.</param>
        /// <returns>Returns <see langword="true"/> if the given Interactable is the highest priority candidate for selection. Otherwise, returns <see langword="false"/>.</returns>
        /// <remarks>
        /// Clears <paramref name="interactors"/> before adding to it.
        /// </remarks>
        public bool IsHighestPriorityTarget(Interactable target, List<IXRTargetPriorityInteractor> interactors = null)
        {
            if (!m_HighestPriorityTargetMap.TryGetValue(target, out var targetPriorityInteractors))
                return false;

            if (interactors == null)
                return true;

            interactors.Clear();
            interactors.AddRange(targetPriorityInteractors);
            return true;
        }

        /// <summary>
        /// Retrieves the list of Interactables that the given Interactor could possibly interact with this frame.
        /// This list is sorted by priority (with highest priority first), and will only contain Interactables
        /// that are registered with this Interaction Manager.
        /// </summary>
        /// <param name="interactor">The Interactor to get valid targets for.</param>
        /// <param name="targets">The results list to populate with Interactables that are valid for selection, hover, or focus.</param>
        /// <remarks>
        /// Unity expects the <paramref name="interactor"/>'s implementation of <see cref="Interactor.GetValidTargets"/> to clear <paramref name="targets"/> before adding to it.
        /// </remarks>
        /// <seealso cref="Interactor.GetValidTargets"/>
        public void GetValidTargets(Interactor interactor, List<Interactable> targets)
        {
            targets.Clear();
            interactor.GetValidTargets(targets);

            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable -- ProfilerMarker.Begin with context object does not have Pure attribute
            using (s_FilterRegisteredValidTargetsMarker.Auto())
                RemoveAllUnregistered(this, targets);
        }

        /// <summary>
        /// Removes all the Interactables from the given list that are not being handled by the manager.
        /// </summary>
        /// <param name="manager">The Interaction Manager to check registration against.</param>
        /// <param name="interactables">List of elements that will be filtered to exclude those not registered.</param>
        /// <returns>Returns the number of elements removed from the list.</returns>
        /// <remarks>
        /// Does not modify the manager at all, just the list.
        /// </remarks>
        internal static int RemoveAllUnregistered(VXRInteractionManager manager, List<Interactable> interactables)
        {
            var numRemoved = 0;
            for (var i = interactables.Count - 1; i >= 0; --i)
            {
                if (!manager.m_Interactables.IsRegistered(interactables[i]))
                {
                    interactables.RemoveAt(i);
                    ++numRemoved;
                }
            }

            return numRemoved;
        }

        /// <summary>
        /// Automatically called each frame during Update to clear the focus of the interaction group if necessary due to current conditions.
        /// </summary>
        /// <param name="interactionGroup">The interaction group to potentially exit its focus state.</param>
        protected virtual void ClearInteractionGroupFocus(IXRInteractionGroup interactionGroup)
        {
            // We want to unfocus whenever we select 'nothing'
            // If nothing is focused, then we are not in that scenario.
            // Otherwise, we check for selection activation with lack of selected object.
            var focusInteractor = interactionGroup.FocusInteractor;
            var focusInteractable = interactionGroup.FocusInteractable;
            if (focusInteractor == null || focusInteractable == null)
            {
                return;
            }

            var cleared = false;
            
            if (focusInteractor != null)
            {
                cleared = (focusInteractor.IsSelectActive && !focusInteractor.IsSelecting(focusInteractable));
            }

            if (cleared || !CanFocus(focusInteractor, focusInteractable))
            {
                FocusExit(interactionGroup, interactionGroup.FocusInteractable);
            }
        }

        void CancelInteractorFocus(Interactor interactor)
        {
            var asGroupMember = interactor as IXRGroupMember;
            var group = asGroupMember?.ContainingGroup;

            if (group != null && group.FocusInteractable != null)
            {
                FocusCancel(group, group.FocusInteractable);
            }
        }

        /// <summary>
        /// Automatically called when an Interactable is unregistered to cancel the focus of the Interactable if necessary.
        /// </summary>
        /// <param name="interactable">The Interactable to potentially exit its focus state due to cancellation.</param>
        public virtual void CancelInteractableFocus(Interactable interactable)
        {
            for (var i = interactable.InteractionGroupsFocusing.Count - 1; i >= 0; --i)
            {
                FocusCancel(interactable.InteractionGroupsFocusing[i], interactable);
            }
        }

        /// <summary>
        /// Automatically called each frame during Update to clear the selection of the Interactor if necessary due to current conditions.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially exit its selection state.</param>
        /// <param name="validTargets">The list of interactables that this Interactor could possibly interact with this frame.</param>
        /// <seealso cref="VXRInteractionManager.ClearInteractorHover(Interactor, List{Interactable})"/>
        public virtual void ClearInteractorSelection(Interactor interactor, List<Interactable> validTargets)
        {
            if (interactor.InteractablesSelected.Count == 0)
                return;

            m_CurrentSelected.Clear();
            m_CurrentSelected.AddRange(interactor.InteractablesSelected);

            // Performance optimization of the Contains checks by putting the valid targets into a HashSet.
            // Some Interactors like ARGestureInteractor can have hundreds of valid Interactables
            // since they will add most ARBaseGestureInteractable instances.
            m_UnorderedValidTargets.Clear();
            if (validTargets.Count > 0)
            {
                foreach (var target in validTargets)
                {
                    m_UnorderedValidTargets.Add(target);
                }
            }

            for (var i = m_CurrentSelected.Count - 1; i >= 0; --i)
            {
                var interactable = m_CurrentSelected[i];
                // Selection, unlike hover, can control whether the interactable has to continue being a valid target
                // to automatically cause it to be deselected.
                if (!CanSelect(interactor, interactable) || (!interactor.KeepSelectedTargetValid && !m_UnorderedValidTargets.Contains(interactable)))
                {
                    SelectExit(interactor, interactable);
                }
            }
        }

        /// <summary>
        /// Automatically called when an Interactor is unregistered to cancel the selection of the Interactor if necessary.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially exit its selection state due to cancellation.</param>
        public virtual void CancelInteractorSelection(Interactor interactor)
        {
            for (var i = interactor.InteractablesSelected.Count - 1; i >= 0; --i)
            {
                SelectCancel(interactor, interactor.InteractablesSelected[i]);
            }
        }

        /// <summary>
        /// Automatically called when an Interactable is unregistered to cancel the selection of the Interactable if necessary.
        /// </summary>
        /// <param name="interactable">The Interactable to potentially exit its selection state due to cancellation.</param>
        public virtual void CancelInteractableSelection(Interactable interactable)
        {
            for (var i = interactable.InteractorsSelecting.Count - 1; i >= 0; --i)
            {
                SelectCancel(interactable.InteractorsSelecting[i], interactable);
            }
        }

        /// <summary>
        /// Automatically called each frame during Update to clear the hover state of the Interactor if necessary due to current conditions.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially exit its hover state.</param>
        /// <param name="validTargets">The list of interactables that this Interactor could possibly interact with this frame.</param>
        /// <seealso cref="VXRInteractionManager.ClearInteractorSelection(Interactor, List{Interactable})"/>
        public virtual void ClearInteractorHover(Interactor interactor, List<Interactable> validTargets)
        {
            if (interactor.InteractablesHovered.Count == 0)
                return;

            m_CurrentHovered.Clear();
            m_CurrentHovered.AddRange(interactor.InteractablesHovered);

            // Performance optimization of the Contains checks by putting the valid targets into a HashSet.
            // Some Interactors like ARGestureInteractor can have hundreds of valid Interactables
            // since they will add most ARBaseGestureInteractable instances.
            m_UnorderedValidTargets.Clear();
            if (validTargets.Count > 0)
            {
                foreach (var target in validTargets)
                {
                    m_UnorderedValidTargets.Add(target);
                }
            }

            for (var i = m_CurrentHovered.Count - 1; i >= 0; --i)
            {
                var interactable = m_CurrentHovered[i];
                if (!CanHover(interactor, interactable) || !m_UnorderedValidTargets.Contains(interactable))
                    HoverExit(interactor, interactable);
            }
        }

        /// <summary>
        /// Automatically called when an Interactor is unregistered to cancel the hover state of the Interactor if necessary.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially exit its hover state due to cancellation.</param>
        public virtual void CancelInteractorHover(Interactor interactor)
        {
            for (var i = interactor.InteractablesHovered.Count - 1; i >= 0; --i)
            {
                HoverCancel(interactor, interactor.InteractablesHovered[i]);
            }
        }

        /// <summary>
        /// Automatically called when an Interactable is unregistered to cancel the hover state of the Interactable if necessary.
        /// </summary>
        /// <param name="interactable">The Interactable to potentially exit its hover state due to cancellation.</param>
        public virtual void CancelInteractableHover(Interactable interactable)
        {
            for (var i = interactable.InteractorsHovering.Count - 1; i >= 0; --i)
            {
                HoverCancel(interactable.InteractorsHovering[i], interactable);
            }
        }

        /// <summary>
        /// Initiates focus of an Interactable by an Interactor. This method may first result in other interaction events
        /// such as causing the Interactable to first lose focus.
        /// </summary>
        /// <param name="interactor">The Interactor that is gaining focus. Must be a member of an Interaction group.</param>
        /// <param name="interactable">The Interactable being focused.</param>
        /// <remarks>
        /// This attempt may be ignored depending on the focus policy of the Interactor and/or the Interactable. This attempt will also be ignored if the Interactor is not a member of an Interaction group.
        /// </remarks>
        public virtual void FocusEnter(Interactor interactor, Interactable interactable)
        {
            var asGroupMember = interactor as IXRGroupMember;
            var group = asGroupMember?.ContainingGroup;

            if (group == null || !CanFocus(interactor, interactable))
            {
                return;
            }

            if (interactable.IsFocused && !ResolveExistingFocus(group, interactable))
            {
                return;
            }

            using (m_FocusEnterEventArgs.Get(out var args))
            {
                args.Manager = this;
                args.InteractorObject = interactor;
                args.InteractableObject = interactable;
                args.InteractionGroup = group;
                FocusEnter(group, interactable, args);
            }
        }

        /// <summary>
        /// Initiates losing focus of an Interactable by an Interactor.
        /// </summary>
        /// <param name="group">The Interaction group that is losing focus.</param>
        /// <param name="interactable">The Interactable that is no longer focused.</param>
        public virtual void FocusExit(IXRInteractionGroup group, Interactable interactable)
        {
            var interactor = group.FocusInteractor;

            using (m_FocusExitEventArgs.Get(out var args))
            {
                args.Manager = this;
                args.InteractorObject = interactor;
                args.InteractableObject = interactable;
                args.InteractionGroup = group;
                args.IsCanceled = false;
                FocusExit(group, interactable, args);
            }
        }

        /// <summary>
        /// Initiates losing focus of an Interactable by an Interaction group due to cancellation,
        /// such as from either being unregistered due to being disabled or destroyed.
        /// </summary>
        /// <param name="group">The Interaction group that is losing focus of the interactable.</param>
        /// <param name="interactable">The Interactable that is no longer focused.</param>
        public virtual void FocusCancel(IXRInteractionGroup group, Interactable interactable)
        {
            using (m_FocusExitEventArgs.Get(out var args))
            {
                args.Manager = this;
                args.InteractorObject = group.FocusInteractor;
                args.InteractableObject = interactable;
                args.InteractionGroup = group;
                args.IsCanceled = true;
                FocusExit(group, interactable, args);
            }
        }

        /// <summary>
        /// Initiates selection of an Interactable by an Interactor. This method may first result in other interaction events
        /// such as causing the Interactable to first exit being selected.
        /// </summary>
        /// <param name="interactor">The Interactor that is selecting.</param>
        /// <param name="interactable">The Interactable being selected.</param>
        /// <remarks>
        /// This attempt may be ignored depending on the selection policy of the Interactor and/or the Interactable.
        /// </remarks>
        public virtual void SelectEnter(Interactor interactor, Interactable interactable)
        {
            if (interactable.IsSelected && !ResolveExistingSelect(interactor, interactable))
                return;

            using (m_SelectEnterEventArgs.Get(out var args))
            {
                args.Manager = this;
                args.InteractorObject = interactor;
                args.InteractableObject = interactable;
                SelectEnter(interactor, interactable, args);
            }

            if (interactable is Interactable focusInteractable)
            {
                FocusEnter(interactor, focusInteractable);                    
            }
        }

        /// <summary>
        /// Initiates ending selection of an Interactable by an Interactor.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer selecting.</param>
        /// <param name="interactable">The Interactable that is no longer being selected.</param>
        public virtual void SelectExit(Interactor interactor, Interactable interactable)
        {
            using (m_SelectExitEventArgs.Get(out var args))
            {
                args.Manager = this;
                args.InteractorObject = interactor;
                args.InteractableObject = interactable;
                args.IsCanceled = false;
                SelectExit(interactor, interactable, args);
            }
        }

        /// <summary>
        /// Initiates ending selection of an Interactable by an Interactor due to cancellation,
        /// such as from either being unregistered due to being disabled or destroyed.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer selecting.</param>
        /// <param name="interactable">The Interactable that is no longer being selected.</param>
        public virtual void SelectCancel(Interactor interactor, Interactable interactable)
        {
            using (m_SelectExitEventArgs.Get(out var args))
            {
                args.Manager = this;
                args.InteractorObject = interactor;
                args.InteractableObject = interactable;
                args.IsCanceled = true;
                SelectExit(interactor, interactable, args);
            }
        }

        /// <summary>
        /// Initiates hovering of an Interactable by an Interactor.
        /// </summary>
        /// <param name="interactor">The Interactor that is hovering.</param>
        /// <param name="interactable">The Interactable being hovered over.</param>
        public virtual void HoverEnter(Interactor interactor, Interactable interactable)
        {
            using (m_HoverEnterEventArgs.Get(out var args))
            {
                args.Manager = this;
                args.InteractorObject = interactor;
                args.InteractableObject = interactable;
                HoverEnter(interactor, interactable, args);
            }
        }

        /// <summary>
        /// Initiates ending hovering of an Interactable by an Interactor.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer hovering.</param>
        /// <param name="interactable">The Interactable that is no longer being hovered over.</param>
        public virtual void HoverExit(Interactor interactor, Interactable interactable)
        {
            using (m_HoverExitEventArgs.Get(out var args))
            {
                args.Manager = this;
                args.InteractorObject = interactor;
                args.InteractableObject = interactable;
                args.IsCanceled = false;
                HoverExit(interactor, interactable, args);
            }
        }

        /// <summary>
        /// Initiates ending hovering of an Interactable by an Interactor due to cancellation,
        /// such as from either being unregistered due to being disabled or destroyed.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer hovering.</param>
        /// <param name="interactable">The Interactable that is no longer being hovered over.</param>
        public virtual void HoverCancel(Interactor interactor, Interactable interactable)
        {
            using (m_HoverExitEventArgs.Get(out var args))
            {
                args.Manager = this;
                args.InteractorObject = interactor;
                args.InteractableObject = interactable;
                args.IsCanceled = true;
                HoverExit(interactor, interactable, args);
            }
        }

        /// <summary>
        /// Initiates focus of an Interactable by an interaction group, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="group">The interaction group that is gaining focus.</param>
        /// <param name="interactable">The Interactable being focused.</param>
        /// <param name="args">Event data containing the interaction group and Interactable involved in the event.</param>
        /// <remarks>
        /// The interaction group and interactable are notified immediately without waiting for a previous call to finish
        /// in the case when this method is called again in a nested way. This means that if this method is
        /// called during the handling of the first event, the second will start and finish before the first
        /// event finishes calling all methods in the sequence to notify of the first event.
        /// </remarks>
        // ReSharper disable PossiblyImpureMethodCallOnReadonlyVariable -- ProfilerMarker.Begin with context object does not have Pure attribute
        protected virtual void FocusEnter(IXRInteractionGroup group, Interactable interactable, FocusEnterEventArgs args)
        {
            Debug.Assert(args.InteractableObject == interactable, this);
            Debug.Assert(args.InteractionGroup == group, this);
            Debug.Assert(args.Manager == this || args.Manager == null, this);
            args.Manager = this;

            using (s_FocusEnterMarker.Auto())
            {
                group.OnFocusEntering(args);
                interactable.OnFocusEntering(args);
                interactable.OnFocusEntered(args);
            }

            lastFocused = interactable;
            focusGained?.Invoke(args);
        }

        /// <summary>
        /// Initiates losing focus of an Interactable by an Interaction Group, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="group">The Interaction Group that is no longer selecting.</param>
        /// <param name="interactable">The Interactable that is no longer being selected.</param>
        /// <param name="args">Event data containing the Interactor and Interactable involved in the event.</param>
        /// <remarks>
        /// The interactable is notified immediately without waiting for a previous call to finish
        /// in the case when this method is called again in a nested way. This means that if this method is
        /// called during the handling of the first event, the second will start and finish before the first
        /// event finishes calling all methods in the sequence to notify of the first event.
        /// </remarks>
        protected virtual void FocusExit(IXRInteractionGroup group, Interactable interactable, FocusExitEventArgs args)
        {
            Debug.Assert(args.InteractorObject == group.FocusInteractor, this);
            Debug.Assert(args.InteractableObject == interactable, this);
            Debug.Assert(args.Manager == this || args.Manager == null, this);
            args.Manager = this;

            using (s_FocusExitMarker.Auto())
            {
                group.OnFocusExiting(args);
                interactable.OnFocusExiting(args);
                interactable.OnFocusExited(args);
            }

            if (interactable == lastFocused)
                lastFocused = null;

            focusLost?.Invoke(args);
        }

        /// <summary>
        /// Initiates selection of an Interactable by an Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="interactor">The Interactor that is selecting.</param>
        /// <param name="interactable">The Interactable being selected.</param>
        /// <param name="args">Event data containing the Interactor and Interactable involved in the event.</param>
        /// <remarks>
        /// The interactor and interactable are notified immediately without waiting for a previous call to finish
        /// in the case when this method is called again in a nested way. This means that if this method is
        /// called during the handling of the first event, the second will start and finish before the first
        /// event finishes calling all methods in the sequence to notify of the first event.
        /// </remarks>
        // ReSharper disable PossiblyImpureMethodCallOnReadonlyVariable -- ProfilerMarker.Begin with context object does not have Pure attribute
        protected virtual void SelectEnter(Interactor interactor, Interactable interactable, SelectEnterEventArgs args)
        {
            Debug.Assert(args.InteractorObject == interactor, this);
            Debug.Assert(args.InteractableObject == interactable, this);
            Debug.Assert(args.Manager == this || args.Manager == null, this);
            args.Manager = this;

            using (s_SelectEnterMarker.Auto())
            {
                interactor.OnSelectEntering(args);
                interactable.OnSelectEntering(args);
                interactor.OnSelectEntered(args);
                interactable.OnSelectEntered(args);
            }
        }

        /// <summary>
        /// Initiates ending selection of an Interactable by an Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer selecting.</param>
        /// <param name="interactable">The Interactable that is no longer being selected.</param>
        /// <param name="args">Event data containing the Interactor and Interactable involved in the event.</param>
        /// <remarks>
        /// The interactor and interactable are notified immediately without waiting for a previous call to finish
        /// in the case when this method is called again in a nested way. This means that if this method is
        /// called during the handling of the first event, the second will start and finish before the first
        /// event finishes calling all methods in the sequence to notify of the first event.
        /// </remarks>
        protected virtual void SelectExit(Interactor interactor, Interactable interactable, SelectExitEventArgs args)
        {
            Debug.Assert(args.InteractorObject == interactor, this);
            Debug.Assert(args.InteractableObject == interactable, this);
            Debug.Assert(args.Manager == this || args.Manager == null, this);
            args.Manager = this;

            using (s_SelectExitMarker.Auto())
            {
                interactor.OnSelectExiting(args);
                interactable.OnSelectExiting(args);
                interactor.OnSelectExited(args);
                interactable.OnSelectExited(args);
            }
        }

        /// <summary>
        /// Initiates hovering of an Interactable by an Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="interactor">The Interactor that is hovering.</param>
        /// <param name="interactable">The Interactable being hovered over.</param>
        /// <param name="args">Event data containing the Interactor and Interactable involved in the event.</param>
        /// <remarks>
        /// The interactor and interactable are notified immediately without waiting for a previous call to finish
        /// in the case when this method is called again in a nested way. This means that if this method is
        /// called during the handling of the first event, the second will start and finish before the first
        /// event finishes calling all methods in the sequence to notify of the first event.
        /// </remarks>
        protected virtual void HoverEnter(Interactor interactor, Interactable interactable, HoverEnterEventArgs args)
        {
            Debug.Assert(args.InteractorObject == interactor, this);
            Debug.Assert(args.InteractableObject == interactable, this);
            Debug.Assert(args.Manager == this || args.Manager == null, this);
            args.Manager = this;

            using (s_HoverEnterMarker.Auto())
            {
                interactor.OnHoverEntering(args);
                interactable.OnHoverEntering(args);
                interactor.OnHoverEntered(args);
                interactable.OnHoverEntered(args);
            }
        }

        /// <summary>
        /// Initiates ending hovering of an Interactable by an Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer hovering.</param>
        /// <param name="interactable">The Interactable that is no longer being hovered over.</param>
        /// <param name="args">Event data containing the Interactor and Interactable involved in the event.</param>
        /// <remarks>
        /// The interactor and interactable are notified immediately without waiting for a previous call to finish
        /// in the case when this method is called again in a nested way. This means that if this method is
        /// called during the handling of the first event, the second will start and finish before the first
        /// event finishes calling all methods in the sequence to notify of the first event.
        /// </remarks>
        protected virtual void HoverExit(Interactor interactor, Interactable interactable, HoverExitEventArgs args)
        {
            Debug.Assert(args.InteractorObject == interactor, this);
            Debug.Assert(args.InteractableObject == interactable, this);
            Debug.Assert(args.Manager == this || args.Manager == null, this);
            args.Manager = this;

            using (s_HoverExitMarker.Auto())
            {
                interactor.OnHoverExiting(args);
                interactable.OnHoverExiting(args);
                interactor.OnHoverExited(args);
                interactable.OnHoverExited(args);
            }
        }
        // ReSharper restore PossiblyImpureMethodCallOnReadonlyVariable

        /// <summary>
        /// Automatically called each frame during Update to enter the selection state of the Interactor if necessary due to current conditions.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially enter its selection state.</param>
        /// <param name="validTargets">The list of interactables that this Interactor could possibly interact with this frame.</param>
        /// <remarks>
        /// If the Interactor implements <see cref="IXRTargetPriorityInteractor"/> and is configured to monitor Targets, this method will update its
        /// Targets For Selection property.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.InteractorHoverValidTargets(Interactor, List{Interactable})"/>
        public virtual void InteractorSelectValidTargets(Interactor interactor, List<Interactable> validTargets)
        {
            if (validTargets.Count == 0)
                return;

            var targetPriorityInteractor = interactor as IXRTargetPriorityInteractor;
            var targetPriorityMode = TargetPriorityMode.None;
            if (targetPriorityInteractor != null)
                targetPriorityMode = targetPriorityInteractor.TargetPriorityMode;

            var foundHighestPriorityTarget = false;
            foreach (var target in validTargets)
            {
                if (target is not Interactable interactable)
                {
                    continue;
                }

                if (targetPriorityMode == TargetPriorityMode.None || targetPriorityMode == TargetPriorityMode.HighestPriorityOnly && foundHighestPriorityTarget)
                {
                    if (CanSelect(interactor, interactable))
                    {
                        SelectEnter(interactor, interactable);
                    }
                }
                else if (IsSelectPossible(interactor, interactable))
                {
                    if (!foundHighestPriorityTarget)
                    {
                        foundHighestPriorityTarget = true;

                        if (!m_HighestPriorityTargetMap.TryGetValue(interactable, out var interactorList))
                        {
                            interactorList = s_TargetPriorityInteractorListPool.Get();
                            m_HighestPriorityTargetMap[interactable] = interactorList;
                        }
                        interactorList.Add(targetPriorityInteractor);
                    }

                    // ReSharper disable once PossibleNullReferenceException -- Guaranteed to not be null in this branch since not TargetPriorityMode.None
                    targetPriorityInteractor.TargetsForSelection?.Add(interactable);

                    if (interactor.IsSelectActive)
                    {
                        SelectEnter(interactor, interactable);
                    }
                }
            }
        }

        /// <summary>
        /// Automatically called each frame during Update to enter the hover state of the Interactor if necessary due to current conditions.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially enter its hover state.</param>
        /// <param name="validTargets">The list of interactables that this Interactor could possibly interact with this frame.</param>
        /// <seealso cref="VXRInteractionManager.InteractorSelectValidTargets(Interactor, List{Interactable})"/>
        public virtual void InteractorHoverValidTargets(Interactor interactor, List<Interactable> validTargets)
        {
            if (validTargets.Count == 0)
                return;

            foreach (var target in validTargets)
            {
                if (target is not Interactable interactable) continue;
                
                if (CanHover(interactor, interactable) && !interactor.IsHovering(interactable))
                {
                    HoverEnter(interactor, interactable);
                }
            }
        }

        /// <summary>
        /// Automatically called when gaining focus of an Interactable by an interaction group is initiated
        /// and the Interactable is already focused.
        /// </summary>
        /// <param name="interactionGroup">The interaction group that is gaining focus.</param>
        /// <param name="interactable">The Interactable being focused.</param>
        /// <returns>Returns <see langword="true"/> if the existing focus was successfully resolved and focus should continue.
        /// Otherwise, returns <see langword="false"/> if the focus should be ignored.</returns>
        /// <seealso cref="FocusEnter(Interactor, Interactable)"/>
        protected virtual bool ResolveExistingFocus(IXRInteractionGroup interactionGroup, Interactable interactable)
        {
            Debug.Assert(interactable.IsFocused, this);

            if (interactionGroup.FocusInteractable == interactable)
                return false;

            switch (interactable.FocusMode)
            {
                case InteractableFocusMode.Single:
                    ExitInteractableFocus(interactable);
                    break;
                case InteractableFocusMode.Multiple:
                    break;
                default:
                    Debug.Assert(false, $"Unhandled {nameof(InteractableFocusMode)}={interactable.FocusMode}", this);
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Automatically called when selection of an Interactable by an Interactor is initiated
        /// and the Interactable is already selected.
        /// </summary>
        /// <param name="interactor">The Interactor that is selecting.</param>
        /// <param name="interactable">The Interactable being selected.</param>
        /// <returns>Returns <see langword="true"/> if the existing selection was successfully resolved and selection should continue.
        /// Otherwise, returns <see langword="false"/> if the select should be ignored.</returns>
        /// <seealso cref="SelectEnter(Interactor, Interactable)"/>
        protected virtual bool ResolveExistingSelect(Interactor interactor, Interactable interactable)
        {
            Debug.Assert(interactable.IsSelected, this);

            if (interactor.IsSelecting(interactable))
                return false;

            switch (interactable.SelectMode)
            {
                case InteractableSelectMode.Single:
                    ExitInteractableSelection(interactable);
                    break;
                case InteractableSelectMode.Multiple:
                    break;
                default:
                    Debug.Assert(false, $"Unhandled {nameof(InteractableSelectMode)}={interactable.SelectMode}", this);
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the Interactor and Interactable share at least one interaction layer
        /// between their Interaction Layer Masks.
        /// </summary>
        /// <param name="interactor">The Interactor to check.</param>
        /// <param name="interactable">The Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if the Interactor and Interactable share at least one interaction layer. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="Interactor.InteractionLayers"/>
        /// <seealso cref="Interactable.InteractionLayers"/>
        //protected static bool HasInteractionLayerOverlap(Interactor interactor, Interactable interactable)
        //{
        //    foreach (var layer in interactable.NewInteractionLayers)
        //    {
        //        if (interactor.NewInteractionLayers.Contains(layer))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;

        //    return (interactor.InteractionLayers & interactable.InteractionLayers) != 0;
        //}

        /// <summary>
        /// Returns the processing value of the filters in <see cref="hoverFilters"/> for the given Interactor and
        /// Interactable.
        /// </summary>
        /// <param name="interactor">The Interactor to be validated by the hover filters.</param>
        /// <param name="interactable">The Interactable to be validated by the hover filters.</param>
        /// <returns>
        /// Returns <see langword="true"/> if all processed filters also return <see langword="true"/>, or if
        /// <see cref="hoverFilters"/> is empty. Otherwise, returns <see langword="false"/>.
        /// </returns>
        protected bool ProcessHoverFilters(Interactor interactor, Interactable interactable)
        {
            return XRFilterUtility.Process(m_HoverFilters, interactor, interactable);
        }

        /// <summary>
        /// Returns the processing value of the filters in <see cref="selectFilters"/> for the given Interactor and
        /// Interactable.
        /// </summary>
        /// <param name="interactor">The Interactor to be validated by the select filters.</param>
        /// <param name="interactable">The Interactable to be validated by the select filters.</param>
        /// <returns>
        /// Returns <see langword="true"/> if all processed filters also return <see langword="true"/>, or if
        /// <see cref="selectFilters"/> is empty. Otherwise, returns <see langword="false"/>.
        /// </returns>
        protected bool ProcessSelectFilters(Interactor interactor, Interactable interactable)
        {
            return XRFilterUtility.Process(m_SelectFilters, interactor, interactable);
        }

        private void ExitInteractableSelection(Interactable interactable)
        {
            for (var i = interactable.InteractorsSelecting.Count - 1; i >= 0; --i)
            {
                SelectExit(interactable.InteractorsSelecting[i], interactable);
            }
        }

        private void ExitInteractableFocus(Interactable interactable)
        {
            for (var i = interactable.InteractionGroupsFocusing.Count - 1; i >= 0; --i)
            {
                FocusExit(interactable.InteractionGroupsFocusing[i], interactable);
            }
        }

        private void ClearPriorityForSelectionMap()
        {
            if (m_HighestPriorityTargetMap.Count == 0)
                return;

            foreach (var interactorList in m_HighestPriorityTargetMap.Values)
            {
                foreach (var interactor in interactorList)
                    interactor?.TargetsForSelection?.Clear();

                s_TargetPriorityInteractorListPool.Release(interactorList);
            }

            m_HighestPriorityTargetMap.Clear();
        }

        private void FlushRegistration()
        {
            m_InteractionGroups.Flush();
            m_Interactors.Flush();
            m_Interactables.Flush();
        }

#if UNITY_EDITOR
        private static string GetHierarchyPath(GameObject gameObject, bool includeScene = true)
        {
            return SearchUtils.GetHierarchyPath(gameObject, includeScene);
        }
#endif
    }
}