using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaporNetcode
{
    public class TransformSync : SyncClass
    {
        public const int PositionKey = 1;
        public const int RotationKey = 2;
        public const int ScaleKey = 3;

        public readonly Vector3DeltaCompressedField Position;
        public readonly QuaternionField Rotation;
        public readonly CompressedQuaternionField CompressedRotation;
        public readonly Vector3DeltaCompressedField Scale;

        private readonly bool syncPos;
        private readonly bool syncRot;
        private readonly bool syncScale;
        private readonly bool compressRot;

        public Vector3 AtPosition => syncPos ? Position : Vector3.zero;
        public Quaternion AtRotaton => syncRot ? compressRot ? CompressedRotation : Rotation : Quaternion.identity;
        public Vector3 AtScale => syncScale ? Scale : Vector3.one;

        public event Action<Vector3?, Quaternion?, Vector3?> TransformChanged;

        public TransformSync(int unqiueID, bool isServer, bool saveValue, bool syncPos = true, bool syncRot = true, bool syncScale = false, bool compressRot = false) : base(unqiueID, isServer, saveValue)
        {
            Type = SyncClassID<TransformSync>.ID;

            this.syncPos = syncPos;
            this.syncRot = syncRot;
            this.syncScale = syncScale;
            this.compressRot = compressRot;

            if (syncPos)
            {
                Position = new Vector3DeltaCompressedField(this, PositionKey, false, default);
                AddField(Position);
            }

            if (syncRot)
            {
                if (compressRot)
                {
                    CompressedRotation = new CompressedQuaternionField(this, RotationKey, false, default);
                    AddField(CompressedRotation);
                }
                else
                {
                    Rotation = new QuaternionField(this, RotationKey, false, default);
                    AddField(Rotation);
                }
            }

            if (syncScale)
            {
                Scale = new Vector3DeltaCompressedField(this, ScaleKey, false, default);
                AddField(Scale);
            }
            Changed += OnSync;            
        }

        private void OnSync(SyncClass obj)
        {
            Vector3? pos = null;
            Quaternion? rot = null;
            Vector3? scl = null;

            if (syncPos)
            {
                pos = Position.Value;
            }
            if (syncRot)
            {
                rot = compressRot ? CompressedRotation.Value : Rotation.Value;
            }
            if (syncScale)
            {
                scl = Scale.Value;
            }
            TransformChanged?.Invoke(pos, rot, scl);
        }
    }
}
