using System.Collections.Generic;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal static class GeneratedTextures
    {
        private static readonly List<Texture2D> _textures = new();

        internal static Texture2D Track(Texture2D texture)
        {
            _textures.Add(texture);
            return texture;
        }

        internal static void DestroyAll()
        {
            foreach (var texture in _textures)
                Object.DestroyImmediate(texture);

            _textures.Clear();
        }
    }
}
