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
    [AddComponentMenu("Interactable")]
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Interactables)]
    public class Interactable : MonoBehaviour, IInteractable
    {
        private static readonly ProfilerMarker s_ProcessInteractionStrengthMarker = new("VXR.ProcessInteractionStrength.Interactables");

        #region Inspector
        [FoldoutGroup("Components"), SerializeField]
        [RichTextTooltip("Colliders to use for interaction with this Interactable (if empty, will use any child Colliders).")]
        private List<Collider> _colliders = new();

        [FoldoutGroup("Interaction"), SerializeField]
        private bool _allowHover = true;
        [FoldoutGroup("Interaction"), SerializeField]
        private bool _allowSelect = true;
        //[FoldoutGroup("Interaction"), SerializeField]
        //[RichTextTooltip("Allows interaction with Interactors whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.")]
        //private InteractionLayerMask _interactionLayers = 1;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("Allows interaction with Interactors whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.")]
        private List<InteractionLayerKey> _interactionLayers = new();
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("Specifies how this Interactable calculates its distance to a location, either using its Transform position, Collider position or Collider volume.")]
        private DistanceCalculationModeType _distanceCalculationMode = DistanceCalculationModeType.InteractionPointPosition;
        [FoldoutGroup("Interaction"), SerializeField, ShowIf("%_allowSelect")]
        [RichTextTooltip("")]
        private InteractableSelectMode _selectMode = InteractableSelectMode.Single;
        [FoldoutGroup("Interaction"), SerializeField, ShowIf("%_allowSelect")]
        [RichTextTooltip("")]
        private InteractableFocusMode _focusMode = InteractableFocusMode.Single;

        [FoldoutGroup("Posing"), SerializeField]
        private bool _overrideHoverPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_overrideHoverPose")]
        private HandPoseDatum _hoverPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_overrideHoverPose")]
        private float _hoverPoseDuration;
        [FoldoutGroup("Posing"), SerializeField]
        private bool _overrideSelectPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_overrideSelectPose")]
        private HandPoseDatum _selectPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_overrideSelectPose")]
        private float _selectPoseDuration;

        [FoldoutGroup("Filters", order: 90), SerializeField, RequireInterface(typeof(IXRHoverFilter)), ShowIf("%_allowHover")]
        [RichTextTooltip("The hover filters that this object uses to automatically populate the <mth>HoverFilters</mth> List at startup (optional, may be empty)." +
            "\nAll objects in this list should implement the <itf>IXRHoverFilter</itf> interface.")]
        private List<Object> _startingHoverFilters = new();
        [FoldoutGroup("Filters"), SerializeField, RequireInterface(typeof(IXRSelectFilter)), ShowIf("%_allowSelect")]
        [RichTextTooltip("The select filters that this object uses to automatically populate the <mth>SelectFilters</mth> List at startup (optional, may be empty)." +
            "\nAll objects in this list should implement the <itf>IXRSelectFilter</itf> interface.")]
        private List<Object> _startingSelectFilters = new();
        [FoldoutGroup("Filters"), SerializeField, RequireInterface(typeof(IXRInteractionStrengthFilter)), ShowIf("%_allowSelect")]
        [RichTextTooltip("The select filters that this object uses to automatically populate the <mth>InteractionStrengthFilters</mth> List at startup (optional, may be empty)." +
            "\nAll objects in this list should implement the <itf>IXRInteractionStrengthFilter</itf> interface.")]
        private List<Object> _startingInteractionStrengthFilters = new();
        #endregion

        #region Properties
        private VXRInteractionManager _interactionManager;
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
                {
                    RegisterWithInteractionManager();
                }
            }
        }

        public List<InteractableModule> Modules { get; } = new();

        public List<Collider> Colliders => _colliders;

        /// <summary>
        /// Allows interaction with Interactors whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.
        /// </summary>
        /// <seealso cref="VXRBaseInteractor.InteractionLayers"/>
        /// <seealso cref="IsHoverableBy(Interactor)"/>
        /// <seealso cref="IsSelectableBy(Interactor)"/>
        public int[] InteractionLayers { get; set; }

        // ***** Hovering *****
        public bool AllowHover { get => _allowHover; set => _allowHover = value; }
        public bool CanBeHovered => _allowHover && HoverableActive.Invoke();
        public bool IsHovered => _interactorsHovering.Count > 0;
        private readonly HashSetList<Interactor> _interactorsHovering = new();
        public List<Interactor> InteractorsHovering => (List<Interactor>)_interactorsHovering.AsList();

        // ***** Selecting *****
        public bool AllowSelect { get => _allowSelect; set => _allowSelect = value; }
        public bool CanBeSelected => _allowSelect && SelectableActive.Invoke();
        public InteractableSelectMode SelectMode { get => _selectMode; set => _selectMode = value; }
        public bool IsSelected => _interactorsSelecting.Count > 0;
        private readonly HashSetList<Interactor> _interactorsSelecting = new();
        public List<Interactor> InteractorsSelecting => (List<Interactor>)_interactorsSelecting.AsList();
        public Interactor FirstInteractorSelecting { get; private set; }
        public (int Before, int After) SelectCountBeforeAndAfterChange { get; private set; }

        // ***** Focusing *****
        public bool IsFocused => _interactionGroupsFocusing.Count > 0;
        public bool CanBeFocused => _focusMode != InteractableFocusMode.None;
        private readonly HashSetList<IXRInteractionGroup> _interactionGroupsFocusing = new();
        public List<IXRInteractionGroup> InteractionGroupsFocusing => (List<IXRInteractionGroup>)_interactionGroupsFocusing.AsList();
        public IXRInteractionGroup FirstInteractionGroupFocusing { get; private set; }
        public InteractableFocusMode FocusMode => _focusMode;

        // ***** Filters *****
        private readonly ExposedRegistrationList<IXRHoverFilter> _hoverFilters = new() { BufferChanges = false };
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

        public int LastSorterType { get; set; }
        #endregion

        #region Fields        
        private VXRInteractionManager _registeredInteractionManager;

        private readonly Dictionary<Interactor, Pose> _attachPoseOnSelect = new();
        private readonly Dictionary<Interactor, Pose> _localAttachPoseOnSelect = new();

        /// <summary>
        /// The set of hovered and/or selected interactors that supports returning a variable select input value,
        /// which is used as the pre-filtered interaction strength.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="VXRInputInteractor"/> as the type to get the select input value to use as the pre-filtered
        /// interaction strength.
        /// </remarks>
        private readonly HashSetList<Interactor> _variableSelectInteractors = new();

        private readonly Dictionary<Interactor, float> _interactionStrengths = new();

        private readonly Dictionary<Type, InteractableModule> _modulesMap = new();
        #endregion

        #region Events        
        public event Action<InteractableRegisteredEventArgs> Registered;
        public event Action<InteractableUnregisteredEventArgs> Unregistered;
        public Func<IInteractable, Vector3, DistanceInfo> GetDistanceOverride { get; set; }
        public Func<IAttachPoint, Transform> OverrideAttachTransform { get; internal set; }

        public event Action<HoverEnterEventArgs> FirstHoverEntered;
        public event Action<HoverEnterEventArgs> HoverEntering;
        public event Action<HoverEnterEventArgs> HoverEntered;

        public event Action<HoverExitEventArgs> HoverExiting;
        public event Action<HoverExitEventArgs> HoverExited;
        public event Action<HoverExitEventArgs> LastHoverExited;

        public Func<bool> HoverableActive { get; set; }

        public event Action<SelectEnterEventArgs> FirstSelectEntered;
        public event Action<SelectEnterEventArgs> SelectEntering;
        public event Action<SelectEnterEventArgs> SelectEntered;

        public event Action<SelectExitEventArgs> SelectExiting;
        public event Action<SelectExitEventArgs> SelectExited;
        public event Action<SelectExitEventArgs> LastSelectExited;

        public event Action<FocusEnterEventArgs> FirstFocusEntered;
        public event Action<FocusEnterEventArgs> FocusEntered;
        public event Action<FocusExitEventArgs> FocusExited;
        public event Action<FocusExitEventArgs> LastFocusExited;

        public Func<bool> SelectableActive { get; set; }       
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

            InteractionLayers = new int[_interactionLayers.Count];
            for (int i = 0; i < _interactionLayers.Count; i++)
            {
                InteractionLayers[i] = _interactionLayers[i].Layer;
            }

            Modules.AddRange(GetComponents<InteractableModule>());
            foreach (var module in Modules)
            {
                if(!_modulesMap.TryAdd(module.GetType(), module))
                {
                    Debug.LogError($"Two or more modules of the same type {module.GetType()}");
                }
                var addSubType = module.GetType().BaseType;
                while (addSubType != typeof(InteractableModule))
                {
                    if (!_modulesMap.TryAdd(addSubType, module))
                    {
                        Debug.LogError($"Two or more modules of the same type {module.GetType()}");
                    }
                    addSubType = addSubType.BaseType;
                }
            }

            // Setup Interaction Manager
            FindCreateInteractionManager();

            HoverableActive = DefaultHoverActive;
            SelectableActive = DefaultSelectActive;

            // Setup the starting filters
            _hoverFilters.RegisterReferences(_startingHoverFilters, this);           
            _selectFilters.RegisterReferences(_startingSelectFilters, this);
            _interactionStrengthFilters.RegisterReferences(_startingInteractionStrengthFilters, this);
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
            {
                return;
            }

            _interactionManager = ComponentLocatorUtility<VXRInteractionManager>.FindOrCreateComponent();
        }

        private void RegisterWithInteractionManager()
        {
            if (_registeredInteractionManager == _interactionManager)
            {
                return;
            }

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
            if (args.Manager != _interactionManager)
            {
                Debug.LogWarning($"An Interactable was registered with an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{_interactionManager}\" but was registered with \"{args.Manager}\".", this);
            }

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
            if (args.Manager != _registeredInteractionManager)
            {
                Debug.LogWarning($"An Interactable was unregistered from an unexpected {nameof(VXRInteractionManager)}." +
                                 $" {this} was expecting to communicate with \"{_registeredInteractionManager}\" but was unregistered from \"{args.Manager}\".", this);
            }

            Unregistered?.Invoke(args);
        }
        #endregion

        #region - Processing -
        public virtual void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            foreach (var module in Modules)
            {
                module.PreProcessInteractable(updatePhase);
            }

            foreach (var module in Modules)
            {
                module.PostProcessInteractable(updatePhase);
            }
        }
        #endregion

        #region - Interaction -
        public Transform GetAttachTransform(IAttachPoint attachPoint)
        {
            return OverrideAttachTransform != null ? (OverrideAttachTransform?.Invoke(attachPoint)) : transform;
        }

        public float GetInteractionStrength(Interactor interactor)
        {
            return _interactionStrengths.TryGetValue(interactor, out var interactionStrength) ? interactionStrength : 0f;
        }

        public virtual void ProcessInteractionStrength(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            var maxInteractionStrength = 0f;

            using (s_ProcessInteractionStrengthMarker.Auto())
            {
                _interactionStrengths.Clear();
                for (int i = 0, count = _variableSelectInteractors.Count; i < count; ++i)
                {
                    var interactor = _variableSelectInteractors[i];
                    var interactionStrength = interactor.LogicalSelectState.CurrentValue;
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
        protected float ProcessInteractionStrengthFilters(Interactor interactor, float interactionStrength)
        {
            return XRFilterUtility.Process(_interactionStrengthFilters, interactor, this, interactionStrength);
        }

        public Pose GetAttachPoseOnSelect(Interactor interactor)
        {
            return _attachPoseOnSelect.TryGetValue(interactor, out var pose) ? pose : Pose.identity;
        }

        public Pose GetLocalAttachPoseOnSelect(Interactor interactor)
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
        protected void CaptureAttachPose(Interactor interactor)
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

        #region - Hover -
        protected virtual bool DefaultHoverActive()
        {
            return true;
        }

        public bool IsHoverableBy(Interactor interactor)
        {
            return CanBeHovered && ProcessHoverFilters(interactor) && (Modules.Count == 0 || _AllModulesHoverableBy(interactor));

            bool _AllModulesHoverableBy(Interactor interactor)
            {
                foreach (var module in Modules)
                {
                    if (!module.IsHoverableBy(interactor))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public bool IsHoveredBy(Interactor interactor) => _interactorsHovering.Contains(interactor);

        /// <summary>
        /// Returns the processing value of the filters in <see cref="HoverFilters"/> for the given Interactor and this
        /// Interactable.
        /// </summary>
        /// <param name="interactor">The Interactor to be validated by the hover filters.</param>
        /// <returns>
        /// Returns <see langword="true"/> if all processed filters also return <see langword="true"/>, or if
        /// <see cref="HoverFilters"/> is empty. Otherwise, returns <see langword="false"/>.
        /// </returns>
        protected bool ProcessHoverFilters(Interactor interactor)
        {
            return XRFilterUtility.Process(_hoverFilters, interactor, this);
        }
        #endregion

        #region - Select -
        protected virtual bool DefaultSelectActive()
        {
            return true;
        }
        
        public bool IsSelectableBy(Interactor interactor)
        {
            return CanBeSelected && ProcessSelectFilters(interactor) && (Modules.Count == 0 || _AllModulesSelectableBy(interactor));

            bool _AllModulesSelectableBy(Interactor interactor)
            {
                foreach (var module in Modules)
                {
                    if (!module.IsSelectableBy(interactor))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        
        public bool IsSelectedBy(Interactor interactor) => _interactorsSelecting.Contains(interactor);

        /// <summary>
        /// Returns the processing value of the filters in <see cref="SelectFilters"/> for the given Interactor and this
        /// Interactable.
        /// </summary>
        /// <param name="interactor">The Interactor to be validated by the select filters.</param>
        /// <returns>
        /// Returns <see langword="true"/> if all processed filters also return <see langword="true"/>, or if
        /// <see cref="SelectFilters"/> is empty. Otherwise, returns <see langword="false"/>.
        /// </returns>
        protected bool ProcessSelectFilters(Interactor interactor)
        {
            return XRFilterUtility.Process(_selectFilters, interactor, this);
        }
        #endregion

        #region - Posing -
        public bool TryGetOverrideHoverPose(out HandPoseDatum pose, out float duration)
        {
            pose = _hoverPose;
            duration = _hoverPoseDuration;
            return _overrideHoverPose;
        }

        public bool TryGetOverrideSelectPose(out HandPoseDatum pose, out float duration)
        {
            pose = _selectPose;
            duration = _selectPoseDuration;
            return _overrideSelectPose;
        }
        #endregion

        #region - Events -        
        public virtual void OnHoverEntering(HoverEnterEventArgs args)
        {
            var added = _interactorsHovering.Add(args.InteractorObject);
            Debug.Assert(added, "An Interactable received a Hover Enter event for an Interactor that was already hovering over it.", this);

            _variableSelectInteractors.Add(args.InteractorObject);

            HoverEntering?.Invoke(args);
        }

        public virtual void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (_interactorsHovering.Count == 1)
            {
                FirstHoverEntered?.Invoke(args);
            }

            HoverEntered?.Invoke(args);
        }

        public virtual void OnHoverExiting(HoverExitEventArgs args)
        {
            var removed = _interactorsHovering.Remove(args.InteractorObject);
            Debug.Assert(removed, "An Interactable received a Hover Exit event for an Interactor that was not hovering over it.", this);

            if (_variableSelectInteractors.Count > 0 &&
                !IsSelectedBy(args.InteractorObject))
            {
                _variableSelectInteractors.Remove(args.InteractorObject);
            }

            HoverExiting?.Invoke(args);
        }

        public virtual void OnHoverExited(HoverExitEventArgs args)
        {
            if (_interactorsHovering.Count == 0)
            {
                LastHoverExited?.Invoke(args);
            }

            HoverExited?.Invoke(args);
        }


        public virtual void OnSelectEntering(SelectEnterEventArgs args)
        {
            int countBefore = _interactorsSelecting.Count;
            var added = _interactorsSelecting.Add(args.InteractorObject);
            int countAfter = _interactorsSelecting.Count;
            SelectCountBeforeAndAfterChange = new(countBefore, countAfter);
            Debug.Assert(added, "An Interactable received a Select Enter event for an Interactor that was already selecting it.", this);

            _variableSelectInteractors.Add(args.InteractorObject);

            if (_interactorsSelecting.Count == 1)
            {
                FirstInteractorSelecting = args.InteractorObject;
            }

            CaptureAttachPose(args.InteractorObject);
            SelectEntering?.Invoke(args);
        }
        
        public virtual void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (_interactorsSelecting.Count == 1)
            {
                FirstSelectEntered?.Invoke(args);
            }

            SelectEntered?.Invoke(args);
        }
        
        public virtual void OnSelectExiting(SelectExitEventArgs args)
        {
            int countBefore = _interactorsSelecting.Count;
            var removed = _interactorsSelecting.Remove(args.InteractorObject);
            int countAfter = _interactorsSelecting.Count;
            SelectCountBeforeAndAfterChange = new(countBefore, countAfter);
            Debug.Assert(removed, "An Interactable received a Select Exit event for an Interactor that was not selecting it.", this);

            if (_variableSelectInteractors.Count > 0)
            {
                _variableSelectInteractors.Remove(args.InteractorObject);
            }
            SelectExiting?.Invoke(args);
        }
        
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

        
        public virtual void OnFocusEntering(FocusEnterEventArgs args)
        {
            var added = _interactionGroupsFocusing.Add(args.InteractionGroup);
            Debug.Assert(added, "An Interactable received a Focus Enter event for an Interaction group that was already focusing it.", this);

            if (_interactionGroupsFocusing.Count == 1)
                FirstInteractionGroupFocusing = args.InteractionGroup;
        }
        
        public virtual void OnFocusEntered(FocusEnterEventArgs args)
        {
            if (_interactionGroupsFocusing.Count == 1)
            {
                FirstFocusEntered?.Invoke(args);
            }

            FocusEntered?.Invoke(args);
        }
        
        public virtual void OnFocusExiting(FocusExitEventArgs args)
        {
            var removed = _interactionGroupsFocusing.Remove(args.InteractionGroup);
            Debug.Assert(removed, "An Interactable received a Focus Exit event for an Interaction group that did not have focus of it.", this);
        }
        
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

        #region - Helper -        
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

                case DistanceCalculationModeType.InteractionPointPosition:
                    XRInteractableUtility.TryGetClosestInteractionPoint(this, position, out distanceInfo);
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

        public bool HasModule<T>() where T : InteractableModule
        {
            return _modulesMap.ContainsKey(typeof(T));
        }

        public T GetModule<T>() where T : InteractableModule
        {
            return (T)_modulesMap[typeof(T)];
        }

        public bool TryGetModule<T>(out T module) where T : InteractableModule
        {
            if (_modulesMap.TryGetValue(typeof(T), out var moduleT))
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
