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
            PrtTypeKind tkind = type.typeKind;
            PrtTypeKind vkind = value.type.typeKind;
            bool isNullValue = PrtIsNullValue(value);

            if (tkind == PrtTypeKind.PRT_KIND_ANY)
                return true;

            if (tkind == PrtTypeKind.PRT_KIND_BOOL)
                return vkind == PrtTypeKind.PRT_KIND_BOOL;

            if (tkind == PrtTypeKind.PRT_KIND_EVENT)
                return (vkind == PrtTypeKind.PRT_KIND_EVENT || isNullValue);

            if (tkind == PrtTypeKind.PRT_KIND_REAL)
                return (vkind == PrtTypeKind.PRT_KIND_REAL || isNullValue);

            if (tkind == PrtTypeKind.PRT_KIND_INT)
                return vkind == PrtTypeKind.PRT_KIND_INT;

            if (tkind == PrtTypeKind.PRT_KIND_MAP)
            {
                if (vkind != PrtTypeKind.PRT_KIND_MAP)
                {
                    return false;
                }
                return value.map.InhabitsType(type);
            }

            if (tkind == PrtTypeKind.PRT_KIND_NMDTUP)
            {
                if (vkind != PrtTypeKind.PRT_KIND_NMDTUP)
                {
                    return false;
                }

                if (type.arity != value.type.arity)
                {
                    return false;
                }

                for (int i = 0; i < type.arity; i++)
                {
                    if ((type.fieldNames)[i] != (value.type.fieldNames)[i])
                    {
                        return false;
                    }
                    if (!PrtInhabitsType((value.tuple)[i], (type.fieldTypes)[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            if (tkind == PrtTypeKind.PRT_KIND_TUPLE)
            {
                if (vkind != PrtTypeKind.PRT_KIND_TUPLE)
                {
                    return false;
                }

                if (type.arity != value.type.arity)
                {
                    return false;
                }

                for (int i = 0; i < type.arity; i++)
                {
                    if (!PrtInhabitsType((value.tuple)[i], (type.fieldTypes)[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            if (tkind == PrtTypeKind.PRT_KIND_SEQ)
            {
                if (vkind != PrtTypeKind.PRT_KIND_SEQ)
                {
                    return false;
                }
                return value.seq.InhabitsType(type);
            }
            //TODO: tracing
            //trace("Invalid tkind value : {0}", tkind);
            Debug.Assert(false);
            return false;
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
            if(val == null)
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
            if(val == null)
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
            foreach(var val in value)
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
            while(index < value.Count)
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
        public PrtNamedTupleValue(PrtNamedTupType tupType)
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
            foreach(var val in value)
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
            foreach(var k in keys)
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
            if(Contains(key))
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
    }
       
        


        public static void PrtPrimSetBool(PrtValue prmVal, bool value)
        {
            prmVal.bl = value;
        }

        public static bool PrtPrimGetBool(PrtValue prmVal)
        {
            return prmVal.bl;
        }

        public static void PrtPrimSetEvent(PrtValue prmVal, PrtEvent value)
        {
            prmVal.ev = value;
        }

        public static PrtEvent PrtPrimGetEvent(PrtValue prmVal)
        {
            return prmVal.ev;
        }

        static void PrtPrimSetInt(PrtValue prmVal, int value)
        {
            prmVal.nt = value;
        }

        static int PrtPrimGetInt(PrtValue prmVal)
        {
            return prmVal.nt;
        }

        static void PrtPrimSetMachine(PrtValue prmVal, PrtMachine value)
        {
            prmVal.mach = value;
        }

        static PrtMachine PrtPrimGetMachine(PrtValue prmVal)
        {
            return prmVal.mach;
        }

        static void PrtTupleSet(PrtValue tuple, int index, PrtValue value)
        {
            (tuple.tuple)[index] = PrtCloneValue(value);
        }

        static PrtValue PrtTupleGet(PrtValue tuple, int index)
        {
            return PrtValue.PrtCloneValue((tuple.tuple)[index]);
        }

        static void PrtSeqSet(PrtValue seq, PrtValue index, PrtValue value)
        {
            seq.seq.Set(index.nt, value);
        }

        static void PrtSeqInsert(PrtValue seq, PrtValue index, PrtValue value)
        {
            seq.seq.Insert(index.nt, value);
        }

        static void PrtSeqRemove(PrtValue seq, PrtValue index)
        {
            seq.seq.Remove(index.nt);
        }

        static PrtValue PrtSeqGet(PrtValue seq, PrtValue index)
        {
            return seq.seq.Get(index.nt);
        }

        static PrtValue PrtSeqGetNoClone(PrtValue seq, PrtValue index)
        {
            return seq.seq.GetNoClone(index.nt);
        }

        static int PrtSeqSizeOf(PrtValue seq)
        {
            return seq.seq.SizeOf();
        }

        static void PrtMapSet(PrtValue map, PrtValue key, PrtValue value)
        {
            map.map.Set(key, value);
        }

        static void PrtMapInsert(PrtValue map, PrtValue key, PrtValue value)
        {
            Debug.Assert(!map.map.Exists(key), "key must not exist in map");
            map.map.Set(key, value);
        }

        static void PrtMapRemove(PrtValue map, PrtValue key)
        {
            map.map.Remove(key);
        }

        static PrtValue PrtMapGet(PrtValue map, PrtValue key)
        {
            return map.map.Get(key);
        }

        static PrtValue PrtMapGetNoClone(PrtValue map, PrtValue key)
        {
            return map.map.GetNoClone(key);
        }

        static PrtValue PrtMapGetKeys(PrtValue map)
        {
            return map.map.GetKeys(map.type.domType);
        }

        static PrtValue PrtMapGetValues(PrtValue map)
        {
            return map.map.GetValues(map.type.codType);
        }

        static bool PrtMapExists(PrtValue map, PrtValue key)
        {
            return map.map.Exists(key);
        }

        static int PrtMapSizeOf(PrtValue map)
        {
            return map.map.SizeOf();
        }
    };


    