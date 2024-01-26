using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.UI;

namespace VaporXR
{
    public class VXRTouchScreenInteractor : VXRBaseInteractor, IUIHoverInteractor, IPokeStateDataProvider, IAttachPointVelocityProvider
    {
        private readonly struct PokeCollision
        {
            public readonly Collider Collider;
            public readonly IXRInteractable Interactable;
            public readonly IXRPokeFilter Filter;
            public readonly bool HasPokeFilter;

            public PokeCollision(Collider collider, IXRInteractable interactable, IXRPokeFilter filter)
            {
                Collider = collider;
                Interactable = interactable;
                Filter = filter;
                HasPokeFilter = filter != null;
            }
        }

        /// <summary>
        /// Reusable list of interactables (used to process the valid targets when this interactor has a filter).
        /// </summary>
        private static readonly List<IXRInteractable> s_Results = new();

        #region Inspector
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("The depth threshold within which an interaction can begin to be evaluated as a poke.")]
        private float _pokeDepth = 0.1f;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("The width threshold within which an interaction can begin to be evaluated as a poke.")]
        private float _pokeWidth = 0.0075f;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("The width threshold within which an interaction can be evaluated as a poke select.")]
        private float _pokeSelectWidth = 0.015f;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("The radius threshold within which an interaction can be evaluated as a poke hover.")]
        private float _pokeHoverRadius = 0.015f;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("Distance along the poke interactable interaction axis that allows for a poke to be triggered sooner/with less precision.")]
        private float _pokeInteractionOffset = 0.005f;

        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("If <lw>true</lw>, this interactor will check for physical interaction.")]
        private bool _enablePhysicsTouch = true;
        [FoldoutGroup("Interaction"), SerializeField, ShowIf("%_enablePhysicsTouch")]
        [RichTextTooltip("Physics layer mask used for limiting poke sphere overlap.")]
        private LayerMask _physicsLayerMask = Physics.AllLayers;
        [FoldoutGroup("Interaction"), SerializeField, ShowIf("%_enablePhysicsTouch")]
        [RichTextTooltip("Determines whether the poke sphere overlap will hit triggers.")]
        private QueryTriggerInteraction _physicsTriggerInteraction = QueryTriggerInteraction.Ignore;

        [SerializeField, FoldoutGroup("Events")]
        private UIHoverEnterEvent _uiHoverEntered = new();
        [SerializeField, FoldoutGroup("Events")]
        private UIHoverExitEvent _uiHoverExited = new();

        [FoldoutGroup("Debug", order: 1000), SerializeField]
        [RichTextTooltip("Denotes whether or not debug visuals are enabled for this poke interactor.")]
        private bool _debugVisualizationsEnabled;
        #endregion

        #region Properties
        private BindableVariable<PokeStateData> _pokeStateData = new();
        /// <inheritdoc />
        public IReadOnlyBindableVariable<PokeStateData> PokeStateData => _pokeStateData;

        /// <summary>
        /// The tracker used to compute the velocity of the attach point.
        /// This behavior automatically updates this velocity tracker each frame during <see cref="PreprocessInteractor"/>.
        /// </summary>
        /// <seealso cref="GetAttachPointVelocity"/>
        /// <seealso cref="GetAttachPointAngularVelocity"/>
        protected IAttachPointVelocityTracker AttachPointVelocityTracker { get; set; } = new AttachPointVelocityTracker();
        #endregion

        #region Fields
        private GameObject _hoverDebugSphere;
        private MeshRenderer _hoverDebugRenderer;

        private Vector3 _lastPokeInteractionPoint;

        private bool _pokeCanSelect;
        private bool _firstFrame = true;
        private IXRSelectInteractable _currentPokeTarget;
        private IXRPokeFilter _currentPokeFilter;

        private readonly RaycastHit[] _sphereCastHits = new RaycastHit[25];
        private readonly Collider[] _overlapSphereHits = new Collider[25];
        private readonly List<PokeCollision> _pokeTargets = new List<PokeCollision>();
        private readonly List<IXRSelectFilter> _interactableSelectFilters = new List<IXRSelectFilter>();

        private RegisteredUIInteractorCache _registeredUIInteractorCache;
        private PhysicsScene _localPhysicsScene;
        #endregion

        #region Events
        /// <inheritdoc />
        public UIHoverEnterEvent UiHoverEntered { get => _uiHoverEntered; set => _uiHoverEntered = value; }

        /// <inheritdoc />
        public UIHoverExitEvent UiHoverExited { get => _uiHoverExited; set => _uiHoverExited = value; }

        // Used to avoid GC Alloc each frame in UpdateUIModel
        private Func<Vector3> _positionGetter;
        #endregion


        #region - Initialization -
        protected override void Awake()
        {
            base.Awake();
            _localPhysicsScene = gameObject.scene.GetPhysicsScene();
            _registeredUIInteractorCache = new RegisteredUIInteractorCache(this);
            _positionGetter = GetPokePosition;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDebugObjectVisibility(true);
            _firstFrame = true;

            _registeredUIInteractorCache.RegisterWithXRUIInputModule();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetDebugObjectVisibility(false);

            _registeredUIInteractorCache.UnregisterFromXRUIInputModule();
        }
        #endregion

        #region - Processing -
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) { return; }


            if (TryGetXROrigin(out var origin))
            {
                AttachPointVelocityTracker.UpdateAttachPointVelocityData(GetAttachTransform(null), origin);
            }
            else
            {
                AttachPointVelocityTracker.UpdateAttachPointVelocityData(GetAttachTransform(null));
            }

            IsInteractingWithUI = TrackedDeviceGraphicRaycaster.IsPokeInteractingWithUI(this);
            _pokeCanSelect = EvaluatePokeInteraction(out _currentPokeTarget, out _currentPokeFilter);
            ProcessPokeStateData();

        }

        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractor(updatePhase);
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) { return; }

            UpdateDebugVisuals();
        }

        private void ProcessPokeStateData()
        {
            PokeStateData newPokeStateData = default;
            if (IsInteractingWithUI)
            {
                TrackedDeviceGraphicRaycaster.TryGetPokeStateDataForInteractor(this, out newPokeStateData);
            }
            else if (_currentPokeFilter is not null and IPokeStateDataProvider pokeStateDataProvider)
            {
                newPokeStateData = pokeStateDataProvider.PokeStateData.Value;
            }
            _pokeStateData.Value = newPokeStateData;
        }
        #endregion

        #region - Interaction -
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            targets.Clear();

            if (!isActiveAndEnabled)
            {
                return;
            }

            foreach (var pokeCollision in _pokeTargets)
            {
                targets.Add(pokeCollision.Interactable);
            }

            var filter = TargetFilter;
            if (filter != null && filter.canProcess)
            {
                filter.Process(this, targets, s_Results);

                // Copy results elements to targets
                targets.Clear();
                targets.AddRange(s_Results);
            }
        }
        #endregion

        #region - Selection -
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return _pokeCanSelect && interactable == _currentPokeTarget && base.CanSelect(interactable);
        }
        #endregion

        #region - Poking -
        /// <summary>
        /// Evaluates whether or not an attempted poke interaction is valid.
        /// </summary>
        /// <param name="newHoveredInteractable">The newly hovered interactable.</param>
        /// <param name="newPokeFilter">The new poke filter.</param>
        /// <returns>
        /// Returns <see langword="true"/> if poke interaction can be completed.
        /// Otherwise, returns <see langword="false"/>.
        /// </returns>
        private bool EvaluatePokeInteraction(out IXRSelectInteractable newHoveredInteractable, out IXRPokeFilter newPokeFilter)
        {
            newHoveredInteractable = default;
            newPokeFilter = default;
            if (!_enablePhysicsTouch)
            {
                return false;
            }

            int sphereOverlapCount = EvaluateSphereOverlap();
            bool hasOverlap = sphereOverlapCount > 0;
            bool canCompletePokeInteraction = false;

            if (hasOverlap)
            {
                var smallestSqrDistance = float.MaxValue;
                int pokeTargetsCount = _pokeTargets.Count;
                IXRSelectInteractable closestInteractable = null;
                IXRPokeFilter closestPokeFilter = null;

                for (var i = 0; i < pokeTargetsCount; ++i)
                {
                    var interactable = _pokeTargets[i].Interactable;
                    if (interactable is IXRSelectInteractable selectable &&
                        interactable is IXRHoverInteractable hoverable && hoverable.IsHoverableBy(this))
                    {
                        var sqrDistance = interactable.GetDistanceSqrToInteractor(this);
                        if (sqrDistance < smallestSqrDistance)
                        {
                            smallestSqrDistance = sqrDistance;
                            closestInteractable = selectable;
                            closestPokeFilter = _pokeTargets[i].Filter;
                        }
                    }
                }

                if (closestInteractable != null)
                {
                    canCompletePokeInteraction = true;
                    newHoveredInteractable = closestInteractable;
                    newPokeFilter = closestPokeFilter;
                }
            }

            return canCompletePokeInteraction;
        }

        private int EvaluateSphereOverlap()
        {
            _pokeTargets.Clear();

            // Hover Check
            Vector3 pokeInteractionPoint = GetAttachTransform(null).position;
            Vector3 overlapStart = _lastPokeInteractionPoint;
            Vector3 interFrameEnd = pokeInteractionPoint;

            BurstPhysicsUtils.GetSphereOverlapParameters(overlapStart, interFrameEnd, out var normalizedOverlapVector, out var overlapSqrMagnitude, out var overlapDistance);

            // If no movement is recorded.
            // Check if spherecast size is sufficient for proper cast, or if first frame since last frame poke position will be invalid.
            if (_firstFrame || overlapSqrMagnitude < 0.001f)
            {
                var numberOfOverlaps = _localPhysicsScene.OverlapSphere(interFrameEnd, _pokeHoverRadius, _overlapSphereHits,
                    _physicsLayerMask, _physicsTriggerInteraction);

                for (var i = 0; i < numberOfOverlaps; ++i)
                {
                    if (FindPokeTarget(_overlapSphereHits[i], out var newPokeCollision))
                    {
                        _pokeTargets.Add(newPokeCollision);
                    }
                }
            }
            else
            {
                var numberOfOverlaps = _localPhysicsScene.SphereCast(
                    overlapStart,
                    _pokeHoverRadius,
                    normalizedOverlapVector,
                    _sphereCastHits,
                    overlapDistance,
                    _physicsLayerMask,
                    _physicsTriggerInteraction);

                for (var i = 0; i < numberOfOverlaps; ++i)
                {
                    if (FindPokeTarget(_sphereCastHits[i].collider, out var newPokeCollision))
                    {
                        _pokeTargets.Add(newPokeCollision);
                    }
                }
            }

            _lastPokeInteractionPoint = pokeInteractionPoint;
            _firstFrame = false;

            return _pokeTargets.Count;
        }

        private bool FindPokeTarget(Collider hitCollider, out PokeCollision newPokeCollision)
        {
            newPokeCollision = default;
            if (InteractionManager.TryGetInteractableForCollider(hitCollider, out var interactable))
            {
                if (interactable is VXRTouchScreenInteractable tsInteractable)
                {
                    var filter = tsInteractable.PokeFilter;
                    if (filter.canProcess)
                    {
                        newPokeCollision = new PokeCollision(hitCollider, interactable, filter);
                        return true;
                    }
                }
            }

            return false;
        }

        private Vector3 GetPokePosition()
        {
            return GetAttachTransform(null).position;
        }

        /// <summary>
        /// Last computed default attach point velocity, based on multi-frame sampling of the pose in world space.
        /// </summary>
        /// <returns>Returns the transformed attach point linear velocity.</returns>
        /// <seealso cref="GetAttachPointAngularVelocity"/>
        public Vector3 GetAttachPointVelocity()
        {
            if (TryGetXROrigin(out var origin))
            {
                return AttachPointVelocityTracker.GetAttachPointVelocity(origin);
            }
            return AttachPointVelocityTracker.GetAttachPointVelocity();
        }

        /// <summary>
        /// Last computed default attach point angular velocity, based on multi-frame sampling of the pose in world space.
        /// </summary>
        /// <returns>Returns the transformed attach point angular velocity.</returns>
        /// <seealso cref="GetAttachPointVelocity"/>
        public Vector3 GetAttachPointAngularVelocity()
        {
            if (TryGetXROrigin(out var origin))
            {
                return AttachPointVelocityTracker.GetAttachPointAngularVelocity(origin);
            }
            return AttachPointVelocityTracker.GetAttachPointAngularVelocity();
        }
        #endregion

        #region - UI -
        public virtual void UpdateUIModel(ref TrackedDeviceModel model)
        {
            if (!isActiveAndEnabled || this.IsBlockedByInteractionWithinGroup())
            {
                model.Reset(false);
                return;
            }

            var pokeInteractionTransform = GetAttachTransform(null);
            var position = pokeInteractionTransform.position;
            var orientation = pokeInteractionTransform.rotation;
            Vector3 startPoint = position;
            Vector3 penetrationDirection = orientation * Vector3.forward;
            Vector3 endPoint = startPoint + (penetrationDirection * _pokeDepth);

            model.position = position;
            model.orientation = orientation;
            model.positionGetter = _positionGetter;
            model.select = TrackedDeviceGraphicRaycaster.HasPokeSelect(this);
            model.raycastLayerMask = _physicsLayerMask;
            model.pokeDepth = _pokeDepth;
            model.interactionType = UIInteractionType.Poke;

            var raycastPoints = model.raycastPoints;
            raycastPoints.Clear();
            raycastPoints.Add(startPoint);
            raycastPoints.Add(endPoint);
        }

        public bool TryGetUIModel(out TrackedDeviceModel model)
        {
            return _registeredUIInteractorCache.TryGetUIModel(out model);
        }

        /// <summary>
        /// The <see cref="XRUIInputModule"/> calls this method when the Interactor begins hovering over a UI element.
        /// </summary>
        /// <param name="args">Event data containing the UI element that is being hovered over.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnUIHoverExited(UIHoverEventArgs)"/>
        public virtual void OnUIHoverEntered(UIHoverEventArgs args)
        {
            UiHoverEntered?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="XRUIInputModule"/> calls this method when the Interactor ends hovering over a UI element.
        /// </summary>
        /// <param name="args">Event data containing the UI element that is no longer hovered over.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnUIHoverEntered(UIHoverEventArgs)"/>
        public virtual void OnUIHoverExited(UIHoverEventArgs args)
        {
            UiHoverExited?.Invoke(args);
        }
        #endregion

        #region - Debug -
        private void SetDebugObjectVisibility(bool isVisible)
        {
            if (_debugVisualizationsEnabled)
            {
                if (_hoverDebugSphere == null)
                {
                    _hoverDebugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    _hoverDebugSphere.name = "[Debug] Poke - HoverVisual: " + this;
                    _hoverDebugSphere.transform.SetParent(GetAttachTransform(null), false);
                    _hoverDebugSphere.transform.localScale = new Vector3(_pokeHoverRadius, _pokeHoverRadius, _pokeHoverRadius);

                    if (_hoverDebugSphere.TryGetComponent<Collider>(out var debugCollider))
                    {
                        Destroy(debugCollider);
                    }

                    _hoverDebugRenderer = _GetOrAddComponent<MeshRenderer>(_hoverDebugSphere);
                }
            }

            var visibility = _debugVisualizationsEnabled && isVisible;

            if (_hoverDebugSphere != null && _hoverDebugSphere.activeSelf != visibility)
            {
                _hoverDebugSphere.SetActive(visibility);
            }

            static T _GetOrAddComponent<T>(GameObject go) where T : Component
            {
                return go.TryGetComponent<T>(out var component) ? component : go.AddComponent<T>();
            }
        }

        private void UpdateDebugVisuals()
        {
            SetDebugObjectVisibility(_currentPokeTarget != null || IsInteractingWithUI);

            if (!_debugVisualizationsEnabled)
            {
                return;
            }

            _hoverDebugRenderer.material.color = _pokeTargets.Count > 0 ? new Color(0f, 0.8f, 0f, 0.1f) : new Color(0.8f, 0f, 0f, 0.1f);
        }
        #endregion
    }
}
