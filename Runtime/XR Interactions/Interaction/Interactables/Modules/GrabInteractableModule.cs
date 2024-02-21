using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Pool;
using Vapor.Utilities;
using VaporInspector;
using VaporXR.Interaction;
using VaporXR.Locomotion.Teleportation;
using VaporXR.Utilities;

namespace VaporXR.Interaction
{
    [DisallowMultipleComponent]
    public class GrabInteractableModule : InteractableModule
    {
        private const float DefaultTighteningAmount = 0.1f;
        private const float DefaultSmoothingAmount = 8f;
        private const float VelocityDamping = 1f;
        private const float VelocityScale = 1f;
        private const float AngularVelocityDamping = 1f;
        private const float AngularVelocityScale = 1f;
        private const int ThrowSmoothingFrameCount = 20;
        private const float DefaultAttachEaseInTime = 0.15f;
        private const float DefaultThrowSmoothingDuration = 0.25f;
        private const float DefaultThrowVelocityScale = 1.5f;
        private const float DefaultThrowAngularVelocityScale = 1f;
        private const float DeltaTimeThreshold = 0.001f;

        private static readonly ProfilerMarker s_ProcessGrabTransformersMarker = new("VXRI.ProcessGrabTransformers");

        private static readonly LinkedPool<Transform> s_DynamicAttachTransformPool = new(OnCreatePooledItem, OnGetPooledItem, OnReleasePooledItem, OnDestroyPooledItem);
        private static readonly LinkedPool<DropEventArgs> s_DropEventArgs = new(() => new DropEventArgs(), collectionCheck: false);

        #region Inspector
        [SerializeField, FoldoutGroup("Components")]
        private Transform m_AttachTransform;
        [SerializeField, FoldoutGroup("Components")]
        private Transform m_SecondaryAttachTransform;

        [SerializeField, FoldoutGroup("Grabbing")]
        private bool m_UseDynamicAttach;
        [SerializeField, FoldoutGroup("Grabbing")]
        private bool m_MatchAttachPosition = true;
        [SerializeField, FoldoutGroup("Grabbing")]
        private bool m_MatchAttachRotation = true;
        [SerializeField, FoldoutGroup("Grabbing")]
        private bool m_SnapToColliderVolume = true;
        [SerializeField, FoldoutGroup("Grabbing")]
        private bool m_ReinitializeDynamicAttachEverySingleGrab = true;
        [SerializeField, FoldoutGroup("Grabbing")]
        private float m_AttachEaseInTime = DefaultAttachEaseInTime;
        [SerializeField, FoldoutGroup("Grabbing")]
        private MovementType m_MovementType = MovementType.Instantaneous;
        [SerializeField, Range(0f, 1f), FoldoutGroup("Grabbing")]
        private float m_VelocityDamping = VelocityDamping;
        [SerializeField, FoldoutGroup("Grabbing")]
        private float m_VelocityScale = VelocityScale;
        [SerializeField, Range(0f, 1f), FoldoutGroup("Grabbing")]
        private float m_AngularVelocityDamping = AngularVelocityDamping;
        [SerializeField, FoldoutGroup("Grabbing")]
        private float m_AngularVelocityScale = AngularVelocityScale;

        [SerializeField, FoldoutGroup("Tracking"), BoxGroup("Tracking/Position", "Position")]
        private bool m_TrackPosition = true;
        [SerializeField, FoldoutGroup("Tracking"), BoxGroup("Tracking/Position")]
        private bool m_SmoothPosition;
        [SerializeField, Range(0f, 20f), FoldoutGroup("Tracking"), BoxGroup("Tracking/Position")]
        private float m_SmoothPositionAmount = DefaultSmoothingAmount;
        [SerializeField, Range(0f, 1f), FoldoutGroup("Tracking"), BoxGroup("Tracking/Position")]
        private float m_TightenPosition = DefaultTighteningAmount;

        [SerializeField, FoldoutGroup("Tracking"), BoxGroup("Tracking/Rotation", "Rotation")]
        private bool m_TrackRotation = true;
        [SerializeField, FoldoutGroup("Tracking"), BoxGroup("Tracking/Rotation")]
        private bool m_SmoothRotation;
        [SerializeField, Range(0f, 20f), FoldoutGroup("Tracking"), BoxGroup("Tracking/Rotation")]
        private float m_SmoothRotationAmount = DefaultSmoothingAmount;
        [SerializeField, Range(0f, 1f), FoldoutGroup("Tracking"), BoxGroup("Tracking/Rotation")]
        private float m_TightenRotation = DefaultTighteningAmount;

        [SerializeField, FoldoutGroup("Tracking"), BoxGroup("Tracking/Scale", "Scale")]
        private bool m_TrackScale = true;
        [SerializeField, FoldoutGroup("Tracking"), BoxGroup("Tracking/Scale")]
        private bool m_SmoothScale;
        [SerializeField, Range(0f, 20f), FoldoutGroup("Tracking"), BoxGroup("Tracking/Scale")]
        private float m_SmoothScaleAmount = DefaultSmoothingAmount;
        [SerializeField, Range(0f, 1f), FoldoutGroup("Tracking"), BoxGroup("Tracking/Scale")]
        private float m_TightenScale = DefaultTighteningAmount;

        [SerializeField, FoldoutGroup("Throwing")]
        private bool m_ThrowOnDetach = true;
        [SerializeField, FoldoutGroup("Throwing")]
        private float m_ThrowSmoothingDuration = DefaultThrowSmoothingDuration;
        [SerializeField, FoldoutGroup("Throwing")]
        private AnimationCurve m_ThrowSmoothingCurve = AnimationCurve.Linear(1f, 1f, 1f, 0f);
        [SerializeField, FoldoutGroup("Throwing")]
        private float m_ThrowVelocityScale = DefaultThrowVelocityScale;
        [SerializeField, FoldoutGroup("Throwing")]
        private float m_ThrowAngularVelocityScale = DefaultThrowAngularVelocityScale;
        [SerializeField, FoldoutGroup("Throwing")]
        private bool m_ForceGravityOnDetach;
        [SerializeField, FoldoutGroup("Throwing")]
        private bool m_RetainTransformParent = true;

        [SerializeField, FoldoutGroup("Transformers")]
        private List<XRBaseGrabTransformer> m_StartingSingleGrabTransformers = new List<XRBaseGrabTransformer>();
        [SerializeField, FoldoutGroup("Transformers")]
        private List<XRBaseGrabTransformer> m_StartingMultipleGrabTransformers = new List<XRBaseGrabTransformer>();
        [SerializeField, FoldoutGroup("Transformers")]
        private bool m_AddDefaultGrabTransformers = true;
        #endregion

        #region Properties
        public bool TrackRotation => m_TrackRotation;

        public bool TrackPosition => m_TrackRotation;

        public bool TrackScale => m_TrackScale;

        public bool UseDynamicAttach { get => m_UseDynamicAttach; set => m_UseDynamicAttach = value; }

        /// <summary>
        /// The grab transformers that this Interactable automatically links at startup (optional, may be empty).
        /// These are used when there is a single interactor selecting this object.
        /// </summary>
        /// <remarks>
        /// To modify the grab transformers used after startup,
        /// the <see cref="AddSingleGrabTransformer"/> or <see cref="RemoveSingleGrabTransformer"/> methods should be used instead.
        /// </remarks>
        /// <seealso cref="StartingMultipleGrabTransformers"/>
        public List<XRBaseGrabTransformer> StartingSingleGrabTransformers { get => m_StartingSingleGrabTransformers; set => m_StartingSingleGrabTransformers = value; }

        /// <summary>
        /// The grab transformers that this Interactable automatically links at startup (optional, may be empty).
        /// These are used when there are multiple interactors selecting this object.
        /// </summary>
        /// <remarks>
        /// To modify the grab transformers used after startup,
        /// the <see cref="AddMultipleGrabTransformer"/> or <see cref="RemoveMultipleGrabTransformer"/> methods should be used instead.
        /// </remarks>
        /// <seealso cref="StartingSingleGrabTransformers"/>
        public List<XRBaseGrabTransformer> StartingMultipleGrabTransformers { get => m_StartingMultipleGrabTransformers; set => m_StartingMultipleGrabTransformers = value; }

        /// <summary>
        /// The number of single grab transformers.
        /// These are the grab transformers used when there is a single interactor selecting this object.
        /// </summary>
        /// <seealso cref="AddSingleGrabTransformer"/>
        public int SingleGrabTransformersCount => m_SingleGrabTransformers.FlushedCount;

        /// <summary>
        /// The number of multiple grab transformers.
        /// These are the grab transformers used when there are multiple interactors selecting this object.
        /// </summary>
        /// <seealso cref="AddMultipleGrabTransformer"/>
        public int MultipleGrabTransformersCount => m_MultipleGrabTransformers.FlushedCount;
        #endregion

        #region Fields
        private readonly SmallRegistrationList<IXRGrabTransformer> m_SingleGrabTransformers = new SmallRegistrationList<IXRGrabTransformer>();
        private readonly SmallRegistrationList<IXRGrabTransformer> m_MultipleGrabTransformers = new SmallRegistrationList<IXRGrabTransformer>();

        private List<IXRGrabTransformer> m_GrabTransformersAddedWhenGrabbed;
        private bool m_GrabCountChanged;
        private (int, int) m_GrabCountBeforeAndAfterChange;
        private bool m_IsProcessingGrabTransformers;

        /// <summary>
        /// The number of registered grab transformers that implement <see cref="IXRDropTransformer"/>.
        /// </summary>
        private int m_DropTransformersCount;

        // World pose we are moving towards each frame (eventually will be at Interactor's attach point assuming default single grab algorithm)
        private Pose m_TargetPose;
        private Vector3 m_TargetLocalScale;

        private bool m_IsTargetPoseDirty;
        private bool m_IsTargetLocalScaleDirty;

        private bool IsTransformDirty
        {
            get => m_IsTargetPoseDirty || m_IsTargetLocalScaleDirty;
            set
            {
                m_IsTargetPoseDirty = value;
                m_IsTargetLocalScaleDirty = value;
            }
        }

        private float m_CurrentAttachEaseTime;
        private MovementType m_CurrentMovementType;

        private bool m_DetachInLateUpdate;
        private Vector3 m_DetachVelocity;
        private Vector3 m_DetachAngularVelocity;

        private int m_ThrowSmoothingCurrentFrame;
        private readonly float[] m_ThrowSmoothingFrameTimes = new float[ThrowSmoothingFrameCount];
        private readonly Vector3[] m_ThrowSmoothingVelocityFrames = new Vector3[ThrowSmoothingFrameCount];
        private readonly Vector3[] m_ThrowSmoothingAngularVelocityFrames = new Vector3[ThrowSmoothingFrameCount];
        private bool m_ThrowSmoothingFirstUpdate;
        private Pose m_LastThrowReferencePose;
        private IXRAimAssist m_ThrowAssist;

        private Rigidbody m_Rigidbody;

        // Rigidbody's settings upon select, kept to restore these values when dropped
        private bool m_WasKinematic;
        private bool m_UsedGravity;
        private float m_OldDrag;
        private float m_OldAngularDrag;

        // Used to keep track of colliders for which to ignore collision with character only while grabbed
        private bool m_IgnoringCharacterCollision;
        private bool m_StopIgnoringCollisionInLateUpdate;
        private CharacterController m_SelectingCharacterController;
        private readonly HashSet<Interactor> m_SelectingCharacterInteractors = new();
        private readonly List<Collider> m_RigidbodyColliders = new List<Collider>();
        private readonly HashSet<Collider> m_CollidersThatAllowedCharacterCollision = new();

        private Transform m_OriginalSceneParent;

        // Account for teleportation to avoid throws with unintentionally high energy
        private TeleportationMonitor m_TeleportationMonitor;

        private readonly Dictionary<Interactor, Transform> m_DynamicAttachTransforms = new();
        #endregion

        #region Events

        #endregion

        #region - Initialization -
        protected override void Awake()
        {
            base.Awake();
            m_TeleportationMonitor = new TeleportationMonitor();
            m_TeleportationMonitor.teleported += OnTeleported;

            m_CurrentMovementType = m_MovementType;
            if (!TryGetComponent(out m_Rigidbody))
                Debug.LogError("XR Grab Interactable does not have a required Rigidbody.", this);

            m_Rigidbody.GetComponentsInChildren(true, m_RigidbodyColliders);
            for (var i = m_RigidbodyColliders.Count - 1; i >= 0; i--)
            {
                if (m_RigidbodyColliders[i].attachedRigidbody != m_Rigidbody)
                    m_RigidbodyColliders.RemoveAt(i);
            }

            InitializeTargetPoseAndScale(transform);

            // Load the starting grab transformers into the Play mode lists.
            // It is more efficient to add than move, but if there are existing items
            // use move to ensure the correct order dictated by the starting lists.
            if (m_SingleGrabTransformers.FlushedCount > 0)
            {
                var index = 0;
                foreach (var transformer in m_StartingSingleGrabTransformers)
                {
                    if (transformer != null)
                        MoveSingleGrabTransformerTo(transformer, index++);
                }
            }
            else
            {
                foreach (var transformer in m_StartingSingleGrabTransformers)
                {
                    if (transformer != null)
                        AddSingleGrabTransformer(transformer);
                }
            }

            if (m_MultipleGrabTransformers.FlushedCount > 0)
            {
                var index = 0;
                foreach (var transformer in m_StartingMultipleGrabTransformers)
                {
                    if (transformer != null)
                        MoveMultipleGrabTransformerTo(transformer, index++);
                }
            }
            else
            {
                foreach (var transformer in m_StartingMultipleGrabTransformers)
                {
                    if (transformer != null)
                        AddMultipleGrabTransformer(transformer);
                }
            }

            FlushRegistration();
        }

        protected virtual void OnEnable()
        {
            Interactable.SelectEntering += OnSelectEntering;
            Interactable.SelectExiting += OnSelectExiting;
            Interactable.SelectExited += OnSelectExited;
        }

        protected virtual void OnDisable()
        {
            Interactable.SelectEntering -= OnSelectEntering;
            Interactable.SelectExiting -= OnSelectExiting;
            Interactable.SelectExited -= OnSelectExited;
        }

        protected void OnDestroy()
        {
            // Unlink this interactable from the grab transformers
            ClearSingleGrabTransformers();
            ClearMultipleGrabTransformers();
        }
        #endregion

        #region - Processing -
        public override void PreProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            // Add the default grab transformers if needed.
            // This is done here (as opposed to Awake) since transformer behaviors automatically register in their Start,
            // so existing components should have a chance to register before we add the default grab transformers.
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                AddDefaultGrabTransformers();
            }

            FlushRegistration();

            switch (updatePhase)
            {
                // During Fixed update we want to apply any Rigidbody-based updates (e.g., Kinematic or VelocityTracking).
                case XRInteractionUpdateOrder.UpdatePhase.Fixed:
                    if (Interactable.IsSelected || IsTransformDirty)
                    {
                        if (m_CurrentMovementType == MovementType.Kinematic ||
                            m_CurrentMovementType == MovementType.VelocityTracking)
                        {
                            // If we only updated the target scale externally, just update that.
                            if (m_IsTargetLocalScaleDirty && !m_IsTargetPoseDirty && !Interactable.IsSelected)
                                ApplyTargetScale();
                            else if (m_CurrentMovementType == MovementType.Kinematic)
                                PerformKinematicUpdate(updatePhase);
                            else if (m_CurrentMovementType == MovementType.VelocityTracking)
                                PerformVelocityTrackingUpdate(updatePhase, Time.deltaTime);
                        }
                    }

                    if (m_IgnoringCharacterCollision && !m_StopIgnoringCollisionInLateUpdate &&
                        m_SelectingCharacterInteractors.Count == 0 && m_SelectingCharacterController != null &&
                        IsOutsideCharacterCollider(m_SelectingCharacterController))
                    {
                        // Wait until Late update so that physics can update before we restore the ability to collide with character
                        m_StopIgnoringCollisionInLateUpdate = true;
                    }

                    break;

                // During Dynamic update and OnBeforeRender we want to update the target pose and apply any Transform-based updates (e.g., Instantaneous).
                case XRInteractionUpdateOrder.UpdatePhase.Dynamic:
                case XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender:
                    if (IsTransformDirty)
                    {
                        // If we only updated the target scale externally, just update that.
                        if (m_IsTargetLocalScaleDirty && !m_IsTargetPoseDirty)
                            ApplyTargetScale();
                        else
                            PerformInstantaneousUpdate(updatePhase);
                    }

                    if (Interactable.IsSelected || (m_GrabCountChanged && m_DropTransformersCount > 0))
                    {
                        UpdateTarget(updatePhase, Time.deltaTime);

                        if (m_CurrentMovementType == MovementType.Instantaneous)
                            PerformInstantaneousUpdate(updatePhase);
                    }

                    break;

                // Late update is used to handle detach and restoring character collision as late as possible.
                case XRInteractionUpdateOrder.UpdatePhase.Late:
                    if (m_DetachInLateUpdate)
                    {
                        if (!Interactable.IsSelected)
                            Detach();
                        m_DetachInLateUpdate = false;
                    }

                    if (m_StopIgnoringCollisionInLateUpdate)
                    {
                        if (m_IgnoringCharacterCollision && m_SelectingCharacterController != null)
                        {
                            StopIgnoringCharacterCollision(m_SelectingCharacterController);
                            m_SelectingCharacterController = null;
                        }

                        m_StopIgnoringCollisionInLateUpdate = false;
                    }

                    break;
            }
        }

        private void PerformInstantaneousUpdate(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic ||
                updatePhase == XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender)
            {
                if (m_TrackPosition && m_TrackRotation)
                    transform.SetPositionAndRotation(m_TargetPose.position, m_TargetPose.rotation);
                else if (m_TrackPosition)
                    transform.position = m_TargetPose.position;
                else if (m_TrackRotation)
                    transform.rotation = m_TargetPose.rotation;

                ApplyTargetScale();

                IsTransformDirty = false;
            }
        }

        private void PerformKinematicUpdate(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Fixed)
            {
                if (m_TrackPosition)
                    m_Rigidbody.MovePosition(m_TargetPose.position);

                if (m_TrackRotation)
                    m_Rigidbody.MoveRotation(m_TargetPose.rotation);

                ApplyTargetScale();

                IsTransformDirty = false;
            }
        }

        private void PerformVelocityTrackingUpdate(XRInteractionUpdateOrder.UpdatePhase updatePhase, float deltaTime)
        {
            // Skip velocity calculations if Time.deltaTime is too low due to a frame-timing issue on Quest
            if (deltaTime < DeltaTimeThreshold)
                return;

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Fixed)
            {
                // Do velocity tracking
                if (m_TrackPosition)
                {
                    // Scale initialized velocity by prediction factor
                    m_Rigidbody.velocity *= (1f - m_VelocityDamping);
                    var positionDelta = m_TargetPose.position - transform.position;
                    var velocity = positionDelta / deltaTime;
                    m_Rigidbody.velocity += (velocity * m_VelocityScale);
                }

                // Do angular velocity tracking
                if (m_TrackRotation)
                {
                    // Scale initialized velocity by prediction factor
                    m_Rigidbody.angularVelocity *= (1f - m_AngularVelocityDamping);
                    var rotationDelta = m_TargetPose.rotation * Quaternion.Inverse(transform.rotation);
                    rotationDelta.ToAngleAxis(out var angleInDegrees, out var rotationAxis);
                    if (angleInDegrees > 180f)
                        angleInDegrees -= 360f;

                    if (Mathf.Abs(angleInDegrees) > Mathf.Epsilon)
                    {
                        var angularVelocity = (rotationAxis * (angleInDegrees * Mathf.Deg2Rad)) / deltaTime;
                        m_Rigidbody.angularVelocity += (angularVelocity * m_AngularVelocityScale);
                    }
                }

                ApplyTargetScale();

                IsTransformDirty = false;
            }
        }

        private void UpdateTarget(XRInteractionUpdateOrder.UpdatePhase updatePhase, float deltaTime)
        {
            // If the grab count changed to a lower number, and it is now 1, we need to recompute the dynamic attach transform for the interactor.
            if (m_ReinitializeDynamicAttachEverySingleGrab && m_GrabCountChanged && m_GrabCountBeforeAndAfterChange.Item2 < m_GrabCountBeforeAndAfterChange.Item1 && Interactable.InteractorsSelecting.Count == 1 &&
                m_DynamicAttachTransforms.Count > 0 && m_DynamicAttachTransforms.TryGetValue(Interactable.InteractorsSelecting[0], out var dynamicAttachTransform))
            {
                InitializeDynamicAttachPoseInternal(Interactable.InteractorsSelecting[0], dynamicAttachTransform);
            }

            var rawTargetPose = m_TargetPose;
            var rawTargetScale = m_TargetLocalScale;

            InvokeGrabTransformersProcess(updatePhase, ref rawTargetPose, ref rawTargetScale);

            if (!Interactable.IsSelected)
            {
                m_TargetPose = rawTargetPose;
                m_TargetLocalScale = rawTargetScale;
                return;
            }

            // Skip during OnBeforeRender since it doesn't require that high accuracy.
            // Skip when not selected since the the detach velocity has already been applied and we no longer need to compute it.
            // This means that the final Process for drop grab transformers does not contribute to throw velocity.
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                // Track the target pose before easing.
                // This avoids an unintentionally high detach velocity if grabbing with an XRRayInteractor
                // with Force Grab enabled causing the target pose to move very quickly between this transform's
                // initial position and the target pose after easing when the easing time is short.
                // By always tracking the target pose result from the grab transformers, it avoids the issue.
                StepThrowSmoothing(rawTargetPose, deltaTime);
            }

            // Apply easing and smoothing (if configured)
            StepSmoothing(rawTargetPose, rawTargetScale, deltaTime);
        }

        private void StepSmoothing(in Pose rawTargetPose, in Vector3 rawTargetLocalScale, float deltaTime)
        {
            if (m_AttachEaseInTime > 0f && m_CurrentAttachEaseTime <= m_AttachEaseInTime)
            {
                EaseAttachBurst(ref m_TargetPose, ref m_TargetLocalScale, rawTargetPose, rawTargetLocalScale, deltaTime,
                    m_AttachEaseInTime, ref m_CurrentAttachEaseTime);
            }
            else
            {
                StepSmoothingBurst(ref m_TargetPose, ref m_TargetLocalScale, rawTargetPose, rawTargetLocalScale, deltaTime,
                    m_SmoothPosition, m_SmoothPositionAmount, m_TightenPosition,
                    m_SmoothRotation, m_SmoothRotationAmount, m_TightenRotation,
                    m_SmoothScale, m_SmoothScaleAmount, m_TightenScale);
            }
        }
        #endregion

        #region - Select -
        private void OnSelectEntering(SelectEnterEventArgs args)
        {
            // Setup the dynamic attach transform.
            // Done before calling the base method so the attach pose captured is the dynamic one.
            var dynamicAttachTransform = CreateDynamicAttachTransform(args.InteractorObject);
            InitializeDynamicAttachPoseInternal(args.InteractorObject, dynamicAttachTransform);

            // Store the grab count change.
            m_GrabCountChanged = true;
            m_GrabCountBeforeAndAfterChange = Interactable.SelectCountBeforeAndAfterChange;
            m_CurrentAttachEaseTime = 0f;

            // Reset the throw data every time the number of grabs increases since
            // each additional grab could cause a large change in target position,
            // making it throw at an unwanted velocity. It is not called when the number
            // of grabs decreases even though it would have the same issue, but doing so
            // would make it almost impossible to throw with both hands.
            ResetThrowSmoothing();

            // Check if we should ignore collision with character every time number of grabs increases since
            // the first select could have happened from a non-character interactor.
            if (!m_IgnoringCharacterCollision)
            {
                m_SelectingCharacterController = args.InteractorObject.transform.GetComponentInParent<CharacterController>();
                if (m_SelectingCharacterController != null)
                {
                    m_SelectingCharacterInteractors.Add(args.InteractorObject);
                    StartIgnoringCharacterCollision(m_SelectingCharacterController);
                }
            }
            else if (m_SelectingCharacterController != null && args.InteractorObject.transform.IsChildOf(m_SelectingCharacterController.transform))
            {
                m_SelectingCharacterInteractors.Add(args.InteractorObject);
            }

            if (Interactable.InteractorsSelecting.Count == 1)
            {
                Grab();
                InvokeGrabTransformersOnGrab();
            }

            SubscribeTeleportationProvider(args.InteractorObject);
        }

        private void OnSelectExiting(SelectExitEventArgs args)
        {
            // Store the grab count change.
            m_GrabCountChanged = true;
            m_GrabCountBeforeAndAfterChange = Interactable.SelectCountBeforeAndAfterChange;
            m_CurrentAttachEaseTime = 0f;

            if (Interactable.InteractorsSelecting.Count == 0)
            {
                if (m_ThrowOnDetach)
                    m_ThrowAssist = args.InteractorObject.transform.GetComponentInParent<IXRAimAssist>();

                Drop();

                if (m_DropTransformersCount > 0)
                {
                    using (s_DropEventArgs.Get(out var dropArgs))
                    {
                        dropArgs.SelectExitEventArgs = args;
                        InvokeGrabTransformersOnDrop(dropArgs);
                    }
                }
            }

            // Don't restore ability to collide with character until the object is not overlapping with the character.
            // This prevents the character from being pushed out of the way of the dropped object while moving.
            m_SelectingCharacterInteractors.Remove(args.InteractorObject);

            UnsubscribeTeleportationProvider(args.InteractorObject);
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            ReleaseDynamicAttachTransform(args.InteractorObject);
        }
        #endregion

        #region - Collision -
        private void StartIgnoringCharacterCollision(Collider characterCollider)
        {
            m_IgnoringCharacterCollision = true;
            m_CollidersThatAllowedCharacterCollision.Clear();
            for (var index = 0; index < m_RigidbodyColliders.Count; ++index)
            {
                var rigidbodyCollider = m_RigidbodyColliders[index];
                if (rigidbodyCollider == null || rigidbodyCollider.isTrigger || Physics.GetIgnoreCollision(rigidbodyCollider, characterCollider))
                    continue;

                m_CollidersThatAllowedCharacterCollision.Add(rigidbodyCollider);
                Physics.IgnoreCollision(rigidbodyCollider, characterCollider, true);
            }
        }

        private bool IsOutsideCharacterCollider(Collider characterCollider)
        {
            var characterBounds = characterCollider.bounds;
            foreach (var rigidbodyCollider in m_CollidersThatAllowedCharacterCollision)
            {
                if (rigidbodyCollider == null)
                    continue;

                if (rigidbodyCollider.bounds.Intersects(characterBounds))
                    return false;
            }

            return true;
        }

        private void StopIgnoringCharacterCollision(Collider characterCollider)
        {
            m_IgnoringCharacterCollision = false;
            foreach (var rigidbodyCollider in m_CollidersThatAllowedCharacterCollision)
            {
                if (rigidbodyCollider != null)
                    Physics.IgnoreCollision(rigidbodyCollider, characterCollider, false);
            }
        }
        #endregion

        #region - Grabbing -
        /// <summary>
        /// Updates the state of the object due to being grabbed.
        /// Automatically called when entering the Select state.
        /// </summary>
        /// <seealso cref="Drop"/>
        protected virtual void Grab()
        {
            var thisTransform = transform;
            m_OriginalSceneParent = thisTransform.parent;
            thisTransform.SetParent(null);

            UpdateCurrentMovementType();
            SetupRigidbodyGrab(m_Rigidbody);

            // Reset detach velocities
            m_DetachVelocity = Vector3.zero;
            m_DetachAngularVelocity = Vector3.zero;

            // Initialize target pose and scale
            InitializeTargetPoseAndScale(thisTransform);
        }

        /// <summary>
        /// Updates the state of the object due to being dropped and schedule to finish the detach during the end of the frame.
        /// Automatically called when exiting the Select state.
        /// </summary>
        /// <seealso cref="Detach"/>
        /// <seealso cref="Grab"/>
        protected virtual void Drop()
        {
            if (m_RetainTransformParent && m_OriginalSceneParent != null && !m_OriginalSceneParent.gameObject.activeInHierarchy)
            {
#if UNITY_EDITOR
                // Suppress the warning when exiting Play mode to avoid confusing the user
                var exitingPlayMode = UnityEditor.EditorApplication.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#else
                var exitingPlayMode = false;
#endif
                if (!exitingPlayMode)
                    Debug.LogWarning("Retain Transform Parent is set to true, and has a non-null Original Scene Parent. " +
                                     "However, the old parent is deactivated so we are choosing not to re-parent upon dropping.", this);
            }
            else if (m_RetainTransformParent && gameObject.activeInHierarchy)
                transform.SetParent(m_OriginalSceneParent);

            SetupRigidbodyDrop(m_Rigidbody);

            m_CurrentMovementType = m_MovementType;
            m_DetachInLateUpdate = true;
            EndThrowSmoothing();
        }

        /// <summary>
        /// Updates the state of the object to finish the detach after being dropped.
        /// Automatically called during the end of the frame after being dropped.
        /// </summary>
        /// <remarks>
        /// This method updates the velocity of the Rigidbody if configured to do so.
        /// </remarks>
        /// <seealso cref="Drop"/>
        protected virtual void Detach()
        {
            if (m_ThrowOnDetach)
            {
                if (m_Rigidbody.isKinematic)
                {
                    Debug.LogWarning(
                        "Cannot throw a kinematic Rigidbody since updating the velocity and angular velocity of a kinematic Rigidbody is not supported. Disable Throw On Detach or Is Kinematic to fix this issue.",
                        this);
                    return;
                }

                if (m_ThrowAssist != null)
                {
                    m_DetachVelocity = m_ThrowAssist.GetAssistedVelocity(m_Rigidbody.position, m_DetachVelocity, m_Rigidbody.useGravity ? -Physics.gravity.y : 0f);
                    m_ThrowAssist = null;
                }

                m_Rigidbody.velocity = m_DetachVelocity;
                m_Rigidbody.angularVelocity = m_DetachAngularVelocity;
            }
        }

        /// <summary>
        /// Setup the <see cref="Rigidbody"/> on this object due to being grabbed.
        /// Automatically called when entering the Select state.
        /// </summary>
        /// <param name="rigidbody">The <see cref="Rigidbody"/> on this object.</param>
        /// <seealso cref="SetupRigidbodyDrop"/>
        // ReSharper disable once ParameterHidesMember
        protected virtual void SetupRigidbodyGrab(Rigidbody rigidbody)
        {
            // Remember Rigidbody settings and setup to move
            m_WasKinematic = rigidbody.isKinematic;
            m_UsedGravity = rigidbody.useGravity;
            m_OldDrag = rigidbody.drag;
            m_OldAngularDrag = rigidbody.angularDrag;
            rigidbody.isKinematic = m_CurrentMovementType == MovementType.Kinematic || m_CurrentMovementType == MovementType.Instantaneous;
            rigidbody.useGravity = false;
            rigidbody.drag = 0f;
            rigidbody.angularDrag = 0f;
        }

        /// <summary>
        /// Setup the <see cref="Rigidbody"/> on this object due to being dropped.
        /// Automatically called when exiting the Select state.
        /// </summary>
        /// <param name="rigidbody">The <see cref="Rigidbody"/> on this object.</param>
        /// <seealso cref="SetupRigidbodyGrab"/>
        // ReSharper disable once ParameterHidesMember
        protected virtual void SetupRigidbodyDrop(Rigidbody rigidbody)
        {
            // Restore Rigidbody settings
            rigidbody.isKinematic = m_WasKinematic;
            rigidbody.useGravity = m_UsedGravity;
            rigidbody.drag = m_OldDrag;
            rigidbody.angularDrag = m_OldAngularDrag;

            if (!Interactable.IsSelected)
                m_Rigidbody.useGravity |= m_ForceGravityOnDetach;
        }

        /// <summary>
        /// Adds the given grab transformer to the list of transformers used when there is a single interactor selecting this object.
        /// </summary>
        /// <param name="transformer">The grab transformer to add.</param>
        /// <seealso cref="AddMultipleGrabTransformer"/>
        public void AddSingleGrabTransformer(IXRGrabTransformer transformer) => AddGrabTransformer(transformer, m_SingleGrabTransformers);

        /// <summary>
        /// Adds the given grab transformer to the list of transformers used when there are multiple interactors selecting this object.
        /// </summary>
        /// <param name="transformer">The grab transformer to add.</param>
        /// <seealso cref="AddSingleGrabTransformer"/>
        public void AddMultipleGrabTransformer(IXRGrabTransformer transformer) => AddGrabTransformer(transformer, m_MultipleGrabTransformers);

        /// <summary>
        /// Removes the given grab transformer from the list of transformers used when there is a single interactor selecting this object.
        /// </summary>
        /// <param name="transformer">The grab transformer to remove.</param>
        /// <returns>
        /// Returns <see langword="true"/> if <paramref name="transformer"/> was removed from the list.
        /// Otherwise, returns <see langword="false"/> if <paramref name="transformer"/> was not found in the list.
        /// </returns>
        /// <seealso cref="RemoveMultipleGrabTransformer"/>
        public bool RemoveSingleGrabTransformer(IXRGrabTransformer transformer) => RemoveGrabTransformer(transformer, m_SingleGrabTransformers);

        /// <summary>
        /// Removes the given grab transformer from the list of transformers used when there is are multiple interactors selecting this object.
        /// </summary>
        /// <param name="transformer">The grab transformer to remove.</param>
        /// <returns>
        /// Returns <see langword="true"/> if <paramref name="transformer"/> was removed from the list.
        /// Otherwise, returns <see langword="false"/> if <paramref name="transformer"/> was not found in the list.
        /// </returns>
        /// <seealso cref="RemoveSingleGrabTransformer"/>
        public bool RemoveMultipleGrabTransformer(IXRGrabTransformer transformer) => RemoveGrabTransformer(transformer, m_MultipleGrabTransformers);

        /// <summary>
        /// Removes all grab transformers from the list of transformers used when there is a single interactor selecting this object.
        /// </summary>
        /// <seealso cref="ClearMultipleGrabTransformers"/>
        public void ClearSingleGrabTransformers() => ClearGrabTransformers(m_SingleGrabTransformers);

        /// <summary>
        /// Removes all grab transformers from the list of transformers used when there is are multiple interactors selecting this object.
        /// </summary>
        /// <seealso cref="ClearSingleGrabTransformers"/>
        public void ClearMultipleGrabTransformers() => ClearGrabTransformers(m_MultipleGrabTransformers);

        /// <summary>
        /// Returns all transformers used when there is a single interactor selecting this object into List <paramref name="results"/>.
        /// </summary>
        /// <param name="results">List to receive grab transformers.</param>
        /// <remarks>
        /// This method populates the list with the grab transformers at the time the
        /// method is called. It is not a live view, meaning grab transformers
        /// added or removed afterward will not be reflected in the
        /// results of this method.
        /// Clears <paramref name="results"/> before adding to it.
        /// </remarks>
        /// <seealso cref="GetMultipleGrabTransformers"/>
        public void GetSingleGrabTransformers(List<IXRGrabTransformer> results) => GetGrabTransformers(m_SingleGrabTransformers, results);

        /// <summary>
        /// Returns all transformers used when there are multiple interactors selecting this object into List <paramref name="results"/>.
        /// </summary>
        /// <param name="results">List to receive grab transformers.</param>
        /// <remarks>
        /// This method populates the list with the grab transformers at the time the
        /// method is called. It is not a live view, meaning grab transformers
        /// added or removed afterward will not be reflected in the
        /// results of this method.
        /// Clears <paramref name="results"/> before adding to it.
        /// </remarks>
        /// <seealso cref="GetSingleGrabTransformers"/>
        public void GetMultipleGrabTransformers(List<IXRGrabTransformer> results) => GetGrabTransformers(m_MultipleGrabTransformers, results);

        /// <summary>
        /// Returns the grab transformer at <paramref name="index"/> in the list of transformers used when there is a single interactor selecting this object.
        /// </summary>
        /// <param name="index">Index of the grab transformer to return. Must be smaller than <see cref="SingleGrabTransformersCount"/> and not negative.</param>
        /// <returns>Returns the grab transformer at the given index.</returns>
        /// <seealso cref="GetMultipleGrabTransformerAt"/>
        public IXRGrabTransformer GetSingleGrabTransformerAt(int index) => m_SingleGrabTransformers.GetRegisteredItemAt(index);

        /// <summary>
        /// Returns the grab transformer at <paramref name="index"/> in the list of transformers used when there are multiple interactors selecting this object.
        /// </summary>
        /// <param name="index">Index of the grab transformer to return. Must be smaller than <see cref="MultipleGrabTransformersCount"/> and not negative.</param>
        /// <returns>Returns the grab transformer at the given index.</returns>
        /// <seealso cref="GetSingleGrabTransformerAt"/>
        public IXRGrabTransformer GetMultipleGrabTransformerAt(int index) => m_MultipleGrabTransformers.GetRegisteredItemAt(index);

        /// <summary>
        /// Moves the given grab transformer in the list of transformers used when there is a single interactor selecting this object.
        /// If the grab transformer is not in the list, this can be used to insert the grab transformer at the specified index.
        /// </summary>
        /// <param name="transformer">The grab transformer to move or add.</param>
        /// <param name="newIndex">New index of the grab transformer.</param>
        /// <seealso cref="MoveMultipleGrabTransformerTo"/>
        public void MoveSingleGrabTransformerTo(IXRGrabTransformer transformer, int newIndex) => MoveGrabTransformerTo(transformer, newIndex, m_SingleGrabTransformers);

        /// <summary>
        /// Moves the given grab transformer in the list of transformers used when there are multiple interactors selecting this object.
        /// If the grab transformer is not in the list, this can be used to insert the grab transformer at the specified index.
        /// </summary>
        /// <param name="transformer">The grab transformer to move or add.</param>
        /// <param name="newIndex">New index of the grab transformer.</param>
        /// <seealso cref="MoveSingleGrabTransformerTo"/>
        public void MoveMultipleGrabTransformerTo(IXRGrabTransformer transformer, int newIndex) => MoveGrabTransformerTo(transformer, newIndex, m_MultipleGrabTransformers);

        void AddGrabTransformer(IXRGrabTransformer transformer, BaseRegistrationList<IXRGrabTransformer> grabTransformers)
        {
            if (transformer == null)
                throw new ArgumentNullException(nameof(transformer));

            if (m_IsProcessingGrabTransformers)
                Debug.LogWarning($"{transformer} added while {name} is processing grab transformers. It won't be processed until the next process.", this);

            if (grabTransformers.Register(transformer))
                OnAddedGrabTransformer(transformer);
        }

        bool RemoveGrabTransformer(IXRGrabTransformer transformer, BaseRegistrationList<IXRGrabTransformer> grabTransformers)
        {
            if (grabTransformers.Unregister(transformer))
            {
                OnRemovedGrabTransformer(transformer);
                return true;
            }

            return false;
        }

        void ClearGrabTransformers(BaseRegistrationList<IXRGrabTransformer> grabTransformers)
        {
            for (var index = grabTransformers.FlushedCount - 1; index >= 0; --index)
            {
                var transformer = grabTransformers.GetRegisteredItemAt(index);
                RemoveGrabTransformer(transformer, grabTransformers);
            }
        }

        void MoveGrabTransformerTo(IXRGrabTransformer transformer, int newIndex, BaseRegistrationList<IXRGrabTransformer> grabTransformers)
        {
            if (transformer == null)
                throw new ArgumentNullException(nameof(transformer));

            // BaseRegistrationList<T> does not yet support reordering with pending registration changes.
            if (m_IsProcessingGrabTransformers)
            {
                Debug.LogError($"Cannot move {transformer} while {name} is processing grab transformers.", this);
                return;
            }

            grabTransformers.Flush();
            if (grabTransformers.MoveItemImmediately(transformer, newIndex))
                OnAddedGrabTransformer(transformer);
        }

        static void GetGrabTransformers(BaseRegistrationList<IXRGrabTransformer> grabTransformers, List<IXRGrabTransformer> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            grabTransformers.GetRegisteredItems(results);
        }

        private void FlushRegistration()
        {
            m_SingleGrabTransformers.Flush();
            m_MultipleGrabTransformers.Flush();
        }

        void InvokeGrabTransformersOnGrab()
        {
            m_IsProcessingGrabTransformers = true;

            if (m_SingleGrabTransformers.RegisteredSnapshot.Count > 0)
            {
                foreach (var transformer in m_SingleGrabTransformers.RegisteredSnapshot)
                {
                    if (m_SingleGrabTransformers.IsStillRegistered(transformer))
                        transformer.OnGrab(this);
                }
            }

            if (m_MultipleGrabTransformers.RegisteredSnapshot.Count > 0)
            {
                foreach (var transformer in m_MultipleGrabTransformers.RegisteredSnapshot)
                {
                    if (m_MultipleGrabTransformers.IsStillRegistered(transformer))
                        transformer.OnGrab(this);
                }
            }

            m_IsProcessingGrabTransformers = false;
        }

        void InvokeGrabTransformersOnDrop(DropEventArgs args)
        {
            m_IsProcessingGrabTransformers = true;

            if (m_SingleGrabTransformers.RegisteredSnapshot.Count > 0)
            {
                foreach (var transformer in m_SingleGrabTransformers.RegisteredSnapshot)
                {
                    if (transformer is IXRDropTransformer dropTransformer && m_SingleGrabTransformers.IsStillRegistered(transformer))
                        dropTransformer.OnDrop(this, args);
                }
            }

            if (m_MultipleGrabTransformers.RegisteredSnapshot.Count > 0)
            {
                foreach (var transformer in m_MultipleGrabTransformers.RegisteredSnapshot)
                {
                    if (transformer is IXRDropTransformer dropTransformer && m_MultipleGrabTransformers.IsStillRegistered(transformer))
                        dropTransformer.OnDrop(this, args);
                }
            }

            m_IsProcessingGrabTransformers = false;
        }

        void InvokeGrabTransformersProcess(XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            m_IsProcessingGrabTransformers = true;

            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable -- ProfilerMarker.Begin with context object does not have Pure attribute
            using (s_ProcessGrabTransformersMarker.Auto())
            {
                // Cache some frequently evaluated properties to local variables.
                // The registration lists are not flushed during this method, so these are invariant.
                var grabbed = Interactable.IsSelected;
                var hasSingleGrabTransformer = m_SingleGrabTransformers.RegisteredSnapshot.Count > 0;
                var hasMultipleGrabTransformer = m_MultipleGrabTransformers.RegisteredSnapshot.Count > 0;

                // Let the transformers setup if the grab count changed.
                if (m_GrabCountChanged)
                {
                    if (grabbed)
                    {
                        if (hasSingleGrabTransformer)
                        {
                            foreach (var transformer in m_SingleGrabTransformers.RegisteredSnapshot)
                            {
                                if (m_SingleGrabTransformers.IsStillRegistered(transformer))
                                    transformer.OnGrabCountChanged(this, targetPose, localScale);
                            }
                        }

                        if (hasMultipleGrabTransformer)
                        {
                            foreach (var transformer in m_MultipleGrabTransformers.RegisteredSnapshot)
                            {
                                if (m_MultipleGrabTransformers.IsStillRegistered(transformer))
                                    transformer.OnGrabCountChanged(this, targetPose, localScale);
                            }
                        }
                    }

                    m_GrabCountChanged = false;
                    m_GrabTransformersAddedWhenGrabbed?.Clear();
                }
                else if (m_GrabTransformersAddedWhenGrabbed?.Count > 0)
                {
                    if (grabbed)
                    {
                        // Calling OnGrabCountChanged on just the grab transformers added when this was already grabbed
                        // avoids calling it needlessly on all linked grab transformers.
                        foreach (var transformer in m_GrabTransformersAddedWhenGrabbed)
                        {
                            transformer.OnGrabCountChanged(this, targetPose, localScale);
                        }
                    }

                    m_GrabTransformersAddedWhenGrabbed.Clear();
                }

                if (grabbed)
                {
                    // Give the Multiple Grab Transformers first chance to process,
                    // and if one actually does, skip the Single Grab Transformers.
                    // Also let the Multiple Grab Transformers process if there aren't any Single Grab Transformers.
                    // An empty Single Grab Transformers list is treated the same as a populated list where none can process.
                    var processed = false;
                    if (hasMultipleGrabTransformer && (Interactable.InteractorsSelecting.Count > 1 || !CanProcessAnySingleGrabTransformer()))
                    {
                        foreach (var transformer in m_MultipleGrabTransformers.RegisteredSnapshot)
                        {
                            if (!m_MultipleGrabTransformers.IsStillRegistered(transformer))
                                continue;

                            if (transformer.CanProcess)
                            {
                                transformer.Process(this, updatePhase, ref targetPose, ref localScale);
                                processed = true;
                            }
                        }
                    }

                    if (!processed && hasSingleGrabTransformer)
                    {
                        foreach (var transformer in m_SingleGrabTransformers.RegisteredSnapshot)
                        {
                            if (!m_SingleGrabTransformers.IsStillRegistered(transformer))
                                continue;

                            if (transformer.CanProcess)
                                transformer.Process(this, updatePhase, ref targetPose, ref localScale);
                        }
                    }
                }
                else
                {
                    // When not selected, we process both Multiple and Single transformers that implement IXRDropTransformer
                    // and do not try to recreate the logic of prioritizing Multiple over Single. The rules for prioritizing
                    // would not be intuitive, so we just process all transformers.
                    if (hasMultipleGrabTransformer)
                    {
                        foreach (var transformer in m_MultipleGrabTransformers.RegisteredSnapshot)
                        {
                            if (!(transformer is IXRDropTransformer dropTransformer) ||
                                !m_MultipleGrabTransformers.IsStillRegistered(transformer))
                            {
                                continue;
                            }

                            if (dropTransformer.CanProcessOnDrop && transformer.CanProcess)
                                transformer.Process(this, updatePhase, ref targetPose, ref localScale);
                        }
                    }

                    if (hasSingleGrabTransformer)
                    {
                        foreach (var transformer in m_SingleGrabTransformers.RegisteredSnapshot)
                        {
                            if (!(transformer is IXRDropTransformer dropTransformer) ||
                                !m_SingleGrabTransformers.IsStillRegistered(transformer))
                            {
                                continue;
                            }

                            if (dropTransformer.CanProcessOnDrop && transformer.CanProcess)
                                transformer.Process(this, updatePhase, ref targetPose, ref localScale);
                        }
                    }
                }
            }

            m_IsProcessingGrabTransformers = false;
        }

        /// <summary>
        /// Same check as Linq code for: <c>Any(t => t.canProcess)</c>.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if the source list is not empty and at least
        /// one element passes the test; otherwise, <see langword="false"/>.</returns>
        bool CanProcessAnySingleGrabTransformer()
        {
            if (m_SingleGrabTransformers.RegisteredSnapshot.Count > 0)
            {
                foreach (var transformer in m_SingleGrabTransformers.RegisteredSnapshot)
                {
                    if (!m_SingleGrabTransformers.IsStillRegistered(transformer))
                        continue;

                    if (transformer.CanProcess)
                        return true;
                }
            }

            return false;
        }

        void OnAddedGrabTransformer(IXRGrabTransformer transformer)
        {
            if (transformer is IXRDropTransformer)
                ++m_DropTransformersCount;

            transformer.OnLink(this);

            if (Interactable.InteractorsSelecting.Count == 0)
                return;

            // OnGrab is invoked immediately, but OnGrabCountChanged is only invoked right before Process so
            // it must be added to a list to maintain those that still need to have it invoked. It functions
            // like a setup method and users should be able to rely on it always being called at least once
            // when grabbed.
            transformer.OnGrab(this);

            if (m_GrabTransformersAddedWhenGrabbed == null)
                m_GrabTransformersAddedWhenGrabbed = new List<IXRGrabTransformer>();

            m_GrabTransformersAddedWhenGrabbed.Add(transformer);
        }

        void OnRemovedGrabTransformer(IXRGrabTransformer transformer)
        {
            if (transformer is IXRDropTransformer)
                --m_DropTransformersCount;

            transformer.OnUnlink(this);
            m_GrabTransformersAddedWhenGrabbed?.Remove(transformer);
        }

        void AddDefaultGrabTransformers()
        {
            if (!m_AddDefaultGrabTransformers)
                return;

            if (m_SingleGrabTransformers.FlushedCount == 0)
                AddDefaultSingleGrabTransformer();

            // Avoid adding the multiple grab transformer component unnecessarily since it may never be needed.
            if (m_MultipleGrabTransformers.FlushedCount == 0 && Interactable.SelectMode == InteractableSelectMode.Multiple && Interactable.InteractorsSelecting.Count > 1)
                AddDefaultMultipleGrabTransformer();
        }

        /// <summary>
        /// Adds the default <seealso cref="XRGeneralGrabTransformer"/> (if the Single or Multiple Grab Transformers lists are empty)
        /// to the list of transformers used when there is a single interactor selecting this object.
        /// </summary>
        /// <seealso cref="addDefaultGrabTransformers"/>
        protected virtual void AddDefaultSingleGrabTransformer()
        {
            if (m_SingleGrabTransformers.FlushedCount == 0)
            {
                var transformer = GetOrAddDefaultGrabTransformer();
                AddSingleGrabTransformer(transformer);
            }
        }

        /// <summary>
        /// Adds the default grab transformer (if the Multiple Grab Transformers list is empty)
        /// to the list of transformers used when there are multiple interactors selecting this object.
        /// </summary>
        /// <seealso cref="addDefaultGrabTransformers"/>
        protected virtual void AddDefaultMultipleGrabTransformer()
        {
            if (m_MultipleGrabTransformers.FlushedCount == 0)
            {
                var transformer = GetOrAddDefaultGrabTransformer();
                AddMultipleGrabTransformer(transformer);
            }
        }

        IXRGrabTransformer GetOrAddDefaultGrabTransformer()
        {
            return GetOrAddComponent<XRGeneralGrabTransformer>();
        }
        #endregion

        #region - Throwing -
        void ResetThrowSmoothing()
        {
            Array.Clear(m_ThrowSmoothingFrameTimes, 0, m_ThrowSmoothingFrameTimes.Length);
            Array.Clear(m_ThrowSmoothingVelocityFrames, 0, m_ThrowSmoothingVelocityFrames.Length);
            Array.Clear(m_ThrowSmoothingAngularVelocityFrames, 0, m_ThrowSmoothingAngularVelocityFrames.Length);
            m_ThrowSmoothingCurrentFrame = 0;
            m_ThrowSmoothingFirstUpdate = true;
        }

        void EndThrowSmoothing()
        {
            if (m_ThrowOnDetach)
            {
                // This can be potentially improved for multi-hand throws by ignoring the frames
                // after the first interactor releases if the second interactor also releases within
                // a short period of time. Since the target pose is tracked before easing, the most
                // recent frames might have been a large change.
                var smoothedVelocity = GetSmoothedVelocityValue(m_ThrowSmoothingVelocityFrames);
                var smoothedAngularVelocity = GetSmoothedVelocityValue(m_ThrowSmoothingAngularVelocityFrames);
                m_DetachVelocity = smoothedVelocity * m_ThrowVelocityScale;
                m_DetachAngularVelocity = smoothedAngularVelocity * m_ThrowAngularVelocityScale;
            }
        }

        void StepThrowSmoothing(Pose targetPose, float deltaTime)
        {
            // Skip velocity calculations if Time.deltaTime is too low due to a frame-timing issue on Quest
            if (deltaTime < DeltaTimeThreshold)
                return;

            if (m_ThrowSmoothingFirstUpdate)
            {
                m_ThrowSmoothingFirstUpdate = false;
            }
            else
            {
                m_ThrowSmoothingVelocityFrames[m_ThrowSmoothingCurrentFrame] = (targetPose.position - m_LastThrowReferencePose.position) / deltaTime;

                var rotationDiff = targetPose.rotation * Quaternion.Inverse(m_LastThrowReferencePose.rotation);
                var eulerAngles = rotationDiff.eulerAngles;
                var deltaAngles = new Vector3(Mathf.DeltaAngle(0f, eulerAngles.x),
                    Mathf.DeltaAngle(0f, eulerAngles.y),
                    Mathf.DeltaAngle(0f, eulerAngles.z));
                m_ThrowSmoothingAngularVelocityFrames[m_ThrowSmoothingCurrentFrame] = (deltaAngles / deltaTime) * Mathf.Deg2Rad;
            }

            m_ThrowSmoothingFrameTimes[m_ThrowSmoothingCurrentFrame] = Time.time;
            m_ThrowSmoothingCurrentFrame = (m_ThrowSmoothingCurrentFrame + 1) % ThrowSmoothingFrameCount;

            m_LastThrowReferencePose = targetPose;
        }

        Vector3 GetSmoothedVelocityValue(Vector3[] velocityFrames)
        {
            var calcVelocity = Vector3.zero;
            var totalWeights = 0f;
            for (var frameCounter = 0; frameCounter < ThrowSmoothingFrameCount; ++frameCounter)
            {
                var frameIdx = (((m_ThrowSmoothingCurrentFrame - frameCounter - 1) % ThrowSmoothingFrameCount) + ThrowSmoothingFrameCount) % ThrowSmoothingFrameCount;
                if (m_ThrowSmoothingFrameTimes[frameIdx] == 0f)
                    break;

                var timeAlpha = (Time.time - m_ThrowSmoothingFrameTimes[frameIdx]) / m_ThrowSmoothingDuration;
                var velocityWeight = m_ThrowSmoothingCurve.Evaluate(Mathf.Clamp(1f - timeAlpha, 0f, 1f));
                calcVelocity += velocityFrames[frameIdx] * velocityWeight;
                totalWeights += velocityWeight;
                if (Time.time - m_ThrowSmoothingFrameTimes[frameIdx] > m_ThrowSmoothingDuration)
                    break;
            }

            if (totalWeights > 0f)
                return calcVelocity / totalWeights;

            return Vector3.zero;
        }
        #endregion

        #region - Posing -
        /// <inheritdoc />
        public Transform GetAttachTransform(IAttachPoint attachPoint)
        {
            bool isFirst = Interactable.InteractorsSelecting.Count <= 1 || ReferenceEquals(attachPoint, Interactable.InteractorsSelecting[0]);

            // If first selector, do normal behavior.
            // If second, we ignore dynamic attach setting if there is no secondary attach transform.
            var shouldUseDynamicAttach = m_UseDynamicAttach || (!isFirst && m_SecondaryAttachTransform == null);

            if (shouldUseDynamicAttach && attachPoint is Interactor selectInteractor &&
                m_DynamicAttachTransforms.TryGetValue(selectInteractor, out var dynamicAttachTransform))
            {
                if (dynamicAttachTransform != null)
                    return dynamicAttachTransform;

                m_DynamicAttachTransforms.Remove(selectInteractor);
                Debug.LogWarning($"Dynamic Attach Transform created by {this} for {attachPoint} was destroyed after being created." +
                                 " Continuing as if Use Dynamic Attach was disabled for this pair.", this);
            }

            // If not first, and not using dynamic attach, then we must have a secondary attach transform set.
            if (!isFirst && !shouldUseDynamicAttach)
            {
                return m_SecondaryAttachTransform;
            }

            return m_AttachTransform != null ? m_AttachTransform : Interactable.GetAttachTransform(attachPoint);
        }

        /// <summary>
        /// Retrieves the current world space target pose.
        /// </summary>
        /// <returns>Returns the current world space target pose in the form of a <see cref="Pose"/> struct.</returns>
        /// <seealso cref="SetTargetPose"/>
        /// <seealso cref="GetTargetLocalScale"/>
        public Pose GetTargetPose()
        {
            return m_TargetPose;
        }

        /// <summary>
        /// Sets a new world space target pose.
        /// </summary>
        /// <param name="pose">The new world space target pose, represented as a <see cref="Pose"/> struct.</param>
        /// <remarks>
        /// This bypasses easing and smoothing.
        /// </remarks>
        /// <seealso cref="GetTargetPose"/>
        /// <seealso cref="SetTargetLocalScale"/>
        public void SetTargetPose(Pose pose)
        {
            m_TargetPose = pose;

            // If there are no interactors selecting this object, we need to set the target pose dirty
            // so that the pose is applied in the next phase it is applied.
            m_IsTargetPoseDirty = Interactable.InteractorsSelecting.Count == 0;
        }

        /// <summary>
        /// Unity calls this method automatically when initializing the dynamic attach pose.
        /// Used to override <see cref="SnapToColliderVolume"/> for a specific interactor.
        /// </summary>
        /// <param name="interactor">The interactor that is initiating the selection.</param>
        /// <returns>Returns whether to adjust the dynamic attachment point to keep it on or inside the Colliders that make up this object.</returns>
        /// <seealso cref="SnapToColliderVolume"/>
        /// <seealso cref="InitializeDynamicAttachPose"/>
        protected virtual bool ShouldSnapToColliderVolume(Interactor interactor)
        {
            return m_SnapToColliderVolume;
        }

        /// <summary>
        /// Unity calls this method automatically when the interactor first initiates selection of this interactable.
        /// Override this method to set the pose of the dynamic attachment point. Before this method is called, the transform
        /// is already set as a child GameObject with inherited Transform values.
        /// </summary>
        /// <param name="interactor">The interactor that is initiating the selection.</param>
        /// <param name="dynamicAttachTransform">The dynamic attachment Transform that serves as the attachment point for the given interactor.</param>
        /// <remarks>
        /// This method is only called when <see cref="UseDynamicAttach"/> is enabled.
        /// </remarks>
        /// <seealso cref="UseDynamicAttach"/>
        protected virtual void InitializeDynamicAttachPose(Interactor interactor, Transform dynamicAttachTransform)
        {
            var matchPosition = ShouldMatchAttachPosition(interactor);
            var matchRotation = ShouldMatchAttachRotation(interactor);
            if (!matchPosition && !matchRotation)
                return;

            // Copy the pose of the interactor's attach transform
            var interactorAttachTransform = interactor.GetAttachTransform(Interactable);
            var position = interactorAttachTransform.position;
            var rotation = interactorAttachTransform.rotation;

            // Optionally constrain the position to within the Collider(s) of this Interactable
            if (matchPosition && ShouldSnapToColliderVolume(interactor) &&
                XRInteractableUtility.TryGetClosestPointOnCollider(Interactable, position, out var distanceInfo))
            {
                position = distanceInfo.point;
            }

            if (matchPosition && matchRotation)
                dynamicAttachTransform.SetPositionAndRotation(position, rotation);
            else if (matchPosition)
                dynamicAttachTransform.position = position;
            else
                dynamicAttachTransform.rotation = rotation;
        }

        Transform CreateDynamicAttachTransform(Interaction.IInteractor interactor)
        {
            Transform dynamicAttachTransform;

            do
            {
                dynamicAttachTransform = s_DynamicAttachTransformPool.Get();
            } while (dynamicAttachTransform == null);

#if UNITY_EDITOR
            dynamicAttachTransform.name = $"[{interactor.transform.name}] Dynamic Attach";
#endif
            dynamicAttachTransform.SetParent(transform, false);

            return dynamicAttachTransform;
        }

        void InitializeDynamicAttachPoseInternal(Interactor interactor, Transform dynamicAttachTransform)
        {
            // InitializeDynamicAttachPose expects it to be initialized with the static pose first
            InitializeDynamicAttachPoseWithStatic(interactor, dynamicAttachTransform);
            InitializeDynamicAttachPose(interactor, dynamicAttachTransform);
        }

        void InitializeDynamicAttachPoseWithStatic(Interactor interactor, Transform dynamicAttachTransform)
        {
            m_DynamicAttachTransforms.Remove(interactor);
            var staticAttachTransform = GetAttachTransform(interactor);
            m_DynamicAttachTransforms[interactor] = dynamicAttachTransform;

            // Base the initial pose on the Attach Transform.
            // Technically we could just do the final else statement, but setting the local position and rotation this way
            // keeps the position and rotation seen in the Inspector tidier by exactly matching instead of potentially having small
            // floating point offsets.
            if (staticAttachTransform == transform)
            {
                dynamicAttachTransform.localPosition = Vector3.zero;
                dynamicAttachTransform.localRotation = Quaternion.identity;
            }
            else if (staticAttachTransform.parent == transform)
            {
                dynamicAttachTransform.localPosition = staticAttachTransform.localPosition;
                dynamicAttachTransform.localRotation = staticAttachTransform.localRotation;
            }
            else
            {
                dynamicAttachTransform.SetPositionAndRotation(staticAttachTransform.position, staticAttachTransform.rotation);
            }
        }

        void ReleaseDynamicAttachTransform(Interactor interactor)
        {
            // Skip checking m_UseDynamicAttach since it may have changed after being grabbed,
            // and we should ensure it is released. We instead check Count first as a faster way to avoid hashing
            // and the Dictionary lookup, which should handle when it was never enabled in the first place.
            if (m_DynamicAttachTransforms.Count > 0 && m_DynamicAttachTransforms.TryGetValue(interactor, out var dynamicAttachTransform))
            {
                if (dynamicAttachTransform != null)
                    s_DynamicAttachTransformPool.Release(dynamicAttachTransform);

                m_DynamicAttachTransforms.Remove(interactor);
            }
        }

        /// <summary>
        /// Unity calls this method automatically when initializing the dynamic attach pose.
        /// Used to override <see cref="MatchAttachPosition"/> for a specific interactor.
        /// </summary>
        /// <param name="interactor">The interactor that is initiating the selection.</param>
        /// <returns>Returns whether to match the position of the interactor's attachment point when initializing the grab.</returns>
        /// <seealso cref="MatchAttachPosition"/>
        /// <seealso cref="InitializeDynamicAttachPose"/>
        protected virtual bool ShouldMatchAttachPosition(Interactor interactor)
        {
            if (!m_MatchAttachPosition)
                return false;

            // We assume the static pose should always be used for sockets.
            // For Ray Interactors that bring the object to hand (Force Grab enabled), we assume that property
            // takes precedence since otherwise this interactable wouldn't move if we copied the interactor's attach position,
            // which would violate the interactor's expected behavior.
            if (interactor.HasModule<SocketInteractorModule>() ||
                interactor.TryGetModule<GrabInteractorModule>(out var grabInteractor) && grabInteractor.DistantGrabActive)
                return false;

            return true;
        }

        /// <summary>
        /// Unity calls this method automatically when initializing the dynamic attach pose.
        /// Used to override <see cref="MatchAttachRotation"/> for a specific interactor.
        /// </summary>
        /// <param name="interactor">The interactor that is initiating the selection.</param>
        /// <returns>Returns whether to match the rotation of the interactor's attachment point when initializing the grab.</returns>
        /// <seealso cref="MatchAttachRotation"/>
        /// <seealso cref="InitializeDynamicAttachPose"/>
        protected virtual bool ShouldMatchAttachRotation(Interactor interactor)
        {
            // We assume the static pose should always be used for sockets.
            // Unlike for position, we allow a Ray Interactor with Force Grab enabled to match the rotation
            // based on the property in this behavior.
            return m_MatchAttachRotation && !(interactor.HasModule<SocketInteractorModule>());
        }
        #endregion

        #region - Teleporting -
        void SubscribeTeleportationProvider(Interactor interactor)
        {
            m_TeleportationMonitor.AddInteractor(interactor);
        }

        void UnsubscribeTeleportationProvider(Interactor interactor)
        {
            m_TeleportationMonitor.RemoveInteractor(interactor);
        }

        void OnTeleported(Pose offset)
        {
            var translated = offset.position;
            var rotated = offset.rotation;

            for (var frameIdx = 0; frameIdx < ThrowSmoothingFrameCount; ++frameIdx)
            {
                if (m_ThrowSmoothingFrameTimes[frameIdx] == 0f)
                    break;

                m_ThrowSmoothingVelocityFrames[frameIdx] = rotated * m_ThrowSmoothingVelocityFrames[frameIdx];
            }

            m_LastThrowReferencePose.position += translated;
            m_LastThrowReferencePose.rotation = rotated * m_LastThrowReferencePose.rotation;
        }
        #endregion

        #region - Helpers -
        /// <summary>
        /// Retrieves the current target local scale.
        /// </summary>
        /// <returns>Returns the current target local scale in the form of a <see cref="Vector3"/> struct.</returns>
        /// <seealso cref="SetTargetLocalScale"/>
        /// <seealso cref="GetTargetPose"/>
        public Vector3 GetTargetLocalScale()
        {
            return m_TargetLocalScale;
        }

        /// <summary>
        /// Sets a new target local scale.
        /// </summary>
        /// <param name="localScale">The new target local scale, represented as a <see cref="Vector3"/> struct.</param>
        /// <remarks>
        /// This bypasses easing and smoothing.
        /// </remarks>
        /// <seealso cref="GetTargetLocalScale"/>
        /// <seealso cref="SetTargetPose"/>
        public void SetTargetLocalScale(Vector3 localScale)
        {
            m_TargetLocalScale = localScale;

            // If there are no interactors selecting this object, we need to set the target local scale dirty
            // so that the pose is applied in the next phase it is applied.
            m_IsTargetLocalScaleDirty = Interactable.InteractorsSelecting.Count == 0;
        }

        void InitializeTargetPoseAndScale(Transform thisTransform)
        {
            m_TargetPose.position = thisTransform.position;
            m_TargetPose.rotation = thisTransform.rotation;
            m_TargetLocalScale = thisTransform.localScale;
        }

        T GetOrAddComponent<T>() where T : Component
        {
            return TryGetComponent<T>(out var component) ? component : gameObject.AddComponent<T>();
        }

        [BurstCompile]
        static void EaseAttachBurst(ref Pose targetPose, ref Vector3 targetLocalScale, in Pose rawTargetPose, in Vector3 rawTargetLocalScale, float deltaTime,
            float attachEaseInTime, ref float currentAttachEaseTime)
        {
            var easePercent = currentAttachEaseTime / attachEaseInTime;
            targetPose.position = math.lerp(targetPose.position, rawTargetPose.position, easePercent);
            targetPose.rotation = math.slerp(targetPose.rotation, rawTargetPose.rotation, easePercent);
            targetLocalScale = math.lerp(targetLocalScale, rawTargetLocalScale, easePercent);
            currentAttachEaseTime += deltaTime;
        }

        [BurstCompile]
        static void StepSmoothingBurst(ref Pose targetPose, ref Vector3 targetLocalScale, in Pose rawTargetPose, in Vector3 rawTargetLocalScale, float deltaTime,
            bool smoothPos, float smoothPosAmount, float tightenPos,
            bool smoothRot, float smoothRotAmount, float tightenRot,
            bool smoothScale, float smoothScaleAmount, float tightenScale)
        {
            if (smoothPos)
            {
                targetPose.position = math.lerp(targetPose.position, rawTargetPose.position, smoothPosAmount * deltaTime);
                targetPose.position = math.lerp(targetPose.position, rawTargetPose.position, tightenPos);
            }
            else
            {
                targetPose.position = rawTargetPose.position;
            }

            if (smoothRot)
            {
                targetPose.rotation = math.slerp(targetPose.rotation, rawTargetPose.rotation, smoothRotAmount * deltaTime);
                targetPose.rotation = math.slerp(targetPose.rotation, rawTargetPose.rotation, tightenRot);
            }
            else
            {
                targetPose.rotation = rawTargetPose.rotation;
            }

            if (smoothScale)
            {
                targetLocalScale = math.lerp(targetLocalScale, rawTargetLocalScale, smoothScaleAmount * deltaTime);
                targetLocalScale = math.lerp(targetLocalScale, rawTargetLocalScale, tightenScale);
            }
            else
            {
                targetLocalScale = rawTargetLocalScale;
            }
        }

        private void ApplyTargetScale()
        {
            if (m_TrackScale)
                transform.localScale = m_TargetLocalScale;

            m_IsTargetLocalScaleDirty = false;
        }

        private void UpdateCurrentMovementType()
        {
            // Special case where the interactor will override this objects movement type (used for Sockets and other absolute interactors).
            // Iterates in reverse order so the most recent interactor with an override will win since that seems like it would
            // be the strategy most users would want by default.
            MovementType? movementTypeOverride = null;
            for (var index = Interactable.InteractorsSelecting.Count - 1; index >= 0; --index)
            {
                var baseInteractor = Interactable.InteractorsSelecting[index];
                if (baseInteractor != null && baseInteractor.SelectedInteractableMovementTypeOverride != null)
                {
                    if (movementTypeOverride.HasValue)
                    {
                        Debug.LogWarning($"Multiple interactors selecting \"{name}\" have different movement type override values set" +
                                         $" ({nameof(Interactor.SelectedInteractableMovementTypeOverride)})." +
                                         $" Conflict resolved using {movementTypeOverride.Value} from the most recent interactor to select this object with an override.", this);
                        break;
                    }

                    movementTypeOverride = baseInteractor.SelectedInteractableMovementTypeOverride.Invoke();
                }
            }

            m_CurrentMovementType = movementTypeOverride ?? m_MovementType;
        }

        static Transform OnCreatePooledItem()
        {
            var item = new GameObject().transform;
            item.localPosition = Vector3.zero;
            item.localRotation = Quaternion.identity;
            item.localScale = Vector3.one;

            return item;
        }

        static void OnGetPooledItem(Transform item)
        {
            if (item == null)
                return;

            item.hideFlags &= ~HideFlags.HideInHierarchy;
        }

        static void OnReleasePooledItem(Transform item)
        {
            if (item == null)
                return;

            // Don't clear the parent of the GameObject on release since there could be issues
            // with changing it while a parent GameObject is deactivating, which logs an error.
            // By keeping it under this interactable, it could mean that GameObjects in the pool
            // have a chance of being destroyed, but we check that the GameObject we obtain from the pool
            // has not been destroyed. This means potentially more creations of new GameObjects, but avoids
            // the issue with reparenting.

            // Hide the GameObject in the Hierarchy so it doesn't pollute this Interactable's hierarchy
            // when it is no longer used.
            item.hideFlags |= HideFlags.HideInHierarchy;
        }

        static void OnDestroyPooledItem(Transform item)
        {
            if (item == null)
                return;

            Destroy(item.gameObject);
        }
        #endregion
    }
}
