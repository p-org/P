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

namespace Microsoft.Pc
{
    internal class ZingTranslationInfo
    {
        public AST<Node> node = null;

        public ZingTranslationInfo(AST<Node> n)
        {
            this.node = n;
        }
    }

    internal class TransitionInfo
    {
        public string target;
        public bool isPush;
        public string exitFunName;

        public TransitionInfo(string target, bool isPush)
        {
            this.target = target;
            this.isPush = isPush;
            this.exitFunName = null;
        }

        public TransitionInfo(string target, string exitFunName)
        {
            this.target = target;
            this.isPush = false;
            this.exitFunName = exitFunName;
        }
    }

    internal class StateInfo
    {
        public string ownerName;
        public string entryActionName;
        public string exitFunName;
        public bool hasDefaultTransition;
        public Dictionary<string, TransitionInfo> transitions;
        public Dictionary<string, string> actions;
        public List<string> deferredEvents;
        public List<string> ignoredEvents;
        public bool isHot;
        public string printedName;

        public StateInfo(string ownerName, string entryActionName, string exitFunName, bool isHot, string printedName)
        {
            this.ownerName = ownerName;
            this.entryActionName = entryActionName;
            this.exitFunName = exitFunName;
            this.hasDefaultTransition = false;
            this.transitions = new Dictionary<string, TransitionInfo>();
            this.actions = new Dictionary<string, string>();
            this.deferredEvents = new List<string>();
            this.ignoredEvents = new List<string>();
            this.isHot = isHot;
            this.printedName = printedName;
        }
    }

    internal class VariableInfo
    {
        public FuncTerm type;

        public VariableInfo(FuncTerm type)
        {
            this.type = type;
        }
    }

    internal class FunInfo
    {
        public bool isModel;
        public AST<FuncTerm> returnType;
        public Node body;
        public Dictionary<string, VariableInfo> parameterNameToInfo;
        public List<string> parameterNames;
        public int numFairChoices;
        public bool isAnonymous;
        public Dictionary<AST<Node>, FuncTerm> typeInfo;

        public FunInfo(bool isModel, AST<FuncTerm> returnType, Node body)
        {
            this.isModel = isModel;
            this.returnType = returnType;
            this.body = body;
            this.parameterNameToInfo = new Dictionary<string, VariableInfo>();
            this.parameterNames = new List<string>();
            this.numFairChoices = 0;
            this.isAnonymous = false;
            this.typeInfo = new Dictionary<AST<Node>, FuncTerm>();
        }

        public FunInfo(Node body, bool isAnonymous)
        {
            this.isModel = false;
            this.returnType = PToZing.PTypeNull;
            this.body = body;
            this.parameterNameToInfo = new Dictionary<string, VariableInfo>();
            this.parameterNames = new List<string>();
            this.numFairChoices = 0;
            this.isAnonymous = isAnonymous;
            this.typeInfo = new Dictionary<AST<Node>, FuncTerm>();
        }
    }

    enum MonitorType { SAFETY, FINALLY, REPEATEDLY };

    internal class MachineInfo
    {
        public bool IsReal { get { return type == "REAL"; } }
        public bool IsModel { get { return type == "MODEL"; } }
        public bool IsMonitor { get { return type == "MONITOR"; } }

        public string type;
        public int maxQueueSize;
        public bool maxQueueSizeAssumed;
        public string initStateName;
        public Dictionary<string, StateInfo> stateNameToStateInfo;
        public Dictionary<string, VariableInfo> localVariableToVarInfo;
        public Dictionary<string, List<string>> eventSetNameToEvents;
        public Dictionary<string, FunInfo> funNameToFunInfo;
        public MonitorType monitorType;

        public MachineInfo()
        {
            type = "REAL";
            maxQueueSize = -1;
            maxQueueSizeAssumed = false;
            initStateName = null;
            stateNameToStateInfo = new Dictionary<string, StateInfo>();
            localVariableToVarInfo = new Dictionary<string, VariableInfo>();
            eventSetNameToEvents = new Dictionary<string, List<string>>();
            funNameToFunInfo = new Dictionary<string, FunInfo>();
            monitorType = MonitorType.SAFETY;
        }
    }

    internal class EventInfo
    {
        public int maxInstances;  // -1 represents no bound
        public bool maxInstancesAssumed;
        public FuncTerm payloadType;

        public EventInfo(FuncTerm payloadType)
        {
            this.payloadType = payloadType;
            this.maxInstances = -1;
        }

        public EventInfo(int maxInstances, bool maxInstancesAssumed, FuncTerm payloadType)
        {
            this.maxInstances = maxInstances;
            this.maxInstancesAssumed = maxInstancesAssumed;
            this.payloadType = payloadType;
        }
    }

    class PToZing
    {
        public static AST<FuncTerm> PTypeNull = AddArgs(Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")), Factory.Instance.MkId("NULL"));
        public static AST<FuncTerm> PTypeBool = AddArgs(Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")), Factory.Instance.MkId("BOOL"));
        public static AST<FuncTerm> PTypeInt = AddArgs(Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")), Factory.Instance.MkId("INT"));
        public static AST<FuncTerm> PTypeEvent = AddArgs(Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")), Factory.Instance.MkId("EVENT"));
        public static AST<FuncTerm> PTypeReal = AddArgs(Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")), Factory.Instance.MkId("REAL"));
        public static AST<FuncTerm> PTypeModel = AddArgs(Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")), Factory.Instance.MkId("MODEL"));
        public static AST<FuncTerm> PTypeAny = AddArgs(Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")), Factory.Instance.MkId("ANY"));

        public const string SM_EVENT = "SM_EVENT";
        public static AST<Node> SmEvent = Factory.Instance.MkCnst("SM_EVENT");
        public const string SM_HANDLE = "SM_HANDLE";
        public static AST<Node> SmHandle = Factory.Instance.MkCnst("SM_HANDLE");
        public static string SM_EVENT_SET = "SM_EVENT_SET";
        public static AST<Node> SmEventSet = Factory.Instance.MkCnst("SM_EVENT_SET");
        public const string PRT_VALUE = "PRT_VALUE";
        public static AST<Node> PrtValue = Factory.Instance.MkCnst("PRT_VALUE");
        public static AST<Node> PrtCastValue = MkZingDot("PRT_VALUE", "PrtCastValue");
        public static AST<Node> PrtMkDefaultValue = MkZingDot("PRT_VALUE", "PrtMkDefaultValue");
        public static AST<Node> PrtCloneValue = MkZingDot("PRT_VALUE", "PrtCloneValue");
        public static AST<Node> PrtIsEqualValue = MkZingDot("PRT_VALUE", "PrtIsEqualValue");

        public const string NullEvent = "null";
        public const string HaltEvent = "halt";

        private Dictionary<string, LinkedList<AST<FuncTerm>>> factBins;
        public Dictionary<string, EventInfo> allEvents;
        public Dictionary<string, MachineInfo> allMachines;
        public string mainMachineName;
        private Dictionary<AST<Node>, string> anonFunToName;

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

        private void IndexModel(AST<Model> model)
        {
            this.factBins = new Dictionary<string, LinkedList<AST<FuncTerm>>>();
            model.FindAll(
                new NodePred[]
                {
                    NodePredFactory.Instance.Star,
                    NodePredFactory.Instance.MkPredicate(NodeKind.ModelFact)
                },

                (path, n) =>
                {
                    var mf = (ModelFact)n;
                    FuncTerm ft = (FuncTerm)mf.Match;
                    GetBin(ft).AddLast((AST<FuncTerm>)Factory.Instance.ToAST(ft));
                });
        }

        public string GetMachineName(FuncTerm ft, int index)
        {
            FuncTerm machineDecl = (FuncTerm)GetArgByIndex(ft, index);
            var machineName = GetName(machineDecl, 0);
            return machineName;
        }

        Dictionary<string, int> uniqIDCounters = new Dictionary<string, int>();
        public string GetUnique(string prefix)
        {
            if (!uniqIDCounters.ContainsKey(prefix))
                uniqIDCounters[prefix] = 0;

            var ret = uniqIDCounters[prefix];
            uniqIDCounters[prefix]++;
            return prefix + '_' + ret;
        }

        Compiler compiler;
        public PToZing(Compiler compiler, AST<Model> model)
        {
            this.compiler = compiler;
            this.typeContext = new TypeTranslationContext();
            IndexModel(model);
            GenerateProgramData(model);
        }

        private void GenerateProgramData(AST<Model> model)
        {
            allEvents = new Dictionary<string, EventInfo>();
            allEvents[HaltEvent] = new EventInfo(1, false, PTypeNull.Node);
            allEvents[NullEvent] = new EventInfo(1, false, PTypeNull.Node);
            allMachines = new Dictionary<string, MachineInfo>();

            LinkedList<AST<FuncTerm>> terms;

            terms = GetBin("EventDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var bound = it.Current;
                    it.MoveNext();
                    var payloadType = (FuncTerm)(it.Current.NodeKind == NodeKind.Id ? PTypeNull.Node : it.Current);
                    if (bound.NodeKind == NodeKind.Id)
                    {
                        allEvents[name] = new EventInfo(payloadType);
                    }
                    else
                    {
                        var ft = (FuncTerm)bound;
                        var maxInstances = (int)((Cnst)GetArgByIndex(ft, 0)).GetNumericValue().Numerator;
                        var maxInstancesAssumed = ((Id)ft.Function).Name == "AssumeMaxInstances";
                        allEvents[name] = new EventInfo(maxInstances, maxInstancesAssumed, payloadType);
                    }
                }
            }

            terms = GetBin("MachineDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var machineName = ((Cnst)it.Current).GetStringValue();
                    allMachines[machineName] = new MachineInfo();
                    it.MoveNext();
                    allMachines[machineName].type = ((Id)it.Current).Name;
                    it.MoveNext();
                    var bound = it.Current;
                    if (bound.NodeKind != NodeKind.Id)
                    {
                        var ft = (FuncTerm)bound;
                        allMachines[machineName].maxQueueSize = (int)((Cnst)GetArgByIndex(ft, 0)).GetNumericValue().Numerator;
                        allMachines[machineName].maxQueueSizeAssumed = ((Id)ft.Function).Name == "AssumeMaxInstances";
                    }
                    it.MoveNext();
                    allMachines[machineName].initStateName = GetNameFromQualifiedName(machineName, (FuncTerm)it.Current);
                    it.MoveNext();
                    if (((Id)it.Current).Name == "TRUE")
                    {
                        mainMachineName = machineName;
                    }
                }
            }

            terms = GetBin("VarDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var varName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var machineDecl = (FuncTerm)it.Current;
                    var machineName = GetName(machineDecl, 0);
                    var varTable = allMachines[machineName].localVariableToVarInfo;
                    it.MoveNext();
                    var type = (FuncTerm)it.Current;
                    varTable[varName] = new VariableInfo(type);
                }
            }

            terms = GetBin("FunDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string funName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var machineDecl = (FuncTerm)it.Current;
                    var machineName = GetName(machineDecl, 0);
                    var machineInfo = allMachines[machineName];
                    it.MoveNext();
                    var isModel = ((Id)it.Current).Name == "MODEL";
                    it.MoveNext();
                    var iter = it.Current as FuncTerm;
                    it.MoveNext();
                    var returnTypeName = it.Current is Id ? PTypeNull : (AST<FuncTerm>)Factory.Instance.ToAST(it.Current);
                    it.MoveNext();
                    var funInfo = new FunInfo(isModel, returnTypeName, it.Current);
                    while (iter != null)
                    {
                        var ft = (FuncTerm)GetArgByIndex(iter, 0);
                        using (var enumerator = ft.Args.GetEnumerator())
                        {
                            enumerator.MoveNext();
                            var varName = ((Cnst)enumerator.Current).GetStringValue();
                            enumerator.MoveNext();
                            var varType = (FuncTerm)enumerator.Current;
                            funInfo.parameterNameToInfo[varName] = new VariableInfo(varType);
                            funInfo.parameterNames.Add(varName);
                        }
                        iter = GetArgByIndex(iter, 1) as FuncTerm;
                    }
                    machineInfo.funNameToFunInfo[funName] = funInfo;
                }
            }

            this.anonFunToName = new Dictionary<AST<Node>, string>();
            terms = GetBin("AnonFunDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var machineDecl = (FuncTerm)it.Current;
                    var machineName = GetName(machineDecl, 0);
                    var machineInfo = allMachines[machineName];
                    it.MoveNext();
                    var funName = "AnonFun" + anonFunToName.Count;
                    machineInfo.funNameToFunInfo[funName] = new FunInfo(it.Current, true);
                    anonFunToName[term] = funName;
                }
            }

            terms = GetBin("StateDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var qualifiedStateName = (FuncTerm)it.Current;
                    it.MoveNext();
                    var machineDecl = (FuncTerm)it.Current;
                    var ownerName = GetName(machineDecl, 0);
                    var stateName = GetNameFromQualifiedName(ownerName, qualifiedStateName);
                    it.MoveNext();
                    var entryActionName = it.Current.NodeKind == NodeKind.Cnst
                                            ? ((Cnst)it.Current).GetStringValue()
                                            : anonFunToName[Factory.Instance.ToAST(it.Current)];
                    it.MoveNext();
                    var exitFunName = it.Current.NodeKind == NodeKind.Cnst
                                            ? ((Cnst)it.Current).GetStringValue()
                                            : anonFunToName[Factory.Instance.ToAST(it.Current)];
                    it.MoveNext();
                    var isHot = compiler.Options.liveness != LivenessOption.None && ((Id)it.Current).Name == "TRUE";
                    var stateTable = allMachines[ownerName].stateNameToStateInfo;
                    stateTable[stateName] = new StateInfo(ownerName, entryActionName, exitFunName, isHot, GetPrintedNameFromQualifiedName(qualifiedStateName));
                }
            }

            terms = GetBin("TransDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var stateDecl = (FuncTerm)it.Current;
                    var qualifiedStateName = (FuncTerm)GetArgByIndex(stateDecl, 0);
                    var stateOwnerMachineName = GetMachineName(stateDecl, 1);
                    var stateName = GetNameFromQualifiedName(stateOwnerMachineName, qualifiedStateName);
                    var stateTable = allMachines[stateOwnerMachineName].stateNameToStateInfo[stateName];
                    it.MoveNext();
                    string eventName;
                    if (it.Current.NodeKind == NodeKind.Id)
                    {
                        var name = ((Id)it.Current).Name;
                        if (name == "DEFAULT")
                        {
                            eventName = NullEvent;
                            stateTable.hasDefaultTransition = true;
                        }
                        else
                        {
                            // name == "HALT"
                            eventName = HaltEvent;
                        }
                    }
                    else
                    {
                        eventName = ((Cnst)it.Current).GetStringValue();
                    }
                    it.MoveNext();
                    var targetStateName = GetNameFromQualifiedName(stateOwnerMachineName, (FuncTerm)it.Current);
                    it.MoveNext();
                    if (it.Current.NodeKind == NodeKind.Id)
                    {
                        stateTable.transitions[eventName] = new TransitionInfo(targetStateName, ((Id)it.Current).Name == "PUSH");
                    }
                    else
                    {
                        var exitFunName = it.Current.NodeKind == NodeKind.Cnst
                                            ? ((Cnst)it.Current).GetStringValue()
                                            : anonFunToName[Factory.Instance.ToAST(it.Current)];
                        stateTable.transitions[eventName] = new TransitionInfo(targetStateName, exitFunName);
                    }
                }
            }

            terms = GetBin("DoDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var stateDecl = (FuncTerm)it.Current;
                    var qualifiedStateName = (FuncTerm)GetArgByIndex(stateDecl, 0);
                    var stateOwnerMachineName = GetMachineName(stateDecl, 1);
                    var stateName = GetNameFromQualifiedName(stateOwnerMachineName, qualifiedStateName);
                    var stateTable = allMachines[stateOwnerMachineName].stateNameToStateInfo[stateName];
                    it.MoveNext();
                    string eventName;
                    if (it.Current.NodeKind == NodeKind.Id)
                    {
                        eventName = HaltEvent;
                    }
                    else
                    {
                        eventName = ((Cnst)it.Current).GetStringValue();
                    }
                    it.MoveNext();
                    var action = it.Current;
                    if (action.NodeKind == NodeKind.Cnst)
                    {
                        stateTable.actions[eventName] = ((Cnst)action).GetStringValue();
                    }
                    else if (action.NodeKind == NodeKind.Id)
                    {
                        if (((Id)action).Name == "DEFER")
                        {
                            stateTable.deferredEvents.Add(eventName);
                        }
                        else
                        {
                            // ((Id)action).Name == "IGNORE"
                            stateTable.ignoredEvents.Add(eventName);
                        }
                    }
                    else
                    {
                        stateTable.actions[eventName] = anonFunToName[Factory.Instance.ToAST(action)];
                    }
                }
            }

            terms = GetBin("TypeOf");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    FuncTerm typingContext = (FuncTerm)it.Current;
                    it.MoveNext();
                    var expr = Factory.Instance.ToAST(it.Current);
                    it.MoveNext();
                    var type = it.Current as FuncTerm;
                    if (type == null) continue;

                    string typingContextKind = ((Id)typingContext.Function).Name;
                    if (typingContextKind == "FunDecl")
                    {
                        string ownerName = GetOwnerName(typingContext, 1, 0);
                        string funName = GetName(typingContext, 0);
                        allMachines[ownerName].funNameToFunInfo[funName].typeInfo[expr] = type;
                    }
                    else
                    {
                        // typingContextKind == "AnonFunDecl"
                        string ownerName = GetOwnerName(typingContext, 0, 0);
                        string funName = anonFunToName[Factory.Instance.ToAST(typingContext)];
                        allMachines[ownerName].funNameToFunInfo[funName].typeInfo[expr] = type;
                    }
                }
            }

            foreach (var machineName in allMachines.Keys)
            {
                if (!allMachines[machineName].IsMonitor) continue;
                var machineInfo = allMachines[machineName];
                HashSet<string> visitedStates = new HashSet<string>();
                Stack<string> dfsStack = new Stack<string>();
                foreach (var stateName in machineInfo.stateNameToStateInfo.Keys)
                {
                    if (machineInfo.stateNameToStateInfo[stateName].isHot)
                    {
                        machineInfo.monitorType = MonitorType.FINALLY;
                        continue;
                    }
                    dfsStack.Push(stateName);
                    while (dfsStack.Count > 0)
                    {
                        var curState = dfsStack.Pop();
                        var curStateInfo = machineInfo.stateNameToStateInfo[curState];
                        foreach (var e in curStateInfo.transitions.Keys)
                        {
                            var nextState = curStateInfo.transitions[e].target;
                            if (visitedStates.Contains(nextState)) continue;
                            visitedStates.Add(nextState);
                            dfsStack.Push(nextState);
                        }
                    }
                }
                foreach (var stateName in visitedStates)
                {
                    if (machineInfo.stateNameToStateInfo[stateName].isHot)
                    {
                        machineInfo.monitorType = MonitorType.REPEATEDLY;
                        break;
                    }
                }
            }
        }

        #region Static helpers
        public static string NodeToString(Node n)
        {
            System.IO.StringWriter sw = new System.IO.StringWriter();
            Factory.Instance.ToAST(n).Print(sw);
            return sw.ToString();
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

        public static string GetName(FuncTerm ft, int nameIndex)
        {
            return ((Cnst)GetArgByIndex(ft, nameIndex)).GetStringValue();
        }

        public static string GetPrintedNameFromQualifiedName(FuncTerm qualifiedName)
        {
            var stateName = GetName(qualifiedName, 0);
            while (true)
            {
                qualifiedName = GetArgByIndex(qualifiedName, 1) as FuncTerm;
                if (qualifiedName == null) break;
                stateName = stateName + "." + GetName(qualifiedName, 0);
            }
            return stateName;
        }

        public static string GetNameFromQualifiedName(string machineName, FuncTerm qualifiedName)
        {
            var stateName = machineName;
            while (qualifiedName != null)
            {
                stateName = stateName + "_" + GetName(qualifiedName, 0);
                qualifiedName = GetArgByIndex(qualifiedName, 1) as FuncTerm;
            }
            return stateName;
        }

        public string GetOwnerName(FuncTerm ft, int ownerIndex, int ownerNameIndex)
        {
            var ownerArg = GetArgByIndex(ft, ownerIndex);
            switch (ownerArg.NodeKind)
            {
                case NodeKind.Id:
                    return ((Cnst)GetArgByIndex((FuncTerm)ownerArg, ownerNameIndex)).GetStringValue();
                case NodeKind.FuncTerm:
                    return ((Cnst)GetArgByIndex((FuncTerm)ownerArg, ownerNameIndex)).GetStringValue();
                default:
                    throw new InvalidOperationException();
            }
        }

        public static AST<Model> Add(AST<Model> m, AST<FuncTerm> ft)
        {
            return Factory.Instance.AddFact(m, Factory.Instance.MkModelFact(null, ft));
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
        private static AST<FuncTerm> MkZingFile(string filename, List<AST<Node>> elements)
        {
            return AddArgs(ZingData.App_File, Factory.Instance.MkCnst(filename), ConstructList(ZingData.App_Decls, elements));
        }

        private static AST<FuncTerm> MkZingIdentifier(string name)
        {
            return Factory.Instance.AddArg(ZingData.App_Identifier, Factory.Instance.MkCnst(name));
        }

        private static AST<FuncTerm> MkZingDot(params string[] names)
        {
            AST<FuncTerm> lhs = MkZingIdentifier(names[0]);
            for (int i = 1; i < names.Length; i++)
            {
                AST<FuncTerm> rhs = MkZingIdentifier(names[i]);
                lhs = MkZingApply(ZingData.Cnst_Dot, lhs, rhs);
            }
            return lhs;
        }

        private static AST<Node> MkZingDot(AST<Node> lhs, string member)
        {
            return MkZingApply(ZingData.Cnst_Dot, lhs, MkZingIdentifier(member));
        }

        private static AST<Node> MkZingEvent(string eventName)
        {
            return MkZingDot("Main", string.Format("{0}_SM_EVENT", eventName));
        }

        private static AST<Node> MkZingState(string stateName)
        {
            return MkZingDot("State", string.Format("_{0}", stateName));
        }

        private static AST<FuncTerm> MkZingReturn(AST<Node> rVal)
        {
            return AddArgs(ZingData.App_Return, rVal);
        }

        public static AST<FuncTerm> MkZingIfThen(AST<Node> cond, AST<Node> then)
        {
            return AddArgs(ZingData.App_ITE, cond, then, ZingData.Cnst_Nil);
        }

        public static AST<FuncTerm> MkZingIfThenElse(AST<Node> cond, AST<Node> thenstmt, AST<Node> elsestmt)
        {
            return AddArgs(ZingData.App_ITE, cond, thenstmt, elsestmt);
        }

        private static AST<Node> MkZingNeq(AST<Node> e1, AST<Node> e2)
        {
            return MkZingApply(ZingData.Cnst_NEq, e1, e2);
        }

        private static AST<Node> MkZingEq(AST<Node> e1, AST<Node> e2)
        {
            return MkZingApply(ZingData.Cnst_Eq, e1, e2);
        }

        private static AST<Node> MkZingOr(AST<Node> e1, AST<Node> e2)
        {
            return MkZingApply(ZingData.Cnst_Or, e1, e2);
        }

        private static void AddEventSet(List<AST<FuncTerm>> stmts, IEnumerable<string> eventNames, AST<FuncTerm> set)
        {
            foreach (var eventName in eventNames)
            {
                var addExpr = MkZingApply(ZingData.Cnst_Add, set, MkZingEvent(eventName));
                var addStmt = AddArgs(ZingData.App_Assign, set, addExpr);
                stmts.Add(addStmt);
            }
        }

        private static void SubtractEventSet(List<AST<FuncTerm>> stmts, IEnumerable<string> eventNames, AST<FuncTerm> actionSet)
        {
            foreach (var eventName in eventNames)
            {
                var subExpr = MkZingApply(ZingData.Cnst_Sub, actionSet, MkZingEvent(eventName));
                var subStmt = AddArgs(ZingData.App_Assign, actionSet, subExpr);
                stmts.Add(subStmt);
            }
        }
        private static AST<FuncTerm> MkZingCall(AST<Node> methodExpr, params AST<Node>[] args)
        {
            List<AST<Node>> argList = new List<AST<Node>>();
            argList.Add(methodExpr);
            foreach (var arg in args)
                argList.Add(arg);
            return Factory.Instance.AddArg(ZingData.App_Call, ConstructList(ZingData.App_Args, argList));
        }

        private static AST<FuncTerm> MkZingStrings(IEnumerable<AST<Node>> args)
        {
            List<AST<Node>> argList = new List<AST<Node>>();
            foreach (var arg in args)
                argList.Add(arg);
            return AddArgs(ZingData.App_Apply, args);
        }
        private static AST<FuncTerm> MkZingCall(AST<Node> methodExpr, IEnumerable<AST<Node>> args)
        {
            List<AST<Node>> argList = new List<AST<Node>>();
            argList.Add(methodExpr);
            foreach (var arg in args)
                argList.Add(arg);
            return Factory.Instance.AddArg(ZingData.App_Call, ConstructList(ZingData.App_Args, argList));
        }

        private static AST<FuncTerm> MkZingCallStmt(AST<Node> callExpr, params AST<Node>[] attrs)
        {
            return AddArgs(ZingData.App_CallStmt, callExpr, ConstructList(ZingData.App_Attrs, attrs));
        }

        private static AST<FuncTerm> MkZingMethodDecl(string methodName, AST<Node> parameters, AST<Node> returnType, AST<Node> locals, AST<Node> body, params AST<Node>[] attrs)
        {
            return AddArgs(ZingData.App_MethodDecl, Factory.Instance.MkCnst(methodName), parameters, returnType, locals, body, ConstructList(ZingData.App_Attrs, attrs));
        }

        private static AST<FuncTerm> MkZingGoto(string labelName)
        {
            return Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst(labelName));
        }

        private static AST<FuncTerm> MkZingBlock(string blockName, AST<Node> body)
        {
            return AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst(blockName), body);
        }

        private static AST<Node> MkZingBlocks(params AST<Node>[] blocks)
        {
            return ConstructList(ZingData.App_Blocks, blocks);
        }

        private static AST<FuncTerm> MkZingAssign(AST<Node> lhs, AST<Node> rhs)
        {
            return AddArgs(ZingData.App_Assign, lhs, rhs);
        }

        private static AST<FuncTerm> MkZingVarDecl(string varName, AST<Node> varType, params AST<Node>[] attrs)
        {
            return AddArgs(ZingData.App_VarDecl, Factory.Instance.MkCnst(varName), varType, ConstructList(ZingData.App_Attrs, attrs));
        }

        private static AST<Node> MkZingVarDecls(IEnumerable<AST<Node>> vars)
        {
            return ConstructList(ZingData.App_VarDecls, vars);
        }

        private static AST<FuncTerm> MkZingApply(AST<Id> op, params AST<Node>[] args)
        {
            return AddArgs(ZingData.App_Apply, op, ConstructList(ZingData.App_Args, args));
        }

        private static AST<Node> MkZingActionOrFun(string machineName, string entityName)
        {
            return MkZingDot("ActionOrFun", string.Format("_{0}_{1}", machineName, entityName));
        }

        private static AST<Node> MkZingVarDecls(params AST<Node>[] vars)
        {
            return ConstructList(ZingData.App_VarDecls, vars);
        }

        private static AST<Node> MkZingIndex(AST<Node> baseExp, AST<Node> indExp)
        {
            return MkZingApply(ZingData.Cnst_Index, baseExp, indExp);
        }

        private static AST<Node> MkZingSeq(List<AST<Node>> stmts)
        {
            AST<Node> ret = ZingData.Cnst_Nil;
            foreach (var stmt in stmts.Reverse<AST<Node>>())
            {
                ret = AddArgs(ZingData.App_Seq, stmt, ret);
            }
            return ret;
        }

        private static AST<Node> MkZingSeq(params AST<Node>[] stmts)
        {
            return MkZingSeq(new List<AST<Node>>(stmts));
        }

        private static AST<FuncTerm> MkZingAssert(AST<Node> condition)
        {
            return AddArgs(ZingData.App_Assert, condition);
        }

        private static AST<FuncTerm> MkZingAssume(AST<Node> condition)
        {
            return AddArgs(ZingData.App_Assume, condition);
        }

        private static AST<FuncTerm> MkZingAdd(AST<Node> a, AST<Node> b)
        {
            return MkZingApply(ZingData.Cnst_Add, a, b);
        }

        private static AST<FuncTerm> MkZingSub(AST<Node> a, AST<Node> b)
        {
            return MkZingApply(ZingData.Cnst_Sub, a, b);
        }
        #endregion

        #region ZingCompiler
        public void GenerateZing(ref AST<Model> outModel)
        {
            List<AST<Node>> elements = new List<AST<Node>>();
            MkZingEnums(elements);
            MkZingClasses(elements);
            outModel = Add(outModel, MkZingFile("output.zing", elements));
        }

        private void MkZingEnums(List<AST<Node>> elements)
        {
            List<AST<Node>> machineConsts = new List<AST<Node>>();
            machineConsts.Add(Factory.Instance.MkCnst("_default"));
            foreach (string machineName in allMachines.Keys)
            {
                machineConsts.Add(Factory.Instance.MkCnst(string.Format("_{0}", machineName)));
            }
            var machineList = ConstructList(ZingData.App_EnumElems, machineConsts);
            elements.Add(AddArgs(ZingData.App_EnumDecl, Factory.Instance.MkCnst("Machine"), machineList));

            List<AST<Node>> eventConsts = new List<AST<Node>>();
            eventConsts.Add(Factory.Instance.MkCnst("_default"));
            foreach (string eventName in allEvents.Keys.Where(x => x != NullEvent))
            {
                eventConsts.Add(Factory.Instance.MkCnst(string.Format("_{0}", eventName)));
            }
            var eventList = ConstructList(ZingData.App_EnumElems, eventConsts);
            elements.Add(AddArgs(ZingData.App_EnumDecl, Factory.Instance.MkCnst("Event"), eventList));

            List<AST<Node>> stateConsts = new List<AST<Node>>();
            stateConsts.Add(Factory.Instance.MkCnst("_default"));
            foreach (var machine in allMachines.Values)
            {
                foreach (var stateName in machine.stateNameToStateInfo.Keys)
                {
                    stateConsts.Add(Factory.Instance.MkCnst(string.Format("_{0}", stateName)));
                }
            }
            var stateList = ConstructList(ZingData.App_EnumElems, stateConsts);
            elements.Add(AddArgs(ZingData.App_EnumDecl, Factory.Instance.MkCnst("State"), stateList));

            List<AST<Node>> actionOrFunConsts = new List<AST<Node>>();
            actionOrFunConsts.Add(Factory.Instance.MkCnst("_default"));
            foreach (string machineName in allMachines.Keys)
            {
                foreach (string funName in allMachines[machineName].funNameToFunInfo.Keys)
                {
                    actionOrFunConsts.Add(Factory.Instance.MkCnst(string.Format("_{0}_{1}", machineName, funName)));
                }
            }
            elements.Add(AddArgs(ZingData.App_EnumDecl, Factory.Instance.MkCnst("ActionOrFun"), ConstructList(ZingData.App_EnumElems, actionOrFunConsts)));

            foreach (var machineName in allMachines.Keys)
            {
                if (!allMachines[machineName].IsMonitor) continue;
                elements.Add(AddArgs(ZingData.App_SetDecl, Factory.Instance.MkCnst(string.Format("{0}_set", machineName)), Factory.Instance.MkCnst(machineName)));
            }
        }

        private string GetMonitorMachineName(string machineName)
        {
            return string.Format("{0}_handles", machineName);
        }

        private string GetFairChoice(string entityName, int i)
        {
            return string.Format("FairChoice_{0}_{1}", entityName, i);
        }

        private AST<FuncTerm> GenerateMainClass()
        {
            // Generate Main
            AST<Node> fields = ZingData.Cnst_Nil;
            foreach (var eventName in allEvents.Keys)
            {
                var field = MkZingVarDecl(string.Format("{0}_SM_EVENT", eventName), SmEvent, ZingData.Cnst_Static);
                fields = AddArgs(ZingData.App_VarDecls, field, fields);
            }
            foreach (var machineName in allMachines.Keys)
            {
                var field = MkZingVarDecl(string.Format("{0}_instance", machineName), ZingData.Cnst_Int, ZingData.Cnst_Static);
                fields = AddArgs(ZingData.App_VarDecls, field, fields);
            }
            foreach (var machineName in allMachines.Keys)
            {
                if (!allMachines[machineName].IsMonitor) continue;
                var field = MkZingVarDecl(GetMonitorMachineName(machineName), Factory.Instance.MkCnst(string.Format("{0}_set", machineName)), ZingData.Cnst_Static);
                fields = AddArgs(ZingData.App_VarDecls, field, fields);
            }

            AST<Node> methods = ZingData.Cnst_Nil;
            foreach (var machineName in allMachines.Keys)
            {
                AST<Node> method;
                if (allMachines[machineName].IsMonitor)
                {
                    method = MkCreateMonitorMethod(machineName);
                }
                else
                {
                    method = MkCreateMachineMethod(machineName);
                }
                methods = AddArgs(ZingData.App_MethodDecls, method, methods);
            }
            foreach (var machineName in allMachines.Keys)
            {
                if (!allMachines[machineName].IsMonitor) continue;
                AST<Node> method = MkInvokeMonitorMethod(machineName);
                methods = AddArgs(ZingData.App_MethodDecls, method, methods);
            }

            // Generate method for computing complement of a set of events
            AST<Node> calculateComplementParameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("eventSet", SmEventSet), ZingData.Cnst_Nil);
            AST<Node> calculateComplementLocalVars = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("returnEventSet", SmEventSet), ZingData.Cnst_Nil);
            AST<Node> calculateComplementBody = MkZingAssign(MkZingIdentifier("returnEventSet"), AddArgs(ZingData.App_New, SmEventSet, ZingData.Cnst_Nil));
            foreach (var eventName in allEvents.Keys)
            {
                if (eventName == HaltEvent)
                    continue;
                var iteExpr = MkZingApply(ZingData.Cnst_In, MkZingEvent(eventName), MkZingIdentifier("eventSet"));
                var assignStmt = MkZingAssign(MkZingIdentifier("returnEventSet"), MkZingApply(ZingData.Cnst_Add, MkZingIdentifier("returnEventSet"), MkZingEvent(eventName)));
                var iteStmt = MkZingIfThenElse(iteExpr, ZingData.Cnst_Nil, assignStmt);
                calculateComplementBody = MkZingSeq(calculateComplementBody, iteStmt);
            }
            calculateComplementBody = MkZingSeq(calculateComplementBody, AddArgs(ZingData.App_Return, MkZingIdentifier("returnEventSet")));
            calculateComplementBody = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("dummy"), calculateComplementBody);
            calculateComplementBody = ConstructList(ZingData.App_Blocks, calculateComplementBody);
            AST<Node> calculateComplementMethod = MkZingMethodDecl("CalculateComplementOfEventSet", calculateComplementParameters, SmEventSet, calculateComplementLocalVars, calculateComplementBody, ZingData.Cnst_Static);
            methods = AddArgs(ZingData.App_MethodDecls, calculateComplementMethod, methods);

            // Generate the PayloadOf static method. PayloadOf :: (EventId) -> Discriminator
            // Returns the statically declared type for the given event.
            var payloadOfBody = new List<AST<Node>>();
            payloadOfBody.Add(MkZingIfThen(MkZingEq(MkZingIdentifier("e"), MkZingIdentifier("null")),
                MkZingReturn(typeContext.PTypeToZingExpr(PTypeNull.Node))));
            foreach (var evt in allEvents.Keys.Where(x => x != NullEvent))
            {
                payloadOfBody.Add(MkZingIfThenElse(MkZingEq(MkZingDot("e", "name"), MkZingDot("Event", "_" + evt)),
                    AddArgs(ZingData.App_Return, typeContext.PTypeToZingExpr(allEvents[evt].payloadType)),
                    ZingData.Cnst_Nil));
            }
            payloadOfBody.Add(AddArgs(ZingData.App_Assert, ZingData.Cnst_False));
            AST<Node> payloadOfMethod = MkZingMethodDecl("PayloadOf",
                MkZingVarDecls(MkZingVarDecl("e", Factory.Instance.MkCnst("SM_EVENT"))),
                Factory.Instance.MkCnst("PRT_TYPE"), ZingData.Cnst_Nil, MkZingBlocks(MkZingBlock("dummy", MkZingSeq(payloadOfBody))), ZingData.Cnst_Static);
            methods = AddArgs(ZingData.App_MethodDecls, payloadOfMethod, methods);

            foreach (var field in typeContext.MainVarDecls())
            {
                fields = AddArgs(ZingData.App_VarDecls, field, fields);
            }

            AST<Node> runBody = ZingData.Cnst_Nil;
            foreach (var eventName in allEvents.Keys)
            {
                AST<Node> rhs;
                if (eventName == NullEvent)
                    rhs = MkZingIdentifier("null");
                else
                    rhs = MkZingCall(
                                    MkZingDot("SM_EVENT", "Construct"),
                                    MkZingDot("Event", string.Format("_{0}", eventName)),
                                    Factory.Instance.MkCnst(allEvents[eventName].maxInstances),
                                    allEvents[eventName].maxInstancesAssumed ? ZingData.Cnst_True : ZingData.Cnst_False);

                var assignStmt = MkZingAssign(MkZingEvent(eventName), rhs);
                runBody = MkZingSeq(runBody, assignStmt);
            }
            foreach (var machineName in allMachines.Keys)
            {
                var assignStmt = MkZingAssign(MkZingIdentifier(string.Format("{0}_instance", machineName)), Factory.Instance.MkCnst(0));
                runBody = MkZingSeq(runBody, assignStmt);
            }
            foreach (var machineName in allMachines.Keys)
            {
                if (allMachines[machineName].IsMonitor)
                {
                    var assignStmt = MkZingAssign(MkZingIdentifier(GetMonitorMachineName(machineName)), MkZingIdentifier("null"));
                    runBody = MkZingSeq(runBody, assignStmt);
                }
            }
            runBody = MkZingSeq(runBody, typeContext.InitializeTypesAndFields());

            var locals = new List<AST<Node>>();
            runBody = MkZingSeq(runBody, MkZingCallStmt(MkZingCall(MkZingDot("Main", string.Format("CreateMachine_{0}", mainMachineName)), MkZingIdentifier("null"))));
            runBody = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("dummy"), runBody);
            runBody = ConstructList(ZingData.App_Blocks, runBody);
            AST<Node> runMethod = MkZingMethodDecl("Run", ZingData.Cnst_Nil, ZingData.Cnst_Void, ConstructList(ZingData.App_VarDecls, locals), runBody,
                ZingData.Cnst_Static, ZingData.Cnst_Activate);
            methods = AddArgs(ZingData.App_MethodDecls, runMethod, methods);

            return AddArgs(ZingData.App_ClassDecl, Factory.Instance.MkCnst("Main"), fields, methods);
        }

        private AST<FuncTerm> GenerateMachineClass(string machineName)
        {
            AST<Node> fields = LocalVariablesToVarDecls(allMachines[machineName].localVariableToVarInfo.Keys, allMachines[machineName].localVariableToVarInfo);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("myHandle", SmHandle), fields);

            AST<Node> methods = ZingData.Cnst_Nil;
            foreach (var x in allMachines[machineName].stateNameToStateInfo)
            {
                AST<Node> dequeueEventMethod = GenerateCalculateDeferredAndActionSetMethodDecl(x.Key, x.Value);
                methods = AddArgs(ZingData.App_MethodDecls, dequeueEventMethod, methods);
            }
            AST<Node> startMethod = GenerateStartMethodDecl(machineName);
            methods = AddArgs(ZingData.App_MethodDecls, startMethod, methods);
            AST<Node> runMethod = GenerateRunMethodDecl();
            methods = AddArgs(ZingData.App_MethodDecls, runMethod, methods);
            AST<Node> runHelperMethod = GenerateRunHelperMethodDecl(machineName);
            methods = AddArgs(ZingData.App_MethodDecls, runHelperMethod, methods);
            AST<Node> actionHelperMethod = GenerateReentrancyHelperMethodDecl(machineName);
            methods = AddArgs(ZingData.App_MethodDecls, actionHelperMethod, methods);
            AST<Node> hasDefaultTransitionMethod = GenerateHasDefaultTransitionMethodDecl(machineName);
            methods = AddArgs(ZingData.App_MethodDecls, hasDefaultTransitionMethod, methods);
            foreach (var funName in allMachines[machineName].funNameToFunInfo.Keys)
            {
                var funInfo = allMachines[machineName].funNameToFunInfo[funName];
                methods = AddArgs(ZingData.App_MethodDecls, MkZingFunMethod(machineName, funName, funInfo), methods);
            }
            foreach (var funName in allMachines[machineName].funNameToFunInfo.Keys)
            {
                var funInfo = allMachines[machineName].funNameToFunInfo[funName];
                for (int i = 0; i < funInfo.numFairChoices; i++)
                {
                    fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl(GetFairChoice(funName, i), Factory.Instance.MkCnst("FairChoice")), fields);
                }
            }
            return AddArgs(ZingData.App_ClassDecl, Factory.Instance.MkCnst(machineName), fields, methods);
        }

        private AST<FuncTerm> GenerateHasDefaultTransitionMethodDecl(string machineName)
        {
            List<AST<Node>> parameters = new List<AST<Node>>();
            parameters.Add(MkZingVarDecl("state", Factory.Instance.MkCnst("State")));

            List<AST<Node>> initStmts = new List<AST<Node>>();
            foreach (var stateName in allMachines[machineName].stateNameToStateInfo.Keys)
            {
                var stateInfo = allMachines[machineName].stateNameToStateInfo[stateName];
                initStmts.Add(
                    MkZingIfThen(MkZingApply(ZingData.Cnst_Eq, MkZingIdentifier("state"), MkZingState(stateName)),
                                 MkZingReturn(stateInfo.hasDefaultTransition ? ZingData.Cnst_True : ZingData.Cnst_False)));
            }
            initStmts.Add(MkZingAssert(ZingData.Cnst_False));
            var initBlock = MkZingBlock("init", MkZingSeq(initStmts));
            return MkZingMethodDecl("HasDefaultTransition", MkZingVarDecls(parameters), ZingData.Cnst_Bool, ZingData.Cnst_Nil, MkZingBlocks(initBlock));
        }

        private void MkZingClasses(List<AST<Node>> elements)
        {
            foreach (string machineName in allMachines.Keys)
            {
                elements.Add(GenerateMachineClass(machineName));
            }
            elements.Add(GenerateMainClass());
        }

        private AST<Node> GenerateCalculateDeferredAndActionSetMethodDecl(string stateName, StateInfo stateInfo)
        {
            var smEventSetType = Factory.Instance.MkCnst("SM_EVENT_SET");
            var stackDeferredSet = MkZingDot("myHandle", "stack", "next", "deferredSet");
            var stackActionSet = MkZingDot("myHandle", "stack", "next", "actionSet");
            var currentDeferredSet = MkZingDot("myHandle", "stack", "deferredSet");
            var currentActionSet = MkZingDot("myHandle", "stack", "actionSet");

            var ownerName = stateInfo.ownerName;
            var localDeferredSet = stateInfo.deferredEvents;
            var actions = stateInfo.actions;
            var transitions = stateInfo.transitions;

            List<AST<FuncTerm>> stmts = new List<AST<FuncTerm>>();
            stmts.Add(MkZingAssign(currentDeferredSet, AddArgs(ZingData.App_New, smEventSetType, ZingData.Cnst_Nil)));
            stmts.Add(MkZingAssign(currentActionSet, AddArgs(ZingData.App_New, smEventSetType, ZingData.Cnst_Nil)));
            stmts.Add(MkZingAssign(currentDeferredSet, MkZingApply(ZingData.Cnst_Add, currentDeferredSet, stackDeferredSet)));
            AddEventSet(stmts, localDeferredSet, currentDeferredSet);
            SubtractEventSet(stmts, actions.Keys, currentDeferredSet);
            SubtractEventSet(stmts, transitions.Keys, currentDeferredSet);
            stmts.Add(MkZingAssign(currentActionSet, MkZingApply(ZingData.Cnst_Add, currentActionSet, stackActionSet)));
            SubtractEventSet(stmts, localDeferredSet, currentActionSet);
            AddEventSet(stmts, actions.Keys, currentActionSet);
            SubtractEventSet(stmts, transitions.Keys, currentActionSet);
            stmts.Add(MkZingAssign(MkZingDot("myHandle", "stack", "es"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("SM_EVENT_array"), Factory.Instance.MkCnst(actions.Count))));
            stmts.Add(MkZingAssign(MkZingDot("myHandle", "stack", "as"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("ActionOrFun_array"), Factory.Instance.MkCnst(actions.Count))));
            int count = 0;
            foreach (var eventName in actions.Keys)
            {
                stmts.Add(MkZingAssign(MkZingApply(ZingData.Cnst_Index, MkZingDot("myHandle", "stack", "es"), Factory.Instance.MkCnst(count)), MkZingEvent(eventName)));
                stmts.Add(MkZingAssign(MkZingApply(ZingData.Cnst_Index, MkZingDot("myHandle", "stack", "as"), Factory.Instance.MkCnst(count)), MkZingActionOrFun(ownerName, actions[eventName])));
                count = count + 1;
            }

            var body = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("dummy"), MkZingSeq(stmts.ToArray()));
            return MkZingMethodDecl(string.Format("{0}_CalculateDeferredAndActionSet", stateName), ZingData.Cnst_Nil, ZingData.Cnst_Void, ZingData.Cnst_Nil, ConstructList(ZingData.App_Blocks, body));
        }

        private AST<Node> GenerateStartMethodDecl(string machineName)
        {
            string initStateName = allMachines[machineName].initStateName;
            var callStmt = MkZingCallStmt(MkZingCall(MkZingIdentifier("Run"), MkZingState(initStateName)));
            var iteStmt = MkZingIfThen(MkZingOr(MkZingEq(MkZingDot("myHandle", "currentEvent"), MkZingEvent("halt")), 
                                                MkZingEq(MkZingDot("myHandle", "currentEvent"), MkZingEvent("null"))),
                                    MkZingSeq(
                                        MkZingAssign(MkZingDot("myHandle", "stack"), MkZingIdentifier("null")),
                                        MkZingAssign(MkZingDot("myHandle", "buffer"), MkZingIdentifier("null")),
                                        MkZingAssign(MkZingDot("myHandle", "currentArg"), MkZingIdentifier("null")),
                                        MkZingAssign(MkZingDot("myHandle", "isDeleted"), ZingData.Cnst_True),
                                        MkZingReturn(ZingData.Cnst_Nil))
                                        );
            string traceString = string.Format("\"<StateLog> Unhandled event exception by machine {0}-{{0}}\\n\"", machineName);
            var traceStmt = MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(traceString), MkZingDot("myHandle", "instance")));
            var assertStmt = Factory.Instance.AddArg(ZingData.App_Assert, ZingData.Cnst_False);
            var body = ConstructList(
                            ZingData.App_Blocks,
                            AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("dummy"), MkZingSeq(callStmt, iteStmt, traceStmt, assertStmt))
                            );
            return MkZingMethodDecl("Start", ZingData.Cnst_Nil, ZingData.Cnst_Void, ZingData.Cnst_Nil, body);
        }

        private AST<Node> GenerateRunMethodDecl()
        {
            List<AST<Node>> parameters = new List<AST<Node>>();
            parameters.Add(MkZingVarDecl("state", Factory.Instance.MkCnst("State")));

            List<AST<Node>> locals = new List<AST<Node>>();
            locals.Add(MkZingVarDecl("cont", Factory.Instance.MkCnst("Continuation")));
            locals.Add(MkZingVarDecl("hasDefaultTransition", ZingData.Cnst_Bool));

            var cont = MkZingIdentifier("cont");
            var hasDefaultTransition = MkZingIdentifier("hasDefaultTransition");

            List<AST<Node>> initStmts = new List<AST<Node>>();
            initStmts.Add(MkZingCallStmt(MkZingCall(MkZingDot("myHandle", "Push"))));
            initStmts.Add(MkZingAssign(MkZingDot("myHandle", "stack", "state"), MkZingIdentifier("state")));
            initStmts.Add(MkZingAssign(cont, MkZingCall(MkZingIdentifier("RunHelper"), ZingData.Cnst_True)));
            initStmts.Add(MkZingGoto("dequeue"));
            var initBlock = MkZingBlock("init", MkZingSeq(initStmts));

            List<AST<Node>> dequeueStmts = new List<AST<Node>>();
            dequeueStmts.Add(MkZingIfThen(MkZingNeq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Return")),
                             MkZingSeq(MkZingCallStmt(MkZingCall(MkZingDot("myHandle", "Pop"))), MkZingReturn(ZingData.Cnst_Nil))));
            dequeueStmts.Add(MkZingAssign(hasDefaultTransition, MkZingCall(MkZingIdentifier("HasDefaultTransition"), MkZingDot("myHandle", "stack", "state"))));
            dequeueStmts.Add(MkZingCallStmt(MkZingCall(MkZingDot("myHandle", "DequeueEvent"), hasDefaultTransition)));
            dequeueStmts.Add(MkZingAssign(cont, MkZingCall(MkZingIdentifier("RunHelper"), ZingData.Cnst_False)));
            dequeueStmts.Add(MkZingGoto("dequeue"));
            var dequeueBlock = MkZingBlock("dequeue", MkZingSeq(dequeueStmts));

            return MkZingMethodDecl("Run", MkZingVarDecls(parameters), ZingData.Cnst_Void, MkZingVarDecls(locals), MkZingBlocks(initBlock, dequeueBlock));
        }

        private AST<Node> GenerateReentrancyHelperMethodDecl(string machineName)
        {
            AST<Node> parameters = ConstructList(ZingData.App_VarDecls,
                                                 MkZingVarDecl("actionFun", Factory.Instance.MkCnst("ActionOrFun"))
                                                 );

            List<AST<Node>> locals = new List<AST<Node>>();
            locals.Add(MkZingVarDecl("cont", Factory.Instance.MkCnst("Continuation")));
            locals.Add(MkZingVarDecl("savedDeferredSet", SmEventSet));
            locals.Add(MkZingVarDecl("savedCurrentEvent", SmEvent));
            locals.Add(MkZingVarDecl("savedCurrentArg", PrtValue));

            var cont = MkZingIdentifier("cont");

            List<AST<Node>> initStmts = new List<AST<Node>>();
            initStmts.Add(MkZingAssign(cont, MkZingCall(MkZingDot(Factory.Instance.MkCnst("Continuation"), "Construct_Default"))));
            initStmts.Add(MkZingCallStmt(MkZingCall(MkZingDot(cont, "PushReturnTo"), Factory.Instance.MkCnst(0))));
            foreach (var funName in allMachines[machineName].funNameToFunInfo.Keys)
            {
                if (allMachines[machineName].funNameToFunInfo[funName].parameterNames.Count > 0) continue;
                var funInfo = allMachines[machineName].funNameToFunInfo[funName];
                var funExpr = MkZingActionOrFun(machineName, funName);
                var condExpr = MkZingApply(ZingData.Cnst_Eq, MkZingIdentifier("actionFun"), funExpr);
                var gotoStmt = Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst("execute_" + funName));
                if (funInfo.isAnonymous)
                {
                    initStmts.Add(MkZingIfThen(condExpr, gotoStmt));
                }
                else
                {
                    string traceString = string.Format("\"<FunctionLog> Machine {0}-{{0}} executing Function {1}\\n\"", machineName, funName);
                    var traceStmt = MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(traceString), MkZingDot("myHandle", "instance")));
                    initStmts.Add(MkZingIfThen(condExpr, MkZingSeq(traceStmt, gotoStmt)));
                }
            }
            initStmts.Add(Factory.Instance.AddArg(ZingData.App_Assert, ZingData.Cnst_False));
            AST<Node> initStmt = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("init"), MkZingSeq(initStmts));

            // Action blocks
            List<AST<Node>> blocks = new List<AST<Node>>();
            blocks.Add(initStmt);
            foreach (var funName in allMachines[machineName].funNameToFunInfo.Keys)
            {
                if (allMachines[machineName].funNameToFunInfo[funName].parameterNames.Count > 0) continue;
                AST<Cnst> executeLabel = Factory.Instance.MkCnst("execute_" + funName);
                var executeStmt = MkZingInvokeWrapperFun(machineName, funName);
                executeStmt = AddArgs(ZingData.App_LabelStmt, executeLabel, executeStmt);
                blocks.Add(executeStmt);
            }
            AST<Node> body = ConstructList(ZingData.App_Blocks, blocks);
            return MkZingMethodDecl("ReentrancyHelper", parameters, Factory.Instance.MkCnst("Continuation"), MkZingVarDecls(locals), body);
        }

        private AST<Node> MkZingInvokeWrapperFun(string machineName, string entityName)
        {
            string name = entityName;
            var currentEvent = MkZingDot("myHandle", "currentEvent");
            var currentArg = MkZingDot("myHandle", "currentArg");
            var savedCurrentEvent = MkZingIdentifier("savedCurrentEvent");
            var savedCurrentArg = MkZingIdentifier("savedCurrentArg");
            var savedDeferredSet = MkZingIdentifier("savedDeferredSet");
            var cont = MkZingIdentifier("cont");

            var restoreCurrentEvent = MkZingAssign(currentEvent, savedCurrentEvent);
            var restoreCurrentArg = MkZingAssign(currentArg, savedCurrentArg);
            var restoreDeferredSet = MkZingAssign(MkZingDot("myHandle", "stack", "deferredSet"), savedDeferredSet);
            var gotoStmt = AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst("transition_" + entityName));
            string errorTraceString = string.Format("\"<StateLog> Call statement terminated due to unhandled event by machine {0}-{{0}}\\n\"", machineName);
            var errorTraceStmt = MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(errorTraceString), MkZingDot("myHandle", "instance")));
            var body = new List<AST<Node>>();
            body.Add(AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("reentry_" + name), MkZingCallStmt(MkZingCall(MkZingIdentifier(name), cont))));
            body.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Return")), MkZingReturn(cont)));
            body.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Pop")), MkZingReturn(cont)));
            body.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Raise")), MkZingReturn(cont)));
            body.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Call")),
                MkZingSeq(
                    MkZingAssign(savedCurrentEvent, currentEvent),
                    MkZingAssign(savedCurrentArg, currentArg),
                    MkZingAssign(savedDeferredSet, MkZingDot("myHandle", "stack", "deferredSet")),
                    MkZingAssign(currentEvent, MkZingIdentifier("null")),
                    MkZingAssign(MkZingDot("myHandle", "currentArg"), MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeNull.Node))),
                    MkZingAssign(MkZingDot("myHandle", "stack", "deferredSet"), MkZingCall(MkZingDot("Main", "CalculateComplementOfEventSet"), MkZingDot("myHandle", "stack", "actionSet"))),
                    MkZingCallStmt(MkZingCall(MkZingIdentifier("Run"), MkZingDot("cont", "state"))),
                    MkZingAssign(MkZingDot("cont", "state"), MkZingState("default")),
                    MkZingIfThenElse(MkZingApply(ZingData.Cnst_Eq, currentEvent, MkZingIdentifier("null")),
                              MkZingSeq(restoreCurrentEvent, restoreCurrentArg, restoreDeferredSet),
                              MkZingSeq(errorTraceStmt, MkZingAssert(ZingData.Cnst_False))),
                    AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst("reentry_" + name)))));
            AST<Node> atChooseLivenessStmt = ZingData.Cnst_Nil;
            AST<Node> atYieldLivenessStmt = ZingData.Cnst_Nil;
            if (compiler.Options.liveness == LivenessOption.Standard)
            {
                atChooseLivenessStmt = MkZingSeq(
                    MkZingCallStmt(MkZingCall(MkZingDot("FairCycle", "GateProgress"))),
                    MkZingCallStmt(MkZingCall(MkZingDot("FairScheduler", "AtChooseStatic"))),
                    MkZingCallStmt(MkZingCall(MkZingDot("FairChoice", "AtYieldOrChooseStatic"))));
                atYieldLivenessStmt = MkZingSeq(
                    MkZingCallStmt(MkZingCall(MkZingDot("FairCycle", "GateProgress"))),
                    MkZingCallStmt(MkZingCall(MkZingDot("FairScheduler", "AtYieldStatic"), MkZingIdentifier("myHandle"))),
                    MkZingCallStmt(MkZingCall(MkZingDot("FairChoice", "AtYieldOrChooseStatic"))));
            }
            body.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Nondet")),
                MkZingSeq(
                    atChooseLivenessStmt,
                    MkZingAssign(MkZingDot(cont, "nondet"), MkZingCall(Factory.Instance.MkCnst("choose"), Factory.Instance.MkCnst("bool"))),
                    AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst("reentry_" + name)))));
            body.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "NewMachine")),
                MkZingSeq(
                    atYieldLivenessStmt,
                    ZingData.Cnst_Yield,
                    AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst("reentry_" + name)))));
            body.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Send")),
                MkZingSeq(
                    atYieldLivenessStmt,
                    ZingData.Cnst_Yield,
                    AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst("reentry_" + name)))));

            return MkZingSeq(body);
        }

        private AST<Node> GenerateRunHelperMethodDecl(string machineName)
        {
            AST<Node> parameters = ConstructList(ZingData.App_VarDecls, MkZingVarDecl("start", ZingData.Cnst_Bool));
            List<AST<Node>> locals = new List<AST<Node>>();
            locals.Add(MkZingVarDecl("state", Factory.Instance.MkCnst("State")));
            locals.Add(MkZingVarDecl("cont", Factory.Instance.MkCnst("Continuation")));
            locals.Add(MkZingVarDecl("actionFun", Factory.Instance.MkCnst("ActionOrFun")));
            locals.Add(MkZingVarDecl("exitFun", Factory.Instance.MkCnst("ActionOrFun")));

            var state = MkZingIdentifier("state");
            // Initial block
            List<AST<Node>> initStmts = new List<AST<Node>>();
            initStmts.Add(MkZingAssign(state, MkZingDot("myHandle", "stack", "state")));
            foreach (var stateName in allMachines[machineName].stateNameToStateInfo.Keys)
            {
                var stateExpr = MkZingState(stateName);
                var condExpr = MkZingApply(ZingData.Cnst_Eq, state, stateExpr);
                var gotoExecuteStmt = Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst("execute_" + stateName));
                var gotoTransitionStmt = Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst("transition_" + stateName));
                initStmts.Add(MkZingIfThen(condExpr, MkZingIfThenElse(MkZingIdentifier("start"), gotoExecuteStmt, gotoTransitionStmt)));
            }
            initStmts.Add(Factory.Instance.AddArg(ZingData.App_Assert, ZingData.Cnst_False));
            var initStmt = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("init"), MkZingSeq(initStmts));

            var cont = MkZingIdentifier("cont");

            // State blocks
            List<AST<Node>> blocks = new List<AST<Node>>();
            blocks.Add(initStmt);
            var smEventSetType = Factory.Instance.MkCnst("SM_EVENT_SET");
            foreach (var stateName in allMachines[machineName].stateNameToStateInfo.Keys)
            {
                var entryAction = MkZingActionOrFun(machineName, allMachines[machineName].stateNameToStateInfo[stateName].entryActionName);
                StateInfo stateInfo = allMachines[machineName].stateNameToStateInfo[stateName];
                AST<Cnst> transitionLabel = Factory.Instance.MkCnst("transition_" + stateName);
                string enterTraceString = string.Format("\"<StateLog> Machine {0}-{{0}} entering State {1}\\n\"", machineName, stateInfo.printedName);
                string exitTraceString = string.Format("\"<StateLog> Machine {0}-{{0}} exiting State {1}\\n\"", machineName, stateInfo.printedName);
                List<AST<Node>> executeStmts = new List<AST<Node>>();
                if (compiler.Options.liveness != LivenessOption.None && allMachines[machineName].IsMonitor)
                {
                    if (compiler.Options.liveness == LivenessOption.Standard)
                    {
                        if (allMachines[machineName].stateNameToStateInfo[stateName].isHot)
                        {
                            executeStmts.Add(MkZingIfThen(MkZingApply(ZingData.Cnst_NEq, MkZingDot("FairCycle", "gate"), MkZingDot("GateStatus", "Closed")),
                                                       MkZingAssign(MkZingDot("FairCycle", "gate"), MkZingDot("GateStatus", "Selected"))));
                        }
                        else
                        {
                            executeStmts.Add(MkZingAssume(MkZingApply(ZingData.Cnst_NEq, MkZingDot("FairCycle", "gate"), MkZingDot("GateStatus", "Closed"))));
                            executeStmts.Add(MkZingAssign(MkZingDot("FairCycle", "gate"), MkZingDot("GateStatus", "InStable")));
                        }
                    }
                    else 
                    {
                        // compiler.Options.liveness == LivenessOption.Mace
                        if (allMachines[machineName].stateNameToStateInfo[stateName].isHot)
                        {
                            executeStmts.Add(MkZingCallStmt(MkZingCall(MkZingIdentifier("accept"), ZingData.Cnst_False)));
                        }
                        else
                        {
                            executeStmts.Add(MkZingCallStmt(MkZingCall(MkZingIdentifier("accept"), ZingData.Cnst_True)));
                        }
                    }
                }
                executeStmts.Add(MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(enterTraceString), MkZingDot("myHandle", "instance"))));
                executeStmts.Add(MkZingCallStmt(MkZingCall(MkZingIdentifier("invokeplugin"), Factory.Instance.MkCnst("\"StateCoveragePlugin.dll\""), Factory.Instance.MkCnst(string.Format("\"{0}\"", machineName)), Factory.Instance.MkCnst(string.Format("\"{0}\"", stateName)))));
                executeStmts.Add(MkZingCallStmt(MkZingCall(MkZingIdentifier(string.Format("{0}_CalculateDeferredAndActionSet", stateName)))));
                executeStmts.Add(MkZingAssign(cont, MkZingCall(MkZingIdentifier("ReentrancyHelper"), entryAction)));
                executeStmts.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Raise")), MkZingGoto("transition_" + stateName)));
                executeStmts.Add(MkZingAssign(MkZingDot("myHandle", "currentEvent"), MkZingIdentifier("null")));
                executeStmts.Add(MkZingAssign(MkZingDot("myHandle", "currentArg"), MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeNull.Node))));
                executeStmts.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Pop")),
                                                 MkZingSeq(
                                                 MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(exitTraceString), MkZingDot("myHandle", "instance"))), 
                                                 MkZingSeq(MkZingCallStmt(MkZingCall(MkZingIdentifier("ReentrancyHelper"), MkZingActionOrFun(machineName, stateInfo.exitFunName)))))));
                executeStmts.Add(MkZingReturn(cont));
                blocks.Add(MkZingBlock("execute_" + stateName, MkZingSeq(executeStmts)));

                var callTransitions = new Dictionary<string, TransitionInfo>();
                var ordinaryTransitions = new Dictionary<string, TransitionInfo>();
                var transitions = allMachines[machineName].stateNameToStateInfo[stateName].transitions;
                foreach (var eventName in transitions.Keys)
                {
                    if (transitions[eventName].isPush)
                    {
                        callTransitions[eventName] = transitions[eventName];
                    }
                    else
                    {
                        ordinaryTransitions[eventName] = transitions[eventName];
                    }
                }

                var actionStmt = 
                MkZingIfThen(MkZingApply(ZingData.Cnst_In, MkZingDot("myHandle", "currentEvent"), MkZingDot("myHandle", "stack", "actionSet")),
                             MkZingSeq(MkZingAssign(MkZingIdentifier("actionFun"), MkZingCall(MkZingDot("myHandle", "stack", "Find"), MkZingDot("myHandle", "currentEvent"))),
                                       MkZingAssign(cont, MkZingCall(MkZingIdentifier("ReentrancyHelper"), MkZingIdentifier("actionFun"))),
                                       MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Raise")), MkZingGoto("transition_" + stateName)),
                                       MkZingAssign(MkZingDot("myHandle", "currentEvent"), MkZingIdentifier("null")),
                                       MkZingAssign(MkZingDot("myHandle", "currentArg"), MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeNull.Node))),
                                       MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Pop")),
                                                    MkZingSeq(
                                                    MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(exitTraceString), MkZingDot("myHandle", "instance"))),
                                                    MkZingSeq(MkZingCallStmt(MkZingCall(MkZingIdentifier("ReentrancyHelper"), MkZingActionOrFun(machineName, stateInfo.exitFunName)))))),
                                      MkZingReturn(cont)));
                AST<Node> callTransitionStmt = ZingData.Cnst_Nil;
                foreach (var eventName in callTransitions.Keys)
                {
                    var targetStateName = callTransitions[eventName].target;
                    var condExpr = MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingEvent(eventName));
                    var callStmt = MkZingCallStmt(MkZingCall(MkZingIdentifier("Run"), MkZingState(targetStateName)));
                    var ite = MkZingIfThenElse(
                        MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingIdentifier("null")),
                        MkZingReturn(cont),
                        Factory.Instance.AddArg(ZingData.App_Goto, transitionLabel)
                        );
                    callTransitionStmt = MkZingIfThenElse(condExpr, MkZingSeq(callStmt, ite), callTransitionStmt);
                }

                AST<Node> exitFunStmt = MkZingAssign(MkZingIdentifier("exitFun"), MkZingActionOrFun(machineName, allMachines[machineName].stateNameToStateInfo[stateName].exitFunName));
                AST<Node> ordinaryTransitionStmt = MkZingReturn(cont);
                foreach (var eventName in ordinaryTransitions.Keys)
                {
                    var targetStateName = ordinaryTransitions[eventName].target;
                    var condExpr = MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingEvent(eventName));
                    AST<Node> jumpStmt = Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst("execute_" + targetStateName));
                    ordinaryTransitionStmt = MkZingIfThenElse(condExpr, jumpStmt, ordinaryTransitionStmt);
                    string exitFunName = ordinaryTransitions[eventName].exitFunName;
                    if (exitFunName != null)
                    {
                        exitFunStmt = MkZingIfThenElse(condExpr, MkZingAssign(MkZingIdentifier("exitFun"), MkZingActionOrFun(machineName, exitFunName)), exitFunStmt);
                    }
                }
                exitFunStmt = MkZingSeq(exitFunStmt,
                                        MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(exitTraceString), MkZingDot("myHandle", "instance"))), 
                                        MkZingCallStmt(MkZingCall(MkZingIdentifier("ReentrancyHelper"), MkZingIdentifier("exitFun"))));
                blocks.Add(AddArgs(ZingData.App_LabelStmt, transitionLabel, MkZingSeq(actionStmt, callTransitionStmt, exitFunStmt, ordinaryTransitionStmt)));
            }
            AST<Node> body = ConstructList(ZingData.App_Blocks, blocks);
            return MkZingMethodDecl("RunHelper", parameters, Factory.Instance.MkCnst("Continuation"), MkZingVarDecls(locals), body);
        }

        class ZingFoldContext
        {
            private PToZing pToZing;
            public string machineName;
            public string entityName;
            public FunInfo entityInfo;
            public Stack<List<AST<Node>>> sideEffectsStack;
            public List<Tuple<AST<Node>, string>> locals;
            private Dictionary<string, int> labels;

            public ZingFoldContext(PToZing comp, string machineName, string entityName, FunInfo entityInfo)
            {
                this.pToZing = comp;
                this.machineName = machineName;
                this.entityName = entityName;
                this.entityInfo = entityInfo;
                this.sideEffectsStack = new Stack<List<AST<Node>>>();
                PushSideEffectStack();
                this.locals = new List<Tuple<AST<Node>, string>>();
                this.labels = new Dictionary<string, int>();
            }

            public int LabelToId(string l)
            {
                return labels[l];
            }

            public string GetFreshLabel()
            {
                var l = pToZing.GetUnique(entityName);
                labels[l] = labels.Count + 1;
                return l;
            }

            public AST<Node> EmitLabelPrelude()
            {
                var prelude = new List<AST<Node>>();
                var tmpVar = GetTmpVar(ZingData.Cnst_Int, "retTo");

                prelude.Add(PToZing.MkZingAssign(tmpVar, PToZing.MkZingCall(MkZingDot("entryCtxt", "PopReturnTo"))));
                prelude.Add(PToZing.MkZingIfThen(PToZing.MkZingEq(tmpVar, Factory.Instance.MkCnst(0)),
                    AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst("start"))));

                foreach (var l in labels.Keys)
                {
                    prelude.Add(PToZing.MkZingIfThen(PToZing.MkZingEq(tmpVar, Factory.Instance.MkCnst(labels[l])),
                    AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst(l))));
                }

                prelude.Add(AddArgs(ZingData.App_Assert, ZingData.Cnst_False));

                return PToZing.MkZingSeq(prelude);
            }

            public void AddSideEffect(AST<Node> stmt)
            {
                this.sideEffectsStack.Peek().Add(stmt);
            }

            public void PushSideEffectStack()
            {
                this.sideEffectsStack.Push(new List<AST<Node>>());
            }

            // type must be Zing type
            public AST<Node> GetTmpVar(AST<Node> type, string baseName)
            {
                var tmpVarName = pToZing.GetUnique(baseName);
                var tmpVar = MkZingIdentifier(tmpVarName);
                this.locals.Add(new Tuple<AST<Node>, string>(type, tmpVarName));
                return tmpVar;
            }

            public AST<Node> EmitZingSideEffects(AST<Node> stmt)
            {
                Debug.Assert(sideEffectsStack.Count > 0);
                var sideEffects = sideEffectsStack.Pop();

                if (sideEffects.Count > 0)
                {
                    sideEffects.Add(stmt);
                    return PToZing.MkZingSeq(sideEffects);
                }
                else
                {
                    return stmt;
                }
            }

            public AST<Node> EmitLocals()
            {
                return ConstructList(ZingData.App_VarDecls, locals.Select(loc => PToZing.MkZingVarDecl(loc.Item2, loc.Item1)));
            }
        }

        private IEnumerable<Node> ZingUnfold(ZingFoldContext ctxt, Node n)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                yield break;
            }

            var ft = (FuncTerm)n;
            var funName = ((Id)ft.Function).Name;
            if (funName == PData.Con_New.Node.Name || funName == PData.Con_NewStmt.Node.Name)
            {
                yield return GetArgByIndex(ft, 1);
            }
            else if (funName == PData.Con_FunApp.Node.Name || funName == PData.Con_FunStmt.Node.Name)
            {
                ctxt.PushSideEffectStack();
                bool first = true;
                foreach (var t in ft.Args)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        foreach (var a in ZingUnfold(ctxt, t))
                        {
                            yield return a;
                        }
                    }
                }
            }
            else if (funName == PData.Con_Name.Node.Name ||
                     funName == PData.Con_NulApp.Node.Name ||
                     funName == PData.Con_UnApp.Node.Name ||
                     funName == PData.Con_BinApp.Node.Name ||
                     funName == PData.Con_Default.Node.Name ||
                     funName == PData.Con_Push.Node.Name ||
                     funName == PData.Con_Monitor.Node.Name ||
                     funName == PData.Con_NulStmt.Node.Name ||
                     funName == PData.Con_UnStmt.Node.Name)
            {
                var first = true;
                foreach (var t in ft.Args)
                {
                    if (first)
                    {
                        first = false;
                        continue;
                    }
                    yield return t;
                }
            }
            else if (funName == PData.Con_BinStmt.Node.Name)
            {
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                if (op == PData.Cnst_Assign.Node.Name)
                {
                    var lhs = (FuncTerm)GetArgByIndex(ft, 1);
                    var lhsName = ((Id)lhs.Function).Name;
                    if (lhsName == PData.Con_BinApp.Node.Name && ((Id)GetArgByIndex(lhs, 0)).Name == PData.Cnst_Idx.Node.Name)
                    {
                        yield return GetArgByIndex(lhs, 1);
                        yield return GetArgByIndex(lhs, 2);
                    }
                    else if (lhsName == PData.Con_Field.Node.Name)
                    {
                        yield return GetArgByIndex(lhs, 0);
                    }
                    else
                    {
                        yield return lhs;
                    }
                    yield return GetArgByIndex(ft, 2);
                }
                else
                {
                    yield return GetArgByIndex(ft, 1);
                    yield return GetArgByIndex(ft, 2);
                }
            }
            else if (funName == PData.Con_Field.Node.Name || funName == PData.Con_Cast.Node.Name)
            {
                yield return ft.Args.First<Node>();
            }
            else if (funName == PData.Con_Tuple.Node.Name)
            {
                foreach (var a in ZingUnfold(ctxt, GetArgByIndex(ft, 0)))
                {
                    yield return a;
                }
            }
            else if (funName == PData.Con_NamedTuple.Node.Name)
            {
                foreach (var a in ZingUnfold(ctxt, GetArgByIndex(ft, 0)))
                {
                    yield return a;
                }
            }
            else if (funName == PData.Con_Exprs.Node.Name)
            {
                do
                {
                    yield return ft.Args.First<Node>();
                    ft = GetArgByIndex(ft, 1) as FuncTerm;
                }
                while (ft != null);
            }
            else if (funName == PData.Con_NamedExprs.Node.Name)
            {
                do
                {
                    yield return GetArgByIndex(ft, 1);
                    ft = GetArgByIndex(ft, 2) as FuncTerm;
                }
                while (ft != null);
            }
            else if (funName == PData.Con_Seq.Node.Name)
            {
                using (var it = ft.Args.GetEnumerator())
                {
                    it.MoveNext();
                    yield return it.Current;
                    it.MoveNext();
                    ctxt.PushSideEffectStack();
                    yield return it.Current;
                }
            }
            else if (funName == PData.Con_Ite.Node.Name)
            {
                using (var it = ft.Args.GetEnumerator())
                {
                    it.MoveNext();
                    yield return it.Current;
                    it.MoveNext();
                    ctxt.PushSideEffectStack();
                    yield return it.Current;
                    it.MoveNext();
                    ctxt.PushSideEffectStack();
                    yield return it.Current;
                }
            }
            else if (funName == PData.Con_While.Node.Name)
            {
                using (var it = ft.Args.GetEnumerator())
                {
                    it.MoveNext();
                    ctxt.PushSideEffectStack();
                    yield return it.Current;
                    it.MoveNext();
                    ctxt.PushSideEffectStack();
                    yield return it.Current;
                }
            }
            else
            {
                foreach (var t in ft.Args)
                {
                    yield return t;
                }
            }
        }

        private AST<Node> MkZingLabeledStmt(string label, AST<Node> stmt)
        {
            return AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst(label), stmt);
        }

        private AST<Node> MkZingAssignWithClone(AST<Node> lhs, AST<Node> rhs)
        {
            return AddArgs(ZingData.App_Assign, lhs, MkZingCall(PrtCloneValue, rhs));
        }

        private ZingTranslationInfo ZingFold(ZingFoldContext ctxt, Node n, IEnumerable<ZingTranslationInfo> children)
        {
            if (n.NodeKind == NodeKind.Id)
                return new ZingTranslationInfo(ZingData.Cnst_Nil);

            var ft = (FuncTerm)n;
            var funName = ((Id)ft.Function).Name;

            if (funName == PData.Con_Name.Node.Name)
            {
                return FoldName(ft, children, ctxt);
            }
            else if (funName == PData.Con_New.Node.Name)
            {
                return FoldNew(ft, children, ctxt);
            }
            else if (funName == PData.Con_FunApp.Node.Name)
            {
                return FoldFunApp(ft, children, ctxt);
            }
            else if (funName == PData.Con_NulApp.Node.Name)
            {
                return FoldNulApp(ft, children, ctxt);
            }
            else if (funName == PData.Con_UnApp.Node.Name)
            {
                return FoldUnApp(ft, children, ctxt);
            }
            else if (funName == PData.Con_BinApp.Node.Name)
            {
                return FoldBinApp(ft, children, ctxt);
            }
            else if (funName == PData.Con_Field.Node.Name)
            {
                return FoldField(ft, children, ctxt);
            }
            else if (funName == PData.Con_Default.Node.Name)
            {
                return FoldDefault(ft, children, ctxt);
            }
            else if (funName == PData.Con_Cast.Node.Name)
            {
                return FoldCast(ft, children, ctxt);
            }
            else if (funName == PData.Con_Tuple.Node.Name)
            {
                return FoldTuple(ft, children, ctxt);
            }
            else if (funName == PData.Con_NamedTuple.Node.Name)
            {
                return FoldNamedTuple(ft, children, ctxt);
            }
            else if (funName == PData.Con_Push.Node.Name)
            {
                return FoldPush(ft, children, ctxt);
            }
            else if (funName == PData.Con_NewStmt.Node.Name)
            {
                return FoldNewStmt(ft, children, ctxt);
            }
            else if (funName == PData.Con_Raise.Node.Name)
            {
                return FoldRaise(ft, children, ctxt);
            }
            else if (funName == PData.Con_Send.Node.Name)
            {
                return FoldSend(ft, children, ctxt);
            }
            else if (funName == PData.Con_Monitor.Node.Name)
            {
                return FoldMonitor(ft, children, ctxt);
            }
            else if (funName == PData.Con_FunStmt.Node.Name)
            {
                return FoldFunStmt(ft, children, ctxt);
            }
            else if (funName == PData.Con_NulStmt.Node.Name)
            {
                return FoldNulStmt(ft, children, ctxt);
            }
            else if (funName == PData.Con_UnStmt.Node.Name)
            {
                return FoldUnStmt(ft, children, ctxt);
            }
            else if (funName == PData.Con_BinStmt.Node.Name)
            {
                return FoldBinStmt(ft, children, ctxt);
            }
            else if (funName == PData.Con_Return.Node.Name)
            {
                return FoldReturn(ft, children, ctxt);
            }
            else if (funName == PData.Con_While.Node.Name)
            {
                return FoldWhile(ft, children, ctxt);
            }
            else if (funName == PData.Con_Ite.Node.Name)
            {
                return FoldIte(ft, children, ctxt);
            }
            else if (funName == PData.Con_Seq.Node.Name)
            {
                return FoldSeq(ft, children, ctxt);
            }
            else
            {
                Console.WriteLine("Unknown term name: " + funName);
                throw new NotImplementedException();
            }
        }

        ZingTranslationInfo FoldName(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var name = GetName(ft, 0);
            FunInfo funInfo = ctxt.entityInfo as FunInfo;
            if (funInfo != null && funInfo.parameterNameToInfo.ContainsKey(name))
            {
                return new ZingTranslationInfo(MkZingIdentifier(name));
            }
            else if (allMachines[ctxt.machineName].localVariableToVarInfo.ContainsKey(name))
            {
                return new ZingTranslationInfo(MkZingIdentifier(name));
            }
            else
            {
                var tmpVar = ctxt.GetTmpVar(PrtValue, "tmp");
                ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeEvent.Node))));
                ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetEvent"), tmpVar, MkZingEvent(name))));
                return new ZingTranslationInfo(tmpVar);
            }
        }

        ZingTranslationInfo FoldNew(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var typeName = GetName(ft, 0);
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                AST<Node> arg = it.Current.node;
                if (arg == ZingData.Cnst_Nil)
                {
                    arg = MkZingIdentifier("null");
                }
                var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpSendPayload");
                ctxt.AddSideEffect(MkZingAssignWithClone(tmpVar, arg));

                MachineInfo machineInfo = allMachines[typeName];
                if (machineInfo.IsMonitor)
                {
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("Main", string.Format("CreateMachine_{0}", typeName)), tmpVar)));
                    return new ZingTranslationInfo(ZingData.Cnst_Nil);
                }
                else
                {
                    var newMachine = ctxt.GetTmpVar(SmHandle, "newMachine");
                    ctxt.AddSideEffect(MkZingAssign(newMachine, MkZingCall(MkZingDot("Main", string.Format("CreateMachine_{0}", typeName)), tmpVar)));
                    string afterLabel = ctxt.GetFreshLabel();
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "NewMachine"), Factory.Instance.MkCnst(ctxt.LabelToId(afterLabel)), newMachine)));
                    ctxt.AddSideEffect(MkZingReturn(ZingData.Cnst_Nil));
                    ctxt.AddSideEffect(MkZingLabeledStmt(afterLabel, MkZingAssign(newMachine, MkZingDot("entryCtxt", "id"))));
                    ctxt.AddSideEffect(MkZingAssign(MkZingDot("entryCtxt", "id"), MkZingIdentifier("null")));
                    if (((Id)ft.Function).Name == "New")
                    {
                        var type = LookupType(ctxt, ft);
                        var retVal = ctxt.GetTmpVar(PrtValue, "tmp");
                        ctxt.AddSideEffect(MkZingAssign(retVal, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(type))));
                        ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetMachine"), retVal, newMachine)));
                        return new ZingTranslationInfo(retVal);
                    }
                    else
                    {
                        // ((Id)ft.Function).Name == "NewStmt"
                        return new ZingTranslationInfo(ZingData.Cnst_Nil);
                    }
                }
            }
        }

        ZingTranslationInfo FoldFunApp(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var calleeName = GetName(ft, 0);
            var calleeInfo = allMachines[ctxt.machineName].funNameToFunInfo[calleeName];
            List<AST<Node>> args = new List<AST<Node>>();
            // Prepend the default entryCtxt argument.
            args.Add(MkZingIdentifier("entryCtxt"));

            int count = 0;
            foreach (var child in children)
            {
                var lhsInfo = calleeInfo.parameterNameToInfo[calleeInfo.parameterNames[count]];
                var rhsInfo = child;
                var tmpVar = ctxt.GetTmpVar(PrtValue, "argCloneVar");
                ctxt.AddSideEffect(MkZingAssignWithClone(tmpVar, child.node));
                count++;
                args.Add(tmpVar);
            }

            AST<Node> callExpr = MkZingCall(MkZingIdentifier(calleeName), args);
            AST<Node> processOutput;
            AST<Node> outExp;

            var beforeLabel = ctxt.GetFreshLabel();
            if (calleeInfo.returnType.Equals(PTypeNull))
            {
                processOutput = ZingData.Cnst_Nil;
                outExp = ZingData.Cnst_Nil;
            }
            else
            {
                var retVar = ctxt.GetTmpVar(PrtValue, "ret");
                processOutput = MkZingAssignWithClone(retVar, MkZingDot("entryCtxt", "retVal"));
                outExp = retVar;
            }

            AST<Node> callStmt = MkZingSeq(
                MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "PushReturnTo"), Factory.Instance.MkCnst(0))),
                MkZingLabeledStmt(beforeLabel, ctxt.EmitZingSideEffects(MkZingCallStmt(callExpr))),
                MkZingIfThenElse(
                                 MkZingEq(MkZingDot("entryCtxt", "reason"), MkZingDot("ContinuationReason", "Return")),
                                 processOutput,
                                 MkZingSeq(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "PushReturnTo"), Factory.Instance.MkCnst(ctxt.LabelToId(beforeLabel)))),
                                           MkZingReturn(ZingData.Cnst_Nil))));
            ctxt.AddSideEffect(callStmt);
            return new ZingTranslationInfo(outExp);
        }

        ZingTranslationInfo FoldNulApp(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var n = GetArgByIndex(ft, 0);
            if (n.NodeKind == NodeKind.Cnst)
            {
                var tmpVar = ctxt.GetTmpVar(PrtValue, "tmp");
                ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeInt.Node))));
                ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetInt"), tmpVar, Factory.Instance.ToAST(n))));
                return new ZingTranslationInfo(tmpVar);
            }

            // n.NodeKind == NodeKind.Id
            var op = ((Id)n).Name;
            if (op == PData.Cnst_True.Node.Name)
            {
                var tmpVar = ctxt.GetTmpVar(PrtValue, "tmp");
                ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, ZingData.Cnst_True)));
                return new ZingTranslationInfo(tmpVar);
            }
            else if (op == PData.Cnst_False.Node.Name)
            {
                var tmpVar = ctxt.GetTmpVar(PrtValue, "tmp");
                ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, ZingData.Cnst_False)));
                return new ZingTranslationInfo(tmpVar);
            }
            else if (op == PData.Cnst_This.Node.Name)
            {
                var machineType = allMachines[ctxt.machineName].IsModel ? PTypeModel : PTypeReal;
                var tmpVar = ctxt.GetTmpVar(PrtValue, "tmp");
                ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(machineType.Node))));
                ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetMachine"), tmpVar, MkZingIdentifier("myHandle"))));
                return new ZingTranslationInfo(tmpVar);
            }
            else if (op == PData.Cnst_Trigger.Node.Name)
            {
                var tmpVar = ctxt.GetTmpVar(PrtValue, "tmp");
                ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeEvent.Node))));
                ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetEvent"), tmpVar, MkZingDot("myHandle", "currentEvent"))));
                return new ZingTranslationInfo(tmpVar);
            }
            else if (op == PData.Cnst_Payload.Node.Name)
            {
                return new ZingTranslationInfo(MkZingDot("myHandle", "currentArg"));
            }
            else if (op == PData.Cnst_Nondet.Node.Name || op == PData.Cnst_FairNondet.Node.Name)
            {
                var afterLabel = ctxt.GetFreshLabel();
                var bvar = ctxt.GetTmpVar(ZingData.Cnst_Bool, "nondet");
                ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Nondet"), Factory.Instance.MkCnst(ctxt.LabelToId(afterLabel)))));
                ctxt.AddSideEffect(MkZingReturn(ZingData.Cnst_Nil));
                ctxt.AddSideEffect(MkZingLabeledStmt(afterLabel, MkZingAssign(bvar, MkZingDot("entryCtxt", "nondet"))));
                ctxt.AddSideEffect(MkZingAssign(MkZingDot("entryCtxt", "nondet"), ZingData.Cnst_False));
                if (compiler.Options.liveness == LivenessOption.Standard && op == PData.Cnst_FairNondet.Node.Name)
                {
                    int i = ctxt.entityInfo.numFairChoices;
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(GetFairChoice(ctxt.entityName, i), "AtChoose"), bvar)));
                    ctxt.entityInfo.numFairChoices++;
                }
                var tmpVar = ctxt.GetTmpVar(PrtValue, "tmp");
                ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, bvar)));
                return new ZingTranslationInfo(tmpVar);
            }
            else if (op == PData.Cnst_Null.Node.Name)
            {
                var tmpVar = ctxt.GetTmpVar(PrtValue, "tmp");
                ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeNull.Node))));
                return new ZingTranslationInfo(tmpVar);
            }
            else
            {
                // op == PData.Cnst_Halt.Node.Name
                var tmpVar = ctxt.GetTmpVar(PrtValue, "tmp");
                ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeEvent.Node))));
                ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetEvent"), tmpVar, MkZingEvent(HaltEvent))));
                return new ZingTranslationInfo(tmpVar);
            }
        }

        ZingTranslationInfo FoldUnApp(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var op = ((Id)GetArgByIndex(ft, 0)).Name;
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                var arg = it.Current.node;
                if (op == PData.Cnst_Not.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, MkZingApply(ZingData.Cnst_Not, MkZingDot(arg, "bl")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Neg.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeInt.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetInt"), tmpVar, MkZingApply(ZingData.Cnst_Neg, MkZingDot(arg, "nt")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Keys.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtMapGetKeys"), arg)));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Values.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtMapGetValues"), arg)));
                    return new ZingTranslationInfo(tmpVar);
                }
                else
                {
                    //  op == PData.Cnst_Sizeof.Node.Name
                    var type = LookupType(ctxt, GetArgByIndex(ft, 1));
                    var typeOp = ((Id)type.Function).Name;
                    var tmpVar = ctxt.GetTmpVar(ZingData.Cnst_Int, "tmpVar");
                    if (typeOp == PData.Con_SeqType.Node.Name)
                    {
                        ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtSeqSizeOf"), arg)));
                    }
                    else
                    {
                        // typeOp == PData.Con_MapType.Node.Name)
                        ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtMapSizeOf"), arg)));
                    }
                    var retVal = ctxt.GetTmpVar(PrtValue, "retVal");
                    ctxt.AddSideEffect(MkZingAssign(retVal, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeInt.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetInt"), retVal, tmpVar)));
                    return new ZingTranslationInfo(retVal);
                }
            }
        }

        ZingTranslationInfo FoldBinApp(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var op = ((Id)GetArgByIndex(ft, 0)).Name;
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                var arg1 = it.Current.node;
                it.MoveNext();
                var arg2 = it.Current.node;

                if (op == PData.Cnst_Add.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeInt.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetInt"), tmpVar, MkZingApply(ZingData.Cnst_Add, MkZingDot(arg1, "nt"), MkZingDot(arg2, "nt")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Sub.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeInt.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetInt"), tmpVar, MkZingApply(ZingData.Cnst_Sub, MkZingDot(arg1, "nt"), MkZingDot(arg2, "nt")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Mul.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeInt.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetInt"), tmpVar, MkZingApply(ZingData.Cnst_Mul, MkZingDot(arg1, "nt"), MkZingDot(arg2, "nt")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_IntDiv.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeInt.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetInt"), tmpVar, MkZingApply(ZingData.Cnst_IntDiv, MkZingDot(arg1, "nt"), MkZingDot(arg2, "nt")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_And.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, MkZingApply(ZingData.Cnst_And, MkZingDot(arg1, "bl"), MkZingDot(arg2, "bl")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Or.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, MkZingApply(ZingData.Cnst_Or, MkZingDot(arg1, "bl"), MkZingDot(arg2, "bl")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Eq.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(ZingData.Cnst_Bool, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtIsEqualValue"), arg1, arg2)));
                    var retVal = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(retVal, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), retVal, tmpVar)));
                    return new ZingTranslationInfo(retVal);
                }
                else if (op == PData.Cnst_NEq.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(ZingData.Cnst_Bool, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtIsEqualValue"), arg1, arg2)));
                    var retVal = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(retVal, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), retVal, MkZingApply(ZingData.Cnst_Not, tmpVar))));
                    return new ZingTranslationInfo(retVal);
                }
                else if (op == PData.Cnst_Lt.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, MkZingApply(ZingData.Cnst_Lt, MkZingDot(arg1, "nt"), MkZingDot(arg2, "nt")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Le.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, MkZingApply(ZingData.Cnst_Le, MkZingDot(arg1, "nt"), MkZingDot(arg2, "nt")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Gt.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, MkZingApply(ZingData.Cnst_Gt, MkZingDot(arg1, "nt"), MkZingDot(arg2, "nt")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Ge.Node.Name)
                {
                    var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, MkZingApply(ZingData.Cnst_Ge, MkZingDot(arg1, "nt"), MkZingDot(arg2, "nt")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Idx.Node.Name)
                {
                    var type = LookupType(ctxt, GetArgByIndex(ft, 1));
                    var typeOp = ((Id)type.Function).Name;
                    if (typeOp == PData.Con_TupType.Node.Name)
                    {
                        var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                        ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtTupleGet"), arg1, arg2)));
                        return new ZingTranslationInfo(tmpVar);
                    }
                    else if (typeOp == PData.Con_SeqType.Node.Name)
                    {
                        var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                        ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtSeqGet"), arg1, arg2)));
                        return new ZingTranslationInfo(tmpVar);
                    }
                    else
                    {
                        // op == PData.Con_MapType.Node.Name
                        var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                        ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtMapGet"), arg1, arg2)));
                        return new ZingTranslationInfo(tmpVar);
                    }
                }
                else
                {
                    // op == PData.Cnst_In.Node.Name
                    var tmpVar = ctxt.GetTmpVar(ZingData.Cnst_Bool, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtMapExists"), arg2, arg1)));
                    var retVal = ctxt.GetTmpVar(PrtValue, "tmpVar");
                    ctxt.AddSideEffect(MkZingAssign(retVal, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), retVal, tmpVar)));
                    return new ZingTranslationInfo(retVal);
                }
            }
        }

        private int GetFieldIndex(string fieldName, FuncTerm nmdTupType)
        {
            int fieldIndex = 0;
            while (nmdTupType != null)
            {
                var fieldInfo = (FuncTerm)GetArgByIndex(nmdTupType, 0);
                var fieldNameInFieldInfo = (Cnst)GetArgByIndex(fieldInfo, 0);
                if (fieldName == fieldNameInFieldInfo.GetStringValue())
                    return fieldIndex;
                nmdTupType = GetArgByIndex(nmdTupType, 1) as FuncTerm;
                fieldIndex++;
            }
            Debug.Assert(false);
            return 0;
        }

        ZingTranslationInfo FoldField(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var expr = GetArgByIndex(ft, 0);
            var field = (Cnst)GetArgByIndex(ft, 1);
            int fieldIndex;
            if (field.CnstKind == CnstKind.Numeric)
            {
                fieldIndex = (int)field.GetNumericValue().Numerator;
            }
            else
            {
                fieldIndex = GetFieldIndex(field.GetStringValue(), LookupType(ctxt, expr));
            }
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                var arg = it.Current.node;
                var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVal");
                ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtTupleGet"), arg, Factory.Instance.MkCnst(fieldIndex))));
                return new ZingTranslationInfo(tmpVar);
            }
        }

        ZingTranslationInfo FoldDefault(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var typeArg = (FuncTerm)GetArgByIndex(ft, 0);
            var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
            ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(typeArg))));
            return new ZingTranslationInfo(tmpVar);
        }

        ZingTranslationInfo FoldCast(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var typeArg = (FuncTerm)GetArgByIndex(ft, 1);
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                var valueArg = it.Current.node;
                var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpVar");
                ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtCastValue, valueArg, typeContext.PTypeToZingExpr(typeArg))));
                return new ZingTranslationInfo(tmpVar);
            }
        }

        ZingTranslationInfo FoldTuple(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var tupType = LookupType(ctxt, ft);
            var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpTuple");
            ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(tupType))));
            int i = 0;
            foreach (var c in children)
            {
                ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtTupleSet"), tmpVar, Factory.Instance.MkCnst(i), c.node)));
                i++;
            }
            return new ZingTranslationInfo(tmpVar);
        }

        ZingTranslationInfo FoldNamedTuple(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var tupType = LookupType(ctxt, ft);
            var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpTuple");
            ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(tupType))));
            int i = 0;
            foreach (var c in children)
            {
                ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtTupleSet"), tmpVar, Factory.Instance.MkCnst(i), c.node)));
                i++;
            }
            return new ZingTranslationInfo(tmpVar);
        }

        ZingTranslationInfo FoldPush(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var qualifiedName = (FuncTerm)GetArgByIndex(ft, 0);
            var stateName = GetNameFromQualifiedName(ctxt.machineName, qualifiedName);
            var afterLabel = ctxt.GetFreshLabel();
            var res = MkZingSeq(
                MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Call"), Factory.Instance.MkCnst(ctxt.LabelToId(afterLabel)), MkZingState(stateName))),
                MkZingReturn(ZingData.Cnst_Nil),
                MkZingLabeledStmt(afterLabel, ZingData.Cnst_Nil));
            return new ZingTranslationInfo(res);
        }

        ZingTranslationInfo FoldNewStmt(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            return FoldNew(ft, children, ctxt);
        }

        ZingTranslationInfo FoldRaise(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                var eventExpr = MkZingDot(it.Current.node, "ev");
                it.MoveNext();
                var payloadExpr = it.Current.node;
                var assertStmt = MkZingAssert(MkZingApply(ZingData.Cnst_NEq, eventExpr, MkZingIdentifier("null")));
                string traceString = string.Format("\"<RaiseLog> Machine {0}-{{0}} raised Event {{1}}\\n\"", ctxt.machineName);
                var traceStmt = MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(traceString), MkZingDot("myHandle", "instance"), MkZingDot(eventExpr, "name")));
                var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpPayload");
                if (payloadExpr == ZingData.Cnst_Nil)
                {
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeNull.Node))));
                }
                else
                {
                    ctxt.AddSideEffect(MkZingAssignWithClone(tmpVar, payloadExpr));
                }

                var assignStmt = MkZingSeq(MkZingAssign(MkZingDot("myHandle", "currentEvent"), eventExpr), MkZingAssign(MkZingDot("myHandle", "currentArg"), tmpVar));
                var createRetCtxt = MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Raise")));
                return new ZingTranslationInfo(MkZingSeq(assertStmt, traceStmt, assignStmt, createRetCtxt, MkZingReturn(ZingData.Cnst_Nil)));
            }
        }

        ZingTranslationInfo FoldSend(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                var targetExpr = MkZingDot(it.Current.node, "mach");
                it.MoveNext();
                var eventExpr = MkZingDot(it.Current.node, "ev");
                it.MoveNext();
                AST<Node> payloadExpr = it.Current.node;
                var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpPayload");
                if (payloadExpr == ZingData.Cnst_Nil)
                {
                    ctxt.AddSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeNull.Node))));
                }
                else
                {
                    ctxt.AddSideEffect(MkZingAssignWithClone(tmpVar, payloadExpr));
                }
                var afterLabel = ctxt.GetFreshLabel();
                ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(targetExpr, "EnqueueEvent"), eventExpr, tmpVar, Factory.Instance.MkCnst("myHandle"))));
                ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Send"), Factory.Instance.MkCnst(ctxt.LabelToId(afterLabel)))));
                return new ZingTranslationInfo(MkZingSeq(MkZingReturn(ZingData.Cnst_Nil), MkZingLabeledStmt(afterLabel, ZingData.Cnst_Nil)));
            }
        }

        ZingTranslationInfo FoldMonitor(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var typeName = GetName(ft, 0);
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                AST<Node> evt = it.Current.node;
                it.MoveNext();
                AST<Node> arg = it.Current.node != ZingData.Cnst_Nil ? it.Current.node : MkZingIdentifier("null");
                var tmpVar = ctxt.GetTmpVar(PrtValue, "tmpSendPayload");
                ctxt.AddSideEffect(MkZingAssignWithClone(tmpVar, arg));
                MachineInfo machineInfo = allMachines[typeName];
                return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot("Main", string.Format("InvokeMachine_{0}", typeName)), MkZingDot(evt, "ev"), tmpVar)));
            }
        }

        ZingTranslationInfo FoldFunStmt(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            return FoldFunApp(ft, children, ctxt);
        }

        ZingTranslationInfo FoldNulStmt(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var op = ((Id)GetArgByIndex(ft, 0)).Name;
            if (op == PData.Cnst_Pop.Node.Name)
            {
                ctxt.AddSideEffect(MkZingAssign(MkZingDot("myHandle", "currentEvent"), MkZingIdentifier("null")));
                ctxt.AddSideEffect(MkZingAssign(MkZingDot("myHandle", "currentArg"), MkZingIdentifier("null")));
                ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Pop"))));
                ctxt.AddSideEffect(MkZingReturn(ZingData.Cnst_Nil));
            }
            return new ZingTranslationInfo(ZingData.Cnst_Nil);

        }

        ZingTranslationInfo FoldUnStmt(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var op = ((Id)GetArgByIndex(ft, 0)).Name;
            // op == PData.Con_Assert.Node.Name
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                return new ZingTranslationInfo(MkZingAssert(MkZingDot(it.Current.node, "bl")));
            }
        }

        ZingTranslationInfo FoldBinStmt(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            var op = ((Id)GetArgByIndex(ft, 0)).Name;
            var lhs = (FuncTerm)GetArgByIndex(ft, 1);
            var type = LookupType(ctxt, lhs);
            var typeName = ((Id)type.Function).Name;
            AST<Node> src = null, index = null, dest = null;
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                dest = it.Current.node;
                it.MoveNext();
                src = it.Current.node;
                if (it.MoveNext())
                {
                    index = src;
                    src = it.Current.node;
                }

                if (op == PData.Cnst_Assign.Node.Name)
                {
                    if (((Id)lhs.Function).Name == PData.Con_Field.Node.Name)
                    {
                        var field = (Cnst)GetArgByIndex(lhs, 1);
                        int fieldIndex;
                        if (field.CnstKind == CnstKind.Numeric)
                        {
                            fieldIndex = (int)field.GetNumericValue().Numerator;
                        }
                        else
                        {
                            fieldIndex = GetFieldIndex(field.GetStringValue(), LookupType(ctxt, GetArgByIndex(lhs, 0)));
                        }
                        return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot("PRT_VALUE", "PrtTupleSet"), dest, Factory.Instance.MkCnst(fieldIndex), src)));
                    }
                    else if (index == null)
                    {
                        return new ZingTranslationInfo(MkZingAssignWithClone(dest, src));
                    }
                    else
                    {
                        lhs = (FuncTerm)GetArgByIndex(lhs, 1);
                        type = LookupType(ctxt, lhs);
                        typeName = ((Id)type.Function).Name;
                        if (typeName == PData.Con_SeqType.Node.Name)
                        {
                            return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot("PRT_VALUE", "PrtSeqSet"), dest, index, src)));
                        }
                        else
                        {
                            // type is PMapType
                            return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot("PRT_VALUE", "PrtMapSet"), dest, index, src)));
                        }
                    }
                }

                if (op == PData.Cnst_Remove.Node.Name)
                {
                    if (typeName == PData.Con_SeqType.Node.Name)
                    {
                        return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtSeqRemove"), dest, src)));
                    }
                    else
                    {
                        return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtMapRemove"), dest, src)));
                    }
                }
                else
                {
                    // op == PData.Cnst_Insert.Node.Name
                    var idx = ctxt.GetTmpVar(PrtValue, "idx");
                    var val = ctxt.GetTmpVar(PrtValue, "val");
                    ctxt.AddSideEffect(MkZingAssign(idx, MkZingCall(MkZingDot("PRT_VALUE", "PrtTupleGet"), src, Factory.Instance.MkCnst(0))));
                    ctxt.AddSideEffect(MkZingAssign(val, MkZingCall(MkZingDot("PRT_VALUE", "PrtTupleGet"), src, Factory.Instance.MkCnst(1))));
                    if (typeName == PData.Con_SeqType.Node.Name)
                    {
                        return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot("PRT_VALUE", "PrtSeqInsert"), dest, idx, val)));
                    }
                    else
                    {
                        return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot("PRT_VALUE", "PrtMapInsert"), dest, idx, val)));
                    }
                }
            }
        }

        ZingTranslationInfo FoldReturn(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            FunInfo funInfo = ctxt.entityInfo as FunInfo;
            AST<FuncTerm> returnType = PTypeNull;
            if (funInfo != null)
            {
                returnType = funInfo.returnType;
            }
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                if (returnType.Equals(PTypeNull))
                {
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Return"))));
                }
                else
                {
                    ctxt.AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "ReturnVal"), it.Current.node)));
                }
                ctxt.AddSideEffect(MkZingReturn(ZingData.Cnst_Nil));
                return new ZingTranslationInfo(ZingData.Cnst_Nil);
            }
        }

        ZingTranslationInfo FoldWhile(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                var condExpr = MkZingDot(it.Current.node, "bl");
                it.MoveNext();
                var loopStart = GetUnique(ctxt.entityName + "_loop_start");
                var loopEnd = GetUnique(ctxt.entityName + "_loop_end");
                var body = it.Current.node;
                body = ctxt.EmitZingSideEffects(body);
                var res = MkZingLabeledStmt(loopStart, MkZingSeq(
                    ctxt.EmitZingSideEffects(MkZingIfThen(MkZingApply(ZingData.Cnst_Not, condExpr), AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst(loopEnd)))),
                    body,
                    AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst(loopStart)),
                    MkZingLabeledStmt(loopEnd, ZingData.Cnst_Nil)));
                return new ZingTranslationInfo(res);
            }
        }

        ZingTranslationInfo FoldIte(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                var condExpr = MkZingDot(it.Current.node, "bl");
                it.MoveNext();
                var thenStmt = it.Current.node;
                it.MoveNext();
                var elseStmt = it.Current.node;

                // Order in which we emit side effets (else,then) is the reverse of the order in which the side effect stacks were pushed(then, else).
                var ifName = GetUnique(ctxt.entityName + "_if");
                var elseLabel = ifName + "_else";
                var afterLabel = ifName + "_end";
                var cookedElse = MkZingLabeledStmt(elseLabel, ctxt.EmitZingSideEffects(elseStmt));
                var cookedThen = ctxt.EmitZingSideEffects(thenStmt);
                var res = MkZingSeq(MkZingIfThen(MkZingApply(ZingData.Cnst_Not, condExpr), AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst(elseLabel))),
                    cookedThen,
                    AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst(afterLabel)),
                    cookedElse,
                    MkZingLabeledStmt(afterLabel, ZingData.Cnst_Nil));
                return new ZingTranslationInfo(res);
            }
        }

        ZingTranslationInfo FoldSeq(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingFoldContext ctxt)
        {
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                var first = it.Current;
                it.MoveNext();
                var second = it.Current;
                return new ZingTranslationInfo(MkZingSeq(first.node, ctxt.EmitZingSideEffects(second.node)));
            }
        }

        private AST<Node> MkZingFunMethod(string machineName, string funName, FunInfo funInfo)
        {
            AST<Node> parameters = LocalVariablesToVarDecls(funInfo.parameterNames, funInfo.parameterNameToInfo);
            parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("entryCtxt", Factory.Instance.MkCnst("Continuation")), parameters);

            var ctxt = new ZingFoldContext(this, machineName, funName, funInfo);
            var tuple = Factory.Instance.ToAST(funInfo.body).Compute<ZingTranslationInfo>(
                x => ZingUnfold(ctxt, x),
                (x, ch) => ZingFold(ctxt, x, ch));
            Debug.Assert(ctxt.sideEffectsStack.Count == 1);
            var funBody = ctxt.EmitZingSideEffects(tuple.node);
            ctxt.PushSideEffectStack();
            funBody = MkZingSeq(
                ctxt.EmitLabelPrelude(),
                MkZingLabeledStmt("start", funBody),
                MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Return"))),
                MkZingReturn(ZingData.Cnst_Nil));
            return MkZingMethodDecl(funName, parameters, ZingData.Cnst_Void, ctxt.EmitLocals(), MkZingBlocks(MkZingBlock("dummy", funBody)));
        }

        private AST<Node> LocalVariablesToVarDecls(IEnumerable<string> varNames, Dictionary<string, VariableInfo> varNameToVarInfo)
        {
            return MkZingVarDecls(varNames.Select(name => MkZingVarDecl(name, PrtValue)));
        }

        private AST<Node> MkInvokeMonitorMethod(string machineName)
        {
            var parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("arg", Factory.Instance.MkCnst("PRT_VALUE")), ZingData.Cnst_Nil);
            parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("evt", Factory.Instance.MkCnst("SM_EVENT")), parameters);

            var localVars = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("cont", Factory.Instance.MkCnst("Continuation")), ZingData.Cnst_Nil);

            var cont = MkZingIdentifier("cont");
            var machineInstance = MkZingIdentifier(string.Format("{0}_instance", machineName));
            var machineHandles = MkZingDot("Main", GetMonitorMachineName(machineName));
            string errorTraceString = string.Format("\"<StateLog> Unhandled event exception by machine {0}-{{0}}\\n\"", machineName);
            
            AST<Node> loopBody =
                MkZingSeq(
                    MkZingAssign(MkZingDot("iter", "myHandle", "currentEvent"), MkZingIdentifier("evt")),
                    MkZingAssign(MkZingDot("iter", "myHandle", "currentArg"), MkZingIdentifier("arg")),
                    MkZingAssign(cont, MkZingCall(MkZingDot("iter", "RunHelper"), ZingData.Cnst_True)),
                    MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Pop")),
                                 MkZingAssign(machineHandles, MkZingSub(machineHandles, MkZingIdentifier("iter")))),
                    MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Raise")),
                                 MkZingSeq(MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(errorTraceString), MkZingDot("iter", "instance"))),
                                           MkZingAssert(ZingData.Cnst_False))));
            AST<Node> body = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("dummy"),
                AddArgs(ZingData.App_Foreach, Factory.Instance.MkCnst(machineName), Factory.Instance.MkCnst("iter"), MkZingDot("Main", GetMonitorMachineName(machineName)), loopBody));
            return MkZingMethodDecl(string.Format("InvokeMachine_{0}", machineName), parameters, ZingData.Cnst_Void, localVars, ConstructList(ZingData.App_Blocks, body), ZingData.Cnst_Static);
        }

        AST<Node> MkInitializers(string machineName, string obj)
        {
            List<AST<Node>> initializers = new List<AST<Node>>();
            var machineInfo = allMachines[machineName];
            foreach (string varName in machineInfo.localVariableToVarInfo.Keys)
            {
                var t = machineInfo.localVariableToVarInfo[varName].type;
                initializers.Add(MkZingAssign(MkZingDot(obj, varName), MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(t))));
            }
            return MkZingSeq(initializers);
        }

        private AST<Node> MkCreateMonitorMethod(string machineName)
        {
            var objectName = string.Format("o_{0}", machineName);
            var parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("arg", Factory.Instance.MkCnst("PRT_VALUE")), ZingData.Cnst_Nil);
            var localVars = AddArgs(ZingData.App_VarDecls, MkZingVarDecl(objectName, Factory.Instance.MkCnst(machineName)), ZingData.Cnst_Nil);
            localVars = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("chooseMonitor", ZingData.Cnst_Bool), localVars);
            localVars = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("cont", Factory.Instance.MkCnst("Continuation")), localVars);

            var machineInfo = allMachines[machineName];
            AST<Node> body = ZingData.Cnst_Nil;
            if (machineInfo.monitorType != MonitorType.SAFETY)
            {
                List<AST<Node>> stmts = new List<AST<Node>>();
                stmts.Add(MkZingIfThen(MkZingApply(ZingData.Cnst_NEq, MkZingDot("FairCycle", "gate"), MkZingDot("GateStatus", "Init")), MkZingReturn(ZingData.Cnst_Nil)));
                stmts.Add(MkZingAssign(MkZingIdentifier("chooseMonitor"), MkZingCall(Factory.Instance.MkCnst("choose"), Factory.Instance.MkCnst("bool"))));
                stmts.Add(MkZingIfThen(MkZingIdentifier("chooseMonitor"), MkZingReturn(ZingData.Cnst_Nil)));
                if (machineInfo.monitorType == MonitorType.FINALLY)
                {
                    stmts.Add(MkZingAssign(MkZingDot("FairCycle", "gate"), MkZingDot("GateStatus", "Closed")));
                }
                body = MkZingSeq(stmts);
            }

            var cont = MkZingIdentifier("cont");
            var machineInstance = MkZingIdentifier(string.Format("{0}_instance", machineName));
            var machineHandles = MkZingDot("Main", GetMonitorMachineName(machineName));
            string createTraceString = string.Format("\"<CreateLog> Created Machine {0}-{{0}}\\n\"", machineName);
            string errorTraceString = string.Format("\"<StateLog> Unhandled event exception by machine {0}-{{0}}\\n\"", machineName);
            
            body = MkZingSeq(body,
                    MkZingAssign(MkZingIdentifier(objectName), AddArgs(ZingData.App_New, Factory.Instance.MkCnst(machineName), ZingData.Cnst_Nil)),
                    MkInitializers(machineName, objectName),
                    MkZingAssign(MkZingDot(objectName, "myHandle"),
                                 MkZingCall(MkZingDot("SM_HANDLE", "Construct"), MkZingDot("Machine", string.Format("_{0}", machineName)), machineInstance, Factory.Instance.MkCnst(0))),
                    MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(createTraceString), machineInstance)),
                    MkZingAssign(MkZingDot(objectName, "myHandle", "currentArg"), MkZingIdentifier("arg")),
                    MkZingAssign(machineInstance, MkZingAdd(machineInstance, Factory.Instance.MkCnst(1))),
                    MkZingCallStmt(MkZingCall(MkZingDot("myHandle", "Push"))),
                    MkZingAssign(MkZingDot(objectName, "myHandle", "stack", "state"), MkZingState(machineInfo.initStateName)),
                    MkZingAssign(machineHandles, MkZingAdd(machineHandles, MkZingIdentifier(objectName))),
                    MkZingAssign(cont, MkZingCall(MkZingDot(objectName, "RunHelper"), ZingData.Cnst_True)),
                    MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Pop")), 
                                 MkZingAssign(machineHandles, MkZingSub(machineHandles, MkZingIdentifier(objectName)))),
                    MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Raise")), 
                                 MkZingSeq(MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(errorTraceString), MkZingDot(objectName, "instance"))),
                                           MkZingAssert(ZingData.Cnst_False)))
                    );
            body = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("dummy"), body);

            return MkZingMethodDecl(string.Format("CreateMachine_{0}", machineName), parameters, ZingData.Cnst_Void, localVars, ConstructList(ZingData.App_Blocks, body), ZingData.Cnst_Static);
        }

        private AST<Node> MkCreateMachineMethod(string machineName)
        {
            var objectName = string.Format("o_{0}", machineName);
            var parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("arg", Factory.Instance.MkCnst("PRT_VALUE")), ZingData.Cnst_Nil);
            var localVars = AddArgs(ZingData.App_VarDecls, MkZingVarDecl(objectName, Factory.Instance.MkCnst(machineName)), ZingData.Cnst_Nil);
            localVars = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("fairScheduler", Factory.Instance.MkCnst("FairScheduler")), localVars);
            localVars = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("fairChoice", Factory.Instance.MkCnst("FairChoice")), localVars);

            string createTraceString = string.Format("\"<CreateLog> Created Machine {0}-{{0}}\\n\"", machineName);
            var machineInstance = MkZingIdentifier(string.Format("{0}_instance", machineName));
            var body = MkZingSeq(
                    MkZingAssign(MkZingIdentifier(objectName), AddArgs(ZingData.App_New, Factory.Instance.MkCnst(machineName), ZingData.Cnst_Nil)),
                    MkInitializers(machineName, objectName),
                    MkZingAssign(MkZingDot(objectName, "myHandle"),
                                 MkZingCall(MkZingDot("SM_HANDLE", "Construct"), MkZingDot("Machine", string.Format("_{0}", machineName)), machineInstance, Factory.Instance.MkCnst(allMachines[machineName].maxQueueSize))),
                    MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(createTraceString), machineInstance)),
                    MkZingAssign(MkZingDot(objectName, "myHandle", "currentArg"), MkZingIdentifier("arg")),
                    MkZingAssign(machineInstance, MkZingApply(ZingData.Cnst_Add, machineInstance, Factory.Instance.MkCnst(1)))
                    );

            if (compiler.Options.liveness == LivenessOption.Standard)
            {
                body = MkZingSeq(body,
                                 MkZingAssign(MkZingIdentifier("fairScheduler"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("FairScheduler"), ZingData.Cnst_Nil)),
                                 MkZingCallStmt(MkZingCall(MkZingDot("FairScheduler", "Init"), MkZingIdentifier("fairScheduler"), MkZingDot(objectName, "myHandle"))));
                foreach (var funName in allMachines[machineName].funNameToFunInfo.Keys)
                {
                    var funInfo = allMachines[machineName].funNameToFunInfo[funName];
                    for (int i = 0; i < funInfo.numFairChoices; i++)
                    {
                        body = MkZingSeq(body,
                                         MkZingAssign(MkZingIdentifier("fairChoice"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("FairChoice"), ZingData.Cnst_Nil)),
                                         MkZingCallStmt(MkZingCall(MkZingDot("FairChoice", "Init"), MkZingIdentifier("fairChoice"))),
                                         MkZingAssign(MkZingDot(objectName, GetFairChoice(funName, i)), MkZingIdentifier("fairChoice")));
                    }
                }
            }

            body = MkZingSeq(
                body,
                MkZingCallStmt(MkZingCall(MkZingDot(objectName, "Start")), ZingData.Cnst_Async),
                MkZingCallStmt(MkZingCall(MkZingIdentifier("invokescheduler"), Factory.Instance.MkCnst("\"map\""), MkZingDot(objectName, "myHandle", "machineId"))),
                Factory.Instance.AddArg(ZingData.App_Return, MkZingDot(objectName, "myHandle"))
                );
            body = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("dummy"), body);

            return MkZingMethodDecl(string.Format("CreateMachine_{0}", machineName), parameters, Factory.Instance.MkCnst("SM_HANDLE"), localVars, ConstructList(ZingData.App_Blocks, body), ZingData.Cnst_Static);
        }

        private FuncTerm LookupType(ZingFoldContext ctxt, Node node)
        {
            return ctxt.entityInfo.typeInfo[Factory.Instance.ToAST(node)];
        }

        TypeTranslationContext typeContext;

        internal class TypeTranslationContext
        {
            private int fieldCount;
            private int typeCount;
            private List<AST<Node>> computation;
            private Dictionary<AST<Node>, AST<Node>> pTypeToZingExpr;

            public TypeTranslationContext()
            {
                fieldCount = 0;
                typeCount = 0;
                computation = new List<AST<Node>>();
                pTypeToZingExpr = new Dictionary<AST<Node>, AST<Node>>();
            }

            public AST<Node> InitializeTypesAndFields()
            {
                return MkZingSeq(computation);
            }

            public IEnumerable<AST<Node>> MainVarDecls()
            {
                List<AST<Node>> varDecls = new List<AST<Node>>();
                for (int i = 0; i < fieldCount; i++)
                {
                    varDecls.Add(MkZingVarDecl(string.Format("field_{0}_PRT_FIELD_NAME", i), Factory.Instance.MkCnst("PRT_FIELD_NAME"), ZingData.Cnst_Static));
                }
                for (int i = 0; i < typeCount; i++)
                {
                    varDecls.Add(MkZingVarDecl(string.Format("type_{0}_PRT_TYPE", i), Factory.Instance.MkCnst("PRT_TYPE"), ZingData.Cnst_Static));
                }
                return varDecls;
            }

            private AST<FuncTerm> GetField()
            {
                var retVal = MkZingDot("Main", string.Format("field_{0}_PRT_FIELD_NAME", fieldCount));
                fieldCount++;
                return retVal;
            }

            private new AST<FuncTerm> GetType()
            {
                var retVal = MkZingDot("Main", string.Format("type_{0}_PRT_TYPE", typeCount));
                typeCount++;
                return retVal;
            }

            private void AddSideEffect(AST<Node> n)
            {
                computation.Add(n);
            }

            public AST<Node> PTypeToZingExpr(FuncTerm pType)
            {
                var pTypeAST = Factory.Instance.ToAST(pType);
                if (!pTypeToZingExpr.ContainsKey(pTypeAST))
                {
                    pTypeToZingExpr[pTypeAST] = ConstructType(pType);
                }
                return pTypeToZingExpr[pTypeAST];
            }

            private AST<Node> ConstructType(FuncTerm type)
            {
                string typeKind = ((Id)type.Function).Name;
                if (typeKind == "BaseType")
                {
                    var primitiveType = ((Id)GetArgByIndex(type, 0)).Name;
                    if (primitiveType == "NULL")
                    {
                        var tmpVar = GetType();
                        AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_NULL"))));
                        return tmpVar;
                    }
                    else if (primitiveType == "BOOL")
                    {
                        var tmpVar = GetType();
                        AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_BOOL"))));
                        return tmpVar;
                    }
                    else if (primitiveType == "INT")
                    {
                        var tmpVar = GetType();
                        AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_INT"))));
                        return tmpVar;
                    }
                    else if (primitiveType == "EVENT")
                    {
                        var tmpVar = GetType();
                        AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_EVENT"))));
                        return tmpVar;
                    }
                    else if (primitiveType == "REAL")
                    {
                        var tmpVar = GetType();
                        AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_REAL"))));
                        return tmpVar;
                    }
                    else if (primitiveType == "MODEL")
                    {
                        var tmpVar = GetType();
                        AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_MODEL"))));
                        return tmpVar;
                    }
                    else if (primitiveType == "ANY")
                    {
                        var tmpVar = GetType();
                        AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_ANY"))));
                        return tmpVar;
                    }
                    else
                    {
                        var tmpVar = GetType();
                        AddSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_FORGN"))));
                        return tmpVar;
                    }
                }
                else if (typeKind == "TupType")
                {
                    List<AST<Node>> memberTypes = new List<AST<Node>>();
                    while (type != null)
                    {
                        memberTypes.Add(PTypeToZingExpr((FuncTerm)GetArgByIndex(type, 0)));
                        type = GetArgByIndex(type, 1) as FuncTerm;
                    }
                    var tupleType = GetType();
                    AddSideEffect(MkZingAssign(tupleType, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkTupType"), Factory.Instance.MkCnst(memberTypes.Count))));
                    for (int i = 0; i < memberTypes.Count; i++)
                    {
                        AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("PRT_TYPE", "PrtSetFieldType"), tupleType, Factory.Instance.MkCnst(i), memberTypes[i])));
                    }
                    return tupleType;
                }
                else if (typeKind == "NmdTupType")
                {
                    List<AST<Node>> memberNames = new List<AST<Node>>();
                    List<AST<Node>> memberTypes = new List<AST<Node>>();
                    while (type != null)
                    {
                        var typeField = (FuncTerm)GetArgByIndex(type, 0);
                        memberNames.Add(GetField());
                        memberTypes.Add(PTypeToZingExpr((FuncTerm)GetArgByIndex(typeField, 1)));
                        type = GetArgByIndex(type, 1) as FuncTerm;
                    }
                    var tupleType = GetType();
                    AddSideEffect(MkZingAssign(tupleType, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkNmdTupType"), Factory.Instance.MkCnst(memberTypes.Count))));
                    for (int i = 0; i < memberTypes.Count; i++)
                    {
                        AddSideEffect(MkZingAssign(memberNames[i], AddArgs(ZingData.App_New, Factory.Instance.MkCnst("PRT_FIELD_NAME"), ZingData.Cnst_Nil)));
                        AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("PRT_TYPE", "PrtSetFieldName"), tupleType, Factory.Instance.MkCnst(i), memberNames[i])));
                        AddSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("PRT_TYPE", "PrtSetFieldType"), tupleType, Factory.Instance.MkCnst(i), memberTypes[i])));
                    }
                    return tupleType;
                }
                else if (typeKind == "SeqType")
                {
                    var innerType = PTypeToZingExpr((FuncTerm)GetArgByIndex(type, 0));
                    var seqType = GetType();
                    AddSideEffect(MkZingAssign(seqType, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkSeqType"), innerType)));
                    return seqType;
                }
                else
                {
                    // typeKind == "MapType"
                    var domType = PTypeToZingExpr((FuncTerm)GetArgByIndex(type, 0));
                    var codType = PTypeToZingExpr((FuncTerm)GetArgByIndex(type, 1));
                    var mapType = GetType();
                    AddSideEffect(MkZingAssign(mapType, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkMapType"), domType, codType)));
                    return mapType;
                }
            }
        }
        #endregion
    }
}
