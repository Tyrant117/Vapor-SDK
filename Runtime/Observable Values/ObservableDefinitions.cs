namespace VaporObservables
{
    public enum ObservableFieldType
    {
        Int8,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Single,
        Int64,
        UInt64,
        Double,
        Vector2,
        Vector2Int,
        Vector3,
        Vector3Int,
        Color,
        Quaternion,
        String,
    }

    public enum ObservableModifyType
    {
        Set, // Sets the value to the new value.
        Add, // A Straight add or subtract from the current value
        Multiplier, // Modify the value by a multiplier
        PercentAdd, // modify the value by a multiplier then add it back to the main value.
    }
}
