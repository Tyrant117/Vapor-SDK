using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace VaporNetcode
{
    [System.Serializable]
    public class NetTransform
    {
        // target transform to sync. can be on a child.
        [Header("Target")]
        [Tooltip("The Transform component to sync. May be on on this GameObject, or on a child.")]
        public Transform target;        

        [Header("Selective Sync [Don't change these at Runtime]")]
        public bool syncPosition = true;  // do not change at runtime!
        public bool syncRotation = true;  // do not change at runtime!
        public bool syncScale = false; // do not change at runtime! rare. off by default.

        // interpolation is on by default, but can be disabled to jump to
        // the destination immediately. some projects need this.
        [Header("Interpolation")]
        [Tooltip("Set to false to have a snap-like effect on position movement.")]
        public bool interpolatePosition = true;
        [Tooltip("Set to false to have a snap-like effect on rotations.")]
        public bool interpolateRotation = true;
        [Tooltip("Set to false to remove scale smoothing. Example use-case: Instant flipping of sprites that use -X and +X for direction.")]
        public bool interpolateScale = true;

        [Header("Send Interval Multiplier")]
        [Tooltip("Check/Sync every multiple of Network Manager send interval (= 1 / NM Send Rate), instead of every send interval.")]
        [Range(1, 120)]
        public uint sendIntervalMultiplier = 1;

        [Header("Timeline Offset")]
        [Tooltip("Add a small timeline offset to account for decoupled arrival of NetTime and NetworkTransform snapshots.\nfixes: https://github.com/MirrorNetworking/Mirror/issues/3427")]
        public bool timelineOffset = false;

        [Header("Sync Only If Changed")]
        [Tooltip("When true, changes are not sent unless greater than sensitivity values below.")]
        public bool onlySyncOnChange = true;        

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

        // Ninja's Notes on offset & mulitplier:
        // In a multiplier scenario:
        // 1. Snapshots are sent every 10 frames.
        // 2. Time Interpolation remains 'behind by 2 frames'.
        // When everything works, we are receiving NT snapshots every 10 frames, but start interpolating after 2. 
        // Even if I assume we had 2 snapshots to begin with to start interpolating (which we don't), by the time we reach 13th frame, we are out of snapshots, and have to wait 7 frames for next snapshot to come. This is the reason why we absolutely need the timestamp adjustment. We are starting way too early to interpolate. 
        //
        protected double TimeStampAdjustment => UDPClient.ServerSendInterval * (sendIntervalMultiplier - 1);
        protected double Offset => timelineOffset ? UDPClient.ServerSendInterval * sendIntervalMultiplier : 0;

        public readonly SortedList<double, TransformSnapshot> clientSnapshots = new();
        public readonly SortedList<double, TransformSnapshot> serverSnapshots = new();

        uint sendIntervalCounter = 0;

        // Used to store last sent snapshots
        protected TransformSnapshot last;
        protected int lastClientCount = 1;
        private TransformSync _sync;
        private bool _isServer;

        public TransformSync Setup(bool isServer, int syncID)
        {
            _isServer = isServer;
            _sync = new TransformSync(syncID, isServer, false, syncPosition, syncRotation, syncScale, compressRotation);
            if (!_isServer)
            {
                _sync.TransformChanged += OnServerToClientSync;
            }
            return _sync;
        }

        public TransformSync Setup(bool isServer, TransformSync sync)
        {
            _isServer = isServer;
            _sync = sync;
            if (!_isServer)
            {
                _sync.TransformChanged += OnServerToClientSync;
            }
            return _sync;
        }

        public void Tick()
        {
            if (_isServer)
            {
                if (sendIntervalCounter == sendIntervalMultiplier && (!onlySyncOnChange || Changed(Construct())))
                {
                    // get current snapshot for broadcasting.
                    TransformSnapshot snapshot = Construct();
                    if (syncPosition)
                    {
                        _sync.Position.ExternalSet(snapshot.position);
                    }
                    if (syncRotation)
                    {
                        if (compressRotation)
                        {
                            _sync.CompressedRotation.ExternalSet(snapshot.rotation);
                        }
                        else
                        {
                            _sync.Rotation.ExternalSet(snapshot.rotation);
                        }
                    }
                    if (syncScale)
                    {
                        _sync.Scale.ExternalSet(snapshot.scale);
                    }

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
                if (sendIntervalCounter == sendIntervalMultiplier)
                {
                    sendIntervalCounter = 0;
                }

                sendIntervalCounter++;
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

        #region - Syncing -
        // snapshot functions //////////////////////////////////////////////////
        // construct a snapshot of the current state
        // => internal for testing
        private TransformSnapshot Construct()
        {
            return new TransformSnapshot(
                // our local time is what the other end uses as remote time
                Time.timeAsDouble,
                0,                     // the other end fills out local time itself
                target.localPosition,
                target.localRotation,
                target.localScale
            );
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
            //Debug.Log($"Client Syncing Snapshot {position} {rotation} {scale}");

            // add a small timeline offset to account for decoupled arrival of
            // NetworkTime and NetworkTransform snapshots.
            // needs to be sendInterval. half sendInterval doesn't solve it.
            // https://github.com/MirrorNetworking/Mirror/issues/3427
            // remove this after LocalWorldState.
            AddSnapshot(clientSnapshots, UDPClient.ServerPeer.RemoteTimestamp + TimeStampAdjustment + Offset, position, rotation, scale);
        }

        private void AddSnapshot(SortedList<double, TransformSnapshot> snapshots, double timeStamp, Vector3? position, Quaternion? rotation, Vector3? scale)
        {
            // position, rotation, scale can have no value if same as last time.
            // saves bandwidth.
            // but we still need to feed it to snapshot interpolation. we can't
            // just have gaps in there if nothing has changed. for example, if
            //   client sends snapshot at t=0
            //   client sends nothing for 10s because not moved
            //   client sends snapshot at t=10
            // then the server would assume that it's one super slow move and
            // replay it for 10 seconds.
            if (!position.HasValue)
            {
                position = snapshots.Count > 0 ? snapshots.Values[snapshots.Count - 1].position : target.localPosition;
            }

            if (!rotation.HasValue)
            {
                rotation = snapshots.Count > 0 ? snapshots.Values[snapshots.Count - 1].rotation : target.localRotation;
            }

            if (!scale.HasValue)
            {
                scale = snapshots.Count > 0 ? snapshots.Values[snapshots.Count - 1].scale : target.localScale;
            }

            // insert transform snapshot
            SnapshotInterpolation.InsertIfNotExists(snapshots, new TransformSnapshot(
                timeStamp, // arrival remote timestamp. NOT remote time.
                Time.timeAsDouble, // Unity 2019 doesn't have timeAsDouble yet
                position.Value,
                rotation.Value,
                scale.Value
            ));
        }
        
        private void Apply(TransformSnapshot interpolated, TransformSnapshot endGoal)
        {
            // local position/rotation for VR support
            //
            // if syncPosition/Rotation/Scale is disabled then we received nulls
            // -> current position/rotation/scale would've been added as snapshot
            // -> we still interpolated
            // -> but simply don't apply it. if the user doesn't want to sync
            //    scale, then we should not touch scale etc.

            if (syncPosition)
                target.localPosition = interpolatePosition ? interpolated.position : endGoal.position;

            if (syncRotation)
                target.localRotation = interpolateRotation ? interpolated.rotation : endGoal.rotation;

            if (syncScale)
                target.localScale = interpolateScale ? interpolated.scale : endGoal.scale;
        }
        #endregion       

        #region - Teleporting -
        public void Teleport(Vector3 destination, Quaternion? rotation = null)
        {
            UDPClient.Send(new TeleportMessage() { Position = destination, Rotation = rotation });
        }

        private void OnTeleport(TeleportMessage msg)
        {
            // reset any in-progress interpolation & buffers
            Reset();

            // set the new position.
            // interpolation will automatically continue.
            if (msg.Rotation.HasValue)
            {
                target.SetPositionAndRotation(msg.Position, msg.Rotation.Value);
            }
            else
            {
                target.position = msg.Position;
            }
        }
        #endregion

        #region - Helpers -
        public virtual void Reset()
        {
            // disabled objects aren't updated anymore.
            // so let's clear the buffers.
            serverSnapshots.Clear();
            clientSnapshots.Clear();

            // reset 'last' for delta too
            last = new TransformSnapshot(0, 0, Vector3.zero, Quaternion.identity, Vector3.zero);
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
        #endregion
    }
}
