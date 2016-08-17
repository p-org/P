using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace P.Runtime
{
    public abstract class PrtType
    {
    }

    public class PrtAnyType : PrtType
    {

    }
    public class PrtMachineType : PrtType
    {

    }
    public class PrtIntType : PrtType
    {

    }
    public class PrtBoolType : PrtType
    {

    }
    public class PrtEventType : PrtType
    {
    }
    public class PrtMapType : PrtType
    {
        public PrtType keyType;
        public PrtType valType;

        public PrtMapType(PrtType k, PrtType v)
        {
            this.keyType = k;
            this.valType = v;
        }
    }

    public class PrtSeqType : PrtType
    {
        public PrtType seqType;

        public PrtSeqType(PrtType s)
        {
            this.seqType = s;
        }
    }


    public class PrtTupleType : PrtType
    {
        public List<PrtType> fieldTypes;

        public PrtTupleType(params PrtType[] fields)
        {
            Debug.Assert(fields.Count() > 0);
            this.fieldTypes = new List<PrtType>();
            foreach(var f in fields)
            {
                fieldTypes.Add(f);
            }
        }
    }


    public class PrtNamedTupleType : PrtType
    {
        public List<string> fieldNames;
        public List<PrtType> fieldTypes;

        public PrtNamedTupleType(params object[] args)
        {
            Debug.Assert(args.Count() > 0);
            fieldNames = new List<string>();
            fieldTypes = new List<PrtType>();

            int index = 0;
            while(index < args.Count())
            {
                fieldNames.Add((string)args[index]);
                index++;
                fieldTypes.Add((PrtType)args[index]);
                index++;
            }
        }
    }

}
