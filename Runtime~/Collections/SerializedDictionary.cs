using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GrayWolf.Gaze.Collections
{
    /// <summary>
    /// A dictionary Unity can serialize, backed by two parallel lists and drawn as an editable table in
    /// the inspector.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lookups go through a <see cref="Dictionary{TKey,TValue}"/> rebuilt lazily from the serialized
    /// lists, so reads are ordinary dictionary reads and the cost of a rebuild is paid once after a
    /// mutation or a deserialize rather than per access.
    /// </para>
    /// <para>
    /// Unity's serializer cannot enforce key uniqueness, so a hand-edited asset or a merge can produce
    /// duplicate keys. The first occurrence wins, later ones are dropped from the lookup, and each
    /// offending key is warned about once. <see cref="Count"/> therefore reports distinct keys and can
    /// be smaller than the number of serialized rows.
    /// </para>
    /// <para>
    /// <typeparamref name="TKey"/> and <typeparamref name="TValue"/> must both be types Unity can
    /// serialize for the contents to survive a domain reload.
    /// </para>
    /// </remarks>
    /// <typeparam name="TKey">Key type. Must be serializable by Unity.</typeparam>
    /// <typeparam name="TValue">Value type. Must be serializable by Unity.</typeparam>
    /// <seealso cref="SerializedDictionaryAttribute"/>
    [Serializable]
    public class SerializedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keyData = new();

        [SerializeField]
        private List<TValue> valueData = new();

        private readonly Dictionary<TKey, TValue> _lookup = new();
        private HashSet<TKey> _warnedDuplicateKeys;
        private bool _lookupDirty = true;

        /// <summary>
        /// Creates an empty dictionary.
        /// </summary>
        public SerializedDictionary() { }

        /// <summary>
        /// Creates a dictionary holding a shallow copy of <paramref name="source"/>.
        /// </summary>
        /// <param name="source">
        /// Entries to copy, in enumeration order. A null source yields an empty dictionary rather than
        /// throwing. Reference values are shared with the source, not cloned.
        /// </param>
        public SerializedDictionary(IDictionary<TKey, TValue> source)
        {
            if (source == null)
                return;

            foreach (var pair in source)
            {
                keyData.Add(pair.Key);
                valueData.Add(pair.Value);
            }

            _lookupDirty = true;
        }

        /// <summary>
        /// Number of distinct keys. Duplicate serialized rows are not counted, so this can be smaller
        /// than the number of rows shown in the inspector.
        /// </summary>
        public int Count
        {
            get
            {
                RebuildIfDirty();
                return _lookup.Count;
            }
        }

        /// <summary>Always false.</summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// The distinct keys. The returned collection is a live view of the internal lookup and is
        /// invalidated by the next mutation.
        /// </summary>
        public ICollection<TKey> Keys
        {
            get
            {
                RebuildIfDirty();
                return _lookup.Keys;
            }
        }

        /// <summary>
        /// The values of the distinct keys. The returned collection is a live view of the internal
        /// lookup and is invalidated by the next mutation.
        /// </summary>
        public ICollection<TValue> Values
        {
            get
            {
                RebuildIfDirty();
                return _lookup.Values;
            }
        }

        /// <summary>
        /// Gets or sets the value for <paramref name="key"/>. Setting overwrites an existing entry or
        /// appends a new one.
        /// </summary>
        /// <param name="key">Key to look up. Must not be null when setting.</param>
        /// <exception cref="KeyNotFoundException">The key is not present, when getting.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null, when setting.</exception>
        public TValue this[TKey key]
        {
            get
            {
                RebuildIfDirty();
                return _lookup[key];
            }
            set
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                var index = IndexOfKey(key);
                if (index >= 0)
                {
                    valueData[index] = value;
                }
                else
                {
                    keyData.Add(key);
                    valueData.Add(value);
                }

                _lookupDirty = true;
            }
        }

        /// <summary>
        /// Appends a new entry.
        /// </summary>
        /// <param name="key">Key to add. Must not be null and must not already be present.</param>
        /// <param name="value">Value to associate with the key.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="ArgumentException">An entry with that key already exists.</exception>
        public void Add(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (IndexOfKey(key) >= 0)
                throw new ArgumentException($"An entry with key '{key}' already exists.", nameof(key));

            keyData.Add(key);
            valueData.Add(value);
            _lookupDirty = true;
        }

        /// <summary>
        /// Appends a new entry.
        /// </summary>
        /// <param name="item">Entry to add. Its key must not be null and must not already be present.</param>
        /// <exception cref="ArgumentNullException">The item's key is null.</exception>
        /// <exception cref="ArgumentException">An entry with that key already exists.</exception>
        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        /// <summary>
        /// Removes every serialized row carrying <paramref name="key"/>, including duplicates left
        /// behind by a hand-edited asset.
        /// </summary>
        /// <param name="key">Key to remove. A null key is a no-op rather than an error.</param>
        /// <returns>True if at least one row was removed.</returns>
        public bool Remove(TKey key)
        {
            if (key == null)
                return false;

            var removed = false;

            for (var i = keyData.Count - 1; i >= 0; i--)
            {
                if (!KeyEquals(keyData[i], key))
                    continue;

                keyData.RemoveAt(i);

                if (i < valueData.Count)
                    valueData.RemoveAt(i);

                removed = true;
            }

            if (removed)
                _lookupDirty = true;

            return removed;
        }

        /// <summary>
        /// Removes the entry only when both its key and its value match.
        /// </summary>
        /// <param name="item">Entry to remove.</param>
        /// <returns>True if the entry was present with that exact value and was removed.</returns>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!TryGetValue(item.Key, out var existing))
                return false;

            if (!EqualityComparer<TValue>.Default.Equals(existing, item.Value))
                return false;

            return Remove(item.Key);
        }

        /// <summary>
        /// Removes every entry, including duplicate rows.
        /// </summary>
        public void Clear()
        {
            keyData.Clear();
            valueData.Clear();
            _lookup.Clear();
            _warnedDuplicateKeys?.Clear();
            _lookupDirty = false;
        }

        /// <summary>
        /// Tests whether the key is present.
        /// </summary>
        /// <param name="key">Key to look for. A null key returns false rather than throwing.</param>
        /// <returns>True if the key is present.</returns>
        public bool ContainsKey(TKey key)
        {
            if (key == null)
                return false;

            RebuildIfDirty();
            return _lookup.ContainsKey(key);
        }

        /// <summary>
        /// Tests whether the entry is present with that exact value.
        /// </summary>
        /// <param name="item">Entry to look for.</param>
        /// <returns>True if the key is present and its value matches.</returns>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return TryGetValue(item.Key, out var existing)
                   && EqualityComparer<TValue>.Default.Equals(existing, item.Value);
        }

        /// <summary>
        /// Looks up a key without throwing when it is absent.
        /// </summary>
        /// <param name="key">Key to look for. A null key returns false rather than throwing.</param>
        /// <param name="value">The value if found, otherwise the default for <typeparamref name="TValue"/>.</param>
        /// <returns>True if the key was found.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
            {
                value = default;
                return false;
            }

            RebuildIfDirty();
            return _lookup.TryGetValue(key, out value);
        }

        /// <summary>
        /// Copies the distinct entries into <paramref name="array"/>.
        /// </summary>
        /// <param name="array">Destination array.</param>
        /// <param name="arrayIndex">Index in the destination to start writing at.</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            RebuildIfDirty();
            ((ICollection<KeyValuePair<TKey, TValue>>)_lookup).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Enumerates the distinct entries. Duplicate serialized rows are not yielded.
        /// </summary>
        /// <returns>An enumerator over the entries.</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            RebuildIfDirty();
            return _lookup.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Marks the lookup stale so the next read rebuilds it from the serialized lists.
        /// </summary>
        /// <remarks>
        /// Only needed after the backing lists have been changed behind this instance's back - which
        /// means through <c>SerializedProperty</c> or reflection, as the inspector drawer does. Every
        /// mutation through this class, and every deserialize, already marks the lookup stale.
        /// </remarks>
        public void Invalidate() => _lookupDirty = true;

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize() => _lookupDirty = true;

        private void RebuildIfDirty()
        {
            if (!_lookupDirty)
                return;

            _lookup.Clear();

            var count = Math.Min(keyData.Count, valueData.Count);
            var duplicates = 0;

            for (var i = 0; i < count; i++)
            {
                var key = keyData[i];

                if (key == null)
                    continue;

                if (_lookup.TryAdd(key, valueData[i]))
                    continue;

                duplicates++;
                _warnedDuplicateKeys ??= new HashSet<TKey>();

                // A rebuild follows every mutation, so warn once per offending key rather than per rebuild.
                if (_warnedDuplicateKeys.Add(key))
                    Debug.LogWarning($"[Gaze] {nameof(SerializedDictionary<TKey, TValue>)} contains " +
                                     $"duplicate key '{key}'. First occurrence kept.");
            }

            if (duplicates == 0)
                _warnedDuplicateKeys?.Clear();

            _lookupDirty = false;
        }

        private int IndexOfKey(TKey key)
        {
            for (var i = 0; i < keyData.Count; i++)
            {
                if (KeyEquals(keyData[i], key))
                    return i;
            }

            return -1;
        }

        private static bool KeyEquals(TKey a, TKey b)
        {
            return a != null && EqualityComparer<TKey>.Default.Equals(a, b);
        }
    }
}