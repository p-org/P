using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PChecker.Runtime.Values.Exceptions;

namespace PChecker.Runtime.Values
{
    [Serializable]
    public class PTuple : IPValue
    {
        private readonly IPValue[] values;

        /// <summary>
        /// The field values. Use the indexer to change a field.
        /// </summary>
        public IReadOnlyList<IPValue> fieldValues => values;

        /// <summary>
        /// Creates a tuple of the specified values. The tuple does not keep the array, and when
        /// there are several values it stores copies of them.
        /// </summary>
        public PTuple(params IPValue[] elems)
        {
            if (elems == null)
            {
                // A single null argument binds to the params array itself.
                values = new IPValue[1];
            }
            else if (elems.Length == 1)
            {
                values = new[] { elems[0] };
            }
            else
            {
                values = new IPValue[elems.Length];
                for (var i = 0; i < elems.Length; i++)
                {
                    values[i] = PValues.Clone(elems[i]);
                }
            }
        }

        private PTuple(IPValue[] values, bool _)
        {
            this.values = values;
        }

        /// <summary>
        /// Creates a tuple that stores the specified array as is. For generated code, which
        /// passes a new array of values that nothing else refers to.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static PTuple FromOwnedValues(params IPValue[] values)
        {
            // A single null argument binds to the params array itself.
            return new PTuple(values ?? new IPValue[1], false);
        }

        public IPValue this[int key]
        {
            get => values[key];
            set => values[key] = value;
        }

        public IPValue Clone()
        {
            var clone = new IPValue[values.Length];
            for (var i = 0; i < clone.Length; i++)
            {
                clone[i] = PValues.Clone(values[i]);
            }

            return new PTuple(clone, false);
        }

        public bool Equals(IPValue val)
        {
            if (val is PNamedTuple)
            {
                return false;
            }

            var tupValue = val as PTuple;
            if (tupValue == null)
            {
                return false;
            }

            if (tupValue.values.Length != values.Length)
            {
                return false;
            }

            for (var i = 0; i < values.Length; i++)
            {
                if (!PValues.SafeEquals(values[i], tupValue.values[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public void Update(int index, IPValue val)
        {
            values[index] = val;
        }

        public override int GetHashCode()
        {
            var hashCode = HashHelper.ComputeHash(values);
            return hashCode;
        }

        public override string ToString()
        {
            var retStr = "<";
            foreach (var field in values)
            {
                retStr = retStr + field + ",";
            }

            retStr += ">";
            return retStr;
        }

        public object ToDict()
        {
            var tupleDict = new Dictionary<int, object>();

            for (var i = 0; i < values.Length; i++)
            {
                var fieldValue = values[i];
                tupleDict.Add(i, fieldValue);
            }

            return tupleDict;
        }
    }

    [Serializable]
    public class PNamedTuple : IPValue
    {
        private readonly IPValue[] values;

        /// <summary>
        /// The field names. Tuples created by generated code share them with all tuples of their type.
        /// </summary>
        private readonly string[] names;

        /// <summary>
        /// The field values. Use the indexer to change a field.
        /// </summary>
        public IReadOnlyList<IPValue> fieldValues => values;

        /// <summary>
        /// Read-only views of field name arrays, made when first asked for, so that tuples need no
        /// field for them.
        /// </summary>
        private static readonly ConditionalWeakTable<string[], ReadOnlyCollection<string>> NameViews = new();

        /// <summary>
        /// The field names. They cannot be changed, since tuples share them.
        /// </summary>
        public ReadOnlyCollection<string> fieldNames =>
            NameViews.GetValue(names, static n => new ReadOnlyCollection<string>(n));

        /// <summary>
        /// Creates a tuple with the specified field names and values. The tuple keeps neither array.
        /// </summary>
        public PNamedTuple(string[] _fieldNames, params IPValue[] _fieldValues)
        {
            names = (string[])_fieldNames.Clone();
            // A single null argument binds to the params array itself.
            values = _fieldValues == null ? new IPValue[1] : (IPValue[])_fieldValues.Clone();
        }

        private PNamedTuple(string[] names, IPValue[] values, bool _)
        {
            this.names = names;
            this.values = values;
        }

        /// <summary>
        /// Creates a tuple that stores the specified arrays as is. For generated code, which passes
        /// a new array of values that nothing else refers to, and a field names array of the tuple
        /// type that is never changed.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static PNamedTuple FromOwnedValues(string[] fieldNames, params IPValue[] values)
        {
            // A single null argument binds to the params array itself.
            return new PNamedTuple(fieldNames, values ?? new IPValue[1], false);
        }

        public IPValue this[string name]
        {
            get => values[IndexOf(name)];
            set => values[IndexOf(name)] = value;
        }

        private int IndexOf(string name)
        {
            for (var i = 0; i < names.Length; i++)
            {
                if (names[i] == name)
                {
                    return i;
                }
            }

            throw UnknownNamedTupleFieldAccess.FromFields(name, names);
        }

        public IPValue Clone()
        {
            var clone = new IPValue[values.Length];
            for (var i = 0; i < clone.Length; i++)
            {
                clone[i] = PValues.Clone(values[i]);
            }

            return new PNamedTuple(names, clone, false);
        }

        public bool Equals(IPValue val)
        {
            if (!(val is PNamedTuple tup))
            {
                return false;
            }

            if (tup.values.Length != values.Length)
            {
                return false;
            }

            for (var i = 0; i < tup.values.Length; i++)
            {
                if (names[i] != tup.names[i])
                {
                    return false;
                }

                if (!PValues.SafeEquals(values[i], tup.values[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = HashHelper.ComputeHash(values);
            return hashCode;
        }

        public override string ToString()
        {
            var retStr = "<";
            for (var i = 0; i < values.Length; i++)
            {
                retStr += names[i] + ":" + values[i] + ", ";
            }

            retStr += ">";
            return retStr;
        }

        public string ToEscapedString()
        {
            var retStr = "<";
            for (var i = 0; i < values.Length; i++)
            {
                var v = values[i] == null ? "null" : values[i].ToEscapedString();
                retStr += names[i] + ":" + v + ", ";
            }

            retStr += ">";
            return retStr;
        }

        public object ToDict()
        {
            var namedTupleDict = new Dictionary<string, object>();

            for (var i = 0; i < values.Length; i++)
            {
                var fieldValue = values[i] == null ? null : values[i].ToDict();
                var fieldName = names[i];
                namedTupleDict.Add(fieldName, fieldValue);
            }

            return namedTupleDict;
        }
    }
}