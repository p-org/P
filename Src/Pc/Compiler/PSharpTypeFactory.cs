using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc
{
    internal class PSharpSeqType : PSharpType
    {
        public PSharpSeqType(string name, PSharpType elementType) : base(
            name,
            PTypeKind.NamedTuple,
            $"seq<{elementType.OriginalRepresentation}>")
        {
            ElementType = elementType;
        }

        public PSharpType ElementType { get; set; }
    }

    internal class PSharpNamedTuple : PSharpType
    {
        public PSharpNamedTuple(string name, IReadOnlyList<TypedName> fields, string namedTupleRepr) : base(name, PTypeKind.NamedTuple, namedTupleRepr)
        {
            this.Fields = fields;
        }

        public IEnumerable<PSharpType> Types => Fields.Select(f => f.Type);
        public IEnumerable<string> Names => Fields.Select(f => f.Name);
        public IReadOnlyList<TypedName> Fields { get; }
    }

    internal class PSharpBaseType : PSharpType
    {
        public static PSharpBaseType Machine = new PSharpBaseType("machine", "machine");
        public static PSharpBaseType Event = new PSharpBaseType("event", "event");
        public static PSharpBaseType Int = new PSharpBaseType("int", "int");
        public static PSharpBaseType Bool = new PSharpBaseType("bool", "bool");
        public static PSharpBaseType Null = new PSharpBaseType("null", "null");

        private PSharpBaseType(string name, string repr) : base(name, PTypeKind.Base, repr) { }
    }

    internal class PSharpType
    {
        public PSharpType(string name, PTypeKind kind, string repr)
        {
            TypeName = name;
            TypeKind = kind;
            OriginalRepresentation = repr;
        }

        /// <summary>
        ///     Unique name for the type, optional to use in generated code.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        ///     The category of type this is (eg. sequence, map, base)
        /// </summary>
        public PTypeKind TypeKind { get; set; }

        /// <summary>
        ///     Original representation of the type in P.
        /// </summary>
        public string OriginalRepresentation { get; set; }
    }

    internal class PTypeKind
    {
        public static readonly PTypeKind Base = new PTypeKind("base");
        public static readonly PTypeKind Sequence = new PTypeKind("sequence");
        public static readonly PTypeKind Map = new PTypeKind("map");
        public static readonly PTypeKind Tuple = new PTypeKind("tuple");
        public static readonly PTypeKind NamedTuple = new PTypeKind("namedtuple");

        private PTypeKind(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }

    internal class PSharpTypeFactory
    {
        private readonly Dictionary<string, PSharpBaseType> baseTypes =
            new Dictionary<string, PSharpBaseType>
            {
                ["NULL"] = PSharpBaseType.Null,
                ["BOOL"] = PSharpBaseType.Bool,
                ["INT"] = PSharpBaseType.Int,
                ["EVENT"] = PSharpBaseType.Event,
                ["MACHINE"] = PSharpBaseType.Machine
            };

        private readonly Dictionary<string, PSharpNamedTuple> namedTuples = new Dictionary<string, PSharpNamedTuple>();
        private readonly Dictionary<string, PSharpSeqType> sequences = new Dictionary<string, PSharpSeqType>();

        internal PSharpType MakePSharpType(FuncTerm type)
        {
            string caseType = (type.Function as Id)?.Name;
            switch (caseType)
            {
                case "BaseType":
                    string actualType = ((Id) type.Args.First()).Name;
                    return baseTypes[actualType];
                case "NmdTupType":
                    var fields = new List<TypedName>();
                    FuncTerm curTerm = type;
                    do
                    {
                        // Get the NmdTupTypeField out
                        var field = (FuncTerm) curTerm.Args.ElementAt(0);
                        Node[] args = field.Args.ToArray();
                        fields.Add(new TypedName {Name = ((Cnst) args[0]).GetStringValue(), Type = MakePSharpType((FuncTerm) args[1])});

                        // Advance to the next FuncTerm (terminated by IdTerm)
                        curTerm = curTerm.Args.ElementAt(1) as FuncTerm;
                    } while (curTerm != null);

                    string namedTupleRepr = $"({string.Join(",", fields.Select(v => $"{v.Name}:{v.Type.OriginalRepresentation}"))})";
                    PSharpNamedTuple pSharpType = namedTuples.GetOrCreate(
                        namedTupleRepr,
                        () => new PSharpNamedTuple($"NamedTuple{namedTuples.Count + 1}", fields, namedTupleRepr));
                    return pSharpType;
                case "SeqType":
                    PSharpType elementType = MakePSharpType(type.Args.ElementAt(0) as FuncTerm);
                    return sequences.GetOrCreate(
                        elementType.OriginalRepresentation,
                        () => new PSharpSeqType($"Sequence{sequences.Count + 1}", elementType));
                case null: throw new Exception("Invalid PType passed");
                default: throw new ArgumentOutOfRangeException(nameof(type), $"{caseType} not yet implemented");
            }
        }

        internal IEnumerable<PSharpType> AllTypes => baseTypes.Values.Cast<PSharpType>().Concat(namedTuples.Values).Concat(sequences.Values);
    }

    internal static class DictionaryExtensions
    {
        internal static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueFactory)
        {
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
            {
                value = valueFactory();
                dictionary.Add(key, value);
            }
            return value;
        }
    }
}