using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    [System.Serializable]
    public class NetTransformBase
    {
        // target transform to sync. can be on a child.
        [Header("Target")]
        [Tooltip("The Transform component to sync. May be on on this GameObject, or on a child.")]
        public Transform target;

        public readonly SortedList<double, TransformSnapshot> clientSnapshots = new();
        public readonly SortedList<double, TransformSnapshot> serverSnapshots = new();

        // selective sync //////////////////////////////////////////////////////
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
        public uint sendIntervalMultiplier = 1; // not implemented yet

        [Header("Timeline Offset")]
        [Tooltip("Add a small timeline offset to account for decoupled arrival of NetTime and NetworkTransform snapshots.\nfixes: https://github.com/MirrorNetworking/Mirror/issues/3427")]
        public bool timelineOffset = false;

        // Ninja's Notes on offset & mulitplier:
        // 
        // In a no multiplier scenario:
        // 1. Snapshots are sent every frame (frame being 1 NM send interval).
        // 2. Time Interpolation is set to be 'behind' by 2 frames times.
        // In theory where everything works, we probably have around 2 snapshots before we need to interpolate snapshots. From NT perspective, we should always have around 2 snapshots ready, so no stutter.
        // 
        // In a multiplier scenario:
        // 1. Snapshots are sent every 10 frames.
        // 2. Time Interpolation remains 'behind by 2 frames'.
        // When everything works, we are receiving NT snapshots every 10 frames, but start interpolating after 2. 
        // Even if I assume we had 2 snapshots to begin with to start interpolating (which we don't), by the time we reach 13th frame, we are out of snapshots, and have to wait 7 frames for next snapshot to come. This is the reason why we absolutely need the timestamp adjustment. We are starting way too early to interpolate. 
        //
        protected double timeStampAdjustment => UDPServer.SendInterval * (sendIntervalMultiplier - 1);
        protected double offset => timelineOffset ? UDPServer.SendInterval * sendIntervalMultiplier : 0;

        // debugging ///////////////////////////////////////////////////////////
        [Header("Debug")]
        public bool showGizmos;
        public bool showOverlay;
        public Color overlayColor = new (0, 0, 0, 0.5f);

        // snapshot functions //////////////////////////////////////////////////
        // construct a snapshot of the current state
        // => internal for testing
        protected virtual TransformSnapshot Construct()
        {
            // NetworkTime.localTime for double precision until Unity has it too
            return new TransformSnapshot(
                // our local time is what the other end uses as remote time
                Time.timeAsDouble, // Unity 2019 doesn't have timeAsDouble yet
                0,                     // the other end fills out local time itself
                target.localPosition,
                target.localRotation,
                target.localScale
            );
        }

        protected void AddSnapshot(SortedList<double, TransformSnapshot> snapshots, double timeStamp, Vector3? position, Quaternion? rotation, Vector3? scale)
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

        // apply a snapshot to the Transform.
        // -> start, end, interpolated are all passed in caes they are needed
        // -> a regular game would apply the 'interpolated' snapshot
        // -> a board game might want to jump to 'goal' directly
        // (it's easier to always interpolate and then apply selectively,
        //  instead of manually interpolating x, y, z, ... depending on flags)
        // => internal for testing
        //
        // NOTE: stuck detection is unnecessary here.
        //       we always set transform.position anyway, we can't get stuck.
        protected virtual void Apply(TransformSnapshot interpolated, TransformSnapshot endGoal)
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

        // client->server teleport to force position without interpolation.
        // otherwise it would interpolate to a (far away) new position.
        // => manually calling Teleport is the only 100% reliable solution.
        public void Teleport(Vector3 destination, Quaternion? rotation = null)
        {
            UDPClient.Send(new TeleportMessage() { Position = destination, Rotation = rotation });
        }

        // common Teleport code for client->server and server->client
        protected virtual void OnTeleport(TeleportMessage msg)
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

            // TODO
            // what if we still receive a snapshot from before the interpolation?
            // it could easily happen over unreliable.
            // -> maybe add destination as first entry?
        }

        public virtual void Reset()
        {
            // disabled objects aren't updated anymore.
            // so let's clear the buffers.
            serverSnapshots.Clear();
            clientSnapshots.Clear();
        }

        protected virtual void OnEnable()
        {
            Reset();
        }

        protected virtual void OnDisable()
        {
            Reset();
        }

//        // OnGUI allocates even if it does nothing. avoid in release.
//#if UNITY_EDITOR || DEVELOPMENT_BUILD
//        // debug ///////////////////////////////////////////////////////////////
//        protected virtual void OnGUI()
//        {
//            if (!showOverlay) return;
//            if (!Camera.main) return;

//            // show data next to player for easier debugging. this is very useful!
//            // IMPORTANT: this is basically an ESP hack for shooter games.
//            //            DO NOT make this available with a hotkey in release builds
//            if (!Debug.isDebugBuild) return;

//            // project position to screen
//            Vector3 point = Camera.main.WorldToScreenPoint(target.position);

//            // enough alpha, in front of camera and in screen?
//            if (point.z >= 0 && Convienence.IsPointInScreen(point))
//            {
//                GUI.color = overlayColor;
//                GUILayout.BeginArea(new Rect(point.x, Screen.height - point.y, 200, 100));

//                // always show both client & server buffers so it's super
//                // obvious if we accidentally populate both.
//                GUILayout.Label($"Server Buffer:{serverSnapshots.Count}");
//                GUILayout.Label($"Client Buffer:{clientSnapshots.Count}");

//                GUILayout.EndArea();
//                GUI.color = Color.white;
//            }
//        }

//        protected virtual void DrawGizmos(SortedList<double, TransformSnapshot> buffer, bool server)
//        {
//            // only draw if we have at least two entries
//            if (buffer.Count < 2) return;

//            // calculate threshold for 'old enough' snapshots
//            double threshold = server ? NetworkTime.localTime - UDPServer.BufferTime : NetworkTime.localTime - UDPClient.BufferTime;
//            Color oldEnoughColor = new (0, 1, 0, 0.5f);
//            Color notOldEnoughColor = new (0.5f, 0.5f, 0.5f, 0.3f);

//            // draw the whole buffer for easier debugging.
//            // it's worth seeing how much we have buffered ahead already
//            for (int i = 0; i < buffer.Count; ++i)
//            {
//                // color depends on if old enough or not
//                TransformSnapshot entry = buffer.Values[i];
//                bool oldEnough = entry.localTime <= threshold;
//                Gizmos.color = oldEnough ? oldEnoughColor : notOldEnoughColor;
//                Gizmos.DrawCube(entry.position, Vector3.one);
//            }

//            // extra: lines between start<->position<->goal
//            Gizmos.color = Color.green;
//            Gizmos.DrawLine(buffer.Values[0].position, target.position);
//            Gizmos.color = Color.white;
//            Gizmos.DrawLine(target.position, buffer.Values[1].position);
//        }

//        protected virtual void OnDrawGizmos()
//        {
//            // This fires in edit mode but that spams NRE's so check isPlaying
//            if (!Application.isPlaying) return;
//            if (!showGizmos) return;

//            if (UDPServer.isRunning) DrawGizmos(serverSnapshots, true);
//            if (UDPClient.IsActive) DrawGizmos(clientSnapshots, false);
//        }
//#endif
    }
}
