using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using VaporInspector;
using VaporXR.Interaction;

namespace VaporXR
{
    /// <summary>
    /// Allow specified ray interactors to fallback to eye-gaze when they are off screen or pointing off screen.
    /// This component enables split interaction functionality to allow the user to aim with eye gaze and select with a controller.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_GazeAssistance)]
    [BurstCompile]
    // ReSharper disable once InconsistentNaming
    public class VXRGazeAssistance : MonoBehaviour, IXRAimAssist
    {
        private const float MinAttachDistance = 0.5f;
        private const float MinFallbackDivergence = 0f;
        private const float MaxFallbackDivergence = 90f;
        private const float MinAimAssistRequiredAngle = 0f;
        private const float MaxAimAssistRequiredAngle = 90f;

        /// <summary>
        /// Contains all the references to objects needed to mediate gaze fallback for a particular ray interactor.
        /// </summary>
        [System.Serializable, DrawWithVapor(UIGroupType.Vertical)]
        public sealed class InteractorData
        {
            #region Inspector
            [SerializeField] [RequireInterface(typeof(IXRRayProvider))] [RichTextTooltip("The interactor that can fall back to gaze data.")]
            private Object _interactor;

            [SerializeField] [RichTextTooltip("Changes mediation behavior to account for teleportation controls.")]
            private bool _teleportRay;
            #endregion
            
            #region Properties
            /// <summary>
            /// The interactor that can fall back to gaze data.
            /// </summary>
            public Object Interactor
            {
                get => _interactor;
                set => _interactor = value;
            }

            /// <summary>
            /// Changes mediation behavior to account for teleportation controls.
            /// </summary>
            public bool TeleportRay
            {
                get => _teleportRay;
                set => _teleportRay = value;
            }

            /// <summary>
            /// If this interactor is currently having its ray data modified to the gaze fallback.
            /// </summary>
            public bool Fallback { get; private set; }
            #endregion

            #region Fields
            private bool _initialized;

            private IXRRayProvider _rayProvider;
            private Interactor _selectInteractor;

            private bool _restoreVisuals;
            private VXRInteractorLineVisual _lineVisual;
            private bool _hasLineVisual;

            private Transform _originalRayOrigin;
            private Transform _originalAttach;
            private Transform _originalVisualLineOrigin;
            private bool _originalOverrideVisualLineOrigin;
            private Transform _fallbackRayOrigin;
            private Transform _fallbackAttach;
            private Transform _fallbackVisualLineOrigin;
            #endregion

            /// <summary>
            /// Hooks up all possible mediated components attached to the interactor.
            /// </summary>
            public void Initialize()
            {
                if (_initialized)
                    return;

                _rayProvider = _interactor as IXRRayProvider;
                _selectInteractor = _interactor as Interactor;
                if (_rayProvider == null || _selectInteractor == null)
                {
                    Debug.LogWarning("No ray and select interactor found!");
                    return;
                }

                _originalRayOrigin = _rayProvider.GetOrCreateRayOrigin();
                _originalAttach = _rayProvider.GetOrCreateAttachTransform();

                var rayTransform = _selectInteractor.transform;
                var rayName = rayTransform.gameObject.name;
                _fallbackRayOrigin = new GameObject($"Gaze Assistance [{rayName}] Ray Origin").transform;
                _fallbackAttach = new GameObject($"Gaze Assistance [{rayName}] Attach").transform;
                _fallbackRayOrigin.parent = _originalRayOrigin.parent;
                _fallbackAttach.parent = _fallbackRayOrigin;

                _hasLineVisual = rayTransform.TryGetComponent(out _lineVisual);
                if (_hasLineVisual)
                {
                    _fallbackVisualLineOrigin = new GameObject($"Gaze Assistance [{rayName}] Visual Origin").transform;
                    _fallbackVisualLineOrigin.parent = _fallbackRayOrigin.parent;
                }

                _initialized = true;
            }

            /// <summary>
            /// Update the fallback ray pose (copying gaze) if we are using it.
            /// </summary>
            /// <param name="gazeTransform">The Transform representing eye gaze origin.</param>
            public void UpdateFallbackRayOrigin(Transform gazeTransform)
            {
                if (!_initialized)
                    return;

                if (Fallback)
                {
                    var gazePosition = gazeTransform.position;
                    var gazeRotation = gazeTransform.rotation;
                    _fallbackRayOrigin.SetPositionAndRotation(gazePosition, gazeRotation);
                }
            }

            /// <summary>
            /// Update the line visual origin pose if we are using it.
            /// </summary>
            public void UpdateLineVisualOrigin()
            {
                if (!_initialized)
                    return;

                if (_hasLineVisual && Fallback)
                {
                    Vector3 position;
                    Quaternion rotation;
                    // The pose for the line visual is copied from the original.
                    // The rotation uses the gaze direction when it is a teleport projectile since it feels better.
                    if (_originalOverrideVisualLineOrigin && _originalVisualLineOrigin != null)
                    {
                        position = _originalVisualLineOrigin.position;
                        rotation = !_teleportRay ? _originalVisualLineOrigin.rotation : _fallbackRayOrigin.rotation;
                    }
                    else
                    {
                        position = _originalRayOrigin.position;
                        rotation = !_teleportRay ? _originalRayOrigin.rotation : _fallbackRayOrigin.rotation;
                    }

                    _fallbackVisualLineOrigin.SetPositionAndRotation(position, rotation);
                }
            }

            /// <summary>
            /// Determines if this interactor should be using fallback data or not.
            /// </summary>
            /// <param name="gazeTransform">The Transform representing eye gaze origin.</param>
            /// <param name="fallbackDivergence">At what angle the fallback data should be used.</param>
            /// <param name="selectionLocked">If another interactor is already using the fallback data.</param>
            /// <returns>Returns <see langword="true"/> if the interactor is using the eye gaze for ray origin, <see langword="false"/> if it is using its original data.</returns>
            public bool UpdateFallbackState(Transform gazeTransform, float fallbackDivergence, bool selectionLocked)
            {
                if (!_initialized)
                    return false;

                var shouldFallback = !selectionLocked && (Vector3.Angle(gazeTransform.forward, _originalRayOrigin.forward) > fallbackDivergence);

                // Only allow state transitions when selecting is not occurring
                if (!_selectInteractor.IsSelectActive)
                {
                    // If the ray is out of view, switch to using the fallback data
                    if (shouldFallback && !Fallback)
                    {
                        // Set to the Transforms managed by this component
                        if (_hasLineVisual)
                        {
                            _originalOverrideVisualLineOrigin = _lineVisual.OverrideInteractorLineOrigin;
                            _originalVisualLineOrigin = _lineVisual.LineOriginTransform;

                            _lineVisual.OverrideInteractorLineOrigin = true;
                            _lineVisual.LineOriginTransform = _fallbackVisualLineOrigin;
                        }

                        _rayProvider.SetRayOrigin(_fallbackRayOrigin);
                        _rayProvider.SetAttachTransform(_fallbackAttach);
                    }
                    else if (!shouldFallback && Fallback)
                    {
                        // Restore the original values from before
                        if (_hasLineVisual)
                        {
                            _lineVisual.OverrideInteractorLineOrigin = _originalOverrideVisualLineOrigin;
                            _lineVisual.LineOriginTransform = _originalVisualLineOrigin;
                        }

                        _rayProvider.SetRayOrigin(_originalRayOrigin);
                        _rayProvider.SetAttachTransform(_originalAttach);

                        if (!_teleportRay)
                            _restoreVisuals = true;
                    }

                    Fallback = shouldFallback;
                }

                if (Fallback)
                {
                    var gazePosition = gazeTransform.position;
                    var gazeRotation = gazeTransform.rotation;

                    if (!_teleportRay && _selectInteractor.IsSelectActive && _selectInteractor.HasSelection)
                    {
                        // Lerp the fallback ray to the original ray
                        var anchorDistance = (_fallbackAttach.position - gazePosition).magnitude;
                        var distancePercent = Mathf.Clamp01(anchorDistance / MinAttachDistance);
                        _fallbackRayOrigin.SetPositionAndRotation(
                            Vector3.Lerp(_originalRayOrigin.position, gazePosition, distancePercent),
                            Quaternion.Lerp(_originalRayOrigin.rotation, gazeRotation, distancePercent));

                        if (_hasLineVisual)
                            _lineVisual.enabled = true;

                        return true;
                    }

                    if (_hasLineVisual && !_teleportRay)
                        _lineVisual.enabled = false;
                }

                return false;
            }

            /// <summary>
            /// Restores the visuals of the <see cref="VXRInteractorLineVisual" /> if they were hidden.
            /// </summary>
            public void RestoreVisuals()
            {
                if (_restoreVisuals && _hasLineVisual && !Fallback)
                    _lineVisual.enabled = true;

                _restoreVisuals = false;
            }
        }

        #region Inspector
        [SerializeField] [BoxGroup("Components")] [RichTextTooltip("Eye data source used as fallback data and to determine if fallback data should be used.")]
        private RayInteractorModule _gazeInteractor;
        
        [SerializeField] [BoxGroup("Components")] [RichTextTooltip("Interactors that can fall back to gaze data.")]
        private List<InteractorData> _rayInteractors = new();

        [SerializeField] [FoldoutGroup("Properties")] [Range(MinFallbackDivergence, MaxFallbackDivergence)]
        [RichTextTooltip("How far an interactor must point away from the user's view area before eye gaze will be used instead.")]
        private float _fallbackDivergence = 60f;

        [SerializeField] [FoldoutGroup("Properties")] [RichTextTooltip("If the eye reticle should be hidden when all interactors are using their original data.")]
        private bool _hideCursorWithNoActiveRays = true;

        [SerializeField] [FoldoutGroup("Aim Assist")] [Range(MinAimAssistRequiredAngle, MaxAimAssistRequiredAngle)]
        [RichTextTooltip("How far projectiles can aim outside of eye gaze and still be considered for aim assist.")]
        private float _aimAssistRequiredAngle = 30f;

        [SerializeField] [FoldoutGroup("Aim Assist")] [RichTextTooltip("How fast a projectile must be moving to be considered for aim assist.")]
        private float _aimAssistRequiredSpeed = 0.25f;

        [SerializeField] [FoldoutGroup("Aim Assist")] [Range(0f, 1f)] [RichTextTooltip("How much of the corrected aim velocity to use, as a percentage.")] 
        private float _aimAssistPercent = 0.8f;

        [SerializeField] [FoldoutGroup("Aim Assist")] [RichTextTooltip("How much additional speed a projectile can receive from aim assistance, as a percentage.")]
        private float _aimAssistMaxSpeedPercent = 10f;
        #endregion

        #region Properties
        /// <summary>
        /// Eye data source used as fallback data and to determine if fallback data should be used.
        /// </summary>
        public RayInteractorModule GazeInteractor
        {
            get => _gazeInteractor;
            set => _gazeInteractor = value;
        }

        /// <summary>
        /// How far an interactor must point away from the user's view area before eye gaze will be used instead.
        /// </summary>
        public float FallbackDivergence
        {
            get => _fallbackDivergence;
            set => _fallbackDivergence = Mathf.Clamp(value, MinFallbackDivergence, MaxFallbackDivergence);
        }

        /// <summary>
        /// If the eye reticle should be hidden when all interactors are using their original data.
        /// </summary>
        public bool HideCursorWithNoActiveRays
        {
            get => _hideCursorWithNoActiveRays;
            set => _hideCursorWithNoActiveRays = value;
        }

        /// <summary>
        /// Interactors that can fall back to gaze data.
        /// </summary>
        public List<InteractorData> RayInteractors
        {
            get => _rayInteractors;
            set => _rayInteractors = value;
        }

        /// <summary>
        /// How far projectiles can aim outside of eye gaze and still be considered for aim assist.
        /// </summary>
        public float AimAssistRequiredAngle
        {
            get => _aimAssistRequiredAngle;
            set => _aimAssistRequiredAngle = Mathf.Clamp(value, MinAimAssistRequiredAngle, MaxAimAssistRequiredAngle);
        }

        /// <summary>
        /// How fast a projectile must be moving to be considered for aim assist.
        /// </summary>
        public float AimAssistRequiredSpeed
        {
            get => _aimAssistRequiredSpeed;
            set => _aimAssistRequiredSpeed = value;
        }

        /// <summary>
        /// How much of the corrected aim velocity to use, as a percentage.
        /// </summary>
        public float AimAssistPercent
        {
            get => _aimAssistPercent;
            set => _aimAssistPercent = Mathf.Clamp01(value);
        }

        /// <summary>
        /// How much additional speed a projectile can receive from aim assistance, as a percentage.
        /// </summary>
        public float AimAssistMaxSpeedPercent
        {
            get => _aimAssistMaxSpeedPercent;
            set => _aimAssistMaxSpeedPercent = value;
        }
        #endregion

        #region Fields
        private InteractorData _selectingInteractorData;
        private XRInteractorReticleVisual _gazeReticleVisual;
        private bool _hasGazeReticleVisual;
        #endregion

        #region - Unity Methods -
        private void OnEnable()
        {
            Application.onBeforeRender += OnBeforeRender;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
        }

        private void Start()
        {
            _Initialize();

            void _Initialize()
            {
                if (_gazeInteractor != null)
                {
                    _hasGazeReticleVisual = _gazeInteractor.TryGetComponent(out _gazeReticleVisual);
                }
                else
                {
                    Debug.LogError($"Gaze Interactor not set or missing on {this}. Disabling this XR Gaze Assistance component.", this);
                    enabled = false;
                    return;
                }

                foreach (var interactorData in _rayInteractors)
                {
                    interactorData.Initialize();
                }
            }
        }

        private void Update()
        {
            var gazeTransform = _gazeInteractor.AttachPoint;

            foreach (var interactorData in _rayInteractors)
            {
                interactorData.RestoreVisuals();
                interactorData.UpdateFallbackRayOrigin(gazeTransform);
            }
        }

        private void LateUpdate()
        {
            if (!_gazeInteractor.isActiveAndEnabled)
            {
                return;
            }

            var gazeTransform = _gazeInteractor.AttachPoint;

            if (_selectingInteractorData != null)
            {
                if (!_selectingInteractorData.UpdateFallbackState(gazeTransform, _fallbackDivergence, false))
                {
                    _selectingInteractorData = null;
                }
            }

            // Go through each interactor
            // If one is selecting, it takes priority and all others just revert
            var anyFallback = false;
            foreach (var interactorData in _rayInteractors)
            {
                if (interactorData.Fallback)
                {
                    anyFallback = true;
                }

                if (interactorData == _selectingInteractorData)
                {
                    continue;
                }

                if (interactorData.UpdateFallbackState(gazeTransform, _fallbackDivergence, _selectingInteractorData != null))
                {
                    _selectingInteractorData = interactorData;
                }
            }

            if (_hideCursorWithNoActiveRays && _hasGazeReticleVisual)
            {
                var selecting = _selectingInteractorData != null;
                _gazeReticleVisual.enabled = anyFallback && !selecting;
            }
        }

        [BeforeRenderOrder(XRInteractionUpdateOrder.k_BeforeRenderGazeAssistance)]
        private void OnBeforeRender()
        {
            foreach (var interactorData in _rayInteractors)
            {
                interactorData.UpdateLineVisualOrigin();
            }
        }
        #endregion

        #region - Helpers -
        /// <inheritdoc />
        public Vector3 GetAssistedVelocity(in Vector3 source, in Vector3 velocity, float gravity)
        {
            GetAssistedVelocityInternal(source, _gazeInteractor.RayEndPoint, velocity, gravity,
                _aimAssistRequiredAngle, _aimAssistRequiredSpeed, _aimAssistMaxSpeedPercent, _aimAssistPercent, Mathf.Epsilon, out var adjustedVelocity);
            return adjustedVelocity;
        }

        /// <inheritdoc />
        public Vector3 GetAssistedVelocity(in Vector3 source, in Vector3 velocity, float gravity, float maxAngle)
        {
            GetAssistedVelocityInternal(source, _gazeInteractor.RayEndPoint, velocity, gravity,
                maxAngle, _aimAssistRequiredSpeed, _aimAssistMaxSpeedPercent, _aimAssistPercent, Mathf.Epsilon, out var adjustedVelocity);
            return adjustedVelocity;
        }

        [BurstCompile]
        private static void GetAssistedVelocityInternal(in Vector3 source, in Vector3 target, in Vector3 velocity, float gravity,
            float maxAngle, float requiredSpeed, float maxSpeedPercent, float assistPercent, float epsilon, out Vector3 adjustedVelocity)
        {
            var toTarget = (target - source);
            var speed = math.length(velocity);

            var originalDirection = math.normalize(velocity);
            var targetDirection = math.normalize(toTarget);

            // If too far out, no aim assistance occurs
            if (Vector3.Angle(originalDirection, targetDirection) > maxAngle)
            {
                adjustedVelocity = velocity;
                return;
            }

            // If there is no gravity, then just go straight to the eye point
            if (gravity < epsilon)
            {
                adjustedVelocity = targetDirection * speed;
                return;
            }

            // If the speed is too low, we don't change anything
            if (speed < requiredSpeed)
            {
                adjustedVelocity = velocity;
                return;
            }

            // We solve the trajectory in 2D and then apply to the XZ angle
            float3 xzFacing = toTarget;
            xzFacing.y = 0f;
            var xzDistance = math.length(xzFacing);

            if (xzDistance < epsilon)
            {
                adjustedVelocity = velocity;
                return;
            }

            // To find the best angle, we solve for 45 degrees (a perfect parabolic arc) and 0 degrees or as low of an arc as we can
            var parabolicSolve = new float2(math.sqrt((0.5f * gravity * (xzDistance * xzDistance)) / (xzDistance - toTarget.y)), 0f);

            parabolicSolve.y = parabolicSolve.x;

            // Solve for a low of a degrees as possible
            var lowSolve = new float2(parabolicSolve.x, 0f);

            // If the target point is not lower than the starting point, we can't do the 0 degree solve
            if (toTarget.y < 0f)
            {
                lowSolve.x = math.sqrt((0.5f * gravity * xzDistance * xzDistance / -toTarget.y));
            }
            else
            {
                // Instead, we just double the horizontal speed of the parabolic solve to lower the height
                lowSolve.x *= 2f;
                lowSolve.y = lowSolve.x * (toTarget.y + (0.5f * gravity * (xzDistance / lowSolve.x) * (xzDistance / lowSolve.x))) / xzDistance;
            }

            // See which one is closer to our target speed
            var parabolicSpeed = math.length(parabolicSolve);
            var lowSpeed = math.length(lowSolve);

            var parabolicDif = math.abs(parabolicSpeed - speed);
            var lowDif = math.abs(lowSpeed - speed);

            // If the original user-supplied velocity was heading down, we give the low angle priority as parabolic would look weird
            if (velocity.y <= 0f)
                lowDif *= 0.25f;

            var chosenSolve = parabolicDif < lowDif ? parabolicSolve : lowSolve;

            // Cap to the assisted speed
            chosenSolve = math.normalize(chosenSolve) * math.min(math.length(chosenSolve), maxSpeedPercent * speed);

            var assistVelocity = math.normalize(xzFacing) * chosenSolve.x;
            assistVelocity.y = chosenSolve.y;

            // Lerp direction and speed for the final velocity
            adjustedVelocity = Vector3.Slerp(originalDirection, math.normalize(assistVelocity), assistPercent) * math.lerp(speed, math.length(assistVelocity), assistPercent);
        }
        #endregion

    }
}
