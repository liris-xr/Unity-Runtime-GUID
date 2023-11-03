using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR

namespace UnityRuntimeGuid.Editor
{
    public static class GuidRegistryUpdater
    {
        public static IEnumerable<string> GetAllScenePaths(bool forceIncludeActiveScene)
        {
            var activeScenePath = SceneManager.GetActiveScene().path;

            var scenePaths = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .DefaultIfEmpty(activeScenePath).ToList();

            if (forceIncludeActiveScene && !scenePaths.Contains(activeScenePath))
            {
                scenePaths.Add(activeScenePath);
            }

            return scenePaths;
        }

        public static void UpdateAssetsGuidRegistry(IEnumerable<string> scenePaths)
        {
            var assetsGuidRegistry = AssetsGuidRegistry.Instance;
            var prevAssetsGuid = assetsGuidRegistry.Copy();

            assetsGuidRegistry.Clear();
            AssetsGuidRegistry.CommitRegistry(assetsGuidRegistry);

            var assets = new List<Object>();
            assets.Add(GraphicsSettings.currentRenderPipeline);
            assets.Add(RenderSettings.skybox);
            assets.Add(RenderSettings.sun);
#if UNITY_2022_1_OR_NEWER
            assets.Add(RenderSettings.customReflectionTexture);
#else
            assets.Add(RenderSettings.customReflection);
#endif

            foreach (var scenePath in scenePaths)
            {
                var dependencies = AssetDatabase.GetDependencies(scenePath, true);

                foreach (var dependency in dependencies)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(dependency);
                    if (assets.Contains(asset)) continue;
                    assets.Add(asset);
                }
            }

            foreach (var asset in assets.Where(IsAsset))
            {
                if (asset.GetType().Namespace == typeof(UnityEditor.Editor).Namespace)
                    continue;
                var hasPrevGuid = prevAssetsGuid.TryGetValue(asset, out var prevAssetGuid);
                assetsGuidRegistry.TryAdd(hasPrevGuid ? prevAssetGuid : AssetsGuidRegistry.CreateNewEntry(asset));
            }

            AssetsGuidRegistry.CommitRegistry(assetsGuidRegistry);
        }

        public static void ClearAssetsGuidRegistry()
        {
            var assetsGuidRegistry = AssetsGuidRegistry.Instance;
            assetsGuidRegistry.Clear();
            AssetsGuidRegistry.CommitRegistry(assetsGuidRegistry);
        }

        [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
        public static void UpdateScenesGuidRegistry(IEnumerable<string> scenePaths)
        {
            var prevScenePath = SceneManager.GetActiveScene().path;

            foreach (var scenePath in scenePaths)
            {
                if (SceneManager.GetActiveScene().path != scenePath)
                    EditorSceneManager.OpenScene(scenePath);

                var sceneObjects = new List<Object>();

                var scene = SceneManager.GetActiveScene();
                var sceneObjectsGuidRegistry = SceneGuidRegistry.GetOrCreateInScene(scene);
                var prevSceneObjectsGuid = sceneObjectsGuidRegistry.Copy();
                sceneObjectsGuidRegistry.Clear();

                sceneObjects.AddRange(scene.GetRootGameObjects());
                sceneObjects.AddRange(EditorUtility.CollectDependencies(scene.GetRootGameObjects()));

                foreach (var sceneObject in sceneObjects.Where(sceneObject => !IsAsset(sceneObject)))
                {
                    var hasPrevGuid = prevSceneObjectsGuid.TryGetValue(sceneObject, out var prevSceneObjectGuid);
                    sceneObjectsGuidRegistry.TryAdd(hasPrevGuid
                        ? prevSceneObjectGuid
                        : SceneGuidRegistry.CreateNewEntry(sceneObject));
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (SceneManager.GetActiveScene().path != prevScenePath)
                EditorSceneManager.OpenScene(prevScenePath);
        }

        public static void ClearScenesGuidRegistry(IEnumerable<string> scenePaths)
        {
            var prevScenePath = SceneManager.GetActiveScene().path;

            foreach (var scenePath in scenePaths)
            {
                if (SceneManager.GetActiveScene().path != scenePath)
                    EditorSceneManager.OpenScene(scenePath);

                var scene = SceneManager.GetActiveScene();
                var sceneObjectsGuidRegistry = SceneGuidRegistry.GetOrCreateInScene(scene);
                sceneObjectsGuidRegistry.Clear();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (SceneManager.GetActiveScene().path != prevScenePath)
                EditorSceneManager.OpenScene(prevScenePath);
        }

        private static bool IsAsset(Object obj)
        {
            return obj != null && AssetDatabase.Contains(obj);
        }
    }
}
#endif