using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PChecker.PRuntime.Exceptions;

namespace PChecker.PRuntime.Values
{
    public sealed class PMap : IPMutableValue, IDictionary<IPValue, IPValue>
    {
        private readonly IDictionary<IPValue, IPValue> map = new Dictionary<IPValue, IPValue>();

        private int hashCode;
        private bool isDirty;
        private bool isFrozen;

        public PMap()
        {
            hashCode = ComputeHashCode();
        }

        public PMap(IDictionary<IPValue, IPValue> map)
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

        public IEnumerator<KeyValuePair<IPValue, IPValue>> GetEnumerator()
        {
            return map.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<IPValue, IPValue> item)
        {
            MutabilityHelper.EnsureFrozen(item.Key);
            map.Add(item.Key?.Clone(), item.Value?.Clone());
            IsDirty = true;
        }

        public void Clear()
        {
            map.Clear();
            IsDirty = true;
        }

        public bool Contains(KeyValuePair<IPValue, IPValue> item)
        {
            return map.Contains(item);
        }

        public void CopyTo(KeyValuePair<IPValue, IPValue>[] array, int arrayIndex)
        {
            foreach (var kv in map)
            {
                array[arrayIndex++] = kv;
            }
        }

        public bool Remove(KeyValuePair<IPValue, IPValue> item)
        {
            var removed = map.Remove(item.Key);
            IsDirty = true;
            return removed;
        }

        public int Count => map.Count;
        public bool IsReadOnly => false;

        public void Add(IPValue key, IPValue value)
        {
            MutabilityHelper.EnsureFrozen(key);
            map.Add(key?.Clone(), value?.Clone());
            IsDirty = true;
        }

        public bool ContainsKey(IPValue key)
        {
            return map.ContainsKey(key);
        }

        public bool Remove(IPValue key)
        {
            var removed = map.Remove(key);
            IsDirty = true;
            return removed;
        }

        public bool TryGetValue(IPValue key, out IPValue value)
        {
            return map.TryGetValue(key, out value);
        }

        public IPValue this[IPValue key]
        {
            get => map[key];
            set
            {
                MutabilityHelper.EnsureFrozen(key);
                map[key] = value;
                IsDirty = true;
            }
        }

        public ICollection<IPValue> Keys => map.Keys;
        public ICollection<IPValue> Values => map.Values;

        public bool Equals(IPValue other)
        {
            return other is PMap otherMap
                   && !map.Keys.Except(otherMap.map.Keys).Any()
                   && !otherMap.map.Keys.Except(map.Keys).Any()
                   && map.All(kv => PValues.SafeEquals(otherMap.map[kv.Key], kv.Value));
        }

        public IPValue Clone()
        {
            return new PMap(map.ToDictionary(
                kv => kv.Key?.Clone(),
                kv => kv.Value?.Clone()));
        }

        public void Freeze()
        {
            foreach (var key in map.Keys)
            {
                MutabilityHelper.EnsureFrozen(key);
            }

            isFrozen = true;
        }

        public PSeq CloneKeys()
        {
            return new PSeq(map.Keys.Select(v => v.Clone()));
        }

        public PSeq CloneValues()
        {
            return new PSeq(map.Values.Select(v => v.Clone()));
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

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("(");
            var sep = "";
            foreach (var value in map)
            {
                sb.Append(sep);
                sb.Append("<");
                sb.Append(value.Key);
                sb.Append("->");
                sb.Append(value.Value);
                sb.Append(">");
                sep = ", ";
            }

            sb.Append(")");
            return sb.ToString();
        }

        public string ToEscapedString()
        {
            var sb = new StringBuilder();
            sb.Append("(");
            var sep = "";
            foreach (var value in map)
            {
                string k = value.Key == null ? "null" : value.Key.ToEscapedString();
                string v = value.Value == null ? "null" : value.Value.ToEscapedString();
                sb.Append(sep);
                sb.Append("<");
                sb.Append(k);
                sb.Append("->");
                sb.Append(v);
                sb.Append(">");
                sep = ", ";
            }

            sb.Append(")");
            return sb.ToString();
        }

        public object ToDict()
        {
            var mapDict = new Dictionary<string, object>();

            foreach (var value in map)
            {
                var mapKey = value.Key.ToEscapedString();
                var mapValue = value.Value.ToDict();

                mapDict.Add(mapKey, mapValue);
            }

            return mapDict;
        }
    }
}