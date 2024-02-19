using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Serialization;
using VaporInspector;
using VaporXR.Utilities;

namespace VaporXR
{
    /// <summary>
    /// Interactor used for interacting with interactables via gaze. This extends <see cref="XRRayInteractor"/> and
    /// uses the same ray cast technique to update a current set of valid targets.
    /// </summary>
    /// <seealso cref="XRBaseInteractable.allowGazeInteraction"/>
    /// <seealso cref="XRBaseInteractable.allowGazeSelect"/>
    /// <seealso cref="XRBaseInteractable.allowGazeAssistance"/>
    // ReSharper disable once InconsistentNaming
    public class VXRGazeInteractor : VXRRayInteractor
    {
        /// <summary>
        /// Defines the way the gaze assistance calculates and sizes the assistance area.
        /// </summary>
        /// <seealso cref="VXRGazeInteractor.GazeAssistanceCalculation"/>
        public enum GazeAssistanceCalculationType
        {
            ///<summary>
            /// Gaze assistance area will be a fixed size set in <see cref="VXRGazeInteractor.GazeAssistanceColliderFixedSize"/>
            /// and scaled by <see cref="VXRGazeInteractor.GazeAssistanceColliderScale"/>.
            /// </summary>
            FixedSize,

            ///<summary>
            /// Gaze assistance area will be sized based on the <see cref="Collider.bounds"/> of the <see cref="IVXRInteractable"/>
            /// this <see cref="VXRGazeInteractor"/> is hovering over and scaled by <see cref="VXRGazeInteractor.GazeAssistanceColliderScale"/>.
            /// </summary>
            ColliderSize,
        }

        #region Inspector
        [SerializeField, FoldoutGroup("Gaze")]
        private GazeAssistanceCalculationType _gazeAssistanceCalculation;
        [SerializeField, FoldoutGroup("Gaze")]
        private float _gazeAssistanceColliderFixedSize = 1f;
        [SerializeField, FoldoutGroup("Gaze")]
        private float _gazeAssistanceColliderScale = 1f;
        [SerializeField, FoldoutGroup("Gaze")]
        private VXRInteractableSnapVolume _gazeAssistanceSnapVolume;
        [SerializeField, FoldoutGroup("Gaze")]
        private bool _gazeAssistanceDistanceScaling;
        [SerializeField, FoldoutGroup("Gaze")]
        private bool _clampGazeAssistanceDistanceScaling;
        [SerializeField, FoldoutGroup("Gaze")]
        private float _gazeAssistanceDistanceScalingClampValue = 1f;
        #endregion

        #region Properties
        /// <summary>
        /// Defines the way the gaze assistance calculates and sizes the assistance area.
        /// </summary>
        /// <seealso cref="GazeAssistanceCalculationType"/>
        public GazeAssistanceCalculationType GazeAssistanceCalculation
        {
            get => _gazeAssistanceCalculation;
            set => _gazeAssistanceCalculation = value;
        }
        
        /// <summary>
        /// The size of the <see cref="GazeAssistanceSnapVolume"/> collider when <see cref="GazeAssistanceCalculation"/> is <see cref="GazeAssistanceCalculationType.FixedSize"/>.
        /// </summary>
        /// <seealso cref="GazeAssistanceCalculationType"/>
        public float GazeAssistanceColliderFixedSize
        {
            get => _gazeAssistanceColliderFixedSize;
            set => _gazeAssistanceColliderFixedSize = value;
        }
        
        /// <summary>
        /// The scale of the <see cref="GazeAssistanceSnapVolume"/> when <see cref="GazeAssistanceCalculation"/> is <see cref="GazeAssistanceCalculationType.FixedSize"/> or <see cref="GazeAssistanceCalculationType.ColliderSize"/> .
        /// </summary>
        /// <seealso cref="GazeAssistanceCalculationType"/>
        public float GazeAssistanceColliderScale
        {
            get => _gazeAssistanceColliderScale;
            set => _gazeAssistanceColliderScale = value;
        }

        /// <summary>
        /// The <see cref="VXRInteractableSnapVolume"/> to place where this <see cref="VXRGazeInteractor"/> hits a
        /// valid target for gaze assistance. If not set, Unity will create one by default.
        /// </summary>
        /// <remarks>
        /// Only <see cref="SphereCollider"/> and <see cref="BoxCollider"/> are supported
        /// for automatic dynamic scaling of the <see cref="VXRInteractableSnapVolume.snapCollider"/>.
        /// </remarks>
        public VXRInteractableSnapVolume GazeAssistanceSnapVolume
        {
            get => _gazeAssistanceSnapVolume;
            set => _gazeAssistanceSnapVolume = value;
        }
        
        /// <summary>
        /// If true, the <see cref="GazeAssistanceSnapVolume"/> will also scale based on the distance from the <see cref="VXRGazeInteractor"/>.
        /// </summary>
        /// <seealso cref="ClampGazeAssistanceDistanceScaling"/>
        public bool GazeAssistanceDistanceScaling
        {
            get => _gazeAssistanceDistanceScaling;
            set => _gazeAssistanceDistanceScaling = value;
        }

        /// <summary>
        /// If true, the <see cref="GazeAssistanceSnapVolume"/> scale will be clamped at <see cref="GazeAssistanceDistanceScalingClampValue"/>.
        /// </summary>
        /// <seealso cref="GazeAssistanceCalculationType"/>
        public bool ClampGazeAssistanceDistanceScaling
        {
            get => _clampGazeAssistanceDistanceScaling;
            set => _clampGazeAssistanceDistanceScaling = value;
        }
        
        /// <summary>
        /// The value the assistance collider scale will be clamped to if <see cref="ClampGazeAssistanceDistanceScaling"/> is true.
        /// </summary>
        /// <seealso cref="GazeAssistanceCalculationType"/>
        public float GazeAssistanceDistanceScalingClampValue
        {
            get => _gazeAssistanceDistanceScalingClampValue;
            set => _gazeAssistanceDistanceScalingClampValue = value;
        }
        #endregion

        #region - Initialization -
        protected override void Awake()
        {
            base.Awake();
            _CreateGazeAssistanceSnapVolume();

            void _CreateGazeAssistanceSnapVolume()
            {
                // If we don't have a snap volume for gaze assistance, generate one.
                if (_gazeAssistanceSnapVolume == null)
                {
                    var snapVolumeGo = new GameObject("Gaze Snap Volume");
                    var snapCollider = snapVolumeGo.AddComponent<SphereCollider>();
                    snapCollider.isTrigger = true;
                    _gazeAssistanceSnapVolume = snapVolumeGo.AddComponent<VXRInteractableSnapVolume>();
                }
                else if (_gazeAssistanceSnapVolume.snapCollider != null)
                {
                    if (!(_gazeAssistanceSnapVolume.snapCollider is SphereCollider || _gazeAssistanceSnapVolume.snapCollider is BoxCollider))
                        Debug.LogWarning("The Gaze Assistance Snap Volume is using a Snap Collider which does not support" +
                                         " automatic dynamic scaling by the XR Gaze Interactor. It must be a Sphere Collider or Box Collider.", this);
                }
            }
        }
        #endregion

        #region - Processing -
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                // Get the nearest valid interactable target that this can interact with,
                // and use it to update the gaze assistance snap volume.
                var gazeInteractable = CanInteract(CurrentNearestValidTarget) ? CurrentNearestValidTarget : null;
                UpdateSnapVolumeInteractable(gazeInteractable);
            }
        }

        /// <summary>
        /// Updates the <see cref="GazeAssistanceSnapVolume"/> based on a target interactable.
        /// </summary>
        /// <param name="interactable">The <see cref="IVXRInteractable"/> this <see cref="VXRGazeInteractor"/> is processing and using to update the <see cref="GazeAssistanceSnapVolume"/>.</param>
        protected virtual void UpdateSnapVolumeInteractable(IVXRInteractable interactable)
        {
            if (_gazeAssistanceSnapVolume == null)
                return;

            var snapVolumePosition = Vector3.zero;
            var snapVolumeScale = _gazeAssistanceColliderScale;
            var snapColliderSize = 0f;
            IVXRInteractable snapInteractable = null;
            Collider snapToCollider = null;

            // Currently assumes no gaze assistance for interactables that are not our abstract base class
            if (interactable is VXRBaseInteractable baseInteractable && baseInteractable != null && baseInteractable.AllowGazeAssistance)
            {
                snapInteractable = interactable;

                // Default to interactable, tries to grab collider position below
                snapVolumePosition = interactable.transform.position;

                if (TryGetHitInfo(out var pos, out _, out _, out _) &&
                    XRInteractableUtility.TryGetClosestCollider(interactable, pos, out var distanceInfo))
                {
                    snapToCollider = distanceInfo.collider;
                    snapVolumePosition = distanceInfo.collider.bounds.center;
                }

                snapColliderSize = CalculateSnapColliderSize(snapToCollider);
            }

            // Update position, size, and scale of the snap volume
            if (_gazeAssistanceDistanceScaling)
            {
                snapVolumeScale *= Vector3.Distance(transform.position, snapVolumePosition);
                if (_clampGazeAssistanceDistanceScaling)
                    snapVolumeScale = Mathf.Clamp(snapVolumeScale, 0f, _gazeAssistanceDistanceScalingClampValue);
            }

            var snapVolumeTransform = _gazeAssistanceSnapVolume.transform;
            snapVolumeTransform.position = snapVolumePosition;
            snapVolumeTransform.localScale = new Vector3(snapVolumeScale, snapVolumeScale, snapVolumeScale);

            switch (_gazeAssistanceSnapVolume.snapCollider)
            {
                case SphereCollider sphereCollider:
                    sphereCollider.radius = snapColliderSize;
                    break;
                case BoxCollider boxCollider:
                    boxCollider.size = new Vector3(snapColliderSize, snapColliderSize, snapColliderSize);
                    break;
            }

            // Update references
            _gazeAssistanceSnapVolume.interactable = snapInteractable;
            _gazeAssistanceSnapVolume.snapToCollider = snapToCollider;
        }
        
        private float CalculateSnapColliderSize(Collider interactableCollider)
        {
            switch (_gazeAssistanceCalculation)
            {
                case GazeAssistanceCalculationType.FixedSize:
                    return _gazeAssistanceColliderFixedSize;
                case GazeAssistanceCalculationType.ColliderSize:
                    if (interactableCollider != null)
                        return interactableCollider.bounds.size.MaxComponent();
                    break;
                default:
                    Debug.Assert(false, $"Unhandled {nameof(GazeAssistanceCalculationType)}={_gazeAssistanceCalculation}", this);
                    break;
            }

            return 0f;
        }
        #endregion

        #region - Interaction -
        /// <summary>
        /// Checks to see if this <see cref="VXRGazeInteractor"/> can interact with an <see cref="IVXRInteractable"/>.
        /// </summary>
        /// <param name="interactable">The <see cref="IVXRInteractable"/> to check if this <see cref="VXRGazeInteractor"/> can interact with.</param>
        /// <returns>Returns <see langword="true"/> if this <see cref="VXRGazeInteractor"/> can interact with <see cref="interactable"/>, otherwise returns <see langword="false"/>.</returns>
        private bool CanInteract(IVXRInteractable interactable)
        {
            return interactable is IVXRHoverInteractable hoverInteractable && InteractionManager.CanHover(this, hoverInteractable) ||
                   interactable is IVXRSelectInteractable selectInteractable && InteractionManager.CanSelect(this, selectInteractable);
        }
        #endregion

        #region - Selection -
        protected override float GetHoverTimeToSelect(IVXRInteractable interactable)
        {
            if (interactable is IXROverridesGazeAutoSelect { OverrideGazeTimeToSelect: true } overrideProvider)
                return overrideProvider.GazeTimeToSelect;

            return base.GetHoverTimeToSelect(interactable);
        }

        protected override float GetTimeToAutoDeselect(IVXRInteractable interactable)
        {
            if (interactable is IXROverridesGazeAutoSelect { OverrideTimeToAutoDeselectGaze: true } overrideProvider)
                return overrideProvider.TimeToAutoDeselectGaze;

            return base.GetTimeToAutoDeselect(interactable);
        }
        #endregion
    }
}
