using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using VaporInspector;

namespace VaporXR.Interaction
{
    [DisallowMultipleComponent]
    public class SpawnInteractorModule : InteractorModule
    {
        #region Inspector
        [FoldoutGroup("Components"), SerializeField, InlineButton("AddManualSorter", label: "+")]
        private VXRManualSorter _manualSorter;
#pragma warning disable IDE0051 // Remove unused private members
        private void AddManualSorter() { if (!_manualSorter) _manualSorter = gameObject.AddComponent<VXRManualSorter>(); }
#pragma warning restore IDE0051 // Remove unused private members

        [FoldoutGroup("Spawner"), SerializeField]
        [RichTextTooltip("Whether socket interaction is enabled.")]
        private bool _spawnerActive = true;
        [FoldoutGroup("Spawner"), SerializeField]
        [RichTextTooltip("The prefab to spawn at the spawner.")]
        private GrabInteractableModule _spawnPrefab;

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
        private readonly HashSetList<GrabInteractableModule> m_InteractablesWithSocketTransformer = new();

        private bool _infiniteStock;
        private float m_LastRemoveTime = -1f;
        #endregion

        #region - Initialization -
        protected override void Awake()
        {
            base.Awake();
            _infiniteStock = _stock == -1;
            SyncTransformerParams();
        }

        protected virtual void OnEnable()
        {
            Interactor.SelectActive = OnSelectActiveCheck;
            Interactor.SelectEntered += OnSelectEntered;
            Interactor.SelectExited += OnSelectExited;
            Interactor.SetOverrideSorter(_manualSorter);
            m_SocketGrabTransformer.CanProcess = _spawnerActive;
            SyncTransformerParams();
        }

        protected virtual void OnDisable()
        {
            Interactor.SelectActive = null;
            Interactor.SelectEntered -= OnSelectEntered;
            Interactor.SelectExited -= OnSelectExited;
            Interactor.SetOverrideSorter(null);
            m_SocketGrabTransformer.CanProcess = false;
        }

        private void SyncTransformerParams()
        {
            m_SocketGrabTransformer.SocketInteractor = Interactor;
            m_SocketGrabTransformer.SocketSnappingRadius = 0.1f;
            m_SocketGrabTransformer.ScaleMode = SocketScaleMode;
            m_SocketGrabTransformer.FixedScale = FixedScale;
            m_SocketGrabTransformer.TargetBoundsSize = TargetBoundsSize;
        }
        #endregion

        #region - Processing -
        private void Update()
        {
            if (IsRespawnAllowed && m_InteractablesWithSocketTransformer.Count == 0)
            {
                if (_infiniteStock)
                {
                    _Spawn();
                }
                else if (_stock > 0)
                {
                    _Spawn();
                    _stock--;
                }
            }

            void _Spawn()
            {
                var grab = GameObject.Instantiate(_spawnPrefab, Interactor.AttachPoint.position, Interactor.AttachPoint.rotation);
                Interactor.StartManualInteraction(grab.Interactable);
            }
        }
        #endregion

        #region - Selection -
        public override bool CanSelect(Interactable interactable)
        {
            return base.CanSelect(interactable) &&
                ((!Interactor.HasSelection && !interactable.IsSelected) || (Interactor.IsSelecting(interactable) && interactable.InteractorsSelecting.Count == 1));
        }

        private XRIneractionActiveState OnSelectActiveCheck()
        {
            return new XRIneractionActiveState(_spawnerActive, false, 1f);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.InteractableObject.TryGetModule<GrabInteractableModule>(out var grabInteractable))
            {
                StartSocketSnapping(grabInteractable);
            }
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            m_LastRemoveTime = Time.time;
            if (args.InteractableObject.TryGetModule<GrabInteractableModule>(out var grabInteractable))
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
        protected virtual bool StartSocketSnapping(GrabInteractableModule grabInteractable)
        {
            Debug.Log($"Started Spawn Snapping: {grabInteractable.name}");
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
            _manualSorter.ManualAddTarget(grabInteractable.Interactable);
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
        protected virtual bool EndSocketSnapping(GrabInteractableModule grabInteractable)
        {
            Debug.Log($"End Spawn Snapping: {grabInteractable.name}");
            grabInteractable.RemoveSingleGrabTransformer(m_SocketGrabTransformer);
            _manualSorter.ManualRemoveTarget(grabInteractable.Interactable);
            return m_InteractablesWithSocketTransformer.Remove(grabInteractable);
        }
        #endregion 
    }
}
