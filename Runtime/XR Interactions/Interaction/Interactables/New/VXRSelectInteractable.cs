using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.XR.CoreUtils.Bindings.Variables;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using VaporInspector;
using VaporXR.Interactors;
using VaporXR.Utilities;
using Object = UnityEngine.Object;

namespace VaporXR.Interactables
{
    public class VXRSelectInteractable : VXRInteractable, IVXRSelectInteractable
    {
        private static readonly ProfilerMarker s_ProcessInteractionStrengthMarker = new("VXRI.ProcessInteractionStrength.Interactables");

        #region Inspector
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("")]
        private InteractableSelectMode _selectMode = InteractableSelectMode.Single;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("")]
        private InteractableFocusMode _focusMode = InteractableFocusMode.Single;

        [FoldoutGroup("Posing"), SerializeField]
        private bool _overrideSelectPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_overrideSelectPose")]
        private HandPoseDatum _selectPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_overrideSelectPose")]
        private float _selectPoseDuration;

        [FoldoutGroup("Filters", order: 90), SerializeField, RequireInterface(typeof(IXRSelectFilter))]
        [RichTextTooltip("The select filters that this object uses to automatically populate the <mth>SelectFilters</mth> List at startup (optional, may be empty)." +
            "\nAll objects in this list should implement the <itf>IXRSelectFilter</itf> interface.")]
        private List<Object> _startingSelectFilters = new();
        [FoldoutGroup("Filters"), SerializeField, RequireInterface(typeof(IXRInteractionStrengthFilter))]
        [RichTextTooltip("The select filters that this object uses to automatically populate the <mth>InteractionStrengthFilters</mth> List at startup (optional, may be empty)." +
            "\nAll objects in this list should implement the <itf>IXRInteractionStrengthFilter</itf> interface.")]
        private List<Object> _startingInteractionStrengthFilters = new();
        #endregion

        #region Properties
        public InteractableSelectMode SelectMode { get => _selectMode; set => _selectMode = value; }

        // ***** Selecting *****
        public bool CanBeSelected => SelectableActive.Invoke();
        public bool IsSelected => _interactorsSelecting.Count > 0;
        private readonly HashSetList<IVXRSelectInteractor> _interactorsSelecting = new();
        public List<IVXRSelectInteractor> InteractorsSelecting => (List<IVXRSelectInteractor>)_interactorsSelecting.AsList();
        public IVXRSelectInteractor FirstInteractorSelecting { get; private set; }
        public (int Before, int After) SelectCountBeforeAndAfterChange { get; private set; }

        // ***** Focusing *****
        public bool IsFocused => _interactionGroupsFocusing.Count > 0;
        public bool CanBeFocused => _focusMode != InteractableFocusMode.None;
        private readonly HashSetList<IXRInteractionGroup> _interactionGroupsFocusing = new();
        public List<IXRInteractionGroup> InteractionGroupsFocusing => (List<IXRInteractionGroup>)_interactionGroupsFocusing.AsList();
        public IXRInteractionGroup FirstInteractionGroupFocusing { get; private set; }

        // ***** Filters *****
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
        #endregion

        #region Events
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
        protected override void Awake()
        {
            base.Awake();
            SelectableActive = AllowSelect;

            // Setup the starting filters
            _selectFilters.RegisterReferences(_startingSelectFilters, this);
            _interactionStrengthFilters.RegisterReferences(_startingInteractionStrengthFilters, this);
        }

        protected virtual bool AllowSelect()
        {
            return true;
        }
        #endregion

        #region - Interaction -
        public float GetInteractionStrength(IVXRSelectInteractor interactor)
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
        protected float ProcessInteractionStrengthFilters(IVXRSelectInteractor interactor, float interactionStrength)
        {
            return XRFilterUtility.Process(_interactionStrengthFilters, interactor, this, interactionStrength);
        }

        public Pose GetAttachPoseOnSelect(IVXRInteractor interactor)
        {
            return _attachPoseOnSelect.TryGetValue(interactor, out var pose) ? pose : Pose.identity;
        }

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

        #region - Select -
        /// <summary>
        /// Determines if a given Interactor can select this Interactable.
        /// </summary>
        /// <param name="interactor">Interactor to check for a valid selection with.</param>
        /// <returns>Returns <see langword="true"/> if selection is valid this frame. Returns <see langword="false"/> if not.</returns>
        /// <seealso cref="VXRBaseInteractor.CanSelect"/>
        public bool IsSelectableBy(IVXRSelectInteractor interactor)
        {
            return CanBeSelected && ProcessSelectFilters(interactor) && (Composite == null || Composite.IsSelectableBy(interactor));
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
            int countBefore = _interactorsSelecting.Count;
            var added = _interactorsSelecting.Add(args.InteractorObject);
            int countAfter = _interactorsSelecting.Count;
            SelectCountBeforeAndAfterChange = new(countBefore, countAfter);
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
            SelectEntering?.Invoke(args);
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

        #region - Posing -
        public bool TryGetOverrideHoverPose(out HandPoseDatum pose, out float duration)
        {
            pose = _selectPose;
            duration = _selectPoseDuration;
            return _overrideSelectPose;
        }
        #endregion
    }
}
