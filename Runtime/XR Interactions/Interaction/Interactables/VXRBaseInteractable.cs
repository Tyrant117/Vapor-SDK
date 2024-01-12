using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.XR.CoreUtils.Bindings.Variables;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Utilities;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace VaporXR
{
    /// <summary>
    /// Abstract base class from which all interactable behaviors derive.
    /// This class hooks into the interaction system (via <see cref="VXRInteractionManager"/>) and provides base virtual methods for handling
    /// hover, selection, and focus.
    /// </summary>
    [SelectionBase]
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Interactables)]
    // ReSharper disable once InconsistentNaming
    public abstract class VXRBaseInteractable : MonoBehaviour, IXRActivateInteractable, IXRHoverInteractable, IXRSelectInteractable, IXRFocusInteractable, IXRInteractionStrengthInteractable,
        IXROverridesGazeAutoSelect
    {
        private const float InteractionStrengthHover = 0f;
        private const float InteractionStrengthSelect = 1f;

        /// <summary>
        /// Options for how to process and perform movement of an Interactable.
        /// </summary>
        /// <remarks>
        /// Each method of movement has tradeoffs, and different values may be more appropriate
        /// for each type of Interactable object in a project.
        /// </remarks>
        /// <seealso cref="VXRGrabInteractable.movementType"/>
        public enum MovementType
        {
            /// <summary>
            /// Move the Interactable object by setting the velocity and angular velocity of the Rigidbody.
            /// Use this if you don't want the object to be able to move through other Colliders without a Rigidbody
            /// as it follows the Interactor, however with the tradeoff that it can appear to lag behind
            /// and not move as smoothly as <see cref="Instantaneous"/>.
            /// </summary>
            /// <remarks>
            /// Unity sets the velocity values during the FixedUpdate function. This Interactable will move at the
            /// framerate-independent interval of the Physics update, which may be slower than the Update rate.
            /// If the Rigidbody is not set to use interpolation or extrapolation, as the Interactable
            /// follows the Interactor, it may not visually update position each frame and be a slight distance
            /// behind the Interactor or controller due to the difference between the Physics update rate
            /// and the render update rate.
            /// </remarks>
            /// <seealso cref="Rigidbody.velocity"/>
            /// <seealso cref="Rigidbody.angularVelocity"/>
            VelocityTracking,

            /// <summary>
            /// Move the Interactable object by moving the kinematic Rigidbody towards the target position and orientation.
            /// Use this if you want to keep the visual representation synchronized to match its Physics state,
            /// and if you want to allow the object to be able to move through other Colliders without a Rigidbody
            /// as it follows the Interactor.
            /// </summary>
            /// <remarks>
            /// Unity will call the movement methods during the FixedUpdate function. This Interactable will move at the
            /// framerate-independent interval of the Physics update, which may be slower than the Update rate.
            /// If the Rigidbody is not set to use interpolation or extrapolation, as the Interactable
            /// follows the Interactor, it may not visually update position each frame and be a slight distance
            /// behind the Interactor or controller due to the difference between the Physics update rate
            /// and the render update rate. Collisions will be more accurate as compared to <see cref="Instantaneous"/>
            /// since with this method, the Rigidbody will be moved by settings its internal velocity rather than
            /// instantly teleporting to match the Transform pose.
            /// </remarks>
            /// <seealso cref="Rigidbody.MovePosition"/>
            /// <seealso cref="Rigidbody.MoveRotation"/>
            Kinematic,

            /// <summary>
            /// Move the Interactable object by setting the position and rotation of the Transform every frame.
            /// Use this if you want the visual representation to be updated each frame, minimizing latency,
            /// however with the tradeoff that it will be able to move through other Colliders without a Rigidbody
            /// as it follows the Interactor.
            /// </summary>
            /// <remarks>
            /// Unity will set the Transform values each frame, which may be faster than the framerate-independent
            /// interval of the Physics update. The Collider of the Interactable object may be a slight distance
            /// behind the visual as it follows the Interactor due to the difference between the Physics update rate
            /// and the render update rate. Collisions will not be computed as accurately as <see cref="Kinematic"/>
            /// since with this method, the Rigidbody will be forced to instantly teleport poses to match the Transform pose
            /// rather than moving the Rigidbody through setting its internal velocity.
            /// </remarks>
            /// <seealso cref="Transform.position"/>
            /// <seealso cref="Transform.rotation"/>
            Instantaneous,
        }

        /// <summary>
        /// Options for how to calculate an Interactable distance to a location in world space.
        /// </summary>
        /// <seealso cref="VXRBaseInteractable.DistanceCalculationMode"/>
        public enum DistanceCalculationModeType
        {
            /// <summary>
            /// Calculates the distance using the Interactable's transform position.
            /// This option has low performance cost, but it may have low distance calculation accuracy for some objects.
            /// </summary>
            TransformPosition,

            /// <summary>
            /// Calculates the distance using the Interactable's colliders list using the shortest distance to each.
            /// This option has moderate performance cost and should have moderate distance calculation accuracy for most objects.
            /// </summary>
            /// <seealso cref="XRInteractableUtility.TryGetClosestCollider"/>
            ColliderPosition,

            /// <summary>
            /// Calculates the distance using the Interactable's colliders list using the shortest distance to the closest point of each
            /// (either on the surface or inside the Collider).
            /// This option has high performance cost but high distance calculation accuracy.
            /// </summary>
            /// <remarks>
            /// The Interactable's colliders can only be of type <see cref="BoxCollider"/>, <see cref="SphereCollider"/>, <see cref="CapsuleCollider"/>, or convex <see cref="MeshCollider"/>.
            /// </remarks>
            /// <seealso cref="Collider.ClosestPoint"/>
            /// <seealso cref="XRInteractableUtility.TryGetClosestPointOnCollider"/>
            ColliderVolume,
        }
        
        private static readonly ProfilerMarker s_ProcessInteractionStrengthMarker = new("VXRI.ProcessInteractionStrength.Interactables");

        #region Inspector
        [SerializeField, FoldoutGroup("Components")]
        [RichTextTooltip("")]
        private VXRInteractionManager _interactionManager;
        [SerializeField, FoldoutGroup("Components")]
        [RichTextTooltip("")]
        private List<Collider> _colliders = new();
        
        [SerializeField, FoldoutGroup("Interaction")]
        [RichTextTooltip("")]
        private InteractionLayerMask _interactionLayers = 1;
        [SerializeField, FoldoutGroup("Interaction")]
        [RichTextTooltip("")]
        private DistanceCalculationModeType _distanceCalculationMode = DistanceCalculationModeType.ColliderPosition;
        [SerializeField, FoldoutGroup("Interaction")]
        [RichTextTooltip("")]
        private InteractableSelectMode _selectMode = InteractableSelectMode.Single;
        [SerializeField, FoldoutGroup("Interaction")]
        [RichTextTooltip("")]
        private InteractableFocusMode _focusMode = InteractableFocusMode.Single;
        
        [SerializeField, FoldoutGroup("Visuals")]
        [RichTextTooltip("")]
        private GameObject _customReticle;
        
        [SerializeField, FoldoutGroup("Gaze")]
        [RichTextTooltip("")]
        private bool _allowGazeInteraction;
        [SerializeField, FoldoutGroup("Gaze")]
        [RichTextTooltip("")]
        private bool _allowGazeSelect;
        [SerializeField, FoldoutGroup("Gaze")]
        [RichTextTooltip("")]
        private bool _overrideGazeTimeToSelect;
        [SerializeField, FoldoutGroup("Gaze")]
        [RichTextTooltip("")]
        private float _gazeTimeToSelect = 0.5f;
        [SerializeField, FoldoutGroup("Gaze")]
        [RichTextTooltip("")]
        private bool _overrideTimeToAutoDeselectGaze;
        [SerializeField, FoldoutGroup("Gaze")]
        [RichTextTooltip("")]
        private float _timeToAutoDeselectGaze = 3f;
        [SerializeField, FoldoutGroup("Gaze")]
        [RichTextTooltip("")]
        private bool _allowGazeAssistance;
        
        [SerializeField, FoldoutGroup("Hover Events", order: 100)]
        [RichTextTooltip("")]
        private HoverEnterEvent _firstHoverEntered = new();
        [SerializeField, FoldoutGroup("Hover Events")]
        [RichTextTooltip("")]
        private HoverExitEvent _lastHoverExited = new();
        [SerializeField, FoldoutGroup("Hover Events")]
        [RichTextTooltip("")]
        private HoverEnterEvent _hoverEntered = new();
        [SerializeField, FoldoutGroup("Hover Events")]
        [RichTextTooltip("")]
        private HoverExitEvent _hoverExited = new();
        
        [SerializeField, FoldoutGroup("Select Events", order: 101)]
        [RichTextTooltip("")]
        private SelectEnterEvent _firstSelectEntered = new();
        [SerializeField, FoldoutGroup("Select Events")]
        [RichTextTooltip("")]
        private SelectExitEvent _lastSelectExited = new();
        [SerializeField, FoldoutGroup("Select Events")]
        [RichTextTooltip("")]
        private SelectEnterEvent _selectEntered = new();
        [SerializeField, FoldoutGroup("Select Events")]
        [RichTextTooltip("")]
        private SelectExitEvent _selectExited = new();
        
        [SerializeField, FoldoutGroup("Focus Events", order: 102)]
        [RichTextTooltip("")]
        private FocusEnterEvent _firstFocusEntered = new();
        [SerializeField, FoldoutGroup("Focus Events")]
        [RichTextTooltip("")]
        private FocusExitEvent _lastFocusExited = new();
        [SerializeField, FoldoutGroup("Focus Events")]
        [RichTextTooltip("")]
        private FocusEnterEvent _focusEntered = new();
        [SerializeField, FoldoutGroup("Focus Events")]
        [RichTextTooltip("")]
        private FocusExitEvent _focusExited = new();
        
        [SerializeField, FoldoutGroup("Activation Events", order: 103)]
        [RichTextTooltip("")]
        private ActivateEvent _activated = new();
        [SerializeField, FoldoutGroup("Activation Events")]
        [RichTextTooltip("")]
        private DeactivateEvent _deactivated = new();
        
        [SerializeField, FoldoutGroup("Filters", order: 90)] [RequireInterface(typeof(IXRHoverFilter))]
        [RichTextTooltip("")]
        private List<Object> _startingHoverFilters = new();
        [SerializeField, FoldoutGroup("Filters")] [RequireInterface(typeof(IXRSelectFilter))]
        [RichTextTooltip("")]
        private List<Object> _startingSelectFilters = new();
        [SerializeField, FoldoutGroup("Filters")] [RequireInterface(typeof(IXRInteractionStrengthFilter))]
        [RichTextTooltip("")]
        private List<Object> _startingInteractionStrengthFilters = new();
        #endregion

        #region Properties
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> that this Interactable will communicate with (will find one if <see langword="null"/>).
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

        /// <summary>
        /// (Read Only) Colliders to use for interaction with this Interactable (if empty, will use any child Colliders).
        /// </summary>
        public List<Collider> colliders => _colliders;

        /// <summary>
        /// Allows interaction with Interactors whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.
        /// </summary>
        /// <seealso cref="VXRBaseInteractor.InteractionLayers"/>
        /// <seealso cref="IsHoverableBy(VXRBaseInteractor)"/>
        /// <seealso cref="IsSelectableBy(VXRBaseInteractor)"/>
        /// <inheritdoc />
        public InteractionLayerMask interactionLayers { get => _interactionLayers; set => _interactionLayers = value; }

        /// <summary>
        /// Specifies how this Interactable calculates its distance to a location, either using its Transform position, Collider
        /// position or Collider volume.
        /// </summary>
        /// <seealso cref="GetDistance"/>
        /// <seealso cref="colliders"/>
        /// <seealso cref="DistanceCalculationModeType"/>
        public DistanceCalculationModeType DistanceCalculationMode { get => _distanceCalculationMode; set => _distanceCalculationMode = value; }

        /// <inheritdoc />
        public InteractableSelectMode selectMode { get => _selectMode; set => _selectMode = value; }
        
        /// <inheritdoc />
        public InteractableFocusMode focusMode { get => _focusMode; set => _focusMode = value; }

        /// <summary>
        /// The reticle that appears at the end of the line when valid.
        /// </summary>
        public GameObject CustomReticle { get => _customReticle; set => _customReticle = value; }
        
        /// <summary>
        /// Enables interaction with <see cref="VXRGazeInteractor"/>.
        /// </summary>
        public bool AllowGazeInteraction { get => _allowGazeInteraction; set => _allowGazeInteraction = value; }
        
        /// <summary>
        /// Enables <see cref="VXRGazeInteractor"/> to select this <see cref="VXRBaseInteractable"/>.
        /// </summary>
        /// <seealso cref="VXRRayInteractor.HoverToSelect"/>
        public bool AllowGazeSelect { get => _allowGazeSelect; set => _allowGazeSelect = value; }
        
        /// <inheritdoc />
        public bool overrideGazeTimeToSelect { get => _overrideGazeTimeToSelect; set => _overrideGazeTimeToSelect = value; }
        
        /// <inheritdoc />
        public float gazeTimeToSelect { get => _gazeTimeToSelect; set => _gazeTimeToSelect = value; }
        
        /// <inheritdoc />
        public bool overrideTimeToAutoDeselectGaze { get => _overrideTimeToAutoDeselectGaze; set => _overrideTimeToAutoDeselectGaze = value; }
        
        /// <inheritdoc />
        public float timeToAutoDeselectGaze { get => _timeToAutoDeselectGaze; set => _timeToAutoDeselectGaze = value; }
        
        /// <summary>
        /// Enables gaze assistance with this interactable.
        /// </summary>
        public bool AllowGazeAssistance { get => _allowGazeAssistance; set => _allowGazeAssistance = value; }
        
        /// <inheritdoc />
        public HoverEnterEvent firstHoverEntered { get => _firstHoverEntered; set => _firstHoverEntered = value; }

        /// <inheritdoc />
        public HoverExitEvent lastHoverExited { get => _lastHoverExited; set => _lastHoverExited = value; }
        
        /// <inheritdoc />
        public HoverEnterEvent hoverEntered { get => _hoverEntered; set => _hoverEntered = value; }
        
        /// <inheritdoc />
        public HoverExitEvent hoverExited { get => _hoverExited; set => _hoverExited = value; }

        /// <inheritdoc />
        public SelectEnterEvent firstSelectEntered { get => _firstSelectEntered; set => _firstSelectEntered = value; }
        
        /// <inheritdoc />
        public SelectExitEvent lastSelectExited { get => _lastSelectExited; set => _lastSelectExited = value; }
        
        /// <inheritdoc />
        public SelectEnterEvent selectEntered { get => _selectEntered; set => _selectEntered = value; }
        
        /// <inheritdoc />
        public SelectExitEvent selectExited { get => _selectExited; set => _selectExited = value; }
        
        /// <inheritdoc />
        public FocusEnterEvent firstFocusEntered { get => _firstFocusEntered; set => _firstFocusEntered = value; }

        /// <inheritdoc />
        public FocusExitEvent lastFocusExited { get => _lastFocusExited; set => _lastFocusExited = value; }

        /// <inheritdoc />
        public FocusEnterEvent focusEntered { get => _focusEntered; set => _focusEntered = value; }

        /// <inheritdoc />
        public FocusExitEvent focusExited { get => _focusExited; set => _focusExited = value; }

        /// <inheritdoc />
        public ActivateEvent activated { get => _activated; set => _activated = value; }
        
        /// <inheritdoc />
        public DeactivateEvent deactivated { get => _deactivated; set => _deactivated = value; }

        private readonly HashSetList<VXRBaseInteractor> _interactorsHovering = new();
        /// <inheritdoc />
        public List<VXRBaseInteractor> interactorsHovering => (List<VXRBaseInteractor>)_interactorsHovering.AsList();

        /// <inheritdoc />
        public bool isHovered => _interactorsHovering.Count > 0;

        private readonly HashSetList<VXRBaseInteractor> _interactorsSelecting = new();

        /// <inheritdoc />
        public List<VXRBaseInteractor> interactorsSelecting => (List<VXRBaseInteractor>)_interactorsSelecting.AsList();

        /// <inheritdoc />
        public VXRBaseInteractor firstInteractorSelecting { get; private set; }

        /// <inheritdoc />
        public bool isSelected => _interactorsSelecting.Count > 0;

        private readonly HashSetList<IXRInteractionGroup> _interactionGroupsFocusing = new();

        /// <inheritdoc />
        public List<IXRInteractionGroup> interactionGroupsFocusing => (List<IXRInteractionGroup>)_interactionGroupsFocusing.AsList();

        /// <inheritdoc />
        public IXRInteractionGroup firstInteractionGroupFocusing { get; private set; }

        /// <inheritdoc />
        public bool isFocused => _interactionGroupsFocusing.Count > 0;

        /// <inheritdoc />
        public bool canFocus => _focusMode != InteractableFocusMode.None;
        
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
        public List<Object> StartingHoverFilters { get => _startingHoverFilters; set => _startingHoverFilters = value; }

        private readonly ExposedRegistrationList<IXRHoverFilter> _hoverFilters = new() { BufferChanges = false };
        /// <summary>
        /// The list of hover filters in this object.
        /// Used as additional hover validations for this Interactable.
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
        public List<Object> StartingSelectFilters { get => _startingSelectFilters; set => _startingSelectFilters = value; }

        private readonly ExposedRegistrationList<IXRSelectFilter> _selectFilters = new() { BufferChanges = false };
        /// <summary>
        /// The list of select filters in this object.
        /// Used as additional select validations for this Interactable.
        /// </summary>
        /// <remarks>
        /// While processing select filters, all changes to this list don't have an immediate effect. Theses changes are
        /// buffered and applied when the processing is finished.
        /// Calling <see cref="IXRFilterList{T}.MoveTo"/> in this list will throw an exception when this list is being processed.
        /// </remarks>
        /// <seealso cref="ProcessSelectFilters"/>
        public IXRFilterList<IXRSelectFilter> SelectFilters => _selectFilters;
        

        /// <summary>
        /// The interaction strength filters that this object uses to automatically populate the <see cref="InteractionStrengthFilters"/> List at
        /// startup (optional, may be empty).
        /// All objects in this list should implement the <see cref="IXRInteractionStrengthFilter"/> interface.
        /// </summary>
        /// <remarks>
        /// To access and modify the select filters used after startup, the <see cref="InteractionStrengthFilters"/> List should
        /// be used instead.
        /// </remarks>
        /// <seealso cref="InteractionStrengthFilters"/>
        public List<Object> StartingInteractionStrengthFilters { get => _startingInteractionStrengthFilters; set => _startingInteractionStrengthFilters = value; }

        private readonly ExposedRegistrationList<IXRInteractionStrengthFilter> _interactionStrengthFilters = new() { BufferChanges = false };
        /// <summary>
        /// The list of interaction strength filters in this object.
        /// Used to modify the default interaction strength of an Interactor relative to this Interactable.
        /// This is useful for interactables that can be poked to report the depth of the poke interactor as a percentage
        /// while the poke interactor is hovering over this object.
        /// </summary>
        /// <remarks>
        /// While processing interaction strength filters, all changes to this list don't have an immediate effect. Theses changes are
        /// buffered and applied when the processing is finished.
        /// Calling <see cref="IXRFilterList{T}.MoveTo"/> in this list will throw an exception when this list is being processed.
        /// </remarks>
        /// <seealso cref="ProcessInteractionStrengthFilters"/>
        public IXRFilterList<IXRInteractionStrengthFilter> InteractionStrengthFilters => _interactionStrengthFilters;

        private readonly BindableVariable<float> _mLargestInteractionStrength = new();
        /// <inheritdoc />
        public IReadOnlyBindableVariable<float> largestInteractionStrength => _mLargestInteractionStrength;
        #endregion

        #region Fields
        private readonly Dictionary<VXRBaseInteractor, Pose> _attachPoseOnSelect = new();
        private readonly Dictionary<VXRBaseInteractor, Pose> _localAttachPoseOnSelect = new();
        private readonly Dictionary<VXRBaseInteractor, GameObject> _reticleCache = new();

        /// <summary>
        /// The set of hovered and/or selected interactors that supports returning a variable select input value,
        /// which is used as the pre-filtered interaction strength.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="VXRInputInteractor"/> as the type to get the select input value to use as the pre-filtered
        /// interaction strength.
        /// </remarks>
        private readonly HashSetList<VXRInputInteractor> _variableSelectInteractors = new();

        private readonly Dictionary<VXRBaseInteractor, float> _interactionStrengths = new();

        private VXRInteractionManager _registeredInteractionManager;
        #endregion

        #region Events
        /// <inheritdoc />
        public event Action<InteractableRegisteredEventArgs> registered;

        /// <inheritdoc />
        public event Action<InteractableUnregisteredEventArgs> unregistered;

        /// <summary>
        /// Overriding callback of this object's distance calculation.
        /// Use this to change the calculation performed in <see cref="GetDistance"/> without needing to create a derived class.
        /// <br />
        /// When a callback is assigned to this property, the <see cref="GetDistance"/> execution calls it to perform the
        /// distance calculation instead of using its default calculation (specified by <see cref="DistanceCalculationMode"/> in this base class).
        /// Assign <see langword="null"/> to this property to restore the default calculation.
        /// </summary>
        /// <remarks>
        /// The assigned callback will be invoked to calculate and return the distance information of the point on this
        /// Interactable (the first parameter) closest to the given location (the second parameter).
        /// The given location and returned distance information are in world space.
        /// </remarks>
        /// <seealso cref="GetDistance"/>
        /// <seealso cref="DistanceInfo"/>
        public Func<IXRInteractable, Vector3, DistanceInfo> GetDistanceOverride { get; set; }
        #endregion
        
        #region - Initialization -
        protected virtual void Awake()
        {
            // If no colliders were set, populate with children colliders
            if (_colliders.Count == 0)
            {
                GetComponentsInChildren(_colliders);
                // Skip any that are trigger colliders since these are usually associated with snap volumes.
                // If a user wants to use a trigger collider, they must serialize the reference manually.
                _colliders.RemoveAll(col => col.isTrigger);
            }

            // Setup the starting filters
            _hoverFilters.RegisterReferences(_startingHoverFilters, this);
            _selectFilters.RegisterReferences(_startingSelectFilters, this);
            _interactionStrengthFilters.RegisterReferences(_startingInteractionStrengthFilters, this);

            // Setup Interaction Manager
            FindCreateInteractionManager();
        }

        protected virtual void OnEnable()
        {
            FindCreateInteractionManager();
            RegisterWithInteractionManager();
        }

        protected virtual void OnDisable()
        {
            UnregisterWithInteractionManager();
        }

        private void FindCreateInteractionManager()
        {
            if (_interactionManager != null)
                return;

            _interactionManager = ComponentLocatorUtility<VXRInteractionManager>.FindOrCreateComponent();
        }

        private void RegisterWithInteractionManager()
        {
            if (_registeredInteractionManager == _interactionManager)
                return;

            UnregisterWithInteractionManager();

            if (_interactionManager != null)
            {
                _interactionManager.RegisterInteractable(this);
                _registeredInteractionManager = _interactionManager;
            }
        }

        private void UnregisterWithInteractionManager()
        {
            if (_registeredInteractionManager != null)
            {
                _registeredInteractionManager.UnregisterInteractable(this);
                _registeredInteractionManager = null;
            }
        }
        
        /// <inheritdoc />
        void IXRInteractable.OnRegistered(InteractableRegisteredEventArgs args) => OnRegistered(args);

        /// <inheritdoc />
        void IXRInteractable.OnUnregistered(InteractableUnregisteredEventArgs args) => OnUnregistered(args);
        
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when this Interactable is registered with it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that registered this Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.RegisterInteractable(IXRInteractable)"/>
        protected virtual void OnRegistered(InteractableRegisteredEventArgs args)
        {
            if (args.manager != _interactionManager)
                Debug.LogWarning($"An Interactable was registered with an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{_interactionManager}\" but was registered with \"{args.manager}\".", this);

            registered?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when this Interactable is unregistered from it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that unregistered this Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.UnregisterInteractable(IXRInteractable)"/>
        protected virtual void OnUnregistered(InteractableUnregisteredEventArgs args)
        {
            if (args.manager != _registeredInteractionManager)
                Debug.LogWarning($"An Interactable was unregistered from an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{_registeredInteractionManager}\" but was unregistered from \"{args.manager}\".", this);

            unregistered?.Invoke(args);
        }
        #endregion

        #region - Processing -
        /// <inheritdoc />
        public virtual void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
        }
        #endregion

        #region - Hover -
        /// <summary>
        /// Determines if a given Interactor can hover over this Interactable.
        /// </summary>
        /// <param name="interactor">Interactor to check for a valid hover state with.</param>
        /// <returns>Returns <see langword="true"/> if hovering is valid this frame. Returns <see langword="false"/> if not.</returns>
        /// <seealso cref="VXRBaseInteractor.CanHover"/>
        public virtual bool IsHoverableBy(VXRBaseInteractor interactor)
        {
            return _allowGazeInteraction || !(interactor is VXRGazeInteractor);
        }
        
        /// <summary>
        /// Determines whether this Interactable is currently being hovered by the Interactor.
        /// </summary>
        /// <param name="interactor">Interactor to check.</param>
        /// <returns>Returns <see langword="true"/> if this Interactable is currently being hovered by the Interactor.
        /// Otherwise, returns <seealso langword="false"/>.</returns>
        /// <remarks>
        /// In other words, returns whether <see cref="interactorsHovering"/> contains <paramref name="interactor"/>.
        /// </remarks>
        /// <seealso cref="interactorsHovering"/>
        public bool IsHovered(VXRBaseInteractor interactor) => _interactorsHovering.Contains(interactor);
        
        /// <inheritdoc />
        bool IXRHoverInteractable.IsHoverableBy(VXRBaseInteractor interactor)
        {
            return IsHoverableBy(interactor) && ProcessHoverFilters(interactor);
        }

        /// <inheritdoc />
        void IXRHoverInteractable.OnHoverEntering(HoverEnterEventArgs args) => OnHoverEntering(args);

        /// <inheritdoc />
        void IXRHoverInteractable.OnHoverEntered(HoverEnterEventArgs args) => OnHoverEntered(args);

        /// <inheritdoc />
        void IXRHoverInteractable.OnHoverExiting(HoverExitEventArgs args) => OnHoverExiting(args);

        /// <inheritdoc />
        void IXRHoverInteractable.OnHoverExited(HoverExitEventArgs args) => OnHoverExited(args);
        
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// right before the Interactor first initiates hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is initiating the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverEntered(HoverEnterEventArgs)"/>
        protected virtual void OnHoverEntering(HoverEnterEventArgs args)
        {
            if (_customReticle != null)
                AttachCustomReticle(args.interactorObject);

            var added = _interactorsHovering.Add(args.interactorObject);
            Debug.Assert(added, "An Interactable received a Hover Enter event for an Interactor that was already hovering over it.", this);

            if (args.interactorObject is VXRInputInteractor variableSelectInteractor)
                _variableSelectInteractors.Add(variableSelectInteractor);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interactor first initiates hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is initiating the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverExited(HoverExitEventArgs)"/>
        protected virtual void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (_interactorsHovering.Count == 1)
                _firstHoverEntered?.Invoke(args);

            _hoverEntered?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// right before the Interactor ends hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is ending the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverExited(HoverExitEventArgs)"/>
        protected virtual void OnHoverExiting(HoverExitEventArgs args)
        {
            if (_customReticle != null)
                RemoveCustomReticle(args.interactorObject);

            var removed = _interactorsHovering.Remove(args.interactorObject);
            Debug.Assert(removed, "An Interactable received a Hover Exit event for an Interactor that was not hovering over it.", this);

            if (_variableSelectInteractors.Count > 0 &&
                args.interactorObject is VXRInputInteractor variableSelectInteractor &&
                !IsSelected(variableSelectInteractor))
            {
                _variableSelectInteractors.Remove(variableSelectInteractor);
            }
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interactor ends hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is ending the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverEntered(HoverEnterEventArgs)"/>
        protected virtual void OnHoverExited(HoverExitEventArgs args)
        {
            if (_interactorsHovering.Count == 0)
                _lastHoverExited?.Invoke(args);

            _hoverExited?.Invoke(args);
        }
        
        /// <summary>
        /// Returns the processing value of the filters in <see cref="HoverFilters"/> for the given Interactor and this
        /// Interactable.
        /// </summary>
        /// <param name="interactor">The Interactor to be validated by the hover filters.</param>
        /// <returns>
        /// Returns <see langword="true"/> if all processed filters also return <see langword="true"/>, or if
        /// <see cref="HoverFilters"/> is empty. Otherwise, returns <see langword="false"/>.
        /// </returns>
        protected bool ProcessHoverFilters(VXRBaseInteractor interactor)
        {
            return XRFilterUtility.Process(_hoverFilters, interactor, this);
        }
        #endregion

        #region - Select -
        /// <summary>
        /// Determines if a given Interactor can select this Interactable.
        /// </summary>
        /// <param name="interactor">Interactor to check for a valid selection with.</param>
        /// <returns>Returns <see langword="true"/> if selection is valid this frame. Returns <see langword="false"/> if not.</returns>
        /// <seealso cref="VXRBaseInteractor.CanSelect"/>
        public virtual bool IsSelectableBy(VXRBaseInteractor interactor)
        {
            return (_allowGazeInteraction && _allowGazeSelect) || !(interactor is VXRGazeInteractor);
        }
        
        /// <summary>
        /// Determines whether this Interactable is currently being selected by the Interactor.
        /// </summary>
        /// <param name="interactor">Interactor to check.</param>
        /// <returns>Returns <see langword="true"/> if this Interactable is currently being selected by the Interactor.
        /// Otherwise, returns <seealso langword="false"/>.</returns>
        /// <remarks>
        /// In other words, returns whether <see cref="interactorsSelecting"/> contains <paramref name="interactor"/>.
        /// </remarks>
        /// <seealso cref="interactorsSelecting"/>
        public bool IsSelected(VXRBaseInteractor interactor) => _interactorsSelecting.Contains(interactor);
        
        /// <inheritdoc />
        bool IXRSelectInteractable.IsSelectableBy(VXRBaseInteractor interactor)
        {
            return IsSelectableBy(interactor) && ProcessSelectFilters(interactor);
        }
        
        /// <inheritdoc />
        void IXRSelectInteractable.OnSelectEntering(SelectEnterEventArgs args) => OnSelectEntering(args);

        /// <inheritdoc />
        void IXRSelectInteractable.OnSelectEntered(SelectEnterEventArgs args) => OnSelectEntered(args);

        /// <inheritdoc />
        void IXRSelectInteractable.OnSelectExiting(SelectExitEventArgs args) => OnSelectExiting(args);

        /// <inheritdoc />
        void IXRSelectInteractable.OnSelectExited(SelectExitEventArgs args) => OnSelectExited(args);
        
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method right
        /// before the Interactor first initiates selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is initiating the selection.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectEntered(SelectEnterEventArgs)"/>
        protected virtual void OnSelectEntering(SelectEnterEventArgs args)
        {
            var added = _interactorsSelecting.Add(args.interactorObject);
            Debug.Assert(added, "An Interactable received a Select Enter event for an Interactor that was already selecting it.", this);

            if (args.interactorObject is VXRInputInteractor variableSelectInteractor)
                _variableSelectInteractors.Add(variableSelectInteractor);

            if (_interactorsSelecting.Count == 1)
                firstInteractorSelecting = args.interactorObject;

            CaptureAttachPose(args.interactorObject);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interactor first initiates selection of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is initiating the selection.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectExited(SelectExitEventArgs)"/>
        protected virtual void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (_interactorsSelecting.Count == 1)
                _firstSelectEntered?.Invoke(args);

            _selectEntered?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// right before the Interactor ends selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is ending the selection.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectExited(SelectExitEventArgs)"/>
        protected virtual void OnSelectExiting(SelectExitEventArgs args)
        {
            var removed = _interactorsSelecting.Remove(args.interactorObject);
            Debug.Assert(removed, "An Interactable received a Select Exit event for an Interactor that was not selecting it.", this);

            if (_variableSelectInteractors.Count > 0 &&
                args.interactorObject is VXRInputInteractor variableSelectInteractor &&
                !IsHovered(variableSelectInteractor))
            {
                _variableSelectInteractors.Remove(variableSelectInteractor);
            }
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interactor ends selection of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is ending the selection.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectEntered(SelectEnterEventArgs)"/>
        protected virtual void OnSelectExited(SelectExitEventArgs args)
        {
            if (_interactorsSelecting.Count == 0)
                _lastSelectExited?.Invoke(args);

            _selectExited?.Invoke(args);

            // The dictionaries are pruned so that they don't infinitely grow in size as selections are made.
            if (_interactorsSelecting.Count == 0)
            {
                firstInteractorSelecting = null;
                _attachPoseOnSelect.Clear();
                _localAttachPoseOnSelect.Clear();
            }
        }
        
        /// <summary>
        /// Returns the processing value of the filters in <see cref="SelectFilters"/> for the given Interactor and this
        /// Interactable.
        /// </summary>
        /// <param name="interactor">The Interactor to be validated by the select filters.</param>
        /// <returns>
        /// Returns <see langword="true"/> if all processed filters also return <see langword="true"/>, or if
        /// <see cref="SelectFilters"/> is empty. Otherwise, returns <see langword="false"/>.
        /// </returns>
        protected bool ProcessSelectFilters(VXRBaseInteractor interactor)
        {
            return XRFilterUtility.Process(_selectFilters, interactor, this);
        }
        #endregion

        #region - Focus -
        /// <inheritdoc />
        void IXRFocusInteractable.OnFocusEntering(FocusEnterEventArgs args) => OnFocusEntering(args);

        /// <inheritdoc />
        void IXRFocusInteractable.OnFocusEntered(FocusEnterEventArgs args) => OnFocusEntered(args);

        /// <inheritdoc />
        void IXRFocusInteractable.OnFocusExiting(FocusExitEventArgs args) => OnFocusExiting(args);

        /// <inheritdoc />
        void IXRFocusInteractable.OnFocusExited(FocusExitEventArgs args) => OnFocusExited(args);
        
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method right
        /// before the Interaction group first gains focus of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interaction group that is initiating focus.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnFocusEntered(FocusEnterEventArgs)"/>
        protected virtual void OnFocusEntering(FocusEnterEventArgs args)
        {
            var added = _interactionGroupsFocusing.Add(args.interactionGroup);
            Debug.Assert(added, "An Interactable received a Focus Enter event for an Interaction group that was already focusing it.", this);

            if (_interactionGroupsFocusing.Count == 1)
                firstInteractionGroupFocusing = args.interactionGroup;
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interaction group first gains focus of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interaction group that is initiating the focus.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnFocusExited(FocusExitEventArgs)"/>
        protected virtual void OnFocusEntered(FocusEnterEventArgs args)
        {
            if (_interactionGroupsFocusing.Count == 1)
                _firstFocusEntered?.Invoke(args);

            _focusEntered?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// right before the Interaction group loses focus of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interaction group that is losing focus.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnFocusExited(FocusExitEventArgs)"/>
        protected virtual void OnFocusExiting(FocusExitEventArgs args)
        {
            var removed = _interactionGroupsFocusing.Remove(args.interactionGroup);
            Debug.Assert(removed, "An Interactable received a Focus Exit event for an Interaction group that did not have focus of it.", this);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when the Interaction group loses focus of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interaction group that is losing focus.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnFocusEntered(FocusEnterEventArgs)"/>
        protected virtual void OnFocusExited(FocusExitEventArgs args)
        {
            if (_interactionGroupsFocusing.Count == 0)
                _lastFocusExited?.Invoke(args);

            _focusExited?.Invoke(args);

            if (_interactionGroupsFocusing.Count == 0)
                firstInteractionGroupFocusing = null;
        }
        #endregion

        #region - Activate -
        /// <inheritdoc />
        void IXRActivateInteractable.OnActivated(ActivateEventArgs args) => OnActivated(args);

        /// <inheritdoc />
        void IXRActivateInteractable.OnDeactivated(DeactivateEventArgs args) => OnDeactivated(args);
        
        /// <summary>
        /// <see cref="VXRInputInteractor"/> calls this method when the
        /// Interactor begins an activation event on this Interactable.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is sending the activate event.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnDeactivated"/>
        protected virtual void OnActivated(ActivateEventArgs args)
        {
            _activated?.Invoke(args);
        }

        /// <summary>
        /// <see cref="VXRInputInteractor"/> calls this method when the
        /// Interactor ends an activation event on this Interactable.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is sending the deactivate event.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnActivated"/>
        protected virtual void OnDeactivated(DeactivateEventArgs args)
        {
            _deactivated?.Invoke(args);
        }
        #endregion

        #region - Interaction -
        /// <inheritdoc />
        public float GetInteractionStrength(VXRBaseInteractor interactor)
        {
            if (_interactionStrengths.TryGetValue(interactor, out var interactionStrength))
                return interactionStrength;

            return 0f;
        }
        
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method to signal to update the interaction strength.
        /// </summary>
        /// <param name="updatePhase">The update phase during which this method is called.</param>
        /// <seealso cref="GetInteractionStrength"/>
        /// <seealso cref="IXRInteractionStrengthInteractable.ProcessInteractionStrength"/>
        public virtual void ProcessInteractionStrength(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            var maxInteractionStrength = 0f;

            using (s_ProcessInteractionStrengthMarker.Auto())
            {
                _interactionStrengths.Clear();

                // Select is checked before Hover to allow process to only be called once per interactor hovering and selecting
                // using the largest initial interaction strength.
                for (int i = 0, count = _interactorsSelecting.Count; i < count; ++i)
                {
                    var interactor = _interactorsSelecting[i];
                    if (interactor is VXRInputInteractor)
                        continue;

                    var interactionStrength = ProcessInteractionStrengthFilters(interactor, InteractionStrengthSelect);
                    _interactionStrengths[interactor] = interactionStrength;

                    maxInteractionStrength = Mathf.Max(maxInteractionStrength, interactionStrength);
                }

                for (int i = 0, count = _interactorsHovering.Count; i < count; ++i)
                {
                    var interactor = _interactorsHovering[i];
                    if (interactor is VXRInputInteractor || IsSelected(interactor))
                        continue;

                    var interactionStrength = ProcessInteractionStrengthFilters(interactor, InteractionStrengthHover);
                    _interactionStrengths[interactor] = interactionStrength;

                    maxInteractionStrength = Mathf.Max(maxInteractionStrength, interactionStrength);
                }

                for (int i = 0, count = _variableSelectInteractors.Count; i < count; ++i)
                {
                    var interactor = _variableSelectInteractors[i];

                    // Use the Select input value as the initial interaction strength.
                    // For interactors that use motion controller input, this is typically the analog trigger or grip press amount.
                    // Fall back to the default values for selected and hovered interactors in the case when the interactor
                    // is misconfigured and is missing the input wrapper or component reference.
                    var interactionStrength = interactor.SelectInput != null ? interactor.SelectInput.ReadValue() : IsSelected(interactor) ? InteractionStrengthSelect : InteractionStrengthHover;

                    interactionStrength = ProcessInteractionStrengthFilters(interactor, interactionStrength);
                    _interactionStrengths[interactor] = interactionStrength;

                    maxInteractionStrength = Mathf.Max(maxInteractionStrength, interactionStrength);
                }
            }

            // This is done outside of the ProfilerMarker since it could trigger user callbacks
            _mLargestInteractionStrength.Value = maxInteractionStrength;
        }
        
        /// <summary>
        /// Returns the processing value of the interaction strength filters in <see cref="InteractionStrengthFilters"/> for the given Interactor and this
        /// Interactable.
        /// </summary>
        /// <param name="interactor">The Interactor to process by the interaction strength filters.</param>
        /// <param name="interactionStrength">The interaction strength before processing.</param>
        /// <returns>Returns the modified interaction strength that is the result of passing the interaction strength through each filter.</returns>
        protected float ProcessInteractionStrengthFilters(VXRBaseInteractor interactor, float interactionStrength)
        {
            return XRFilterUtility.Process(_interactionStrengthFilters, interactor, this, interactionStrength);
        }
        #endregion

        #region - Posing -
        /// <inheritdoc />
        public virtual Transform GetAttachTransform(VXRBaseInteractor interactor)
        {
            return transform;
        }

        /// <inheritdoc />
        public Pose GetAttachPoseOnSelect(VXRBaseInteractor interactor)
        {
            return _attachPoseOnSelect.TryGetValue(interactor, out var pose) ? pose : Pose.identity;
        }

        /// <inheritdoc />
        public Pose GetLocalAttachPoseOnSelect(VXRBaseInteractor interactor)
        {
            return _localAttachPoseOnSelect.TryGetValue(interactor, out var pose) ? pose : Pose.identity;
        }
        
        /// <summary>
        /// Capture the current Attach Transform pose.
        /// This method is automatically called by Unity to capture the pose during the moment of selection.
        /// </summary>
        /// <param name="interactor">The specific Interactor as context to get the attachment point for.</param>
        /// <remarks>
        /// Unity automatically calls this method during <see cref="OnSelectEntering(SelectEnterEventArgs)"/>
        /// and should not typically need to be called by a user.
        /// </remarks>
        /// <seealso cref="GetAttachPoseOnSelect"/>
        /// <seealso cref="GetLocalAttachPoseOnSelect"/>
        /// <seealso cref="VXRBaseInteractor.CaptureAttachPose"/>
        protected void CaptureAttachPose(VXRBaseInteractor interactor)
        {
            var thisAttachTransform = GetAttachTransform(interactor);
            if (thisAttachTransform != null)
            {
                _attachPoseOnSelect[interactor] =
                    new Pose(thisAttachTransform.position, thisAttachTransform.rotation);
                _localAttachPoseOnSelect[interactor] =
                    new Pose(thisAttachTransform.localPosition, thisAttachTransform.localRotation);
            }
            else
            {
                _attachPoseOnSelect.Remove(interactor);
                _localAttachPoseOnSelect.Remove(interactor);
            }
        }
        #endregion

        #region - Visuals -
        /// <summary>
        /// Looks for the current custom reticle that is attached based on a specific Interactor.
        /// </summary>
        /// <param name="interactor">Interactor that is interacting with this Interactable.</param>
        /// <returns>Returns <see cref="GameObject"/> that represents the attached custom reticle.</returns>
        /// <seealso cref="AttachCustomReticle(VXRBaseInteractor)"/>
        public virtual GameObject GetCustomReticle(VXRBaseInteractor interactor)
        {
            if (_reticleCache.TryGetValue(interactor, out var reticle))
            {
                return reticle;
            }

            return null;
        }

        /// <summary>
        /// Attaches the custom reticle to the Interactor.
        /// </summary>
        /// <param name="interactor">Interactor that is interacting with this Interactable.</param>
        /// <remarks>
        /// If the custom reticle has an <see cref="IXRInteractableCustomReticle"/> component, this will call
        /// <see cref="IXRInteractableCustomReticle.OnReticleAttached"/> on it.
        /// </remarks>
        /// <seealso cref="RemoveCustomReticle(VXRBaseInteractor)"/>
        public virtual void AttachCustomReticle(VXRBaseInteractor interactor)
        {
            if (interactor == null)
                return;
            
            var interactorTransform = interactor.transform;

            // Try and find any attached reticle and swap it
            var reticleProvider = interactorTransform.GetComponent<IXRCustomReticleProvider>();
            if (reticleProvider != null)
            {
                if (_reticleCache.TryGetValue(interactor, out var prevReticle))
                {
                    Destroy(prevReticle);
                    _reticleCache.Remove(interactor);
                }

                if (_customReticle != null)
                {
                    var reticleInstance = Instantiate(_customReticle);
                    _reticleCache.Add(interactor, reticleInstance);
                    reticleProvider.AttachCustomReticle(reticleInstance);
                    var customReticleBehavior = reticleInstance.GetComponent<IXRInteractableCustomReticle>();
                    if (customReticleBehavior != null)
                        customReticleBehavior.OnReticleAttached(this, reticleProvider);
                }
            }
        }

        /// <summary>
        /// Removes the custom reticle from the Interactor.
        /// </summary>
        /// <param name="interactor">Interactor that is no longer interacting with this Interactable.</param>
        /// <remarks>
        /// If the custom reticle has an <see cref="IXRInteractableCustomReticle"/> component, this will call
        /// <see cref="IXRInteractableCustomReticle.OnReticleDetaching"/> on it.
        /// </remarks>
        /// <seealso cref="AttachCustomReticle(VXRBaseInteractor)"/>
        public virtual void RemoveCustomReticle(VXRBaseInteractor interactor)
        {
            if (interactor == null)
            {
                return;
            }

            var interactorTransform = interactor.transform;

            // Try and find any attached reticle and swap it
            var reticleProvider = interactorTransform.GetComponent<IXRCustomReticleProvider>();
            if (reticleProvider == null)
            {
                return;
            }

            if (!_reticleCache.TryGetValue(interactor, out var reticleInstance))
            {
                return;
            }

            var customReticleBehavior = reticleInstance.GetComponent<IXRInteractableCustomReticle>();
            if (customReticleBehavior != null)
            {
                customReticleBehavior.OnReticleDetaching();
            }

            Destroy(reticleInstance);
            _reticleCache.Remove(interactor);
            reticleProvider.RemoveCustomReticle();
        }
        #endregion

        #region - Helper -
        /// <inheritdoc />
        /// <remarks>
        /// This method calls the <see cref="GetDistance"/> method to perform the distance calculation.
        /// </remarks>
        public virtual float GetDistanceSqrToInteractor(VXRBaseInteractor interactor)
        {
            if (interactor == null)
            {
                return float.MaxValue;
            }
            
            var interactorAttachTransform = interactor.GetAttachTransform(this);
            if (interactorAttachTransform == null)
            {
                return float.MaxValue;
            }

            var interactorPosition = interactorAttachTransform.position;
            var distanceInfo = GetDistance(interactorPosition);
            return distanceInfo.distanceSqr;
        }
        
        /// <summary>
        /// Gets the distance from this Interactable to the given location.
        /// This method uses the calculation mode configured in <see cref="DistanceCalculationMode"/>.
        /// <br />
        /// This method can be overridden (without needing to subclass) by assigning a callback to <see cref="GetDistanceOverride"/>.
        /// To restore the previous calculation mode configuration, assign <see langword="null"/> to <see cref="GetDistanceOverride"/>.
        /// </summary>
        /// <param name="position">Location in world space to calculate the distance to.</param>
        /// <returns>Returns the distance information (in world space) from this Interactable to the given location.</returns>
        /// <remarks>
        /// This method is used by other methods and systems to calculate this Interactable distance to other objects and
        /// locations (<see cref="GetDistanceSqrToInteractor(VXRBaseInteractor)"/>).
        /// </remarks>
        public virtual DistanceInfo GetDistance(Vector3 position)
        {
            if (GetDistanceOverride != null)
            {
                return GetDistanceOverride(this, position);
            }

            switch (_distanceCalculationMode)
            {
                case DistanceCalculationModeType.TransformPosition:
                    var thisObjectPosition = transform.position;
                    var offset = thisObjectPosition - position;
                    var distanceInfo = new DistanceInfo
                    {
                        point = thisObjectPosition,
                        distanceSqr = offset.sqrMagnitude
                    };
                    return distanceInfo;

                case DistanceCalculationModeType.ColliderPosition:
                    XRInteractableUtility.TryGetClosestCollider(this, position, out distanceInfo);
                    return distanceInfo;

                case DistanceCalculationModeType.ColliderVolume:
                    XRInteractableUtility.TryGetClosestPointOnCollider(this, position, out distanceInfo);
                    return distanceInfo;

                default:
                    Debug.Assert(false, $"Unhandled {nameof(DistanceCalculationModeType)}={_distanceCalculationMode}.", this);
                    goto case DistanceCalculationModeType.TransformPosition;
            }
        }
        #endregion
    }
}
