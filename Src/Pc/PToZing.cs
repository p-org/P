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
        public ActionFun exitFun;

        public TransitionInfo(string target, bool isPush)
        {
            this.target = target;
            this.isPush = isPush;
            this.exitFun = null;
        }

        public TransitionInfo(string target, Node exitFun)
        {
            this.target = target;
            this.isPush = false;
            if (exitFun.NodeKind == NodeKind.Cnst)
            {
                this.exitFun = new NamedActionFun(((Cnst)exitFun).GetStringValue());
            }
            else
            {
                this.exitFun = new AnonymousActionFun(exitFun);
            }
        }
    }

    internal interface ActionFun
    {

    }

    internal class NamedActionFun : ActionFun
    {
        public string s;
        public NamedActionFun(string s)
        {
            this.s = s;
        }
    }

    internal class AnonymousActionFun : ActionFun
    {
        public Node n;

        public AnonymousActionFun(Node n)
        {
            this.n = n;
        }
    }

    internal class StateInfo
    {
        public string ownerName;
        public ActionFun entryAction;
        public ActionFun exitFun;
        public bool hasDefaultTransition;
        public Dictionary<string, TransitionInfo> transitions;
        public Dictionary<string, ActionFun> actions;
        public List<string> deferredEvents;
        public List<string> ignoredEvents;
        public bool isStable;
        public int numFairChoicesInEntry;
        public int numFairChoicesInExit;

        public Dictionary<AST<Node>, FuncTerm> typeInfo;

        public StateInfo(string ownerName, Node entryAction, Node exitFun, bool isStable)
        {
            this.ownerName = ownerName;
            if (entryAction.NodeKind == NodeKind.Cnst)
            {
                this.entryAction = new NamedActionFun(((Cnst)entryAction).GetStringValue());
            }
            else
            {
                this.entryAction = new AnonymousActionFun(entryAction);
            }
            if (exitFun.NodeKind == NodeKind.Cnst)
            {
                this.exitFun = new NamedActionFun(((Cnst)exitFun).GetStringValue());
            }
            else
            {
                this.exitFun = new AnonymousActionFun(exitFun);
            }
            this.hasDefaultTransition = false;
            this.transitions = new Dictionary<string, TransitionInfo>();
            this.actions = new Dictionary<string, ActionFun>();
            this.deferredEvents = new List<string>();
            this.ignoredEvents = new List<string>();
            this.isStable = isStable;
            this.numFairChoicesInEntry = 0;
            this.numFairChoicesInExit = 0;

            this.typeInfo = new Dictionary<AST<Node>, FuncTerm>();
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
        public Dictionary<string, VariableInfo> parameterNameToInfo;
        public List<string> parameterNames;
        public AST<Node> returnType;
        public string ownerName;
        public Node body;
        public int numFairChoices;

        public Dictionary<AST<Node>, FuncTerm> typeInfo;

        public FunInfo(string ownerName, bool isModel, AST<Node> returnType, Node body)
        {
            this.ownerName = ownerName;
            this.isModel = isModel;
            this.parameterNameToInfo = new Dictionary<string, VariableInfo>();
            this.parameterNames = new List<string>();
            this.returnType = returnType;
            this.body = body;
            this.numFairChoices = 0;

            typeInfo = new Dictionary<AST<Node>, FuncTerm>();
        }
    }

    internal class ActionInfo
    {
        public Node actionFun;
        public int numFairChoices;

        public Dictionary<AST<Node>, FuncTerm> typeInfo;

        public ActionInfo(Node actionFun)
        {
            this.actionFun = actionFun;
            this.numFairChoices = 0;

            typeInfo = new Dictionary<AST<Node>, FuncTerm>();
        }
    }

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
        public Dictionary<string, ActionInfo> actionNameToActionInfo;
        public bool isInfinitelyOftenMonitor;

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
            actionNameToActionInfo = new Dictionary<string, ActionInfo>();
            isInfinitelyOftenMonitor = false;
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
        public const string SPEC_HANDLE = "SPEC_HANDLE";
        public static AST<Node> SpecHandle = Factory.Instance.MkCnst("SPEC_HANDLE");
        public static string SM_EVENT_SET = "SM_EVENT_SET";
        public static AST<Node> SmEventSet = Factory.Instance.MkCnst("SM_EVENT_SET");
        public const string PRT_VALUE = "PRT_VALUE";
        public static AST<Node> PrtValue = Factory.Instance.MkCnst("PRT_VALUE");
        public static AST<Node> PrtCastValue = MkZingDot("PRT_VALUE", "PrtCastValue");
        public static AST<Node> PrtMkDefaultValue = MkZingDot("PRT_VALUE", "PrtMkDefaultValue");
        public static AST<Node> PrtCloneValue = MkZingDot("PRT_VALUE", "PrtCloneValue");
        public static AST<Node> PrtIsEqualValue = MkZingDot("PRT_VALUE", "PrtIsEqualValue");

        public const string NullEvent = "null";
        public const string DefaultEvent = "default";
        public const string HaltEvent = "halt";

        private Dictionary<string, LinkedList<AST<FuncTerm>>> factBins; 
        private Dictionary<string, AST<FuncTerm>> modelAliases;
        public Dictionary<string, EventInfo> allEvents;
        public Dictionary<string, MachineInfo> allMachines;
        public string mainMachineName;

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
            this.modelAliases = new Dictionary<string, AST<FuncTerm>>();
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
                    if (mf.Binding != null)
                    {
                        modelAliases.Add(((Id)mf.Binding).Name, (AST<FuncTerm>)Factory.Instance.ToAST(mf.Match));
                    }

                    FuncTerm ft = (FuncTerm)mf.Match;
                    GetBin(ft).AddLast((AST<FuncTerm>)Factory.Instance.ToAST(ft));
                });
        }

        public FuncTerm GetFuncTerm(Node node)
        {
            if (node.NodeKind == NodeKind.FuncTerm)
                return (FuncTerm)node;
            else if (node.NodeKind == NodeKind.Id)
                return modelAliases[((Id)node).Name].Node;
            Debug.Assert(false);
            return null;
        }

        public string GetMachineName(FuncTerm ft, int index)
        {
            FuncTerm machineDecl = GetFuncTerm(GetArgByIndex(ft, index));
            var machineName = GetName(machineDecl, 0);
            return machineName;
        }

        Dictionary<string, int> uniqIDCounters = new Dictionary<string, int>();
        public string getUnique(string prefix)
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
            allEvents[DefaultEvent] = new EventInfo(1, false, PTypeNull.Node);
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
                    var payloadType = (FuncTerm) (it.Current.NodeKind == NodeKind.Id ? PTypeNull.Node : it.Current);
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
                    var machineDecl = GetFuncTerm(it.Current);
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
                    var machineDecl = GetFuncTerm(it.Current);
                    var machineName = GetName(machineDecl, 0);
                    var machineInfo = allMachines[machineName];
                    it.MoveNext();
                    var isModel = ((Id)it.Current).Name == "MODEL"; 
                    it.MoveNext();
                    var iter = it.Current as FuncTerm;
                    it.MoveNext();
                    var returnTypeName = ((Id)it.Current).Name == "NIL" ? PTypeNull : Factory.Instance.ToAST(it.Current);
                    it.MoveNext();
                    var funInfo = new FunInfo(machineName, isModel, returnTypeName, it.Current);
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

            terms = GetBin("ActionDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var actionName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var actionOwnerMachineDecl = GetFuncTerm(it.Current);
                    var actionOwnerMachineName = GetName(actionOwnerMachineDecl, 0);
                    it.MoveNext();
                    allMachines[actionOwnerMachineName].actionNameToActionInfo[actionName] = new ActionInfo(it.Current);
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
                    var machineDecl = GetFuncTerm(it.Current);
                    var ownerName = GetName(machineDecl, 0);
                    var stateName = GetNameFromQualifiedName(ownerName, qualifiedStateName);
                    it.MoveNext();
                    var entryFun = it.Current;
                    it.MoveNext();
                    var exitFun = it.Current;
                    it.MoveNext();
                    var isStable = compiler.liveness != LivenessOption.None && ((Id)it.Current).Name == "TRUE";
                    var stateTable = allMachines[ownerName].stateNameToStateInfo;
                    stateTable[stateName] = new StateInfo(ownerName, entryFun, exitFun, isStable);
                }
            }

            terms = GetBin("TransDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var stateDecl = GetFuncTerm(it.Current);
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
                            eventName = DefaultEvent;
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
                    var action = it.Current;
                    if (action.NodeKind == NodeKind.Id)
                    {
                        stateTable.transitions[eventName] = new TransitionInfo(targetStateName, ((Id)action).Name == "PUSH");
                    }
                    else
                    {
                        stateTable.transitions[eventName] = new TransitionInfo(targetStateName, action);
                    }
                }
            }

            terms = GetBin("DoDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var stateDecl = GetFuncTerm(it.Current);
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
                        stateTable.actions[eventName] = new NamedActionFun(((Cnst)action).GetStringValue());
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
                        stateTable.actions[eventName] = new AnonymousActionFun(action);
                    }
                }
            }


            terms = GetBin("TypeOf");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    FuncTerm typingContext = GetFuncTerm(it.Current);
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
                    else if (typingContextKind == "ActionDecl")
                    {
                        string ownerName = GetOwnerName(typingContext, 1, 0);
                        string actionName = GetName(typingContext, 0);
                        allMachines[ownerName].actionNameToActionInfo[actionName].typeInfo[expr] = type;
                    }
                    else
                    {
                        // typingContextKind == "StateDecl" || typingContextKind == "TransDecl"
                        if (typingContextKind == "TransDecl")
                        {
                            typingContext = GetFuncTerm(GetArgByIndex(typingContext, 0));
                        }
                        string ownerName = GetOwnerName(typingContext, 1, 0);
                        var qualifiedStateName = (FuncTerm)GetArgByIndex(typingContext, 0);
                        var stateName = GetNameFromQualifiedName(ownerName, qualifiedStateName);
                        allMachines[ownerName].stateNameToStateInfo[stateName].typeInfo[expr] = type;
                    }
                }
            }

            if (compiler.liveness != LivenessOption.None)
            {
                foreach (var machineName in allMachines.Keys)
                {
                    if (!allMachines[machineName].IsMonitor) continue;
                    var machineInfo = allMachines[machineName];
                    HashSet<string> visitedStates = new HashSet<string>();
                    Stack<string> dfsStack = new Stack<string>();
                    foreach (var stateName in machineInfo.stateNameToStateInfo.Keys)
                    {
                        if (!machineInfo.stateNameToStateInfo[stateName].isStable) continue;
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
                        if (machineInfo.stateNameToStateInfo[stateName].isStable) continue;
                        machineInfo.isInfinitelyOftenMonitor = true;
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

        public static string getFtName(FuncTerm n)
        {
            return ((Id)n.Function).Name;
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

        public static string GetNameFromQualifiedName(string machineName, FuncTerm qualifiedName)
        {
            var stateName = "_" + machineName;
            while (qualifiedName != null)
            {
                stateName = "_" + GetName(qualifiedName, 0) + stateName;
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
                    return ((Cnst)GetArgByIndex(GetFuncTerm(ownerArg), ownerNameIndex)).GetStringValue();
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

        private static AST<Node> MkZingReturn(AST<Node> rVal)
        {
            return AddArgs(ZingData.App_Return, rVal);
        }

        public static AST<Node> MkZingIfThen(AST<Node> cond, AST<Node> then)
        {
            return AddArgs(ZingData.App_ITE, cond, then, ZingData.Cnst_Nil);
        }

        public static AST<Node> MkZingIfThenElse(AST<Node> cond, AST<Node> thenstmt, AST<Node> elsestmt)
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
            return AddArgs(ZingData.App_Apply,args);
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

        private static AST<FuncTerm> MkZingBlock(string blockName, AST<Node> body)
        {
            return AddArgs(ZingData.App_Blocks, AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst(blockName), body), ZingData.Cnst_Nil);
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

        private static AST<Node> MkZingAction(string machineName, string actionFunName)
        {
            return MkZingDot("ActionFun", string.Format("_{0}_{1}", machineName, actionFunName));
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

        private static AST<Node> MkZingAssert(AST<Node> condition, params AST<Node>[] msgs)
        {
            return AddArgs(ZingData.App_Assert, condition);
        }

        private static AST<Node> MkZingAssume(AST<Node> condition)
        {
            return AddArgs(ZingData.App_Assume, condition);
        }

        private static AST<FuncTerm> MkZingPlus(AST<Node> n, int i)
        {
            return MkZingApply(ZingData.Cnst_Add, n, Factory.Instance.MkCnst(i));
        }

        private static AST<FuncTerm> MkZingMinus(AST<Node> n, int i)
        {
            return MkZingApply(ZingData.Cnst_Sub, n, Factory.Instance.MkCnst(i));
        }

        private static AST<Node> MkZingListIter(AST<Node> iterVar, AST<Node> start, AST<Node> end, AST<Node> body)
        {
            return MkZingSeq(
                MkZingAssign(iterVar, start),
                AddArgs(ZingData.App_While, MkZingApply(ZingData.Cnst_NEq, iterVar, end),
                    MkZingSeq(body, MkZingAssign(iterVar, MkZingDot(iterVar, "next")))));
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
            foreach (string machineName in allMachines.Keys)
            {
                machineConsts.Add(Factory.Instance.MkCnst(string.Format("_{0}", machineName)));
            }
            var machineList = ConstructList(ZingData.App_EnumElems, machineConsts);
            elements.Add(AddArgs(ZingData.App_EnumDecl, Factory.Instance.MkCnst("Machine"), machineList));

            List<AST<Node>> eventConsts = new List<AST<Node>>();
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

            List<AST<Node>> actionFunConsts = new List<AST<Node>>();
            foreach (string machineName in allMachines.Keys)
            {
                foreach (string actionFunName in allMachines[machineName].actionNameToActionInfo.Keys)
                {
                    actionFunConsts.Add(Factory.Instance.MkCnst(string.Format("_{0}_{1}", machineName, actionFunName)));
                }
            }
            var actionFunList = ConstructList(ZingData.App_EnumElems, actionFunConsts);
            elements.Add(AddArgs(ZingData.App_EnumDecl, Factory.Instance.MkCnst("ActionFun"), actionFunList));
        }

        private string GetMonitorMachineName(string machineName)
        {
            return string.Format("{0}_handles", machineName);
        }

        private string GetFairChoice(TranslationContext translationContext, string entityName, int i)
        {
            if (translationContext == TranslationContext.Action)
                return string.Format("FairChoice_Action_{0}_{1}", entityName, i);
            else if (translationContext == TranslationContext.Entry)
                return string.Format("FairChoice_Entry_{0}_{1}", entityName, i);
            else if (translationContext == TranslationContext.Exit)
                return string.Format("FairChoice_Exit_{0}_{1}", entityName, i);
            else
                return string.Format("FairChoice_Function_{0}_{1}", entityName, i);
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
                var field = MkZingVarDecl(GetMonitorMachineName(machineName), Factory.Instance.MkCnst(machineName), ZingData.Cnst_Static);
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
            AST<Node> runMethod = MkZingMethodDecl("run", ZingData.Cnst_Nil, ZingData.Cnst_Void, ConstructList(ZingData.App_VarDecls, locals), runBody,
                ZingData.Cnst_Static, ZingData.Cnst_Activate);
            methods = AddArgs(ZingData.App_MethodDecls, runMethod, methods);

            // Generate method for computing complement of a set of events
            AST<Node> calculateComplementParameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("eventSet", SmEventSet), ZingData.Cnst_Nil);
            AST<Node> calculateComplementLocalVars = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("returnEventSet", SmEventSet), ZingData.Cnst_Nil);
            AST<Node> calculateComplementBody = MkZingAssign(MkZingIdentifier("returnEventSet"), AddArgs(ZingData.App_New, SmEventSet, ZingData.Cnst_Nil));
            foreach (var eventName in allEvents.Keys)
            {
                if (eventName == DefaultEvent || eventName == HaltEvent)
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
                Factory.Instance.MkCnst("PRT_TYPE"), ZingData.Cnst_Nil, MkZingBlock("dummy", MkZingSeq(payloadOfBody)), ZingData.Cnst_Static);
            methods = AddArgs(ZingData.App_MethodDecls, payloadOfMethod, methods);

            foreach (var field in typeContext.MainVarDecls())
            {
                fields = AddArgs(ZingData.App_VarDecls, field, fields);
            }

            return AddArgs(ZingData.App_ClassDecl, Factory.Instance.MkCnst("Main"), fields, methods);
        }

        private AST<FuncTerm> GenerateMonitorClass(string machineName)
        {
            AST<Node> fields = LocalVariablesToVarDecls(allMachines[machineName].localVariableToVarInfo.Keys, allMachines[machineName].localVariableToVarInfo);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("myHandle", SpecHandle), fields);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("localActions", Factory.Instance.MkCnst("LocalActions")), fields);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("next", Factory.Instance.MkCnst(machineName)), fields);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("startState", Factory.Instance.MkCnst("State")), fields);

            AST<Node> methods = ZingData.Cnst_Nil;
            foreach (var x in allMachines[machineName].stateNameToStateInfo)
            {
                AST<Node> dequeueEventMethod = GenerateCalculateActionSetMethodDeclForMonitor(x.Key, x.Value);
                methods = AddArgs(ZingData.App_MethodDecls, dequeueEventMethod, methods);
            }
            AST<Node> runHelperMethod = GenerateRunHelperMethodDeclForMonitor(machineName);
            methods = AddArgs(ZingData.App_MethodDecls, runHelperMethod, methods);
            AST<Node> actionHelperMethod = GenerateActionHelperMethodDeclForMonitor(machineName);
            methods = AddArgs(ZingData.App_MethodDecls, actionHelperMethod, methods);
            foreach (var funName in allMachines[machineName].funNameToFunInfo.Keys)
            {
                var funInfo = allMachines[machineName].funNameToFunInfo[funName];
                methods = AddArgs(ZingData.App_MethodDecls, MkZingFunMethod(funName, funInfo), methods);
            }

            foreach (var stateInfo in allMachines[machineName].stateNameToStateInfo)
            {
                var stateName = stateInfo.Key;
                var entryFun = (stateInfo.Value.entryAction as AnonymousActionFun).n;
                var exitFun = (stateInfo.Value.exitFun as AnonymousActionFun).n;
                methods = AddArgs(ZingData.App_MethodDecls, MkZingWrapperFn(entryFun, machineName, TranslationContext.Entry, stateName), methods);
                if (exitFun != null)
                    methods = AddArgs(ZingData.App_MethodDecls, MkZingWrapperFn(exitFun, machineName, TranslationContext.Exit, stateName), methods);
            }

            foreach (var actName in allMachines[machineName].actionNameToActionInfo.Keys)
            {
                var actInfo = allMachines[machineName].actionNameToActionInfo[actName];
                methods = AddArgs(ZingData.App_MethodDecls, MkZingWrapperFn(actInfo.actionFun, machineName, TranslationContext.Action, actName), methods);
            }

            return AddArgs(ZingData.App_ClassDecl, Factory.Instance.MkCnst(machineName), fields, methods);
        }

        private AST<FuncTerm> GenerateMachineClass(string machineName)
        {
            AST<Node> fields = LocalVariablesToVarDecls(allMachines[machineName].localVariableToVarInfo.Keys, allMachines[machineName].localVariableToVarInfo);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("myHandle", SmHandle), fields);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("stackStable", ZingData.Cnst_Bool), fields);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("stackDeferredSet", SmEventSet), fields);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("stackActionSet", SmEventSet), fields);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("localActions", Factory.Instance.MkCnst("LocalActions")), fields);

            AST<Node> methods = ZingData.Cnst_Nil;
            foreach (var x in allMachines[machineName].stateNameToStateInfo)
            {
                AST<Node> dequeueEventMethod = GenerateCalculateDeferredAndActionSetMethodDecl(x.Key, x.Value);
                methods = AddArgs(ZingData.App_MethodDecls, dequeueEventMethod, methods);
            }
            AST<Node> runMethod = GenerateRunMethodDecl(machineName);
            methods = AddArgs(ZingData.App_MethodDecls, runMethod, methods);
            AST<Node> runHelperMethod = GenerateRunHelperMethodDecl(machineName);
            methods = AddArgs(ZingData.App_MethodDecls, runHelperMethod, methods);
            AST<Node> actionHelperMethod = GenerateActionHelperMethodDecl(machineName);
            methods = AddArgs(ZingData.App_MethodDecls, actionHelperMethod, methods);
            foreach (var funName in allMachines[machineName].funNameToFunInfo.Keys)
            {
                var funInfo = allMachines[machineName].funNameToFunInfo[funName];
                methods = AddArgs(ZingData.App_MethodDecls, MkZingFunMethod(funName, funInfo), methods);
            }

            foreach (var stateInfo in allMachines[machineName].stateNameToStateInfo)
            {
                var stateName = stateInfo.Key;
                var entryFun = (stateInfo.Value.entryAction as AnonymousActionFun).n;
                var exitFun = (stateInfo.Value.exitFun as AnonymousActionFun).n;
                methods = AddArgs(ZingData.App_MethodDecls, MkZingWrapperFn(entryFun, machineName, TranslationContext.Entry, stateName), methods);
                if (exitFun != null)
                    methods = AddArgs(ZingData.App_MethodDecls, MkZingWrapperFn(exitFun, machineName, TranslationContext.Exit, stateName), methods);
            }

            foreach (var actName in allMachines[machineName].actionNameToActionInfo.Keys)
            {
                var actInfo = allMachines[machineName].actionNameToActionInfo[actName];
                methods = AddArgs(ZingData.App_MethodDecls, MkZingWrapperFn(actInfo.actionFun, machineName, TranslationContext.Action, actName), methods);
            }

            if (compiler.liveness == LivenessOption.Standard)
            {
                foreach (var stateName in allMachines[machineName].stateNameToStateInfo.Keys)
                {
                    var stateInfo = allMachines[machineName].stateNameToStateInfo[stateName];
                    for (int i = 0; i < stateInfo.numFairChoicesInEntry; i++)
                    {
                        fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl(GetFairChoice(TranslationContext.Entry, stateName, i), Factory.Instance.MkCnst("FairChoice")), fields);
                    }
                    for (int i = 0; i < stateInfo.numFairChoicesInExit; i++)
                    {
                        fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl(GetFairChoice(TranslationContext.Exit, stateName, i), Factory.Instance.MkCnst("FairChoice")), fields);
                    }
                }
                foreach (var actionName in allMachines[machineName].actionNameToActionInfo.Keys)
                {
                    var actionInfo = allMachines[machineName].actionNameToActionInfo[actionName];
                    for (int i = 0; i < actionInfo.numFairChoices; i++)
                    {
                        fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl(GetFairChoice(TranslationContext.Action, actionName, i), Factory.Instance.MkCnst("FairChoice")), fields);
                    }
                }
                foreach (var funName in allMachines[machineName].funNameToFunInfo.Keys)
                {
                    var funInfo = allMachines[machineName].funNameToFunInfo[funName];
                    for (int i = 0; i < funInfo.numFairChoices; i++)
                    {
                        fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl(GetFairChoice(TranslationContext.Function, funName, i), Factory.Instance.MkCnst("FairChoice")), fields);
                    }
                }
            }

            return AddArgs(ZingData.App_ClassDecl, Factory.Instance.MkCnst(machineName), fields, methods);
        }

        private void MkZingClasses(List<AST<Node>> elements)
        {
            foreach (string machineName in allMachines.Keys)
            {
                if (allMachines[machineName].IsMonitor)
                {
                    elements.Add(GenerateMonitorClass(machineName));
                }
                else
                {
                    elements.Add(GenerateMachineClass(machineName));
                }
            }

            elements.Add(GenerateMainClass());
        }

        private AST<Node> MkZingNeq(ZingEntryFun_FoldContext ctxt, AST<Node> e1, AST<Node> t1, AST<Node> e2, AST<Node> t2)
        {
            return MkZingApply(ZingData.Cnst_Not, MkZingEq(ctxt, e1, t1, e2, t2));
        }

        private AST<Node> MkZingEq(ZingEntryFun_FoldContext ctxt, AST<Node> e1, AST<Node> t1, AST<Node> e2, AST<Node> t2)
        {
            var tmpEqVar = ctxt.getTmpVar(ZingData.Cnst_Bool, "eqVar");
            ctxt.addSideEffect(MkZingAssign(tmpEqVar, MkZingCall(PrtIsEqualValue, e1, e2)));
            return tmpEqVar;
        }

        private void AddEventSet(List<AST<FuncTerm>> stmts, IEnumerable<string> eventNames, AST<FuncTerm> set)
        {
            foreach (var eventName in eventNames)
            {
                var addExpr = MkZingApply(ZingData.Cnst_Add, set, MkZingEvent(eventName));
                var addStmt = AddArgs(ZingData.App_Assign, set, addExpr);
                stmts.Add(addStmt);
            }
        }

        private void SubtractEventSet(List<AST<FuncTerm>> stmts, IEnumerable<string> eventNames, AST<FuncTerm> actionSet)
        {
            foreach (var eventName in eventNames)
            {
                var subExpr = MkZingApply(ZingData.Cnst_Sub, actionSet, MkZingEvent(eventName));
                var subStmt = AddArgs(ZingData.App_Assign, actionSet, subExpr);
                stmts.Add(subStmt);
            }
        }

        private AST<Node> GenerateCalculateDeferredAndActionSetMethodDecl(string stateName, StateInfo stateInfo)
        {
            AST<Node> parameters = ZingData.Cnst_Nil;
            parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("currentActionSet", SmEventSet), parameters);
            parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("currentDeferredSet", SmEventSet), parameters);

            List<AST<FuncTerm>> stmts = new List<AST<FuncTerm>>();
            var smEventSetType = Factory.Instance.MkCnst("SM_EVENT_SET");
            var stackDeferredSet = MkZingIdentifier("stackDeferredSet");
            var stackActionSet = MkZingIdentifier("stackActionSet");
            var currentDeferredSet = MkZingIdentifier("currentDeferredSet");
            var currentActionSet = MkZingIdentifier("currentActionSet");

            var ownerName = stateInfo.ownerName;
            var localDeferredSet = stateInfo.deferredEvents;
            var actions = stateInfo.actions;
            var transitions = stateInfo.transitions;

            stmts.Add(MkZingAssign(currentDeferredSet, MkZingApply(ZingData.Cnst_Add, currentDeferredSet, stackDeferredSet)));
            AddEventSet(stmts, localDeferredSet, currentDeferredSet);
            SubtractEventSet(stmts, actions.Keys, currentDeferredSet);
            SubtractEventSet(stmts, transitions.Keys, currentDeferredSet);

            stmts.Add(MkZingAssign(currentActionSet, MkZingApply(ZingData.Cnst_Add, currentActionSet, stackActionSet)));
            SubtractEventSet(stmts, localDeferredSet, currentActionSet);
            AddEventSet(stmts, actions.Keys, currentActionSet);
            SubtractEventSet(stmts, transitions.Keys, currentActionSet);

            stmts.Add(MkZingAssign(MkZingDot("localActions", "es"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("SM_EVENT_array"), Factory.Instance.MkCnst(actions.Count))));
            stmts.Add(MkZingAssign(MkZingDot("localActions", "as"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("ActionFun_array"), Factory.Instance.MkCnst(actions.Count))));
            int count = 0;
            foreach (var eventName in actions.Keys)
            {
                stmts.Add(MkZingAssign(MkZingApply(ZingData.Cnst_Index, MkZingDot("localActions", "es"), Factory.Instance.MkCnst(count)), MkZingEvent(eventName)));
                stmts.Add(MkZingAssign(MkZingApply(ZingData.Cnst_Index, MkZingDot("localActions", "as"), Factory.Instance.MkCnst(count)), MkZingAction(ownerName, (actions[eventName] as NamedActionFun).s)));
                count = count + 1;
            }

            var body = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("dummy"), MkZingSeq(stmts.ToArray()));
            return MkZingMethodDecl(string.Format("{0}_CalculateDeferredAndActionSet", stateName), parameters, ZingData.Cnst_Void, ZingData.Cnst_Nil, ConstructList(ZingData.App_Blocks, body));
        }

        private AST<Node> GenerateCalculateActionSetMethodDeclForMonitor(string stateName, StateInfo stateInfo)
        {
            AST<Node> parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("currentActionSet", SmEventSet), ZingData.Cnst_Nil);

            List<AST<FuncTerm>> stmts = new List<AST<FuncTerm>>();
            var smEventSetType = Factory.Instance.MkCnst("SM_EVENT_SET");
            var currentActionSet = MkZingIdentifier("currentActionSet");

            var ownerName = stateInfo.ownerName;
            var actions = stateInfo.actions;
            var transitions = stateInfo.transitions;

            AddEventSet(stmts, actions.Keys, currentActionSet);
            SubtractEventSet(stmts, transitions.Keys, currentActionSet);

            stmts.Add(MkZingAssign(MkZingDot("localActions", "es"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("SM_EVENT_array"), Factory.Instance.MkCnst(actions.Count))));
            stmts.Add(MkZingAssign(MkZingDot("localActions", "as"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("ActionFun_array"), Factory.Instance.MkCnst(actions.Count))));
            int count = 0;
            foreach (var eventName in actions.Keys)
            {
                stmts.Add(MkZingAssign(MkZingApply(ZingData.Cnst_Index, MkZingDot("localActions", "es"), Factory.Instance.MkCnst(count)), MkZingEvent(eventName)));
                stmts.Add(MkZingAssign(MkZingApply(ZingData.Cnst_Index, MkZingDot("localActions", "as"), Factory.Instance.MkCnst(count)), MkZingAction(ownerName, (actions[eventName] as NamedActionFun).s)));
                count = count + 1;
            }

            var body = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("dummy"), MkZingSeq(stmts.ToArray()));
            return MkZingMethodDecl(string.Format("{0}_CalculateActionSet", stateName), parameters, ZingData.Cnst_Void, ZingData.Cnst_Nil, ConstructList(ZingData.App_Blocks, body));
        }

        private AST<Node> GenerateRunMethodDecl(string machineName)
        {
            AST<Node> locals =
                ConstructList(
                    ZingData.App_VarDecls,
                    MkZingVarDecl("currentDeferredSet", SmEventSet)
                    );

            string initStateName = allMachines[machineName].initStateName;
            var callStmt = MkZingCallStmt(
                            MkZingCall(MkZingIdentifier("runHelper"),
                                       MkZingDot("State", string.Format("_{0}", initStateName))));

            var currentDeferredSet = MkZingIdentifier("currentDeferredSet");
            var smEventSetType = Factory.Instance.MkCnst("SM_EVENT_SET");
            var iteStmt = MkZingIfThenElse(MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingEvent("halt")),
                                    MkZingSeq(MkZingAssign(currentDeferredSet, MkZingIdentifier("null")),
                                    MkZingAssign(MkZingDot("myHandle", "buffer"), MkZingIdentifier("null")),
                                    MkZingAssign(MkZingDot("myHandle", "isDeleted"), ZingData.Cnst_True),
                                    MkZingAssign(MkZingIdentifier("localActions"), MkZingIdentifier("null")),
                                    MkZingAssign(MkZingIdentifier("stackActionSet"), MkZingIdentifier("null")),
                                    MkZingAssign(MkZingIdentifier("stackDeferredSet"), MkZingIdentifier("null")),
                                    MkZingReturn(ZingData.Cnst_Nil)),
                                    ZingData.Cnst_Nil);


            string traceString = string.Format("\"<StateLog> Unhandled event exception by machine {0}-{{0}}\"", machineName);
            var traceStmt = MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(traceString), MkZingDot("myHandle", "instance")));
            var assertStmt = Factory.Instance.AddArg(ZingData.App_Assert, ZingData.Cnst_False);

            var body = ConstructList(
                            ZingData.App_Blocks,
                            AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("dummy"), MkZingSeq(callStmt, iteStmt, traceStmt, assertStmt))
                            );
            return MkZingMethodDecl("run", ZingData.Cnst_Nil, ZingData.Cnst_Void, locals, body);
        }

        private AST<Node> GenerateActionHelperMethodDeclForMonitor(string machineName)
        {
            AST<Node> parameters = ConstructList(ZingData.App_VarDecls,
                                                 MkZingVarDecl("actionFun", Factory.Instance.MkCnst("ActionFun")),
                                                 MkZingVarDecl("currentActionSet", SmEventSet)
                                                 );
            List<AST<Node>> locals = new List<AST<Node>>();
            locals.Add(MkZingVarDecl("cont", Factory.Instance.MkCnst("Continuation")));

            var cont = MkZingIdentifier("cont");
            AST<Node> initStmt = Factory.Instance.AddArg(ZingData.App_Assert, ZingData.Cnst_False);
            foreach (var actionFunName in allMachines[machineName].actionNameToActionInfo.Keys)
            {
                var actionExpr = MkZingAction(machineName, actionFunName);
                var condExpr = MkZingApply(ZingData.Cnst_Eq, MkZingIdentifier("actionFun"), actionExpr);
                var gotoStmt = Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst("execute_" + actionFunName));
                initStmt = MkZingIfThenElse(condExpr, gotoStmt, initStmt);
            }
            initStmt = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("init"), initStmt);

            // Action blocks
            List<AST<Node>> blocks = new List<AST<Node>>();
            blocks.Add(initStmt);
            foreach (var actionFunName in allMachines[machineName].actionNameToActionInfo.Keys)
            {
                AST<Cnst> executeLabel = Factory.Instance.MkCnst("execute_" + actionFunName);
                string traceString = string.Format("\"<ActionLog> Machine {0}-{{0}} executing Action {1}\"", machineName, actionFunName);
                var executeStmt = MkZingSeq(
                                    MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(traceString), MkZingDot("myHandle", "instance"))),
                                    MkZingAssign(cont, MkZingCall(MkZingDot(Factory.Instance.MkCnst("Continuation"), "Construct_Default"))),
                                    MkZingCallStmt(MkZingCall(MkZingDot(cont, "PushReturnTo"), Factory.Instance.MkCnst(0))),
                                    MkZingAssign(cont, MkZingCall(MkZingIdentifier(getZingWrapperFunName(actionFunName, TranslationContext.Action)), cont)),
                                    MkZingReturn(ZingData.Cnst_Nil)
                                    );
                executeStmt = AddArgs(ZingData.App_LabelStmt, executeLabel, executeStmt);
                blocks.Add(executeStmt);
            }

            AST<Node> body = ConstructList(ZingData.App_Blocks, blocks);
            return MkZingMethodDecl("actionHelper", parameters, ZingData.Cnst_Void, MkZingVarDecls(locals), body);
        }

        private AST<Node> GenerateActionHelperMethodDecl(string machineName)
        {
            AST<Node> parameters = ConstructList(ZingData.App_VarDecls,
                                                 MkZingVarDecl("actionFun", Factory.Instance.MkCnst("ActionFun")),
                                                 MkZingVarDecl("currentActionSet", SmEventSet),
                                                 MkZingVarDecl("currentStable", ZingData.Cnst_Bool)
                                                 );

            List<AST<Node>> locals = new List<AST<Node>>();

            locals.Add(MkZingVarDecl("savedStable", ZingData.Cnst_Bool));
            locals.Add(MkZingVarDecl("savedDeferredSet", SmEventSet));
            locals.Add(MkZingVarDecl("savedActionSet", SmEventSet));
            locals.Add(MkZingVarDecl("savedCurrentEvent", SmEvent));
            locals.Add(MkZingVarDecl("savedCurrentArg", PrtValue));
            locals.Add(MkZingVarDecl("cont", Factory.Instance.MkCnst("Continuation")));
            if (compiler.liveness == LivenessOption.Standard)
            {
                locals.Add(MkZingVarDecl("gateProgress", ZingData.Cnst_Bool));
            }

            AST<Node> initStmt = Factory.Instance.AddArg(ZingData.App_Assert, ZingData.Cnst_False);
            foreach (var actionFunName in allMachines[machineName].actionNameToActionInfo.Keys)
            {
                var actionExpr = MkZingAction(machineName, actionFunName);
                var condExpr = MkZingApply(ZingData.Cnst_Eq, MkZingIdentifier("actionFun"), actionExpr);
                var gotoStmt = Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst("execute_" + actionFunName));
                initStmt = MkZingIfThenElse(condExpr, gotoStmt, initStmt);
            }
            initStmt = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("init"), initStmt);

            // Action blocks
            List<AST<Node>> blocks = new List<AST<Node>>();
            blocks.Add(initStmt);
            foreach (var actionFunName in allMachines[machineName].actionNameToActionInfo.Keys)
            {
                AST<Cnst> executeLabel = Factory.Instance.MkCnst("execute_" + actionFunName);
                string traceString = string.Format("\"<ActionLog> Machine {0}-{{0}} executing Action {1}\"", machineName, actionFunName);
                var executeStmt = MkZingSeq(
                                    MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(traceString), MkZingDot("myHandle", "instance"))),
                                    MkZingInvokeWrapperFun(machineName, actionFunName, TranslationContext.Action));
                executeStmt = AddArgs(ZingData.App_LabelStmt, executeLabel, executeStmt);
                blocks.Add(executeStmt);
            }
            AST<Node> body = ConstructList(ZingData.App_Blocks, blocks);
            return MkZingMethodDecl("actionHelper", parameters, ZingData.Cnst_Bool, MkZingVarDecls(locals), body);
        }

        private AST<Node> MkZingInvokeWrapperFun(string machineName, string entityName, TranslationContext type)
        {
            string name = getZingWrapperFunName(entityName, type);
            var currentDeferredSet = MkZingIdentifier("currentDeferredSet");
            var currentActionSet = MkZingIdentifier("currentActionSet");
            var savedDeferredSet = MkZingIdentifier("savedDeferredSet");
            var savedActionSet = MkZingIdentifier("savedActionSet");
            var stackDeferredSet = MkZingIdentifier("stackDeferredSet");
            var stackActionSet = MkZingIdentifier("stackActionSet");
            var currentEvent = MkZingDot("myHandle", "currentEvent");
            var currentArg = MkZingDot("myHandle", "currentArg");
            var savedCurrentEvent = MkZingIdentifier("savedCurrentEvent");
            var savedCurrentArg = MkZingIdentifier("savedCurrentArg");
            var cont = MkZingIdentifier("cont");
            var currentStable = MkZingIdentifier("currentStable");
            var savedStable = MkZingIdentifier("savedStable");
            var stackStable = MkZingIdentifier("stackStable");

            string traceString = type == TranslationContext.Action
                                    ? string.Format("\"<ReEntryLog> Machine {0}-{{0}} reentered Action {1}\"", machineName, entityName)
                                    : string.Format("\"<ReEntryLog> Machine {0}-{{0}} reentered State {1}\"", machineName, entityName);

            var restoreCurrentEvent = MkZingAssign(currentEvent, savedCurrentEvent);
            var restoreCurrentArg = MkZingAssign(currentArg, savedCurrentArg);
            var gotoStmt = AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst("transition_" + entityName));
            string errorTraceString = string.Format("\"<StateLog> Call statement terminated due to unhandled event by machine {0}-{{0}}\"", machineName);
            var errorTraceStmt = MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(errorTraceString), MkZingDot("myHandle", "instance")));

            var body = new List<AST<Node>>();
            body.Add(MkZingAssign(cont, MkZingCall(MkZingDot(Factory.Instance.MkCnst("Continuation"), "Construct_Default"))));
            body.Add(MkZingCallStmt(MkZingCall(MkZingDot(cont, "PushReturnTo"), Factory.Instance.MkCnst(0))));
            body.Add(AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("reentry_" + name), MkZingAssign(cont,
                MkZingCall(MkZingIdentifier(name), cont))));
            body.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Return")),
                MkZingSeq(
                type == TranslationContext.Action ? MkZingReturn(ZingData.Cnst_False) : type == TranslationContext.Exit ? (AST<Node>)ZingData.Cnst_Nil : AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst("wait_" + entityName)))));
            body.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Pop")),
                MkZingSeq(
                type == TranslationContext.Action ? MkZingReturn(ZingData.Cnst_True) : MkZingReturn(ZingData.Cnst_Nil))));
            body.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Raise")),
                    type == TranslationContext.Action ? AddArgs(ZingData.App_Return, ZingData.Cnst_False) : type == TranslationContext.Exit ? MkZingAssert(ZingData.Cnst_False) : AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst("transition_" + entityName))));
            body.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Call")),
                MkZingSeq(
                    MkZingAssign(savedDeferredSet, stackDeferredSet),
                    MkZingAssign(savedActionSet, stackActionSet),
                    MkZingAssign(savedCurrentEvent, currentEvent),
                    MkZingAssign(savedCurrentArg, currentArg),
                    MkZingAssign(savedStable, stackStable),
                    MkZingAssign(stackStable, currentStable),
                    MkZingAssign(stackActionSet, currentActionSet),
                    MkZingAssign(stackDeferredSet, MkZingCall(MkZingDot("Main", "CalculateComplementOfEventSet"), currentActionSet)),
                    MkZingAssign(currentEvent, MkZingIdentifier("null")),
                    MkZingAssign(currentArg, MkZingIdentifier("null")),
                    MkZingAssign(MkZingIdentifier("localActions"), MkZingCall(MkZingDot("LocalActions", "Construct"), MkZingIdentifier("localActions"))),
                    MkZingCallStmt(MkZingCall(MkZingIdentifier("runHelper"), MkZingDot("cont", "state"))),
                    MkZingAssign(MkZingIdentifier("localActions"), MkZingDot("localActions", "next")),
                    MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(traceString), MkZingDot("myHandle", "instance"))),
                    MkZingAssign(stackDeferredSet, savedDeferredSet), 
                    MkZingAssign(stackActionSet, savedActionSet),
                    MkZingAssign(stackStable, savedStable),
                    MkZingAssign(MkZingDot("cont", "state"), MkZingDot("State", "_default")),
                    MkZingIfThenElse(MkZingApply(ZingData.Cnst_Eq, currentEvent, MkZingIdentifier("null")),
                              MkZingSeq(restoreCurrentEvent, restoreCurrentArg),
                              MkZingSeq(errorTraceStmt, MkZingAssert(ZingData.Cnst_False))),
                    AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst("reentry_" + name)))));
            AST<Node> atChooseLivenessStmt = ZingData.Cnst_Nil;
            AST<Node> atYieldLivenessStmt = ZingData.Cnst_Nil;
            if (compiler.liveness == LivenessOption.Standard)
            {
                AST<Node> thenStmt = MkZingSeq(
                                MkZingAssign(MkZingIdentifier("gateProgress"), MkZingCall(Factory.Instance.MkCnst("choose"), Factory.Instance.MkCnst("bool"))),
                                MkZingIfThen(MkZingIdentifier("gateProgress"), MkZingAssign(MkZingDot("FairCycle", "gate"), MkZingDot("GateStatus", "Closed"))));
                AST<Node> infinitelyOftenNondetStmt = MkZingIfThen(MkZingApply(ZingData.Cnst_Eq, MkZingDot("FairCycle", "gate"), MkZingDot("GateStatus", "Selected")), thenStmt);
                atChooseLivenessStmt = MkZingSeq(
                    infinitelyOftenNondetStmt,
                    MkZingCallStmt(MkZingCall(MkZingDot("FairScheduler", "AtChooseStatic"))),
                    MkZingCallStmt(MkZingCall(MkZingDot("FairChoice", "AtYieldOrChooseStatic"))));
                atYieldLivenessStmt = MkZingSeq(
                    infinitelyOftenNondetStmt,
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
            AST<Node> parameters = ConstructList(ZingData.App_VarDecls,
                                                 MkZingVarDecl("startState", Factory.Instance.MkCnst("State")));

            List<AST<Node>> locals = new List<AST<Node>>();

            locals.Add(MkZingVarDecl("currentDeferredSet", SmEventSet));
            locals.Add(MkZingVarDecl("currentActionSet", SmEventSet));
            locals.Add(MkZingVarDecl("savedDeferredSet", SmEventSet));
            locals.Add(MkZingVarDecl("savedActionSet", SmEventSet));
            locals.Add(MkZingVarDecl("actionFun", Factory.Instance.MkCnst("ActionFun")));
            locals.Add(MkZingVarDecl("cont", Factory.Instance.MkCnst("Continuation")));
            locals.Add(MkZingVarDecl("savedCurrentEvent", SmEvent));
            locals.Add(MkZingVarDecl("savedCurrentArg", PrtValue));
            locals.Add(MkZingVarDecl("didActionPop", ZingData.Cnst_Bool));
            locals.Add(MkZingVarDecl("currentStable", ZingData.Cnst_Bool));
            locals.Add(MkZingVarDecl("savedStable", ZingData.Cnst_Bool));
            if (compiler.liveness == LivenessOption.Standard)
            {
                locals.Add(MkZingVarDecl("gateProgress", ZingData.Cnst_Bool));
            }

            // Initial block
            AST<Node> initStmt = Factory.Instance.AddArg(ZingData.App_Assert, ZingData.Cnst_False);
            foreach (var stateName in allMachines[machineName].stateNameToStateInfo.Keys)
            {
                var stateExpr = MkZingDot("State", string.Format("_{0}", stateName));
                var condExpr = MkZingApply(ZingData.Cnst_Eq, MkZingIdentifier("startState"), stateExpr);
                var gotoStmt = Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst("execute_" + stateName));
                initStmt = MkZingIfThenElse(condExpr, gotoStmt, initStmt);
            }
            initStmt = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("init"), initStmt);

            var currentDeferredSet = MkZingIdentifier("currentDeferredSet");
            var currentActionSet = MkZingIdentifier("currentActionSet");
            var savedDeferredSet = MkZingIdentifier("savedDeferredSet");
            var savedActionSet = MkZingIdentifier("savedActionSet");
            var stackDeferredSet = MkZingIdentifier("stackDeferredSet");
            var stackActionSet = MkZingIdentifier("stackActionSet");
            var currentStable = MkZingIdentifier("currentStable");
            var stackStable = MkZingIdentifier("stackStable");
            var savedStable = MkZingIdentifier("savedStable");

            // State blocks
            List<AST<Node>> blocks = new List<AST<Node>>();
            blocks.Add(initStmt);
            var smEventSetType = Factory.Instance.MkCnst("SM_EVENT_SET");
            foreach (var stateName in allMachines[machineName].stateNameToStateInfo.Keys)
            {
                StateInfo stateInfo = allMachines[machineName].stateNameToStateInfo[stateName];
                AST<Cnst> executeLabel = Factory.Instance.MkCnst("execute_" + stateName);
                AST<Cnst> waitLabel = Factory.Instance.MkCnst("wait_" + stateName);
                AST<Cnst> transitionLabel = Factory.Instance.MkCnst("transition_" + stateName);
                string traceString = string.Format("\"<StateLog> Machine {0}-{{0}} entered State {1}\"", machineName, stateName);
                var executeStmt = MkZingSeq(
                                    MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(traceString), MkZingDot("myHandle", "instance"))),
                                    MkZingCallStmt(MkZingCall(MkZingIdentifier("invokeplugin"), Factory.Instance.MkCnst("\"StateCoveragePlugin.dll\""), Factory.Instance.MkCnst(string.Format("\"{0}\"", machineName)), Factory.Instance.MkCnst(string.Format("\"{0}\"", stateName)))),
                                    MkZingAssign(currentStable, MkZingApply(ZingData.Cnst_Or, stackStable, stateInfo.isStable ? ZingData.Cnst_True : ZingData.Cnst_False)),
                                    MkZingAssign(currentDeferredSet, AddArgs(ZingData.App_New, smEventSetType, ZingData.Cnst_Nil)),
                                    MkZingAssign(currentActionSet, AddArgs(ZingData.App_New, smEventSetType, ZingData.Cnst_Nil)),
                                    MkZingCallStmt(MkZingCall(MkZingIdentifier(string.Format("{0}_CalculateDeferredAndActionSet", stateName)), currentDeferredSet, currentActionSet)),
                                    MkZingInvokeWrapperFun(machineName, stateName, TranslationContext.Entry),
                                    MkZingAssert(ZingData.Cnst_False)
                                    );
                executeStmt = AddArgs(ZingData.App_LabelStmt, executeLabel, executeStmt);
                blocks.Add(executeStmt);
                var waitStmt = MkZingSeq(
                    MkZingCallStmt(MkZingCall(MkZingDot("myHandle", "DequeueEvent"), allMachines[machineName].stateNameToStateInfo[stateName].hasDefaultTransition ? ZingData.Cnst_True : ZingData.Cnst_False, currentDeferredSet, currentStable)),
                                    Factory.Instance.AddArg(ZingData.App_Goto, transitionLabel));
                waitStmt = AddArgs(ZingData.App_LabelStmt, waitLabel, waitStmt);
                blocks.Add(waitStmt);

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
                    MkZingIfThenElse(
                            MkZingApply(ZingData.Cnst_In, MkZingDot("myHandle", "currentEvent"), currentActionSet),
                            MkZingSeq(MkZingAssign(MkZingIdentifier("actionFun"), MkZingCall(MkZingDot("localActions", "Find"), MkZingDot("myHandle", "currentEvent"))),
                                      MkZingAssign(MkZingIdentifier("didActionPop"), MkZingCall(MkZingIdentifier("actionHelper"), MkZingIdentifier("actionFun"), currentActionSet, currentStable)),
                                      MkZingIfThenElse(MkZingIdentifier("didActionPop"), Factory.Instance.AddArg(ZingData.App_Return, ZingData.Cnst_Nil), ZingData.Cnst_Nil),
                                      MkZingIfThenElse(
                                              MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingIdentifier("null")),
                                              Factory.Instance.AddArg(ZingData.App_Goto, waitLabel),
                                              Factory.Instance.AddArg(ZingData.App_Goto, transitionLabel))),
                            ZingData.Cnst_Nil);

                AST<Node> callTransitionStmt = ZingData.Cnst_Nil;
                foreach (var eventName in callTransitions.Keys)
                {
                    var targetStateName = callTransitions[eventName].target;
                    var condExpr = MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingEvent(eventName));
                    var save = MkZingSeq(MkZingAssign(savedDeferredSet, stackDeferredSet), MkZingAssign(savedActionSet, stackActionSet), MkZingAssign(savedStable, stackStable));
                    var update = MkZingSeq(MkZingAssign(stackDeferredSet, currentDeferredSet), MkZingAssign(stackActionSet, currentActionSet), MkZingAssign(stackStable, currentStable));
                    var push = MkZingAssign(MkZingIdentifier("localActions"), MkZingCall(MkZingDot("LocalActions", "Construct"), MkZingIdentifier("localActions")));
                    var callStmt = MkZingCallStmt(
                        MkZingCall(MkZingIdentifier("runHelper"),
                                   MkZingDot("State", string.Format("_{0}", targetStateName))
                                   ));
                    var pop = MkZingAssign(MkZingIdentifier("localActions"), MkZingDot("localActions", "next"));
                    var restore = MkZingSeq(MkZingAssign(stackDeferredSet, savedDeferredSet), MkZingAssign(stackActionSet, savedActionSet), MkZingAssign(stackStable, savedStable));
                    var ite = MkZingIfThenElse(
                        MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingIdentifier("null")),
                        Factory.Instance.AddArg(ZingData.App_Goto, waitLabel),
                        Factory.Instance.AddArg(ZingData.App_Goto, transitionLabel)
                        );
                    callTransitionStmt = MkZingIfThenElse(condExpr, MkZingSeq(save, update, push, callStmt, pop, restore, ite), callTransitionStmt);
                }

                AST<Node> exitFunction = allMachines[machineName].stateNameToStateInfo[stateName].exitFun != null ?
                    MkZingInvokeWrapperFun(machineName, stateName, TranslationContext.Exit) :
                    ZingData.Cnst_Nil;


                AST<Node> ordinaryTransitionStmt = Factory.Instance.AddArg(ZingData.App_Return, ZingData.Cnst_Nil);
                foreach (var eventName in ordinaryTransitions.Keys)
                {
                    var targetStateName = ordinaryTransitions[eventName].target;
                    var condExpr = MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingEvent(eventName));
                    AST<Node> jumpStmt = Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst("execute_" + targetStateName));
                    ordinaryTransitionStmt = MkZingIfThenElse(condExpr, jumpStmt, ordinaryTransitionStmt);
                }

                blocks.Add(AddArgs(ZingData.App_LabelStmt, transitionLabel, MkZingSeq(actionStmt, callTransitionStmt, exitFunction, ordinaryTransitionStmt)));
            }
            AST<Node> body = ConstructList(ZingData.App_Blocks, blocks);
            return MkZingMethodDecl("runHelper", parameters, ZingData.Cnst_Void, MkZingVarDecls(locals), body);
        }

        private AST<Node> GenerateRunHelperMethodDeclForMonitor(string machineName)
        {
            List<AST<Node>> locals = new List<AST<Node>>();
            locals.Add(MkZingVarDecl("currentActionSet", SmEventSet));
            locals.Add(MkZingVarDecl("actionFun", Factory.Instance.MkCnst("ActionFun")));
            locals.Add(MkZingVarDecl("cont", Factory.Instance.MkCnst("Continuation")));
            locals.Add(MkZingVarDecl("gateProgress", ZingData.Cnst_Bool));

            var cont = MkZingIdentifier("cont");

            // Initial block
            AST<Node> initStmt = Factory.Instance.AddArg(ZingData.App_Assert, ZingData.Cnst_False);
            foreach (var stateName in allMachines[machineName].stateNameToStateInfo.Keys)
            {
                var stateExpr = MkZingDot("State", string.Format("_{0}", stateName));
                var condExpr = MkZingApply(ZingData.Cnst_Eq, MkZingIdentifier("startState"), stateExpr);
                var gotoStmt = Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst("transition_" + stateName));
                initStmt = MkZingIfThenElse(condExpr, gotoStmt, initStmt);
            }
            string initStateName = allMachines[machineName].initStateName;
            initStmt = MkZingIfThenElse(MkZingApply(ZingData.Cnst_Eq, MkZingIdentifier("startState"), MkZingDot("State", "_default")),
                                        MkZingSeq(MkZingAssign(MkZingIdentifier("startState"), MkZingDot("State", string.Format("_{0}", initStateName))),
                                                  Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst("execute_" + initStateName))),
                                        initStmt);
            initStmt = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("init"), initStmt);

            var currentActionSet = MkZingIdentifier("currentActionSet");

            // State blocks
            List<AST<Node>> blocks = new List<AST<Node>>();
            blocks.Add(initStmt);
            var smEventSetType = Factory.Instance.MkCnst("SM_EVENT_SET");
            foreach (var stateName in allMachines[machineName].stateNameToStateInfo.Keys)
            {
                StateInfo stateInfo = allMachines[machineName].stateNameToStateInfo[stateName];
                AST<Cnst> executeLabel = Factory.Instance.MkCnst("execute_" + stateName);
                AST<Cnst> waitLabel = Factory.Instance.MkCnst("wait_" + stateName);
                AST<Cnst> transitionLabel = Factory.Instance.MkCnst("transition_" + stateName);
                string traceString = string.Format("\"<StateLog> Machine {0}-{{0}} entered State {1}\"", machineName, stateName);
                List<AST<Node>> executeStmts = new List<AST<Node>>();
                if (compiler.liveness == LivenessOption.Standard)
                {
                    if (allMachines[machineName].stateNameToStateInfo[stateName].isStable)
                    {
                        executeStmts.Add(MkZingAssume(MkZingApply(ZingData.Cnst_NEq, MkZingDot("FairCycle", "gate"), MkZingDot("GateStatus", "Closed"))));
                        executeStmts.Add(MkZingAssign(MkZingDot("FairCycle", "gate"), MkZingDot("GateStatus", "InStable")));
                    }
                    else
                    {
                        executeStmts.Add(MkZingIfThen(MkZingApply(ZingData.Cnst_NEq, MkZingDot("FairCycle", "gate"), MkZingDot("GateStatus", "Closed")),
                                                   MkZingAssign(MkZingDot("FairCycle", "gate"), MkZingDot("GateStatus", "Selected"))));
                    }
                }
                else if (compiler.liveness == LivenessOption.Mace)
                {
                    if (allMachines[machineName].stateNameToStateInfo[stateName].isStable)
                    {
                        executeStmts.Add(MkZingCallStmt(MkZingCall(MkZingIdentifier("accept"), ZingData.Cnst_True)));
                    }
                    else
                    {
                        executeStmts.Add(MkZingCallStmt(MkZingCall(MkZingIdentifier("accept"), ZingData.Cnst_False)));
                    }
                }
                executeStmts.Add(MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(traceString), MkZingDot("myHandle", "instance"))));
                executeStmts.Add(MkZingCallStmt(MkZingCall(MkZingIdentifier("invokeplugin"), Factory.Instance.MkCnst("\"StateCoveragePlugin.dll\""), Factory.Instance.MkCnst(string.Format("\"{0}\"", machineName)), Factory.Instance.MkCnst(string.Format("\"{0}\"", stateName)))));
                executeStmts.Add(MkZingAssign(cont, MkZingCall(MkZingDot(Factory.Instance.MkCnst("Continuation"), "Construct_Default"))));
                executeStmts.Add(MkZingCallStmt(MkZingCall(MkZingDot(cont, "PushReturnTo"), Factory.Instance.MkCnst(0))));
                executeStmts.Add(MkZingAssign(cont, MkZingCall(MkZingIdentifier(getZingWrapperFunName(stateName, TranslationContext.Entry)), cont)));
                executeStmts.Add(MkZingIfThenElse(
                          MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingIdentifier("null")),
                          Factory.Instance.AddArg(ZingData.App_Goto, waitLabel),
                          Factory.Instance.AddArg(ZingData.App_Goto, transitionLabel)));
                blocks.Add(AddArgs(ZingData.App_LabelStmt, executeLabel, MkZingSeq(executeStmts)));
                var waitStmt = MkZingReturn(ZingData.Cnst_Nil);
                waitStmt = AddArgs(ZingData.App_LabelStmt, waitLabel, waitStmt);
                blocks.Add(waitStmt);

                var actionStmt =
                    MkZingIfThenElse(
                            MkZingApply(ZingData.Cnst_In, MkZingDot("myHandle", "currentEvent"), currentActionSet),
                            MkZingSeq(MkZingAssign(MkZingIdentifier("actionFun"), MkZingCall(MkZingDot("localActions", "Find"), MkZingDot("myHandle", "currentEvent"))),
                                      MkZingCallStmt(MkZingCall(MkZingIdentifier("actionHelper"), MkZingIdentifier("actionFun"), currentActionSet)),
                                      MkZingIfThenElse(
                                              MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingIdentifier("null")),
                                              Factory.Instance.AddArg(ZingData.App_Goto, waitLabel),
                                              Factory.Instance.AddArg(ZingData.App_Goto, transitionLabel))),
                            ZingData.Cnst_Nil);

                AST<Node> exitFunction = allMachines[machineName].stateNameToStateInfo[stateName].exitFun != null ?
                    (AST<Node>)MkZingSeq(MkZingAssign(cont, MkZingCall(MkZingDot(Factory.Instance.MkCnst("Continuation"), "Construct_Default"))),
                                          MkZingCallStmt(MkZingCall(MkZingDot(cont, "PushReturnTo"), Factory.Instance.MkCnst(0))),
                                          MkZingAssign(cont, MkZingCall(MkZingIdentifier(getZingWrapperFunName(stateName, TranslationContext.Exit)), cont))) :
                    (AST<Node>)ZingData.Cnst_Nil;

                AST<Node> ordinaryTransitionStmt = MkZingAssert(ZingData.Cnst_False);
                var transitions = allMachines[machineName].stateNameToStateInfo[stateName].transitions;
                foreach (var eventName in transitions.Keys)
                {
                    var targetStateName = transitions[eventName].target;
                    var condExpr = MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingEvent(eventName));
                    List<AST<Node>> jumpStmts = new List<AST<Node>>();
                    jumpStmts.Add(MkZingAssign(MkZingIdentifier("startState"), MkZingDot("State", string.Format("_{0}", targetStateName))));
                    jumpStmts.Add(Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst("execute_" + targetStateName)));
                    ordinaryTransitionStmt = MkZingIfThenElse(condExpr, MkZingSeq(jumpStmts), ordinaryTransitionStmt);
                }

                blocks.Add(AddArgs(ZingData.App_LabelStmt, transitionLabel,
                                            MkZingSeq(MkZingAssign(currentActionSet, AddArgs(ZingData.App_New, smEventSetType, ZingData.Cnst_Nil)),
                                                      MkZingCallStmt(MkZingCall(MkZingIdentifier(string.Format("{0}_CalculateActionSet", stateName)), currentActionSet)),
                                                      actionStmt, exitFunction, ordinaryTransitionStmt)));
            }
            AST<Node> body = ConstructList(ZingData.App_Blocks, blocks);
            return MkZingMethodDecl("runHelper", ZingData.Cnst_Nil, ZingData.Cnst_Void, MkZingVarDecls(locals), body);
        }

        enum TranslationContext { Action, Entry, Exit, Function, Transition };
        class ZingEntryFun_FoldContext
        {
            public string machineName;
            public TranslationContext translationContext;
            public string entityName;
            public Stack<List<AST<Node>>> sideEffectsStack;
            public List<Tuple<AST<Node>, string>> locals;
            private Dictionary<string, int> labels;
            private PToZing pToZing;

            public ZingEntryFun_FoldContext(string machineName, TranslationContext translationContext, string entityName, PToZing comp)
            {
                this.machineName = machineName;
                this.translationContext = translationContext;
                this.entityName = entityName;
                this.sideEffectsStack = new Stack<List<AST<Node>>>();
                pushSideEffectStack();
                this.locals = new List<Tuple<AST<Node>, string>>();
                this.pToZing = comp;
                this.labels = new Dictionary<string, int>();
            }

            public int labelToId(string l)
            {
                return labels[l];
            }

            public string getFreshLabel()
            {
                string baseL;
                if (translationContext == TranslationContext.Entry)
                    baseL = "entry_" + entityName;
                else if (translationContext == TranslationContext.Entry)
                    baseL = "exit_" + entityName;
                else if (translationContext == TranslationContext.Function)
                {
                    baseL = "function_" + entityName;
                }
                else
                    baseL = "action_" + entityName;

                var l = pToZing.getUnique(baseL);
                labels[l] = labels.Count + 1;

                return l;
            }

            public AST<Node> emitLabelPrelude()
            {
                var prelude = new List<AST<Node>>();
                var tmpVar = getTmpVar(ZingData.Cnst_Int, "retTo");

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

            public void addSideEffect(AST<Node> stmt)
            {
                this.sideEffectsStack.Peek().Add(stmt);
            }

            public void pushSideEffectStack()
            {
                this.sideEffectsStack.Push(new List<AST<Node>>());
            }

            // type must be Zing type
            public AST<Node> getTmpVar(AST<Node> type, string baseName)
            {
                var tmpVarName = pToZing.getUnique(baseName);
                var tmpVar = MkZingIdentifier(tmpVarName);
                this.locals.Add(new Tuple<AST<Node>, string>(type, tmpVarName));
                return tmpVar;
            }

            public AST<Node> emitZingSideEffects(AST<Node> stmt)
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

            public AST<Node> emitLocals()
            {
                return ConstructList(ZingData.App_VarDecls, locals.Select(loc => PToZing.MkZingVarDecl(loc.Item2, loc.Item1)));
            }
        }

        private IEnumerable<Node> ZingEntryFun_UnFold(ZingEntryFun_FoldContext ctxt, Node n)
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
                ctxt.pushSideEffectStack();
                bool first = true;
                foreach (var t in ft.Args)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        foreach (var a in ZingEntryFun_UnFold(ctxt, t))
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
                yield break;
            }
            else if (funName == PData.Con_Field.Node.Name || funName == PData.Con_Cast.Node.Name)
            {
                yield return ft.Args.First<Node>();
            }
            else if (funName == PData.Con_Tuple.Node.Name)
            {
                foreach (var a in ZingEntryFun_UnFold(ctxt, GetArgByIndex(ft, 0)))
                {
                    yield return a;
                }
            }
            else if (funName == PData.Con_NamedTuple.Node.Name)
            {
                foreach (var a in ZingEntryFun_UnFold(ctxt, GetArgByIndex(ft, 0)))
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
                    ctxt.pushSideEffectStack();
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
                    ctxt.pushSideEffectStack();
                    yield return it.Current;
                    it.MoveNext();
                    ctxt.pushSideEffectStack();
                    yield return it.Current;
                }
            }
            else if (funName == PData.Con_While.Node.Name)
            {
                using (var it = ft.Args.GetEnumerator())
                {
                    it.MoveNext();
                    ctxt.pushSideEffectStack();
                    yield return it.Current;
                    it.MoveNext();
                    ctxt.pushSideEffectStack();
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

        private AST<Node> zingWrapExprToStmts(AST<Node> exp)
        {
            if (isInstanceOf(exp.Node, ZingData.App_Call))
            {
                return MkZingCallStmt(exp);
            }
            else if (isInstanceOf(exp.Node, ZingData.App_Identifier))
            {
                // This can happen when we have a function, that returns a compound type,
                // whose return value is ignored.
                return ZingData.Cnst_Nil;
            }
            else
                return exp;
        }

        private AST<Node> MkZingAssignWithClone(AST<Node> lhs, AST<Node> rhs)
        {
            return AddArgs(ZingData.App_Assign, lhs, MkZingCall(PrtCloneValue, rhs));
        }

        private ZingTranslationInfo ZingEntryFun_Fold(ZingEntryFun_FoldContext ctxt, Node n, IEnumerable<ZingTranslationInfo> children)
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

        ZingTranslationInfo FoldName(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            var name = GetName(ft, 0);
            if (ctxt.translationContext == TranslationContext.Function && 
                allMachines[ctxt.machineName].funNameToFunInfo[ctxt.entityName].parameterNameToInfo.ContainsKey(name))
            {
                // TBD: The name should be qualified to prevent collision with machine variable names
                return new ZingTranslationInfo(MkZingIdentifier(name)); 
            }
            else if (allMachines[ctxt.machineName].localVariableToVarInfo.ContainsKey(name))
            {
                return new ZingTranslationInfo(MkZingIdentifier(name));
            }
            else 
            {
                var tmpVar = ctxt.getTmpVar(PrtValue, "tmp");
                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeEvent.Node))));
                ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetEvent"), tmpVar, MkZingEvent(name))));
                return new ZingTranslationInfo(tmpVar);
            }
        }

        ZingTranslationInfo FoldNew(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            var type = LookupType(ctxt, ft);
            var typeName = GetName(ft, 0);
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                AST<Node> arg = it.Current.node;
                if (arg == ZingData.Cnst_Nil)
                {
                    arg = MkZingIdentifier("null");
                }
                var tmpVar = ctxt.getTmpVar(PrtValue, "tmpSendPayload");
                ctxt.addSideEffect(MkZingAssignWithClone(tmpVar, arg));

                MachineInfo machineInfo = allMachines[typeName];
                if (machineInfo.IsMonitor)
                {
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("Main", string.Format("CreateMachine_{0}", typeName)), tmpVar)));
                    return new ZingTranslationInfo(ZingData.Cnst_Nil);
                }
                else
                {
                    var newMachine = ctxt.getTmpVar(SmHandle, "newMachine");
                    ctxt.addSideEffect(MkZingAssign(newMachine, MkZingCall(MkZingDot("Main", string.Format("CreateMachine_{0}", typeName)), tmpVar)));
                    string afterLabel = ctxt.getFreshLabel();
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "NewMachine"), Factory.Instance.MkCnst(ctxt.labelToId(afterLabel)), newMachine)));
                    ctxt.addSideEffect(MkZingReturn(MkZingIdentifier("entryCtxt")));
                    ctxt.addSideEffect(MkZingLabeledStmt(afterLabel, MkZingAssign(newMachine, MkZingDot("entryCtxt", "id"))));
                    ctxt.addSideEffect(MkZingAssign(MkZingDot("entryCtxt", "id"), MkZingIdentifier("null")));
                    var retVal = ctxt.getTmpVar(PrtValue, "tmp");
                    ctxt.addSideEffect(MkZingAssign(retVal, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(type))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetMachine"), retVal, newMachine)));
                    return new ZingTranslationInfo(retVal);
                }
            }
        }

        ZingTranslationInfo FoldFunApp(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
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
                var tmpVar = ctxt.getTmpVar(PrtValue, "argCloneVar");
                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(child.node, "Clone"))));
                count++;
                args.Add(tmpVar);
            }

            AST<Node> callExpr = MkZingCall(MkZingIdentifier(calleeName), args);
            AST<Node> processOutput;
            AST<Node> outExp;

            var beforeLabel = ctxt.getFreshLabel();
            if (calleeInfo.returnType.Equals(PTypeNull))
            {
                processOutput = ZingData.Cnst_Nil;
                outExp = ZingData.Cnst_Nil;
            }
            else
            {
                var retVar = ctxt.getTmpVar(PrtValue, "ret");
                processOutput = MkZingAssignWithClone(retVar, MkZingDot("myHandle", "currentArg"));
                outExp = retVar;
            }

            AST<Node> callStmt = MkZingSeq(
                MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "PushReturnTo"), Factory.Instance.MkCnst(0))),
                MkZingLabeledStmt(beforeLabel, ctxt.emitZingSideEffects(MkZingAssign(MkZingIdentifier("entryCtxt"), callExpr))),
                MkZingIfThenElse(
                                 MkZingEq(MkZingDot("entryCtxt", "reason"), MkZingDot("ContinuationReason", "Return")),
                                 processOutput,
                                 MkZingSeq(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "PushReturnTo"), Factory.Instance.MkCnst(ctxt.labelToId(beforeLabel)))),
                                           MkZingReturn(MkZingIdentifier("entryCtxt")))));
            ctxt.addSideEffect(callStmt);
            return new ZingTranslationInfo(outExp);
        }

        ZingTranslationInfo FoldNulApp(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            var n = GetArgByIndex(ft, 0);
            if (n.NodeKind == NodeKind.Cnst)
            {
                var tmpVar = ctxt.getTmpVar(PrtValue, "tmp");
                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeInt.Node))));
                ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetInt"), tmpVar, Factory.Instance.ToAST(n))));
                return new ZingTranslationInfo(tmpVar);
            }

            // n.NodeKind == NodeKind.Id
            var op = ((Id)n).Name;
            if (op == PData.Cnst_True.Node.Name)
            {
                var tmpVar = ctxt.getTmpVar(PrtValue, "tmp");
                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, ZingData.Cnst_True)));
                return new ZingTranslationInfo(tmpVar);
            }
            else if (op == PData.Cnst_False.Node.Name)
            {
                var tmpVar = ctxt.getTmpVar(PrtValue, "tmp");
                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, ZingData.Cnst_False)));
                return new ZingTranslationInfo(tmpVar);
            }
            else if (op == PData.Cnst_This.Node.Name)
            {
                var machineType = allMachines[ctxt.machineName].IsModel ? PTypeModel : PTypeReal;
                var tmpVar = ctxt.getTmpVar(PrtValue, "tmp");
                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(machineType.Node))));
                ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetMachine"), tmpVar, MkZingIdentifier("myHandle"))));
                return new ZingTranslationInfo(tmpVar);
            }
            else if (op == PData.Cnst_Trigger.Node.Name)
            {
                var tmpVar = ctxt.getTmpVar(PrtValue, "tmp");
                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeEvent.Node))));
                ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetEvent"), tmpVar, MkZingDot("myHandle", "currentEvent"))));
                return new ZingTranslationInfo(tmpVar);
            }
            else if (op == PData.Cnst_Payload.Node.Name)
            {
                return new ZingTranslationInfo(MkZingDot("myHandle", "currentArg"));
            }
            else if (op == PData.Cnst_Nondet.Node.Name || op == PData.Cnst_FairNondet.Node.Name)
            {
                var afterLabel = ctxt.getFreshLabel();
                var bvar = ctxt.getTmpVar(ZingData.Cnst_Bool, "nondet");
                ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Nondet"), Factory.Instance.MkCnst(ctxt.labelToId(afterLabel)))));
                ctxt.addSideEffect(MkZingReturn(MkZingIdentifier("entryCtxt")));
                ctxt.addSideEffect(MkZingLabeledStmt(afterLabel, MkZingAssign(bvar, MkZingDot("entryCtxt", "nondet"))));
                ctxt.addSideEffect(MkZingAssign(MkZingDot("entryCtxt", "nondet"), ZingData.Cnst_False));
                if (compiler.liveness == LivenessOption.Standard && op == PData.Cnst_FairNondet.Node.Name)
                {
                    int i;
                    if (ctxt.translationContext == TranslationContext.Action)
                    {
                        i = allMachines[ctxt.machineName].actionNameToActionInfo[ctxt.entityName].numFairChoices++;
                    }
                    else if (ctxt.translationContext == TranslationContext.Entry)
                    {
                        i = allMachines[ctxt.machineName].stateNameToStateInfo[ctxt.entityName].numFairChoicesInEntry++;
                    }
                    else if (ctxt.translationContext == TranslationContext.Exit)
                    {
                        i = allMachines[ctxt.machineName].stateNameToStateInfo[ctxt.entityName].numFairChoicesInExit++;
                    }
                    else
                    {
                        i = allMachines[ctxt.machineName].funNameToFunInfo[ctxt.entityName].numFairChoices++;
                    }
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(GetFairChoice(ctxt.translationContext, ctxt.entityName, i), "AtChoose"), bvar)));
                }
                var tmpVar = ctxt.getTmpVar(PrtValue, "tmp");
                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, bvar)));
                return new ZingTranslationInfo(tmpVar);
            }
            else if (op == PData.Cnst_Null.Node.Name)
            {
                var tmpVar = ctxt.getTmpVar(PrtValue, "tmp");
                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeNull.Node))));
                return new ZingTranslationInfo(tmpVar);
            }
            else
            {
                // op == PData.Cnst_Halt.Node.Name
                var tmpVar = ctxt.getTmpVar(PrtValue, "tmp");
                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeEvent.Node))));
                ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetEvent"), tmpVar, MkZingEvent(HaltEvent))));
                return new ZingTranslationInfo(tmpVar);
            }
        }

        ZingTranslationInfo FoldUnApp(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            var op = ((Id)GetArgByIndex(ft, 0)).Name; 
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                var arg = it.Current.node;
                if (op == PData.Cnst_Not.Node.Name)
                {
                    var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, MkZingApply(ZingData.Cnst_Not, MkZingDot(arg, "bl")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Neg.Node.Name)
                {
                    var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeInt.Node))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetInt"), tmpVar, MkZingApply(ZingData.Cnst_Neg, MkZingDot(arg, "nt")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Keys.Node.Name)
                {
                    var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtMapGetKeys"), arg)));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Values.Node.Name)
                {
                    var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtMapGetValues"), arg)));
                    return new ZingTranslationInfo(tmpVar);
                }
                else
                {
                    //  op == PData.Cnst_Sizeof.Node.Name
                    var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtMapSizeOf"), arg)));
                    return new ZingTranslationInfo(tmpVar);
                }
            }
        }

        ZingTranslationInfo FoldBinApp(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
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
                    var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeInt.Node))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetInt"), tmpVar, MkZingApply(ZingData.Cnst_Add, MkZingDot(arg1, "nt"), MkZingDot(arg2, "nt")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Sub.Node.Name)
                {
                    var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeInt.Node))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetInt"), tmpVar, MkZingApply(ZingData.Cnst_Sub, MkZingDot(arg1, "nt"), MkZingDot(arg2, "nt")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Mul.Node.Name)
                {
                    var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeInt.Node))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetInt"), tmpVar, MkZingApply(ZingData.Cnst_Mul, MkZingDot(arg1, "nt"), MkZingDot(arg2, "nt")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_IntDiv.Node.Name)
                {
                    var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeInt.Node))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetInt"), tmpVar, MkZingApply(ZingData.Cnst_IntDiv, MkZingDot(arg1, "nt"), MkZingDot(arg2, "nt")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_And.Node.Name)
                {
                    var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, MkZingApply(ZingData.Cnst_And, MkZingDot(arg1, "bl"), MkZingDot(arg2, "bl")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Or.Node.Name)
                {
                    var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, MkZingApply(ZingData.Cnst_Or, MkZingDot(arg1, "bl"), MkZingDot(arg2, "bl")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Eq.Node.Name)
                {
                    var tmpVar = ctxt.getTmpVar(ZingData.Cnst_Bool, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtIsEqualValue"), arg1, arg2)));
                    var retVal = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(retVal, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), retVal, tmpVar)));
                    return new ZingTranslationInfo(retVal);
                }
                else if (op == PData.Cnst_NEq.Node.Name)
                {
                    var tmpVar = ctxt.getTmpVar(ZingData.Cnst_Bool, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtIsEqualValue"), arg1, arg2)));
                    var retVal = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(retVal, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), retVal, MkZingApply(ZingData.Cnst_Not, tmpVar))));
                    return new ZingTranslationInfo(retVal);
                }
                else if (op == PData.Cnst_Lt.Node.Name)
                {
                    var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, MkZingApply(ZingData.Cnst_Lt, MkZingDot(arg1, "bl"), MkZingDot(arg2, "bl")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Le.Node.Name)
                {
                    var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, MkZingApply(ZingData.Cnst_Le, MkZingDot(arg1, "bl"), MkZingDot(arg2, "bl")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Gt.Node.Name)
                {
                    var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, MkZingApply(ZingData.Cnst_Gt, MkZingDot(arg1, "bl"), MkZingDot(arg2, "bl")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Ge.Node.Name)
                {
                    var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), tmpVar, MkZingApply(ZingData.Cnst_Ge, MkZingDot(arg1, "bl"), MkZingDot(arg2, "bl")))));
                    return new ZingTranslationInfo(tmpVar);
                }
                else if (op == PData.Cnst_Idx.Node.Name)
                {
                    var type = LookupType(ctxt, arg1.Node);
                    var typeOp = ((Id)GetArgByIndex(type, 0)).Name;
                    if (typeOp == PData.Con_TupType.Node.Name)
                    {
                        var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                        ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtTupleGet"), arg1, arg2)));
                        return new ZingTranslationInfo(tmpVar);
                    }
                    else if (typeOp == PData.Con_SeqType.Node.Name)
                    {
                        var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                        ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtSeqGet"), arg1, arg2)));
                        return new ZingTranslationInfo(tmpVar);
                    }
                    else 
                    {
                        // op == PData.Con_MapType.Node.Name
                        var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                        ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtMapGet"), arg1, arg2)));
                        return new ZingTranslationInfo(tmpVar);
                    }
                }
                else 
                {
                    // op == PData.Cnst_In.Node.Name
                    var tmpVar = ctxt.getTmpVar(ZingData.Cnst_Bool, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtMapExists"), arg2, arg1)));
                    var retVal = ctxt.getTmpVar(PrtValue, "tmpVar");
                    ctxt.addSideEffect(MkZingAssign(retVal, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeBool.Node))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtPrimSetBool"), retVal, tmpVar)));
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

        ZingTranslationInfo FoldField(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
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
                var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVal");
                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(PRT_VALUE, "PrtTupleGet"), arg, Factory.Instance.MkCnst(fieldIndex))));
                return new ZingTranslationInfo(tmpVar);
            }
        }

        ZingTranslationInfo FoldDefault(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            var typeArg = (FuncTerm)GetArgByIndex(ft, 0);
            var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
            ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(typeArg))));
            return new ZingTranslationInfo(tmpVar);
        }

        ZingTranslationInfo FoldCast(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            var typeArg = (FuncTerm)GetArgByIndex(ft, 1);
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                var valueArg = it.Current.node;
                var tmpVar = ctxt.getTmpVar(PrtValue, "tmpVar");
                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtCastValue, valueArg, typeContext.PTypeToZingExpr(typeArg))));
                return new ZingTranslationInfo(tmpVar);
            }
        }

        ZingTranslationInfo FoldTuple(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            var tupType = LookupType(ctxt, ft);
            var tmpVar = ctxt.getTmpVar(PrtValue, "tmpTuple");
            ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(tupType))));
            int i = 0;
            foreach (var c in children)
            {
                ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtTupleSet"), tmpVar, Factory.Instance.MkCnst(i), c.node)));
                i++;
            }
            return new ZingTranslationInfo(tmpVar);
        }

        ZingTranslationInfo FoldNamedTuple(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            var tupType = LookupType(ctxt, ft);
            var tmpVar = ctxt.getTmpVar(PrtValue, "tmpTuple");
            ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(tupType))));
            int i = 0;
            foreach (var c in children)
            {
                ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(PRT_VALUE, "PrtTupleSet"), tmpVar, Factory.Instance.MkCnst(i), c.node)));
                i++;
            }
            return new ZingTranslationInfo(tmpVar);
        }

        ZingTranslationInfo FoldPush(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            var qualifiedName = (FuncTerm)GetArgByIndex(ft, 0);
            var stateName = GetNameFromQualifiedName(ctxt.machineName, qualifiedName);
            var afterLabel = ctxt.getFreshLabel();
            var res = MkZingSeq(
                MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Call"), Factory.Instance.MkCnst(ctxt.labelToId(afterLabel)), MkZingDot("State", string.Format("_{0}", stateName)))),
                MkZingReturn(MkZingIdentifier("entryCtxt")),
                MkZingLabeledStmt(afterLabel, ZingData.Cnst_Nil));
            return new ZingTranslationInfo(res);
        }

        ZingTranslationInfo FoldNewStmt(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            return FoldNew(ft, children, ctxt);
        }

        ZingTranslationInfo FoldRaise(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                var eventExpr = MkZingDot(it.Current.node, "ev");
                it.MoveNext();
                var payloadExpr = it.Current.node;
                var assertStmt = MkZingSeq(Factory.Instance.AddArg(ZingData.App_Assert, MkZingApply(ZingData.Cnst_NEq, eventExpr, MkZingIdentifier("null"))),
                                           Factory.Instance.AddArg(ZingData.App_Assert, MkZingApply(ZingData.Cnst_NEq, eventExpr, MkZingEvent("default"))));
                string traceString = string.Format("\"<RaiseLog> Machine {0}-{{0}} raised Event {{1}}\"", ctxt.machineName);
                var traceStmt = MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(traceString), MkZingDot("myHandle", "instance"), MkZingDot(eventExpr, "name")));
                var tmpVar = ctxt.getTmpVar(PrtValue, "tmpPayload");
                if (payloadExpr == ZingData.Cnst_Nil)
                {
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeNull.Node))));
                }
                else
                {
                    ctxt.addSideEffect(MkZingAssignWithClone(tmpVar, payloadExpr));
                }

                var assignStmt = MkZingSeq(MkZingAssign(MkZingDot("myHandle", "currentEvent"), eventExpr), MkZingAssign(MkZingDot("myHandle", "currentArg"), tmpVar));
                var createRetCtxt = MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Raise")));
                return new ZingTranslationInfo(MkZingSeq(assertStmt, traceStmt, assignStmt, createRetCtxt, MkZingReturn(MkZingIdentifier("entryCtxt"))));
            }
        }

        ZingTranslationInfo FoldSend(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                var targetExpr = MkZingDot(it.Current.node, "mach");
                it.MoveNext();
                var eventExpr = MkZingDot(it.Current.node, "ev");
                it.MoveNext();
                AST<Node> payloadExpr = it.Current.node;
                var tmpVar = ctxt.getTmpVar(PrtValue, "tmpPayload");
                if (payloadExpr == ZingData.Cnst_Nil)
                {
                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeNull.Node))));
                }
                else
                {
                    ctxt.addSideEffect(MkZingAssignWithClone(tmpVar, payloadExpr));
                }
                var afterLabel = ctxt.getFreshLabel();
                ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot(targetExpr, "EnqueueEvent"), eventExpr, tmpVar, Factory.Instance.MkCnst("myHandle"))));
                ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Send"), Factory.Instance.MkCnst(ctxt.labelToId(afterLabel)))));
                return new ZingTranslationInfo(MkZingSeq(MkZingReturn(MkZingIdentifier("entryCtxt")), MkZingLabeledStmt(afterLabel, ZingData.Cnst_Nil)));
            }
        }

        ZingTranslationInfo FoldMonitor(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            var typeName = GetName(ft, 0);
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                AST<Node> evt = it.Current.node;
                it.MoveNext();
                AST<Node> arg = it.Current.node != ZingData.Cnst_Nil ? it.Current.node : MkZingIdentifier("null");
                var tmpVar = ctxt.getTmpVar(PrtValue, "tmpSendPayload");
                ctxt.addSideEffect(MkZingAssignWithClone(tmpVar, arg));
                MachineInfo machineInfo = allMachines[typeName];
                return new ZingTranslationInfo(MkZingCall(MkZingDot("Main", string.Format("InvokeMachine_{0}", typeName)), evt, tmpVar));
            }
        }

        ZingTranslationInfo FoldFunStmt(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            return FoldFunApp(ft, children, ctxt);
        }

        ZingTranslationInfo FoldNulStmt(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            var op = ((Id)GetArgByIndex(ft, 0)).Name;
            if (op == PData.Cnst_Pop.Node.Name)
            {
                ctxt.addSideEffect(MkZingAssign(MkZingDot("myHandle", "currentEvent"), MkZingIdentifier("null")));
                ctxt.addSideEffect(MkZingAssign(MkZingDot("myHandle", "currentArg"), MkZingIdentifier("null")));
                ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Pop"))));
                ctxt.addSideEffect(MkZingReturn(MkZingIdentifier("entryCtxt")));
            }
            return new ZingTranslationInfo(ZingData.Cnst_Nil);

        }

        ZingTranslationInfo FoldUnStmt(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            var op = ((Id)GetArgByIndex(ft, 0)).Name; 
            // op == PData.Con_Assert.Node.Name
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                return new ZingTranslationInfo(AddArgs(ZingData.App_Assert, MkZingDot(it.Current.node, "bl")));
            }
        }

        ZingTranslationInfo FoldBinStmt(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt) 
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
                    var idx = ctxt.getTmpVar(PrtValue, "idx");
                    var val = ctxt.getTmpVar(PrtValue, "val");
                    ctxt.addSideEffect(MkZingAssign(idx, MkZingCall(MkZingDot("PRT_VALUE", "PrtTupleGet"), src, Factory.Instance.MkCnst(0))));
                    ctxt.addSideEffect(MkZingAssign(val, MkZingCall(MkZingDot("PRT_VALUE", "PrtTupleGet"), src, Factory.Instance.MkCnst(1))));
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

        ZingTranslationInfo FoldReturn(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            var returnType = allMachines[ctxt.machineName].funNameToFunInfo[ctxt.entityName].returnType;
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                if (returnType.Equals(PTypeNull))
                {
                    ctxt.addSideEffect(MkZingAssign(MkZingDot("myHandle", "currentArg"), MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeNull.Node))));
                }
                else
                {
                    ctxt.addSideEffect(MkZingAssignWithClone(MkZingDot("myHandle", "currentArg"), it.Current.node));
                }
                ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Return"))));
                ctxt.addSideEffect(MkZingReturn(MkZingIdentifier("entryCtxt")));
                return new ZingTranslationInfo(ZingData.Cnst_Nil);
            }
        }

        ZingTranslationInfo FoldWhile(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                var condExpr = MkZingDot(it.Current.node, "bl");
                it.MoveNext();
                var loopStart = getUnique(ctxt.entityName + "_loop_start");
                var loopEnd = getUnique(ctxt.entityName + "_loop_end");
                var body = it.Current.node;
                body = ctxt.emitZingSideEffects(zingWrapExprToStmts(body));
                var res = MkZingLabeledStmt(loopStart, MkZingSeq(
                    ctxt.emitZingSideEffects(MkZingIfThen(MkZingApply(ZingData.Cnst_Not, condExpr), AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst(loopEnd)))),
                    body,
                    AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst(loopStart)),
                    MkZingLabeledStmt(loopEnd, ZingData.Cnst_Nil)));
                return new ZingTranslationInfo(res);
            }
        }

        ZingTranslationInfo FoldIte(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
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
                var ifName = getUnique(ctxt.entityName + "_if");
                var elseLabel = ifName + "_else";
                var afterLabel = ifName + "_end";
                var cookedElse = MkZingLabeledStmt(elseLabel, ctxt.emitZingSideEffects(zingWrapExprToStmts(elseStmt)));
                var cookedThen = ctxt.emitZingSideEffects(zingWrapExprToStmts(thenStmt));
                var res = MkZingSeq(MkZingIfThen(MkZingApply(ZingData.Cnst_Not, condExpr), AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst(elseLabel))),
                    cookedThen,
                    AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst(afterLabel)),
                    cookedElse,
                    MkZingLabeledStmt(afterLabel, ZingData.Cnst_Nil));
                return new ZingTranslationInfo(res);
            }
        }

        ZingTranslationInfo FoldSeq(FuncTerm ft, IEnumerable<ZingTranslationInfo> children, ZingEntryFun_FoldContext ctxt)
        {
            using (var it = children.GetEnumerator())
            {
                it.MoveNext();
                var first = it.Current;
                it.MoveNext();
                var second = it.Current;
                return new ZingTranslationInfo(MkZingSeq(zingWrapExprToStmts(first.node), ctxt.emitZingSideEffects(zingWrapExprToStmts(second.node))));
            }
        }

        private string getZingWrapperFunName(string entity, TranslationContext ctxType)
        {
            if (ctxType == TranslationContext.Entry)
                return "entry_" + entity;
            else if (ctxType == TranslationContext.Exit)
                return "exit_" + entity;
            else
            {
                Debug.Assert(TranslationContext.Action == ctxType);
                return "action_" + entity;
            }
        }

        private AST<Node> MkZingWrapperFn(Node n, string machine, TranslationContext ctxType, string entity)
        {
            var name = getZingWrapperFunName(entity, ctxType);
            AST<Node> body;

            var ctxt = new ZingEntryFun_FoldContext(machine, ctxType, entity, this);
            var tuple = Factory.Instance.ToAST(n).Compute<ZingTranslationInfo>(
                x => ZingEntryFun_UnFold(ctxt, x),
                (x, ch) => ZingEntryFun_Fold(ctxt, x, ch));

            if (tuple == null)
                body = ZingData.Cnst_Nil;
            else
            {
                Debug.Assert(ctxt.sideEffectsStack.Count == 1);
                body = ctxt.emitZingSideEffects(zingWrapExprToStmts(tuple.node));
            }
                                
            body = MkZingSeq(
                ctxt.emitLabelPrelude(),
                MkZingLabeledStmt("start", body),
                (ctxt.translationContext == TranslationContext.Entry || ctxt.translationContext == TranslationContext.Action)
                          ? MkZingSeq(MkZingAssign(MkZingDot("myHandle", "currentEvent"), MkZingIdentifier("null")),
                                      MkZingAssign(MkZingDot("myHandle", "currentArg"), MkZingCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(PTypeNull.Node)))) 
                          : ZingData.Cnst_Nil,
                MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Return"))),
                MkZingReturn(MkZingIdentifier("entryCtxt")));

            return MkZingMethodDecl(name, MkZingVarDecls(MkZingVarDecl("entryCtxt", Factory.Instance.MkCnst("Continuation"))), Factory.Instance.MkCnst("Continuation"),
                ctxt.emitLocals(), MkZingBlock("dummy", body));
        }

        private AST<Node> MkZingFunMethod(string funName, FunInfo funInfo)
        {
            var machineName = funInfo.ownerName;
            AST<Node> parameters = LocalVariablesToVarDecls(funInfo.parameterNames, funInfo.parameterNameToInfo);
            parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("entryCtxt", Factory.Instance.MkCnst("Continuation")), parameters);
            AST<Node> funBody;
            AST<Node> entry = Factory.Instance.ToAST(funInfo.body);

            var ctxt = new ZingEntryFun_FoldContext(machineName, TranslationContext.Function, funName, this);
            var tuple = Factory.Instance.ToAST(entry.Node).Compute<ZingTranslationInfo>(
                x => ZingEntryFun_UnFold(ctxt, x),
                (x, ch) => ZingEntryFun_Fold(ctxt, x, ch));

            if (tuple == null)
            {
                funBody = ZingData.Cnst_Nil;
            }
            else
            {
                Debug.Assert(ctxt.sideEffectsStack.Count == 1);
                funBody = ctxt.emitZingSideEffects(zingWrapExprToStmts(tuple.node));
                ctxt.pushSideEffectStack();
            }

            funBody = MkZingSeq(
                ctxt.emitLabelPrelude(),
                MkZingLabeledStmt("start", funBody),
                MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Return"))),
                MkZingReturn(MkZingIdentifier("entryCtxt")));

            var retType = Factory.Instance.MkCnst("Continuation");
            return MkZingMethodDecl(funName, parameters, retType, ctxt.emitLocals(), MkZingBlock("dummy", funBody));
        }

        private AST<Node> MkZingFor(AST<Node> indVar, AST<Node> start, AST<Node> end, AST<Node> body)
        {
            return MkZingSeq(
                MkZingAssign(indVar, start),
                AddArgs(ZingData.App_While, MkZingApply(ZingData.Cnst_Lt, indVar, end),
                    MkZingSeq(body,
                        MkZingAssignWithClone(indVar, MkZingPlus(indVar, 1)))));
        }

        private AST<Node> MkZingFor(AST<Node> indVar, int start, AST<Node> end, AST<Node> body)
        {
            return MkZingFor(indVar, Factory.Instance.MkCnst(start), end, body);
        }

        private AST<Node> LocalVariablesToVarDecls(IEnumerable<string> varNames, Dictionary<string, VariableInfo> varNameToVarInfo)
        {
            return MkZingVarDecls(varNames.Select(name => MkZingVarDecl(name, PrtValue)));
        }

        private AST<Node> MkInvokeMonitorMethod(string machineName)
        {
            var parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("arg", Factory.Instance.MkCnst("PRT_VALUE")), ZingData.Cnst_Nil);
            parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("evt", Factory.Instance.MkCnst("SM_EVENT")), parameters);
            var localVars = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("iter", Factory.Instance.MkCnst(machineName)), ZingData.Cnst_Nil);
            var iter = MkZingIdentifier("iter");
            var myHandle = MkZingDot("iter", "myHandle");
            AST<Node> loopBody =
                MkZingSeq(
                    MkZingAssign(MkZingDot("iter", "myHandle", "currentEvent"), MkZingIdentifier("evt")),
                    MkZingAssign(MkZingDot("iter", "myHandle", "currentArg"), MkZingIdentifier("arg")),
                    MkZingCallStmt(MkZingCall(MkZingDot("iter", "runHelper"))));
            AST<Node> body = MkZingListIter(iter, MkZingDot("Main", GetMonitorMachineName(machineName)), MkZingIdentifier("null"), loopBody);
            body = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("dummy"), body);
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

            AST<Node> body = ZingData.Cnst_Nil;
            if (compiler.liveness == LivenessOption.Standard) 
            {
                localVars = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("gateProgress", ZingData.Cnst_Bool), localVars);
                List<AST<Node>> stmts = new List<AST<Node>>();
                stmts.Add(MkZingIfThen(MkZingApply(ZingData.Cnst_NEq, MkZingDot("FairCycle", "gate"), MkZingDot("GateStatus", "Init")), MkZingReturn(ZingData.Cnst_Nil)));
                stmts.Add(MkZingAssign(MkZingIdentifier("gateProgress"), MkZingCall(Factory.Instance.MkCnst("choose"), Factory.Instance.MkCnst("bool"))));
                stmts.Add(MkZingIfThen(MkZingIdentifier("gateProgress"), MkZingReturn(ZingData.Cnst_Nil)));
                var machineInfo = allMachines[machineName];
                if (!machineInfo.isInfinitelyOftenMonitor)
                {
                    stmts.Add(MkZingAssign(MkZingDot("FairCycle", "gate"), MkZingDot("GateStatus", "Closed")));
                }
                body = MkZingSeq(stmts);
            }

            var machineInstance = MkZingIdentifier(string.Format("{0}_instance", machineName));
            var machineHandles = MkZingDot("Main", GetMonitorMachineName(machineName));
            body = MkZingSeq(body,
                    MkZingAssign(MkZingIdentifier(objectName), AddArgs(ZingData.App_New, Factory.Instance.MkCnst(machineName), ZingData.Cnst_Nil)),
                    MkInitializers(machineName, objectName),
                    MkZingAssign(MkZingDot(objectName, "myHandle"),
                                 MkZingCall(MkZingDot("SPEC_HANDLE", "Construct"), MkZingDot("Machine", string.Format("_{0}", machineName)), machineInstance)),
                    MkZingAssign(MkZingDot(objectName, "myHandle", "currentArg"), MkZingIdentifier("arg")),
                    MkZingAssign(machineInstance, MkZingApply(ZingData.Cnst_Add, machineInstance, Factory.Instance.MkCnst(1))),
                    MkZingAssign(MkZingDot(objectName, "startState"), MkZingDot("State", "_default")),
                    MkZingAssign(MkZingDot(objectName, "localActions"), MkZingCall(MkZingDot("LocalActions", "Construct"), MkZingIdentifier("null"))),
                    MkZingAssign(MkZingDot(objectName, "next"), machineHandles),
                    MkZingAssign(machineHandles, MkZingIdentifier(objectName)),
                    MkZingCallStmt(MkZingCall(MkZingDot(objectName, "runHelper")))
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

            var machineInstance = MkZingIdentifier(string.Format("{0}_instance", machineName));
            var body = MkZingSeq(
                    MkZingAssign(MkZingIdentifier(objectName), AddArgs(ZingData.App_New, Factory.Instance.MkCnst(machineName), ZingData.Cnst_Nil)),
                    MkInitializers(machineName, objectName),
                    MkZingAssign(MkZingDot(objectName, "myHandle"),
                                 MkZingCall(MkZingDot("SM_HANDLE", "Construct"), MkZingDot("Machine", string.Format("_{0}", machineName)), machineInstance, Factory.Instance.MkCnst(allMachines[machineName].maxQueueSize))),
                    MkZingAssign(MkZingDot(objectName, "myHandle", "currentArg"), MkZingIdentifier("arg")),
                    MkZingAssign(machineInstance, MkZingApply(ZingData.Cnst_Add, machineInstance, Factory.Instance.MkCnst(1))),
                    MkZingAssign(MkZingDot(objectName, "stackStable"), ZingData.Cnst_False),
                    MkZingAssign(MkZingDot(objectName, "stackDeferredSet"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("SM_EVENT_SET"), ZingData.Cnst_Nil)),
                    MkZingAssign(MkZingDot(objectName, "stackActionSet"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("SM_EVENT_SET"), ZingData.Cnst_Nil)),
                    MkZingAssign(MkZingDot(objectName, "localActions"), MkZingCall(MkZingDot("LocalActions", "Construct"), MkZingIdentifier("null")))
                    );

            if (compiler.liveness == LivenessOption.Standard)
            {
                body = MkZingSeq(body,
                                 MkZingAssign(MkZingIdentifier("fairScheduler"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("FairScheduler"), ZingData.Cnst_Nil)),
                                 MkZingCallStmt(MkZingCall(MkZingDot("FairScheduler", "Init"), MkZingIdentifier("fairScheduler"), MkZingDot(objectName, "myHandle"))));

                foreach (var stateName in allMachines[machineName].stateNameToStateInfo.Keys)
                {
                    var stateInfo = allMachines[machineName].stateNameToStateInfo[stateName];
                    for (int i = 0; i < stateInfo.numFairChoicesInEntry; i++)
                    {
                        body = MkZingSeq(body,
                                         MkZingAssign(MkZingIdentifier("fairChoice"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("FairChoice"), ZingData.Cnst_Nil)),
                                         MkZingCallStmt(MkZingCall(MkZingDot("FairChoice", "Init"), MkZingIdentifier("fairChoice"))),
                                         MkZingAssign(MkZingDot(objectName, GetFairChoice(TranslationContext.Entry, stateName, i)), MkZingIdentifier("fairChoice")));
                    }
                    for (int i = 0; i < stateInfo.numFairChoicesInExit; i++)
                    {
                        body = MkZingSeq(body,
                                         MkZingAssign(MkZingIdentifier("fairChoice"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("FairChoice"), ZingData.Cnst_Nil)),
                                         MkZingCallStmt(MkZingCall(MkZingDot("FairChoice", "Init"), MkZingIdentifier("fairChoice"))),
                                         MkZingAssign(MkZingDot(objectName, GetFairChoice(TranslationContext.Exit, stateName, i)), MkZingIdentifier("fairChoice")));
                    }
                }
                foreach (var actionName in allMachines[machineName].actionNameToActionInfo.Keys)
                {
                    var actionInfo = allMachines[machineName].actionNameToActionInfo[actionName];
                    for (int i = 0; i < actionInfo.numFairChoices; i++)
                    {
                        body = MkZingSeq(body,
                                         MkZingAssign(MkZingIdentifier("fairChoice"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("FairChoice"), ZingData.Cnst_Nil)),
                                         MkZingCallStmt(MkZingCall(MkZingDot("FairChoice", "Init"), MkZingIdentifier("fairChoice"))),
                                         MkZingAssign(MkZingDot(objectName, GetFairChoice(TranslationContext.Action, actionName, i)), MkZingIdentifier("fairChoice")));
                    }
                }
                foreach (var funName in allMachines[machineName].funNameToFunInfo.Keys)
                {
                    var funInfo = allMachines[machineName].funNameToFunInfo[funName];
                    for (int i = 0; i < funInfo.numFairChoices; i++)
                    {
                        body = MkZingSeq(body,
                                         MkZingAssign(MkZingIdentifier("fairChoice"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("FairChoice"), ZingData.Cnst_Nil)),
                                         MkZingCallStmt(MkZingCall(MkZingDot("FairChoice", "Init"), MkZingIdentifier("fairChoice"))),
                                         MkZingAssign(MkZingDot(objectName, GetFairChoice(TranslationContext.Function, funName, i)), MkZingIdentifier("fairChoice")));
                    }
                }
            }

            body = MkZingSeq(
                body,
                MkZingAssign(MkZingDot(objectName, "myHandle", "machineId"), MkZingCall(MkZingDot("MachineId", "GetNextId"))),
                MkZingCallStmt(MkZingCall(MkZingDot(objectName, "run")), ZingData.Cnst_Async),
                MkZingCallStmt(MkZingCall(MkZingIdentifier("invokescheduler"), Factory.Instance.MkCnst("\"map\""), MkZingDot(objectName, "myHandle", "machineId"))),
                Factory.Instance.AddArg(ZingData.App_Return, MkZingDot(objectName, "myHandle"))
                );
            body = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("dummy"), body);

            return MkZingMethodDecl(string.Format("CreateMachine_{0}", machineName), parameters, Factory.Instance.MkCnst("SM_HANDLE"), localVars, ConstructList(ZingData.App_Blocks, body), ZingData.Cnst_Static);
        }
        #endregion

        private FuncTerm LookupType(ZingEntryFun_FoldContext ctxt, Node node)
        {
            var ast = Factory.Instance.ToAST(node);
            if (ctxt.translationContext == TranslationContext.Action)
            {
                return allMachines[ctxt.machineName].actionNameToActionInfo[ctxt.entityName].typeInfo[ast];
            }
            else if (ctxt.translationContext == TranslationContext.Function)
            {
                return allMachines[ctxt.machineName].funNameToFunInfo[ctxt.entityName].typeInfo[ast];
            }
            else
            {
                return allMachines[ctxt.machineName].stateNameToStateInfo[ctxt.entityName].typeInfo[ast];
            }
        }

        private void Debug_PrintTypes(string machName, string entityName)
        {
            foreach (var kv in allMachines[machName].stateNameToStateInfo[entityName].typeInfo)
            {
                var s = string.Format("{0} : {1}", NodeToString(kv.Key.Node), NodeToString(kv.Value));
            }
        }

        TypeTranslationContext typeContext;

        internal class TypeTranslationContext 
        {
            private int fieldCount;
            private int typeCount;
            private List<AST<Node>> computation;
            private Dictionary<FuncTerm, AST<Node>> pTypeToZingExpr;

            public TypeTranslationContext()
            {
                fieldCount = 0;
                typeCount = 0;
                computation = new List<AST<Node>>();
                pTypeToZingExpr = new Dictionary<FuncTerm, AST<Node>>();
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

            public void AddSideEffect(AST<Node> n)
            {
                computation.Add(n);
            }

            public AST<Node> PTypeToZingExpr(FuncTerm pType)
            {
                if (!pTypeToZingExpr.ContainsKey(pType))
                {
                    pTypeToZingExpr[pType] = ConstructType(pType);
                }
                return pTypeToZingExpr[pType];
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
                        memberTypes.Add(ConstructType((FuncTerm)GetArgByIndex(type, 0)));
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
                        memberTypes.Add(ConstructType((FuncTerm)GetArgByIndex(typeField, 1)));
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
                    var innerType = ConstructType((FuncTerm)GetArgByIndex(type, 0));
                    var seqType = GetType();
                    AddSideEffect(MkZingAssign(seqType, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkSeqType"), innerType)));
                    return seqType;
                }
                else
                {
                    // typeKind == "MapType"
                    var domType = ConstructType((FuncTerm)GetArgByIndex(type, 0));
                    var codType = ConstructType((FuncTerm)GetArgByIndex(type, 1));
                    var mapType = GetType();
                    AddSideEffect(MkZingAssign(mapType, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkMapType"), domType, codType)));
                    return mapType;
                }
            }
        }
    }
}
