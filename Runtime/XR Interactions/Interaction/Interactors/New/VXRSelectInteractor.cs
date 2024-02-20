using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.XR.CoreUtils.Bindings.Variables;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using VaporInspector;
using VaporXR.Interactables;
using VaporXR.Utilities;
using Object = UnityEngine.Object;

namespace VaporXR.Interactors
{
    public class VXRSelectInteractor : VXRInteractor, IVXRSelectInteractor, IPoseSource
    {
        private static readonly ProfilerMarker s_ProcessInteractionStrengthMarker = new("VXR.ProcessInteractionStrength.Interactors");
        private const float InteractionStrengthSelect = 1f;

        #region Inspector
        [FoldoutGroup("Selection"), SerializeField]
        private InputTriggerType _selectActionTrigger = InputTriggerType.StateChange;
        [FoldoutGroup("Selection"), SerializeField]
        private bool _keepSelectedTargetValid = true;
        [FoldoutGroup("Selection"), SerializeField]
        private VXRBaseInteractable _startingSelectedInteractable;

        [FoldoutGroup("Posing"), SerializeField]
        private bool _posingEnabled;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_posingEnabled")]
        private VXRHand _hand;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_posingEnabled")]
        private HandPoseDatum _selectPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_posingEnabled")]
        private float _selectPoseDuration;

        [FoldoutGroup("Filters"), SerializeField]
        [RequireInterface(typeof(IXRSelectFilter))]
        private List<Object> _startingSelectFilters = new();
        #endregion

        #region Properties
        public virtual bool IsSelectActive
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

        private readonly HashSetList<IVXRSelectInteractable> _interactablesSelected = new();
        
        public List<IVXRSelectInteractable> InteractablesSelected => (List<IVXRSelectInteractable>)_interactablesSelected.AsList();
        
        public IVXRSelectInteractable FirstInteractableSelected { get; private set; }
        
        public bool HasSelection => _interactablesSelected.Count > 0;

        private readonly ExposedRegistrationList<IXRSelectFilter> _selectFilters = new() { BufferChanges = false };
        
        public IXRFilterList<IXRSelectFilter> SelectFilters => _selectFilters;

        public IXRInteractionGroup ContainingGroup { get; private set; }

        private readonly BindableVariable<float> _largestInteractionStrength = new();
        public IReadOnlyBindableVariable<float> LargestInteractionStrength => _largestInteractionStrength;
        #endregion

        #region Fields
        private readonly Dictionary<IVXRSelectInteractable, Pose> _attachPoseOnSelect = new();
        private readonly Dictionary<IVXRSelectInteractable, Pose> _localAttachPoseOnSelect = new();
        private readonly HashSetList<IXRInteractionStrengthInteractable> _interactionStrengthInteractables = new();
        private readonly Dictionary<IVXRInteractable, float> _interactionStrengths = new();

        private IVXRSelectInteractable _manualInteractionInteractable;
        #endregion

        #region Events
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
        /// Interactors. An example would be having an Interactable use <see cref="VXRBaseInteractable.MovementType.VelocityTracking"/>
        /// so it does not move through geometry with a Collider when interacting with it using a Ray or Direct Interactor,
        /// but have a Socket Interactor override the movement type to be <see cref="VXRBaseInteractable.MovementType.Instantaneous"/>
        /// for reduced movement latency.
        /// </remarks>
        /// <seealso cref="VXRGrabInteractable.movementType"/>
        public Func<MovementType> SelectedInteractableMovementTypeOverride { get; set; }
        #endregion

        #region - Initialization -
        protected override void Awake()
        {
            base.Awake();
            _selectFilters.RegisterReferences(_startingSelectFilters, this);
        }

        protected virtual void Start()
        {
            if (InteractionManager != null && _startingSelectedInteractable != null)
            {
                InteractionManager.SelectEnter(this, _startingSelectedInteractable);
            }
        }
        #endregion

        #region - Processing -
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) return;

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
            return ProcessSelectFilters(interactable) && (Composite == null || Composite.CanSelect(interactable));
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
        public bool IsSelecting(IVXRInteractable interactable) => interactable is IVXRSelectInteractable selectable && IsSelecting(selectable);

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
            Debug.Log($"{Handedness} Hand Select Exiting: {args.GetinteractableObject()}");
            var removed = _interactablesSelected.Remove(args.GetinteractableObject());
            Debug.Assert(removed, "An Interactor received a Select Exit event for an Interactable that it was not selecting.", this);

            if (_interactionStrengthInteractables.Count > 0 &&
                args.GetinteractableObject() is IXRInteractionStrengthInteractable interactionStrengthInteractable &&
                !Composite.IsHovering(interactionStrengthInteractable))
            {
                _interactionStrengthInteractables.Remove(interactionStrengthInteractable);
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
            Debug.Log($"{Handedness} Hand Select Exited: {args.GetinteractableObject()}");
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

        private void OnSelectPoseEntered(SelectEnterEventArgs args)
        {
            if (_posingEnabled)
            {
                if (args.InteractableObject is VXRSelectInteractable interactable && interactable.TryGetOverrideHoverPose(out var pose, out var duration))
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

        #region - Force Selection -
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
    }
}
