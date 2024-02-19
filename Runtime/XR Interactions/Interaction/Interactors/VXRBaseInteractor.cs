using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.XR.CoreUtils;
using Unity.XR.CoreUtils.Bindings.Variables;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using Vapor.Utilities;
using VaporEvents;
using VaporInspector;
using VaporXR.Interactors;
using VaporXR.Utilities;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace VaporXR
{
    /// <summary>
    /// Abstract base class from which all interactor behaviors derive.
    /// This class hooks into the interaction system (via <see cref="VXRInteractionManager"/>) and provides base virtual methods for handling
    /// hover and selection.
    /// </summary>
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Interactors)]
    public class VXRBaseInteractor : MonoBehaviour, IXRGroupMember, IXRTargetPriorityInteractor, IVXRHoverInteractor, IVXRSelectInteractor
    {
        private static readonly ProfilerMarker s_ProcessInteractionStrengthMarker = new ProfilerMarker("XRI.ProcessInteractionStrength.Interactors");

        private const float InteractionStrengthHover = 0f;
        private const float InteractionStrengthSelect = 1f;
        
        #region Inspector
        [SerializeField, BoxGroup("Components")]
        private VXRInteractionManager _interactionManager;
        
        [SerializeField, FoldoutGroup("Interaction")]
        private InteractionLayerMask _interactionLayers = -1;
        [SerializeField, FoldoutGroup("Interaction")]
        private InteractorHandedness _handedness;
        [SerializeField, FoldoutGroup("Interaction")]
        private Transform _attachTransform;
        [SerializeField, FoldoutGroup("Interaction")]
        [RichTextTooltip("Whether to disable Interactor visuals <cls>VXRInteractorLineVisual</cls> when this Interactor" +
            " is part of an <itf>IXRInteractionGroup</itf> and is incapable of interacting due to active interaction" +
            " by another Interactor in the Group.")]
        private bool _disableVisualsWhenBlockedInGroup = true;
        
        [SerializeField, FoldoutGroup("Input")]
        private bool _keepSelectedTargetValid = true;
        [SerializeField, FoldoutGroup("Input")]
        private VXRBaseInteractable _startingSelectedInteractable;
        
        [SerializeField, FoldoutGroup("Filters", order: 90)]
        private XRBaseTargetFilter _startingTargetFilter;
        [SerializeField, FoldoutGroup("Filters")]
        [RequireInterface(typeof(IXRHoverFilter))]
        private List<Object> _startingHoverFilters = new();
        [SerializeField, FoldoutGroup("Filters")]
        [RequireInterface(typeof(IXRSelectFilter))]
        private List<Object> _startingSelectFilters = new();
        
        [SerializeField, FoldoutGroup("Events", order: 100)]
        private HoverEnterEvent _hoverEntered = new ();
        [SerializeField, FoldoutGroup("Events")]
        private HoverExitEvent _hoverExited = new();
        [SerializeField, FoldoutGroup("Events")]
        private SelectEnterEvent _selectEntered = new();
        [SerializeField, FoldoutGroup("Events")]
        private SelectExitEvent _selectExited = new();
        #endregion
        
        #region Properties
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

        public VXRCompositeInteractor Bridge => throw new NotImplementedException();

        /// <inheritdoc />
        public IXRInteractionGroup ContainingGroup { get; private set; }
        
        /// <summary>
        /// (Read Only) Allows interaction with Interactables whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.
        /// </summary>
        /// <seealso cref="IVXRInteractable.InteractionLayers"/>
        public InteractionLayerMask InteractionLayers
        {
            get => _interactionLayers;
            set => _interactionLayers = value;
        }
        
        /// <summary>
        /// Represents which hand or controller the interactor is associated with.
        /// </summary>
        public InteractorHandedness Handedness
        {
            get => _handedness;
            set => _handedness = value;
        }
        
        /// <summary>
        /// The <see cref="Transform"/> that is used as the attach point for Interactables.
        /// </summary>
        /// <remarks>
        /// Automatically instantiated and set in <see cref="Awake"/> if <see langword="null"/>.
        /// Setting this will not automatically destroy the previous object.
        /// </remarks>
        public Transform AttachTransform
        {
            get => _attachTransform;
            set => _attachTransform = value;
        }
        
        /// <summary>
        /// Whether to keep selecting an Interactable after initially selecting it even when it is no longer a valid target.
        /// </summary>
        /// <remarks>
        /// Return <see langword="true"/> to make the <see cref="VXRInteractionManager"/> retain the selection even if the
        /// Interactable is not contained within the list of valid targets. Return <see langword="false"/> to make
        /// the Interaction Manager clear the selection if it isn't within the list of valid targets.
        /// <br/>
        /// A common use for disabling this is for Ray Interactors used for teleportation to make the teleportation Interactable
        /// no longer selected when not currently pointing at it.
        /// </remarks>
        public bool KeepSelectedTargetValid
        {
            get => _keepSelectedTargetValid;
            set => _keepSelectedTargetValid = value;
        }
        
        /// <summary>
        /// Whether to disable Interactor visuals (such as <see cref="VXRInteractorLineVisual"/>) when this Interactor
        /// is part of an <see cref="IXRInteractionGroup"/> and is incapable of interacting due to active interaction
        /// by another Interactor in the Group.
        /// </summary>
        public bool DisableVisualsWhenBlockedInGroup { get => _disableVisualsWhenBlockedInGroup; set => _disableVisualsWhenBlockedInGroup = value; }
        
        /// <summary>
        /// The Interactable that this Interactor automatically selects at startup (optional, may be <see langword="null"/>).
        /// </summary>
        public VXRBaseInteractable StartingSelectedInteractable { get => _startingSelectedInteractable; set => _startingSelectedInteractable = value; }
        
        /// <summary>
        /// The Target Filter that this Interactor automatically links at startup (optional, may be <see langword="null"/>).
        /// </summary>
        /// <remarks>
        /// To modify the Target Filter after startup, the <see cref="TargetFilter"/> property should be used instead.
        /// </remarks>
        /// <seealso cref="TargetFilter"/>
        public XRBaseTargetFilter StartingTargetFilter
        {
            get => _startingTargetFilter;
            set => _startingTargetFilter = value;
        }

        private IXRTargetFilter _targetFilter;
        /// <summary>
        /// The Target Filter that this Interactor is linked to.
        /// </summary>
        /// <seealso cref="StartingTargetFilter"/>
        public IXRTargetFilter TargetFilter
        {
            get
            {
                if (_targetFilter is Object unityObj && unityObj == null)
                    return null;

                return _targetFilter;
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

        /// <summary>
        /// Defines whether this interactor allows hover events.
        /// </summary>
        /// <remarks>
        /// A hover exit event will still occur if this value is disabled while hovering.
        /// </remarks>
        public bool AllowHover { get; set; } = true;

        /// <summary>
        /// Defines whether this interactor allows select events.
        /// </summary>
        /// <remarks>
        /// A select exit event will still occur if this value is disabled while selecting.
        /// </remarks>
        public bool AllowSelect { get; set; } = true;

        /// <summary>
        /// Defines whether this interactor is performing a manual interaction or not.
        /// </summary>
        /// <seealso cref="StartManualInteraction(IVXRSelectInteractable)"/>
        /// <seealso cref="EndManualInteraction"/>
        public bool IsPerformingManualInteraction { get; private set; }

        private readonly HashSetList<IVXRHoverInteractable> _interactablesHovered = new();
        /// <summary>
        /// (Read Only) The list of Interactables that are currently being hovered over (may by empty).
        /// </summary>
        /// <remarks>
        /// You should treat this as a read only view of the list and should not modify it.
        /// It is exposed as a <see cref="List{T}"/> rather than an <see cref="IReadOnlyList{T}"/> to avoid GC Allocations
        /// when enumerating the list.
        /// </remarks>
        /// <seealso cref="HasHover"/>
        /// <seealso cref="IVXRHoverInteractable.InteractorsHovering"/>
        public List<IVXRHoverInteractable> InteractablesHovered => (List<IVXRHoverInteractable>)_interactablesHovered.AsList();
        
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
        /// <seealso cref="IVXRHoverInteractable.IsHovered"/>
        public bool HasHover => _interactablesHovered.Count > 0;

        private readonly HashSetList<IVXRSelectInteractable> _interactablesSelected = new();
        /// <summary>
        /// (Read Only) The list of Interactables that are currently being selected (may by empty).
        /// </summary>
        /// <remarks>
        /// This should be treated as a read only view of the list and should not be modified by external callers.
        /// It is exposed as a <see cref="List{T}"/> rather than an <see cref="IReadOnlyList{T}"/> to avoid GC Allocations
        /// when enumerating the list.
        /// </remarks>
        /// <seealso cref="HasSelection"/>
        /// <seealso cref="IVXRSelectInteractable.InteractorsSelecting"/>
        public List<IVXRSelectInteractable> InteractablesSelected => (List<IVXRSelectInteractable>)_interactablesSelected.AsList();
        
        /// <summary>
        /// (Read Only) The first Interactable selected since not having any selection.
        /// This Interactor may not currently be selecting the Interactable, which would be the case
        /// when it was released while multiple Interactables were selected.
        /// </summary>
        /// <seealso cref="IVXRSelectInteractable.FirstInteractorSelecting"/>
        public IVXRSelectInteractable FirstInteractableSelected { get; private set; }
        
        /// <summary>
        /// (Read Only) Indicates whether this Interactor is currently selecting an Interactable.
        /// </summary>
        /// <remarks>
        /// In other words, returns whether <see cref="InteractablesSelected"/> contains any Interactables.
        /// <example>
        /// <code>interactablesSelected.Count > 0</code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InteractablesSelected"/>
        /// <seealso cref="IVXRSelectInteractable.IsSelected"/>
        public bool HasSelection => _interactablesSelected.Count > 0;
        
        /// <summary>
        /// Determines if interactor is interacting with UGUI canvas.
        /// </summary>
        public bool IsInteractingWithUI { get; set; }
        
        /// <summary>
        /// The hover filters that this object uses to automatically populate the <see cref="HoverFilters"/> List at
        /// startup (optional, may be empty).
        /// All objects in this list should implement the <see cref="IXRHoverFilter"/> interface.
        /// </summary>
        /// <remarks>
        /// To access and modify the hover filters used after startup, the <see cref="HoverFilters"/> List should
        /// be used instead.
        /// </remarks>
        /// <seealso cref="HoverFilters"/>
        public List<Object> StartingHoverFilters
        {
            get => _startingHoverFilters;
            set => _startingHoverFilters = value;
        }

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
        
        /// <summary>
        /// The select filters that this object uses to automatically populate the <see cref="SelectFilters"/> List at
        /// startup (optional, may be empty).
        /// All objects in this list should implement the <see cref="IXRSelectFilter"/> interface.
        /// </summary>
        /// <remarks>
        /// To access and modify the select filters used after startup, the <see cref="SelectFilters"/> List should
        /// be used instead.
        /// </remarks>
        /// <seealso cref="SelectFilters"/>
        public List<Object> StartingSelectFilters
        {
            get => _startingSelectFilters;
            set => _startingSelectFilters = value;
        }

        private readonly ExposedRegistrationList<IXRSelectFilter> _selectFilters = new ExposedRegistrationList<IXRSelectFilter> { BufferChanges = false };
        /// <summary>
        /// The list of select filters in this object.
        /// Used as additional select validations for this Interactor.
        /// </summary>
        /// <remarks>
        /// While processing select filters, all changes to this list don't have an immediate effect. Theses changes are
        /// buffered and applied when the processing is finished.
        /// Calling <see cref="IXRFilterList{T}.MoveTo"/> in this list will throw an exception when this list is being processed.
        /// </remarks>
        /// <seealso cref="ProcessSelectFilters"/>
        public IXRFilterList<IXRSelectFilter> SelectFilters => _selectFilters;

        private readonly BindableVariable<float> _largestInteractionStrength = new BindableVariable<float>();
        /// <summary>
        /// The largest interaction strength value of all interactables this interactor is hovering or selecting.
        /// </summary>
        public IReadOnlyBindableVariable<float> LargestInteractionStrength => _largestInteractionStrength;
        
        /// <summary>
        /// (Read Only) Indicates whether this Interactor is in a state where it could hover.
        /// </summary>
        public virtual bool IsHoverActive => AllowHover;

        /// <summary>
        /// (Read Only) Indicates whether this Interactor is in a state where it could select.
        /// </summary>
        public virtual bool IsSelectActive => AllowSelect;
        
        /// <summary>
        /// Specifies how many Interactables should be monitored in the <see cref="TargetsForSelection"/>
        /// property.
        /// </summary>
        public virtual TargetPriorityMode TargetPriorityMode { get; set; }

        /// <summary>
        /// The Interactables with priority for selection in the current frame, some Interactables might be already selected.
        /// This list is sorted by priority (with highest priority first).
        /// How many Interactables appear in this list is configured by the <see cref="TargetPriorityMode"/> property.
        /// </summary>
        /// <remarks>
        /// Unity automatically clears and updates this list every frame if <see cref="TargetPriorityMode"/> has a
        /// value different from <see cref="TargetPriorityMode.None"/>, in this case a valid list must be returned.
        /// </remarks>
        public virtual List<IVXRSelectInteractable> TargetsForSelection { get; set; }
        
        /// <summary>
        /// (Read Only) Overriding movement type of the selected Interactable's movement.
        /// By default, this does not override the movement type.
        /// </summary>
        /// <remarks>
        /// You can use this to change the effective movement type of an Interactable for different
        /// Interactors. An example would be having an Interactable use <see cref="VXRBaseInteractable.MovementType.VelocityTracking"/>
        /// so it does not move through geometry with a Collider when interacting with it using a Ray or Direct Interactor,
        /// but have a Socket Interactor override the movement type to be <see cref="VXRBaseInteractable.MovementType.Instantaneous"/>
        /// for reduced movement latency.
        /// </remarks>
        /// <seealso cref="VXRGrabInteractable.movementType"/>
        public virtual MovementType? SelectedInteractableMovementTypeOverride => null;

        public VXRCompositeInteractor Composite => throw new NotImplementedException();

        public LogicalInputState LogicalSelectState => throw new NotImplementedException();

        public Transform AttachPoint => throw new NotImplementedException();

        public Func<(bool, bool, float)> SelectActive { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        Func<XRIneractionActiveState> IVXRSelectInteractor.SelectActive { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Func<bool> HoverActive { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        Func<MovementType> IVXRSelectInteractor.SelectedInteractableMovementTypeOverride { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        #endregion

        #region Fields
        private readonly Dictionary<IVXRSelectInteractable, Pose> _attachPoseOnSelect = new();
        private readonly Dictionary<IVXRSelectInteractable, Pose> _localAttachPoseOnSelect = new();
        private readonly HashSetList<IXRInteractionStrengthInteractable> _interactionStrengthInteractables = new();
        private readonly Dictionary<IVXRInteractable, float> _interactionStrengths = new();
        
        private IVXRSelectInteractable _manualInteractionInteractable;

        private VXRInteractionManager _registeredInteractionManager;

        private Transform _xrOriginTransform;
        private bool _hasXROrigin;
        private bool _failedToFindXROrigin;
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

        public event Action<HoverEnterEventArgs> HoverEntered;

        public event Action<HoverExitEventArgs> HoverExited;

        /// <summary>
        /// The event that is called when this Interactor begins selecting an Interactable.
        /// </summary>
        /// <remarks>
        /// The <see cref="SelectEnterEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="SelectExited"/>
        public event Action<SelectEnterEventArgs> SelectEntered;
        
        /// <summary>
        /// The event that is called when this Interactor ends selecting an Interactable.
        /// </summary>
        /// <remarks>
        /// The <see cref="SelectEnterEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="SelectEntered"/>
        public event Action<SelectExitEventArgs> SelectExited;
        public event Action<SelectEnterEventArgs> SelectEntering;
        public event Action<SelectExitEventArgs> SelectExiting;
        public event Action<HoverEnterEventArgs> HoverEntering;
        public event Action<HoverExitEventArgs> HoverExiting;
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

            _hoverFilters.RegisterReferences(_startingHoverFilters, this);
            _selectFilters.RegisterReferences(_startingSelectFilters, this);

            // Setup Interaction Manager
            FindCreateInteractionManager();
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
            if (args.manager != _interactionManager)
                Debug.LogWarning($"An Interactor was registered with an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{_interactionManager}\" but was registered with \"{args.manager}\".", this);

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
            if (args.manager != _registeredInteractionManager)
                Debug.LogWarning($"An Interactor was unregistered from an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{_registeredInteractionManager}\" but was unregistered from \"{args.manager}\".", this);

            Unregistered?.Invoke(args);
        }

        protected virtual void OnDisable()
        {
            UnregisterWithInteractionManager();
        }

        protected virtual void Start()
        {
            if (_interactionManager != null && _startingSelectedInteractable != null)
                _interactionManager.SelectEnter(this, _startingSelectedInteractable);
        }

        protected virtual void OnDestroy()
        {
            // Unlink this Interactor from the Target Filter
            TargetFilter?.Unlink(this);

            if (ContainingGroup != null && (!(ContainingGroup is Object unityObject) || unityObject != null))
                ContainingGroup.RemoveGroupMember(this);
        }
        #endregion

        #region - Processing -
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> or containing <see cref="IXRInteractionGroup"/> calls this method to
        /// update the Interactor before interaction events occur. Interactors should use this method to
        /// do tasks like determine their valid targets.
        /// </summary>
        /// <param name="updatePhase">The update phase this is called during.</param>
        /// <remarks>
        /// Please see the <see cref="VXRInteractionManager"/> and <see cref="XRInteractionUpdateOrder.UpdatePhase"/> documentation for more
        /// details on update order.
        /// </remarks>
        /// <seealso cref="XRInteractionUpdateOrder.UpdatePhase"/>
        public virtual void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> or containing <see cref="IXRInteractionGroup"/> calls this method to
        /// update the Interactor after interaction events occur.
        /// </summary>
        /// <param name="updatePhase">The update phase this is called during.</param>
        /// <remarks>
        /// Please see the <see cref="VXRInteractionManager"/> and <see cref="XRInteractionUpdateOrder.UpdatePhase"/> documentation for more
        /// details on update order.
        /// </remarks>
        /// <seealso cref="XRInteractionUpdateOrder.UpdatePhase"/>
        /// <seealso cref="IVXRInteractable.ProcessInteractable"/>
        public virtual void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
        }
        #endregion

        #region - Hovering -
        /// <summary>
        /// Determines if the Interactable is valid for hover this frame.
        /// </summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if the interactable can be hovered over this frame.</returns>
        /// <seealso cref="IVXRHoverInteractable.IsHoverableBy"/>
        public virtual bool CanHover(IVXRHoverInteractable interactable)
        {
            return ProcessHoverFilters(interactable);
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
        public bool IsHovering(IVXRHoverInteractable interactable) => _interactablesHovered.Contains(interactable);
        
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
        /// <seealso cref="IXRHoverInteractor.IsHovering"/>
        public bool IsHovering(IVXRInteractable interactable) => interactable is IVXRHoverInteractable hoverable && IsHovering(hoverable);
        
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
            Debug.Log($"{Handedness} Hand Hover Entering: {args.interactableObject}");

            var added = _interactablesHovered.Add(args.interactableObject);
            Debug.Assert(added, "An Interactor received a Hover Enter event for an Interactable that it was already hovering over.", this);

            if (args.interactableObject is IXRInteractionStrengthInteractable interactionStrengthInteractable)
            {
                _interactionStrengthInteractables.Add(interactionStrengthInteractable);
            }
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
            Debug.Log($"{Handedness} Hand Hover Entered: {args.interactableObject}");
            _hoverEntered?.Invoke(args);
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
            Debug.Log($"{Handedness} Hand Hover Exiting: {args.interactableObject}");
            var removed = _interactablesHovered.Remove(args.interactableObject);
            Debug.Assert(removed, "An Interactor received a Hover Exit event for an Interactable that it was not hovering over.", this);

            if (_interactionStrengthInteractables.Count > 0 &&
                args.interactableObject is IXRInteractionStrengthInteractable interactionStrengthInteractable &&
                !IsSelecting(interactionStrengthInteractable))
            {
                _interactionStrengthInteractables.Remove(interactionStrengthInteractable);
            }
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
            Debug.Log($"{Handedness} Hand Hover Exited: {args.interactableObject}");
            _hoverExited?.Invoke(args);
        }
        
        /// <summary>
        /// Returns the processing value of the filters in <see cref="HoverFilters"/> for this Interactor and the
        /// given Interactable.
        /// </summary>
        /// <param name="interactable">The Interactable to be validated by the hover filters.</param>
        /// <returns>
        /// Returns <see langword="true"/> if all processed filters also return <see langword="true"/>, or if
        /// <see cref="HoverFilters"/> is empty. Otherwise, returns <see langword="false"/>.
        /// </returns>
        protected bool ProcessHoverFilters(IVXRHoverInteractable interactable)
        {
            return XRFilterUtility.Process(_hoverFilters, this, interactable);
        }
        #endregion

        #region - Selection -
        /// <summary>
        /// Determines if the Interactable is valid for selection this frame.
        /// </summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if the Interactable can be selected this frame.</returns>
        /// <seealso cref="IVXRSelectInteractable.IsSelectableBy"/>
        public virtual bool CanSelect(IVXRSelectInteractable interactable)
        {
            return ProcessSelectFilters(interactable);
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
        public bool IsSelecting(IVXRSelectInteractable interactable) => _interactablesSelected.Contains(interactable);
        
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
        /// <seealso cref="IXRSelectInteractor.IsSelecting"/>
        protected bool IsSelecting(IVXRInteractable interactable) => interactable is IVXRSelectInteractable selectable && IsSelecting(selectable);
        
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

            if (args.InteractableObject is IXRInteractionStrengthInteractable interactionStrengthInteractable)
            {
                _interactionStrengthInteractables.Add(interactionStrengthInteractable);
            }

            if (_interactablesSelected.Count == 1)
            {
                FirstInteractableSelected = args.InteractableObject;
            }            

            CaptureAttachPose(args.InteractableObject);
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

            _selectEntered?.Invoke(args);
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
            Debug.Log($"{Handedness} Hand Select Exiting: {args.GetinteractableObject()}");
            var removed = _interactablesSelected.Remove(args.GetinteractableObject());
            Debug.Assert(removed, "An Interactor received a Select Exit event for an Interactable that it was not selecting.", this);

            if (_interactionStrengthInteractables.Count > 0 &&
                args.GetinteractableObject() is IXRInteractionStrengthInteractable interactionStrengthInteractable &&
                !IsHovering(interactionStrengthInteractable))
            {
                _interactionStrengthInteractables.Remove(interactionStrengthInteractable);
            }            
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
            Debug.Log($"{Handedness} Hand Select Exited: {args.GetinteractableObject()}");
            _selectExited?.Invoke(args);

            // The dictionaries are pruned so that they don't infinitely grow in size as selections are made.
            if (_interactablesSelected.Count == 0)
            {
                FirstInteractableSelected = null;
                _attachPoseOnSelect.Clear();
                _localAttachPoseOnSelect.Clear();
            }            
        }
        
        /// <summary>
        /// Returns the processing value of the filters in <see cref="SelectFilters"/> for this Interactor and the
        /// given Interactable.
        /// </summary>
        /// <param name="interactable">The Interactor to be validated by the select filters.</param>
        /// <returns>
        /// Returns <see langword="true"/> if all processed filters also return <see langword="true"/>, or if
        /// <see cref="SelectFilters"/> is empty. Otherwise, returns <see langword="false"/>.
        /// </returns>
        protected bool ProcessSelectFilters(IVXRSelectInteractable interactable)
        {
            return XRFilterUtility.Process(_selectFilters, this, interactable);
        }
        #endregion

        #region - Grouping -
        /// <inheritdoc />
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

        /// <inheritdoc />
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
        public float GetInteractionStrength(IVXRInteractable interactable)
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
                    if (interactable is IXRInteractionStrengthInteractable)
                        continue;

                    _interactionStrengths[interactable] = InteractionStrengthSelect;

                    maxInteractionStrength = InteractionStrengthSelect;
                }

                for (int i = 0, count = _interactablesHovered.Count; i < count; ++i)
                {
                    var interactable = _interactablesHovered[i];
                    if (interactable is IXRInteractionStrengthInteractable || IsSelecting(interactable))
                        continue;

                    _interactionStrengths[interactable] = InteractionStrengthHover;
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
        /// <seealso cref="VXRBaseInteractable.CaptureAttachPose"/>
        protected void CaptureAttachPose(IVXRSelectInteractable interactable)
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
        /// <seealso cref="IVXRSelectInteractable.GetAttachPoseOnSelect"/>
        public Pose GetAttachPoseOnSelect(IVXRSelectInteractable interactable)
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
        /// <seealso cref="IVXRSelectInteractable.GetLocalAttachPoseOnSelect"/>
        public Pose GetLocalAttachPoseOnSelect(IVXRSelectInteractable interactable)
        {
            return _localAttachPoseOnSelect.TryGetValue(interactable, out var pose) ? pose : Pose.identity;
        }

        /// <summary>
        /// Create a new child GameObject to use as the attach transform if one is not set.
        /// </summary>
        /// <seealso cref="AttachTransform"/>
        protected void CreateAttachTransform()
        {
            if (_attachTransform == null)
            {
                _attachTransform = new GameObject($"[{gameObject.name}] Attach").transform;
                _attachTransform.SetParent(transform, false);
                _attachTransform.localPosition = Vector3.zero;
                _attachTransform.localRotation = Quaternion.identity;
            }
        }
        
        /// <summary>
        /// Gets the <see cref="Transform"/> that is used as the attachment point for a given Interactable.
        /// </summary>
        /// <param name="interactable">The specific Interactable as context to get the attachment point for.</param>
        /// <returns>Returns the attachment point <see cref="Transform"/>.</returns>
        /// <seealso cref="IVXRInteractable.GetAttachTransform"/>
        /// <remarks>
        /// This should typically return the Transform of a child GameObject or the <see cref="transform"/> itself.
        /// </remarks>
        public virtual Transform GetAttachTransform(IVXRInteractable interactable)
        {
            return _attachTransform != null ? _attachTransform : transform;
        }
        #endregion

        #region - Manual Interaction -
        /// <summary>
        /// Manually initiate selection of an Interactable.
        /// </summary>
        /// <param name="interactable">Interactable that is being selected.</param>
        /// <seealso cref="EndManualInteraction"/>
        public virtual void StartManualInteraction(IVXRSelectInteractable interactable)
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
        /// <seealso cref="StartManualInteraction(IVXRSelectInteractable)"/>
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

        #region - Helpers -
        /// <summary>
        /// Attempts to locate and return the XR Origin reference frame for the interactor.
        /// </summary>
        /// <seealso cref="XROrigin"/>
        public bool TryGetXROrigin(out Transform origin)
        {
            if (_hasXROrigin)
            {
                origin = _xrOriginTransform;
                return true;
            }

            if (!_failedToFindXROrigin)
            {
                var xrOrigin = GetComponentInParent<XROrigin>();
                if (xrOrigin != null)
                {
                    var originGo = xrOrigin.Origin;
                    if (originGo != null)
                    {
                        _xrOriginTransform = originGo.transform;
                        _hasXROrigin = true;
                        origin = _xrOriginTransform;
                        return true;
                    }
                }
                _failedToFindXROrigin = true;
            }
            origin = null;
            return false;
        }
        
        /// <summary>
        /// Retrieve the list of Interactables that this Interactor could possibly interact with this frame.
        /// This list is sorted by priority (with highest priority first).
        /// </summary>
        /// <param name="targets">The results list to populate with Interactables that are valid for selection or hover.</param>
        /// <remarks>
        /// When implementing this method, Unity expects you to clear <paramref name="targets"/> before adding to it.
        /// </remarks>
        public virtual void GetValidTargets(List<IVXRInteractable> targets)
        {
        }

        public bool TryGetSelectInteractor(out IVXRSelectInteractor interactor)
        {
            throw new NotImplementedException();
        }

        public bool TryGetHoverInteractor(out IVXRHoverInteractor interactor)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
