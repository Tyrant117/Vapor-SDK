using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace VaporNetcode
{
    [System.Serializable]
    public class NetTransformReliable : NetTransformBase
    {
        [Header("Sync Only If Changed")]
        [Tooltip("When true, changes are not sent unless greater than sensitivity values below.")]
        public bool onlySyncOnChange = true;

        uint sendIntervalCounter = 0;
        double lastSendIntervalTime = double.MinValue;

        [Tooltip("If we only sync on change, then we need to correct old snapshots if more time than sendInterval * multiplier has elapsed.\n\nOtherwise the first move will always start interpolating from the last move sequence's time, which will make it stutter when starting every time.")]
        public float onlySyncOnChangeCorrectionMultiplier = 2;

        [Header("Rotation")]
        [Tooltip("Sensitivity of changes needed before an updated state is sent over the network")]
        public float rotationSensitivity = 0.01f;
        [Tooltip("Apply smallest-three quaternion compression. This is lossy, you can disable it if the small rotation inaccuracies are noticeable in your project.")]
        public bool compressRotation = false;

        // delta compression is capable of detecting byte-level changes.
        // if we scale float position to bytes,
        // then small movements will only change one byte.
        // this gives optimal bandwidth.
        //   benchmark with 0.01 precision: 130 KB/s => 60 KB/s
        //   benchmark with 0.1  precision: 130 KB/s => 30 KB/s
        [Header("Precision")]
        [Tooltip("Position is rounded in order to drastically minimize bandwidth.\n\nFor example, a precision of 0.01 rounds to a centimeter. In other words, sub-centimeter movements aren't synced until they eventually exceeded an actual centimeter.\n\nDepending on how important the object is, a precision of 0.01-0.10 (1-10 cm) is recommended.\n\nFor example, even a 1cm precision combined with delta compression cuts the Benchmark demo's bandwidth in half, compared to sending every tiny change.")]
        [Range(0.00_01f, 1f)]                   // disallow 0 division. 1mm to 1m precision is enough range.
        public float positionPrecision = 0.01f; // 1 cm
        [Range(0.00_01f, 1f)]                   // disallow 0 division. 1mm to 1m precision is enough range.
        public float scalePrecision = 0.01f; // 1 cm

        // delta compression needs to remember 'last' to compress against
        protected Vector3Long lastSerializedPosition = Vector3Long.zero;
        protected Vector3Long lastDeserializedPosition = Vector3Long.zero;

        protected Vector3Long lastSerializedScale = Vector3Long.zero;
        protected Vector3Long lastDeserializedScale = Vector3Long.zero;

        // Used to store last sent snapshots
        protected TransformSnapshot last;

        protected int lastClientCount = 1;

        private IServerIdentity _serverID;
        private IClientIdentity _clientID;
        private bool _isServer;
        private bool _initialSend;

        public void Setup(IServerIdentity id)
        {
            _isServer = true;
            _initialSend = true;
            _serverID = id;
        }

        public void Setup(IClientIdentity id)
        {
            _isServer = false;
            _clientID = id;
        }

        public void Tick()
        {
            if (_isServer)
            {
                if (sendIntervalCounter == sendIntervalMultiplier && (!onlySyncOnChange || Changed(Construct())))
                {
                    // get current snapshot for broadcasting.
                    TransformSnapshot snapshot = Construct();

                    if (_initialSend)
                    {
                        if (last.remoteTime > 0) snapshot = last;
                        var snapshotMsg = new TransformSnapshotMessage()
                        {
                            NetID = _serverID.IsPeer ? null : _serverID.NetID,
                            Position = syncPosition ? snapshot.position : null,
                            Rotation = syncRotation ? snapshot.rotation : null,
                            Scale = syncScale ? snapshot.scale : null,
                        };
                        _initialSend = false;
                        //if (_serverID.IsPeer)
                        //{
                        //    UDPServer.Send(_serverID.Peer, snapshotMsg);
                        //}
                        UDPServer.SendToObservers(_serverID, snapshotMsg);
                    }
                    else
                    {
                        Vector3Long? deltaPos = null;
                        Vector3Long? deltaScale = null;
                        if (syncPosition)
                        {
                            // quantize -> delta -> varint
                            Compression.ScaleToLong(snapshot.position, positionPrecision, out Vector3Long quantized);
                            deltaPos = quantized - lastSerializedPosition;                            
                        }
                        if (syncScale)
                        {
                            // quantize -> delta -> varint
                            Compression.ScaleToLong(snapshot.scale, scalePrecision, out Vector3Long quantized);
                            deltaScale = quantized - lastSerializedScale;
                        }

                        var snapshotDeltaMsg = new TransformSnapshotDeltaMessage()
                        {
                            NetID = _serverID.IsPeer ? null : _serverID.NetID,
                            DeltaPosition = deltaPos ?? null,
                            Rotation = syncRotation ? snapshot.rotation : null,
                            DeltaScale = deltaScale ?? null,
                        };

                        //if (_serverID.IsPeer)
                        //{
                        //    UDPServer.Send(_serverID.Peer, snapshotDeltaMsg);
                        //}
                        UDPServer.SendToObservers(_serverID, snapshotDeltaMsg);
                    }

                    // save serialized as 'last' for next delta compression
                    if (syncPosition) Compression.ScaleToLong(snapshot.position, positionPrecision, out lastSerializedPosition);
                    if (syncScale) Compression.ScaleToLong(snapshot.scale, scalePrecision, out lastSerializedScale);

                    // set 'last'
                    last = snapshot;
                }

                _CheckLastSendTime();
            }
            else
            {
                _UpdateClient();
            }

            void _CheckLastSendTime()
            {
                // timeAsDouble not available in older Unity versions.
                if (AccurateInterval.Elapsed(Time.timeAsDouble, UDPServer.SendInterval, ref lastSendIntervalTime))
                {
                    if (sendIntervalCounter == sendIntervalMultiplier)
                    {
                        sendIntervalCounter = 0;
                    }

                    sendIntervalCounter++;
                }
            }

            void _UpdateClient()
            {
                // only while we have snapshots
                if (clientSnapshots.Count > 0)
                {
                    // step the interpolation without touching time.
                    // NetworkClient is responsible for time globally.
                    SnapshotInterpolation.StepInterpolation(
                        clientSnapshots,
                        NetTime.Time, // == NetworkClient.localTimeline from snapshot interpolation
                        out TransformSnapshot from,
                        out TransformSnapshot to,
                        out double t);

                    // interpolate & apply
                    TransformSnapshot computed = TransformSnapshot.Interpolate(from, to, t);
                    Apply(computed, to);
                }

                lastClientCount = clientSnapshots.Count;
            }
        }        

        // check if position / rotation / scale changed since last sync
        private bool Changed(TransformSnapshot current) =>
            // position is quantized and delta compressed.
            // only consider it changed if the quantized representation is changed.
            // careful: don't use 'serialized / deserialized last'. as it depends on sync mode etc.
            QuantizedChanged(last.position, current.position, positionPrecision) ||
            // rotation isn't quantized / delta compressed.
            // check with sensitivity.
            Quaternion.Angle(last.rotation, current.rotation) > rotationSensitivity ||
            // scale is quantized and delta compressed.
            // only consider it changed if the quantized representation is changed.
            // careful: don't use 'serialized / deserialized last'. as it depends on sync mode etc.
            QuantizedChanged(last.scale, current.scale, scalePrecision);

        // helper function to compare quantized representations of a Vector3
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool QuantizedChanged(Vector3 u, Vector3 v, float precision)
        {
            Compression.ScaleToLong(u, precision, out Vector3Long uQuantized);
            Compression.ScaleToLong(v, precision, out Vector3Long vQuantized);
            return uQuantized != vQuantized;
        }

        public void OnRecieveTransform(TransformSnapshotMessage msg)
        {
            OnServerToClientSync(msg.Position, msg.Rotation, msg.Scale);

            // save deserialized as 'last' for next delta compression
            if (syncPosition) Compression.ScaleToLong(msg.Position.Value, positionPrecision, out lastDeserializedPosition);
            if (syncScale) Compression.ScaleToLong(msg.Scale.Value, scalePrecision, out lastDeserializedScale);
        }

        public void OnRecieveTransformDelta(TransformSnapshotDeltaMessage msg)
        {
            Vector3? position = null;
            Vector3? scale = null;

            if (syncPosition)
            {
                Vector3Long quantized = lastDeserializedPosition + msg.DeltaPosition.Value;
                position = Compression.ScaleToFloat(quantized, positionPrecision);
            }
            if (syncScale)
            {
                Vector3Long quantized = lastDeserializedScale + msg.DeltaScale.Value;
                scale = Compression.ScaleToFloat(quantized, scalePrecision);
            }

            OnServerToClientSync(position, msg.Rotation, scale);

            // save deserialized as 'last' for next delta compression
            if (syncPosition) Compression.ScaleToLong(position.Value, positionPrecision, out lastDeserializedPosition);
            if (syncScale) Compression.ScaleToLong(scale.Value, scalePrecision, out lastDeserializedScale);
        }

        private void OnServerToClientSync(Vector3? position, Quaternion? rotation, Vector3? scale)
        {
            // 'only sync on change' needs a correction on every new move sequence.
            if (onlySyncOnChange &&
                NeedsCorrection(clientSnapshots, UDPClient.ServerPeer.RemoteTimestamp, UDPClient.SendInterval * sendIntervalMultiplier, onlySyncOnChangeCorrectionMultiplier))
            {
                RewriteHistory(
                    clientSnapshots,
                    UDPClient.ServerPeer.RemoteTimestamp,               // arrival remote timestamp. NOT remote timeline.
                    Time.timeAsDouble,                                  // Unity 2019 doesn't have timeAsDouble yet
                    UDPClient.SendInterval * sendIntervalMultiplier,
                    target.localPosition,
                    target.localRotation,
                    target.localScale);
            }

            // add a small timeline offset to account for decoupled arrival of
            // NetworkTime and NetworkTransform snapshots.
            // needs to be sendInterval. half sendInterval doesn't solve it.
            // https://github.com/MirrorNetworking/Mirror/issues/3427
            // remove this after LocalWorldState.
            AddSnapshot(clientSnapshots, UDPClient.ServerPeer.RemoteTimestamp + timeStampAdjustment + offset, position, rotation, scale);
        }

        // only sync on change /////////////////////////////////////////////////
        // snap interp. needs a continous flow of packets.
        // 'only sync on change' interrupts it while not changed.
        // once it restarts, snap interp. will interp from the last old position.
        // this will cause very noticeable stutter for the first move each time.
        // the fix is quite simple.

        // 1. detect if the remaining snapshot is too old from a past move.
        private static bool NeedsCorrection(SortedList<double, TransformSnapshot> snapshots, double remoteTimestamp, double bufferTime, double toleranceMultiplier) =>
                snapshots.Count == 1 &&
                remoteTimestamp - snapshots.Keys[0] >= bufferTime * toleranceMultiplier;

        // 2. insert a fake snapshot at current position,
        //    exactly one 'sendInterval' behind the newly received one.
        private static void RewriteHistory(
            SortedList<double, TransformSnapshot> snapshots,
            // timestamp of packet arrival, not interpolated remote time!
            double remoteTimeStamp,
            double localTime,
            double sendInterval,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            // clear the previous snapshot
            snapshots.Clear();

            // insert a fake one at where we used to be,
            // 'sendInterval' behind the new one.
            SnapshotInterpolation.InsertIfNotExists(snapshots, new TransformSnapshot(
                remoteTimeStamp - sendInterval, // arrival remote timestamp. NOT remote time.
                localTime - sendInterval,       // Unity 2019 doesn't have timeAsDouble yet
                position,
                rotation,
                scale
            ));
        }

        public override void Reset()
        {
            base.Reset();

            // reset delta
            lastSerializedPosition = Vector3Long.zero;
            lastDeserializedPosition = Vector3Long.zero;

            lastSerializedScale = Vector3Long.zero;
            lastDeserializedScale = Vector3Long.zero;

            // reset 'last' for delta too
            last = new TransformSnapshot(0, 0, Vector3.zero, Quaternion.identity, Vector3.zero);
        }
    }
}
