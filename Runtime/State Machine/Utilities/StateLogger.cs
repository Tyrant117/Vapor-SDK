using System.Collections.Generic;
using UnityEngine;

namespace VaporStateMachine
{
    [System.Serializable]
    public class StateLogger
    {
        private const int SNAPSHOT_SIZE = 100;

        [System.Serializable]
        public class LayerLog
        {
            [SerializeField]
            private List<string> _log = new(SNAPSHOT_SIZE);
            [SerializeField]
            private List<LayerLog> _layers = new();

            public int Count => _log.Count;

            public LayerLog GetOrCreateSubLayer()
            {
                var ll = new LayerLog();
                _layers.Add(ll);
                return ll;
            }

            public void Reset()
            {
                Clear();
                foreach (var layer in _layers)
                {
                    layer.Reset();
                }
            }

            public void Clear()
            {
                _log.Clear();
            }

            public void LogEnter(string state)
            {
                if (_log.Count >= SNAPSHOT_SIZE - 1)
                {
                    Clear();
                }
                _log.Add($"[Entered] {state}");
            }

            public void LogExit(string state)
            {
                if (_log.Count >= SNAPSHOT_SIZE - 1)
                {
                    Clear();
                }
                _log.Add($"[Exited] {state}");
            }
        }

        [SerializeField]
        private LayerLog _layer;

        public StateLogger()
        {
            _layer = new();
        }

        public void Reset()
        {
            _layer.Reset();
        }

        public LayerLog GetSubLayerLog()
        {
            return _layer;
        }
    }
}
