using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace P.Runtime
{
    public abstract class PrtValue
    {
        public static PrtNullValue NullValue = new PrtNullValue();
        public static PrtEventValue HaltEvent = new PrtEventValue(new PrtEvent("Halt", new PrtNullType(), PrtEvent.DefaultMaxInstances, false));

        public abstract PrtValue Clone();

        public static PrtValue PrtMkDefaultValue(PrtType type)
        {
            if (type is PrtAnyType || type is PrtNullType || type is PrtEventType || type is PrtMachineType)
                return new PrtNullValue();

            else if(type is PrtIntType)
            {
                return new PrtIntValue();
            }
            else if(type is PrtBoolType)
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
            else if(type is PrtTupleType)
            {
                return new PrtTupleValue(type as PrtTupleType);
            }
            else
            {
                Debug.Assert(false, "Invalid type !!");
                return null;
            }
        }

        public abstract string GetString();

        public abstract bool IsEqual(PrtValue value);

        public static bool PrtInhabitsType(PrtValue value, PrtType type)
        {
            if (type is PrtAnyType)
            {
                return true;
            }

            if (value is PrtNullValue)
            {
                if (type is PrtEventType || type is PrtMachineType)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (type is PrtIntType)
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
            else if (type is PrtTupleType)
            {
                if ((!(value is PrtTupleValue)) || (value as PrtTupleValue).fieldValues.Count != (type as PrtTupleType).fieldTypes.Count)
                {
                    return false;
                }
                else
                {
                    int index = 0;
                    var tupVal = (value as PrtTupleValue);
                    var tupType = (type as PrtTupleType);
                    while (index < tupVal.fieldValues.Count)
                    {
                        if (!PrtInhabitsType(tupVal.fieldValues[index], tupType.fieldTypes[index]))
                        {
                            return false;
                        }
                        index++;
                    }

                    return true;
                }
            }
            else if (type is PrtNamedTupleType)
            {
                if ((!(value is PrtNamedTupleValue)) || (value as PrtNamedTupleValue).fieldValues.Count != (type as PrtNamedTupleType).fieldTypes.Count)
                {
                    return false;
                }
                else
                {
                    int index = 0;
                    var nmtupVal = (value as PrtNamedTupleValue);
                    var nmtupType = (type as PrtNamedTupleType);
                    while (index < nmtupVal.fieldValues.Count)
                    {
                        if (nmtupVal.fieldNames[index] != nmtupType.fieldNames[index])
                        {
                            return false;
                        }
                        else if (!PrtInhabitsType(nmtupVal.fieldValues[index], nmtupType.fieldTypes[index]))
                        {
                            return false;
                        }
                        index++;
                    }

                    return true;
                }
            }
            else if (type is PrtMapType)
            {
                if (!(value is PrtMapValue))
                {
                    return false;
                }
                else
                {
                    var mapVal = (value as PrtMapValue);
                    foreach (var key in mapVal.keys)
                    {
                        if (!PrtInhabitsType(key, (type as PrtMapType).keyType))
                        {
                            return false;
                        }
                    }
                    foreach (var val in mapVal.values)
                    {
                        if (!PrtInhabitsType(val, (type as PrtMapType).valType))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            else if (type is PrtSeqType)
            {
                if (!(value is PrtSeqValue))
                {
                    return false;
                }
                else
                {
                    var seqVal = (value as PrtSeqValue);
                    foreach (var elem in seqVal.elements)
                    {
                        if (!PrtInhabitsType(elem, (type as PrtSeqType).elemType))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            else
            {
                Debug.Assert(false, "Unknown type");
                return false;
            }
        }

        public static PrtValue PrtCastValue(PrtValue value, PrtType type)
        {
            if (!PrtInhabitsType(value, type))
                throw new PrtInhabitsTypeException(String.Format("value {0} is not a member of type {1}", value.GetString(), type.GetString()));
            return value.Clone();
        }

    }

    public class PrtNullValue : PrtValue
    {
        public override PrtValue Clone()
        {
            return this;
        }

        public override string GetString()
        {
            return "null";
        }

        public override bool IsEqual(PrtValue value)
        {
            if (value is PrtNullValue)
                return true;
            else
                return false;
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

        public override bool IsEqual(PrtValue val)
        {
            Debug.Assert(val is PrtIntValue, "Error in type checking, invalid equals invocation");
            return this.nt == (val as PrtIntValue).nt;
        }

        public override string GetString()
        {
            return nt.ToString();
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

        public override bool IsEqual(PrtValue val)
        {
            Debug.Assert(val is PrtBoolValue, "Error in type checking, invalid equals invocation");
            return this.bl == (val as PrtBoolValue).bl;
        }

        public override string GetString()
        {
            return bl.ToString();
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

        public override bool IsEqual(PrtValue val)
        {
            Debug.Assert(val is PrtEventValue, "Error in type checking, invalid equals invocation");
            return this.evt == (val as PrtEventValue).evt;
        }

        public override string GetString()
        {
            return evt.name;
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

        public override bool IsEqual(PrtValue val)
        {
            Debug.Assert(val is PrtMachineValue, "Error in type checking, invalid equals invocation");
            return this.mach == (val as PrtMachineValue).mach;
        }

        public override string GetString()
        {
            return String.Format("{0}({1})", mach.Name, mach.instanceNumber);
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
            fieldValues = new List<PrtValue>(tupType.fieldTypes.Count);
            foreach (var elem in elems)
            {
                fieldValues.Add(elem.Clone());
            }
            if (tupType.fieldTypes.Count != fieldValues.Count)
                throw new PrtInternalException();
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

        public override bool IsEqual(PrtValue val)
        {
            Debug.Assert(val is PrtTupleValue, "Error in type checking, invalid equals invocation");
            var tupValue = (val as PrtTupleValue);
            Debug.Assert(tupValue.fieldValues.Count == this.fieldValues.Count, "Error in type checking, tuple sizes not equal");

            int index = 0;
            while (index < fieldValues.Count)
            {
                if (!this.fieldValues[index].IsEqual(tupValue.fieldValues[index]))
                    return false;

                index++;
            }
            return true;
        }

        public override string GetString()
        {
            string retStr = "<";
            foreach (var field in fieldValues)
            {
                retStr = retStr + field.GetString() + ",";
            }
            retStr += ">";
            return retStr;
        }
    }

    public class PrtNamedTupleValue : PrtValue
    {
        public List<string> fieldNames;
        public List<PrtValue> fieldValues;

        public PrtNamedTupleValue()
        {
            fieldNames = new List<string>();
            fieldValues = new List<PrtValue>();
        }

        public PrtNamedTupleValue(PrtNamedTupleType tupType)
        {
            fieldNames = new List<string>(tupType.fieldTypes.Count);
            fieldValues = new List<PrtValue>(tupType.fieldTypes.Count);
            foreach (var fn in tupType.fieldNames)
            {
                fieldNames.Add(fn);
            }
            foreach (var ft in tupType.fieldTypes)
            {
                fieldValues.Add(PrtMkDefaultValue(ft));
            }
        }

        public PrtNamedTupleValue(PrtNamedTupleType tupType, params PrtValue[] elems)
        {
            fieldNames = new List<string>(tupType.fieldTypes.Count);
            fieldValues = new List<PrtValue>(tupType.fieldTypes.Count);
            foreach (var fn in tupType.fieldNames)
            {
                fieldNames.Add(fn);
            }
            foreach (var elem in elems)
            {
                fieldValues.Add(elem.Clone());
            }
            if (tupType.fieldTypes.Count != fieldValues.Count)
                throw new PrtInternalException();
        }

        public override PrtValue Clone()
        {
            var clone = new PrtNamedTupleValue();
            clone.fieldNames = this.fieldNames;
            foreach (var val in fieldValues)
            {
                clone.fieldValues.Add(val.Clone());
            }
            return clone;
        }

        public override bool IsEqual(PrtValue val)
        {
            Debug.Assert(val is PrtNamedTupleValue, "Error in type checking, invalid equals invocation");
            var tup = (val as PrtNamedTupleValue);
            var tupValues = tup.fieldValues;
            Debug.Assert(tup.fieldValues.Count == this.fieldValues.Count, "Error in type checking, tuple sizes not equal");

            int index = 0;

            while (index < tupValues.Count)
            {
                if (!this.fieldValues[index].IsEqual(tupValues[index]))
                    return false;

                index++;
            }
            return true;
        }

        public override string GetString()
        {
            string retStr = "<";
            int index = 0;

            while (index < fieldValues.Count)
            {
                retStr += fieldNames[index] + ":" + fieldValues[index].GetString() + ", ";
                index++;
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

        public void Insert(int index, PrtValue val)
        {
            //TODO: raise an exception for invalid index
            elements.Insert(index, val.Clone());
        }

        public void Remove(int index)
        {
            //TODO: raise an exception for invalid index
            elements.RemoveAt(index);
        }

        public int SizeOf()
        {
            return elements.Count();
        }

        public override bool IsEqual(PrtValue val)
        {
            Debug.Assert(val is PrtSeqValue, "Error in type checking, invalid equals invocation");
            var seqVal = val as PrtSeqValue;
            if (seqVal.elements.Count != this.elements.Count)
            {
                return false;
            }
            else
            {
                int index = 0;
                while (index < this.elements.Count)
                {
                    if (!this.elements[index].IsEqual(seqVal.elements[index]))
                        return false;

                    index++;
                }
            }
            return true;
        }

        public override string GetString()
        {
            string retStr = "(";
            int index = 0;

            while (index < elements.Count)
            {
                retStr += elements[index] + ", ";
                index++;
            }
            retStr += ")";
            return retStr;
        }
    }

    public class PrtMapValue : PrtValue
    {
        public List<PrtValue> keys;
        public List<PrtValue> values;

        public PrtMapValue()
        {
            values = new List<PrtValue>();
            keys = new List<PrtValue>();
        }

        public override PrtValue Clone()
        {
            var clone = new PrtMapValue();
            foreach (var v in values)
            {
                clone.values.Add(v.Clone());
            }
            foreach (var k in keys)
            {
                clone.keys.Add(k.Clone());
            }
            return clone;
        }

        public int SizeOf()
        {
            return values.Count();
        }

        public bool Contains(PrtValue key)
        {
            return keys.Where(k => k.IsEqual(key)).Count() > 0;
        }

        public void Add(PrtValue key, PrtValue val)
        {
            if (Contains(key))
            {
                //TODO: raise an exception for invalid add
            }
            else
            {
                keys.Add(key.Clone());
                values.Add(val.Clone());
            }
        }

        public void Remove(PrtValue key)
        {
            if (!Contains(key))
            {
                //TODO: raise an exception for invalid remove
            }
            var index = keys.FindIndex((k => k.Equals(key)));
            keys.RemoveAt(index);
            values.RemoveAt(index);
        }

        public void Update(PrtValue key, PrtValue val)
        {
            if (!Contains(key))
            {
                //TODO: raise an exception for invalid update
            }
            var index = keys.FindIndex((k => k.Equals(key)));
            values[index] = val.Clone();
        }

        public override bool IsEqual(PrtValue val)
        {
            Debug.Assert(val is PrtMapValue, "Error in type checking, invalid equals invocation");
            var mapVal = val as PrtMapValue;
            if (mapVal.keys.Count != this.keys.Count)
            {
                return false;
            }
            else
            {
                foreach (var k in this.keys)
                {
                    if (!mapVal.Contains(k))
                    {
                        return false;
                    }
                    else
                    {
                        var index = this.keys.FindIndex(_k => _k.IsEqual(k));
                        var _index = mapVal.keys.FindIndex(_k => _k.IsEqual(k));
                        if (this.values[index].IsEqual(mapVal.values[_index]))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        public override string GetString()
        {
            string retStr = "(";
            int index = 0;

            while (index < values.Count)
            {
                retStr += "(" + keys[index].GetString() + "," + values[index].GetString() + "), ";
                index++;
            }
            retStr += ")";
            return retStr;
        }
    }
}