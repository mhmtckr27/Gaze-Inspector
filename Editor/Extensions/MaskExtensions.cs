using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrayWolf.Gaze.Editor
{
    /// <summary>Editor-only helpers for formatting various Unity masks.</summary>
    internal static class MaskExtensions
    {
        /// <summary>
        /// Formats given LayerMask to layer name(s) (string).
        /// </summary>
        /// <param name="mask">Layer mask</param>
        /// <param name="truncateAfter">To provide a max layer name count</param>
        /// <param name="everythingLabel"></param>
        /// <param name="nothingLabel"></param>
        /// <returns>Formatted layer name(s).</returns>
        public static string ToNiceString(
            this LayerMask mask,
            int truncateAfter = 0,
            string everythingLabel = "Everything",
            string nothingLabel = "Nothing")
        {
            return ToNiceStringInternal((uint)mask.value, 32, i =>
            {
                var n = LayerMask.LayerToName(i);
                return string.IsNullOrEmpty(n) ? $"Layer {i}" : n;
            }, truncateAfter, everythingLabel, nothingLabel);
        }
        
#if UNITY_6000_0_OR_NEWER
        /// <summary>
        /// Formats given RenderingLayerMask to layer name(s) (string).
        /// </summary>
        /// <param name="mask">Rendering layer mask</param>
        /// <param name="truncateAfter">To provide a max layer name count</param>
        /// <param name="everythingLabel"></param>
        /// <param name="nothingLabel"></param>
        /// <returns>Formatted layer name(s).</returns>
        public static string ToNiceString(
            this RenderingLayerMask mask, 
            int truncateAfter = 0,
            string everythingLabel = "Everything",
            string nothingLabel = "Nothing")
        {
            var names = RenderingLayerMask.GetDefinedRenderingLayerNames() ?? Array.Empty<string>();
            var bitCount = Mathf.Min(32, names.Length > 0 ? names.Length : 32);

            return ToNiceStringInternal(mask.value, bitCount, i =>
            {
                var n = (i < names.Length) ? names[i] : null;
                return string.IsNullOrEmpty(n) ? $"Rendering Layer {i}" : n;
            }, truncateAfter, everythingLabel, nothingLabel);
        }
#endif
        
        /// <summary>
        /// Formats given rendering mask (uint) to layer name(s) (string).
        /// </summary>
        /// <param name="renderingMask">Mask value</param>
        /// <param name="truncateAfter">To provide a max layer name count</param>
        /// <param name="everythingLabel"></param>
        /// <param name="nothingLabel"></param>
        /// <returns>Formatted layer name(s).</returns>
        public static string ToNiceStringFromRenderingMask(
            this uint renderingMask, 
            int truncateAfter = 0,
            string everythingLabel = "Everything", 
            string nothingLabel = "Nothing")
        {
#if UNITY_6000_0_OR_NEWER
            var names = RenderingLayerMask.GetDefinedRenderingLayerNames() ?? Array.Empty<string>();
#else
            var rpAsset = QualitySettings.renderPipeline;
            if (rpAsset == null)
                rpAsset = GraphicsSettings.defaultRenderPipeline;

            var names = rpAsset != null
                ? rpAsset.renderingLayerMaskNames ?? Array.Empty<string>()
                : Array.Empty<string>();
#endif
            var bitCount = Math.Min(32, names.Length > 0 ? names.Length : 32);

            return ToNiceStringInternal(
                renderingMask, bitCount,
                i => (i < names.Length && !string.IsNullOrEmpty(names[i])) ? names[i] : $"Rendering Layer {i}",
                truncateAfter, everythingLabel, nothingLabel);
        }

        /// <summary>
        /// Formats given mask (uint) to layer name(s) (string).
        /// </summary>
        /// <param name="mask">Mask value</param>
        /// <param name="bitCount">Total bit count for mask</param>
        /// <param name="nameOf">Func callback that returns name for given mask value (uint)</param>
        /// <param name="truncateAfter">To provide a max layer name count</param>
        /// <param name="everythingLabel">String that is displayed if all layers are selected ("Everything" by default)</param>
        /// <param name="nothingLabel">String that is displayed if no layer selected ("Nothing" by default)</param>
        /// <returns>Formatted layer name(s).</returns>
        private static string ToNiceStringInternal(uint mask, int bitCount, Func<int, string> nameOf,
            int truncateAfter, string everythingLabel, string nothingLabel)
        {
            if (mask == 0u) return nothingLabel;

            var allBits = bitCount >= 32 ? 0xFFFFFFFFu : ((1u << bitCount) - 1u);
            if ((mask & allBits) == allBits) return everythingLabel;

            var names = new List<string>(8);
            for (var i = 0; i < bitCount; i++)
            {
                if ((mask & (1u << i)) != 0u)
                    names.Add(nameOf(i));
            }

            if (names.Count == 0) return nothingLabel;
            if (truncateAfter > 0 && names.Count > truncateAfter)
                return string.Join(", ", names.Take(truncateAfter)) + $" +{names.Count - truncateAfter}";
            
            return string.Join(", ", names);
        }


        public static string[] GetLayerNames()
        {
            var layers = new List<string>(32);
            for (var i = 0; i < 32; i++)
            {
                var n = LayerMask.LayerToName(i);

                if (!string.IsNullOrEmpty(n))
                    layers.Add(n);
            }

            return layers.ToArray();
        }
        
#if UNITY_6000_0_OR_NEWER
        public static string[] GetRenderingLayerNames()
        {
            return RenderingLayerMask.GetDefinedRenderingLayerNames();
        }
#endif

    }
}