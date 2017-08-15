using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc.TypeChecker
{
    public class PTypeUniverse
    {
        private readonly Dictionary<string, PrimitiveType> baseTypes =
            new Dictionary<string, PrimitiveType>
            {
                ["NULL"] = PrimitiveType.Null,
                ["BOOL"] = PrimitiveType.Bool,
                ["INT"] = PrimitiveType.Int,
                ["EVENT"] = PrimitiveType.Event,
                ["MACHINE"] = PrimitiveType.Machine
            };

        private readonly Dictionary<EventSet, BoundedType> boundedTypes = new Dictionary<EventSet, BoundedType>();
        private readonly Dictionary<string, MapType> maps = new Dictionary<string, MapType>();
        private readonly Dictionary<string, NamedTupleType> namedTuples = new Dictionary<string, NamedTupleType>();
        private readonly Dictionary<string, SequenceType> sequences = new Dictionary<string, SequenceType>();
        private readonly Dictionary<string, TupleType> tuples = new Dictionary<string, TupleType>();

        internal IEnumerable<PLanguageType> AllTypes => baseTypes
            .Values.Cast<PLanguageType>().Concat(namedTuples.Values).Concat(sequences.Values);

        internal PLanguageType FromFormulaTerm(FuncTerm type)
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
                        fields.Add(new TypedName {Name = ((Cnst) args[0]).GetStringValue(), Type = FromFormulaTerm((FuncTerm) args[1])});

                        // Advance to the next FuncTerm (terminated by IdTerm)
                        curTerm = curTerm.Args.ElementAt(1) as FuncTerm;
                    } while (curTerm != null);

                    return GetOrCreateNamedTupleType(fields.ToArray());
                case "SeqType":
                    PLanguageType elementType = FromFormulaTerm(type.Args.ElementAt(0) as FuncTerm);
                    return GetOrCreateSeqType(elementType);
                case null: throw new Exception("Invalid PType passed");
                default: throw new ArgumentOutOfRangeException(nameof(type), $"{caseType} not yet implemented");
            }
        }

        public PLanguageType GetOrCreateBoundedType(EventSet eventSet)
        {
            return boundedTypes.GetOrCreate(eventSet, () => new BoundedType($"BoundedAny{boundedTypes.Count + 1}", eventSet));
        }

        public SequenceType GetOrCreateSeqType(PLanguageType elementType)
        {
            return sequences.GetOrCreate(
                elementType.OriginalRepresentation,
                () => new SequenceType($"Sequence{sequences.Count + 1}", elementType));
        }

        public PLanguageType GetOrCreateTupleType(PLanguageType[] types)
        {
            string representation = $"({string.Join(",", types.Select(ty => ty.OriginalRepresentation))})";
            return tuples.GetOrCreate(representation, () => new TupleType($"Tuple{tuples.Count + 1}", types));
        }

        public PLanguageType GetOrCreateMapType(PLanguageType keyType, PLanguageType valueType)
        {
            string representation = $"map[{keyType.OriginalRepresentation},{valueType.OriginalRepresentation}]";
            return maps.GetOrCreate(representation, () => new MapType($"Map{maps.Count + 1}", keyType, valueType));
        }

        public PLanguageType GetOrCreateNamedTupleType(TypedName[] fields)
        {
            string namedTupleRepr = $"({string.Join(",", fields.Select(v => $"{v.Name}:{v.Type.OriginalRepresentation}"))})";
            return namedTuples.GetOrCreate(
                namedTupleRepr,
                () => new NamedTupleType($"NamedTuple{namedTuples.Count + 1}", fields, namedTupleRepr));
        }
    }
}