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
                if ((!(value is PrtTupleValue)) || (value as PrtTupleValue).value.Count != (type as PrtTupleType).fieldTypes.Count)
                {
                    return false;
                }
                else
                {
                    int index = 0;
                    var tupVal = (value as PrtTupleValue);
                    var tupType = (type as PrtTupleType);
                    while (index < tupVal.value.Count)
                    {
                        if (!PrtInhabitsType(tupVal.value[index], tupType.fieldTypes[index]))
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
                if ((!(value is PrtNamedTupleValue)) || (value as PrtNamedTupleValue).value.Count != (type as PrtNamedTupleType).fieldTypes.Count)
                {
                    return false;
                }
                else
                {
                    int index = 0;
                    var nmtupVal = (value as PrtNamedTupleValue);
                    var nmtupType = (type as PrtNamedTupleType);
                    while (index < nmtupVal.value.Count)
                    {
                        if (nmtupVal.fieldNames[index] != nmtupType.fieldNames[index])
                        {
                            return false;
                        }
                        else if (!PrtInhabitsType(nmtupVal.value[index], nmtupType.fieldTypes[index]))
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
                    foreach (var elem in seqVal.value)
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
        public int value;

        public PrtIntValue()
        {
            value = 0;
        }

        public PrtIntValue(int val)
        {
            value = val;
        }

        public override PrtValue Clone()
        {
            return new PrtIntValue(this.value);
        }

        public override bool IsEqual(PrtValue val)
        {
            Debug.Assert(val is PrtIntValue, "Error in type checking, invalid equals invocation");
            return this.value == (val as PrtIntValue).value;
        }

        public override string GetString()
        {
            return value.ToString();
        }
    }

    public class PrtBoolValue : PrtValue
    {
        public bool value;

        public PrtBoolValue()
        {
            value = false;
        }

        public PrtBoolValue(bool val)
        {
            this.value = val;
        }

        public override PrtValue Clone()
        {
            return new PrtBoolValue(this.value);
        }

        public override bool IsEqual(PrtValue val)
        {
            Debug.Assert(val is PrtBoolValue, "Error in type checking, invalid equals invocation");
            return this.value == (val as PrtBoolValue).value;
        }

        public override string GetString()
        {
            return value.ToString();
        }
    }

    public class PrtEventValue : PrtValue
    {
        public PrtEvent value;

        public PrtEventValue(PrtEvent val)
        {
            this.value = val;
        }

        public override PrtValue Clone()
        {
            return new PrtEventValue(this.value);
        }

        public override bool IsEqual(PrtValue val)
        {
            Debug.Assert(val is PrtEventValue, "Error in type checking, invalid equals invocation");
            return this.value == (val as PrtEventValue).value;
        }

        public override string GetString()
        {
            return value.name;
        }
    }

    public class PrtMachineValue : PrtValue
    {
        public PrtImplMachine value;

        public PrtMachineValue(PrtImplMachine mach)
        {
            this.value = mach;
        }

        public override PrtValue Clone()
        {
            return new PrtMachineValue(this.value);
        }

        public override bool IsEqual(PrtValue val)
        {
            Debug.Assert(val is PrtMachineValue, "Error in type checking, invalid equals invocation");
            return this.value == (val as PrtMachineValue).value;
        }

        public override string GetString()
        {
            return String.Format("{0}({1})", value.Name, value.instanceNumber);
        }
    }

    public class PrtTupleValue : PrtValue
    {
        public List<PrtValue> value;

        public PrtTupleValue()
        {
            value = new List<PrtValue>();
        }

        public PrtTupleValue(PrtTupleType tupType)
        {
            foreach(var ft in tupType.fieldTypes)
            {
                value.Add(PrtMkDefaultValue(ft));
            }
        }

        public override PrtValue Clone()
        {
            var clone = new PrtTupleValue();
            foreach (var val in value)
            {
                clone.value.Add(val.Clone());
            }

            return clone;
        }

        public override bool IsEqual(PrtValue val)
        {
            Debug.Assert(val is PrtTupleValue, "Error in type checking, invalid equals invocation");
            var tupValue = (val as PrtTupleValue);
            Debug.Assert(tupValue.value.Count == this.value.Count, "Error in type checking, tuple sizes not equal");

            int index = 0;
            while (index < value.Count)
            {
                if (!this.value[index].IsEqual(tupValue.value[index]))
                    return false;

                index++;
            }
            return true;
        }

        public override string GetString()
        {
            string retStr = "<";
            foreach (var field in value)
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
        public List<PrtValue> value;

        public PrtNamedTupleValue()
        {
            value = new List<PrtValue>();
            fieldNames = new List<string>();
        }

        public PrtNamedTupleValue(PrtNamedTupleType tupType)
        {
            foreach (var fn in tupType.fieldNames)
            {
                fieldNames.Add(fn);
            }
            foreach (var ft in tupType.fieldTypes)
            {
                value.Add(PrtMkDefaultValue(ft));
            }
        }

        public override PrtValue Clone()
        {
            var clone = new PrtNamedTupleValue();
            clone.fieldNames = this.fieldNames;
            foreach (var val in value)
            {
                clone.value.Add(val.Clone());
            }
            return clone;
        }

        public override bool IsEqual(PrtValue val)
        {
            Debug.Assert(val is PrtNamedTupleValue, "Error in type checking, invalid equals invocation");
            var tup = (val as PrtNamedTupleValue);
            var tupValues = tup.value;
            Debug.Assert(tup.value.Count == this.value.Count, "Error in type checking, tuple sizes not equal");

            int index = 0;

            while (index < tupValues.Count)
            {
                if (!this.value[index].IsEqual(tupValues[index]))
                    return false;

                index++;
            }
            return true;
        }

        public override string GetString()
        {
            string retStr = "<";
            int index = 0;

            while (index < value.Count)
            {
                retStr += fieldNames[index] + ":" + value[index].GetString() + ", ";
                index++;
            }
            retStr += ">";
            return retStr;
        }
    }

    public class PrtSeqValue : PrtValue
    {
        public List<PrtValue> value;

        public PrtSeqValue()
        {
            value = new List<PrtValue>();
        }

        public override PrtValue Clone()
        {
            var clone = new PrtSeqValue();
            foreach (var val in value)
            {
                clone.value.Add(val.Clone());
            }

            return clone;
        }

        public void Insert(int index, PrtValue val)
        {
            //TODO: raise an exception for invalid index
            value.Insert(index, val.Clone());
        }

        public void Remove(int index)
        {
            //TODO: raise an exception for invalid index
            value.RemoveAt(index);
        }

        public int SizeOf()
        {
            return value.Count();
        }

        public override bool IsEqual(PrtValue val)
        {
            Debug.Assert(val is PrtSeqValue, "Error in type checking, invalid equals invocation");
            var seqVal = val as PrtSeqValue;
            if (seqVal.value.Count != this.value.Count)
            {
                return false;
            }
            else
            {
                int index = 0;
                while (index < this.value.Count)
                {
                    if (!this.value[index].IsEqual(seqVal.value[index]))
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

            while (index < value.Count)
            {
                retStr += value[index] + ", ";
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