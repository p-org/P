using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PChecker.PRuntime
{
    public abstract class PType
    {
        public override string ToString()
        {
            throw new NotImplementedException("ToString function is not overriden in the derived class");
        }
    }

    public class PNullType : PType
    {
        public override string ToString()
        {
            return "NULL";
        }
    }

    public class PAnyType : PType
    {
        public override string ToString()
        {
            return "ANY";
        }
    }

    public class PMachineType : PType
    {
        public override string ToString()
        {
            return "MACHINE";
        }
    }

    public class PIntType : PType
    {
        public override string ToString()
        {
            return "INT";
        }
    }

    public class PFloatType : PType
    {
        public override string ToString()
        {
            return "FLOAT";
        }
    }

    public class PEnumType : PType
    {
        public Dictionary<long, string> enumConstants;
        public string name;

        public PEnumType(string typeName, params object[] args)
        {
            name = typeName;
            enumConstants = new Dictionary<long, string>();
            var i = 0;
            while (i < args.Length)
            {
                var enumConstantName = (string)args[i];
                i++;
                var enumConstantValue = (int)args[i];
                i++;
                enumConstants[enumConstantValue] = enumConstantName;
            }
        }

        public string DefaultConstant => enumConstants[0];

        public override string ToString()
        {
            return name;
        }
    }

    public class PPermissionType : PType
    {
        public string name;
        public List<string> permissions;

        public PPermissionType(string name, IEnumerable<string> perm)
        {
            this.name = name;
            permissions = perm.ToList();
        }

        public override string ToString()
        {
            return name;
        }
    }

    public class PBoolType : PType
    {
        public override string ToString()
        {
            return "BOOL";
        }
    }

    public class PStringType : PType
    {
        public override string ToString()
        {
            return "STRING";
        }
    }

    public class PEventType : PType
    {
        public override string ToString()
        {
            return "EVENT";
        }
    }

    public class PMapType : PType
    {
        public PType keyType;
        public PType valType;

        public PMapType(PType k, PType v)
        {
            keyType = k;
            valType = v;
        }

        public override string ToString()
        {
            return $"({keyType} -> {valType})";
        }
    }

    public class PSeqType : PType
    {
        public PType elemType;

        public PSeqType(PType s)
        {
            elemType = s;
        }

        public override string ToString()
        {
            return $"seq[{elemType}]";
        }
    }

    public class PSetType : PType
    {
        public PType elemType;

        public PSetType(PType s)
        {
            elemType = s;
        }

        public override string ToString()
        {
            return $"set[{elemType}]";
        }
    }

    public class PTupleType : PType
    {
        public List<PType> fieldTypes;

        public PTupleType()
        {
            /*
               This constructor is added only to prevent the other constructor from being called
               when an instance of PNamedTupleType is created.
             */
        }

        public PTupleType(params PType[] fields)
        {
            Debug.Assert(fields.Any());
            fieldTypes = new List<PType>();
            foreach (var f in fields)
            {
                fieldTypes.Add(f);
            }
        }

        public override string ToString()
        {
            var retStr = "<";
            foreach (var f in fieldTypes)
            {
                retStr += f + ", ";
            }

            retStr += ">";
            return retStr;
        }
    }

    public class PNamedTupleType : PTupleType
    {
        public List<string> fieldNames;

        public PNamedTupleType(params object[] args)
        {
            Debug.Assert(args.Length > 0);
            fieldNames = new List<string>();
            fieldTypes = new List<PType>();

            var index = 0;
            while (index < args.Length)
            {
                fieldNames.Add((string)args[index]);
                index++;
                fieldTypes.Add((PType)args[index]);
                index++;
            }
        }

        public override string ToString()
        {
            var retStr = "<";
            var index = 0;
            while (index < fieldTypes.Count)
            {
                retStr += fieldNames[index] + ":" + fieldTypes[index] + ", ";
                index++;
            }

            retStr += ">";
            return retStr;
        }
    }
}