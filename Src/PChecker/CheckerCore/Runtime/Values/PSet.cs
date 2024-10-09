using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PChecker.Runtime.Values.Exceptions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace PChecker.Runtime.Values
{
    public sealed class PSet : IPMutableValue, IReadOnlyList<IPValue>, ICollection<IPValue>
    {
        private readonly ISet<IPValue> set = new HashSet<IPValue>();

        private int hashCode;
        private bool isDirty;
        private bool isFrozen;

        public PSet()
        {
            hashCode = ComputeHashCode();
        }

        public PSet(ISet<IPValue> set)
        {
            this.set = set;
            hashCode = ComputeHashCode();
        }

        private bool IsDirty
        {
            get => isDirty;
            set
            {
                if (value && isFrozen) throw new PFrozenMutationException();

                isDirty = value;
            }
        }

        public override int GetHashCode()
        {
            if (!IsDirty)
            {
                return hashCode;
            }

            hashCode = ComputeHashCode();
            IsDirty = false;

            return hashCode;
        }

        public IEnumerator<IPValue> GetEnumerator()
        {
            return set.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Add(IPValue item)
        {
            MutabilityHelper.EnsureFrozen(item);
            IsDirty = true;
            return set.Add(item.Clone());
        }

        public void Clear()
        {
            set.Clear();
            IsDirty = true;
        }

        public bool Contains(IPValue item)
        {
            return set.Contains(item);
        }

        public bool Remove(IPValue item)
        {
            var removed = set.Remove(item);
            IsDirty = true;
            return removed;
        }

        public int Count => set.Count;
        public bool IsReadOnly => false;

        public bool Equals(IPValue other)
        {
            return other is PSet otherSet
                   && set.SetEquals(otherSet.set);
        }

        public IPValue Clone()
        {
            var clone = new PSet(new HashSet<IPValue>());
            foreach (var item in set) clone.Add(item.Clone());
            return clone;
        }

        public void Freeze()
        {
            foreach (var item in set) MutabilityHelper.EnsureFrozen(item);
            isFrozen = true;
        }

        private int ComputeHashCode()
        {
            return HashHelper.ComputeHash(set);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("(");
            var sep = "";
            foreach (var value in set)
            {
                sb.Append(sep);
                sb.Append("<");
                sb.Append(value);
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
            foreach (var value in set)
            {
                var v = value == null ? "null" : value.ToEscapedString();
                sb.Append(sep);
                sb.Append("<");
                sb.Append(v);
                sb.Append(">");
                sep = ", ";
            }

            sb.Append(")");
            return sb.ToString();
        }

        public object ToDict()
        {
            return set.Select(value => value == null ? null : value.ToDict()).ToList();
        }

        public IPValue this[int index]
        {
            get => set.ElementAt(index);
            set => throw new Exception("Setting set elements using indexing is not allowed!");
        }
        public void UnionWith(IEnumerable<IPValue> other)
        {
            set.UnionWith(other.Select(i => i.Clone()));
        }

        public void IntersectWith(IEnumerable<IPValue> other)
        {
            set.IntersectWith(other);
        }

        public void ExceptWith(IEnumerable<IPValue> other)
        {
            set.ExceptWith(other);
        }

        public void SymmetricExceptWith(IEnumerable<IPValue> other)
        {
            set.SymmetricExceptWith(other);
        }

        public bool IsSubsetOf(IEnumerable<IPValue> other)
        {
            return set.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<IPValue> other)
        {
            return set.IsSupersetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<IPValue> other)
        {
            return set.IsProperSupersetOf(other);
        }

        public bool IsProperSubsetOf(IEnumerable<IPValue> other)
        {
            return set.IsProperSubsetOf(other);
        }

        public bool Overlaps(IEnumerable<IPValue> other)
        {
            return set.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<IPValue> other)
        {
            return set.SetEquals(other);
        }

        void ICollection<IPValue>.Add(IPValue item)
        {
            set.Add(item);
        }

        public void CopyTo(IPValue[] array, int arrayIndex)
        {
            set.CopyTo(array, arrayIndex);
        }
    }
}