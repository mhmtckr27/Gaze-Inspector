using System;
using UnityEngine;

namespace GrayWolf.Gaze
{
    //Vector2Int extensions
    internal static partial class VectorExtensions
    {
        public static Vector2Int Clamp(this Vector2Int vector, float min, float max)
        {
            return vector.Clamp((int)min, (int)max);
        }
        
        public static Vector2Int Clamp(this Vector2Int vector, float min, int max)
        {
            return vector.Clamp((int)min, max);
        }
        
        public static Vector2Int Clamp(this Vector2Int vector, int min, int max)
        {
            vector.x = Mathf.Clamp(vector.x, min, max);
            vector.y = Mathf.Clamp(vector.y, min, max);
            return vector;
        }
        
        public static Vector2Int FromArray(this Vector2Int vector, int[] array)
        {
            if (array is not { Length: > 0 })
                return vector;

            vector.x = array[0];
            if (array.Length == 1)
                return vector;

            vector.y = array[1];
            if (array.Length == 2)
                return vector;

            return vector;
        }
        
        public static int[] ToArray(this Vector2Int vector)
        {
            return new[] { vector.x, vector.y };
        }
        
        public static Vector3Int ToVector3Int(this Vector2Int vector)
        {
            return new Vector3Int(vector.x, vector.y, 0);
        }
    }

    //Vector3Int extensions
    internal static partial class VectorExtensions
    {
        public static Vector3Int Clamp(this Vector3Int vector, float min, float max)
        {
            return vector.Clamp((int)min, (int)max);
        }
        
        public static Vector3Int Clamp(this Vector3Int vector, int min, float max)
        {
            return vector.Clamp(min, (int)max);
        }

        public static Vector3Int Clamp(this Vector3Int vector, int min, int max)
        {
            vector.x = Mathf.Clamp(vector.x, min, max);
            vector.y = Mathf.Clamp(vector.y, min, max);
            vector.z = Mathf.Clamp(vector.z, min, max);
            return vector;
        }
        
        public static int[] ToArray(this Vector3Int vector)
        {
            return new[] { vector.x, vector.y, vector.z };
        }

        public static Vector3Int FromArray(this Vector3Int vector, int[] array)
        {
            if (array is not { Length: > 0 })
                return vector;

            vector.x = array[0];
            if (array.Length == 1)
                return vector;

            vector.y = array[1];
            if (array.Length == 2)
                return vector;

            vector.z = array[2];

            return vector;
        }
        
        public static Vector2Int ToVector2Int(this Vector3Int vector)
        {
            return new Vector2Int(vector.x, vector.y);
        }
    }

    //Vector2 extensions
    internal static partial class VectorExtensions
    {
        public static Vector2 Clamp(this Vector2 vector, float min, float max)
        {
            vector.x = Mathf.Clamp(vector.x, min, max);
            vector.y = Mathf.Clamp(vector.y, min, max);
            return vector;
        }
        
        public static float[] ToArray(this Vector2 vector)
        {
            return new[] { vector.x, vector.y };
        }

        public static Vector2 FromArray(this Vector2 vector, float[] array)
        {
            if (array is not { Length: > 0 })
                return vector;

            vector.x = array[0];
            if (array.Length == 1)
                return vector;

            vector.y = array[1];
            return vector;
        }
        
        public static Vector2Int ToVector2Int(this Vector2 vector)
        {
            return new Vector2Int((int)vector.x, (int)vector.y);
        }
        
        public static bool TryParse(string input, out Vector2 vector)
        {
            vector = default;

            if (string.IsNullOrEmpty(input))
                return false;

            var numbers = input.Trim().Replace("(", "").Replace(")", "").Split(',');

            if (numbers.Length != 2)
                return false;

            if (!float.TryParse(numbers[0], out var x) ||
                !float.TryParse(numbers[1], out var y))
                return false;

            vector = new Vector2(x, y);
            return true;
        }
    }

    //Vector3 extensions
    internal static partial class VectorExtensions
    {
        public static Vector3 Clamp(this Vector3 vector, float min, float max)
        {
            vector.x = Mathf.Clamp(vector.x, min, max);
            vector.y = Mathf.Clamp(vector.y, min, max);
            vector.z = Mathf.Clamp(vector.z, min, max);
            return vector;
        }
        
        public static float[] ToArray(this Vector3 vector)
        {
            return new[] { vector.x, vector.y, vector.z };
        }

        public static Vector3 FromArray(this Vector3 vector, float[] array)
        {
            if (array is not { Length: > 0 })
                return vector;

            vector.x = array[0];
            if (array.Length == 1)
                return vector;

            vector.y = array[1];
            if (array.Length == 2)
                return vector;

            vector.z = array[2];
            return vector;
        }
        
        public static Vector3Int ToVector3Int(this Vector3 vector)
        {
            return new Vector3Int((int)vector.x, (int)vector.y, (int)vector.z);
        }
        
        public static bool TryParse(string input, out Vector3 vector)
        {
            vector = default;

            if (string.IsNullOrEmpty(input))
                return false;

            var numbers = input.Trim().Replace("(", "").Replace(")", "").Split(',');

            if (numbers.Length != 3)
                return false;

            if (!float.TryParse(numbers[0], out var x) ||
                !float.TryParse(numbers[1], out var y) ||
                !float.TryParse(numbers[2], out var z))
                return false;

            vector = new Vector3(x, y, z);
            return true;
        }
    }

    //Vector4 extensions
    internal static partial class VectorExtensions
    {
        public static Vector4 Clamp(this Vector4 vector, float min, float max)
        {
            vector.x = Mathf.Clamp(vector.x, min, max);
            vector.y = Mathf.Clamp(vector.y, min, max);
            vector.z = Mathf.Clamp(vector.z, min, max);
            vector.w = Mathf.Clamp(vector.w, min, max);
            return vector;
        }
        
        public static float[] ToArray(this Vector4 vector)
        {
            return new[] { vector.x, vector.y, vector.z, vector.w };
        }

        public static Vector4 FromArray(this Vector4 vector, float[] array)
        {
            if (array is not { Length: > 0 })
                return vector;

            vector.x = array[0];
            if (array.Length == 1)
                return vector;

            vector.y = array[1];
            if (array.Length == 2)
                return vector;

            vector.z = array[2];
            if (array.Length == 3)
                return vector;

            vector.w = array[3];
            return vector;
        }

        public static bool TryParse(string input, out Vector4 vector)
        {
            vector = default;

            if (string.IsNullOrEmpty(input))
                return false;

            var numbers = input.Trim().Replace("(", "").Replace(")", "").Split(',');

            if (numbers.Length != 4)
                return false;

            if (!float.TryParse(numbers[0], out var x) ||
                !float.TryParse(numbers[1], out var y) ||
                !float.TryParse(numbers[2], out var z) ||
                !float.TryParse(numbers[3], out var w))
                return false;

            vector = new Vector4(x, y, z, w);
            return true;
        }

        public static Vector4 Parse(string input)
        {
            if (TryParse(input, out Vector2 vector2))
                return vector2;

            if (TryParse(input, out Vector3 vector3))
                return vector3;

            if (TryParse(input, out Vector4 vector4))
                return vector4;

            throw new FormatException($"[Gaze] {nameof(VectorExtensions)} | '{input}' is not a valid vector string.");
        }
        
        public static Vector2Int ToVector2Int(this Vector4 vector)
        {
            return new Vector2Int((int)vector.x, (int)vector.y);
        }
        
        public static Vector3Int ToVector3Int(this Vector4 vector)
        {
            return new Vector3Int((int)vector.x, (int)vector.y, (int)vector.z);
        }
    }
}