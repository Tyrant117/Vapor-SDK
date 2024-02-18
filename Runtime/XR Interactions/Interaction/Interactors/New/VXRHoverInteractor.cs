using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using VaporInspector;
using VaporXR.Utilities;
using Object = UnityEngine.Object;

namespace VaporXR.Interactors
{
    public class VXRHoverInteractor : VXRInteractor, IVXRHoverInteractor, IPoseSource
    {
        #region Inspector
        [FoldoutGroup("Posing"), SerializeField]
        private bool _posingEnabled;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_posingEnabled")]
        private VXRHand _hand;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_posingEnabled")]
        private HandPoseDatum _hoverPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_posingEnabled")]
        private float _hoverPoseDuration;

        [FoldoutGroup("Filters"), SerializeField]
        [RequireInterface(typeof(IXRHoverFilter))]
        private List<Object> _startingHoverFilters = new();
        #endregion

        #region Properties
        /// <summary>
        /// (Read Only) Indicates whether this Interactor is in a state where it could hover.
        /// </summary>
        public virtual bool IsHoverActive => HoverActive.Invoke();

        private readonly HashSetList<IXRHoverInteractable> _interactablesHovered = new();
        /// <summary>
        /// (Read Only) The list of Interactables that are currently being hovered over (may by empty).
        /// </summary>
        /// <remarks>
        /// You should treat this as a read only view of the list and should not modify it.
        /// It is exposed as a <see cref="List{T}"/> rather than an <see cref="IReadOnlyList{T}"/> to avoid GC Allocations
        /// when enumerating the list.
        /// </remarks>
        /// <seealso cref="HasHover"/>
        /// <seealso cref="IXRHoverInteractable.InteractorsHovering"/>
        public List<IXRHoverInteractable> InteractablesHovered => (List<IXRHoverInteractable>)_interactablesHovered.AsList();

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
        /// <seealso cref="IXRHoverInteractable.IsHovered"/>
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
        #endregion

        #region Events
        public event Action<HoverEnterEventArgs> HoverEntering;
        public event Action<HoverEnterEventArgs> HoverEntered;

        public event Action<HoverExitEventArgs> HoverExiting;
        public event Action<HoverExitEventArgs> HoverExited;

        public Func<bool> HoverActive { get; set; }
        #endregion

        #region - Initialization -
        protected override void Awake()
        {
            base.Awake();
            HoverActive = AllowHover;
            _hoverFilters.RegisterReferences(_startingHoverFilters, this);
        }

        protected virtual bool AllowHover()
        {
            return true;
        }
        #endregion

        #region - Hovering -
        /// <summary>
        /// Determines if the Interactable is valid for hover this frame.
        /// </summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if the interactable can be hovered over this frame.</returns>
        /// <seealso cref="IXRHoverInteractable.IsHoverableBy"/>
        public virtual bool CanHover(IXRHoverInteractable interactable)
        {
            return ProcessHoverFilters(interactable) && (Composite == null || Composite.CanHover(interactable));
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
        public bool IsHovering(IXRHoverInteractable interactable) => _interactablesHovered.Contains(interactable);

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
        public bool IsHovering(IXRInteractable interactable) => interactable is IXRHoverInteractable hoverable && IsHovering(hoverable);

        /// <summary>
        /// Returns the processing value of the filters in <see cref="HoverFilters"/> for this Interactor and the
        /// given Interactable.
        /// </summary>
        /// <param name="interactable">The Interactable to be validated by the hover filters.</param>
        /// <returns>
        /// Returns <see langword="true"/> if all processed filters also return <see langword="true"/>, or if
        /// <see cref="HoverFilters"/> is empty. Otherwise, returns <see langword="false"/>.
        /// </returns>
        protected bool ProcessHoverFilters(IXRHoverInteractable interactable)
        {
            return XRFilterUtility.Process(_hoverFilters, this, interactable);
        }
        #endregion

        #region - Posing -
        private void OnHoverPoseEntered(HoverEnterEventArgs args)
        {
            if (_posingEnabled)
            {
                if (args.interactableObject is VXRHoverInteractable interactable && interactable.TryGetOverrideHoverPose(out var pose, out var duration))
                {
                    _hand.RequestHandPose(HandPoseType.Hover, this, pose.Value, duration: duration);
                }
                else
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
            Debug.Log($"{Handedness} Hand Hover Entering: {args.interactableObject}");

            var added = _interactablesHovered.Add(args.interactableObject);
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
            Debug.Log($"{Handedness} Hand Hover Entered: {args.interactableObject}");
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
            Debug.Log($"{Handedness} Hand Hover Exiting: {args.interactableObject}");
            var removed = _interactablesHovered.Remove(args.interactableObject);
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
            Debug.Log($"{Handedness} Hand Hover Exited: {args.interactableObject}");
            HoverExited?.Invoke(args);
            OnHoverPoseExited(args);
        }
        #endregion
    }
}
