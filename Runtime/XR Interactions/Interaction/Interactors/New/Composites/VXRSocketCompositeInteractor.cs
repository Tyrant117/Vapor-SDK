using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using VaporInspector;

namespace VaporXR
{
    public class VXRSocketCompositeInteractor : VXRCompositeInteractor
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

        #region Inspector
        protected override bool RequiresHoverInteractor => true;
        protected override bool RequiresSelectInteractor => true;


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
                m_SocketGrabTransformer.SocketSnappingRadius = value;
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
                m_SocketGrabTransformer.ScaleMode = value;
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
                m_SocketGrabTransformer.FixedScale = value;
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
                m_SocketGrabTransformer.TargetBoundsSize = value;
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
        private readonly Dictionary<IXRInteractable, ValueTuple<MeshFilter, Renderer>[]> m_MeshFilterCache = new();
        private readonly XRSocketGrabTransformer m_SocketGrabTransformer = new();
        private readonly HashSetList<VXRGrabInteractable> m_InteractablesWithSocketTransformer = new();

        private float m_LastRemoveTime = -1f;
        #endregion


        #region - Initialization -
        protected override void Awake()
        {
            base.Awake();
            SyncTransformerParams();
            CreateDefaultHoverMaterials();

            
        }

        private void OnEnable()
        {
            m_SocketGrabTransformer.canProcess = _socketActive;
            SyncTransformerParams();

            Hover.HoverActive = OnOverrideHoverActive;
            Select.SelectActive = OnOverrideSelectActive;
            Select.SelectedInteractableMovementTypeOverride = OnInteractableMovementTypeOverride;

            Hover.HoverEntering += OnHoverEntering;
            Hover.HoverEntered += OnHoverEntered;
            Hover.HoverExiting += OnHoverExiting;

            Select.SelectEntered += OnSelectEntered;
            Select.SelectExiting += OnSelectExiting;
            Select.SelectExited += OnSelectExited;
        }        

        private void OnDisable()
        {
            m_SocketGrabTransformer.canProcess = false;

            Hover.HoverActive = null;
            Select.SelectActive = null;
            Select.SelectedInteractableMovementTypeOverride = null;

            Hover.HoverEntering -= OnHoverEntering;
            Hover.HoverEntered -= OnHoverEntered;
            Hover.HoverExiting -= OnHoverExiting;

            Select.SelectEntered -= OnSelectEntered;
            Select.SelectExiting -= OnSelectExiting;
            Select.SelectExited -= OnSelectExited;
        }

        private void SyncTransformerParams()
        {
            m_SocketGrabTransformer.SocketInteractor = this;
            m_SocketGrabTransformer.SocketSnappingRadius = socketSnappingRadius;
            m_SocketGrabTransformer.ScaleMode = socketScaleMode;
            m_SocketGrabTransformer.FixedScale = fixedScale;
            m_SocketGrabTransformer.TargetBoundsSize = targetBoundsSize;
        }
        #endregion

        #region - Processing -
        private void Update()
        {
            // An explicit check for isHoverRecycleAllowed is done since an interactable may have been deselected
            // after this socket was updated by the manager, such as when a later Interactor takes the selection
            // from this socket. The recycle delay time could cause the hover to be effectively disabled.
            if (_showInteractableHoverMeshes && HasHover && IsHoverRecycleAllowed)
            {
                DrawHoveredInteractables();
            }
        }

        private VXRBaseInteractable.MovementType OnInteractableMovementTypeOverride()
        {
            return VXRBaseInteractable.MovementType.Instantaneous;
        }
        #endregion

        #region - Hovering -
        private bool OnOverrideHoverActive()
        {
            return _socketActive;
        }

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

        private void OnHoverEntering(HoverEnterEventArgs args)
        {
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

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (!CanHoverSnap(args.interactableObject))
            {
                return;
            }

            if (args.interactableObject is VXRGrabInteractable grabInteractable)
            {
                StartSocketSnapping(grabInteractable);
            }
        }

        private void OnHoverExiting(HoverExitEventArgs args)
        {
            var interactable = args.interactableObject;
            m_MeshFilterCache.Remove(interactable);

            if (interactable is VXRGrabInteractable grabInteractable)
            {
                EndSocketSnapping(grabInteractable);
            }
        }
        #endregion

        #region - Selection -
        private XRIneractionActiveState OnOverrideSelectActive()
        {
            return new XRIneractionActiveState(_socketActive, false, 1f);
        }

        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return base.CanSelect(interactable) &&
                ((!HasSelection && !interactable.IsSelected) ||
                    (IsSelecting(interactable) && interactable.InteractorsSelecting.Count == 1));
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactableObject is VXRGrabInteractable grabInteractable)
            {
                StartSocketSnapping(grabInteractable);
            }
        }

        private void OnSelectExiting(SelectExitEventArgs args)
        {
            m_LastRemoveTime = Time.time;
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
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

        #region - Socketing -
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

            foreach (var interactable in Hover.InteractablesHovered)
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
