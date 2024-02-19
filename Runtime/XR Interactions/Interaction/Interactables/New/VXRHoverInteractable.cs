using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using VaporInspector;
using VaporXR.Interactors;
using VaporXR.Utilities;
using Object = UnityEngine.Object;

namespace VaporXR.Interactables
{
    public class VXRHoverInteractable : VXRInteractable, IVXRHoverInteractable
    {
        #region Inspector
        [FoldoutGroup("Posing"), SerializeField]
        private bool _overrideHoverPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_overrideHoverPose")]
        private HandPoseDatum _hoverPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_overrideHoverPose")]
        private float _hoverPoseDuration;

        [FoldoutGroup("Filters", order: 90), SerializeField, RequireInterface(typeof(IXRHoverFilter))]
        [RichTextTooltip("The hover filters that this object uses to automatically populate the <mth>HoverFilters</mth> List at startup (optional, may be empty)." +
            "\nAll objects in this list should implement the <itf>IXRHoverFilter</itf> interface.")]
        private List<Object> _startingHoverFilters = new();
        #endregion

        #region Properties
        // ***** Hovering *****
        public bool CanBeHovered => HoverableActive.Invoke();
        public bool IsHovered => _interactorsHovering.Count > 0;
        private readonly HashSetList<IVXRHoverInteractor> _interactorsHovering = new();
        public List<IVXRHoverInteractor> InteractorsHovering => (List<IVXRHoverInteractor>)_interactorsHovering.AsList();

        // ***** Filters *****
        private readonly ExposedRegistrationList<IXRHoverFilter> _hoverFilters = new() { BufferChanges = false };        
        public IXRFilterList<IXRHoverFilter> HoverFilters => _hoverFilters;
        #endregion

        #region Fields

        #endregion

        #region Events
        public event Action<HoverEnterEventArgs> FirstHoverEntered;
        public event Action<HoverEnterEventArgs> HoverEntering;
        public event Action<HoverEnterEventArgs> HoverEntered;

        public event Action<HoverExitEventArgs> HoverExiting;
        public event Action<HoverExitEventArgs> HoverExited;
        public event Action<HoverExitEventArgs> LastHoverExited;

        public Func<bool> HoverableActive { get; set; }
        #endregion

        #region - Initialization -
        protected override void Awake()
        {
            base.Awake();
            HoverableActive = AllowHover;

            // Setup the starting filters
            _hoverFilters.RegisterReferences(_startingHoverFilters, this);
        }

        protected virtual bool AllowHover()
        {
            return true;
        }
        #endregion

        #region - Hover -        
        public bool IsHoverableBy(IVXRHoverInteractor interactor)
        {
            return CanBeHovered && ProcessHoverFilters(interactor) && (Composite == null || Composite.IsHoverableBy(interactor));
        }
        
        public bool IsHoveredBy(IVXRHoverInteractor interactor) => _interactorsHovering.Contains(interactor);        

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

        #region - Posing -
        public bool TryGetOverrideHoverPose(out HandPoseDatum pose, out float duration)
        {
            pose = _hoverPose;
            duration = _hoverPoseDuration;
            return _overrideHoverPose;
        }
        #endregion

        #region - Events -        
        public virtual void OnHoverEntering(HoverEnterEventArgs args)
        {
            var added = _interactorsHovering.Add(args.interactorObject);
            Debug.Assert(added, "An Interactable received a Hover Enter event for an Interactor that was already hovering over it.", this);

            //if (args.interactorObject.TryGetSelectInteractor(out var selectInteractor))
            //{
            //    _variableSelectInteractors.Add(selectInteractor);
            //}

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
            var removed = _interactorsHovering.Remove(args.interactorObject);
            Debug.Assert(removed, "An Interactable received a Hover Exit event for an Interactor that was not hovering over it.", this);

            //if (_variableSelectInteractors.Count > 0 &&
            //    args.interactorObject.TryGetSelectInteractor(out var variableSelectInteractor) &&
            //    !IsSelectedBy(variableSelectInteractor))
            //{
            //    _variableSelectInteractors.Remove(variableSelectInteractor);
            //}

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
        #endregion
    }
}
