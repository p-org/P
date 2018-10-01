using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrtSharp.Values
{
    public sealed class PrtSeq<T> : IPrtMutableValue, IReadOnlyList<T>
        where T : class, IPrtValue
    {
        private readonly List<T> values = new List<T>();

        private int hashCode;
        private bool isDirty;
        private bool isFrozen;

        public PrtSeq()
        {
            hashCode = ComputeHashCode();
        }

        public PrtSeq(IEnumerable<T> values)
        {
            this.values = values.ToList();
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

        public void Freeze()
        {
            foreach (T value in values)
            {
                MutabilityHelper.EnsureFrozen(value);
            }

            isFrozen = true;
        }

        public IPrtValue Clone()
        {
            return new PrtSeq<T>(values.Select(item => item.Clone()).Cast<T>());
        }

        public bool Equals(IPrtValue other)
        {
            return other is PrtSeq<T> otherValue && Equals(otherValue);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => values.Count;

        public T this[int index]
        {
            get => values[index];
            set
            {
                if (isFrozen)
                {
                    throw new PFrozenMutationException();
                }

                values[index] = value;
            }
        }

        private int ComputeHashCode()
        {
            return HashHelper.ComputeHash(values);
        }

        public void Add(T item)
        {
            IsDirty = true;
            values.Add(item);
        }

        public void Insert(int index, T item)
        {
            IsDirty = true;
            values.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            IsDirty = true;
            values.RemoveAt(index);
        }

        private bool Equals(PrtSeq<T> other)
        {
            return other != null && values.SequenceEqual(other.values);
        }

        public override bool Equals(object obj)
        {
            return !ReferenceEquals(null, obj) &&
                   (ReferenceEquals(this, obj) ||
                    obj.GetType() == GetType() &&
                    Equals((PrtSeq<T>) obj));
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

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("(");
            string sep = "";
            foreach (T value in values)
            {
                sb.Append(sep);
                sb.Append(value);
                sep = ", ";
            }

            sb.Append(")");
            return sb.ToString();
        }
    }
}