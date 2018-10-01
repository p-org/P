using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PrtSharp.Values
{
    public sealed class PrtMap<TKey, TValue> : IPrtMutableValue, IDictionary<TKey, TValue>
        where TKey : class, IPrtValue
        where TValue : class, IPrtValue
    {
        private readonly Dictionary<TKey, TValue> map = new Dictionary<TKey, TValue>();

        private int hashCode;
        private bool isDirty;
        private bool isFrozen;

        public PrtMap()
        {
            hashCode = ComputeHashCode();
        }

        public PrtMap(Dictionary<TKey, TValue> map)
        {
            this.map = map;
            hashCode = ComputeHashCode();
        }

        private bool IsDirty
        {
            get => isDirty;
            set
            {
                if (value && isFrozen)
                {
                    throw new PFrozenMutationException();
                }

                isDirty = value;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return map.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            MutabilityHelper.EnsureFrozen(item.Key);
            map.Add(item.Key, item.Value);
            IsDirty = true;
        }

        public void Clear()
        {
            map.Clear();
            IsDirty = true;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return map.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<TKey, TValue> kv in map)
            {
                array[arrayIndex++] = kv;
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            bool removed = map.Remove(item.Key);
            IsDirty = true;
            return removed;
        }

        public int Count => map.Count;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            MutabilityHelper.EnsureFrozen(key);
            map.Add(key, value);
            IsDirty = true;
        }

        public bool ContainsKey(TKey key)
        {
            return map.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            bool removed = map.Remove(key);
            IsDirty = true;
            return removed;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return map.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get => map[key];
            set
            {
                MutabilityHelper.EnsureFrozen(key);
                map[key] = value;
                IsDirty = true;
            }
        }

        public ICollection<TKey> Keys => map.Keys;
        public ICollection<TValue> Values => map.Values;

        public bool Equals(IPrtValue other)
        {
            return other is PrtMap<TKey, TValue> otherMap
                   && !map.Keys.Except(otherMap.map.Keys).Any()
                   && !otherMap.map.Keys.Except(map.Keys).Any()
                   && map.All(kv => Equals(otherMap.map[kv.Key], kv.Value));
        }

        public IPrtValue Clone()
        {
            return new PrtMap<TKey, TValue>(map.ToDictionary(
                kv => (TKey) kv.Key.Clone(),
                kv => (TValue) kv.Value.Clone()));
        }

        public void Freeze()
        {
            foreach (var key in map.Keys)
            {
                MutabilityHelper.EnsureFrozen(key);
            }
            isFrozen = true;
        }

        public override int GetHashCode()
        {
            if (IsDirty)
            {
                hashCode = ComputeHashCode();
                IsDirty = false;
            }

            return hashCode;
        }

        private int ComputeHashCode()
        {
            return HashHelper.ComputeHash(map.Keys);
        }
    }
}