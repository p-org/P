using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace P.Runtime
{
    public abstract class PrtType
    {
        public abstract string GetString();
    }

    public class PrtNullType : PrtType
    {
        public override string GetString()
        {
            return "NULL";
        }
    }
    public class PrtAnyType : PrtType
    {
        public override string GetString()
        {
            return "ANY";
        }
    }
    public class PrtMachineType : PrtType
    {
        public override string GetString()
        {
            return "MACHINE";
        }
    }
    public class PrtIntType : PrtType
    {
        public override string GetString()
        {
            return "INT";
        }
    }
    public class PrtBoolType : PrtType
    {
        public override string GetString()
        {
            return "BOOL";
        }
    }
    public class PrtEventType : PrtType
    {
        public override string GetString()
        {
            return "EVENT";
        }
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

        public override string GetString()
        {
            return String.Format("({0} -> {1})", keyType.GetString(), valType.GetString());
        }
    }

    public class PrtSeqType : PrtType
    {
        public PrtType elemType;

        public PrtSeqType(PrtType s)
        {
            this.elemType = s;
        }

        public override string GetString()
        {
            return String.Format("seq[{0}]", elemType.GetString());
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

        public override string GetString()
        {
            string retStr = "<";
            foreach (var f in fieldTypes)
            {
                retStr += f.GetString() + ", ";
            }
            retStr += ">";
            return retStr;
        }
    }

    public class PrtNamedTupleType : PrtTupleType
    {
        public List<string> fieldNames;

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

        public override string GetString()
        {
            string retStr = "<";
            int index = 0;
            while (index < fieldTypes.Count)
            {
                retStr += fieldNames[index] + ":" + fieldTypes[index].GetString() + ", ";
            }
            retStr += ">";
            return retStr;
        }
    }
}
