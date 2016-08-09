using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace P.PRuntime
{
    public class PrtValue
    {
        public static PrtValue NullValue = PrtMkDefaultValue(PrtType.NullType);

        public PrtType type;
        public bool bl;
        public Event ev;
        public int nt;
        public BaseMachine mach;
        public List<PrtValue> tuple;
        public PrtSeq seq;
        public PrtMap map;

        //TODO: Print method is skipped

        public static PrtValue PrtMkDefaultValue(PrtType type)
        {
            PrtValue value = new PrtValue();
            value.type = type;

            if (type.typeKind == PrtTypeKind.PRT_KIND_ANY)
            {
                value.type = PrtType.PrtMkPrimitiveType(PrtTypeKind.PRT_KIND_NULL);
            }
            else if (type.typeKind == PrtTypeKind.PRT_KIND_TUPLE ||
                     type.typeKind == PrtTypeKind.PRT_KIND_NMDTUP)
            {
                value.tuple = new List<PrtValue>(type.arity);
                var fieldTypesArray = (type.fieldTypes).ToArray();

                for (int i = 0; i <= type.arity; i++)
                {
                    value.tuple.Add(PrtMkDefaultValue((type.fieldTypes)[i]));
                }
            }
            else if (type.typeKind == PrtTypeKind.PRT_KIND_SEQ)
            {
                value.seq = PrtSeq.PrtMkDefaultSeq();
            }
            else if (type.typeKind == PrtTypeKind.PRT_KIND_MAP)
            {
                value.map = PrtMap.PrtMkDefaultMap();
            }
            return value;
        }

        public static PrtValue PrtCloneValue(PrtValue value)
        {
            PrtValue newValue = new PrtValue();
            newValue.type = value.type;
            newValue.bl = value.bl;
            newValue.ev = value.ev;
            newValue.nt = value.nt;
            newValue.mach = value.mach;
            newValue.tuple = value.tuple;
            newValue.seq = value.seq;
            newValue.map = value.map;

            if (value.type.typeKind == PrtTypeKind.PRT_KIND_TUPLE ||
                value.type.typeKind == PrtTypeKind.PRT_KIND_NMDTUP)
            {
                newValue.tuple = new List<PrtValue>(value.type.arity);
                foreach (PrtValue elem in value.tuple)
                {
                    (newValue.tuple).Add(PrtCloneValue(elem));
                }
            }
            else if (value.type.typeKind == PrtTypeKind.PRT_KIND_SEQ)
            {
                newValue.seq = value.seq.Clone();
            }
            else if (value.type.typeKind == PrtTypeKind.PRT_KIND_MAP)
            {
                newValue.map = value.map.Clone();
            }
            return newValue;
        }

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

        public static bool PrtIsNullValue(PrtValue value)
        {
            PrtTypeKind kind = value.type.typeKind;
            Debug.Assert(kind != PrtTypeKind.PRT_KIND_ANY, "Value must have a concrete type");

            if (kind == PrtTypeKind.PRT_KIND_EVENT)
                return value.ev == null;

            if (kind == PrtTypeKind.PRT_KIND_REAL)
                return value.mach == null;

            if (kind == PrtTypeKind.PRT_KIND_NULL)
                return true;

            return false;
        }

        public static PrtValue PrtCastValue(PrtValue value, PrtType type)
        {
            Debug.Assert(PrtInhabitsType(value, type), "value must be a member of type");
            value.type = type;
            return PrtCloneValue(value);
        }

        public static bool PrtIsEqualValue(PrtValue value1, PrtValue value2)
        {
            PrtType type1 = value1.type;
            PrtType type2 = value2.type;
            PrtTypeKind kind1 = type1.typeKind;
            PrtTypeKind kind2 = type2.typeKind;
            bool isNullValue1 = PrtIsNullValue(value1);
            bool isNullValue2 = PrtIsNullValue(value2);

            if (isNullValue1 && isNullValue2)
            {
                return true;
            }
            else if (kind1 != kind2)
            {
                return false;
            }
            else if (value1 == value2)
            {
                return true;
            }

            if (kind1 == PrtTypeKind.PRT_KIND_BOOL)
                return value1.bl == value2.bl;

            if (kind1 == PrtTypeKind.PRT_KIND_EVENT)
                return value1.ev == value2.ev;

            if (kind1 == PrtTypeKind.PRT_KIND_REAL)
                return value1.mach == value2.mach;

            if (kind1 == PrtTypeKind.PRT_KIND_INT)
                return value1.nt == value2.nt;

            if (kind1 == PrtTypeKind.PRT_KIND_MAP)
            {
                return value1.map.Equals(value2.map);
            }

            if (kind1 == PrtTypeKind.PRT_KIND_NMDTUP)
            {
                if (type1.arity != type2.arity)
                {
                    return false;
                }
                for (int i = 0; i < type1.arity; i++)
                {
                    if ((type1.fieldNames)[i] != (type2.fieldNames)[i])
                    {
                        return false;
                    }
                    if (!PrtIsEqualValue((value1.tuple)[i], (value2.tuple)[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            if (kind1 == PrtTypeKind.PRT_KIND_SEQ)
            {
                return value1.seq.Equals(value2.seq);
            }

            if (kind1 == PrtTypeKind.PRT_KIND_TUPLE)
            {
                if (type1.arity != type2.arity)
                {
                    return false;
                }
                for (int i = 0; i < type1.arity; i++)
                {
                    if (!PrtIsEqualValue((value1.tuple)[i], (value2.tuple)[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            Debug.Assert(false);
            return false;
        }

        public static void PrtPrimSetBool(PrtValue prmVal, bool value)
        {
            prmVal.bl = value;
        }

        public static bool PrtPrimGetBool(PrtValue prmVal)
        {
            return prmVal.bl;
        }

        public static void PrtPrimSetEvent(PrtValue prmVal, Event value)
        {
            prmVal.ev = value;
        }

        public static Event PrtPrimGetEvent(PrtValue prmVal)
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

        static void PrtPrimSetMachine(PrtValue prmVal, BaseMachine value)
        {
            prmVal.mach = value;
        }

        static BaseMachine PrtPrimGetMachine(PrtValue prmVal)
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

    public class PrtSeq
    {
        public int size;
        public List<PrtValue> contents;

        public static PrtSeq PrtMkDefaultSeq()
        {
            PrtSeq seq = new PrtSeq();
            seq.size = 0;
            seq.contents = new List<PrtValue>(0);
            return seq;
        }

        public PrtSeq Clone()
        {
            PrtSeq seq = new PrtSeq();
            seq.size = this.size;
            seq.contents = new List<PrtValue>(this.size);
            for (int i = 0; i < this.size; i++)
            {
                seq.contents[i] = PrtValue.PrtCloneValue(this.contents[i]);
            }
            return seq;
        }

        public void Set(int index, PrtValue value)
        {
            Debug.Assert(0 <= index && index < this.size, "index out of bound");
            this.contents[index] = PrtValue.PrtCloneValue(value);
        }

        public void Insert(int index, PrtValue value)
        {
            List<PrtValue> newContents = new List<PrtValue>(this.size + 1);
            Debug.Assert(0 <= index && index <= this.size, "index out of bound");
            for (int i = 0; i < this.size + 1; i++)
            {
                if (i < index)
                {
                    newContents[i] = (this.contents)[i];
                }
                else if (i == index)
                {
                    newContents[i] = PrtValue.PrtCloneValue(value);
                }
                else
                {
                    newContents[i] = (this.contents)[i - 1];
                }
            }
            this.contents = newContents;
            this.size = this.size + 1;
        }

        public void Remove(int index)
        {
            Debug.Assert(0 <= index && index < this.size, "index out of bound");
            for (int i = index; i < this.size - 1; i++)
            {
                (this.contents)[i] = (this.contents)[i + 1];
            }
            this.size = this.size - 1;
        }

        public PrtValue Get(int index)
        {
            Debug.Assert(0 <= index && index < this.size, "index out of bound");
            return PrtValue.PrtCloneValue((this.contents)[index]);
        }

        public PrtValue GetNoClone(int index)
        {
            Debug.Assert(0 <= index && index < this.size, "index out of bound");
            return (this.contents)[index];
        }

        public int SizeOf()
        {
            return this.size;
        }

        public bool InhabitsType(PrtType type)
        {
            for (int i = 0; i < this.size; i++)
            {
                if (!PrtValue.PrtInhabitsType((this.contents)[i], type.innerType))
                {
                    return false;
                }
            }
            return true;
        }

        public bool Equals(PrtSeq seq)
        {
            if (this.size != seq.size)
            {
                return false;
            }
            for (int i = 0; i < this.size; i++)
            {
                if (!PrtValue.PrtIsEqualValue((this.contents)[i], (seq.contents)[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class PrtMap
    {
        public int size;
        public List<PrtValue> keys;
        public List<PrtValue> values;

        public static PrtMap PrtMkDefaultMap()
        {
            PrtMap map = new PrtMap();
            map.size = 0;
            map.keys = new List<PrtValue>(0);
            map.values = new List<PrtValue>(0);
            return map;
        }

        public PrtMap Clone()
        {
            PrtMap map = new PrtMap();
            map.size = this.size;
            map.keys = new List<PrtValue>(this.size);
            map.values = new List<PrtValue>(this.size);
            for (int i = 0; i < this.size; i++)
            {
                map.keys[i] = PrtValue.PrtCloneValue(this.keys[i]);
                map.values[i] = PrtValue.PrtCloneValue(this.values[i]);
            }
            return map;
        }

        public void Set(PrtValue key, PrtValue value)
        {
            for (int i = 0; i < this.size; i++)
            {
                if (PrtValue.PrtIsEqualValue(this.keys[i], key))
                {
                    this.values[i] = PrtValue.PrtCloneValue(value);
                    return;
                }
            }

            List<PrtValue> newKeys = new List<PrtValue>(this.size + 1);
            List<PrtValue> newValues = new List<PrtValue>(this.size + 1);
            for (int i = 0; i < this.size; i++)
            {
                newKeys[i] = this.keys[i];
                newValues[i] = this.values[i];
            }
            newKeys[this.size] = PrtValue.PrtCloneValue(key);
            newValues[this.size] = PrtValue.PrtCloneValue(value);

            this.keys = newKeys;
            this.values = newValues;
            this.size = this.size + 1;
        }

        public void Remove(PrtValue key)
        {
            for (int i = 0; i < this.size; i++)
            {
                if (PrtValue.PrtIsEqualValue(this.keys[i], key))
                {
                    List<PrtValue> newKeys = new List<PrtValue>(this.size - 1);
                    List<PrtValue> newValues = new List<PrtValue>(this.size - 1);

                    for (int j = 0; j < this.size; j++)
                    {
                        if (j < i)
                        {
                            newKeys[i] = this.keys[i];
                            newValues[i] = this.values[i];
                        }
                        else if (j > i)
                        {
                            newKeys[j - 1] = this.keys[j];
                            newValues[j - 1] = this.values[j];
                        }
                    }

                    this.keys = newKeys;
                    this.values = newValues;
                    this.size = this.size - 1;
                    return;
                }
            }
            Debug.Assert(false, "key not found");
        }

        public PrtValue Get(PrtValue key)
        {
            for (int i = 0; i < this.size; i++)
            {
                if (PrtValue.PrtIsEqualValue(this.keys[i], key))
                {
                    return PrtValue.PrtCloneValue(this.values[i]);
                }
            }
            Debug.Assert(false, "key not found");
            return null;
        }

        public PrtValue GetNoClone(PrtValue key)
        {
            for (int i = 0; i < this.size; i++)
            {
                if (PrtValue.PrtIsEqualValue(this.keys[i], key))
                {
                    return this.values[i];
                }
            }
            Debug.Assert(false, "key not found");
            return null;
        }

        public PrtValue GetKeys(PrtType domType)
        {
            PrtSeq seq = new PrtSeq();
            seq.size = this.size;
            seq.contents = new List<PrtValue>(this.size);
            for (int i = 0; i < this.size; i++)
            {
                seq.contents[i] = PrtValue.PrtCloneValue(this.keys[i]);
            }
            PrtType seqType = PrtType.PrtMkSeqType(domType);
            PrtValue retVal = PrtValue.PrtMkDefaultValue(seqType);
            retVal.seq = seq;
            return retVal;
        }

        public PrtValue GetValues(PrtType codType)
        {
            PrtSeq seq = new PrtSeq();
            seq.size = this.size;
            seq.contents = new List<PrtValue>(this.size);
            for (int i = 0; i < this.size; i++)
            {
                seq.contents[i] = PrtValue.PrtCloneValue(this.values[i]);
            }
            PrtType seqType = PrtType.PrtMkSeqType(codType);
            PrtValue retVal = PrtValue.PrtMkDefaultValue(seqType);
            retVal.seq = seq;
            return retVal;
        }

        public bool Exists(PrtValue key)
        {
            for (int i = 0; i < this.size; i++)
            {
                if (PrtValue.PrtIsEqualValue(this.keys[i], key))
                {
                    return true;
                }
            }
            return false;
        }

        public int SizeOf()
        {
            return this.size;
        }

        public bool IsSameMapping(PrtValue key, PrtValue value)
        {
            for (int i = 0; i < this.size; i++)
            {
                if (PrtValue.PrtIsEqualValue(this.keys[i], key))
                {
                    return PrtValue.PrtIsEqualValue(this.values[i], value);
                }
            }
            return false;
        }

        public bool InhabitsType(PrtType type)
        {
            for (int i = 0; i < this.size; i++)
            {
                if (!PrtValue.PrtInhabitsType(this.keys[i], type.domType))
                {
                    return false;
                }
                if (!PrtValue.PrtInhabitsType(this.values[i], type.codType))
                {
                    return false;
                }
            }
            return true;
        }

        public bool Equals(PrtMap map)
        {
            if (this.size != map.size)
            {
                return false;
            }
            for (int i = 0; i < this.size; i++)
            {
                if (!map.IsSameMapping(this.keys[i], this.values[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
