using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace GrayWolf.Gaze.Editor
{
    internal class ScalableGUIContent
    {
        private GUIContent _content;
        
        public string Text => _content.text;
        public Texture2D Image => _content.image as Texture2D;
        public string Tooltip => _content.tooltip;
        public bool IsValid => _content != null && _content.image != null;

        internal ScalableGUIContent(
            string resourceName, 
            int width = 32, 
            int height = 32, 
            Color? color = null, 
            bool applyPaddingW = false, 
            bool applyPaddingH = false,
            TextureFormat? format = null,
            bool? mipChain = null) 
            : this(EditorGUIUtility.IconContent(resourceName).image as Texture2D,
                width,
                height,
                color ?? Color.white,
                applyPaddingW,
                applyPaddingH,
                format,
                mipChain
            )
        {
        }
        
        internal ScalableGUIContent(
            Texture2D texture,
            int width = 32,
            int height = 32,
            Color? color = null,
            bool applyPaddingW = false,
            bool applyPaddingH = false,
            TextureFormat? format = null,
            bool? mipChain = null)
        {
            _content = new GUIContent
            {
                text = null,
                image = ScaleTexture(
                    texture,
                    width,
                    height,
                    color ?? Color.white,
                    applyPaddingW,
                    applyPaddingH,
                    format,
                    mipChain),
                tooltip = null
            };
        }

        // Loads a texture the package ships itself (Editor/Icons) by a path resolved relative
        // to this package's own location on disk, rather than by name via IconContent - which
        // only resolves bare names against a single "Editor Default Resources" folder at the
        // fixed location Assets/Editor Default Resources, so it can't be used for icons shipped
        // inside a relocatable package folder.
        internal static ScalableGUIContent FromPackageIcon(
            string fileName,
            int width = 32,
            int height = 32,
            Color? color = null,
            bool applyPaddingW = false,
            bool applyPaddingH = false,
            TextureFormat? format = null,
            bool? mipChain = null)
        {
            var path = PackagePaths.IconPath(fileName);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            
            if (texture == null)
            {
                Debug.LogWarning($"[Gaze] Packaged icon not found at '{path}'.");
                return null;
            }

            return new ScalableGUIContent(texture, width, height, color, applyPaddingW, applyPaddingH, format, mipChain);
        }

        private static Texture2D ScaleTexture(
            Texture2D src, 
            int width, 
            int height, 
            Color color, 
            bool applyPaddingW = false, 
            bool applyPaddingH = false,
            TextureFormat? format = null, 
            bool? mipChain = null)
        {
            var srcWidth = src.width;
            var srcHeight = src.height;
            
            var rt = RenderTexture.GetTemporary(srcWidth, srcHeight);
            
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            var temp = new Texture2D(srcWidth, srcHeight);
            temp.ReadPixels(new Rect(0, 0, srcWidth, srcHeight), 0, 0);

            var widthPadding = applyPaddingW ? 2 : 0;
            var heightPadding = applyPaddingH ? 2 : 0;
            
            var result = new Texture2D(srcWidth, srcHeight);
            for (var i = 0; i < srcWidth; i++)
            {
                for (var j = 0; j < srcHeight; j++)
                {
                    var sp32 = temp.GetPixel(i, j + heightPadding);
                    result.SetPixel(i + widthPadding, j, sp32 * color);
                }
            }
            
            result.Apply();

            
            format ??= result.format;
            mipChain ??= true;
            
            var result2 = new Texture2D(width, height, format.Value, mipChain.Value)
            {
                alphaIsTransparency = true,
                hideFlags = HideFlags.HideAndDontSave,
            };
            
            RenderTexture.ReleaseTemporary(rt);
            rt = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(result, rt);
            RenderTexture.active = rt;
            result2.ReadPixels(new Rect(0, 0, width, height), 0, 0);

            result2.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            Object.DestroyImmediate(temp);
            Object.DestroyImmediate(result);

            return GeneratedTextures.Track(result2);
        }
    }
}