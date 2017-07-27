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

            IList<EventDecl> events = GenerateEvents();
            IList<MachineDecl> machines = GenerateMachines();

            t.Add("pgm", new {Namespace = "Test", Events = events, Machines = machines, Types = typeFactory.AllTypes});
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

        private IList<EventDecl> GenerateEvents()
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
                }).ToList();
        }

        private IList<MachineDecl> GenerateMachines()
        {
            return allMachines.Select(
                kv =>
                {
                    MachineInfo info = kv.Value;
                    return new MachineDecl
                    {
                        Name = kv.Key,
                        States = GenerateStates(info, info.stateNameToStateInfo),
                        Methods = GenerateMethods(info.funNameToFunInfo, info)
                    };
                }).ToList();
        }

        private IList<MethodDecl> GenerateMethods(IDictionary<string, FunInfo> infoFunNameToFunInfo, MachineInfo info)
        {
            // this function is pointless
            infoFunNameToFunInfo.Remove("ignore");
            return infoFunNameToFunInfo.Select(
                kv =>
                {
                    string funName = kv.Key;
                    FunInfo funInfo = kv.Value;
                    List<TypedName> parameters = (from name in funInfo.parameterNames
                                                  let type = typeFactory.MakePSharpType(funInfo.localNameToInfo[name].type)
                                                  where !type.Equals(PSharpBaseType.Null)
                                                  select new TypedName {Name = name, Type = type}).ToList();

                    List<TypedName> locals = (from name in funInfo.localNames
                                              let type = typeFactory.MakePSharpType(funInfo.localNameToInfo[name].type)
                                              where !type.Equals(PSharpBaseType.Null)
                                              select new TypedName {Name = name, Type = type}).ToList();

                    Dictionary<string, TypedName> localSymbolTable = parameters.Concat(locals).ToDictionary(v => v.Name, v => v);

                    return new MethodDecl
                    {
                        Name = funName,
                        ReturnType = typeFactory.MakePSharpType(funInfo.returnType.Node),
                        Parameters = parameters,
                        LocalVariables = locals
                    };
                }).ToList();
        }

        private IList<StateDecl> GenerateStates(MachineInfo nameToStateInfo, IReadOnlyDictionary<string, StateInfo> stateNameToStateInfo)
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
                }).ToList();
        }

        private static IList<StateEventHandler> GenerateStateEventHandlers(
            Dictionary<string, TransitionInfo> transitions,
            IEnumerable<KeyValuePair<string, string>> valueDos,
            IReadOnlyDictionary<string, StateInfo> stateNameToStateInfo)
        {
            IEnumerable<StateEventHandler> eventTransitions =
                transitions.Select(
                    kv => new StateEventHandler
                    {
                        OnEvent = kv.Key,
                        IsPush = kv.Value.IsPush,
                        Target = stateNameToStateInfo[kv.Value.target].printedName,
                        Function = kv.Value.transFunName
                    });
            IEnumerable<StateEventHandler> eventActions =
                valueDos.Select(kv => new StateEventHandler {OnEvent = kv.Key, IsPush = false, Target = null, Function = kv.Value});
            return eventTransitions.Concat(eventActions).ToList();
        }
    }

    internal class TypedName
    {
        public string Name { get; set; }

        public PSharpType Type { get; set; }
    }

    internal class MethodDecl
    {
        public string Name { get; set; }
        public PSharpType ReturnType { get; set; }
        public List<TypedName> Parameters { get; set; }
        public List<TypedName> LocalVariables { get; set; }
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
        public IList<StateEventHandler> Transitions { get; set; }
        public string EntryFun { get; set; }
        public string ExitFun { get; set; }
        public List<string> IgnoredEvents { get; set; }
        public List<string> DeferredEvents { get; set; }
    }

    internal class MachineDecl
    {
        public string Name { get; set; }
        public IList<StateDecl> States { get; set; }
        public IList<MethodDecl> Methods { get; set; }
    }

    internal class EventDecl
    {
        public string Name { get; set; }
        public int Assert { get; set; }
        public int Assume { get; set; }
        public PSharpType PayloadType { get; set; }
    }
}