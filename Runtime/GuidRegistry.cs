using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityRuntimeGuid
{
    [Serializable]
    public class GuidRegistry<T> : ISerializationCallbackReceiver where T : GuidRegistryEntry
    {
        [SerializeField]
        [SerializeReference]
        private List<T> entries = new();
        
        [NonSerialized]
        private Dictionary<Object, T> _objectToGuid = new();

        [NonSerialized] private Dictionary<string, T> _guidToEntry = new();

        public int Count => _objectToGuid.Count;

        public T GetOrCreateEntry(Object obj, System.Func<Object, T> createEntryFunc)
        {
            if (TryGetValue(obj, out var registryEntry)) return registryEntry;
            registryEntry = createEntryFunc(obj);
            TryAdd(registryEntry);
            return registryEntry;
        }
        
        public void Clear()
        {
            _objectToGuid.Clear();
        }

        public bool TryAdd(T guidEntry)
        {
            if (guidEntry == null || guidEntry.@object == null)
                return false;

            var added = _objectToGuid.TryAdd(guidEntry.@object, guidEntry);
            
            if(added)
                _guidToEntry[guidEntry.guid] = guidEntry;
            
            return added;
        }

        public Dictionary<Object, T> Copy()
        {
            return new Dictionary<Object, T>(_objectToGuid);
        }

        public bool TryGetValue(Object obj, out T entry)
        {
            if (obj != null) return _objectToGuid.TryGetValue(obj, out entry);
            entry = null;
            return false;
        }

        public bool TryGetEntryByGuid(string guid, out T entry)
        {
            return _guidToEntry.TryGetValue(guid, out entry);
        }
        
        public T GetEntryByGuid(string guid)
        {
            return _guidToEntry[guid];
        }

        public bool Remove(Object obj)
        {
            if (!_objectToGuid.TryGetValue(obj, out var entry)) return false;
            _guidToEntry.Remove(entry.guid);
            _objectToGuid.Remove(obj);
            return true;
        }
        
        public void OnBeforeSerialize()
        {
            entries ??= new List<T>();
            entries.Clear();
            foreach (var (obj, entry) in _objectToGuid)
            {
                if(obj != null)
                    entries.Add(entry);
            }
        }

        public void OnAfterDeserialize()
        {
            _objectToGuid ??= new Dictionary<Object, T>();
            _objectToGuid.Clear();
            foreach (var entry in entries.Where(entry => entry != null && entry.@object != null))
            {
                if (entry.@object == null) continue;
                
                try
                {
                    _objectToGuid.Add(entry.@object, entry);
                    _guidToEntry[entry.guid] = entry;
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        public List<T> GetAllEntries()
        {
            return entries;
        }
    }
}