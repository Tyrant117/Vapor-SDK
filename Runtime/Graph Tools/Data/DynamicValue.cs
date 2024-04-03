using System;
using System.Collections.Generic;

namespace VaporGraphTools
{
    public readonly struct DynamicValue : IEquatable<DynamicValue>
    {
        public static bool operator ==(DynamicValue first, DynamicValue second) =>
            first.Type == second.Type
            && first.BoolValue == second.BoolValue
            && first.IntValue == second.IntValue
            && first.FloatValue == second.FloatValue;

        public static bool operator !=(DynamicValue first, DynamicValue second) => !(first == second);

        public static bool operator >=(DynamicValue first, DynamicValue second) => first.AsValue() >= second.AsValue();
        public static bool operator <=(DynamicValue first, DynamicValue second) => first.AsValue() <= second.AsValue();
        public static bool operator >(DynamicValue first, DynamicValue second) => first.AsValue() > second.AsValue();
        public static bool operator <(DynamicValue first, DynamicValue second) => first.AsValue() < second.AsValue();

        public enum DynamicValueType
        {
            Bool,
            Int,
            Float,
        }

        public readonly DynamicValueType Type;
        public readonly bool BoolValue;
        public readonly int IntValue;
        public readonly float FloatValue;

        public DynamicValue(float value)
        {
            Type = DynamicValueType.Float;
            BoolValue = false;
            IntValue = 0;
            FloatValue = value;
        }

        public DynamicValue(int value)
        {
            Type = DynamicValueType.Int;
            BoolValue = false;
            IntValue = value;
            FloatValue = 0;
        }

        public DynamicValue(bool value)
        {
            Type = DynamicValueType.Bool;
            BoolValue = value;
            IntValue = 0;
            FloatValue = 0;
        }

        public override bool Equals(object obj) => obj is DynamicValue modifier && Equals(modifier);
        public bool Equals(DynamicValue other)
        {
            return Type == other.Type
                && BoolValue == other.BoolValue
                && IntValue == other.IntValue
                && FloatValue == other.FloatValue;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, BoolValue, IntValue, FloatValue);
        }

        public double AsValue()
        {
            return Type switch
            {
                DynamicValueType.Bool => Convert.ToDouble(BoolValue),
                DynamicValueType.Int => IntValue,
                DynamicValueType.Float => FloatValue,
                _ => 0,
            };
        }
    }
}
