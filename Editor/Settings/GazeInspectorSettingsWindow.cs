using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    internal sealed class GazeInspectorSettingsWindow : EditorWindow
    {
        [Serializable]
        private sealed class AuthorInfo
        {
            public string name;
            public string email;
            public string url;
        }

        [Serializable]
        private sealed class PackageManifest
        {
            public string displayName;
            public string version;
            public string description;
            public string documentationUrl;
            public string changelogUrl;
            public string licensesUrl;
            public AuthorInfo author;
        }

        private const float HeaderHeight = 64f;
        private const float WindowMinWidth = 420f;
        private const float WindowMinHeight = 460f;
        private const float CardSpacing = 10f;
        private const float ButtonHeight = 26f;
        private const float ButtonSpacing = 4f;
        
        private const string DiscordUrl = "https://www.graywolfstudio.net/discord";
        private const string YouTubeUrl = "https://www.graywolfstudio.net/youtube";

        // Shown only when package.json could not be read; the real address comes from there.
        private const string FallbackEmail = "info@graywolfstudio.net";
        private const double CopiedFadeoutSeconds = 1.2;

        private static readonly string[] TabLabels = { "General", "Appearance", "Troubleshooting", "About" };

        [SerializeField] private int _selectedTab;
        private Vector2 _scroll;

        private GUIStyle _headerTitleStyle;
        private GUIStyle _headerVersionStyle;
        private GUIStyle _tabButtonStyle;
        private GUIStyle _tabButtonActiveStyle;
        private GUIStyle _sectionTitleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _cardStyle;
        private bool _stylesBuilt;

        private Texture2D _webIcon;
        private bool _iconsLoaded;

        private PackageManifest _manifest;
        private bool _manifestLoaded;

        [MenuItem("Tools/Gray Wolf/Gaze/Settings")]
        internal static void Open()
        {
            var window = GetWindow<GazeInspectorSettingsWindow>(false, "Gaze");
            window.minSize = new Vector2(WindowMinWidth, WindowMinHeight);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Gaze", EditorGUIUtility.IconContent("d_unityeditor.inspectorwindow").image);
        }

        private void OnGUI()
        {
            BuildStylesIfNeeded();
            LoadIconsIfNeeded();
            LoadManifestIfNeeded();

            var settings = GazeInspectorSettings.instance;

            DrawHeader(settings);
            DrawTabBar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.Space(12);
            switch (_selectedTab)
            {
                case 0: DrawGeneralTab(settings); break;
                case 1: DrawAppearanceTab(settings); break;
                case 2: DrawTroubleshootingTab(settings); break;
                case 3: DrawAboutTab(); break;
            }
            EditorGUILayout.Space(16);
            EditorGUILayout.EndScrollView();
        }

        // --------- Header / tabs --------- //

        private void DrawHeader(GazeInspectorSettings settings)
        {
            var rect = GUILayoutUtility.GetRect(position.width, /*HeaderHeight,*/ position.width * 0.2822265625f, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(rect, EditorGuiStyles._bannerBg, ScaleMode.StretchToFill);

            var textX = rect.xMin + 10;

            var versionText = string.IsNullOrEmpty(_manifest?.version) ? "" : $"v{_manifest.version}";
            var versionRect = new Rect(textX, rect.yMax - 24, rect.width - textX - 16, 18);
            GUI.Label(versionRect, versionText, _headerVersionStyle);
        }

        private void DrawTabBar()
        {
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                for (var i = 0; i < TabLabels.Length; i++)
                {
                    var active = i == _selectedTab;
                    if (GUILayout.Toggle(active, TabLabels[i], active ? _tabButtonActiveStyle : _tabButtonStyle))
                        _selectedTab = i;
                }
            }

            var underlineRect = GUILayoutUtility.GetRect(position.width, 2f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(underlineRect, EditorGUIUtility.isProSkin ? new Color(0, 0, 0, 0.35f) : new Color(0, 0, 0, 0.15f));
        }

        // --------- Tabs --------- //

        private void DrawGeneralTab(GazeInspectorSettings settings)
        {
            BeginCard();
            EditorGUILayout.LabelField("Inspector Takeover", _sectionTitleStyle);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Enable Gaze", GUILayout.ExpandWidth(true));
                var newEnabled = EditorGUILayout.Toggle(settings.Enabled, GUILayout.Width(18));
                if (newEnabled != settings.Enabled)
                    settings.Enabled = newEnabled;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(
                "When enabled, Gaze draws the inspector for any object that " +
                "doesn't already have its own custom editor, yours or from another asset. " +
                "Existing [CustomEditor] implementations always take priority and are never " +
                "overridden.",
                _bodyStyle);

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "Turn this off to fall back to Unity's built-in inspector.",
                MessageType.Info);
            EndCard();
        }

        private void DrawTroubleshootingTab(GazeInspectorSettings settings)
        {
            BeginCard();
            EditorGUILayout.LabelField("Saved Data", _sectionTitleStyle);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField(
                "Unity can write a component's old data back to disk while the Inspector goes on showing " +
                "what you typed. No error, and the edit is gone at the next script reload. It is rare.",
                _bodyStyle);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(
                "Gaze is what tells you an object's values, so showing one that never reached the file is " +
                "this tool getting its own job wrong. That is why the check lives here.",
                _bodyStyle);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(
                "After each save it compares what you edited against what landed in the file. A component " +
                "that was skipped says so, and can be rebuilt in place: same values, same references.",
                _bodyStyle);

            EditorGUILayout.Space(10);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Check after saving", GUILayout.ExpandWidth(true));
                var newValidate = EditorGUILayout.Toggle(settings.ValidateSerialization, GUILayout.Width(18));
                if (newValidate != settings.ValidateSerialization)
                    settings.ValidateSerialization = newValidate;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(
                "Only what you changed is checked, so this costs nothing on a large scene.",
                _bodyStyle);

            EditorGUILayout.Space(15);

            EditorGUILayout.LabelField(
                "If you need to check whole scene for some case, button below sweeps every component in the open scenes instead of only what you touched. Takes as long " +
                "as the scene is big, and can be cancelled.",
                _bodyStyle);

            if (GUILayout.Button("Verify Saved Data"))
                SerializationWatcher.VerifySavedData();

            EditorGUILayout.Space(6);
            EndCard();

            EditorGUILayout.Space(CardSpacing);

            BeginCard();
            EditorGUILayout.LabelField("Missing SerializeReference Types", _sectionTitleStyle);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField(
                "Renaming, moving or deleting a type breaks every [SerializeReference] field pointing at " +
                "it. Unity stores those references by class, namespace and assembly, so changing any of the " +
                "three leaves a record it can no longer read.",
                _bodyStyle);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(
                "The type picker Gaze puts on those fields is how the references got assigned, so helping " +
                "you recover them when a type moves belongs here too.",
                _bodyStyle);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(
                "An error box on the object names the missing types. Rename the type back, or add " +
                "[MovedFrom] to its replacement, to get the values back. Verify Saved Data finds every " +
                "object in this state at once.",
                _bodyStyle);

            EndCard();
        }

        private void DrawAppearanceTab(GazeInspectorSettings settings)
        {
            BeginCard();
            EditorGUILayout.LabelField("Collections", _sectionTitleStyle);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Page Size", GUILayout.ExpandWidth(true));
                var newPageSize = EditorGUILayout.DelayedIntField(settings.PageSize, GUILayout.Width(48));
                if (newPageSize != settings.PageSize)
                    settings.PageSize = newPageSize;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(
                "Lists and dictionaries longer than this are split into pages. Applies to every " +
                "collection in the inspector.",
                _bodyStyle);

            EditorGUILayout.Space(10);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Dictionary Value Foldout Threshold", GUILayout.ExpandWidth(true));
                var newThreshold = EditorGUILayout.DelayedIntField(
                    settings.DictionaryFoldoutThreshold, GUILayout.Width(48));

                if (newThreshold != settings.DictionaryFoldoutThreshold)
                    settings.DictionaryFoldoutThreshold = newThreshold;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(
                "A dictionary value with more fields than this draws as a collapsible row instead of " +
                "being laid out in full. Naming a value title field on the dictionary always draws " +
                "collapsible rows, whatever this is set to.",
                _bodyStyle);
            EndCard();
        }

        private void DrawAboutTab()
        {
            BeginCard();
            EditorGUILayout.LabelField(_manifest?.displayName ?? "Gaze", _sectionTitleStyle);
            EditorGUILayout.Space(4);
            if (!string.IsNullOrEmpty(_manifest?.description))
                EditorGUILayout.LabelField(_manifest.description, _bodyStyle);

            EditorGUILayout.Space(10);
            if (!string.IsNullOrEmpty(_manifest?.author?.name))
                EditorGUILayout.LabelField($"By {_manifest.author.name}", EditorStyles.miniLabel);
            EndCard();

            BeginCard();
            EditorGUILayout.LabelField("Connect", _sectionTitleStyle);
            EditorGUILayout.Space(6);

            var contactRow = ReserveButtonRow();
            DrawLinkButton(RowCell(contactRow, 0, 2), new GUIContent("Website", _webIcon), _manifest?.author?.url);
            DrawEmailButton(RowCell(contactRow, 1, 2), _manifest?.author?.email);

            EditorGUILayout.Space(4);

            var socialRow = ReserveButtonRow();
            DrawLinkButton(RowCell(socialRow, 0, 2), new GUIContent(" Discord", EditorGuiStyles._discordIcon), DiscordUrl);
            DrawLinkButton(RowCell(socialRow, 1, 2), new GUIContent(" YouTube", EditorGuiStyles._youtubeIcon), YouTubeUrl);
            EndCard();

            BeginCard();
            EditorGUILayout.LabelField("Resources", _sectionTitleStyle);
            EditorGUILayout.Space(6);

            var resourcesRow = ReserveButtonRow();
            DrawLinkButton(RowCell(resourcesRow, 0, 3), new GUIContent("Documentation"), NullIfPlaceholder(_manifest?.documentationUrl));
            DrawLinkButton(RowCell(resourcesRow, 1, 3), new GUIContent("Changelog"), NullIfPlaceholder(_manifest?.changelogUrl));
            DrawLinkButton(RowCell(resourcesRow, 2, 3), new GUIContent("License"), NullIfPlaceholder(_manifest?.licensesUrl));
            EndCard();

            BeginCard();
            EditorGUILayout.LabelField("Explore", _sectionTitleStyle);
            EditorGUILayout.Space(6);
            DrawDemoSceneButton();
            EndCard();
        }

        // Reserves one full-width row of button height, to be divided up with RowCell.
        private static Rect ReserveButtonRow() =>
            GUILayoutUtility.GetRect(0f, ButtonHeight, GUILayout.ExpandWidth(true));

        // GUILayout gives a stretchable control its own content width plus an equal share of
        // whatever space is left, so a longer label produces a wider button and a row of them
        // comes out ragged. These rows are meant to read as even columns, so place them by hand.
        private static Rect RowCell(Rect row, int index, int count)
        {
            var width = (row.width - ButtonSpacing * (count - 1)) / count;
            return new Rect(row.x + index * (width + ButtonSpacing), row.y, width, row.height);
        }

        // Renders an enabled link button when url is a real value, or a disabled one (with a
        // "not set up yet" tooltip) when it's null/empty/still a placeholder - so unfinished
        // links read as "not configured" rather than as dead or broken buttons.
        private static void DrawLinkButton(Rect rect, GUIContent content, string url)
        {
            var hasUrl = !string.IsNullOrEmpty(url);
            using (new EditorGUI.DisabledScope(!hasUrl))
            {
                var display = hasUrl ? content : new GUIContent(content.text, content.image, "Not set up yet.");
                if (GUI.Button(rect, display) && hasUrl)
                    Application.OpenURL(url);
            }
        }

        // Copies rather than opening a mailto: link. mailto: needs a mail client registered
        // with the OS, and on a machine without one the click does nothing at all, with no
        // way to tell that from a broken button. Copying always works and says so.
        private void DrawEmailButton(Rect rect, string email)
        {
            var hasEmail = !string.IsNullOrEmpty(email);
            var content = new GUIContent(
                $" {(hasEmail ? email : FallbackEmail)}",
                EditorGuiStyles._emailIcon,
                hasEmail ? "Copy to clipboard" : "Not set up yet.");

            using (new EditorGUI.DisabledScope(!hasEmail))
            {
                if (!GUI.Button(rect, content) || !hasEmail)
                    return;

                EditorGUIUtility.systemCopyBuffer = email;
                ShowNotification(new GUIContent("Copied!"), CopiedFadeoutSeconds);
            }
        }

        private static string NullIfPlaceholder(string url) =>
            string.IsNullOrEmpty(url) || url.Contains("example.com") ? null : url;

        // --------- Demo scene: works whether Samples/ is live (dev-time) or Samples~/ (shipped) --------- //

        private enum DemoState { Available, NeedsImport, Missing }

        private void DrawDemoSceneButton()
        {
            var (state, scenePath) = ResolveDemoScene();

            using (new EditorGUI.DisabledScope(state == DemoState.Missing))
            {
                var label = state switch
                {
                    DemoState.Available => "Open Demo Scene",
                    DemoState.NeedsImport => "Import Samples",
                    _ => "Demo Scene Unavailable"
                };

                if (!GUILayout.Button(label, GUILayout.Height(26)))
                    return;

                // Deferred: SaveCurrentModifiedScenesIfUserWantsTo can pop a modal dialog, and
                // OpenScene/AssetDatabase.Refresh are heavy enough to reenter Unity's GUI event
                // pump. Running any of that synchronously inside this click handler - while
                // we're still nested inside BeginScrollView/BeginVertical - corrupts the
                // GUILayout group stack ("Invalid GUILayout state"). delayCall runs it after
                // OnGUI has fully returned instead.
                switch (state)
                {
                    case DemoState.Available:
                        EditorApplication.delayCall += () => OpenScene(scenePath);
                        break;
                    case DemoState.NeedsImport:
                        EditorApplication.delayCall += ImportDemoSamples;
                        break;
                }
            }
        }

        // scenePath in the returned tuple is always the path the scene will live at once
        // available - either where it already is (Available) or where importing will put it
        // (NeedsImport) - so the caller never has to re-derive it.
        private (DemoState state, string scenePath) ResolveDemoScene()
        {
            const string liveRelative = "Samples/Demo/InspectorDemo.unity";
            var livePath = $"{PackagePaths.PackageRoot}/{liveRelative}";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(livePath) != null)
                return (DemoState.Available, livePath);

            var importedPath = $"{ImportedDemoRoot}/InspectorDemo.unity";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(importedPath) != null)
                return (DemoState.Available, importedPath);

            var hiddenSourceAbsolute = PackagePaths.ToAbsolutePath($"{PackagePaths.PackageRoot}/Samples~/Demo");
            if (Directory.Exists(hiddenSourceAbsolute))
                return (DemoState.NeedsImport, importedPath);

            return (DemoState.Missing, null);
        }

        // Assets/Samples/<display name>/<version>/Demo - mirrors the Package Manager's own
        // sample-import convention so this reads as familiar once the package ships via UPM.
        private string ImportedDemoRoot
        {
            get
            {
                var displayName = string.IsNullOrEmpty(_manifest?.displayName) ? "Gaze" : _manifest.displayName;
                var version = string.IsNullOrEmpty(_manifest?.version) ? "0.0.0" : _manifest.version;
                return $"Assets/Samples/{displayName}/{version}/Demo";
            }
        }

        // Imports and stops there. The click agreed to bring the samples into the project, not
        // to leave whatever scene is open, so the demo is left for the user to open: the button
        // re-reads as "Open Demo Scene" once the files land. Pings the imported folder instead,
        // so it is clear what arrived and where it went.
        private void ImportDemoSamples()
        {
            var sourceAbsolute = PackagePaths.ToAbsolutePath($"{PackagePaths.PackageRoot}/Samples~/Demo");
            var targetRelative = ImportedDemoRoot;
            var targetAbsolute = PackagePaths.ToAbsolutePath(targetRelative);

            if (!Directory.Exists(sourceAbsolute))
            {
                Debug.LogWarning($"[Gaze] Demo samples not found at '{sourceAbsolute}'.");
                return;
            }

            CopyDirectoryRecursive(sourceAbsolute, targetAbsolute);
            AssetDatabase.Refresh();

            var importedFolder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetRelative);
            if (importedFolder != null)
                EditorGUIUtility.PingObject(importedFolder);
            else
                Debug.LogWarning($"[Gaze] Imported the demo samples, but '{targetRelative}' did not resolve to a folder to ping.");

            // The button reads its label from ResolveDemoScene, which only now reports
            // Available. Without this it keeps saying "Import Samples" until the next repaint.
            Repaint();
        }

        private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite: true);

            foreach (var dir in Directory.GetDirectories(sourceDir))
                CopyDirectoryRecursive(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }

        private static void OpenScene(string scenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(scenePath);
        }

        private void LoadManifestIfNeeded()
        {
            if (_manifestLoaded) return;
            _manifestLoaded = true;

            try
            {
                var absolutePath = PackagePaths.ToAbsolutePath($"{PackagePaths.PackageRoot}/package.json");
                if (!File.Exists(absolutePath)) return;

                var json = File.ReadAllText(absolutePath);
                _manifest = JsonUtility.FromJson<PackageManifest>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Gaze] Could not read package.json: {e.Message}");
            }
        }

        private void BeginCard()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
        }

        private static void EndCard()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(CardSpacing);
        }

        // Loaded once and cached - not because it's expensive, but because loading is intentionally
        // kept to a plain AssetDatabase read here. ScalableGUIContent.FromPackageIcon (used
        // elsewhere for tinted/padded icons) does its rescaling via RenderTexture.GetTemporary +
        // Graphics.Blit + RenderTexture.active swaps, which are GPU render-state changes. Doing
        // that during OnGUI - even once, even cached - corrupts Unity's GUIClip stack for that
        // Repaint pass ("pushing more GUIClips than popping", cascading into "Invalid GUILayout
        // state"). GUIContent's own image display already scales an arbitrary-resolution source
        // texture safely at draw time, so there's no need for that pipeline for a plain icon.
        private void LoadIconsIfNeeded()
        {
            if (_iconsLoaded) return;
            _iconsLoaded = true;

            _webIcon = EditorGUIUtility.IconContent("d_buildsettings.web.small").image as Texture2D;
        }

        private void BuildStylesIfNeeded()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            _headerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                normal = { textColor = Color.white },
            };

            _headerVersionStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 1f, 1f, 0.75f) },
                fontSize = 11
            };

            _tabButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fixedHeight = 24,
                fontStyle = FontStyle.Normal,
            };

            _tabButtonActiveStyle = new GUIStyle(_tabButtonStyle)
            {
                fontStyle = FontStyle.Bold,
            };

            _sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };

            _bodyStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                normal = { textColor = EditorStyles.label.normal.textColor * new Color(1, 1, 1, 0.85f) },
            };

            var cardBg = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.045f) : new Color(0, 0, 0, 0.035f);
            var cardTex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            cardTex.SetPixel(0, 0, cardBg);
            cardTex.Apply();

            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = cardTex },
                padding = new RectOffset(14, 14, 12, 12),
                margin = new RectOffset(16, 16, 0, 0), // symmetric left/right inset from the scroll view
                border = new RectOffset(0, 0, 0, 0),
            };
        }
    }
}
