using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.Formula.API;
using Microsoft.Formula.API.ASTQueries;
using Microsoft.Formula.API.Nodes;
using Microsoft.Formula.Common;

namespace PCompiler
{

    internal class Compiler
    {
        public const string Con_LabeledExpr = "___InternalLabeledExpression";
        public static AST<FuncTerm> App_LabeledExpr = Factory.Instance.MkFuncTerm(Factory.Instance.MkId(Con_LabeledExpr));

        private const string pdataDomainStr = "PData";
        private const string cdataDomainStr = "CData";
        private const string zingdataDomainStr = "ZingData";

        private const string cRenderDefaultsStr = "CRenderDefaults.4ml";
        private const string cRenderDefaultsDebugStr = "CRenderDefaults_Debug.4ml";
        private const string cParserDefaultsStr = "CParserDefaults.4ml";
        private const string zingRenderDefaultsStr = "ZingRenderDefaults.4ml";

        public const string SM_ARG_UNION = "SM_ARG_UNION";
        public const string SM_NULL = "SM_NULL";
        public const string SMF_PACKED_VALUE = "SMF_PACKED_VALUE";
        public const string SMF_ARRAYLIST = "SMF_ARRAYLIST";
        public const string SMF_HASHTABLE = "SMF_HASHTABLE";

        public const string NullEvent = "null";
        public const string DefaultEvent = "default";
        public const string DeleteEvent = "delete";

        public string target;
        public string outputPath;
        public bool erase;
        public bool kernelMode;
        public bool emitHeaderComment;
        public bool emitDebugC;
        public bool liveness;
        public AST<Model> model = null;
        private string domainPath = null;

        private int nextOutputId = 0;

        private MyDictionary<string, AST<FuncTerm>> modelAliases;

        private Dictionary<string, LinkedList<AST<FuncTerm>>> factBins;

        public Dictionary<string, int> modelMachineIds = new Dictionary<string, int>();

        public Dictionary<string, int> modelEventIds = new Dictionary<string, int>();

        public Dictionary<string, int> modelEventSetIds = new Dictionary<string, int>();

        public Dictionary<string, int> modelStateIds = new Dictionary<string, int>();

        public Dictionary<string, int> modelVarIds = new Dictionary<string, int>();

        public Dictionary<string, int> modelTransSizes = new Dictionary<string, int>();

        public ProgramName CompilingProgram
        {
            get;
            private set;
        }

        public Dictionary<Node, ZingTranslationInfo> computedType;

        public Dictionary<string, EventInfo> allEvents;
        public Dictionary<string, MachineInfo> allMachines;
        public Dictionary<PType, TypeInfo> declaredTypes;
        public HashSet<PType> allTypes;
        public Dictionary<PType, List<PType>> subtypes, supertypes;

        public List<Flag> errors;

        Dictionary<string, int> uniqIDCounters = new Dictionary<string, int>();
        public string getUnique(string prefix)
        {
            if (!uniqIDCounters.ContainsKey(prefix))
                uniqIDCounters[prefix] = 0;

            var ret = uniqIDCounters[prefix];
            uniqIDCounters[prefix]++;
            return prefix + '_' + ret;
        }

        public Compiler(string target, string outputPath, bool erase, bool kernelMode, bool emitHeaderComment, bool emitDebugC, bool liveness)
        {
            this.target = target;
            this.outputPath = outputPath;
            this.erase = erase;
            this.kernelMode = kernelMode;
            this.emitHeaderComment = emitHeaderComment;
            this.emitDebugC = emitDebugC;
            this.liveness = liveness;

            this.modelAliases = new MyDictionary<string, AST<FuncTerm>>();
            this.factBins = new Dictionary<string, LinkedList<AST<FuncTerm>>>();
            this.computedType = new Dictionary<Node, ZingTranslationInfo>();

            this.allTypes = new HashSet<PType>();
            this.allTypes.Add(PType.Nil);
            this.allTypes.Add(PType.Id);
            this.allTypes.Add(PType.Int);
            this.allTypes.Add(PType.Bool);
            this.allTypes.Add(PType.Event);
            this.allTypes.Add(PType.Any);

            this.declaredTypes = new Dictionary<PType, TypeInfo>();
            this.subtypes = new Dictionary<PType, List<PType>>();
            this.supertypes = new Dictionary<PType, List<PType>>();
            this.registerType(PType.Any);

            errors = new List<Flag>();
        }

        #region Accessors to P program information
        public FuncTerm GetFuncTerm(Node node)
        {
            if (node.NodeKind == NodeKind.FuncTerm)
                return (FuncTerm)node;
            else if (node.NodeKind == NodeKind.Id)
                return modelAliases[((Id)node).Name].Node;
            Debug.Assert(false);
            return null;
        }

        // Returns all super and sub types of t, and t itself
        public IEnumerable<PType> relatives(PType t)
        {
            var res = new List<PType>(supertypes[t]);
            res.AddRange(subtypes[t]);
            res.Add(t);
            return res;
        }

        public string GetMachineName(FuncTerm ft, int index)
        {
            FuncTerm machineDecl = GetFuncTerm(Compiler.GetArgByIndex(ft, index));
            var machineName = GetName(machineDecl, 0);
            return machineName;
        }

        public string DriverDeclName()
        {
            return string.Format("DriverDecl_{0}", model.Node.Name);
        }

        public void GetEventInfo(Node node, out string eventName, out PType eventArgTypeName)
        {
            if (node.NodeKind == NodeKind.Id && ((Id)node).Name != PData.Cnst_Default.Node.Name)
            {
                var eventDecl = GetFuncTerm(node);
                eventName = ((Cnst)Compiler.GetArgByIndex(eventDecl, 0)).GetStringValue();
                eventArgTypeName = GetPType(Compiler.GetArgByIndex(eventDecl, 2));
            }
            else
            {
                eventName = Compiler.DefaultEvent;
                eventArgTypeName = new PNilType();
            }
        }

        public void DeconstructEventSetDecl(FuncTerm term, out string eventSetName, out string machineName)
        {
            using (var enumerator = term.Args.GetEnumerator())
            {
                enumerator.MoveNext();
                eventSetName = ((Cnst)enumerator.Current).GetStringValue();
                enumerator.MoveNext();
                var machine = (eventSetName == "None") ? (FuncTerm)enumerator.Current : GetFuncTerm(enumerator.Current);
                machineName = GetName(machine, 0);
            }
        }

        private string LookupOwnerName(Node n)
        {
            return ((Cnst)GetArgByIndex(GetFuncTerm(n), 0)).GetStringValue();
        }

        public string GetName(FuncTerm ft, int nameIndex)
        {
            return ((Cnst)GetArgByIndex(ft, nameIndex)).GetStringValue();
        }

        public string GetOwnerName(FuncTerm ft, int ownerIndex, int ownerNameIndex)
        {
            var ownerArg = GetArgByIndex(ft, ownerIndex);
            switch (ownerArg.NodeKind)
            {
                case NodeKind.Id:
                    return ((Cnst)GetArgByIndex(GetFuncTerm(ownerArg), ownerNameIndex)).GetStringValue();
                case NodeKind.FuncTerm:
                    return ((Cnst)GetArgByIndex((FuncTerm)ownerArg, ownerNameIndex)).GetStringValue();
                default:
                    throw new InvalidOperationException();
            }
        }

        public string GetOwnerOwnerName(FuncTerm ft, int ownerIndex, int ownerOwnerIndex, int ownerOwnerNameIndex)
        {
            var ownerArg = GetArgByIndex(ft, ownerIndex);
            switch (ownerArg.NodeKind)
            {
                case NodeKind.Id:
                    return GetOwnerName(GetFuncTerm(ownerArg), ownerOwnerIndex, ownerOwnerNameIndex);
                case NodeKind.FuncTerm:
                    return GetOwnerName((FuncTerm)ownerArg, ownerOwnerIndex, ownerOwnerNameIndex);
                default:
                    throw new InvalidOperationException();
            }
        }

        public LinkedList<AST<FuncTerm>> GetBin(FuncTerm ft)
        {
            var fun = (Id)ft.Function;
            return GetBin(fun.Name);
        }

        public LinkedList<AST<FuncTerm>> GetBin(string name)
        {
            Contract.Requires(!string.IsNullOrEmpty(name));
            LinkedList<AST<FuncTerm>> bin;
            if (!factBins.TryGetValue(name, out bin))
            {
                bin = new LinkedList<AST<FuncTerm>>();
                factBins.Add(name, bin);
            }

            return bin;
        }
        #endregion

        public bool Compile()
        {
            InstallResult result;
            var env = new Env();
            if (!env.Install(target, out result))
            {
                return false;
            }
            PrintResult(result);

            ProgramName inputName;
            try
            {
                inputName = new ProgramName(target);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not find an input model at {0}: {1}", target, e.Message);
                return false;
            }

            try
            {
                var outInfo = new System.IO.DirectoryInfo(outputPath);
                if (!outInfo.Exists)
                {
                    Console.WriteLine("The output directory {0} does not exist", outputPath);
                    return false;
                }

                outputPath = outInfo.FullName;
            }
            catch (Exception e)
            {
                Console.WriteLine("Bad output directory: {0}", e.Message);
                return false;
            }

            var pDataQuery = new NodePred[]
            {
                NodePredFactory.Instance.Star,
                NodePredFactory.Instance.MkPredicate(NodeKind.Domain) &
                NodePredFactory.Instance.MkNamePredicate("PData")
            };

            foreach (var p in result.Touched)
            {
                if (model == null && p.Program.Node.Name.Equals(inputName))
                {
                    CompilingProgram = p.Program.Node.Name;
                    model = p.Program.FindAny(
                        new NodePred[] 
                        {
                            NodePredFactory.Instance.Star,
                            NodePredFactory.Instance.MkPredicate(NodeKind.Model)
                        }) as AST<Model>;
                }

                if (domainPath == null &&
                    p.Program.FindAny(pDataQuery) != null &&
                    p.Program.Node.Name.IsFileProgramName)
                {
                    domainPath = System.IO.Path.GetDirectoryName(p.Program.Node.Name.Uri.LocalPath);
                }
            }

            if (model == null)
            {
                Console.WriteLine("Could not find an input model at {0}", target);
                return false;
            }
            else if (domainPath == null)
            {
                Console.WriteLine("Could not find required data models");
                return false;
            }

            AST<Node> reduced;
            if (!Microsoft.Formula.Compiler.Compiler.TryGetReducedForm(model, out reduced))
            {
                Console.WriteLine("Unable to compile input model at {0}", target);
                return false;
            }

            //// From now on, use the reduced model
            model = (AST<Model>)reduced;

            IndexModel(model);

            if (!GenerateMachineData(model))
            {
                return false;
            }

            var outZingModel = MkZingOutputModel();
            var success = new PToZing(this).GenerateZing(env, ref outZingModel);
            if (!success)
            {
                PrintResult(errors);
                return false;
            }
            if (!PrintFile(outZingModel, env))
            {
                return false;
            }

            PropagateFlags();

            if (erase)
            {
                EraseGhostDecls();
            }

            var outCModel = MkCOutputModel();
            new PToC(this).GenerateC(env, ref outCModel);

            if (!PrintFile(outCModel, env))
            {
                return false;
            }
            return true;
        }

        private bool GenerateMachineData(AST<Model> model)
        {
            allEvents = new Dictionary<string, EventInfo>();
            allEvents[DefaultEvent] = new EventInfo(1, false, PType.Nil);
            allEvents[DeleteEvent] = new EventInfo(1, false, PType.Nil);
            allEvents[NullEvent] = new EventInfo(1, false, PType.Nil);
            allMachines = new Dictionary<string, MachineInfo>();

            LinkedList<AST<FuncTerm>> terms;

            terms = GetBin("EventDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    if (allEvents.ContainsKey(name))
                    {
                        errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Event {0} has been declared before.", name), 0, CompilingProgram));
                    }
                    else
                    {
                        it.MoveNext();
                        var bound = it.Current;
                        it.MoveNext();
                        var payloadType = it.Current;
                        if (bound.NodeKind == NodeKind.Id)
                        {
                            allEvents[name] = new EventInfo(GetPType(payloadType));
                        }
                        else
                        {
                            var ft = (FuncTerm)bound;
                            var maxInstances = (int)((Cnst)Compiler.GetArgByIndex(ft, 0)).GetNumericValue().Numerator;
                            if (maxInstances < 0)
                            {
                                errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Bound must be non-negative."), 0, CompilingProgram));
                            }
                            else
                            {
                                var maxInstancesAssumed = ((Id)ft.Function).Name == "AssumeMaxInstances";
                                allEvents[name] = new EventInfo(maxInstances, maxInstancesAssumed, GetPType(payloadType));
                            }
                        }
                    }
                }
            }

            terms = GetBin("MachineDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    if (allMachines.ContainsKey(name))
                    {
                        errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Machine {0} has been declared before.", name), 0, CompilingProgram));
                    }
                    else
                    {
                        allMachines[name] = new MachineInfo();
                        it.MoveNext();
                        allMachines[name].isGhost = ((Id)it.Current).Name == "TRUE";
                        it.MoveNext();
                        if (it.Current.NodeKind != NodeKind.Id)
                        {
                            int maxQueueSize = (int)((Cnst)it.Current).GetNumericValue().Numerator;
                            if (maxQueueSize < 0)
                            {
                                errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Machine {0} must have a non-negative maxQueueSize.", name), 0, CompilingProgram));
                            }
                            else
                            {
                                allMachines[name].maxQueueSize = maxQueueSize;
                            }
                        }
                    }
                }
            }

            terms = GetBin("StateDecl");
            foreach (var term in terms)
            {
                try
                {
                    using (var it = term.Node.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        var stateName = ((Cnst)it.Current).GetStringValue();
                        it.MoveNext();
                        var machineDecl = GetFuncTerm(it.Current);
                        var machineName = GetName(machineDecl, 0);
                        var stateTable = allMachines[machineName].stateNameToStateInfo;
                        if (stateTable.ContainsKey(stateName))
                        {
                            errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("State {0} for machine {1} has been declared before.", stateName, machineName), 0, CompilingProgram));
                        }
                        else
                        {
                            stateTable[stateName] = new StateInfo(term.Node);
                        }
                    }
                }
                catch (MissingKeyException<string> e)
                {
                    errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Alias {0} has not been declared before.", e.MissingKey), 0, CompilingProgram));
                }
            }

            terms = GetBin("ExitFun");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var stateDecl = GetFuncTerm(it.Current);
                    var stateName = GetName(stateDecl, 0);
                    var stateOwnerMachineName = GetMachineName(stateDecl, 1);
                    it.MoveNext();
                    if (allMachines[stateOwnerMachineName].stateNameToStateInfo[stateName].exitFun != null)
                    {
                        errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Exit function for state {0} has been declared before.", stateName), 0, CompilingProgram));
                    }
                    else
                    {
                        allMachines[stateOwnerMachineName].stateNameToStateInfo[stateName].exitFun = it.Current;
                    }
                }
            }

            terms = GetBin("VarDecl");
            foreach (var term in terms)
            {
                try
                {
                    using (var it = term.Node.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        var varName = ((Cnst)it.Current).GetStringValue();
                        it.MoveNext();
                        var machineDecl = GetFuncTerm(it.Current);
                        var machineName = GetName(machineDecl, 0);
                        var varTable = allMachines[machineName].localVariableToVarInfo;
                        if (varTable.ContainsKey(varName))
                        {
                            errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Variable {0} for machine {1} has been declared before.", varName, machineName), 0, CompilingProgram));
                        }
                        else
                        {
                            it.MoveNext();
                            var type = GetPType(it.Current);
                            it.MoveNext();
                            var isGhost = ((Id)it.Current).Name == "TRUE";
                            varTable[varName] = new VariableInfo(type, isGhost);
                        }
                    }
                }
                catch (MissingKeyException<string> e)
                {
                    errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Alias {0} has not been declared before.", e.MissingKey), 0, CompilingProgram));
                }
            }

            terms = GetBin("TransDecl");
            foreach (var term in terms)
            {
                try
                {
                    using (var it = term.Node.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        var stateDecl = GetFuncTerm(it.Current);
                        var stateName = GetName(stateDecl, 0);
                        var stateOwnerMachineName = GetMachineName(stateDecl, 1);
                        var stateTable = allMachines[stateOwnerMachineName].stateNameToStateInfo[stateName];
                        it.MoveNext();
                        string eventName;
                        PType eventArgTypeName;
                        GetEventInfo(it.Current, out eventName, out eventArgTypeName);
                        it.MoveNext();
                        var targetStateDecl = GetFuncTerm(it.Current);
                        var targetStateName = GetName(targetStateDecl, 0);
                        var targetStateOwnerMachineName = GetMachineName(targetStateDecl, 1);
                        var targetStateInfo = allMachines[targetStateOwnerMachineName].stateNameToStateInfo[targetStateName];
                        it.MoveNext();
                        var isPush = ((Id)it.Current).Name == "TRUE";
                        if (stateOwnerMachineName == targetStateOwnerMachineName)
                        {
                            var tinfo = new TransitionInfo(targetStateName, isPush);
                            if (stateTable.transitions.ContainsKey(eventName) && !stateTable.transitions[eventName].Equals(tinfo))
                            {
                                errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("A conflicting transition on event {0} out of state {1} has been declared before.", eventName, stateName), 0, CompilingProgram));
                            }
                            else
                            {
                                stateTable.transitions[eventName] = tinfo;
                                if (eventName == DefaultEvent)
                                {
                                    stateTable.hasDefaultTransition = true;
                                }
                                targetStateInfo.nIncomingTransitions++;
                            }
                        }
                        else
                        {
                            errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("State {0} and {1} belong to different machines.", stateOwnerMachineName, targetStateOwnerMachineName), 0, CompilingProgram));
                        }
                    }
                }
                catch (MissingKeyException<string> e)
                {
                    errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Alias {0} has not been declared before.", e.MissingKey), 0, CompilingProgram));
                }
            }

            terms = GetBin("ActionDecl");
            foreach (var term in terms)
            {
                try
                {
                    using (var it = term.Node.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        var actionName = ((Cnst)it.Current).GetStringValue();
                        it.MoveNext();
                        var actionOwnerMachineDecl = GetFuncTerm(it.Current);
                        var actionOwnerMachineName = GetName(actionOwnerMachineDecl, 0);
                        it.MoveNext();
                        allMachines[actionOwnerMachineName].actionFunNameToActionFun[actionName] = new ActionInfo(it.Current);
                    }
                }
                catch (MissingKeyException<string> e)
                {
                    errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Alias {0} has not been declared before.", e.MissingKey), 0, CompilingProgram));
                }
            }

            terms = GetBin("Install");
            foreach (var term in terms)
            {
                try
                {
                    using (var it = term.Node.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        var stateDecl = GetFuncTerm(it.Current);
                        var stateName = GetName(stateDecl, 0);
                        var stateOwnerMachineName = GetMachineName(stateDecl, 1);
                        var stateTable = allMachines[stateOwnerMachineName].stateNameToStateInfo[stateName];
                        it.MoveNext();
                        string eventName;
                        PType eventArgTypeName;
                        GetEventInfo(it.Current, out eventName, out eventArgTypeName);
                        it.MoveNext();
                        var actionDecl = GetFuncTerm(it.Current);
                        var actionFunName = GetName(actionDecl, 0);
                        var actionOwnerMachineName = GetOwnerName(actionDecl, 1, 0);
                        var actionInfo = allMachines[actionOwnerMachineName].actionFunNameToActionFun[actionFunName];
                        if (stateOwnerMachineName != actionOwnerMachineName)
                        {
                            errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("State {0} and action {1} belong to different machines.", stateOwnerMachineName, actionOwnerMachineName), 0, CompilingProgram));
                        }
                        else if (eventName == DefaultEvent)
                        {
                            errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("An action on default event in state {0} is not allowed.", stateName), 0, CompilingProgram));
                        }
                        else if (stateTable.actions.ContainsKey(eventName))
                        {
                            errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("A conflicting action on event {0} in state {1} has been declared before.", eventName, stateName), 0, CompilingProgram));
                        }
                        else
                        {
                            stateTable.actions[eventName] = actionFunName;
                        }
                    }
                }
                catch (MissingKeyException<string> e)
                {
                    errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Alias {0} has not been declared before.", e.MissingKey), 0, CompilingProgram));
                }
            }

            terms = GetBin("MachStart");
            foreach (var term in terms)
            {
                try
                {
                    using (var it = term.Node.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        var machineDecl = GetFuncTerm(it.Current);
                        var machineName = GetName(machineDecl, 0);
                        var machineInfo = allMachines[machineName];
                        if (machineInfo.initStateDecl == null)
                        {
                            it.MoveNext();
                            machineInfo.initStateDecl = GetFuncTerm(it.Current);
                            var initStateOwnerName = GetOwnerName(machineInfo.initStateDecl, 1, 0);
                            var initStateName = GetName(machineInfo.initStateDecl, 0);
                            var initStateInfo = machineInfo.stateNameToStateInfo[initStateName];

                            if (initStateOwnerName != machineName)
                            {
                                errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Initial state {0} must be owned by the machine {1}.", initStateName, machineName), 0, CompilingProgram));
                            }
                        }
                        else
                        {
                            errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Initial state for machine {0} has been declared before.", machineName), 0, CompilingProgram));
                        }
                    }
                }
                catch (MissingKeyException<string> e)
                {
                    errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Alias {0} has not been declared before.", e.MissingKey), 0, CompilingProgram));
                }
            }
            foreach (var machineName in allMachines.Keys)
            {
                var machineInfo = allMachines[machineName];
                if (machineInfo.initStateDecl == null)
                {
                    errors.Add(new Flag(SeverityKind.Error, new Span(), string.Format("Initial state for machine {0} not declared.", machineName), 0, CompilingProgram));
                }
            }

            terms = GetBin("EventSetDecl");
            foreach (var term in terms)
            {
                try
                {
                    string eventSetName, machineName;
                    DeconstructEventSetDecl(term.Node, out eventSetName, out machineName);
                    var eventSetNameToEvents = allMachines[machineName].eventSetNameToEvents;
                    if (eventSetNameToEvents.ContainsKey(eventSetName))
                    {
                        errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Event set {0} has been declared before.", eventSetName), 0, CompilingProgram));
                    }
                    else
                    {
                        eventSetNameToEvents[eventSetName] = new List<string>();
                    }
                }
                catch (MissingKeyException<string> e)
                {
                    errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Alias {0} has not been declared before.", e.MissingKey), 0, CompilingProgram));
                }
            }

            terms = GetBin("InEventSet");
            foreach (var term in terms)
            {
                try
                {
                    using (var it = term.Node.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        string eventSetName, machineName;
                        DeconstructEventSetDecl(GetFuncTerm(it.Current), out eventSetName, out machineName);
                        it.MoveNext();
                        var eventName = GetName(GetFuncTerm(it.Current), 0);
                        allMachines[machineName].eventSetNameToEvents[eventSetName].Add(eventName);
                    }
                }
                catch (MissingKeyException<string> e)
                {
                    errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Alias {0} has not been declared before.", e.MissingKey), 0, CompilingProgram));
                }
            }

            terms = GetBin("FunDecl");
            foreach (var term in terms)
            {
                try
                {
                    using (var it = term.Node.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        string funName = ((Cnst)it.Current).GetStringValue();
                        it.MoveNext();
                        var machineDecl = GetFuncTerm(it.Current);
                        var machineName = GetName(machineDecl, 0);
                        var machineInfo = allMachines[machineName];
                        if (machineInfo.funNameToFunInfo.ContainsKey(funName))
                        {
                            errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Function {0} for machine {1} has been declared before.", funName, machineName), 0, CompilingProgram));
                        }
                        else
                        {
                            it.MoveNext();
                            var iter = it.Current;
                            it.MoveNext();
                            var returnTypeName = GetPType(it.Current);
                            it.MoveNext();
                            var isForeign = ((Id)it.Current).Name == "TRUE";
                            if (isForeign)
                            {
                                var funInfo = new FunInfo(isForeign, returnTypeName, term.Node);
                                Dictionary<string, VariableInfo> parameters = funInfo.parameterNameToInfo;
                                while (true)
                                {
                                    if (iter.NodeKind == NodeKind.Id)
                                        break;
                                    FuncTerm ft = (FuncTerm)iter;
                                    using (var enumerator = ft.Args.GetEnumerator())
                                    {
                                        enumerator.MoveNext();
                                        var varName = ((Cnst)enumerator.Current).GetStringValue();
                                        if (parameters.ContainsKey(varName))
                                        {
                                            errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Parameter {0} for function {1} has been declared before.", varName, funName), 0, CompilingProgram));
                                        }
                                        else
                                        {
                                            enumerator.MoveNext();
                                            var typeName = GetPType(enumerator.Current);
                                            parameters[varName] = new VariableInfo(typeName, false);
                                            funInfo.parameterNames.Add(varName);
                                            enumerator.MoveNext();
                                            iter = enumerator.Current;
                                        }
                                    }
                                }
                                machineInfo.funNameToFunInfo[funName] = funInfo;
                            }
                            else
                            {
                                errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Function {0} for machine {1} is not foreign.", funName, machineName), 0, CompilingProgram));
                            }
                        }
                    }
                }
                catch (MissingKeyException<string> e)
                {
                    errors.Add(new Flag(SeverityKind.Error, term.Node, string.Format("Alias {0} has not been declared before.", e.MissingKey), 0, CompilingProgram));
                }
            }

            terms = GetBin("Flags");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var funDecl = GetFuncTerm(it.Current);
                    var funName = GetName(funDecl, 0);
                    var ownerMachineName = GetOwnerName(funDecl, 1, 0);
                    allMachines[ownerMachineName].funNameToFunInfo[funName].atPassive = true;
                }
            }

            terms = GetBin("Stable");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var stateDecl = GetFuncTerm(it.Current);
                    var stateName = GetName(stateDecl, 0);
                    var ownerMachineName = GetOwnerName(stateDecl, 1, 0);
                    allMachines[ownerMachineName].stateNameToStateInfo[stateName].isStable = true;
                }
            }

            terms = GetBin("Fair");
            foreach (var term in terms)
            {
                FuncTerm decl;
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    decl = GetFuncTerm(it.Current);
                }
                if ((decl.Function as Id).Name == "TransDecl")
                {
                    using (var it = decl.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        var stateDecl = GetFuncTerm(it.Current);
                        var stateName = GetName(stateDecl, 0);
                        var stateOwnerMachineName = GetMachineName(stateDecl, 1);
                        var stateTable = allMachines[stateOwnerMachineName].stateNameToStateInfo[stateName];
                        it.MoveNext();
                        string eventName;
                        PType eventArgTypeName;
                        GetEventInfo(it.Current, out eventName, out eventArgTypeName);
                        stateTable.transitions[eventName].isFair = true;
                    }
                }
                else
                {
                    var machineName = GetName(decl, 0);
                    allMachines[machineName].isFair = true;
                }
            }

            // Compute the possible incomming payloads for States/Actions
            GenericCtxt ctxt = new GenericCtxt();
            model.Compute<int>(
                n => Model_UnFold(ctxt, n),
                (n, ch) => ComputePayloadSets(ctxt, n, ch));

            // For each state determine what (if any) is its submachine
            model.Compute<int>(
                n => Model_UnFold(ctxt, n),
                (n, ch) => ComputeSubmachines(ctxt, n, ch));


            // Check if any transition violates a submachine boundary
            model.Compute<int>(
                n => Model_UnFold(ctxt, n),
                (n, ch) => CheckSubmachineTransitions(ctxt, n, ch));

            if (errors.Count > 0)
            {
                PrintResult(errors);
                return false;
            }
            else
            {
                return true;
            }
        }

        internal class GenericCtxt
        {
            public string curMachine;
            public string curState;
        }

        private int ComputePayloadSets(GenericCtxt ctxt, Node n, IEnumerable<int> dummy)
        {
            if (Compiler.isInstanceOf(n, PData.Con_Scall))
            {
                using (var it = ((FuncTerm)n).Args.GetEnumerator())
                {
                    // SCalls
                    it.MoveNext();
                    var target = it.Current;
                    if (!(target is FuncTerm) || Compiler.getFtName((FuncTerm)target) != PData.Con_Use.Node.Name) // Error will becaught in type checking, so don't need to report it here
                        return 0;

                    var targetUse = (FuncTerm)target;
                    var val = ((Cnst)Compiler.GetArgByIndex(targetUse, 0)).GetStringValue();
                    var kind = ((Id)Compiler.GetArgByIndex(targetUse, 1)).Name;

                    if (!(kind == PData.Cnst_State.Node.Name)) // Error will becaught in type checking, so don't need to report it here
                        return 0;

                    allMachines[ctxt.curMachine].stateNameToStateInfo[val].argTypes.Add(PType.Nil);
                }
            }
            else if (Compiler.isInstanceOf(n, PData.Con_TransDecl))
            {
                // Normal transition
                var dst = ((Cnst)GetArgByIndex(GetFuncTerm(Compiler.GetArgByIndex((FuncTerm)n, 2)), 0)).GetStringValue();
                PType eventArgTypeName;
                string eventName;
                GetEventInfo(Compiler.GetArgByIndex((FuncTerm)n, 1), out eventName, out eventArgTypeName);
                allMachines[ctxt.curMachine].stateNameToStateInfo[dst].argTypes.Add(eventArgTypeName);
            }
            else if (Compiler.isInstanceOf(n, PData.Con_MachStart))
            {
                // Start state implicitly can take the Nil payloads
                string state = LookupOwnerName(Compiler.GetArgByIndex((FuncTerm)n, 1));
                allMachines[ctxt.curMachine].stateNameToStateInfo[state].argTypes.Add(PType.Nil);
            }
            else if (Compiler.isInstanceOf(n, PData.Con_Install))
            {
                PType eventArgTypeName;
                string eventName;
                GetEventInfo(Compiler.GetArgByIndex((FuncTerm)n, 1), out eventName, out eventArgTypeName);
                string action = ((Cnst)Compiler.GetArgByIndex(GetFuncTerm(Compiler.GetArgByIndex((FuncTerm)n, 2)), 0)).GetStringValue();
                allMachines[ctxt.curMachine].actionFunNameToActionFun[action].argTypes.Add(eventArgTypeName);
            }

            return 0;
        }

        private IEnumerable<Node> Model_UnFold(GenericCtxt ctxt, Node n)
        {
            if (n.NodeKind == NodeKind.ModelFact)
            {
                ModelFact mf = (ModelFact)n;
                Node inner = mf.Match;

                bool unsetMachine = false, unsetState = false;

                if (Compiler.isInstanceOf(inner, PData.Con_Fair))
                {
                    yield return null;
                }
                else if (Compiler.isInstanceOf(inner, PData.Con_Stable))
                {
                    yield return null;
                }
                else if (Compiler.isInstanceOf(inner, PData.Con_ActionDecl))
                {
                    ctxt.curMachine = LookupOwnerName(Compiler.GetArgByIndex((FuncTerm)inner, 1));
                    unsetMachine = true;
                }
                else if (Compiler.isInstanceOf(inner, PData.Con_StateDecl))
                {
                    ctxt.curMachine = LookupOwnerName(Compiler.GetArgByIndex((FuncTerm)inner, 1));
                    ctxt.curState = ((Cnst)Compiler.GetArgByIndex((FuncTerm)inner, 0)).GetStringValue();
                    unsetMachine = true;
                    unsetState = true;
                }
                else if (Compiler.isInstanceOf(inner, PData.Con_ExitFun))
                {
                    var stateNode = GetFuncTerm(Compiler.GetArgByIndex((FuncTerm)inner, 0));
                    ctxt.curMachine = LookupOwnerName(Compiler.GetArgByIndex(stateNode, 1));
                    ctxt.curState = ((Cnst)Compiler.GetArgByIndex(stateNode, 0)).GetStringValue();
                    unsetMachine = true;
                    unsetState = true;
                }
                else if (Compiler.isInstanceOf(inner, PData.Con_VarDecl))
                {
                    ctxt.curMachine = LookupOwnerName(Compiler.GetArgByIndex((FuncTerm)inner, 1));
                    unsetMachine = true;
                }
                else if (Compiler.isInstanceOf(inner, PData.Con_TransDecl))
                {
                    var stateNode = GetFuncTerm(Compiler.GetArgByIndex((FuncTerm)inner, 0));
                    ctxt.curMachine = LookupOwnerName(Compiler.GetArgByIndex(stateNode, 1));
                    ctxt.curState = ((Cnst)Compiler.GetArgByIndex(stateNode, 0)).GetStringValue();
                    unsetMachine = true;
                    unsetState = true;
                }
                else if (Compiler.isInstanceOf(inner, PData.Con_FunDecl))
                {
                    ctxt.curMachine = LookupOwnerName(Compiler.GetArgByIndex((FuncTerm)inner, 1));
                    unsetMachine = true;
                }
                else if (Compiler.isInstanceOf(inner, PData.Con_StateSetDecl))
                {
                    ctxt.curMachine = LookupOwnerName(Compiler.GetArgByIndex((FuncTerm)inner, 1));
                    unsetMachine = true;
                }
                else if (Compiler.isInstanceOf(inner, PData.Con_EventSetDecl))
                {
                    ctxt.curMachine = LookupOwnerName(Compiler.GetArgByIndex((FuncTerm)inner, 1));
                    unsetMachine = true;
                }
                else if (Compiler.isInstanceOf(inner, PData.Con_Install))
                {
                    var stateNode = GetFuncTerm(Compiler.GetArgByIndex((FuncTerm)inner, 0));
                    ctxt.curMachine = LookupOwnerName(Compiler.GetArgByIndex(stateNode, 1));
                    ctxt.curState = ((Cnst)Compiler.GetArgByIndex(stateNode, 0)).GetStringValue();
                    unsetMachine = true;
                    unsetState = true;
                }
                else if (Compiler.isInstanceOf(inner, PData.Con_MachStart))
                {
                    ctxt.curMachine = LookupOwnerName(Compiler.GetArgByIndex((FuncTerm)inner, 0));
                    unsetMachine = true;
                }
                else if (Compiler.isInstanceOf(inner, PData.Con_InStateSet))
                {
                    var state = GetFuncTerm(Compiler.GetArgByIndex((FuncTerm)inner, 1));
                    ctxt.curMachine = LookupOwnerName(Compiler.GetArgByIndex(state, 1));
                    unsetMachine = true;
                }

                foreach (var t in n.Children)
                    yield return t;

                if (unsetMachine)
                    ctxt.curMachine = null;

                if (unsetState)
                    ctxt.curState = null;
            }
            else
            {
                foreach (var t in n.Children)
                    yield return t;
            }
        }

        private int ComputeSubmachines(GenericCtxt ctxt, Node n, IEnumerable<int> dummy)
        {
            if (Compiler.isInstanceOf(n, PData.Con_InStateSet))
            {
                var setName = LookupOwnerName(Compiler.GetArgByIndex((FuncTerm)n, 0));
                var stateName = LookupOwnerName(Compiler.GetArgByIndex((FuncTerm)n, 1));
                allMachines[ctxt.curMachine].stateNameToStateInfo[stateName].submachineName = setName;
            }
            return 0;
        }

        private int CheckSubmachineTransitions(GenericCtxt ctxt, Node n, IEnumerable<int> dummy)
        {
            if (Compiler.isInstanceOf(n, PData.Con_TransDecl))
            {
                var from = LookupOwnerName(Compiler.GetArgByIndex((FuncTerm)n, 0));
                var to = LookupOwnerName(Compiler.GetArgByIndex((FuncTerm)n, 2));
                var fromInfo = allMachines[ctxt.curMachine].stateNameToStateInfo[from];
                var toInfo = allMachines[ctxt.curMachine].stateNameToStateInfo[to];
                var isPush = ((Id)Compiler.GetArgByIndex((FuncTerm)n, 3)).Name == "TRUE";

                if (!isPush && fromInfo.submachineName != toInfo.submachineName)
                {
                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Ordinary transition from {0} to {1} escapes submachine {2}.", from, to,
                        fromInfo.submachineName != null ? fromInfo.submachineName : toInfo.submachineName), 0, CompilingProgram));
                }
            }

            return 0;
        }

        void PropagateFlags()
        {
            foreach (MachineInfo info in allMachines.Values)
            {
                Queue<string> workList = new Queue<string>();
                HashSet<string> visited = new HashSet<string>();
                foreach (string funName in info.funNameToFunInfo.Keys)
                {
                    if (info.funNameToFunInfo[funName].atPassive)
                        workList.Enqueue(funName);
                }
                while (workList.Count > 0)
                {
                    var funName = workList.Dequeue();
                    if (visited.Contains(funName)) continue;
                    visited.Add(funName);
                    info.funNameToFunInfo[funName].atPassive = true;
                    foreach (var caller in info.funNameToFunInfo[funName].callers)
                    {
                        workList.Enqueue(caller);
                    }
                }
                foreach (ActionInfo actionInfo in info.actionFunNameToActionFun.Values)
                {
                    foreach (string callee in actionInfo.callees)
                    {
                        if (info.funNameToFunInfo[callee].atPassive)
                            actionInfo.atPassive = true;
                    }
                }
                foreach (StateInfo stateInfo in info.stateNameToStateInfo.Values)
                {
                    foreach (string callee in stateInfo.entryFunCallees)
                    {
                        if (info.funNameToFunInfo[callee].atPassive)
                            stateInfo.entryFunAtPassive = true;
                    }
                    foreach (string callee in stateInfo.exitFunCallees)
                    {
                        if (info.funNameToFunInfo[callee].atPassive)
                            stateInfo.exitFunAtPassive = true;
                    }
                }
            }
        }

        private void EraseGhostDecls()
        {
            LinkedList<AST<FuncTerm>> bin, newDecls;

            bin = GetBin("MachineDecl");
            newDecls = new LinkedList<AST<FuncTerm>>();
            foreach (var e in bin)
            {
                if (allMachines[GetName(e.Node, 0)].isGhost)
                    continue;
                newDecls.AddLast(e);
            }
            factBins["MachineDecl"] = newDecls;

            string[] declNames1 = {"VarDecl", "StateDecl", "EventSetDecl", "StateSetDecl", "FunDecl", "ActionDecl"};
            foreach (string s in declNames1)
            {
                bin = GetBin(s);
                newDecls = new LinkedList<AST<FuncTerm>>();
                foreach (var e in bin)
                {
                    var ownerName = GetOwnerName(e.Node, 1, 0);
                    if (allMachines[ownerName].isGhost)
                        continue;
                    newDecls.AddLast(e);
                }
                factBins[s] = newDecls;
            }

            bin = GetBin("VarDecl");
            newDecls = new LinkedList<AST<FuncTerm>>();
            foreach (var e in bin)
            {
                var varName = GetName(e.Node, 0);
                var ownerName = GetOwnerName(e.Node, 1, 0);
                if (allMachines[ownerName].localVariableToVarInfo[varName].isGhost)
                    continue;
                newDecls.AddLast(e);
            }
            factBins["VarDecl"] = newDecls;

            string[] declNames2 = {"TransDecl", "ExitFun"};
            foreach (string s in declNames2)
            {
                bin = GetBin(s);
                newDecls = new LinkedList<AST<FuncTerm>>();
                foreach (var e in bin)
                {
                    var stateDecl = GetFuncTerm(GetArgByIndex(e.Node, 0));
                    var ownerName = GetOwnerName(stateDecl, 1, 0);
                    if (allMachines[ownerName].isGhost)
                        continue;
                    newDecls.AddLast(e);
                }
                factBins[s] = newDecls;
            }

            bin = GetBin("MachStart");
            newDecls = new LinkedList<AST<FuncTerm>>();
            foreach (var e in bin)
            {
                var ownerName = GetOwnerName(e.Node, 0, 0);
                if (allMachines[ownerName].isGhost)
                    continue;
                newDecls.AddLast(e);
            }
            factBins["MachStart"] = newDecls;
        }

        public void registerType(PType t)
        {
            if (declaredTypes.ContainsKey(t))
                return; // Already registered

            allTypes.Add(t);
            TypeInfo ti = new TypeInfo();

            if (t is PTupleType)
                ti.cType = ti.zingType = getUnique("Tuple");
            else if (t is PNamedTupleType)
                ti.cType = ti.zingType = getUnique("NamedTuple");
            else if (t is PSeqType)
                ti.cType = ti.zingType = getUnique("Seq");
            else if (t is PMapType)
            {
                registerType(new PSeqType((t as PMapType).KeyT));
                ti.cType = ti.zingType = getUnique("Map");
            }
            else if (t is PAnyType)
            { // TODO: We shouldn't register Any and just always emit code for it.
                ti.cType = Compiler.SMF_PACKED_VALUE;
                ti.zingType = Compiler.SM_ARG_UNION;
            }
            else
                throw new NotImplementedException("Can't register unknown complex type " + t);

            declaredTypes[t] = ti;
        }

        public PType GetPType(Node n)
        {
            if (n is Id)
            {
                var name = (n as Id).Name;

                if (name == PData.Cnst_Any.Node.Name)
                    return new PAnyType();
                else
                    return PType.primtiveTypeFromName((n as Id).Name);
            }
            else
            {
                var fname = ((n as FuncTerm).Function as Id).Name;
                if (fname == PData.Con_TypeTuple.Node.Name)
                {
                    var fieldTypes = new List<PType>();

                    while (n is FuncTerm)
                    {
                        var nf = (FuncTerm)n;

                        fieldTypes.Add(GetPType(nf.Args.ElementAt(0)));
                        n = nf.Args.ElementAt(1);
                    }

                    var type = new PTupleType(fieldTypes);
                    registerType(type);
                    return type;
                }

                if (fname == PData.Con_TypeNamedTuple.Node.Name)
                {
                    var fieldTypes = new List<Tuple<string, PType>>();

                    while (n is FuncTerm)
                    {
                        var nf = (FuncTerm)n;
                        var fName = ((Cnst)Compiler.GetArgByIndex((FuncTerm)Compiler.GetArgByIndex(nf, 0), 0)).GetStringValue();
                        var fType = GetPType(Compiler.GetArgByIndex((FuncTerm)Compiler.GetArgByIndex(nf, 0), 1));
                        fieldTypes.Add(new Tuple<string, PType>(fName, fType));
                        n = nf.Args.ElementAt(1);
                    }

                    var type = new PNamedTupleType(fieldTypes);
                    registerType(type);
                    return type;

                }

                if (fname == PData.Con_TypeSeq.Node.Name)
                {
                    var innerT = GetPType(Compiler.GetArgByIndex((n as FuncTerm), 0));
                    var type = new PSeqType(innerT);
                    registerType(type);
                    return type;
                }

                if (fname == PData.Con_TypeMap.Node.Name)
                {
                    FuncTerm ft = n as FuncTerm;
                    var domain = GetPType(Compiler.GetArgByIndex(ft, 0));
                    var range = GetPType(Compiler.GetArgByIndex(ft, 1));
                    var type = new PMapType(domain, range);
                    registerType(type);
                    return type;
                }

                throw new NotImplementedException("Unknown complex type " + fname);
            }
        }

        private void IndexModel(AST<Model> model)
        {
            model.FindAll(
                new NodePred[]
                {
                    NodePredFactory.Instance.Star,
                    NodePredFactory.Instance.MkPredicate(NodeKind.ModelFact)
                },

                (path, n) =>
                    {
                        var mf = (ModelFact)n;
                        if (mf.Binding != null)
                        {
                            modelAliases.Add(((Id)mf.Binding).Name, (AST<FuncTerm>)Factory.Instance.ToAST(mf.Match));
                        }

                        FuncTerm ft = (FuncTerm)mf.Match;
                        GetBin(ft).AddLast((AST<FuncTerm>)Factory.Instance.ToAST(ft));
                    });

            //// For each machine inject a none event set.
            var mBin = GetBin(PData.Con_MachineDecl.Node.Name);
            var esBin = GetBin(PData.Con_EventSetDecl.Node.Name);
            foreach (var m in mBin)
            {
                var none = Factory.Instance.AddArg(PData.App_EventSetDecl, Factory.Instance.MkCnst("None"));
                esBin.AddLast(Factory.Instance.AddArg(none, m));
            }
        }

        private AST<Model> MkCOutputModel()
        {
            var mod = Factory.Instance.MkModel(
                string.Format("Output{0}", nextOutputId++),
                false,
                Factory.Instance.MkModRef(cdataDomainStr, null, cdataDomainStr + ".4ml"),
                ComposeKind.Extends);

            var conf = (AST<Config>)mod.FindAny(
                new NodePred[] 
                {
                    NodePredFactory.Instance.MkPredicate(NodeKind.AnyNodeKind),
                    NodePredFactory.Instance.MkPredicate(NodeKind.Config)
                });

            var myDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            conf = Factory.Instance.AddSetting(
                conf,
                Factory.Instance.MkId("parsers.C"),
                Factory.Instance.MkCnst("Parser at " +  myDir + "\\CParser.dll"));
            conf = Factory.Instance.AddSetting(
                conf,
                Factory.Instance.MkId("parse_ActiveRenderer"),
                Factory.Instance.MkCnst("C"));

            if (emitDebugC)
            {
                conf = Factory.Instance.AddSetting(
                conf,
                Factory.Instance.MkId("parsers.C.lineDirectives"),
                Factory.Instance.MkCnst("TRUE"));
            }

            return (AST<Model>)Factory.Instance.ToAST(conf.Root);
        }

        private AST<Model> MkZingOutputModel()
        {
            var mod = Factory.Instance.MkModel(
                string.Format("Output{0}", nextOutputId++),
                false,
                Factory.Instance.MkModRef(zingdataDomainStr, null, zingdataDomainStr + ".4ml"),
                ComposeKind.Extends);

            var conf = (AST<Config>)mod.FindAny(
                new NodePred[] 
                {
                    NodePredFactory.Instance.MkPredicate(NodeKind.AnyNodeKind),
                    NodePredFactory.Instance.MkPredicate(NodeKind.Config)
                });

            var myDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            conf = Factory.Instance.AddSetting(
                conf,
                Factory.Instance.MkId("parsers.Zing"),
                Factory.Instance.MkCnst("Parser at " + myDir + "\\ZingParser.dll"));
            conf = Factory.Instance.AddSetting(
                conf,
                Factory.Instance.MkId("parse_ActiveRenderer"),
                Factory.Instance.MkCnst("Zing"));

            return (AST<Model>)Factory.Instance.ToAST(conf.Root);
        }

        #region Print methods
        private bool PrintToStream(AST<Model> m, System.IO.TextWriter wr, Env env)
        {
            var progName = new ProgramName(System.IO.Path.Combine(domainPath, m.Node.Name + ".4ml"));
            var prog = Factory.Instance.MkProgram(progName);
            prog = Factory.Instance.AddModule(prog, m);

            InstallResult result;
            var success = env.Install(prog, out result);
            PrintResult(result);
            if (!success)
            {
                return false;
            }

            Task<RenderResult> renderResult;
            success = env.Render(progName, m.Node.Name, out renderResult);
            if (!success)
            {
                return false;
            }

            renderResult.Wait();
            PrintResult(progName, renderResult.Result);
            if (!renderResult.Result.Succeeded)
            {
                return false;
            }

            var fileQuery = new NodePred[]
            {
                NodePredFactory.Instance.Star,
                NodePredFactory.Instance.MkPredicate(NodeKind.ModelFact),
                NodePredFactory.Instance.MkPredicate(NodeKind.FuncTerm) &
                NodePredFactory.Instance.MkNamePredicate("File")
            };

            renderResult.Result.Module.Print(wr);
            return true;
        }

        private bool PrintFile(AST<Model> m, Env env)
        {
            var progName = new ProgramName(System.IO.Path.Combine(domainPath, m.Node.Name + ".4ml"));
            var prog = Factory.Instance.MkProgram(progName);
            prog = Factory.Instance.AddModule(prog, m);

            System.IO.StreamWriter sw = new System.IO.StreamWriter("output.4ml");
            prog.Print(sw);
            sw.Flush(); 
            sw.Close();

            InstallResult result;
            var success = env.Install(prog, out result);
            PrintResult(result);
            if (!success)
            {
                return false;
            }

            Task<RenderResult> renderResult;
            success = env.Render(progName, m.Node.Name, out renderResult);
            if (!success)
            {
                return false;
            }

            renderResult.Wait();
            PrintResult(progName, renderResult.Result);
            if (!renderResult.Result.Succeeded)
            {
                return false;
            }

            var fileQuery = new NodePred[]
            {
                NodePredFactory.Instance.Star,
                NodePredFactory.Instance.MkPredicate(NodeKind.ModelFact),
                NodePredFactory.Instance.MkPredicate(NodeKind.FuncTerm) &
                NodePredFactory.Instance.MkNamePredicate("File")
            };

            success = true;
            renderResult.Result.Module.FindAll(
                fileQuery,
                (p, n) =>
                {
                    success = PrintFile(n) && success;
                });

            return success;
        }

        private bool PrintFile(Node n)
        {
            var file = (FuncTerm)n;
            var fileName = System.IO.Path.Combine(outputPath, ((Cnst)GetArgByIndex(file, 0)).GetStringValue());
            var fileBody = (Quote)GetArgByIndex(file, 1);

            try
            {
                using (var sw = new System.IO.StreamWriter(fileName))
                {
                    foreach (var c in fileBody.Contents)
                    {
                        Factory.Instance.ToAST(c).Print(sw);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not save file {0} - {1}", fileName, e.Message);
                return false;
            }

            Console.WriteLine("Wrote {0}", fileName);
            return true;
        }

        private void PrintResult(InstallResult result)
        {
            foreach (var f in result.Flags)
            {
                if (f.Item2.Severity != SeverityKind.Error)
                {
                    continue;
                }

                Console.WriteLine(
                    "{0} ({1}, {2}): {3} - {4}",
                    f.Item1.Node.Name,
                    f.Item2.Span.StartLine,
                    f.Item2.Span.StartCol,
                    f.Item2.Severity,
                    f.Item2.Message);
            }
        }

        private void PrintResult(IEnumerable<Flag> flags)
        {
            foreach (var f in flags)
            {
                if (f.Severity != SeverityKind.Error)
                {
                    continue;
                }

                Console.WriteLine(
                    "{0} ({1}, {2}): {3} - {4}",
                    f.ProgramName == null ? "?" : f.ProgramName.ToString(),
                    f.Span.StartLine,
                    f.Span.StartCol,
                    f.Severity,
                    f.Message);
            }
        }

        private void PrintResult(ProgramName name, RenderResult result)
        {
            foreach (var f in result.Flags)
            {
                if (f.Severity != SeverityKind.Error)
                {
                    continue;
                }

                Console.WriteLine(
                    "{0} ({1}, {2}): {3} - {4}",
                    name,
                    f.Span.StartLine,
                    f.Span.StartCol,
                    f.Severity,
                    f.Message);
            }
        }
        #endregion

        #region Static helpers
        public static string getTupleField(int fNum)
        {
            return "field_" + fNum;
        }

        public static string getFuncArg(int fNum)
        {
            return "arg_" + fNum;
        }

        public static string getFtName(FuncTerm n)
        {
            return ((Id)n.Function).Name;
        }

        public static AST<FuncTerm> AddArgs(AST<FuncTerm> ft, params AST<Node>[] args)
        {
            AST<FuncTerm> ret = ft;
            foreach (var v in args)
            {
                ret = Factory.Instance.AddArg(ret, v);
            }
            return ret;
        }

        public static AST<FuncTerm> AddArgs(AST<FuncTerm> ft, IEnumerable<AST<Node>> args)
        {
            AST<FuncTerm> ret = ft;
            foreach (var v in args)
            {
                ret = Factory.Instance.AddArg(ret, v);
            }
            return ret;
        }

        public static AST<Node> ConstructCList(AST<FuncTerm> constructor, IEnumerable<AST<Node>> elems)
        {
            return ConstructList(constructor, elems, CData.Cnst_Nil());
        }

        public static AST<Node> ConstructList(AST<FuncTerm> constructor, IEnumerable<AST<Node>> elems, AST<Node> def = null)
        {
            AST<Node> ret = def == null ? ZingData.Cnst_Nil : def;
            var reverseElems = new List<AST<Node>>(elems);
            reverseElems.Reverse();
            foreach (var n in reverseElems)
            {
                ret = Factory.Instance.AddArg(Factory.Instance.AddArg(constructor, n), ret);
            }

            return ret;
        }

        public static AST<Node> ConstructList(AST<FuncTerm> constructor, params AST<Node>[] elems)
        {
            return ConstructList(constructor, new List<AST<Node>>(elems));
        }

        public static bool isInstanceOf(Node n, AST<FuncTerm> m)
        {
            return isInstanceOf(n, (Id)m.Node.Function);
        }

        public static bool isInstanceOf(Node n, AST<Id> m)
        {
            return isInstanceOf(n, m.Node);
        }

        public static bool isInstanceOf(Node n, Id m)
        {
            return n is FuncTerm && getFtName(n as FuncTerm) == m.Name;
        }

        public static Node GetArgByIndex(FuncTerm ft, int index)
        {
            Contract.Requires(index >= 0 && index < ft.Args.Count);

            int i = 0;
            foreach (var a in ft.Args)
            {
                if (i == index)
                {
                    return a;
                }
                else
                {
                    ++i;
                }
            }

            throw new InvalidOperationException();
        }

        public static AST<Model> Add(AST<Model> m, AST<FuncTerm> ft)
        {
            return Factory.Instance.AddFact(m, Factory.Instance.MkModelFact(null, ft));
        }

        public static AST<Node> ConstructPartialList(AST<FuncTerm> constructor, string placeHolder, params AST<Node>[] elems)
        {
            return ConstructList(constructor, new List<AST<Node>>(elems), Factory.Instance.MkId(placeHolder));
        }

        public static AST<Node> ReverseList(AST<Node> list)
        {
            List<AST<Node>> els = new List<AST<Node>>();
            var listN = list.Node as FuncTerm;
            var constr = Factory.Instance.MkFuncTerm(Factory.Instance.MkId(((Id)listN.Function).Name));

            while (listN != null)
            {
                els.Insert(0, Factory.Instance.ToAST(listN.Args.ElementAt(0)));
                listN = listN.Args.ElementAt(1) as FuncTerm;
            }

            return ConstructList(constr, els);
        }
        #endregion
    }
}