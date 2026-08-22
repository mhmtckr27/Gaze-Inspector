using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal static partial class EditorGuiStyles
    {
        static EditorGuiStyles()
        {
            GazeEditorCache.OnClear -= Clear;
            GazeEditorCache.OnClear += Clear;
        }

        private static void Clear()
        {
            foreach (var fieldInfo in typeof(EditorGuiStyles).GetFields(
                         BindingFlags.Default | 
                         BindingFlags.Static |
                         BindingFlags.Public | 
                         BindingFlags.NonPublic))
            {
                // SetValue(null) zeroes a value type field, which wiped the row colours on every scene load.
                if(!fieldInfo.IsStatic || fieldInfo.FieldType.IsValueType)
                    continue;

                fieldInfo.SetValue(null, null);
            }

            GeneratedTextures.DestroyAll();
        }
        
        private static bool HasBackground(GUIStyle style) => style != null && style.normal.background != null;

        private static Texture2D SolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };

            texture.SetPixel(0, 0, color);
            texture.Apply();
            return GeneratedTextures.Track(texture);
        }

        private static GUIStyle _collectionDrawerBox;
        private static GUIStyle _listItemEvenIndex;
        private static GUIStyle _listItemOddIndex;
        private static GUIStyle _dictionaryItemEvenIndex;
        private static GUIStyle _dictionaryItemOddIndex;
        private static GUIStyle _label;
        private static GUIStyle _labelPadded;
        private static GUIStyle _labelCentered;
        private static GUIStyle _labelCenteredSmall;
        private static GUIStyle _labelLeft;
        private static GUIStyle _labelRight;
        private static GUIStyle _listHeaderLabel;
        private static GUIStyle _dictionaryHeaderLabel;
        private static GUIStyle _messageBox;
        private static GUIStyle _propertyPadding;
        private static GUIStyle _foldout;
        private static GUIStyle _removeButton;
        private static GUIStyle _gripButton;
        private static GUIStyle _addButton;
        private static GUIStyle _addButtonFull;
        private static GUIStyle _pagePrevButton;
        private static GUIStyle _pageNextButton;
        private static GUIStyle _pageShowAllButton;
        private static GUIStyle _none;
        private static GUIStyle _listItem;
        private static GUIStyle _numberField;
        private static GUIStyle _roundedBox;
        private static GUIStyle _textArea;

        internal static float MinLabelWidth => 45f;

        // Mirrors of UnityEditor internals the layout has to agree with. All three are internal const
        // on EditorGUI, so they cannot be referenced from here, and a value that disagrees with Unity's
        // puts a control on the other side of Unity's own branch from where we think it is.
        internal static float SubLabelWidth   => 16f; // label width of one vector component
        internal static float SubLabelSpacing => 4f;  // EditorGUI.kSpacingSubLabel, gap between components
        internal static float FieldSpacing    => 5f;  // EditorGUI.kSpacing
        internal static float MinSliderWidth  => 50f; // EditorGUI.kSliderMinW

        // Below this width EditorGUI.Slider draws no slider track. It clamps the number field to
        // fieldWidth, pins it to the right edge, and leaves the rest of the rect blank.
        internal static float MinSliderFieldWidth => MinSliderWidth + FieldSpacing + EditorGUIUtility.fieldWidth;

        internal static float SectionSpacing     => EditorGUIUtility.standardVerticalSpacing; // between root properties
        internal static float HeaderBodySpacing  => EditorGUIUtility.standardVerticalSpacing; // between foldout header and children
        internal static float ChildSpacing       => EditorGUIUtility.standardVerticalSpacing; // between children inside groups
        internal static float ListRowSpacing     => EditorGUIUtility.standardVerticalSpacing; // vertical pad per list row
        
        internal static Color BorderColor   = new(0.13f, 0.13f, 0.13f, 1f);
        internal static Color EvenRowColor  = new(0.235f, 0.235f, 0.235f, 1f);
        internal static Color OddRowColor   = new(0.2f, 0.2f, 0.2f, 1f);
        internal static Color ShadowColor   = new(0.5f, 0.5f, 0.5f, 0.35f);
        internal static Color DuplicateKeyTint = new(0.9f, 0.35f, 0.1f, 0.25f);

        internal static GUIStyle CollectionDrawerBox =>
            _collectionDrawerBox ??= new GUIStyle(None)
            {  
                padding = new RectOffset(1, 1, 0, 1),
            };

        internal static GUIStyle ListItemEvenIndex =>
            HasBackground(_listItemEvenIndex) ? _listItemEvenIndex : _listItemEvenIndex = new GUIStyle()
            {
                padding = new RectOffset(25, 22, 3, 3),
                margin = new RectOffset(0, 0, 0, 0),
                normal = new GUIStyleState()
                {
                    background = SolidTexture(EvenRowColor)
                }
            };

        internal static GUIStyle ListItemOddIndex =>
            HasBackground(_listItemOddIndex) ? _listItemOddIndex : _listItemOddIndex = new GUIStyle()
            {
                padding = new RectOffset(25, 20, 3, 3),
                margin = new RectOffset(0, 0, 0, 0),
                // padding = new RectOffset(0, 0, 3, 3),
                normal = new GUIStyleState()
                {
                    background = SolidTexture(OddRowColor)
                }
            };

        internal static GUIStyle DictionaryItemEvenIndex =>
            HasBackground(_dictionaryItemEvenIndex) ? _dictionaryItemEvenIndex : _dictionaryItemEvenIndex = new GUIStyle()
            {
                // padding = new RectOffset(0, 0, 3, 3),
                padding = new RectOffset(3, 0, 3, 3),
                margin = new RectOffset(0, 0, 0, 0),
                normal = new GUIStyleState()
                {
                    background = SolidTexture(EvenRowColor)
                }
            };

        internal static GUIStyle DictionaryItemOddIndex =>
            HasBackground(_dictionaryItemOddIndex) ? _dictionaryItemOddIndex : _dictionaryItemOddIndex = new GUIStyle()
            {
                padding = new RectOffset(3, 0, 3, 3),
                margin = new RectOffset(0, 0, 0, 0),
                // padding = new RectOffset(0, 0, 3, 3),
                normal = new GUIStyleState()
                {
                    background = SolidTexture(OddRowColor)
                }
            };
        
        static GUIStyle s_NoMargin;

        internal static GUIStyle NoMargin => 
            s_NoMargin ??= new GUIStyle(GUIStyle.none)
            {
                margin = new RectOffset(-2, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                stretchWidth = true,
                stretchHeight = false
            };
        
        internal static GUIStyle None =>
            _none ??= new GUIStyle()
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0)
            };

        internal static GUIStyle ListItem =>
            _listItem ??= new GUIStyle(None)
            {
                padding = new RectOffset(0, 0, 3, 3)
            };

        internal static GUIStyle ListHeaderLabel =>
            _listHeaderLabel ??= new GUIStyle(EditorStyles.label)
            {
                // Margin folded into padding: geometrically identical (the row grows by exactly what the
                // margin used to reserve outside it) but margin-free. A margin resolves differently inside
                // an animating fade group than at rest - measured as 4 vs 3 per side - which slid the
                // centred column labels and the divider for the length of the tween. The data rows carry
                // margin 0 and provably never moved, so the header now matches them.
                margin = new RectOffset(0, 0, 0, 0),
                // 5, not 6: the old 4px margin actually resolved to 3 at rest, and the row divider's
                // position was tuned against that. 3 + the old 2px padding is what has to be preserved,
                // otherwise the header divider lands a pixel right of the one between the row cells.
                padding = new RectOffset(5, 5, 3, 3),
                // Only feeds stretchHeight, never a height - keeps the header row from stretching.
                stretchHeight = false,
            };
        
        internal static GUIStyle DictionaryHeaderLabel =>
            _dictionaryHeaderLabel ??= new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
            };
        
        internal static GUIStyle AddButtonFull =>
            _addButtonFull ??= new GUIStyle(EditorStyles.miniButtonMid)
            {
                margin = new RectOffset(4, 4, 2, 2),
                padding = new RectOffset(2, 2, 1, 1),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fixedHeight = 0
            };
        
        internal static GUIStyle Label => 
            _label ??= new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(0, 0, 0, 0),
            };
        
        internal static GUIStyle NumberField => 
            _numberField ??= new GUIStyle(EditorStyles.numberField)
            {
                padding = new RectOffset(EditorStyles.numberField.padding.left, EditorStyles.numberField.padding.right, 3, 3),
            };
        
        internal static GUIStyle LabelPadded => 
            _labelPadded ??= new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(3, 3, 3, 3)
            };

        internal static GUIStyle LabelCentered =>
            _labelCentered ??= new GUIStyle(Label)
            {
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 0, 0),
                clipping = TextClipping.Clip
            };
        
        internal static GUIStyle LabelCenteredSmall =>
            _labelCenteredSmall ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                clipping = TextClipping.Clip,
                fontSize = 10,
                normal =
                {
                    textColor = new Color(0.7f, 0.7f, 0.7f, 1.0f),
                }
            };
        
        internal static GUIStyle LabelRight =>
            _labelRight ??= new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                margin = new RectOffset(0, 0, 0, 0),
                clipping = TextClipping.Clip,
                normal =
                {
                    textColor = new Color(0.7f, 0.7f, 0.7f, 1.0f),
                }
            };
        
        internal static GUIStyle LabelLeft =>
            _labelLeft ??= new GUIStyle(Label)
            {
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(5, 0, 0, 0),
                clipping = TextClipping.Clip,
                normal =
                {
                    textColor = new Color(0.7f, 0.7f, 0.7f, 1.0f),
                }
            };

        internal static GUIStyle MessageBox =>
            _messageBox ??= new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(4, 4, 2, 2),
                fontSize = 10,
                richText = true,
                border = new RectOffset(20, 20, 20, 20)
            };

        internal static GUIStyle PropertyPadding =>
            _propertyPadding ??= new GUIStyle(GUIStyle.none)
            {
                padding = new RectOffset(0, 0, 0, 3),
                margin = new RectOffset(0, 0, 0, 0)
            };

        internal static GUIStyle Foldout =>
            _foldout ??= new GUIStyle(EditorStyles.foldout)
            {
                // padding = new RectOffset(3, 3, 3, 3),
                // margin = new RectOffset(EditorStyles.foldout.margin.left, EditorStyles.foldout.margin.right, 1, 1),
                fixedWidth = 0.0f,
                fixedHeight = 0.0f,
                stretchHeight = false,
                stretchWidth = true,
                margin = new RectOffset(3, 0, 0, 0),
            };
        

        internal static GUIStyle ListItemGripButton =>
            _gripButton ??= new GUIStyle(EditorStyles.iconButton)
            {
                fixedWidth = 0,
                fixedHeight = 0,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(1, 1, 1, 1),
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    background = _listItemGripIcon
                },
                hover = new GUIStyleState()
                {
                    background = _listItemGripIconHover
                },
                active = new GUIStyleState()
                {
                    background = _listItemGripIconHover
                }
            };
        
        internal static GUIStyle AddButton =>
            _addButton ??= new GUIStyle(EditorStyles.iconButton)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(1, 1, 1, 1),
                normal =
                {
                    background = _plusIcon
                },
                hover = new GUIStyleState()
                {
                    background = _plusIconHover
                },
                active = new GUIStyleState()
                {
                    background = _plusIconHover
                },
                fixedWidth = 0,
                fixedHeight = 0,
            };
        
        internal static GUIStyle RemoveButton =>
            _removeButton ??= new GUIStyle(EditorStyles.iconButton)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(1, 1, 1, 1),
                normal =
                {
                    background = _crossIcon
                },
                hover = new GUIStyleState()
                {
                    background = _crossIconHover
                },
                active = new GUIStyleState()
                {
                    background = _crossIconHover
                }
            };

        internal static GUIStyle PagePrevButton =>
            _pagePrevButton ??= new GUIStyle(EditorStyles.iconButton)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(1, 1, 1, 1),
                normal =
                {
                    background = _pagePrevIcon
                },
                hover = new GUIStyleState()
                {
                    background = _pagePrevIconHover
                },
                active = new GUIStyleState()
                {
                    background = _pagePrevIconHover
                },
                fixedWidth = 0,
                fixedHeight = 0,
            };

        internal static GUIStyle PageNextButton =>
            _pageNextButton ??= new GUIStyle(EditorStyles.iconButton)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(1, 1, 1, 1),
                normal =
                {
                    background = _pageNextIcon
                },
                hover = new GUIStyleState()
                {
                    background = _pageNextIconHover
                },
                active = new GUIStyleState()
                {
                    background = _pageNextIconHover
                },
                fixedWidth = 0,
                fixedHeight = 0,
            };

        internal static GUIStyle PageShowAllButton =>
            _pageShowAllButton ??= new GUIStyle(EditorStyles.miniLabel)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                normal =
                {
                    textColor = new Color(0.7f, 0.7f, 0.7f, 1.0f),
                },
                hover =
                {
                    textColor = Color.white,
                }
            };

        internal static GUIStyle RoundedBox => _roundedBox ??= new GUIStyle(EditorStyles.helpBox);

        internal static GUIStyle VerticalScrollBar => GUI.skin.verticalScrollbar;
    }
}