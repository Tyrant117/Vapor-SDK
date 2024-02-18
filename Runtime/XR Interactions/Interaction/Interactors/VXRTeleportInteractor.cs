using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Utilities;

namespace VaporXR
{
    public class VXRTeleportInteractor : VXRBaseInteractor, IAdvancedLineRenderable, IXRRayProvider
    {
        /// <summary>
        /// Sets which trajectory path Unity uses for the cast when detecting collisions.
        /// </summary>
        /// <seealso cref="VXRRayInteractor.LineType"/>
        public enum LineModeType
        {
            /// <summary>
            /// Performs a single ray cast into the Scene with a set ray length.
            /// </summary>
            StraightLine,

            /// <summary>
            /// Samples the trajectory of a projectile to generate a projectile curve.
            /// </summary>
            ProjectileCurve,

            /// <summary>
            /// Uses a control point and an end point to create a quadratic Bézier curve.
            /// </summary>
            BezierCurve,
        }

        /// <summary>
        /// Sets whether ray cast queries hit Trigger colliders and include or ignore snap volume trigger colliders.
        /// </summary>
        /// <seealso cref="VXRRayInteractor.RaycastSnapVolumeInteraction"/>
        public enum QuerySnapVolumeInteraction
        {
            /// <summary>
            /// Queries never report Trigger hits that are registered with a snap volume.
            /// </summary>
            Ignore,

            /// <summary>
            /// Queries always report Trigger hits that are registered with a snap volume.
            /// </summary>
            Collide,
        }

        /// <summary>
        /// A point within a polygonal chain of endpoints which form line segments
        /// to approximate the curve. Each line segment is where the ray cast starts and ends.
        /// </summary>
        private struct SamplePoint
        {
            /// <summary>
            /// The world space position of the sample.
            /// </summary>
            public float3 Position { get; set; }

            /// <summary>
            /// For <see cref="LineModeType.ProjectileCurve"/>, this represents flight time at the sample.
            /// For <see cref="LineModeType.BezierCurve"/> and <see cref="LineModeType.StraightLine"/>, this represents
            /// the parametric parameter <i>t</i> of the curve at the sample (with range [0, 1]).
            /// </summary>
            public float Parameter { get; set; }
        }

        private const int MaxRaycastHits = 10;
        private const int MaxSphereCastHits = 10;
        private const int MinSampleFrequency = 2;
        private const int MaxSampleFrequency = 100;

        /// <summary>
        /// Reusable list of interactables (used to process the valid targets when this interactor has a filter).
        /// </summary>
        private static readonly List<IXRInteractable> s_Results = new();

        /// <summary>
        /// Reusable list of raycast hits, used to avoid allocations during sphere casting.
        /// </summary>
        private static readonly RaycastHit[] s_SphereCastScratch = new RaycastHit[MaxSphereCastHits];

        /// <summary>
        /// Reusable list of optimal raycast hits, for lookup during sphere casting.
        /// </summary>
        private static readonly HashSet<Collider> s_OptimalHits = new();

        /// <summary>
        /// Reusable list to hold the current sample points.
        /// </summary>
        private static List<SamplePoint> s_ScratchSamplePoints;

        /// <summary>
        /// Reusable array to hold the current control points for a quadratic Bezier curve.
        /// </summary>
        private static readonly float3[] s_ScratchControlPoints = new float3[3];

        /// <summary>
        /// Compares ray cast hits by distance, to sort in ascending order.
        /// </summary>
        protected sealed class RaycastHitComparer : IComparer<RaycastHit>
        {
            /// <summary>
            /// Compares ray cast hits by distance in ascending order.
            /// </summary>
            /// <param name="a">The first ray cast hit to compare.</param>
            /// <param name="b">The second ray cast hit to compare.</param>
            /// <returns>Returns less than 0 if a is closer than b. 0 if a and b are equal. Greater than 0 if b is closer than a.</returns>
            public int Compare(RaycastHit a, RaycastHit b)
            {
                var aDistance = a.collider != null ? a.distance : float.MaxValue;
                var bDistance = b.collider != null ? b.distance : float.MaxValue;
                return aDistance.CompareTo(bDistance);
            }
        }

        #region Inspector
        private bool IsStraightLine => _lineType == LineModeType.StraightLine;
        private bool IsProjectileLine => _lineType == LineModeType.ProjectileCurve;
        private bool IsBezierLine => _lineType == LineModeType.BezierCurve;

        [BoxGroup("Components"), SerializeField, AutoReference(searchParents: true)]
        private VXRInputDeviceUpdateProvider _updateProvider;

        [FoldoutGroup("Interaction"), SerializeField]
        private TargetPriorityMode _targetPriorityMode;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("The layer mask used for limiting ray cast targets.")]
        private LayerMask _raycastMask = -1;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("The type of interaction with trigger colliders via ray cast.")]
        private QueryTriggerInteraction _raycastTriggerInteraction = QueryTriggerInteraction.Ignore;
        [FoldoutGroup("Interaction"), SerializeField]
        [RichTextTooltip("Whether ray cast should include or ignore hits on trigger colliders that are snap volume colliders, even if the ray cast is set to ignore triggers.\n" +
                         "If you are not using gaze assistance or XR Interactable Snap Volume components, you should set this property to <itf>QuerySnapVolumeInteraction</itf>.<mth>Ignore</mth> to avoid the performance cost.")]
        private QuerySnapVolumeInteraction _raycastSnapVolumeInteraction = QuerySnapVolumeInteraction.Collide;

        [FoldoutGroup("Input"), SerializeField]
        private ButtonInputProvider _teleportDrawInput;
        [FoldoutGroup("Input"), SerializeField]
        private ButtonInputProvider _teleportActivateInput;

        [SerializeField, FoldoutGroup("Raycast Configuration")]
        [RichTextTooltip("The starting position and direction of any ray casts.")]
        private Transform _rayOriginTransform;
        [SerializeField, FoldoutGroup("Raycast Configuration")]
        [RichTextTooltip("Blend the line sample points Unity uses for ray casting with the current pose of the controller.\n Use this to make the line visual stay connected with the controller instead of lagging behind.")]
        private bool _blendVisualLinePoints = true;

        [SerializeField, FoldoutGroup("Raycast Configuration")]
        [RichTextTooltip("The type of ray cast.\n" +
                         "<mth>StraightLine:</mth> Performs a single ray cast into the Scene with a set ray length.\n" +
                         "<mth>ProjectileCurve:</mth> Samples the trajectory of a projectile to generate a projectile curve.\n" +
                         "<mth>BezierCurve:</mth> Uses a control point and an end point to create a quadratic Bézier curve.")]
        private LineModeType _lineType = LineModeType.StraightLine;
        [SerializeField, FoldoutGroup("Raycast Configuration")]
        [RichTextTooltip("The reference frame of the curve to define the ground plane and up.\n" +
                         "If not set at startup it will try to find the <cls>VXROrigin</cls>.<mth>Origin</mth> GameObject, and if that does not exist it will use global up and origin by default.")]
        private Transform _referenceFrame;
        [SerializeField, FoldoutGroup("Raycast Configuration")]
        [Range(MinSampleFrequency, MaxSampleFrequency)]
        [RichTextTooltip("The number of sample points Unity uses to approximate curved paths.\n" +
                         "Larger values produce a better quality approximate at the cost of reduced performance due to the number of ray casts.")]
        private int _sampleFrequency = 20;

        [FoldoutGroup("Raycast Configuration"), SerializeField, ShowIf("$IsStraightLine")]
        [RichTextTooltip("Gets or sets the max distance of ray cast when the line type is a straight line. Increasing this value will make the line reach further.")]
        private float _maxRaycastDistance = 30f;

        [FoldoutGroup("Raycast Configuration"), SerializeField,ShowIf("$IsProjectileLine")]
        [RichTextTooltip("Initial velocity of the projectile. Increasing this value will make the curve reach further.")]
        private float _velocity = 16f;
        [FoldoutGroup("Raycast Configuration"), SerializeField, ShowIf("$IsProjectileLine")]
        [RichTextTooltip("Gravity of the projectile in the reference frame.")]
        private float _acceleration = 9.8f;
        [FoldoutGroup("Raycast Configuration"), SerializeField, ShowIf("$IsProjectileLine")]
        [RichTextTooltip("Additional height below ground level that the projectile will continue to. Increasing this value will make the end point drop lower in height.")]
        private float _additionalGroundHeight = 0.1f;
        [FoldoutGroup("Raycast Configuration"), SerializeField, ShowIf("$IsProjectileLine")]
        [RichTextTooltip("Additional flight time after the projectile lands at the adjusted ground level. Increasing this value will make the end point drop lower in height.")]
        private float _additionalFlightTime = 0.5f;

        [FoldoutGroup("Raycast Configuration"), SerializeField, ShowIf("$IsBezierLine")]
        [RichTextTooltip("Increase this value distance to make the end of the curve further from the start point.")]
        private float _endPointDistance = 30f;
        [FoldoutGroup("Raycast Configuration"), SerializeField, ShowIf("$IsBezierLine")]
        [RichTextTooltip("Decrease this value to make the end of the curve drop lower relative to the start point.")]
        private float _endPointHeight = -10f;
        [FoldoutGroup("Raycast Configuration"), SerializeField, ShowIf("$IsBezierLine")]
        [RichTextTooltip("Increase this value to make the peak of the curve further from the start point.")]
        private float _controlPointDistance = 10f;
        [FoldoutGroup("Raycast Configuration"), SerializeField, ShowIf("$IsBezierLine")]
        [RichTextTooltip("Increase this value to make the peak of the curve higher relative to the start point.")]
        private float _controlPointHeight = 5f;
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
        /// Input to use for teleporting to an <see cref="Locomotion.Teleportation.BaseTeleportationInteractable"/> an interactable.
        /// </summary>
        public ButtonInputProvider TeleportInput
        {
            get => _teleportActivateInput;
            set => _teleportActivateInput = value;
        }

        /// <inheritdoc />
        public override TargetPriorityMode TargetPriorityMode
        {
            get => _targetPriorityMode;
            set => _targetPriorityMode = value;
        }

        public bool ShouldDrawLine
        {
            get
            {
                if (IsPerformingManualInteraction)
                    return true;

                return _teleportDrawInput.IsHeld;
            }
        }

        public override bool IsSelectActive
        {
            get
            {
                if (!base.IsSelectActive)
                    return false;

                if (IsPerformingManualInteraction)
                    return true;

                return _teleportActivateInput.IsHeld;
            }
        }

        /// <summary>
        /// The nearest <see cref="IXRInteractable"/> object hit by the ray that was inserted into the valid targets
        /// list when not selecting anything.
        /// </summary>
        /// <remarks>
        /// Updated during <see cref="PreprocessInteractor"/>.
        /// </remarks>
        protected IXRInteractable CurrentNearestValidTarget { get; private set; }

        private Vector3 ReferenceUp => _hasReferenceFrame ? _referenceFrame.up : Vector3.up;

        private Vector3 ReferencePosition => _hasReferenceFrame ? _referenceFrame.position : Vector3.zero;

        /// <summary>
        /// The starting position and direction of any ray casts.
        /// Safe version of <see cref="RayOriginTransform"/>, falls back to this Transform if not set.
        /// </summary>
        private Transform EffectiveRayOrigin => _hasRayOriginTransform ? _rayOriginTransform : transform;

        /// <inheritdoc />
        public Vector3 RayEndPoint { get; private set; }

        /// <inheritdoc />
        public Transform RayEndTransform { get; private set; }

        /// <summary>
        /// The starting position and direction of any ray casts.
        /// </summary>
        /// <remarks>
        /// Automatically instantiated and set in <see cref="Awake"/> if <see langword="null"/>
        /// and initialized with the pose of the <see cref="VXRBaseInteractor.AttachTransform"/>.
        /// Setting this will not automatically destroy the previous object.
        /// </remarks>
        public Transform RayOriginTransform
        {
            get => _rayOriginTransform;
            set
            {
                _rayOriginTransform = value;
                _hasRayOriginTransform = _rayOriginTransform != null;
            }
        }

        public LineModeType LineType => _lineType;
        #endregion

        #region Fields
        private static readonly List<IXRActivateInteractable> s_ActivateTargets = new();

        private readonly LinkedPool<ActivateEventArgs> _activateEventArgs = new(() => new ActivateEventArgs(), collectionCheck: false);
        private readonly LinkedPool<DeactivateEventArgs> _deactivateEventArgs = new(() => new DeactivateEventArgs(), collectionCheck: false);

        private VXRInteractorAudioFeedback _audioFeedback;
        private VXRInteractorHapticFeedback _hapticFeedback;

        private AudioSource _audioSource;
        private HapticImpulsePlayer _hapticImpulsePlayer;

        private bool _hasRayOriginTransform;
        private bool _hasReferenceFrame;

        private readonly List<IXRInteractable> _validTargets = new List<IXRInteractable>();

        private readonly RaycastHit[] _raycastHits = new RaycastHit[MaxRaycastHits];
        private int _raycastHitsCount;
        private readonly RaycastHitComparer _raycastHitComparer = new RaycastHitComparer();

        /// <summary>
        /// A polygonal chain represented by a list of endpoints which form line segments
        /// to approximate the curve. Each line segment is where the ray cast starts and ends.
        /// World space coordinates.
        /// </summary>
        private List<SamplePoint> _samplePoints;

        /// <summary>
        /// The <see cref="Time.frameCount"/> when Unity last updated the sample points.
        /// Used as an optimization to avoid recomputing the points during <see cref="PreprocessInteractor"/>
        /// when it was already computed and used for an input module in <see cref="UpdateUIModel"/>.
        /// </summary>
        private int _samplePointsFrameUpdated = -1;

        /// <summary>
        /// The index of the sample endpoint if a 3D hit occurred. Otherwise, a value of <c>0</c> if no hit occurred.
        /// </summary>
        private int _raycastHitEndpointIndex;

        /// <summary>
        /// The index of the sample endpoint if a UI hit occurred. Otherwise, a value of <c>0</c> if no hit occurred.
        /// </summary>
        private int _uiRaycastHitEndpointIndex;

        /// <summary>
        /// Control points to calculate the quadratic Bezier curve used for aiming.
        /// </summary>
        private readonly float3[] _controlPoints = new float3[3];

        /// <summary>
        /// Control points to calculate the equivalent quadratic Bezier curve to the endpoint where a hit occurred.
        /// </summary>
        private readonly float3[] _hitChordControlPoints = new float3[3];

        private PhysicsScene _localPhysicsScene;

        // Cached raycast data
        private bool _raycastHitOccurred;
        private RaycastHit _raycastHit;
        private IXRInteractable _raycastInteractable;

        /// <summary>
        /// The closest index of the sample endpoint where a 3D or UI hit occurred.
        /// </summary>
        private int ClosestAnyHitIndex => (_raycastHitEndpointIndex > 0 && _uiRaycastHitEndpointIndex > 0) // Are both valid?
            ? Mathf.Min(_raycastHitEndpointIndex, _uiRaycastHitEndpointIndex) // When both are valid, return the closer one
            : (_raycastHitEndpointIndex > 0 ? _raycastHitEndpointIndex : _uiRaycastHitEndpointIndex); // Otherwise return the valid one        
        #endregion


        #region - Initialization -
        protected override void Awake()
        {
            TargetsForSelection = new List<IXRSelectInteractable>();

            base.Awake();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _teleportDrawInput.BindToUpdateEvent(_updateProvider);
            _teleportActivateInput.BindToUpdateEvent(_updateProvider);
        }

        protected override void OnDisable()
        {
            _teleportDrawInput.UnbindUpdateEvent();
            _teleportActivateInput.UnbindUpdateEvent();
            base.OnDisable();
        }
        #endregion

        #region - Processing -
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) return;

            UpdateSamplePointsIfNecessary();
            if (_samplePoints != null && _samplePoints.Count >= 2)
            {
                // Perform ray casts and store the equivalent Bezier curve to the endpoint where a hit occurred (used for blending)
                UpdateRaycastHits();
                CacheRaycastHit();
                CreateBezierCurve(_samplePoints, ClosestAnyHitIndex, _hitChordControlPoints);
            }

            // Determine the Interactables that this Interactor could possibly interact with this frame
            GetValidTargets(_validTargets);

            // Check to see if we have a new hover object.
            // This handles auto select and deselect.
            var nearestObject = (_validTargets.Count > 0) ? _validTargets[0] : null;
            if (nearestObject != CurrentNearestValidTarget && !HasSelection)
            {
                CurrentNearestValidTarget = nearestObject;
            }
        }

        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractor(updatePhase);
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) return;
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

            if (_raycastHitsCount > 0)
            {
                for (var i = 0; i < _raycastHitsCount; ++i)
                {
                    var raycastHit = _raycastHits[i];

                    // A hit on geometry not associated with Interactables should block Interactables behind it from being a valid target
                    if (!InteractionManager.TryGetInteractableForCollider(raycastHit.collider, out var interactable))
                    {
                        break;
                    }

                    if (!targets.Contains(interactable))
                    {
                        targets.Add(interactable);

                        // Stop after the first if enabled
                        break;
                    }
                }
            }

            var filter = TargetFilter;
            if (filter != null && filter.CanProcess)
            {
                filter.Process(this, targets, s_Results);

                // Copy results elements to targets
                targets.Clear();
                targets.AddRange(s_Results);
            }
        }
        #endregion

        #region - Raycasting -
        private void CreateRayOrigin()
        {
            _hasRayOriginTransform = _rayOriginTransform != null;
            if (_hasRayOriginTransform)
            {
                return;
            }

            _rayOriginTransform = new GameObject($"[{gameObject.name}] Ray Origin").transform;
            _hasRayOriginTransform = true;
            _rayOriginTransform.SetParent(transform, false);

            if (AttachTransform == null)
            {
                CreateAttachTransform();
            }

            // Keep the position value seen in the Inspector tidier
            if (AttachTransform == null)
            {
                _rayOriginTransform.localPosition = Vector3.zero;
                _rayOriginTransform.localRotation = Quaternion.identity;
            }
            else if (AttachTransform.parent == transform)
            {
                _rayOriginTransform.localPosition = AttachTransform.localPosition;
                _rayOriginTransform.localRotation = AttachTransform.localRotation;
            }
            else
            {
                _rayOriginTransform.SetPositionAndRotation(AttachTransform.position, AttachTransform.rotation);
            }
        }

        /// <inheritdoc />
        public Transform GetOrCreateRayOrigin()
        {
            CreateRayOrigin();
            return _rayOriginTransform;
        }

        /// <inheritdoc />
        public Transform GetOrCreateAttachTransform()
        {
            CreateAttachTransform();
            return AttachTransform;
        }

        /// <inheritdoc />
        public void SetRayOrigin(Transform newOrigin)
        {
            RayOriginTransform = newOrigin;
        }

        /// <inheritdoc />
        public void SetAttachTransform(Transform newAttach)
        {
            AttachTransform = newAttach;
        }

        /// <inheritdoc />
        public bool GetLinePoints(ref NativeArray<Vector3> linePoints, out int numPoints, Ray? rayOriginOverride = null)
        {
            if (_samplePoints == null || _samplePoints.Count < 2)
            {
                numPoints = default;
                return false;
            }

            NativeArray<float3> linePointsAsFloat;

            if (!_blendVisualLinePoints)
            {
                numPoints = _samplePoints.Count;
                EnsureCapacity(ref linePoints, numPoints);
                linePointsAsFloat = linePoints.Reinterpret<float3>();

                for (var i = 0; i < numPoints; ++i)
                    linePointsAsFloat[i] = _samplePoints[i].Position;

                return true;
            }

            // Because this method may be invoked during OnBeforeRender, the current positions
            // of sample points may be different as the controller moves. Recompute the current
            // positions of sample points.
            CreateSamplePointsListsIfNecessary();
            UpdateSamplePoints(_samplePoints.Count, s_ScratchSamplePoints, rayOriginOverride);

            if (_lineType == LineModeType.StraightLine)
            {
                numPoints = 2;
                EnsureCapacity(ref linePoints, numPoints);
                linePointsAsFloat = linePoints.Reinterpret<float3>();

                linePointsAsFloat[0] = s_ScratchSamplePoints[0].Position;
                linePointsAsFloat[1] = _samplePoints[_samplePoints.Count - 1].Position;

                return true;
            }

            // Recompute the equivalent Bezier curve.
            var hitIndex = ClosestAnyHitIndex;
            CreateBezierCurve(s_ScratchSamplePoints, hitIndex, s_ScratchControlPoints, rayOriginOverride);

            // Blend between the current curve and the sample curve,
            // using the beginning of the current curve and the end of the sample curve.
            // Together it forms a new cubic Bezier curve with control points P₀, P₁, P₂, P₃.
            CurveUtility.ElevateQuadraticToCubicBezier(s_ScratchControlPoints[0], s_ScratchControlPoints[1], s_ScratchControlPoints[2],
                out var p0, out var p1, out _, out _);
            CurveUtility.ElevateQuadraticToCubicBezier(_hitChordControlPoints[0], _hitChordControlPoints[1], _hitChordControlPoints[2],
                out _, out _, out var p2, out var p3);

            if (hitIndex > 0 && hitIndex != _samplePoints.Count - 1 && _lineType == LineModeType.ProjectileCurve)
            {
                numPoints = _samplePoints.Count;
                EnsureCapacity(ref linePoints, numPoints);
                linePointsAsFloat = linePoints.Reinterpret<float3>();

                linePointsAsFloat[0] = p0;

                // Sample from the blended cubic Bezier curve
                // until the line segment endpoint where the hit occurred.
                var interval = 1f / hitIndex;
                for (var i = 1; i <= hitIndex; ++i)
                {
                    // Parametric parameter t where 0 ≤ t ≤ 1
                    var percent = i * interval;
                    CurveUtility.SampleCubicBezierPoint(p0, p1, p2, p3, percent, out var point);
                    linePointsAsFloat[i] = point;
                }

                // Use the original sample curve beyond that point.
                for (var i = hitIndex + 1; i < _samplePoints.Count; ++i)
                {
                    linePointsAsFloat[i] = _samplePoints[i].Position;
                }
            }
            else
            {
                numPoints = _sampleFrequency;
                EnsureCapacity(ref linePoints, numPoints);
                linePointsAsFloat = linePoints.Reinterpret<float3>();

                linePointsAsFloat[0] = p0;

                // Sample from the blended cubic Bezier curve
                var interval = 1f / (_sampleFrequency - 1);
                for (var i = 1; i < _sampleFrequency; ++i)
                {
                    // Parametric parameter t where 0 ≤ t ≤ 1
                    var percent = i * interval;
                    CurveUtility.SampleCubicBezierPoint(p0, p1, p2, p3, percent, out var point);
                    linePointsAsFloat[i] = point;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public bool GetLinePoints(ref Vector3[] linePoints, out int numPoints)
        {
            if (linePoints == null)
            {
                linePoints = Array.Empty<Vector3>();
            }

            var tempNativeArray = new NativeArray<Vector3>(linePoints, Allocator.Temp);
            var getLinePointsSuccessful = GetLinePoints(ref tempNativeArray, out numPoints);

            // Resize line points array to match destination target
            var tempArrayLength = tempNativeArray.Length;
            if (linePoints.Length != tempArrayLength)
            {
                linePoints = new Vector3[tempArrayLength];
            }

            // Move point data back into line points
            tempNativeArray.CopyTo(linePoints);
            tempNativeArray.Dispose();

            return getLinePointsSuccessful;
        }

        /// <inheritdoc />
        public bool TryGetHitInfo(out Vector3 position, out Vector3 normal, out int positionInLine, out bool isValidTarget)
        {
            position = default;
            normal = default;
            positionInLine = default;
            isValidTarget = default;

            if (!TryGetCurrentRaycast(out var raycastHit, out var raycastHitIndex, out var raycastResultIndex))
            {
                return false;
            }

            if (raycastHit.HasValue)
            {
                position = raycastHit.Value.point;
                normal = raycastHit.Value.normal;
                positionInLine = raycastHitIndex;

                // Determine if the collider is registered as an interactable and the interactable is being hovered
                isValidTarget = InteractionManager.TryGetInteractableForCollider(raycastHit.Value.collider, out var interactable) &&
                    IsHovering(interactable);
            }

            return true;
        }

        private void CreateSamplePointsListsIfNecessary()
        {
            if (_samplePoints != null && s_ScratchSamplePoints != null)
                return;

            var capacity = _lineType == LineModeType.StraightLine ? 2 : _sampleFrequency;

            _samplePoints ??= new List<SamplePoint>(capacity);
            s_ScratchSamplePoints ??= new List<SamplePoint>(capacity);
        }

        /// <summary>
        /// Update curve approximation used for ray casts for this frame.
        /// </summary>
        /// <remarks>
        /// This method is called first by <see cref="UpdateUIModel"/> due to the UI Input Module
        /// before <see cref="PreprocessInteractor"/> gets called later in the frame, so this
        /// method is a performance optimization so it only gets done once each frame.
        /// </remarks>
        private void UpdateSamplePointsIfNecessary()
        {
            CreateSamplePointsListsIfNecessary();
            if (_samplePointsFrameUpdated != Time.frameCount)
            {
                UpdateSamplePoints(_sampleFrequency, _samplePoints);
                _samplePointsFrameUpdated = Time.frameCount;
            }
        }

        /// <summary>
        /// Approximates the curve into a polygonal chain of endpoints, whose line segments can be used as
        /// the rays for doing Physics ray casts.
        /// </summary>
        /// <param name="count">The number of sample points to calculate.</param>
        /// <param name="samplePoints">The result list of sample points to populate.</param>
        /// <param name="rayOriginOverride">Optional ray origin override used when re-computing the line.</param>
        private void UpdateSamplePoints(int count, List<SamplePoint> samplePoints, Ray? rayOriginOverride = null)
        {
            Assert.IsTrue(count >= 2);
            Assert.IsNotNull(samplePoints);

            GetLineOriginAndDirection(rayOriginOverride, out var lineOrigin, out var lineDirection);

            samplePoints.Clear();
            var samplePoint = new SamplePoint
            {
                Position = lineOrigin,
                Parameter = 0f,
            };
            samplePoints.Add(samplePoint);

            switch (_lineType)
            {
                case LineModeType.StraightLine:
                    samplePoint.Position = samplePoints[0].Position + (float3)lineDirection * _maxRaycastDistance;
                    samplePoint.Parameter = 1f;
                    samplePoints.Add(samplePoint);
                    break;
                case LineModeType.ProjectileCurve:
                    {
                        var initialPosition = (float3)lineOrigin;
                        CalculateProjectileParameters(initialPosition, lineDirection, out var initialVelocity, out var constantAcceleration, out var flightTime);

                        var interval = flightTime / (count - 1);
                        for (var i = 1; i < count; ++i)
                        {
                            var time = i * interval;
                            CurveUtility.SampleProjectilePoint(initialPosition, initialVelocity, constantAcceleration, time, out var position);
                            samplePoint.Position = position;
                            samplePoint.Parameter = time;
                            samplePoints.Add(samplePoint);
                        }
                        break;
                    }
                case LineModeType.BezierCurve:
                    {
                        // Update control points for Bezier curve
                        UpdateBezierControlPoints(lineOrigin, lineDirection, ReferenceUp);
                        var p0 = _controlPoints[0];
                        var p1 = _controlPoints[1];
                        var p2 = _controlPoints[2];

                        var interval = 1f / (count - 1);
                        for (var i = 1; i < count; ++i)
                        {
                            // Parametric parameter t where 0 ≤ t ≤ 1
                            var percent = i * interval;
                            CurveUtility.SampleQuadraticBezierPoint(p0, p1, p2, percent, out var position);
                            samplePoint.Position = position;
                            samplePoint.Parameter = percent;
                            samplePoints.Add(samplePoint);
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Walks the line segments from the approximated curve, casting from one endpoint to the next.
        /// </summary>
        private void UpdateRaycastHits()
        {
            _raycastHitsCount = 0;
            _raycastHitEndpointIndex = 0;

            var has3DHit = false;
            for (var i = 1; i < _samplePoints.Count; ++i)
            {
                var origin = _samplePoints[0].Position;
                var fromPoint = _samplePoints[i - 1].Position;
                var toPoint = _samplePoints[i].Position;

                CheckCollidersBetweenPoints(fromPoint, toPoint, origin);
                if (_raycastHitsCount > 0)
                {
                    _raycastHitEndpointIndex = i;
                    has3DHit = true;
                }

                if (has3DHit)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Gets the first 3D and UI ray cast hits and caches them for further lookup, if any ray cast hits are available.
        /// </summary>
        private void CacheRaycastHit()
        {
            _raycastHit = default;
         
            _raycastHitOccurred = false;
            RayEndTransform = null;
            _raycastInteractable = null;

            var hitIndex = int.MaxValue;
            var distance = float.MaxValue;
            if (TryGetCurrent3DRaycastHit(out var raycastHitValue, out var raycastHitIndex))
            {
                _raycastHit = raycastHitValue;
                hitIndex = raycastHitIndex;
                distance = raycastHitValue.distance;

                _raycastHitOccurred = true;
            }

            if (_raycastHitOccurred)
            {
                RayEndPoint = _raycastHit.point;
                RayEndTransform = InteractionManager.TryGetInteractableForCollider(_raycastHit.collider, out _raycastInteractable)
                    ? _raycastInteractable.GetAttachTransform(this)
                    : _raycastHit.transform;
            }
            else
            {
                UpdateSamplePointsIfNecessary();
                RayEndPoint = _samplePoints[^1].Position;
            }
        }

        private void CheckCollidersBetweenPoints(Vector3 from, Vector3 to, Vector3 origin)
        {
            Array.Clear(_raycastHits, 0, MaxRaycastHits);
            _raycastHitsCount = 0;

            var direction = (to - from).normalized;
            var maxDistance = Vector3.Distance(to, from);
            var queryTriggerInteraction = _raycastSnapVolumeInteraction == QuerySnapVolumeInteraction.Collide
                ? QueryTriggerInteraction.Collide
                : _raycastTriggerInteraction;

            _raycastHitsCount = _localPhysicsScene.Raycast(from, direction, _raycastHits, maxDistance, _raycastMask, queryTriggerInteraction);

            if (_raycastHitsCount > 0)
            {
                var baseQueryHitsTriggers = _raycastTriggerInteraction == QueryTriggerInteraction.Collide ||
                    (_raycastTriggerInteraction == QueryTriggerInteraction.UseGlobal && Physics.queriesHitTriggers);

                if (_raycastSnapVolumeInteraction == QuerySnapVolumeInteraction.Ignore && baseQueryHitsTriggers)
                {
                    // Filter out Snap Volume trigger collider hits
                    _raycastHitsCount = FilterTriggerColliders(InteractionManager, _raycastHits, _raycastHitsCount, snapVolume => snapVolume != null);
                }
                else if (_raycastSnapVolumeInteraction == QuerySnapVolumeInteraction.Collide && !baseQueryHitsTriggers)
                {
                    // Filter out trigger collider hits that are not Snap Volume snap colliders
                    _raycastHitsCount = FilterTriggerColliders(InteractionManager, _raycastHits, _raycastHitsCount, snapVolume => snapVolume == null);
                }

                // Sort all the hits by distance along the curve since the results of the 3D ray cast are not ordered.
                // Sorting is done after filtering above for performance.
                SortingHelpers.Sort(_raycastHits, _raycastHitComparer, _raycastHitsCount);
            }
        }

        private void CreateBezierCurve(List<SamplePoint> samplePoints, int endSamplePointIndex, float3[] quadraticControlPoints, Ray? rayOriginOverride = null)
        {
            // Convert the ray cast curve ranging from the controller to the sample endpoint
            // where the hit occurred into a quadratic Bezier curve
            // with control points P₀, P₁, P₂.
            var endSamplePoint = endSamplePointIndex > 0 && endSamplePointIndex < samplePoints.Count
                ? samplePoints[endSamplePointIndex]
                : samplePoints[samplePoints.Count - 1];
            var p2 = endSamplePoint.Position;
            var p0 = samplePoints[0].Position;

            var midpoint = 0.5f * (p0 + p2);

            switch (_lineType)
            {
                case LineModeType.StraightLine:
                    quadraticControlPoints[0] = p0;
                    quadraticControlPoints[1] = midpoint;
                    quadraticControlPoints[2] = p2;
                    break;
                case LineModeType.ProjectileCurve:
                    GetLineOriginAndDirection(rayOriginOverride, out var lineOrigin, out var lineDirection);
                    CalculateProjectileParameters(lineOrigin, lineDirection, out var initialVelocity, out var constantAcceleration, out _);

                    var midTime = 0.5f * endSamplePoint.Parameter;
                    CurveUtility.SampleProjectilePoint(p0, initialVelocity, constantAcceleration, midTime, out var sampleMidTime);
                    var p1 = midpoint + 2f * (sampleMidTime - midpoint);

                    quadraticControlPoints[0] = p0;
                    quadraticControlPoints[1] = p1;
                    quadraticControlPoints[2] = p2;
                    break;
                case LineModeType.BezierCurve:
                    quadraticControlPoints[0] = _controlPoints[0];
                    quadraticControlPoints[1] = _controlPoints[1];
                    quadraticControlPoints[2] = _controlPoints[2];
                    break;
            }
        }

        /// <summary>
        /// Calculates the quadratic Bezier control points used for <see cref="LineModeType.BezierCurve"/>.
        /// </summary>
        private void UpdateBezierControlPoints(in float3 lineOrigin, in float3 lineDirection, in float3 curveReferenceUp)
        {
            _controlPoints[0] = lineOrigin;
            _controlPoints[1] = _controlPoints[0] + lineDirection * _controlPointDistance + curveReferenceUp * _controlPointHeight;
            _controlPoints[2] = _controlPoints[0] + lineDirection * _endPointDistance + curveReferenceUp * _endPointHeight;
        }

        [BurstCompile]
        private void CalculateProjectileParameters(in float3 lineOrigin, in float3 lineDirection, out float3 initialVelocity, out float3 constantAcceleration, out float flightTime)
        {
            initialVelocity = lineDirection * _velocity;
            var referenceUpAsFloat3 = (float3)ReferenceUp;
            var referencePositionAsFloat3 = (float3)ReferencePosition;
            constantAcceleration = referenceUpAsFloat3 * -_acceleration;
            var angleRad = math.sin(GetProjectileAngle(lineDirection) * Mathf.Deg2Rad);

            var projectedReferenceOffset = math.project(referencePositionAsFloat3 - lineOrigin, referenceUpAsFloat3);
            var height = math.length(projectedReferenceOffset) + _additionalGroundHeight;

            CurveUtility.CalculateProjectileFlightTime(_velocity, _acceleration, angleRad, height, _additionalFlightTime, out flightTime);
        }

        private float GetProjectileAngle(Vector3 lineDirection)
        {
            var up = ReferenceUp;
            var projectedForward = Vector3.ProjectOnPlane(lineDirection, up);
            return Mathf.Approximately(Vector3.Angle(lineDirection, projectedForward), 0f)
                ? 0f
                : Vector3.SignedAngle(lineDirection, projectedForward, Vector3.Cross(up, lineDirection));
        }

        public void GetLineOriginAndDirection(out Vector3 origin, out Vector3 direction) => GetLineOriginAndDirection(EffectiveRayOrigin, out origin, out direction);

        private void GetLineOriginAndDirection(Ray? rayOriginOverride, out Vector3 origin, out Vector3 direction)
        {
            if (rayOriginOverride.HasValue)
            {
                var ray = rayOriginOverride.Value;
                origin = ray.origin;
                direction = ray.direction;
            }
            else
            {
                GetLineOriginAndDirection(out origin, out direction);
            }
        }

        /// <summary>
        /// Gets the first 3D and UI ray cast hits, if any ray cast hits are available.
        /// </summary>
        /// <param name="raycastHit">When this method returns, contains the ray cast hit if available; otherwise, the default value.</param>
        /// <param name="raycastHitIndex">When this method returns, contains the index of the sample endpoint if a hit occurred.
        /// Otherwise, a value of <c>0</c> if no hit occurred.</param>
        /// <param name="uiRaycastHit">When this method returns, contains the UI ray cast result if available; otherwise, the default value.</param>
        /// <param name="uiRaycastHitIndex">When this method returns, contains the index of the sample endpoint if a hit occurred.
        /// Otherwise, a value of <c>0</c> if no hit occurred.</param>
        /// <param name="isUIHitClosest">When this method returns, contains whether the UI ray cast result was the closest hit.</param>
        /// <returns>Returns <see langword="true"/> if either hit occurred, implying the ray cast hit information is valid.
        /// Otherwise, returns <see langword="false"/>.</returns>
        public bool TryGetCurrentRaycast(out RaycastHit? raycastHit, out int raycastHitIndex, out int uiRaycastHitIndex)
        {
            raycastHit = _raycastHit;
            raycastHitIndex = _raycastHitEndpointIndex;
            uiRaycastHitIndex = _uiRaycastHitEndpointIndex;

            return _raycastHitOccurred;
        }

        /// <inheritdoc cref="TryGetCurrent3DRaycastHit(out RaycastHit, out int)"/>
        public bool TryGetCurrent3DRaycastHit(out RaycastHit raycastHit)
        {
            return TryGetCurrent3DRaycastHit(out raycastHit, out _);
        }

        /// <summary>
        /// Gets the first 3D ray cast hit, if any ray cast hits are available.
        /// </summary>
        /// <param name="raycastHit">When this method returns, contains the ray cast hit if available; otherwise, the default value.</param>
        /// <param name="raycastEndpointIndex">When this method returns, contains the index of the sample endpoint if a hit occurred.
        /// Otherwise, a value of <c>0</c> if no hit occurred.</param>
        /// <returns>Returns <see langword="true"/> if a hit occurred, implying the ray cast hit information is valid.
        /// Otherwise, returns <see langword="false"/>.</returns>
        public bool TryGetCurrent3DRaycastHit(out RaycastHit raycastHit, out int raycastEndpointIndex)
        {
            if (_raycastHitsCount > 0)
            {
                Assert.IsTrue(_raycastHits.Length >= _raycastHitsCount);
                raycastHit = _raycastHits[0];
                raycastEndpointIndex = _raycastHitEndpointIndex;
                return true;
            }

            raycastHit = default;
            raycastEndpointIndex = default;
            return false;
        }
        #endregion

        #region - Hovering -
        public override bool CanHover(IXRHoverInteractable interactable)
        {
            return base.CanHover(interactable) && (!HasSelection || IsSelecting(interactable));
        }
        #endregion

        #region - Selection -
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return base.CanSelect(interactable) && (!HasSelection || IsSelecting(interactable));
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

        #region - Helpers -
        private static void GetLineOriginAndDirection(Transform rayOrigin, out Vector3 origin, out Vector3 direction)
        {
            origin = rayOrigin.position;
            direction = rayOrigin.forward;
        }

        private static int FilterTriggerColliders(VXRInteractionManager interactionManager, RaycastHit[] raycastHits, int count, Func<VXRInteractableSnapVolume, bool> removeRule)
        {
            for (var index = 0; index < count; ++index)
            {
                var hitCollider = raycastHits[index].collider;
                if (hitCollider.isTrigger)
                {
                    interactionManager.TryGetInteractableForCollider(hitCollider, out _, out var snapVolume);
                    if (removeRule(snapVolume))
                    {
                        RemoveAt(raycastHits, index, count);
                        --count;
                        --index;
                    }
                }
            }

            return count;

            static void RemoveAt<T>(T[] array, int index, int count) where T : struct
            {
                Array.Copy(array, index + 1, array, index, count - index - 1);
                Array.Clear(array, count - 1, 1);
            }
        }

        private static void EnsureCapacity(ref NativeArray<Vector3> linePoints, int numPoints)
        {
            if (linePoints.IsCreated && linePoints.Length < numPoints)
            {
                linePoints.Dispose();
                linePoints = new NativeArray<Vector3>(numPoints, Allocator.Persistent);
            }
            else if (!linePoints.IsCreated)
            {
                linePoints = new NativeArray<Vector3>(numPoints, Allocator.Persistent);
            }
        }
        #endregion

        #region - Debug -
        protected virtual void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || _samplePoints == null || _samplePoints.Count < 2)
            {
                return;
            }

            if (TryGetCurrent3DRaycastHit(out var raycastHit))
            {
                // Draw the normal of the surface at the hit point
                Gizmos.color = new Color(58 / 255f, 122 / 255f, 248 / 255f, 237 / 255f);
                const float length = 0.075f;
                Gizmos.DrawLine(raycastHit.point, raycastHit.point + raycastHit.normal.normalized * length);
            }

            var hitIndex = ClosestAnyHitIndex;

            // Draw sample points where the ray cast line segments took place
            for (var i = 0; i < _samplePoints.Count; ++i)
            {
                var samplePoint = _samplePoints[i];

                // Change the color of the points after the segment where a hit happened
                var radius = 0.025f;
                var color = hitIndex == 0 || i < hitIndex
                    ? new Color(163 / 255f, 73 / 255f, 164 / 255f, 0.75f)
                    : new Color(205 / 255f, 143 / 255f, 205 / 255f, 0.5f);
                Gizmos.color = color;
                Gizmos.DrawSphere(samplePoint.Position, radius);
                if (i < _samplePoints.Count - 1)
                {
                    var nextPoint = _samplePoints[i + 1];
                    Gizmos.DrawLine(samplePoint.Position, nextPoint.Position);
                }
            }

            switch (_lineType)
            {
                case LineModeType.ProjectileCurve:
                    DrawQuadraticBezierGizmo(_hitChordControlPoints[0], _hitChordControlPoints[1], _hitChordControlPoints[2]);
                    break;
                case LineModeType.BezierCurve:
                    DrawQuadraticBezierGizmo(_controlPoints[0], _controlPoints[1], _controlPoints[2]);
                    break;
            }

            static void DrawQuadraticBezierGizmo(Vector3 p0, Vector3 p1, Vector3 p2)
            {
                // Draw the control points of the quadratic Bezier curve
                // (P₀ = start point, P₁ = control point, P₂ = end point)
                const float radius = 0.025f;
                Gizmos.color = new Color(1f, 0f, 0f, 0.75f);
                Gizmos.DrawSphere(p0, radius);
                Gizmos.DrawSphere(p1, radius);
                Gizmos.DrawSphere(p2, radius);

                // Draw lines between the control points
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.75f);
                Gizmos.DrawLine(p0, p1);
                Gizmos.DrawLine(p1, p2);

                // Draw tangent lines along the curve like string art
                // (Q₀ = intermediate start point, Q₁ = intermediate end point, and the linear interpolation between them is the curve)
                Gizmos.color = new Color(0f, 0f, 205 / 25f, 0.75f);
                for (var t = 0.1f; t <= 0.9f; t += 0.1f)
                {
                    var q0 = Vector3.Lerp(p0, p1, t);
                    var q1 = Vector3.Lerp(p1, p2, t);
                    Gizmos.DrawLine(q0, q1);
                }
            }
        }
        #endregion
    }
}
