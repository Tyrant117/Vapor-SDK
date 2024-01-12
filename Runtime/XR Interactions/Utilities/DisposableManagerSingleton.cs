using System;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;

namespace VaporXR.Utilities
{
    /// <summary>
    /// Manager singleton for <see cref="IDisposable"/> objects to help ensure they are disposed at the end of the application's life.
    /// </summary>
    public sealed class DisposableManagerSingleton : MonoBehaviour
    {
        private static DisposableManagerSingleton Instance => Initialize();
        private static DisposableManagerSingleton s_disposableManagerSingleton;

        private readonly HashSetList<IDisposable> _disposables = new HashSetList<IDisposable>();

        private static DisposableManagerSingleton Initialize()
        {
            if (s_disposableManagerSingleton != null) return s_disposableManagerSingleton;
            
            var singleton = new GameObject("[DisposableManagerSingleton]");
            DontDestroyOnLoad(singleton);

            s_disposableManagerSingleton = singleton.AddComponent<DisposableManagerSingleton>();

            return s_disposableManagerSingleton;
        }

        private void Awake()
        {
            if (s_disposableManagerSingleton != null && s_disposableManagerSingleton != this)
            {
                Destroy(this);
                return;
            }

            if (s_disposableManagerSingleton == null)
            {
                s_disposableManagerSingleton = this;
            }
        }

        private void OnDestroy()
        {
            DisposeAll();
        }

        private void OnApplicationQuit()
        {
            DisposeAll();
        }

        private void DisposeAll()
        {
            var disposableList = _disposables.AsList();
            foreach (var disposable in disposableList)
            {
                disposable.Dispose();
            }

            _disposables.Clear();
        }

        /// <summary>
        /// Register disposable to auto dispose on Destroy or application quit.
        /// </summary>
        /// <param name="disposableToRegister">Disposable to auto-dispose when application quits.</param>
        public static void RegisterDisposable(IDisposable disposableToRegister)
        {
            Instance._disposables.Add(disposableToRegister);
        }
    }
}