﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityRuntimeGuid
{
    [Serializable]
    public class AssetsGuidRegistry : ScriptableObject
    {
        private const string BuiltinResourcesPath = "Resources/unity_builtin_extra";
        private const string BuiltinExtraResourcesPath = "Library/unity default resources";
        
        private static string _assetsGuidRegistryName = "AssetsGuidRegistry";
        private static string _assetsGuidRegistryPath = $"Resources/{_assetsGuidRegistryName}.asset";

        [SerializeField] private GuidRegistry<AssetGuidRegistryEntry> registry = new();

        private static AssetsGuidRegistry _instance;

        public static AssetsGuidRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GetRegistry();
                }

                return _instance;
            }
        }

        private void OnEnable()
        {
            _instance = this;
        }

#if UNITY_EDITOR
        private static string GetRegistryAssetPath()
        {
            var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, _assetsGuidRegistryPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            var configUri = new Uri(fullPath);
            var projectUri = new Uri(Application.dataPath);
            var relativeUri = projectUri.MakeRelativeUri(configUri);
            return relativeUri.ToString();
        }

        public static void CommitRegistry(AssetsGuidRegistry registry)
        {
            var registryAssetPath = GetRegistryAssetPath();
            if (AssetDatabase.GetAssetPath(registry) != registryAssetPath)
            {
                Debug.LogWarningFormat("The asset path of AssetsGuidRegistry is wrong. Expect {0}, get {1}",
                    registryAssetPath, AssetDatabase.GetAssetPath(registry));
            }

            EditorUtility.SetDirty(registry);
        }

        internal void AddToPreloadedAssets()
        {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets().ToList();
            if (preloadedAssets.Contains(this)) return;
            preloadedAssets.Add(this);
            PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
        }
#endif

        public static AssetsGuidRegistry GetRegistry()
        {
            AssetsGuidRegistry registry = null;
#if UNITY_EDITOR
            var registryAssetPath = GetRegistryAssetPath();
            try
            {
                registry =
                    AssetDatabase.LoadAssetAtPath(registryAssetPath, typeof(AssetsGuidRegistry)) as
                        AssetsGuidRegistry;
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat("Unable to load AssetsGuidRegistry from {0}, error {1}", registryAssetPath,
                    e.Message);
            }

            if (registry == null)
            {
                registry = CreateInstance<AssetsGuidRegistry>();
                AssetDatabase.CreateAsset(registry, registryAssetPath);
                registry.AddToPreloadedAssets();
            }
#else
            registry = Resources.Load<AssetsGuidRegistry>(_assetsGuidRegistryName);
            if (registry == null)
            {
                Debug.LogWarning("Failed to load assets GUID registry. Using default registry instead.");
                registry = CreateInstance<AssetsGuidRegistry>();
            }
#endif
            return registry;
        }

        public bool TryAdd(AssetGuidRegistryEntry guidEntry)
        {
            return registry.TryAdd(guidEntry);
        }

        public void Clear()
        {
            registry.Clear();
        }

        public Dictionary<Object, AssetGuidRegistryEntry> Copy()
        {
            return registry.Copy();
        }

        public bool TryGetValue(Object obj, out AssetGuidRegistryEntry entry)
        {
            return registry.TryGetValue(obj, out entry);
        }

        public bool Remove(Object obj)
        {
            return registry.Remove(obj);
        }
        
        public AssetGuidRegistryEntry GetOrCreate(Object obj)
        {
            if (registry.TryGetValue(obj, out var assetGuidRegistryEntry)) return assetGuidRegistryEntry;
            assetGuidRegistryEntry = CreateNewEntry(obj);
            registry.TryAdd(assetGuidRegistryEntry);
            return assetGuidRegistryEntry;
        }

        public static AssetGuidRegistryEntry CreateNewEntry(Object obj)
        {
            return new AssetGuidRegistryEntry
            {
                @object = obj,
#if UNITY_EDITOR
                guid = GetAssetGuid(obj),
                assetBundlePath = GetFullAssetBundlePath(obj)
#else
                guid = Guid.NewGuid().ToString(),
                assetBundlePath = ""
#endif
            };
        }

#if UNITY_EDITOR
        private static string GetAssetGuid(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            var guid = AssetDatabase.GUIDFromAssetPath(path);
            var isBuiltin = path.Equals(BuiltinResourcesPath) || path.Equals(BuiltinExtraResourcesPath);
            return guid.Empty() || isBuiltin ? Guid.NewGuid().ToString() : guid.ToString();
        }

        private static string GetFullAssetBundlePath(Object asset)
        {
            if (!AssetDatabase.Contains(asset))
                return "";

            var path = AssetDatabase.GetAssetPath(asset);

            var prefix = path.Equals(BuiltinResourcesPath) || path.Equals(BuiltinExtraResourcesPath)
                ? "Builtin"
                : "Custom";

            var assetType = asset.GetType().FullName + ", " + asset.GetType().Assembly.GetName().Name;

            return $"{prefix}:{assetType}:{path}:{asset.name}";
        }
#endif
    }
}