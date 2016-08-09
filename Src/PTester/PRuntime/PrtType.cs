using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P.PRuntime
{
    public enum PrtTypeKind : int
    {
        PRT_KIND_ANY,
        PRT_KIND_BOOL,
        PRT_KIND_EVENT,
        PRT_KIND_REAL,
        PRT_KIND_INT,
        PRT_KIND_MAP,
        PRT_KIND_NMDTUP,
        PRT_KIND_NULL,
        PRT_KIND_SEQ,
        PRT_KIND_TUPLE,
    };

    public class PrtType
    {
        public PrtTypeKind typeKind;
        public int typeTag;
        public int arity;
        public List<string> fieldNames;
        public List<PrtType> fieldTypes;
        public PrtType innerType;
        public PrtType domType;
        public PrtType codType;

        public static PrtType NullType = PrtMkPrimitiveType(PrtTypeKind.PRT_KIND_NULL);

        public static PrtType BuildDefault(PrtTypeKind typeKind)
        {
            PrtType type = new PrtType();
            type.typeKind = typeKind;
            return type;
        }

        public static PrtType PrtMkPrimitiveType(PrtTypeKind primType)
        {
            PrtType type = PrtType.BuildDefault(primType);
            return type;
        }

        public static PrtType PrtMkMapType(PrtType domType, PrtType codType)
        {
            PrtType type = PrtType.BuildDefault(PrtTypeKind.PRT_KIND_MAP);
            type.domType = domType;
            type.codType = codType;
            return type;
        }

        public static PrtType PrtMkNmdTupType(int arity)
        {
            PrtType type = PrtType.BuildDefault(PrtTypeKind.PRT_KIND_NMDTUP);
            type.arity = arity;
            type.fieldNames = new List<string>(arity);
            type.fieldTypes = new List<PrtType>(arity);
            return type;
        }

        public static PrtType PrtMkSeqType(PrtType innerType)
        {
            PrtType type = PrtType.BuildDefault(PrtTypeKind.PRT_KIND_SEQ);
            type.innerType = innerType;
            return type;
        }

        public static PrtType PrtMkTupType(int arity)
        {
            PrtType type = PrtType.BuildDefault(PrtTypeKind.PRT_KIND_TUPLE);
            type.arity = arity;
            type.fieldTypes = new List<PrtType>(arity);
            return type;
        }
        public static void PrtSetFieldType(PrtType tupleType, int index, PrtType fieldType)
        {
            (tupleType.fieldTypes).Insert(index, fieldType);
        }

        public static void PrtSetFieldName(PrtType tupleType, int index, string fieldName)
        {
            (tupleType.fieldNames).Insert(index, fieldName);
        }
    };
}
