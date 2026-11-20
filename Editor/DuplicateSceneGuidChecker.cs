using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityRuntimeGuid.Editor
{
    /// <summary>
    /// Detects scene assets sharing a scene GUID. Duplicating a scene asset copies its <see cref="SceneGuidRegistry"/>,
    /// so the copy carries the scene GUID and object GUIDs of its source and both are identified as one scene at
    /// runtime. Detection reads the serialized scene files (text serialization only) without opening them.
    /// </summary>
    public static class DuplicateSceneGuidChecker
    {
        private const string MenuPath = "Tools/Unity Runtime GUID/Find Duplicate Scene GUIDs";
        private const string SkippedGroupsSessionKey = "UnityRuntimeGuid.SkippedDuplicateSceneGroups";

        private static readonly Regex SceneGuidRegex =
            new(@"^\s*sceneGuid:\s*([0-9a-fA-F]{32})\s*$", RegexOptions.Compiled);

        // Scene path -> (last write time, scene GUID or null), to avoid reading unchanged scene files again.
        private static readonly Dictionary<string, (DateTime lastWrite, string guid)> SceneGuidCache = new();

        private static string _registryScriptGuid;

        /// <summary>Paths of every scene asset under Assets.</summary>
        public static IEnumerable<string> GetProjectScenePaths()
        {
            return AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct();
        }

        /// <summary>Groups of scene paths sharing a scene GUID. Scenes without registry are ignored.</summary>
        public static List<List<string>> FindDuplicateGroups(IEnumerable<string> scenePaths)
        {
            return scenePaths
                .Select(path => (path, guid: GetSceneGuid(path)))
                .Where(scene => scene.guid != null)
                .GroupBy(scene => scene.guid)
                .Where(group => group.Count() > 1)
                .Select(group => group.Select(scene => scene.path).OrderBy(path => path, StringComparer.Ordinal).ToList())
                .ToList();
        }

        /// <summary>
        /// Scene GUID serialized in the scene file, or null if the scene has no registry or is not text-serialized.
        /// </summary>
        public static string GetSceneGuid(string scenePath)
        {
            var lastWrite = File.GetLastWriteTimeUtc(scenePath);

            if (SceneGuidCache.TryGetValue(scenePath, out var cached) && cached.lastWrite == lastWrite)
                return cached.guid;

            var guid = ReadSceneGuid(scenePath);
            SceneGuidCache[scenePath] = (lastWrite, guid);
            return guid;
        }

        private static string ReadSceneGuid(string scenePath)
        {
            var scriptGuid = GetRegistryScriptGuid();

            try
            {
                var isFirstLine = true;
                var inRegistry = false;

                foreach (var line in File.ReadLines(scenePath))
                {
                    if (isFirstLine)
                    {
                        // Binary-serialized scene.
                        if (!line.StartsWith("%YAML"))
                            return null;
                        isFirstLine = false;
                        continue;
                    }

                    if (line.StartsWith("--- "))
                    {
                        inRegistry = false;
                        continue;
                    }

                    if (line.Contains("m_Script:"))
                    {
                        // Only accept a sceneGuid field from a SceneGuidRegistry component when the script is known.
                        inRegistry = scriptGuid == null || line.Contains(scriptGuid);
                        continue;
                    }

                    if (!inRegistry)
                        continue;

                    var match = SceneGuidRegex.Match(line);
                    if (match.Success)
                        return match.Groups[1].Value.ToLowerInvariant();
                }
            }
            catch (IOException)
            {
                // Deleted or locked file.
            }
            catch (UnauthorizedAccessException)
            {
            }

            return null;
        }

        private static string GetRegistryScriptGuid()
        {
            if (_registryScriptGuid != null)
                return _registryScriptGuid;

            foreach (var guid in AssetDatabase.FindAssets("SceneGuidRegistry t:MonoScript"))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (script == null || script.GetClass() != typeof(SceneGuidRegistry))
                    continue;
                _registryScriptGuid = guid;
                break;
            }

            return _registryScriptGuid;
        }

        /// <summary>
        /// Assigns a new scene GUID and new object GUIDs to the scene at <paramref name="scenePath"/> and saves it,
        /// opening it additively if it is not loaded.
        /// </summary>
        public static bool RegenerateGuids(string scenePath)
        {
            if (Application.isPlaying)
                throw new InvalidOperationException($"Calling {nameof(RegenerateGuids)} in play mode is not allowed.");

            var scene = SceneManager.GetSceneByPath(scenePath);
            var wasLoaded = scene.IsValid() && scene.isLoaded;

            // Saving the scene below would also save the user's pending changes: let them decide first.
            if (wasLoaded && scene.isDirty && !EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { scene }))
                return false;

            if (!wasLoaded)
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            var registry = SceneGuidRegistry.GetOrCreate(scene);
            registry.RegenerateGuids();
            EditorUtility.SetDirty(registry);
            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);

            if (!wasLoaded)
                EditorSceneManager.CloseScene(scene, true);

            SceneGuidCache.Remove(scenePath);

            if (saved)
                Debug.Log($"Regenerated the scene and object GUIDs of {scenePath}.");
            else
                Debug.LogError($"Could not save {scenePath} after regenerating its GUIDs.");

            return saved;
        }

        [MenuItem(MenuPath)]
        private static void FindDuplicatesMenu()
        {
            var groups = FindDuplicateGroups(GetProjectScenePaths());

            if (groups.Count == 0)
            {
                EditorUtility.DisplayDialog("Duplicate scene GUIDs", "No scenes share a scene GUID.", "OK");
                return;
            }

            foreach (var group in groups)
                PromptRegenerate(group, Array.Empty<string>());
        }

        [MenuItem(MenuPath, true)]
        private static bool FindDuplicatesMenuValidate()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        /// <summary>Checks the imported scenes against the project and offers to fix the copies.</summary>
        internal static void CheckImportedScenes(IReadOnlyCollection<string> importedScenePaths)
        {
            var groups = FindDuplicateGroups(GetProjectScenePaths())
                .Where(group => group.Any(importedScenePaths.Contains))
                .ToList();

            foreach (var group in groups)
            {
                var interactive = !Application.isBatchMode && !EditorApplication.isPlayingOrWillChangePlaymode;

                if (!interactive || IsSkipped(group))
                {
                    Debug.LogWarning(FormatWarning(group));
                    continue;
                }

                PromptRegenerate(group, importedScenePaths);
            }
        }

        private static void PromptRegenerate(List<string> group, IReadOnlyCollection<string> importedScenePaths)
        {
            var original = PickOriginal(group, importedScenePaths);
            var copies = group.Where(path => path != original).ToList();

            var message =
                $"These scenes share the scene GUID {GetSceneGuid(original)}, most likely because they were duplicated " +
                "from one another. They also share their object GUIDs, so they are identified as one scene at runtime " +
                "(e.g. in records).\n\n" +
                $"Keep the GUIDs of:\n{original}\n\n" +
                $"Regenerate the scene and object GUIDs of:\n{string.Join("\n", copies)}";

            var choice = EditorUtility.DisplayDialogComplex("Duplicate scene GUID", message,
                "Regenerate", "Skip", "Keep another scene");

            if (choice == 2)
            {
                // Rotate the kept scene through the group.
                var next = group[(group.IndexOf(original) + 1) % group.Count];
                PromptRegenerateKeeping(group, next);
                return;
            }

            if (choice != 0)
            {
                Skip(group);
                Debug.LogWarning(FormatWarning(group));
                return;
            }

            foreach (var copy in copies)
                RegenerateGuids(copy);
        }

        private static void PromptRegenerateKeeping(List<string> group, string original)
        {
            // Treat every other scene as imported so that PickOriginal keeps the chosen one.
            PromptRegenerate(group, group.Where(path => path != original).ToList());
        }

        // The source of a duplication is the scene that was not just imported. Otherwise prefer the first scene in the
        // build settings, then the oldest file.
        private static string PickOriginal(List<string> group, IReadOnlyCollection<string> importedScenePaths)
        {
            var candidates = group.Where(path => !importedScenePaths.Contains(path)).ToList();
            if (candidates.Count == 0)
                candidates = group;

            var buildScenePaths = EditorBuildSettings.scenes.Select(scene => scene.path).ToList();

            return candidates
                .OrderBy(path =>
                {
                    var index = buildScenePaths.IndexOf(path);
                    return index < 0 ? int.MaxValue : index;
                })
                .ThenBy(File.GetCreationTimeUtc)
                .ThenBy(path => path, StringComparer.Ordinal)
                .First();
        }

        private static string FormatWarning(IEnumerable<string> group)
        {
            return "These scenes share a scene GUID and object GUIDs (duplicated scene assets?); they are identified " +
                   "as one scene at runtime. Regenerate the GUIDs of the copies from " +
                   $"'{MenuPath.Replace("/", " > ")}':\n{string.Join("\n", group)}";
        }

        private static string GroupKey(IEnumerable<string> group) => string.Join("|", group);

        private static bool IsSkipped(IEnumerable<string> group)
        {
            return SessionState.GetString(SkippedGroupsSessionKey, "").Split('\n').Contains(GroupKey(group));
        }

        private static void Skip(IEnumerable<string> group)
        {
            var skipped = SessionState.GetString(SkippedGroupsSessionKey, "");
            SessionState.SetString(SkippedGroupsSessionKey, skipped + "\n" + GroupKey(group));
        }
    }

    internal class DuplicateSceneGuidPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            var importedScenes = importedAssets
                .Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (importedScenes.Count == 0)
                return;

            // Scenes cannot be opened and saved during an import.
            EditorApplication.delayCall += () => DuplicateSceneGuidChecker.CheckImportedScenes(importedScenes);
        }
    }

    internal class DuplicateSceneGuidBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var buildScenePaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path);

            foreach (var group in DuplicateSceneGuidChecker.FindDuplicateGroups(buildScenePaths))
            {
                Debug.LogError("Build scenes share a scene GUID and object GUIDs; they will be identified as one " +
                               "scene at runtime. Regenerate the GUIDs of the copies from " +
                               $"'Tools > Unity Runtime GUID > Find Duplicate Scene GUIDs':\n{string.Join("\n", group)}");
            }
        }
    }
}
