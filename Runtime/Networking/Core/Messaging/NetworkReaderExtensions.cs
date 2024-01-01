using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VaporNetcode
{
    // Mirror's Weaver automatically detects all NetworkReader function types,
    // but they do all need to be extensions.
    public static class NetworkReaderExtensions
    {
        public static byte ReadByte(this NetworkReader reader) => reader.ReadBlittable<byte>();
        public static byte? ReadByteNullable(this NetworkReader reader) => reader.ReadBlittableNullable<byte>();

        public static sbyte ReadSByte(this NetworkReader reader) => reader.ReadBlittable<sbyte>();
        public static sbyte? ReadSByteNullable(this NetworkReader reader) => reader.ReadBlittableNullable<sbyte>();

        // bool is not blittable. read as ushort.
        public static char ReadChar(this NetworkReader reader) => (char)reader.ReadBlittable<ushort>();
        public static char? ReadCharNullable(this NetworkReader reader) => (char?)reader.ReadBlittableNullable<ushort>();

        // bool is not blittable. read as byte.
        public static bool ReadBool(this NetworkReader reader) => reader.ReadBlittable<byte>() != 0;
        public static bool? ReadBoolNullable(this NetworkReader reader)
        {
            byte? value = reader.ReadBlittableNullable<byte>();
            return value.HasValue ? (value.Value != 0) : default(bool?);
        }

        public static short ReadShort(this NetworkReader reader) => (short)reader.ReadUShort();
        public static short? ReadShortNullable(this NetworkReader reader) => reader.ReadBlittableNullable<short>();

        public static ushort ReadUShort(this NetworkReader reader) => reader.ReadBlittable<ushort>();
        public static ushort? ReadUShortNullable(this NetworkReader reader) => reader.ReadBlittableNullable<ushort>();

        public static int ReadInt(this NetworkReader reader) => reader.ReadBlittable<int>();
        public static int? ReadIntNullable(this NetworkReader reader) => reader.ReadBlittableNullable<int>();

        public static uint ReadUInt(this NetworkReader reader) => reader.ReadBlittable<uint>();
        public static uint? ReadUIntNullable(this NetworkReader reader) => reader.ReadBlittableNullable<uint>();

        public static long ReadLong(this NetworkReader reader) => reader.ReadBlittable<long>();
        public static long? ReadLongNullable(this NetworkReader reader) => reader.ReadBlittableNullable<long>();

        public static ulong ReadULong(this NetworkReader reader) => reader.ReadBlittable<ulong>();
        public static ulong? ReadULongNullable(this NetworkReader reader) => reader.ReadBlittableNullable<ulong>();

        public static float ReadFloat(this NetworkReader reader) => reader.ReadBlittable<float>();
        public static float? ReadFloatNullable(this NetworkReader reader) => reader.ReadBlittableNullable<float>();

        public static double ReadDouble(this NetworkReader reader) => reader.ReadBlittable<double>();
        public static double? ReadDoubleNullable(this NetworkReader reader) => reader.ReadBlittableNullable<double>();

        public static decimal ReadDecimal(this NetworkReader reader) => reader.ReadBlittable<decimal>();
        public static decimal? ReadDecimalNullable(this NetworkReader reader) => reader.ReadBlittableNullable<decimal>();

        /// <exception cref="T:System.ArgumentException">if an invalid utf8 string is sent</exception>
        public static string ReadString(this NetworkReader reader)
        {
            // read number of bytes
            ushort size = reader.ReadUShort();

            // null support, see NetworkWriter
            if (size == 0)
                return null;

            ushort realSize = (ushort)(size - 1);

            // make sure it's within limits to avoid allocation attacks etc.
            if (realSize > NetworkWriter.MaxStringLength)
                throw new EndOfStreamException($"NetworkReader.ReadString - Value too long: {realSize} bytes. Limit is: {NetworkWriter.MaxStringLength} bytes");

            ArraySegment<byte> data = reader.ReadBytesSegment(realSize);

            // convert directly from buffer to string via encoding
            // throws in case of invalid utf8.
            // see test: ReadString_InvalidUTF8()
            return reader.encoding.GetString(data.Array, data.Offset, data.Count);
        }

        /// <exception cref="T:OverflowException">if count is invalid</exception>
        public static byte[] ReadBytesAndSize(this NetworkReader reader)
        {
            // count = 0 means the array was null
            // otherwise count -1 is the length of the array
            uint count = reader.ReadUInt();
            // Use checked() to force it to throw OverflowException if data is invalid
            return count == 0 ? null : reader.ReadBytes(checked((int)(count - 1u)));
        }

        public static byte[] ReadBytes(this NetworkReader reader, int count)
        {
            byte[] bytes = new byte[count];
            reader.ReadBytes(bytes, count);
            return bytes;
        }

        /// <exception cref="T:OverflowException">if count is invalid</exception>
        public static ArraySegment<byte> ReadBytesAndSizeSegment(this NetworkReader reader)
        {
            // count = 0 means the array was null
            // otherwise count - 1 is the length of the array
            uint count = reader.ReadUInt();
            // Use checked() to force it to throw OverflowException if data is invalid
            return count == 0 ? default : reader.ReadBytesSegment(checked((int)(count - 1u)));
        }

        public static Vector2 ReadVector2(this NetworkReader reader) => reader.ReadBlittable<Vector2>();
        public static Vector2? ReadVector2Nullable(this NetworkReader reader) => reader.ReadBlittableNullable<Vector2>();

        public static Vector3 ReadVector3(this NetworkReader reader) => reader.ReadBlittable<Vector3>();
        public static Vector3? ReadVector3Nullable(this NetworkReader reader) => reader.ReadBlittableNullable<Vector3>();

        public static Vector4 ReadVector4(this NetworkReader reader) => reader.ReadBlittable<Vector4>();
        public static Vector4? ReadVector4Nullable(this NetworkReader reader) => reader.ReadBlittableNullable<Vector4>();

        public static Vector2Int ReadVector2Int(this NetworkReader reader) => reader.ReadBlittable<Vector2Int>();
        public static Vector2Int? ReadVector2IntNullable(this NetworkReader reader) => reader.ReadBlittableNullable<Vector2Int>();

        public static Vector3Int ReadVector3Int(this NetworkReader reader) => reader.ReadBlittable<Vector3Int>();
        public static Vector3Int? ReadVector3IntNullable(this NetworkReader reader) => reader.ReadBlittableNullable<Vector3Int>();

        public static Color ReadColor(this NetworkReader reader) => reader.ReadBlittable<Color>();
        public static Color? ReadColorNullable(this NetworkReader reader) => reader.ReadBlittableNullable<Color>();

        public static Color32 ReadColor32(this NetworkReader reader) => reader.ReadBlittable<Color32>();
        public static Color32? ReadColor32Nullable(this NetworkReader reader) => reader.ReadBlittableNullable<Color32>();

        public static Quaternion ReadQuaternion(this NetworkReader reader) => reader.ReadBlittable<Quaternion>();
        public static Quaternion? ReadQuaternionNullable(this NetworkReader reader) => reader.ReadBlittableNullable<Quaternion>();

        // Rect is a struct with properties instead of fields
        public static Rect ReadRect(this NetworkReader reader) => new Rect(reader.ReadVector2(), reader.ReadVector2());
        public static Rect? ReadRectNullable(this NetworkReader reader) => reader.ReadBool() ? ReadRect(reader) : default(Rect?);

        // Plane is a struct with properties instead of fields
        public static Plane ReadPlane(this NetworkReader reader) => new Plane(reader.ReadVector3(), reader.ReadFloat());
        public static Plane? ReadPlaneNullable(this NetworkReader reader) => reader.ReadBool() ? ReadPlane(reader) : default(Plane?);

        // Ray is a struct with properties instead of fields
        public static Ray ReadRay(this NetworkReader reader) => new Ray(reader.ReadVector3(), reader.ReadVector3());
        public static Ray? ReadRayNullable(this NetworkReader reader) => reader.ReadBool() ? ReadRay(reader) : default(Ray?);

        public static Matrix4x4 ReadMatrix4x4(this NetworkReader reader) => reader.ReadBlittable<Matrix4x4>();
        public static Matrix4x4? ReadMatrix4x4Nullable(this NetworkReader reader) => reader.ReadBlittableNullable<Matrix4x4>();

        public static Guid ReadGuid(this NetworkReader reader)
        {
#if !UNITY_2021_3_OR_NEWER
            // Unity 2019 doesn't have Span yet
            return new Guid(reader.ReadBytes(16));
#else
            // ReadBlittable(Guid) isn't safe. see ReadBlittable comments.
            // Guid is Sequential, but we can't guarantee packing.
            if (reader.Remaining >= 16)
            {
                ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(reader.buffer.Array, reader.buffer.Offset + reader.Position, 16);
                reader.Position += 16;
                return new Guid(span);
            }
            throw new EndOfStreamException($"ReadGuid out of range: {reader}");
#endif
        }
        public static Guid? ReadGuidNullable(this NetworkReader reader) => reader.ReadBool() ? ReadGuid(reader) : default(Guid?);

        // while SyncList<T> is recommended for NetworkBehaviours,
        // structs may have .List<T> members which weaver needs to be able to
        // fully serialize for NetworkMessages etc.
        // note that Weaver/Readers/GenerateReader() handles this manually.
        public static int? BeginReadList(this NetworkReader reader)
        {
            int length = reader.ReadInt();
            return length < 0 ? null : length;
        }

        public static void ReadList<T>(this NetworkReader reader, List<T> list) where T : struct, ISerializablePacket
        {
            for (int i = 0; i < list.Count; i++)
            {
                var copy = PacketManager.Deserialize<T>(reader);
                list[i] = copy;
            }
        }

        public static int? BeginReadArray(this NetworkReader reader)
        {
            int length = reader.ReadInt();
            return length < 0 ? null : length;
        }

        public static void ReadArray<T>(this NetworkReader reader, T[] array) where T : struct, ISerializablePacket
        {
            // todo throw an exception for other negative values (we never write them, likely to be attacker)

            // this assumes that a reader for T reads at least 1 bytes
            // we can't know the exact size of T because it could have a user created reader
            // NOTE: don't add to length as it could overflow if value is int.max
            if (array.Length > reader.Remaining)
            {
                throw new EndOfStreamException($"Received array that is too large: {array.Length}");
            }

            for (int i = 0; i < array.Length; i++)
            {
                var copy = PacketManager.Deserialize<T>(reader);
                array[i] = copy;
            }
        }

        public static Uri ReadUri(this NetworkReader reader)
        {
            string uriString = reader.ReadString();
            return (string.IsNullOrWhiteSpace(uriString) ? null : new Uri(uriString));
        }

        public static Texture2D ReadTexture2D(this NetworkReader reader)
        {
            // TODO allocation protection when sending textures to server.
            //      currently can allocate 32k x 32k x 4 byte = 3.8 GB

            // support 'null' textures for [SyncVar]s etc.
            // https://github.com/vis2k/Mirror/issues/3144
            short width = reader.ReadShort();
            if (width == -1) return null;

            // read height
            short height = reader.ReadShort();
            Texture2D texture2D = new Texture2D(width, height);

            // read pixel content
            int length = reader.ReadInt();

            //  we write -1 for null
            if (length < 0)
            {
                return null;
            }

            // todo throw an exception for other negative values (we never write them, likely to be attacker)

            // this assumes that a reader for T reads at least 1 bytes
            // we can't know the exact size of T because it could have a user created reader
            // NOTE: don't add to length as it could overflow if value is int.max
            if (length > reader.Remaining)
            {
                throw new EndOfStreamException($"Received array that is too large: {length}");
            }

            Color32[] pixels = new Color32[length];
            for (int i = 0; i < length; i++)
            {
                pixels[i] = reader.ReadColor32();
            }

            texture2D.SetPixels32(pixels);
            texture2D.Apply();
            return texture2D;
        }

        public static Sprite ReadSprite(this NetworkReader reader)
        {
            // support 'null' textures for [SyncVar]s etc.
            // https://github.com/vis2k/Mirror/issues/3144
            Texture2D texture = reader.ReadTexture2D();
            if (texture == null) return null;

            // otherwise create a valid sprite
            return Sprite.Create(texture, reader.ReadRect(), reader.ReadVector2());
        }

        public static DateTime ReadDateTime(this NetworkReader reader) => DateTime.FromOADate(reader.ReadDouble());
        public static DateTime? ReadDateTimeNullable(this NetworkReader reader) => reader.ReadBool() ? ReadDateTime(reader) : default(DateTime?);
    }
}
