using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PChecker.Runtime.Values.Exceptions;

namespace PChecker.Runtime.Values
{
    public sealed class PSeq : IPMutableValue, IReadOnlyList<IPValue>
    {
        private readonly List<IPValue> values = new List<IPValue>();

        private int hashCode;
        private bool isDirty;
        private bool isFrozen;

        public PSeq()
        {
            hashCode = ComputeHashCode();
        }

        public PSeq(IEnumerable<IPValue> values)
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
            foreach (var value in values)
            {
                MutabilityHelper.EnsureFrozen(value);
            }

            isFrozen = true;
        }

        public IPValue Clone()
        {
            return new PSeq(values.Select(item => item?.Clone()));
        }

        public bool Equals(IPValue other)
        {
            return other is PSeq otherValue && Equals(otherValue);
        }

        public IEnumerator<IPValue> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => values.Count;

        public IPValue this[int index]
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

        public void Add(IPValue item)
        {
            IsDirty = true;
            values.Add(item?.Clone());
        }

        public void Insert(int index, IPValue item)
        {
            IsDirty = true;
            values.Insert(index, item?.Clone());
        }

        public void RemoveAt(int index)
        {
            IsDirty = true;
            values.RemoveAt(index);
        }

        public bool Contains(IPValue item)
        {
            return values.Contains(item);
        }

        private bool Equals(PSeq other)
        {
            return other != null && values.SequenceEqual(other.values);
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

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("(");
            var sep = "";
            foreach (var value in values)
            {
                sb.Append(sep);
                sb.Append(value);
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
            foreach (var value in values)
            {
                var v = value == null ? "null" : value.ToEscapedString();
                sb.Append(sep);
                sb.Append(v);
                sep = ", ";
            }

            sb.Append(")");
            return sb.ToString();
        }

        public object ToDict()
        {
            return values.Select(value => value == null ? null : value.ToDict()).ToList();
        }
    }
}