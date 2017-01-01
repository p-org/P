using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace P.Runtime
{
    public abstract class PrtType
    {
        public override string ToString()
        {
            throw new NotImplementedException("ToString function is not overriden in the derived class");
        }
    }

    public class PrtNullType : PrtType
    {
        public override string ToString()
        {
            return "NULL";
        }
    }
    public class PrtAnyType : PrtType
    {
        public override string ToString()
        {
            return "ANY";
        }
    }
    public class PrtMachineType : PrtType
    {
        public override string ToString()
        {
            return "MACHINE";
        }
    }
    public class PrtIntType : PrtType
    {
        public override string ToString()
        {
            return "INT";
        }
    }
    public class PrtBoolType : PrtType
    {
        public override string ToString()
        {
            return "BOOL";
        }
    }
    public class PrtEventType : PrtType
    {
        public override string ToString()
        {
            return "EVENT";
        }
    }
    public class PrtUninterpretedEnumType : PrtType
    {
        public string name;
        public HashSet<string> enumConstants;
        public string defaultValue;

        public PrtUninterpretedEnumType(string typeName, params string[] args)
        {
            name = typeName;
            enumConstants = new HashSet<string>(args);
            defaultValue = args[0];
        }
        public override string ToString()
        {
            return name;
        }
    }
    public class PrtInterpretedEnumType : PrtType
    {
        public string name;
        public HashSet<int> enumConstants;
        public int defaultValue;

        public PrtInterpretedEnumType(string typeName, params int[] args)
        {
            name = typeName;
            enumConstants = new HashSet<int>(args);
            defaultValue = args[0];
        }
        public override string ToString()
        {
            return name;
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

        public override string ToString()
        {
            return String.Format("({0} -> {1})", keyType.ToString(), valType.ToString());
        }
    }

    public class PrtSeqType : PrtType
    {
        public PrtType elemType;

        public PrtSeqType(PrtType s)
        {
            this.elemType = s;
        }

        public override string ToString()
        {
            return String.Format("seq[{0}]", elemType.ToString());
        }
    }


    public class PrtTupleType : PrtType
    {
        public List<PrtType> fieldTypes;

        public PrtTupleType() 
        {
            /*
               This constructor is added only to prevent the other constructor from being called
               when an instance of PrtNamedTupleType is created.
             */
        }

        public PrtTupleType(params PrtType[] fields)
        {
            Debug.Assert(fields.Count() > 0);
            this.fieldTypes = new List<PrtType>();
            foreach(var f in fields)
            {
                fieldTypes.Add(f);
            }
        }

        public override string ToString()
        {
            string retStr = "<";
            foreach (var f in fieldTypes)
            {
                retStr += f.ToString() + ", ";
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

        public override string ToString()
        {
            string retStr = "<";
            int index = 0;
            while (index < fieldTypes.Count)
            {
                retStr += fieldNames[index] + ":" + fieldTypes[index].ToString() + ", ";
            }
            retStr += ">";
            return retStr;
        }
    }
}
