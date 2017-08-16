using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Pc.TypeChecker
{
    public class PTypeUniverse
    {
        private readonly Dictionary<string, PrimitiveType> baseTypes =
            new Dictionary<string, PrimitiveType>
            {
                ["bool"] = PrimitiveType.Bool,
                ["int"] = PrimitiveType.Int,
                ["float"] = PrimitiveType.Float,
                ["event"] = PrimitiveType.Event,
                ["machine"] = PrimitiveType.Machine,
                ["data"] = PrimitiveType.Data,
                ["any"] = PrimitiveType.Any
            };

        private readonly Dictionary<EventSet, BoundedType> boundedTypes = new Dictionary<EventSet, BoundedType>();
        private readonly Dictionary<string, MapType> maps = new Dictionary<string, MapType>();
        private readonly Dictionary<string, NamedTupleType> namedTuples = new Dictionary<string, NamedTupleType>();
        private readonly Dictionary<string, SequenceType> sequences = new Dictionary<string, SequenceType>();
        private readonly Dictionary<string, TupleType> tuples = new Dictionary<string, TupleType>();

        internal IEnumerable<PLanguageType> AllTypes => baseTypes
            .Values.Cast<PLanguageType>().Concat(namedTuples.Values).Concat(sequences.Values);

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

        public PLanguageType GetPrimitiveType(string typeName)
        {
            if (baseTypes.TryGetValue(typeName, out var type))
            {
                return type;
            }

            throw new ArgumentException("INTERNAL ERROR: Unrecognized primitive type!", nameof(typeName));
        }
    }
}