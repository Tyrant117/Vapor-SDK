
namespace VaporNetcode
{
    public enum SyncFieldType
    {
        Byte,   // Byte     - 1 byte
        Short,  // Short    - 2 bytes
        UShort,
        Int,  // Int      - 4 bytes
        UInt,  // Int      - 4 bytes
        Float, // Float    - 4 bytes
        Long,  // long     - 8 bytes
        ULong, // ulong    - 8 bytes
        Double, // double   - 8 bytes
        Vector2,// Vector2  - 8 bytes
        Vector2Int,
        Vector3,// Vector3, Color32  - 12 bytes
        Vector3Int,
        Vector3DeltaCompressed,// Vector3, Color32  - 12 bytes
        Vector4,// Quaternion, Color - 16 bytes
        Color,
        Quaternion,
        CompressedQuaternion,
        String, // string   - 255 bytes fixed size, packed over the network
    }

    public enum SyncModifyType
    {
        Set, // Sets the value to the new value.
        Add, // A Straight add or subtract from the current value
        Percent, // Modify the value by a multiplier
        PercentAdd, // modify the value by a multiplier then add it back to the main value.
    }
}
