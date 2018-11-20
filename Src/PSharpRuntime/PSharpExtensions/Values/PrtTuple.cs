using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PrtSharp.Values
{
    [Serializable]
    public class PrtTuple : IPrtValue
    {
        public readonly List<IPrtValue> fieldValues;

        public PrtTuple()
        {
            fieldValues = new List<IPrtValue>();
        }

        public PrtTuple(PrtType type)
        {
            var tupType = type as PrtTupleType;
            Debug.Assert(tupType != null, nameof(tupType) + " != null");
            fieldValues = tupType.fieldTypes.Select(PrtMkDefaultValue).ToList();
        }

        public PrtTuple(params IPrtValue[] elems)
        {
            fieldValues = new List<IPrtValue>(elems.Count());
            foreach (var elem in elems) fieldValues.Add(elem.Clone());
        }


        public IPrtValue Clone()
        {
            var clone = new PrtTuple();
            foreach (var val in fieldValues) clone.fieldValues.Add(val.Clone());
            return clone;
        }

        public bool Equals(IPrtValue val)
        {
            if (val is PrtNamedTuple) return false;
            var tupValue = val as PrtTuple;
            if (tupValue == null) return false;
            if (tupValue.fieldValues.Count != fieldValues.Count) return false;
            for (var i = 0; i < fieldValues.Count; i++)
                if (!fieldValues[i].Equals(tupValue.fieldValues[i]))
                    return false;
            return true;
        }

        private static IPrtValue PrtMkDefaultValue(PrtType type)
        {
            switch (type)
            {
                case PrtAnyType _:
                case PrtNullType _:
                case PrtEventType _:
                case PrtMachineType _:
                case PrtPermissionType _:
                    return null;
                case PrtIntType _:
                    return new PrtInt(0);
                case PrtFloatType _:
                    return new PrtFloat(0);
                case PrtEnumType _:
                    throw new NotImplementedException("asd");
                case PrtBoolType _:
                    return (PrtBool) false;
                case PrtMapType _:
                    return new PrtMap();
                case PrtSeqType _:
                    return new PrtSeq();
                case PrtNamedTupleType tupleType:
                    return new PrtNamedTuple(tupleType);
                case PrtTupleType prtTupleType:
                    return new PrtTuple(prtTupleType);
                default:
                    throw new Exception("Invalid type in PrtMkDefaultType");
            }
        }

        public IPrtValue GetVal(int field)
        {
            return fieldValues[field];
        }

        public void Update(int index, IPrtValue val)
        {
            fieldValues[index] = val;
        }

        public override int GetHashCode()
        {
            var hashCode = fieldValues.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            var retStr = "<";
            foreach (var field in fieldValues) retStr = retStr + field + ",";
            retStr += ">";
            return retStr;
        }
    }

    [Serializable]
    public class PrtNamedTuple : PrtTuple
    {
        public List<string> fieldNames;

        public PrtNamedTuple()
        {
            fieldNames = new List<string>();
        }

        public PrtNamedTuple(PrtType type) : base(type)
        {
            var nmdTupleType = type as PrtNamedTupleType;
            Debug.Assert(nmdTupleType != null, nameof(nmdTupleType) + " != null");
            fieldNames = nmdTupleType.fieldNames.ToList();
        }

        public PrtNamedTuple(PrtType type, params IPrtValue[] elems) : base(elems)
        {
            var tupType = type as PrtNamedTupleType;
            fieldNames = new List<string>(tupType.fieldTypes.Count);
            foreach (var fn in tupType.fieldNames) fieldNames.Add(fn);
        }

        public IPrtValue GetVal(string fieldName)
        {
            return fieldValues[fieldName.IndexOf(fieldName, StringComparison.Ordinal)];
        }

        public new IPrtValue Clone()
        {
            var clone = new PrtNamedTuple();
            foreach (var name in fieldNames) clone.fieldNames.Add(name);
            foreach (var val in fieldValues) clone.fieldValues.Add(val.Clone());
            return clone;
        }

        public override bool Equals(object val)
        {
            if (!(val is PrtNamedTuple tup)) return false;
            if (tup.fieldValues.Count != fieldValues.Count) return false;
            for (var i = 0; i < tup.fieldValues.Count; i++)
            {
                if (fieldNames[i] != tup.fieldNames[i]) return false;
                if (!fieldValues[i].Equals(tup.fieldValues[i])) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return fieldValues.GetHashCode();
        }

        public override string ToString()
        {
            var retStr = "<";
            for (var i = 0; i < fieldValues.Count; i++) retStr += fieldNames[i] + ":" + fieldValues[i] + ", ";
            retStr += ">";
            return retStr;
        }
    }
}