// SerializableDictionary.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver {

        [SerializeField] private List<TKey> keys = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();

        private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

        public TValue this[TKey key] {
            get => dictionary[key];
            set => dictionary[key] = value;
        }

        public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);
        public void Add(TKey key, TValue value) => dictionary.Add(key, value);
        public IEnumerable<TKey> Keys => dictionary.Keys;

        public void OnBeforeSerialize() {
            keys.Clear();
            values.Clear();
            foreach (var pair in dictionary) {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize() {
            dictionary.Clear();
            for (int i = 0; i < keys.Count; i++) {
                dictionary[keys[i]] = values[i];
            }
        }
    }
}
