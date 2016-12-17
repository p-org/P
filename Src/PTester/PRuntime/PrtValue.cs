using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace P.Runtime
{
    public abstract class PrtValue : IEquatable<PrtValue>
    {
        public static PrtNullValue NullValue = new PrtNullValue();
        public static PrtEventValue HaltEvent = new PrtEventValue(new PrtEvent("Halt", new PrtNullType(), PrtEvent.DefaultMaxInstances, false));

        public abstract PrtValue Clone();

        public static PrtValue PrtMkDefaultValue(PrtType type)
        {
            if (type is PrtAnyType || type is PrtNullType || type is PrtEventType || type is PrtMachineType)
            {
                return new PrtNullValue();
            }
            else if (type is PrtIntType)
            {
                return new PrtIntValue();
            }
            else if (type is PrtBoolType)
            {
                return new PrtBoolValue();
            }
            else if (type is PrtMapType)
            {
                return new PrtMapValue();
            }
            else if (type is PrtSeqType)
            {
                return new PrtSeqValue();
            }
            else if (type is PrtNamedTupleType)
            {
                return new PrtNamedTupleValue(type as PrtNamedTupleType);
            }
            else if (type is PrtTupleType)
            {
                return new PrtTupleValue(type as PrtTupleType);
            }
            else
            {
                throw new PrtInternalException("Invalid type in PrtMkDefaultType");
            }
        }

        public override string ToString()
        {
            throw new NotImplementedException("ToString method is not overridden in the derived class");
        }

        public abstract int Size();

        public abstract bool Equals(PrtValue val);

        public static bool PrtInhabitsType(PrtValue value, PrtType type)
        {
            if (type is PrtAnyType)
            {
                return true;
            }
            else if (value is PrtNullValue)
            {
                return (type is PrtEventType || type is PrtMachineType);
            }
            else if (type is PrtIntType)
            {
                return value is PrtIntValue;
            }
            else if (type is PrtBoolType)
            {
                return value is PrtBoolValue;
            }
            else if (type is PrtEventType)
            {
                return value is PrtEventValue;
            }
            else if (type is PrtMachineType)
            {
                return value is PrtMachineValue;
            }
            else if (type is PrtNamedTupleType) // must come before PrtTupleType since PrtNamedTupleType derives from PrtTupleType
            {
                var nmtupType = type as PrtNamedTupleType;
                var nmtupVal = value as PrtNamedTupleValue;
                if (nmtupVal == null) return false;
                if (nmtupVal.fieldValues.Count != nmtupType.fieldTypes.Count) return false;
                for (int i = 0; i < nmtupVal.fieldValues.Count; i++)
                {
                    if (nmtupVal.fieldNames[i] != nmtupType.fieldNames[i]) return false;
                }
                for (int i = 0; i < nmtupVal.fieldValues.Count; i++)
                {
                    if (!PrtInhabitsType(nmtupVal.fieldValues[i], nmtupType.fieldTypes[i])) return false;
                }
                return true;
            }
            else if (type is PrtTupleType)
            {
                var tupType = type as PrtTupleType;
                var tupVal = value as PrtTupleValue;
                if (tupVal == null) return false;
                if (tupVal.fieldValues.Count != tupType.fieldTypes.Count) return false;
                for (int i = 0; i < tupVal.fieldValues.Count; i++)
                {
                    if (!PrtInhabitsType(tupVal.fieldValues[i], tupType.fieldTypes[i])) return false;
                }
                return true;
            }
            else if (type is PrtMapType)
            {
                var mapType = type as PrtMapType;
                var mapVal = value as PrtMapValue;
                if (mapVal == null) return false;
                foreach (var p in mapVal.keyToValueMap)
                {
                    if (!PrtInhabitsType(p.Key.key, mapType.keyType)) return false;
                    if (!PrtInhabitsType(p.Value, mapType.valType)) return false;
                }
                return true;
            }
            else if (type is PrtSeqType)
            {
                var seqType = type as PrtSeqType;
                var seqVal = value as PrtSeqValue;
                if (seqVal == null) return false;
                foreach (var elem in seqVal.elements)
                {
                    if (!PrtInhabitsType(elem, seqType.elemType)) return false;
                }
                return true;
            }
            else
            {
                throw new PrtInternalException("Unknown type in PrtInhabitsType");
            }
        }

        public static PrtValue PrtCastValue(PrtValue value, PrtType type)
        {
            if (!PrtInhabitsType(value, type))
                throw new PrtInhabitsTypeException(String.Format("value {0} is not a member of type {1}", value.ToString(), type.ToString()));
            return value.Clone();
        }
    }

    public class PrtNullValue : PrtValue
    {
        public override PrtValue Clone()
        {
            return this;
        }

        public override string ToString()
        {
            return "null";
        }

        public override bool Equals(PrtValue value)
        {
            return (value is PrtNullValue);
        }

        public override int Size()
        {
            throw new NotImplementedException();
        }
    }

    public class PrtIntValue : PrtValue
    {
        public int nt;

        public PrtIntValue()
        {
            nt = 0;
        }

        public PrtIntValue(int val)
        {
            nt = val;
        }

        public override PrtValue Clone()
        {
            return new PrtIntValue(this.nt);
        }

        public override bool Equals(PrtValue val)
        {
            var intVal = val as PrtIntValue;
            if (intVal == null) return false;
            return this.nt == intVal.nt;
        }

        public override string ToString()
        {
            return nt.ToString();
        }

        public override int Size()
        {
            throw new NotImplementedException();
        }
    }

    public class PrtBoolValue : PrtValue
    {
        public bool bl;

        public PrtBoolValue()
        {
            bl = false;
        }

        public PrtBoolValue(bool val)
        {
            this.bl = val;
        }

        public override PrtValue Clone()
        {
            return new PrtBoolValue(this.bl);
        }

        public override bool Equals(PrtValue val)
        {
            var boolVal = val as PrtBoolValue;
            if (boolVal == null) return false;
            return this.bl == boolVal.bl;
        }

        public override string ToString()
        {
            return bl.ToString();
        }

        public override int Size()
        {
            throw new NotImplementedException();
        }
    }

    public class PrtEventValue : PrtValue
    {
        public PrtEvent evt;

        public PrtEventValue(PrtEvent val)
        {
            this.evt = val;
        }

        public override PrtValue Clone()
        {
            return new PrtEventValue(this.evt);
        }

        public override bool Equals(PrtValue val)
        {
            var eventVal = val as PrtEventValue;
            if (eventVal == null) return false;
            return this.evt == eventVal.evt;
        }

        public override string ToString()
        {
            return evt.name;
        }

        public override int Size()
        {
            throw new NotImplementedException();
        }
    }

    public class PrtMachineValue : PrtValue
    {
        public PrtImplMachine mach;

        public PrtMachineValue(PrtImplMachine mach)
        {
            this.mach = mach;
        }

        public override PrtValue Clone()
        {
            return new PrtMachineValue(this.mach);
        }

        public override bool Equals(PrtValue val)
        {
            var machineVal = val as PrtMachineValue;
            if (machineVal == null) return false;
            return this.mach == machineVal.mach;
        }

        public override string ToString()
        {
            return String.Format("{0}({1})", mach.Name, mach.instanceNumber);
        }

        public override int Size()
        {
            throw new NotImplementedException();
        }
    }

    public class PrtTupleValue : PrtValue
    {
        public List<PrtValue> fieldValues;

        public PrtTupleValue()
        {
            fieldValues = new List<PrtValue>();
        }

        public PrtTupleValue(PrtTupleType tupType)
        {
            fieldValues = new List<PrtValue>(tupType.fieldTypes.Count);
            foreach (var ft in tupType.fieldTypes)
            {
                fieldValues.Add(PrtMkDefaultValue(ft));
            }
        }

        public PrtTupleValue(PrtTupleType tupType, params PrtValue[] elems)
        {
            if (tupType.fieldTypes.Count != elems.Count())
                throw new PrtInternalException("Number of fields is different from number of arguments");
            fieldValues = new List<PrtValue>(tupType.fieldTypes.Count);
            foreach (var elem in elems)
            {
                fieldValues.Add(elem.Clone());
            }
        }

        public override PrtValue Clone()
        {
            var clone = new PrtTupleValue();
            foreach (var val in fieldValues)
            {
                clone.fieldValues.Add(val.Clone());
            }
            return clone;
        }

        public override bool Equals(PrtValue val)
        {
            if (val is PrtNamedTupleValue) return false;
            var tupValue = (val as PrtTupleValue);
            if (tupValue == null) return false;
            if (tupValue.fieldValues.Count != this.fieldValues.Count) return false;
            for (int i = 0;  i < fieldValues.Count; i++)
            {
                if (!this.fieldValues[i].Equals(tupValue.fieldValues[i])) return false;
            }
            return true;
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

        public override int Size()
        {
            throw new NotImplementedException();
        }
    }

    public class PrtNamedTupleValue : PrtTupleValue
    {
        public List<string> fieldNames;

        public PrtNamedTupleValue() : base()
        {
            fieldNames = new List<string>();
        }

        public PrtNamedTupleValue(PrtNamedTupleType tupType) : base (tupType)
        {
            fieldNames = new List<string>(tupType.fieldTypes.Count);
            foreach (var fn in tupType.fieldNames)
            {
                fieldNames.Add(fn);
            }
        }

        public PrtNamedTupleValue(PrtNamedTupleType tupType, params PrtValue[] elems) : base (tupType, elems)
        {
            fieldNames = new List<string>(tupType.fieldTypes.Count);
            foreach (var fn in tupType.fieldNames)
            {
                fieldNames.Add(fn);
            }
        }

        public override PrtValue Clone()
        {
            var clone = new PrtNamedTupleValue();
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

        public override bool Equals(PrtValue val)
        {
            var tup = val as PrtNamedTupleValue;
            if (tup == null) return false;
            if (tup.fieldValues.Count != this.fieldValues.Count) return false;
            for (int i = 0; i < tup.fieldValues.Count; i++)
            {
                if (this.fieldNames[i] != tup.fieldNames[i]) return false;
                if (!this.fieldValues[i].Equals(tup.fieldValues[i])) return false;
            }
            return true;
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

    public class PrtSeqValue : PrtValue
    {
        public List<PrtValue> elements;

        public PrtSeqValue()
        {
            elements = new List<PrtValue>();
        }

        public override PrtValue Clone()
        {
            var clone = new PrtSeqValue();
            foreach (var val in elements)
            {
                clone.elements.Add(val.Clone());
            }
            return clone;
        }

        public PrtValue Lookup(int index)
        {
            if (index < 0 || index >= elements.Count)
            {
                throw new PrtAssertFailureException("Illegal index for Lookup");
            }
            return elements[index];
        }

        public PrtValue Lookup(PrtValue index)
        {
            return Lookup(((PrtIntValue)index).nt);
        }

        public void Insert(int index, PrtValue val)
        {
            if (index < 0 || index > elements.Count)
            {
                throw new PrtAssertFailureException("Illegal index for Insert");
            }
            elements.Insert(index, val.Clone());
        }

        public void Update(int index, PrtValue val)
        {
            if (index < 0 || index > elements.Count)
            {
                throw new PrtAssertFailureException("Illegal index for Update");
            }
            if (index == elements.Count)
            {
                elements.Insert(index, val.Clone());
            }
            else
            {
                elements[index] = val.Clone();
            }
        }

        public void Remove(int index)
        {
            if (index < 0 || index >= elements.Count)
            {
                throw new PrtAssertFailureException("Illegal index for Remove");
            }
            elements.RemoveAt(index);
        }

        public override int Size()
        {
            return elements.Count();
        }

        public override bool Equals(PrtValue val)
        {
            var seqVal = val as PrtSeqValue;
            if (seqVal == null) return false;
            if (seqVal.elements.Count != this.elements.Count) return false;
            for (int i = 0; i < this.elements.Count; i++)
            {
                if (!this.elements[i].Equals(seqVal.elements[i])) return false;
            }
            return true;
        }

        public override string ToString()
        {
            string retStr = "(";
            for (int i = 0; i < elements.Count; i++)
            {
                retStr += elements[i] + ", ";
            }
            retStr += ")";
            return retStr;
        }
    }

    public class PrtMapKey
    {
        public PrtValue key;
        public int keyIndex;
        public PrtMapKey(PrtValue x, int i)
        {
            key = x;
            keyIndex = i;
        }
        public override bool Equals(object obj)
        {
            var mapKey = obj as PrtMapKey;
            if (mapKey == null) return false;
            return key.Equals(mapKey);
        }

        public override int GetHashCode()
        {
            return key.GetHashCode();
        }
    }

    public class PrtMapValue : PrtValue
    {
        public int nextKeyIndex;
        public Dictionary<PrtMapKey, PrtValue> keyToValueMap;

        public PrtMapValue()
        {
            nextKeyIndex = 0;
            keyToValueMap = new Dictionary<PrtMapKey, PrtValue>();
        }

        public override PrtValue Clone()
        {
            var clone = new PrtMapValue();
            int count = 0;
            foreach (var k in keyToValueMap.Keys.OrderBy(x => x.keyIndex))
            {
                clone.keyToValueMap[new PrtMapKey(k.key.Clone(), count)] = keyToValueMap[k].Clone();
                count++;
            }
            clone.nextKeyIndex = count;
            return clone;
        }

        public override int Size()
        {
            return keyToValueMap.Count;
        }

        public PrtValue Lookup(PrtValue key)
        {
            if (!Contains(key))
            {
                throw new PrtAssertFailureException("Illegal key in Lookup");
            }
            return keyToValueMap[new PrtMapKey(key, 0)];
        }

        public PrtSeqValue Keys()
        {
            var seqKeys = new PrtSeqValue();
            foreach (var k in keyToValueMap.Keys.OrderBy(x => x.keyIndex))
            {
                seqKeys.elements.Add(k.key.Clone());
            }
            return seqKeys;
        }

        public PrtSeqValue Values()
        {
            var seqValues = new PrtSeqValue();
            foreach (var k in keyToValueMap.Keys.OrderBy(x => x.keyIndex))
            {
                seqValues.elements.Add(keyToValueMap[k].Clone());
            }
            return seqValues;
        }

        public bool Contains(PrtValue key)
        {
            return keyToValueMap.ContainsKey(new PrtMapKey(key, 0));
        }

        public void Add(PrtValue key, PrtValue val)
        {
            if (Contains(key))
            {
                throw new PrtAssertFailureException("Illegal key in Add");
            }
            else
            {
                keyToValueMap[new PrtMapKey(key.Clone(), nextKeyIndex)] = val.Clone();
                nextKeyIndex++;
            }
        }

        public void Remove(PrtValue key)
        {
            if (!Contains(key))
            {
                throw new PrtAssertFailureException("Illegal key in Remove");
            }
            keyToValueMap.Remove(new PrtMapKey(key, 0));
        }

        public void Update(PrtValue key, PrtValue val)
        {
            keyToValueMap[new PrtMapKey(key, 0)] = val.Clone();
        }

        public override bool Equals(PrtValue val)
        {
            var mapVal = val as PrtMapValue;
            if (mapVal == null) return false;
            if (this.keyToValueMap.Count != mapVal.keyToValueMap.Count) return false;
            foreach (var k in this.keyToValueMap.Keys)
            {
                if (!mapVal.Contains(k.key)) return false;
            }
            return true;
        }

        public override string ToString()
        {
            string retStr = "(";
            foreach (var k in keyToValueMap.Keys.OrderBy(x => x.keyIndex))
            {
                retStr += "(" + k.key.ToString() + "," + keyToValueMap[k].ToString() + "), ";
            }
            retStr += ")";
            return retStr;
        }
    }
}