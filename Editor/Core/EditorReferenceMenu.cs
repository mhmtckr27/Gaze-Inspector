using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;

namespace GrayWolf.Gaze.Editor
{
    // Assets > Gaze > Add Gaze Editor Reference: adds GrayWolf.Gaze.Editor to the selected
    // assembly definitions, for users who write drawers against Gaze's editor API from their own
    // asmdef. The runtime assemblies need no such step: they ship as DLLs, which Unity references
    // from every assembly on its own.
    internal static class EditorReferenceMenu
    {
        private const string MenuPath = "Assets/Gaze/Add Gaze Editor Reference";
        private const string EditorAssemblyName = "GrayWolf.Gaze.Editor";

        [Serializable]
        private sealed class AsmdefData
        {
            public string name;
            public string[] references = Array.Empty<string>();
            public string[] includePlatforms = Array.Empty<string>();
            public string[] defineConstraints = Array.Empty<string>();
        }

        [MenuItem(MenuPath, true)]
        private static bool Validate() => Selection.objects.OfType<AssemblyDefinitionAsset>().Any();

        [MenuItem(MenuPath, false, 2000)]
        private static void Run()
        {
            var editorAsmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(EditorAssemblyName);
            if (string.IsNullOrEmpty(editorAsmdefPath))
            {
                Debug.LogError($"Gaze: could not find the {EditorAssemblyName} assembly definition.");
                return;
            }
            var reference = "GUID:" + AssetDatabase.AssetPathToGUID(editorAsmdefPath);

            foreach (var asset in Selection.objects.OfType<AssemblyDefinitionAsset>())
            {
                var path = AssetDatabase.GetAssetPath(asset);
                if (IsReadOnlyPackage(path))
                {
                    Debug.LogWarning($"Gaze: {path} is in a read-only package and was not changed.");
                    continue;
                }
                AddReference(path, reference);
            }
        }

        private static bool IsReadOnlyPackage(string assetPath)
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
            return package != null
                   && package.source != UnityEditor.PackageManager.PackageSource.Embedded
                   && package.source != UnityEditor.PackageManager.PackageSource.Local;
        }

        private static void AddReference(string path, string reference)
        {
            // Path.GetFullPath resolves Packages/... asset paths to the package's real folder.
            var file = Path.GetFullPath(path);
            var original = File.ReadAllText(file);
            var data = JsonUtility.FromJson<AsmdefData>(original);

            if (data.references.Any(r => r == reference || r == EditorAssemblyName))
            {
                Debug.Log($"Gaze: {path} already references {EditorAssemblyName}.");
                return;
            }

            // An editor reference in an assembly that also builds for players compiles in the
            // Editor and then breaks the player build, so only editor-only assemblies qualify.
            var editorOnly = (data.includePlatforms.Length == 1 && data.includePlatforms[0] == "Editor")
                             || data.defineConstraints.Contains("UNITY_EDITOR");
            if (!editorOnly)
            {
                EditorUtility.DisplayDialog("Gaze",
                    $"{data.name} is not an editor-only assembly, so it can't reference {EditorAssemblyName}: " +
                    "player builds would fail.\n\nPut your drawers in a separate assembly definition whose " +
                    "Include Platforms is Editor only, and add the reference there.",
                    "OK");
                return;
            }

            var updated = Insert(original, reference);
            var check = updated == null ? null : JsonUtility.FromJson<AsmdefData>(updated);
            if (check == null || check.name != data.name || check.references.Length != data.references.Length + 1
                || !check.references.Contains(reference))
            {
                Debug.LogError($"Gaze: could not edit {path} safely; it was left unchanged. " +
                               $"Add {EditorAssemblyName} to its Assembly Definition References by hand.");
                return;
            }

            File.WriteAllText(file, updated);
            AssetDatabase.ImportAsset(path);
            Debug.Log($"Gaze: added {EditorAssemblyName} to {path}.");
        }

        // Edits the JSON text in place rather than re-serializing it, so every other field and the
        // file's formatting stay exactly as they were.
        private static string Insert(string json, string reference)
        {
            var entry = "\"" + reference + "\"";
            var match = Regex.Match(json, "\"references\"\\s*:\\s*\\[(?<items>[^\\]]*)\\]");
            if (match.Success)
            {
                var items = match.Groups["items"];
                var newItems = items.Value.Trim().Length == 0
                    ? "\n        " + entry + "\n    "
                    : items.Value.TrimEnd() + ",\n        " + entry + "\n    ";
                return json.Substring(0, items.Index) + newItems + json.Substring(items.Index + items.Length);
            }

            var brace = json.IndexOf('{');
            if (brace < 0) return null;
            var rest = json.Substring(brace + 1);
            var separator = rest.TrimStart().StartsWith("}") ? "\n" : ",";
            return json.Substring(0, brace + 1) + "\n    \"references\": [\n        " + entry + "\n    ]" + separator + rest;
        }
    }
}
