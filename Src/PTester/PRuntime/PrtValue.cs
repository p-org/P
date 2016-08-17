using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System;

namespace P.Runtime
{
    public abstract class PrtValue
    {

        public abstract PrtValue Clone();

        public static PrtValue PrtMkDefaultValue(PrtType type)
        {
            throw new NotImplementedException();
        }

        public abstract bool IsEqual(PrtValue value);

        public static bool PrtInhabitsType(PrtValue value, PrtType type)
        {
            // Shaz: can we represent null value using c# "null" ?
            if (type is PrtAnyType)
                return true;

            if(type is PrtIntType)
            {
                return value is PrtIntValue;
            }
            else if(type is PrtBoolType)
            {
                return value is PrtBoolValue;
            }
            else if (type is PrtEventType)
            {
                return value == null || value is PrtEventValue;
            }
            else if(type is PrtMachineType)
            {
                return value == null || value is PrtMachineValue;
            }
            else if(type is PrtTupleType)
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
            else if(type is PrtNamedTupleType)
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
            else if(type is PrtMapType)
            {

            }


        }

        public static PrtValue PrtCastValue(PrtValue value, PrtType type)
        {
            Debug.Assert(PrtInhabitsType(value, type), "value must be a member of type");
            return value.Clone();
        }
    }

    public class PrtAnyValue : PrtValue
    {
        public override PrtValue Clone()
        {
            return this;
        }

        public override bool IsEqual(PrtValue value)
        {
            throw new PrtInvalidEqualsException("Equals operation performed on an uninitialized value");
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
    }

    public class PrtEventValue : PrtValue
    {
        public PrtEvent value;

        public PrtEventValue()
        {
            value = PrtEvent.NullEvent;
        }

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
            if (val == null)
            {
                return value == PrtEvent.NullEvent;
            }
            Debug.Assert(val is PrtEventValue, "Error in type checking, invalid equals invocation");
            return this.value == (val as PrtEventValue).value;
        }
    }

    public class PrtMachineValue : PrtValue
    {
        public PrtMachine value;

        public PrtMachineValue()
        {
            value = null;
        }

        public PrtMachineValue(PrtMachine mach)
        {
            this.value = mach;
        }

        public override PrtValue Clone()
        {
            return new PrtMachineValue(this.value);
        }

        public override bool IsEqual(PrtValue val)
        {
            if (val == null)
            {
                return value == null;
            }
            Debug.Assert(val is PrtMachineValue, "Error in type checking, invalid equals invocation");
            return this.value == (val as PrtMachineValue).value;
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
            //implement this function.
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
            //implement this function.
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
                        if (!this.values[index].IsEqual(mapVal.values[_index]))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

        }
    }
}    