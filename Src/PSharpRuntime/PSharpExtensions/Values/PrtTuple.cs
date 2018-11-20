using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrtSharp.Values
{
    [Serializable]
    public class PrtTuple : IPrtValue
    {
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
        public List<IPrtValue> fieldValues;

        public IPrtValue GetVal(int field)
        {
            return fieldValues[field];
        }

        public PrtTuple()
        {
            fieldValues = new List<IPrtValue>();
        }

        public PrtTuple(PrtType type)
        {
            var tupType = type as PrtTupleType;
            fieldValues = new List<IPrtValue>(tupType.fieldTypes.Count);
            foreach (var ft in tupType.fieldTypes)
            {
                fieldValues.Add(PrtMkDefaultValue(ft));
            }
        }

        public PrtTuple(params IPrtValue[] elems)
        {
            fieldValues = new List<IPrtValue>(elems.Count());
            foreach (var elem in elems)
            {
                fieldValues.Add(elem.Clone());
            }
        }

        public void Update(int index, IPrtValue val)
        {
            fieldValues[index] = val;
        }


        public IPrtValue Clone()
        {
            var clone = new PrtTuple();
            foreach (var val in fieldValues)
            {
                clone.fieldValues.Add(val.Clone());
            }
            return clone;
        }

        public bool Equals(IPrtValue val)
        {
            if (val is PrtNamedTuple) return false;
            var tupValue = (val as PrtTuple);
            if (tupValue == null) return false;
            if (tupValue.fieldValues.Count != this.fieldValues.Count) return false;
            for (int i = 0; i < fieldValues.Count; i++)
            {
                if (!this.fieldValues[i].Equals(tupValue.fieldValues[i])) return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return fieldValues.GetHashCode();
        }

        public override string ToString()
        {
            string retStr = "<";
            foreach (var field in fieldValues)
            {
                retStr = retStr + field.ToString() + ",";
            }
            retStr += ">";
            return retStr;
        }
    }

    [Serializable]
    public class PrtNamedTuple : PrtTuple
    {
        public List<string> fieldNames;

        public IPrtValue GetVal(string fieldName)
        {
            return fieldValues[fieldName.IndexOf(fieldName, StringComparison.Ordinal)];
        }

        public PrtNamedTuple() : base()
        {
            fieldNames = new List<string>();
        }

        public PrtNamedTuple(PrtType type) : base(type)
        {
            var tupType = type as PrtNamedTupleType;
            fieldNames = new List<string>(tupType.fieldTypes.Count);
            foreach (var fn in tupType.fieldNames)
            {
                fieldNames.Add(fn);
            }
        }

        public PrtNamedTuple(PrtType type, params IPrtValue[] elems) : base(elems)
        {
            var tupType = type as PrtNamedTupleType;
            fieldNames = new List<string>(tupType.fieldTypes.Count);
            foreach (var fn in tupType.fieldNames)
            {
                fieldNames.Add(fn);
            }
        }

        public new IPrtValue Clone()
        {
            var clone = new PrtNamedTuple();
            foreach (var name in fieldNames)
            {
                clone.fieldNames.Add(name);
            }
            foreach (var val in fieldValues)
            {
                clone.fieldValues.Add(val.Clone());
            }
            return clone;
        }

        public override bool Equals(object val)
        {
            if (!(val is PrtNamedTuple tup)) return false;
            if (tup.fieldValues.Count != this.fieldValues.Count) return false;
            for (var i = 0; i < tup.fieldValues.Count; i++)
            {
                if (this.fieldNames[i] != tup.fieldNames[i]) return false;
                if (!this.fieldValues[i].Equals(tup.fieldValues[i])) return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return fieldValues.GetHashCode();
        }

        public override string ToString()
        {
            string retStr = "<";
            for (int i = 0; i < fieldValues.Count; i++)
            {
                retStr += fieldNames[i] + ":" + fieldValues[i].ToString() + ", ";
            }
            retStr += ">";
            return retStr;
        }
    }
}