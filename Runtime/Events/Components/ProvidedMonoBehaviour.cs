using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Vapor.Utilities;
using VaporInspector;
using VaporKeys;

namespace VaporEvents
{
    public abstract class ProvidedMonoBehaviour : MonoBehaviour
    {
        [BoxGroup("Provided Key", order: -10000), SerializeField, ValueDropdown("@GetAllProviderKeyValues", searchable: true), IgnoreCustomDrawer]
        protected KeyDropdownValue Key;
        
        protected virtual void OnEnable()
        {
            if (Key.IsNone) return;
            
            ProviderBus.Get<ProviderData<Component>>(Key).Subscribe(OnComponentRequested);
        }

        protected virtual void OnDisable()
        {
            if (Key.IsNone) return;
            
            ProviderBus.Get<ProviderData<Component>>(Key).Unsubscribe(OnComponentRequested);
        }

        protected Component OnComponentRequested()
        {
            return this;
        }

        /// <summary>
        /// Returns the <see cref="ProvidedMonoBehaviour"/> cast to its inherited class.
        /// The result should be cached if used more than once.
        /// </summary>
        /// <typeparam name="T">The type to cast to. Must inherit from <see cref="ProvidedMonoBehaviour"/></typeparam>
        /// <returns>T: Cannot return null</returns>
        public T As<T>() where T : ProvidedMonoBehaviour
        {
            Assert.IsNotNull((T)this, $"Type {TooltipMarkup.ClassMarkup(nameof(T))} must inherit from {TooltipMarkup.ClassMarkup(nameof(ProvidedMonoBehaviour))}");
            return (T)this;
        }

        public static List<(string, KeyDropdownValue)> GetAllProviderKeyValues()
        {
            return KeyUtility.GetAllProviderKeyValues();
        }
    }
}
