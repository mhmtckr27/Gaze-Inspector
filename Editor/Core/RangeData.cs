namespace GrayWolf.Gaze.Editor
{
    /// <summary>
    /// Bounds and slider preference collected from a range attribute. A null member was never
    /// configured: <see cref="MinDecorator"/> supplies only <see cref="min"/>, while
    /// <see cref="RangeDecorator"/> supplies all three.
    /// </summary>
    internal class RangeData : ContextItem
    {
        internal bool? isSlider;
        internal float? min;
        internal float? max;

        /// <summary><see cref="min"/> narrowed to int, or null when unbounded.</summary>
        internal int? MinAsInt => ToInt(min);

        /// <summary><see cref="max"/> narrowed to int, or null when unbounded.</summary>
        internal int? MaxAsInt => ToInt(max);

        public override void Reset()
        {
            isSlider = null;
            min = null;
            max = null;
        }

        /// <summary>
        /// Narrows a bound to int, saturating at either end of the int range.
        /// </summary>
        /// <remarks>
        /// A plain cast is not safe here. int.MaxValue has no exact float representation and widens to
        /// 2147483648, which is outside int; casting that back is undefined, and Mono returns
        /// int.MinValue, which turns an upper bound into a lower one.
        /// </remarks>
        private static int? ToInt(float? value)
        {
            if (!value.HasValue)
                return null;

            if (value.Value <= int.MinValue)
                return int.MinValue;

            if (value.Value >= int.MaxValue)
                return int.MaxValue;

            return (int) value.Value;
        }
    }
}
