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
    internal class ZingTranslationInfo : ICloneable
    {
        public AST<Node> node = null;
        public PType type = null;
        public string stateName = null;
        public bool isMapIndex = false;
        public bool isKeys = false;
        public string newAfterLabel = null;

        private ZingTranslationInfo() { }
        public object Clone()
        {
            ZingTranslationInfo info = new ZingTranslationInfo();
            info.node = node;
            info.type = type;
            info.stateName = stateName;
            info.isMapIndex = isMapIndex;
            info.isKeys = isKeys;
            info.newAfterLabel = newAfterLabel;
            return info;
        }
          
        private PType primitiveTypeFromStr(string s)
        {
            switch (s)
            {   // Its fairly annoying that we can't use PData.Cnst_*.Node.Name here (switches require constants). Need to keep this in sync..
                case "NIL": return new PNilType();
                case "BOOL": return new PBoolType();
                case "INT": return new PIntType();
                case "ID": return new PIdType();
                case "MID": return new PMidType();
                case "EVENT": return new PEventType();
                case "STATE": return new PStateType();
                default:
                    throw new NotImplementedException("Unknown primitive type " + s);
            }
        }

        public ZingTranslationInfo(AST<Node> n, PType t)
        {
            this.node = n;
            this.type = t;
        }

        public ZingTranslationInfo(AST<Node> n, PType t, string stateName)
        {
            this.node = n;
            this.type = t;
            this.stateName = stateName;
        }

        public ZingTranslationInfo(AST<Node> n, PType t, bool isMapIndex, bool isKeys)
        {
            this.node = n;
            this.type = t;
            this.isMapIndex = isMapIndex;
            this.isKeys = isKeys;
        }
    }

    internal class TransitionInfo
    {
        public string target;
        public bool isPush;
        public TransitionInfo(string target, bool isPush)
        {
            this.target = target;
            this.isPush = isPush;
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            TransitionInfo p = obj as TransitionInfo;
            if ((System.Object)p == null)
            {
                return false;
            }

            return (target == p.target) && (isPush == p.isPush);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    internal class StateInfo
    {
        public FuncTerm stateDecl;
        public Node exitFun;
        public bool hasDefaultTransition;
        public Dictionary<string, TransitionInfo> transitions;
        public Dictionary<string, string> actions;
        public HashSet<string> entryFunCallees;
        public bool entryFunAtPassive;
        public HashSet<string> exitFunCallees;
        public bool exitFunAtPassive;
        public HashSet<PType> argTypes;
        public string submachineName;
        public bool isStable;
        public int numFairChoicesInEntry;
        public int numFairChoicesInExit;

        public StateInfo(FuncTerm stateDecl)
        {
            this.stateDecl = stateDecl;
            this.exitFun = null;
            this.hasDefaultTransition = false;
            this.transitions = new Dictionary<string, TransitionInfo>();
            this.actions = new Dictionary<string, string>();
            this.argTypes = new HashSet<PType>();
            this.entryFunCallees = new HashSet<string>();
            this.exitFunCallees = new HashSet<string>();
            this.isStable = false;
            this.numFairChoicesInEntry = 0;
            this.numFairChoicesInExit = 0;
        }
    }

    internal class VariableInfo
    {
        public PType type;

        public VariableInfo(PType type)
        {
            this.type = type;
        }
    }

    internal class FunInfo
    {
        public bool isModel;
        public Dictionary<string, VariableInfo> parameterNameToInfo;
        public List<string> parameterNames;
        public PType returnType;
        public FuncTerm funDecl;
        public HashSet<string> callers;
        public bool atPassive;
        public int numFairChoices;

        public FunInfo(bool isModel, PType returnType, FuncTerm funDecl)
        {
            this.isModel = isModel;
            this.parameterNameToInfo = new Dictionary<string, VariableInfo>();
            this.parameterNames = new List<string>();
            this.returnType = returnType;
            this.funDecl = funDecl;
            this.callers = new HashSet<string>();
            this.numFairChoices = 0;
        }
    }

    internal class ActionInfo
    {
        public Node actionFun;
        public HashSet<PType> argTypes;
        public HashSet<string> callees;
        public bool atPassive;
        public int numFairChoices;

        public ActionInfo(Node actionFun)
        {
            this.actionFun = actionFun;
            this.callees = new HashSet<string>();
            this.argTypes = new HashSet<PType>();
            this.numFairChoices = 0;
        }
    }

    internal class MachineInfo
    {
        public bool IsReal { get { return type == "REAL"; } }
        public bool IsModel { get { return type == "MODEL"; } }
        public bool IsSpec { get { return type == "SPEC"; } }

        public string type;
        public int maxQueueSize;
        public FuncTerm initStateDecl;
        public Dictionary<string, StateInfo> stateNameToStateInfo;
        public Dictionary<string, VariableInfo> localVariableToVarInfo;
        public Dictionary<string, List<string>> eventSetNameToEvents;
        public Dictionary<string, FunInfo> funNameToFunInfo;
        public Dictionary<string, ActionInfo> actionFunNameToActionFun;
        public bool isInfinitelyOftenMonitor;

        public MachineInfo()
        {
            type = "REAL";
            maxQueueSize = -1;
            initStateDecl = null;
            stateNameToStateInfo = new Dictionary<string, StateInfo>();
            localVariableToVarInfo = new Dictionary<string, VariableInfo>();
            eventSetNameToEvents = new Dictionary<string, List<string>>();
            funNameToFunInfo = new Dictionary<string, FunInfo>();
            actionFunNameToActionFun = new Dictionary<string, ActionInfo>();
            isInfinitelyOftenMonitor = false;
        }
    }

    internal class EventInfo
    {
        public int maxInstances;  // -1 represents no bound
        public bool maxInstancesAssumed;
        public PType payloadType;

        public EventInfo(PType payloadType)
        {
            this.payloadType = payloadType;
            this.maxInstances = -1;
        }

        public EventInfo(int maxInstances, bool maxInstancesAssumed, PType payloadType)
        {
            this.maxInstances = maxInstances;
            this.maxInstancesAssumed = maxInstancesAssumed;
            this.payloadType = payloadType;
        }
    }

    internal class TypeInfo
    {
        public string cType;
        public string zingType;
    }

    class PToZing
    {
        public const string Con_LabeledExpr = "___InternalLabeledExpression";
        public static AST<FuncTerm> App_LabeledExpr = Factory.Instance.MkFuncTerm(Factory.Instance.MkId(Con_LabeledExpr));

        public const string SM_ARG_UNION = "SM_ARG_UNION";
        public const string SM_NULL = "SM_NULL";
        public const string SMF_PACKED_VALUE = "SMF_PACKED_VALUE";
        public const string SMF_ARRAYLIST = "SMF_ARRAYLIST";
        public const string SMF_HASHTABLE = "SMF_HASHTABLE";

        public const string NullEvent = "null";
        public const string DefaultEvent = "default";
        public const string DeleteEvent = "delete";

        private Dictionary<string, AST<FuncTerm>> modelAliases;
        private Dictionary<string, LinkedList<AST<FuncTerm>>> factBins;
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

        public FuncTerm GetFuncTerm(Node node)
        {
            if (node.NodeKind == NodeKind.FuncTerm)
                return (FuncTerm)node;
            else if (node.NodeKind == NodeKind.Id)
                return modelAliases[((Id)node).Name].Node;
            Debug.Assert(false);
            return null;
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

        public Dictionary<Node, ZingTranslationInfo> computedType;
        public Dictionary<string, EventInfo> allEvents;
        public Dictionary<string, MachineInfo> allMachines;
        public Dictionary<PType, TypeInfo> declaredTypes;
        public HashSet<PType> allTypes;
        public Dictionary<PType, List<PType>> subtypes, supertypes;
        public IEnumerable<PType> relatives(PType t)
        {
            var res = new List<PType>(supertypes[t]);
            res.AddRange(subtypes[t]);
            res.Add(t);
            return res;
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
                ti.cType = SMF_PACKED_VALUE;
                ti.zingType = SM_ARG_UNION;
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
                        var fName = ((Cnst)GetArgByIndex((FuncTerm)GetArgByIndex(nf, 0), 0)).GetStringValue();
                        var fType = GetPType(GetArgByIndex((FuncTerm)GetArgByIndex(nf, 0), 1));
                        fieldTypes.Add(new Tuple<string, PType>(fName, fType));
                        n = nf.Args.ElementAt(1);
                    }

                    var type = new PNamedTupleType(fieldTypes);
                    registerType(type);
                    return type;

                }

                if (fname == PData.Con_TypeSeq.Node.Name)
                {
                    var innerT = GetPType(GetArgByIndex((n as FuncTerm), 0));
                    var type = new PSeqType(innerT);
                    registerType(type);
                    return type;
                }

                if (fname == PData.Con_TypeMap.Node.Name)
                {
                    FuncTerm ft = n as FuncTerm;
                    var domain = GetPType(GetArgByIndex(ft, 0));
                    var range = GetPType(GetArgByIndex(ft, 1));
                    var type = new PMapType(domain, range);
                    registerType(type);
                    return type;
                }

                throw new NotImplementedException("Unknown complex type " + fname);
            }
        }

        Compiler compiler;
        public PToZing(Compiler compiler)
        {
            this.compiler = compiler;
        }

        #region Static helpers
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

        public static string getTupleField(int fNum)
        {
            return "field_" + fNum;
        }

        public static string getFuncArg(int fNum)
        {
            return "arg_" + fNum;
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

        private static AST<Node> MkZingDot(params string[] names)
        {
            AST<Node> lhs = MkZingIdentifier(names[0]);
            for (int i = 1; i < names.Length; i++)
            {
                AST<Node> rhs = MkZingIdentifier(names[i]);
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
        public bool GenerateZing(Env env, ref AST<Model> outModel)
        {
            List<AST<Node>> elements = new List<AST<Node>>();
            MkZingEnums(elements);
            MkZingClasses(elements);
            outModel = Add(outModel, MkZingFile("output.zing", elements));
            return true;
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
            var terms = GetBin("StateDecl");
            foreach (var term in terms)
            {
                var stateName = GetName(term.Node, 0);
                stateConsts.Add(Factory.Instance.MkCnst(string.Format("_{0}", stateName)));
            }
            var stateList = ConstructList(ZingData.App_EnumElems, stateConsts);
            elements.Add(AddArgs(ZingData.App_EnumDecl, Factory.Instance.MkCnst("State"), stateList));

            List<AST<Node>> actionFunConsts = new List<AST<Node>>();
            foreach (string machineName in allMachines.Keys)
            {
                foreach (string actionFunName in allMachines[machineName].actionFunNameToActionFun.Keys)
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
                var field = MkZingVarDecl(string.Format("{0}_SM_EVENT", eventName), ZingData.Cnst_SmEvent, ZingData.Cnst_Static);
                fields = AddArgs(ZingData.App_VarDecls, field, fields);
            }
            foreach (var machineName in allMachines.Keys)
            {
                var field = MkZingVarDecl(string.Format("{0}_instance", machineName), ZingData.Cnst_Int, ZingData.Cnst_Static);
                fields = AddArgs(ZingData.App_VarDecls, field, fields);
            }
            foreach (var machineName in allMachines.Keys)
            {
                if (!allMachines[machineName].IsSpec) continue;
                var field = MkZingVarDecl(GetMonitorMachineName(machineName), Factory.Instance.MkCnst(machineName), ZingData.Cnst_Static);
                fields = AddArgs(ZingData.App_VarDecls, field, fields);
            }
            AST<Node> methods = ZingData.Cnst_Nil;
            foreach (var machineName in allMachines.Keys)
            {
                AST<Node> method;
                if (allMachines[machineName].IsSpec)
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
                if (!allMachines[machineName].IsSpec) continue;
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
                if (allMachines[machineName].IsSpec)
                {
                    var assignStmt = MkZingAssign(MkZingIdentifier(GetMonitorMachineName(machineName)), MkZingIdentifier("null"));
                    runBody = MkZingSeq(runBody, assignStmt);
                }
            }
            var terms = GetBin("MainDecl");
            var locals = new List<AST<Node>>();
            foreach (var term in terms)
            {
                var ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, null, this);
                var mainConstructor = Factory.Instance.ToAST(GetArgByIndex(term.Node, 0)).Compute<ZingTranslationInfo>(
                    x => ZingEntryFun_UnFold(ctxt, x),
                    (x, ch) => ZingEntryFun_Fold(ctxt, x, ch));

                if (mainConstructor != null)
                {
                    locals.AddRange(ctxt.emitLocalsList());
                    Debug.Assert(ctxt.sideEffectsStack.Count == 1);

                    runBody = MkZingSeq(runBody, ctxt.emitZingSideEffects(mainConstructor.node));
                }
            }
            runBody = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("dummy"), runBody);
            runBody = ConstructList(ZingData.App_Blocks, runBody);
            AST<Node> runMethod = MkZingMethodDecl("run", ZingData.Cnst_Nil, ZingData.Cnst_Void, ConstructList(ZingData.App_VarDecls, locals), runBody,
                ZingData.Cnst_Static, ZingData.Cnst_Activate);
            methods = AddArgs(ZingData.App_MethodDecls, runMethod, methods);

            // Generate method for computing complement of a set of events
            AST<Node> calculateComplementParameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("eventSet", ZingData.Cnst_SmEventSet), ZingData.Cnst_Nil);
            AST<Node> calculateComplementLocalVars = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("returnEventSet", ZingData.Cnst_SmEventSet), ZingData.Cnst_Nil);
            AST<Node> calculateComplementBody = MkZingAssign(MkZingIdentifier("returnEventSet"), AddArgs(ZingData.App_New, ZingData.Cnst_SmEventSet, ZingData.Cnst_Nil));
            foreach (var eventName in allEvents.Keys)
            {
                if (eventName == DefaultEvent || eventName == DeleteEvent)
                    continue;
                var iteExpr = MkZingApply(ZingData.Cnst_In, MkZingEvent(eventName), MkZingIdentifier("eventSet"));
                var assignStmt = MkZingAssign(MkZingIdentifier("returnEventSet"), MkZingApply(ZingData.Cnst_Add, MkZingIdentifier("returnEventSet"), MkZingEvent(eventName)));
                var iteStmt = MkZingIfThenElse(iteExpr, ZingData.Cnst_Nil, assignStmt);
                calculateComplementBody = MkZingSeq(calculateComplementBody, iteStmt);
            }
            calculateComplementBody = MkZingSeq(calculateComplementBody, AddArgs(ZingData.App_Return, MkZingIdentifier("returnEventSet")));
            calculateComplementBody = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("dummy"), calculateComplementBody);
            calculateComplementBody = ConstructList(ZingData.App_Blocks, calculateComplementBody);
            AST<Node> calculateComplementMethod = MkZingMethodDecl("CalculateComplementOfEventSet", calculateComplementParameters, ZingData.Cnst_SmEventSet, calculateComplementLocalVars, calculateComplementBody, ZingData.Cnst_Static);
            methods = AddArgs(ZingData.App_MethodDecls, calculateComplementMethod, methods);

            // At this point, the set of Types mentioned anywhere in the program is collected.
            // Compute subtyping relations by checking all possible pairs of types.
            foreach (var t in allTypes)
            {
                subtypes[t] = new List<PType>();
                supertypes[t] = new List<PType>();
            }

            foreach (var t1 in allTypes)
            {
                foreach (var t2 in allTypes)
                {
                    if (t1 != t2 && t1.isSubtypeOf(t2))
                    {
                        subtypes[t2].Add(t1);
                        supertypes[t1].Add(t2);
                        Debug.Assert(   // Reminder to revisit this when adding new types.
                            t2 is PAnyType ||
                            (t2 is PTupleType && t1 is PTupleType) ||
                            (t2 is PNamedTupleType && t1 is PNamedTupleType) ||
                            (t2.IsMachineId && t1 is PNilType) ||
                            (t2 is PEventType && t1 is PNilType) ||
                            (t2 is PSeqType && t1 is PSeqType));
                    }
                }
            }

            // Generate the IsSubtype static method. IsSybtype :: (Discriminator, Discriminator) -> Bool
            // Allows the runtime to check whether one statically declared type is a subtype of another.
            var isSubBody = new List<AST<Node>>();
            isSubBody.Add(MkZingIfThenElse(MkZingEq(MkZingIdentifier("b"), pTypeToZingDiscriminator(PType.Any)),
                    AddArgs(ZingData.App_Return, ZingData.Cnst_True), ZingData.Cnst_Nil));

            foreach (var t in allTypes)
            {
                if (t is PAnyType)
                    continue;

                var ifBody = new List<AST<Node>>();
                ifBody.Add(MkZingIfThenElse(MkZingEq(MkZingIdentifier("a"), pTypeToZingDiscriminator(t)),
                        AddArgs(ZingData.App_Return, ZingData.Cnst_True), ZingData.Cnst_Nil));

                foreach (var subT in subtypes[t])
                {
                    ifBody.Add(MkZingIfThenElse(MkZingEq(MkZingIdentifier("a"), pTypeToZingDiscriminator(subT)),
                        AddArgs(ZingData.App_Return, ZingData.Cnst_True), ZingData.Cnst_Nil));
                }
                ifBody.Add(AddArgs(ZingData.App_Return, ZingData.Cnst_False));
                isSubBody.Add(MkZingIfThenElse(MkZingEq(MkZingIdentifier("b"), pTypeToZingDiscriminator(t)),
                    MkZingSeq(ifBody), ZingData.Cnst_Nil));
            }
            isSubBody.Add(AddArgs(ZingData.App_Return, ZingData.Cnst_False));
            AST<Node> isSubtypeMethod = MkZingMethodDecl("IsSubtype",
                MkZingVarDecls(MkZingVarDecl("a", Factory.Instance.MkCnst("Discriminator")), MkZingVarDecl("b", Factory.Instance.MkCnst("Discriminator"))),
                pTypeToZingType(PType.Bool), ZingData.Cnst_Nil, MkZingBlock("dummy", MkZingSeq(isSubBody)), ZingData.Cnst_Static);
            methods = AddArgs(ZingData.App_MethodDecls, isSubtypeMethod, methods);

            // Generate the PayloadOf static method. PayloadOf :: (EventId) -> Discriminator
            // Returns the statically declared type for the given event.
            var payloadOfBody = new List<AST<Node>>();

            // NULL Event
            payloadOfBody.Add(MkZingIfThen(MkZingEq(MkZingIdentifier("e"), MkZingIdentifier("null")),
                MkZingReturn(pTypeToZingDiscriminator(PType.Any))));

            foreach (var evt in allEvents.Keys.Where(x => x != NullEvent))
            {
                payloadOfBody.Add(MkZingIfThenElse(MkZingEq(MkZingDot("e", "name"), MkZingDot("Event", "_" + evt)),
                    AddArgs(ZingData.App_Return, pTypeToZingDiscriminator(allEvents[evt].payloadType)),
                    ZingData.Cnst_Nil));
            }

            payloadOfBody.Add(AddArgs(ZingData.App_Assert, ZingData.Cnst_False));
            AST<Node> payloadOfMethod = MkZingMethodDecl("PayloadOf",
                MkZingVarDecls(MkZingVarDecl("e", Factory.Instance.MkCnst("SM_EVENT"))),
                Factory.Instance.MkCnst("Discriminator"), ZingData.Cnst_Nil, MkZingBlock("dummy", MkZingSeq(payloadOfBody)), ZingData.Cnst_Static);
            methods = AddArgs(ZingData.App_MethodDecls, payloadOfMethod, methods);

            return AddArgs(ZingData.App_ClassDecl, Factory.Instance.MkCnst("Main"), fields, methods);
        }

        private AST<FuncTerm> GenerateMonitorClass(string machineName)
        {
            AST<Node> fields = LocalVariablesToVarDecls(allMachines[machineName].localVariableToVarInfo.Keys, allMachines[machineName].localVariableToVarInfo);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("myHandle", ZingData.Cnst_SpecHandle), fields);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("localActions", Factory.Instance.MkCnst("LocalActions")), fields);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("next", Factory.Instance.MkCnst(machineName)), fields);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("startState", Factory.Instance.MkCnst("State")), fields);

            AST<Node> methods = ZingData.Cnst_Nil;
            foreach (var stateDecl in allMachines[machineName].stateNameToStateInfo.Values.Select(x => x.stateDecl))
            {
                AST<Node> dequeueEventMethod = GenerateCalculateActionSetMethodDeclForMonitor(stateDecl);
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
                var entryFun = GetArgByIndex(stateInfo.Value.stateDecl, 2);
                var exitFun = stateInfo.Value.exitFun;
                methods = AddArgs(ZingData.App_MethodDecls, MkZingWrapperFn(entryFun, machineName, TranslationContext.Entry, stateName), methods);
                if (exitFun != null)
                    methods = AddArgs(ZingData.App_MethodDecls, MkZingWrapperFn(exitFun, machineName, TranslationContext.Exit, stateName), methods);
            }

            foreach (var actName in allMachines[machineName].actionFunNameToActionFun.Keys)
            {
                var actInfo = allMachines[machineName].actionFunNameToActionFun[actName];
                methods = AddArgs(ZingData.App_MethodDecls, MkZingWrapperFn(actInfo.actionFun, machineName, TranslationContext.Action, actName), methods);
            }

            return AddArgs(ZingData.App_ClassDecl, Factory.Instance.MkCnst(machineName), fields, methods);
        }

        private AST<FuncTerm> GenerateMachineClass(string machineName)
        {
            AST<Node> fields = LocalVariablesToVarDecls(allMachines[machineName].localVariableToVarInfo.Keys, allMachines[machineName].localVariableToVarInfo);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("myHandle", ZingData.Cnst_SmHandle), fields);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("stackStable", ZingData.Cnst_Bool), fields);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("stackDeferredSet", ZingData.Cnst_SmEventSet), fields);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("stackActionSet", ZingData.Cnst_SmEventSet), fields);
            fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("localActions", Factory.Instance.MkCnst("LocalActions")), fields);

            AST<Node> methods = ZingData.Cnst_Nil;
            foreach (var stateDecl in allMachines[machineName].stateNameToStateInfo.Values.Select(x => x.stateDecl))
            {
                AST<Node> dequeueEventMethod = GenerateCalculateDeferredAndActionSetMethodDecl(stateDecl);
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
                var entryFun = GetArgByIndex(stateInfo.Value.stateDecl, 2);
                var exitFun = stateInfo.Value.exitFun;
                methods = AddArgs(ZingData.App_MethodDecls, MkZingWrapperFn(entryFun, machineName, TranslationContext.Entry, stateName), methods);
                if (exitFun != null)
                    methods = AddArgs(ZingData.App_MethodDecls, MkZingWrapperFn(exitFun, machineName, TranslationContext.Exit, stateName), methods);
            }

            foreach (var actName in allMachines[machineName].actionFunNameToActionFun.Keys)
            {
                var actInfo = allMachines[machineName].actionFunNameToActionFun[actName];
                methods = AddArgs(ZingData.App_MethodDecls, MkZingWrapperFn(actInfo.actionFun, machineName, TranslationContext.Action, actName), methods);
            }

            if (compiler.liveness)
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
                foreach (var actionName in allMachines[machineName].actionFunNameToActionFun.Keys)
                {
                    var actionInfo = allMachines[machineName].actionFunNameToActionFun[actionName];
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

        private AST<FuncTerm> GenerateUnionClass()
        {
            var anyMemberTypes = allTypes.Where(t => !(t is PAnyType));
            var anyVars = new List<AST<Node>>();
            var anyMethods = new List<AST<Node>>();
            anyVars.Add(MkZingVarDecl("d", Factory.Instance.MkCnst("Discriminator")));
            anyVars.AddRange(anyMemberTypes.Select(type => MkZingVarDecl(pTypeToZingUnionMember(type), pTypeToZingType(type))));


            // Make BuildDefault
            var body = new List<AST<Node>>();
            var locals = new List<AST<Node>>();
            locals.Add(MkZingVarDecl("result", Factory.Instance.MkCnst(SM_ARG_UNION)));
            body.Add(MkZingAssign(MkZingIdentifier("result"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst(SM_ARG_UNION), ZingData.Cnst_Nil)));
            body.Add(MkZingAssign(MkZingDot("result", "d"), pTypeToZingDiscriminator(PType.Nil)));

            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "BuildDefault", this);
            foreach (var t in anyMemberTypes)
            {
                body.Add(ctxt.emitZingSideEffects(MkZingAssign(MkZingDot("result", pTypeToZingUnionMember(t)),
                    t is PPrimitiveType ? getZingDefault(ctxt, t) : MkZingIdentifier("null"))));
                ctxt.pushSideEffectStack();
            }

            locals.AddRange(ctxt.emitLocalsList());
            body.Add(AddArgs(ZingData.App_Return, MkZingIdentifier("result")));
            anyMethods.Add(MkZingMethodDecl("BuildDefault", ZingData.Cnst_Nil, Factory.Instance.MkCnst(SM_ARG_UNION),
                ConstructList(ZingData.App_VarDecls, locals), MkZingBlock("dummy", MkZingSeq(body)), ZingData.Cnst_Static));

            // Emit Clone Method
            locals = new List<AST<Node>>();
            body = new List<AST<Node>>();

            locals.Add(MkZingVarDecl("result", Factory.Instance.MkCnst(SM_ARG_UNION)));
            body.Add(MkZingAssign(MkZingIdentifier("result"), MkZingCall(MkZingDot(SM_ARG_UNION, "BuildDefault"))));
            body.Add(MkZingAssign(MkZingDot("result", "d"), MkZingDot("this", "d")));

            foreach (var t in anyMemberTypes)
            {
                var thisM = MkZingDot("result", pTypeToZingUnionMember(t));
                var otherM = MkZingDot("this", pTypeToZingUnionMember(t));
                body.Add(MkZingIfThen(MkZingEq(MkZingDot("result", "d"), pTypeToZingDiscriminator(t)), MkZingAssignOrCast(thisM, t, otherM, t)));
            }

            body.Add(AddArgs(ZingData.App_Return, MkZingIdentifier("result")));
            anyMethods.Add(MkZingMethodDecl("Clone", ZingData.Cnst_Nil, Factory.Instance.MkCnst(SM_ARG_UNION),
                ConstructList(ZingData.App_VarDecls, locals), MkZingBlock("dummy", MkZingSeq(body))));

            // Emit Equals Method. Note: Equality is aware of subtyping. For example, Any values containing values of types
            // a:(any, any) and b:(int, int) are considered equal if they both contain the concrete value (1,1).
            var equalsBody = new List<AST<Node>>();
            var eqCtxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Equals", this);

            foreach (var t in anyMemberTypes)
            {
                var thisM = MkZingDot("this", pTypeToZingUnionMember(t));
                var otherD = MkZingDot("other", "d");
                var ifBody = new List<AST<Node>>();

                foreach (var otherT in relatives(t).Where(tp => !(tp is PAnyType)))
                {
                    ifBody.Add(MkZingIfThen(MkZingEq(otherD, pTypeToZingDiscriminator(otherT)),
                        eqCtxt.emitZingSideEffects(MkZingReturn(MkZingEq(eqCtxt, thisM, t, MkZingDot("other", pTypeToZingUnionMember(otherT)), otherT)))));
                    eqCtxt.pushSideEffectStack();
                }

                equalsBody.Add(MkZingIfThen(MkZingEq(MkZingDot("this", "d"), pTypeToZingDiscriminator(t)), MkZingSeq(ifBody)));
            }

            equalsBody.Add(MkZingReturn(ZingData.Cnst_False));

            anyMethods.Add(MkZingMethodDecl("Equals", MkZingVarDecls(MkZingVarDecl("other", Factory.Instance.MkCnst(SM_ARG_UNION))),
                ZingData.Cnst_Bool, eqCtxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(equalsBody))));

            // Emit UpCast,Downcast and Equals Methods
            foreach (var t in anyMemberTypes)
            {
                // Emit the Equals_<T> Method
                ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, getZingEqualsName(t), this);
                var equalsSubBody = new List<AST<Node>>();

                foreach (var otherT in relatives(t).Where(tp => !(tp is PAnyType)))
                {
                    equalsSubBody.Add(MkZingIfThen(MkZingEq(MkZingDot("this", "d"), pTypeToZingDiscriminator(otherT)),
                        ctxt.emitZingSideEffects(MkZingReturn(MkZingEq(ctxt, MkZingDot("this", pTypeToZingUnionMember(otherT)), otherT, MkZingIdentifier("other"), t)))));
                    ctxt.pushSideEffectStack();
                }

                equalsSubBody.Add(MkZingReturn(ZingData.Cnst_False));
                anyMethods.Add(MkZingMethodDecl(getZingEqualsName(t),
                    MkZingVarDecls(MkZingVarDecl("other", pTypeToZingType(t))), pTypeToZingType(PType.Bool),
                    ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(equalsSubBody))));

                // Emit the UpCastFrom Method
                var upcastBody = MkZingSeq(
                    MkZingAssign(MkZingIdentifier("result"), MkZingCall(MkZingDot(SM_ARG_UNION, "BuildDefault"))),
                    MkZingAssign(MkZingDot("result", "d"), pTypeToZingDiscriminator(t)),
                    MkZingAssignOrCast(MkZingDot("result", pTypeToZingUnionMember(t)), t, MkZingIdentifier("other"), t),
                    AddArgs(ZingData.App_Return, MkZingIdentifier("result")));

                anyMethods.Add(MkZingMethodDecl(getZingUpCastName(t),
                    MkZingVarDecls(MkZingVarDecl("other", pTypeToZingType(t))), pTypeToZingType(PType.Any),
                    MkZingVarDecls(MkZingVarDecl("result", pTypeToZingType(PType.Any))),
                    MkZingBlock("dummy", upcastBody), ZingData.Cnst_Static));

                // Emit the DownCastTo Method
                var downcastBody = new List<AST<Node>>();
                ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, getZingDownCastName(t), this);

                var resVar = ctxt.getTmpVar(t, "tmpRes");
                foreach (var midT in relatives(t).Where(tp => !(tp is PAnyType)))
                {
                    downcastBody.Add(MkZingIfThen(MkZingEq(MkZingDot("this", "d"), pTypeToZingDiscriminator(midT)),
                        MkZingSeq(
                            MkZingAssignOrCast(resVar, t, MkZingDot("this", pTypeToZingUnionMember(midT)), midT),
                            AddArgs(ZingData.App_Return, resVar))));

                }
                downcastBody.Add(MkZingAssert(ZingData.Cnst_False));

                anyMethods.Add(MkZingMethodDecl(getZingDownCastName(t),
                    ZingData.Cnst_Nil, pTypeToZingType(t), ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(downcastBody))));

                // Emit the CanDownCastTo Method
                var candowncastBody = new List<AST<Node>>();
                ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, getZingDownCastName(t), this);

                foreach (var midT in supertypes[t].Where(tp => !(tp is PAnyType)))
                {
                    candowncastBody.Add(MkZingIfThenElse(MkZingEq(MkZingDot("this", "d"), pTypeToZingDiscriminator(midT)),
                        ctxt.emitZingSideEffects(AddArgs(ZingData.App_Return, MkZingCanDownCastTo(ctxt, MkZingDot("this", pTypeToZingUnionMember(midT)), midT, t))),
                        ZingData.Cnst_Nil));
                    ctxt.pushSideEffectStack();
                }

                foreach (var subT in subtypes[t])
                {
                    candowncastBody.Add(MkZingIfThenElse(MkZingEq(MkZingDot("this", "d"), pTypeToZingDiscriminator(subT)),
                            AddArgs(ZingData.App_Return, ZingData.Cnst_True), ZingData.Cnst_Nil));
                }

                candowncastBody.Add(AddArgs(ZingData.App_Return, MkZingEq(MkZingDot("this", "d"), pTypeToZingDiscriminator(t))));
                anyMethods.Add(MkZingMethodDecl(getZingCanDownCastName(t),
                    ZingData.Cnst_Nil, pTypeToZingType(PType.Bool), ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(candowncastBody))));
            }

            // Emit the CanCastTo(Discriminator t) method
            var canCastBody = new List<AST<Node>>();
            var canCastCtxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "CanCastTo", this);

            var tmpEq = canCastCtxt.getTmpVar(PType.Bool, "tmpEq");

            // this.d == toT
            canCastBody.Add(MkZingIfThen(MkZingEq(MkZingIdentifier("toT"), MkZingDot("this", "d")), MkZingReturn(ZingData.Cnst_True)));
            // this.d is a subtype of toT
            canCastBody.Add(MkZingAssign(tmpEq, MkZingCall(MkZingDot("Main", "IsSubtype"), MkZingDot("this", "d"), MkZingIdentifier("toT"))));
            canCastBody.Add(MkZingIfThen(tmpEq, MkZingReturn(ZingData.Cnst_True)));
            // this.d is a supertype of toT, and we can downcast to it.
            foreach (var t in anyMemberTypes)
            {
                canCastBody.Add(MkZingIfThen(MkZingEq(MkZingIdentifier("toT"), pTypeToZingDiscriminator(t)),
                    MkZingSeq(
                        MkZingAssign(tmpEq, MkZingCall(MkZingDot("this", getZingCanDownCastName(t)))),
                        MkZingReturn(tmpEq))));
            }

            canCastBody.Add(MkZingAssert(ZingData.Cnst_False)); // Sanity Check that we don't make it there.
            anyMethods.Add(MkZingMethodDecl("CanCastTo", MkZingVarDecls(MkZingVarDecl("toT", Factory.Instance.MkCnst("Discriminator"))),
                pTypeToZingType(PType.Bool), canCastCtxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(canCastBody))));

            return AddArgs(ZingData.App_ClassDecl, Factory.Instance.MkCnst(SM_ARG_UNION),
                MkZingVarDecls(anyVars), ConstructList(ZingData.App_MethodDecls, anyMethods));
        }

        private void MkZingClasses(List<AST<Node>> elements)
        {
            foreach (string machineName in allMachines.Keys)
            {
                if (allMachines[machineName].IsSpec)
                {
                    elements.Add(GenerateMonitorClass(machineName));
                }
                else
                {
                    elements.Add(GenerateMachineClass(machineName));
                }
            }

            elements.Add(GenerateMainClass());
    
            // Emit class declarations for each Tuple/Named/Seq Tuple
            foreach (PType t in declaredTypes.Keys)
            {
                if (t is PTupleType || t is PNamedTupleType)
                    elements.Add(MkZingTupleClassDefinition(getFieldDescriptions(t), pTypeToZingType(t), declaredTypes[t].zingType, t));
                else if (t is PSeqType)
                {
                    PSeqType seqT = t as PSeqType;
                    elements.Add(MkZingSeqClassDefinition(seqT));
                    elements.Add(AddArgs(ZingData.App_ArrayDecl, Factory.Instance.MkCnst(pTypeToZingName(seqT.T) + "_array"),
                        pTypeToZingType(seqT.T)));
                }
                else if (t is PMapType)
                {
                    PMapType mapT = t as PMapType;
                    AST<FuncTerm> mapClass, mapEntryClass;
                    MkZingMapClassDefinition(mapT, out mapClass, out mapEntryClass);
                    elements.Add(mapClass);
                    elements.Add(mapEntryClass);
                }
                else if (!(t is PAnyType))
                    throw new NotImplementedException("Unknown complex type " + t);
            }

            // Emit the Discriminator enum
            elements.Add(AddArgs(ZingData.App_EnumDecl, Factory.Instance.MkCnst("Discriminator"),
                ConstructList(ZingData.App_EnumElems, allTypes.Select(type => Factory.Instance.MkCnst(pTypeToZingName(type))))));

            // Emit the SM_ARG_UNION class
            elements.Add(GenerateUnionClass());
        }

        private AST<Node> MkZingNeq(ZingEntryFun_FoldContext ctxt, AST<Node> e1, PType t1, AST<Node> e2, PType t2)
        {
            return MkZingApply(ZingData.Cnst_Not, MkZingEq(ctxt, e1, t1, e2, t2));
        }

        private AST<Node> MkZingEq(ZingEntryFun_FoldContext ctxt, AST<Node> e1, PType t1, AST<Node> e2, PType t2)
        {
            AST<Node> sub, super;
            PType subT, supT;

            if (t1 == t2)
            {
                if (t1 is PNilType)
                {
                    return ZingData.Cnst_True;
                }
                else if (t1 is PPrimitiveType)
                    return MkZingApply(ZingData.Cnst_Eq, e1, e2);
                else
                {
                    var tmpEqVar = ctxt.getTmpVar(PType.Bool, "eqVar");
                    ctxt.addSideEffect(MkZingAssign(tmpEqVar, MkZingCall(MkZingDot(e1, "Equals"), e2)));
                    return tmpEqVar;
                }
            }
            else
            {
                if (t1.isSubtypeOf(t2))
                {
                    super = e2;
                    sub = e1;
                    subT = t1;
                    supT = t2;
                }
                else
                {
                    Debug.Assert(t2.isSubtypeOf(t1));
                    super = e1;
                    sub = e2;
                    subT = t2;
                    supT = t1;
                }

                // Special case Null Events/Machine Ids
                if (supT is PIdType && subT is PNilType)
                {
                    return MkZingEq(super, getZingDefault(ctxt, PType.Id));
                }
                else if (supT is PMidType && subT is PNilType)
                {
                    return MkZingEq(super, getZingDefault(ctxt, PType.Mid));
                }
                else if (supT is PEventType && subT is PNilType)
                {
                    return MkZingEq(super, getZingDefault(ctxt, PType.Event));
                }
                else
                {
                    var tmpEqVar = ctxt.getTmpVar(PType.Bool, "eqVar");
                    ctxt.addSideEffect(MkZingAssign(tmpEqVar, MkZingCall(MkZingDot(super, getZingEqualsName(subT)), sub)));
                    return tmpEqVar;
                }
            }
        }

        private AST<Node> MkZingCanDownCastTo(ZingEntryFun_FoldContext ctxt, AST<Node> from, PType fromT, PType toT)
        {
            if (fromT is PAnyType || fromT is PCompoundType || fromT is PSeqType)
            {
                var tmpVar = ctxt.getTmpVar(PType.Bool, "tmpEq");
                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(from, getZingCanDownCastName(toT)))));

                return tmpVar;
            }
            else
            {
                Debug.Assert(fromT.IsMachineId || fromT is PEventType);
                return MkZingEq(from, MkZingIdentifier("null"));
            }
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

        private AST<FuncTerm> MkZingVarDecl(string varName, PType varType, params AST<Node>[] attrs)
        {
            return MkZingVarDecl(varName, pTypeToZingType(varType), attrs);
        }

        private AST<Node> GenerateCalculateDeferredAndActionSetMethodDecl(FuncTerm stateDecl)
        {
            var stateName = GetName(stateDecl, 0);
            AST<Node> parameters = ZingData.Cnst_Nil;
            parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("currentActionSet", ZingData.Cnst_SmEventSet), parameters);
            parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("currentDeferredSet", ZingData.Cnst_SmEventSet), parameters);

            List<AST<FuncTerm>> stmts = new List<AST<FuncTerm>>();
            var smEventSetType = Factory.Instance.MkCnst("SM_EVENT_SET");
            var stackDeferredSet = MkZingIdentifier("stackDeferredSet");
            var stackActionSet = MkZingIdentifier("stackActionSet");
            var currentDeferredSet = MkZingIdentifier("currentDeferredSet");
            var currentActionSet = MkZingIdentifier("currentActionSet");
            var deferredSetDecl = GetArgByIndex(stateDecl, 3);
            var ownerName = GetOwnerName(stateDecl, 1, 0);

            List<string> localDeferredSet = new List<string>();
            if (deferredSetDecl.NodeKind == NodeKind.Id && ((Id)deferredSetDecl).Name != PData.Cnst_Nil.Node.Name)
            {
                string eventSetName, machineName;
                DeconstructEventSetDecl(GetFuncTerm(deferredSetDecl), out eventSetName, out machineName);
                localDeferredSet = allMachines[machineName].eventSetNameToEvents[eventSetName];
            }

            var actions = allMachines[ownerName].stateNameToStateInfo[stateName].actions;
            var transitions = allMachines[ownerName].stateNameToStateInfo[stateName].transitions;

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
                stmts.Add(MkZingAssign(MkZingApply(ZingData.Cnst_Index, MkZingDot("localActions", "as"), Factory.Instance.MkCnst(count)), MkZingAction(ownerName, actions[eventName])));
                count = count + 1;
            }

            var body = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("dummy"), MkZingSeq(stmts.ToArray()));
            return MkZingMethodDecl(string.Format("{0}_CalculateDeferredAndActionSet", stateName), parameters, ZingData.Cnst_Void, ZingData.Cnst_Nil, ConstructList(ZingData.App_Blocks, body));
        }

        private AST<Node> GenerateCalculateActionSetMethodDeclForMonitor(FuncTerm stateDecl)
        {
            var stateName = GetName(stateDecl, 0);
            AST<Node> parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("currentActionSet", ZingData.Cnst_SmEventSet), ZingData.Cnst_Nil);

            List<AST<FuncTerm>> stmts = new List<AST<FuncTerm>>();
            var smEventSetType = Factory.Instance.MkCnst("SM_EVENT_SET");
            var currentActionSet = MkZingIdentifier("currentActionSet");
            var ownerName = GetOwnerName(stateDecl, 1, 0);

            var actions = allMachines[ownerName].stateNameToStateInfo[stateName].actions;
            var transitions = allMachines[ownerName].stateNameToStateInfo[stateName].transitions;

            AddEventSet(stmts, actions.Keys, currentActionSet);
            SubtractEventSet(stmts, transitions.Keys, currentActionSet);

            stmts.Add(MkZingAssign(MkZingDot("localActions", "es"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("SM_EVENT_array"), Factory.Instance.MkCnst(actions.Count))));
            stmts.Add(MkZingAssign(MkZingDot("localActions", "as"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("ActionFun_array"), Factory.Instance.MkCnst(actions.Count))));
            int count = 0;
            foreach (var eventName in actions.Keys)
            {
                stmts.Add(MkZingAssign(MkZingApply(ZingData.Cnst_Index, MkZingDot("localActions", "es"), Factory.Instance.MkCnst(count)), MkZingEvent(eventName)));
                stmts.Add(MkZingAssign(MkZingApply(ZingData.Cnst_Index, MkZingDot("localActions", "as"), Factory.Instance.MkCnst(count)), MkZingAction(ownerName, actions[eventName])));
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
                    MkZingVarDecl("currentDeferredSet", ZingData.Cnst_SmEventSet)
                    );

            FuncTerm initStateDecl = allMachines[machineName].initStateDecl;
            var callStmt = MkZingCallStmt(
                            MkZingCall(MkZingIdentifier("runHelper"),
                                       MkZingDot("State", string.Format("_{0}", GetName(initStateDecl, 0)))));

            var currentDeferredSet = MkZingIdentifier("currentDeferredSet");
            var smEventSetType = Factory.Instance.MkCnst("SM_EVENT_SET");
            var iteStmt = MkZingIfThenElse(MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingEvent("delete")),
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
                                                 MkZingVarDecl("currentActionSet", ZingData.Cnst_SmEventSet)
                                                 );
            List<AST<Node>> locals = new List<AST<Node>>();
            locals.Add(MkZingVarDecl("cont", Factory.Instance.MkCnst("Continuation")));

            var cont = MkZingIdentifier("cont");
            AST<Node> initStmt = Factory.Instance.AddArg(ZingData.App_Assert, ZingData.Cnst_False);
            foreach (var actionFunName in allMachines[machineName].actionFunNameToActionFun.Keys)
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
            foreach (var actionFunName in allMachines[machineName].actionFunNameToActionFun.Keys)
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
                                                 MkZingVarDecl("currentActionSet", ZingData.Cnst_SmEventSet),
                                                 MkZingVarDecl("currentStable", ZingData.Cnst_Bool)
                                                 );

            List<AST<Node>> locals = new List<AST<Node>>();

            locals.Add(MkZingVarDecl("savedStable", ZingData.Cnst_Bool));
            locals.Add(MkZingVarDecl("savedDeferredSet", ZingData.Cnst_SmEventSet));
            locals.Add(MkZingVarDecl("savedActionSet", ZingData.Cnst_SmEventSet));
            locals.Add(MkZingVarDecl("savedCurrentEvent", ZingData.Cnst_SmEvent));
            locals.Add(MkZingVarDecl("savedCurrentArg", ZingData.Cnst_SmUnion));
            locals.Add(MkZingVarDecl("cont", Factory.Instance.MkCnst("Continuation")));
            if (compiler.liveness)
            {
                locals.Add(MkZingVarDecl("gateProgress", ZingData.Cnst_Bool));
            }

            AST<Node> initStmt = Factory.Instance.AddArg(ZingData.App_Assert, ZingData.Cnst_False);
            foreach (var actionFunName in allMachines[machineName].actionFunNameToActionFun.Keys)
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
            foreach (var actionFunName in allMachines[machineName].actionFunNameToActionFun.Keys)
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
            body.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Leave")),
                MkZingSeq(
                type == TranslationContext.Action ? MkZingReturn(ZingData.Cnst_False) : type == TranslationContext.Exit ? (AST<Node>)ZingData.Cnst_Nil : AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst("wait_" + entityName)))));
            body.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Return")),
                MkZingSeq(
                type == TranslationContext.Action ? AddArgs(ZingData.App_Return, ZingData.Cnst_True) : type == TranslationContext.Exit ? MkZingAssert(ZingData.Cnst_False) : AddArgs(ZingData.App_Return, ZingData.Cnst_Nil))));
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
                    MkZingAssignOrCast(currentEvent, PType.Event, MkZingIdentifier("null"), PType.Nil),
                    MkZingAssignOrCast(currentArg, PType.Any, MkZingIdentifier("null"), PType.Nil),
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
            if (compiler.liveness)
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
            body.Add(MkZingIfThen(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "NewM")),
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

            locals.Add(MkZingVarDecl("currentDeferredSet", ZingData.Cnst_SmEventSet));
            locals.Add(MkZingVarDecl("currentActionSet", ZingData.Cnst_SmEventSet));
            locals.Add(MkZingVarDecl("savedDeferredSet", ZingData.Cnst_SmEventSet));
            locals.Add(MkZingVarDecl("savedActionSet", ZingData.Cnst_SmEventSet));
            locals.Add(MkZingVarDecl("actionFun", Factory.Instance.MkCnst("ActionFun")));
            locals.Add(MkZingVarDecl("cont", Factory.Instance.MkCnst("Continuation")));
            locals.Add(MkZingVarDecl("savedCurrentEvent", ZingData.Cnst_SmEvent));
            locals.Add(MkZingVarDecl("savedCurrentArg", ZingData.Cnst_SmUnion));
            locals.Add(MkZingVarDecl("didActionPop", ZingData.Cnst_Bool));
            locals.Add(MkZingVarDecl("currentStable", ZingData.Cnst_Bool));
            locals.Add(MkZingVarDecl("savedStable", ZingData.Cnst_Bool));
            if (compiler.liveness)
            {
                locals.Add(MkZingVarDecl("gateProgress", ZingData.Cnst_Bool));
            }

            // Initial block
            AST<Node> initStmt = Factory.Instance.AddArg(ZingData.App_Assert, ZingData.Cnst_False);
            foreach (var stateDecl in allMachines[machineName].stateNameToStateInfo.Values.Select(x => x.stateDecl))
            {
                var stateName = GetName(stateDecl, 0);
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
            locals.Add(MkZingVarDecl("currentActionSet", ZingData.Cnst_SmEventSet));
            locals.Add(MkZingVarDecl("actionFun", Factory.Instance.MkCnst("ActionFun")));
            locals.Add(MkZingVarDecl("cont", Factory.Instance.MkCnst("Continuation")));
            locals.Add(MkZingVarDecl("gateProgress", ZingData.Cnst_Bool));

            var cont = MkZingIdentifier("cont");

            // Initial block
            AST<Node> initStmt = Factory.Instance.AddArg(ZingData.App_Assert, ZingData.Cnst_False);
            foreach (var stateDecl in allMachines[machineName].stateNameToStateInfo.Values.Select(x => x.stateDecl))
            {
                var stateName = GetName(stateDecl, 0);
                var stateExpr = MkZingDot("State", string.Format("_{0}", stateName));
                var condExpr = MkZingApply(ZingData.Cnst_Eq, MkZingIdentifier("startState"), stateExpr);
                var gotoStmt = Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst("transition_" + stateName));
                initStmt = MkZingIfThenElse(condExpr, gotoStmt, initStmt);
            }
            string initStateName = GetName(allMachines[machineName].initStateDecl, 0);
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
                if (compiler.liveness)
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
                else if (compiler.maceLiveness)
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

        enum TranslationContext { Action, Entry, Exit, Function };
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
                var tmpVar = getTmpVar(PType.Int, "retTo");

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

            public void addLocal(AST<Node> t, string name)
            {
                this.locals.Add(new Tuple<AST<Node>, string>(t, name));
            }

            public void pushSideEffectStack()
            {
                this.sideEffectsStack.Push(new List<AST<Node>>());
            }

            public List<AST<Node>> popSideEffectStack()
            {
                return this.sideEffectsStack.Pop();
            }

            public AST<Node> getTmpVar(PType t, string baseName)
            {
                return getTmpVar(pToZing.pTypeToZingType(t), baseName);
            }

            public AST<Node> getTmpVar(AST<Node> type, string baseName)
            {
                var tmpVarName = pToZing.getUnique(baseName);
                var tmpVar = MkZingIdentifier(tmpVarName);
                addLocal(type, tmpVarName);

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

            public IEnumerable<AST<Node>> emitLocalsList()
            {
                return locals.Select(loc => PToZing.MkZingVarDecl(loc.Item2, loc.Item1));
            }

            public AST<Node> emitLocals()
            {
                return ConstructList(ZingData.App_VarDecls, emitLocalsList());
            }

            public string prettyName()
            {
                if (this.translationContext == TranslationContext.Function)
                {
                    return "function " + this.entityName;
                }
                else if (this.translationContext == TranslationContext.Exit)
                {
                    return "exit function in state " + this.entityName;
                }
                else if (this.translationContext == TranslationContext.Entry)
                {
                    return "entry function in state " + this.entityName;
                }
                else
                {
                    return "action " + this.entityName;
                }
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
            if (funName == PData.Con_Exprs.Node.Name)
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
                    yield return AddArgs(App_LabeledExpr, Factory.Instance.ToAST(GetArgByIndex(ft, 0)), Factory.Instance.ToAST(GetArgByIndex(ft, 1))).Node;
                    ft = GetArgByIndex(ft, 2) as FuncTerm;
                }
                while (ft != null);
            }
            else if (funName == Con_LabeledExpr)
            {
                yield return GetArgByIndex(ft, 1);
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
            else if (funName == PData.Con_DataOp.Node.Name)
            {
                foreach (var a in ZingEntryFun_UnFold(ctxt, GetArgByIndex(ft, 1)))
                {
                    yield return a;
                }
            }
            else if (funName == PData.Con_Apply.Node.Name || funName == PData.Con_Call.Node.Name)
            {
                if (funName == PData.Con_Call.Node.Name)
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
            else if (funName == PData.Con_ITE.Node.Name)
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
            else if (funName == PData.Con_Strings.Node.Name)
            {
                do
                {
                    yield return ft.Args.First<Node>();
                    ft = GetArgByIndex(ft, 1) as FuncTerm;
                }
                while (ft != null);
            }
            else
            {
                foreach (var t in ft.Args)
                {
                    yield return t;
                }
            }
        }

        private AST<Node> emitZingSideEffects(List<AST<Node>> sideEffects, AST<Node> stmt)
        {
            if (sideEffects.Count > 0)
            {
                sideEffects.Add(stmt);
                return MkZingSeq(sideEffects);
            }
            else
            {
                return stmt;
            }
        }

        private AST<Node> generatePayloadCastAsserts(ZingEntryFun_FoldContext ctxt, PType type)
        {
            AST<Node> assert = ZingData.Cnst_Nil;

            if (!(type is PAnyType))
            {
                assert = ctxt.emitZingSideEffects(MkZingAssert(MkZingCanDownCastTo(ctxt, MkZingDot("myHandle", "currentArg"), PType.Any, type)));
                ctxt.pushSideEffectStack();
            }

            if (!(type is PNilType))
            {
                assert = MkZingSeq(MkZingAssert(MkZingNeq(MkZingDot("myHandle", "currentArg"), MkZingIdentifier("null"))), assert);
            }

            return assert;
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

        private AST<Node> MkZingAssignOrCast(AST<Node> lhs, PType lhsType, AST<Node> rhs, PType rhsType)
        {
            AST<Node> rhsNode;

            if (lhsType == rhsType)
            {
                rhsNode = (rhsType is PPrimitiveType) ? rhs : MkZingCall(MkZingDot(rhs, "Clone"));
            }
            else if (rhsType.isSubtypeOf(lhsType))
            {   // UPCAST
                if (lhsType is PTupleType || lhsType is PNamedTupleType || lhsType is PAnyType || lhsType is PSeqType)
                {
                    rhsNode = MkZingCall(MkZingDot(pTypeToZingClassName(lhsType), getZingUpCastName(rhsType)), rhs);
                }
                else if (lhsType.IsMachineId || lhsType is PEventType)
                {
                    Debug.Assert(rhsType is PNilType);
                    rhsNode = MkZingIdentifier("null");
                }
                else
                    throw new NotImplementedException(string.Format("Unknown LHS Type {0} in assignment.", lhsType));
            }
            else if (lhsType.isSubtypeOf(rhsType))
            {   // DOWNCAST
                if (!(lhsType is PNilType))
                {
                    rhsNode = MkZingCall(MkZingDot(rhs, getZingDownCastName(lhsType)));
                }
                else
                {
                    rhsNode = MkZingIdentifier("null");
                }
            }
            else
                throw new Exception(string.Format("Cannot assign from type {0} to type {1}", rhsType, lhsType));

            return AddArgs(ZingData.App_Assign, lhs, rhsNode);
        }

        private ZingTranslationInfo ZingEntryFun_Fold(ZingEntryFun_FoldContext ctxt, Node n, IEnumerable<ZingTranslationInfo> children)
        {
            var res = ZingEntryFun_Fold_Impl(ctxt, n, children);
            if (res != null)
                computedType[n] = res;

            return res;
        }

        private ZingTranslationInfo ZingEntryFun_Fold_Impl(ZingEntryFun_FoldContext ctxt, Node n, IEnumerable<ZingTranslationInfo> children)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                if (n.NodeKind == NodeKind.Cnst)
                {
                    Cnst cnst = (Cnst)n;
                    if (cnst.CnstKind == CnstKind.String)
                        return new ZingTranslationInfo(Factory.Instance.ToAST(n), new PNilType());
                    else
                        return new ZingTranslationInfo(Factory.Instance.ToAST(n), new PIntType());
                }
                else if (n.NodeKind == NodeKind.Id)
                {
                    var id = (Id)n;
                    if (id.Name == PData.Cnst_This.Node.Name)
                    {
                        MachineInfo machineInfo = allMachines[ctxt.machineName];
                        if (machineInfo.IsSpec)
                        {
                            Debug.Assert(false, string.Format("THIS disallowed in spec machine."));
                            return null;
                        }
                        else
                        {
                            return new ZingTranslationInfo(MkZingIdentifier("myHandle"), machineInfo.IsModel ? (PType)new PMidType() : (PType)new PIdType());
                        }
                    }
                    else if (id.Name == PData.Cnst_Trigger.Node.Name)
                    {
                        if (ctxt.translationContext == TranslationContext.Function)
                        {
                            Debug.Assert(false, string.Format("TRIGGER disallowed in body of function {0}.", ctxt.entityName));
                            return null;
                        }
                        return new ZingTranslationInfo(MkZingDot("myHandle", "currentEvent"), new PEventType());
                    }
                    else if (id.Name == PData.Cnst_Nil.Node.Name)
                    {
                        return new ZingTranslationInfo(ZingData.Cnst_Nil, new PNilType());
                    }
                    else if (id.Name == PData.Cnst_True.Node.Name)
                    {
                        return new ZingTranslationInfo(ZingData.Cnst_True, new PBoolType());
                    }
                    else if (id.Name == PData.Cnst_False.Node.Name)
                    {
                        return new ZingTranslationInfo(ZingData.Cnst_False, new PBoolType());
                    }
                    else if (id.Name == PData.Cnst_Nondet.Node.Name || id.Name == PData.Cnst_FairNondet.Node.Name)
                    {
                        if (allMachines[ctxt.machineName].IsSpec)
                        {
                            Debug.Assert(false, string.Format("A spec machine cannot use nondeterministic choice."));
                            return null;
                        }
                        if (!allMachines[ctxt.machineName].IsReal ||
                            (ctxt.translationContext == TranslationContext.Function && allMachines[ctxt.machineName].funNameToFunInfo[ctxt.entityName].isModel))
                        {
                            var afterLabel = ctxt.getFreshLabel();
                            var bvar = ctxt.getTmpVar(PType.Bool, "nondet");
                            ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Nondet"), Factory.Instance.MkCnst(ctxt.labelToId(afterLabel)))));
                            ctxt.addSideEffect(MkZingReturn(MkZingIdentifier("entryCtxt")));
                            ctxt.addSideEffect(MkZingLabeledStmt(afterLabel, MkZingAssign(bvar, MkZingDot("entryCtxt", "nondet"))));
                            ctxt.addSideEffect(MkZingAssign(MkZingDot("entryCtxt", "nondet"), ZingData.Cnst_False));
                            if (compiler.liveness && id.Name == PData.Cnst_FairNondet.Node.Name)
                            {
                                int i;
                                if (ctxt.translationContext == TranslationContext.Action)
                                {
                                    i = allMachines[ctxt.machineName].actionFunNameToActionFun[ctxt.entityName].numFairChoices++;
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
                            return new ZingTranslationInfo(bvar, new PBoolType());
                        }
                        else
                        {
                            Debug.Assert(false, string.Format("Nondeterministic choice allowed only in model machines or model functions.", ctxt.entityName));
                            return null;
                        }
                    }
                    else if (id.Name == PData.Cnst_Leave.Node.Name)
                    {
                        if (ctxt.translationContext == TranslationContext.Function)
                        {
                            Debug.Assert(false, string.Format("Leave statement disallowed in body of function {0}.", ctxt.entityName));
                            return null;
                        }
                        if (ctxt.translationContext == TranslationContext.Entry || ctxt.translationContext == TranslationContext.Action)
                        {
                            ctxt.addSideEffect(MkZingAssignOrCast(MkZingDot("myHandle", "currentEvent"), PType.Event, MkZingIdentifier("null"), PType.Nil));
                            ctxt.addSideEffect(MkZingAssignOrCast(MkZingDot("myHandle", "currentArg"), PType.Any, MkZingIdentifier("null"), PType.Nil));
                        }
                        ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Leave"))));
                        ctxt.addSideEffect(MkZingReturn(MkZingIdentifier("entryCtxt")));
                        return new ZingTranslationInfo(ZingData.Cnst_Nil, new PNilType());
                    }
                    else if (id.Name == PData.Cnst_Bool.Node.Name)
                    {
                        return new ZingTranslationInfo(PData.Cnst_Bool, new PNilType());
                    }
                    else if (id.Name == PData.Cnst_Int.Node.Name)
                    {
                        return new ZingTranslationInfo(PData.Cnst_Int, new PNilType());
                    }
                    else if (id.Name == PData.Cnst_Id.Node.Name)
                    {
                        return new ZingTranslationInfo(PData.Cnst_Id, new PNilType());
                    }
                    else if (id.Name == PData.Cnst_Event.Node.Name)
                    {
                        return new ZingTranslationInfo(PData.Cnst_Event, new PNilType());
                    }
                    else if (id.Name == PData.Cnst_Null.Node.Name)
                    {
                        return new ZingTranslationInfo(MkZingIdentifier("null"), new PNilType());
                    }
                    else if (id.Name == PData.Cnst_Default.Node.Name)
                    {
                        return new ZingTranslationInfo(MkZingEvent(DefaultEvent), new PEventType());
                    }
                    else if (id.Name == PData.Cnst_Delete.Node.Name)
                    {
                        return new ZingTranslationInfo(MkZingEvent(DeleteEvent), new PEventType());
                    }
                    else
                    {
                        // comes here if id.Name == VAR
                        return null;
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            var ft = (FuncTerm)n;
            var funName = ((Id)ft.Function).Name;

            if (funName == PData.Con_Assert.Node.Name)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    if (it.Current == null)
                        return null;
                    if (it.Current.type != PType.Bool)
                    {
                        Debug.Assert(false, string.Format("Assert expression must be of Boolean type."));
                        return null;
                    }

                    return new ZingTranslationInfo(AddArgs(ZingData.App_Assert, it.Current.node), new PNilType());
                }
            }
            else if (funName == PData.Con_DataOp.Node.Name)
            {
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    if (it.Current == null)
                        return null;
                    var mutatedVar = it.Current.node;
                    var mutatedVarT = it.Current.type;

                    if (op == PData.Cnst_Insert.Node.Name || op == PData.Cnst_Remove.Node.Name || op == PData.Cnst_Update.Node.Name)
                    {
                        // expected case, so do nothing
                    }
                    else
                    {
                        Debug.Assert(false, string.Format("Unknown operation {0}.", op));
                        return null;
                    }
                    if (mutatedVarT is PSeqType || (mutatedVarT is PMapType && op != PData.Cnst_Insert.Node.Name))
                    {
                        // expected case, so do nothing
                    }
                    else
                    {
                        Debug.Assert(false, string.Format("Cannot perform {0} on variable of type {1}.", op, mutatedVarT));
                        return null;
                    }

                    PSeqType mutatedVarTAsSeqType = mutatedVarT as PSeqType;
                    PMapType mutatedVarTAsMapType = mutatedVarT as PMapType;

                    it.MoveNext();
                    if (it.Current == null)
                        return null;
                    var ind = it.Current.node;
                    var indT = it.Current.type;

                    if (mutatedVarT is PSeqType)
                    {
                        if (!(indT is PIntType))
                        {
                            Debug.Assert(false, string.Format("Expected the index to be of type {0}, not {1}.", PType.Int, indT));
                            return null;
                        }
                    }
                    else
                    {
                        if (!indT.isSubtypeOf(mutatedVarTAsMapType.KeyT))
                        {
                            Debug.Assert(false, string.Format("Expected the index to be subtype of type {0}, not {1}.", mutatedVarTAsMapType, indT));
                            return null;
                        }
                    }

                    if (op == PData.Cnst_Remove.Node.Name)
                    {
                        if (it.MoveNext())
                        {
                            Debug.Assert(false, string.Format("Unnecessary arguments to remove(.)."));
                            return null;
                        }

                        if (mutatedVarTAsMapType == null)
                        {
                            return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot(mutatedVar, "Remove"), ind)), new PNilType());
                        }
                        else
                        {
                            var tmpKey = ctxt.getTmpVar(mutatedVarTAsMapType.KeyT, "tmpVal");
                            ctxt.addSideEffect(MkZingAssignOrCast(tmpKey, mutatedVarTAsMapType.KeyT, ind, indT));
                            return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot(mutatedVar, "Remove"), tmpKey)), new PNilType());
                        }
                    }

                    it.MoveNext();
                    if (it.Current == null)
                        return null;
                    var val = it.Current.node;
                    var valT = it.Current.type;

                    if (it.MoveNext())
                    {
                        Debug.Assert(false, string.Format("Unnecessary arguments to insert(.,.)."));
                        return null;
                    }

                    var innerT = mutatedVarTAsMapType == null ? mutatedVarTAsSeqType.T : mutatedVarTAsMapType.ValT;
                    if (!valT.isSubtypeOf(innerT))
                    {
                        if (mutatedVarTAsMapType == null)
                        {
                            Debug.Assert(false, string.Format("Cannot insert a value of type {0} into a sequence of type {1}.", valT, innerT));
                        }
                        else
                        {
                            Debug.Assert(false, string.Format("Cannot insert a value of type {0} into a map with value type {1}.", valT, innerT));
                        }
                        return null;
                    }

                    if (mutatedVarTAsMapType == null)
                    {
                        var tmpVal = ctxt.getTmpVar(innerT, "tmpVal");
                        ctxt.addSideEffect(MkZingAssignOrCast(tmpVal, innerT, val, valT));
                        if (op == PData.Cnst_Insert.Node.Name)
                        {
                            return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot(mutatedVar, "Insert"), ind, tmpVal)), new PNilType());
                        }
                        else
                        {
                            return new ZingTranslationInfo(MkZingAssign(MkZingIndex(MkZingDot(mutatedVar, "arr"), ind), tmpVal), new PNilType());
                        }
                    }
                    else
                    {
                        Debug.Assert(op == PData.Cnst_Update.Node.Name);
                        var tmpKey = ctxt.getTmpVar(mutatedVarTAsMapType.KeyT, "tmpVal");
                        ctxt.addSideEffect(MkZingAssignOrCast(tmpKey, mutatedVarTAsMapType.KeyT, ind, indT));
                        var tmpVal = ctxt.getTmpVar(innerT, "tmpVal");
                        ctxt.addSideEffect(MkZingAssignOrCast(tmpVal, innerT, val, valT));
                        return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot(mutatedVar, "Update"), tmpKey, tmpVal)), new PNilType());
                    }
                }
            }
            else if (funName == PData.Con_Return.Node.Name)
            {
                if (allMachines[ctxt.machineName].IsSpec && ctxt.translationContext != TranslationContext.Function)
                {
                    Debug.Assert(false, string.Format("A spec machine cannot make a return statement."));
                    return null;
                }
                if (ctxt.translationContext == TranslationContext.Exit)
                {
                    Debug.Assert(false, string.Format("Return statement not allowed in exit functions.", ctxt.entityName));
                    return null;
                }

                if (ctxt.translationContext == TranslationContext.Entry || ctxt.translationContext == TranslationContext.Action)
                {
                    using (var it = children.GetEnumerator())
                    {
                        it.MoveNext();
                        if (it.Current == null)
                        {
                            return null;
                        }
                        else if (it.Current.type != PType.Nil)
                        {
                            Debug.Assert(false, string.Format("Return statement should not have an argument.", ctxt.entityName));
                            return null;
                        }
                        else
                        {
                            ctxt.addSideEffect(MkZingAssignOrCast(MkZingDot("myHandle", "currentEvent"), PType.Event, MkZingIdentifier("null"), PType.Nil));
                            ctxt.addSideEffect(MkZingAssignOrCast(MkZingDot("myHandle", "currentArg"), PType.Any, MkZingIdentifier("null"), PType.Nil));
                            ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Return"))));
                            ctxt.addSideEffect(MkZingReturn(MkZingIdentifier("entryCtxt")));
                            return new ZingTranslationInfo(ZingData.Cnst_Nil, new PNilType());
                        }
                    }
                }

                var returnType = allMachines[ctxt.machineName].funNameToFunInfo[ctxt.entityName].returnType;
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    if (it.Current == null)
                        return null;

                    if (it.Current.type.isSubtypeOf(returnType))
                    {
                        ctxt.addSideEffect(MkZingAssignOrCast(MkZingDot("myHandle", "currentEvent"), PType.Event, MkZingIdentifier("null"), PType.Nil));
                        if (returnType is PNilType)
                        {
                            ctxt.addSideEffect(MkZingAssignOrCast(MkZingDot("myHandle", "currentArg"), PType.Any, MkZingIdentifier("null"), PType.Nil));
                        }
                        else
                        {
                            ctxt.addSideEffect(MkZingAssignOrCast(MkZingDot("myHandle", "currentArg"), PType.Any, it.Current.node, it.Current.type));
                        }
                        ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Return"))));
                        ctxt.addSideEffect(MkZingReturn(MkZingIdentifier("entryCtxt")));
                        return new ZingTranslationInfo(ZingData.Cnst_Nil, new PNilType());
                    }
                    else
                    {
                        Debug.Assert(false, string.Format("Type mismatch in return expression of function {0}. Expected {1}, got {2}", ctxt.entityName, returnType, it.Current.type));
                        return null;
                    }
                }
            }
            else if (funName == PData.Con_Scall.Node.Name)
            {
                if (allMachines[ctxt.machineName].IsSpec)
                {
                    Debug.Assert(false, string.Format("A spec machine cannot make a call statement."));
                    return null;
                }
                if (ctxt.translationContext == TranslationContext.Function)
                {
                    Debug.Assert(false, string.Format("Call statement not allowed in function."));
                    return null;
                }

                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    if (it.Current == null || it.Current.type != PType.State)
                        return null;

                    var stateName = it.Current.stateName;

                    var afterLabel = ctxt.getFreshLabel();
                    var res = MkZingSeq(
                        MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Call"), Factory.Instance.MkCnst(ctxt.labelToId(afterLabel)), it.Current.node)),
                        MkZingReturn(MkZingIdentifier("entryCtxt")),
                        MkZingLabeledStmt(afterLabel, ZingData.Cnst_Nil));
                    return new ZingTranslationInfo(res, new PNilType());
                }
            }
            else if (funName == PData.Con_Ecall.Node.Name)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    if (it.Current == null)
                        return null;
                    
                    //Implement Scheduler seal operation
                    var arglist = (it.Current.node.Node as FuncTerm).Args;
                    var listnodes = new List<AST<Node>>();
                    foreach(var command in arglist)
                    {
                        var c = (command as Cnst).GetStringValue() ;
                        if(c == "seal" || c == "unseal" || c == "rtc" || c=="rr")
                        {
                            listnodes.Add(Factory.Instance.MkCnst("\"" + c + "\""));
                        }
                        else
                        {
                            Debug.Assert(false, string.Format("only __seal() __unseal() are allowed scheduler operations"));
                            return null;
                        }
                    }
                    
                    it.MoveNext();
                        
                    var res = MkZingSeq(
                        MkZingCallStmt(MkZingCall(MkZingIdentifier("invokescheduler"), listnodes)));
                    return new ZingTranslationInfo(res, new PNilType());
                
                    

                }
                
                
            }
            else if (funName == PData.Con_Strings.Node.Name)
            {
                List<AST<Node>> ll = new List<AST<Node>>();
                using (var it = children.GetEnumerator())
                {
                    while (it.MoveNext())
                    {
                        ll.Add(it.Current.node);
                    }
                    return new ZingTranslationInfo(MkZingStrings(ll), new PNilType());
                }
            }
            else if (funName == PData.Con_Seq.Node.Name)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var first = it.Current;
                    it.MoveNext();
                    var second = it.Current;
                    if (first == null || second == null)
                    {
                        return null;
                    }
                    else
                    {
                        return new ZingTranslationInfo(MkZingSeq(zingWrapExprToStmts(first.node), ctxt.emitZingSideEffects(zingWrapExprToStmts(second.node))), new PNilType());
                    }
                }
            }
            else if (funName == PData.Con_Assign.Node.Name)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var lhs = it.Current;
                    it.MoveNext();
                    var rhs = it.Current;
                    if (lhs == null || rhs == null)
                    {
                        return null;
                    }
                    else
                    {
                        // type(lhs) == type(rhs)
                        if (!rhs.type.isSubtypeOf(lhs.type))
                        {
                            Debug.Assert(false,
                                string.Format("Type of left-hand side ({0}) does not match type of right-hand side ({1}) in assignment.", lhs.type, rhs.type));
                            return null;
                        }
                        if (lhs.isMapIndex)
                        {
                            Debug.Assert(false, string.Format("Cannot assign to map via an index."));
                            return null;
                        }

                        if (rhs.isKeys)
                        {
                            return new ZingTranslationInfo(MkZingAssignOrCast(lhs.node, lhs.type, MkZingCall(MkZingDot(rhs.node, "ToSeq")), rhs.type), new PNilType());
                        }
                        else
                        {
                            return new ZingTranslationInfo(MkZingAssignOrCast(lhs.node, lhs.type, rhs.node, rhs.type), new PNilType());
                        }
                    }
                }
            }
            else if (funName == PData.Con_ITE.Node.Name)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var condExpr = it.Current;
                    it.MoveNext();
                    var thenStmt = it.Current;
                    it.MoveNext();
                    var elseStmt = it.Current;

                    if (condExpr == null || thenStmt == null || elseStmt == null)
                    {
                        return null;
                    }
                    if (condExpr.type != PType.Bool)
                    {
                        Debug.Assert(false, string.Format("Conditional expression must be Boolean."));
                        return null;
                    }

                    // Order in which we emit side effets (else,then) is the reverse of the order in which the side effect stacks were pushed(then, else).
                    var ifName = getUnique(ctxt.entityName + "_if");
                    var elseLabel = ifName + "_else";
                    var afterLabel = ifName + "_end";
                    var cookedElse = MkZingLabeledStmt(elseLabel, ctxt.emitZingSideEffects(zingWrapExprToStmts(elseStmt.node)));
                    var cookedThen = ctxt.emitZingSideEffects(zingWrapExprToStmts(thenStmt.node));
                    var res = MkZingSeq(MkZingIfThen(MkZingApply(ZingData.Cnst_Not, condExpr.node), AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst(elseLabel))),
                        cookedThen,
                        AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst(afterLabel)),
                        cookedElse,
                        MkZingLabeledStmt(afterLabel, ZingData.Cnst_Nil));

                    return new ZingTranslationInfo(res, new PNilType());
                }
            }
            else if (funName == PData.Con_Payload.Node.Name)
            {
                if (ctxt.translationContext == TranslationContext.Function)
                {
                    Debug.Assert(false, string.Format("ARG disallowed in body of function {0}.", ctxt.entityName));
                    return null;
                }
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var typeArg = ft.Args.ElementAt(0);

                    var curPayload = MkZingDot("myHandle", "currentArg");
                    var curTrigger = MkZingDot("myHandle", "currentEvent");

                    IEnumerable<PType> possiblePayloads;

                    if (ctxt.translationContext == TranslationContext.Action)
                    {
                        possiblePayloads = allMachines[ctxt.machineName].actionFunNameToActionFun[ctxt.entityName].argTypes;
                    }
                    else if (ctxt.translationContext == TranslationContext.Entry)
                    {
                        if (GetName(allMachines[ctxt.machineName].initStateDecl, 0) == ctxt.entityName)
                            possiblePayloads = Enumerable.Repeat<PType>(PType.Any, 1);
                        else
                            possiblePayloads = allMachines[ctxt.machineName].stateNameToStateInfo[ctxt.entityName].argTypes;
                    }
                    else
                    {
                        Debug.Assert(ctxt.translationContext == TranslationContext.Exit);
                        possiblePayloads = Enumerable.Repeat<PType>(PType.Any, 1);
                    }

                    PType castType = (typeArg is Id && ((Id)typeArg).Name == PData.Cnst_Nil.Node.Name) ?
                        PType.computeLUB(possiblePayloads) : GetPType(typeArg);

                    // Statically check that our cast can be a relative of at least one of the possible payloads.
                    if (!possiblePayloads.Any(t => t.relative(castType)))
                    {
                        Debug.Assert(false, string.Format("Cannot cast to {0} from any of the possible payload types in {1}: {2}", castType, ctxt.prettyName(),
                            possiblePayloads.Aggregate("", (str, t) => str + ", " + t)));
                        return null;
                    }

                    // Emit a check that the payload is castable to whatever we are trying to cast it to
                    ctxt.addSideEffect(generatePayloadCastAsserts(ctxt, castType));

                    // Emit a check that the payload is castable to the expected event payload (a.k.a it matches the event)
                    var tmpEvPayload = ctxt.getTmpVar(Factory.Instance.MkCnst("Discriminator"), "tmpEvPayloadType");
                    ctxt.addSideEffect(MkZingAssign(tmpEvPayload, MkZingCall(MkZingDot("Main", "PayloadOf"), curTrigger)));
                    var tmpEq = ctxt.getTmpVar(PType.Bool, "tmpEq");
                    ctxt.addSideEffect(MkZingAssignOrCast(tmpEq, PType.Bool, MkZingCall(MkZingDot(curPayload, "CanCastTo"), tmpEvPayload), PType.Bool));
                    ctxt.addSideEffect(MkZingAssert(tmpEq));
                    AST<Node> outNode;

                    if (!(castType is PAnyType))
                    {
                        outNode = ctxt.getTmpVar(castType, "tmpPayloadCast");
                        ctxt.addSideEffect(MkZingAssign(outNode, MkZingCall(MkZingDot("myHandle", "currentArg", getZingDownCastName(castType)))));
                    }
                    else
                    {
                        outNode = MkZingDot("myHandle", "currentArg");
                    }

                    return new ZingTranslationInfo(outNode, castType);
                }
            }
            else if (funName == PData.Con_Use.Node.Name)
            {
                var kind = (Id)GetArgByIndex(ft, 1);
                if (kind.Name == PData.Cnst_Var.Node.Name)
                {
                    var varName = GetName(ft, 0);
                    VariableInfo varInfo;
                    if (allMachines[ctxt.machineName].localVariableToVarInfo.ContainsKey(varName))
                    {
                        varInfo = allMachines[ctxt.machineName].localVariableToVarInfo[varName];
                    }
                    else if (ctxt.translationContext == TranslationContext.Function && allMachines[ctxt.machineName].funNameToFunInfo[ctxt.entityName].parameterNameToInfo.ContainsKey(varName))
                    {
                        varInfo = allMachines[ctxt.machineName].funNameToFunInfo[ctxt.entityName].parameterNameToInfo[varName];
                    }
                    else
                    {
                        Debug.Assert(false, string.Format("Use of undeclared variable {0}.", varName));
                        return null;
                    }

                    return new ZingTranslationInfo(MkZingIdentifier(varName), varInfo.type);
                }
                else if (kind.Name == PData.Cnst_Event.Node.Name)
                {
                    var eventName = GetName(ft, 0);
                    if (!allEvents.ContainsKey(eventName))
                    {
                        Debug.Assert(false, string.Format("Use of undeclared event {0}.", eventName));
                        return null;
                    }
                    return new ZingTranslationInfo(MkZingEvent(eventName), new PEventType());
                }
                else if (kind.Name == PData.Cnst_State.Node.Name)
                {
                    var stateName = GetName(ft, 0);
                    if (!allMachines[ctxt.machineName].stateNameToStateInfo.ContainsKey(stateName))
                    {
                        Debug.Assert(false, string.Format("Use of undeclared state {0}.", stateName));
                        return null;
                    }
                    return new ZingTranslationInfo(MkZingDot("State", string.Format("_{0}", stateName)), new PStateType(), stateName);
                }
                else if (kind.Name == PData.Cnst_Field.Node.Name)
                {
                    var field = GetName(ft, 0);
                    // Fields by themselves really don't have a type
                    return new ZingTranslationInfo(Factory.Instance.MkCnst(field), new PNilType());
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if (funName == PData.Con_Tuple.Node.Name)
            {
                if (ctxt.machineName == null) // We are in the main context. Shouldn't have tuples here!
                {
                    Debug.Assert(false, string.Format("Unexpected tuple in MainDecl statement!"));
                    return null;
                }

                if (children.Any(child => child == null))
                    return null;

                var tupType = new PTupleType(children.Select(child => child.type));
                registerType(tupType);

                var node = MkZingCall(MkZingDot(pTypeToZingType(tupType), "Build"), children.Select(child => child.node));
                var tmpVar = ctxt.getTmpVar(tupType, "tmpTuple");
                ctxt.addSideEffect(MkZingAssign(tmpVar, node));

                return new ZingTranslationInfo(tmpVar, tupType);
            }
            else if (funName == PData.Con_NamedTuple.Node.Name)
            {
                if (ctxt.machineName == null) // We are in the main context. Shouldn't have tuples here!
                {
                    Debug.Assert(false, string.Format("Unexpected named tuple in MainDecl statement!"));
                    return null;
                }

                if (children.Any(child => child == null))
                    return null;

                var type = new PNamedTupleType(children.Select(child => new Tuple<string, PType>(
                    ((Cnst)GetArgByIndex((FuncTerm)child.node.Node, 0)).GetStringValue(), child.type)));
                registerType(type);

                var buildArgs = ConstructList(ZingData.App_Args,
                    children.OrderBy(child => ((Cnst)GetArgByIndex((FuncTerm)child.node.Node, 0)).GetStringValue()).Select(
                        child => Factory.Instance.ToAST(GetArgByIndex((FuncTerm)child.node.Node, 1))
                    ));

                var node = AddArgs(ZingData.App_Call, AddArgs(ZingData.App_Args, MkZingDot(pTypeToZingType(type), "Build"), buildArgs));
                var tmpVar = ctxt.getTmpVar(type, "tmpNamedTuple");
                ctxt.addSideEffect(MkZingAssign(tmpVar, node));
                return new ZingTranslationInfo(tmpVar, type);
            }
            else if (funName == PData.Con_Apply.Node.Name)
            {
                var pOp = (Id)GetArgByIndex(ft, 0);
                int arity;
                var zingOp = PData.POpToZingOp(pOp, out arity);
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var arg1 = it.Current;
                    if (arg1 == null)
                        return null;
                    if (arity == 1)
                    {
                        if (pOp.Name == PData.Cnst_Not.Node.Name && arg1.type == PType.Bool)
                        {   // TODO: In some places we return, in others we use outTerm. I think this is legacy could. Should refactor to always return
                            return new ZingTranslationInfo(MkZingApply(zingOp, arg1.node), new PBoolType());
                        }
                        else if (pOp.Name == PData.Cnst_Neg.Node.Name && arg1.type == PType.Int)
                        {
                            return new ZingTranslationInfo(MkZingApply(zingOp, arg1.node), new PIntType());
                        }
                        else if (pOp.Name == PData.Cnst_Sizeof.Node.Name)
                        {
                            if (!(arg1.type is PSeqType || arg1.type is PMapType))
                            {
                                Debug.Assert(false, string.Format("sizeof() expects a sequence or map."));
                                return null;
                            }
                            return new ZingTranslationInfo(MkZingDot(arg1.node, "size"), new PIntType());
                        }
                        else if (pOp.Name == PData.Cnst_Keys.Node.Name)
                        {
                            if (!(arg1.type is PMapType))
                            {
                                Debug.Assert(false, string.Format("keys() expects a map."));
                                return null;
                            }
                            return new ZingTranslationInfo(arg1.node, new PSeqType((arg1.type as PMapType).KeyT), false, true);
                        }
                        else
                        {
                            Debug.Assert(false, string.Format("Mismatched types in expression."));
                            return null;
                        }
                    }
                    else if (arity == 2)
                    {
                        it.MoveNext();
                        var arg2 = it.Current;
                        if (arg2 == null)
                            return null;
                        if (pOp.Name == PData.Cnst_In.Node.Name)
                        {
                            PMapType mapType = arg2.type as PMapType;
                            if (mapType == null)
                            {
                                Debug.Assert(false, string.Format("Expected a map with the in operator"));
                                return null;
                            }
                            PType keyType = arg1.type;
                            if (!(keyType.isSubtypeOf(mapType.KeyT)))
                            {
                                Debug.Assert(false, string.Format("Expected a subtype of {0} for the key when applying in operator to a map of type {1}", keyType, mapType));
                                return null;
                            }
                            var tmpVar = ctxt.getTmpVar(PType.Bool, "tmpVal");
                            ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(arg2.node, "Contains"), arg1.node)));
                            return new ZingTranslationInfo(tmpVar, PType.Bool);
                        }
                        else if (pOp.Name == PData.Cnst_Idx.Node.Name)
                        {
                            var indType = arg2.type;

                            if (arg1.type is PTupleType)
                            {
                                var baseType = arg1.type as PTupleType;
                                var indN = arg2.node.Node;

                                if (indType != PType.Int)
                                {
                                    Debug.Assert(false, string.Format("Expected an int when indexing a tuple"));
                                    return null;
                                }

                                if (!(indN is Cnst))
                                {
                                    Debug.Assert(false, string.Format("Can only index a tuple with a constant"));
                                    return null;
                                }

                                var elInd = (int)((Cnst)indN).GetNumericValue().Numerator;

                                if (elInd < 0 || elInd > baseType.elements.Count())
                                {
                                    Debug.Assert(false, string.Format("Invalid index {0} into tuple of length {1}", elInd, baseType.elements.Count()));
                                    return null;
                                }

                                return new ZingTranslationInfo(MkZingDot(arg1.node, getTupleField(elInd)), baseType.elements.ElementAt(elInd));
                            }
                            else if (arg1.type is PSeqType)
                            {
                                var baseType = arg1.type as PSeqType;

                                if (indType != PType.Int)
                                {
                                    Debug.Assert(false, string.Format("Expected an int when indexing a sequence"));
                                    return null;
                                }
                                if (arg1.isKeys)
                                {
                                    var tmpVar = ctxt.getTmpVar(baseType.T, "tmpVal");
                                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(arg1.node, "LookupKeyAtIndex"), arg2.node)));
                                    return new ZingTranslationInfo(tmpVar, baseType.T);
                                }
                                else
                                {
                                    return new ZingTranslationInfo(MkZingIndex(MkZingDot(arg1.node, "arr"), arg2.node), baseType.T);
                                }
                            }
                            else if (arg1.type is PMapType)
                            {
                                var baseType = arg1.type as PMapType;

                                if (!indType.isSubtypeOf(baseType.KeyT))
                                {
                                    Debug.Assert(false, string.Format("Expected subtype of type {0} when indexing a map of type {1}", baseType.KeyT, baseType));
                                    return null;
                                }
                                var tmpVar = ctxt.getTmpVar(baseType.ValT, "tmpVal");
                                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(arg1.node, "Lookup"), arg2.node)));
                                return new ZingTranslationInfo(tmpVar, baseType.ValT, true, false);
                            }
                            else
                            {
                                Debug.Assert(false, string.Format("Unexpected expression of type {0} on the left side of indexing", arg1.type));
                                return null;
                            }
                        }
                        else if (pOp.Name == PData.Cnst_Fld.Node.Name)
                        {
                            var baseType = arg1.type;
                            var indType = arg2.type;
                            var tupType = baseType as PNamedTupleType;

                            if (tupType == null)
                            {
                                Debug.Assert(false, string.Format("Unexpected expression of type {0} on the left side of field lookup", baseType));
                                return null;
                            }

                            // Verify that the second P Node is indeed a Use("f", FIELD) node
                            var useNode = GetArgByIndex((FuncTerm)GetArgByIndex((FuncTerm)GetArgByIndex(ft, 1), 1), 0) as FuncTerm;
                            if (useNode == null || ((Id)useNode.Function).Name != PData.Con_Use.Node.Name ||
                                !(GetArgByIndex(useNode, 1) is Id) || ((Id)GetArgByIndex(useNode, 1)).Name != PData.Cnst_Field.Node.Name)
                            {
                                Debug.Assert(false, string.Format("Invalid Field expression in field lookup", baseType));
                                return null;
                            }

                            var memberName = (arg2.node.Node as Cnst).GetStringValue();
                            PType memberType = null;

                            foreach (var el in tupType.elements)
                            {
                                if (el.Item1 == memberName)
                                {
                                    memberType = el.Item2;
                                    break;
                                }
                            }

                            if (memberType == null)
                            {
                                Debug.Assert(false, string.Format("Named Tuple {0} doesn't conatin a field {1}.", tupType, memberName));
                                return null;
                            }

                            return new ZingTranslationInfo(MkZingDot(arg1.node, memberName), memberType);
                        } 
                        if (pOp.Name == PData.Cnst_Eq.Node.Name || pOp.Name == PData.Cnst_NEq.Node.Name)
                        {
                            if (!arg1.type.isSubtypeOf(arg2.type) && !arg2.type.isSubtypeOf(arg1.type))
                            {
                                Debug.Assert(false, string.Format("Cannot compare for equality items of types {0} and {1}", arg1.type, arg2.type));
                                return null;
                            }

                            var outNode = pOp.Name == PData.Cnst_Eq.Node.Name ?
                                MkZingEq(ctxt, arg1.node, arg1.type, arg2.node, arg2.type) :
                                MkZingNeq(ctxt, arg1.node, arg1.type, arg2.node, arg2.type);
                            return new ZingTranslationInfo(outNode, new PBoolType());
                        }
                        else if (arg1.type == arg2.type)
                        {
                            if (pOp.Name == PData.Cnst_And.Node.Name || pOp.Name == PData.Cnst_Or.Node.Name)
                            {
                                if (arg1.type == PType.Bool)
                                {
                                    return new ZingTranslationInfo(MkZingApply(zingOp, arg1.node, arg2.node), new PBoolType());
                                }
                                else
                                {
                                    Debug.Assert(false, string.Format("Mismatched types in expression."));
                                    return null;
                                }
                            }
                            else if (arg1.type == PType.Int)
                            {
                                if (pOp.Name == PData.Cnst_Add.Node.Name || pOp.Name == PData.Cnst_Sub.Node.Name || pOp.Name == PData.Cnst_Mul.Node.Name || pOp.Name == PData.Cnst_IntDiv.Node.Name)
                                {
                                    return new ZingTranslationInfo(MkZingApply(zingOp, arg1.node, arg2.node), new PIntType());
                                }
                                else
                                {
                                    return new ZingTranslationInfo(MkZingApply(zingOp, arg1.node, arg2.node), new PBoolType());
                                }
                            }
                            else
                            {
                                Debug.Assert(false, string.Format("Mismatched types in expression."));
                                return null;
                            }
                        }
                        else
                        {
                            Debug.Assert(false, string.Format("Mismatched types in expression. Arguments have different types - {0} and {1}", arg1.type, arg2.type));
                            return null;
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            else if (funName == Con_LabeledExpr)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    if (it.Current == null)
                        return null;

                    ZingTranslationInfo info = (ZingTranslationInfo)it.Current.Clone();
                    info.node = AddArgs(App_LabeledExpr, Factory.Instance.ToAST(GetArgByIndex(ft, 0)), info.node);
                    return info;
                }
            }
            else if (funName == PData.Con_Mcall.Node.Name)
            {
                if (ctxt.machineName != null && allMachines[ctxt.machineName].IsSpec)
                {
                    Debug.Assert(false, string.Format("A spec machine cannot invoke other machines."));
                    return null;
                }
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var typeName = ((Cnst)it.Current.node.Node).GetStringValue();
                    if (!allMachines.ContainsKey(typeName))
                    {
                        Debug.Assert(false, string.Format("Machine type {0} has not been declared.", typeName));
                        return null;
                    }
                    if (!allMachines[typeName].IsSpec)
                    {
                        Debug.Assert(false, string.Format("Only monitor machine can be invoked."));
                        return null;
                    }
                    it.MoveNext();
                    if (it.Current == null)
                        return null;
                    if (it.Current.type != PType.Event)
                    {
                        Debug.Assert(false, string.Format("First argument to a monitor invocation must have event type."));
                        return null;
                    }
                    AST<Node> evt = it.Current.node;
                    it.MoveNext();
                    if (it.Current == null)
                        return null;

                    PType argType = it.Current.node != ZingData.Cnst_Nil ? it.Current.type : PType.Nil;
                    AST<Node> arg = it.Current.node != ZingData.Cnst_Nil ? it.Current.node : MkZingIdentifier("null");
                    var tmpVar = ctxt.getTmpVar(PType.Any, "tmpSendPayload");
                    ctxt.addSideEffect(MkZingAssignOrCast(tmpVar, PType.Any, arg, argType));
                    MachineInfo machineInfo = allMachines[typeName];
                    return new ZingTranslationInfo(
                        MkZingCall(MkZingDot("Main", string.Format("InvokeMachine_{0}", typeName)), evt, tmpVar), new PNilType());
                }
            }
            else if (funName == PData.Con_New.Node.Name)
            {
                if (ctxt.machineName != null && allMachines[ctxt.machineName].IsSpec)
                {
                    Debug.Assert(false, string.Format("A spec machine cannot create machines."));
                    return null;
                }
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var typeName = ((Cnst)it.Current.node.Node).GetStringValue();
                    if (!allMachines.ContainsKey(typeName))
                    {
                        Debug.Assert(false, string.Format("Machine type {0} has not been declared.", typeName));
                        return null;
                    }
                    if (ctxt.machineName == null && allMachines[typeName].IsSpec)
                    {
                        Debug.Assert(false, string.Format("Main machine cannot be a monitor."));
                        return null;
                    }
                    it.MoveNext();
                    if (it.Current == null)
                        return null;

                    PType argType = it.Current.node != ZingData.Cnst_Nil ? it.Current.type : PType.Nil;
                    AST<Node> arg = it.Current.node != ZingData.Cnst_Nil ? it.Current.node : MkZingIdentifier("null");
                    var tmpVar = ctxt.getTmpVar(PType.Any, "tmpSendPayload");
                    ctxt.addSideEffect(MkZingAssignOrCast(tmpVar, PType.Any, arg, argType));
                   
                    MachineInfo machineInfo = allMachines[typeName];
                    if (machineInfo.IsSpec || ctxt.entityName == null)
                    {
                        ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("Main", string.Format("CreateMachine_{0}", typeName)), tmpVar)));
                        return new ZingTranslationInfo(ZingData.Cnst_Nil, new PNilType());
                    }
                    else
                    {
                        PType machineType = machineInfo.IsModel ? (PType)new PMidType() : (PType)new PIdType();
                        var newMachine = ctxt.getTmpVar(machineType, "newMachine");
                        ctxt.addSideEffect(MkZingAssign(newMachine, MkZingCall(MkZingDot("Main", string.Format("CreateMachine_{0}", typeName)), tmpVar)));
                        string afterLabel = ctxt.getFreshLabel();
                        ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "NewM"), Factory.Instance.MkCnst(ctxt.labelToId(afterLabel)), newMachine)));
                        ctxt.addSideEffect(MkZingReturn(MkZingIdentifier("entryCtxt")));
                        ctxt.addSideEffect(MkZingLabeledStmt(afterLabel, MkZingAssign(newMachine, MkZingDot("entryCtxt", "id"))));
                        ctxt.addSideEffect(MkZingAssign(MkZingDot("entryCtxt", "id"), MkZingIdentifier("null")));
                        return new ZingTranslationInfo(newMachine, machineType);
                    }
                }
            }
            else if (funName == PData.Con_Raise.Node.Name)
            {
                if (ctxt.translationContext != TranslationContext.Entry && ctxt.translationContext != TranslationContext.Action)
                {
                    Debug.Assert(false, string.Format("Raise statement allowed only in entry functions or actions.", ctxt.entityName));
                    return null;
                }
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var eventExpr = it.Current;

                    if (eventExpr == null)
                    {
                        return null;
                    }

                    PEventType evtType = it.Current.type as PEventType;

                    if (evtType == null)
                    {
                        Debug.Assert(false, string.Format("The argument to raise must be an event."));
                        return null;
                    }

                    it.MoveNext();

                    var payloadExpr = it.Current;
                    if (payloadExpr == null)
                    {
                        return null;
                    }
                    AST<Node> payload = payloadExpr.node != ZingData.Cnst_Nil ? payloadExpr.node : MkZingIdentifier("null");
                    PType payloadType = payloadExpr.type;

                    var assertStmt = MkZingSeq(Factory.Instance.AddArg(ZingData.App_Assert, MkZingApply(ZingData.Cnst_NEq, eventExpr.node, MkZingIdentifier("null"))),
                                               Factory.Instance.AddArg(ZingData.App_Assert, MkZingApply(ZingData.Cnst_NEq, eventExpr.node, MkZingEvent("default"))));
                    string traceString = string.Format("\"<RaiseLog> Machine {0}-{{0}} raised Event {{1}}\"", ctxt.machineName);
                    var traceStmt = MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(traceString), MkZingDot("myHandle", "instance"), MkZingApply(ZingData.Cnst_Dot, eventExpr.node, Factory.Instance.MkCnst("name"))));
                    var tmpArg = ctxt.getTmpVar(PType.Any, "arg");
                    var tmpEv = ctxt.getTmpVar(PType.Event, "ev");
                    var eventPayloadCasts = MkZingSeq(MkZingAssignOrCast(tmpEv, PType.Event, eventExpr.node, PType.Event),
                                               MkZingAssignOrCast(tmpArg, PType.Any, payload, payloadType));
                    // Emit a check that the payload can be casted to the expected Event Payload
                    var tmpEvPayload = ctxt.getTmpVar(Factory.Instance.MkCnst("Discriminator"), "tmpEvPayloadType");
                    var tmpEq = ctxt.getTmpVar(PType.Bool, "tmpEq");
                    var payloadEventMatchAssert = MkZingSeq(
                        MkZingAssign(tmpEvPayload, MkZingCall(MkZingDot("Main", "PayloadOf"), eventExpr.node)),
                        MkZingAssignOrCast(tmpEq, PType.Bool, MkZingCall(MkZingDot(tmpArg, "CanCastTo"), tmpEvPayload), PType.Bool),
                        MkZingAssert(tmpEq));
                    var assignStmt = MkZingSeq(MkZingAssign(MkZingDot("myHandle", "currentEvent"), tmpEv), MkZingAssign(MkZingDot("myHandle", "currentArg"), tmpArg));
                    var createRetCtxt = MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Raise")));

                    var outNode = MkZingSeq(assertStmt, traceStmt, eventPayloadCasts, payloadEventMatchAssert, assignStmt, createRetCtxt,
                        MkZingReturn(MkZingIdentifier("entryCtxt")));
                    return new ZingTranslationInfo(outNode, new PNilType());
                }
            }
            else if (funName == PData.Con_Call.Node.Name)
            {
                var calleeName = GetName(ft, 0);
                if (!allMachines[ctxt.machineName].funNameToFunInfo.ContainsKey(calleeName))
                {
                    Debug.Assert(false, string.Format("Function {0} has not been declared.", calleeName));
                    return null;
                }
                var calleeInfo = allMachines[ctxt.machineName].funNameToFunInfo[calleeName];
                if (ctxt.translationContext == TranslationContext.Action)
                {
                    allMachines[ctxt.machineName].actionFunNameToActionFun[ctxt.entityName].callees.Add(calleeName);
                }
                else if (ctxt.translationContext == TranslationContext.Function)
                {
                    allMachines[ctxt.machineName].funNameToFunInfo[calleeName].callers.Add(ctxt.entityName);
                }
                else if (ctxt.translationContext == TranslationContext.Entry)
                {
                    allMachines[ctxt.machineName].stateNameToStateInfo[ctxt.entityName].entryFunCallees.Add(calleeName);
                }
                else
                {
                    allMachines[ctxt.machineName].stateNameToStateInfo[ctxt.entityName].exitFunCallees.Add(calleeName);
                }

                List<AST<Node>> args = new List<AST<Node>>();
                // Prepend the default entryCtxt argument.
                args.Add(MkZingIdentifier("entryCtxt"));

                int count = 0;
                foreach (var child in children)
                {
                    if (child == null)
                    {
                        return null;
                    }
                    if (count == calleeInfo.parameterNames.Count)
                    {
                        Debug.Assert(false, string.Format("Fewer parameters than arguments."));
                        return null;
                    }
                    var lhsInfo = calleeInfo.parameterNameToInfo[calleeInfo.parameterNames[count]];
                    var rhsInfo = child;
                    if (!rhsInfo.type.isSubtypeOf(lhsInfo.type))
                    {
                        Debug.Assert(false, string.Format("Mismatch in type of argument {0}.", count));
                        return null;
                    }

                    Debug.Assert(!(lhsInfo.type is PNilType)); // Can't declare formal parameters of type null.
                    AST<Node> argActual;
                    if (rhsInfo.type == lhsInfo.type)
                    {
                        if (rhsInfo.type is PPrimitiveType)
                        {
                            argActual = child.node;
                        }
                        else
                        {
                            var tmpVar = ctxt.getTmpVar(rhsInfo.type, "argCloneVar");
                            ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(child.node, "Clone"))));
                            argActual = tmpVar;
                        }
                    }
                    else
                    {
                        var tmpVar = ctxt.getTmpVar(lhsInfo.type, "argCastVar");
                        ctxt.addSideEffect(MkZingAssignOrCast(tmpVar, lhsInfo.type, child.node, rhsInfo.type));
                        argActual = tmpVar;
                    }
                    count++;
                    args.Add(argActual);
                }
                if (count < calleeInfo.parameterNames.Count)
                {
                    Debug.Assert(false, string.Format("Fewer arguments than parameters."));
                    return null;
                }

                AST<Node> callExpr = MkZingCall(MkZingIdentifier(calleeName), args);
                AST<Node> processOutput;
                AST<Node> outExp;

                var beforeLabel = ctxt.getFreshLabel();
                if (calleeInfo.returnType is PNilType)
                {
                    processOutput = ZingData.Cnst_Nil;
                    outExp = ZingData.Cnst_Nil;
                }
                else
                {
                    var retVar = ctxt.getTmpVar(calleeInfo.returnType, "ret");
                    processOutput = MkZingAssignOrCast(retVar, calleeInfo.returnType, MkZingDot("myHandle", "currentArg"), PType.Any);
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
                return new ZingTranslationInfo(outExp, calleeInfo.returnType);
            }
            else if (funName == PData.Con_Send.Node.Name)
            {
                if (allMachines[ctxt.machineName].IsSpec)
                {
                    Debug.Assert(false, string.Format("A spec machine cannot send messages."));
                    return null;
                }
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    if (it.Current == null)
                    {
                        return null;
                    }
                    if (!it.Current.type.IsMachineId)
                    {
                        Debug.Assert(false, string.Format("The target of a send must be of machine type."));
                        return null;
                    }
                    var targetExpr = it.Current.node;
                    it.MoveNext();
                    if (it.Current == null)
                    {
                        return null;
                    }
                    if (it.Current.type != PType.Event)
                    {
                        Debug.Assert(false, string.Format("The argument to send must be an event."));
                        return null;
                    }
                    var eventExpr = it.Current.node;
                    var evtType = it.Current.type as PEventType;
                    it.MoveNext();
                    if (it.Current == null)
                    {
                        return null;
                    }
                    PType payloadType = it.Current.node != ZingData.Cnst_Nil ? it.Current.type : PType.Nil;
                    AST<Node> payload = it.Current.node != ZingData.Cnst_Nil ? it.Current.node : MkZingIdentifier("null");

                    // Emit a Cast/Clone from the actual payload to Any
                    var tmpVar = ctxt.getTmpVar(PType.Any, "tmpSendPayload");
                    ctxt.addSideEffect(MkZingAssignOrCast(tmpVar, PType.Any, payload, payloadType));
                    // Emit a check that the payload can be casted to the expected Event Payload
                    var tmpEvPayload = ctxt.getTmpVar(Factory.Instance.MkCnst("Discriminator"), "tmpEvPayloadType");
                    ctxt.addSideEffect(MkZingAssign(tmpEvPayload, MkZingCall(MkZingDot("Main", "PayloadOf"), eventExpr)));
                    var tmpEq = ctxt.getTmpVar(PType.Bool, "tmpEq");
                    ctxt.addSideEffect(MkZingAssignOrCast(tmpEq, PType.Bool, MkZingCall(MkZingDot(tmpVar, "CanCastTo"), tmpEvPayload), PType.Bool));
                    ctxt.addSideEffect(MkZingAssert(tmpEq));
                    var afterLabel = ctxt.getFreshLabel();
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingApply(ZingData.Cnst_Dot, targetExpr, MkZingIdentifier("EnqueueEvent")), eventExpr, tmpVar, Factory.Instance.MkCnst("myHandle"))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Send"), Factory.Instance.MkCnst(ctxt.labelToId(afterLabel)))));
                    // Actual Send statement

                    return new ZingTranslationInfo(MkZingSeq(MkZingReturn(MkZingIdentifier("entryCtxt")),
                        MkZingLabeledStmt(afterLabel, ZingData.Cnst_Nil)), new PNilType());
                }
            }
            else if (funName == PData.Con_While.Node.Name)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var condExpr = it.Current;
                    it.MoveNext();

                    if (condExpr == null || it.Current == null)
                    {
                        return null;
                    }

                    if (condExpr.node != null && condExpr.type != PType.Bool)
                    {
                        Debug.Assert(false, string.Format("Conditional expression must be Boolean not {0}", condExpr.type));
                        return null;
                    }

                    var loopStart = getUnique(ctxt.entityName + "_loop_start");
                    var loopEnd = getUnique(ctxt.entityName + "_loop_end");

                    var body = it.Current.node;
                    body = ctxt.emitZingSideEffects(zingWrapExprToStmts(body));

                    var res = MkZingLabeledStmt(loopStart, MkZingSeq(
                        ctxt.emitZingSideEffects(MkZingIfThen(MkZingApply(ZingData.Cnst_Not, condExpr.node), AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst(loopEnd)))),
                        body,
                        AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst(loopStart)),
                        MkZingLabeledStmt(loopEnd, ZingData.Cnst_Nil)));

                    return new ZingTranslationInfo(res, new PNilType());
                }
            }
            else if (funName == PData.Con_TypeTuple.Node.Name || funName == PData.Con_TypeNamedTuple.Node.Name || funName == PData.Con_TypeSeq.Node.Name || funName == PData.Con_TypeMap.Node.Name)
            {
                return new ZingTranslationInfo(null, new PNilType()); // Ignore
            }
            else if (funName == PData.Con_TypeField.Node.Name)
            {
                return new ZingTranslationInfo(null, new PNilType()); // Ignore
            }
            else
            {
                Console.WriteLine("Unknown term name: " + funName);
                throw new NotImplementedException();
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
                          ? MkZingSeq(MkZingAssignOrCast(MkZingDot("myHandle", "currentEvent"), PType.Event, MkZingIdentifier("null"), PType.Nil),
                                      MkZingAssignOrCast(MkZingDot("myHandle", "currentArg"), PType.Any, MkZingIdentifier("null"), PType.Nil)) 
                          : ZingData.Cnst_Nil,
                MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Leave"))),
                MkZingReturn(MkZingIdentifier("entryCtxt")));

            return MkZingMethodDecl(name, MkZingVarDecls(MkZingVarDecl("entryCtxt", Factory.Instance.MkCnst("Continuation"))), Factory.Instance.MkCnst("Continuation"),
                ctxt.emitLocals(), MkZingBlock("dummy", body));
        }

        private AST<Node> MkZingFunMethod(string funName, FunInfo funInfo)
        {
            var machineName = GetName(GetFuncTerm(GetArgByIndex(funInfo.funDecl, 1)), 0);
            AST<Node> parameters = LocalVariablesToVarDecls(funInfo.parameterNames, funInfo.parameterNameToInfo);
            parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("entryCtxt", Factory.Instance.MkCnst("Continuation")), parameters);
            AST<Node> funBody;
            AST<Node> entry = Factory.Instance.ToAST(GetArgByIndex(funInfo.funDecl, 5));

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
                        MkZingAssignOrCast(indVar, PType.Int, MkZingPlus(indVar, 1), PType.Int))));
        }

        private AST<Node> MkZingFor(AST<Node> indVar, int start, AST<Node> end, AST<Node> body)
        {
            return MkZingFor(indVar, Factory.Instance.MkCnst(start), end, body);
        }

        private AST<FuncTerm> MkZingSeqEqMethod(PSeqType meT, PSeqType otherT)
        {
            var name = meT == otherT ? "Equals" : getZingEqualsName(otherT);
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, name, this);
            var ind = ctxt.getTmpVar(PType.Int, "ind");
            var body = MkZingSeq(
                MkZingIfThen(MkZingNeq(MkZingDot("this", "size"), MkZingDot("other", "size")),
                    MkZingReturn(ZingData.Cnst_False)),
                    MkZingFor(ind, 0, MkZingDot("this", "size"),
                        ctxt.emitZingSideEffects(MkZingIfThen(MkZingNeq(ctxt, MkZingIndex(MkZingDot("this", "arr"), ind), meT.T, MkZingIndex(MkZingDot("other", "arr"), ind), otherT.T),
                            MkZingReturn(ZingData.Cnst_False)))),
                MkZingReturn(ZingData.Cnst_True));
            ctxt.pushSideEffectStack();

            return MkZingMethodDecl(name, MkZingVarDecls(MkZingVarDecl("other", otherT)),
                ZingData.Cnst_Bool, ctxt.emitLocals(), MkZingBlock("dummy", ctxt.emitZingSideEffects(body)));
        }

        private AST<FuncTerm> MkZingSeqCastOrClone(PSeqType fromT, PSeqType toT)
        {
            var name = getZingCastOrCloneName(fromT, toT);
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, name, this);
            var res = ctxt.getTmpVar(toT, "res");
            var ind = ctxt.getTmpVar(PType.Int, "ind");
            var isUpcast = fromT.isSubtypeOf(toT) && toT != fromT;
            var from = isUpcast ? "obj" : "this"; // Only Upcasts are static
            var body = MkZingSeq(MkZingAssign(res, AddArgs(ZingData.App_New, pTypeToZingType(toT), ZingData.Cnst_Nil)),
                MkZingAssign(MkZingDot(res, "arr"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst(pTypeToZingName(toT.T) + "_array"), MkZingDot(from, "size"))),
                MkZingAssign(MkZingDot(res, "size"), MkZingDot(from, "size")),
                MkZingFor(ind, 0, MkZingDot(from, "size"),
                        MkZingAssignOrCast(MkZingIndex(MkZingDot(res, "arr"), ind), toT.T, MkZingIndex(MkZingDot(from, "arr"), ind), fromT.T)),
                MkZingReturn(res));

            if (isUpcast)
                return MkZingMethodDecl(name, MkZingVarDecls(MkZingVarDecl(from, fromT)), pTypeToZingType(toT), ctxt.emitLocals(),
                    MkZingBlock("dummy", ctxt.emitZingSideEffects(body)), ZingData.Cnst_Static);
            else
                return MkZingMethodDecl(name, ZingData.Cnst_Nil, pTypeToZingType(toT), ctxt.emitLocals(),
                    MkZingBlock("dummy", ctxt.emitZingSideEffects(body)));
        }

        private AST<FuncTerm> MkZingSeqCanDownCast(PSeqType fromT, PSeqType toT)
        {
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Clone", this);
            var ind = ctxt.getTmpVar(PType.Int, "ind");
            var body = MkZingSeq(
                MkZingFor(ind, 0, MkZingDot("this", "size"),
                    ctxt.emitZingSideEffects(MkZingIfThen(MkZingApply(ZingData.Cnst_Not, MkZingCanDownCastTo(ctxt, MkZingIndex(MkZingDot("this", "arr"), ind), fromT.T, toT.T)),
                        MkZingReturn(ZingData.Cnst_False)))),
                MkZingReturn(ZingData.Cnst_True));

            ctxt.pushSideEffectStack();

            return MkZingMethodDecl(getZingCanDownCastName(toT), ZingData.Cnst_Nil, pTypeToZingType(PType.Bool), ctxt.emitLocals(),
                MkZingBlock("dummy", ctxt.emitZingSideEffects(body)));
        }

        private AST<FuncTerm> MkZingSeqInsert(PSeqType t)
        {
            ZingEntryFun_FoldContext insertCtxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Insert", this);
            var arrT = Factory.Instance.MkCnst(pTypeToZingName(t.T) + "_array");
            var tmpArrVar = insertCtxt.getTmpVar(arrT, "newArr");
            var ind = insertCtxt.getTmpVar(PType.Int, "i");
            var insertBody = MkZingSeq(
                MkZingAssert(MkZingApply(ZingData.Cnst_Le, Factory.Instance.MkCnst(0), MkZingIdentifier("ind"))),
                MkZingAssert(MkZingApply(ZingData.Cnst_Le, MkZingIdentifier("ind"), MkZingDot("this", "size"))),
                MkZingAssign(MkZingDot("this", "size"), MkZingPlus(MkZingDot("this", "size"), 1)),
                MkZingAssign(tmpArrVar, AddArgs(ZingData.App_New, arrT, MkZingDot("this", "size"))),
                MkZingFor(ind, 0, MkZingIdentifier("ind"),
                    MkZingAssignOrCast(MkZingIndex(tmpArrVar, ind), t.T, MkZingIndex(MkZingDot("this", "arr"), ind), t.T)),
                MkZingAssignOrCast(MkZingIndex(tmpArrVar, MkZingIdentifier("ind")), t.T, MkZingIdentifier("val"), t.T),
                MkZingFor(ind, MkZingPlus(MkZingIdentifier("ind"), 1), MkZingDot("this", "size"),
                    MkZingAssignOrCast(MkZingIndex(tmpArrVar, ind), t.T, MkZingIndex(MkZingDot("this", "arr"), MkZingMinus(ind, 1)), t.T)),
                MkZingAssign(MkZingDot("this", "arr"), tmpArrVar));

            return MkZingMethodDecl("Insert", MkZingVarDecls(MkZingVarDecl("ind", PType.Int), MkZingVarDecl("val", t.T)),
                ZingData.Cnst_Void, insertCtxt.emitLocals(), MkZingBlock("dummy", insertBody));
        }

        private AST<FuncTerm> MkZingSeqRemove(PSeqType t)
        {
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Remove", this);
            var arrT = Factory.Instance.MkCnst(pTypeToZingName(t.T) + "_array");
            var tmpArrVar = ctxt.getTmpVar(arrT, "newArr");
            var ind = ctxt.getTmpVar(PType.Int, "i");
            var insertBody = MkZingSeq(
                MkZingAssert(MkZingApply(ZingData.Cnst_Le, Factory.Instance.MkCnst(0), MkZingIdentifier("ind"))),
                MkZingAssert(MkZingApply(ZingData.Cnst_Lt, MkZingIdentifier("ind"), MkZingDot("this", "size"))),
                MkZingAssign(MkZingDot("this", "size"), MkZingMinus(MkZingDot("this", "size"), 1)),
                MkZingAssign(tmpArrVar, AddArgs(ZingData.App_New, arrT, MkZingDot("this", "size"))),
                MkZingFor(ind, 0, MkZingIdentifier("ind"),
                    MkZingAssignOrCast(MkZingIndex(tmpArrVar, ind), t.T, MkZingIndex(MkZingDot("this", "arr"), ind), t.T)),
                MkZingFor(ind, MkZingIdentifier("ind"), MkZingDot("this", "size"),
                    MkZingAssignOrCast(MkZingIndex(tmpArrVar, ind), t.T, MkZingIndex(MkZingDot("this", "arr"), MkZingPlus(ind, 1)), t.T)),
                MkZingAssign(MkZingDot("this", "arr"), tmpArrVar));

            return MkZingMethodDecl("Remove", MkZingVarDecls(MkZingVarDecl("ind", PType.Int)),
                ZingData.Cnst_Void, ctxt.emitLocals(), MkZingBlock("dummy", insertBody));
        }

        private AST<FuncTerm> MkZingSeqClassDefinition(PSeqType t)
        {
            var zType = pTypeToZingType(t);
            var arrayName = Factory.Instance.MkCnst(pTypeToZingName(t.T) + "_array");
            var fields = ConstructList(ZingData.App_VarDecls, MkZingVarDecl("arr", arrayName), MkZingVarDecl("size", pTypeToZingType(PType.Int)));

            var methods = new List<AST<Node>>();

            // Create the BuildDefault Method
            ZingEntryFun_FoldContext buildDefCtxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "BuildDefault", this);
            var buildDefRes = buildDefCtxt.getTmpVar(t, "res");
            var buildDefBody = MkZingSeq(
                                    MkZingAssign(buildDefRes, AddArgs(ZingData.App_New, Factory.Instance.MkCnst(pTypeToZingName(t)), ZingData.Cnst_Nil)),
                                    MkZingAssign(MkZingDot(buildDefRes, "arr"), AddArgs(ZingData.App_New, arrayName, Factory.Instance.MkCnst(0))),
                                    MkZingAssign(MkZingDot(buildDefRes, "size"), Factory.Instance.MkCnst(0)),
                                    AddArgs(ZingData.App_Return, buildDefRes));
            methods.Add(MkZingMethodDecl("BuildDefault", ZingData.Cnst_Nil, zType, buildDefCtxt.emitLocals(), // Local variables
                MkZingBlock("dummy", buildDefCtxt.emitZingSideEffects(buildDefBody)), ZingData.Cnst_Static));

            // Create the Clone Method
            methods.Add(MkZingSeqCastOrClone(t, t));

            // Create the Equals Method
            methods.Add(MkZingSeqEqMethod(t, t));

            foreach (var subT in subtypes[t])
            {
                Debug.Assert(subT is PSeqType);
                // Emit "Equals_<subtype>" method
                methods.Add(MkZingSeqEqMethod(t, (PSeqType)subT));

                // Emit "UpCastFrom_<SubT>" method
                methods.Add(MkZingSeqCastOrClone((PSeqType)subT, t));

                // Emit the "DownCatTo_<SubT>" method
                methods.Add(MkZingSeqCastOrClone(t, (PSeqType)subT));

                // Emit the "CanDownCastTo_<SubT>" method
                methods.Add(MkZingSeqCanDownCast(t, (PSeqType)subT));
            }

            // Create the Insert Method.
            methods.Add(MkZingSeqInsert(t));

            // Create the Remove Method.
            methods.Add(MkZingSeqRemove(t));

            return AddArgs(ZingData.App_ClassDecl, Factory.Instance.MkCnst(pTypeToZingName(t)), fields, ConstructList(ZingData.App_MethodDecls, methods));
        }

        private AST<FuncTerm> MkZingMapEqMethod(PMapType meT, PMapType otherT)
        {
            var name = meT == otherT ? "Equals" : getZingEqualsName(otherT);
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, name, this);

            var iter = ctxt.getTmpVar(Factory.Instance.MkCnst(pTypeToZingClassName(meT) + "_Entry"), "iter");
            var found = ctxt.getTmpVar(PType.Bool, "found");
            var val = ctxt.getTmpVar(pTypeToZingType(otherT.ValT), "val");
            var body = MkZingSeq(
                MkZingIfThen(MkZingNeq(MkZingDot("this", "size"), MkZingDot("other", "size")), MkZingReturn(ZingData.Cnst_False)),
                MkZingListIter(iter, MkZingDot("head", "next"), MkZingIdentifier("head"),
                               ctxt.emitZingSideEffects(
                                    MkZingSeq(MkZingAssign(found, MkZingCall(MkZingDot("other", "Contains"), MkZingDot(iter, "key"))),
                                              MkZingIfThen(MkZingApply(ZingData.Cnst_Not, found), MkZingReturn(ZingData.Cnst_False)),
                                              MkZingAssign(val, MkZingCall(MkZingDot("other", "Lookup"), MkZingDot(iter, "key"))),
                                              MkZingIfThen(MkZingApply(ZingData.Cnst_Not, MkZingEq(ctxt, val, otherT.ValT, MkZingDot(iter, "val"), meT.ValT)), MkZingReturn(ZingData.Cnst_False))
                                              ))),
                MkZingReturn(ZingData.Cnst_True));
            ctxt.pushSideEffectStack();

            return MkZingMethodDecl(name, MkZingVarDecls(MkZingVarDecl("other", otherT)),
                ZingData.Cnst_Bool, ctxt.emitLocals(), MkZingBlock("dummy", ctxt.emitZingSideEffects(body)));
        }

        private AST<FuncTerm> MkZingMapCastOrClone(PMapType fromT, PMapType toT)
        {
            var name = getZingCastOrCloneName(fromT, toT);
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, name, this);
            var res = ctxt.getTmpVar(toT, "res");
            var iter = ctxt.getTmpVar(Factory.Instance.MkCnst(pTypeToZingClassName(fromT) + "_Entry"), "iter");
            var isUpcast = fromT.isSubtypeOf(toT) && toT != fromT;
            var from = isUpcast ? "obj" : "this"; // Only Upcasts are static

            var body = MkZingSeq(
                MkZingAssign(res, MkZingCall(MkZingIdentifier("BuildDefault"))),
                MkZingListIter(iter, MkZingDot("head", "next"), MkZingIdentifier("head"),
                               MkZingCallStmt(MkZingCall(MkZingDot(res, "Update"), MkZingDot(iter, "key"), MkZingDot(iter, "val")))),
                MkZingReturn(res));

            if (isUpcast)
                return MkZingMethodDecl(name, MkZingVarDecls(MkZingVarDecl(from, fromT)), pTypeToZingType(toT), ctxt.emitLocals(),
                    MkZingBlock("dummy", ctxt.emitZingSideEffects(body)), ZingData.Cnst_Static);
            else
                return MkZingMethodDecl(name, ZingData.Cnst_Nil, pTypeToZingType(toT), ctxt.emitLocals(),
                    MkZingBlock("dummy", ctxt.emitZingSideEffects(body)));
        }

        private AST<FuncTerm> MkZingMapCanDownCast(PMapType fromT, PMapType toT)
        {
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Clone", this);

            var iter = ctxt.getTmpVar(Factory.Instance.MkCnst(pTypeToZingClassName(fromT) + "_Entry"), "iter");
            var body = MkZingSeq(
                MkZingListIter(iter, MkZingDot("head", "next"), MkZingIdentifier("head"),
                    ctxt.emitZingSideEffects(MkZingIfThen(MkZingApply(ZingData.Cnst_Not,
                                                                  MkZingApply(ZingData.Cnst_And,
                                                                              MkZingCanDownCastTo(ctxt, MkZingDot("iter", "key"), fromT.KeyT, toT.KeyT),
                                                                              MkZingCanDownCastTo(ctxt, MkZingDot("iter", "val"), fromT.ValT, toT.ValT))),
                                                      MkZingReturn(ZingData.Cnst_False)))),
                MkZingReturn(ZingData.Cnst_True));

            ctxt.pushSideEffectStack();

            return MkZingMethodDecl(getZingCanDownCastName(toT), ZingData.Cnst_Nil, pTypeToZingType(PType.Bool), ctxt.emitLocals(),
                MkZingBlock("dummy", ctxt.emitZingSideEffects(body)));
        }

        private AST<FuncTerm> MkZingMapContains(PMapType t)
        {
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Contains", this);
            var iter = ctxt.getTmpVar(Factory.Instance.MkCnst(pTypeToZingClassName(t) + "_Entry"), "iter");
            var found = ctxt.getTmpVar(PType.Bool, "found");
            var containsBody = MkZingSeq(
                MkZingListIter(iter, MkZingDot("head", "next"), MkZingIdentifier("head"),
                               ctxt.emitZingSideEffects(MkZingIfThen(MkZingEq(ctxt, MkZingIdentifier("key"), t.KeyT, MkZingDot(iter, "key"), t.KeyT), MkZingReturn(ZingData.Cnst_True)))),
                MkZingReturn(ZingData.Cnst_False));
            ctxt.pushSideEffectStack();

            return MkZingMethodDecl("Contains", MkZingVarDecls(MkZingVarDecl("key", t.KeyT)),
                ZingData.Cnst_Bool, ctxt.emitLocals(), MkZingBlock("dummy", containsBody));
        }

        private AST<FuncTerm> MkZingMapLookup(PMapType t)
        {
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Lookup", this);
            var iter = ctxt.getTmpVar(Factory.Instance.MkCnst(pTypeToZingClassName(t) + "_Entry"), "iter");
            var found = ctxt.getTmpVar(PType.Bool, "found");
            var lookupBody = MkZingSeq(
                MkZingListIter(iter, MkZingDot("head", "next"), MkZingIdentifier("head"),
                               ctxt.emitZingSideEffects(MkZingIfThen(MkZingEq(ctxt, MkZingIdentifier("key"), t.KeyT, MkZingDot(iter, "key"), t.KeyT), MkZingReturn(MkZingDot(iter, "val"))))),
                MkZingAssert(ZingData.Cnst_False));
            ctxt.pushSideEffectStack();

            return MkZingMethodDecl("Lookup", MkZingVarDecls(MkZingVarDecl("key", t.KeyT)),
                pTypeToZingType(t.ValT), ctxt.emitLocals(), MkZingBlock("dummy", lookupBody));
        }

        private AST<FuncTerm> MkZingMapLookupKeyAtIndex(PMapType t)
        {
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "LookupKeyAtIndex", this);
            var iter = ctxt.getTmpVar(Factory.Instance.MkCnst(pTypeToZingClassName(t) + "_Entry"), "iter");
            var i = ctxt.getTmpVar(PType.Int, "i");
            var lookupBody = MkZingSeq(
                MkZingAssert(MkZingApply(ZingData.Cnst_Le, Factory.Instance.MkCnst(0), MkZingIdentifier("index"))),
                MkZingAssert(MkZingApply(ZingData.Cnst_Lt, MkZingIdentifier("index"), MkZingIdentifier("size"))),
                MkZingAssign(iter, MkZingDot("head", "next")),
                MkZingAssign(i, Factory.Instance.MkCnst(0)),
                MkZingIfThen(MkZingApply(ZingData.Cnst_Le, MkZingIdentifier("iterCacheIndex"), MkZingIdentifier("index")),
                         MkZingSeq(MkZingAssign(iter, MkZingIdentifier("iterCache")), MkZingAssign(i, MkZingIdentifier("iterCacheIndex")))),
                AddArgs(ZingData.App_While,
                        MkZingApply(ZingData.Cnst_Lt, i, MkZingIdentifier("index")),
                        MkZingSeq(MkZingAssign(iter, MkZingDot(iter, "next")),
                        MkZingAssignOrCast(i, PType.Int, MkZingPlus(i, 1), PType.Int))),
                MkZingAssert(MkZingApply(ZingData.Cnst_NEq, iter, MkZingIdentifier("head"))),
                MkZingAssign(MkZingIdentifier("iterCache"), iter),
                MkZingAssign(MkZingIdentifier("iterCacheIndex"), MkZingIdentifier("index")),
                MkZingReturn(MkZingDot(iter, "key")));
            ctxt.pushSideEffectStack();

            return MkZingMethodDecl("LookupKeyAtIndex", MkZingVarDecls(MkZingVarDecl("index", PType.Int)),
                pTypeToZingType(t.KeyT), ctxt.emitLocals(), MkZingBlock("dummy", lookupBody));
        }

        private AST<FuncTerm> MkZingMapUpdate(PMapType t)
        {
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Update", this);
            var prev = ctxt.getTmpVar(Factory.Instance.MkCnst(pTypeToZingClassName(t) + "_Entry"), "prev");
            var iter = ctxt.getTmpVar(Factory.Instance.MkCnst(pTypeToZingClassName(t) + "_Entry"), "iter");
            var found = ctxt.getTmpVar(PType.Bool, "found");
            var insertBody = MkZingSeq(
                MkZingAssign(prev, MkZingIdentifier("head")),
                MkZingListIter(iter, MkZingDot("head", "next"), MkZingIdentifier("head"),
                               ctxt.emitZingSideEffects(
                                    MkZingSeq(MkZingIfThen(MkZingEq(ctxt, MkZingIdentifier("key"), t.KeyT, MkZingDot(iter, "key"), t.KeyT), MkZingSeq(MkZingAssign(MkZingDot(iter, "val"), MkZingIdentifier("val")), MkZingReturn(ZingData.Cnst_Nil))),
                                              MkZingAssign(prev, iter)))),
                MkZingAssign(MkZingDot("head", "key"), MkZingIdentifier("key")),
                MkZingAssign(MkZingDot("head", "val"), MkZingIdentifier("val")),
                MkZingAssign(MkZingIdentifier("head"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst(pTypeToZingClassName(t) + "_Entry"), ZingData.Cnst_Nil)),
                MkZingAssign(MkZingDot("head", "next"), iter),
                MkZingAssign(MkZingDot(prev, "next"), MkZingIdentifier("head")),
                MkZingAssign(MkZingIdentifier("size"), MkZingApply(ZingData.Cnst_Add, MkZingIdentifier("size"), Factory.Instance.MkCnst(1)))
                );
            ctxt.pushSideEffectStack();

            return MkZingMethodDecl("Update", MkZingVarDecls(MkZingVarDecl("key", t.KeyT), MkZingVarDecl("val", t.ValT)),
                ZingData.Cnst_Void, ctxt.emitLocals(), MkZingBlock("dummy", insertBody));
        }

        private AST<FuncTerm> MkZingMapRemove(PMapType t)
        {
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Remove", this);
            var prev = ctxt.getTmpVar(Factory.Instance.MkCnst(pTypeToZingClassName(t) + "_Entry"), "prev");
            var iter = ctxt.getTmpVar(Factory.Instance.MkCnst(pTypeToZingClassName(t) + "_Entry"), "iter");
            var found = ctxt.getTmpVar(PType.Bool, "found");
            var removeBody = MkZingSeq(
                MkZingAssign(prev, MkZingIdentifier("head")),
                MkZingListIter(iter, MkZingDot("head", "next"), MkZingIdentifier("head"),
                               ctxt.emitZingSideEffects(
                                    MkZingSeq(MkZingIfThen(MkZingEq(ctxt, MkZingIdentifier("key"), t.KeyT, MkZingDot(iter, "key"), t.KeyT), MkZingSeq(
                                                                MkZingAssign(MkZingDot(prev, "next"), MkZingDot(iter, "next")),
                                                                MkZingAssign(MkZingIdentifier("size"), MkZingApply(ZingData.Cnst_Sub, MkZingIdentifier("size"), Factory.Instance.MkCnst(1))),
                                                                MkZingAssign(MkZingIdentifier("iterCache"), MkZingDot("head", "next")),
                                                                MkZingAssign(MkZingIdentifier("iterCacheIndex"), Factory.Instance.MkCnst(0)),
                                                                MkZingReturn(ZingData.Cnst_Nil))),
                                              MkZingAssign(prev, iter))))
                );
            ctxt.pushSideEffectStack();

            return MkZingMethodDecl("Remove", MkZingVarDecls(MkZingVarDecl("key", t.KeyT)),
                ZingData.Cnst_Void, ctxt.emitLocals(), MkZingBlock("dummy", removeBody));
        }

        private AST<FuncTerm> MkZingMapToSeq(PMapType fromT)
        {
            PSeqType toT = new PSeqType(fromT.KeyT);
            var name = "ToSeq";
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, name, this);
            var res = ctxt.getTmpVar(toT, "res");
            var iter = ctxt.getTmpVar(Factory.Instance.MkCnst(pTypeToZingClassName(fromT) + "_Entry"), "iter");
            var iterCount = ctxt.getTmpVar(PType.Int, "iterCount");
            var isUpcast = fromT.isSubtypeOf(toT) && toT != fromT;
            var from = isUpcast ? "obj" : "this"; // Only Upcasts are static

            var listIterBody =  MkZingSeq(MkZingCallStmt(MkZingCall(MkZingDot(res, "Insert"), iterCount, MkZingDot(iter, "key"))),
                                          MkZingAssign(iterCount, MkZingApply(ZingData.Cnst_Add, iterCount, Factory.Instance.MkCnst(1))));

            var body = MkZingSeq(
                MkZingAssign(res, MkZingCall(MkZingDot(pTypeToZingClassName(toT), "BuildDefault"))),
                MkZingAssign(iterCount, Factory.Instance.MkCnst(0)),
                MkZingListIter(iter, MkZingDot("head", "next"), MkZingIdentifier("head"), listIterBody),
                MkZingReturn(res));

            if (isUpcast)
                return MkZingMethodDecl(name, MkZingVarDecls(MkZingVarDecl(from, fromT)), pTypeToZingType(toT), ctxt.emitLocals(),
                    MkZingBlock("dummy", ctxt.emitZingSideEffects(body)), ZingData.Cnst_Static);
            else
                return MkZingMethodDecl(name, ZingData.Cnst_Nil, pTypeToZingType(toT), ctxt.emitLocals(),
                    MkZingBlock("dummy", ctxt.emitZingSideEffects(body)));
        }

        private void MkZingMapClassDefinition(PMapType t, out AST<FuncTerm> mapClass, out AST<FuncTerm> mapEntryClass)
        {
            var zType = pTypeToZingType(t);
            var domainType = pTypeToZingType(t.KeyT);
            var rangeType = pTypeToZingType(t.ValT);

            var entryClassName = Factory.Instance.MkCnst(pTypeToZingClassName(t) + "_Entry");
            var entryClassFields = ConstructList(ZingData.App_VarDecls, MkZingVarDecl("next", entryClassName), MkZingVarDecl("key", domainType), MkZingVarDecl("val", rangeType));
            mapEntryClass = AddArgs(ZingData.App_ClassDecl, entryClassName, entryClassFields, ZingData.Cnst_Nil);

            var fields = ConstructList(ZingData.App_VarDecls, MkZingVarDecl("head", entryClassName), MkZingVarDecl("size", pTypeToZingType(PType.Int)), MkZingVarDecl("iterCache", Factory.Instance.MkCnst(pTypeToZingClassName(t) + "_Entry")), MkZingVarDecl("iterCacheIndex", PType.Int));

            var methods = new List<AST<Node>>();

            // Create the BuildDefault Method
            ZingEntryFun_FoldContext buildDefCtxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "BuildDefault", this);
            var buildDefRes = buildDefCtxt.getTmpVar(t, "res");
            var buildDefHead = buildDefCtxt.getTmpVar(entryClassName, "head");
            var buildDefBody = MkZingSeq(
                                    MkZingAssign(buildDefRes, AddArgs(ZingData.App_New, Factory.Instance.MkCnst(pTypeToZingName(t)), ZingData.Cnst_Nil)),
                                    MkZingAssign(buildDefHead, AddArgs(ZingData.App_New, Factory.Instance.MkCnst(pTypeToZingClassName(t) + "_Entry"), ZingData.Cnst_Nil)),
                                    MkZingAssign(MkZingDot(buildDefHead, "next"), buildDefHead),
                                    MkZingAssign(MkZingDot(buildDefRes, "head"), buildDefHead),
                                    MkZingAssign(MkZingDot(buildDefRes, "size"), Factory.Instance.MkCnst(0)),
                                    MkZingAssign(MkZingDot(buildDefRes, "iterCache"), buildDefHead),
                                    MkZingAssign(MkZingDot(buildDefRes, "iterCacheIndex"), Factory.Instance.MkCnst(0)),
                                    AddArgs(ZingData.App_Return, buildDefRes));
            methods.Add(MkZingMethodDecl("BuildDefault", ZingData.Cnst_Nil, zType, buildDefCtxt.emitLocals(), // Local variables
                MkZingBlock("dummy", buildDefCtxt.emitZingSideEffects(buildDefBody)), ZingData.Cnst_Static));

            // Create the Clone Method
            methods.Add(MkZingMapCastOrClone(t, t));

            // Create the Equals Method
            methods.Add(MkZingMapEqMethod(t, t));

            foreach (var subT in subtypes[t])
            {
                Debug.Assert(subT is PMapType);
                // Emit "Equals_<subtype>" method
                methods.Add(MkZingMapEqMethod(t, (PMapType)subT));

                // Emit "UpCastFrom_<SubT>" method
                methods.Add(MkZingMapCastOrClone((PMapType)subT, t));

                // Emit the "DownCatTo_<SubT>" method
                methods.Add(MkZingMapCastOrClone(t, (PMapType)subT));

                // Emit the "CanDownCastTo_<SubT>" method
                methods.Add(MkZingMapCanDownCast(t, (PMapType)subT));
            }

            methods.Add(MkZingMapContains(t));

            methods.Add(MkZingMapLookup(t));

            methods.Add(MkZingMapLookupKeyAtIndex(t));

            methods.Add(MkZingMapUpdate(t));

            methods.Add(MkZingMapRemove(t));

            methods.Add(MkZingMapToSeq(t));

            mapClass = AddArgs(ZingData.App_ClassDecl, Factory.Instance.MkCnst(pTypeToZingName(t)), fields, ConstructList(ZingData.App_MethodDecls, methods));
        }

        private AST<FuncTerm> MkZingTupleClassDefinition(IEnumerable<Tuple<PType, string, string>> fieldDesc, AST<Node> zType, string zTypeName, PType pType)
        {
            var fields = ConstructList(ZingData.App_VarDecls, fieldDesc.Select(field => MkZingVarDecl(field.Item2, pTypeToZingType(field.Item1))));
            var methods = new List<AST<Node>>();
            var buildParams = ConstructList(ZingData.App_VarDecls, fieldDesc.Select(field => MkZingVarDecl(field.Item3, pTypeToZingType(field.Item1))));

            // Create the Build method
            var buildBody = new List<AST<Node>>();

            buildBody.Add(MkZingAssign(MkZingIdentifier("result"), AddArgs(ZingData.App_New, zType, ZingData.Cnst_Nil)));
            buildBody.AddRange(fieldDesc.Select(fDesc => MkZingAssignOrCast(MkZingDot("result", fDesc.Item2), fDesc.Item1, MkZingIdentifier(fDesc.Item3), fDesc.Item1)));
            buildBody.Add(AddArgs(ZingData.App_Return, MkZingIdentifier("result")));
            methods.Add(MkZingMethodDecl("Build", buildParams, zType,
                AddArgs(ZingData.App_VarDecls, MkZingVarDecl("result", zType), ZingData.Cnst_Nil), // Local variables
                MkZingBlock("dummy", ConstructList(ZingData.App_Seq, buildBody)), ZingData.Cnst_Static));

            // Create the BuildDefault method
            ZingEntryFun_FoldContext buildDefCtxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "BuildDefault", this);
            var buildDefAssigns = new List<AST<Node>>();
            foreach (var fDesc in fieldDesc)
            {
                buildDefAssigns.Add(buildDefCtxt.emitZingSideEffects(MkZingAssign(MkZingDot("result", fDesc.Item2), getZingDefault(buildDefCtxt, fDesc.Item1))));
                buildDefCtxt.pushSideEffectStack();
            }
            var buildDefBody = MkZingBlock("dummy", MkZingSeq(
                                    MkZingAssign(MkZingIdentifier("result"), AddArgs(ZingData.App_New, zType, ZingData.Cnst_Nil)),
                                    MkZingSeq(buildDefAssigns),
                                    AddArgs(ZingData.App_Return, MkZingIdentifier("result"))));
            methods.Add(MkZingMethodDecl("BuildDefault", ZingData.Cnst_Nil, zType,
                AddArgs(ZingData.App_VarDecls, MkZingVarDecl("result", zType), buildDefCtxt.emitLocals()), // Local variables
                buildDefBody, ZingData.Cnst_Static));

            // Create the "Clone" method.
            var cloneBuildArgs = new List<AST<Node>>(fieldDesc.Select(fDesc => MkZingDot("this", fDesc.Item2)));

            var cloneBody = MkZingBlock("dummy", MkZingSeq(
                MkZingAssign(MkZingIdentifier("result"), MkZingCall(MkZingDot(zType, "Build"), cloneBuildArgs)),
                AddArgs(ZingData.App_Return, MkZingIdentifier("result"))));
            var cloneLocals = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("result", zType), ZingData.Cnst_Nil);
            methods.Add(MkZingMethodDecl("Clone", ZingData.Cnst_Nil, zType, cloneLocals, cloneBody));

            // Create the "Equals" method.
            var equalsBody = new List<AST<Node>>();
            ZingEntryFun_FoldContext equalsCtxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Equals", this);
            foreach (var field in fieldDesc)
            {
                var check = MkZingNeq(equalsCtxt, MkZingDot("this", field.Item2), field.Item1, MkZingDot("other", field.Item2), field.Item1);
                equalsBody.Add(equalsCtxt.emitZingSideEffects(MkZingIfThenElse(check, AddArgs(ZingData.App_Return, ZingData.Cnst_False), ZingData.Cnst_Nil)));
                equalsCtxt.pushSideEffectStack();
            }

            equalsBody.Add(AddArgs(ZingData.App_Return, ZingData.Cnst_True));

            methods.Add(MkZingMethodDecl("Equals", AddArgs(ZingData.App_VarDecls, MkZingVarDecl("other", zType), ZingData.Cnst_Nil),
                ZingData.Cnst_Bool, equalsCtxt.emitLocals(), MkZingBlock("dummy", ConstructList(ZingData.App_Seq, equalsBody))));

            // For all subtypes, emit the DownCastTo and UpCastFrom methods
            foreach (var subT in subtypes[pType])
            {
                ZingEntryFun_FoldContext ctxt;
                List<AST<Node>> buildArgs;

                var subFields = getFieldDescriptions(subT);
                Debug.Assert(subFields.Count() == fieldDesc.Count());

                // Emit dynamic "Equals_<subtype>" method
                ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, getZingEqualsName(subT), this);
                equalsBody = new List<AST<Node>>();

                for (int i = 0; i < fieldDesc.Count(); i++)
                {
                    var subF = subFields.ElementAt(i);
                    var supF = fieldDesc.ElementAt(i);

                    equalsBody.Add(ctxt.emitZingSideEffects(MkZingIfThen(MkZingNeq(ctxt, MkZingDot("this", supF.Item2), supF.Item1,
                        MkZingDot("other", subF.Item2), subF.Item1), MkZingReturn(ZingData.Cnst_False))));
                    ctxt.pushSideEffectStack();
                }

                equalsBody.Add(MkZingReturn(ZingData.Cnst_True));
                methods.Add(MkZingMethodDecl(getZingEqualsName(subT), MkZingVarDecls(MkZingVarDecl("other", pTypeToZingType(subT))), pTypeToZingType(PType.Bool),
                    ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(equalsBody))));

                // Emit static 'UpCastFrom' Method
                ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, getZingUpCastName(subT), this);
                buildArgs = new List<AST<Node>>();
                AST<Node> resVar, body;

                for (int i = 0; i < fieldDesc.Count(); i++)
                {
                    var subF = subFields.ElementAt(i);
                    var supF = fieldDesc.ElementAt(i);

                    if (subF.Item1 == supF.Item1)
                    {
                        buildArgs.Add(MkZingDot("other", subF.Item2));
                    }
                    else if (!(subF.Item1 is PNilType))
                    {
                        var tmpVar = ctxt.getTmpVar(supF.Item1, "tmp");
                        ctxt.addSideEffect(MkZingAssign(tmpVar,
                            MkZingCall(MkZingDot(pTypeToZingClassName(supF.Item1), getZingUpCastName(subF.Item1)), MkZingDot("other", subF.Item2))));
                        buildArgs.Add(tmpVar);
                    }
                    else
                    {
                        Debug.Assert(supF.Item1 is PAnyType || supF.Item1 is PEventType || supF.Item1.IsMachineId);
                        buildArgs.Add(getZingDefault(ctxt, supF.Item1));
                    }
                }

                resVar = ctxt.getTmpVar(pType, "res");
                body = MkZingSeq(
                    ctxt.emitZingSideEffects(MkZingAssign(resVar, MkZingCall(MkZingDot(pTypeToZingClassName(pType), "Build"), buildArgs))),
                    AddArgs(ZingData.App_Return, resVar));

                methods.Add(MkZingMethodDecl(getZingUpCastName(subT), MkZingVarDecls(MkZingVarDecl("other", pTypeToZingType(subT))), zType,
                    ctxt.emitLocals(), MkZingBlock("dummy", body), ZingData.Cnst_Static));

                // Emit 'DownCastTo' Method
                ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, getZingDownCastName(subT), this);
                buildArgs = new List<AST<Node>>();

                for (int i = 0; i < fieldDesc.Count(); i++)
                {
                    var subF = subFields.ElementAt(i);
                    var supF = fieldDesc.ElementAt(i);

                    if (subF.Item1 == supF.Item1)
                    {
                        buildArgs.Add(MkZingDot("this", supF.Item2));
                    }
                    else
                    {
                        var tmpVar = ctxt.getTmpVar(subF.Item1, "tmp");
                        ctxt.addSideEffect(MkZingAssignOrCast(tmpVar, subF.Item1, MkZingDot("this", supF.Item2), supF.Item1));
                        buildArgs.Add(tmpVar);
                    }
                }

                resVar = ctxt.getTmpVar(subT, "res");
                body = MkZingSeq(
                    ctxt.emitZingSideEffects(MkZingAssign(resVar, MkZingCall(MkZingDot(pTypeToZingClassName(subT), "Build"), buildArgs))),
                    AddArgs(ZingData.App_Return, resVar));

                methods.Add(MkZingMethodDecl(getZingDownCastName(subT), ZingData.Cnst_Nil, pTypeToZingType(subT),
                    ctxt.emitLocals(), MkZingBlock("dummy", body)));

                // Emit 'CanDownCastTo' Method
                ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, getZingCanDownCastName(subT), this);
                var checks = new List<AST<Node>>();

                for (int i = 0; i < fieldDesc.Count(); i++)
                {
                    var toF = subFields.ElementAt(i);
                    var fromF = fieldDesc.ElementAt(i);

                    if (fromF.Item1 == toF.Item1)
                    {
                        // Nothing to do.
                    }
                    else
                    {
                        var cond = MkZingApply(ZingData.Cnst_Not, MkZingCanDownCastTo(ctxt, MkZingDot("this", fromF.Item2), fromF.Item1, toF.Item1));
                        checks.Add(ctxt.emitZingSideEffects(MkZingIfThenElse(cond, AddArgs(ZingData.App_Return, ZingData.Cnst_False), ZingData.Cnst_Nil)));
                        ctxt.pushSideEffectStack();
                    }
                }

                checks.Add(AddArgs(ZingData.App_Return, ZingData.Cnst_True));
                methods.Add(MkZingMethodDecl(getZingCanDownCastName(subT), ZingData.Cnst_Nil, pTypeToZingType(PType.Bool),
                    ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(checks))));

            }

            return AddArgs(ZingData.App_ClassDecl, Factory.Instance.MkCnst(zTypeName), fields, ConstructList(ZingData.App_MethodDecls, methods));
        }

        private IEnumerable<Tuple<PType, string, string>> getFieldDescriptions(PType t)
        {
            if (t is PTupleType)
            {
                int fcount = (t as PTupleType).elements.Count();
                return (t as PTupleType).elements.Zip(System.Linq.Enumerable.Range(0, fcount), (type, num) => new Tuple<PType, string, string>(type, getTupleField(num), getFuncArg(num)));
            }
            else if (t is PNamedTupleType)
            {
                return (t as PNamedTupleType).elements.Select(elmnt => new Tuple<PType, string, string>(elmnt.Item2, elmnt.Item1, "arg_" + elmnt.Item1));
            }
            else
                throw new NotImplementedException(string.Format("Unexpected type {0} in getFieldDescriptions.", t));
        }

        private AST<Node> LocalVariablesToVarDecls(IEnumerable<string> varNames, Dictionary<string, VariableInfo> varNameToVarInfo)
        {
            return MkZingVarDecls(varNames.Select(name => MkZingVarDecl(name, pTypeToZingType(varNameToVarInfo[name].type))));
        }

        private AST<Node> MkInvokeMonitorMethod(string machineName)
        {
            var parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("arg", Factory.Instance.MkCnst("SM_ARG_UNION")), ZingData.Cnst_Nil);
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
                if (t is PIntType)
                {
                    initializers.Add(MkZingAssign(MkZingDot(obj, varName), Factory.Instance.MkCnst(0)));
                }
                else if (t is PBoolType)
                {
                    initializers.Add(MkZingAssign(MkZingDot(obj, varName), MkZingIdentifier("false")));
                }
                else if (t.IsMachineId)
                {
                    initializers.Add(MkZingAssign(MkZingDot(obj, varName), MkZingIdentifier("null")));
                }
                else if (t is PEventType)
                {
                    initializers.Add(MkZingAssign(MkZingDot(obj, varName), MkZingIdentifier("null")));
                }
                else if (t is PNilType)
                {
                    initializers.Add(MkZingAssign(MkZingDot(obj, varName), MkZingIdentifier("null")));
                }
                else if (t is PAnyType)
                {
                    initializers.Add(MkZingAssign(MkZingDot(obj, varName), MkZingCall(MkZingDot(SM_ARG_UNION, "BuildDefault"))));
                }
                else if (t is PTupleType || t is PNamedTupleType || t is PSeqType || t is PMapType)
                {
                    initializers.Add(MkZingAssign(MkZingDot(obj, varName), MkZingCall(MkZingDot(pTypeToZingType(t), "BuildDefault"))));
                }
                else
                {
                    throw new NotImplementedException("Unknown type " + t);
                }
            }
            return MkZingSeq(initializers);
        }

        private AST<Node> MkCreateMonitorMethod(string machineName)
        {
            var objectName = string.Format("o_{0}", machineName);
            var parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("arg", Factory.Instance.MkCnst("SM_ARG_UNION")), ZingData.Cnst_Nil);
            var localVars = AddArgs(ZingData.App_VarDecls, MkZingVarDecl(objectName, Factory.Instance.MkCnst(machineName)), ZingData.Cnst_Nil);

            AST<Node> body = ZingData.Cnst_Nil;
            if (compiler.liveness) 
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
            var parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("arg", Factory.Instance.MkCnst("SM_ARG_UNION")), ZingData.Cnst_Nil);
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

            if (compiler.liveness)
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
                foreach (var actionName in allMachines[machineName].actionFunNameToActionFun.Keys)
                {
                    var actionInfo = allMachines[machineName].actionFunNameToActionFun[actionName];
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

        private AST<Node> pTypeToZingType(PType t)
        {
            if (t is PNilType)
            {
                return Factory.Instance.MkCnst(SM_NULL);
            }
            else if (t is PPrimitiveType)
            {
                return ZingData.pTypeToZingType((t as PPrimitiveType).name);
            }
            else if (t is PTupleType || t is PNamedTupleType || t is PSeqType || t is PMapType)
            {
                return Factory.Instance.MkCnst(declaredTypes[t].zingType);
            }
            else if (t is PAnyType)
            {
                return Factory.Instance.MkCnst(SM_ARG_UNION);
            }
            else
            {
                throw new NotImplementedException("Unknown type " + t);
            }
        }

        private string pTypeToZingClassName(PType t)
        {
            if (t is PTupleType || t is PNamedTupleType || t is PSeqType || t is PMapType)
                return declaredTypes[t].zingType;
            else if (t is PAnyType)
                return SM_ARG_UNION;
            else if (t is PNilType)
                return SM_NULL;
            else
                throw new Exception("Unexpected type " + t);
        }

        private string pTypeToZingName(PType t)
        {
            if (t is PIntType)
            {
                return "Int";
            }
            else if (t is PBoolType)
            {
                return "Bool";
            }
            else if (t is PIdType)
            {
                return "Id";
            }
            else if (t is PMidType)
            {
                return "Mid";
            }
            else if (t is PEventType)
            {
                return "Eid";
            }
            else if (t is PNilType)
            {
                return "Null";
            }
            else if (t is PAnyType)
            {
                return "Any";
            }
            else if (t is PTupleType || t is PNamedTupleType || t is PSeqType || t is PMapType)
            {
                return declaredTypes[t].zingType;
            }
            else
            {
                throw new NotImplementedException("Unknown type " + t);
            }
        }

        private string pTypeToZingUnionMember(PType t)
        {
            return "m_" + pTypeToZingName(t);
        }

        private AST<Node> pTypeToZingDiscriminator(PType t)
        {
            return MkZingDot("Discriminator", pTypeToZingName(t));
        }

        private string getZingUpCastName(PType t)
        {
            return "UpCastFrom_" + pTypeToZingName(t);
        }

        private string getZingEqualsName(PType t)
        {
            return "Equals_" + pTypeToZingName(t);
        }

        private string getZingDownCastName(PType t)
        {
            return "DownCastTo_" + pTypeToZingName(t);
        }

        private string getZingCanDownCastName(PType t)
        {
            return "CanDownCastTo_" + pTypeToZingName(t);
        }

        private string getZingCastOrCloneName(PType fromT, PType toT)
        {
            if (fromT == toT) return "Clone";
            else if (fromT.isSubtypeOf(toT)) return getZingUpCastName(fromT); // static toT UpCastFrom_<formT>(fromT from)
            else return getZingDownCastName(toT);   // toT DownCastTo_<ToT>()
        }

        private AST<Node> getZingDefault(ZingEntryFun_FoldContext ctxt, PType t)
        {
            if (t is PIntType)
            {
                return Factory.Instance.MkCnst(0);
            }
            else if (t is PBoolType)
            {
                return MkZingIdentifier("false");
            }
            else if (t.IsMachineId)
            {
                return MkZingIdentifier("null");
            }
            else if (t is PEventType)
            {
                return MkZingIdentifier("null");
            }
            else if (t is PNilType)
            {
                return MkZingIdentifier("null");
            }
            else if (t is PAnyType)
            {
                var tmpVar = ctxt.getTmpVar(Factory.Instance.MkCnst(SM_ARG_UNION), "tmpDefault");
                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(SM_ARG_UNION, "BuildDefault"))));

                return tmpVar;
            }
            else if (t is PTupleType || t is PNamedTupleType || t is PSeqType || t is PMapType)
            {
                var tmpVar = ctxt.getTmpVar(pTypeToZingType(t), "tmpDefault");
                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(pTypeToZingType(t), "BuildDefault"))));

                return tmpVar;
            }
            else
            {
                throw new NotImplementedException("Unknown type " + t);
            }

        }
    }
}
