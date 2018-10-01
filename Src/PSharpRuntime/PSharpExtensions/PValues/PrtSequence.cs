using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrtSharp.PValues
{
    public sealed class PrtSequence<T> : IPrtValue, IReadOnlyList<T>
        where T : class, IPrtValue
    {
        private readonly List<T> values = new List<T>();
        private bool isDirty;
        private int hashCode;

        public PrtSequence()
        {
            this.hashCode = ComputeHashCode();
        }

        public PrtSequence(IEnumerable<T> values)
        {
            this.values = values.ToList();
            this.hashCode = ComputeHashCode();
        }

        private int ComputeHashCode()
        {
            return HashHelper.ComputeHash(values);
        }

        public IPrtValue Clone()
        {
            return new PrtSequence<T>(values.Select(item => item.Clone()).Cast<T>());
        }

        public void Add(T item)
        {
            values.Add(item);
            isDirty = true;
        }

        public void Insert(int index, T item)
        {
            values.Insert(index, item);
            isDirty = true;
        }

        public void RemoveAt(int index)
        {
            values.RemoveAt(index);
            isDirty = true;
        }

        private bool Equals(PrtSequence<T> other)
        {
            return values.SequenceEqual(other.values);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((PrtSequence<T>) obj);
        }

        public override int GetHashCode()
        {
            if (isDirty)
            {
                this.hashCode = ComputeHashCode();
            }

            return this.hashCode;
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

        public T this[int index] => values[index];

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