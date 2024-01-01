using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Vapor;
using VaporInspector;

namespace VaporEvents
{
    public class ProvidesComponent : MonoBehaviour
    {
        [SerializeField, ValueDropdown("@GetAllProviderKeys", searchable: true)]
        private ProviderKeySo key;
        [SerializeField]
        private Component component;

        private void OnEnable()
        {
            if (key != null)
            {
                var t = component.GetType();
                ProviderBus.Get<ProviderData<Component>>(key).Subscribe(OnComponentRequested);
            }
        }

        private void OnDisable()
        {
            if (key != null)
            {
                ProviderBus.Get<ProviderData<Component>>(key).Unsubscribe(OnComponentRequested);
            }
        }

        private Component OnComponentRequested()
        {
            return component;
        }

        public static List<(string, ProviderKeySo)> GetAllProviderKeys()
        {
            var allProviderKeys = RuntimeAssetDatabaseUtility.FindAssetsByType<ProviderKeySo>();
            return allProviderKeys.Select(so => (so.DisplayName, so)).ToList();
        }
    }
}
