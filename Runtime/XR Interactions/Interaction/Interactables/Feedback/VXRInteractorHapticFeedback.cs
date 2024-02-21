using UnityEngine;
using VaporInspector;
using VaporXR.Interaction;

namespace VaporXR
{
    // ReSharper disable once InconsistentNaming
    public class VXRInteractorHapticFeedback : MonoBehaviour
    {
        #region Inspector
        [SerializeField, FoldoutGroup("Components"), AutoReference(searchParents: true)]
        [RichTextTooltip("The <cls>VXRBaseInteractor</cls> component to that the events are subscribed to.")]
        private Interactor _interactorSourceObject;

        [SerializeField, FoldoutGroup("Components"), AutoReference(searchParents: true)]
        [RichTextTooltip("The <cls>HapticImpulsePlayer</cls> component to use to play haptic impulses.")]
        private HapticImpulsePlayer _hapticImpulsePlayer;
        
        [SerializeField, FoldoutGroup("Hover"), LabelWidth] 
        [RichTextTooltip("Whether to allow hover haptics to play while the interactor is selecting an interactable.")]
        private bool _allowHoverHapticsWhileSelecting;
        [SerializeField, FoldoutGroup("Hover")] 
        [RichTextTooltip("Whether to play a haptic impulse when the interactor starts hovering over an interactable.")]
        private bool _playHoverEntered;
        [SerializeField, FoldoutGroup("Hover"), ShowIf("%_playHoverEntered"), Margins(left:15)] 
        [RichTextTooltip("The haptic impulse to play when the interactor starts hovering over an interactable.")]
        private HapticImpulseData _hoverEnteredData = new() { Amplitude = 0.25f, Duration = 0.1f, };
        [SerializeField, FoldoutGroup("Hover")]
        [RichTextTooltip("Whether to play a haptic impulse when the interactor stops hovering over an interactable without being canceled.")]
        private bool _playHoverExited;
        [SerializeField, FoldoutGroup("Hover"), ShowIf("%_playHoverExited"), Margins(left:15)] 
        [RichTextTooltip("The haptic impulse to play when the interactor stops hovering over an interactable without being canceled.")]
        private HapticImpulseData _hoverExitedData = new() { Amplitude = 0.25f, Duration = 0.1f, };
        [SerializeField, FoldoutGroup("Hover")] 
        [RichTextTooltip("Whether to play a haptic impulse when the interactor stops hovering over an interactable due to being canceled.")]
        private bool _playHoverCanceled;
        [SerializeField, FoldoutGroup("Hover"), ShowIf("%_playHoverCanceled"), Margins(left:15)]
        [RichTextTooltip("The haptic impulse to play when the interactor stops hovering over an interactable due to being canceled.")]
        private HapticImpulseData _hoverCanceledData = new() { Amplitude = 0.25f, Duration = 0.1f, };
        
        [SerializeField, FoldoutGroup("Select")]
        [RichTextTooltip("Whether to play a haptic impulse when the interactor starts selecting an interactable.")]
        private bool _playSelectEntered;
        [SerializeField, FoldoutGroup("Select"), ShowIf("%_playSelectEntered"), Margins(left:15)] 
        [RichTextTooltip("The haptic impulse to play when the interactor starts selecting an interactable.")]
        private HapticImpulseData _selectEnteredData = new() { Amplitude = 0.5f, Duration = 0.1f, };
        [SerializeField, FoldoutGroup("Select")] 
        [RichTextTooltip("Whether to play a haptic impulse when the interactor stops selecting an interactable without being canceled.")]
        private bool _playSelectExited;
        [SerializeField, FoldoutGroup("Select"), ShowIf("%_playSelectExited"), Margins(left:15)] 
        [RichTextTooltip("The haptic impulse to play when the interactor stops selecting an interactable without being canceled.")]
        private HapticImpulseData _selectExitedData = new() { Amplitude = 0.5f, Duration = 0.1f, };
        [SerializeField, FoldoutGroup("Select")] 
        [RichTextTooltip("Whether to play a haptic impulse when the interactor stops selecting an interactable due to being canceled.")]
        private bool _playSelectCanceled;
        [SerializeField, FoldoutGroup("Select"), ShowIf("%_playSelectCanceled"), Margins(left:15)] 
        [RichTextTooltip("The haptic impulse to play when the interactor stops selecting an interactable due to being canceled.")]
        private HapticImpulseData _selectCanceledData = new() { Amplitude = 0.5f, Duration = 0.1f, };
        #endregion

        #region Properties
        /// <summary>
        /// The Haptic Impulse Player component to use to play haptic impulses.
        /// </summary>
        public HapticImpulsePlayer HapticImpulsePlayer { get => _hapticImpulsePlayer; set => _hapticImpulsePlayer = value; }

        /// <summary>
        /// Whether to play a haptic impulse when the interactor starts selecting an interactable.
        /// </summary>
        public bool PlaySelectEntered { get => _playSelectEntered; set => _playSelectEntered = value; }

        /// <summary>
        /// The haptic impulse to play when the interactor starts selecting an interactable.
        /// </summary>
        public HapticImpulseData SelectEnteredData { get => _selectEnteredData; set => _selectEnteredData = value; }

        /// <summary>
        /// Whether to play a haptic impulse when the interactor stops selecting an interactable without being canceled.
        /// </summary>
        public bool PlaySelectExited { get => _playSelectExited; set => _playSelectExited = value; }

        /// <summary>
        /// The haptic impulse to play when the interactor stops selecting an interactable without being canceled.
        /// </summary>
        public HapticImpulseData SelectExitedData { get => _selectExitedData; set => _selectExitedData = value; }

        /// <summary>
        /// Whether to play a haptic impulse when the interactor stops selecting an interactable due to being canceled.
        /// </summary>
        public bool PlaySelectCanceled { get => _playSelectCanceled; set => _playSelectCanceled = value; }

        /// <summary>
        /// The haptic impulse to play when the interactor stops selecting an interactable due to being canceled.
        /// </summary>
        public HapticImpulseData SelectCanceledData { get => _selectCanceledData; set => _selectCanceledData = value; }

        /// <summary>
        /// Whether to play a haptic impulse when the interactor starts hovering over an interactable.
        /// </summary>
        public bool PlayHoverEntered { get => _playHoverEntered; set => _playHoverEntered = value; }

        /// <summary>
        /// The haptic impulse to play when the interactor starts hovering over an interactable.
        /// </summary>
        public HapticImpulseData HoverEnteredData { get => _hoverEnteredData; set => _hoverEnteredData = value; }

        /// <summary>
        /// Whether to play a haptic impulse when the interactor stops hovering over an interactable without being canceled.
        /// </summary>
        public bool PlayHoverExited { get => _playHoverExited; set => _playHoverExited = value; }

        /// <summary>
        /// The haptic impulse to play when the interactor stops hovering over an interactable without being canceled.
        /// </summary>
        public HapticImpulseData HoverExitedData { get => _hoverExitedData; set => _hoverExitedData = value; }

        /// <summary>
        /// Whether to play a haptic impulse when the interactor stops hovering over an interactable due to being canceled.
        /// </summary>
        public bool PlayHoverCanceled { get => _playHoverCanceled; set => _playHoverCanceled = value; }

        /// <summary>
        /// The haptic impulse to play when the interactor stops hovering over an interactable due to being canceled.
        /// </summary>
        public HapticImpulseData HoverCanceledData { get => _hoverCanceledData; set => _hoverCanceledData = value; }

        /// <summary>
        /// Whether to allow hover haptics to play while the interactor is selecting an interactable.
        /// </summary>
        public bool AllowHoverHapticsWhileSelecting { get => _allowHoverHapticsWhileSelecting; set => _allowHoverHapticsWhileSelecting = value; }
        #endregion

        #region - Initialization -
        protected void Awake()
        {
            if (_playSelectEntered || _playSelectExited || _playSelectCanceled || _playHoverEntered || _playHoverExited || _playHoverCanceled)
            {
                CreateHapticImpulsePlayer();
            }
        }

        protected void OnEnable()
        {
            Subscribe();
        }

        protected void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            _interactorSourceObject.HoverEntered += OnHoverEntered;
            _interactorSourceObject.HoverExited += OnHoverExited;

            _interactorSourceObject.SelectEntered += OnSelectEntered;
            _interactorSourceObject.SelectExited += OnSelectExited;
        }

        private void Unsubscribe()
        {
            _interactorSourceObject.HoverEntered -= OnHoverEntered;
            _interactorSourceObject.HoverExited -= OnHoverExited;

            _interactorSourceObject.SelectEntered -= OnSelectEntered;
            _interactorSourceObject.SelectExited -= OnSelectExited;
        }
        #endregion

        #region - Haptics -
        /// <summary>
        /// Sends a haptic impulse to the referenced haptic impulse player component.
        /// </summary>
        /// <param name="data">The parameters of the haptic impulse.</param>
        /// <returns>Returns <see langword="true"/> if successful. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="SendHapticImpulse(float,float,float)"/>
        protected void SendHapticImpulse(HapticImpulseData data)
        {
            if (data == null)
            {
                return;
            }

            SendHapticImpulse(data.Amplitude, data.Duration, data.Frequency);
        }

        /// <summary>
        /// Sends a haptic impulse to the referenced haptic impulse player component.
        /// </summary>
        /// <param name="amplitude">The desired motor amplitude that should be within a [0-1] range.</param>
        /// <param name="duration">The desired duration of the impulse in seconds.</param>
        /// <param name="frequency">The desired frequency of the impulse in Hz. A value of 0 means to use the default frequency of the device.</param>
        /// <returns>Returns <see langword="true"/> if successful. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="HapticImpulsePlayer.SendHapticImpulse(float,float,float)"/>
        protected void SendHapticImpulse(float amplitude, float duration, float frequency)
        {
            if (_hapticImpulsePlayer == null)
            {
                CreateHapticImpulsePlayer();
            }

            _hapticImpulsePlayer.SendHapticImpulse(amplitude, duration, frequency);
        }

        private void CreateHapticImpulsePlayer()
        {
            _hapticImpulsePlayer = HapticImpulsePlayer.GetOrCreateInHierarchy(gameObject);
        }
        #endregion

        #region - Hover -
        private bool IsHoverHapticsAllowed(Interactor interactor, Interactable interactable)
        {
            return _allowHoverHapticsWhileSelecting || !IsSelecting(interactor, interactable);
        }
        
        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (_playHoverEntered && IsHoverHapticsAllowed(args.InteractorObject, args.InteractableObject))
            {
                SendHapticImpulse(_hoverEnteredData);
            }
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            if (!IsHoverHapticsAllowed(args.InteractorObject, args.InteractableObject))
            {
                return;
            }

            if (_playHoverCanceled && args.IsCanceled)
            {
                SendHapticImpulse(_hoverCanceledData);
            }

            if (_playHoverExited && !args.IsCanceled)
            {
                SendHapticImpulse(_hoverExitedData);
            }
        }
        #endregion

        #region - Select -
        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (_playSelectEntered)
            {
                SendHapticImpulse(_selectEnteredData);
            }
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (_playSelectCanceled && args.IsCanceled)
            {
                SendHapticImpulse(_selectCanceledData);
            }

            if (_playSelectExited && !args.IsCanceled)
            {
                SendHapticImpulse(_selectExitedData);
            }
        }

        private static bool IsSelecting(Interactor interactor, Interactable interactable)
        {
            return interactor != null &&
                   interactor.IsSelecting(interactable);
        }
        #endregion
        
    }
}
