using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VaporEvents;
using VaporInspector;
using VaporKeys;

namespace VaporNetcodeForGo
{
    public class TransformModule : PeerModule
    {
        [SerializeField, ValueDropdown("@GetAllProviderKeyValues", searchable: true), IgnoreCustomDrawer]
        private KeyDropdownValue _localKey;
        [SerializeField]
        private GameObject _peerTransformPrefab;

        private Transform _target;


        protected override void OnLocalClientInitialize()
        {
            StartCoroutine(ProviderBus.Get<ProviderData<Transform>>(_localKey).RequestRoutine<Transform>(_OnCacheTransform));

            void _OnCacheTransform(Transform t)
            {
                _target = t;
            }
        }

        protected override void OnRemoteClientInitialize()
        {
            _target = GameObject.Instantiate(_peerTransformPrefab).transform;
        }

        protected override void OnServerInitialize()
        {
            _target = GameObject.Instantiate(_peerTransformPrefab).transform;
        }

        public override void OnLocalClientUpdate(PeerUpdateOrder.UpdatePhase updatePhase)
        {
            if (!_target) { return; }
        }

        public override void OnRemoteClientUpdate(PeerUpdateOrder.UpdatePhase updatePhase)
        {
            if (!_target) { return; }
        }

        public override void OnServerUpdate(PeerUpdateOrder.UpdatePhase updatePhase)
        {
            Apply();
        }

        protected void Apply()
        {
            // interpolate parts
            //if (syncPosition) SetPosition(interpolatePosition ? interpolated.position : endGoal.position);
            //if (syncRotation) SetRotation(interpolateRotation ? interpolated.rotation : endGoal.rotation);
            //if (syncScale) SetScale(interpolateScale ? interpolated.scale : endGoal.scale);
        }

        [Rpc(SendTo.Server)]
        private void SyncToServerRpc()
        {

        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SyncTransformsRpc()
        {

        }

        public static List<(string, KeyDropdownValue)> GetAllProviderKeyValues()
        {
            return KeyUtility.GetAllProviderKeyValues();
        }
    }
}
