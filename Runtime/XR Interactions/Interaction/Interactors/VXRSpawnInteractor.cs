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
    /// Interactor used for holding interactables. It cannot accept new interactables, but will continously spawn them from its pool.
    /// </summary>
    [DisallowMultipleComponent]
    public class VXRSpawnInteractor : VXRBaseInteractor
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
        [FoldoutGroup("Spawner"), SerializeField]
        [RichTextTooltip("Whether socket interaction is enabled.")]
        private bool _spawnerActive = true;
        [FoldoutGroup("Spawner"), SerializeField]
        [RichTextTooltip("The prefab to spawn at the spawner.")]
        private VXRGrabInteractable _spawnPrefab;

        [FoldoutGroup("Socket"), SerializeField, Min(-1)]
        [RichTextTooltip("The quantity that can respawn, -1 for infinite stock")]
        private int _stock = -1;
        [FoldoutGroup("Socket"), SerializeField, Min(0)]
        [RichTextTooltip("Sets the amount of time the socket takes to respawn after being selected")]
        private float _respawnTime = 1f;

        [FoldoutGroup("Socket"), SerializeField]
        [RichTextTooltip("Scale mode used to calculate the scale factor applied to the interactable when hovering.")]
        private SocketScaleMode _socketScaleMode = SocketScaleMode.None;
        [FoldoutGroup("Socket"), SerializeField]
        [RichTextTooltip("Scale factor applied to the interactable when scale mode is set to <itf>SocketScaleMode</itf><mth>.Fixed</mth>.")]
        private Vector3 _fixedScale = Vector3.one;
        [FoldoutGroup("Socket"), SerializeField]
        [RichTextTooltip("Bounds size used to calculate the scale factor applied to the interactable when scale mode is set to <itf>SocketScaleMode</itf><mth>.StretchedToFitSize</mth>.")]
        private Vector3 _targetBoundsSize = Vector3.one;
        #endregion

        #region Properties
        /// <summary>
        /// Whether socket interaction is enabled.
        /// </summary>
        public bool SpawnerActive
        {
            get => _spawnerActive;
            set
            {
                _spawnerActive = value;
                m_SocketGrabTransformer.canProcess = value && isActiveAndEnabled;
            }
        }

        public int Stock { get => _stock; set => _stock = value; }

        /// <summary>
        /// Sets the amount of time the socket will refuse hovers after an object is removed.
        /// </summary>
        /// <remarks>
        /// Does nothing if <see cref="HoverSocketSnapping"/> is enabled to prevent snap flickering.
        /// </remarks>
        public float RespawnTime { get => _respawnTime; set => _respawnTime = value; }

        /// <summary>
        /// Scale mode used to calculate the scale factor applied to the interactable when hovering.
        /// </summary>
        /// <seealso cref="VaporXR.SocketScaleMode"/>
        public SocketScaleMode SocketScaleMode
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
        /// <seealso cref="SocketScaleMode"/>
        public Vector3 FixedScale
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
        /// <seealso cref="SocketScaleMode"/>
        public Vector3 TargetBoundsSize
        {
            get => _targetBoundsSize;
            set
            {
                _targetBoundsSize = value;
                m_SocketGrabTransformer.TargetBoundsSize = value;
            }
        }

        public override bool IsHoverActive => base.IsHoverActive && _spawnerActive;

        public override bool IsSelectActive => base.IsSelectActive && _spawnerActive;

        public override VXRBaseInteractable.MovementType? SelectedInteractableMovementTypeOverride => VXRBaseInteractable.MovementType.Instantaneous;

        // ***** Internal *****
        /// <summary>
        /// The set of Interactables that this Interactor could possibly interact with this frame.
        /// This list is not sorted by priority.
        /// </summary>
        /// <seealso cref="IXRInteractor.GetValidTargets"/>
        protected List<IXRInteractable> UnsortedValidTargets { get; } = new List<IXRInteractable>();

        /// <summary>
        /// Maximum number of interactables this interactor can socket.
        /// Used for hover socket snapping evaluation.
        /// </summary>
        protected virtual int SocketSnappingLimit => 1;

        /// <summary>
        /// Determines if when snapping to a socket, any existing sockets should be ejected.
        /// </summary>
        protected virtual bool EjectExistingSocketsWhenSnapping => true;

        private bool IsRespawnAllowed => m_LastRemoveTime < 0f || _respawnTime <= 0f || (Time.time > m_LastRemoveTime + _respawnTime);
        #endregion

        #region Fields
        private readonly XRSocketGrabTransformer m_SocketGrabTransformer = new();
        private readonly HashSetList<VXRGrabInteractable> m_InteractablesWithSocketTransformer = new();

        private bool _infiniteStock;
        private float m_LastRemoveTime = -1f;
        #endregion

        #region - Initialization -
        protected virtual void OnValidate()
        {
            SyncTransformerParams();
        }

        protected override void Awake()
        {
            base.Awake();

            _infiniteStock = _stock == -1;
            SyncTransformerParams();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_SocketGrabTransformer.canProcess = _spawnerActive;
            SyncTransformerParams();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_SocketGrabTransformer.canProcess = false;
        }

        private void SyncTransformerParams()
        {
            m_SocketGrabTransformer.SocketInteractor = this;
            m_SocketGrabTransformer.SocketSnappingRadius = 0.1f;
            m_SocketGrabTransformer.ScaleMode = SocketScaleMode;
            m_SocketGrabTransformer.FixedScale = FixedScale;
            m_SocketGrabTransformer.TargetBoundsSize = TargetBoundsSize;
        }

        public override void OnRegistered(InteractorRegisteredEventArgs args)
        {
            base.OnRegistered(args);
            args.manager.interactableRegistered += OnInteractableRegistered;
            args.manager.interactableUnregistered += OnInteractableUnregistered;

            VXRInteractionManager.RemoveAllUnregistered(args.manager, UnsortedValidTargets);
        }

        public override void OnUnregistered(InteractorUnregisteredEventArgs args)
        {
            base.OnUnregistered(args);
            args.manager.interactableRegistered -= OnInteractableRegistered;
            args.manager.interactableUnregistered -= OnInteractableUnregistered;
        }

        private void OnInteractableRegistered(InteractableRegisteredEventArgs args)
        {

        }

        private void OnInteractableUnregistered(InteractableUnregisteredEventArgs args)
        {
            UnsortedValidTargets.Remove(args.interactableObject);
        }
        #endregion

        #region - Processing -
        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractor(updatePhase);
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                if(IsRespawnAllowed && m_InteractablesWithSocketTransformer.Count == 0)
                {
                    if(_infiniteStock)
                    {
                        _Spawn();
                    }
                    else if(_stock > 0)
                    {
                        _Spawn();
                        _stock--;
                    }
                }
            }

            void _Spawn()
            {
                var grab = GameObject.Instantiate(_spawnPrefab, AttachTransform.position, AttachTransform.rotation);                
                StartManualInteraction(grab);
            }
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
            if (filter != null && filter.CanProcess)
            {
                filter.Process(this, UnsortedValidTargets, targets);
            }
            else
            {
                SortingHelpers.SortByDistanceToInteractor(this, UnsortedValidTargets, targets);
            }
        }
        #endregion

        #region - Hover -
        public override bool CanHover(IXRHoverInteractable interactable)
        {
            return false;
        }
        #endregion

        #region - Select -
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return base.CanSelect(interactable) &&
                ((!HasSelection && !interactable.IsSelected) || (IsSelecting(interactable) && interactable.InteractorsSelecting.Count == 1));
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
            if (interactablesSocketedCount >= SocketSnappingLimit ||
                m_InteractablesWithSocketTransformer.Contains(grabInteractable))
            {
                return false;
            }

            if (interactablesSocketedCount > 0 && EjectExistingSocketsWhenSnapping)
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
            UnsortedValidTargets.Add(grabInteractable);
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
            UnsortedValidTargets.Remove(grabInteractable);
            return m_InteractablesWithSocketTransformer.Remove(grabInteractable);
        }
        #endregion      
    }
}
