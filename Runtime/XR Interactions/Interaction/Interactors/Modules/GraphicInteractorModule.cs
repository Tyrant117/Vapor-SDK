using System;
using UnityEngine;
using VaporInspector;
using VaporXR.UI;

namespace VaporXR.Interaction
{
    [DisallowMultipleComponent]
    public class GraphicInteractorModule : InteractorModule
    {
        #region Inspector
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("The depth threshold within which an interaction can begin to be evaluated as a poke.")]
        private float _pokeDepth = 0.1f;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("The radius threshold within which an interaction can be evaluated as a poke hover.")]
        private float _pokeHoverRadius = 0.015f;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("Physics layer mask used for limiting ui interaction.")]
        private LayerMask _uiLayerMask = 1 << 5;

        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("(Optional) Allow for either and input or a poke to be used for UI select")]
        private XRInputButton _leftPressInput;

        [FoldoutGroup("Posing"), SerializeField]
        private bool _hoverPoseEnabled;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_hoverPoseEnabled")]
        private VXRHand _hand;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_hoverPoseEnabled")]
        private HandPoseDatum _hoverPose;
        [FoldoutGroup("Posing"), SerializeField, ShowIf("%_hoverPoseEnabled")]
        private float _hoverPoseDuration;

        [FoldoutGroup("Debug", order: 1000), SerializeField]
        [RichTextTooltip("Denotes whether or not debug visuals are enabled for this poke interactor.")]
        private bool _debugVisualizationsEnabled;
        #endregion

        #region Properties        
        public bool IsInteractingWithUI { get; set; }
        #endregion

        #region Fields
        private RegisteredUIInteractorCache _registeredUIInteractorCache;

        private GameObject _hoverDebugSphere;
        private MeshRenderer _hoverDebugRenderer;

        private float _hoverTimer;
        private bool _hasPosed;
        private bool _wasInteractingWithUI;
        #endregion

        #region Events
        public event Action<UIHoverEventArgs> UiHoverEntered;
        public event Action<UIHoverEventArgs> UiHoverExited;

        // Used to avoid GC Alloc each frame in UpdateUIModel
        private Func<Vector3> _positionGetter;
        #endregion

        #region - Initialization -
        protected override void Awake()
        {
            base.Awake();
            _registeredUIInteractorCache = new RegisteredUIInteractorCache(this);
            _positionGetter = GetPokePosition;
        }

        protected virtual void OnEnable()
        {
            SetDebugObjectVisibility(true);

            _registeredUIInteractorCache.RegisterWithXRUIInputModule();
        }

        protected virtual void OnDisable()
        {
            SetDebugObjectVisibility(false);

            _registeredUIInteractorCache.UnregisterFromXRUIInputModule();
        }
        #endregion

        #region - Processing -
        public override void PrePreProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) { return; }
            IsInteractingWithUI = TrackedDeviceGraphicRaycaster.IsPokeInteractingWithUI(this);
            PokeStateData newPokeStateData = default;
            if (IsInteractingWithUI)
            {
                if (!_wasInteractingWithUI)
                {
                    _leftPressInput.Enable();
                    _wasInteractingWithUI = true;
                }
                TrackedDeviceGraphicRaycaster.TryGetPokeStateDataForInteractor(this, out newPokeStateData);
                _hoverTimer += Time.deltaTime;
                if (_hoverTimer > 0.1f)
                {
                    OnStartPose();
                }
            }
            else
            {
                if (_wasInteractingWithUI)
                {
                    _leftPressInput.Disable();
                    _wasInteractingWithUI = false;
                }
                OnEndPose();
            }
        }

        public override void PreProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) { return; }

            UpdateDebugVisuals();
        }

        private Vector3 GetPokePosition()
        {
            return GetAttachTransform(null).position;
        }
        #endregion

        #region - UI -
        public virtual void UpdateUIModel(ref TrackedDeviceModel model)
        {
            if (!isActiveAndEnabled)
            {
                model.Reset(false);
                return;
            }

            bool fromInputSelected = false;
            if (_leftPressInput.IsActive)
            {
                fromInputSelected = _leftPressInput.IsHeld;
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
            model.select = TrackedDeviceGraphicRaycaster.HasPokeSelect(this) || fromInputSelected;
            model.raycastLayerMask = _uiLayerMask;
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

        #region - Posing -
        private void OnStartPose()
        {
            if (_hoverPoseEnabled && !_hasPosed)
            {
                _hand.RequestHandPose(HandPoseType.Hover, Interactor, _hoverPose.Value, duration: _hoverPoseDuration);
                _hasPosed = true;
            }
        }

        private void OnEndPose()
        {
            if (_hoverPoseEnabled && _hasPosed)
            {
                _hand.RequestReturnToIdle(Interactor, _hoverPoseDuration);
                _hasPosed = false;
                _hoverTimer = 0;
            }
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
            SetDebugObjectVisibility(IsInteractingWithUI);

            if (!_debugVisualizationsEnabled)
            {
                return;
            }

            _hoverDebugRenderer.material.color = new Color(0.8f, 0f, 0f, 0.1f);
        }

        public Transform GetAttachTransform(Interactable interactable)
        {
            return Interactor.GetAttachTransform(interactable);
        }
        #endregion
    }
}
