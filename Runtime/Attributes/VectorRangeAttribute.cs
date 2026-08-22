using System;
using UnityEngine;

namespace GrayWolf.Gaze
{
    /// <summary>
    /// Clamps each component of a vector field to its own range.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every overload takes all minimums first, then all maximums. Applies to
    /// <see cref="Vector2"/>, <see cref="Vector3"/>, <see cref="Vector4"/>, <see cref="Vector2Int"/>
    /// and <see cref="Vector3Int"/> fields; other field types are left alone.
    /// </para>
    /// <para>
    /// The range clamps the value the user types, not just the travel of the slider. Components the
    /// chosen overload does not cover are left unbounded, so tagging a <see cref="Vector3"/> field with
    /// the two-axis overload constrains X and Y and leaves Z free.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class VectorRangeAttribute : PropertyAttribute
    {
        /// <summary>Lower bound for the X component, or null when unbounded.</summary>
        public readonly float? minX;
        /// <summary>Lower bound for the Y component, or null when unbounded.</summary>
        public readonly float? minY;
        /// <summary>Lower bound for the Z component, or null when unbounded.</summary>
        public readonly float? minZ;
        /// <summary>Lower bound for the W component, or null when unbounded.</summary>
        public readonly float? minW;

        /// <summary>Upper bound for the X component, or null when unbounded.</summary>
        public readonly float? maxX;
        /// <summary>Upper bound for the Y component, or null when unbounded.</summary>
        public readonly float? maxY;
        /// <summary>Upper bound for the Z component, or null when unbounded.</summary>
        public readonly float? maxZ;
        /// <summary>Upper bound for the W component, or null when unbounded.</summary>
        public readonly float? maxW;

        /// <summary>
        /// Draws each component as a slider across its range instead of a plain number field.
        /// </summary>
        public readonly bool showSliders;

        /// <summary>
        /// Bounds the X and Y components and leaves Z and W unbounded.
        /// </summary>
        /// <param name="minX">Lower bound for X.</param>
        /// <param name="minY">Lower bound for Y.</param>
        /// <param name="maxX">Upper bound for X.</param>
        /// <param name="maxY">Upper bound for Y.</param>
        /// <param name="showSliders">Draw sliders rather than number fields.</param>
        public VectorRangeAttribute(
            float minX,
            float minY,
            float maxX,
            float maxY,
            bool showSliders = false)
            : this(
            minX,
            minY,
            null,
            null,
            maxX,
            maxY,
            null,
            null,
            showSliders)
        {
        }

        /// <summary>
        /// Bounds the X, Y and Z components and leaves W unbounded.
        /// </summary>
        /// <param name="minX">Lower bound for X.</param>
        /// <param name="minY">Lower bound for Y.</param>
        /// <param name="minZ">Lower bound for Z.</param>
        /// <param name="maxX">Upper bound for X.</param>
        /// <param name="maxY">Upper bound for Y.</param>
        /// <param name="maxZ">Upper bound for Z.</param>
        /// <param name="showSliders">Draw sliders rather than number fields.</param>
        public VectorRangeAttribute(
            float minX,
            float minY,
            float minZ,
            float maxX,
            float maxY,
            float maxZ,
            bool showSliders = false)
            : this(
                minX,
                minY,
                minZ,
                null,
                maxX,
                maxY,
                maxZ,
                null,
                showSliders)
        {
        }
        
        /// <summary>
        /// Bounds all four components.
        /// </summary>
        /// <param name="minX">Lower bound for X.</param>
        /// <param name="minY">Lower bound for Y.</param>
        /// <param name="minZ">Lower bound for Z.</param>
        /// <param name="minW">Lower bound for W.</param>
        /// <param name="maxX">Upper bound for X.</param>
        /// <param name="maxY">Upper bound for Y.</param>
        /// <param name="maxZ">Upper bound for Z.</param>
        /// <param name="maxW">Upper bound for W.</param>
        /// <param name="showSliders">Draw sliders rather than number fields.</param>
        public VectorRangeAttribute(
            float minX,
            float minY,
            float minZ,
            float minW,
            float maxX,
            float maxY,
            float maxZ,
            float maxW,
            bool showSliders = false)
        {
            this.minX = minX;
            this.minY = minY;
            this.minZ = minZ;
            this.minW = minW;

            this.maxX = maxX;
            this.maxY = maxY;
            this.maxZ = maxZ;
            this.maxW = maxW;

            this.showSliders = showSliders;
        }

        private VectorRangeAttribute(
            float? minX,
            float? minY,
            float? minZ,
            float? minW,
            float? maxX,
            float? maxY,
            float? maxZ,
            float? maxW,
            bool showSliders = false)
        {
            this.minX = minX;
            this.minY = minY;
            this.minZ = minZ;
            this.minW = minW;

            this.maxX = maxX;
            this.maxY = maxY;
            this.maxZ = maxZ;
            this.maxW = maxW;

            this.showSliders = showSliders;
        }
    }
}