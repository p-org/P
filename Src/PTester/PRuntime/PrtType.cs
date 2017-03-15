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
    public class PrtEnumType : PrtType
    {
        public string name;
        public Dictionary<int, string> enumConstants;
        public PrtEnumType(string typeName, params object[] args)
        {
            name = typeName;
            enumConstants = new Dictionary<int, string>();
            int i = 0;
            while (i < args.Count())
            {
                string enumConstantName = (string)args[i];
                i++;
                int enumConstantValue = (int)args[i];
                i++;
                enumConstants[enumConstantValue] = enumConstantName;
            }
        }
        public override string ToString()
        {
            return name;
        }
        public string DefaultConstant
        {
            get
            {
                return enumConstants[0];
            }
        }
    }

    public class PrtInterfaceType: PrtType
    {
        public List<PrtEventValue> permissions;
        public string name;
        public PrtInterfaceType(string name)
        {
            this.name = name;
        }

        public override string ToString()
        {
            return name;
        }

        public PrtValue PrtReduceValue(PrtValue value)
        {
            if(value == PrtValue.@null)
            {
                return value.Clone();
            }
            else if(value is PrtMachineValue)
            {
                return new PrtInterfaceValue(((PrtMachineValue)value).mach, permissions);
            }
            else if(value is PrtInterfaceValue)
            {
                var iVal = value as PrtInterfaceValue;
                //type_permissions is subset of value_permissions
                if(permissions.Where(ev => !iVal.permissions.Contains(ev)).Count() > 0)
                {
                    throw new PrtInhabitsTypeException(String.Format("value {0} cannot be reduced to value of type {1}", value.ToString(), this.ToString()));
                }
                else
                {
                    return new PrtInterfaceValue(iVal.mach, permissions);
                }
            }
            else
            {
                throw new PrtInhabitsTypeException(String.Format("value {0} cannot be reduced to value of type {1}", value.ToString(), this.ToString()));
            }
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
                index++;
            }
            retStr += ">";
            return retStr;
        }
    }
}
