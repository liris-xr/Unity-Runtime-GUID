using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityRuntimeGuid
{
    [Serializable]
    public class GuidRegistry<T> : ISerializationCallbackReceiver where T : GuidRegistryEntry
    {
        private Dictionary<Object, T>  _objectToGuid = new();
        private Dictionary<string, T> _guidToEntry = new();
        
        [SerializeField] private List<T> entries = new();
        
        public void OnBeforeSerialize()
        {
        }
        
        public void OnAfterDeserialize()
        {
            _objectToGuid.Clear();
            _guidToEntry.Clear();
            
            foreach (var entry in entries)
            {
                _objectToGuid[entry.@object] = entry;
                _guidToEntry[entry.guid] = entry;
            }
        }
        
        public GuidRegistry<T> Copy()
        {
            var copy = new GuidRegistry<T>
            {
                _objectToGuid = new Dictionary<Object, T>(_objectToGuid),
                _guidToEntry = new Dictionary<string, T>(_guidToEntry),
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
            _objectToGuid.Clear();
            _guidToEntry.Clear();
            entries.Clear();
        }

        public bool TryAdd(T guidEntry)
        {
            if (guidEntry == null || guidEntry.@object == null)
                return false;

            var added = _objectToGuid.TryAdd(guidEntry.@object, guidEntry);

            if (added)
            {
                _guidToEntry[guidEntry.guid] = guidEntry;
                entries.Add(guidEntry);
            }

            return added;
        }

        public bool TryGetEntry(Object obj, out T entry)
        {
            if (obj != null)
                return _objectToGuid.TryGetValue(obj, out entry);
            entry = null;
            return false;
        }

        public bool TryGetEntry(string guid, out T entry)
        {
            return _guidToEntry.TryGetValue(guid, out entry);
        }

        public T GetEntryByGuid(string guid)
        {
            return _guidToEntry[guid];
        }

        public bool Remove(Object obj)
        {
            if (!_objectToGuid.TryGetValue(obj, out var entry))
                return false;
            _guidToEntry.Remove(entry.guid);
            _objectToGuid.Remove(obj);
            entries.Remove(entry);
            return true;
        }

        public IReadOnlyList<T> GetAllEntries()
        {
            return entries;
        }
    }
}