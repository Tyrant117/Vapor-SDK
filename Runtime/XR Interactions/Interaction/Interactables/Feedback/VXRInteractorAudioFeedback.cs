using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using VaporInspector;
using VaporXR.Interactors;
using VaporXR.Utilities;

namespace VaporXR
{
    // ReSharper disable once InconsistentNaming
    public class VXRInteractorAudioFeedback : MonoBehaviour
    {
        [SerializeField]
        [RequireInterface(typeof(VXRBaseInteractor))]
        Object m_InteractorSourceObject;

        [SerializeField]
        AudioSource m_AudioSource;

        /// <summary>
        /// The Audio Source component to use to play audio clips.
        /// </summary>
        public AudioSource audioSource
        {
            get => m_AudioSource;
            set => m_AudioSource = value;
        }

        [SerializeField]
        bool m_PlaySelectEntered;

        /// <summary>
        /// Whether to play a sound when the interactor starts selecting an interactable.
        /// </summary>
        public bool playSelectEntered
        {
            get => m_PlaySelectEntered;
            set => m_PlaySelectEntered = value;
        }

        [SerializeField]
        AudioClip m_SelectEnteredClip;

        /// <summary>
        /// The audio clip to play when the interactor starts selecting an interactable.
        /// </summary>
        public AudioClip selectEnteredClip
        {
            get => m_SelectEnteredClip;
            set => m_SelectEnteredClip = value;
        }

        [SerializeField]
        bool m_PlaySelectExited;

        /// <summary>
        /// Whether to play a sound when the interactor stops selecting an interactable without being canceled.
        /// </summary>
        public bool playSelectExited
        {
            get => m_PlaySelectExited;
            set => m_PlaySelectExited = value;
        }

        [SerializeField]
        AudioClip m_SelectExitedClip;

        /// <summary>
        /// The audio clip to play when the interactor stops selecting an interactable without being canceled.
        /// </summary>
        public AudioClip selectExitedClip
        {
            get => m_SelectExitedClip;
            set => m_SelectExitedClip = value;
        }

        [SerializeField]
        bool m_PlaySelectCanceled;

        /// <summary>
        /// Whether to play a sound when the interactor stops selecting an interactable due to being canceled.
        /// </summary>
        public bool playSelectCanceled
        {
            get => m_PlaySelectCanceled;
            set => m_PlaySelectCanceled = value;
        }

        [SerializeField]
        AudioClip m_SelectCanceledClip;

        /// <summary>
        /// The audio clip to play when the interactor stops selecting an interactable due to being canceled.
        /// </summary>
        public AudioClip selectCanceledClip
        {
            get => m_SelectCanceledClip;
            set => m_SelectCanceledClip = value;
        }

        [SerializeField]
        bool m_PlayHoverEntered;

        /// <summary>
        /// Whether to play a sound when the interactor starts hovering over an interactable.
        /// </summary>
        public bool playHoverEntered
        {
            get => m_PlayHoverEntered;
            set => m_PlayHoverEntered = value;
        }

        [SerializeField]
        AudioClip m_HoverEnteredClip;

        /// <summary>
        /// The audio clip to play when the interactor starts hovering over an interactable.
        /// </summary>
        public AudioClip hoverEnteredClip
        {
            get => m_HoverEnteredClip;
            set => m_HoverEnteredClip = value;
        }

        [SerializeField]
        bool m_PlayHoverExited;

        /// <summary>
        /// Whether to play a sound when the interactor stops hovering over an interactable without being canceled.
        /// </summary>
        public bool playHoverExited
        {
            get => m_PlayHoverExited;
            set => m_PlayHoverExited = value;
        }

        [SerializeField]
        AudioClip m_HoverExitedClip;

        /// <summary>
        /// The audio clip to play when the interactor stops hovering over an interactable without being canceled.
        /// </summary>
        public AudioClip hoverExitedClip
        {
            get => m_HoverExitedClip;
            set => m_HoverExitedClip = value;
        }

        [SerializeField]
        bool m_PlayHoverCanceled;

        /// <summary>
        /// Whether to play a sound when the interactor stops hovering over an interactable due to being canceled.
        /// </summary>
        public bool playHoverCanceled
        {
            get => m_PlayHoverCanceled;
            set => m_PlayHoverCanceled = value;
        }

        [SerializeField]
        AudioClip m_HoverCanceledClip;

        /// <summary>
        /// The audio clip to play when the interactor stops hovering over an interactable due to being canceled.
        /// </summary>
        public AudioClip hoverCanceledClip
        {
            get => m_HoverCanceledClip;
            set => m_HoverCanceledClip = value;
        }

        [SerializeField]
        bool m_AllowHoverAudioWhileSelecting;

        /// <summary>
        /// Whether to allow hover audio to play while the interactor is selecting an interactable.
        /// </summary>
        public bool allowHoverAudioWhileSelecting
        {
            get => m_AllowHoverAudioWhileSelecting;
            set => m_AllowHoverAudioWhileSelecting = value;
        }

        readonly UnityObjectReferenceCache<VXRInteractor, Object> m_InteractorSource = new();

        [Conditional("UNITY_EDITOR")]
        protected void Reset()
        {
#if UNITY_EDITOR
            m_InteractorSourceObject = GetComponentInParent<VXRInteractor>(true) as Object;
            m_AudioSource = GetComponent<AudioSource>();
#endif
        }

        protected void Awake()
        {
            if (m_InteractorSourceObject == null)
                m_InteractorSourceObject = GetComponentInParent<VXRInteractor>(true) as Object;

            if (m_PlaySelectEntered || m_PlaySelectExited || m_PlaySelectCanceled ||
                m_PlayHoverEntered || m_PlayHoverExited || m_PlayHoverCanceled)
            {
                CreateAudioSource();
            }
        }

        protected void OnEnable()
        {
            Subscribe(GetInteractorSource());
        }

        protected void OnDisable()
        {
            Unsubscribe(GetInteractorSource());
        }

        /// <summary>
        /// Gets the interactor this behavior should subscribe to for events.
        /// </summary>
        /// <returns>Returns the interactor this behavior should subscribe to for events.</returns>
        /// <seealso cref="SetInteractorSource"/>
        public VXRInteractor GetInteractorSource()
        {
            return m_InteractorSource.Get(m_InteractorSourceObject);
        }

        /// <summary>
        /// Sets the interactor this behavior should subscribe to for events.
        /// </summary>
        /// <param name="interactor">The interactor this behavior should subscribe to for events.</param>
        /// <remarks>
        /// This also sets the serialized field to the given interactor as a Unity Object.
        /// </remarks>
        /// <seealso cref="GetInteractorSource"/>
        public void SetInteractorSource(VXRInteractor interactor)
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                Unsubscribe(m_InteractorSource.Get(m_InteractorSourceObject));
            }

            m_InteractorSource.Set(ref m_InteractorSourceObject, interactor);

            if (Application.isPlaying && isActiveAndEnabled)
            {
                Subscribe(interactor);
            }
        }

        /// <summary>
        /// Play the given audio clip.
        /// </summary>
        /// <param name="clip">The audio clip to play.</param>
        protected void PlayAudio(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (m_AudioSource == null)
            {
                CreateAudioSource();
            }

            m_AudioSource.PlayOneShot(clip);
        }

        private void CreateAudioSource()
        {
            if (!TryGetComponent(out m_AudioSource))
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
            }

            m_AudioSource.loop = false;
            m_AudioSource.playOnAwake = false;
        }

        private void Subscribe(VXRInteractor interactor)
        {
            if (interactor == null || (interactor is Object interactorObject && interactorObject == null))
                return;

            if (interactor is IVXRSelectInteractor selectInteractor)
            {
                selectInteractor.SelectEntered += (OnSelectEntered);
                selectInteractor.SelectExited += (OnSelectExited);
            }

            if (interactor is VXRHoverInteractor hoverInteractor)
            {
                hoverInteractor.HoverEntered += (OnHoverEntered);
                hoverInteractor.HoverExited += (OnHoverExited);
            }
        }

        private void Unsubscribe(VXRInteractor interactor)
        {
            if (interactor == null || (interactor is Object interactorObject && interactorObject == null))
                return;

            if (interactor is IVXRSelectInteractor selectInteractor)
            {
                selectInteractor.SelectEntered -= (OnSelectEntered);
                selectInteractor.SelectExited -= (OnSelectExited);
            }

            if (interactor is VXRHoverInteractor hoverInteractor)
            {
                hoverInteractor.HoverEntered -= (OnHoverEntered);
                hoverInteractor.HoverExited -= (OnHoverExited);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (m_PlaySelectEntered)
                PlayAudio(m_SelectEnteredClip);
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (m_PlaySelectCanceled && args.IsCanceled)
                PlayAudio(m_SelectCanceledClip);

            if (m_PlaySelectExited && !args.IsCanceled)
                PlayAudio(m_SelectExitedClip);
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (m_PlayHoverEntered && IsHoverAudioAllowed(args.interactorObject, args.interactableObject))
                PlayAudio(m_HoverEnteredClip);
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            if (!IsHoverAudioAllowed(args.interactorObject, args.interactableObject))
                return;

            if (m_PlayHoverCanceled && args.isCanceled)
                PlayAudio(m_HoverCanceledClip);

            if (m_PlayHoverExited && !args.isCanceled)
                PlayAudio(m_HoverExitedClip);
        }

        private bool IsHoverAudioAllowed(IVXRHoverInteractor interactor, IVXRInteractable interactable)
        {
            return m_AllowHoverAudioWhileSelecting || !IsSelecting(interactor, interactable);
        }

        private static bool IsSelecting(IVXRHoverInteractor interactor, IVXRInteractable interactable)
        {
            return interactor.Composite != null &&
                interactable is IVXRSelectInteractable selectable &&
                interactor.Composite.IsSelecting(selectable);
        }
    }
}
