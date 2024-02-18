using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.UI;
using VaporXR.Utilities;

#if AR_FOUNDATION_PRESENT
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#endif

namespace VaporXR
{
    // ReSharper disable once InconsistentNaming
    public class VXRRayInteractor : VXRInputInteractor, IAdvancedLineRenderable, IUIHoverInteractor, IXRRayProvider, IXRScaleValueProvider
#if AR_FOUNDATION_PRESENT
    ,IARInteractor
#endif
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
        /// Sets which shape of physics cast to use for the cast when detecting collisions.
        /// </summary>
        /// <seealso cref="VXRRayInteractor.HitDetectionType"/>
        public enum HitDetectionModeType
        {
            /// <summary>
            /// Uses <see cref="Physics"/> Ray cast to detect collisions.
            /// </summary>
            Raycast,

            /// <summary>
            /// Uses <see cref="Physics"/> Sphere Cast to detect collisions.
            /// </summary>
            SphereCast,

            /// <summary>
            /// Uses cone casting to detect collisions.
            /// </summary>
            ConeCast,
        }

        /// <summary>
        /// Sets how Attach Transform rotation is controlled.
        /// </summary>
        /// <seealso cref="VXRRayInteractor.RotateMode"/>
        public enum RotateModeType
        {
            /// <summary>
            /// The Attach Transform rotates over time while rotation input is active.
            /// </summary>
            RotateOverTime,

            /// <summary>
            /// The Attach Transform rotates to match the direction of the 2-dimensional rotation input.
            /// </summary>
            MatchDirection,
        }
        
        private const int MaxRaycastHits = 10;
        // How many ray hits to register when sphere casting.
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
        [SerializeField, FoldoutGroup("Interaction")]
        [RichTextTooltip("Allows the user to move the Attach Transform using the thumbstick.")]
        private bool _manipulateAttachTransform = true;
        [SerializeField, FoldoutGroup("Interaction")]
        [RichTextTooltip("Force grab moves the object to your hand rather than interacting with it at a distance.")]
        private bool _useForceGrab = true;
        [SerializeField, FoldoutGroup("Interaction")]
        [RichTextTooltip("Speed that the Attach Transform is rotated when <itf>RotateModeType</itf> is set to <itf>RotateModeType</itf>.<mth>RotateOverTime</mth>.")]
        private float _rotateSpeed = 180f;
        [SerializeField, FoldoutGroup("Interaction")]
        [RichTextTooltip("Speed that the Attach Transform is translated along the ray.")]
        private float _translateSpeed = 1f;
        [SerializeField, FoldoutGroup("Interaction")]
        [RichTextTooltip("The optional reference frame to define the up axis when rotating the Attach Transform.\nWhen not set, rotates about the local up axis of the Attach Transform.")]
        private Transform _rotateReferenceFrame;
        [SerializeField, FoldoutGroup("Interaction")]
        [RichTextTooltip("How the Attach Transform rotation manipulation is controlled.\n" +
                         "<mth>RotateOverTime:</mth> The Attach Transform rotates over time while rotation input is active.\n" +
                         "<mth>MatchDirection:</mth> The Attach Transform rotates to match the direction of the 2-dimensional rotation input.")]
        private RotateModeType _rotateMode;
        [SerializeField, FoldoutGroup("Interaction")]
        [RichTextTooltip("Property representing the scale mode that is supported by the implementation of the interface.\n" +
                         "<mth>None:</mth> No scale mode is active or supported. Use this when a controller does not support scaling or when scaling is not needed.\n" +
                         "<mth>ScaleOverTime:</mth> The scale is resized over time and represented by input in range of -1 to 1. This mode is typically used with a thumbstick input on a controller.\n" +
                         "<mth>DistanceDelta:</mth> The scale is based on the delta distance between 2 physical (or virtual) inputs, such as the pinch gap between fingers where the distance is calculated based on the screen DPI, and delta from the previous frame." +
                         "This mode is typically used with a touchscreen for mobile AR.")]
        private ScaleMode _scaleMode = ScaleMode.None;
        
        [SerializeField, FoldoutGroup("Select")]
        [RichTextTooltip("Whether this Interactor will automatically select an Interactable after hovering over it for a period of time.")]
        private bool _hoverToSelect;
        [SerializeField, FoldoutGroup("Select")]
        [RichTextTooltip("Number of seconds for which this Interactor must hover over an Interactable to select it if Hover To Select is enabled.")]
        private float _hoverTimeToSelect = 0.5f;
        [SerializeField, FoldoutGroup("Select")]
        [RichTextTooltip("Whether this Interactor will automatically deselect an Interactable after selecting it via hover for a period of time.")]
        private bool _autoDeselect;
        [SerializeField, FoldoutGroup("Select")]
        [RichTextTooltip("Number of seconds for which this Interactor will keep an Interactable selected before automatically deselecting it.")]
        private float _timeToAutoDeselect = 3f;
        
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
        
        [FoldoutGroup("Raycast Configuration")]
        [SerializeField, VerticalGroup("Raycast Configuration/Straight")]
        [RichTextTooltip("Gets or sets the max distance of ray cast when the line type is a straight line. Increasing this value will make the line reach further.")]
        private float _maxRaycastDistance = 30f;
        
        [FoldoutGroup("Raycast Configuration")]
        [SerializeField, VerticalGroup("Raycast Configuration/Projectile")]
        [RichTextTooltip("Initial velocity of the projectile. Increasing this value will make the curve reach further.")]
        private float _velocity = 16f;
        [FoldoutGroup("Raycast Configuration")]
        [SerializeField, VerticalGroup("Raycast Configuration/Projectile")]
        [RichTextTooltip("Gravity of the projectile in the reference frame.")]
        private float _acceleration = 9.8f;
        [FoldoutGroup("Raycast Configuration")]
        [SerializeField, VerticalGroup("Raycast Configuration/Projectile")]
        [RichTextTooltip("Additional height below ground level that the projectile will continue to. Increasing this value will make the end point drop lower in height.")]
        private float _additionalGroundHeight = 0.1f;
        [FoldoutGroup("Raycast Configuration")]
        [SerializeField, VerticalGroup("Raycast Configuration/Projectile")]
        [RichTextTooltip("Additional flight time after the projectile lands at the adjusted ground level. Increasing this value will make the end point drop lower in height.")]
        private float _additionalFlightTime = 0.5f;
        
        [FoldoutGroup("Raycast Configuration")]
        [SerializeField, VerticalGroup("Raycast Configuration/Bezier")]
        [RichTextTooltip("Increase this value distance to make the end of the curve further from the start point.")]
        private float _endPointDistance = 30f;
        [FoldoutGroup("Raycast Configuration")]
        [SerializeField, VerticalGroup("Raycast Configuration/Bezier")]
        [RichTextTooltip("Decrease this value to make the end of the curve drop lower relative to the start point.")]
        private float _endPointHeight = -10f;
        [FoldoutGroup("Raycast Configuration")]
        [SerializeField, VerticalGroup("Raycast Configuration/Bezier")]
        [RichTextTooltip("Increase this value to make the peak of the curve further from the start point.")]
        private float _controlPointDistance = 10f;
        [FoldoutGroup("Raycast Configuration")]
        [SerializeField, VerticalGroup("Raycast Configuration/Bezier")]
        [RichTextTooltip("Increase this value to make the peak of the curve higher relative to the start point.")]
        private float _controlPointHeight = 5f;
        
        
        [SerializeField, FoldoutGroup("Raycast Collision")]
        [RichTextTooltip("The type of hit detection to use for the ray cast.\n" +
                         "<mth>Raycast:</mth> Uses <cls>Physics</cls> Ray cast to detect collisions.\n" +
                         "<mth>SphereCast:</mth> Uses <cls>Physics</cls> Sphere Cast to detect collisions.\n" +
                         "<mth>ConeCast:</mth> Uses cone casting to detect collisions.")]
        private HitDetectionModeType _hitDetectionType = HitDetectionModeType.Raycast;
        [SerializeField, FoldoutGroup("Raycast Collision")]
        [Range(0.01f, 0.25f)]
        [RichTextTooltip("The radius used for sphere casting.")]
        private float _sphereCastRadius = 0.1f;
        [SerializeField, FoldoutGroup("Raycast Collision")]
        [Range(0f, 180f)]
        [RichTextTooltip("The angle in degrees of the cone used for cone casting. Will use regular ray casting if set to 0.")]
        private float _coneCastAngle = 6f;
        [SerializeField, FoldoutGroup("Raycast Collision")]
        [RichTextTooltip("The layer mask used for limiting ray cast targets.")]
        private LayerMask _raycastMask = -1;
        [SerializeField, FoldoutGroup("Raycast Collision")]
        [RichTextTooltip("The type of interaction with trigger colliders via ray cast.")]
        private QueryTriggerInteraction _raycastTriggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField, FoldoutGroup("Raycast Collision")]
        [RichTextTooltip("Whether ray cast should include or ignore hits on trigger colliders that are snap volume colliders, even if the ray cast is set to ignore triggers.\n" +
                         "If you are not using gaze assistance or XR Interactable Snap Volume components, you should set this property to <itf>QuerySnapVolumeInteraction</itf>.<mth>Ignore</mth> to avoid the performance cost.")]
        private QuerySnapVolumeInteraction _raycastSnapVolumeInteraction = QuerySnapVolumeInteraction.Collide;
        [SerializeField, FoldoutGroup("Raycast Collision")]
        [RichTextTooltip("Whether Unity considers only the closest Interactable as a valid target for interaction.")]
        private bool _hitClosestOnly;
        
        [SerializeField, FoldoutGroup("Input/Raycast", header: "Raycast")] 
        [RichTextTooltip("Input to use for pressing UI elements.\nFunctions like a mouse button when pointing over UI.")]
        private ButtonInputProvider _uiPressInput;
        [SerializeField, FoldoutGroup("Input/Raycast")]
        [RichTextTooltip("Input to use for scrolling UI elements.\nFunctions like a mouse scroll wheel when pointing over UI.")]
        private Axis2DInputProvider _uiScrollInput;
        [SerializeField, FoldoutGroup("Input/Raycast")]
        [RichTextTooltip("Input to use for translating the attach point closer or further away from the interactor.\nThis effectively moves the selected grab interactable along the ray.")]
        private Axis2DInputProvider _translateManipulationInput;
        [SerializeField, FoldoutGroup("Input/Raycast")]
        [RichTextTooltip("Input to use for rotating the attach point over time.\nThis effectively rotates the selected grab interactable while the input is pushed in either direction.")]
        private Axis2DInputProvider _rotateManipulationInput;
        [SerializeField, FoldoutGroup("Input/Raycast")]
        [RichTextTooltip("Input to use for rotating the attach point to match the direction of the input.\nThis effectively rotates the selected grab interactable or teleport target to match the direction of the input.")]
        private Axis2DInputProvider _directionalManipulationInput;
        [SerializeField, FoldoutGroup("Input/Raycast")]
        [RichTextTooltip("The input to use for toggling between Attach Transform manipulation modes to either scale or translate/rotate.")]
        private ButtonInputProvider _scaleToggleInput;
        [SerializeField, FoldoutGroup("Input/Raycast")]
        [RichTextTooltip("The input to use for providing a scale value to grab transformers for scaling over time.\nThis effectively scales the selected grab interactable while the input is pushed in either direction.")]
        private Axis2DInputProvider _scaleOverTimeInput;
        [SerializeField, FoldoutGroup("Input/Raycast")]
        [RichTextTooltip("The input to use for providing a scale value to grab transformers for scaling based on a distance delta from last frame.\nThis input is typically used for scaling with a pinch gesture on mobile AR.")]
        private Axis1DInputProvider _scaleDistanceDeltaInput;
        
        [SerializeField, FoldoutGroup("UI")]
        [RichTextTooltip("Enable to affect Unity UI GameObjects in a way that is similar to a mouse pointer.\nRequires the XR UI Input Module on the Event System.")]
        private bool _enableUIInteraction = true;
        [SerializeField, FoldoutGroup("UI")]
        [RichTextTooltip("Enabling this option will block UI interaction when selecting interactables.")]
        private bool _blockUIOnInteractableSelection = true;
        
        [SerializeField, FoldoutGroup("AR")]
        [RichTextTooltip("Whether this interactor is able to raycast against AR environment trackables.")]
        private bool _enableARRaycasting;
        [SerializeField, FoldoutGroup("AR")]
        [RichTextTooltip("Whether AR raycast hits will be occluded by 3D objects.")]
        private bool _occludeARHitsWith3DObjects;
        [SerializeField, FoldoutGroup("AR")]
        [RichTextTooltip("Whether AR raycast hits will be occluded by 2D world space objects such as UI.")]
        private bool _occludeARHitsWith2DObjects;        
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the type of ray cast.
        /// </summary>
        public LineModeType LineType
        {
            get => _lineType;
            set => _lineType = value;
        }
        
        /// <summary>
        /// Blend the line sample points Unity uses for ray casting with the current pose of the controller.
        /// Use this to make the line visual stay connected with the controller instead of lagging behind.
        /// </summary>
        /// <remarks>
        /// When the controller is configured to sample tracking input directly before rendering to reduce
        /// input latency, the controller may be in a new position or rotation relative to the starting point
        /// of the sample curve used for ray casting.
        /// <br/>
        /// A value of <see langword="false"/> will make the line visual stay at a fixed reference frame rather than bending
        /// or curving towards the end of the ray cast line.
        /// </remarks>
        public bool BlendVisualLinePoints
        {
            get => _blendVisualLinePoints;
            set => _blendVisualLinePoints = value;
        }
        
        /// <summary>
        /// Gets or sets the max distance of ray cast when the line type is a straight line.
        /// Increasing this value will make the line reach further.
        /// </summary>
        /// <seealso cref="LineModeType.StraightLine"/>
        public float MaxRaycastDistance
        {
            get => _maxRaycastDistance;
            set => _maxRaycastDistance = value;
        }
        
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
        
        /// <summary>
        /// The reference frame of the curve to define the ground plane and up.
        /// If not set at startup it will try to find the <see cref="XROrigin.Origin"/> GameObject,
        /// and if that does not exist it will use global up and origin by default.
        /// </summary>
        /// <seealso cref="LineModeType.ProjectileCurve"/>
        /// <seealso cref="LineModeType.BezierCurve"/>
        public Transform ReferenceFrame
        {
            get => _referenceFrame;
            set
            {
                _referenceFrame = value;
                _hasReferenceFrame = _referenceFrame != null;
            }
        }
        
        /// <summary>
        /// Initial velocity of the projectile. Increasing this value will make the curve reach further.
        /// </summary>
        /// <seealso cref="LineModeType.ProjectileCurve"/>
        public float Velocity
        {
            get => _velocity;
            set => _velocity = value;
        }
        
        /// <summary>
        /// Gravity of the projectile in the reference frame.
        /// </summary>
        /// <seealso cref="LineModeType.ProjectileCurve"/>
        public float Acceleration
        {
            get => _acceleration;
            set => _acceleration = value;
        }
        
        /// <summary>
        /// Additional height below ground level that the projectile will continue to.
        /// Increasing this value will make the end point drop lower in height.
        /// </summary>
        /// <seealso cref="LineModeType.ProjectileCurve"/>
        public float AdditionalGroundHeight
        {
            get => _additionalGroundHeight;
            set => _additionalGroundHeight = value;
        }
        
        /// <summary>
        /// Additional flight time after the projectile lands at the adjusted ground level.
        /// Increasing this value will make the end point drop lower in height.
        /// </summary>
        /// <seealso cref="LineModeType.ProjectileCurve"/>
        public float AdditionalFlightTime
        {
            get => _additionalFlightTime;
            set => _additionalFlightTime = value;
        }
        
        /// <summary>
        /// Increase this value distance to make the end of the curve further from the start point.
        /// </summary>
        /// <seealso cref="LineModeType.BezierCurve"/>
        public float EndPointDistance
        {
            get => _endPointDistance;
            set => _endPointDistance = value;
        }
        
        /// <summary>
        /// Decrease this value to make the end of the curve drop lower relative to the start point.
        /// </summary>
        /// <seealso cref="LineModeType.BezierCurve"/>
        public float EndPointHeight
        {
            get => _endPointHeight;
            set => _endPointHeight = value;
        }
        
        /// <summary>
        /// Increase this value to make the peak of the curve further from the start point.
        /// </summary>
        /// <seealso cref="LineModeType.BezierCurve"/>
        public float ControlPointDistance
        {
            get => _controlPointDistance;
            set => _controlPointDistance = value;
        }
        
        /// <summary>
        /// Increase this value to make the peak of the curve higher relative to the start point.
        /// </summary>
        /// <seealso cref="LineModeType.BezierCurve"/>
        public float ControlPointHeight
        {
            get => _controlPointHeight;
            set => _controlPointHeight = value;
        }
        
        /// <summary>
        /// The number of sample points Unity uses to approximate curved paths.
        /// Larger values produce a better quality approximate at the cost of reduced performance
        /// due to the number of ray casts.
        /// </summary>
        /// <remarks>
        /// A value of <i>n</i> will result in <i>n - 1</i> line segments for ray cast.
        /// This property is not used when using <see cref="LineModeType.StraightLine"/> since the value would always be 2.
        /// </remarks>
        /// <seealso cref="LineModeType.ProjectileCurve"/>
        /// <seealso cref="LineModeType.BezierCurve"/>
        public int SampleFrequency
        {
            get => _sampleFrequency;
            set => _sampleFrequency = SanitizeSampleFrequency(value);
        }
        
        /// <summary>
        /// Gets or sets which type of hit detection to use for the ray cast.
        /// </summary>
        public HitDetectionModeType HitDetectionType
        {
            get => _hitDetectionType;
            set => _hitDetectionType = value;
        }

        
        /// <summary>
        /// Gets or sets radius used for sphere casting.
        /// </summary>
        /// <seealso cref="HitDetectionModeType.SphereCast"/>
        /// <seealso cref="HitDetectionType"/>
        public float SphereCastRadius
        {
            get => _sphereCastRadius;
            set => _sphereCastRadius = value;
        }

        /// <summary>
        /// Gets or sets the angle in degrees of the cone used for cone casting. Will use regular ray casting if set to 0.
        /// </summary>
        public float ConeCastAngle
        {
            get => _coneCastAngle;
            set => _coneCastAngle = value;
        }
        
        /// <summary>
        /// Gets or sets layer mask used for limiting ray cast targets.
        /// </summary>
        public LayerMask RaycastMask
        {
            get => _raycastMask;
            set => _raycastMask = value;
        }
        
        /// <summary>
        /// Gets or sets type of interaction with trigger colliders via ray cast.
        /// </summary>
        public QueryTriggerInteraction RaycastTriggerInteraction
        {
            get => _raycastTriggerInteraction;
            set => _raycastTriggerInteraction = value;
        }
        
        /// <summary>
        /// Whether ray cast should include or ignore hits on trigger colliders that are snap volume colliders,
        /// even if the ray cast is set to ignore triggers.
        /// If you are not using gaze assistance or XR Interactable Snap Volume components, you should set this property
        /// to <see cref="QuerySnapVolumeInteraction.Ignore"/> to avoid the performance cost.
        /// </summary>
        /// <remarks>
        /// When set to <see cref="QuerySnapVolumeInteraction.Collide"/> when <see cref="RaycastTriggerInteraction"/> is set to ignore trigger colliders
        /// (when set to <see cref="QueryTriggerInteraction.Ignore"/> or when set to <see cref="QueryTriggerInteraction.UseGlobal"/>
        /// while <see cref="Physics.queriesHitTriggers"/> is <see langword="false"/>),
        /// the ray cast query will be modified to include trigger colliders, but then this behavior will ignore any trigger collider
        /// hits that are not snap volumes.
        /// <br />
        /// When set to <see cref="QuerySnapVolumeInteraction.Ignore"/> when <see cref="RaycastTriggerInteraction"/> is set to hit trigger colliders
        /// (when set to <see cref="QueryTriggerInteraction.Collide"/> or when set to <see cref="QueryTriggerInteraction.UseGlobal"/>
        /// while <see cref="Physics.queriesHitTriggers"/> is <see langword="true"/>),
        /// this behavior will ignore any trigger collider hits that are snap volumes.
        /// </remarks>
        /// <seealso cref="RaycastTriggerInteraction"/>
        /// <seealso cref="VXRInteractableSnapVolume.snapCollider"/>
        public QuerySnapVolumeInteraction RaycastSnapVolumeInteraction
        {
            get => _raycastSnapVolumeInteraction;
            set => _raycastSnapVolumeInteraction = value;
        }
        
        /// <summary>
        /// Whether Unity considers only the closest Interactable as a valid target for interaction.
        /// </summary>
        /// <remarks>
        /// Enable this to make only the closest Interactable receive hover events.
        /// Otherwise, all hit Interactables will be considered valid and this Interactor will multi-hover.
        /// </remarks>
        /// <seealso cref="GetValidTargets"/>
        public bool HitClosestOnly
        {
            get => _hitClosestOnly;
            set => _hitClosestOnly = value;
        }
        
        /// <summary>
        /// Whether this Interactor will automatically select an Interactable after hovering over it for a period of time.
        /// </summary>
        /// <seealso cref="HoverTimeToSelect"/>
        public bool HoverToSelect
        {
            get => _hoverToSelect;
            set => _hoverToSelect = value;
        }
        
        /// <summary>
        /// Number of seconds for which this Interactor must hover over an Interactable to select it if Hover To Select is enabled.
        /// </summary>
        /// <seealso cref="HoverToSelect"/>
        public float HoverTimeToSelect
        {
            get => _hoverTimeToSelect;
            set => _hoverTimeToSelect = value;
        }

        
        /// <summary>
        /// Whether this Interactor will automatically deselect an Interactable after selecting it via hover for a period of time.
        /// </summary>
        /// <remarks>
        /// This only applies when an interactable is selected due to <see cref="HoverToSelect"/>.
        /// </remarks>
        /// <seealso cref="TimeToAutoDeselect"/>
        public bool AutoDeselect
        {
            get => _autoDeselect;
            set => _autoDeselect = value;
        }

        
        /// <summary>
        /// Number of seconds for which this Interactor will keep an Interactable selected before automatically deselecting it.
        /// </summary>
        /// <remarks>
        /// This only applies when an interactable is selected due to <see cref="HoverToSelect"/>.
        /// </remarks>
        /// <seealso cref="HoverToSelect"/>
        public float TimeToAutoDeselect
        {
            get => _timeToAutoDeselect;
            set => _timeToAutoDeselect = value;
        }
        
        /// <summary>
        /// Enable to affect Unity UI GameObjects in a way that is similar to a mouse pointer.
        /// Requires the XR UI Input Module on the Event System.
        /// </summary>
        public bool EnableUIInteraction
        {
            get => _enableUIInteraction;
            set
            {
                if (_enableUIInteraction != value)
                {
                    _enableUIInteraction = value;
                    _registeredUIInteractorCache?.RegisterOrUnregisterXRUIInputModule(_enableUIInteraction);
                }
            }
        }

        /// <summary>
        /// Enabling this option will block UI interaction when selecting interactables.
        /// </summary>
        public bool BlockUIOnInteractableSelection
        {
            get => _blockUIOnInteractableSelection;
            set => _blockUIOnInteractableSelection = value;
        }
        
        /// <summary>
        /// Allows the user to move the Attach Transform using the thumbstick.
        /// </summary>
        /// <seealso cref="RotateSpeed"/>
        /// <seealso cref="TranslateSpeed"/>
        /// <seealso cref="RotateReferenceFrame"/>
        /// <seealso cref="RotateMode"/>
        public bool ManipulateAttachTransform
        {
            get => _manipulateAttachTransform;
            set => _manipulateAttachTransform = value;
        }
        
        /// <summary>
        /// Force grab moves the object to your hand rather than interacting with it at a distance.
        /// </summary>
        public bool UseForceGrab
        {
            get => _useForceGrab;
            set => _useForceGrab = value;
        }
        
        /// <summary>
        /// Speed that the Attach Transform is rotated when <see cref="RotateMode"/> is set to <see cref="RotateModeType.RotateOverTime"/>.
        /// </summary>
        /// <seealso cref="ManipulateAttachTransform"/>
        /// <seealso cref="TranslateSpeed"/>
        /// <seealso cref="RotateMode"/>
        public float RotateSpeed
        {
            get => _rotateSpeed;
            set => _rotateSpeed = value;
        }
        
        /// <summary>
        /// Speed that the Attach Transform is translated along the ray.
        /// </summary>
        /// <seealso cref="ManipulateAttachTransform"/>
        /// <seealso cref="RotateSpeed"/>
        public float TranslateSpeed
        {
            get => _translateSpeed;
            set => _translateSpeed = value;
        }
        
        /// <summary>
        /// The optional reference frame to define the up axis when rotating the Attach Transform.
        /// When not set, rotates about the local up axis of the Attach Transform.
        /// </summary>
        /// <seealso cref="ManipulateAttachTransform"/>
        /// <seealso cref="RotateAttachTransform(Transform,float)"/>
        /// <seealso cref="RotateAttachTransform(Transform,Vector2,Quaternion)"/>
        public Transform RotateReferenceFrame
        {
            get => _rotateReferenceFrame;
            set => _rotateReferenceFrame = value;
        }
        
        /// <summary>
        /// How the Attach Transform rotation manipulation is controlled.
        /// </summary>
        /// <seealso cref="ManipulateAttachTransform"/>
        /// <seealso cref="RotateModeType"/>
        public RotateModeType RotateMode
        {
            get => _rotateMode;
            set => _rotateMode = value;
        }
        /// <inheritdoc />
        public ScaleMode ScaleMode
        {
            get => _scaleMode;
            set => _scaleMode = value;
        }
        
        /// <summary>
        /// The launch angle of the Projectile Curve.
        /// More specifically, this is the signed angle in degrees between the original attach forward
        /// direction and the plane of the reference frame, with positive angles when pointing upward.
        /// </summary>
        public float Angle
        {
            get
            {
                GetLineOriginAndDirection(out _, out var lineDirection);
                return GetProjectileAngle(lineDirection);
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

        /// <inheritdoc />
        public Vector3 RayEndPoint { get; private set; }

        /// <inheritdoc />
        public Transform RayEndTransform { get; private set; }

        /// <inheritdoc />
        public float ScaleValue { get; protected set; }
        
        /// <summary>
        /// The starting position and direction of any ray casts.
        /// Safe version of <see cref="RayOriginTransform"/>, falls back to this Transform if not set.
        /// </summary>
        private Transform EffectiveRayOrigin => _hasRayOriginTransform ? _rayOriginTransform : transform;

        private Vector3 ReferenceUp => _hasReferenceFrame ? _referenceFrame.up : Vector3.up;

        private Vector3 ReferencePosition => _hasReferenceFrame ? _referenceFrame.position : Vector3.zero;
        
        /// <summary>
        /// Gets or sets whether this interactor is able to raycast against AR environment trackables.
        /// </summary>
        public bool EnableARRaycasting
        {
            get => _enableARRaycasting;
            set => _enableARRaycasting = value;
        }

        /// <summary>
        /// Gets or sets whether AR raycast hits will be occluded by 3D objects.
        /// </summary>
        public bool OccludeARHitsWith3DObjects
        {
            get => _occludeARHitsWith3DObjects;
            set => _occludeARHitsWith3DObjects = value;
        }

        /// <summary>
        /// Gets or sets whether AR raycast hits will be occluded by 2D world space objects such as UI.
        /// </summary>
        public bool OccludeARHitsWith2DObjects
        {
            get => _occludeARHitsWith2DObjects;
            set => _occludeARHitsWith2DObjects = value;
        }

        /// <summary>
        /// Input to use for pressing UI elements.
        /// Functions like a mouse button when pointing over UI.
        /// </summary>
        public ButtonInputProvider UIPressInput
        {
            get => _uiPressInput;
            set => _uiPressInput = value;
        }

        /// <summary>
        /// Input to use for scrolling UI elements.
        /// Functions like a mouse scroll wheel when pointing over UI.
        /// </summary>
        public Axis2DInputProvider UIScrollInput
        {
            get => _uiScrollInput;
            set => _uiScrollInput = value;
        }

        /// <summary>
        /// Input to use for translating the attach point closer or further away from the interactor.
        /// This effectively moves the selected grab interactable along the ray.
        /// </summary>
        /// <remarks>
        /// Uses the y-axis as the translation input.
        /// </remarks>
        public Axis2DInputProvider TranslateManipulationInput
        {
            get => _translateManipulationInput;
            set => _translateManipulationInput = value;
        }

        /// <summary>
        /// Input to use for rotating the attach point over time.
        /// This effectively rotates the selected grab interactable while the input is pushed in either direction.
        /// </summary>
        /// <remarks>
        /// Uses the x-axis as the rotation input.
        /// </remarks>
        /// <seealso cref="RotateModeType.RotateOverTime"/>
        public Axis2DInputProvider RotateManipulationInput
        {
            get => _rotateManipulationInput;
            set => _rotateManipulationInput = value;
        }
        
        /// <summary>
        /// Input to use for rotating the attach point to match the direction of the input.
        /// This effectively rotates the selected grab interactable or teleport target to match the direction of the input.
        /// </summary>
        /// <remarks>
        /// The direction angle should be computed as the arctangent function of x/y.
        /// </remarks>
        /// <seealso cref="RotateModeType.MatchDirection"/>
        public Axis2DInputProvider DirectionalManipulationInput
        {
            get => _directionalManipulationInput;
            set => _rotateManipulationInput = value;
        }

        /// <summary>
        /// The input to use for toggling between Attach Transform manipulation modes to either scale or translate/rotate.
        /// </summary>
        public ButtonInputProvider ScaleToggleInput
        {
            get => _scaleToggleInput;
            set => _scaleToggleInput = value;
        }
        
        /// <summary>
        /// The input to use for providing a scale value to grab transformers for scaling over time.
        /// This effectively scales the selected grab interactable while the input is pushed in either direction.
        /// </summary>
        /// <remarks>
        /// Uses the y-axis as the scale input.
        /// </remarks>
        /// <seealso cref="ScaleMode.ScaleOverTime"/>
        /// <seealso cref="XRGeneralGrabTransformer.allowOneHandedScaling"/>
        public Axis2DInputProvider ScaleOverTimeInput
        {
            get => _scaleOverTimeInput;
            set => _scaleOverTimeInput = value;
        }

        /// <summary>
        /// The input to use for providing a scale value to grab transformers for scaling based on a distance delta from last frame.
        /// This input is typically used for scaling with a pinch gesture on mobile AR.
        /// </summary>
        /// <seealso cref="ScaleMode.DistanceDelta"/>
        public Axis1DInputProvider ScaleDistanceDeltaInput
        {
            get => _scaleDistanceDeltaInput;
            set => _scaleDistanceDeltaInput = value;
        }
        
#if AR_FOUNDATION_PRESENT
        //Once other trackables are supported, this will become a serialized field.
        TrackableType m_TrackableType = TrackableType.PlaneWithinPolygon;
        /// <summary>
        /// The <see cref="ARTrackable"/> types that will taken into consideration with the performed <see cref="ARRaycast"/>. 
        /// </summary>
        public TrackableType trackableType
        {
            get => m_TrackableType;
            set => m_TrackableType = value;
        }
#endif
        #endregion

        #region Fields
        /// <summary>
        /// Reusable list to hold the current sample points.
        /// </summary>
        private static List<SamplePoint> s_ScratchSamplePoints;

        /// <summary>
        /// Reusable array to hold the current control points for a quadratic Bezier curve.
        /// </summary>
        private static readonly float3[] s_ScratchControlPoints = new float3[3];
        
        private bool _hasRayOriginTransform;
        private bool _hasReferenceFrame;

        private bool _scaleInputActive;
        
        private readonly List<IXRInteractable> _validTargets = new List<IXRInteractable>();

        private float _lastTimeHoveredObjectChanged;
        private bool _passedHoverTimeToSelect;
        private float _lastTimeAutoSelected;
        private bool _passedTimeToAutoDeselect;

        private GameObject _lastUIObject;
        private float _lastTimeHoveredUIChanged;
        private bool _hoverUISelectActive;
        private bool _blockUIAutoDeselect;

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
        /// <seealso cref="LineModeType.BezierCurve"/>
        /// <seealso cref="EndPointDistance"/>
        /// <seealso cref="EndPointHeight"/>
        /// <seealso cref="ControlPointDistance"/>
        /// <seealso cref="ControlPointHeight"/>
        private readonly float3[] _controlPoints = new float3[3];

        /// <summary>
        /// Control points to calculate the equivalent quadratic Bezier curve to the endpoint where a hit occurred.
        /// </summary>
        private readonly float3[] _hitChordControlPoints = new float3[3];

        private PhysicsScene _localPhysicsScene;

        private RegisteredUIInteractorCache _registeredUIInteractorCache;

        // Cached raycast data
        private bool _raycastHitOccurred;
        private RaycastHit _raycastHit;
        private RaycastResult _uiRaycastHit;
        private bool _isUIHitClosest;
        private IXRInteractable _raycastInteractable;
#if AR_FOUNDATION_PRESENT 
        private int m_ARRaycastHitEndpointIndex;
        private readonly List<ARRaycastHit> m_ARRaycastHits = new List<ARRaycastHit>();
        private int m_ARRaycastHitsCount;
        private ARRaycastManager m_ARRaycastManager;
        private ARRaycastHit m_ARRaycastHit;
        private bool m_IsARHitClosest;
#endif
        #endregion

        #region Events
        public event Action<UIHoverEventArgs> UiHoverEntered;
        public event Action<UIHoverEventArgs> UiHoverExited;
        #endregion

#if AR_FOUNDATION_PRESENT
        /// <summary>
        /// The closest index of the sample endpoint where a 3D, UI or AR hit occurred.
        /// </summary>
        private int ClosestAnyHitIndex  
        {
            get
            {
                if (m_RaycastHitEndpointIndex > 0 && m_UIRaycastHitEndpointIndex > 0 && m_ARRaycastHitEndpointIndex > 0)
                {
                    return Math.Min(m_RaycastHitEndpointIndex, Math.Min(m_UIRaycastHitEndpointIndex, m_ARRaycastHitEndpointIndex));
                }
                else if (m_RaycastHitEndpointIndex > 0 && m_UIRaycastHitEndpointIndex > 0)
                {
                    return Mathf.Min(m_RaycastHitEndpointIndex, m_UIRaycastHitEndpointIndex);
                }
                else if (m_RaycastHitEndpointIndex > 0 && m_ARRaycastHitEndpointIndex > 0)
                {
                    return Mathf.Min(m_RaycastHitEndpointIndex, m_ARRaycastHitEndpointIndex);
                }
                else if (m_UIRaycastHitEndpointIndex > 0 && m_ARRaycastHitEndpointIndex > 0)
                {
                    return Mathf.Min(m_UIRaycastHitEndpointIndex, m_ARRaycastHitEndpointIndex);
                }
                else if (m_RaycastHitEndpointIndex > 0)
                {
                    return m_RaycastHitEndpointIndex;
                }
                else if (m_UIRaycastHitEndpointIndex > 0)
                {
                    return m_UIRaycastHitEndpointIndex;
                }
                else if (m_ARRaycastHitEndpointIndex > 0)
                {
                    return m_ARRaycastHitEndpointIndex;
                }
               
                return 0;
            }
        }
#else
        /// <summary>
        /// The closest index of the sample endpoint where a 3D or UI hit occurred.
        /// </summary>
        private int ClosestAnyHitIndex => (_raycastHitEndpointIndex > 0 && _uiRaycastHitEndpointIndex > 0) // Are both valid?
            ? Mathf.Min(_raycastHitEndpointIndex, _uiRaycastHitEndpointIndex) // When both are valid, return the closer one
            : (_raycastHitEndpointIndex > 0 ? _raycastHitEndpointIndex : _uiRaycastHitEndpointIndex); // Otherwise return the valid one
#endif


        protected void OnValidate()
        {
            _hasRayOriginTransform = _rayOriginTransform != null;
            _hasReferenceFrame = _referenceFrame != null;
            _sampleFrequency = SanitizeSampleFrequency(_sampleFrequency);
            _registeredUIInteractorCache?.RegisterOrUnregisterXRUIInputModule(_enableUIInteraction);
        }

        protected override void Awake()
        {
            base.Awake();

            // ButtonReaders.Add(_uiPressInput);
            // ValueReaders.Add(_uiScrollInput);
            // ValueReaders.Add(_translateManipulationInput);
            // ValueReaders.Add(_rotateManipulationInput);
            // ValueReaders.Add(_directionalManipulationInput);
            // ButtonReaders.Add(_scaleToggleInput);
            // ValueReaders.Add(_scaleOverTimeInput);
            // ValueReaders.Add(_scaleDistanceDeltaInput);

            _localPhysicsScene = gameObject.scene.GetPhysicsScene();
            _registeredUIInteractorCache = new RegisteredUIInteractorCache(this);

            CreateSamplePointsListsIfNecessary();

            FindReferenceFrame();
            CreateRayOrigin();

#if AR_FOUNDATION_PRESENT
            FindCreateARRaycastManager();
#endif
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _uiPressInput.BindToUpdateEvent(UpdateProvider);
            _scaleToggleInput.BindToUpdateEvent(UpdateProvider);
            _uiScrollInput.BindToUpdateEvent(UpdateProvider);
            _translateManipulationInput.BindToUpdateEvent(UpdateProvider);
            _rotateManipulationInput.BindToUpdateEvent(UpdateProvider);
            _directionalManipulationInput.BindToUpdateEvent(UpdateProvider);
            _scaleOverTimeInput.BindToUpdateEvent(UpdateProvider);
            _scaleDistanceDeltaInput.BindToUpdateEvent(UpdateProvider);
            if (_enableUIInteraction)
                _registeredUIInteractorCache?.RegisterWithXRUIInputModule();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _uiPressInput.UnbindUpdateEvent();
            _scaleToggleInput.UnbindUpdateEvent();
            _uiScrollInput.UnbindUpdateEvent();
            _translateManipulationInput.UnbindUpdateEvent();
            _rotateManipulationInput.UnbindUpdateEvent();
            _directionalManipulationInput.UnbindUpdateEvent();
            _scaleOverTimeInput.UnbindUpdateEvent();
            _scaleDistanceDeltaInput.UnbindUpdateEvent();

            // Clear lines
            _samplePoints?.Clear();

            if (_enableUIInteraction)
                _registeredUIInteractorCache?.UnregisterFromXRUIInputModule();
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (_lineType == LineModeType.StraightLine)
            {
                var transformData = _rayOriginTransform != null ? _rayOriginTransform : transform;
                var gizmoStart = transformData.position;
                var gizmoEnd = gizmoStart + (transformData.forward * _maxRaycastDistance);
                Gizmos.color = new Color(58 / 255f, 122 / 255f, 248 / 255f, 237 / 255f);

                switch (_hitDetectionType)
                {
                    case HitDetectionModeType.Raycast:
                        // Draw the raycast line
                        Gizmos.DrawLine(gizmoStart, gizmoEnd);
                        break;

                    case HitDetectionModeType.SphereCast:
                    {
                        var gizmoUp = transformData.up * _sphereCastRadius;
                        var gizmoSide = transformData.right * _sphereCastRadius;
                        Gizmos.DrawWireSphere(gizmoStart, _sphereCastRadius);
                        Gizmos.DrawLine(gizmoStart + gizmoSide, gizmoEnd + gizmoSide);
                        Gizmos.DrawLine(gizmoStart - gizmoSide, gizmoEnd - gizmoSide);
                        Gizmos.DrawLine(gizmoStart + gizmoUp, gizmoEnd + gizmoUp);
                        Gizmos.DrawLine(gizmoStart - gizmoUp, gizmoEnd - gizmoUp);
                        Gizmos.DrawWireSphere(gizmoEnd, _sphereCastRadius);
                        break;
                    }

                    case HitDetectionModeType.ConeCast:
                    {
                        var coneRadius = Mathf.Tan(_coneCastAngle * Mathf.Deg2Rad * 0.5f) * _maxRaycastDistance;
                        var gizmoUp = transformData.up * coneRadius;
                        var gizmoSide = transformData.right * coneRadius;
                        Gizmos.DrawLine(gizmoStart, gizmoEnd);
                        Gizmos.DrawLine(gizmoStart, gizmoEnd + gizmoSide);
                        Gizmos.DrawLine(gizmoStart, gizmoEnd - gizmoSide);
                        Gizmos.DrawLine(gizmoStart, gizmoEnd + gizmoUp);
                        Gizmos.DrawLine(gizmoStart, gizmoEnd - gizmoUp);
                        Gizmos.DrawWireSphere(gizmoEnd, coneRadius);
                        break;
                    }
                }
            }

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

            if (TryGetCurrentUIRaycastResult(out var uiRaycastResult))
            {
                // Draw the normal of the surface at the hit point
                Gizmos.color = new Color(58 / 255f, 122 / 255f, 248 / 255f, 237 / 255f);
                const float length = 0.075f;
                Gizmos.DrawLine(uiRaycastResult.worldPosition, uiRaycastResult.worldPosition + uiRaycastResult.worldNormal.normalized * length);
            }

#if AR_FOUNDATION_PRESENT 
            if (TryGetCurrentARRaycastHit(out var arRaycastHit))
            {
                // Draw the normal of the surface at the hit point
                Gizmos.color = new Color(58 / 255f, 122 / 255f, 248 / 255f, 237 / 255f);
                const float length = 0.075f;
                Gizmos.DrawLine(arRaycastHit.pose.position, arRaycastHit.pose.position + arRaycastHit.pose.up * length);
            }
#endif
            var hitIndex = ClosestAnyHitIndex;

            // Draw sample points where the ray cast line segments took place
            for (var i = 0; i < _samplePoints.Count; ++i)
            {
                var samplePoint = _samplePoints[i];

                // Change the color of the points after the segment where a hit happened
                var radius = _hitDetectionType == HitDetectionModeType.SphereCast ? _sphereCastRadius : 0.025f;
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
        }

        private static void DrawQuadraticBezierGizmo(Vector3 p0, Vector3 p1, Vector3 p2)
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

        /// <summary>
        /// Attempts to locate a reference frame for the curve (if necessary).
        /// </summary>
        /// <seealso cref="ReferenceFrame"/>
        private void FindReferenceFrame()
        {
            _hasReferenceFrame = _referenceFrame != null;
            if (_hasReferenceFrame)
                return;

            if (ComponentLocatorUtility<VXROrigin>.TryFindComponent(out var xrOrigin))
            {
                var origin = xrOrigin.Origin;
                if (origin != null)
                {
                    _referenceFrame = origin.transform;
                    _hasReferenceFrame = true;
                }
                else
                {
                    Debug.Log($"Reference frame of the curve not set and {nameof(XROrigin)}.{nameof(XROrigin.Origin)} is not set, using global up as default.", this);
                }
            }
            else
            {
                Debug.Log($"Reference frame of the curve not set and {nameof(XROrigin)} is not found, using global up as default.", this);
            }
        }

        private void CreateRayOrigin()
        {
            _hasRayOriginTransform = _rayOriginTransform != null;
            if (_hasRayOriginTransform)
                return;

            _rayOriginTransform = new GameObject($"[{gameObject.name}] Ray Origin").transform;
            _hasRayOriginTransform = true;
            _rayOriginTransform.SetParent(transform, false);

            if (AttachTransform == null)
                CreateAttachTransform();

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

        /// <summary>
        /// Use this to determine if the ray is currently hovering over a UI GameObject.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if hovering over a UI element. Otherwise, returns <see langword="false"/>.</returns>
        /// <remarks>
        /// <see cref="EnableUIInteraction"/> must be enabled, otherwise the function will always return <see langword="false"/>.
        /// </remarks>
        /// <seealso cref="UIInputModule.IsPointerOverGameObject(int)"/>
        /// <seealso cref="EventSystem.IsPointerOverGameObject(int)"/>
        public bool IsOverUIGameObject()
        {
            return _enableUIInteraction && _registeredUIInteractorCache != null && _registeredUIInteractorCache.IsOverUIGameObject();
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
        public void GetLineOriginAndDirection(out Vector3 origin, out Vector3 direction) =>
            GetLineOriginAndDirection(EffectiveRayOrigin, out origin, out direction);

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

        private static void GetLineOriginAndDirection(Transform rayOrigin, out Vector3 origin, out Vector3 direction)
        {
            origin = rayOrigin.position;
            direction = rayOrigin.forward;
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

        /// <inheritdoc />
        public bool TryGetHitInfo(out Vector3 position, out Vector3 normal, out int positionInLine, out bool isValidTarget)
        {
            position = default;
            normal = default;
            positionInLine = default;
            isValidTarget = default;

            if (!TryGetCurrentRaycast(
                out var raycastHit,
                out var raycastHitIndex,
                out var raycastResult,
                out var raycastResultIndex,
                out var isUIHitClosest))
            {
                return false;
            }

            if (raycastResult.HasValue && isUIHitClosest)
            {
                position = raycastResult.Value.worldPosition;
                normal = raycastResult.Value.worldNormal;
                positionInLine = raycastResultIndex;

                isValidTarget = raycastResult.Value.gameObject != null;
            }
            else if (raycastHit.HasValue)
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

        /// <inheritdoc />
        public virtual void UpdateUIModel(ref TrackedDeviceModel model)
        {
            if (!isActiveAndEnabled || _samplePoints == null ||
                // If selecting interactables, don't update UI model.
                (_enableUIInteraction && _blockUIOnInteractableSelection && HasSelection)
                || this.IsBlockedByInteractionWithinGroup())
            {
                model.Reset(false);
                return;
            }

            var originTransform = EffectiveRayOrigin;

            bool select;
            if (_hoverToSelect && _hoverUISelectActive)
                select = AllowSelect;
            else
                select = _uiPressInput.IsHeld;

            var scrollDelta = _uiScrollInput.CurrentValue;

            model.position = originTransform.position;
            model.orientation = originTransform.rotation;
            model.select = select;
            model.scrollDelta = scrollDelta;
            model.raycastLayerMask = _raycastMask;
            model.interactionType = UIInteractionType.Ray;

            var raycastPoints = model.raycastPoints;
            raycastPoints.Clear();

            UpdateSamplePointsIfNecessary();
            var numPoints = _samplePoints.Count;
            if (numPoints > 0)
            {
                if (raycastPoints.Capacity < numPoints)
                    raycastPoints.Capacity = numPoints;

                for (var i = 0; i < numPoints; ++i)
                    raycastPoints.Add(_samplePoints[i].Position);
            }
        }

        /// <inheritdoc />
        public bool TryGetUIModel(out TrackedDeviceModel model)
        {
            if (_registeredUIInteractorCache == null)
            {
                model = TrackedDeviceModel.invalid;
                return false;
            }
            return _registeredUIInteractorCache.TryGetUIModel(out model);
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

        /// <inheritdoc cref="TryGetCurrentUIRaycastResult(out RaycastResult, out int)"/>
        public bool TryGetCurrentUIRaycastResult(out RaycastResult raycastResult)
        {
            return TryGetCurrentUIRaycastResult(out raycastResult, out _);
        }

        /// <summary>
        /// Gets the first UI ray cast result, if any ray cast results are available.
        /// </summary>
        /// <param name="raycastResult">When this method returns, contains the UI ray cast result if available; otherwise, the default value.</param>
        /// <param name="raycastEndpointIndex">When this method returns, contains the index of the sample endpoint if a hit occurred.
        /// Otherwise, a value of <c>0</c> if no hit occurred.</param>
        /// <returns>Returns <see langword="true"/> if a hit occurred, implying the ray cast hit information is valid.
        /// Otherwise, returns <see langword="false"/>.</returns>
        public bool TryGetCurrentUIRaycastResult(out RaycastResult raycastResult, out int raycastEndpointIndex)
        {
            if (TryGetUIModel(out var model) && model.currentRaycast.isValid)
            {
                raycastResult = model.currentRaycast;
                raycastEndpointIndex = model.currentRaycastEndpointIndex;
                return true;
            }

            raycastResult = default;
            raycastEndpointIndex = default;
            return false;
        }

#if AR_FOUNDATION_PRESENT || PACKAGE_DOCS_GENERATION
        /// <inheritdoc cref="TryGetCurrentARRaycastHit(out ARRaycastHit, out int)"/>
        public bool TryGetCurrentARRaycastHit(out ARRaycastHit raycastHit)
        {
            return TryGetCurrentARRaycastHit(out raycastHit, out _);
        }

        /// <summary>
        /// Gets the first AR ray cast hit, if any ray cast hits are available.
        /// </summary>
        /// <param name="raycastHit">When this method returns, contains the ray cast hit if available; otherwise, the default value.</param>
        /// <param name="raycastEndpointIndex">When this method returns, contains the index of the sample endpoint if a hit occurred.
        /// Otherwise, a value of <c>0</c> if no hit occurred.</param>
        /// <returns>Returns <see langword="true"/> if a hit occurred, implying the ray cast hit information is valid.
        /// Otherwise, returns <see langword="false"/>.</returns>
        /// <remarks>
        /// If <see cref="occludeARHitsWith2DObjects"/> or <see cref="occludeARHitsWith3DObjects"/> are set to <see langword="true"/> and a 
        /// 2D UI or 3D object are closer, the result will be <see langword="false"/> with the default values for both the <paramref name="raycastHit"/>
        /// and <paramref name="raycastEndpointIndex"/>.
        /// </remarks>
        public bool TryGetCurrentARRaycastHit(out ARRaycastHit raycastHit, out int raycastEndpointIndex)
        {
            TryGetCurrent3DRaycastHit(out var currentRaycastHit);
            var isSelectedSameAsHit = interactablesSelected.Count > 0 && currentRaycastHit.transform != null ? interactablesSelected[0].transform.gameObject == currentRaycastHit.transform.gameObject : false;
            var occludedBy3DObject = m_OccludeARHitsWith3DObjects && m_RaycastHitEndpointIndex > 0 && !isSelectedSameAsHit && (m_RaycastHitEndpointIndex < m_ARRaycastHitEndpointIndex || (m_ARRaycastHitEndpointIndex == m_RaycastHitEndpointIndex && m_ARRaycastHits[0].distance > m_RaycastHit.distance));
            var occludedBy2DObject = m_OccludeARHitsWith2DObjects && m_UIRaycastHitEndpointIndex > 0 && m_UIRaycastHit.isValid && (m_UIRaycastHitEndpointIndex < m_ARRaycastHitEndpointIndex || (m_ARRaycastHitEndpointIndex == m_UIRaycastHitEndpointIndex && m_ARRaycastHits[0].distance > m_UIRaycastHit.distance));

            if (m_ARRaycastHitsCount > 0 && m_ARRaycastHitEndpointIndex > 0 && !occludedBy3DObject && !occludedBy2DObject)
            {
                Assert.IsTrue(m_ARRaycastHits.Count >= m_ARRaycastHitsCount);
                raycastHit = m_ARRaycastHits[0];
                raycastEndpointIndex = m_ARRaycastHitEndpointIndex;
                return true;
            }

            raycastHit = default;
            raycastEndpointIndex = default;
            return false;
        }
#endif

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
        public bool TryGetCurrentRaycast(
            out RaycastHit? raycastHit,
            out int raycastHitIndex,
            out RaycastResult? uiRaycastHit,
            out int uiRaycastHitIndex,
            out bool isUIHitClosest)
        {
            raycastHit = _raycastHit;
            raycastHitIndex = _raycastHitEndpointIndex;
            uiRaycastHit = _uiRaycastHit;
            uiRaycastHitIndex = _uiRaycastHitEndpointIndex;
            isUIHitClosest = _isUIHitClosest;

            return _raycastHitOccurred;
        }

#if AR_FOUNDATION_PRESENT || PACKAGE_DOCS_GENERATION
        /// <summary>
        /// Gets the first 3D, AR and UI ray cast hits, if any ray cast hits are available.
        /// </summary>
        /// <param name="raycastHit">When this method returns, contains the ray cast hit if available; otherwise, the default value.</param>
        /// <param name="raycastHitIndex">When this method returns, contains the index of the sample endpoint if a hit occurred.
        /// Otherwise, a value of <c>0</c> if no hit occurred.</param>
        /// <param name="uiRaycastHit">When this method returns, contains the UI ray cast result if available; otherwise, the default value.</param>
        /// <param name="uiRaycastHitIndex">When this method returns, contains the index of the sample endpoint if a hit occurred.
        /// Otherwise, a value of <c>0</c> if no hit occurred.</param>
        /// <param name="isUIHitClosest">When this method returns, contains whether the UI ray cast result was the closest hit.</param>
        /// <param name="arRaycastHit">When this method returns, contains the AR ray cast hit if available; otherwise, the default value.</param>
        /// <param name="arRaycastHitIndex">When this method returns, contains the index of the sample endpoint if a hit occurred.
        /// Otherwise, a value of <c>0</c> if no hit occurred.</param>
        /// <param name="isARHitClosest">When this method returns, contains whether the AR ray cast result was the closest hit.</param>
        /// <returns>Returns <see langword="true"/> if either hit occurred, implying the ray cast hit information is valid.
        /// Otherwise, returns <see langword="false"/>.</returns>
        public bool TryGetCurrentRaycast(
            out RaycastHit? raycastHit,
            out int raycastHitIndex,
            out RaycastResult? uiRaycastHit,
            out int uiRaycastHitIndex,
            out bool isUIHitClosest,
            out ARRaycastHit? arRaycastHit,
            out int arRaycastHitIndex,
            out bool isARHitClosest)
        {
            raycastHit = m_RaycastHit;
            raycastHitIndex = m_RaycastHitEndpointIndex;
            uiRaycastHit = m_UIRaycastHit;
            uiRaycastHitIndex = m_UIRaycastHitEndpointIndex;
            isUIHitClosest = m_IsUIHitClosest;
            arRaycastHit = m_ARRaycastHit;
            arRaycastHitIndex = m_ARRaycastHitEndpointIndex;
            isARHitClosest = m_IsARHitClosest;

            return m_RaycastHitOccurred;
        }
#endif

        /// <summary>
        /// Gets the first 3D and UI ray cast hits and caches them for further lookup, if any ray cast hits are available.
        /// </summary>
        private void CacheRaycastHit()
        {
            _raycastHit = default;
            _uiRaycastHit = default;

#if AR_FOUNDATION_PRESENT 
            m_ARRaycastHit = default;
            m_IsARHitClosest = default;

#endif            
            _isUIHitClosest = default;

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

            if (TryGetCurrentUIRaycastResult(out var raycastResultValue, out _uiRaycastHitEndpointIndex))
            {
                _uiRaycastHit = raycastResultValue;

                // Determine if the UI hit is closer than the 3D hit.
                // The ray cast segments are sourced from a polygonal chain of endpoints.
                // Within each segment, this Interactor could have hit either a 3D object or a UI object.
                // The distance is just from the segment start position, not from the origin of the whole curve.
                _isUIHitClosest = _uiRaycastHitEndpointIndex > 0 && (_uiRaycastHitEndpointIndex < hitIndex || (_uiRaycastHitEndpointIndex == hitIndex && raycastResultValue.distance <= distance));

                _raycastHitOccurred = true;
            }

#if AR_FOUNDATION_PRESENT
            if (TryGetCurrentARRaycastHit(out var arRaycastHitValue, out var arRaycastHitIndex))
            {
                m_ARRaycastHit = arRaycastHitValue;

                if (m_IsUIHitClosest)
                {
                    m_IsARHitClosest = arRaycastHitIndex > 0 && (arRaycastHitIndex < m_UIRaycastHitEndpointIndex || (arRaycastHitIndex == m_UIRaycastHitEndpointIndex && arRaycastHitValue.distance <= raycastResultValue.distance));
                    if (m_IsARHitClosest)
                    {
                        m_IsUIHitClosest = false;
                    }
                }
                else
                {
                    m_IsARHitClosest = arRaycastHitIndex > 0 && (arRaycastHitIndex < hitIndex || (arRaycastHitIndex == hitIndex && arRaycastHitValue.distance <= distance));
                }

                m_RaycastHitOccurred = true;
            }
#endif
            if (_raycastHitOccurred)
            {
                if (_isUIHitClosest)
                {
                    RayEndPoint = _uiRaycastHit.worldPosition;
                    RayEndTransform = _uiRaycastHit.gameObject.transform;
                }
 #if AR_FOUNDATION_PRESENT
                else if (m_IsARHitClosest)
                {
                    rayEndPoint = arRaycastHitValue.pose.position;
                }
#endif
                else
                {
                    RayEndPoint = _raycastHit.point;
                    RayEndTransform = InteractionManager.TryGetInteractableForCollider(_raycastHit.collider, out _raycastInteractable)
                        ? _raycastInteractable.GetAttachTransform(this)
                        : _raycastHit.transform;
                }
            }
            else
            {
                UpdateSamplePointsIfNecessary();
                RayEndPoint = _samplePoints[_samplePoints.Count - 1].Position;
            }
        }

        /// <summary>
        /// If UI is being hovered, updates the selection flag to account for hovering over elements that need to be deselected (buttons) or elements that need to maintain selection (sliders).
        /// </summary>
        private void UpdateUIHover()
        {
            var timeDelta = Time.time - _lastTimeHoveredUIChanged;
            if (_isUIHitClosest && timeDelta > _hoverTimeToSelect && (timeDelta < (_hoverTimeToSelect + _timeToAutoDeselect) || _blockUIAutoDeselect))
                _hoverUISelectActive = true;
            else
                _hoverUISelectActive = false;
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

        private float GetProjectileAngle(Vector3 lineDirection)
        {
            var up = ReferenceUp;
            var projectedForward = Vector3.ProjectOnPlane(lineDirection, up);
            return Mathf.Approximately(Vector3.Angle(lineDirection, projectedForward), 0f)
                ? 0f
                : Vector3.SignedAngle(lineDirection, projectedForward, Vector3.Cross(up, lineDirection));
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

        /// <summary>
        /// Rotates the Attach Transform for this interactor. This can be useful to rotate a held object.
        /// </summary>
        /// <param name="attach">The Attach Transform of the interactor.</param>
        /// <param name="directionAmount">The rotation amount.</param>
        protected virtual void RotateAttachTransform(Transform attach, float directionAmount)
        {
            if (Mathf.Approximately(directionAmount, 0f))
                return;

            var rotateAngle = directionAmount * (_rotateSpeed * Time.deltaTime);

            if (_rotateReferenceFrame != null)
                attach.Rotate(_rotateReferenceFrame.up, rotateAngle, Space.World);
            else
                attach.Rotate(Vector3.up, rotateAngle);
        }

        /// <summary>
        /// Rotates the Attach Transform for this interactor to match a given direction. This can be useful to compute a direction angle for teleportation.
        /// </summary>
        /// <param name="attach">The Attach Transform of the interactor.</param>
        /// <param name="direction">The directional input.</param>
        /// <param name="referenceRotation">The reference rotation to define the up axis for rotation.</param>
        protected virtual void RotateAttachTransform(Transform attach, Vector2 direction, Quaternion referenceRotation)
        {
            if (Mathf.Approximately(direction.sqrMagnitude, 0f))
                return;

            var rotateAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            var directionalQuaternion = Quaternion.AngleAxis(rotateAngle, Vector3.up);
            attach.rotation = referenceRotation * directionalQuaternion;
        }

        /// <summary>
        /// Translates the Attach Transform for this interactor. This can be useful to move a held object closer or further away from the interactor.
        /// </summary>
        /// <param name="rayOrigin">The starting position and direction of any ray casts.</param>
        /// <param name="attach">The Attach Transform of the interactor.</param>
        /// <param name="directionAmount">The translation amount.</param>
        protected virtual void TranslateAttachTransform(Transform rayOrigin, Transform attach, float directionAmount)
        {
            if (Mathf.Approximately(directionAmount, 0f))
                return;

            GetLineOriginAndDirection(rayOrigin, out var lineOrigin, out var lineDirection);

            var resultingPosition = attach.position + lineDirection * (directionAmount * _translateSpeed * Time.deltaTime);

            // Check the delta between the origin position and the calculated position.
            // Clamp so it doesn't go further back than the origin position.
            var posInAttachSpace = resultingPosition - lineOrigin;
            var dotResult = Vector3.Dot(posInAttachSpace, lineDirection);

            attach.position = dotResult > 0f ? resultingPosition : lineOrigin;
        }

        /// <inheritdoc />
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                UpdateSamplePointsIfNecessary();
                if (_samplePoints != null && _samplePoints.Count >= 2)
                {
                    // Perform ray casts and store the equivalent Bezier curve to the endpoint where a hit occurred (used for blending)
                    UpdateRaycastHits();
                    CacheRaycastHit();
                    UpdateUIHover();
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
                    _lastTimeHoveredObjectChanged = Time.time;
                    _passedHoverTimeToSelect = false;
                }
                else if (!_passedHoverTimeToSelect && nearestObject != null)
                {
                    var progressToHoverSelect = Mathf.Clamp01((Time.time - _lastTimeHoveredObjectChanged) / GetHoverTimeToSelect(CurrentNearestValidTarget));

                    // If we have a selection and we're processing hover to select, don't allow hover to pass
                    // Selection likely came from non-hover method and we don't want to auto-deselect
                    if (progressToHoverSelect >= 1f && !HasSelection)
                        _passedHoverTimeToSelect = true;
                }

                // If we have a selection and interactable is set to auto deselect, process the select time
                if (_autoDeselect && HasSelection && !_passedTimeToAutoDeselect)
                {
                    var progressToDeselect = Mathf.Clamp01((Time.time - _lastTimeAutoSelected) / GetTimeToAutoDeselect(CurrentNearestValidTarget));
                    if (progressToDeselect >= 1f)
                        _passedTimeToAutoDeselect = true;
                }
            }
        }

        /// <inheritdoc />
        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractor(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                ScaleValue = 0f;

                // Update the pose of the Attach Transform
                if (_manipulateAttachTransform && HasSelection)
                {
                    _ProcessManipulationInput();
                }
            }

            void _ProcessManipulationInput()
            {
                // Check if the scaling toggle was performed this frame.
                if (_scaleToggleInput.CurrentState.ActivatedThisFrame)
                    _scaleInputActive = !_scaleInputActive;

                // If not scaling, we can translate and rotate
                if (!_scaleInputActive)
                {
                    switch (_rotateMode)
                    {
                        case RotateModeType.RotateOverTime:
                            RotateAttachTransform(AttachTransform, _rotateManipulationInput.CurrentValue.x);
                            break;
                        case RotateModeType.MatchDirection:
                            var referenceRotation = _rotateReferenceFrame != null ? _rotateReferenceFrame.rotation : EffectiveRayOrigin.rotation;
                            RotateAttachTransform(AttachTransform, _directionalManipulationInput.CurrentValue, referenceRotation);

                            break;
                        default:
                            Assert.IsTrue(false, $"Unhandled {nameof(RotateModeType)}={_rotateMode}.");
                            break;
                    }

                    TranslateAttachTransform(EffectiveRayOrigin, AttachTransform, _translateManipulationInput.CurrentValue.y);
                }
                else if (_scaleMode == ScaleMode.ScaleOverTime)
                {
                    ScaleValue = _scaleOverTimeInput.CurrentValue.y;
                }

                if (_scaleMode == ScaleMode.DistanceDelta)
                {
                    ScaleValue = _scaleDistanceDeltaInput.CurrentValue;
                }
            }
        }

        /// <inheritdoc />
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            targets.Clear();

            if (!isActiveAndEnabled)
                return;

            if (_raycastHitsCount > 0)
            {
                var hasUIHit = TryGetCurrentUIRaycastResult(out var uiRaycastResult, out var uiHitIndex);
                for (var i = 0; i < _raycastHitsCount; ++i)
                {
                    var raycastHit = _raycastHits[i];

                    // A hit on UI should block Interactables behind it from being a valid target
                    if (hasUIHit && uiHitIndex > 0 && (uiHitIndex < _raycastHitEndpointIndex || (uiHitIndex == _raycastHitEndpointIndex && uiRaycastResult.distance <= raycastHit.distance)))
                        break;

                    // A hit on geometry not associated with Interactables should block Interactables behind it from being a valid target
                    if (!InteractionManager.TryGetInteractableForCollider(raycastHit.collider, out var interactable))
                        break;

                    if (!targets.Contains(interactable))
                    {
                        targets.Add(interactable);

                        // Stop after the first if enabled
                        if (_hitClosestOnly)
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
#if AR_FOUNDATION_PRESENT
            bool hasARHit = false;

            m_ARRaycastHitsCount = 0;
            m_ARRaycastHitEndpointIndex = 0;
#endif
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
#if AR_FOUNDATION_PRESENT
                if (m_ARRaycastHitsCount > 0)
                {
                    m_ARRaycastHitEndpointIndex = i;
                    hasARHit = true;
                }
                if (has3DHit && (hasARHit || !enableARRaycasting))
                {
                    break;
                }
#else
                if (has3DHit)
                {
                    break;
                }
#endif
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

            switch (_hitDetectionType)
            {
                case HitDetectionModeType.Raycast:
                    _raycastHitsCount = _localPhysicsScene.Raycast(from, direction,
                        _raycastHits, maxDistance, _raycastMask, queryTriggerInteraction);
                    break;

                case HitDetectionModeType.SphereCast:
                    _raycastHitsCount = _localPhysicsScene.SphereCast(from, _sphereCastRadius, direction,
                        _raycastHits, maxDistance, _raycastMask, queryTriggerInteraction);
                    break;

                case HitDetectionModeType.ConeCast:
                    if (_lineType == LineModeType.StraightLine)
                    {
                        _raycastHitsCount = FilteredConeCast(from, _coneCastAngle, direction, origin,
                            _raycastHits, maxDistance, _raycastMask, queryTriggerInteraction);
                    }
                    break;
            }

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

#if AR_FOUNDATION_PRESENT
            m_ARRaycastHits.Clear();
            m_ARRaycastHitsCount = 0;
            if (m_EnableARRaycasting && m_ARRaycastManager != null)
            {
                var ray = new Ray(from, direction);
                m_ARRaycastManager.Raycast(ray, m_ARRaycastHits, trackableType);
                m_ARRaycastHitsCount = m_ARRaycastHits.Count;
            }
#endif
        }

        private int FilteredConeCast(in Vector3 from, float coneCastAngleDegrees, in Vector3 direction, in Vector3 origin,
            RaycastHit[] results, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            s_OptimalHits.Clear();

            // Set up all the sphere casts
            var obstructionDistance = math.min(maxDistance, 1000f);

            // Raycast looking for obstructions and any optimal targets
            var hitCounter = 0;
            var optimalHits = _localPhysicsScene.Raycast(origin, direction, s_SphereCastScratch, obstructionDistance, layerMask, queryTriggerInteraction);
            if (optimalHits > 0)
            {
                for (var i = 0; i < optimalHits; ++i) 
                {
                    var hitInfo = s_SphereCastScratch[i];
                    if (hitInfo.distance > obstructionDistance)
                        continue;

                    // If an obstruction is found, then reject anything behind it
                    if (!InteractionManager.TryGetInteractableForCollider(hitInfo.collider, out _))
                    {
                        obstructionDistance = math.min(hitInfo.distance, obstructionDistance);

                        // Since we are rejecting anything past the obstruction, we push its distance back to allow for objects in the periphery to be selected first
                        hitInfo.distance += 1.5f;
                    }

                    results[hitCounter] = hitInfo;
                    s_OptimalHits.Add(hitInfo.collider);
                    hitCounter++;
                }
            }

            // Now do a series of sphere casts that increase in size.
            // We don't process obstructions here
            // We don't do ultra-fine cone rejection instead add horizontal distance to the spherecast depth
            var angleRadius = math.tan(math.radians(coneCastAngleDegrees) * 0.5f);
            var currentOffset = (origin - from).magnitude;
            while (currentOffset < obstructionDistance)
            {
                BurstPhysicsUtils.GetConecastParameters(angleRadius, currentOffset, obstructionDistance, direction, out var originOffset, out var endRadius, out var castMax);

                // Spherecast
                var initialResults = _localPhysicsScene.SphereCast(origin + originOffset, endRadius, direction, s_SphereCastScratch, castMax, layerMask, queryTriggerInteraction);

                // Go through each result
                for (var i = 0; (i < initialResults && hitCounter < results.Length); i++)
                {
                    var hit = s_SphereCastScratch[i];

                    // Range check
                    if (hit.distance > obstructionDistance)
                        continue;

                    // If it's an optimal hit, then skip it
                    if (s_OptimalHits.Contains(hit.collider))
                        continue;

                    // It must have an interactable
                    if (!InteractionManager.TryGetInteractableForCollider(hit.collider, out _))
                        continue;

                    if (Mathf.Approximately(hit.distance, 0f) && hit.point == Vector3.zero)
                    {
                        // Sphere cast can return hits where point is (0, 0, 0) in error.
                        continue;
                    }

                    // Adjust distance by distance from ray center for default sorting
                    BurstPhysicsUtils.GetConecastOffset(origin, hit.point, direction, out var hitToRayDist);
                    
                    // We penalize these off-center hits by a meter + whatever horizontal offset they have
                    hit.distance += currentOffset + 1f + (hitToRayDist);
                    results[hitCounter] = hit;
                    hitCounter++;
                }
                currentOffset += castMax;
            }

            s_OptimalHits.Clear();
            Array.Clear(s_SphereCastScratch, 0, MaxSphereCastHits);
            return hitCounter;
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
        }

        /// <summary>
        /// Remove the array element by shifting the remaining elements down by one index.
        /// This does not resize the length of the array.
        /// </summary>
        /// <typeparam name="T">The struct type.</typeparam>
        /// <param name="array">The array to modify.</param>
        /// <param name="index">The index of the array element to effectively remove.</param>
        /// <param name="count">The number of elements contained in the array, which may be less than the array length.</param>
        private static void RemoveAt<T>(T[] array, int index, int count) where T : struct
        {
            Array.Copy(array, index + 1, array, index, count - index - 1);
            Array.Clear(array, count - 1, 1);
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

        /// <inheritdoc />
        public override bool IsSelectActive
        {
            get
            {
                if (_hoverToSelect && _passedHoverTimeToSelect)
                    return AllowSelect;

                return base.IsSelectActive;
            }
        }
        

        /// <inheritdoc />
        public override bool CanHover(IXRHoverInteractable interactable)
        {
            if (base.CanHover(interactable) && (!HasSelection || IsSelecting(interactable)))
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            if (CurrentNearestValidTarget == interactable && _autoDeselect && HasSelection && _passedHoverTimeToSelect && _passedTimeToAutoDeselect)
                return false;

            if (_hoverToSelect && _passedHoverTimeToSelect && CurrentNearestValidTarget != interactable)
                return false;

            return base.CanSelect(interactable) && (!HasSelection || IsSelecting(interactable));
        }

        /// <summary>
        /// Gets the number of seconds for which this interactor must hover over the interactable to select it if <see cref="HoverToSelect"/> is enabled.
        /// </summary>
        /// <param name="interactable">The interactable to get the duration for.</param>
        /// <returns>Returns the number of seconds for which this Interactor must hover over an Interactable to select it.</returns>
        /// <seealso cref="HoverTimeToSelect"/>
        protected virtual float GetHoverTimeToSelect(IXRInteractable interactable) => _hoverTimeToSelect;

        /// <summary>
        /// Gets the number of seconds for which this interactor will keep the interactable selected before automatically deselecting it.
        /// </summary>
        /// <param name="interactable">The interactable to get the duration for.</param>
        /// <returns>Returns the number of seconds for which this Interactor will keep an Interactable selected before automatically deselecting it.</returns>
        /// <seealso cref="TimeToAutoDeselect"/>
        protected virtual float GetTimeToAutoDeselect(IXRInteractable interactable) => _timeToAutoDeselect;

        /// <inheritdoc />
        public override void OnSelectEntering(SelectEnterEventArgs args)
        {
            base.OnSelectEntering(args);

            // Update when selecting via hover to select
            if (_autoDeselect && _passedHoverTimeToSelect)
            {
                _lastTimeAutoSelected = Time.time;
                _passedTimeToAutoDeselect = false;
            }

            if (!_useForceGrab && InteractablesSelected.Count == 1 && TryGetCurrent3DRaycastHit(out var raycastHit))
                AttachTransform.position = raycastHit.point;
        }

        /// <inheritdoc />
        public override void OnSelectExiting(SelectExitEventArgs args)
        {
            base.OnSelectExiting(args);

            // Reset to allow stop hover from automatically selecting again after auto deselect
            _passedHoverTimeToSelect = false;

            // Reset the auto select/deselect properties to allow this Interactor to select again after select exit
            _lastTimeHoveredObjectChanged = Time.time;
            _passedTimeToAutoDeselect = false;

            if (!HasSelection)
                RestoreAttachTransform();
        }

        /// <inheritdoc />
        void IUIHoverInteractor.OnUIHoverEntered(UIHoverEventArgs args) => OnUIHoverEntered(args);

        /// <inheritdoc />
        void IUIHoverInteractor.OnUIHoverExited(UIHoverEventArgs args) => OnUIHoverExited(args);

        /// <summary>
        /// The <see cref="XRUIInputModule"/> calls this method when the Interactor begins hovering over a UI element.
        /// </summary>
        /// <param name="args">Event data containing the UI element that is being hovered over.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnUIHoverExited(UIHoverEventArgs)"/>
        protected virtual void OnUIHoverEntered(UIHoverEventArgs args)
        {
            // Our hovering logic is all based on time-hovered, so if the selected element has changed it all must be reset
            var selectable = args.deviceModel.selectableObject;
            if (_lastUIObject != selectable)
            {
                _lastUIObject = selectable;

                if (selectable != null)
                {
                    _lastTimeHoveredUIChanged = Time.time;
                    _blockUIAutoDeselect = _lastUIObject.GetComponent<Slider>() != null;
                }
                else
                {
                    _blockUIAutoDeselect = false;
                }

                _hoverUISelectActive = false;
            }

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
        protected virtual void OnUIHoverExited(UIHoverEventArgs args)
        {
            // We might be triggered an onHover of a child object, so don't reset in that case
            var selectable = args.deviceModel.selectableObject;
            if (_lastUIObject != selectable)
            {
                _lastUIObject = null;
                _lastTimeHoveredUIChanged = Time.time;
                _blockUIAutoDeselect = false;
                _hoverUISelectActive = false;
            }

            UiHoverExited?.Invoke(args);
        }

        private void RestoreAttachTransform()
        {
            var pose = GetLocalAttachPoseOnSelect(FirstInteractableSelected);
            AttachTransform.localPosition = pose.position;
            AttachTransform.localRotation = pose.rotation;
        }

#if AR_FOUNDATION_PRESENT
        void FindCreateARRaycastManager()
        {
            if (m_ARRaycastManager != null || ComponentLocatorUtility<ARRaycastManager>.TryFindComponent(out m_ARRaycastManager))
                return;

            if (ComponentLocatorUtility<XROrigin>.TryFindComponent(out var xrOrigin))
            {
                // Add to the GameObject with the XR Origin component itself, not its potentially different Origin GameObject reference.
                m_ARRaycastManager = xrOrigin.gameObject.AddComponent<ARRaycastManager>();
            }
            else
            {
                Debug.LogWarning($"{nameof(XROrigin)} not found, cannot add the {nameof(ARRaycastManager)} automatically. Cannot ray cast against AR environment trackables.", this);
            }
        }
#endif

        private static int SanitizeSampleFrequency(int value)
        {
            // Upper range does not need to be enforced, just the minimum.
            // The max const just provides a reasonable slider range.
            return Mathf.Max(value, MinSampleFrequency);
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
    }
}
