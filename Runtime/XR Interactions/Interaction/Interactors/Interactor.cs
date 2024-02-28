using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.XR.CoreUtils.Bindings.Variables;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Utilities;
using Object = UnityEngine.Object;

namespace VaporXR.Interaction
{
    [SelectionBase]
    [AddComponentMenu("Interactor")]
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Interactors)]
    public class Interactor : MonoBehaviour, IInteractor, IPoseSource
    {
        private static readonly ProfilerMarker s_ProcessInteractionStrengthMarker = new("VXR.ProcessInteractionStrength.Interactors");
        private const float InteractionStrengthSelect = 1f;

        [FoldoutGroup("Sorting"), SerializeField]
        [RichTextTooltip("If <lw>true</lw>, valid targeting sorting will only include the first sorter in the list that returns a result." +
            "\n\n Remarks\n This is useful for creating a priority for grab interactors that can interact with both overlapping interactables and raycasting interactables")]
        private bool _usePrioritySorting = true;
        [FoldoutGroup("Sorting"), SerializeField]
        private List<VXRSorter> _sorters;

        [FoldoutGroup("Interaction"), SerializeField]
        private bool _allowHover;
        [FoldoutGroup("Interaction"), SerializeField]
        private bool _allowSelect;
        //[FoldoutGroup("Interaction"), SerializeField]
        //private InteractionLayerMask _interactionLayers = -1;
        [FoldoutGroup("Interaction"), SerializeField]
        private bool _overrideSorterInteractionLayer;
        [FoldoutGroup("Interaction"), SerializeField, ShowIf("%_overrideSorterInteractionLayer")]
        private List<InteractionLayerKey> _interactionLayers = new();
        [FoldoutGroup("Interaction"), SerializeField]
        private InteractorHandedness _handedness;
        [FoldoutGroup("Interaction"), SerializeField]
        private Transform _attachPoint;

        [FoldoutGroup("Interaction"), SerializeField, ShowIf("%_allowSelect")]
        private InputTriggerType _selectActionTrigger = InputTriggerType.StateChange;
        [FoldoutGroup("Interaction"), SerializeField, ShowIf("%_allowSelect")]
        private bool _keepSelectedTargetValid = true;
        [FoldoutGroup("Interaction"), SerializeField, ShowIf("%_allowSelect")]
        private Interactable _startingSelectedInteractable;

        [FoldoutGroup("Posing"), SerializeField]
        private bool _posingEnabled;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_posingEnabled")]
        private VXRHand _hand;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_posingEnabled")]
        private bool _hoverPosingEnabled;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_hoverPosingEnabled")]
        private HandPoseDatum _hoverPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_hoverPosingEnabled")]
        private float _hoverPoseDuration;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_posingEnabled")]
        private bool _selectPosingEnabled;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_selectPosingEnabled")]
        private HandPoseDatum _selectPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_selectPosingEnabled")]
        private float _selectPoseDuration;

        [SerializeField, FoldoutGroup("Filters", order: 90)]
        private XRBaseTargetFilter _startingTargetFilter;
        [FoldoutGroup("Filters"), SerializeField]
        [RequireInterface(typeof(IXRHoverFilter))]
        private List<Object> _startingHoverFilters = new();
        [FoldoutGroup("Filters"), SerializeField]
        [RequireInterface(typeof(IXRSelectFilter))]
        private List<Object> _startingSelectFilters = new();

        #region Properties
        private VXRInteractionManager _interactionManager;
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> that this Interactor will communicate with (will find one if <see langword="null"/>).
        /// </summary>
        public VXRInteractionManager InteractionManager
        {
            get => _interactionManager;
            set
            {
                _interactionManager = value;
                if (Application.isPlaying && isActiveAndEnabled)
                    RegisterWithInteractionManager();
            }
        }

        public List<InteractorModule> Modules { get; private set; } = new();

        public List<VXRSorter> Sorters => _sorters;

        public VXRSorter OverrideSorter => _overrideSorter;

        ///// <summary>
        ///// (Read Only) Allows interaction with Interactables whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.
        ///// </summary>
        ///// <seealso cref="Interactable.InteractionLayers"/>
        //public InteractionLayerMask InteractionLayers => _interactionLayers;

        public bool OverrideSorterInteractionLayer { get => _overrideSorterInteractionLayer; set => _overrideSorterInteractionLayer = value; }

        /// <summary>
        /// (Read Only) Allows interaction with Interactables whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.
        /// </summary>
        /// <seealso cref="Interactable.InteractionLayers"/>
        public HashSet<int> InteractionLayers { get; } = new();

        /// <summary>
        /// (Read Only) Represents which hand or controller the interactor is associated with.
        /// </summary>
        public InteractorHandedness Handedness => _handedness;

        /// <summary>
        /// (Read Only) The <see cref="Transform"/> that is used as the attach point for Interactables.
        /// </summary>
        /// <remarks>
        /// Automatically instantiated and set in <see cref="Awake"/> if <see langword="null"/>.
        /// </remarks>
        public Transform AttachPoint { get => _attachPoint; set => _attachPoint = value; }

        private IXRTargetFilter _targetFilter;
        /// <summary>
        /// The Target Filter that this Interactor is linked to.
        /// </summary>
        /// <seealso cref="StartingTargetFilter"/>
        public IXRTargetFilter TargetFilter
        {
            get
            {
                return _targetFilter is Object unityObj && unityObj == null ? null : _targetFilter;
            }
            set
            {
                if (Application.isPlaying)
                {
                    TargetFilter?.Unlink(this);
                    _targetFilter = value;
                    TargetFilter?.Link(this);
                }
                else
                {
                    _targetFilter = value;
                }
            }
        }


        // ***** Hover *****
        /// <summary>
        /// (Read Only) Indicates whether this Interactor is in a state where it could hover.
        /// </summary>
        public virtual bool IsHoverActive => _allowHover && HoverActive.Invoke();

        private readonly HashSetList<Interactable> _interactablesHovered = new();
        /// <summary>
        /// (Read Only) The list of Interactables that are currently being hovered over (may by empty).
        /// </summary>
        /// <remarks>
        /// You should treat this as a read only view of the list and should not modify it.
        /// It is exposed as a <see cref="List{T}"/> rather than an <see cref="IReadOnlyList{T}"/> to avoid GC Allocations
        /// when enumerating the list.
        /// </remarks>
        /// <seealso cref="HasHover"/>
        /// <seealso cref="Interactable.InteractorsHovering"/>
        public List<Interactable> InteractablesHovered => (List<Interactable>)_interactablesHovered.AsList();

        /// <summary>
        /// (Read Only) Indicates whether this Interactor is currently hovering an Interactable.
        /// </summary>
        /// <remarks>
        /// In other words, returns whether <see cref="InteractablesHovered"/> contains any Interactables.
        /// <example>
        /// <code>interactablesHovered.Count > 0</code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InteractablesHovered"/>
        /// <seealso cref="Interactable.IsHovered"/>
        public bool HasHover => _interactablesHovered.Count > 0;

        private readonly ExposedRegistrationList<IXRHoverFilter> _hoverFilters = new() { BufferChanges = false };
        /// <summary>
        /// The list of hover filters in this object.
        /// Used as additional hover validations for this Interactor.
        /// </summary>
        /// <remarks>
        /// While processing hover filters, all changes to this list don't have an immediate effect. These changes are
        /// buffered and applied when the processing is finished.
        /// Calling <see cref="IXRFilterList{T}.MoveTo"/> in this list will throw an exception when this list is being processed.
        /// </remarks>
        /// <seealso cref="ProcessHoverFilters"/>
        public IXRFilterList<IXRHoverFilter> HoverFilters => _hoverFilters;

        // ***** Select *****
        public bool IsSelectActive
        {
            get
            {
                if (IsPerformingManualInteraction)
                {
                    return true;
                }

                LogicalSelectState.Mode = _selectActionTrigger;
                return LogicalSelectState.Active;
            }
        }

        /// <summary>
        /// The logical state of the select input.
        /// </summary>
        /// <seealso cref="SelectInput"/>
        public LogicalInputState LogicalSelectState { get; } = new();

        public bool IsPerformingManualInteraction { get; private set; }

        public InputTriggerType SelectActionTrigger { get => _selectActionTrigger; set => _selectActionTrigger = value; }
        public bool KeepSelectedTargetValid => _keepSelectedTargetValid;

        private readonly HashSetList<Interactable> _interactablesSelected = new();

        public List<Interactable> InteractablesSelected => (List<Interactable>)_interactablesSelected.AsList();

        public Interactable FirstInteractableSelected { get; private set; }

        public bool HasSelection => _interactablesSelected.Count > 0;

        private readonly ExposedRegistrationList<IXRSelectFilter> _selectFilters = new() { BufferChanges = false };

        public IXRFilterList<IXRSelectFilter> SelectFilters => _selectFilters;

        public IXRInteractionGroup ContainingGroup { get; private set; }

        private readonly BindableVariable<float> _largestInteractionStrength = new();
        public IReadOnlyBindableVariable<float> LargestInteractionStrength => _largestInteractionStrength;
        #endregion

        #region Fields
        private VXRInteractionManager _registeredInteractionManager;
        private readonly Dictionary<Type, InteractorModule> _moduleMap = new();

        private Transform _vxrOriginTransform;
        private bool _hasVXROrigin;
        private bool _failedToFindVXROrigin;

        private VXRSorter _overrideSorter;
        private bool _useOverrideSorter;

        private readonly Dictionary<Interactable, Pose> _attachPoseOnSelect = new();
        private readonly Dictionary<Interactable, Pose> _localAttachPoseOnSelect = new();
        private readonly HashSetList<Interactable> _interactionStrengthInteractables = new();
        private readonly Dictionary<Interactable, float> _interactionStrengths = new();

        private Interactable _manualInteractionInteractable;
        #endregion

        #region Events
        /// <summary>
        /// Calls the methods in its invocation list when this Interactor is registered with an Interaction Manager.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractorRegisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.interactorRegistered"/>
        public event Action<InteractorRegisteredEventArgs> Registered;

        /// <summary>
        /// Calls the methods in its invocation list when this Interactor is unregistered from an Interaction Manager.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractorUnregisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.interactorUnregistered"/>
        public event Action<InteractorUnregisteredEventArgs> Unregistered;

        public event Action<HoverEnterEventArgs> HoverEntering;
        public event Action<HoverEnterEventArgs> HoverEntered;

        public event Action<HoverExitEventArgs> HoverExiting;
        public event Action<HoverExitEventArgs> HoverExited;

        public Func<bool> HoverActive { get; set; }

        public event Action<SelectEnterEventArgs> SelectEntering;
        public event Action<SelectEnterEventArgs> SelectEntered;

        public event Action<SelectExitEventArgs> SelectExiting;
        public event Action<SelectExitEventArgs> SelectExited;

        /// <summary>
        /// This function returns whether the <see cref="LogicalInput"/> was performed this frame.
        /// </summary>
        /// <remarks>
        /// Usually this listens to an input event, but can be any method that returns a boolean.
        /// </remarks>
        public Func<XRIneractionActiveState> SelectActive { get; set; }

        /// <summary>
        /// (Read Only) Overriding movement type of the selected Interactable's movement.
        /// By default, this does not override the movement type.
        /// </summary>
        /// <remarks>
        /// You can use this to change the effective movement type of an Interactable for different
        /// Interactors. An example would be having an Interactable use <see cref="Interactable.MovementType.VelocityTracking"/>
        /// so it does not move through geometry with a Collider when interacting with it using a Ray or Direct Interactor,
        /// but have a Socket Interactor override the movement type to be <see cref="Interactable.MovementType.Instantaneous"/>
        /// for reduced movement latency.
        /// </remarks>
        /// <seealso cref="VXRGrabInteractable.movementType"/>
        public Func<MovementType> SelectedInteractableMovementTypeOverride { get; set; }
        #endregion

        #region - Initialization -
        protected virtual void Awake()
        {
            // Create empty attach transform if none specified
            CreateAttachTransform();

            // Setup the starting filters
            if (_startingTargetFilter != null)
            {
                TargetFilter = _startingTargetFilter;
            }

            // Setup Interaction Manager
            FindCreateInteractionManager();

            foreach (var layer in _interactionLayers)
            {
                InteractionLayers.Add(layer.Layer);
            }

            Modules.AddRange(GetComponents<InteractorModule>());
            foreach (var module in Modules)
            {
                if (!_moduleMap.TryAdd(module.GetType(), module))
                {
                    Debug.LogError($"Two or more modules of the same type {module.GetType()}");
                }
                var addSubType = module.GetType().BaseType;
                while (addSubType != typeof(InteractorModule))
                {
                    if (!_moduleMap.TryAdd(addSubType, module))
                    {
                        Debug.LogError($"Two or more modules of the same type {module.GetType()}");
                    }
                    addSubType = addSubType.BaseType;
                }
            }

            HoverActive = AllowHover;


            _hoverFilters.RegisterReferences(_startingHoverFilters, this);
            _selectFilters.RegisterReferences(_startingSelectFilters, this);
        }

        protected virtual void Start()
        {
            if (InteractionManager != null && _startingSelectedInteractable != null)
            {
                InteractionManager.SelectEnter(this, _startingSelectedInteractable);
            }
        }

        protected virtual void OnEnable()
        {
            FindCreateInteractionManager();
            RegisterWithInteractionManager();
        }

        private void FindCreateInteractionManager()
        {
            if (_interactionManager == null)
            {
                _interactionManager = ComponentLocatorUtility<VXRInteractionManager>.FindOrCreateComponent();
            }
        }

        private void RegisterWithInteractionManager()
        {
            if (_registeredInteractionManager == _interactionManager)
            {
                return;
            }

            UnregisterWithInteractionManager();

            if (_interactionManager == null)
            {
                return;
            }

            _interactionManager.RegisterInteractor(this);
            _registeredInteractionManager = _interactionManager;
        }

        private void UnregisterWithInteractionManager()
        {
            if (_registeredInteractionManager == null)
            {
                return;
            }

            _registeredInteractionManager.UnregisterInteractor(this);
            _registeredInteractionManager = null;
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when this Interactor is registered with it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that registered this Interactor.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.RegisterInteractor(IXRInteractor)"/>
        public virtual void OnRegistered(InteractorRegisteredEventArgs args)
        {
            if (args.Manager != _interactionManager)
            {
                Debug.LogWarning($"An Interactor was registered with an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{_interactionManager}\" but was registered with \"{args.Manager}\".", this);
            }

            args.Manager.interactableRegistered += OnInteractableRegistered;
            args.Manager.interactableUnregistered += OnInteractableUnregistered;
            Registered?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when this Interactor is unregistered from it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that unregistered this Interactor.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.UnregisterInteractor(IXRInteractor)"/>
        public virtual void OnUnregistered(InteractorUnregisteredEventArgs args)
        {
            if (args.Manager != _registeredInteractionManager)
            {
                Debug.LogWarning($"An Interactor was unregistered from an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{_registeredInteractionManager}\" but was unregistered from \"{args.Manager}\".", this);
            }

            args.Manager.interactableRegistered -= OnInteractableRegistered;
            args.Manager.interactableUnregistered -= OnInteractableUnregistered;

            Unregistered?.Invoke(args);
        }

        private void OnInteractableRegistered(InteractableRegisteredEventArgs args)
        {
            if (_useOverrideSorter)
            {
                _overrideSorter.OnInteractableRegistered(args);
            }
            else
            {
                foreach (var sorter in Sorters)
                {
                    sorter.OnInteractableRegistered(args);
                }
            }
        }

        private void OnInteractableUnregistered(InteractableUnregisteredEventArgs args)
        {
            if (_useOverrideSorter)
            {
                _overrideSorter.OnInteractableUnregistered(args);
            }
            else
            {
                foreach (var sorter in Sorters)
                {
                    sorter.OnInteractableUnregistered(args);
                }
            }
        }

        protected virtual void OnDisable()
        {
            UnregisterWithInteractionManager();
        }

        protected virtual void OnDestroy()
        {
            // Unlink this Interactor from the Target Filter
            TargetFilter?.Unlink(this);
        }

        /// <summary>
        /// Enables or disables the sorters attached to this interactor.
        /// This will start/stop interaction, but will not unregister the interactor and it will still receive processing events.
        /// </summary>
        /// <param name="active">Whether the sorters are active</param>
        public void SetInteractorActive(bool active)
        {
            if (_overrideSorter)
            {
                _overrideSorter.IsActive = active;
            }
            else
            {
                foreach (var sorter in _sorters)
                {
                    sorter.IsActive = active;
                }
            }
        }
        #endregion

        #region - Processing -
        public virtual void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            foreach (var module in Modules)
            {
                module.PrePreProcessInteractor(updatePhase);
            }

            if(updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                if (SelectActive != null)
                {
                    var processed = SelectActive.Invoke();
                    LogicalSelectState.UpdateInput(processed.Performed, processed.WasPerformedThisFrame, HasSelection, processed.Value);
                }
                else
                {
                    LogicalSelectState.UpdateInput(false, false, HasSelection, 0f);
                }
            }
            
            if (_useOverrideSorter)
            {
                _overrideSorter.ProcessContacts(updatePhase);
            }
            else
            {
                foreach (var sorter in _sorters)
                {
                    sorter.ProcessContacts(updatePhase);
                }
            }

            foreach (var module in Modules)
            {
                module.PostPreProcessInteractor(updatePhase);
            }
        }

        public virtual void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            foreach (var module in Modules)
            {
                module.PreProcessInteractor(updatePhase);
            }

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Late)
            {
                if (_useOverrideSorter)
                {
                    _overrideSorter.ProcessContacts(updatePhase);
                }
                else
                {
                    foreach (var sorter in _sorters)
                    {
                        sorter.ProcessContacts(updatePhase);
                    }
                }
            }

            foreach (var module in Modules)
            {
                module.PostProcessInteractor(updatePhase);
            }
        }

        public void SetOverrideSorter(VXRSorter sorter)
        {
            _overrideSorter = sorter;
            _useOverrideSorter = _overrideSorter != null;
        }

        public virtual void GetValidTargets(List<Interactable> targets)
        {
            if (_useOverrideSorter)
            {
                _overrideSorter.GetValidTargets(this, targets, TargetFilter);
            }
            else
            {
                foreach (var sorter in _sorters)
                {
                    sorter.GetValidTargets(this, targets, TargetFilter);
                    if (_usePrioritySorting && targets.Count > 0)
                    {
                        break;
                    }
                }
            }
        }
        #endregion

        #region - Hover -
        protected virtual bool AllowHover()
        {
            return true;
        }

        /// <summary>
        /// Determines if the Interactable is valid for hover this frame.
        /// </summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if the interactable can be hovered over this frame.</returns>
        /// <seealso cref="Interactable.IsHoverableBy"/>
        public virtual bool CanHover(Interactable interactable)
        {
            return ProcessHoverFilters(interactable) && (Modules.Count == 0 || _AllModulesCanHover(interactable));

            bool _AllModulesCanHover(Interactable interactable)
            {
                foreach (var module in Modules)
                {
                    if (!module.CanHover(interactable))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Determines whether this Interactor is currently hovering the Interactable.
        /// </summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if this Interactor is currently hovering the Interactable.
        /// Otherwise, returns <seealso langword="false"/>.</returns>
        /// <remarks>
        /// In other words, returns whether <see cref="InteractablesHovered"/> contains <paramref name="interactable"/>.
        /// </remarks>
        /// <seealso cref="InteractablesHovered"/>
        public bool IsHovering(Interactable interactable) => _interactablesHovered.Contains(interactable);

        /// <summary>
        /// Returns the processing value of the filters in <see cref="HoverFilters"/> for this Interactor and the
        /// given Interactable.
        /// </summary>
        /// <param name="interactable">The Interactable to be validated by the hover filters.</param>
        /// <returns>
        /// Returns <see langword="true"/> if all processed filters also return <see langword="true"/>, or if
        /// <see cref="HoverFilters"/> is empty. Otherwise, returns <see langword="false"/>.
        /// </returns>
        protected bool ProcessHoverFilters(Interactable interactable)
        {
            return XRFilterUtility.Process(_hoverFilters, this, interactable);
        }
        #endregion

        #region - Select -
        /// <summary>
        /// Determines if the Interactable is valid for selection this frame.
        /// </summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if the Interactable can be selected this frame.</returns>
        /// <seealso cref="Interactable.IsSelectableBy"/>
        public virtual bool CanSelect(Interactable interactable)
        {
            return ProcessSelectFilters(interactable) && (Modules.Count == 0 || _AllModulesCanSelect(interactable));

            bool _AllModulesCanSelect(Interactable interactable)
            {
                foreach (var module in Modules)
                {
                    if (!module.CanSelect(interactable))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Determines whether this Interactor is currently selecting the Interactable.
        /// </summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if this Interactor is currently selecting the Interactable.
        /// Otherwise, returns <seealso langword="false"/>.</returns>
        /// <remarks>
        /// In other words, returns whether <see cref="InteractablesSelected"/> contains <paramref name="interactable"/>.
        /// </remarks>
        /// <seealso cref="InteractablesSelected"/>
        public bool IsSelecting(Interactable interactable) => _interactablesSelected.Contains(interactable);

        /// <summary>
        /// Returns the processing value of the filters in <see cref="SelectFilters"/> for this Interactor and the
        /// given Interactable.
        /// </summary>
        /// <param name="interactable">The Interactor to be validated by the select filters.</param>
        /// <returns>
        /// Returns <see langword="true"/> if all processed filters also return <see langword="true"/>, or if
        /// <see cref="SelectFilters"/> is empty. Otherwise, returns <see langword="false"/>.
        /// </returns>
        protected bool ProcessSelectFilters(Interactable interactable)
        {
            return XRFilterUtility.Process(_selectFilters, this, interactable);
        }

        /// <summary>
        /// Manually initiate selection of an Interactable.
        /// </summary>
        /// <param name="interactable">Interactable that is being selected.</param>
        /// <seealso cref="EndManualInteraction"/>
        public virtual void StartManualInteraction(Interactable interactable)
        {
            if (InteractionManager == null)
            {
                Debug.LogWarning("Cannot start manual interaction without an Interaction Manager set.", this);
                return;
            }

            InteractionManager.SelectEnter(this, interactable);
            IsPerformingManualInteraction = true;
            _manualInteractionInteractable = interactable;
        }

        /// <summary>
        /// Ends the manually initiated selection of an Interactable.
        /// </summary>
        /// <seealso cref="StartManualInteraction(Interactable)"/>
        public virtual void EndManualInteraction()
        {
            if (InteractionManager == null)
            {
                Debug.LogWarning("Cannot end manual interaction without an Interaction Manager set.", this);
                return;
            }

            if (!IsPerformingManualInteraction)
            {
                Debug.LogWarning("Tried to end manual interaction but was not performing manual interaction. Ignoring request.", this);
                return;
            }

            InteractionManager.SelectExit(this, _manualInteractionInteractable);
            IsPerformingManualInteraction = false;
            _manualInteractionInteractable = null;
        }
        #endregion

        #region - Grouping -
        public void OnRegisteringAsGroupMember(IXRInteractionGroup group)
        {
            if (ContainingGroup != null)
            {
                Debug.LogError($"{name} is already part of a Group. Remove the member from the Group first.", this);
                return;
            }

            if (!group.ContainsGroupMember(this))
            {
                Debug.LogError($"{nameof(IXRGroupMember.OnRegisteringAsGroupMember)} was called but the Group does not contain {name}. " +
                               "Add the member to the Group rather than calling this method directly.", this);
                return;
            }

            ContainingGroup = group;
        }

        public void OnRegisteringAsNonGroupMember()
        {
            ContainingGroup = null;
        }
        #endregion

        #region - Interaction Strength -
        /// <summary>
        /// Gets the interaction strength between the given interactable and this interactor.
        /// </summary>
        /// <param name="interactable">The specific interactable to get the interaction strength between.</param>
        /// <returns>Returns a value <c>[0.0, 1.0]</c> of the interaction strength.</returns>
        public float GetInteractionStrength(Interactable interactable)
        {
            return _interactionStrengths.GetValueOrDefault(interactable, 0f);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method to signal to update the interaction strength.
        /// </summary>
        /// <param name="updatePhase">The update phase during which this method is called.</param>
        /// <seealso cref="XRInteractionUpdateOrder.UpdatePhase"/>
        public virtual void ProcessInteractionStrength(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            var maxInteractionStrength = 0f;

            using (s_ProcessInteractionStrengthMarker.Auto())
            {
                _interactionStrengths.Clear();

                // Select is checked before Hover to allow process to only be called once per interactor hovering and selecting
                // using the largest initial interaction strength.
                for (int i = 0, count = _interactablesSelected.Count; i < count; ++i)
                {
                    var interactable = _interactablesSelected[i];
                    if (interactable is Interactable)
                    {
                        continue;
                    }

                    _interactionStrengths[interactable] = LogicalSelectState.CurrentValue;

                    maxInteractionStrength = LogicalSelectState.CurrentValue;
                }

                for (int i = 0, count = _interactionStrengthInteractables.Count; i < count; ++i)
                {
                    var interactable = _interactionStrengthInteractables[i];
                    var interactionStrength = interactable.GetInteractionStrength(this);
                    _interactionStrengths[interactable] = interactionStrength;

                    maxInteractionStrength = Mathf.Max(maxInteractionStrength, interactionStrength);
                }
            }

            // This is done outside of the ProfilerMarker since it could trigger user callbacks
            _largestInteractionStrength.Value = maxInteractionStrength;
        }
        #endregion

        #region - Events -
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// right before the Interactor first initiates hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is being hovered over.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverEntered(HoverEnterEventArgs)"/>
        public virtual void OnHoverEntering(HoverEnterEventArgs args)
        {
            Debug.Log($"{Handedness} Hand Hover Entering: {args.InteractableObject}");

            var added = _interactablesHovered.Add(args.InteractableObject);
            Debug.Assert(added, "An Interactor received a Hover Enter event for an Interactable that it was already hovering over.", this);
            HoverEntering?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interactor first initiates hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is being hovered over.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverExited(HoverExitEventArgs)"/>
        public virtual void OnHoverEntered(HoverEnterEventArgs args)
        {
            Debug.Log($"{Handedness} Hand Hover Entered: {args.InteractableObject}");
            HoverEntered?.Invoke(args);
            OnHoverPoseEntered(args);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// right before the Interactor ends hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is no longer hovered over.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverExited(HoverExitEventArgs)"/>
        public virtual void OnHoverExiting(HoverExitEventArgs args)
        {
            Debug.Log($"{Handedness} Hand Hover Exiting: {args.InteractableObject}");
            var removed = _interactablesHovered.Remove(args.InteractableObject);
            Debug.Assert(removed, "An Interactor received a Hover Exit event for an Interactable that it was not hovering over.", this);
            HoverExiting?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interactor ends hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is no longer hovered over.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverEntered(HoverEnterEventArgs)"/>
        public virtual void OnHoverExited(HoverExitEventArgs args)
        {
            Debug.Log($"{Handedness} Hand Hover Exited: {args.InteractableObject}");
            HoverExited?.Invoke(args);
            OnHoverPoseExited(args);
        }


        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// right before the Interactor first initiates selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is being selected.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectEntered(SelectEnterEventArgs)"/>
        public virtual void OnSelectEntering(SelectEnterEventArgs args)
        {
            Debug.Log($"{Handedness} Hand Select Entering: {args.InteractableObject}");
            var added = _interactablesSelected.Add(args.InteractableObject);
            Debug.Assert(added, "An Interactor received a Select Enter event for an Interactable that it was already selecting.", this);

            if (args.InteractableObject is Interactable interactionStrengthInteractable)
            {
                _interactionStrengthInteractables.Add(interactionStrengthInteractable);
            }

            if (_interactablesSelected.Count == 1)
            {
                FirstInteractableSelected = args.InteractableObject;
            }

            CaptureAttachPose(args.InteractableObject);

            LogicalSelectState.UpdateHasSelection(true);
            SelectEntering?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interactor first initiates selection of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is being selected.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectExited(SelectExitEventArgs)"/>
        public virtual void OnSelectEntered(SelectEnterEventArgs args)
        {
            Debug.Log($"{Handedness} Hand Select Entered: {args.InteractableObject}");

            SelectEntered?.Invoke(args);
            OnSelectPoseEntered(args);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// right before the Interactor ends selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is no longer selected.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectExited(SelectExitEventArgs)"/>
        public virtual void OnSelectExiting(SelectExitEventArgs args)
        {
            Debug.Log($"{Handedness} Hand Select Exiting: {args.InteractableObject}");
            var removed = _interactablesSelected.Remove(args.InteractableObject);
            Debug.Assert(removed, "An Interactor received a Select Exit event for an Interactable that it was not selecting.", this);

            if (_interactionStrengthInteractables.Count > 0 &&
                args.InteractableObject != null &&
                !IsHovering(args.InteractableObject))
            {
                _interactionStrengthInteractables.Remove(args.InteractableObject);
            }

            // Wait until all selections have been exited in case multiple selections are allowed.
            if (HasSelection)
            {
                return;
            }

            LogicalSelectState.UpdateHasSelection(false);
            SelectExiting?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interactor ends selection of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is no longer selected.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectEntered(SelectEnterEventArgs)"/>
        public virtual void OnSelectExited(SelectExitEventArgs args)
        {
            Debug.Log($"{Handedness} Hand Select Exited: {args.InteractableObject}");
            SelectExited?.Invoke(args);

            // The dictionaries are pruned so that they don't infinitely grow in size as selections are made.
            if (_interactablesSelected.Count == 0)
            {
                FirstInteractableSelected = null;
                _attachPoseOnSelect.Clear();
                _localAttachPoseOnSelect.Clear();
            }
            OnSelectPoseExited(args);
        }
        #endregion

        #region - Posing -
        /// <summary>
        /// Capture the current Attach Transform pose.
        /// This method is automatically called by Unity to capture the pose during the moment of selection.
        /// </summary>
        /// <param name="interactable">The specific Interactable as context to get the attachment point for.</param>
        /// <remarks>
        /// Unity automatically calls this method during <see cref="OnSelectEntering(SelectEnterEventArgs)"/>
        /// and should not typically need to be called by a user.
        /// </remarks>
        /// <seealso cref="GetAttachPoseOnSelect"/>
        /// <seealso cref="GetLocalAttachPoseOnSelect"/>
        /// <seealso cref="Interactable.CaptureAttachPose"/>
        protected void CaptureAttachPose(Interactable interactable)
        {
            var thisAttachTransform = GetAttachTransform(interactable);
            if (thisAttachTransform != null)
            {
                _attachPoseOnSelect[interactable] =
                    new Pose(thisAttachTransform.position, thisAttachTransform.rotation);
                _localAttachPoseOnSelect[interactable] =
                    new Pose(thisAttachTransform.localPosition, thisAttachTransform.localRotation);
            }
            else
            {
                _attachPoseOnSelect.Remove(interactable);
                _localAttachPoseOnSelect.Remove(interactable);
            }
        }

        /// <summary>
        /// Gets the world position and rotation of the Attach Transform captured during the moment of selection.
        /// </summary>
        /// <param name="interactable">The specific Interactable as context to get the attachment point for.</param>
        /// <returns>Returns the world pose of the attachment point during the moment of selection,
        /// and otherwise the identity <see cref="Pose"/> if it was not selected during the current selection stack.</returns>
        /// <seealso cref="GetLocalAttachPoseOnSelect"/>
        /// <seealso cref="IXRInteractor.GetAttachTransform"/>
        /// <seealso cref="Interactable.GetAttachPoseOnSelect"/>
        public Pose GetAttachPoseOnSelect(Interactable interactable)
        {
            return _attachPoseOnSelect.TryGetValue(interactable, out var pose) ? pose : Pose.identity;
        }

        /// <summary>
        /// Gets the local position and rotation of the Attach Transform captured during the moment of selection.
        /// </summary>
        /// <param name="interactable">The specific Interactable as context to get the attachment point for.</param>
        /// <returns>Returns the local pose of the attachment point during the moment of selection,
        /// and otherwise the identity <see cref="Pose"/> if it was not selected during the current selection stack.</returns>
        /// <seealso cref="GetAttachPoseOnSelect"/>
        /// <seealso cref="IXRInteractor.GetAttachTransform"/>
        /// <seealso cref="Interactable.GetLocalAttachPoseOnSelect"/>
        public Pose GetLocalAttachPoseOnSelect(Interactable interactable)
        {
            return _localAttachPoseOnSelect.TryGetValue(interactable, out var pose) ? pose : Pose.identity;
        }

        private void OnHoverPoseEntered(HoverEnterEventArgs args)
        {
            if (_posingEnabled)
            {
                if (args.InteractableObject.TryGetOverrideHoverPose(out var pose, out var duration))
                {
                    _hand.RequestHandPose(HandPoseType.Hover, this, pose.Value, duration: duration);
                }
                else if (_hoverPose != null)
                {
                    _hand.RequestHandPose(HandPoseType.Hover, this, _hoverPose.Value, duration: _hoverPoseDuration);
                }
            }
        }

        private void OnHoverPoseExited(HoverExitEventArgs args)
        {
            if (_posingEnabled)
            {
                _hand.RequestReturnToIdle(this, _hoverPoseDuration);
            }
        }

        private void OnSelectPoseEntered(SelectEnterEventArgs args)
        {
            if (_posingEnabled)
            {
                if (args.InteractableObject.TryGetOverrideSelectPose(out var pose, out var duration))
                {
                    _hand.RequestHandPose(HandPoseType.Grab, this, pose.Value, duration: duration);
                }
                else if (_selectPose != null)
                {
                    _hand.RequestHandPose(HandPoseType.Grab, this, _selectPose.Value, duration: _selectPoseDuration);
                }
            }
        }

        private void OnSelectPoseExited(SelectExitEventArgs args)
        {
            if (_posingEnabled)
            {
                _hand.RequestReturnToIdle(this, _selectPoseDuration);
            }
        }
        #endregion

        #region - Attaching -
        /// <summary>
        /// Create a new child GameObject to use as the attach transform if one is not set.
        /// </summary>
        /// <seealso cref="AttachTransform"/>
        protected void CreateAttachTransform()
        {
            if (_attachPoint == null)
            {
                _attachPoint = new GameObject($"[{gameObject.name}] Attach").transform;
                _attachPoint.SetParent(transform, false);
                _attachPoint.localPosition = Vector3.zero;
                _attachPoint.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Gets the <see cref="Transform"/> that is used as the attachment point for a given Interactable.
        /// </summary>
        /// <param name="interactable">The specific Interactable as context to get the attachment point for.</param>
        /// <returns>Returns the attachment point <see cref="Transform"/>.</returns>
        /// <seealso cref="Interactable.GetAttachTransform"/>
        /// <remarks>
        /// This should typically return the Transform of a child GameObject or the <see cref="transform"/> itself.
        /// </remarks>
        public virtual Transform GetAttachTransform(Interactable interactable)
        {
            return _attachPoint;
        }
        #endregion

        #region - Helpers -
        /// <summary>
        /// Attempts to locate and return the XR Origin reference frame for the interactor.
        /// </summary>
        /// <seealso cref="XROrigin"/>
        public bool TryGetXROrigin(out Transform origin)
        {
            if (_hasVXROrigin)
            {
                origin = _vxrOriginTransform;
                return true;
            }

            if (!_failedToFindVXROrigin)
            {
                var xrOrigin = GetComponentInParent<VXROrigin>();
                if (xrOrigin != null)
                {
                    var originGo = xrOrigin.Origin;
                    if (originGo != null)
                    {
                        _vxrOriginTransform = originGo.transform;
                        _hasVXROrigin = true;
                        origin = _vxrOriginTransform;
                        return true;
                    }
                }
                _failedToFindVXROrigin = true;
            }
            origin = null;
            return false;
        }

        public bool HasModule<T>() where T : InteractorModule
        {
            return _moduleMap.ContainsKey(typeof(T));
        }

        public T GetModule<T>() where T : InteractorModule
        {
            return (T)_moduleMap[typeof(T)];
        }

        public bool TryGetModule<T>(out T module) where T : InteractorModule
        {
            if (_moduleMap.TryGetValue(typeof(T), out var moduleT))
            {
                module = (T)moduleT;
                return true;
            }
            module = null;
            return false;
        }
        #endregion
    }
}
