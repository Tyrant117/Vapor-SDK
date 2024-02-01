using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using VaporInspector;

namespace VaporXR
{
    /// <summary>
    /// An component that represents an Interactor component that can activate
    /// an Interactable component. Not to be confused with the active state of a GameObject,
    /// an activate event in this context refers to a contextual command action, such as
    /// toggling a flashlight on and off.
    /// </summary>
    /// <seealso cref="IXRActivateInteractable"/>
    public class VXRInputInteractor : VXRBaseInteractor
    {
        /// <summary>
        /// This defines the type of input that triggers an interaction.
        /// </summary>
        /// <seealso cref="VXRInputInteractor.SelectActionTrigger"/>
        public enum InputTriggerType
        {
            /// <summary>
            /// Unity will consider the input active while the button is pressed.
            /// A user can hold the button before the interaction is possible
            /// and still trigger the interaction when it is possible.
            /// </summary>
            /// <remarks>
            /// When multiple interactors select an interactable at the same time and that interactable's
            /// <see cref="InteractableSelectMode"/> is set to <see cref="InteractableSelectMode.Single"/>, you may
            /// experience undesired behavior of selection repeatedly passing between the interactors and the select
            /// interaction events firing each frame. State Change is the recommended and default option. 
            /// </remarks>
            /// <seealso cref="InteractionState.active"/>
            /// <seealso cref="InteractableSelectMode"/>
            State,

            /// <summary>
            /// Unity will consider the input active only on the frame the button is pressed,
            /// and if successful remain engaged until the input is released.
            /// A user must press the button while the interaction is possible to trigger the interaction.
            /// They will not trigger the interaction if they started pressing the button before the interaction was possible.
            /// </summary>
            /// <seealso cref="InteractionState.activatedThisFrame"/>
            StateChange,

            /// <summary>
            /// The interaction starts on the frame the input is pressed
            /// and remains engaged until the second time the input is pressed.
            /// </summary>
            Toggle,

            /// <summary>
            /// The interaction starts on the frame the input is pressed
            /// and remains engaged until the second time the input is released.
            /// </summary>
            Sticky,
        }

        /// <summary>
        /// Interpreted input from an input reader. Represents the logical state of an interaction input,
        /// such as the select input, which may not be the same as the physical state of the input.
        /// </summary>
        /// <seealso cref="InputTriggerType"/>
        public class LogicalInputState
        {
            /// <summary>
            /// Whether the logical input state is currently active.
            /// </summary>
            public bool Active { get; private set; }

            private InputTriggerType _mode;
            /// <summary>
            /// The type of input
            /// </summary>
            public InputTriggerType Mode
            {
                get => _mode;
                set
                {
                    if (_mode == value) return;
                    
                    _mode = value;
                    Refresh();
                }
            }

            /// <summary>
            /// Read whether the button is currently performed, which typically means whether the button is being pressed.
            /// This is typically true for multiple frames.
            /// </summary>
            /// <seealso cref="IXRInputButtonReader.ReadIsPerformed"/>
            public bool IsPerformed { get; private set; }

            /// <summary>
            /// Read whether the button performed this frame, which typically means whether the button started being pressed during this frame.
            /// This is typically only true for one single frame.
            /// </summary>
            /// <seealso cref="IXRInputButtonReader.ReadWasPerformedThisFrame"/>
            public bool WasPerformedThisFrame { get; private set; }

            /// <summary>
            /// Read whether the button stopped performing this frame, which typically means whether the button stopped being pressed during this frame.
            /// This is typically only true for one single frame.
            /// </summary>
            public bool WasUnperformedThisFrame { get; private set; }

            private bool _hasSelection;

            private float _timeAtPerformed;
            private float _timeAtUnperformed;

            private bool _toggleActive;
            private bool _toggleDeactivatedThisFrame;
            private bool _waitingForDeactivate;

            // Add wasUnperformedThisFrame when Input System 1.8.0 is available since this method may not be called every frame.
            // Kept internal until then to avoid breaking changes.
            // See https://github.com/Unity-Technologies/InputSystem/pull/1795
            public void UpdateInput(bool performed, bool performedThisFrame, bool hasSelection) =>
                UpdateInput(performed, performedThisFrame, hasSelection, Time.realtimeSinceStartup);

            private void UpdateInput(bool performed, bool performedThisFrame, bool hasSelection, float realtime)
            {
                var prevPerformed = IsPerformed;

                IsPerformed = performed;
                WasPerformedThisFrame = performedThisFrame;
                WasUnperformedThisFrame = prevPerformed && !performed;
                _hasSelection = hasSelection;

                if (WasPerformedThisFrame)
                    _timeAtPerformed = realtime;

                if (WasUnperformedThisFrame)
                    _timeAtUnperformed = realtime;

                _toggleDeactivatedThisFrame = false;
                if (Mode == InputTriggerType.Toggle || Mode == InputTriggerType.Sticky)
                {
                    if (_toggleActive && performedThisFrame)
                    {
                        _toggleActive = false;
                        _toggleDeactivatedThisFrame = true;
                        _waitingForDeactivate = true;
                    }

                    if (WasUnperformedThisFrame)
                        _waitingForDeactivate = false;
                }

                Refresh();
            }

            public void UpdateHasSelection(bool hasSelection)
            {
                if (_hasSelection == hasSelection)
                    return;

                // Reset toggle values when no longer selecting
                // (can happen by another Interactor taking the Interactable or through method calls).
                _hasSelection = hasSelection;
                _toggleActive = hasSelection;
                _waitingForDeactivate = false;

                Refresh();
            }

            private void Refresh()
            {
                switch (Mode)
                {
                    case InputTriggerType.State:
                        Active = IsPerformed;
                        break;

                    case InputTriggerType.StateChange:
                        Active = WasPerformedThisFrame || (_hasSelection && !WasUnperformedThisFrame);
                        break;

                    case InputTriggerType.Toggle:
                        Active = _toggleActive || (WasPerformedThisFrame && !_toggleDeactivatedThisFrame);
                        break;

                    case InputTriggerType.Sticky:
                        Active = _toggleActive || _waitingForDeactivate || WasPerformedThisFrame;
                        break;

                    default:
                        Assert.IsTrue(false, $"Unhandled {nameof(InputTriggerType)}={Mode}");
                        break;
                }
            }
        }

        #region Inspector
        [SerializeField, BoxGroup("Components"), AutoReference(searchParents: true)]
        private VXRInputDeviceUpdateProvider _updateProvider;

        [SerializeField, FoldoutGroup("Interaction")]
        private TargetPriorityMode _targetPriorityMode;
        
        [SerializeField, FoldoutGroup("Input")] 
        private ButtonInputProvider _selectInput;
        [SerializeField, FoldoutGroup("Input")]
        private InputTriggerType _selectActionTrigger = InputTriggerType.StateChange;
        
        [SerializeField, FoldoutGroup("Input")]
        private ButtonInputProvider _activateInput;
        [SerializeField, FoldoutGroup("Input")]
        private bool _allowHoveredActivate;
        #endregion

        #region Properties
        /// <summary>
        /// Update provider used for polling input for the button providers.
        /// </summary>
        public VXRInputDeviceUpdateProvider UpdateProvider
        {
            get => _updateProvider;
            set => _updateProvider = value;
        }
        
        /// <summary>
        /// Input to use for selecting an interactable.
        /// </summary>
        public ButtonInputProvider SelectInput
        {
            get => _selectInput;
            set => _selectInput = value;
        }

        
        /// <summary>
        /// Input to use for activating an interactable.
        /// This can be used to trigger a secondary action on an interactable object,
        /// such as pulling a trigger on a ball launcher after picking it up.
        /// </summary>
        public ButtonInputProvider ActivateInput
        {
            get => _activateInput;
            set => _activateInput = value;
        }

        
        /// <summary>
        /// Choose how Unity interprets the select input.
        /// Controls between different input styles for determining if this interactor can select,
        /// such as whether the button is currently held or whether the button toggles select upon being pressed.
        /// </summary>
        /// <seealso cref="InputTriggerType"/>
        /// <seealso cref="LogicalSelectState"/>
        public InputTriggerType SelectActionTrigger
        {
            get => _selectActionTrigger;
            set => _selectActionTrigger = value;
        }

        
        /// <summary>
        /// Controls whether to send activate and deactivate events to interactables
        /// that this interactor is hovered over but not selected when there is no current selection.
        /// By default, the interactor will only send activate and deactivate events to interactables that it's selected.
        /// </summary>
        /// <seealso cref="AllowActivate"/>
        /// <seealso cref="GetActivateTargets"/>
        public bool AllowHoveredActivate
        {
            get => _allowHoveredActivate;
            set => _allowHoveredActivate = value;
        }
        
         /// <inheritdoc />
        public override TargetPriorityMode TargetPriorityMode
        {
            get => _targetPriorityMode;
            set => _targetPriorityMode = value;
        }

        /// <summary>
        /// Defines whether this interactor allows sending activate and deactivate events.
        /// </summary>
        /// <seealso cref="AllowHoveredActivate"/>
        /// <seealso cref="ShouldActivate"/>
        /// <seealso cref="ShouldDeactivate"/>
        public bool AllowActivate { get; set; } = true;

        public override bool IsSelectActive
        {
            get
            {
                if (!base.IsSelectActive)
                    return false;

                if (IsPerformingManualInteraction)
                    return true;

                LogicalSelectState.Mode = _selectActionTrigger;
                return LogicalSelectState.Active;
            }
        }

        /// <summary>
        /// (Read Only) Indicates whether this interactor is in a state where it should send the activate event this frame.
        /// </summary>
        public virtual bool ShouldActivate
        {
            get
            {
                if (AllowActivate && (HasSelection || _allowHoveredActivate && HasHover))
                {
                    return LogicalActivateState.WasPerformedThisFrame;
                }

                return false;
            }
        }

        /// <summary>
        /// (Read Only) Indicates whether this interactor is in a state where it should send the deactivate event this frame.
        /// </summary>
        public virtual bool ShouldDeactivate
        {
            get
            {
                if (AllowActivate && (HasSelection || _allowHoveredActivate && HasHover))
                {
                    return LogicalActivateState.WasUnperformedThisFrame;
                }

                return false;
            }
        }

        /// <summary>
        /// The logical state of the select input.
        /// </summary>
        /// <seealso cref="SelectInput"/>
        public LogicalInputState LogicalSelectState { get; } = new();

        /// <summary>
        /// The logical state of the activate input.
        /// </summary>
        /// <seealso cref="ActivateInput"/>
        public LogicalInputState LogicalActivateState { get; } = new();

        // /// <summary>
        // /// The list of button input readers used by this interactor. This interactor will automatically enable or disable direct actions
        // /// if that mode is used during <see cref="OnEnable"/> and <see cref="OnDisable"/>.
        // /// </summary>
        // /// <seealso cref="XRInputButtonReader.EnableDirectActionIfModeUsed"/>
        // /// <seealso cref="XRInputButtonReader.DisableDirectActionIfModeUsed"/>
        // protected List<ButtonInputProvider> ButtonReaders { get; } = new();
        // protected List<XRInputDeviceValueReader> ValueReaders { get; } = new();
        #endregion

        #region Fields
        private static readonly List<IXRActivateInteractable> s_ActivateTargets = new();
        
        private readonly LinkedPool<ActivateEventArgs> _activateEventArgs = new(() => new ActivateEventArgs(), collectionCheck: false);
        private readonly LinkedPool<DeactivateEventArgs> _deactivateEventArgs = new(() => new DeactivateEventArgs(), collectionCheck: false);

        private VXRInteractorAudioFeedback _audioFeedback;
        private VXRInteractorHapticFeedback _hapticFeedback;

        private AudioSource _audioSource;
        private HapticImpulsePlayer _hapticImpulsePlayer;
        #endregion


        #region - Initialization -
        protected override void Awake()
        {
            TargetsForSelection = new List<IXRSelectInteractable>();

            base.Awake();

            // ButtonReaders.Add(_selectInput);
            // ButtonReaders.Add(_activateInput);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _selectInput.BindToUpdateEvent(_updateProvider);
            _activateInput.BindToUpdateEvent(_updateProvider);
        }

        protected override void OnDisable()
        {
            _selectInput.UnbindUpdateEvent();
            _activateInput.UnbindUpdateEvent();
            base.OnDisable();
        }
        #endregion

        #region - Processing -
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) return;
            
            LogicalSelectState.UpdateInput(_selectInput.CurrentState.Active, _selectInput.CurrentState.ActivatedThisFrame, HasSelection);
            LogicalActivateState.UpdateInput(_activateInput.CurrentState.Active, _activateInput.CurrentState.ActivatedThisFrame, HasSelection);
        }

        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractor(updatePhase);
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) return;
            
            // Send activate/deactivate events as necessary.
            if (!AllowActivate) return;
                
            var sendActivate = ShouldActivate;
            var sendDeactivate = ShouldDeactivate;
            if (!sendActivate && !sendDeactivate) return;
                    
            GetActivateTargets(s_ActivateTargets);

            if (sendActivate)
            {
                SendActivateEvent(s_ActivateTargets);
            }

            // Note that this makes it possible for an interactable to receive an OnDeactivated event
            // but not the earlier OnActivated event if it was selected afterward.
            if (sendDeactivate)
            {
                SendDeactivateEvent(s_ActivateTargets);
            }
        }
        #endregion

        #region - Selection -
        /// <inheritdoc />
        public override void OnSelectEntering(SelectEnterEventArgs args)
        {
            base.OnSelectEntering(args);

            LogicalSelectState.UpdateHasSelection(true);
        }

        /// <inheritdoc />
        public override void OnSelectExiting(SelectExitEventArgs args)
        {
            base.OnSelectExiting(args);

            // Wait until all selections have been exited in case multiple selections are allowed.
            if (HasSelection)
            {
                return;
            }

            LogicalSelectState.UpdateHasSelection(false);
        }
        #endregion

        #region - Activation -
        private void SendActivateEvent(List<IXRActivateInteractable> targets)
        {
            foreach (var interactable in targets)
            {
                if (interactable == null || interactable as Object == null || !interactable.CanActivate)
                {
                    continue;
                }

                using (_activateEventArgs.Get(out var args))
                {
                    args.interactorObject = this;
                    args.interactableObject = interactable;
                    interactable.OnActivated(args);
                }
            }
        }

        private void SendDeactivateEvent(List<IXRActivateInteractable> targets)
        {
            foreach (var interactable in targets)
            {
                if (interactable == null || interactable as Object == null || !interactable.CanActivate)
                {
                    continue;
                }

                using (_deactivateEventArgs.Get(out var args))
                {
                    args.interactorObject = this;
                    args.interactableObject = interactable;
                    interactable.OnDeactivated(args);
                }
            }
        }

        /// <summary>
        /// Retrieve the list of Interactables that this Interactor could possibly activate or deactivate this frame.
        /// </summary>
        /// <param name="targets">The results list to populate with Interactables that are valid for activate or deactivate.</param>
        /// <remarks>
        /// When implementing this method, Unity expects you to clear <paramref name="targets"/> before adding to it.
        /// </remarks>
        public virtual void GetActivateTargets(List<IXRActivateInteractable> targets)
        {
            targets.Clear();
            if (HasSelection)
            {
                foreach (var interactable in InteractablesSelected)
                {
                    if (interactable is IXRActivateInteractable activateInteractable)
                    {
                        targets.Add(activateInteractable);
                    }
                }
            }
            else if (_allowHoveredActivate && HasHover)
            {
                foreach (var interactable in InteractablesHovered)
                {
                    if (interactable is IXRActivateInteractable activateInteractable)
                    {
                        targets.Add(activateInteractable);
                    }
                }
            }
        }
        #endregion

        #region - Audio -
        private void GetOrCreateAudioSource()
        {
            if (!TryGetComponent(out _audioSource))
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.loop = false;
            _audioSource.playOnAwake = false;
        }
        
        /// <summary>
        /// Play an <see cref="AudioClip"/>.
        /// </summary>
        /// <param name="audioClip">The clip to play.</param>
        protected virtual void PlayAudio(AudioClip audioClip)
        {
            if (audioClip == null)
            {
                return;
            }

            if (_audioSource == null)
            {
                GetOrCreateAudioSource();
            }

            _audioSource.PlayOneShot(audioClip);
        }
        #endregion

        #region - Haptics -
        private void GetOrCreateHapticImpulsePlayer()
        {
            _hapticImpulsePlayer = HapticImpulsePlayer.GetOrCreateInHierarchy(gameObject);
        }
        
        /// <summary>
        /// Play a haptic impulse on the controller if one is available.
        /// </summary>
        /// <param name="amplitude">Amplitude (from 0.0 to 1.0) to play impulse at.</param>
        /// <param name="duration">Duration (in seconds) to play haptic impulse.</param>
        /// <returns>Returns <see langword="true"/> if successful. Otherwise, returns <see langword="false"/>.</returns>
        public bool SendHapticImpulse(float amplitude, float duration)
        {
            if (_hapticImpulsePlayer == null)
            {
                GetOrCreateHapticImpulsePlayer();
            }

            return _hapticImpulsePlayer.SendHapticImpulse(amplitude, duration);
        }
        #endregion
    }
}
