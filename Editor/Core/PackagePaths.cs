using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace GrayWolf.Gaze.Editor
{
    // Resolves package-internal paths in the two coordinate systems that matter:
    //
    //   PackageRoot / EditorDir / IconPath -> AssetDatabase paths, for LoadAssetAtPath
    //   ToAbsolutePath                     -> filesystem paths, for raw File/Directory I/O
    //
    // Once the package is installed rather than copied into Assets, those two are no
    // longer a shared prefix apart. UPM addresses a package as "Packages/<name>" while
    // the files live in Library/PackageCache/<name>@<version> (registry, git URL or
    // tarball install) or wherever the folder happens to sit on disk (local "file:"
    // install, which can be outside the project entirely). So ask UPM for both paths,
    // and only fall back to this file's own compile-time location when UPM has no
    // answer, which is the case when the package lives under Assets/: developed in
    // place, or installed from the Asset Store.
    internal static class PackagePaths
    {
        private static bool s_resolved;
        private static string s_packageRoot;
        private static string s_diskRoot;

        // AssetDatabase path to this package's root folder. "Packages/net.graywolfstudio.gaze"
        // when installed, "Assets/<publisher>/<asset>" when it lives under Assets/.
        internal static string PackageRoot
        {
            get
            {
                Resolve();
                return s_packageRoot;
            }
        }

        // AssetDatabase path to this package's Editor/ folder.
        internal static string EditorDir => $"{PackageRoot}/Editor";

        internal static string IconsFolder => $"{EditorDir}/Icons";

        internal static string IconPath(string fileName) => $"{IconsFolder}/{fileName}";

        // Absolute filesystem path to this package's root folder.
        private static string DiskRoot
        {
            get
            {
                Resolve();
                return s_diskRoot;
            }
        }

        // Turns a project-relative path into an absolute filesystem path, for the cases that
        // need raw File I/O rather than the AssetDatabase: reading package.json, copying the
        // hidden Samples~ folder, scanning an asset file for serialization drift. Accepts
        // package-internal paths and ordinary Assets/... paths alike.
        internal static string ToAbsolutePath(string projectRelativePath)
        {
            if (string.IsNullOrEmpty(projectRelativePath))
                return projectRelativePath;

            var normalized = projectRelativePath.Replace('\\', '/');

            // Inside this package: rebase onto where this package actually is on disk,
            // which for an installed package is nowhere near "Packages/<name>".
            if (DiskRoot != null && PackageRoot != null && IsUnder(normalized, PackageRoot))
                return DiskRoot + normalized.Substring(PackageRoot.Length);

            // Inside some other package: ask UPM where that one landed.
            if (normalized.StartsWith("Packages/", StringComparison.Ordinal))
            {
                var other = PackageInfo.FindForAssetPath(normalized);
                if (other != null && !string.IsNullOrEmpty(other.resolvedPath) && !string.IsNullOrEmpty(other.assetPath))
                    return Normalize(other.resolvedPath) + normalized.Substring(other.assetPath.Length);
            }

            return ProjectRoot + normalized;
        }

        // ".../<project>/", with the trailing slash.
        private static string ProjectRoot
        {
            get
            {
                var dataPath = Application.dataPath.Replace('\\', '/'); // .../Assets
                return dataPath.Substring(0, dataPath.Length - "Assets".Length);
            }
        }

        private static bool IsUnder(string path, string root) =>
            path.Equals(root, StringComparison.Ordinal) ||
            path.StartsWith(root + "/", StringComparison.Ordinal);

        private static string Normalize(string path) => path.Replace('\\', '/').TrimEnd('/');

        private static void Resolve()
        {
            if (s_resolved) return;
            s_resolved = true;

            // Installed as a UPM package: registry, git URL, local tarball or local folder.
            var info = PackageInfo.FindForAssembly(typeof(PackagePaths).Assembly);
            if (info != null && !string.IsNullOrEmpty(info.assetPath))
            {
                s_packageRoot = Normalize(info.assetPath);
                s_diskRoot = string.IsNullOrEmpty(info.resolvedPath) ? null : Normalize(info.resolvedPath);
                return;
            }

            // Under Assets/: developed in place, or installed from the Asset Store.
            var absolute = ResolvePackageRootAbsolute();
            s_diskRoot = absolute;
            s_packageRoot = ToProjectRelative(absolute);
        }

        // thisFilePath points at .../<package root>/Editor/Core/PackagePaths.cs, so the root
        // is three directories up. It is baked in when Unity compiles the source, which is
        // exactly why it can only be the fallback: it says nothing about where a consumer's
        // package manager later put the files.
        private static string ResolvePackageRootAbsolute([CallerFilePath] string thisFilePath = null)
        {
            var coreDir = Path.GetDirectoryName(thisFilePath);   // .../Editor/Core
            var editorDir = Path.GetDirectoryName(coreDir);      // .../Editor
            var root = Path.GetDirectoryName(editorDir);         // .../<package root>

            return Normalize(Path.GetFullPath(root ?? string.Empty));
        }

        private static string ToProjectRelative(string absolutePath)
        {
            var projectRoot = ProjectRoot;

            if (absolutePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                return absolutePath.Substring(projectRoot.Length);

            Debug.LogWarning($"[Gaze] Could not resolve '{absolutePath}' relative to project root '{projectRoot}'.");
            return absolutePath;
        }
    }
}
