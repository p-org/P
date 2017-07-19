using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Antlr4.StringTemplate;
using Antlr4.StringTemplate.Visualizer.Extensions;
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
        private const string PTranslationIgnoredActionName = "ignore";
        private const int LineWidth = 100;

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

        private readonly PSharpTypeFactory typeFactory = new PSharpTypeFactory();

        public PToPSharpCompiler(
            Compiler compiler,
            AST<Model> model,
            Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo) : base(compiler, model, idToSourceInfo) { }

        public string GenerateCode()
        {
            TemplateGroup templateGroup = pSharpTemplates.Value;
            Template t = templateGroup.GetInstanceOf("topLevel");

            IEnumerable<EventDecl> events = GenerateEvents();
            IEnumerable<MachineDecl> machines = GenerateMachines();

            t.Add("pgm", new {Namespace = "Test", Events = events, Machines = machines});
#if DEBUG
            var thread = new Thread(() => t.Visualize(LineWidth));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
#endif
            string generatedCode = t.Render(LineWidth);
            Console.WriteLine(generatedCode);
            return generatedCode;
        }

        private IEnumerable<EventDecl> GenerateEvents()
        {
            return allEvents.Select(
                kv =>
                {
                    PSharpType payloadType = typeFactory.MakePSharpType(kv.Value.payloadType);
                    return new EventDecl
                    {
                        Name = kv.Key,
                        Assert = kv.Value.maxInstances == -1 || kv.Value.maxInstancesAssumed ? -1 : kv.Value.maxInstances,
                        Assume = kv.Value.maxInstances == -1 || !kv.Value.maxInstancesAssumed ? -1 : kv.Value.maxInstances,
                        PayloadType = payloadType == PSharpBaseType.Null ? null : payloadType
                    };
                });
        }

        private IEnumerable<MachineDecl> GenerateMachines()
        {
            return allMachines.Select(
                kv =>
                {
                    var decl = new MachineDecl {Name = kv.Key, States = GenerateStates(kv.Value, kv.Value.stateNameToStateInfo)};
                    return decl;
                });
        }

        private IEnumerable<StateDecl> GenerateStates(MachineInfo nameToStateInfo, Dictionary<string, StateInfo> stateNameToStateInfo)
        {
            //TODO: what about null transitions? how are those expressed in P#?
            return stateNameToStateInfo.Select(
                kv =>
                {
                    IEnumerable<KeyValuePair<string, string>> ignoredEvents =
                        kv.Value.dos.Where(evt => evt.Value == PTranslationIgnoredActionName);
                    IEnumerable<KeyValuePair<string, string>> actionOnlyEvents =
                        kv.Value.dos.Where(evt => evt.Value != PTranslationIgnoredActionName);
                    return new StateDecl
                    {
                        Name = kv.Value.printedName,
                        IsHot = kv.Value.IsHot,
                        IsCold = kv.Value.IsCold,
                        IsWarm = kv.Value.IsWarm,
                        IsStart = kv.Key.Equals(nameToStateInfo.initStateName),
                        EntryFun = kv.Value.entryActionName,
                        ExitFun = kv.Value.exitFunName,
                        Transitions = GenerateStateEventHandlers(kv.Value.transitions, actionOnlyEvents, stateNameToStateInfo),
                        IgnoredEvents = ignoredEvents.Select(evt => evt.Key).ToList(),
                        DeferredEvents = kv.Value.deferredEvents
                    };
                });
        }

        private IEnumerable<StateEventHandler> GenerateStateEventHandlers(
            Dictionary<string, TransitionInfo> transitions,
            IEnumerable<KeyValuePair<string, string>> valueDos,
            IReadOnlyDictionary<string, StateInfo> stateNameToStateInfo)
        {
            return transitions
                .Select(
                    kv => new StateEventHandler
                    {
                        OnEvent = kv.Key,
                        IsPush = kv.Value.IsPush,
                        Target = stateNameToStateInfo[kv.Value.target].printedName,
                        Function = kv.Value.transFunName
                    }).Concat(
                    valueDos.Select(kv => new StateEventHandler {OnEvent = kv.Key, IsPush = false, Target = null, Function = kv.Value}));
        }
    }

    internal class StateEventHandler
    {
        public string OnEvent { get; set; }
        public bool IsPush { get; set; }
        public string Target { get; set; }
        public string Function { get; set; }
    }

    internal class StateDecl
    {
        public string Name { get; set; }
        public bool IsHot { get; set; }
        public bool IsCold { get; set; }
        public bool IsWarm { get; set; }
        public bool IsStart { get; set; }
        public IEnumerable<StateEventHandler> Transitions { get; set; }
        public string EntryFun { get; set; }
        public string ExitFun { get; set; }
        public List<string> IgnoredEvents { get; set; }
        public List<string> DeferredEvents { get; set; }
    }

    internal class MachineDecl
    {
        public string Name { get; set; }
        public IEnumerable<StateDecl> States { get; set; }
    }

    internal class EventDecl
    {
        public string Name { get; set; }
        public int Assert { get; set; }
        public int Assume { get; set; }
        public PSharpType PayloadType { get; set; }
    }
}