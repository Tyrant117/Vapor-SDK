using System;
using Unity.Collections;
using Unity.Mathematics;
using Unity.XR.CoreUtils;
using Unity.XR.CoreUtils.Bindings;
using Unity.XR.CoreUtils.Bindings.Variables;
using Unity.Burst;
using UnityEngine;
using Vapor.Utilities;
using VaporXR.Utilities.Tweenables;
using VaporInspector;

namespace VaporXR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_LineVisual)]
    [BurstCompile]
    public class VXRInteractorLineVisual : MonoBehaviour, IXRCustomReticleProvider
    {
        private const float MinLineWidth = 0.0001f;
        private const float MaxLineWidth = 0.05f;
        private const float MinLineBendRatio = 0.01f;
        private const float MaxLineBendRatio = 1f;
        private const int NumberOfSegmentsForBendableLine = 20;

        #region Inspector
        [FoldoutGroup("Settings"), SerializeField, Range(MinLineWidth, MaxLineWidth)]
        private float _lineWidth = 0.005f;
        [FoldoutGroup("Settings"), SerializeField] 
        private AnimationCurve _widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [FoldoutGroup("Settings"), SerializeField] 
        private bool _overrideInteractorLineOrigin = true;
        [FoldoutGroup("Settings"), SerializeField, ShowIf("%_overrideInteractorLineOrigin")] 
        private Transform _lineOriginTransform;
        [FoldoutGroup("Settings"), SerializeField, ShowIf("%_overrideInteractorLineOrigin")] 
        private float _lineOriginOffset;

        [FoldoutGroup("Settings"), SerializeField] 
        private bool _overrideInteractorLineLength = true;
        [FoldoutGroup("Settings"), SerializeField, ShowIf("%_overrideInteractorLineLength")] 
        private float _lineLength = 10f;
        [FoldoutGroup("Settings"), SerializeField, ShowIf("%_overrideInteractorLineLength")] 
        private bool _autoAdjustLineLength;
        [FoldoutGroup("Settings"), SerializeField, ShowIf("%_autoAdjustLineLength")] 
        private float _minLineLength = 0.5f;
        [FoldoutGroup("Settings"), SerializeField, ShowIf("%_autoAdjustLineLength")] 
        private bool _useDistanceToHitAsMaxLineLength = true;
        [FoldoutGroup("Settings"), SerializeField, ShowIf("%_autoAdjustLineLength")] 
        private float _lineRetractionDelay = 0.5f;
        [FoldoutGroup("Settings"), SerializeField, ShowIf("%_autoAdjustLineLength")] 
        private float _lineLengthChangeSpeed = 12f;        

        [FoldoutGroup("Settings"), SerializeField] 
        private bool _stopLineAtFirstRaycastHit = true;
        [FoldoutGroup("Settings"), SerializeField] 
        private bool _stopLineAtSelection;

        [FoldoutGroup("Settings"), SerializeField] 
        private bool _smoothMovement;
        [FoldoutGroup("Settings"), SerializeField, ShowIf("%_smoothMovement")] 
        private float _followTightness = 10f;
        [FoldoutGroup("Settings"), SerializeField, ShowIf("%_smoothMovement")] 
        private float _snapThresholdDistance = 10f;

        [FoldoutGroup("Settings"), SerializeField] 
        private bool _snapEndpointIfAvailable = true;
        [FoldoutGroup("Settings"), SerializeField]
        [Range(MinLineBendRatio, MaxLineBendRatio)]
        private float _lineBendRatio = 0.5f;

        [FoldoutGroup("Visuals"), SerializeField] 
        private bool _setLineColorGradient = true;
        [FoldoutGroup("Visuals"), SerializeField] 
        private Gradient _validColorGradient = new()
        {
            colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) },
        };
        [FoldoutGroup("Visuals"), SerializeField]
        private Gradient _invalidColorGradient = new()
        {
            colorKeys = new[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.red, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) },
        };
        [FoldoutGroup("Visuals"), SerializeField] 
        private Gradient _blockedColorGradient = new()
        {
            colorKeys = new[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(Color.yellow, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) },
        };
        [FoldoutGroup("Visuals"), SerializeField] 
        private bool _treatSelectionAsValidState;               
        
        [FoldoutGroup("Reticle"), SerializeField] 
        private GameObject _reticle;
        [FoldoutGroup("Reticle"), SerializeField] 
        private GameObject _blockedReticle;
        #endregion

        #region Properties
        /// <summary>
        /// Controls the width of the line.
        /// </summary>
        public float LineWidth
        {
            get => _lineWidth;
            set
            {
                _lineWidth = value;
                _performSetup = true;

                // Force update user scale since it calls an update to the line width
                _userScaleVar.BroadcastValue();
            }
        }

        /// <summary>
        /// A boolean value that controls which source Unity uses to determine the length of the line.
        /// Set to <see langword="true"/> to use the Line Length set by this behavior.
        /// Set to <see langword="false"/> to have the length of the line determined by the Interactor.
        /// </summary>
        /// <seealso cref="LineLength"/>
        public bool OverrideInteractorLineLength { get => _overrideInteractorLineLength; set => _overrideInteractorLineLength = value; }

        /// <summary>
        /// Controls the length of the line when overriding.
        /// </summary>
        /// <seealso cref="OverrideInteractorLineLength"/>
        /// <seealso cref="MinLineLength"/>
        public float LineLength { get => _lineLength; set => _lineLength = value; }

        /// <summary>
        /// Determines whether the length of the line will retract over time when no valid hits or selection occur.
        /// </summary>
        /// <seealso cref="MinLineLength"/>
        /// <seealso cref="LineRetractionDelay"/>
        public bool AutoAdjustLineLength { get => _autoAdjustLineLength; set => _autoAdjustLineLength = value; }

        /// <summary>
        /// Controls the minimum length of the line when overriding.
        /// When no valid hits occur, the ray visual shrinks down to this size.
        /// </summary>
        /// <seealso cref="OverrideInteractorLineLength"/>
        /// <seealso cref="AutoAdjustLineLength"/>
        /// <seealso cref="LineLength"/>
        public float MinLineLength { get => _minLineLength; set => _minLineLength = value; }

        /// <summary>
        /// Determines whether the max line length will be the the distance to the hit point or the fixed line length.
        /// </summary>
        /// <seealso cref="LineLength"/>
        public bool UseDistanceToHitAsMaxLineLength { get => _useDistanceToHitAsMaxLineLength; set => _useDistanceToHitAsMaxLineLength = value; }

        /// <summary>
        /// Time in seconds elapsed after last valid hit or selection for line to begin retracting to the minimum override length.
        /// </summary>
        /// <seealso cref="LineRetractionDelay"/>
        /// <seealso cref="MinLineLength"/>
        public float LineRetractionDelay { get => _lineRetractionDelay; set => _lineRetractionDelay = value; }

        /// <summary>
        /// Scalar used to control the speed of changes in length of the line when overriding it's length.
        /// </summary>
        /// <seealso cref="MinLineLength"/>
        /// <seealso cref="LineRetractionDelay"/>
        public float LineLengthChangeSpeed { get => _lineLengthChangeSpeed; set => _lineLengthChangeSpeed = value; }

        /// <summary>
        /// Controls the relative width of the line from start to end.
        /// </summary>
        public AnimationCurve WidthCurve
        {
            get => _widthCurve;
            set
            {
                _widthCurve = value;
                _performSetup = true;
            }
        }

        /// <summary>
        /// Determines whether or not this component will control the color of the Line Renderer.
        /// Disable to manually control the color externally from this component.
        /// </summary>
        /// <remarks>
        /// Useful to disable when using the affordance system for line color control instead of through this behavior.
        /// </remarks>
        public bool SetLineColorGradient { get => _setLineColorGradient; set => _setLineColorGradient = value; }

        /// <summary>
        /// Controls the color of the line as a gradient from start to end to indicate a valid state.
        /// </summary>
        public Gradient ValidColorGradient { get => _validColorGradient; set => _validColorGradient = value; }

        /// <summary>
        /// Controls the color of the line as a gradient from start to end to indicate an invalid state.
        /// </summary>
        public Gradient InvalidColorGradient { get => _invalidColorGradient; set => _invalidColorGradient = value; }

        /// <summary>
        /// Controls the color of the line as a gradient from start to end to indicate a state where the interactor has
        /// a valid target but selection is blocked.
        /// </summary>
        public Gradient BlockedColorGradient { get => _blockedColorGradient; set => _blockedColorGradient = value; }

        /// <summary>
        /// Forces the use of valid state visuals while the interactor is selecting an interactable, whether or not the Interactor has any valid targets.
        /// </summary>
        /// <seealso cref="ValidColorGradient"/>
        public bool TreatSelectionAsValidState { get => _treatSelectionAsValidState; set => _treatSelectionAsValidState = value; }

        /// <summary>
        /// Controls whether the rendered segments will be delayed from and smoothly follow the target segments.
        /// </summary>
        /// <seealso cref="FollowTightness"/>
        /// <seealso cref="SnapThresholdDistance"/>
        public bool SmoothMovement { get => _smoothMovement; set => _smoothMovement = value; }
        
        /// <summary>
        /// Controls the speed that the rendered segments follow the target segments when Smooth Movement is enabled.
        /// </summary>
        /// <seealso cref="SmoothMovement"/>
        /// <seealso cref="SnapThresholdDistance"/>
        public float FollowTightness { get => _followTightness; set => _followTightness = value; }

        /// <summary>
        /// Controls the threshold distance between line points at two consecutive frames to snap rendered segments to target segments when Smooth Movement is enabled.
        /// </summary>
        /// <seealso cref="SmoothMovement"/>
        /// <seealso cref="FollowTightness"/>
        public float SnapThresholdDistance
        {
            get => _snapThresholdDistance;
            set
            {
                _snapThresholdDistance = value;
                _squareSnapThresholdDistance = _snapThresholdDistance * _snapThresholdDistance;
            }
        }
        
        /// <summary>
        /// Stores the reticle that appears at the end of the line when it is valid.
        /// </summary>
        /// <remarks>
        /// Unity will instantiate it while playing when it is a Prefab asset.
        /// </remarks>
        public GameObject Reticle
        {
            get => _reticle;
            set
            {
                _reticle = value;
                if (Application.isPlaying)
                    SetupReticle();
            }
        }

        /// <summary>
        /// Stores the reticle that appears at the end of the line when the interactor has a valid target but selection is blocked.
        /// </summary>
        /// <remarks>
        /// Unity will instantiate it while playing when it is a Prefab asset.
        /// </remarks>
        public GameObject BlockedReticle
        {
            get => _blockedReticle;
            set
            {
                _blockedReticle = value;
                if (Application.isPlaying)
                    SetupBlockedReticle();
            }
        }
        
        /// <summary>
        /// Controls whether this behavior always cuts the line short at the first ray cast hit, even when invalid.
        /// </summary>
        /// <remarks>
        /// The line will always stop short at valid targets, even if this property is set to false.
        /// If you wish this line to pass through valid targets, they must be placed on a different layer.
        /// <see langword="true"/> means to do the same even when pointing at an invalid target.
        /// <see langword="false"/> means the line will continue to the configured line length.
        /// </remarks>
        public bool StopLineAtFirstRaycastHit { get => _stopLineAtFirstRaycastHit; set => _stopLineAtFirstRaycastHit = value; }

        /// <summary>
        /// Controls whether the line will stop at the attach point of the closest interactable selected by the interactor, if there is one.
        /// </summary>
        public bool StopLineAtSelection { get => _stopLineAtSelection; set => _stopLineAtSelection = value; }

        /// <summary>
        /// Controls whether the visualized line will snap endpoint if the ray hits a <see cref="VXRInteractableSnapVolume"/>.
        /// </summary>
        /// <remarks>
        /// Currently snapping only works with an <see cref="VXRRayInteractor"/>.
        /// </remarks>
        public bool SnapEndpointIfAvailable { get => _snapEndpointIfAvailable; set => _snapEndpointIfAvailable = value; }

        /// <summary>
        /// This ratio determines where the bend point is on a bent line. Line bending occurs due to hitting a snap volume or because the target end point is out of line with the ray. A value of 1 means the line will not bend.
        /// </summary>
        public float LineBendRatio { get => _lineBendRatio; set => _lineBendRatio = Mathf.Clamp(value, MinLineBendRatio, MaxLineBendRatio); }

        /// <summary>
        /// A boolean value that controls whether to use a different <see cref="Transform"/> as the starting position and direction of the line.
        /// Set to <see langword="true"/> to use the line origin specified by <see cref="LineOriginTransform"/>.
        /// Set to <see langword="false"/> to use the the line origin specified by the interactor.
        /// </summary>
        /// <seealso cref="LineOriginTransform"/>
        /// <seealso cref="IAdvancedLineRenderable.GetLinePoints(ref NativeArray{Vector3},out int,Ray?)"/>
        public bool OverrideInteractorLineOrigin { get => _overrideInteractorLineOrigin; set => _overrideInteractorLineOrigin = value; }

        /// <summary>
        /// The starting position and direction of the line when overriding.
        /// </summary>
        /// <seealso cref="OverrideInteractorLineOrigin"/>
        public Transform LineOriginTransform { get => _lineOriginTransform; set => _lineOriginTransform = value; }

        /// <summary>
        /// Offset from line origin along the line direction before line rendering begins. Only works if the line provider is using straight lines.
        /// This value applies even when not overriding the line origin with a different <see cref="Transform"/>.
        /// </summary>
        public float LineOriginOffset { get => _lineOriginOffset; set => _lineOriginOffset = value; }
        #endregion

        #region Fields
        private float _squareSnapThresholdDistance;

        private Vector3 _reticlePos;
        private Vector3 _reticleNormal;
        private int _endPositionInLine;

        private bool _snapCurve = true;
        private bool _performSetup;
        private GameObject _reticleToUse;

        private LineRenderer _lineRenderer;

        // Interface to get target point
        private ILineRenderable _lineRenderable;
        private IAdvancedLineRenderable _advancedLineRenderable;
        private bool _hasAdvancedLineRenderable;

        private VXRBaseInteractor _lineRenderableAsSelectInteractor;
        private VXRBaseInteractor _lineRenderableAsHoverInteractor;
        private VXRBaseInteractor _lineRenderableAsBaseInteractor;
        private VXRTeleportInteractor _lineRenderableAsRayInteractor;

        // Reusable list of target points
        private NativeArray<Vector3> _targetPoints;
        private int _numTargetPoints = -1;

        // Reusable lists of target points for the old interface
        private Vector3[] _targetPointsFallback = Array.Empty<Vector3>();

        // Reusable list of rendered points
        private NativeArray<Vector3> _renderPoints;
        private int _numRenderPoints = -1;

        // Reusable list of rendered points to smooth movement
        private NativeArray<Vector3> _previousRenderPoints;
        private int _numPreviousRenderPoints = -1;

        private readonly Vector3[] _clearArray = { Vector3.zero, Vector3.zero };

        private GameObject _customReticle;
        private bool _customReticleAttached;

        // Snapping
        private VXRInteractableSnapVolume _xrInteractableSnapVolume;

        private bool _previousShouldBendLine;
        private Vector3 _previousLineDirection;

        // Most recent hit information
        private Vector3 _currentHitPoint;
        private bool _hasHitInfo;
        private bool _validHit;
        private float _lastValidHitTime;
        private float _lastValidLineLength;

        // Previously hit collider
        private Collider _previousCollider;
        private VXROrigin _xrOrigin;

        private bool _hasRayInteractor;
        private bool _hasBaseInteractor;
        private bool _hasHoverInteractor;
        private bool _hasSelectInteractor;

        private readonly BindableVariable<float> _userScaleVar = new BindableVariable<float>();
        private readonly FloatTweenableVariable _lineLengthOverrideTweenableVariable = new FloatTweenableVariable();
        private readonly BindingsGroup _bindingsGroup = new BindingsGroup();
        #endregion

        #region - Initialization -
        protected void OnValidate()
        {
            if (Application.isPlaying)
            {
                UpdateSettings();
            }
        }

        protected void Awake()
        {
            _lineRenderable = GetComponent<ILineRenderable>();
            _advancedLineRenderable = _lineRenderable as IAdvancedLineRenderable;
            _hasAdvancedLineRenderable = _advancedLineRenderable != null;

            if (_lineRenderable != null)
            {
                if (_lineRenderable is VXRBaseInteractor baseInteractor)
                {
                    _lineRenderableAsBaseInteractor = baseInteractor;
                    _hasBaseInteractor = true;
                }

                if (_lineRenderable is VXRBaseInteractor selectInteractor)
                {
                    _lineRenderableAsSelectInteractor = selectInteractor;
                    _hasSelectInteractor = true;
                }

                if (_lineRenderable is VXRBaseInteractor hoverInteractor)
                {
                    _lineRenderableAsHoverInteractor = hoverInteractor;
                    _hasHoverInteractor = true;
                }

                if (_lineRenderable is VXRTeleportInteractor rayInteractor)
                {
                    _lineRenderableAsRayInteractor = rayInteractor;
                    _hasRayInteractor = true;
                }
            }

            _FindXROrigin();
            SetupReticle();
            SetupBlockedReticle();
            ClearLineRenderer();
            UpdateSettings();
            
            void _FindXROrigin()
            {
                if (_xrOrigin == null)
                {
                    ComponentLocatorUtility<VXROrigin>.TryFindComponent(out _xrOrigin);
                }
            }
        }

        protected void OnEnable()
        {
            if (_lineRenderer == null)
            {
                XRLoggingUtils.LogError($"Missing Line Renderer component on {this}. Disabling line visual.", this);
                enabled = false;
                return;
            }

            if (_lineRenderable == null)
            {
                XRLoggingUtils.LogError($"Missing {nameof(ILineRenderable)} / Ray Interactor component on {this}. Disabling line visual.", this);
                enabled = false;

                _lineRenderer.enabled = false;
                return;
            }

            _snapCurve = true;
            if (_reticleToUse != null)
            {
                _reticleToUse.SetActive(false);
                _reticleToUse = null;
            }

            _bindingsGroup.AddBinding(_userScaleVar.Subscribe(userScale => _lineRenderer.widthMultiplier = userScale * Mathf.Clamp(_lineWidth, MinLineWidth, MaxLineWidth)));

            Application.onBeforeRender += OnBeforeRenderLineVisual;
        }

        protected void OnDisable()
        {
            _bindingsGroup.Clear();

            if (_lineRenderer != null)
                _lineRenderer.enabled = false;

            if (_reticleToUse != null)
            {
                _reticleToUse.SetActive(false);
                _reticleToUse = null;
            }

            Application.onBeforeRender -= OnBeforeRenderLineVisual;
        }

        protected void OnDestroy()
        {
            if (_targetPoints.IsCreated)
                _targetPoints.Dispose();
            if (_renderPoints.IsCreated)
                _renderPoints.Dispose();
            if (_previousRenderPoints.IsCreated)
                _previousRenderPoints.Dispose();

            _lineLengthOverrideTweenableVariable.Dispose();
        }
        #endregion

        #region - Update -
        protected void LateUpdate()
        {
            if (_performSetup)
            {
                UpdateSettings();
                _performSetup = false;
            }

            if (_lineRenderer.useWorldSpace && _xrOrigin != null)
            {
                // Update line width with user scale
                var xrOrigin = _xrOrigin.Origin;
                var userScale = xrOrigin != null ? xrOrigin.transform.localScale.x : 1f;
                _userScaleVar.Value = userScale;
            }
        }

        private void UpdateSettings()
        {
            _squareSnapThresholdDistance = _snapThresholdDistance * _snapThresholdDistance;

            if (TryFindLineRenderer())
            {
                _lineRenderer.widthMultiplier = Mathf.Clamp(_lineWidth, MinLineWidth, MaxLineWidth);
                _lineRenderer.widthCurve = _widthCurve;
                _snapCurve = true;
            }

            _lineLengthOverrideTweenableVariable.target = LineLength;
            _lineLengthOverrideTweenableVariable.HandleTween(1f);
        }
        #endregion

        #region - Rendering -
        [BeforeRenderOrder(XRInteractionUpdateOrder.k_BeforeRenderLineVisual)]
        private void OnBeforeRenderLineVisual()
        {
            UpdateLineVisual();
        }

        public void UpdateLineVisual()
        {
            if (_lineRenderableAsBaseInteractor != null &&
                _lineRenderableAsBaseInteractor.DisableVisualsWhenBlockedInGroup &&
                _lineRenderableAsBaseInteractor.IsBlockedByInteractionWithinGroup())
            {
                _lineRenderer.enabled = false;
                return;
            }

            if (!_lineRenderableAsRayInteractor.ShouldDrawLine)
            {
                _lineRenderer.enabled = false;
                return;
            }

            _numRenderPoints = 0;

            // Get all the line sample points from the ILineRenderable interface
            if (!GetLinePoints(ref _targetPoints, out _numTargetPoints) || _numTargetPoints == 0)
            {
                _lineRenderer.enabled = false;
                return;
            }

            var hasSelection = _hasSelectInteractor && _lineRenderableAsSelectInteractor.HasSelection;

            // Using a straight line type because it's likely the straight line won't gracefully follow an object not in it's path.
            var hasStraightRayCast = _hasRayInteractor && _lineRenderableAsRayInteractor.LineType == VXRTeleportInteractor.LineModeType.StraightLine;

            // Query the line provider for origin data and apply overrides if needed.
            GetLineOriginAndDirection(ref _targetPoints, _numTargetPoints, hasStraightRayCast, out var lineOrigin, out var lineDirection);

            // Query the raycaster to determine line hit information and determine if hit was valid. Also check for snap volumes.
            _validHit = ExtractHitInformation(ref _targetPoints, _numTargetPoints, out var targetEndPoint, out var hitSnapVolume);

            var curveRayTowardAttachPoint = hasSelection && hasStraightRayCast;

            // If overriding ray origin, the line end point will be decoupled from the raycast hit point, so we bend towards it.
            bool bendForOverride = _overrideInteractorLineOrigin && _validHit;
            var curveRayTowardHitPoint = bendForOverride && hasStraightRayCast;

            var shouldBendLine = (hitSnapVolume || curveRayTowardAttachPoint || curveRayTowardHitPoint) && _lineBendRatio < 1f;

            if (shouldBendLine)
            {
                _numTargetPoints = NumberOfSegmentsForBendableLine;
                _endPositionInLine = _numTargetPoints - 1;

                if (curveRayTowardAttachPoint)
                {
                    // This function assumes there is an active selection. Calling it without selection will lead to errors.
                    FindClosestInteractableAttachPoint(lineOrigin, out targetEndPoint);
                }
            }

            // Make sure we have the correct sized arrays for everything.
            EnsureSize(ref _targetPoints, _numTargetPoints);
            if (!EnsureSize(ref _renderPoints, _numTargetPoints))
            {
                _numRenderPoints = 0;
            }

            if (!EnsureSize(ref _previousRenderPoints, _numTargetPoints))
            {
                _numPreviousRenderPoints = 0;
            }

            if (shouldBendLine)
            {
                // Since curves regenerate the whole line from key points, we only need to lerp the origin and forward to achieve ideal smoothing results.
                if (_smoothMovement)
                {
                    if (_previousShouldBendLine && _numPreviousRenderPoints > 0)
                    {
                        var lineDelta = _followTightness * Time.deltaTime;
                        lineDirection = Vector3.Lerp(_previousLineDirection, lineDirection, lineDelta);
                        lineOrigin = Vector3.Lerp(_previousRenderPoints[0], lineOrigin, lineDelta);
                    }

                    _previousLineDirection = lineDirection;
                }

                CalculateLineCurveRenderPoints(_numTargetPoints, _lineBendRatio, lineOrigin, lineDirection, targetEndPoint, ref _targetPoints);
            }

            _previousShouldBendLine = shouldBendLine;

            // Unchanged
            // If there is a big movement (snap turn, teleportation), snap the curve
            if (_numPreviousRenderPoints != _numTargetPoints)
            {
                _snapCurve = true;
            }
            // Compare the two endpoints of the curve, as that will have the largest delta.
            else if (_smoothMovement &&
                     _numPreviousRenderPoints > 0 &&
                     _numPreviousRenderPoints <= _previousRenderPoints.Length &&
                     _numTargetPoints > 0 &&
                     _numTargetPoints <= _targetPoints.Length)
            {
                var prevPointIndex = _numPreviousRenderPoints - 1;
                var currPointIndex = _numTargetPoints - 1;
                _snapCurve = Vector3.SqrMagnitude(_previousRenderPoints[prevPointIndex] - _targetPoints[currPointIndex]) > _squareSnapThresholdDistance;
            }

            AdjustLineAndReticle(hasSelection, shouldBendLine, lineOrigin, targetEndPoint);

            // We don't smooth points for the bent line as we smooth it when computing the curve
            var shouldSmoothPoints = !shouldBendLine && _smoothMovement && (_numPreviousRenderPoints == _numTargetPoints) && !_snapCurve;

            if (_overrideInteractorLineLength || shouldSmoothPoints)
            {
                var float3TargetPoints = _targetPoints.Reinterpret<float3>();
                var float3PrevRenderPoints = _previousRenderPoints.Reinterpret<float3>();
                var float3RenderPoints = _renderPoints.Reinterpret<float3>();

                var newLineLength = _overrideInteractorLineLength && _autoAdjustLineLength
                    ? UpdateTargetLineLength(lineOrigin, targetEndPoint, _minLineLength, _lineLength, _lineRetractionDelay, _lineLengthChangeSpeed, _validHit || hasSelection,
                        _useDistanceToHitAsMaxLineLength)
                    : _lineLength;

                _numRenderPoints = ComputeNewRenderPoints(_numRenderPoints, _numTargetPoints, newLineLength,
                    shouldSmoothPoints, _overrideInteractorLineLength, _followTightness * Time.deltaTime,
                    ref float3TargetPoints, ref float3PrevRenderPoints, ref float3RenderPoints);
            }
            else
            {
                // Copy from m_TargetPoints into m_RenderPoints
                NativeArray<Vector3>.Copy(_targetPoints, 0, _renderPoints, 0, _numTargetPoints);
                _numRenderPoints = _numTargetPoints;
            }

            // When a straight line has only two points and color gradients have more than two keys,
            // interpolate points between the two points to enable better color gradient effects.
            if (_validHit || _treatSelectionAsValidState && hasSelection)
            {
                // Use regular valid state visuals unless we are hovering and selection is blocked.
                // We use regular valid state visuals if not hovering because the blocked state does not apply
                // (e.g. we could have a valid target that is UI and therefore not hoverable or selectable as an interactable).
                var useBlockedVisuals = false;
                if (!hasSelection && _hasBaseInteractor && _lineRenderableAsBaseInteractor.HasHover)
                {
                    var interactionManager = _lineRenderableAsBaseInteractor.InteractionManager;
                    var canSelectSomething = false;
                    foreach (var interactable in _lineRenderableAsBaseInteractor.InteractablesHovered)
                    {
                        if (interactable is IXRSelectInteractable selectInteractable && interactionManager.IsSelectPossible(_lineRenderableAsBaseInteractor, selectInteractable))
                        {
                            canSelectSomething = true;
                            break;
                        }
                    }

                    useBlockedVisuals = !canSelectSomething;
                }

                SetColorGradient(useBlockedVisuals ? _blockedColorGradient : _validColorGradient);
                AssignReticle(useBlockedVisuals);
            }
            else
            {
                ClearReticle();
                SetColorGradient(_invalidColorGradient);
            }

            if (_numRenderPoints >= 2)
            {
                _lineRenderer.enabled = true;
                _lineRenderer.positionCount = _numRenderPoints;
                _lineRenderer.SetPositions(_renderPoints);
            }
            else
            {
                _lineRenderer.enabled = false;
                return;
            }

            // Update previous points
            // Copy from m_RenderPoints into m_PreviousRenderPoints
            NativeArray<Vector3>.Copy(_renderPoints, 0, _previousRenderPoints, 0, _numRenderPoints);
            _numPreviousRenderPoints = _numRenderPoints;
            _snapCurve = false;
        }
        
        private bool GetLinePoints(ref NativeArray<Vector3> linePoints, out int numPoints)
        {
            if (_hasAdvancedLineRenderable)
            {
                Ray? rayOriginOverride = null;
                if (_overrideInteractorLineOrigin && _lineOriginTransform != null)
                {
                    var lineOrigin = _lineOriginTransform.position;
                    var lineDirection = _lineOriginTransform.forward;
                    rayOriginOverride = new Ray(lineOrigin, lineDirection);
                }

                return _advancedLineRenderable.GetLinePoints(ref linePoints, out numPoints, rayOriginOverride);
            }

            var hasLinePoint = _lineRenderable.GetLinePoints(ref _targetPointsFallback, out numPoints);
            EnsureSize(ref linePoints, numPoints);
            NativeArray<Vector3>.Copy(_targetPointsFallback, linePoints, numPoints);
            return hasLinePoint;
        }
        
        private void AdjustLineAndReticle(bool hasSelection, bool bendLine, in Vector3 lineOrigin, in Vector3 targetEndPoint)
        {
            // If the line hits, insert reticle position into the list for smoothing.
            // Remove the last point in the list to keep the number of points consistent.
            if (_hasHitInfo)
            {
                _reticlePos = targetEndPoint;

                // End the line at the current hit point.
                if ((_validHit || _stopLineAtFirstRaycastHit) && _endPositionInLine > 0 && _endPositionInLine < _numTargetPoints)
                {
                    // The hit position might not lie within the line segment, for example if a sphere cast is used, so use a point projected onto the
                    // segment so that the endpoint is continuous with the rest of the curve.
                    var lastSegmentStartPoint = _targetPoints[_endPositionInLine - 1];
                    var lastSegmentEndPoint = _targetPoints[_endPositionInLine];
                    var lastSegment = lastSegmentEndPoint - lastSegmentStartPoint;
                    var projectedHitSegment = Vector3.Project(_reticlePos - lastSegmentStartPoint, lastSegment);

                    // Don't bend the line backwards
                    if (Vector3.Dot(projectedHitSegment, lastSegment) < 0)
                        projectedHitSegment = Vector3.zero;

                    _reticlePos = lastSegmentStartPoint + projectedHitSegment;
                    _targetPoints[_endPositionInLine] = _reticlePos;
                    _numTargetPoints = _endPositionInLine + 1;
                }
            }

            // Stop line if there is a selection
            if (_stopLineAtSelection && hasSelection && !bendLine)
            {
                // Use the selected interactable closest to the start of the line.
                var sqrMagnitude = Vector3.SqrMagnitude(targetEndPoint - lineOrigin);

                // Only stop at selection if it is closer than the current end point.
                var currentEndSqDistance = Vector3.SqrMagnitude(_targetPoints[_endPositionInLine] - lineOrigin);
                if (sqrMagnitude < currentEndSqDistance || _endPositionInLine == 0)
                {
                    // Find out where the selection point belongs in the line points. Use the closest target point.
                    var endPositionForSelection = 1;
                    var sqDistanceFromEndPoint = Vector3.SqrMagnitude(_targetPoints[endPositionForSelection] - targetEndPoint);
                    for (var i = 2; i < _numTargetPoints; i++)
                    {
                        var sqDistance = Vector3.SqrMagnitude(_targetPoints[i] - targetEndPoint);
                        if (sqDistance < sqDistanceFromEndPoint)
                        {
                            endPositionForSelection = i;
                            sqDistanceFromEndPoint = sqDistance;
                        }
                        else
                        {
                            break;
                        }
                    }

                    _endPositionInLine = endPositionForSelection;
                    _numTargetPoints = _endPositionInLine + 1;
                    _reticlePos = targetEndPoint;
                    if (!_hasHitInfo)
                        _reticleNormal = Vector3.Normalize(_targetPoints[_endPositionInLine - 1] - _reticlePos);
                    _targetPoints[_endPositionInLine] = _reticlePos;
                }
            }
        }
        
        private void FindClosestInteractableAttachPoint(in Vector3 lineOrigin, out Vector3 closestPoint)
        {
            // Use the selected interactable closest to the start of the line.
            var interactablesSelected = _lineRenderableAsSelectInteractor.InteractablesSelected;
            closestPoint = interactablesSelected[0].GetAttachTransform(_lineRenderableAsSelectInteractor).position;

            if (interactablesSelected.Count > 1)
            {
                var closestSqDistance = Vector3.SqrMagnitude(closestPoint - lineOrigin);
                for (var i = 1; i < interactablesSelected.Count; ++i)
                {
                    var endPoint = interactablesSelected[i].GetAttachTransform(_lineRenderableAsSelectInteractor).position;
                    var sqDistance = Vector3.SqrMagnitude(endPoint - lineOrigin);
                    if (sqDistance < closestSqDistance)
                    {
                        closestPoint = endPoint;
                        closestSqDistance = sqDistance;
                    }
                }
            }
        }
        
        private void GetLineOriginAndDirection(ref NativeArray<Vector3> targetPoints, int numTargetPoints, bool isLineStraight, out Vector3 lineOrigin, out Vector3 lineDirection)
        {
            if (_overrideInteractorLineOrigin && _lineOriginTransform != null)
            {
                lineOrigin = _lineOriginTransform.position;
                lineDirection = _lineOriginTransform.forward;
            }
            else
            {
                if (_hasAdvancedLineRenderable)
                {
                    // Get accurate line origin and direction.
                    _advancedLineRenderable.GetLineOriginAndDirection(out lineOrigin, out lineDirection);
                }
                else
                {
                    lineOrigin = targetPoints[0];
                    var lineEnd = targetPoints[numTargetPoints - 1];
                    lineDirection = (lineEnd - lineOrigin).normalized;
                }
            }

            // If we have a straight line and offset is greater than 0, but smaller than our override length, we apply the offset.
            if (isLineStraight &&
                _lineOriginOffset > 0f && (!_overrideInteractorLineLength || _lineOriginOffset < _lineLength))
            {
                lineOrigin += lineDirection * _lineOriginOffset;
            }

            // Write the modified line origin back into the array
            targetPoints[0] = lineOrigin;
        }

        private bool ExtractHitInformation(ref NativeArray<Vector3> targetPoints, int numTargetPoints, out Vector3 targetEndPoint, out bool hitSnapVolume)
        {
            Collider hitCollider = null;
            hitSnapVolume = false;
            // NativeArray<T> does not implement the indexer operator as a readonly get (C# 8 feature),
            // so this method param is ref instead of in to avoid a defensive copy being created.
            targetEndPoint = targetPoints[numTargetPoints - 1];

            _hasHitInfo = _lineRenderable.TryGetHitInfo(out _currentHitPoint, out _reticleNormal, out _endPositionInLine, out var validHit);
            if (_hasHitInfo)
            {
                targetEndPoint = _currentHitPoint;

                if (validHit && _snapEndpointIfAvailable && _hasRayInteractor)
                {
                    // When hovering a new collider, check if it has a specified snapping volume, if it does then get the closest point on it
                    if (_lineRenderableAsRayInteractor.TryGetCurrent3DRaycastHit(out var raycastHit, out _))
                        hitCollider = raycastHit.collider;

                    if (hitCollider != _previousCollider && hitCollider != null)
                        _lineRenderableAsBaseInteractor.InteractionManager.TryGetInteractableForCollider(hitCollider, out _, out _xrInteractableSnapVolume);

                    if (_xrInteractableSnapVolume != null)
                    {
                        // If we have a selection, get the closest point to the attach transform position on the snap to collider 
                        targetEndPoint = _lineRenderableAsRayInteractor.HasSelection
                            ? _xrInteractableSnapVolume.GetClosestPointOfAttachTransform(_lineRenderableAsRayInteractor)
                            : _xrInteractableSnapVolume.GetClosestPoint(targetEndPoint);

                        _endPositionInLine = NumberOfSegmentsForBendableLine - 1; // Override hit index because we're going to use a custom line where the hit point is the end
                        hitSnapVolume = true;
                    }
                }
            }

            if (hitCollider == null)
                _xrInteractableSnapVolume = null;

            _previousCollider = hitCollider;

            return validHit;
        }
        
        private float UpdateTargetLineLength(in Vector3 lineOrigin, in Vector3 hitPoint, float minimumLineLength, float maximumLineLength, float lineRetractionDelaySeconds, float lineRetractionScalar,
            bool hasHit, bool deriveMaxLineLength)
        {
            var currentTime = Time.unscaledTime;

            if (hasHit)
            {
                _lastValidHitTime = Time.unscaledTime;
                _lastValidLineLength = deriveMaxLineLength ? Mathf.Min(Vector3.Distance(lineOrigin, hitPoint), maximumLineLength) : maximumLineLength;
            }

            var timeSinceLastValidHit = currentTime - _lastValidHitTime;

            if (timeSinceLastValidHit > lineRetractionDelaySeconds)
            {
                _lineLengthOverrideTweenableVariable.target = minimumLineLength;

                var timeScalar = (timeSinceLastValidHit - lineRetractionDelaySeconds) * lineRetractionScalar;

                // Accelerate line shrinking over time
                _lineLengthOverrideTweenableVariable.HandleTween(Time.unscaledDeltaTime * timeScalar);
            }
            else
            {
                _lineLengthOverrideTweenableVariable.target = Mathf.Max(_lastValidLineLength, minimumLineLength);
                _lineLengthOverrideTweenableVariable.HandleTween(Time.unscaledDeltaTime * lineRetractionScalar);
            }

            return _lineLengthOverrideTweenableVariable.Value;
        }

        private void AssignReticle(bool useBlockedVisuals)
        {
            // Set reticle position and show reticle
            var previouslyUsedReticle = _reticleToUse;
            var validStateReticle = useBlockedVisuals ? _blockedReticle : _reticle;
            _reticleToUse = _customReticleAttached ? _customReticle : validStateReticle;
            if (previouslyUsedReticle != null && previouslyUsedReticle != _reticleToUse)
                previouslyUsedReticle.SetActive(false);

            if (_reticleToUse != null)
            {
                _reticleToUse.transform.position = _reticlePos;
                if (_hasHoverInteractor && _lineRenderableAsHoverInteractor.GetOldestInteractableHovered() is IXRReticleDirectionProvider reticleDirectionProvider)
                {
                    reticleDirectionProvider.GetReticleDirection(_lineRenderableAsHoverInteractor, _reticleNormal, out var reticleUp, out var reticleForward);
                    Quaternion lookRotation;
                    if (reticleForward.HasValue)
                        BurstMathUtility.LookRotationWithForwardProjectedOnPlane(reticleForward.Value, reticleUp, out lookRotation);
                    else
                        BurstMathUtility.LookRotationWithForwardProjectedOnPlane(_reticleToUse.transform.forward, reticleUp, out lookRotation);

                    _reticleToUse.transform.rotation = lookRotation;
                }
                else
                {
                    _reticleToUse.transform.forward = -_reticleNormal;
                }

                _reticleToUse.SetActive(true);
            }
        }

        private void ClearReticle()
        {
            if (_reticleToUse != null)
            {
                _reticleToUse.SetActive(false);
                _reticleToUse = null;
            }
        }
        #endregion

        #region - Helpers -
        private void SetColorGradient(Gradient colorGradient)
        {
            if (!_setLineColorGradient)
            {
                return;
            }

            _lineRenderer.colorGradient = colorGradient;
        }

        private bool TryFindLineRenderer()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer != null) return true;
            
            Debug.LogWarning("No Line Renderer found for Interactor Line Visual.", this);
            enabled = false;
            return false;
        }

        private void ClearLineRenderer()
        {
            if (!TryFindLineRenderer()) return;
            
            _lineRenderer.SetPositions(_clearArray);
            _lineRenderer.positionCount = 0;
        }

        #region - Reticle -
        private void SetupReticle()
        {
            if (_reticle == null)
                return;

            // Instantiate if the reticle is a Prefab asset rather than a scene GameObject
            if (!_reticle.scene.IsValid())
                _reticle = Instantiate(_reticle);

            _reticle.SetActive(false);
        }

        private void SetupBlockedReticle()
        {
            if (_blockedReticle == null)
                return;

            // Instantiate if the reticle is a Prefab asset rather than a scene GameObject
            if (!_blockedReticle.scene.IsValid())
                _blockedReticle = Instantiate(_blockedReticle);

            _blockedReticle.SetActive(false);
        }

        /// <inheritdoc />
        public bool AttachCustomReticle(GameObject reticleInstance)
        {
            _customReticle = reticleInstance;
            _customReticleAttached = true;
            return true;
        }

        /// <inheritdoc />
        public bool RemoveCustomReticle()
        {
            _customReticle = null;
            _customReticleAttached = false;
            return true;
        }
        #endregion
        
        private static bool EnsureSize(ref NativeArray<Vector3> array, int targetSize)
        {
            if (array.IsCreated && array.Length >= targetSize)
                return true;

            if (array.IsCreated)
                array.Dispose();

            array = new NativeArray<Vector3>(targetSize, Allocator.Persistent);
            return false;
        }
        
        /// <summary>
        /// Calculates the target render points based on the targeted snapped endpoint and the actual position of the raycast line.
        /// </summary>
        [BurstCompile]
        private static void CalculateLineCurveRenderPoints(int numTargetPoints, float curveRatio, in Vector3 lineOrigin, in Vector3 lineDirection, in Vector3 endPoint, ref NativeArray<Vector3> targetPoints)
        {
            var float3TargetPoints = targetPoints.Reinterpret<float3>();
            CurveUtility.GenerateCubicBezierCurve(numTargetPoints, curveRatio, lineOrigin, lineDirection, endPoint, ref float3TargetPoints);
        }
        
        [BurstCompile]
        private static int ComputeNewRenderPoints(int numRenderPoints, int numTargetPoints, float targetLineLength, bool shouldSmoothPoints, bool shouldOverwritePoints, float pointSmoothIncrement,
            ref NativeArray<float3> targetPoints, ref NativeArray<float3> previousRenderPoints, ref NativeArray<float3> renderPoints)
        {
            var length = 0f;
            var maxRenderPoints = renderPoints.Length;
            var finalNumRenderPoints = numRenderPoints;
            for (var i = 0; i < numTargetPoints && finalNumRenderPoints < maxRenderPoints; ++i)
            {
                var targetPoint = targetPoints[i];
                var newPoint = !shouldSmoothPoints ? targetPoint : math.lerp(previousRenderPoints[i], targetPoint, pointSmoothIncrement);

                if (shouldOverwritePoints && finalNumRenderPoints > 0 && maxRenderPoints > 0)
                {
                    var lastRenderPoint = renderPoints[finalNumRenderPoints - 1];
                    if (EvaluateLineEndPoint(targetLineLength, shouldSmoothPoints, targetPoint, lastRenderPoint, ref newPoint, ref length))
                    {
                        renderPoints[finalNumRenderPoints] = newPoint;
                        finalNumRenderPoints++;
                        break;
                    }
                }

                renderPoints[finalNumRenderPoints] = newPoint;
                finalNumRenderPoints++;
            }

            return finalNumRenderPoints;
        }
        
        [BurstCompile]
        static bool EvaluateLineEndPoint(float targetLineLength, bool shouldSmoothPoint, in float3 unsmoothedTargetPoint, in float3 lastRenderPoint, ref float3 newRenderPoint, ref float lineLength)
        {
            var segmentVector = newRenderPoint - lastRenderPoint;
            var segmentLength = math.length(segmentVector);

            if (shouldSmoothPoint)
            {
                var lengthToUnsmoothedSegment = math.distance(lastRenderPoint, unsmoothedTargetPoint);

                // If we hit something, we need to shorten the ray immediately and not wait for the smoothed end point to catch up.
                if (lengthToUnsmoothedSegment < segmentLength)
                {
                    newRenderPoint = lastRenderPoint + math.normalize(segmentVector) * lengthToUnsmoothedSegment;
                    segmentLength = lengthToUnsmoothedSegment;
                }
            }

            lineLength += segmentLength;
            if (lineLength <= targetLineLength)
                return false;

            var delta = lineLength - targetLineLength;

            // Re-project final point to match the desired length
            var tVal = 1 - (delta / segmentLength);
            newRenderPoint = math.lerp(lastRenderPoint, newRenderPoint, tVal);
            return true;
        }
        #endregion
        
    }
}