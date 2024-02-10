using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using VaporInspector;
using VaporXR.Utilities;

namespace VaporXR
{
    /// <summary>
    /// Interactor used for holding interactables via a socket.
    /// </summary>
    /// <remarks>
    /// A socket is defined as the target for a specific interactable, such as a keyhole for a key
    /// or a battery socket for a battery. Not to be confused with network programming.
    /// <br />
    /// This component is not designed to use input (thus does not derive from <see cref="XRBaseInputInteractor"/>)
    /// and instead will always attempt to select an interactable that it is hovering over.
    /// </remarks>
    [DisallowMultipleComponent]
    public class VXRSocketInteractor : VXRBaseInteractor
    {
        private readonly struct ShaderPropertyLookup
        {
            public static readonly int surface = Shader.PropertyToID("_Surface");
            public static readonly int mode = Shader.PropertyToID("_Mode");
            public static readonly int srcBlend = Shader.PropertyToID("_SrcBlend");
            public static readonly int dstBlend = Shader.PropertyToID("_DstBlend");
            public static readonly int zWrite = Shader.PropertyToID("_ZWrite");
            public static readonly int baseColor = Shader.PropertyToID("_BaseColor");
            public static readonly int color = Shader.PropertyToID("_Color"); // Legacy
        }

        /// <summary>
        /// Reusable list of type <see cref="MeshFilter"/> to reduce allocations.
        /// </summary>
        private static readonly List<MeshFilter> s_MeshFilters = new();

        /// <summary>
        /// Reusable value of <see cref="WaitForFixedUpdate"/> to reduce allocations.
        /// </summary>
        private static readonly WaitForFixedUpdate s_WaitForFixedUpdate = new();

        #region Inspector
        [FoldoutGroup("Socket"), SerializeField]
        [RichTextTooltip("Whether socket interaction is enabled.")]
        private bool _socketActive = true;
        [FoldoutGroup("Socket"), SerializeField]
        [RichTextTooltip("Sets the amount of time the socket will refuse hovers after an object is removed.")]
        private float _recycleDelayTime = 1f;
        [FoldoutGroup("Socket"), SerializeField]
        [RichTextTooltip("Determines if the interactable should snap to the socket's attach transform when hovering." +
            "Note this will cause z-fighting with the hover mesh visuals, so it is recommended to disable <mth>ShowInteractableHoverMeshes</mth> if this is active." +
            "If enabled, hover recycle delay functionality is disabled to prevent snap flickering.")]
        private bool _hoverSocketSnapping;
        [FoldoutGroup("Socket"), SerializeField]        
        [RichTextTooltip("When socket snapping is enabled, this is the radius within which the interactable will snap to the socket's attach transform while hovering.")]
        private float _socketSnappingRadius = 0.1f;
        [FoldoutGroup("Socket"), SerializeField]
        [RichTextTooltip("Scale mode used to calculate the scale factor applied to the interactable when hovering.")]
        private SocketScaleMode _socketScaleMode = SocketScaleMode.None;
        [FoldoutGroup("Socket"), SerializeField]
        [RichTextTooltip("Scale factor applied to the interactable when scale mode is set to <itf>SocketScaleMode</itf><mth>.Fixed</mth>.")]
        private Vector3 _fixedScale = Vector3.one;
        [FoldoutGroup("Socket"), SerializeField]
        [RichTextTooltip("Bounds size used to calculate the scale factor applied to the interactable when scale mode is set to <itf>SocketScaleMode</itf><mth>.StretchedToFitSize</mth>.")]
        private Vector3 _targetBoundsSize = Vector3.one;

        [FoldoutGroup("Visuals"), SerializeField]
        [RichTextTooltip("Whether this socket should show a mesh at socket's attach point for Interactables that are hovering it.")]
        private bool _showInteractableHoverMeshes = true;

        [FoldoutGroup("Visuals"), SerializeField]
        [RichTextTooltip("Material used for rendering interactable meshes on hover\n (a default material will be created if none is supplied).")]
        private Material _interactableHoverMeshMaterial;
        [FoldoutGroup("Visuals"), SerializeField]
        [RichTextTooltip("Material used for rendering interactable meshes on hover when there is already a selected object in the socket\n (a default material will be created if none is supplied).")]
        private Material _interactableCantHoverMeshMaterial;

        [FoldoutGroup("Visuals"), SerializeField]
        [RichTextTooltip("Scale at which to render hovered Interactable.")]
        private float _interactableHoverScale = 1f;
        #endregion

        #region Properties
        /// <summary>
        /// Whether socket interaction is enabled.
        /// </summary>
        public bool SocketActive
        {
            get => _socketActive;
            set
            {
                _socketActive = value;
                m_SocketGrabTransformer.canProcess = value && isActiveAndEnabled;
            }
        }

        /// <summary>
        /// Sets the amount of time the socket will refuse hovers after an object is removed.
        /// </summary>
        /// <remarks>
        /// Does nothing if <see cref="HoverSocketSnapping"/> is enabled to prevent snap flickering.
        /// </remarks>
        public float RecycleDelayTime { get => _recycleDelayTime; set => _recycleDelayTime = value; }

        /// <summary>
        /// Determines if the interactable should snap to the socket's attach transform when hovering.
        /// Note this will cause z-fighting with the hover mesh visuals, so it is recommended to disable <see cref="ShowInteractableHoverMeshes"/> if this is active.
        /// If enabled, hover recycle delay functionality is disabled to prevent snap flickering.
        /// </summary>
        public bool HoverSocketSnapping { get => _hoverSocketSnapping; set => _hoverSocketSnapping = value; }

        /// <summary>
        /// When socket snapping is enabled, this is the radius within which the interactable will snap to the socket's attach transform while hovering.
        /// </summary>
        public float socketSnappingRadius
        {
            get => _socketSnappingRadius;
            set
            {
                _socketSnappingRadius = value;
                m_SocketGrabTransformer.socketSnappingRadius = value;
            }
        }

        /// <summary>
        /// Scale mode used to calculate the scale factor applied to the interactable when hovering.
        /// </summary>
        /// <seealso cref="SocketScaleMode"/>
        public SocketScaleMode socketScaleMode
        {
            get => _socketScaleMode;
            set
            {
                _socketScaleMode = value;
                m_SocketGrabTransformer.scaleMode = value;
            }
        }

        /// <summary>
        /// Scale factor applied to the interactable when scale mode is set to <see cref="SocketScaleMode.Fixed"/>.
        /// </summary>
        /// <seealso cref="socketScaleMode"/>
        public Vector3 fixedScale
        {
            get => _fixedScale;
            set
            {
                _fixedScale = value;
                m_SocketGrabTransformer.fixedScale = value;
            }
        }

        /// <summary>
        /// Bounds size used to calculate the scale factor applied to the interactable when scale mode is set to <see cref="SocketScaleMode.StretchedToFitSize"/>.
        /// </summary>
        /// <seealso cref="socketScaleMode"/>
        public Vector3 targetBoundsSize
        {
            get => _targetBoundsSize;
            set
            {
                _targetBoundsSize = value;
                m_SocketGrabTransformer.targetBoundsSize = value;
            }
        }

        /// <summary>
        /// Whether this socket should show a mesh at socket's attach point for Interactables that are hovering it.
        /// </summary>
        /// <remarks>
        /// The interactable's attach transform must not change parent Transform while selected
        /// for the position and rotation of the hover mesh to be correctly calculated.
        /// </remarks>
        public bool ShowInteractableHoverMeshes { get => _showInteractableHoverMeshes; set => _showInteractableHoverMeshes = value; }

        /// <summary>
        /// Material used for rendering interactable meshes on hover
        /// (a default material will be created if none is supplied).
        /// </summary>
        public Material InteractableHoverMeshMaterial { get => _interactableHoverMeshMaterial; set => _interactableHoverMeshMaterial = value; }

        /// <summary>
        /// Material used for rendering interactable meshes on hover when there is already a selected object in the socket
        /// (a default material will be created if none is supplied).
        /// </summary>
        public Material InteractableCantHoverMeshMaterial { get => _interactableCantHoverMeshMaterial; set => _interactableCantHoverMeshMaterial = value; }

        /// <summary>
        /// Scale at which to render hovered Interactable.
        /// </summary>
        public float InteractableHoverScale { get => _interactableHoverScale; set => _interactableHoverScale = value; }

        public override bool IsHoverActive => base.IsHoverActive && _socketActive;

        public override bool IsSelectActive => base.IsSelectActive && _socketActive;

        public override VXRBaseInteractable.MovementType? SelectedInteractableMovementTypeOverride => VXRBaseInteractable.MovementType.Instantaneous;

        // ***** Internal *****
        /// <summary>
        /// The set of Interactables that this Interactor could possibly interact with this frame.
        /// This list is not sorted by priority.
        /// </summary>
        /// <seealso cref="IXRInteractor.GetValidTargets"/>
        protected List<IXRInteractable> unsortedValidTargets { get; } = new List<IXRInteractable>();

        /// <summary>
        /// Maximum number of interactables this interactor can socket.
        /// Used for hover socket snapping evaluation.
        /// </summary>
        protected virtual int socketSnappingLimit => 1;

        /// <summary>
        /// Determines if when snapping to a socket, any existing sockets should be ejected.
        /// </summary>
        protected virtual bool ejectExistingSocketsWhenSnapping => true;

        private bool IsHoverRecycleAllowed => _hoverSocketSnapping || (m_LastRemoveTime < 0f || _recycleDelayTime <= 0f || (Time.time > m_LastRemoveTime + _recycleDelayTime));
        #endregion

        #region Fields
        /// <summary>
        /// The set of Colliders that stayed in touch with this Interactor on fixed updated.
        /// This list will be populated by colliders in OnTriggerStay.
        /// </summary>
        private readonly HashSet<Collider> m_StayedColliders = new();
        private readonly TriggerContactMonitor m_TriggerContactMonitor = new();
        private readonly Dictionary<IXRInteractable, ValueTuple<MeshFilter, Renderer>[]> m_MeshFilterCache = new();
        readonly XRSocketGrabTransformer m_SocketGrabTransformer = new();
        readonly HashSetList<VXRGrabInteractable> m_InteractablesWithSocketTransformer = new();

        private float m_LastRemoveTime = -1f;

        /// <summary>
        /// Reference to Coroutine that updates the trigger contact monitor with the current
        /// stayed colliders.
        /// </summary>
        IEnumerator m_UpdateCollidersAfterTriggerStay;
        #endregion

        #region - Initialization -
        protected virtual void OnValidate()
        {
            SyncTransformerParams();
        }

        protected override void Awake()
        {
            base.Awake();
            m_TriggerContactMonitor.interactionManager = InteractionManager;
            m_UpdateCollidersAfterTriggerStay = UpdateCollidersAfterOnTriggerStay();

            SyncTransformerParams();
            CreateDefaultHoverMaterials();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_TriggerContactMonitor.contactAdded += OnContactAdded;
            m_TriggerContactMonitor.contactRemoved += OnContactRemoved;
            m_SocketGrabTransformer.canProcess = _socketActive;
            SyncTransformerParams();
            ResetCollidersAndValidTargets();
            StartCoroutine(m_UpdateCollidersAfterTriggerStay);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_SocketGrabTransformer.canProcess = false;
            m_TriggerContactMonitor.contactAdded -= OnContactAdded;
            m_TriggerContactMonitor.contactRemoved -= OnContactRemoved;
            ResetCollidersAndValidTargets();
            StopCoroutine(m_UpdateCollidersAfterTriggerStay);
        }

        private void SyncTransformerParams()
        {
            m_SocketGrabTransformer.socketInteractor = this;
            m_SocketGrabTransformer.socketSnappingRadius = socketSnappingRadius;
            m_SocketGrabTransformer.scaleMode = socketScaleMode;
            m_SocketGrabTransformer.fixedScale = fixedScale;
            m_SocketGrabTransformer.targetBoundsSize = targetBoundsSize;
        }

        public override void OnRegistered(InteractorRegisteredEventArgs args)
        {
            base.OnRegistered(args);
            args.manager.interactableRegistered += OnInteractableRegistered;
            args.manager.interactableUnregistered += OnInteractableUnregistered;

            // Attempt to resolve any colliders that entered this trigger while this was not subscribed,
            // and filter out any targets that were unregistered while this was not subscribed.
            m_TriggerContactMonitor.interactionManager = args.manager;
            m_TriggerContactMonitor.ResolveUnassociatedColliders();
            VXRInteractionManager.RemoveAllUnregistered(args.manager, unsortedValidTargets);
        }

        public override void OnUnregistered(InteractorUnregisteredEventArgs args)
        {
            base.OnUnregistered(args);
            args.manager.interactableRegistered -= OnInteractableRegistered;
            args.manager.interactableUnregistered -= OnInteractableUnregistered;
        }

        private void OnInteractableRegistered(InteractableRegisteredEventArgs args)
        {
            m_TriggerContactMonitor.ResolveUnassociatedColliders(args.interactableObject);
            if (m_TriggerContactMonitor.IsContacting(args.interactableObject) && !unsortedValidTargets.Contains(args.interactableObject))
            {
                unsortedValidTargets.Add(args.interactableObject);
            }
        }

        private void OnInteractableUnregistered(InteractableUnregisteredEventArgs args)
        {
            unsortedValidTargets.Remove(args.interactableObject);
        }
        #endregion

        #region - Processing -
        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractor(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Fixed)
            {
                // Clear stayed Colliders at the beginning of the physics cycle before
                // the OnTriggerStay method populates this list.
                // Then the UpdateCollidersAfterOnTriggerStay coroutine will use this list to remove Colliders
                // that no longer stay in this frame after previously entered and add any stayed Colliders
                // that are not currently tracked by the TriggerContactMonitor.
                m_StayedColliders.Clear();
            }
            else if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                // An explicit check for isHoverRecycleAllowed is done since an interactable may have been deselected
                // after this socket was updated by the manager, such as when a later Interactor takes the selection
                // from this socket. The recycle delay time could cause the hover to be effectively disabled.
                if (_showInteractableHoverMeshes && HasHover && IsHoverRecycleAllowed)
                {
                    DrawHoveredInteractables();
                }
            }
        }

        /// <summary>
        /// This coroutine functions like a LateFixedUpdate method that executes after OnTriggerXXX.
        /// </summary>
        /// <returns>Returns enumerator for coroutine.</returns>
        private IEnumerator UpdateCollidersAfterOnTriggerStay()
        {
            while (true)
            {
                // Wait until the end of the physics cycle so that OnTriggerXXX can get called.
                // See https://docs.unity3d.com/Manual/ExecutionOrder.html
                yield return s_WaitForFixedUpdate;

                m_TriggerContactMonitor.UpdateStayedColliders(m_StayedColliders);
            }
            // ReSharper disable once IteratorNeverReturns -- stopped when behavior is destroyed.
        }
        #endregion

        #region - Contacts -
        protected void OnTriggerEnter(Collider other)
        {
            m_TriggerContactMonitor.AddCollider(other);
        }

        protected void OnTriggerStay(Collider other)
        {
            m_StayedColliders.Add(other);
        }

        protected void OnTriggerExit(Collider other)
        {
            m_TriggerContactMonitor.RemoveCollider(other);
        }

        private void OnContactAdded(IXRInteractable interactable)
        {
            if (!unsortedValidTargets.Contains(interactable))
            {
                unsortedValidTargets.Add(interactable);
            }
        }

        private void OnContactRemoved(IXRInteractable interactable)
        {
            unsortedValidTargets.Remove(interactable);
        }

        /// <summary>
        /// Clears current valid targets and stayed colliders.
        /// </summary>
        private void ResetCollidersAndValidTargets()
        {
            unsortedValidTargets.Clear();
            m_StayedColliders.Clear();
            m_TriggerContactMonitor.UpdateStayedColliders(m_StayedColliders);
        }
        #endregion

        #region - Interaction -
        /// <inheritdoc />
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            targets.Clear();

            if (!isActiveAndEnabled)
            {
                return;
            }

            var filter = TargetFilter;
            if (filter != null && filter.canProcess)
            {
                filter.Process(this, unsortedValidTargets, targets);
            }
            else
            {
                SortingHelpers.SortByDistanceToInteractor(this, unsortedValidTargets, targets);
            }
        }
        #endregion

        #region - Hover -
        public override bool CanHover(IXRHoverInteractable interactable)
        {
            return base.CanHover(interactable) && IsHoverRecycleAllowed;
        }

        /// <summary>
        /// Determines whether the specified <see cref="IXRInteractable"/> object can hover snap.
        /// </summary>
        /// <param name="interactable">The <see cref="IXRInteractable"/> object to check for hover snap capability.</param>
        /// <returns>Returns <see langword="true"/> if hover socket snapping is enabled and the interactable has no selection or is selecting; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This method checks whether hover socket snapping is allowed and whether the specified interactable has no current selection or is in the process of selecting.
        /// </remarks>
        protected virtual bool CanHoverSnap(IXRInteractable interactable)
        {
            return _hoverSocketSnapping && (!HasSelection || IsSelecting(interactable));
        }

        public override void OnHoverEntering(HoverEnterEventArgs args)
        {
            base.OnHoverEntering(args);

            // Avoid the performance cost of GetComponents if we don't need to show the hover meshes.
            if (!_showInteractableHoverMeshes)
                return;

            var interactable = args.interactableObject;
            s_MeshFilters.Clear();
            interactable.transform.GetComponentsInChildren(true, s_MeshFilters);
            if (s_MeshFilters.Count == 0)
            {
                return;
            }

            var interactableTuples = new ValueTuple<MeshFilter, Renderer>[s_MeshFilters.Count];
            for (var i = 0; i < s_MeshFilters.Count; ++i)
            {
                var meshFilter = s_MeshFilters[i];
                interactableTuples[i] = (meshFilter, meshFilter.GetComponent<Renderer>());
            }
            m_MeshFilterCache.Add(interactable, interactableTuples);
        }

        public override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);

            if (!CanHoverSnap(args.interactableObject))
            {
                return;
            }

            if (args.interactableObject is VXRGrabInteractable grabInteractable)
            {
                StartSocketSnapping(grabInteractable);
            }
        }

        public override void OnHoverExiting(HoverExitEventArgs args)
        {
            base.OnHoverExiting(args);

            var interactable = args.interactableObject;
            m_MeshFilterCache.Remove(interactable);

            if (interactable is VXRGrabInteractable grabInteractable)
            {
                EndSocketSnapping(grabInteractable);
            }
        }
        #endregion

        #region - Select -
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return base.CanSelect(interactable) &&
                ((!HasSelection && !interactable.IsSelected) ||
                    (IsSelecting(interactable) && interactable.InteractorsSelecting.Count == 1));
        }

        public override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            if (args.interactableObject is VXRGrabInteractable grabInteractable)
            {
                StartSocketSnapping(grabInteractable);
            }
        }

        public override void OnSelectExiting(SelectExitEventArgs args)
        {
            base.OnSelectExiting(args);
            m_LastRemoveTime = Time.time;
        }

        public override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);

            if (_hoverSocketSnapping)
            {
                return;
            }

            if (args.interactableObject is VXRGrabInteractable grabInteractable)
            {
                EndSocketSnapping(grabInteractable);
            }
        }
        #endregion

        #region - Socket -
        /// <summary>
        /// Initiates socket snapping for a specified <see cref="XRGrabInteractable"/> object.
        /// </summary>
        /// <param name="grabInteractable">The <see cref="XRGrabInteractable"/> object to initiate socket snapping for.</param>
        /// <returns>Returns <see langword="true"/> if the operation is successful; false if the socket snapping has already started for the interactable or if the number of interactables with socket transformer exceeds the socket limit.</returns>
        /// <remarks>
        /// If the socket snapping has already started for the interactable, or if the number of interactables with socket transformer exceeds the socket limit, the method does nothing.
        /// Otherwise, it adds the specified grab interactable to the socket grab transformer and adds it to the global and local interactables with socket transformer lists.
        /// </remarks>
        /// <seealso cref="EndSocketSnapping"/>
        protected virtual bool StartSocketSnapping(VXRGrabInteractable grabInteractable)
        {
            // If we've already started socket snapping this interactable, do nothing
            var interactablesSocketedCount = m_InteractablesWithSocketTransformer.Count;
            if (interactablesSocketedCount >= socketSnappingLimit ||
                m_InteractablesWithSocketTransformer.Contains(grabInteractable))
            {
                return false;
            }

            if (interactablesSocketedCount > 0 && ejectExistingSocketsWhenSnapping)
            {
                // Be sure to eject any existing grab interactable from the snap grab socket
                foreach (var interactable in m_InteractablesWithSocketTransformer.AsList())
                {
                    interactable.RemoveSingleGrabTransformer(m_SocketGrabTransformer);
                }
                m_InteractablesWithSocketTransformer.Clear();
            }

            grabInteractable.AddSingleGrabTransformer(m_SocketGrabTransformer);
            m_InteractablesWithSocketTransformer.Add(grabInteractable);
            return true;
        }

        /// <summary>
        /// Ends socket snapping for a specified <see cref="XRGrabInteractable"/> object.
        /// </summary>
        /// <param name="grabInteractable">The <see cref="XRGrabInteractable"/> object to end socket snapping for.</param>
        /// <returns>Returns <see langword="true"/> if the specified grab interactable was found and removed; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Removes the specified grab interactable from the local interactables with socket transformer list and removes it from the socket grab transformer.
        /// </remarks>
        /// <seealso cref="StartSocketSnapping"/>
        protected virtual bool EndSocketSnapping(VXRGrabInteractable grabInteractable)
        {
            grabInteractable.RemoveSingleGrabTransformer(m_SocketGrabTransformer);
            return m_InteractablesWithSocketTransformer.Remove(grabInteractable);
        }
        #endregion

        #region - Visuals -
        /// <summary>
        /// Creates the default hover materials
        /// for <see cref="InteractableHoverMeshMaterial"/> and <see cref="InteractableCantHoverMeshMaterial"/> if necessary.
        /// </summary>
        protected virtual void CreateDefaultHoverMaterials()
        {
            if (_interactableHoverMeshMaterial != null && _interactableCantHoverMeshMaterial != null)
                return;

            var shaderName = GraphicsSettings.currentRenderPipeline ? "Universal Render Pipeline/Lit" : "Standard";
            var defaultShader = Shader.Find(shaderName);

            if (defaultShader == null)
            {
                Debug.LogWarning("Failed to create default materials for Socket Interactor," +
                    $" was unable to find \"{shaderName}\" Shader. Make sure the shader is included into the game build.", this);
                return;
            }

            if (_interactableHoverMeshMaterial == null)
            {
                _interactableHoverMeshMaterial = new Material(defaultShader);
                SetMaterialFade(_interactableHoverMeshMaterial, new Color(0f, 0f, 1f, 0.6f));
            }

            if (_interactableCantHoverMeshMaterial == null)
            {
                _interactableCantHoverMeshMaterial = new Material(defaultShader);
                SetMaterialFade(_interactableCantHoverMeshMaterial, new Color(1f, 0f, 0f, 0.6f));
            }
        }

        /// <summary>
        /// Unity calls this method automatically in order to draw the Interactables that are currently being hovered over.
        /// </summary>
        /// <seealso cref="GetHoveredInteractableMaterial"/>
        protected virtual void DrawHoveredInteractables()
        {
            if (!_showInteractableHoverMeshes || _interactableHoverScale <= 0f)
                return;

            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            foreach (var interactable in InteractablesHovered)
            {
                if (interactable == null)
                    continue;

                if (IsSelecting(interactable))
                    continue;

                if (!m_MeshFilterCache.TryGetValue(interactable, out var interactableTuples))
                    continue;

                if (interactableTuples == null || interactableTuples.Length == 0)
                    continue;

                var materialToDrawWith = GetHoveredInteractableMaterial(interactable);
                if (materialToDrawWith == null)
                    continue;

                foreach (var tuple in interactableTuples)
                {
                    var meshFilter = tuple.Item1;
                    var meshRenderer = tuple.Item2;
                    if (!ShouldDrawHoverMesh(meshFilter, meshRenderer, mainCamera))
                        continue;

                    var matrix = GetHoverMeshMatrix(interactable, meshFilter, _interactableHoverScale);
                    var sharedMesh = meshFilter.sharedMesh;
                    for (var submeshIndex = 0; submeshIndex < sharedMesh.subMeshCount; ++submeshIndex)
                    {
                        Graphics.DrawMesh(
                            sharedMesh,
                            matrix,
                            materialToDrawWith,
                            gameObject.layer,
                            null, // Draw mesh in all cameras (default value)
                            submeshIndex);
                    }
                }
            }
        }

        private Matrix4x4 GetHoverMeshMatrix(IXRInteractable interactable, MeshFilter meshFilter, float hoverScale)
        {
            var interactableAttachTransform = interactable.GetAttachTransform(this);

            var grabInteractable = interactable as VXRGrabInteractable;

            // Get the "static" pose of the interactable's attach transform in world space.
            // While the XR Grab Interactable is selected, the Attach Transform pose may have been modified
            // by user code, and we assume it will be restored back to the initial captured pose.
            // When Use Dynamic Attach is enabled, we can instead rely on using the dedicated GameObject for this interactor.
            Pose interactableAttachPose;
            if (grabInteractable != null && !grabInteractable.UseDynamicAttach &&
                grabInteractable.IsSelected &&
                interactableAttachTransform != interactable.transform &&
                interactableAttachTransform.IsChildOf(interactable.transform))
            {
                // The interactable's attach transform must not change parent Transform while selected
                // for the pose to be calculated correctly. This transforms the captured pose in local space
                // into the current pose in world space. If the pose of the attach transform was not modified
                // after being selected, this will be the same value as calculated in the else statement.
                var localAttachPose = grabInteractable.GetLocalAttachPoseOnSelect(grabInteractable.FirstInteractorSelecting);
                var attachTransformParent = interactableAttachTransform.parent;
                interactableAttachPose =
                    new Pose(attachTransformParent.TransformPoint(localAttachPose.position),
                        attachTransformParent.rotation * localAttachPose.rotation);
            }
            else
            {
                interactableAttachPose = new Pose(interactableAttachTransform.position, interactableAttachTransform.rotation);
            }

            var attachOffset = meshFilter.transform.position - interactableAttachPose.position;
            var interactableLocalPosition = InverseTransformDirection(interactableAttachPose, attachOffset) * hoverScale;
            var interactableLocalRotation = Quaternion.Inverse(Quaternion.Inverse(meshFilter.transform.rotation) * interactableAttachPose.rotation);

            Vector3 position;
            Quaternion rotation;

            var interactorAttachTransform = GetAttachTransform(interactable);
            var interactorAttachPose = new Pose(interactorAttachTransform.position, interactorAttachTransform.rotation);
            if (grabInteractable == null || grabInteractable.trackRotation)
            {
                position = interactorAttachPose.rotation * interactableLocalPosition + interactorAttachPose.position;
                rotation = interactorAttachPose.rotation * interactableLocalRotation;
            }
            else
            {
                position = interactableAttachPose.rotation * interactableLocalPosition + interactorAttachPose.position;
                rotation = meshFilter.transform.rotation;
            }

            // Rare case that Track Position is disabled
            if (grabInteractable != null && !grabInteractable.trackPosition)
                position = meshFilter.transform.position;

            var scale = meshFilter.transform.lossyScale * hoverScale;

            return Matrix4x4.TRS(position, rotation, scale);
        }

        /// <summary>
        /// Gets the material used to draw the given hovered Interactable.
        /// </summary>
        /// <param name="interactable">The hovered Interactable to get the material for.</param>
        /// <returns>Returns the material Unity should use to draw the given hovered Interactable.</returns>
        protected virtual Material GetHoveredInteractableMaterial(IXRHoverInteractable interactable)
        {
            return HasSelection ? _interactableCantHoverMeshMaterial : _interactableHoverMeshMaterial;
        }

        /// <summary>
        /// Unity calls this method automatically in order to determine whether the mesh should be drawn.
        /// </summary>
        /// <param name="meshFilter">The <see cref="MeshFilter"/> which will be drawn when returning <see langword="true"/>.</param>
        /// <param name="meshRenderer">The <see cref="Renderer"/> on the same <see cref="GameObject"/> as the <paramref name="meshFilter"/>.</param>
        /// <param name="mainCamera">The Main Camera.</param>
        /// <returns>Returns <see langword="true"/> if the mesh should be drawn. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="DrawHoveredInteractables"/>
        protected virtual bool ShouldDrawHoverMesh(MeshFilter meshFilter, Renderer meshRenderer, Camera mainCamera)
        {
            // Graphics.DrawMesh will automatically handle camera culling of the hover mesh using
            // the GameObject layer of this socket that we pass as the argument value.
            // However, we also check here to skip drawing the hover mesh if the mesh of the interactable
            // itself isn't also drawn by the main camera. For the typical scene with one camera,
            // this means that for the hover mesh to be rendered, the camera should have a culling mask
            // which overlaps with both the GameObject layer of this socket and the GameObject layer of the interactable.
            var cullingMask = mainCamera.cullingMask;
            return meshFilter != null && (cullingMask & (1 << meshFilter.gameObject.layer)) != 0 && meshRenderer != null && meshRenderer.enabled;
        }
        #endregion

        #region - Helpers -
        /// <summary>
        /// Sets Standard <paramref name="material"/> with Fade rendering mode
        /// and set <paramref name="color"/> as the main color.
        /// </summary>
        /// <param name="material">The <see cref="Material"/> whose properties will be set.</param>
        /// <param name="color">The main color to set.</param>
        private static void SetMaterialFade(Material material, Color color)
        {
            material.SetOverrideTag("RenderType", "Transparent");

            // In a Scripted Render Pipeline (URP/HDRP), we need to set the surface mode to 1 for transparent.
            var isSRP = GraphicsSettings.currentRenderPipeline != null;
            if (isSRP)
                material.SetFloat(ShaderPropertyLookup.surface, 1f);

            material.SetFloat(ShaderPropertyLookup.mode, 2f);
            material.SetInt(ShaderPropertyLookup.srcBlend, (int)BlendMode.SrcAlpha);
            material.SetInt(ShaderPropertyLookup.dstBlend, (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt(ShaderPropertyLookup.zWrite, 0);
            // ReSharper disable StringLiteralTypo
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            // ReSharper restore StringLiteralTypo
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetColor(isSRP ? ShaderPropertyLookup.baseColor : ShaderPropertyLookup.color, color);
        }

        /// <summary>
        /// Transforms a direction from world space to local space. The opposite of <c>Transform.TransformDirection</c>,
        /// but using a world Pose instead of a Transform.
        /// </summary>
        /// <param name="pose">The world space position and rotation of the Transform.</param>
        /// <param name="direction">The direction to transform.</param>
        /// <returns>Returns the transformed direction.</returns>
        /// <remarks>
        /// This operation is unaffected by scale.
        /// <br/>
        /// You should use <c>Transform.InverseTransformPoint</c> equivalent if the vector represents a position in space rather than a direction.
        /// </remarks>
        private static Vector3 InverseTransformDirection(Pose pose, Vector3 direction)
        {
            return Quaternion.Inverse(pose.rotation) * direction;
        }
        #endregion        

    }
}
