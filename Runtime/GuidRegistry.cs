using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityRuntimeGuid
{
    [Serializable]
    public class GuidRegistry<T> where T : GuidRegistryEntry
    {
        private bool _isCacheInitialized;
        private Dictionary<int, T>  _objectIdToGuidCache = new();
        private Dictionary<string, T> _guidToEntryCache = new();
        
        [SerializeField] private List<T> entries = new();
        
        public void InitializeCache()
        {
            if (_isCacheInitialized)
                return;
            
            _isCacheInitialized = true;
            
            _objectIdToGuidCache.Clear();
            _guidToEntryCache.Clear();
            
            foreach (var entry in entries)
            {
                _objectIdToGuidCache[entry.@object.GetInstanceID()] = entry;
                _guidToEntryCache[entry.guid] = entry;
            }
        }
        
        public GuidRegistry<T> Copy()
        {
            var copy = new GuidRegistry<T>
            {
                _isCacheInitialized = _isCacheInitialized,
                _objectIdToGuidCache = new Dictionary<int, T>(_objectIdToGuidCache),
                _guidToEntryCache = new Dictionary<string, T>(_guidToEntryCache),
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
            _objectIdToGuidCache.Clear();
            _guidToEntryCache.Clear();
            entries.Clear();
        }

        public bool TryAdd(T guidEntry)
        {
            if (guidEntry == null || guidEntry.@object == null)
                return false;
            
            if(!_isCacheInitialized)
                InitializeCache();
            
            var added = _objectIdToGuidCache.TryAdd(guidEntry.@object.GetInstanceID(), guidEntry);

            if (added)
            {
                _guidToEntryCache[guidEntry.guid] = guidEntry;
                entries.Add(guidEntry);
            }

            return added;
        }

        public bool TryGetEntry(Object obj, out T entry)
        {
            if(!_isCacheInitialized)
                InitializeCache();
            
            if (obj != null)
                return _objectIdToGuidCache.TryGetValue(obj.GetInstanceID(), out entry);
            entry = null;
            return false;
        }

        public bool TryGetEntry(string guid, out T entry)
        {
            if(!_isCacheInitialized)
                InitializeCache();
            
            return _guidToEntryCache.TryGetValue(guid, out entry);
        }

        public T GetEntryByGuid(string guid)
        { 
            if(!_isCacheInitialized)
                InitializeCache();
            
            return _guidToEntryCache[guid];
        }

        public bool Remove(Object obj)
        {
            if(!_isCacheInitialized)
                InitializeCache();
            
            if (!_objectIdToGuidCache.TryGetValue(obj.GetInstanceID(), out var entry))
                return false;
            _guidToEntryCache.Remove(entry.guid);
            _objectIdToGuidCache.Remove(obj.GetInstanceID());
            entries.Remove(entry);
            return true;
        }

        public IReadOnlyList<T> GetAllEntries()
        {
            return entries;
        }
    }
}