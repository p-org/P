using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Antlr4.StringTemplate;
using Microsoft.Formula.API;
using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc
{
    internal class PToPSharpCompiler : PTranslation
    {
        private const string TemplatesNamespace = "Templates";
        private const string BaseNamespace = nameof(Microsoft) + "." + nameof(Pc) + "." + TemplatesNamespace;
        private const string TemplateFileName = "PSharp.stg";
        private const string PSharpResourceName = BaseNamespace + "." + TemplateFileName;
        private const string EventPayloadFieldName = "Payload";

        private readonly Lazy<TemplateGroup> pSharpTemplates = new Lazy<TemplateGroup>(
            () =>
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                Stream stream = asm.GetManifestResourceStream(PSharpResourceName);
                Debug.Assert(stream != null);
                using (var reader = new StreamReader(stream))
                {
                    return new TemplateGroupString(reader.ReadToEnd());
                }
            });

        public PToPSharpCompiler(
            Compiler compiler,
            AST<Model> model,
            Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo) : base(compiler, model, idToSourceInfo) { }

        public string GenerateCode()
        {
            TemplateGroup templateGroup = pSharpTemplates.Value;
            Template t = templateGroup.GetInstanceOf("topLevel");
            
            t.Add("pgm", new {Namespace = "Test", Events = GenerateEvents()});
            string generatedCode = t.Render();
            Console.WriteLine(generatedCode);
            return generatedCode;
        }

        private IEnumerable<EventDecl> GenerateEvents()
        {
            return allEvents.Select(
                kv =>
                {
                    PSharpType[] types;
                    string[] names;
                    TypeToEventArgumentsList(kv.Value.payloadType, out types, out names);
                    return new EventDecl
                    {
                        Name = kv.Key,
                        Assert = kv.Value.maxInstances == -1 || kv.Value.maxInstancesAssumed ? -1 : kv.Value.maxInstances,
                        Assume = kv.Value.maxInstances == -1 || !kv.Value.maxInstancesAssumed ? -1 : kv.Value.maxInstances,
                        PayloadTypes = types,
                        PayloadNames = names
                    };
                });
        }

        private PSharpType PTypeToPSharpType(FuncTerm type)
        {
            string caseType = (type.Function as Id)?.Name;
            switch (caseType)
            {
                case "BaseType":
                    string actualType = ((Id) type.Args.First()).Name;
                    switch (actualType)
                    {
                        case "NULL": return PSharpBaseType.Null;
                        case "BOOL": return PSharpBaseType.Bool;
                        case "INT": return PSharpBaseType.Int;
                        case "EVENT": return PSharpBaseType.Event;
                        case "MACHINE": return PSharpBaseType.Machine;
                    }

                    break;
                case "NmdTupType":
                    var names = new List<string>();
                    var types = new List<PSharpType>();
                    FuncTerm curTerm = type;
                    do
                    {
                        var field = (FuncTerm) curTerm.Args.ElementAt(0);
                        Node[] args = field.Args.ToArray();
                        names.Add(((Cnst) args[0]).GetStringValue());
                        types.Add(PTypeToPSharpType((FuncTerm) args[1]));

                        curTerm = curTerm.Args.ElementAt(1) as FuncTerm;
                    } while (curTerm != null);

                    return new PSharpNamedTuple {Types = types, Names = names};
                case "SeqType": return new PSharpSeqType {ItemType = PTypeToPSharpType(type.Args.ElementAt(0) as FuncTerm)};
                case null: throw new Exception("Invalid PType passed");
                default: throw new ArgumentOutOfRangeException(nameof(type), $"{caseType} not yet implemented");
            }

            return null;
        }

        private void TypeToEventArgumentsList(FuncTerm payloadType, out PSharpType[] types, out string[] names)
        {
            PSharpType type = PTypeToPSharpType(payloadType);
            // Null (unit) type results in no fields.
            if (type == PSharpBaseType.Null)
            {
                types = new PSharpType[] { };
                names = new string[] { };
                return;
            }

            // Named tuples become fields of events (saves an allocation)
            // other types are assigned to a single "Payload" field
            var namedTuple = type as PSharpNamedTuple;
            types = namedTuple?.Types.ToArray() ?? new[] {type};
            names = namedTuple?.Names.ToArray() ?? new[] {EventPayloadFieldName};
        }
    }

    internal class PSharpSeqType : PSharpType
    {
        public PSharpType ItemType { get; set; }

        public override string ToString()
        {
            return $"List<{ItemType}>";
        }
    }

    internal class PSharpNamedTuple : PSharpType
    {
        public IEnumerable<PSharpType> Types { get; set; }
        public IEnumerable<string> Names { get; set; }
    }

    internal class EventDecl
    {
        public string Name { get; set; }
        public int Assert { get; set; }
        public int Assume { get; set; }
        public IEnumerable<PSharpType> PayloadTypes { get; set; }
        public IEnumerable<string> PayloadNames { get; set; }
    }

    internal class PSharpType { }

    internal class PSharpBaseType : PSharpType
    {
        public static PSharpBaseType Machine = new PSharpBaseType("Machine");
        public static PSharpBaseType Event = new PSharpBaseType("Event");
        public static PSharpBaseType Int = new PSharpBaseType("int");
        public static PSharpBaseType Bool = new PSharpBaseType("bool");
        public static PSharpBaseType Null = new PSharpBaseType("PUnitType");

        private PSharpBaseType(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}