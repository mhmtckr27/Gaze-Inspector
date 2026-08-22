using System;
using System.Linq;

namespace GrayWolf.Gaze.Editor
{
    internal static class EnumExtensions
    {
        // Converts ANY enum (signed/unsigned underlying, high-bit safe) to ulong
        internal static ulong EnumToUInt64(this Enum e)
        {
            var t = Enum.GetUnderlyingType(e.GetType());
            if (t == typeof(sbyte)) return unchecked((ulong)(sbyte)(object)e);
            if (t == typeof(short)) return unchecked((ulong)(short)(object)e);
            if (t == typeof(int)) return unchecked((ulong)(int)(object)e);
            if (t == typeof(long)) return unchecked((ulong)(long)(object)e);
            if (t == typeof(byte)) return (byte)(object)e;
            if (t == typeof(ushort)) return (ushort)(object)e;
            if (t == typeof(uint)) return (uint)(object)e;
            if (t == typeof(ulong)) return (ulong)(object)e;
            throw new NotSupportedException($"Unsupported enum underlying type {t}");
        }

        // Converts a boxed integral (byte/…/long/ulong/int/etc.) to ulong safely
        internal static ulong ObjectIntegralToUInt64(object o)
        {
            return o switch
            {
                sbyte  v => unchecked((ulong)v),
                short  v => unchecked((ulong)v),
                int    v => unchecked((ulong)v),
                long   v => unchecked((ulong)v),
                byte   v => v,
                ushort v => v,
                uint   v => v,
                ulong  v => v,
                _        => unchecked((ulong)Convert.ToInt64(o ?? 0))
            };
        }

        // Turn ulong bit-pattern back into the enum’s underlying integral, boxed
        internal static object UInt64ToUnderlying(ulong bits, Type underlying)
        {
            if (underlying == typeof(sbyte)) return unchecked((sbyte)bits);
            if (underlying == typeof(short)) return unchecked((short)bits);
            if (underlying == typeof(int)) return unchecked((int)bits);
            if (underlying == typeof(long)) return unchecked((long)bits);
            if (underlying == typeof(byte)) return (byte)bits;
            if (underlying == typeof(ushort)) return (ushort)bits;
            if (underlying == typeof(uint)) return (uint)bits;
            if (underlying == typeof(ulong)) return bits;
            throw new NotSupportedException($"Unsupported enum underlying type {underlying}");
        }
        
        // Normalize a signed long to the enum’s underlying bit-width (two’s complement)
        public static ulong NormalizeToUnderlying(long value, Type underlying)
        {
            unchecked
            {
                switch (Type.GetTypeCode(underlying))
                {
                    case TypeCode.SByte:  return (ulong)(byte)(sbyte)value;   // 8-bit
                    case TypeCode.Int16:  return (ulong)(ushort)(short)value; // 16-bit
                    case TypeCode.Int32:  return (ulong)(uint)(int)value;     // 32-bit
                    case TypeCode.Int64:  return (ulong)value;                // 64-bit
                    case TypeCode.Byte:   return (byte)value;
                    case TypeCode.UInt16: return (ushort)value;
                    case TypeCode.UInt32: return (uint)value;
                    case TypeCode.UInt64: return (ulong)value;
                    default: throw new ArgumentOutOfRangeException(nameof(underlying));
                }
            }
        }
        // Build union mask of all named values (as ulong)
        
        public static ulong GetFlagsMask(Type enumType)
            => Enum.GetValues(enumType).Cast<object>()
                .Select(v => ToUInt64(v, Enum.GetUnderlyingType(enumType)))
                .Aggregate(0UL, (acc, v) => acc | v);
        
        static ulong ToUInt64(object enumValue, Type underlying)
        {
            unchecked
            {
                switch (Type.GetTypeCode(underlying))
                {
                    case TypeCode.SByte:  return (ulong)(byte)(sbyte)enumValue;
                    case TypeCode.Int16:  return (ulong)(ushort)(short)enumValue;
                    case TypeCode.Int32:  return (ulong)(uint)(int)enumValue;
                    case TypeCode.Int64:  return (ulong)(long)enumValue;
                    case TypeCode.Byte:   return (byte)enumValue;
                    case TypeCode.UInt16: return (ushort)enumValue;
                    case TypeCode.UInt32: return (uint)enumValue;
                    case TypeCode.UInt64: return (ulong)enumValue;
                    default: throw new ArgumentOutOfRangeException(nameof(underlying));
                }
            }
        }
        
        public static ulong CoerceFlagsValue(Type enumType, long pasted)
        {
            var utype = Enum.GetUnderlyingType(enumType);
            var norm  = NormalizeToUnderlying(pasted, utype); // respects width
            var mask  = GetFlagsMask(enumType);
            return norm & mask; // -1 turns into mask (e.g., 31)
        }

        
        // Accepts boxed enum *or* boxed underlying integral; returns boxed enum of enumType
        internal static Enum NormalizeToEnum(Type enumType, object value)
        {
            if (value is Enum e)
            {
                // If it's already an enum but not the same type, coerce via bits
                if (e.GetType() == enumType) return e;
                var bits = e.EnumToUInt64();
                var coerced = Enum.ToObject(enumType, UInt64ToUnderlying(bits, Enum.GetUnderlyingType(enumType)));
                return (Enum)coerced;
            }

            // value might be null or an integral; treat null as zero
            object underlying = value ?? Activator.CreateInstance(Enum.GetUnderlyingType(enumType));
            // Ensure it's the correct underlying numeric type
            ulong bitsNorm = ObjectIntegralToUInt64(underlying);
            object boxedCorrectUnderlying = UInt64ToUnderlying(bitsNorm, Enum.GetUnderlyingType(enumType));
            return (Enum)Enum.ToObject(enumType, boxedCorrectUnderlying);
        }

        internal static Enum NormalizeToEnum<T>(object value)
        {
            return NormalizeToEnum(typeof(T), value);
        }
    }
}