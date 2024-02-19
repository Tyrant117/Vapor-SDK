using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using VaporInspector;

namespace VaporXR.Interactors
{
    public class VXRRayCompositeInteractor : VXRCompositeInteractor, IAdvancedLineRenderable, IXRRayProvider
    {
        #region Inspector
        protected override bool RequiresHoverInteractor => true;

        [FoldoutGroup("Components"), SerializeField]
        protected VXRCurvedSorter _curveSorter;
        #endregion

        #region Properties
        public Vector3 RayEndPoint => _curveSorter.RayEndPoint;

        public Transform RayEndTransform => _curveSorter.RayEndTransform;

        public virtual bool ShouldDrawLine
        {
            get
            {
                return true;
            }
        }

        public LineModeType LineType => _curveSorter.LineType;
        #endregion

        #region Fields

        #endregion

        #region - Initialization -
        protected override void Awake()
        {
            base.Awake();
            Hover.SetOverrideSorter(_curveSorter);
        }
        #endregion

        #region - Hovering -
        public override bool CanHover(IVXRHoverInteractable interactable)
        {
            return base.CanHover(interactable) && (!HasSelection || IsSelecting(interactable));
        }
        #endregion

        #region - Selection -
        public override bool CanSelect(IVXRSelectInteractable interactable)
        {
            return base.CanSelect(interactable) && (!HasSelection || IsSelecting(interactable));
        }
        #endregion

        #region - Ray Provider -
        public Transform GetOrCreateAttachTransform()
        {
            return AttachPoint;
        }

        public Transform GetOrCreateRayOrigin()
        {
            return _curveSorter.AttachPoint;
        }

        public void SetAttachTransform(Transform newAttach)
        {
            AttachPoint = newAttach;
        }

        public void SetRayOrigin(Transform newOrigin)
        {
            _curveSorter.AttachPoint = newOrigin;
        }

        public bool TryGetCurrentRaycast(out RaycastHit? raycastHit, out int raycastHitIndex,
            out RaycastResult? uiRaycastHit, out int uiRaycastHitIndex, out bool isUIHitClosest)
        {
            return _curveSorter.TryGetCurrentRaycast(out raycastHit, out raycastHitIndex, out uiRaycastHit, out uiRaycastHitIndex, out isUIHitClosest);

        }

        public bool TryGetCurrent3DRaycastHit(out RaycastHit raycastHit)
        {
            return _curveSorter.TryGetCurrent3DRaycastHit(out raycastHit, out _);
        }
        #endregion

        #region - Line Rendering -
        public void GetLineOriginAndDirection(out Vector3 origin, out Vector3 direction)
        {
            _curveSorter.GetLineOriginAndDirection(out origin, out direction);
        }

        public bool GetLinePoints(ref NativeArray<Vector3> linePoints, out int numPoints, Ray? rayOriginOverride = null)
        {
            return _curveSorter.GetLinePoints(ref linePoints, out numPoints, rayOriginOverride);
        }

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

        public bool TryGetHitInfo(out Vector3 position, out Vector3 normal, out int positionInLine, out bool isValidTarget)
        {
            position = default;
            normal = default;
            positionInLine = default;
            isValidTarget = default;

            if (!TryGetCurrentRaycast(out var raycastHit, out var raycastHitIndex,
                out var raycastResult, out var raycastResultIndex, out var isUIHitClosest))
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
        #endregion
    }
}
