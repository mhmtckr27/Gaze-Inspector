using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal static partial class EditorGuiStyles
    {
        private static ScalableGUIContent s_InfoIcon;
        private static ScalableGUIContent s_WarningIcon;
        private static ScalableGUIContent s_ErrorIcon;
        
        private static ScalableGUIContent s_listItemGripIcon;
        private static ScalableGUIContent s_listItemGripIconHover;
        
        private static ScalableGUIContent s_CrossIcon;
        private static ScalableGUIContent s_CrossIconHover;
        
        private static ScalableGUIContent s_PlusIcon;
        private static ScalableGUIContent s_PlusIconHover;

        private static ScalableGUIContent s_RestrictedIcon;
        private static ScalableGUIContent s_ScriptableObjectIcon;

        private static ScalableGUIContent s_PagePrevIcon;
        private static ScalableGUIContent s_PagePrevIconHover;
        private static ScalableGUIContent s_PageNextIcon;
        private static ScalableGUIContent s_PageNextIconHover;
        
        private static ScalableGUIContent s_DiscordIcon;
        private static ScalableGUIContent s_EmailIcon;
        private static ScalableGUIContent s_TwitterIcon;
        private static ScalableGUIContent s_YoutubeIcon;
        private static ScalableGUIContent s_BannerBg;
        private static ScalableGUIContent s_GazeIcon;
        
        
        internal static Texture2D _infoIcon
        {
            get
            {
                s_InfoIcon ??= new ScalableGUIContent(EditorIcons.INFO, 20, 20, applyPaddingW: true);
                return s_InfoIcon.Image;
            }
        }

        internal static Texture2D _warningIcon
        {
            get
            {
                s_WarningIcon ??= new ScalableGUIContent(EditorIcons.WARNING, 20, 20, applyPaddingW: true, applyPaddingH: true);
                return s_WarningIcon.Image;
            }
        }

        internal static Texture2D _errorIcon
        {
            get
            {
                s_ErrorIcon ??= new ScalableGUIContent(EditorIcons.ERROR, 20, 20, Color.red, true, true);
                return s_ErrorIcon.Image;
            }
        }

        internal static Texture2D _listItemGripIcon
        {
            get
            {
                s_listItemGripIcon ??= ScalableGUIContent.FromPackageIcon(EditorIcons.hamburger_menu, 128, 128, new Color(0.8f, 0.8f, 0.8f, 1f));
                return s_listItemGripIcon?.Image;
            }
        }
        internal static Texture2D _emailIcon
        {
            get
            {
                s_EmailIcon ??= ScalableGUIContent.FromPackageIcon(EditorIcons.email, 16, 16, new Color(0.8f, 0.8f, 0.8f, 1f));
                return s_EmailIcon?.Image;
            }
        }
        
        internal static Texture2D _discordIcon
        {
            get
            {
                s_DiscordIcon ??= ScalableGUIContent.FromPackageIcon(EditorIcons.discord, 16, 16, new Color(0.8f, 0.8f, 0.8f, 1f));
                return s_DiscordIcon?.Image;
            }
        }
        
        internal static Texture2D _gazeIcon
        {
            get
            {
                s_GazeIcon ??= ScalableGUIContent.FromPackageIcon(EditorIcons.gazeIcon, 256, 256, new Color(0.8f, 0.8f, 0.8f, 1f));
                return s_GazeIcon?.Image;
            }
        }
        
        
        internal static Texture2D _bannerBg
        {
            get
            {
                s_BannerBg ??= ScalableGUIContent.FromPackageIcon(EditorIcons.bannerBg, 2048, 578, new Color(0.8f, 0.8f, 0.8f, 1f));
                return s_BannerBg?.Image;
            }
        }
        
        internal static Texture2D _twitterIcon
        {
            get
            {
                s_TwitterIcon ??= ScalableGUIContent.FromPackageIcon(EditorIcons.twitter, 14, 14, new Color(0.8f, 0.8f, 0.8f, 1f));
                return s_TwitterIcon?.Image;
            }
        }
        
        internal static Texture2D _youtubeIcon
        {
            get
            {
                s_YoutubeIcon ??= ScalableGUIContent.FromPackageIcon(EditorIcons.youtube, 18, 18, new Color(0.8f, 0.8f, 0.8f, 1f));
                return s_YoutubeIcon?.Image;
            }
        }

        internal static Texture2D _listItemGripIconHover
        {
            get
            {
                s_listItemGripIconHover ??= ScalableGUIContent.FromPackageIcon(EditorIcons.hamburger_menu, 128, 128, Color.white * 2);
                return s_listItemGripIconHover?.Image;
            }
        }
        
        internal static Texture2D _crossIcon
        {
            get
            {
                s_CrossIcon ??= ScalableGUIContent.FromPackageIcon(EditorIcons.CROSS, 32, 32, new Color(0.8f, 0.8f, 0.8f, 0.8f));
                return s_CrossIcon.Image;
            }
        }
        
        internal static Texture2D _crossIconHover
        {
            get
            {
                s_CrossIconHover ??= ScalableGUIContent.FromPackageIcon(EditorIcons.CROSS, 32, 32, Color.white * 2);
                return s_CrossIconHover.Image;
            }
        }

        internal static Texture2D _pagePrevIcon
        {
            get
            {
                s_PagePrevIcon ??= ScalableGUIContent.FromPackageIcon(EditorIcons.ARROW_LEFT, 32, 32, new Color(0.8f, 0.8f, 0.8f, 1f));
                return s_PagePrevIcon.Image;
            }
        }

        internal static Texture2D _pagePrevIconHover
        {
            get
            {
                s_PagePrevIconHover ??= ScalableGUIContent.FromPackageIcon(EditorIcons.ARROW_LEFT, 32, 32, Color.white * 2);
                return s_PagePrevIconHover.Image;
            }
        }

        internal static Texture2D _pageNextIcon
        {
            get
            {
                s_PageNextIcon ??= ScalableGUIContent.FromPackageIcon(EditorIcons.ARROW_RIGHT, 32, 32, new Color(0.8f, 0.8f, 0.8f, 1f));
                return s_PageNextIcon.Image;
            }
        }

        internal static Texture2D _pageNextIconHover
        {
            get
            {
                s_PageNextIconHover ??= ScalableGUIContent.FromPackageIcon(EditorIcons.ARROW_RIGHT, 32, 32, Color.white * 2);
                return s_PageNextIconHover.Image;
            }
        }

        internal static Texture2D _plusIcon
        {
            get
            {
                // s_PlusIcon ??= new ScalableGUIContent(EditorIcons.PLUS, 32, 32, new Color(0.8f, 0.8f, 0.8f, 1f));
                s_PlusIcon ??= ScalableGUIContent.FromPackageIcon(EditorIcons.PLUS, 32, 32, new Color(0.8f, 0.8f, 0.8f, 1f));
                return s_PlusIcon.Image;
            }
        }
        
        internal static Texture2D _plusIconHover
        {
            get
            {
                s_PlusIconHover ??= ScalableGUIContent.FromPackageIcon(EditorIcons.PLUS, 32, 32, Color.white * 2);
                return s_PlusIconHover.Image;
            }
        }
        
        internal static Texture2D GetHelpIcon(MessageType type)
        {
            return type switch
            {
                MessageType.Info => _infoIcon,
                MessageType.Warning => _warningIcon,
                MessageType.Error => _errorIcon,
                _ => null
            };
        }
    }
}