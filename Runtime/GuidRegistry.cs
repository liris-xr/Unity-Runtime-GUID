using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityRuntimeGuid
{
    [Serializable]
    public class GuidRegistry<T> where T : GuidRegistryEntry
    {
        [SerializeField] private SerializableDictionary<Object, T> objectToGuid = new();
        [SerializeField] private SerializableDictionary<string, T> guidToEntry = new();
        [SerializeField] private List<T> entries = new();

        public GuidRegistry<T> Copy()
        {
            var copy = new GuidRegistry<T>
            {
                objectToGuid = new SerializableDictionary<Object, T>(objectToGuid),
                guidToEntry = new SerializableDictionary<string, T>(guidToEntry),
                entries = new List<T>(entries)
            };
            return copy;
        }

        public T GetOrCreateEntry(Object obj, Func<Object, T> createEntryFunc)
        {
            if (TryGetEntry(obj, out var registryEntry)) return registryEntry;
            registryEntry = createEntryFunc(obj);
            TryAdd(registryEntry);
            return registryEntry;
        }

        public void Clear()
        {
            objectToGuid.Clear();
            guidToEntry.Clear();
            entries.Clear();
        }

        public bool TryAdd(T guidEntry)
        {
            if (guidEntry == null || guidEntry.@object == null)
                return false;

            var added = objectToGuid.TryAdd(guidEntry.@object, guidEntry);

            if (added)
            {
                guidToEntry[guidEntry.guid] = guidEntry;
                entries.Add(guidEntry);
            }

            return added;
        }

        public bool TryGetEntry(Object obj, out T entry)
        {
            if (obj != null) return objectToGuid.TryGetValue(obj, out entry);
            entry = null;
            return false;
        }

        public bool TryGetEntry(string guid, out T entry)
        {
            return guidToEntry.TryGetValue(guid, out entry);
        }

        public T GetEntryByGuid(string guid)
        {
            return guidToEntry[guid];
        }

        public bool Remove(Object obj)
        {
            if (!objectToGuid.TryGetValue(obj, out var entry)) return false;
            guidToEntry.Remove(entry.guid);
            objectToGuid.Remove(obj);
            entries.Remove(entry);
            return true;
        }

        public IReadOnlyList<T> GetAllEntries()
        {
            return entries;
        }
    }
}