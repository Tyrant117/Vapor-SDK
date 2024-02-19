using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.XR.CoreUtils.Bindings.Variables;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Interactables;
using VaporXR.Interactors;
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
    public abstract class VXRBaseInteractable : MonoBehaviour, IXRActivateInteractable, IVXRHoverInteractable, IVXRSelectInteractable, IXRFocusInteractable, IXRInteractionStrengthInteractable,
        IXROverridesGazeAutoSelect
    {
        private const float InteractionStrengthHover = 0f;
        private const float InteractionStrengthSelect = 1f;        
        
        private static readonly ProfilerMarker s_ProcessInteractionStrengthMarker = new("VXRI.ProcessInteractionStrength.Interactables");

        #region Inspector
        [BoxGroup("Components"), SerializeField]
        [RichTextTooltip("The <cls>VXRInteractionManager</cls> that this Interactable will communicate with (will find one if <lw>null</lw>).")]
        private VXRInteractionManager _interactionManager;
        [BoxGroup("Components"), SerializeField]
        [RichTextTooltip("Colliders to use for interaction with this Interactable (if empty, will use any child Colliders).")]
        private List<Collider> _colliders = new();
        
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("Allows interaction with Interactors whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.")]
        private InteractionLayerMask _interactionLayers = 1;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("Indicates whether this interactable can be hovered by an interactor.")]
        private bool _canHover = true;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("Indicates whether this interactable can be selected by an interactor.")]
        private bool _canSelect;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("Indicates whether this interactable can be activated by an interactor.")]
        private bool _canActivate;

        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("")]
        private DistanceCalculationModeType _distanceCalculationMode = DistanceCalculationModeType.ColliderPosition;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("")]
        private InteractableSelectMode _selectMode = InteractableSelectMode.Single;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("")]
        private InteractableFocusMode _focusMode = InteractableFocusMode.Single;               
        
        [TitleGroup("Interaction/Gaze", title: "Gaze", order: 100), SerializeField]
        [RichTextTooltip("Enables interaction with <cls>VXRGazeInteractor</cls>.")]
        private bool _allowGazeInteraction;
        [TitleGroup("Interaction/Gaze"), SerializeField, ShowIf("%_allowGazeInteraction")]
        [RichTextTooltip("Enables gaze assistance with this interactable.")]
        private bool _allowGazeAssistance;
        [TitleGroup("Interaction/Gaze"), SerializeField, ShowIf("%_allowGazeInteraction")]
        [RichTextTooltip("Enables <cls>VXRGazeInteractor</cls> to select this <cls>VXRBaseInteractable</cls>.")]
        private bool _allowGazeSelect;
        [TitleGroup("Interaction/Gaze"), SerializeField, ShowIf("%_allowGazeSelect")]
        [RichTextTooltip("Enables this interactable to override the <cls>VXRRayInteractor</cls><mth>.HoverTimeToSelect</mth> on a <cls>VXRGazeInteractor</cls>.")]
        private bool _overrideGazeTimeToSelect;
        [TitleGroup("Interaction/Gaze"), SerializeField, ShowIf("%_allowGazeSelect"), Suffix("s")]
        [RichTextTooltip("Number of seconds for which an <cls>VXRGazeInteractor</cls> must hover over this interactable to select it if <cls>VXRRayInteractor</cls><mth>.HoverToSelect</mth> is enabled.")]
        private float _gazeTimeToSelect = 0.5f;
        [TitleGroup("Interaction/Gaze"), SerializeField, ShowIf("%_allowGazeSelect")]
        [RichTextTooltip("Enables this interactable to override the <cls>VXRRayInteractor</cls><mth>.TimeToAutoDeselect</mth> on a <cls>VXRGazeInteractor</cls>.")]
        private bool _overrideTimeToAutoDeselectGaze;
        [TitleGroup("Interaction/Gaze"), SerializeField, ShowIf("%_allowGazeSelect"), Suffix("s")]
        [RichTextTooltip("Number of seconds that the interactable will remain selected by a <cls>VXRGazeInteractor</cls> before being automatically deselected if <mth>OverrideTimeToAutoDeselectGaze</mth> is <lw>true</lw>.")]
        private float _timeToAutoDeselectGaze = 3f;

        [FoldoutGroup("Visuals"), SerializeField]
        [RichTextTooltip("The reticle that appears at the end of the line when valid.")]
        private GameObject _customReticle;        
        
        [FoldoutGroup("Filters", order: 90), SerializeField, RequireInterface(typeof(IXRHoverFilter)), ShowIf("$CanHover")] 
        [RichTextTooltip("The hover filters that this object uses to automatically populate the <mth>HoverFilters</mth> List at startup (optional, may be empty)." +
            "\nAll objects in this list should implement the <itf>IXRHoverFilter</itf> interface.")]
        private List<Object> _startingHoverFilters = new();
        [FoldoutGroup("Filters"), SerializeField, RequireInterface(typeof(IXRSelectFilter)), ShowIf("$CanSelect")]
        [RichTextTooltip("The select filters that this object uses to automatically populate the <mth>SelectFilters</mth> List at startup (optional, may be empty)." +
            "\nAll objects in this list should implement the <itf>IXRSelectFilter</itf> interface.")]
        private List<Object> _startingSelectFilters = new();
        [FoldoutGroup("Filters"), SerializeField, RequireInterface(typeof(IXRInteractionStrengthFilter)), ShowIf("$CanSelect")]
        [RichTextTooltip("The select filters that this object uses to automatically populate the <mth>InteractionStrengthFilters</mth> List at startup (optional, may be empty)." +
            "\nAll objects in this list should implement the <itf>IXRInteractionStrengthFilter</itf> interface.")]
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
        public List<Collider> Colliders => _colliders;

        /// <summary>
        /// Allows interaction with Interactors whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.
        /// </summary>
        /// <seealso cref="VXRBaseInteractor.InteractionLayers"/>
        /// <seealso cref="IsHoverableBy(IVXRHoverInteractor)"/>
        /// <seealso cref="IsSelectableBy(IVXRSelectInteractor)"/>
        /// <inheritdoc />
        public InteractionLayerMask InteractionLayers { get => _interactionLayers; set => _interactionLayers = value; }

        /// <summary>
        /// Specifies how this Interactable calculates its distance to a location, either using its Transform position, Collider
        /// position or Collider volume.
        /// </summary>
        /// <seealso cref="GetDistance"/>
        /// <seealso cref="Colliders"/>
        /// <seealso cref="DistanceCalculationModeType"/>
        public DistanceCalculationModeType DistanceCalculationMode { get => _distanceCalculationMode; set => _distanceCalculationMode = value; }

        public InteractableSelectMode SelectMode { get => _selectMode; set => _selectMode = value; }
        
        public InteractableFocusMode FocusMode { get => _focusMode; set => _focusMode = value; }

        /// <summary>
        /// The reticle that appears at the end of the line when valid.
        /// </summary>
        public GameObject CustomReticle { get => _customReticle; set => _customReticle = value; }
        

        // ***** Gazing *****
        /// <summary>
        /// Enables interaction with <see cref="VXRGazeInteractor"/>.
        /// </summary>
        public bool AllowGazeInteraction { get => _allowGazeInteraction; set => _allowGazeInteraction = value; }
        
        /// <summary>
        /// Enables <see cref="VXRGazeInteractor"/> to select this <see cref="VXRBaseInteractable"/>.
        /// </summary>
        /// <seealso cref="VXRRayInteractor.HoverToSelect"/>
        public bool AllowGazeSelect { get => _allowGazeSelect; set => _allowGazeSelect = value; }
        
        public bool OverrideGazeTimeToSelect { get => _overrideGazeTimeToSelect; set => _overrideGazeTimeToSelect = value; }
        
        public float GazeTimeToSelect { get => _gazeTimeToSelect; set => _gazeTimeToSelect = value; }
        
        public bool OverrideTimeToAutoDeselectGaze { get => _overrideTimeToAutoDeselectGaze; set => _overrideTimeToAutoDeselectGaze = value; }
        
        public float TimeToAutoDeselectGaze { get => _timeToAutoDeselectGaze; set => _timeToAutoDeselectGaze = value; }
        
        /// <summary>
        /// Enables gaze assistance with this interactable.
        /// </summary>
        public bool AllowGazeAssistance { get => _allowGazeAssistance; set => _allowGazeAssistance = value; }               


        // ***** Hovering *****
        public bool CanHover { get => _canHover; set => _canHover = value; }
        public bool IsHovered => _interactorsHovering.Count > 0;
        private readonly HashSetList<IVXRHoverInteractor> _interactorsHovering = new();
        public List<IVXRHoverInteractor> InteractorsHovering => (List<IVXRHoverInteractor>)_interactorsHovering.AsList();


        // ***** Selecting *****
        public bool CanSelect { get => _canSelect; set => _canSelect = value; }
        public bool IsSelected => _interactorsSelecting.Count > 0;
        private readonly HashSetList<IVXRSelectInteractor> _interactorsSelecting = new();
        public List<IVXRSelectInteractor> InteractorsSelecting => (List<IVXRSelectInteractor>)_interactorsSelecting.AsList();
        public IVXRSelectInteractor FirstInteractorSelecting { get; private set; }

        // ***** Focusing *****
        public bool IsFocused => _interactionGroupsFocusing.Count > 0;
        public bool CanFocus => _focusMode != InteractableFocusMode.None;
        private readonly HashSetList<IXRInteractionGroup> _interactionGroupsFocusing = new();
        public List<IXRInteractionGroup> InteractionGroupsFocusing => (List<IXRInteractionGroup>)_interactionGroupsFocusing.AsList();
        public IXRInteractionGroup FirstInteractionGroupFocusing { get; private set; }


        // ***** Activation *****
        public bool CanActivate { get => _canActivate; set => _canActivate = value; }


        // ***** Filters *****
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
        public IReadOnlyBindableVariable<float> LargestInteractionStrength => _mLargestInteractionStrength;
        #endregion

        #region Fields
        private readonly Dictionary<IVXRInteractor, Pose> _attachPoseOnSelect = new();
        private readonly Dictionary<IVXRInteractor, Pose> _localAttachPoseOnSelect = new();
        private readonly Dictionary<IVXRInteractor, GameObject> _reticleCache = new();

        /// <summary>
        /// The set of hovered and/or selected interactors that supports returning a variable select input value,
        /// which is used as the pre-filtered interaction strength.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="VXRInputInteractor"/> as the type to get the select input value to use as the pre-filtered
        /// interaction strength.
        /// </remarks>
        private readonly HashSetList<IVXRSelectInteractor> _variableSelectInteractors = new();

        private readonly Dictionary<IVXRSelectInteractor, float> _interactionStrengths = new();

        private VXRInteractionManager _registeredInteractionManager;
        #endregion

        #region Events
        public event Action<InteractableRegisteredEventArgs> Registered;
        public event Action<InteractableUnregisteredEventArgs> Unregistered;

        public event Action<HoverEnterEventArgs> FirstHoverEntered;
        public event Action<HoverExitEventArgs> LastHoverExited;
        public event Action<HoverEnterEventArgs> HoverEntered;
        public event Action<HoverExitEventArgs> HoverExited;

        public event Action<SelectEnterEventArgs> FirstSelectEntered;
        public event Action<SelectExitEventArgs> LastSelectExited;
        public event Action<SelectEnterEventArgs> SelectEntered;
        public event Action<SelectExitEventArgs> SelectExited;

        public event Action<FocusEnterEventArgs> FirstFocusEntered;
        public event Action<FocusExitEventArgs> LastFocusExited;
        public event Action<FocusEnterEventArgs> FocusEntered;
        public event Action<FocusExitEventArgs> FocusExited;

        public event Action<ActivateEventArgs> Activated;
        public event Action<DeactivateEventArgs> Deactivated;
        public event Action<HoverEnterEventArgs> HoverEntering;
        public event Action<HoverExitEventArgs> HoverExiting;
        public event Action<SelectEnterEventArgs> SelectEntering;
        public event Action<SelectExitEventArgs> SelectExiting;
        public event Action<XRInteractionUpdateOrder.UpdatePhase> Process;

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
        public Func<IVXRInteractable, Vector3, DistanceInfo> GetDistanceOverride { get; set; }

        public bool CanBeHovered => throw new NotImplementedException();

        public bool CanBeSelected => throw new NotImplementedException();

        public bool CanBeFocused => throw new NotImplementedException();

        public Func<bool> SelectableActive { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public VXRCompositeInteractable Composite => throw new NotImplementedException();

        public (int Before, int After) SelectCountBeforeAndAfterChange => throw new NotImplementedException();
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
               
        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when this Interactable is registered with it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that registered this Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.RegisterInteractable(IVXRInteractable)"/>
        public virtual void OnRegistered(InteractableRegisteredEventArgs args)
        {
            if (args.manager != _interactionManager)
                Debug.LogWarning($"An Interactable was registered with an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{_interactionManager}\" but was registered with \"{args.manager}\".", this);

            Registered?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="VXRInteractionManager"/> calls this method
        /// when this Interactable is unregistered from it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that unregistered this Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="VXRInteractionManager.UnregisterInteractable(IVXRInteractable)"/>
        public virtual void OnUnregistered(InteractableUnregisteredEventArgs args)
        {
            if (args.manager != _registeredInteractionManager)
                Debug.LogWarning($"An Interactable was unregistered from an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{_registeredInteractionManager}\" but was unregistered from \"{args.manager}\".", this);

            Unregistered?.Invoke(args);
        }
        #endregion

        #region - Processing -
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
        public virtual bool IsHoverableBy(IVXRHoverInteractor interactor)
        {
            return (_allowGazeInteraction || !(interactor is VXRGazeInteractor)) && ProcessHoverFilters(interactor);
        }
        
        /// <summary>
        /// Determines whether this Interactable is currently being hovered by the Interactor.
        /// </summary>
        /// <param name="interactor">Interactor to check.</param>
        /// <returns>Returns <see langword="true"/> if this Interactable is currently being hovered by the Interactor.
        /// Otherwise, returns <seealso langword="false"/>.</returns>
        /// <remarks>
        /// In other words, returns whether <see cref="InteractorsHovering"/> contains <paramref name="interactor"/>.
        /// </remarks>
        /// <seealso cref="InteractorsHovering"/>
        public bool IsHoveredBy(IVXRHoverInteractor interactor) => _interactorsHovering.Contains(interactor);

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
        public virtual void OnHoverEntering(HoverEnterEventArgs args)
        {
            if (_customReticle != null)
            {
                AttachCustomReticle(args.interactorObject);
            }

            var added = _interactorsHovering.Add(args.interactorObject);
            Debug.Assert(added, "An Interactable received a Hover Enter event for an Interactor that was already hovering over it.", this);

            if (args.interactorObject.TryGetSelectInteractor(out var selectInteractor))
            {
                _variableSelectInteractors.Add(selectInteractor);
            }
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
        public virtual void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (_interactorsHovering.Count == 1)
            {
                FirstHoverEntered?.Invoke(args);
            }

            HoverEntered?.Invoke(args);
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
        public virtual void OnHoverExiting(HoverExitEventArgs args)
        {
            if (_customReticle != null)
            {
                RemoveCustomReticle(args.interactorObject);
            }

            var removed = _interactorsHovering.Remove(args.interactorObject);
            Debug.Assert(removed, "An Interactable received a Hover Exit event for an Interactor that was not hovering over it.", this);

            if (_variableSelectInteractors.Count > 0 &&
                args.interactorObject.TryGetSelectInteractor(out var variableSelectInteractor) &&
                !IsSelectedBy(variableSelectInteractor))
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
        public virtual void OnHoverExited(HoverExitEventArgs args)
        {
            if (_interactorsHovering.Count == 0)
            {
                LastHoverExited?.Invoke(args);
            }

            HoverExited?.Invoke(args);
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
        protected bool ProcessHoverFilters(IVXRHoverInteractor interactor)
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
        public virtual bool IsSelectableBy(IVXRSelectInteractor interactor)
        {
            return ((_allowGazeInteraction && _allowGazeSelect) || !(interactor is VXRGazeInteractor)) && ProcessSelectFilters(interactor);
        }

        /// <summary>
        /// Determines whether this Interactable is currently being selected by the Interactor.
        /// </summary>
        /// <param name="interactor">Interactor to check.</param>
        /// <returns>Returns <see langword="true"/> if this Interactable is currently being selected by the Interactor.
        /// Otherwise, returns <seealso langword="false"/>.</returns>
        /// <remarks>
        /// In other words, returns whether <see cref="InteractorsSelecting"/> contains <paramref name="interactor"/>.
        /// </remarks>
        /// <seealso cref="InteractorsSelecting"/>
        public bool IsSelectedBy(IVXRSelectInteractor interactor) => _interactorsSelecting.Contains(interactor);
        
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
        public virtual void OnSelectEntering(SelectEnterEventArgs args)
        {
            var added = _interactorsSelecting.Add(args.InteractorObject);
            Debug.Assert(added, "An Interactable received a Select Enter event for an Interactor that was already selecting it.", this);

            if (args.InteractorObject.TryGetSelectInteractor(out var variableSelectInteractor))
            {
                _variableSelectInteractors.Add(variableSelectInteractor);
            }

            if (_interactorsSelecting.Count == 1)
            {
                FirstInteractorSelecting = args.InteractorObject;
            }

            CaptureAttachPose(args.InteractorObject);
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
        public virtual void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (_interactorsSelecting.Count == 1)
            {
                FirstSelectEntered?.Invoke(args);
            }

            SelectEntered?.Invoke(args);
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
        public virtual void OnSelectExiting(SelectExitEventArgs args)
        {
            var removed = _interactorsSelecting.Remove(args.InteractorObject);
            Debug.Assert(removed, "An Interactable received a Select Exit event for an Interactor that was not selecting it.", this);

            if (_variableSelectInteractors.Count > 0 &&
                args.InteractorObject.TryGetHoverInteractor(out var hoverInteractor) &&
                !IsHoveredBy(hoverInteractor))
            {
                _variableSelectInteractors.Remove(args.InteractorObject);
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
        public virtual void OnSelectExited(SelectExitEventArgs args)
        {
            if (_interactorsSelecting.Count == 0)
            {
                LastSelectExited?.Invoke(args);
            }

            SelectExited?.Invoke(args);

            // The dictionaries are pruned so that they don't infinitely grow in size as selections are made.
            if (_interactorsSelecting.Count == 0)
            {
                FirstInteractorSelecting = null;
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
        protected bool ProcessSelectFilters(IVXRSelectInteractor interactor)
        {
            return XRFilterUtility.Process(_selectFilters, interactor, this);
        }
        #endregion

        #region - Focus -       
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
        public virtual void OnFocusEntering(FocusEnterEventArgs args)
        {
            var added = _interactionGroupsFocusing.Add(args.interactionGroup);
            Debug.Assert(added, "An Interactable received a Focus Enter event for an Interaction group that was already focusing it.", this);

            if (_interactionGroupsFocusing.Count == 1)
                FirstInteractionGroupFocusing = args.interactionGroup;
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
        public virtual void OnFocusEntered(FocusEnterEventArgs args)
        {
            if (_interactionGroupsFocusing.Count == 1)
            {
                FirstFocusEntered?.Invoke(args);
            }

            FocusEntered?.Invoke(args);
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
        public virtual void OnFocusExiting(FocusExitEventArgs args)
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
        public virtual void OnFocusExited(FocusExitEventArgs args)
        {
            if (_interactionGroupsFocusing.Count == 0)
            {
                LastFocusExited?.Invoke(args);
            }

            FocusExited?.Invoke(args);

            if (_interactionGroupsFocusing.Count == 0)
            {
                FirstInteractionGroupFocusing = null;
            }
        }
        #endregion

        #region - Activate -
        /// <summary>
        /// <see cref="VXRInputInteractor"/> calls this method when the
        /// Interactor begins an activation event on this Interactable.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is sending the activate event.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnDeactivated"/>
        public virtual void OnActivated(ActivateEventArgs args)
        {
            Activated?.Invoke(args);
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
        public virtual void OnDeactivated(DeactivateEventArgs args)
        {
            Deactivated?.Invoke(args);
        }
        #endregion

        #region - Interaction -
        /// <inheritdoc />
        public float GetInteractionStrength(IVXRSelectInteractor interactor)
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
                //for (int i = 0, count = _interactorsSelecting.Count; i < count; ++i)
                //{
                //    var interactor = _interactorsSelecting[i];
                //    if (interactor is VXRInputInteractor)
                //        continue;

                //    var interactionStrength = ProcessInteractionStrengthFilters(interactor, InteractionStrengthSelect);
                //    _interactionStrengths[interactor] = interactionStrength;

                //    maxInteractionStrength = Mathf.Max(maxInteractionStrength, interactionStrength);
                //}

                //for (int i = 0, count = _interactorsHovering.Count; i < count; ++i)
                //{
                //    var interactor = _interactorsHovering[i];
                //    if (interactor is VXRInputInteractor || IsSelectedBy(interactor))
                //        continue;

                //    var interactionStrength = ProcessInteractionStrengthFilters(interactor, InteractionStrengthHover);
                //    _interactionStrengths[interactor] = interactionStrength;

                //    maxInteractionStrength = Mathf.Max(maxInteractionStrength, interactionStrength);
                //}

                for (int i = 0, count = _variableSelectInteractors.Count; i < count; ++i)
                {
                    var interactor = _variableSelectInteractors[i];

                    // Use the Select input value as the initial interaction strength.
                    // For interactors that use motion controller input, this is typically the analog trigger or grip press amount.
                    // Fall back to the default values for selected and hovered interactors in the case when the interactor
                    // is misconfigured and is missing the input wrapper or component reference.
                    var interactionStrength = interactor.LogicalSelectState != null ? interactor.LogicalSelectState.CurrentValue : IsSelectedBy(interactor) ? InteractionStrengthSelect : InteractionStrengthHover;

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
        protected float ProcessInteractionStrengthFilters(IVXRSelectInteractor interactor, float interactionStrength)
        {
            return XRFilterUtility.Process(_interactionStrengthFilters, interactor, this, interactionStrength);
        }
        #endregion

        #region - Posing -
        /// <inheritdoc />
        public virtual Transform GetAttachTransform(IAttachPoint attachPoint)
        {
            return transform;
        }

        /// <inheritdoc />
        public Pose GetAttachPoseOnSelect(IVXRInteractor interactor)
        {
            return _attachPoseOnSelect.TryGetValue(interactor, out var pose) ? pose : Pose.identity;
        }

        /// <inheritdoc />
        public Pose GetLocalAttachPoseOnSelect(IVXRInteractor interactor)
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
        protected void CaptureAttachPose(IVXRInteractor interactor)
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
        public virtual GameObject GetCustomReticle(IVXRInteractor interactor)
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
        /// <seealso cref="RemoveCustomReticle(IVXRInteractor)"/>
        public virtual void AttachCustomReticle(IVXRInteractor interactor)
        {
            if (interactor == null)
                return;
            
            var interactorTransform = interactor.transform;

            // Try and find any attached reticle and swap it
            if (interactorTransform.TryGetComponent<IXRCustomReticleProvider>(out var reticleProvider))
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
                    if (reticleInstance.TryGetComponent<IXRInteractableCustomReticle>(out var customReticleBehavior))
                    {
                        customReticleBehavior.OnReticleAttached(this, reticleProvider);
                    }
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
        /// <seealso cref="AttachCustomReticle(IVXRInteractor)"/>
        public virtual void RemoveCustomReticle(IVXRInteractor interactor)
        {
            if (interactor == null)
            {
                return;
            }

            var interactorTransform = interactor.transform;

            // Try and find any attached reticle and swap it
            if (!interactorTransform.TryGetComponent<IXRCustomReticleProvider>(out var reticleProvider))
            {
                return;
            }

            if (!_reticleCache.TryGetValue(interactor, out var reticleInstance))
            {
                return;
            }

            if (reticleInstance.TryGetComponent<IXRInteractableCustomReticle>(out var customReticleBehavior))
            {
                customReticleBehavior.OnReticleDetaching();
            }

            Destroy(reticleInstance);
            _reticleCache.Remove(interactor);
            reticleProvider.RemoveCustomReticle();
        }
        #endregion

        #region - Helper -
        /// <remarks>
        /// This method calls the <see cref="GetDistance"/> method to perform the distance calculation.
        /// </remarks>
        public virtual float GetDistanceSqrToInteractor(IAttachPoint attachPoint)
        {
            if (attachPoint == null)
            {
                return float.MaxValue;
            }
            
            var interactorAttachTransform = attachPoint.GetAttachTransform(this);
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
