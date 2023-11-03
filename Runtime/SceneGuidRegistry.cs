using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityRuntimeGuid
{
    public class SceneGuidRegistry : MonoBehaviour
    {
        private const string GameObjectName = "SceneGuidRegistry";
        
        private static readonly Dictionary<Scene, SceneGuidRegistry> SceneGuidRegistries = new();

        [SerializeField] private GuidRegistry<SceneGuidRegistryEntry> registry = new();
        
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

        public static SceneGuidRegistryEntry CreateNewEntry(Object obj)
        {
            return new SceneGuidRegistryEntry
            {
                @object = obj,
                guid = Guid.NewGuid().ToString()
            };
        }
        
        public static SceneGuidRegistry GetOrCreateInActiveScene()
        {
            return GetOrCreateInScene(SceneManager.GetActiveScene());
        }
        
        public static SceneGuidRegistry GetOrCreateInScene(Scene scene)
        {
            if (!scene.IsValid())
                throw new Exception("Scene is invalid.");
            if (!scene.isLoaded)
                throw new Exception("Scene is not loaded.");

            if (SceneGuidRegistries.TryGetValue(scene, out var sceneObjectsGuidRegistry)) return sceneObjectsGuidRegistry;
            
            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                sceneObjectsGuidRegistry = rootGameObject.GetComponentInChildren<SceneGuidRegistry>();

                if (sceneObjectsGuidRegistry != null)
                    break;
            }

            if (sceneObjectsGuidRegistry == null)
            {
                var go = new GameObject(GameObjectName, typeof(SceneGuidRegistry));
                sceneObjectsGuidRegistry = go.GetComponent<SceneGuidRegistry>();
            }

            SceneGuidRegistries[scene] = sceneObjectsGuidRegistry;

            return sceneObjectsGuidRegistry;
        }

        public SceneGuidRegistryEntry GetOrCreate(Object obj)
        {
            if (registry.TryGetValue(obj, out var sceneObjectGuidRegistryEntry)) return sceneObjectGuidRegistryEntry;
            sceneObjectGuidRegistryEntry = CreateNewEntry(obj);
            registry.TryAdd(sceneObjectGuidRegistryEntry);
            return sceneObjectGuidRegistryEntry;
        }
    }
}