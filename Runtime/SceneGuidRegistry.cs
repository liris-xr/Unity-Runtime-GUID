using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityRuntimeGuid
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class SceneGuidRegistry : MonoBehaviour
    {
        private const string GameObjectName = "SceneGuidRegistry";

        private static readonly Dictionary<Scene, SceneGuidRegistry> SceneGuidRegistries = new();

        [SerializeField] private GuidRegistry<SceneGuidRegistryEntry> registry = new();

        public void Awake()
        {
            var sceneGuidRegistries =
                FindObjectsByType<SceneGuidRegistry>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (sceneGuidRegistries.Length <= 1) return;
            Debug.LogWarning("Only one scene GUID registry is allowed per scene. Deleting.");
            DestroyImmediate(this);
        }

        public static SceneGuidRegistry GetOrCreate(Scene scene)
        {
            if (!scene.IsValid())
                throw new Exception("Scene is invalid.");
            if (!scene.isLoaded)
                throw new Exception("Scene is not loaded.");

            SceneGuidRegistries.TryGetValue(scene, out var sceneObjectsGuidRegistry);

            if (sceneObjectsGuidRegistry == null)
            {
                sceneObjectsGuidRegistry =
                    FindObjectsByType<SceneGuidRegistry>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .FirstOrDefault(r => r.gameObject.scene == scene);
            }

            if (sceneObjectsGuidRegistry == null)
            {
                var go = new GameObject(GameObjectName);
                sceneObjectsGuidRegistry = go.AddComponent<SceneGuidRegistry>();
            }

            SceneGuidRegistries[scene] = sceneObjectsGuidRegistry;

            return sceneObjectsGuidRegistry;
        }

        public bool TryAdd(SceneGuidRegistryEntry guidEntry)
        {
            return registry.TryAdd(guidEntry);
        }

        public void Clear()
        {
            registry.Clear();
        }

        public Dictionary<Object, SceneGuidRegistryEntry> Copy()
        {
            return registry.Copy();
        }

        public bool TryGetValue(Object obj, out SceneGuidRegistryEntry entry)
        {
            return registry.TryGetValue(obj, out entry);
        }

        public bool Remove(Object obj)
        {
            return registry.Remove(obj);
        }

        public SceneGuidRegistryEntry GetOrCreateEntry(Object obj)
        {
            return registry.GetOrCreateEntry(obj, o =>
                new SceneGuidRegistryEntry
                {
                    @object = obj,
                    guid = Guid.NewGuid().ToString()
                });
        }
    }
}