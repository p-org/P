using System;
using System.Collections.Generic;
using PChecker.Runtime.Values.Exceptions;

namespace PChecker.Runtime.Values
{
    [Serializable]
    public class PTuple : IPValue
    {
        public readonly IPValue[] fieldValues;

        public PTuple(params IPValue[] elems)
        {
            if (elems == null)
            {
                // A single null argument binds to the params array itself.
                fieldValues = new IPValue[1];
            }
            else if (elems.Length == 1)
            {
                fieldValues = elems;
            }
            else
            {
                fieldValues = elems;
                for (var i = 0; i < elems.Length; i++)
                {
                    elems[i] = PValues.Clone(elems[i]);
                }
            }
        }

        private PTuple(IPValue[] fieldValues, bool _)
        {
            this.fieldValues = fieldValues;
        }

        public IPValue this[int key]
        {
            get => fieldValues[key];
            set => fieldValues[key] = value;
        }

        public IPValue Clone()
        {
            var values = new IPValue[fieldValues.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = PValues.Clone(fieldValues[i]);
            }

            return new PTuple(values, false);
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

            if (tupValue.fieldValues.Length != fieldValues.Length)
            {
                return false;
            }

            for (var i = 0; i < fieldValues.Length; i++)
            {
                if (!PValues.SafeEquals(fieldValues[i], tupValue.fieldValues[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public void Update(int index, IPValue val)
        {
            fieldValues[index] = val;
        }

        public override int GetHashCode()
        {
            var hashCode = HashHelper.ComputeHash(fieldValues);
            return hashCode;
        }

        public override string ToString()
        {
            var retStr = "<";
            foreach (var field in fieldValues)
            {
                retStr = retStr + field + ",";
            }

            retStr += ">";
            return retStr;
        }

        public object ToDict()
        {
            var tupleDict = new Dictionary<int, object>();

            for (var i = 0; i < fieldValues.Length; i++)
            {
                var fieldValue = fieldValues[i];
                tupleDict.Add(i, fieldValue);
            }

            return tupleDict;
        }
    }

    [Serializable]
    public class PNamedTuple : IPValue
    {
        public readonly IPValue[] fieldValues;

        /// <summary>
        /// Field names, shared by all tuples of the same type and never mutated.
        /// </summary>
        public readonly string[] fieldNames;

        public PNamedTuple(string[] _fieldNames, params IPValue[] _fieldValues)
        {
            fieldNames = _fieldNames;
            // A single null argument binds to the params array itself.
            fieldValues = _fieldValues ?? new IPValue[1];
        }

        public IPValue this[string name]
        {
            get => fieldValues[IndexOf(name)];
            set => fieldValues[IndexOf(name)] = value;
        }

        private int IndexOf(string name)
        {
            for (var i = 0; i < fieldNames.Length; i++)
            {
                if (fieldNames[i] == name)
                {
                    return i;
                }
            }

            throw UnknownNamedTupleFieldAccess.FromFields(name, fieldNames);
        }

        public IPValue Clone()
        {
            var values = new IPValue[fieldValues.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = PValues.Clone(fieldValues[i]);
            }

            return new PNamedTuple(fieldNames, values);
        }

        public bool Equals(IPValue val)
        {
            if (!(val is PNamedTuple tup))
            {
                return false;
            }

            if (tup.fieldValues.Length != fieldValues.Length)
            {
                return false;
            }

            for (var i = 0; i < tup.fieldValues.Length; i++)
            {
                if (fieldNames[i] != tup.fieldNames[i])
                {
                    return false;
                }

                if (!PValues.SafeEquals(fieldValues[i], tup.fieldValues[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = HashHelper.ComputeHash(fieldValues);
            return hashCode;
        }

        public override string ToString()
        {
            var retStr = "<";
            for (var i = 0; i < fieldValues.Length; i++)
            {
                retStr += fieldNames[i] + ":" + fieldValues[i] + ", ";
            }

            retStr += ">";
            return retStr;
        }

        public string ToEscapedString()
        {
            var retStr = "<";
            for (var i = 0; i < fieldValues.Length; i++)
            {
                var v = fieldValues[i] == null ? "null" : fieldValues[i].ToEscapedString();
                retStr += fieldNames[i] + ":" + v + ", ";
            }

            retStr += ">";
            return retStr;
        }

        public object ToDict()
        {
            var namedTupleDict = new Dictionary<string, object>();

            for (var i = 0; i < fieldValues.Length; i++)
            {
                var fieldValue = fieldValues[i] == null ? null : fieldValues[i].ToDict();
                var fieldName = fieldNames[i];
                namedTupleDict.Add(fieldName, fieldValue);
            }

            return namedTupleDict;
        }
    }
}