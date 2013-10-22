namespace DemoCompiler
{
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

    public class MyDictionary<S, T>
    {
        private Dictionary<S, T> dictionary = 
            new Dictionary<S, T>();

        public T this[S key]
        {
            get
            {
                T val;
                if (!dictionary.TryGetValue(key, out val))
                {
                    throw new MissingKeyException<S>(key);
                }

                return val;
            }

            set
            {
                dictionary[key] = value;
            }
        }

        public void Add(S key, T value)
        {
            dictionary.Add(key, value);
        }

        public bool TryGetValue(S key, out T value)
        {
            return dictionary.TryGetValue(key, out value);
        }
    }

    internal class MissingKeyException<S> : Exception
    {

        public S MissingKey
        {
            get;
            private set;
        }

        public MissingKeyException(S key)
            : base(string.Format("Could not find key {0}", key))
        {
            MissingKey = key;        
        }
    }

    internal class CTranslationInfo
    {
        public AST<Node> node;

        public CTranslationInfo(AST<Node> n)
        {
            this.node = n;
        }
    }

    internal class ZingTranslationInfo
    {
        public AST<Node> node;
        public PType type;
        public bool isGhost;
        public string stateName;
        public bool isKeys;
        public bool isNew;
        public string newAfterLabel;

        private PType primitiveTypeFromStr(string s)
        {
            switch (s)
            {   // Its fairly annoying that we can't use PData.Cnst_*.Node.Name here (switches require constants). Need to keep this in sync..
                case "NIL": return new PNilType();
                case "BOOL": return new PBoolType(); 
                case "INT": return new PIntType();
                case "ID": return new PIdType();
                case "EVENT": return new PEventType(null);
                case "STATE": return new PStateType();
                default:
                    throw new NotImplementedException("Unknown primitive type " + s);
            }
        }

        public ZingTranslationInfo(AST<Node> n, PType t)
        {
            this.node = n;
            this.type = t;
            this.isGhost = false;
            this.stateName = null;
            this.isKeys = false;
            this.isNew = false;
            this.newAfterLabel = null;
        }

        public ZingTranslationInfo(AST<Node> n, PType t, bool isGhost)
        {
            this.node = n;
            this.type = t;
            this.isGhost = isGhost;
            this.stateName = null;
            this.isKeys = false;
            this.isNew = false;
            this.newAfterLabel = null;
        }

        public ZingTranslationInfo(AST<Node> n, PType t, bool isGhost, string stateName)
        {
            this.node = n;
            this.type = t;
            this.isGhost = isGhost;
            this.stateName = stateName;
            this.isKeys = false;
            this.isNew = false;
            this.newAfterLabel = null;
        }

        public ZingTranslationInfo(AST<Node> n, PType t, bool isGhost, bool isNew, string afterlabel)
        {
            this.node = n;
            this.type = t;
            this.isGhost = isGhost;
            this.stateName = null;
            this.isKeys = false;
            this.isNew = isNew;
            this.newAfterLabel = afterlabel;
        }

        public ZingTranslationInfo(AST<Node> n, PType t, bool isGhost, string stateName, bool isKeys)
        {
            this.node = n;
            this.type = t;
            this.isGhost = isGhost;
            this.stateName = stateName;
            this.isKeys = isKeys;
            this.isNew = false;
            this.newAfterLabel = null;
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
        public int nIncommingTransitions;
        public string submachineName;

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
            this.nIncommingTransitions = 0;
        }
    }

    internal class VariableInfo
    {
        public bool isGhost;
        public PType type;

        public VariableInfo(PType type, bool isGhost)
        {
            this.type = type;
            this.isGhost = isGhost;
        }
    }

    internal class FunInfo
    {
        public bool isForeign;
        public Dictionary<string, VariableInfo> parameterNameToInfo;
        public List<string> parameterNames;
        public PType returnType;
        public FuncTerm funDecl;
        public HashSet<string> callers;
        public bool atPassive;
        public FunInfo(bool isForeign, PType returnType, FuncTerm funDecl)
        {
            this.isForeign = isForeign;
            parameterNameToInfo = new Dictionary<string, VariableInfo>();
            parameterNames = new List<string>();
            this.returnType = returnType;
            this.funDecl = funDecl;
            this.callers = new HashSet<string>();
        }
    }

    internal class ActionInfo
    {
        public Node actionFun;
        public HashSet<PType> argTypes;
        public HashSet<string> callees;
        public bool atPassive;
        public ActionInfo(Node actionFun)
        {
            this.actionFun = actionFun;
            this.callees = new HashSet<string>();
            this.argTypes = new HashSet<PType>();
        }
    }

    internal class MachineInfo
    {
        public bool isGhost;
        public int maxQueueSize;
        public FuncTerm initStateDecl;
        public Dictionary<string, StateInfo> stateNameToStateInfo;
        public Dictionary<string, VariableInfo> localVariableToVarInfo;
        public Dictionary<string, List<string>> eventSetNameToEvents;
        public Dictionary<string, FunInfo> funNameToFunInfo;
        public Dictionary<string, ActionInfo> actionFunNameToActionFun;

        public MachineInfo()
        {
            isGhost = false;
            maxQueueSize = -1;
            initStateDecl = null;
            stateNameToStateInfo = new Dictionary<string, StateInfo>();
            localVariableToVarInfo = new Dictionary<string, VariableInfo>();
            eventSetNameToEvents = new Dictionary<string, List<string>>();
            funNameToFunInfo = new Dictionary<string, FunInfo>();
            actionFunNameToActionFun = new Dictionary<string, ActionInfo>();
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

    internal class Compiler
    {
        private const string pdataDomainStr = "PData";
        private const string cdataDomainStr = "CData";
        private const string zingdataDomainStr = "ZingData";

        private const string cRenderDefaultsStr = "CRenderDefaults.4ml";
        private const string cRenderDefaultsDebugStr = "CRenderDefaults_Debug.4ml";
        private const string cParserDefaultsStr = "CParserDefaults.4ml";
        private const string zingRenderDefaultsStr = "ZingRenderDefaults.4ml";

        private string target;
        private string outputPath;
        private bool erase;
        private bool kernelMode;
        private bool emitHeaderComment;
        private bool emitDebugC;
        private AST<Model> model = null;
        private string domainPath = null;

        private int nextOutputId = 0;

        private MyDictionary<string, AST<FuncTerm>> modelAliases = new MyDictionary<string, AST<FuncTerm>>();

        private FuncTerm GetFuncTerm(Node node)
        {
            if (node.NodeKind == NodeKind.FuncTerm)
                return (FuncTerm)node;
            else if (node.NodeKind == NodeKind.Id)
                return modelAliases[((Id)node).Name].Node;
            Debug.Assert(false);
            return null;
        }

        private Dictionary<string, LinkedList<AST<FuncTerm>>> factBins =
            new Dictionary<string, LinkedList<AST<FuncTerm>>>();

        // TODO: I don't think theres actually a need for the *Ids vars to be global.
        private Dictionary<string, int> modelMachineIds =
            new Dictionary<string, int>();

        private Dictionary<string, int> modelEventIds = 
            new Dictionary<string, int>();

        private Dictionary<string, int> modelEventSetIds =
            new Dictionary<string, int>();

        private Dictionary<string, int> modelStateIds =
            new Dictionary<string, int>();

        private Dictionary<string, int> modelVarIds =
            new Dictionary<string, int>();

        private Dictionary<string, int> modelTransSizes =
            new Dictionary<string, int>();


        Dictionary<Node, ZingTranslationInfo> computedType =
            new Dictionary<Node, ZingTranslationInfo>();

        private PType getComputedType(Node n)
        {
            return computedType[n].type;
        }

        public ProgramName CompilingProgram
        {
            get;
            private set;
        }

        Dictionary<string, EventInfo> allEvents;
        Dictionary<string, MachineInfo> allMachines;
        Dictionary<PType, TypeInfo> declaredTypes;
        HashSet<PType> allTypes;
        Dictionary<PType, List<PType>> subtypes, supertypes;
        Dictionary<string, int> uniqIDCounters = new Dictionary<string, int>();
        List<Flag> errors;

        private const string SM_ARG_UNION = "SM_ARG_UNION";
        private const string SM_NULL = "SM_NULL";
        private const string SMF_PACKED_VALUE = "SMF_PACKED_VALUE";
        private const string SMF_ARRAYLIST = "SMF_ARRAYLIST";
        private const string SMF_HASHTABLE = "SMF_HASHTABLE";

        private AST<FuncTerm> PSMF_DRIVERDECL_TYPE;

        public Compiler(string target, string outputPath, bool erase, bool kernelMode, bool emitHeaderComment, bool emitDebugC)
        {
            this.target = target;
            this.outputPath = outputPath;
            this.erase = erase;
            this.kernelMode = kernelMode;
            this.emitHeaderComment = emitHeaderComment;
            this.emitDebugC = emitDebugC;

            allTypes = new HashSet<PType>();
            allTypes.Add(PType.Nil);
            allTypes.Add(PType.Id);
            allTypes.Add(PType.Int);
            allTypes.Add(PType.Bool);
            allTypes.Add(PType.Event);
            allTypes.Add(PType.Any);

            declaredTypes = new Dictionary<PType, TypeInfo>();
            subtypes = new Dictionary<PType, List<PType>>();
            supertypes = new Dictionary<PType, List<PType>>();
            // Add Any Type to complex types.
            registerType(PType.Any);
            PSMF_DRIVERDECL_TYPE = MkNmdType("PSMF_DRIVERDECL");
        }

        private string DriverDeclName()
        {
            return string.Format("DriverDecl_{0}", model.Node.Name);
        }

        public bool Compile()
        {
            InstallResult result;
            var env = new Env();
            var success = env.Install(target, out result);    
            PrintResult(result);
            if (!success)
            {
                return false;
            }

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

            /*
            AST<Model> expModel;
            Microsoft.Formula.Compiler.Compiler.TryGetExpandedModel(model, model.Node.Name, out expModel);
            model = expModel;
            */

            IndexModel(model);

            errors = new List<Flag>();
            
            success = GenerateMachineData(model) && success;
            if (!success)
            {
                return false;
            }
            
            success = GenerateZing(env) && success;
            if (!success)
            {
                return false;
            }
            PropagateFlags();

            if (erase)
            {
                EraseGhostDecls();
            }

            success = GenerateC(env) && success;
            return success;
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
        
        bool GenerateZing(Env env)
        {
            var outModel = MkZingOutputModel();
            List<AST<Node>> elements = new List<AST<Node>>();
            MkZingEnums(elements);
            MkZingClasses(elements);
            if (errors.Count > 0)
            {
                PrintResult(errors);
                return false;
            }
            outModel = Add(outModel, MkZingFile("output.zing", elements));
            //outModel.Print(Console.Out);
            return PrintFile(outModel, env);
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

        string headerCmt = @"++

Copyright (c) 1990-<Year> Microsoft Corporation. All rights reserved.

Module Name:

    {0}

Abstract:

    {1}

    !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    THIS FILE WAS AUTO-GENERATED FROM THE FILE(S):

        {2}

    PLEASE DO NOT MODIFY THIS FILE.

    PLEASE DO NOT CHECK THIS FILE IN TO SOURCE CONTROL.
    !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

Generated Date:

    {3}

Environment:

    {4}
--";

        private string getCBuildName(PType t) {
            return "Build_" + declaredTypes[t].cType;
        }
        private string getCDestroyName(PType t)
        {
            if (t is PAnyType)
            {
                return "Destroy_PackedValue";
            }
            else if (t is PSeqType)
            {
                return "SmfArrayListDestroy";
            }
            else if (t is PMapType)
            {
                return "SmfHashtableDestroy";
            }
            else
                return "Destroy_" + declaredTypes[t].cType;
        }
        private string getCBuildDefName(PType t)
        {
            if (t is PAnyType)
            {
                return "BuildDefault_PackedValue";
            }
            else
                return "BuildDefault_" + declaredTypes[t].cType;
        }
        private string getCCloneName(PType t)
        {
            if (t is PAnyType)
            {
                return "Clone_PackedValue";
            }
            else if (t is PSeqType)
            {
                return "SmfArrayListClone";
            }
            else if (t is PMapType)
            {
                return "SmfHashtableClone";
            }
            else
                return "Clone_" + declaredTypes[t].cType;
        }

        private string getCHashCodeName(PType t)
        {
            return "HashCode_" + pTypeToCEnum(t);
        }

        private string getCEqualsName(PType super, PType sub)
        {
            return "Equals_" + pTypeToCEnum(super) + "_" + pTypeToCEnum(sub);
        }

        private string getCUpCastName(PType from, PType to)
        {
            return "UpCastFrom_" + pTypeToCEnum(from) + "_To_" + pTypeToCEnum(to);
        }

        private string getCDownCastName(PType from, PType to)
        {
            return "DownCastFrom_" + pTypeToCEnum(from) + "_To_" + pTypeToCEnum(to);
        }

        private string getCCanDownCastName(PType from)
        {
            return "CanDownCastFrom_" + pTypeToCEnum(from);
        }

        private string getCCanCastName(PType from)
        {
            return "CanCastFrom_" + pTypeToCEnum(from) + "_To";
        }

        private static bool typeNeedsDestroy(PType t)
        {
            if (t is PNilType)
                return false;

            if (t is PPrimitiveType)
                return false;

            if (t is PTupleType)
            {
                return ((PTupleType)t).elements.Any(elT => typeNeedsDestroy(elT));
            }

            if (t is PNamedTupleType)
            {
                return ((PNamedTupleType)t).elements.Any(elT => typeNeedsDestroy(elT.Item2));
            }

            if (t is PAnyType)
                return true;

            if (t is PSeqType || t is PMapType)
                return true;

            throw new NotImplementedException("TODO: Does " + t + " need a destructor");
        }

        private static bool typeNeedsClone(PType t)
        {
            return (!(t is PPrimitiveType));
        }

        private static bool typeNeedsBuildDefault(PType t)
        {
            return (!(t is PPrimitiveType));
        }

        private static bool typeNeedsEquals(PType t)
        {
            return (!(t is PPrimitiveType));
        }

        private static bool typeNeedsHashCode(PType t)
        {
            return (!(t is PPrimitiveType) && t.Hashable);
        }

        private AST<FuncTerm> MkTypeDecl(PType t)
        {
            var cTypeName = pTypeToCEnum(t);

            if (!(t is PTupleType || t is PNamedTupleType || t is PAnyType || t is PPrimitiveType || t is PAnyType || t is PSeqType || t is PMapType))
                throw new NotImplementedException("TODO: Unkown type " + t);

            var clone = typeNeedsClone(t) ? MkFunApp("MAKE_OPAQUE_CLONE", default(Span), MkId(getCCloneName(t))) : MkId("NULL");
            var buildDef = typeNeedsBuildDefault(t) ? MkFunApp("MAKE_OPAQUE_BUILDDEF", default(Span), MkId(getCBuildDefName(t))) : MkId("NULL");
            var destroy = typeNeedsDestroy(t) ? MkFunApp("MAKE_OPAQUE_DESTROY", default(Span), MkId(getCDestroyName(t))) : MkId("NULL");
            var equals = typeNeedsEquals(t) ? MkFunApp("MAKE_OPAQUE_EQUALS", default(Span), MkId(getCEqualsName(t, t))) : MkId("NULL");
            var hashCode = typeNeedsHashCode(t) ? MkFunApp("MAKE_OPAQUE_HASHCODE", default(Span), MkId(getCHashCodeName(t))) : MkId("NULL");
            var cPrimitive = t is PPrimitiveType ? MkId("TRUE") : MkId("FALSE");
            var def = t is PPrimitiveType ? GetCDefault(null, t) : MkIntLiteral(0);

            return MkInit(MkStringLiteral(cTypeName), GetCTypeSize(t), cPrimitive, def, MkId(pTypeToCEnum(t) + "SuperTypes"), MkId(pTypeToCEnum(t) + "SubTypes"), clone, buildDef, destroy, equals, hashCode);
        }

        private AST<Node> MkIf(AST<Node> cond, AST<Node> then)
        {
            return AddArgs(CData.App_ITE(), cond, then, CData.Cnst_Nil());
        }

        private AST<Node> MkIdx(AST<Node> baseE, AST<Node> idxE)
        {
            return AddArgs(CData.App_BinApp(), CData.Cnst_AAc(), baseE, idxE);
        }

        private AST<Node> MkEq(AST<Node> e1, AST<Node> e2)
        {
            return AddArgs(CData.App_BinApp(), CData.Cnst_Eq(), e1, e2);
        }

        private AST<Node> MkEq(AST<Node> driver, AST<Node> e1, PType t1, AST<Node> e2, PType t2)
        {
            PType supT, subT;
            AST<Node> supE, subE;

            if (t1 == t2)
            {
                if (t1 is PNilType) // Since Nil is a singleton type, implicitly any two expressions of this type are equal.
                {
                    return MkId("TRUE");
                } else if (t1 is PPrimitiveType)
                {
                    return AddArgs(CData.App_BinApp(), CData.Cnst_Eq(), e1, e2);
                }

                else
                {
                    return MkFunApp(getCEqualsName(t1, t2), default(Span), driver, MkAddrOf(e1), MkAddrOf(e2));
                }
            }
            else
            {
                if (t1.isSubtypeOf(t2))
                {
                    supT = t2; supE = e2;
                    subT = t1; subE = e1;
                }
                else if (t2.isSubtypeOf(t1))
                {
                    supT = t1; supE = e1;
                    subT = t2; subE = e2;
                }
                else
                    throw new Exception(string.Format("Cannot compare types {0} and {1}", t1, t2));

                if (supT is PIdType && subT is PNilType)
                {
                    return MkEq(supE, MkId("SmfNull"));
                }
                else if (supT is PEventType && subT is PNilType)
                {
                    return MkEq(supE, MkId("SmfNull"));
                }
                else
                {
                    var subArg = (subT is PPrimitiveType) ? subE : MkAddrOf(subE);
                    return MkFunApp(getCEqualsName(supT, subT), default(Span), driver, MkAddrOf(supE), subArg);
                }
            }
        }

        private AST<Node> MkNeq(AST<Node> driver, AST<Node> e1, PType t1, AST<Node> e2, PType t2)
        {
            return MkUnop(CData.Cnst_LNot(), MkEq(driver, e1, t1, e2, t2));
        }

        // Generic Types/Forward Declarations for Type Methods
        private AST<FuncTerm> MkBuildDefFunType(PType t)
        {
            return MkFunType(MkBaseType(CData.Cnst_Void()), PSMF_DRIVERDECL_TYPE, MkPtrType(pTypeToCType(t)));
        }

        private AST<FuncTerm> MkBuildDefMethod(PType t, AST<Node> body = null, AST<Node> locals = null)
        {
            if (body != null)
            {
                var block = AddArgs(CData.App_Block(), locals == null ? CData.Cnst_Nil() : locals, body);
                return MkFunDef(MkBuildDefFunType(t), getCBuildDefName(t), MkFunParams(false, "Driver", "dst"), block);
            }
            else
                return MkFunDef(MkBuildDefFunType(t), getCBuildDefName(t), MkFunParams(false, "Driver", "dst"));
        }

        // Routines for Generating Tuple Methods
        private AST<FuncTerm> MkTupleBuildFDecl(IEnumerable<Tuple<PType, AST<Node>, string, string>> fieldDesc, PType t)
        {
            var cType = pTypeToCType(t);
            var buildFunName = getCBuildName(t);
            var buildParamTypes = new List<AST<Node>>();
            buildParamTypes.Add(PSMF_DRIVERDECL_TYPE);
            buildParamTypes.Add(MkPtrType(cType));
            buildParamTypes.AddRange(fieldDesc.Select(fDesc => (fDesc.Item1 is PPrimitiveType) ? fDesc.Item2 : MkPtrType(fDesc.Item2)));
            var buildFunType = MkFunType(MkBaseType(CData.Cnst_Void()), buildParamTypes);

            var buildParamNames = new List<string>();
            buildParamNames.Add("Driver");
            buildParamNames.Add("dst");
            buildParamNames.AddRange(fieldDesc.Select(fDesc => fDesc.Item4));

            return MkForwardFunDecl(CData.Cnst_Extern(), buildFunType, buildFunName, buildParamNames);
        }

        private AST<FuncTerm> MkTupleBuildMethod(IEnumerable<Tuple<PType, AST<Node>, string, string>> fieldDesc, PType t)
        {
            var cType = pTypeToCType(t);
            var buildFunName = getCBuildName(t);

            var buildParamTypes = new List<AST<Node>>();

            buildParamTypes.Add(PSMF_DRIVERDECL_TYPE);
            buildParamTypes.Add(MkPtrType(cType));
            buildParamTypes.AddRange(fieldDesc.Select(fDesc => (fDesc.Item1 is PPrimitiveType) ? fDesc.Item2 : MkPtrType(fDesc.Item2)));
            var buildFunType = MkFunType(MkBaseType(CData.Cnst_Void()), buildParamTypes);
            PToCFoldContext ctxt = new PToCFoldContext(null, true, this);

            var buildParamNames = new List<string>();
            buildParamNames.Add("Driver");
            buildParamNames.Add("dst");
            buildParamNames.AddRange(fieldDesc.Select(fDesc => fDesc.Item4));

            // Create a body for the Build_<Type> method
            var stmts = new List<AST<Node>>(fieldDesc.Select(fDesc => 
                fDesc.Item1 is PPrimitiveType ?
                MkAssignment(MkArrow("dst", fDesc.Item3), MkId(fDesc.Item4)) :
                MkClone(ctxt, fDesc.Item1, default(Span), MkAddrOf(MkArrow("dst", fDesc.Item3)), MkId(fDesc.Item4))));
            
            var buildBody = MkBlock(MkSeq(stmts));
            return MkFunDef(buildFunType, buildFunName, MkFunParams(false, buildParamNames.ToArray()), buildBody);
        }

        private AST<FuncTerm> MkTupleBuildDefMethod(IEnumerable<Tuple<PType, AST<Node>, string, string>> fieldDesc, PType t)
        {
            var ctxt = new PToCFoldContext("build_def", true, this);
            var exp = GetCDefault(ctxt, t);
            Debug.Assert(ctxt.isTmpVar(exp.Node));
            ctxt.replaceTempVar(exp, MkDrf(MkId("dst")));
            Debug.Assert(ctxt.sideEffectsStack.Count == 1);
            return MkBuildDefMethod(t, ctxt.emitCSideEffects(CData.Cnst_Nil()), ctxt.emitCLocals());
        }

        private AST<Node> MkCast(AST<Node> t, AST<Node> e)
        {
            return AddArgs(CData.App_Cast(), t, e);
        }


        private AST<Node> MkCast(PType t, AST<Node> e)
        {
            return MkCast(pTypeToCType(t), e);
        }

        private AST<Node> MkPtrCast(PType t, AST<Node> e)
        {
            return MkCast(MkPtrType(pTypeToCType(t)), e);
        }

        private IEnumerable<PType> subtypesAndMe(PType t)
        {
            var res = new HashSet<PType>(subtypes[t]);
            res.Add(t);
            return res;
        }

        private IEnumerable<PType> supertypesAndMe(PType t)
        {
            var res = new HashSet<PType>(supertypes[t]);
            res.Add(t);
            return res;
        }

        // Returns all super and sub types of t, and t itself
        private IEnumerable<PType> relatives(PType t)
        {
            var res = new List<PType>(supertypes[t]);
            res.AddRange(subtypes[t]);
            res.Add(t);
            return res;
        }

        private AST<Node> MkAssert(AST<Node> cond, Span s, string msg = null)
        {
            if (msg == null)
            {
                return MkFunApp("SMF_ASSERT", s, cond);
            }
            else
            {
                return MkFunApp("SMF_ASSERTMSG", s, MkStringLiteral(msg), cond);
            }
        }

        private void MkUpCastMethods(List<AST<Node>> forwardDecls, List<AST<Node>> methodDefs)
        {
            foreach (var t in allTypes)
            {
                foreach (var subT in subtypes[t])
                {
                    if ((t is PEventType || t is PIdType || t is PAnyType) && subT is PNilType)
                    {
                        // We should never need to call such an upcast. This case is handled in MkAssignOrCast, where it is
                        // compiled down to a simple assignment
                        continue;
                    }

                    if (t is PAnyType)
                    {
                        // This is handled by PackValue, defined in SMRuntime
                        continue;
                    }

                    PToCFoldContext ctxt = new PToCFoldContext(null, true, this);
                    var funType = MkFunType(MkBaseType(CData.Cnst_Void()), MkNmdType("PSMF_DRIVERDECL"),
                        MkPtrType(pTypeToCType(t)), MkPtrType(pTypeToCType(subT)));
                    var name = getCUpCastName(subT, t);
                    var prms = MkFunParams(false, "Driver", "dst", "src");
                    var body = new List<AST<Node>>();


                    if (t is PTupleType || t is PNamedTupleType)
                    {
                        var fromFields = getFieldDesc(subT).ToArray();
                        var toFields = getFieldDesc(t).ToArray();

                        for (int i = 0; i < fromFields.Count(); i++)
                        {
                            var fromField = fromFields[i];
                            var toField = toFields[i];

                            body.Add(MkAssignOrCast(ctxt, default(Span), MkArrow("dst", toField.Item3), toField.Item1,
                                MkArrow("src", fromField.Item3), fromField.Item1));
                        }
                    }
                    else if (t is PSeqType)
                    {
                        Debug.Assert(subT is PSeqType);
                        PSeqType seqSupT = t as PSeqType;
                        PSeqType seqSubT = subT as PSeqType;
                        var indV = ctxt.getTmpVar(PType.Int, false);
                        var tmpV = ctxt.getTmpVar(seqSupT.T, false);

                        var otherEl = MkCastFromULONGPTR(MkIdx(MkArrow("src", "Values"), indV), seqSubT.T);
                        body.Add(MkFunApp(getCBuildDefName(t), default(Span), ctxt.driver, MkId("dst")));
                        body.Add(MkIncFor(ctxt.consumeExp(indV), 0, MkArrow("src", "Size"), 
                            MkSeq(MkAssignOrCast(ctxt, default(Span), tmpV, seqSupT.T, otherEl, seqSubT.T),
                            MkFunApp("SmfArrayListInsert", default(Span), ctxt.driver, MkId("dst"), MkArrow("dst", "Size"),
                                MkCastToULONGPTR(ctxt.consumeExp(tmpV), seqSupT.T)))));
                    }
                    else if (t is PMapType)
                    {
                        Debug.Assert(subT is PMapType);
                        PMapType mapSupT = t as PMapType;
                        PMapType mapSubT = subT as PMapType;
                        var indV = ctxt.getTmpVar(PType.Int, false);
                        var keyV = ctxt.getTmpVar(mapSupT.KeyT, false);
                        var valV = ctxt.getTmpVar(mapSupT.ValT, false);
                        body.Add(MkFunApp(getCBuildDefName(t), default(Span), ctxt.driver, MkId("dst")));
                        body.Add(MkIncFor(ctxt.consumeExp(indV), 0, MkArrow("src", "Size"),
                            MkSeq(MkAssignOrCast(ctxt, default(Span), keyV, mapSupT.KeyT, MkFunApp("SmfHashtableLookupKeyAtIndex", default(Span), ctxt.driver, MkId("src"), indV), mapSubT.KeyT),
                                  MkAssignOrCast(ctxt, default(Span), valV, mapSupT.ValT, MkFunApp("SmfHashtableLookup", default(Span), ctxt.driver, MkId("src"), keyV), mapSubT.ValT),
                                  MkFunApp("SmfHashtableUpdate", default(Span), ctxt.driver, MkId("dst"), MkCastToULONGPTR(ctxt.consumeExp(keyV), mapSupT.KeyT), MkCastToULONGPTR(ctxt.consumeExp(valV), mapSupT.ValT))
                                 )));
                    }
                    else
                        throw new NotImplementedException(string.Format("TODO: Emit UpCast from {0} to {1}", subT, t));

                    forwardDecls.Add(MkFunDef(funType, name, prms));
                    methodDefs.Add(MkFunDef(funType, name, prms,
                        AddArgs(CData.App_Block(), ctxt.emitCLocals(), ctxt.emitCSideEffects(MkSeq(body)))));
                }
            }
        }

        private AST<Node> MkIncFor(AST<Node> var, int start, AST<Node> end, AST<Node> body)
        {
            return AddArgs(CData.App_For(),
                MkBinApp(CData.Cnst_Asn(), var, MkIntLiteral(start)),
                MkBinApp(CData.Cnst_Le(), var, end),
                MkBinApp(CData.Cnst_Asn(), var, MkBinApp(CData.Cnst_Add(), var, MkIntLiteral(1))),
                body);
        }

        private void MkDownCastMethods(List<AST<Node>> forwardDecls, List<AST<Node>> methodDefs)
        {
            foreach (var t in allTypes) {
                foreach (var subT in subtypes[t]) {
                    PToCFoldContext ctxt = new PToCFoldContext(null, true, this);
                    var funType = MkFunType(MkBaseType(CData.Cnst_Void()), MkNmdType("PSMF_DRIVERDECL"),
                        MkPtrType(pTypeToCType(subT)), (t is PPrimitiveType) ? pTypeToCType(t) : MkPtrType(pTypeToCType(t)));
                    var name = getCDownCastName(t, subT);
                    var prms = MkFunParams(false, "Driver", "dst", "src");
                    var body = new List<AST<Node>>();
                    var errMsg =  "Failed Downcasting From " + t + " to " + subT;

                    if (t is PAnyType)
                    {
                        var cases = new List<Tuple<AST<Node>, AST<Node>>>();

                        foreach (var midT in relatives(subT).Where(tp => !(tp is PAnyType))) {
                            cases.Add(new Tuple<AST<Node>, AST<Node>>(MkId(pTypeToCEnum(midT)),
                            MkAssignOrCast(ctxt, default(Span), MkDrf(MkId("dst")), subT, getPackedValueMember(MkId("src"), midT), midT)));
                        }

                        body.Add(MkSwitch(MkArrow("src", "Type"), cases, MkAssert(MkId("FALSE"), default(Span), errMsg)));
                    }
                    else if (t is PTupleType || t is PNamedTupleType)
                    {
                        var fromFields = getFieldDesc(t).ToArray();
                        var toFields = getFieldDesc(subT).ToArray();

                        for (int i = 0; i < fromFields.Count(); i++)
                        {
                            body.Add(MkAssignOrCast(ctxt, default(Span), MkArrow("dst", toFields[i].Item3), toFields[i].Item1,
                                MkArrow("src", fromFields[i].Item3), fromFields[i].Item1));
                        }
                    }
                    else if (t is PSeqType)
                    {
                        Debug.Assert(subT is PSeqType);
                        PSeqType seqSupT = t as PSeqType;
                        PSeqType seqSubT = subT as PSeqType;
                        var indV = ctxt.getTmpVar(PType.Int, false);
                        var tmpV = ctxt.getTmpVar(seqSubT.T, false);

                        var otherEl = MkCastFromULONGPTR(MkIdx(MkArrow("src", "Values"), indV), seqSupT.T);
                        body.Add(MkFunApp(getCBuildDefName(subT), default(Span), ctxt.driver, MkId("dst")));
                        body.Add(MkIncFor(ctxt.consumeExp(indV), 0, MkArrow("src", "Size"),
                            MkSeq(MkAssignOrCast(ctxt, default(Span), tmpV, seqSubT.T, otherEl, seqSupT.T),
                            MkFunApp("SmfArrayListInsert", default(Span), ctxt.driver, MkId("dst"), MkArrow("dst", "Size"),
                                MkCastToULONGPTR(ctxt.consumeExp(tmpV), seqSubT.T)))));
                    }
                    else if (t is PMapType)
                    {
                        Debug.Assert(subT is PMapType);
                        PMapType mapSupT = t as PMapType;
                        PMapType mapSubT = subT as PMapType;
                        var indV = ctxt.getTmpVar(PType.Int, false);
                        var keyV = ctxt.getTmpVar(mapSubT.KeyT, false);
                        var valV = ctxt.getTmpVar(mapSubT.ValT, false);

                        body.Add(MkFunApp(getCBuildDefName(subT), default(Span), ctxt.driver, MkId("dst")));
                        body.Add(MkIncFor(ctxt.consumeExp(indV), 0, MkArrow("src", "Size"),
                            MkSeq(MkAssignOrCast(ctxt, default(Span), keyV, mapSubT.KeyT, MkFunApp("SmfHashtableLookupKeyAtIndex", default(Span), ctxt.driver, MkId("src"), indV), mapSupT.KeyT),
                                  MkAssignOrCast(ctxt, default(Span), valV, mapSubT.ValT, MkFunApp("SmfHashtableLookup", default(Span), ctxt.driver, MkId("src"), keyV), mapSupT.ValT),
                                  MkFunApp("SmfHashtableUpdate", default(Span), ctxt.driver, MkId("dst"), MkArrow("dst", "Size"), MkCastToULONGPTR(ctxt.consumeExp(keyV), mapSubT.KeyT), MkCastToULONGPTR(ctxt.consumeExp(valV), mapSubT.ValT))
                                 )));
                    }
                    else
                    {
                        // We must be in the eid->nil or mid->nil down cast case.
                        if (t is PEventType || t is PIdType)
                        {
                            body.Add(MkAssert(MkEq(MkId("src"), MkCast(t, MkIntLiteral(0))), default(Span), errMsg));
                            body.Add(MkAssignment(MkDrf(MkId("dst")), MkIntLiteral(0)));
                        }
                    }

                    forwardDecls.Add(MkFunDef(funType, name, prms));
                    methodDefs.Add(MkFunDef(funType, name, prms,
                        AddArgs(CData.App_Block(), ctxt.emitCLocals(), ctxt.emitCSideEffects(MkSeq(body)))));

                }
            }
        }

        private AST<Node> MkCanCast(AST<Node> driver, AST<Node> from, PType fromT, PType toT)
        {
            if (fromT.isSubtypeOf(toT)) // fromT==toT or fromT <: toT
            {
                return MkId("TRUE");
            }
            else if (toT.isSubtypeOf(fromT))    // toT <: FromT
            {
                if (fromT is PIdType && toT is PNilType) // Optimization
                {
                    return MkEq(driver, from, fromT, null, toT); // Its safe to pass null here since the expression is ignore when comparing with Null
                }
                else if (fromT is PEventType && toT is PNilType) // Optimization
                {
                    return MkEq(driver, from, fromT, null, toT); // Its safe to pass null here since the expression is ignore when comparing with Null
                }
                {
                    return MkFunApp(getCCanDownCastName(fromT), default(Span),
                        (fromT is PPrimitiveType ? from : MkAddrOf(from)), MkId(pTypeToCEnum(toT)));
                }
            }
            else    // Unrelated types
            {
                return MkId("FALSE");
            }
        }
        private void MkCanDownCastMethods(List<AST<Node>> forwardDecls, List<AST<Node>> methodDefs)
        {
            foreach (var t in allTypes)
            {
                if (t is PEventType || t is PIdType)
                {
                    Debug.Assert(subtypes[t].Count == 1 && subtypes[t][0] == PType.Nil);
                    continue; // These cases are handled in MkCanCast
                }

                PToCFoldContext ctxt = new PToCFoldContext(null, true, this);
                var funType = MkFunType(pTypeToCType(PType.Bool),
                    (t is PPrimitiveType) ? pTypeToCType(t) : MkPtrType(pTypeToCType(t)), MkNmdType("SMF_TYPEDECL_INDEX"));
                var name = getCCanDownCastName(t);
                var prms = MkFunParams(false, "obj", "toT");
                var outerCases = new List<Tuple<AST<Node>, AST<Node>>>();

                foreach (var subT in subtypes[t])
                {
                    if (t is PAnyType)
                    {
                        var cases = new List<Tuple<AST<Node>, AST<Node>>>();

                        foreach (var midT in relatives(subT).Where(tp => !(tp is PAnyType)))
                        {
                            cases.Add(new Tuple<AST<Node>, AST<Node>>(MkId(pTypeToCEnum(midT)),
                                AddArgs(CData.App_Return(), MkCanCast(ctxt.driver, getPackedValueMember(MkId("obj"), midT), midT, subT))));
                        }

                        outerCases.Add(new Tuple<AST<Node>, AST<Node>>(MkId(pTypeToCEnum(subT)),
                            MkSwitch(MkArrow("obj", "Type"), cases, AddArgs(CData.App_Return(), MkId("FALSE")))));
                    }
                    else if (t is PTupleType || t is PNamedTupleType)
                    {
                        var fromFields = getFieldDesc(t).ToArray();
                        var toFields = getFieldDesc(subT).ToArray();
                        var fieldChecks = new List<AST<Node>>();

                        for (int i = 0; i < fromFields.Count(); i++)
                        {
                            var canCast = MkCanCast(ctxt.driver, MkArrow(MkId("obj"), fromFields[i].Item3), fromFields[i].Item1, toFields[i].Item1);

                            if (isInstanceOf(canCast.Node, CData.App_Ident()) &&
                                ((Cnst)GetArgByIndex((FuncTerm)canCast.Node, 0)).GetStringValue() == "TRUE")
                                continue; // Ignore Trivial Ifs

                            fieldChecks.Add(MkIf(MkUnop(CData.Cnst_LNot(), canCast),
                                AddArgs(CData.App_Return(), MkId("FALSE"))));
                        }

                        fieldChecks.Add(AddArgs(CData.App_Return(), MkId("TRUE")));
                        outerCases.Add(new Tuple<AST<Node>, AST<Node>>(MkId(pTypeToCEnum(subT)),
                            MkSeq(fieldChecks)));
                    }
                    else if (t is PSeqType)
                    {
                        Debug.Assert(subT is PSeqType);
                        PSeqType seqSupT = t as PSeqType;
                        PSeqType seqSubT = subT as PSeqType;
                        var indV = ctxt.getTmpVar(PType.Int, false);

                        var otherEl = MkIdx(MkArrow(MkId("obj"), "Values"), indV);
                        otherEl = seqSupT.T is PPrimitiveType ? MkCast(seqSupT.T, otherEl) : MkDrf(MkPtrCast(seqSupT.T, otherEl));
                        outerCases.Add(new Tuple<AST<Node>, AST<Node>>(MkId(pTypeToCEnum(subT)), MkSeq(
                            MkIncFor(ctxt.consumeExp(indV), 0, MkArrow("obj", "Size"),
                                MkIf(MkUnop(CData.Cnst_LNot(), MkCanCast(ctxt.driver, otherEl, seqSupT.T, seqSubT)), MkReturn(MkId("FALSE")))),
                            MkReturn(MkId("TRUE")))));
                    }
                    else if (t is PMapType)
                    {
                        Debug.Assert(subT is PMapType);
                        PMapType mapSupT = t as PMapType;
                        PMapType mapSubT = subT as PMapType;
                        var indV = ctxt.getTmpVar(PType.Int, false);
                        var keyV = ctxt.getTmpVar(mapSubT.KeyT, false);
                        var valV = ctxt.getTmpVar(mapSubT.ValT, false);
                        
                        outerCases.Add(new Tuple<AST<Node>, AST<Node>>(MkId(pTypeToCEnum(subT)), MkSeq(
                            MkIncFor(ctxt.consumeExp(indV), 0, MkArrow("obj", "Size"),
                                MkSeq(
                                  MkAssignOrCast(ctxt, default(Span), keyV, mapSubT.KeyT, MkFunApp("SmfHashtableLookupKeyAtIndex", default(Span), ctxt.driver, MkId("src"), indV), mapSupT.KeyT),
                                  MkAssignOrCast(ctxt, default(Span), valV, mapSubT.ValT, MkFunApp("SmfHashtableLookup", default(Span), ctxt.driver, MkId("src"), keyV), mapSupT.ValT),
                                  MkIf(MkUnop(CData.Cnst_LNot(), 
                                       MkBinApp(CData.Cnst_Add(), MkCanCast(ctxt.driver, keyV, mapSupT.KeyT, mapSubT.KeyT), MkCanCast(ctxt.driver, valV, mapSupT.ValT, mapSubT.ValT))), 
                                       MkReturn(MkId("FALSE"))),
                                  MkReturn(MkId("TRUE")))))));
                    }
                    else
                    {
                        throw new NotImplementedException(string.Format("Haven't implemented 'CanCast' from {0} to {1}", t, subT));
                    }
                }

                if (outerCases.Count == 0) // This type has no subtypes. Don't need a function here. Handled in MkCanCast
                {
                    continue;
                }

                forwardDecls.Add(MkFunDef(funType, name, prms));
                methodDefs.Add(MkFunDef(funType, name, prms, AddArgs(CData.App_Block(), ctxt.emitCLocals(),
                    ctxt.emitCSideEffects(MkSwitch(MkId("toT"), outerCases, AddArgs(CData.App_Return(), MkId("FALSE")))))));
            }
        }

        // TODO: Ugly hack. Make this more generic. Currently only emitting for Any.
        private void MkCanCastMethods(List<AST<Node>> forwardDecls, List<AST<Node>> methodDefs)
        {
            PType t = PType.Any;

            PToCFoldContext ctxt = new PToCFoldContext(null, true, this);
            var funType = MkFunType(pTypeToCType(PType.Bool), PSMF_DRIVERDECL_TYPE,
                (t is PPrimitiveType) ? pTypeToCType(t) : MkPtrType(pTypeToCType(t)), MkNmdType("SMF_TYPEDECL_INDEX"));
            var name = getCCanCastName(t);
            var prms = MkFunParams(false, "Driver", "obj", "toT");
            var body = new List<AST<Node>>();
            body.Add(MkIf(MkEq(MkId("toT"), MkId(pTypeToCEnum(t))), AddArgs(CData.App_Return(), MkId("TRUE"))));
            body.Add(AddArgs(CData.App_Return(), MkFunApp(getCCanDownCastName(t), default(Span), MkId("obj"), MkId("toT"))));
            forwardDecls.Add(MkFunDef(funType, name, prms));
            methodDefs.Add(MkFunDef(funType, name, prms, AddArgs(CData.App_Block(), ctxt.emitCLocals(),
                ctxt.emitCSideEffects(MkSeq(body)))));
        }

        private void MkEqualsMethods(List<AST<Node>> forwardDecls, List<AST<Node>> methodDefs)
        {
            foreach (var t in allTypes)
            {
                // All comparisons between primitive types (including mid, eid and null) are handled
                // by MkEq(), and don't need a special function
                if (t is PPrimitiveType)
                    continue;

                var arg1T = MkPtrType(pTypeToCType(t));
                foreach (var subT in subtypesAndMe(t))
                {
                    var name = getCEqualsName(t, subT);
                    PToCFoldContext ctxt = new PToCFoldContext(null, true, this);
                    var arg2T = subT is PPrimitiveType ? pTypeToCType(subT) :  MkPtrType(pTypeToCType(subT));
                    var funType = MkFunType(pTypeToCType(PType.Bool), PSMF_DRIVERDECL_TYPE, arg1T, arg2T);
                    var prms = MkFunParams(false, "Driver", "e1", "e2");
                    var body = new List<AST<Node>>();

                    if (t is PAnyType)
                    {
                        if (subT is PAnyType)
                        {   // Switch on e1's type, and in each case defer to comparing an "Any" value with a concrete type.
                            var cases = new List<Tuple<AST<Node>, AST<Node>>>();
                            foreach (var rightT in subtypes[t])
                            {
                                cases.Add(new Tuple<AST<Node>, AST<Node>>(MkId(pTypeToCEnum(rightT)),
                                        AddArgs(CData.App_Return(), MkEq(ctxt.driver, MkDrf(MkId("e1")), t, getPackedValueMember(MkId("e2"), rightT), rightT))));
                            }
                            var sw = MkSwitch(MkArrow("e2", "Type"), cases, 
                                MkSeq(MkFunApp("SMF_ASSERTMSG", default(Span), MkStringLiteral("Unknown Type Values in Packed Types"), MkId("FALSE")),
                                AddArgs(CData.App_Return(), MkId("FALSE"))));
                            body.Add(sw);
                        }
                        else
                        {   // Comparing an Any type to a concrete type T. Check if Any holds any relative of T, or T itself, and if so, invoke
                            // the correct equality check.
                            var cases = new List<Tuple<AST<Node>, AST<Node>>>();
                            foreach (var midT in relatives(subT).Where(tp => !(tp is PAnyType)))
                            {
                                if (midT is PAnyType)
                                    continue;

                                var arg2 = (subT is PPrimitiveType) ? MkId("e2") : MkDrf(MkId("e2"));
                                cases.Add(new Tuple<AST<Node>, AST<Node>>(MkId(pTypeToCEnum(midT)),
                                    AddArgs(CData.App_Return(), MkEq(ctxt.driver, getPackedValueMember(MkId("e1"), midT), midT, arg2, subT))));
                            }
                            var sw = MkSwitch(MkArrow("e1", "Type"), cases, AddArgs(CData.App_Return(), MkId("FALSE")));
                            body.Add(sw);
                        }
                    }
                    else if (t is PTupleType || t is PNamedTupleType)
                    {
                        // Comparing a Tuple/Named tuple with a subtype. The subtype must also be tuple-like;
                        Debug.Assert( (t is PTupleType && subT is PTupleType) || (t is PNamedTupleType && subT is PNamedTupleType) );
                        var e1Fields = getFieldDesc(t);
                        var e2Fields = getFieldDesc(subT);

                        var fieldsEqualityTerms = e1Fields.Zip(e2Fields, (supField, subField) =>
                            MkEq(ctxt.driver, MkArrow("e1", supField.Item3), supField.Item1, MkArrow("e2", subField.Item3), subField.Item1)).ToArray<AST<Node>>();

                        AST<Node> equalsExp = fieldsEqualityTerms[0];

                        for (int i = 1; i < fieldsEqualityTerms.Length; i++)
                            equalsExp = AddArgs(CData.App_BinApp(), CData.Cnst_LAnd(), fieldsEqualityTerms[i], equalsExp);

                        body.Add(AddArgs(CData.App_Return(), equalsExp));
                    }
                    else if (t is PSeqType)
                    {
                        Debug.Assert(subT is PSeqType);
                        body.Add(MkIf(MkNeq(ctxt.driver, MkArrow("e1", "Size"), PType.Int, MkArrow("e2", "Size"), PType.Int),
                            MkReturn(MkId("FALSE"))));
                        var indV = ctxt.getTmpVar(PType.Int, false);
                        body.Add(MkIncFor(ctxt.consumeExp(indV), 0, MkArrow("e1", "Size"),
                            MkIf(MkNeq(ctxt.driver, MkCastFromULONGPTR(MkIdx(MkArrow("e1", "Values"), indV), (t as PSeqType).T), (t as PSeqType).T,
                                        MkCastFromULONGPTR(MkIdx(MkArrow("e2", "Values"), indV), (subT as PSeqType).T), (subT as PSeqType).T),
                                MkReturn(MkId("FALSE")))));
                        body.Add(MkReturn(MkId("TRUE")));
                    }
                    else if (t is PMapType)
                    {
                        Debug.Assert(subT is PMapType);
                        PMapType mapSupT = t as PMapType;
                        PMapType mapSubT = subT as PMapType;

                        body.Add(MkIf(MkNeq(ctxt.driver, MkArrow("e1", "Size"), PType.Int, MkArrow("e2", "Size"), PType.Int),
                            MkReturn(MkId("FALSE"))));
                        var indV = ctxt.getTmpVar(PType.Int, false);
                        var keyVSup = ctxt.getTmpVar(mapSupT.KeyT, false);
                        var keyVSub = ctxt.getTmpVar(mapSubT.KeyT, false);

                        body.Add(MkIncFor(ctxt.consumeExp(indV), 0, MkArrow("e1", "Size"),
                            MkSeq(MkAssignOrCast(ctxt, default(Span), ctxt.consumeExp(keyVSup), mapSupT.KeyT, MkFunApp("SmfHashtableLookupKeyAtIndex", default(Span), ctxt.driver, MkId("e1"), indV), mapSupT.KeyT),
                                  MkAssignOrCast(ctxt, default(Span), ctxt.consumeExp(keyVSub), mapSubT.KeyT, keyVSup, mapSupT.KeyT),
                            MkIf(MkUnop(CData.Cnst_LNot(), MkFunApp("SmfHashtableContains", default(Span), ctxt.driver, MkId("e2"), keyVSub)), MkReturn(MkId("FALSE"))),
                            MkIf(MkNeq(ctxt.driver, MkFunApp("SmfHashtableLookup", default(Span), ctxt.driver, MkId("e1"), keyVSup), mapSupT.ValT, MkFunApp("SmfHashtableLookup", default(Span), ctxt.driver, MkId("e2"), keyVSub), mapSubT.ValT), MkReturn(MkId("FALSE")))
                            )));
                        body.Add(MkReturn(MkId("TRUE")));
                    }
                    else
                    {
                        throw new NotImplementedException(string.Format("Haven't implemented 'Equals' for {0} and {1}", t, subT));
                    }

                    forwardDecls.Add(MkFunDef(funType, name, prms));
                    methodDefs.Add(MkFunDef(funType, name, prms,
                        AddArgs(CData.App_Block(), ctxt.emitCLocals(), ctxt.emitCSideEffects(MkSeq(body)))));
                }
            }
        }

        private AST<Node> MkCastFromULONGPTR(AST<Node> e, PType to)
        {
            return to is PPrimitiveType ? MkCast(to, e) : MkDrf(MkPtrCast(to, e));
        }

        private AST<Node> MkCastToULONGPTR(AST<Node> e, PType from)
        {
            return from is PPrimitiveType ? AddArgs(CData.App_Cast(), MkNmdType("ULONG_PTR"), e) :
                AddArgs(CData.App_Cast(), MkNmdType("ULONG_PTR"), MkAddrOf(e));
        }

        private AST<Node> MkReturn(AST<Node> retV)
        {
            return AddArgs(CData.App_Return(), retV);
        }

        private void MkHashCodeMethods(List<AST<Node>> forwardDecls, List<AST<Node>> methodDefs)
        {
            foreach (var t in allTypes)
            {
                if (!typeNeedsHashCode(t))
                    continue;

                var name = getCHashCodeName(t);
                PToCFoldContext ctxt = new PToCFoldContext(null, true, this);
                var funType = MkFunType(MkNmdType("ULONG"), MkNmdType("PSMF_DRIVERDECL"), MkPtrType(pTypeToCType(t)));
                var prms = MkFunParams(false, "Driver", "obj");
                var body = new List<AST<Node>>();

                if (t is PNamedTupleType || t is PTupleType)
                {
                    var res = ctxt.getTmpVar(PType.Int, false);
                    bool first = true;
                    foreach (var field in getFieldDesc(t))
                    {
                        AST<Node> expr = null;
                        if (field.Item1 is PPrimitiveType)
                        {
                            expr = MkArrow("obj", field.Item3);
                        }
                        else
                        {
                            expr = MkFunApp(getCHashCodeName(field.Item1), default(Span), ctxt.driver, MkAddrOf(MkArrow("obj", field.Item3)));
                        }
                        if (first)
                        {
                            body.Add(MkAssignment(ctxt.consumeExp(res), expr));
                        }
                        else
                        {
                            body.Add(MkAssignment(ctxt.consumeExp(res), MkBinApp(CData.Cnst_Bxor(), res, expr)));
                        }
                        first = false;
                    }
                    if (first)
                    {
                        body.Add(MkReturn(MkCnst(0)));
                    }
                    {
                        body.Add(MkReturn(res));
                    }
                }
                else if (t is PSeqType)
                {
                    PSeqType seqT = t as PSeqType;
                    var res = ctxt.getTmpVar(PType.Int, false);
                    var indV = ctxt.getTmpVar(PType.Int, false);

                    body.Add(MkIf(MkEq(MkArrow("obj", "Size"), MkIntLiteral(0)), MkReturn(MkIntLiteral(0))));
                    body.Add(MkAssignment(indV, MkIntLiteral(0)));
                    body.Add(MkAssignment(res, MkIdx(MkArrow("obj", "Values"), MkIntLiteral(0))));
                    AST<Node> expr = null;
                    if (seqT.T is PPrimitiveType)
                    {
                        expr = MkIdx(MkArrow("obj", "Values"), indV);
                    }
                    else
                    {
                        expr = MkFunApp(getCHashCodeName(seqT), default(Span), ctxt.driver, MkAddrOf(MkCastFromULONGPTR(MkIdx(MkArrow("obj", "Values"), indV), seqT)));
                    }
                    body.Add(MkIncFor(ctxt.consumeExp(indV), 1, MkArrow("obj", "Size"), MkAssignment(ctxt.consumeExp(res), MkBinApp(CData.Cnst_Bxor(), res, expr))));
                    body.Add(MkReturn(res));
                }
                else
                    throw new NotImplementedException(string.Format("TODO: Emit Hashcode method for type: {0}", t));

                forwardDecls.Add(MkFunDef(funType, name, prms));
                methodDefs.Add(MkFunDef(funType, name, prms,
                    AddArgs(CData.App_Block(), ctxt.emitCLocals(), ctxt.emitCSideEffects(MkSeq(body)))));
            }
        }

        private void MkDestroyMethods(List<AST<Node>> forwardDecls, List<AST<Node>> methodDefs)
        {
            foreach (var t in allTypes)
            {
                if (!typeNeedsDestroy(t))
                    continue;

                if (t is PAnyType) // This is handled by Destroy_PackedValue in SmfRuntime.c
                    continue;
                var name = getCDestroyName(t);
                PToCFoldContext ctxt = new PToCFoldContext(null, true, this);
                var funType = MkFunType(MkBaseType(CData.Cnst_Void()), MkNmdType("PSMF_DRIVERDECL"), MkPtrType(pTypeToCType(t)));
                var prms = MkFunParams(false, "Driver", "obj");
                var body = new List<AST<Node>>();

                if (t is PNamedTupleType || t is PTupleType)
                {
                    var fieldDesc = getFieldDesc(t);
                    body.AddRange(fieldDesc.Where(field => typeNeedsDestroy(field.Item1)).Select(
                        field => MkFunApp(getCDestroyName(field.Item1), default(Span), ctxt.driver, MkAddrOf(MkArrow("obj", field.Item3)))));
                }
                else if (t is PSeqType || t is PMapType)
                {
                    // This is handled by SmfArrayListDestroy and SmfHashtableDestroy. No need to emit anything here.
                    continue;
                }
                else
                    throw new NotImplementedException(string.Format("TODO: Emit Destroy method for type: {0}", t));
                
                forwardDecls.Add(MkFunDef(funType, name, prms));
                methodDefs.Add(MkFunDef(funType, name, prms,
                    AddArgs(CData.App_Block(), ctxt.emitCLocals(), ctxt.emitCSideEffects(MkSeq(body)))));

            }
        }

        private void MkCloneMethods(List<AST<Node>> forwardDecls, List<AST<Node>> methodDefs)
        {
            foreach (var t in allTypes)
            {
                if (!typeNeedsClone(t))
                    continue;

                if (t is PAnyType) // This is handled by Clone_PackedValue in SmfRuntime.c
                    continue;

                PToCFoldContext ctxt = new PToCFoldContext(null, true, this);
                var funType = MkFunType(MkBaseType(CData.Cnst_Void()), MkNmdType("PSMF_DRIVERDECL"), MkPtrType(pTypeToCType(t)), MkPtrType(pTypeToCType(t)));
                var name = getCCloneName(t);
                var prms = MkFunParams(false, "Driver","dst", "src");
                var body = new List<AST<Node>>();

                if (t is PNamedTupleType || t is PTupleType)
                {
                    var fieldDesc = getFieldDesc(t);
                    var buildFunName = getCBuildName(t);
                    var cloneBuildArgs = new List<AST<Node>>();

                    cloneBuildArgs.Add(ctxt.driver);
                    cloneBuildArgs.Add(MkId("dst"));
                    cloneBuildArgs.AddRange(fieldDesc.Select(fDesc => fDesc.Item1 is PPrimitiveType ?
                        MkArrow(MkId("src"), fDesc.Item3) : MkAddrOf(MkArrow(MkId("src"), fDesc.Item3))));

                    body.Add(MkFunApp(getCBuildName(t), default(Span), cloneBuildArgs));
                }
                else if (t is PSeqType || t is PMapType)
                {
                    // This is handled by SmfArrayListClone and SmfHashtableClone. No need to emit anything here.
                    continue;
                }
                else
                    throw new NotImplementedException(string.Format("TODO: Emit Clone method for type: {0}", t));

                forwardDecls.Add(MkFunDef(funType, name, prms));
                methodDefs.Add(MkFunDef(funType, name, prms,
                    AddArgs(CData.App_Block(), ctxt.emitCLocals(), ctxt.emitCSideEffects(MkSeq(body)))));

            }
        }

        IEnumerable<Tuple<PType, AST<Node>, string, string>> getFieldDesc(PType t)
        {
            if (t is PTupleType)
            {
                var tupT = t as PTupleType;
                return tupT.elements.Zip(System.Linq.Enumerable.Range(0, tupT.elements.Count()),
                    (type, ind) => new Tuple<PType, AST<Node>, string, string>(type, pTypeToCType(type), getTupleField(ind), getFuncArg(ind)));

            }
            else if (t is PNamedTupleType)
            {
                return ((PNamedTupleType) t).elements.Select(field => new Tuple<PType, AST<Node>, string, string>(field.Item2, pTypeToCType(field.Item2), field.Item1, "arg_" + field.Item1));
            }
            else
                throw new Exception("Cannot generate field descriptions for non-tuple like type " + t);
        }

        AST<Node> getPackedValueMember(AST<Node> value, PType memT)
        {
            if (memT is PNilType)
                return MkIntLiteral(0);
            if (memT is PPrimitiveType)
                return AddArgs(CData.App_Cast(), pTypeToCType(memT), MkArrow(value, "Value"));
            else
                return MkDrf(AddArgs(CData.App_Cast(), MkPtrType(pTypeToCType(memT)), MkArrow(value, "Value")));
        }

        AST<Node> MkSwitch(AST<Node> expr, IEnumerable<Tuple<AST<Node>, AST<Node>>> cases, AST<Node> def = null)
        {
            Debug.Assert(cases.Count() != 0 || def != null);
            AST<Node> defCase = def != null ? (AST<Node>)AddArgs(CData.App_Cases(), CData.Cnst_Default(), def, CData.Cnst_Nil()) : (AST<Node>)CData.Cnst_Nil();

            if (cases.Count() == 0)
            {
                return def;
            }

            return AddArgs(CData.App_Switch(), expr,
                ConstructList2(CData.App_Cases(), cases.Select(c => c.Item1),
                cases.Select(c => MkSeq(c.Item2, AddArgs(CData.App_StrJmp(), CData.Cnst_Break()))), defCase));
        }

        bool GenerateC(Env env)
        {
            var outModel = MkCOutputModel();
            Dictionary<string, int> typeIds = new Dictionary<string, int>();

            string pblEnmTypesCmt = 
                string.Format(headerCmt, 
                              "PublicEnumTypes.h", 
                              "This file contains enumerated types for events, machines, and variables.", 
                              target,
                              DateTime.Today.Date.ToShortDateString(), 
                              kernelMode ? "Kernel mode" : "User mode");
            var pblEnmTypes = MkCFile(
                "PublicEnumTypes.h",
                emitHeaderComment ? MkComment(pblEnmTypesCmt, true) : null,
                CData.Trm_PragmaOnce(),
                MkEnum(
                    PData.Con_EventDecl.Node.Name,
                    "Events",
                    "Event_",
                    0,
                    modelEventIds,
                    NullEvent),
                MkEnum(
                    PData.Con_MachineDecl.Node.Name,
                    "MachineTypes",
                    "MachineType_",
                    0,
                    modelMachineIds),
               MkEnums(
                    PData.Con_VarDecl.Node.Name,
                    PData.Con_MachineDecl.Node.Name,
                    "Vars",
                    "Var",
                    0,
                    1,
                    0,
                    modelVarIds),
               MkEnum(allTypes.Select(pType => pTypeToCEnum(pType)), "", "Types", typeIds));

            outModel = Add(outModel, pblEnmTypes);

            var complexClassStructDefs = new List<AST<Node>> ();
            var complexClassForwardDefs = new List<AST<Node>> ();
            var complexClassMethods = new List<AST<Node>> ();
            var complexClassTypeDecls = new List<AST<Node>>();

            foreach (PType t in allTypes) {
                // Create the TYPEDECL. It contains all the neccessary metadata/function pointers
                // for the runtime to handle values of a given type.

                complexClassTypeDecls.Add(MkTypeDecl(t));

                if (t is PPrimitiveType)
                {
                    // No Build/BuildDef Methods needed
                }
                else if (t is PAnyType)
                {
                    // BuildDefault
                    complexClassForwardDefs.Add(MkBuildDefMethod(t));
                    var defBody = MkSeq(
                        MkAssignment(MkArrow("dst", "Type"), MkId(pTypeToCEnum(PType.Nil))),
                        MkAssignment(MkArrow("dst", "Value"), MkIntLiteral(0)));
                    complexClassMethods.Add(MkBuildDefMethod(t, defBody));
                }
                else if (t is PTupleType || t is PNamedTupleType)
                {
                    var cType = pTypeToCType(t);
                    var cTypeName = declaredTypes[t].cType;

                    IEnumerable<Tuple<PType, AST<Node>, string, string>> fieldDesc = getFieldDesc(t);

                    // Add Struct Def
                    complexClassStructDefs.Add(MkDataDef(cTypeName, t));

                    // Add Build_<Type> method
                    complexClassForwardDefs.Add(MkTupleBuildFDecl(fieldDesc, t));
                    complexClassMethods.Add(MkTupleBuildMethod(fieldDesc, t));

                    // Add the BuildDefault_<Type> method
                    complexClassForwardDefs.Add(MkBuildDefMethod(t));
                    complexClassMethods.Add(MkTupleBuildDefMethod(fieldDesc, t));
                }
                else if (t is PSeqType)
                {
                    // BuildDefault
                    complexClassForwardDefs.Add(MkBuildDefMethod(t));
                    var defBody = MkFunApp("BuildEmptyArrayList", default(Span),
                        AddArgs(CData.App_Cast(), MkPtrType(pTypeToCType(t)), MkId("dst")),
                        MkId(pTypeToCEnum((t as PSeqType).T)));
                    complexClassMethods.Add(MkBuildDefMethod(t, defBody));
                }
                else if (t is PMapType)
                {
                    // BuildDefault
                    complexClassForwardDefs.Add(MkBuildDefMethod(t));
                    var defBody = MkFunApp("BuildEmptyHashtable", default(Span),
                        AddArgs(CData.App_Cast(), MkPtrType(pTypeToCType(t)), MkId("dst")),
                        MkId(pTypeToCEnum((t as PMapType).KeyT)),
                        MkId(pTypeToCEnum((t as PMapType).ValT)));
                    complexClassMethods.Add(MkBuildDefMethod(t, defBody));
                }
                else
                    throw new NotImplementedException("Can't CGEN for unknown complex type " + t);
            }

            MkUpCastMethods(complexClassForwardDefs, complexClassMethods);
            MkDownCastMethods(complexClassForwardDefs, complexClassMethods);
            MkCanDownCastMethods(complexClassForwardDefs, complexClassMethods);
            MkCanCastMethods(complexClassForwardDefs, complexClassMethods);
            MkEqualsMethods(complexClassForwardDefs, complexClassMethods);
            MkCloneMethods(complexClassForwardDefs, complexClassMethods);
            MkDestroyMethods(complexClassForwardDefs, complexClassMethods);
            MkHashCodeMethods(complexClassForwardDefs, complexClassMethods);

            var pblComplexTypesHeader = MkCFile(
                "PublicComplexTypes.h",
                emitHeaderComment ? MkComment(pblEnmTypesCmt, true) : null,
                CData.Trm_PragmaOnce(),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfPublic.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfProtectedTypes.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfArrayList.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfHashtable.h"), PData.Cnst_False),
                ConstructCList(CData.App_Section(), complexClassStructDefs),
                ConstructCList(CData.App_Section(), complexClassForwardDefs));

            outModel = Add(outModel, pblComplexTypesHeader);
            
            var prtComplexTypesFuns = MkCFile(
                "ComplexTypesMethods.c",
                emitHeaderComment ? MkComment(pblEnmTypesCmt, true) : null,
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("PublicComplexTypes.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("PublicEnumTypes.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfProtected.h"), PData.Cnst_False),
                ConstructCList(CData.App_Section(), complexClassMethods));

            outModel = Add(outModel, prtComplexTypesFuns);

            string prtEnmTypesCmt =
                string.Format(headerCmt,
                              "ProtectedEnumTypes.h",
                              "This file contains enumerated types for states and event sets.",
                              target,
                              DateTime.Today.Date.ToShortDateString(),
                              kernelMode ? "Kernel mode" : "User mode");
            var prtEnmTypes = MkCFile(
                "ProtectedEnumTypes.h",
                emitHeaderComment ? MkComment(prtEnmTypesCmt, true) : null,
                CData.Trm_PragmaOnce(),
               MkEnums(
                    PData.Con_StateDecl.Node.Name,
                    PData.Con_MachineDecl.Node.Name,
                    "States",
                    "State",
                    0,
                    1,
                    0,
                    modelStateIds),
               MkEnums(
                    PData.Con_EventSetDecl.Node.Name,
                    PData.Con_MachineDecl.Node.Name,
                    "EventSets",
                    "EventSet",
                    0,
                    1,
                    0,
                    modelEventSetIds));

            outModel = Add(outModel, prtEnmTypes);

            AST<FuncTerm> entriesH, entriesB;
            MkEntryFuns(out entriesH, out entriesB);
            AST<FuncTerm> exitsH, exitsB;
            MkExitFuns(out exitsH, out exitsB);
            AST<FuncTerm> actionsH, actionsB;
            MkActionFuns(out actionsH, out actionsB);

            AST<FuncTerm> constructorsH;
            MkConstructorFunDecls(out constructorsH);

            string prtMachDeclsCmt =
                string.Format(headerCmt,
                              "ProtectedMachineDecls.h",
                              @"This file contains headers for entry functions, exit functions, action functions, and constructors; 
    it also contains tables for event sets, transitions, actions, states, and variables.",
                              target,
                              DateTime.Today.Date.ToShortDateString(),
                              kernelMode ? "Kernel mode" : "User mode");
            var prtMachDecls = MkCFile(
                "ProtectedMachineDecls.h",
                emitHeaderComment ? MkComment(prtMachDeclsCmt, true) : null, 
                CData.Trm_PragmaOnce(),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("PublicEnumTypes.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("ProtectedEnumTypes.h"), PData.Cnst_False),
                entriesH,
                exitsH,
                constructorsH,
                actionsH,
                MkEventSets(),
                MkTransTables(),
                MkActionTables(),
                MkStateTables(),
                MkVarTables());

            outModel = Add(outModel, prtMachDecls);

            AST<FuncTerm> funDeclHeaders, funDeclBodies;
            MkFunDecls(out funDeclHeaders, out funDeclBodies);
            string functionPrototypesCmt =
                string.Format(headerCmt,
                              "FunctionPrototypes.h",
                              "This file contains headers for functions.",
                              target,
                              DateTime.Today.Date.ToShortDateString(),
                              kernelMode ? "Kernel mode" : "User mode");
            var functionPrototypes = MkCFile(
                "FunctionPrototypes.h",
                emitHeaderComment ? MkComment(functionPrototypesCmt, true) : null, 
                CData.Trm_PragmaOnce(),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfPublic.h"), PData.Cnst_True),
                erase ? (AST<Node>)CData.Cnst_Nil() : (AST<Node>)AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfProtected.h"), PData.Cnst_True),
                funDeclHeaders);

            outModel = Add(outModel, functionPrototypes);

            string entryAndExitFnsCmt =
                string.Format(headerCmt,
                              "EntryFunctions.c",
                              "This file contains definitions for entry functions, exit functions, action functions, and declared functions.",
                              target,
                              DateTime.Today.Date.ToShortDateString(),
                              kernelMode ? "Kernel mode" : "User mode");
            var entryAndExitFns = MkCFile(
                "EntryFunctions.c",
                emitHeaderComment ? MkComment(entryAndExitFnsCmt, true) : null, 
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("FunctionPrototypes.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfPublic.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfProtected.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("PublicEnumTypes.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("ProtectedEnumTypes.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("ProtectedDriverDecl.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("PublicComplexTypes.h"), PData.Cnst_False),
                entriesB,
                exitsB,
                actionsB,
                funDeclBodies);

            outModel = Add(outModel, entryAndExitFns);

            string driverDeclCmt =
                string.Format(headerCmt,
                              "ProtectedDriverDecl.h",
                              "This file contains tables for events, machines, and driver.",
                              target,
                              DateTime.Today.Date.ToShortDateString(),
                              kernelMode ? "Kernel mode" : "User mode");
            var driverDecl = MkCFile(
                "ProtectedDriverDecl.h",
                emitHeaderComment ? MkComment(driverDeclCmt, true) : null,
                CData.Trm_PragmaOnce(),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfPublic.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfProtected.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("PublicEnumTypes.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("ProtectedMachineDecls.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("ProtectedEnumTypes.h"), PData.Cnst_False),
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("PublicComplexTypes.h"), PData.Cnst_False),
                MkEventTable(),
                MkMachineTable(),
                MkTypeRelativesSets(typeIds),
                MkTypeTable(complexClassTypeDecls),
                MkDriverDecl());

            outModel = Add(outModel, driverDecl);

            string driverName = string.Format("{0}.h", model.Node.Name);
            string driverCmt =
                string.Format(headerCmt,
                              driverName,
                              "This file contains the extern driver declaration.",
                              target,
                              DateTime.Today.Date.ToShortDateString(),
                              kernelMode ? "Kernel mode" : "User mode");
            var driver = MkCFile(
                driverName,
                emitHeaderComment ? MkComment(driverCmt, true) : null,
                AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfPublicTypes.h"), PData.Cnst_False),
                MkVarDef(CData.Cnst_Extern(), MkNmdType("SMF_DRIVERDECL"), DriverDeclName(), CData.Cnst_Nil()));

            outModel = Add(outModel, driver);

            //// outModel.Print(Console.Out);

            if (!PrintFile(outModel, env))
            {
                return false;
            }

            return true;
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

        private string getTupleField(int fNum) { return "field_" + fNum; }
        private string getFuncArg(int fNum) { return "arg_" + fNum; }



        private AST<FuncTerm> MkEnums(
            string binName,
            string ownerBinName,
            string enumPrefix, 
            string elemPrefix, 
            int nameIndex,
            int ownerIndex,
            int ownerNameIndex,
            Dictionary<string, int> idMap = null)
        {
            Contract.Requires(!string.IsNullOrEmpty(binName));
            var enumMap = new Dictionary<string, LinkedList<AST<FuncTerm>>>();
            var bin = GetBin(binName);
            var ownerBin = GetBin(ownerBinName);
            string name;
            LinkedList<AST<FuncTerm>> memList;

            foreach (var e in ownerBin)
            {
                var ownerName = GetName(e.Node, ownerNameIndex);
                enumMap[ownerName] = new LinkedList<AST<FuncTerm>>();
            }

            foreach (var e in bin)
            {
                name = GetOwnerName(e.Node, ownerIndex, ownerNameIndex);
                if (!enumMap.TryGetValue(name, out memList))
                {
                    memList = new LinkedList<AST<FuncTerm>>();
                    enumMap.Add(name, memList);
                }
                memList.AddLast(e);
            }

            AST<FuncTerm> defs = null, def;
            foreach (var enm in enumMap)
            {
                int id = enm.Value.Count - 1;
                AST<FuncTerm> elements = null, element, idNode;
              
                idNode = Factory.Instance.AddArg(CData.App_IntLit(), Factory.Instance.MkCnst(enm.Value.Count));
                idNode = Factory.Instance.AddArg(idNode, CData.Cnst_Dec());
                idNode = Factory.Instance.AddArg(idNode, CData.Cnst_Nil());

                element = Factory.Instance.AddArg(CData.App_Elements(), Factory.Instance.MkCnst(string.Format("_n{0}_{1}", enumPrefix, enm.Key)));
                element = Factory.Instance.AddArg(element, idNode);
                element = Factory.Instance.AddArg(element, CData.Cnst_Nil());
                elements = element;
                if (idMap != null)
                {
                    idMap.Add(string.Format("_n{0}_{1}", enumPrefix, enm.Key), enm.Value.Count);
                }

                foreach (var e in enm.Value)
                {
                    name = string.Format("{0}_{1}_{2}", elemPrefix, enm.Key, GetName(e.Node, nameIndex));
                    if (idMap != null)
                    {
                        idMap.Add(name, id);
                    }

                    idNode = Factory.Instance.AddArg(CData.App_IntLit(), Factory.Instance.MkCnst(id--));
                    idNode = Factory.Instance.AddArg(idNode, CData.Cnst_Dec());
                    idNode = Factory.Instance.AddArg(idNode, CData.Cnst_Nil());

                    element = Factory.Instance.AddArg(CData.App_Elements(), Factory.Instance.MkCnst(name));
                    element = Factory.Instance.AddArg(element, idNode);
                    element = Factory.Instance.AddArg(element, elements);
                    elements = element;
                }

                var enmDef = Factory.Instance.AddArg(CData.App_EnmDef(), CData.Cnst_Nil());
                enmDef = Factory.Instance.AddArg(enmDef, Factory.Instance.MkCnst(string.Format("{0}_{1}", enumPrefix, enm.Key)));
                enmDef = Factory.Instance.AddArg(enmDef, elements);
                if (defs != null)
                {
                    def = Factory.Instance.AddArg(CData.App_Section(), enmDef);
                    defs = Factory.Instance.AddArg(def, defs);
                }
                else
                {
                    defs = enmDef;
                }
            }

            return defs;
        }

        private AST<FuncTerm> MkEnums(
            string enumName,
            string prefix,
            IEnumerable<string> names)
        {
            var elements = new List<AST<Node>>(names.Select(name => Factory.Instance.MkCnst(prefix + name)));
            elements.Add(Factory.Instance.MkCnst("_n" + enumName));
            var indices = Enumerable.Range(0, names.Count() + 1).Select(num => MkIntLiteral(num));
            return AddArgs(CData.App_EnmDef(), CData.Cnst_Nil(), Factory.Instance.MkCnst(enumName),
                ConstructList2(CData.App_Elements(), elements, indices));
        }

        private AST<FuncTerm> MkEnum(IEnumerable<string> names, string prefix, string enumName, Dictionary<string, int> idMap = null)
        {
            if (idMap != null)
            {   // Record the index for each name. Note we do this WITHOUT the prifx.
                foreach (var nmId in names.Zip(Enumerable.Range(0, names.Count()), (nm, id) => new Tuple<string, int>(nm, id)))
                    if (!idMap.ContainsKey(nmId.Item1)) idMap[nmId.Item1] = nmId.Item2;
            }

            List<string> allNames = new List<string>(names.Select(nm => prefix + nm));  // Add the prefix
            allNames.Add("_n" + enumName);  // Add the "count" enum member

            return AddArgs(CData.App_EnmDef(), CData.Cnst_Nil(), Factory.Instance.MkCnst(enumName),
                ConstructList2(CData.App_Elements(),
                    allNames.Select(name => Factory.Instance.MkCnst(name)),
                    Enumerable.Range(0, allNames.Count()).Select(id => MkIntLiteral(id))));
        }
        // TODO: MkEnum/MkEnums code could use a cleanup. Its currently too complicated.
        private AST<FuncTerm> MkEnum(
            string binName, 
            string enumName, 
            string elemPrefix, 
            int nameIndex,
            Dictionary<string, int> idMap = null,
            string zeroth = null)
        {
            Contract.Requires(!string.IsNullOrEmpty(binName));
            var bin = GetBin(binName);
            string name;
            List<string> names = new List<string>();

            if (zeroth != null)
                names.Add(zeroth);

            foreach (var e in bin)
            {
                name = GetName(e.Node, nameIndex);
                names.Add(name);
            }

            return MkEnum(names, elemPrefix, enumName, idMap);
        }

        private AST<FuncTerm> MkEventSets()
        {
            string setName, machName, varDefName;
            AST<Node> packedDef;
            AST<FuncTerm> packedElements;
            AST<FuncTerm> section, sections = null;
            var bin = GetBin(PData.Con_EventSetDecl.Node.Name);
            
            //// Allocate space for event set declarations.
            Dictionary<string, AST<FuncTerm>[]> machESDecls = 
                new Dictionary<string, AST<FuncTerm>[]>();

            foreach (var m in modelMachineIds.Keys)
            {
                machESDecls.Add(m, new AST<FuncTerm>[modelEventSetIds["_nEventSets_" + m]]);
            }

            foreach (var e in bin)
            {
                MkEventSet(e, out setName, out machName, out varDefName, out packedElements);
                packedDef = MkVarDef(MkArrType("ULONG32"), string.Format("{0}_Packed", varDefName), packedElements);
                section = Factory.Instance.AddArg(CData.App_Section(), packedDef);
                sections = Factory.Instance.AddArg(section, sections == null ? (AST<Node>)CData.Cnst_Nil() : sections);

                var declId = modelEventSetIds[string.Format("EventSet_{0}_{1}", machName, setName)];
                var declArr = machESDecls[machName];
                declArr[declId] = MkInit(
                    MkId(string.Format("EventSet_{0}_{1}", machName, setName)),
                    MkId(string.Format("MachineType_{0}", machName, setName)),
                    MkStringLiteral(setName),
                    MkId(string.Format("{0}_Packed", varDefName)));
            }

            AST<FuncTerm> dclSections = null;
            foreach (var kv in machESDecls)
            {
                section = Factory.Instance.AddArg(
                    CData.App_Section(), 
                    MkVarDef(
                        MkArrType("SMF_EVENTSETDECL"),
                        string.Format("EventSetTable_{0}", kv.Key),
                        MkInit(kv.Value)));
                dclSections = Factory.Instance.AddArg(section, dclSections == null ? (AST<Node>)CData.Cnst_Nil() : dclSections);               
            }

            if (sections != null)
            {
                sections = Factory.Instance.AddArg(CData.App_Section(), sections);
            return Factory.Instance.AddArg(sections, dclSections);
        }
            else
            {
                return dclSections;
            }
        }

        private AST<FuncTerm> MkStateTables()
        {
            var bin = GetBin(PData.Con_MachineDecl.Node.Name);
            AST<FuncTerm> table, tables = null;
            foreach (var m in bin)
            {
                table = Factory.Instance.AddArg(CData.App_Section(), MkStateTable(m));
                tables = Factory.Instance.AddArg(table, tables == null ? (AST<Node>)CData.Cnst_Nil() : tables);
            }

            return tables;
        }

        private AST<FuncTerm> MkEventTable()
        {
            var eventsTable = new AST<FuncTerm>[modelEventIds.Count];     
            foreach (var ev in allEvents)
            {
                var eventName = ev.Key;
                var maxInstances = ev.Value.maxInstances;
                var payloadType = ev.Value.payloadType;

                if (eventName == DefaultEvent || eventName == DeleteEvent)
                    continue;

                    var data = MkInit(
                        MkId(string.Format("Event_{0}", eventName)),
                        MkStringLiteral(eventName),
                    maxInstances == -1 ? MkId("UINT16_MAX") : MkIntLiteral(maxInstances),
                    MkId(pTypeToCEnum(payloadType)));

                    eventsTable[modelEventIds[eventName]] = data;

            }

            if (eventsTable.Length == 0)
            {
                return MkSimpleDefine("EventTable", "NULL");
            }
            else
            {
                return MkVarDef(
                    MkArrType("SMF_EVENTDECL"),
                    "EventTable",
                    MkInit(eventsTable));
            }  
        }
        
        private AST<FuncTerm> MkMachineTable()
        {
            var bin = GetBin(PData.Con_MachineDecl.Node.Name);
            var binInits = GetBin(PData.Con_MachStart.Node.Name);
            var machTable = new AST<FuncTerm>[modelMachineIds.Count];
            string machName, initName;           
            foreach (var e in bin)
            {
                machName = GetName(e.Node, 0);
                initName = null;
                foreach (var init in binInits)
                {
                    if (GetOwnerName(init.Node, 0, 0) == machName)
                    {
                        initName = GetOwnerName(init.Node, 1, 0);
                        break;
                    }
                }

                if (initName == null)
                {
                    throw new Exception(string.Format("The machine {0} does not have an initial state", machName));
                }

                var data = MkInit(
                    MkId(string.Format("MachineType_{0}", machName)),
                    MkStringLiteral(machName),
                    MkId(string.Format("_nVars_{0}", machName)),
                    MkId(string.Format("VarTable_{0}", machName)),
                    MkId(string.Format("_nStates_{0}", machName)),
                    MkId(string.Format("StateTable_{0}", machName)),
                    allMachines[machName].maxQueueSize == -1 ? MkId("UINT8_MAX") : MkIntLiteral(allMachines[machName].maxQueueSize),
                    MkId(string.Format("_nEventSets_{0}", machName)),
                    MkId(string.Format("EventSetTable_{0}", machName)),
                    MkId(string.Format("State_{0}_{1}", machName, initName)),
                    MkFunApp("MAKE_OPAQUE_CONSTRUCTOR", default(Span), MkId(string.Format("Constructor_{0}", machName))));
                machTable[modelMachineIds[machName]] = data;
            }

            if (machTable.Length == 0)
            {
                return MkSimpleDefine("MachineTable", "NULL");
            }
            else
            {
                return MkVarDef(
                    MkArrType("SMF_MACHINEDECL"),
                    "MachineTable",
                    MkInit(machTable));
            }
        }

        private AST<FuncTerm> MkTypeRelativesSets(Dictionary<string, int> typeIds)
        {
            List<AST<Node>> typeSets = new List<AST<Node>>();
            foreach (var t in allTypes)
            {
                typeSets.Add(MkVarDef(MkArrType("ULONG32"), pTypeToCEnum(t) + "SuperTypes",
                        MkPackedSet(supertypes[t].Select(superT => typeIds[pTypeToCEnum(superT)]), allTypes.Count)));
                typeSets.Add(MkVarDef(MkArrType("ULONG32"), pTypeToCEnum(t) + "SubTypes",
                    MkPackedSet(subtypes[t].Select(subT => typeIds[pTypeToCEnum(subT)]), allTypes.Count)));
            }
            return (AST<FuncTerm>)ConstructCList(CData.App_Section(), typeSets);
        }

        private AST<FuncTerm> MkTypeTable(List<AST<Node>> typeDecls)
        {
            if (typeDecls.Count() == 0)
            {
                return MkSimpleDefine("TypeTable", "NULL");
            }
            else
            {
                return MkVarDef(
                    MkArrType("SMF_TYPEDECL"),
                    "TypeTable",
                    MkInit(typeDecls.ToArray()));
            }
        }
        
        private AST<FuncTerm> MkDriverDecl()
        {
            var data = MkInit(
                MkId("_nEvents"),
                MkId("EventTable"),
                MkId("_nMachineTypes"),
                MkId("MachineTable"),
                MkId("_nTypes"),
                MkId("TypeTable"));

            return MkVarDef(
                    MkNmdType("SMF_DRIVERDECL"),
                    DriverDeclName(),
                    data);
        }

        private AST<FuncTerm> MkVarTables()
        {
            var bin = GetBin(PData.Con_MachineDecl.Node.Name);
            AST<FuncTerm> table, tables = null;
            foreach (var m in bin)
            {
                table = Factory.Instance.AddArg(CData.App_Section(), MkVarTable(m));
                tables = Factory.Instance.AddArg(table, tables == null ? (AST<Node>)CData.Cnst_Nil() : tables);
            }

            return tables;
        }

        private AST<Node> MkVarTable(AST<FuncTerm> machine)
        {
            var bin = GetBin(PData.Con_VarDecl.Node.Name);
            var machName = GetName(machine.Node, 0);
            Node typeNode;
            string varName, varMach;
            var varTable = new AST<FuncTerm>[modelVarIds[string.Format("_nVars_{0}", machName)]];
            foreach (var s in bin)
            {
                varName = GetName(s.Node, 0);
                varMach = GetOwnerName(s.Node, 1, 0);
                if (varMach != machName)
                {
                    continue;
                }

                typeNode = GetArgByIndex(s.Node, 2);
                var pType = GetPType(typeNode);

                var data = MkInit(
                    MkId(string.Format("Var_{0}_{1}", machName, varName)),
                    MkId(string.Format("MachineType_{0}", machName)),
                    MkStringLiteral(varName),
                    MkId(pTypeToCEnum(pType)));
                varTable[modelVarIds[string.Format("Var_{0}_{1}", machName, varName)]] = data;
            }

            if (varTable.Length == 0)
            {
                return MkSimpleDefine(string.Format("VarTable_{0}", machName), "NULL");
            }
            else
            {
                return MkVarDef(
                    MkArrType("SMF_VARDECL"),
                    string.Format("VarTable_{0}", machName),
                    MkInit(varTable));
            }
        }

        private AST<Node> MkStateTable(AST<FuncTerm> machine)
        {
            var bin = GetBin(PData.Con_StateDecl.Node.Name);
            var machName = GetName(machine.Node, 0);
            string stateName, stateMach;
            AST<FuncTerm> section, sections = null;
            var stateTable = new AST<FuncTerm>[modelStateIds[string.Format("_nStates_{0}", machName)]];
            foreach (var s in bin)
            {
                stateName = GetName(s.Node, 0);
                stateMach = GetOwnerName(s.Node, 1, 0);
                if (stateMach != machName)
                {
                    continue;
                }

                var stateInfo = allMachines[machName].stateNameToStateInfo[stateName];
                
                var packedTransitionDefName = string.Format("Transitions_{0}_{1}_Packed", machName, stateName);
                var packedTransitionEvents = PackedEvents(stateInfo.transitions.Keys.Where(x => x != DefaultEvent));
                var packedTransitionDef = MkVarDef(MkArrType("ULONG32"), packedTransitionDefName, packedTransitionEvents);
                section = Factory.Instance.AddArg(CData.App_Section(), packedTransitionDef);
                sections = Factory.Instance.AddArg(section, sections == null ? (AST<Node>)CData.Cnst_Nil() : sections);

                var packedActionDefName = string.Format("Actions_{0}_{1}_Packed", machName, stateName);
                var packedActionEvents = PackedEvents(stateInfo.actions.Keys);
                var packedActionDef = MkVarDef(MkArrType("ULONG32"), packedActionDefName, packedActionEvents);
                section = Factory.Instance.AddArg(CData.App_Section(), packedActionDef);
                sections = Factory.Instance.AddArg(section, sections == null ? (AST<Node>)CData.Cnst_Nil() : sections);

                AST<FuncTerm> passiveFlag = null;
                if (stateInfo.entryFunAtPassive)
                {
                    if (stateInfo.exitFunAtPassive)
                    {
                        passiveFlag = AddArgs(CData.App_BinApp(), CData.Cnst_Bor(), MkId("SmfEntryFunPassiveLevel"), MkId("SmfExitFunPassiveLevel"));
                    }
                    else
                    {
                        passiveFlag = MkId("SmfEntryFunPassiveLevel");
                    }
                }
                else
                {
                    if (stateInfo.exitFunAtPassive)
                    {
                        passiveFlag = MkId("SmfExitFunPassiveLevel");
                    }
                    else
                    {
                        passiveFlag = MkId("SmfNoFlag");
                    }
                }
                var defersSet = GetArgByIndex(s.Node, 3);
                var data = MkInit(
                    MkId(string.Format("State_{0}_{1}", machName, stateName)),
                    MkId(string.Format("MachineType_{0}", machName)),
                    MkStringLiteral(stateName),
                    passiveFlag,
                    MkFunApp("MAKE_OPAQUE", default(Span), MkId(string.Format("EntryFun_{0}_{1}", machName, stateName))),
                    stateInfo.exitFun != null ? MkFunApp("MAKE_OPAQUE", default(Span), MkId(string.Format("ExitFun_{0}_{1}", machName, stateName))) : MkId("NULL"),
                    defersSet.NodeKind == NodeKind.Id && ((Id)defersSet).Name == "NIL" ? MkId(string.Format("EventSet_{0}_None", machName)) : MkId(string.Format("EventSet_{0}_{1}", machName, GetName(GetFuncTerm(defersSet), 0))),
                    MkIntLiteral(modelTransSizes[string.Format("State_{0}_{1}", machName, stateName)]),
                    MkId(string.Format("TransTable_{0}_{1}", machName, stateName)),
                    MkId(packedTransitionDefName),
                    MkIntLiteral(stateInfo.actions.Count),
                    MkId(string.Format("ActionTable_{0}_{1}", machName, stateName)),
                    MkId(packedActionDefName),
                    MkIntLiteral(allMachines[machName].stateNameToStateInfo[stateName].hasDefaultTransition ? 1 : 0));
                stateTable[modelStateIds[string.Format("State_{0}_{1}", machName, stateName)]] = data;
            }

            AST<FuncTerm> dclSection;
            if (stateTable.Length == 0)
            {
                dclSection =  MkSimpleDefine(string.Format("StateTable_{0}", machName), "NULL");
            }
            else
            {
                dclSection = MkVarDef(
                    MkArrType("SMF_STATEDECL"),
                    string.Format("StateTable_{0}", machName),
                    MkInit(stateTable));
            }

            return AddArgs(CData.App_Section(), sections, dclSection);
        }

        private AST<FuncTerm> MkTransTables()
        {
            var bin = GetBin(PData.Con_StateDecl.Node.Name);
            AST<FuncTerm> table, tables = null;
            foreach (var s in bin)
            {
                table = Factory.Instance.AddArg(CData.App_Section(), MkTransTable(s));
                tables = Factory.Instance.AddArg(table, tables == null ? (AST<Node>)CData.Cnst_Nil() : tables);
            }
            return tables;
        }

        private AST<Node> MkTransTable(AST<FuncTerm> state)
        {
            var stateName = GetName(state.Node, 0);
            var machName = GetOwnerName(state.Node, 1, 0);
            var bin = GetBin(PData.Con_TransDecl.Node.Name);

            string tState, tMachine;
            int index = 0;
            var transTable = new List<AST<FuncTerm>>();
            foreach (var t in bin)
            {
                tState = GetOwnerName(t.Node, 0, 0);
                tMachine = GetOwnerOwnerName(t.Node, 0, 1, 0);
                var isCall = ((Id)GetArgByIndex(t.Node, 3)).Name == "TRUE";
                if (tState == stateName && tMachine == machName)
                {
                    var transitionEvent = (Id) GetArgByIndex(t.Node, 1);
                    transTable.Add(
                        MkInit(
                            MkIntLiteral(index++),
                            MkId(string.Format("State_{0}_{1}", machName, stateName)),
                            MkId(string.Format("MachineType_{0}", machName)), 
                            transitionEvent.Name != PData.Cnst_Default.Node.Name ? MkId(string.Format("Event_{0}", GetOwnerName(t.Node, 1, 0))) : MkId("SmfDefaultEvent"),
                            MkId(string.Format("State_{0}_{1}", machName, GetOwnerName(t.Node, 2, 0))),
                            MkId(isCall ? "TRUE" : "FALSE")));                        
                }
            }
            
            modelTransSizes.Add(string.Format("State_{0}_{1}", machName, stateName), transTable.Count);
            if (transTable.Count == 0)
            {
                return MkSimpleDefine(string.Format("TransTable_{0}_{1}", machName, stateName), "NULL");
            }
            else
            {
                return MkVarDef(
                    MkArrType("SMF_TRANSDECL"),
                    string.Format("TransTable_{0}_{1}", machName, stateName),
                    MkInit(transTable.ToArray()));
            }           
        }

        private AST<FuncTerm> MkActionTables()
        {
            var bin = GetBin(PData.Con_StateDecl.Node.Name);
            AST<FuncTerm> table, tables = null;
            foreach (var s in bin)
            {
                table = Factory.Instance.AddArg(CData.App_Section(), MkActionTable(s));
                tables = Factory.Instance.AddArg(table, tables == null ? (AST<Node>)CData.Cnst_Nil() : tables);
            }
            return tables;
        }
        
        private AST<Node> MkActionTable(AST<FuncTerm> state)
        {
            var stateName = GetName(state.Node, 0);
            var machName = GetOwnerName(state.Node, 1, 0);
            var bin = GetBin(PData.Con_Install.Node.Name);

            string tState, tMachine;
            int index = 0;
            var actionTable = new List<AST<FuncTerm>>();
            foreach (var t in bin)
            {
                tState = GetOwnerName(t.Node, 0, 0);
                tMachine = GetOwnerOwnerName(t.Node, 0, 1, 0);
                if (tState == stateName && tMachine == machName)
                {
                    var eventName = GetOwnerName(t.Node, 1, 0);
                    var actionFunName = GetOwnerName(t.Node, 2, 0);
                    actionTable.Add(
                        MkInit(
                            MkIntLiteral(index++),
                            MkId(string.Format("State_{0}_{1}", machName, stateName)),
                            MkId(string.Format("MachineType_{0}", machName)),
                            MkStringLiteral(actionFunName),
                            MkId(string.Format("Event_{0}", GetOwnerName(t.Node, 1, 0))),
                            MkFunApp("MAKE_OPAQUE", default(Span), MkId(string.Format("ActionFun_{0}_{1}", machName, actionFunName))),
                            allMachines[machName].actionFunNameToActionFun[actionFunName].atPassive ? MkId("TRUE") : MkId("FALSE")));
                }
            }

            if (actionTable.Count == 0)
            {
                return MkSimpleDefine(string.Format("ActionTable_{0}_{1}", machName, stateName), "NULL");
            }
            else
            {
                return MkVarDef(
                    MkArrType("SMF_ACTIONDECL"),
                    string.Format("ActionTable_{0}_{1}", machName, stateName),
                    MkInit(actionTable.ToArray()));
            }
        }

        private void MkEventSet(AST<FuncTerm> eventSet,
                                out string setName,
                                out string ownerName,
                                out string varDefName,
                                out AST<FuncTerm> packedElements)
        {
            setName = GetName(eventSet.Node, 0);
            ownerName = GetOwnerName(eventSet.Node, 1, 0);
            var bin = GetBin(PData.Con_InEventSet.Node.Name);
            varDefName = string.Format("EventSetMbrs_{0}_{1}", ownerName, setName);

            HashSet<string> eventNames = new HashSet<string>();
            foreach (var e in bin)
            {
                if (GetOwnerName(e.Node, 0, 0) != setName)
                {
                    continue;
                }
                var eventName = GetOwnerName(e.Node, 1, 0);
                eventNames.Add(eventName);
            }
            packedElements = PackedEvents(eventNames);
        }

        private AST<FuncTerm> MkPackedSet(IEnumerable<int> ids, int totalNumIds)
        {
            Dictionary<int, uint> packedChunks = new Dictionary<int, uint>();
            foreach (var id in ids)
            {
                var chunk = id / 32;
                uint pack;
                if (!packedChunks.TryGetValue(chunk, out pack))
                {
                    pack = 0;
                }
                packedChunks[chunk] = pack | (1U << (short)(id % 32));
            }

            AST<Node> args = CData.Cnst_Nil();
            for (int i = (totalNumIds == 0 ? 0 : (totalNumIds - 1) / 32); i >= 0; --i)
            {
                uint pack;
                if (!packedChunks.TryGetValue(i, out pack))
                {
                    pack = 0;
                }
                args = AddArgs(CData.App_Args(), MkIntLiteral(pack, CData.Cnst_Hex()), args);
            }

            return Factory.Instance.AddArg(CData.App_Init(), args);
        }

        private AST<FuncTerm> PackedEvents(IEnumerable<string> eventNames)
        {
            return MkPackedSet(eventNames.Select(eventName => modelEventIds[eventName]), modelEventIds.Count);
        }

        private void MkFunDecls(out AST<FuncTerm> headers, out AST<FuncTerm> bodies)
        {
            var bin = GetBin(PData.Con_FunDecl.Node.Name);
            headers = erase ? null : MkFunDef(MkFunType(MkNmdType("BOOLEAN"), Factory.Instance.AddArg(CData.App_BaseType(), Factory.Instance.MkId("VOID"))), "NONDET", MkFunParams(false));
            bodies = null;

            AST<FuncTerm> header, body;
            foreach (var s in bin)
            {
                MkFunDecl(s, out header, out body);
                if (headers == null)
                {
                    headers = header;
                }
                else
                {
                    headers = AddArgs(CData.App_Section(), header, headers);
                }

                if (bodies == null)
                {
                    bodies = body;
                }
                else
                {
                    body = Factory.Instance.AddArg(CData.App_Section(), body);
                    bodies = Factory.Instance.AddArg(body, bodies);
                }
            }
        }

        private string pTypeToCEnum(PType pType)
        {
            if (pType == PType.Nil)
                return "Null";
            else if (pType == PType.Bool)
                return "Bool";
            else if (pType == PType.Event)
                return "Eid";
            else if (pType == PType.Id)
                return "Mid";
            else if (pType == PType.Int)
                return "Int";
            else if (pType == PType.Any)
                return "Any";
            else if (pType is PTupleType || pType is PNamedTupleType || pType is PSeqType || pType is PMapType)
                return declaredTypes[pType].cType;
            else 
                throw new NotImplementedException("Unknown complex type " + pType);
        }

        private AST<FuncTerm> pTypeToCType(PType pType)
        {
            if (pType == PType.Nil)
                return MkNmdType("LONG");
            else if (pType is PPrimitiveType)
            {
                if (pType == PType.Bool)
                    return MkNmdType("BOOLEAN");
                else if (pType == PType.Event)
                    return MkNmdType("SMF_EVENTDECL_INDEX");
                else if (pType == PType.Id)
                    return MkNmdType("SMF_MACHINE_HANDLE");
                else if (pType == PType.Int)
                    return MkNmdType("LONG");
            }
            else if (pType is PCompoundType)
            {
                if (pType is PTupleType)
                    return MkStructType(declaredTypes[pType].cType);
                else if (pType is PNamedTupleType)
                    return MkStructType(declaredTypes[pType].cType);
                else if (pType is PSeqType)
                    return MkNmdType(SMF_ARRAYLIST);
                else if (pType is PMapType)
                    return MkNmdType(SMF_HASHTABLE);
            }
            else if (pType is PAnyType)
            {
                return MkNmdType(SMF_PACKED_VALUE);
            }

            throw new NotImplementedException("Unknown complex type conversion to C type " + pType);
        }

        /// NOTE: This should only be called while building the BuildDefault_<type> method.
        /// There are some baked in assumptions here, such as for example the presence of the "Driver" parameters
        private AST<Node> GetCDefault(PToCFoldContext ctxt, PType t)
        {
            if (t == PType.Nil)
                return MkIntLiteral(0);
            else if (t == PType.Bool)
                return MkId("FALSE");
            else if (t == PType.Int)
                return MkIntLiteral(0);
            else if (t == PType.Id)
                return MkId("SmfNull");
            else if (t == PType.Event)
                return MkId("SmfNull");
            else if (t is PTupleType)
            {
                var tmpVar = ctxt.getTmpVar(t, false);
                var fieldDesc = getFieldDesc(t);

                List<AST<Node>> rawArgs = new List<AST<Node>>(
                    ((PTupleType)t).elements.Select(elT => GetCDefault(ctxt, elT)));

                List<AST<Node>> sideEffects = new List<AST<Node>>(
                    fieldDesc.Zip(rawArgs, (field, val) => MkAssignOrCast(ctxt, default(Span),
                        MkDot(tmpVar, field.Item3), field.Item1, val, field.Item1)));

                foreach (var s in sideEffects) ctxt.addSideEffect(s);
                return tmpVar;
            }
            else if (t is PNamedTupleType)
            {
                var tmpVar = ctxt.getTmpVar(t, false);
                var fieldDesc = getFieldDesc(t);

                List<AST<Node>> rawArgs = new List<AST<Node>>(
                    ((PNamedTupleType)t).elements.Select(field => GetCDefault(ctxt, field.Item2)));

                List<AST<Node>> sideEffects = new List<AST<Node>>(
                    fieldDesc.Zip(rawArgs, (field, val) => MkAssignOrCast(ctxt, default(Span),
                        MkDot(tmpVar, field.Item3), field.Item1, val, field.Item1)));

                foreach (var s in sideEffects) ctxt.addSideEffect(s);
                return tmpVar;
            }
            else if (t is PAnyType)
            {
                var tmpVar = ctxt.getTmpVar(t, false);
                ctxt.addSideEffect(MkFunApp(getCBuildDefName(t), default(Span), ctxt.driver, MkAddrOf(tmpVar)));
                return tmpVar;
            }
                throw new NotImplementedException("Can't get Unknown type: " + t);
        }

        private AST<Node> GetCTypeSize(PType t)
        {
            if (t is PPrimitiveType)
            {
                return AddArgs(CData.App_Sizeof(), MkNmdType("ULONG_PTR"));
            }
            else if (t is PAnyType || t is PSeqType || t is PMapType)
            {
                return AddArgs(CData.App_Sizeof(), pTypeToCType(t));
            }
            else
            {
                var addOp = AddArgs(CData.App_BinApp(), CData.Cnst_Add());
                if (t is PTupleType)
                    return (t as PTupleType).elements.Aggregate(MkIntLiteral(0), (acc, el) => AddArgs(addOp, GetCTypeSize(el), acc));

                if (t is PNamedTupleType)
                    return (t as PNamedTupleType).elements.Aggregate(MkIntLiteral(0), (acc, el) => AddArgs(addOp, GetCTypeSize(el.Item2), acc));

                throw new NotImplementedException("Unknown complex type: " + t);
            }
        }

        private AST<FuncTerm> MkForwardFunDecl(AST<Node> storageClass, AST<Node> funType, string funName, IEnumerable<string> paramNames)
        {
            return AddArgs(CData.App_FunDef(), storageClass, funType, Factory.Instance.MkCnst(funName), MkFunParams(false, paramNames.ToArray()), CData.Cnst_Unknown());
        }

        private AST<FuncTerm> MkForwardFunDecl(AST<Node> storageClass, AST<Node> funType, string funName, params string[] paramNames)
        {
            return MkForwardFunDecl(storageClass, funType, funName, new List<string>(paramNames));
        }
        
        
        private AST<FuncTerm> MkFunDef(AST<FuncTerm> funType, string funName, AST<Node> parameters)
        {
            return AddArgs(CData.App_FunDef(), CData.Cnst_Nil(), funType, Factory.Instance.MkCnst(funName), parameters, CData.Cnst_Unknown());
        }

        private AST<FuncTerm> MkFunDef(AST<FuncTerm> funType, string funName, AST<Node> parameters, AST<Node> body)
        {
            return AddArgs(CData.App_FunDef(), CData.Cnst_Nil(), funType, Factory.Instance.MkCnst(funName), parameters, body);
        }

        private void MkFunDecl(AST<FuncTerm> fun, out AST<FuncTerm> funHeader, out AST<FuncTerm> funBody)
        {
            var funName = GetName(fun.Node, 0);
            var ownerName = GetOwnerName(fun.Node, 1, 0);
            var qualifiedFunName = allMachines[ownerName].funNameToFunInfo[funName].isForeign ? funName : string.Format("{0}_{1}", ownerName, funName);
            var parameters = GetArgByIndex(fun.Node, 2);
            var pReturnType = GetPType(GetArgByIndex(fun.Node, 3));
            AST<FuncTerm> cReturnType;
            var cParameterNames = new List<string>();
            var cParameterTypes = new List<AST<Node>>();
            if (this.erase)
            {
                // all functions are foreign
                cParameterNames.Add("ExtContext");
                cParameterTypes.Add(MkNmdType("PVOID"));
            }
            else
            {
                cParameterNames.Add("Context");
                cParameterTypes.Add(MkNmdType("PSMF_SMCONTEXT"));
            }

            if (pReturnType is PNilType)
            {
                cReturnType = MkBaseType(CData.Cnst_Void());
            }
            else if (pReturnType is PPrimitiveType)
            {
                cReturnType = pTypeToCType(pReturnType);
            }
            else 
            {
                cReturnType = MkBaseType(CData.Cnst_Void());
                cParameterNames.Add("dst");
                cParameterTypes.Add(MkPtrType(pTypeToCType(pReturnType)));
            }

            while (true)
            {
                if (parameters.NodeKind == NodeKind.Id)
                    break;
                FuncTerm ft = (FuncTerm)parameters;
                using (var it = ft.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var parameterName = ((Cnst)it.Current).GetStringValue();
                    Debug.Assert(parameterName != "dst");
                    cParameterNames.Add(parameterName);
                    it.MoveNext();
                    var pParameterType = GetPType(it.Current);
                    cParameterTypes.Add(pParameterType is PPrimitiveType ?
                        pTypeToCType(pParameterType) :
                        MkPtrType(pTypeToCType(pParameterType)));

                    it.MoveNext();
                    parameters = it.Current;
                }
            }

            funHeader = MkFunDef(MkFunType(cReturnType, cParameterTypes.ToArray()),
                                 qualifiedFunName,
                                 MkFunParams(false, cParameterNames.ToArray()));
            funBody = null;
            if (this.erase)
                return;

            var ctxt = new PToCFoldContext(ownerName, true, this, MkArrow("Context", "Driver"), funName);
            var outTerm = Factory.Instance.ToAST(GetArgByIndex(fun.Node, 5)).Compute<AST<Node>>(
                x => EntryFun_UnFold(ctxt, x),
                (x, ch) => EntryFun_Fold(ctxt, x, ch));

            // Add temporary local variables, accumulated as a result of side effects
            funBody = AddArgs(CData.App_Block(), ctxt.emitCLocals(), ctxt.emitCSideEffects(outTerm));
            Debug.Assert(ctxt.sideEffectsStack.Count == 0);
           
            funBody = AddArgs(CData.App_Seq(), MkFunApp("DUMMYREFERENCE", default(Span), MkId("Context")), funBody);
            funBody = MkFunDef(MkFunType(cReturnType, cParameterTypes.ToArray()),
                                 qualifiedFunName,
                                 MkFunParams(false, cParameterNames.ToArray()),
                                 funBody);
        }

        private void MkEntryFuns(out AST<FuncTerm> headers, out AST<FuncTerm> bodies)
        {
            var bin = GetBin(PData.Con_StateDecl.Node.Name);
            headers = null;
            bodies = null;

            AST<FuncTerm> header, body;
            foreach (var s in bin)
            {
                var name = GetName(s.Node, 0);
                var ownerName = GetOwnerName(s.Node, 1, 0);
                var entryFunName = string.Format("EntryFun_{0}_{1}", ownerName, name);
                var entryFun = Factory.Instance.ToAST(GetArgByIndex(s.Node, 2));
                header = AddArgs(CData.App_VarDef(), CData.Cnst_Nil(), MkNmdType("SMF_ENTRYFUN"), Factory.Instance.MkCnst(entryFunName), CData.Cnst_Nil());
                MkEntryFun(ownerName, entryFunName, entryFun, out body);
                
                if (headers == null)
                {
                    headers = header;
                }
                else
                {
                    header = Factory.Instance.AddArg(CData.App_Section(), header);
                    headers = Factory.Instance.AddArg(header, headers);
                }

                if (bodies == null)
                {
                    bodies = body;
                }
                else
                {
                    body = Factory.Instance.AddArg(CData.App_Section(), body);
                    bodies = Factory.Instance.AddArg(body, bodies);
                }
            }
        }

        private void MkActionFuns(out AST<FuncTerm> headers, out AST<FuncTerm> bodies)
        {
            var bin = GetBin(PData.Con_ActionDecl.Node.Name);
            headers = null;
            bodies = null;

            AST<FuncTerm> header, body;
            foreach (var s in bin)
            {
                var actionFunName = GetName(s.Node, 0);
                var ownerName = GetOwnerName(s.Node, 1, 0);
                var actionFun = Factory.Instance.ToAST(GetArgByIndex(s.Node, 2));
                var uniqueActionFunName = string.Format("ActionFun_{0}_{1}", ownerName, actionFunName);
                header = AddArgs(CData.App_VarDef(), CData.Cnst_Nil(), MkNmdType("SMF_ENTRYFUN"), Factory.Instance.MkCnst(uniqueActionFunName), CData.Cnst_Nil());
                MkEntryFun(ownerName, uniqueActionFunName, actionFun, out body);

                if (headers == null)
                {
                    headers = header;
                }
                else
                {
                    header = Factory.Instance.AddArg(CData.App_Section(), header);
                    headers = Factory.Instance.AddArg(header, headers);
                }

                if (bodies == null)
                {
                    bodies = body;
                }
                else
                {
                    body = Factory.Instance.AddArg(CData.App_Section(), body);
                    bodies = Factory.Instance.AddArg(body, bodies);
                }
            }
        }

        private void MkExitFuns(out AST<FuncTerm> headers, out AST<FuncTerm> bodies)
        {
            var bin = GetBin("ExitFun");
            headers = null;
            bodies = null;

            AST<FuncTerm> header, body;
            foreach (var s in bin)
            {
                using (var it = s.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var stateDecl = GetFuncTerm(it.Current);
                    var stateName = GetName(stateDecl, 0);
                    var ownerName = GetMachineName(stateDecl, 1);
                    var exitFunName = string.Format("ExitFun_{0}_{1}", ownerName, stateName);
                    it.MoveNext();
                    var exitFun = Factory.Instance.ToAST(it.Current);
                    header = AddArgs(CData.App_VarDef(), CData.Cnst_Nil(), MkNmdType("SMF_EXITFUN"), Factory.Instance.MkCnst(exitFunName), CData.Cnst_Nil());
                    MkEntryFun(ownerName, exitFunName, exitFun, out body);
                }
                if (headers == null)
                {
                    headers = header;
                }
                else
                {
                    header = Factory.Instance.AddArg(CData.App_Section(), header);
                    headers = Factory.Instance.AddArg(header, headers);
                }

                if (bodies == null)
                {
                    bodies = body;
                }
                else
                {
                    body = Factory.Instance.AddArg(CData.App_Section(), body);
                    bodies = Factory.Instance.AddArg(body, bodies);
                }
            }
        }

        private void MkConstructorFunDecls(out AST<FuncTerm> consHeaders)
        {
            var bin = GetBin(PData.Con_MachineDecl.Node.Name);
            consHeaders = null;
            foreach (var m in bin)
            {
                var machineName = GetName(m.Node, 0);
                var constructorName = string.Format("Constructor_{0}", machineName);
                AST<FuncTerm> consHeader = AddArgs(CData.App_VarDef(), CData.Cnst_Nil(), MkNmdType("SMF_CONSTRUCTORFUN"), Factory.Instance.MkCnst(constructorName), CData.Cnst_Nil());

                if (consHeaders == null)
                {
                    consHeaders = consHeader;
                }
                else
                {
                    consHeaders = AddArgs(CData.App_Section(), consHeader, consHeaders);
                }
            }
        }

        private IEnumerable<Node> EntryFun_CalculateNumCallsUnFold(Node n)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                yield break;
            }

            var ft = (FuncTerm)n;
            foreach (var t in ft.Args)
            {
                yield return t;
            }
        }

         private int EntryFun_CalculateNumCallsFold(Node n, IEnumerable<int> children)
         {
             if (n.NodeKind != NodeKind.FuncTerm)
                 return 0;
             
             int count = 0;
             foreach (var i in children) {
                 count = count + i;
             }
             var ft = (FuncTerm)n;
             var funName = ((Id)ft.Function).Name;
             if (funName == PData.Con_Scall.Node.Name)
                 return count + 1;
             else
                 return count;
         }

        private void MkEntryFun(
            string ownerName,
            string entryFunName,
            AST<Node> entryFun,
            out AST<FuncTerm> funBody)
        {
            var numCallStatements = entryFun.Compute<int>(
            (x) => EntryFun_CalculateNumCallsUnFold(x),
            (x, ch) => EntryFun_CalculateNumCallsFold(x, ch));
            AST<Node> trampoline = CData.Cnst_Nil();
            for (int i = 1; i <= numCallStatements; i++)
            {
                var app = AddArgs(CData.App_BinApp(), CData.Cnst_Eq(), AddArgs(CData.App_BinApp(), CData.Cnst_PFld(), MkId("Context"), MkId("ReturnTo")), MkIntLiteral(i));
                var gotoStmt = AddArgs(CData.App_Goto(), Factory.Instance.MkCnst(string.Format("L{0}", i)));
                trampoline = AddArgs(CData.App_ITE(), app, gotoStmt, trampoline);
            }

            var ctxt = new PToCFoldContext(ownerName, false, this, MkArrow("Context", "Driver"));
            var outTerm = entryFun.Compute<AST<Node>>(
                x => EntryFun_UnFold(ctxt, x),
                (x, ch) => EntryFun_Fold(ctxt, x, ch));

            AST<Node> rawBody = ctxt.emitCSideEffects(outTerm);
            Debug.Assert(ctxt.sideEffectsStack.Count == 0);

            if (trampoline != CData.Cnst_Nil())
            {
                rawBody = AddArgs(CData.App_Seq(), trampoline, rawBody);
            }
            rawBody = AddArgs(CData.App_Seq(), MkFunApp("DUMMYREFERENCE", default(Span), MkId("Context")), rawBody);
            funBody = MkEntryFun(entryFunName, ctxt.emitCLocals(), rawBody);
        }

        private static AST<FuncTerm> MkFunApp(AST<Node> funExp, Span span, List<AST<Node>> args)
        {
            return AddArgs(CData.App_FunApp(span), funExp, ConstructList(CData.App_Args(span), args, CData.Cnst_Nil(span)));
        }

        private static AST<FuncTerm> MkFunApp(string funName, Span span, List<AST<Node>> args)
        {
            return MkFunApp(MkId(funName, span), span, args);
        }

        private static AST<FuncTerm> MkFunApp(AST<Node> funExp, Span span, params AST<Node>[] args)
        {
            return MkFunApp(funExp, span, new List<AST<Node>>(args));
        }

        private static AST<FuncTerm> MkFunApp(string funName, Span span, params AST<Node>[] args)
        {
            return MkFunApp(funName, span, new List<AST<Node>>(args));
        }

        private AST<FuncTerm> MkClone(PToCFoldContext ctxt, PType t, Span span, AST<Node> dst, AST<Node> src)
        {
            Debug.Assert(t is PCompoundType || t is PAnyType);
            return MkFunApp(getCCloneName(t), span, ctxt.driver, dst, src);
        }

        private static AST<FuncTerm> MkVarDef(AST<FuncTerm> type, string name)
        {
            return MkVarDef(CData.Cnst_Nil(), type, name, CData.Cnst_Nil());
        }

        private static AST<FuncTerm> MkVarDef(AST<FuncTerm> type, string name, AST<Node> init)
        {
            return MkVarDef(CData.Cnst_Nil(), type, name, init);
        }

        private static AST<FuncTerm> MkVarDef(AST<Id> storageClass, AST<FuncTerm> type, string name, AST<Node> init)
        {
            var varDef = Factory.Instance.AddArg(CData.App_VarDef(), storageClass);
            varDef = Factory.Instance.AddArg(varDef, type);
            varDef = Factory.Instance.AddArg(varDef, Factory.Instance.MkCnst(name));
            return Factory.Instance.AddArg(varDef, init);
        }

        private AST<FuncTerm> MkComment(string comment, bool isBlockStyle)
        {
            return Factory.Instance.AddArg(
                    Factory.Instance.AddArg(CData.App_Comment(), Factory.Instance.MkCnst(comment)),
                    isBlockStyle ? CData.Cnst_True() : CData.Cnst_False());
        }

        private static AST<FuncTerm> MkId(string name, Span span = default(Span))
        {
            return Factory.Instance.AddArg(CData.App_Ident(span), Factory.Instance.MkCnst(name));
        }

        private AST<FuncTerm> MkStringLiteral(string value, Span span = default(Span))
        {
            var strLit = Factory.Instance.AddArg(CData.App_StringLit(span), Factory.Instance.MkCnst(value));
            return Factory.Instance.AddArg(strLit, CData.Cnst_Long(span));
        }


        private AST<FuncTerm> MkIntLiteral(int value, Span span = default(Span))
        {
            var intLit = Factory.Instance.AddArg(CData.App_IntLit(span), Factory.Instance.MkCnst(value));
            intLit = Factory.Instance.AddArg(intLit, CData.Cnst_Dec(span));
            return Factory.Instance.AddArg(intLit, CData.Cnst_Nil(span));
        }

        private AST<FuncTerm> MkIntLiteral(int value, AST<Id> format, Span span = default(Span))
        {
            var intLit = Factory.Instance.AddArg(CData.App_IntLit(span), Factory.Instance.MkCnst(value));
            intLit = Factory.Instance.AddArg(intLit, format);
            return Factory.Instance.AddArg(intLit, CData.Cnst_Nil(span));
        }

        private AST<FuncTerm> MkIntLiteral(uint value, AST<Id> format, Span span = default(Span))
        {
            var intLit = Factory.Instance.AddArg(CData.App_IntLit(span), Factory.Instance.MkCnst(value));
            intLit = Factory.Instance.AddArg(intLit, format);
            return Factory.Instance.AddArg(intLit, CData.Cnst_Nil(span));
        }

        private AST<FuncTerm> MkArrType(string typeName)
        {
            var nmdType = Factory.Instance.AddArg(CData.App_NmdType(), CData.Cnst_Nil());
            nmdType = Factory.Instance.AddArg(nmdType, Factory.Instance.MkCnst(typeName));
            var arrType = Factory.Instance.AddArg(CData.App_ArrType(), nmdType);
            return Factory.Instance.AddArg(arrType, CData.Cnst_Nil());
        }

        private static AST<FuncTerm> MkPtrType(AST<Node> toType)
        {
            return Factory.Instance.AddArg(CData.App_PtrType(), toType);
        }

        private AST<FuncTerm> MkPtrType(string typeName)
        {
            var nmdType = Factory.Instance.AddArg(CData.App_NmdType(), CData.Cnst_Nil());
            nmdType = Factory.Instance.AddArg(nmdType, Factory.Instance.MkCnst(typeName));
            return MkPtrType(nmdType);
        }

        private AST<FuncTerm> MkFunType(AST<Node> retType, IEnumerable<AST<Node>> argTypes)
        {
            return AddArgs(CData.App_FunType(), retType, ConstructCList(CData.App_PrmTypes(), argTypes));
        }

        private AST<FuncTerm> MkFunType(AST<Node> retType, params AST<Node>[] argTypes)
        {
            return MkFunType(retType, new List<AST<Node>>(argTypes));
        }

        private AST<Node> MkFunParams(bool withEllipsis = false, params string[] prmNames)
        {
            AST<Node> prms = withEllipsis ? CData.Cnst_Ellipse() : CData.Cnst_Nil();
            AST<FuncTerm> prm;
            for (int i = prmNames.Length - 1; i >= 0; i--)
            {
                prm = Factory.Instance.AddArg(CData.App_Params(), CData.Cnst_Nil());
                prm = Factory.Instance.AddArg(prm, Factory.Instance.MkCnst(prmNames[i]));
                prms = Factory.Instance.AddArg(prm, prms);
            }

            return prms;
        }

        AST<Node> MkBlock(AST<Node> body, params AST<Node>[] locals)
        {
            return AddArgs(CData.App_Block(), ConstructCList(CData.App_Defs(), locals), body);
        }

        AST<Node> MkSeq(IEnumerable<AST<Node>> stmts)
        {
            return ConstructCList(CData.App_Seq(), stmts);
        }

        AST<Node> MkSeq(params AST<Node>[] stmts)
        {
            return ConstructCList(CData.App_Seq(), stmts);
        }

        AST<Node> MkAssignment(AST<Node> lhs, AST<Node> rhs)
        {
            return AddArgs(CData.App_BinApp(), CData.Cnst_Asn(), lhs, rhs);
        }

        AST<Node> MkDot(AST<Node> lhs, string member)
        {
            return AddArgs(CData.App_BinApp(), CData.Cnst_Fld(), lhs, AddArgs(CData.App_Ident(), Factory.Instance.MkCnst(member)));
        }

        static AST<FuncTerm> MkBinApp(AST<Id> op, AST<Node> exp1, AST<Node> exp2)
        {
            return AddArgs(CData.App_BinApp(), op, exp1, exp2);
        }


        static AST<FuncTerm> MkUnop(AST<Id> op, AST<Node> exp)
        {
            return AddArgs(CData.App_UnApp(), op, exp);
        }

        static AST<FuncTerm> MkAddrOf(AST<Node> exp)
        {
            return MkUnop(CData.Cnst_Addr(), exp);
        }

        AST<FuncTerm> MkDrf(AST<Node> exp)
        {
            return MkUnop(CData.Cnst_Drf(), exp);
        }

        static AST<Node> MkArrow(string baseE, string member)
        {
            return AddArgs(CData.App_BinApp(), CData.Cnst_PFld(), MkId(baseE), MkId(member));            
        }

        static AST<Node> MkArrow(AST<Node> lhs, string member)
        {
            return AddArgs(CData.App_BinApp(), CData.Cnst_PFld(), lhs, AddArgs(CData.App_Ident(), Factory.Instance.MkCnst(member)));
        }

        private AST<FuncTerm> MkNmdType(string typeName, AST<Node> kind)
        {
            var nmdType = Factory.Instance.AddArg(CData.App_NmdType(), kind);
            return Factory.Instance.AddArg(nmdType, Factory.Instance.MkCnst(typeName));
        }

        private AST<FuncTerm> MkBaseType(AST<Id> type)
        {
            return AddArgs(CData.App_BaseType(), type);
        }

        private AST<FuncTerm> MkNmdType(string typeName)
        {
            return MkNmdType(typeName, CData.Cnst_Nil());
        }

        private AST<FuncTerm> MkStructType(string typeName)
        {
            var nmdType = Factory.Instance.AddArg(CData.App_NmdType(), CData.Cnst_Struct());
            return Factory.Instance.AddArg(nmdType, Factory.Instance.MkCnst(typeName));
        }

        private string dbgAst2Str(AST<Node> n)
        {
            System.IO.StringWriter sw = new System.IO.StringWriter();
            n.Print(sw);
            return sw.ToString();
        }

        private string dbgAst2Str(Node n)
        {
            return dbgAst2Str(Factory.Instance.ToAST(n));
        }

        private AST<Node> MkDataDef(string name, PType t)
        {
            var fieldDesc = getFieldDesc(t);
            var cType = pTypeToCType(t);

            var fields = ConstructList3(CData.App_Fields(),
                fieldDesc.Select(fd => pTypeToCType(fd.Item1)),
                fieldDesc.Select(fd => Factory.Instance.MkCnst(fd.Item3)),
                fieldDesc.Select(fd => CData.Cnst_Nil()));

            return AddArgs(CData.App_DataDef(), CData.Cnst_Nil(), CData.Cnst_Struct(), Factory.Instance.MkCnst(name), fields);
        }

        private AST<FuncTerm> MkSimpleDefine(string identDefining, string identDefinition)
        {
            var ppDef = Factory.Instance.AddArg(CData.App_PpDefine(), MkId(identDefining));
            return Factory.Instance.AddArg(ppDef, MkId(identDefinition));
        }

        private AST<FuncTerm> MkInit(params AST<Node>[] values)
        {
            Contract.Requires(values != null && values.Length > 0);
            AST<FuncTerm> args = null, arg;
            for (int i = values.Length - 1; i >= 0; --i)
            {
                arg = Factory.Instance.AddArg(CData.App_Args(), values[i]);
                args = Factory.Instance.AddArg(arg, args == null ? (AST<Node>)CData.Cnst_Nil() : args);
            }

            return Factory.Instance.AddArg(CData.App_Init(), args);
        }

        private AST<FuncTerm> MkVar(string varName, string ownerName, PType type)
        {
            //// Context->Values[Var_<ownerName>_<varName>]
            var vals = Factory.Instance.AddArg(CData.App_BinApp(), CData.Cnst_PFld());
            vals = Factory.Instance.AddArg(vals, MkId("Context"));
            vals = Factory.Instance.AddArg(vals, MkId("Values"));

            var arr = Factory.Instance.AddArg(CData.App_BinApp(), CData.Cnst_AAc());
            arr = Factory.Instance.AddArg(arr, vals);
            arr = Factory.Instance.AddArg(arr, MkId(string.Format("Var_{0}_{1}", ownerName, varName)));

            if (type is PPrimitiveType)
                return arr;
            else
            {
                if (type is PTupleType || type is PNamedTupleType) {
                    var structType = AddArgs(CData.App_NmdType(), CData.Cnst_Struct(), Factory.Instance.MkCnst(declaredTypes[type].cType));
                    var pStructType = AddArgs(CData.App_PtrType(), structType);
                    var casted = AddArgs(CData.App_Cast(), pStructType, arr);
                    var derefed = MkUnop(CData.Cnst_Drf(), casted);
                    return derefed;
                }
                else if (type is PAnyType)
                {
                    var casted = AddArgs(CData.App_Cast(), MkNmdType("PSMF_PACKED_VALUE"), arr);
                    var derefed = MkUnop(CData.Cnst_Drf(), casted);
                    return derefed;
                }
                else if (type is PSeqType)
                {
                    var casted = AddArgs(CData.App_Cast(), MkNmdType("PSMF_ARRAYLIST"), arr);
                    var derefed = MkUnop(CData.Cnst_Drf(), casted);
                    return derefed;
                }
                else if (type is PMapType)
                {
                    var casted = AddArgs(CData.App_Cast(), MkNmdType("PSMF_HASHTABLE"), arr);
                    var derefed = MkUnop(CData.Cnst_Drf(), casted);
                    return derefed;
                }

                throw new NotImplementedException("Unknown complex type " + type);
            }
        }

        private AST<Node> MkUpCast(PToCFoldContext ctxt, AST<Node> lhs, PType lhsType, AST<Node> rhs, PType rhsType)
        {
            Debug.Assert(rhsType.isSubtypeOf(lhsType));

            if (lhsType is PAnyType)
            {
                return MkFunApp("PackValue", default(Span), ctxt.driver, MkAddrOf(lhs), AddArgs(CData.App_Cast(default(Span)), MkNmdType("ULONG_PTR"), rhs), MkId(pTypeToCEnum(rhsType)));
            } else
            {
                return MkFunApp(getCUpCastName(rhsType, lhsType), default(Span), ctxt.driver, MkAddrOf(lhs), rhs);
            }
        }

        private AST<Node> MkAssignOrCast(PToCFoldContext ctxt, Span span, AST<Node> lhs, PType lhsType, AST<Node> rhs, PType rhsType)
        {
            if (lhsType == rhsType)
            {
                if (ctxt.isTmpVar(rhs.Node))
                {
                    ctxt.replaceTempVar(rhs, lhs);
                    return CData.Cnst_Nil();
                }
                else
                {
                    if (lhsType is PNilType)
                    {
                        return MkAssignment(lhs, MkIntLiteral(0)); // TODO: Maybe don't even need to emit this.
                    }
                    if (lhsType is PPrimitiveType)
                    {
                        return MkAssignment(lhs, ctxt.consumeExp(rhs));
                    }
                    else
                    {
                        return MkClone(ctxt, lhsType, span, MkAddrOf(lhs), MkAddrOf(ctxt.consumeExp(rhs)));
                    }
                }
            }
            else if (lhsType is PEventType && rhsType is PNilType)
            {
                return MkAssignment(lhs, MkId("SmfNull"));
            }
            else if (lhsType is PIdType && rhsType is PNilType)
            {
                return MkAssignment(lhs, MkId("SmfNull"));
            }
            else if (rhsType.isSubtypeOf(lhsType))
            {
                var consumedRhs = ctxt.consumeExp(rhs);
                return MkUpCast(ctxt, lhs, lhsType, (rhsType is PPrimitiveType) ? consumedRhs : MkAddrOf(consumedRhs), rhsType);
            }
            else if (lhsType.isSubtypeOf(rhsType))
            {
                var consumedRhs = ctxt.consumeExp(rhs);
                return MkFunApp(getCDownCastName(rhsType, lhsType), default(Span), ctxt.driver,
                    AddArgs(CData.App_Cast(), MkPtrType(pTypeToCType(lhsType)), MkAddrOf(lhs)),
                    (rhsType is PPrimitiveType) ? consumedRhs : MkAddrOf(consumedRhs));
            }
            else
                throw new Exception(string.Format("Can't assign ({0})::{1} to ({2})::{3}.", dbgAst2Str(rhs), rhsType, dbgAst2Str(lhs), lhsType));   
        }

        private void GetEventInfo(Node node, out string eventName, out PType eventArgTypeName)
        {
            if (node.NodeKind == NodeKind.Id && ((Id)node).Name != PData.Cnst_Default.Node.Name)
            {
                var eventDecl = GetFuncTerm(node);
                eventName = ((Cnst)GetArgByIndex(eventDecl, 0)).GetStringValue();
                eventArgTypeName = GetPType(GetArgByIndex(eventDecl, 2));
            }
            else
            {
                eventName = DefaultEvent;
                eventArgTypeName = new PNilType();
            }
        }

        private string LookupOwnerName(Node n)
        {
            return ((Cnst)GetArgByIndex(GetFuncTerm(n), 0)).GetStringValue();
        }

        private string GetName(
            FuncTerm ft,
            int nameIndex)
        {
            return ((Cnst)GetArgByIndex(ft, nameIndex)).GetStringValue();
        }

        private string GetOwnerName(
            FuncTerm ft,
            int ownerIndex,
            int ownerNameIndex)
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

        private string GetOwnerOwnerName(
            FuncTerm ft,
            int ownerIndex,
            int ownerOwnerIndex,
            int ownerOwnerNameIndex)
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

        private static Node GetArgByIndex(FuncTerm ft, int index)
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

        private LinkedList<AST<FuncTerm>> GetBin(FuncTerm ft)
        {
            var fun = (Id)ft.Function;
            return GetBin(fun.Name);
        }        

        private LinkedList<AST<FuncTerm>> GetBin(string name)
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

        private AST<FuncTerm> MkEntryFun(string name, AST<Node> locals, AST<Node> body)
        {
            var retTypes = Factory.Instance.AddArg(CData.App_PrmTypes(), MkNmdType("PSMF_SMCONTEXT"));
            retTypes = Factory.Instance.AddArg(retTypes, CData.Cnst_Nil());
            var funType = Factory.Instance.AddArg(CData.App_FunType(), MkNmdType("VOID"));
            funType = Factory.Instance.AddArg(funType, retTypes);

            var prms = AddArgs(CData.App_Params(), CData.Cnst_Nil(), Factory.Instance.MkCnst("Context"), CData.Cnst_Nil());

            var funDef = Factory.Instance.AddArg(CData.App_FunDef(), CData.Cnst_Nil());
            funDef = Factory.Instance.AddArg(funDef, funType);
            funDef = Factory.Instance.AddArg(funDef, Factory.Instance.MkCnst(name));
            funDef = Factory.Instance.AddArg(funDef, prms);
            return Factory.Instance.AddArg(funDef, AddArgs(CData.App_Block(), locals, body));
        }

        private AST<FuncTerm> MkCFile(string filename, params AST<Node>[] elements)
        {
            Contract.Requires(elements != null && elements.Length > 0);
            return AddArgs(CData.App_File(), Factory.Instance.MkCnst(filename), ConstructCList(CData.App_Section(), elements.Where(el => el != null)));
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

        private int getCIntConst(FuncTerm cExp)
        {
            if (getFtName(cExp) == CData.Con_Cast().Node.Name)
            {
                return getCIntConst((FuncTerm)GetArgByIndex(cExp, 1));
            }
            else if (getFtName(cExp) == CData.Con_IntLit().Node.Name)
            {
                return (int)((Cnst)GetArgByIndex(cExp, 0)).GetNumericValue().Numerator;
            }
            else
            {
                throw new Exception(string.Format("C expression '{0}' is not an integer constant", dbgAst2Str(cExp)));
            }
        }
        
        private const string Con_LabeledExpr = "___InternalLabeledExpression";
        private AST<FuncTerm> App_LabeledExpr = Factory.Instance.MkFuncTerm(Factory.Instance.MkId(Con_LabeledExpr));
        
        private IEnumerable<Node> EntryFun_UnFold(PToCFoldContext ctxt, Node n)
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
            } else if (funName == PData.Con_NamedExprs.Node.Name)
            {
                do
                {
                    yield return AddArgs(App_LabeledExpr, Factory.Instance.ToAST(GetArgByIndex(ft, 0)), Factory.Instance.ToAST(GetArgByIndex(ft,1))).Node;
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
                foreach (var a in EntryFun_UnFold(ctxt, GetArgByIndex(ft, 0)))
                {
                    yield return a;
                }
            }
            else if (funName == PData.Con_NamedTuple.Node.Name)
            {
                foreach (var a in EntryFun_UnFold(ctxt, GetArgByIndex(ft, 0)))
                {
                    yield return a;
                }
            }
            else if (funName == PData.Con_DataOp.Node.Name)
            {
                foreach (var a in EntryFun_UnFold(ctxt, GetArgByIndex(ft, 1)))
                {
                    yield return a;
                }
            }
            else if (funName == PData.Con_Seq.Node.Name)
            {
                using (var it = ft.Args.GetEnumerator())
                {
                    it.MoveNext();
                    ctxt.pushSideEffectStack();
                    yield return it.Current;
                    it.MoveNext();
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
                    ctxt.pushSideEffectStack();
                    it.MoveNext();
                    yield return it.Current;
                    it.MoveNext();
                    ctxt.pushSideEffectStack();
                    yield return it.Current;
                }
            } 
            else if (funName == PData.Con_New.Node.Name)
            {
                foreach (var a in EntryFun_UnFold(ctxt, GetArgByIndex(ft, 1)))
                {
                    yield return a;
                }
                }
            else
            {
                foreach (var t in ft.Args)
                {
                    if (t.NodeKind == NodeKind.FuncTerm &&
                        (((Id)((FuncTerm)t).Function).Name == PData.Con_Exprs.Node.Name))
                    {
                        foreach (var a in EntryFun_UnFold(ctxt, t))
                        {
                            yield return a;
                        }
                    }
                    else
                    {
                        yield return t;
                    }
                }
            }
        }

        /// <summary>
        /// The PToCFoldContext maintains (amongst other things) a set of
        /// side effect for each statement, and a set of temporary stack variables
        /// needed for these side effects. Those are stored using a stack of side effects,
        /// and a stack of variables that may need to be cleaned up. We push/pop from these stacks on every
        /// code block, and sequence of 2 statements. (see EntryFun_Unfold for more details).
        /// 
        /// Invariants about temporary variables:
        ///     - all temporary variables are linear
        ///     - the life time of a temporary variable is always between two P Statements. Its from its initializations
        ///     in the side effects of some statement S, to S itself.
        /// </summary>
        class PToCFoldContext
        {
            public string ownerName;
            public int callStatementCounter;
            public bool isFunction;
            public string funName;
            public Stack<List<AST<Node>>> sideEffectsStack;
            public Dictionary<PType, HashSet<Tuple<string, bool>>> tmpVars;
            public Stack<List<Tuple<PType, bool, string>>> destroyStack;
            private HashSet<string> freeVars;
            private HashSet<string> busyVars;
            private Dictionary<string, int> tmpUseCount;
            private Compiler compiler;
            private AST<Node> driverRef;

            public PToCFoldContext(string ownerName, bool isFunction, Compiler comp, AST<Node> driver = null, string funName = null)
            {
                this.ownerName = ownerName;
                this.callStatementCounter = 0;
                this.isFunction = isFunction;
                this.sideEffectsStack = new Stack<List<AST<Node>>>();
                this.freeVars = new HashSet<string>();
                this.busyVars = new HashSet<string>();
                this.tmpUseCount = new Dictionary<string, int>();
                this.destroyStack = new Stack<List<Tuple<PType, bool, string>>>();
                this.compiler = comp;
                this.tmpVars = new Dictionary<PType, HashSet<Tuple<string, bool>>>();
                this.pushSideEffectStack();
                this.driverRef = driver == null ? MkId("Driver") : driver;
                this.funName = funName;
            }

            public AST<Node> driver { get { return driverRef; } }

            public void pushSideEffectStack()
            {
                this.sideEffectsStack.Push(new List<AST<Node>>());
                this.destroyStack.Push(new List<Tuple<PType, bool, string>>());
            }

            public AST<Node> emitCLocals()
            {
                // At the end of a block, the only variables that should still be "busy", should be ones that never
                // got consumed, and therefore shouldn't be part of any expression
                
                //Debug.Assert(busyVars.All(var => tmpUseCount[var] == 0));  //[Ankush]  BUG : Not sure if this assertion is needed 
                Debug.Assert(freeVars.All(var => tmpUseCount[var] > 0));
                var varDefs = new List<AST<Node>>();

                foreach (PType t in tmpVars.Keys) {
                    foreach (Tuple<string, bool> var in tmpVars[t])
                    {
                        if (tmpUseCount[var.Item1] == 0)
                            continue;

                        varDefs.Add(MkVarDef(var.Item2 ? MkPtrType(compiler.pTypeToCType(t)) : compiler.pTypeToCType(t), var.Item1));
                    }
                }
                return ConstructCList(CData.App_Defs(), varDefs);
        }

            public AST<Node> emitCSideEffects(AST<Node> stmt)
            {
                var sideEffects = this.sideEffectsStack.Pop();
                var cleanup = this.destroyStack.Pop();
                List<AST<Node>> res = new List<AST<Node>>();

                res.AddRange(sideEffects);
                res.Add(stmt);

                foreach (var v in cleanup)
                {
                    if (v.Item2)
                        throw new NotImplementedException("Haven't implemented cleanup for heap temporary vars yet!");

                    if (!(v.Item1 is PTupleType) && !(v.Item1 is PNamedTupleType) && !(v.Item1 is PAnyType) && !(v.Item1 is PSeqType))
                        throw new NotImplementedException("Revisit cleanup for new type " + v.Item1);

                    if (!typeNeedsDestroy(v.Item1))
                        continue;

                    if (tmpUseCount[v.Item3] == 0)
                        continue;

                    res.Add(MkFunApp(compiler.getCDestroyName(v.Item1), default(Span), driver, MkAddrOf(MkId(v.Item3))));

                }

                if (res.Count > 1)
                    return compiler.MkSeq(res);
                else
                    return stmt;
            }

            // This routine emits all the accumulated cleanup code, and prepends it to the given statement. This
            // is used for the raise (..) code, to emit all the neccessary cleanup code before the return.
            // Note that we don't remove any of the accumulated clean up actions from the stack.
            public AST<Node> emitAllCleanup(AST<Node> stmt)
            {
                List<AST<Node>> res = new List<AST<Node>>();
                // Copy the destroy stack
                var currentCleanup = new List<List<Tuple<PType, bool, string>>>(this.destroyStack);
                // Reverse it.
                currentCleanup.Reverse();

                foreach (var cleanupScope in currentCleanup)
                {
                    foreach (var v in cleanupScope)
                    {
                        if (v.Item2)
                            throw new NotImplementedException("Haven't implemented cleanup for heap temporary vars yet!");

                        if (!(v.Item1 is PTupleType) && !(v.Item1 is PNamedTupleType) && !(v.Item1 is PAnyType))
                            throw new NotImplementedException("Revisit cleanup for new type " + v.Item1);

                        if (!typeNeedsDestroy(v.Item1))
                            continue;

                        if (tmpUseCount[v.Item3] == 0)
                            continue;

                        res.Add(MkFunApp(compiler.getCDestroyName(v.Item1), default(Span), driver, MkAddrOf(MkId(v.Item3))));
                    }
                }

                res.Add(stmt);

                if (res.Count > 1)
                    return compiler.MkSeq(res);
                else
                    return stmt;
            }

            public void addSideEffect(AST<Node> seffect)
            {
                this.sideEffectsStack.Peek().Add(seffect);
            }

            public bool hasFreeVar(PType t, bool isPtr)
            {
                return tmpVars.ContainsKey(t) && tmpVars[t].Any(var => (var.Item2 == isPtr) && freeVars.Contains(var.Item1));
            }

            public AST<Node> getTmpVar(PType pType, bool isPtr, bool cleanup = true)
            {
                string tmpVarName;
                if (hasFreeVar(pType, isPtr)) {
                    var tmpVar = tmpVars[pType].First(var => var.Item2 == isPtr && freeVars.Contains(var.Item1));
                    freeVars.Remove(tmpVar.Item1);
                    tmpVarName = tmpVar.Item1;
                } else {
                    var cType = compiler.pTypeToCType(pType);
                    tmpVarName = compiler.getUnique("tmp");
                    tmpUseCount[tmpVarName] = 0;

                    if (!tmpVars.ContainsKey(pType))
                        tmpVars[pType] = new HashSet<Tuple<string,bool>>();

                    tmpVars[pType].Add(new Tuple<string, bool>(tmpVarName, isPtr));
                }

                if (cleanup && typeNeedsDestroy(pType))
                {
                    if (isPtr)
                        throw new NotImplementedException("Check that this logic is still ok with new complex types");

                    this.destroyStack.Peek().Add(new Tuple<PType, bool, string>(pType, isPtr, tmpVarName));
                }

                busyVars.Add(tmpVarName);
                return MkId(tmpVarName);
            }

            public AST<Node> consumeExp(AST<Node> n)
            {
                if (isTmpVar(n.Node))
                {
                    var name = ((Cnst)GetArgByIndex((FuncTerm)n.Node, 0)).GetStringValue();
                    Debug.Assert(busyVars.Contains(name));
                    tmpUseCount[name]++;
                    busyVars.Remove(name);
                    freeVars.Add(name);
                }

                return n;
            }

            public bool isTmpVar(Node n)
            {
                if (!isInstanceOf(n, CData.App_Ident()))
                    return false;

                var name = ((Cnst)GetArgByIndex((FuncTerm)n, 0)).GetStringValue();
                return busyVars.Contains(name);
            }

            public AST<Node> removeLastSideEffect()
            {
                var sEffects = this.sideEffectsStack.Peek();
                var ret = sEffects.Last();
                sEffects.RemoveAt(sEffects.Count - 1);
                return ret;
            }

            public void replaceTempVar(AST<Node> var, AST<Node> expr)
            {
                Debug.Assert(isInstanceOf(var.Node, CData.App_Ident()));
                var name = ((Cnst)GetArgByIndex((FuncTerm)var.Node, 0)).GetStringValue();

                var topSideEffectStack = this.sideEffectsStack.Pop();
                var newSideEffectStack = topSideEffectStack.Select(term => compiler.ReplaceVar(term, name, expr));
                this.sideEffectsStack.Push(new List<AST<Node>>(newSideEffectStack));
            }
        }

        private Dictionary<Node, Node> cnodeToPNode = new Dictionary<Node,Node>();

        internal class RenameVarCtxt
        {
            public string var;
            public AST<Node> expr;
        }

        private IEnumerable<Node> RenameVar_UnFold(RenameVarCtxt ctxt, Node n)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                yield break;
            }

            var ft = (FuncTerm)n;
            foreach (var t in ft.Args)
            {
                yield return t;
            }
        }

        private AST<Node> RenameVar_Fold(RenameVarCtxt ctxt, Node n, IEnumerable<AST<Node>> children)
        {
            if (n.NodeKind == NodeKind.FuncTerm)
            {
                if (getFtName((FuncTerm)n) == CData.Con_Ident().Node.Name)
                {
                    var varName = ((Cnst)GetArgByIndex((FuncTerm)n, 0)).GetStringValue();
                    return varName == ctxt.var ? ctxt.expr : MkId(varName);
                }
                else
                {
                    var ftId = (AST<Id>)Factory.Instance.ToAST((Id)((FuncTerm)n).Function);
                    var res = Factory.Instance.MkFuncTerm(ftId, n.Span);
                    return AddArgs(res, children);
                }
            } else
                return Factory.Instance.ToAST(n);
        }

        private AST<Node> ReplaceVar(AST<Node> term, string var, AST<Node> expr)
        {
            var ctxt = new RenameVarCtxt();
            ctxt.var = var;
            ctxt.expr = expr;

            var res = term.Compute<AST<Node>>(
                x => RenameVar_UnFold(ctxt, x),
                (x, ch) => RenameVar_Fold(ctxt, x, ch));

            return res;
        }

        private AST<Node> EntryFun_Fold(PToCFoldContext ctxt, Node n, IEnumerable<AST<Node>> children)
        {
            var ret = EntryFun_Fold_Impl(ctxt, n, children);

            if (n != null && ret != null)
                cnodeToPNode[ret.Node] = n;

            return ret;
        }

        private bool shouldErase(Node pNode) { return erase && computedType[pNode].isGhost; }

        private AST<Node> EntryFun_Fold_Impl(PToCFoldContext ctxt, Node n, IEnumerable<AST<Node>> children)
        {
            string ownerName = ctxt.ownerName;
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                if (n.NodeKind == NodeKind.Cnst)
                {
                    var cnst = (Cnst)n;
                    if (cnst.CnstKind == CnstKind.Numeric)
                    {
                        return MkIntLiteral((int)cnst.GetNumericValue().Numerator, n.Span);
                    }
                    else
                    {
                        return Factory.Instance.ToAST(n);
                    }
                }
                else if (n.NodeKind == NodeKind.Id)
                {
                    var id = (Id)n;
                    if (id.Name == PData.Cnst_This.Node.Name)
                    {
                        return CData.Trm_This(n.Span);
                    }
                    else if (id.Name == PData.Cnst_Trigger.Node.Name)
                    {
                        return CData.Trm_Trigger(n.Span);
                    }
                    else if (id.Name == PData.Cnst_Nondet.Node.Name)
                    {
                        if (shouldErase(n))
                            return CData.Cnst_Nil(n.Span);

                        return MkFunApp("NONDET", n.Span);
                    }
                    else if (id.Name == PData.Cnst_Nil.Node.Name)
                    {
                        return CData.Cnst_Nil(n.Span);
                    }
                    else if (id.Name == PData.Cnst_True.Node.Name)
                    {
                        return MkId("TRUE", n.Span);
                    }
                    else if (id.Name == PData.Cnst_False.Node.Name)
                    {
                        return MkId("FALSE", n.Span);
                    }
                    else if (id.Name == PData.Cnst_Leave.Node.Name)
                    {
                        return Factory.Instance.AddArg(CData.App_Return(n.Span), CData.Cnst_Nil(n.Span));
                    }
                    else if (id.Name == PData.Cnst_Delete.Node.Name)
                    {
                        return AddArgs(CData.App_Seq(n.Span), MkFunApp("SmfDelete", n.Span, MkId("Context")),
                            ctxt.emitAllCleanup(AddArgs(CData.App_Return(n.Span), CData.Cnst_Nil(n.Span))));
                    }
                    else if (id.Name == PData.Cnst_Null.Node.Name)
                    {
                        return MkIntLiteral(0);
                    }
                    else
                    {
                        return null;
                    }
                }

                throw new NotImplementedException();
            }

            var ft = (FuncTerm)n;
            var funName = ((Id)ft.Function).Name;
            if (funName == PData.Con_Assert.Node.Name)
            {
                if (shouldErase(n))
                    return CData.Cnst_Nil(n.Span);

                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    return MkAssert(ctxt.consumeExp(it.Current), n.Span);
                }
            }
            else if (funName == PData.Con_Return.Node.Name)
            {
                if (shouldErase(n))
                    return CData.Cnst_Nil(n.Span);

                if (ctxt.isFunction)
                {
                    using (var it = children.GetEnumerator())
                    {
                        it.MoveNext();
                        var formalRetType = allMachines[ownerName].funNameToFunInfo[ctxt.funName].returnType;
                        var actualRetType = getComputedType(cnodeToPNode[it.Current.Node]);

                        if (formalRetType is PNilType)
                        {
                            Debug.Assert(actualRetType is PNilType);
                            return ctxt.emitAllCleanup(AddArgs(CData.App_Return(n.Span), CData.Cnst_Nil(n.Span)));
                        } else if (formalRetType is PPrimitiveType)
                        {   // For primitive types, just return the expected value
                            if (formalRetType != actualRetType)
                            {   // Need an implicit up cast from the actual return value to the formal return type.
                                var tmpVar = ctxt.getTmpVar(formalRetType, false);
                                ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, tmpVar, formalRetType, ctxt.consumeExp(it.Current), actualRetType));
                                return ctxt.emitAllCleanup(AddArgs(CData.App_Return(n.Span), tmpVar));
                            }
                            else
                            {
                                return ctxt.emitAllCleanup(AddArgs(CData.App_Return(n.Span), ctxt.consumeExp(it.Current)));
                            }
                        }
                        else
                        {   // For compound return types, assign them to the "dst" parameter and return void.
                            Debug.Assert(formalRetType is PAnyType || formalRetType is PCompoundType); // Just a reminder to revisit this when adding new types.
                            return MkSeq(MkAssignOrCast(ctxt, n.Span, MkDrf(MkId("dst")), formalRetType, ctxt.consumeExp(it.Current), actualRetType),
                                ctxt.emitAllCleanup(AddArgs(CData.App_Return(n.Span), CData.Cnst_Nil(n.Span))));
                        }
                    }
                }
                else
                {
                    return AddArgs(CData.App_Seq(n.Span), MkFunApp("SmfPop", n.Span, MkId("Context")),
                        ctxt.emitAllCleanup(AddArgs(CData.App_Return(n.Span), CData.Cnst_Nil(n.Span))));
                }
            }
            else if (funName == PData.Con_DataOp.Node.Name)
            {
                if (shouldErase(n))
                    return CData.Cnst_Nil(n.Span);
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var mutatedVar = it.Current;
                    var mutatedVarT = getComputedType(cnodeToPNode[mutatedVar.Node]);
                    if (mutatedVarT is PSeqType)
                    {
                        var innerT = (mutatedVarT as PSeqType).T;
                        it.MoveNext();
                        var ind = it.Current;

                        if (op == PData.Cnst_Remove.Node.Name)
                        {
                            return MkFunApp("SmfArrayListRemove", n.Span, ctxt.driver, MkAddrOf(mutatedVar), ind);
                        }

                        it.MoveNext();
                        var val = it.Current;
                        var valT = getComputedType(cnodeToPNode[val.Node]);

                        var tmpUp = ctxt.getTmpVar(innerT, false);
                        ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, ctxt.consumeExp(tmpUp), innerT, val, valT));
                        if (op == PData.Cnst_Insert.Node.Name)
                        {
                            return MkFunApp("SmfArrayListInsert", n.Span, ctxt.driver, MkAddrOf(mutatedVar), ind, MkCastToULONGPTR(tmpUp, innerT));
                        }
                        else
                        {
                            return MkAssignment(MkIdx(MkArrow(MkAddrOf(mutatedVar), "Values"), ind), MkCastToULONGPTR(tmpUp, innerT));
                        }
                    }
                    else
                    {
                        Debug.Assert(mutatedVarT is PMapType);
                        var mutatedVarKeyT = (mutatedVarT as PMapType).KeyT;
                        var mutatedVarValT = (mutatedVarT as PMapType).ValT;
                        it.MoveNext();
                        var key = it.Current;
                        var keyT = getComputedType(cnodeToPNode[key.Node]);

                        var tmpKey = ctxt.getTmpVar(mutatedVarKeyT, false);
                        ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, tmpKey, mutatedVarKeyT, key, keyT));
                        if (op == PData.Cnst_Remove.Node.Name)
                        {
                            return MkFunApp("SmfHashtableRemove", n.Span, ctxt.driver, MkAddrOf(mutatedVar), MkCastToULONGPTR(ctxt.consumeExp(tmpKey), keyT));
                        }

                        it.MoveNext();
                        var val = it.Current;
                        var valT = getComputedType(cnodeToPNode[val.Node]);

                        var tmpVal = ctxt.getTmpVar(mutatedVarValT, false);
                        ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, tmpVal, mutatedVarValT, val, valT));
                        return MkFunApp("SmfHashtableUpdate", n.Span, ctxt.driver, MkAddrOf(mutatedVar), MkCastToULONGPTR(ctxt.consumeExp(tmpKey), keyT), MkCastToULONGPTR(ctxt.consumeExp(tmpVal), mutatedVarValT));
                    }
                }
            }
            else if (funName == PData.Con_Scall.Node.Name)
            {
                // Allways non-ghost since there are no arguments, and states are always non-erasible.
                var callLabel = ++ctxt.callStatementCounter;
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    AST<Node> result = CData.Cnst_Nil(n.Span);
                    result = AddArgs(CData.App_Seq(n.Span), result, AddArgs(CData.App_BinApp(n.Span), CData.Cnst_Asn(n.Span), AddArgs(CData.App_BinApp(n.Span), CData.Cnst_PFld(n.Span), MkId("Context"), MkId("ReturnTo")), MkIntLiteral(callLabel)));
                    result = AddArgs(CData.App_Seq(n.Span), result, MkFunApp("SmfCall", n.Span, MkId("Context"), ctxt.consumeExp(it.Current)));
                    result = AddArgs(CData.App_Seq(n.Span), result, ctxt.emitAllCleanup(Factory.Instance.AddArg(CData.App_Return(n.Span), CData.Cnst_Nil(n.Span))));
                    result = AddArgs(CData.App_Seq(n.Span), result, AddArgs(CData.App_Lbl(n.Span), Factory.Instance.MkCnst(string.Format("L{0}", callLabel)), CData.Cnst_Nil(n.Span)));
                    return result;
                }
            }
            else if (funName == PData.Con_Seq.Node.Name)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var outTerm = Factory.Instance.AddArg(CData.App_Seq(n.Span), ctxt.emitCSideEffects(it.Current));
                    it.MoveNext();
                    return Factory.Instance.AddArg(outTerm, it.Current);
                }
            }
            else if (funName == PData.Con_Assign.Node.Name)
            {
                if (shouldErase(n))
                    return CData.Cnst_Nil(n.Span);

                var lhsType = getComputedType(GetArgByIndex(ft, 0));
                var rhsType = getComputedType(GetArgByIndex(ft, 1));

                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var lhs = it.Current;
                    it.MoveNext();
                    var rhs = it.Current;

                    return MkAssignOrCast(ctxt, n.Span, lhs, lhsType, rhs, rhsType);
                }
            }
            else if (funName == PData.Con_ITE.Node.Name)
            {
                if (shouldErase(n))
                    return CData.Cnst_Nil(n.Span);

                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var cond = ctxt.consumeExp(it.Current);
                    it.MoveNext();
                    var thenBody = it.Current;
                    it.MoveNext();
                    var elseBody = it.Current;

                    var elseStmt = ctxt.emitCSideEffects(elseBody);
                    var thenStmt = ctxt.emitCSideEffects(thenBody);

                    return AddArgs(CData.App_ITE(n.Span), cond, thenStmt, elseStmt);
                }
            }
            else if (funName == PData.Con_Payload.Node.Name)
            {
                PType argT = getComputedType(ft);

                if (argT is PAnyType)
                {
                    return CData.Trm_Arg(n.Span);
                }
                else
                {   // Downcast
                    var tmpVar = ctxt.getTmpVar(argT, false);
                    ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, tmpVar, argT, CData.Trm_Arg(n.Span), PType.Any));
                    return tmpVar;
                }
            }
            else if (funName == PData.Con_Use.Node.Name)
            {
                var kind = (Id)GetArgByIndex(ft, 1);
                if (kind.Name == PData.Cnst_Var.Node.Name)
                {
                    var varName = GetName(ft, 0);
                    Debug.Assert((this.erase && allMachines[ownerName].localVariableToVarInfo.ContainsKey(varName) && allMachines[ownerName].localVariableToVarInfo[varName].isGhost) ==
                        shouldErase(n));

                    if (shouldErase(n))
                        return CData.Cnst_Nil(n.Span);
                    PType type;
                    AST<Node> varExp;

                    if (ctxt.isFunction && allMachines[ownerName].funNameToFunInfo[ctxt.funName].parameterNameToInfo.ContainsKey(varName))
                    {   // This is a function parameter. (And we are in /doNotErase mode)
                        type = allMachines[ownerName].funNameToFunInfo[ctxt.funName].parameterNameToInfo[varName].type;
                        varExp = type is PPrimitiveType ? MkId(varName) : MkDrf(MkId(varName));
                    }
                    else
                    {
                        // This is a machine local variable.
                        type = allMachines[ownerName].localVariableToVarInfo[varName].type;
                        varExp = MkVar(varName, ownerName, type);
                    }

                    return varExp;
                }
                else if (kind.Name == PData.Cnst_Event.Node.Name)
                {
                    var eventName = GetName(ft, 0);
                    if (eventName == PData.Cnst_Default.Node.Name)
                        return MkId("SmfDefaultEvent", n.Span);
                    return MkId(string.Format("Event_{0}", eventName), n.Span);
                }
                else if (kind.Name == PData.Cnst_State.Node.Name)
                {
                    return MkId(string.Format("State_{0}_{1}", ownerName, GetName(ft, 0)), n.Span);
                }
                else if (kind.Name == PData.Cnst_Field.Node.Name)
                {
                    var field = GetName(ft, 0);
                    return Factory.Instance.MkCnst(field);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if (funName == PData.Con_Call.Node.Name)
            {
                if (shouldErase(n))
                    return CData.Cnst_Nil(n.Span);

                var calleeName = GetName(ft, 0);
                FunInfo funInfo = allMachines[ctxt.ownerName].funNameToFunInfo[calleeName];
                var argList = new List<AST<Node>>();
                AST<Node> tmpRetVar = null;

                if (erase)
                {
                    argList.Add(AddArgs(CData.App_BinApp(n.Span), CData.Cnst_PFld(n.Span), MkId("Context"), MkId("ExtContext")));
                }
                else
                {
                    argList.Add(MkId("Context", n.Span));
                }

                // Functions with compound return values have an implicit return parameter(dst) as their second parameter.
                if (funInfo.returnType is PCompoundType)
                {
                    tmpRetVar = ctxt.getTmpVar(funInfo.returnType, false);
                    argList.Add(MkAddrOf(tmpRetVar));
                }

                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var paramIndex = 0;
                    while (it.MoveNext())
                    {
                        var actual = it.Current;

                        if (actual.Node.NodeKind == NodeKind.Id && ((Id)actual.Node).Name == "NIL")
                            break;

                        var actualType = computedType[cnodeToPNode[actual.Node]].type;
                        var formalType = funInfo.parameterNameToInfo[funInfo.parameterNames[paramIndex]].type;
                        if (actualType == formalType)
                        {
                            if (formalType is PPrimitiveType)
                                // Since primitive types are ULONG_PTR in C, they are implictly pass-by-value
                                argList.Add(MkCast(formalType, ctxt.consumeExp(actual)));
                            else
                            {
                                if (ctxt.isTmpVar(actual.Node))
                                {
                                    // Don't need to clone temporary variables since they are single use (optimization).
                                    argList.Add(MkAddrOf(ctxt.consumeExp(actual)));
                                }
                                else
                                {
                                    // Non-temporary compound variable. Clone it before passing to the function, to ensure pass-by-value semantics
                                    var argTmpVar = ctxt.getTmpVar(formalType, false);
                                    ctxt.addSideEffect(MkClone(ctxt, formalType, n.Span, MkAddrOf(argTmpVar), MkAddrOf(ctxt.consumeExp(actual))));
                                    argList.Add(MkAddrOf(ctxt.consumeExp(argTmpVar)));
                                }
                            }
                        }
                        else
                        {
                            // The formal is a supertype of the actual. Insert the implicit cast. Note that the implicit cast
                            // also takes care of the cloning of the var, and thus preserves pass-by-value semantics.
                            var argTmpVar = ctxt.getTmpVar(formalType, false);
                            ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, argTmpVar, formalType, actual, actualType));
                            if (formalType is PPrimitiveType)
                                argList.Add(ctxt.consumeExp(argTmpVar));
                            else
                                argList.Add(MkAddrOf(ctxt.consumeExp(argTmpVar)));
                        }
                        paramIndex++;
                    }
                }

                if (funInfo.returnType is PPrimitiveType)
                    return MkFunApp(calleeName, n.Span, argList.ToArray());
                else
                {
                    ctxt.addSideEffect(MkFunApp(calleeName, n.Span, argList.ToArray()));
                    return ctxt.consumeExp(tmpRetVar);
                }
            }
            else if (funName == PData.Con_Apply.Node.Name)
            {
                if (shouldErase(n))
                    return CData.Cnst_Nil(n.Span);

                using (var it = children.GetEnumerator())
                {

                    int arity;
                    var cOp = PData.POpToCOp((Id)GetArgByIndex(ft, 0), out arity);
                    var pOp = ((Id)GetArgByIndex(ft, 0)).Name;
                    it.MoveNext();
                    it.MoveNext();
                    
                    var arg1 = ctxt.consumeExp(it.Current);

                    if (arg1 == null)
                        return null;

                    if (arity == 1)
                    {
                        if (pOp == PData.Cnst_Sizeof.Node.Name)
                        {
                            return MkDot(arg1, "Size");
                        }
                        else
                        {
                            return AddArgs(CData.App_UnApp(n.Span), cOp, arg1);
                        }
                    }
                    else if (arity == 2)
                    {
                        it.MoveNext();
                        var arg2 = ctxt.consumeExp(it.Current);

                        if (arg2 == null)
                            return null;

                        var arg1Type = getComputedType(GetArgByIndex((FuncTerm)GetArgByIndex(ft, 1), 0));
                        var arg2Type = getComputedType(GetArgByIndex((FuncTerm)GetArgByIndex((FuncTerm)GetArgByIndex(ft, 1), 1), 0));

                        if (pOp == PData.Cnst_In.Node.Name)
                        {
                            return MkFunApp("SmfHashtableContains", n.Span, ctxt.driver, MkAddrOf(arg2), arg1);
                        }
                        else if (pOp == PData.Cnst_Idx.Node.Name)
                        {
                            var baseType = getComputedType(cnodeToPNode[arg1.Node]);
                            if (baseType is PTupleType)
                            {
                                return MkDot(arg1, getTupleField(getCIntConst((FuncTerm)arg2.Node)));
                            }
                            else if (baseType is PSeqType)
                            {
                                return MkCastFromULONGPTR(MkIdx(MkDot(arg1, "Values"), arg2), (baseType as PSeqType).T);
                            }
                            else
                            {
                                Debug.Assert(baseType is PMapType);
                                return MkCastFromULONGPTR(MkFunApp("SmfHashtableLookup", n.Span, ctxt.driver, MkAddrOf(arg1), arg2), (baseType as PMapType).ValT);
                            }
                        }
                        else if (pOp == PData.Cnst_Fld.Node.Name)
                        {
                            return MkDot(arg1, ((Cnst)arg2.Node).GetStringValue());
                        }
                        else if (cOp.Node.Name == CData.Cnst_Eq().Node.Name || cOp.Node.Name == CData.Cnst_NEq().Node.Name)
                        {
                            var app = MkEq(ctxt.driver, arg1, arg1Type, arg2, arg2Type);
                            return cOp.Node.Name == CData.Cnst_Eq().Node.Name ?
                                app : AddArgs(CData.App_UnApp(n.Span), CData.Cnst_LNot(n.Span), app);
                        }
                        else
                        {
                            if (arg1Type is PIntType)
                            {
                                Debug.Assert(arg2Type is PIntType);
                                arg1 = arg1Type is PPrimitiveType ? MkCast(arg1Type, arg1) : arg1; // TODO: Ternary looks unnecessary here. Fix this
                                arg2 = arg2Type is PPrimitiveType ? MkCast(arg1Type, arg2) : arg2;
                            }

                            return AddArgs(CData.App_BinApp(n.Span), cOp, arg1, arg2);
                        }
                    }
                    throw new NotImplementedException();

                }
            }
            else if (funName == Con_LabeledExpr)
            {
                if (shouldErase(GetArgByIndex(ft, 1)))
                    return CData.Cnst_Nil(n.Span);

                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    if (it.Current == null)
                        return null;

                    return AddArgs(App_LabeledExpr, Factory.Instance.MkCnst(((Cnst)GetArgByIndex(ft, 0)).GetStringValue()),
                        it.Current);
                }
            }
            else if (funName == PData.Con_New.Node.Name)
            {
                if (shouldErase(n))
                    return CData.Cnst_Nil(n.Span);

                var machTypeName = GetOwnerName(ft, 0, 0);
                List<AST<Node>> argList = new List<AST<Node>>();
                argList.Add(AddArgs(CData.App_UnApp(n.Span), CData.Cnst_Addr(n.Span), MkId(DriverDeclName())));
                argList.Add(MkId("Context"));
                argList.Add(MkId(string.Format("MachineType_{0}", machTypeName)));

                if (children.Any(child => child == null))
                    return null;

                IEnumerable<Tuple<string, Node>> inits =
                    children.Select(child => new Tuple<string, Node>(
                        ((Cnst)GetArgByIndex((FuncTerm)child.Node, 0)).GetStringValue(),
                        GetArgByIndex((FuncTerm)child.Node, 1)));

                // Remove ghosts if neccessary
                inits = inits.Where(init =>
                    !(this.erase && allMachines[machTypeName].localVariableToVarInfo.ContainsKey(init.Item1) &&
                    allMachines[machTypeName].localVariableToVarInfo[init.Item1].isGhost));

                foreach (var init in inits)
                {
                    var varName = init.Item1;
                    argList.Add(MkId(string.Format("Var_{0}_{1}", machTypeName, varName)));
                    var actualType = getComputedType(cnodeToPNode[init.Item2]);
                    var varInfo = allMachines[machTypeName].localVariableToVarInfo[varName];
                    var formalType = varInfo.type;
                    AST<Node> arg;
                    Debug.Assert(!(formalType is PNilType)); // Can't declare Null variables

                    // Add A Cast if Neccessary.
                    if (formalType != actualType)
                    {
                        var tmpCastVar = ctxt.getTmpVar(formalType, false);
                        ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, tmpCastVar, formalType, Factory.Instance.ToAST(init.Item2), actualType));
                        arg = tmpCastVar;
                    }
                    else
                        arg = Factory.Instance.ToAST(init.Item2);


                    if (formalType is PPrimitiveType)
                        argList.Add(arg);
                    else
                    {
                        if (ctxt.isTmpVar(init.Item2))
                        {
                            argList.Add(MkAddrOf(ctxt.consumeExp(arg)));
                        }
                        else
                        {
                            var tmpVar = ctxt.getTmpVar(formalType, false);
                            ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, tmpVar, formalType, arg, formalType));
                            argList.Add(MkAddrOf(ctxt.consumeExp(tmpVar)));
                        }
                    }

                }

                Debug.Assert((argList.Count - 3) % 2 == 0);
                var argCount = (argList.Count - 3) / 2;
                argList.Insert(3, MkIntLiteral(argCount, n.Span));

                return MkFunApp("SmfNew", n.Span, argList.ToArray());
            }
            else if (funName == PData.Con_Raise.Node.Name)
            {
                // Raise's arguments are apparently always real, so don't need to check for erasure here.
                var args = new AST<Node>[3];
                args[0] = MkId("Context");
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    args[1] = ctxt.consumeExp(it.Current);
                    it.MoveNext();
                    if (it.Current.Node.NodeKind == NodeKind.Id &&
                        ((Id)it.Current.Node).Name == PData.Cnst_Nil.Node.Name)
                    {
                        args[2] = AddArgs(CData.App_Cast(n.Span), MkNmdType("PSMF_PACKED_VALUE"), MkAddrOf(MkId("g_SmfNullPayload", n.Span)));
                    }
                    else
                    {
                        var argType = getComputedType(GetArgByIndex(ft, 1));
                        var tmpVar = ctxt.getTmpVar(PType.Any, false, false);

                        ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, tmpVar, PType.Any, ctxt.consumeExp(it.Current), argType));
                        args[2] = MkAddrOf(ctxt.consumeExp(tmpVar));
                    }
                    // Emit an assert that the payload can be cast to the event's expected type.
                    var eventPayloadType = MkDot(MkIdx(MkArrow(ctxt.driver, "Events"), args[1]), "Type");
                    ctxt.addSideEffect(MkAssert(MkFunApp(getCCanCastName(PType.Any), default(Span), ctxt.driver, args[2], eventPayloadType),
                        n.Span, "Payload not Cast-able to expected event payload on Send"));
                    return AddArgs(CData.App_Seq(), MkFunApp("SmfRaise", n.Span, args),
                        ctxt.emitAllCleanup(Factory.Instance.AddArg(CData.App_Return(n.Span), CData.Cnst_Nil(n.Span))));
                }
            }
            else if (funName == PData.Con_Send.Node.Name)
            {
                if (shouldErase(n))
                    return CData.Cnst_Nil(n.Span);

                var args = new AST<Node>[4];
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    args[0] = ctxt.consumeExp(it.Current); // Target ID
                    if (it.Current.Node.NodeKind == NodeKind.Id && ((Id)it.Current.Node).Name == CData.Cnst_Nil().Node.Name)
                        return CData.Cnst_Nil(n.Span);
                    it.MoveNext();
                    args[1] = ctxt.consumeExp(it.Current); // Event ID
                    it.MoveNext();
                    if (it.Current.Node.NodeKind == NodeKind.Id &&
                        ((Id)it.Current.Node).Name == PData.Cnst_Nil.Node.Name)
                    {   // No Payload Case
                        args[2] = AddArgs(CData.App_Cast(n.Span), MkNmdType("PSMF_PACKED_VALUE"), MkAddrOf(MkId("g_SmfNullPayload", n.Span)));
                    }
                    else
                    {   // We have a payload - upcast it to Any. We always send Any values. (SMF_PACKED_VALUE in C).
                        var argType = getComputedType(GetArgByIndex(ft, 2));
                        var tmpVar = ctxt.getTmpVar(PType.Any, false, false);
                        ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, tmpVar, PType.Any, ctxt.consumeExp(it.Current), argType));
                        args[2] = MkAddrOf(ctxt.consumeExp(tmpVar));
                    }
                    args[3] = MkId("FALSE");

                    // Emit an assert that the payload can be cast to the event's expected type.
                    var eventPayloadType = MkDot(MkIdx(MkArrow(ctxt.driver, "Events"), args[1]), "Type");
                    ctxt.addSideEffect(MkAssert(MkFunApp(getCCanCastName(PType.Any), default(Span), ctxt.driver, args[2], eventPayloadType), n.Span, "Payload not Cast-able to expected event payload on Send"));

                    return MkFunApp("SmfEnqueueEvent", n.Span, args);
                }
            }
            else if (funName == PData.Con_While.Node.Name)
            {
                if (shouldErase(n))
                    return CData.Cnst_Nil(n.Span);

                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var cond = it.Current;
                    it.MoveNext();
                    var body = it.Current;

                    body = ctxt.emitCSideEffects(body);

                    var loop_start = getUnique("loop_start");
                    var loop_end = getUnique("loop_end");
                    var loop = MkSeq(AddArgs(CData.App_Lbl(n.Span), MkCnst(loop_start),
                        ctxt.emitCSideEffects(MkIf(MkUnop(CData.Cnst_LNot(n.Span), cond),
                            AddArgs(CData.App_Goto(n.Span), MkCnst(loop_end))))),
                            body,
                            AddArgs(CData.App_Goto(n.Span), MkCnst(loop_start)),
                            AddArgs(CData.App_Lbl(n.Span), MkCnst(loop_end), CData.Cnst_Nil(n.Span)));

                    return loop;
                }
            }
            else if (funName == PData.Con_MachType.Node.Name)
            {
                return Factory.Instance.ToAST(n);
            }
            else if (funName == PData.Con_Tuple.Node.Name)
            {
                if (shouldErase(n))
                    return CData.Cnst_Nil(n.Span);

                var type = (PTupleType)getComputedType(ft);
                
                var tmpVar = ctxt.getTmpVar(type, false);
                var fieldDesc = getFieldDesc(type);

                var sideEffects = fieldDesc.Zip(children, (field, val) => MkAssignOrCast(ctxt, n.Span, MkDot(tmpVar, field.Item3), field.Item1,
                    val, getComputedType(cnodeToPNode[val.Node])));

                foreach (var sideEff in sideEffects) ctxt.addSideEffect(sideEff);
                return tmpVar;
            }
            else if (funName == PData.Con_NamedTuple.Node.Name)
            {
                if (shouldErase(n))
                    return CData.Cnst_Nil(n.Span);

                var type = (PNamedTupleType) getComputedType(ft);
                var tmpVar = ctxt.getTmpVar(type, false);
                var fieldDesc = getFieldDesc(type);

                var sortedRawArgs = children.OrderBy(labeledExp => ((Cnst)GetArgByIndex((FuncTerm)labeledExp.Node, 0)).GetStringValue()).Select(
                    labeledExp => Factory.Instance.ToAST(GetArgByIndex((FuncTerm)labeledExp.Node, 1)));

                var sideEffects = fieldDesc.Zip(sortedRawArgs, (field, val) => MkAssignOrCast(ctxt, n.Span, MkDot(tmpVar, field.Item3), field.Item1,
                    val, getComputedType(cnodeToPNode[val.Node])));

                foreach (var sideEff in sideEffects) ctxt.addSideEffect(sideEff);
                return tmpVar;
            }
            else if (funName == PData.Con_TypeTuple.Node.Name ||
                funName == PData.Con_TypeNamedTuple.Node.Name ||
                funName == PData.Con_TypeField.Node.Name)
            {
                return CData.Cnst_Nil(n.Span);
            }
            else
            {
                throw new NotImplementedException("Unknown function term name: " + funName);
            }
        }

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

            /*
            System.IO.StreamWriter sw = new System.IO.StreamWriter("output.4ml");
            prog.Print(sw);
            sw.Flush(); 
            sw.Close();
            */

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

        private AST<Model> Add(AST<Model> m, AST<FuncTerm> ft)
        {
            return Factory.Instance.AddFact(m, Factory.Instance.MkModelFact(null, ft));
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

        #region ZingCompiler
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

            Dictionary<string, List<string>> machineNameToStates = new Dictionary<string, List<string>>();
            var terms = GetBin("StateDecl");
            foreach (var term in terms)
            {
                var stateName = GetName(term.Node, 0);
                var machineName = GetMachineName(term.Node, 1);
                if (!machineNameToStates.ContainsKey(machineName))
                    machineNameToStates[machineName] = new List<string>();
                machineNameToStates[machineName].Add(stateName);
            }
            foreach (string machineName in machineNameToStates.Keys)
            {
                var states = machineNameToStates[machineName];
                List<AST<Node>> stateConsts = new List<AST<Node>>();
                foreach (string stateName in states)
                {
                    stateConsts.Add(Factory.Instance.MkCnst(string.Format("_{0}", stateName)));
                }
                var stateList = ConstructList(ZingData.App_EnumElems, stateConsts);
                elements.Add(AddArgs(ZingData.App_EnumDecl, Factory.Instance.MkCnst(machineName + "_State"), stateList));
            }

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

        private static AST<Node> ConstructList(AST<FuncTerm> constructor, IEnumerable<AST<Node>> elems, AST<Node> def = null) 
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

        private static AST<Node> ConstructList2(AST<FuncTerm> constructor,
            IEnumerable<AST<Node>> elems1,
            IEnumerable<AST<Node>> elems2,
            AST<Node> def = null)
        {
            AST<Node> ret = def == null ? ZingData.Cnst_Nil : def;
            var revElems1 = new List<AST<Node>>(elems1);
            var revElems2 = new List<AST<Node>>(elems2);
            revElems1.Reverse();
            revElems2.Reverse();

            var zipped = revElems1.Zip(revElems2, (e1, e2) => new Tuple<AST<Node>, AST<Node>>(e1, e2));
            return zipped.Aggregate(ret, (aggr, el) => AddArgs(constructor, el.Item1, el.Item2, aggr));
        }

        
        private static AST<Node> ConstructList3(AST<FuncTerm> constructor,
            IEnumerable<AST<Node>> elems1,
            IEnumerable<AST<Node>> elems2,
            IEnumerable<AST<Node>> elems3, 
            AST<Node> def = null)
        {
            AST<Node> ret = def == null ? ZingData.Cnst_Nil : def;
            var revElems1 = new List<AST<Node>>(elems1);
            var revElems2 = new List<AST<Node>>(elems2);
            var revElems3 = new List<AST<Node>>(elems3);
            revElems1.Reverse();
            revElems2.Reverse();
            revElems3.Reverse();

            var zipped = revElems1.Zip(revElems2.Zip(revElems3, (e2, e3) => new Tuple<AST<Node>, AST<Node>>(e2, e3)),
                (e1, e23) => new Tuple<AST<Node>, AST<Node>, AST<Node>>(e1, e23.Item1, e23.Item2));

            return zipped.Aggregate(ret, (aggr, el) => AddArgs(constructor, el.Item1, el.Item2, el.Item3, aggr));
        }

        private static AST<Node> ConstructCList(AST<FuncTerm> constructor, IEnumerable<AST<Node>> elems)
        {
            return ConstructList(constructor, elems, CData.Cnst_Nil());
        }

        
        private static AST<Node> ConstructList(AST<FuncTerm> constructor, params AST<Node>[] elems)
        {
            return ConstructList(constructor, new List<AST<Node>>(elems));
        }

        private AST<Node> ConstructPartialList(AST<FuncTerm> constructor, string placeHolder, params AST<Node>[] elems)
        {
            return ConstructList(constructor, new List<AST<Node>>(elems), Factory.Instance.MkId(placeHolder));
        }

        private AST<Node> ReverseList(AST<Node> list)
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

        private static AST<FuncTerm> AddArgs(AST<FuncTerm> ft, params AST<Node>[] args)
        {
            AST<FuncTerm> ret = ft;
            foreach (var v in args)
            {
                ret = Factory.Instance.AddArg(ret, v);
            }
            return ret;
        }

        private static AST<FuncTerm> AddArgs(AST<FuncTerm> ft, IEnumerable<AST<Node>> args)
        {
            AST<FuncTerm> ret = ft;
            foreach (var v in args)
            {
                ret = Factory.Instance.AddArg(ret, v);
            }
            return ret;
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

        private AST<Node> MkZingDot(AST<Node> lhs, string member)
        {
            return MkZingApply(ZingData.Cnst_Dot, lhs, MkZingIdentifier(member));
        }

        private AST<Node> MkZingEvent(string eventName)
        {
            return MkZingDot("Main", string.Format("{0}_SM_EVENT", eventName));
        }

        private AST<Node> MkZingReturn(AST<Node> rVal)
        {
            return AddArgs(ZingData.App_Return, rVal);
        }

        public static AST<Node> MkZingIf(AST<Node> cond, AST<Node> then)
        {
            return AddArgs(ZingData.App_ITE, cond, then, ZingData.Cnst_Nil);
        }


        private AST<Node> MkZingNeq(AST<Node> e1, AST<Node> e2)
        {
            return MkZingApply(ZingData.Cnst_NEq, e1, e2);
        }

        private AST<Node> MkZingEq(AST<Node> e1, AST<Node> e2)
        {
            return MkZingApply(ZingData.Cnst_Eq, e1, e2);
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
                Debug.Assert(fromT is PIdType || fromT is PEventType);
                return MkZingEq(from, MkZingIdentifier("null"));
            }
        }

        private AST<FuncTerm> MkZingCall(AST<Node> methodExpr, params AST<Node>[] args)
        {
            List<AST<Node>> argList = new List<AST<Node>>();
            argList.Add(methodExpr);
            foreach (var arg in args)
                argList.Add(arg);
            return Factory.Instance.AddArg(ZingData.App_Call, ConstructList(ZingData.App_Args, argList));
        }

        private AST<FuncTerm> MkZingCall(AST<Node> methodExpr, IEnumerable<AST<Node>> args)
        {
            List<AST<Node>> argList = new List<AST<Node>>();
            argList.Add(methodExpr);
            foreach (var arg in args)
                argList.Add(arg);
            return Factory.Instance.AddArg(ZingData.App_Call, ConstructList(ZingData.App_Args, argList));
        }

        private AST<FuncTerm> MkZingCallStmt(AST<Node> callExpr, params AST<Node>[] attrs)
        {
            return AddArgs(ZingData.App_CallStmt, callExpr, ConstructList(ZingData.App_Attrs, attrs));
        }
        
        private AST<FuncTerm> MkZingMethodDecl(string methodName, AST<Node> parameters, AST<Node> returnType, AST<Node> locals, AST<Node> body, params AST<Node>[] attrs)
        {
            return AddArgs(ZingData.App_MethodDecl, Factory.Instance.MkCnst(methodName), parameters, returnType, locals, body, ConstructList(ZingData.App_Attrs, attrs));
        }

        private AST<FuncTerm> MkZingBlock(string blockName, AST<Node> body)
        {
            return AddArgs(ZingData.App_Blocks, AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst(blockName), body), ZingData.Cnst_Nil);
        }

        private AST<FuncTerm> MkZingAssign(AST<Node> lhs, AST<Node> rhs)
        {
            return AddArgs(ZingData.App_Assign, lhs, rhs);
        }

        void AddEventSet(List<AST<FuncTerm>> stmts, IEnumerable<string> eventNames, AST<FuncTerm> set)
        {
            foreach (var eventName in eventNames)
            {
                var addExpr = MkZingApply(ZingData.Cnst_Add, set, MkZingEvent(eventName));
                var addStmt = AddArgs(ZingData.App_Assign, set, addExpr);
                stmts.Add(addStmt);
            }
        }

        void SubtractEventSet(List<AST<FuncTerm>> stmts, IEnumerable<string> eventNames, AST<FuncTerm> actionSet)
        {
            foreach (var eventName in eventNames)
            {
                var subExpr = MkZingApply(ZingData.Cnst_Sub, actionSet, MkZingEvent(eventName));
                var subStmt = AddArgs(ZingData.App_Assign, actionSet, subExpr);
                stmts.Add(subStmt);
            }
        }

        AST<FuncTerm> MkZingVarDecl(string varName, AST<Node> varType, params AST<Node>[] attrs)
        {
            return AddArgs(ZingData.App_VarDecl, Factory.Instance.MkCnst(varName), varType, ConstructList(ZingData.App_Attrs, attrs));
        }

        AST<FuncTerm> MkZingVarDecl(string varName, PType varType, params AST<Node>[] attrs)
        {
            return MkZingVarDecl(varName, pTypeToZingType(varType), attrs);
        }


        AST<Node> MkZingVarDecls(params AST<Node>[] vars)
        {
            return ConstructList(ZingData.App_VarDecls, vars);
        }

        AST<Node> MkZingVarDecls(IEnumerable<AST<Node>> vars)
        {
            return ConstructList(ZingData.App_VarDecls, vars);
        }

        private static AST<FuncTerm> MkZingApply(AST<Id> op, params AST<Node>[] args)
        {
            return AddArgs(ZingData.App_Apply, op, ConstructList(ZingData.App_Args, args));
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
                                       MkZingDot(string.Format("{0}_State", machineName), string.Format("_{0}", GetName(initStateDecl, 0)))));

            var currentDeferredSet = MkZingIdentifier("currentDeferredSet");
            var smEventSetType = Factory.Instance.MkCnst("SM_EVENT_SET");
            var iteStmt = AddArgs(ZingData.App_ITE,   
                                    MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingEvent("delete")),
                                    MkZingSeq(MkZingAssign(currentDeferredSet, AddArgs(ZingData.App_New, smEventSetType, ZingData.Cnst_Nil)),
                                              MkZingCallStmt(MkZingCall(MkZingDot("myHandle", "DequeueEvent"), ZingData.Cnst_False, currentDeferredSet))),
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

        AST<Node> MkZingAction(string machineName, string actionFunName)
        {
            return MkZingDot("ActionFun", string.Format("_{0}_{1}", machineName, actionFunName));
        }

        private AST<Node> GenerateActionHelperMethodDecl(string machineName)
        {
            AST<Node> parameters = ConstructList(ZingData.App_VarDecls,
                                                 MkZingVarDecl("actionFun", Factory.Instance.MkCnst("ActionFun")),
                                                 MkZingVarDecl("currentActionSet", ZingData.Cnst_SmEventSet)
                                                 );

            List<AST<Node>> locals = new List<AST<Node>>();

            locals.Add(MkZingVarDecl("savedDeferredSet", ZingData.Cnst_SmEventSet));
            locals.Add(MkZingVarDecl("savedActionSet", ZingData.Cnst_SmEventSet));
            locals.Add(MkZingVarDecl("savedCurrentEvent", ZingData.Cnst_SmEvent));
            locals.Add(MkZingVarDecl("savedCurrentArg", ZingData.Cnst_SmUnion));
            locals.Add(MkZingVarDecl("cont", MkCnst(getZingContinuationCtxtType(machineName))));

            AST<Node> initStmt = Factory.Instance.AddArg(ZingData.App_Assert, ZingData.Cnst_False);
            foreach (var actionFunName in allMachines[machineName].actionFunNameToActionFun.Keys)
            {
                var actionExpr = MkZingAction(machineName, actionFunName);
                var condExpr = MkZingApply(ZingData.Cnst_Eq, MkZingIdentifier("actionFun"), actionExpr);
                var gotoStmt = Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst("execute_" + actionFunName));
                initStmt = AddArgs(ZingData.App_ITE, condExpr, gotoStmt, initStmt);
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
            var exitStmt = MkZingSeq(MkZingAssignOrCast(MkZingDot("myHandle", "currentEvent"), PType.Event, MkZingIdentifier("null"), PType.Nil),
                                     MkZingAssignOrCast(MkZingDot("myHandle", "currentArg"), PType.Any, MkZingIdentifier("null"), PType.Nil),
                                     Factory.Instance.AddArg(ZingData.App_Return, ZingData.Cnst_False));
            var exitBlock = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("exit"), exitStmt);
            blocks.Add(exitBlock);
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

            string traceString = type == TranslationContext.Action
                                    ? string.Format("\"<ActionLog> Machine {0}-{{0}} reentered Action {1}\"", machineName, entityName)
                                    : string.Format("\"<StateLog> Machine {0}-{{0}} reentered State {1}\"", machineName, entityName);

            var restoreCurrentEvent = MkZingAssign(currentEvent, savedCurrentEvent);
            var restoreCurrentArg = MkZingAssign(currentArg, savedCurrentArg);
            var gotoStmt = AddArgs(ZingData.App_Goto, Factory.Instance.MkCnst("transition_" + entityName));
            string errorTraceString = string.Format("\"<StateLog> Call statement terminated due to unhandled event by machine {0}-{{0}}\"", machineName);
            var errorTraceStmt = MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(errorTraceString), MkZingDot("myHandle", "instance")));

            var body = new List<AST<Node>>();
            body.Add(MkZingAssign(cont, MkZingCall(MkZingDot(MkCnst(getZingContinuationCtxtType(machineName)), "Construct_Default"))));
            body.Add(MkZingCallStmt(MkZingCall(MkZingDot(cont, "PushReturnTo"), MkCnst(0))));
            body.Add(AddArgs(ZingData.App_LabelStmt, MkCnst("reentry_" + name), MkZingAssign(cont,
                MkZingCall(MkZingIdentifier(name), cont))));
            body.Add(MkZingIf(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Leave")),
                MkZingSeq(
                type == TranslationContext.Action ? AddArgs(ZingData.App_Goto, MkCnst("exit")) : type == TranslationContext.Exit ? (AST<Node>) ZingData.Cnst_Nil : AddArgs(ZingData.App_Goto, MkCnst("wait_" + entityName)))));
            body.Add(MkZingIf(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Return")),
                MkZingSeq(
                    MkZingAssignOrCast(currentEvent, PType.Event, MkZingIdentifier("null"), PType.Nil),
                    MkZingAssignOrCast(currentArg, PType.Any, MkZingIdentifier("null"), PType.Nil),
                    type == TranslationContext.Action ? AddArgs(ZingData.App_Return, ZingData.Cnst_True) : AddArgs(ZingData.App_Return, ZingData.Cnst_Nil))));
            body.Add(MkZingIf(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Delete")),
                MkZingSeq(
                    MkZingAssignOrCast(currentEvent, PType.Event, MkZingEvent("delete"), PType.Event),
                    MkZingAssignOrCast(currentArg, PType.Any, MkZingIdentifier("null"), PType.Nil),
                    type == TranslationContext.Action ? AddArgs(ZingData.App_Return, ZingData.Cnst_True) : AddArgs(ZingData.App_Goto, MkCnst("transition_" + entityName)))));
            body.Add(MkZingIf(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Raise")),
                MkZingSeq(
                    MkZingAssign(currentEvent, MkZingDot("cont", "ev")),
                    MkZingAssign(currentArg, MkZingDot("cont", "payload")),
                    MkZingAssign(MkZingDot("cont", "ev"), MkZingIdentifier("null")),
                    MkZingAssign(MkZingDot("cont", "payload"), MkZingIdentifier("null")),
                    type == TranslationContext.Action ? AddArgs(ZingData.App_Return, ZingData.Cnst_False) : AddArgs(ZingData.App_Goto, MkCnst("transition_" + entityName)))));
            body.Add(MkZingIf(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Call")),
                MkZingSeq(
                    MkZingAssign(savedDeferredSet, stackDeferredSet),
                    MkZingAssign(savedActionSet, stackActionSet),
                    MkZingAssign(savedCurrentEvent, currentEvent),
                    MkZingAssign(savedCurrentArg, currentArg),
                    MkZingAssign(stackActionSet, currentActionSet),
                    MkZingAssign(stackDeferredSet, MkZingCall(MkZingDot("Main", "CalculateComplementOfEventSet"), currentActionSet)),
                    MkZingAssignOrCast(currentEvent, PType.Event, MkZingIdentifier("null"), PType.Nil),
                    MkZingAssignOrCast(currentArg, PType.Any, MkZingIdentifier("null"), PType.Nil),
                    MkZingAssign(MkZingIdentifier("localActions"), MkZingCall(MkZingDot("LocalActions", "Construct"), MkZingIdentifier("localActions"))),
                    MkZingCallStmt(MkZingCall(MkZingIdentifier("runHelper"), MkZingDot("cont", "state"))),
                    MkZingAssign(MkZingIdentifier("localActions"), MkZingDot("localActions", "next")),
                    MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(traceString), MkZingDot("myHandle", "instance"))),
                    MkZingSeq(MkZingAssign(stackDeferredSet, savedDeferredSet), MkZingAssign(stackActionSet, savedActionSet)),
                    MkZingAssign(MkZingDot("cont", "state"), MkZingDot(machineName + "_State", "_" + allMachines[machineName].stateNameToStateInfo.Keys.First())),
                    AddArgs(ZingData.App_ITE,
                                    MkZingApply(ZingData.Cnst_Eq, currentEvent, MkZingIdentifier("null")),
                                    MkZingSeq(restoreCurrentEvent, restoreCurrentArg),
                                    MkZingSeq(errorTraceStmt, MkZingAssert(ZingData.Cnst_False))
                                    ),
                    AddArgs(ZingData.App_Goto, MkCnst("reentry_" + name)))));
            body.Add(MkZingIf(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Nondet")),
                MkZingSeq(
                    MkZingAssign(MkZingDot(cont, "nondet"), MkZingCall(MkCnst("choose"), MkCnst("bool"))),
                    AddArgs(ZingData.App_Goto, MkCnst("reentry_" + name)))));
            body.Add(MkZingIf(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "NewM")),
                MkZingSeq(
                    ZingData.Cnst_Yield,
                    AddArgs(ZingData.App_Goto, MkCnst("reentry_" + name)))));
            body.Add(MkZingIf(MkZingEq(MkZingDot("cont", "reason"), MkZingDot("ContinuationReason", "Send")),
                MkZingSeq(
                    ZingData.Cnst_Yield,
                    AddArgs(ZingData.App_Goto, MkCnst("reentry_" + name)))));

            return MkZingSeq(body);
        }

        private AST<Node> GenerateRunHelperMethodDecl(string machineName)
        {
            var machineStateTypeName = machineName + "_State";
            AST<Node> parameters = ConstructList(ZingData.App_VarDecls, 
                                                 MkZingVarDecl("startState", Factory.Instance.MkCnst(machineStateTypeName)));

            List<AST<Node>> locals = new List<AST<Node>>();
                
            locals.Add(MkZingVarDecl("currentDeferredSet", ZingData.Cnst_SmEventSet));
            locals.Add(MkZingVarDecl("currentActionSet", ZingData.Cnst_SmEventSet));
            locals.Add(MkZingVarDecl("savedDeferredSet", ZingData.Cnst_SmEventSet));
            locals.Add(MkZingVarDecl("savedActionSet", ZingData.Cnst_SmEventSet));
            locals.Add(MkZingVarDecl("actionFun", Factory.Instance.MkCnst("ActionFun")));
            locals.Add(MkZingVarDecl("cont", MkCnst(getZingContinuationCtxtType(machineName))));
            locals.Add(MkZingVarDecl("savedCurrentEvent", ZingData.Cnst_SmEvent));
            locals.Add(MkZingVarDecl("savedCurrentArg", ZingData.Cnst_SmUnion));
            locals.Add(MkZingVarDecl("didActionPop", ZingData.Cnst_Bool));
                    
            // Initial block
            AST<Node> initStmt = Factory.Instance.AddArg(ZingData.App_Assert, ZingData.Cnst_False);
            foreach (var stateDecl in allMachines[machineName].stateNameToStateInfo.Values.Select(x => x.stateDecl)) 
            {
                var stateName = GetName(stateDecl, 0);
                var stateExpr = MkZingDot(machineStateTypeName, string.Format("_{0}", stateName));
                var condExpr = MkZingApply(ZingData.Cnst_Eq, MkZingIdentifier("startState"), stateExpr);
                var gotoStmt = Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst("execute_" + stateName));
                initStmt = AddArgs(ZingData.App_ITE, condExpr, gotoStmt, initStmt);
            }
            initStmt = AddArgs(ZingData.App_LabelStmt, Factory.Instance.MkCnst("init"), initStmt);
            
            var currentDeferredSet = MkZingIdentifier("currentDeferredSet");
            var currentActionSet = MkZingIdentifier("currentActionSet");
            var savedDeferredSet = MkZingIdentifier("savedDeferredSet");
            var savedActionSet = MkZingIdentifier("savedActionSet");
            var stackDeferredSet = MkZingIdentifier("stackDeferredSet");
            var stackActionSet = MkZingIdentifier("stackActionSet");

            // State blocks
            List<AST<Node>> blocks = new List<AST<Node>>();
            blocks.Add(initStmt);
            var smEventSetType = Factory.Instance.MkCnst("SM_EVENT_SET");
            foreach (var stateDecl in allMachines[machineName].stateNameToStateInfo.Values.Select(x => x.stateDecl))
            {
                string stateName = GetName(stateDecl, 0);
                AST<Cnst> executeLabel = Factory.Instance.MkCnst("execute_" + stateName);
                AST<Cnst> waitLabel = Factory.Instance.MkCnst("wait_" + stateName);
                AST<Cnst> transitionLabel = Factory.Instance.MkCnst("transition_" + stateName);
                string traceString = string.Format("\"<StateLog> Machine {0}-{{0}} entered State {1}\"", machineName, stateName);
                var executeStmt = MkZingSeq(
                                    MkZingCallStmt(MkZingCall(MkZingIdentifier("trace"), Factory.Instance.MkCnst(traceString), MkZingDot("myHandle", "instance"))),
                                    MkZingCallStmt(MkZingCall(MkZingIdentifier("invokeplugin"), Factory.Instance.MkCnst("\"StateCoveragePlugin.dll\""), Factory.Instance.MkCnst(string.Format("\"{0}\"", machineName)), Factory.Instance.MkCnst(string.Format("\"{0}\"", stateName)))),
                                    MkZingAssign(currentDeferredSet, AddArgs(ZingData.App_New, smEventSetType, ZingData.Cnst_Nil)),
                                    MkZingAssign(currentActionSet, AddArgs(ZingData.App_New, smEventSetType, ZingData.Cnst_Nil)),
                                    MkZingCallStmt(MkZingCall(MkZingIdentifier(string.Format("{0}_CalculateDeferredAndActionSet", stateName)), currentDeferredSet, currentActionSet)),
                                    MkZingInvokeWrapperFun(machineName, stateName, TranslationContext.Entry), 
                                    Factory.Instance.AddArg(ZingData.App_Goto, waitLabel)
                                    );
                executeStmt = AddArgs(ZingData.App_LabelStmt, executeLabel, executeStmt);
                blocks.Add(executeStmt);
                var waitStmt = MkZingSeq(
                    MkZingCallStmt(MkZingCall(MkZingDot("myHandle", "DequeueEvent"), allMachines[machineName].stateNameToStateInfo[stateName].hasDefaultTransition ? ZingData.Cnst_True : ZingData.Cnst_False, currentDeferredSet)),
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
                    AddArgs(ZingData.App_ITE, 
                            MkZingApply(ZingData.Cnst_In, MkZingDot("myHandle", "currentEvent"), MkZingIdentifier("currentActionSet")),
                            MkZingSeq(MkZingAssign(MkZingIdentifier("actionFun"), MkZingCall(MkZingDot("localActions", "Find"), MkZingDot("myHandle", "currentEvent"))),
                                      MkZingAssign(MkZingIdentifier("didActionPop"), MkZingCall(MkZingIdentifier("actionHelper"), MkZingIdentifier("actionFun"), MkZingIdentifier("currentActionSet"))),
                                      AddArgs(ZingData.App_ITE, MkZingIdentifier("didActionPop"), Factory.Instance.AddArg(ZingData.App_Return, ZingData.Cnst_Nil), ZingData.Cnst_Nil),
                                      AddArgs(ZingData.App_ITE, 
                                              MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingIdentifier("null")),
                                              Factory.Instance.AddArg(ZingData.App_Goto, waitLabel),
                                              Factory.Instance.AddArg(ZingData.App_Goto, transitionLabel))), 
                            ZingData.Cnst_Nil);

                AST<Node> callTransitionStmt = ZingData.Cnst_Nil;
                foreach (var eventName in callTransitions.Keys)
                {
                    var targetStateName = callTransitions[eventName].target;
                    var condExpr = MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingEvent(eventName));
                    var save = MkZingSeq(MkZingAssign(savedDeferredSet, stackDeferredSet), MkZingAssign(savedActionSet, stackActionSet));
                    var update = MkZingSeq(MkZingAssign(stackDeferredSet, currentDeferredSet), MkZingAssign(stackActionSet, currentActionSet));
                    var push = MkZingAssign(MkZingIdentifier("localActions"), MkZingCall(MkZingDot("LocalActions", "Construct"), MkZingIdentifier("localActions")));
                    var callStmt = MkZingCallStmt(
                        MkZingCall(MkZingIdentifier("runHelper"),
                                   MkZingDot(machineStateTypeName, string.Format("_{0}", targetStateName))
                                   ));
                    var pop = MkZingAssign(MkZingIdentifier("localActions"), MkZingDot("localActions", "next"));
                    var restore = MkZingSeq(MkZingAssign(stackDeferredSet, savedDeferredSet), MkZingAssign(stackActionSet, savedActionSet));
                    var ite = AddArgs(
                        ZingData.App_ITE,
                        MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingIdentifier("null")),
                        Factory.Instance.AddArg(ZingData.App_Goto, waitLabel),
                        Factory.Instance.AddArg(ZingData.App_Goto, transitionLabel)
                        );
                    callTransitionStmt = AddArgs(ZingData.App_ITE, condExpr, MkZingSeq(save, update, push, callStmt, pop, restore, ite), callTransitionStmt);
                }

                AST<Node> exitFunction = allMachines[machineName].stateNameToStateInfo[stateName].exitFun != null ?
                    MkZingInvokeWrapperFun(machineName, stateName, TranslationContext.Exit) :
                    ZingData.Cnst_Nil;


                AST<Node> ordinaryTransitionStmt = Factory.Instance.AddArg(ZingData.App_Return, ZingData.Cnst_Nil);
                foreach (var eventName in ordinaryTransitions.Keys)
                {
                    var targetStateName = ordinaryTransitions[eventName].target;
                    var condExpr = MkZingApply(ZingData.Cnst_Eq, MkZingDot("myHandle", "currentEvent"), MkZingEvent(eventName));
                    ordinaryTransitionStmt = AddArgs(ZingData.App_ITE, condExpr, Factory.Instance.AddArg(ZingData.App_Goto, Factory.Instance.MkCnst("execute_" + targetStateName)), ordinaryTransitionStmt);
                }
                
                blocks.Add(AddArgs(ZingData.App_LabelStmt, transitionLabel, MkZingSeq(actionStmt, callTransitionStmt, exitFunction, ordinaryTransitionStmt)));
            }
            AST<Node> body = ConstructList(ZingData.App_Blocks, blocks);
            return MkZingMethodDecl("runHelper", parameters, ZingData.Cnst_Void, MkZingVarDecls(locals), body);
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
            private Compiler compiler;

            public ZingEntryFun_FoldContext(string machineName, TranslationContext translationContext, string entityName, Compiler comp)
            {
                this.machineName = machineName;
                this.translationContext = translationContext;
                this.entityName = entityName;
                this.sideEffectsStack = new Stack<List<AST<Node>>>();
                pushSideEffectStack();
                this.locals = new List<Tuple<AST<Node>, string>>();
                this.compiler = comp;
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

                var l = compiler.getUnique(baseL);
                labels[l] = labels.Count + 1;

                return l;
            }

            public AST<Node> emitLabelPrelude()
            {
                var prelude = new List<AST<Node>>();
                var tmpVar = getTmpVar(PType.Int, "retTo");

                prelude.Add(compiler.MkZingAssign(tmpVar, compiler.MkZingCall(MkZingDot("entryCtxt", "PopReturnTo"))));
                prelude.Add(MkZingIf(compiler.MkZingEq(tmpVar, MkCnst(0)),
                    AddArgs(ZingData.App_Goto, MkCnst("start"))));

                foreach(var l in labels.Keys) {
                    prelude.Add(MkZingIf(compiler.MkZingEq(tmpVar, MkCnst(labels[l])),
                    AddArgs(ZingData.App_Goto, MkCnst(l))));
                }

                prelude.Add(AddArgs(ZingData.App_Assert, ZingData.Cnst_False));

                return compiler.MkZingSeq(prelude);
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
                return getTmpVar(compiler.pTypeToZingType(t), baseName);
            }

            public AST<Node> getTmpVar(AST<Node> type, string baseName)
            {
                var tmpVarName = compiler.getUnique(baseName);
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
                    return compiler.MkZingSeq(sideEffects);
                }
                else
                {
                    return stmt;
                }
            }

            public IEnumerable<AST<Node>> emitLocalsList()
            {
                return locals.Select(loc => compiler.MkZingVarDecl(loc.Item2, loc.Item1));
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

        private static string getFtName(FuncTerm n)
        {
            return ((Id)n.Function).Name;
        }

        private static bool isInstanceOf(Node n, AST<FuncTerm> m)
            {
            return isInstanceOf(n, (Id)m.Node.Function);
            }

        private static bool isInstanceOf(Node n, AST<Id> m)
            {
            return isInstanceOf(n, m.Node);
            }

        private static bool isInstanceOf(Node n, Id m)
        {
            return n is FuncTerm && getFtName(n as FuncTerm) == m.Name;
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
            else if (funName == PData.Con_New.Node.Name)
            {
                yield return GetArgByIndex(ft, 0);
                foreach (var a in ZingEntryFun_UnFold(ctxt, GetArgByIndex(ft, 1)))
                {
                    yield return a;
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

        private AST<Node> MkZingAssert(AST<Node> condition, params AST<Node>[] msgs)
        {
            return AddArgs(ZingData.App_Assert, condition);
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
                assert = MkSeq(MkZingAssert(MkZingNeq(MkZingDot("myHandle", "currentArg"), MkZingIdentifier("null"))), assert);
            }

            return assert;
        }

        private AST<Node> MkZingLabeledStmt(string label, AST<Node> stmt)
        {
            return AddArgs(ZingData.App_LabelStmt, MkCnst(label), stmt);
        }

        private AST<Node> zingWrapExprToStmts(AST<Node> exp)
        {
            if (isInstanceOf(exp.Node,  ZingData.App_Call))
            {
                return MkZingCallStmt(exp);
            } else if (isInstanceOf(exp.Node, ZingData.App_Identifier)) {
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
                else if (lhsType is PIdType || lhsType is PEventType)
                {
                    Debug.Assert(rhsType is PNilType);
                    rhsNode = MkZingIdentifier("null");
                }
                else
                    throw new NotImplementedException(string.Format("Unknown LHS Type {0} in assignment.", lhsType));
            } else if (lhsType.isSubtypeOf(rhsType))
            {   // DOWNCAST
                if (!(lhsType is PNilType))
                {
                    rhsNode = MkZingCall(MkZingDot(rhs, getZingDownCastName(lhsType)));
                }
                else
                {
                    rhsNode = MkZingIdentifier("null");
                }

            } else
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
            string machineName = ctxt.machineName;
            string entityName = ctxt.entityName;
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
                        return new ZingTranslationInfo(MkZingIdentifier("myHandle"), new PIdType(), allMachines[machineName].isGhost);
                    }
                    else if (id.Name == PData.Cnst_Trigger.Node.Name)
                    {
                        if (ctxt.translationContext == TranslationContext.Function)
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("TRIGGER disallowed in body of function {0}.", entityName), 0, CompilingProgram));
                            return null;
                        }
                        return new ZingTranslationInfo(MkZingDot("myHandle", "currentEvent"), new PEventType(null));
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
                    else if (id.Name == PData.Cnst_Nondet.Node.Name)
                    {
                        var afterLabel = ctxt.getFreshLabel();
                        var bvar = ctxt.getTmpVar(PType.Bool, "nondet");
                        ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Nondet"), MkCnst(ctxt.labelToId(afterLabel)))));
                        ctxt.addSideEffect(MkZingReturn(MkZingIdentifier("entryCtxt")));
                        ctxt.addSideEffect(MkZingLabeledStmt(afterLabel, MkZingAssign(bvar, MkZingDot("entryCtxt", "nondet"))));
                        ctxt.addSideEffect(MkZingAssign(MkZingDot("entryCtxt", "nondet"), ZingData.Cnst_False));
                        return new ZingTranslationInfo(bvar, new PBoolType());
                    }
                    else if (id.Name == PData.Cnst_Leave.Node.Name)
                    {
                        if (ctxt.translationContext == TranslationContext.Function)
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Leave statement disallowed in body of function {0}.", entityName), 0, CompilingProgram));
                            return null;
                        }
                        var res = MkZingSeq(
                            MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Leave"))),
                            MkZingReturn(MkZingIdentifier("entryCtxt")));
                        return new ZingTranslationInfo(res, new PNilType());
                    }
                    else if (id.Name == PData.Cnst_Delete.Node.Name)
                    {
                        if (ctxt.translationContext != TranslationContext.Entry)
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Delete statement allowed only in entry functions.", entityName), 0, CompilingProgram));
                            return null;
                        }
                        var res = MkZingSeq(
                            MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Delete"))),
                            MkZingReturn(MkZingIdentifier("entryCtxt")));
                        
                        return new ZingTranslationInfo(res, new PNilType());
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
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Assert expression must be of Boolean type."), 0, CompilingProgram));
                        return null;
                    }

                    return new ZingTranslationInfo(AddArgs(ZingData.App_Assert, it.Current.node), new PNilType(), it.Current.isGhost);
                }
            }
            else if (funName == PData.Con_DataOp.Node.Name)
            {
                var op = ((Id)GetArgByIndex(ft, 0)).Name;
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var mutatedVar = it.Current.node;
                    var mutatedVarT = it.Current.type;
                    bool isGhost = it.Current.isGhost;

                    if (op == PData.Cnst_Insert.Node.Name || op == PData.Cnst_Remove.Node.Name || op == PData.Cnst_Update.Node.Name)
                    {
                        // expected case, so do nothing
                    }
                    else 
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Unknown operation {0}.", op), 0, CompilingProgram));
                        return null;
                    }
                    if (mutatedVarT is PSeqType || (mutatedVarT is PMapType && op != PData.Cnst_Insert.Node.Name))
                    {
                        // expected case, so do nothing
                    }
                    else 
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Cannot perform {0} on variable of type {1}.", op, mutatedVarT), 0, CompilingProgram));
                        return null;
                    }

                    PSeqType mutatedVarTAsSeqType = mutatedVarT as PSeqType;
                    PMapType mutatedVarTAsMapType = mutatedVarT as PMapType;

                    it.MoveNext();
                    var ind = it.Current.node;
                    var indT = it.Current.type;

                    if (mutatedVarT is PSeqType)
                    {
                        if (!(indT is PIntType))
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Expected the index to be of type {0}, not {1}.", PType.Int, indT), 0, CompilingProgram));
                            return null;
                        }
                    }
                    else
                    {
                        if (!indT.isSubtypeOf(mutatedVarTAsMapType.KeyT))
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Expected the index to be subtype of type {0}, not {1}.", mutatedVarTAsMapType, indT), 0, CompilingProgram));
                            return null;
                        }
                    }

                    // Cannot index into a real variable with a ghost expr. I believe this gives the equivalent power of doing control flow on ghost conditionals.
                    if (!isGhost && it.Current.isGhost)
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Cannot index a real sequence or map with a ghost expressions."), 0, CompilingProgram));
                        return null;
                    }

                    if (op == PData.Cnst_Remove.Node.Name)
                    {
                        if (it.MoveNext())
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Unnecessary arguments to remove(.)."), 0, CompilingProgram));
                            return null;
                        }

                        if (mutatedVarTAsMapType == null)
                        {
                            return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot(mutatedVar, "Remove"), ind)), new PNilType(), isGhost);
                        }
                        else
                        {
                            var tmpKey = ctxt.getTmpVar(mutatedVarTAsMapType.KeyT, "tmpVal");
                            ctxt.addSideEffect(MkZingAssignOrCast(tmpKey, mutatedVarTAsMapType.KeyT, ind, indT));
                            return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot(mutatedVar, "Remove"), tmpKey)), new PNilType(), isGhost);
                        }
                    }

                    it.MoveNext();
                    var val = it.Current.node;
                    var valT = it.Current.type;

                    if (!isGhost && it.Current.isGhost)
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Cannot insert a ghost expression into a real sequence or map."), 0, CompilingProgram));
                        return null;
                    }

                    if (it.MoveNext())
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Unnecessary arguments to insert(.,.)."), 0, CompilingProgram));
                        return null;
                    }

                    var innerT = mutatedVarTAsMapType == null ? mutatedVarTAsSeqType.T : mutatedVarTAsMapType.ValT;
                    if (!valT.isSubtypeOf(innerT))
                    {
                        if (mutatedVarTAsMapType == null)
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Cannot insert a value of type {0} into a sequence of type {1}.", valT, innerT), 0, CompilingProgram));
                        }
                        else
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Cannot insert a value of type {0} into a map with value type {1}.", valT, innerT), 0, CompilingProgram));
                        }
                        return null;
                    }

                    if (mutatedVarTAsMapType == null)
                    {
                        var tmpVal = ctxt.getTmpVar(innerT, "tmpVal");
                        ctxt.addSideEffect(MkZingAssignOrCast(tmpVal, innerT, val, valT));
                        if (op == PData.Cnst_Insert.Node.Name)
                        {
                            return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot(mutatedVar, "Insert"), ind, tmpVal)), new PNilType(), isGhost);
                        }
                        else
                        {
                            return new ZingTranslationInfo(MkZingAssign(MkZingIndex(MkZingDot(mutatedVar, "arr"), ind), tmpVal), new PNilType(), isGhost);
                        }
                    }
                    else
                    {
                        Debug.Assert(op == PData.Cnst_Update.Node.Name);
                        var tmpKey = ctxt.getTmpVar(mutatedVarTAsMapType.KeyT, "tmpVal");
                        ctxt.addSideEffect(MkZingAssignOrCast(tmpKey, mutatedVarTAsMapType.KeyT, ind, indT));
                        var tmpVal = ctxt.getTmpVar(innerT, "tmpVal");
                        ctxt.addSideEffect(MkZingAssignOrCast(tmpVal, innerT, val, valT));
                        return new ZingTranslationInfo(MkZingCallStmt(MkZingCall(MkZingDot(mutatedVar, "Update"), tmpKey, tmpVal)), new PNilType(), isGhost);
                    }
                }
            }
            else if (funName == PData.Con_Return.Node.Name)
            {
                if (ctxt.translationContext == TranslationContext.Exit)
                {
                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Return statement not allowed in exit functions.", entityName), 0, CompilingProgram));
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
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Return statement should not have an argument.", entityName), 0, CompilingProgram));
                            return null;
                        }
                        else
                        {
                            ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Return"))));
                            return new ZingTranslationInfo(MkZingReturn(MkZingIdentifier("entryCtxt")), new PNilType());
                        }
                    }
                }

                var returnType = allMachines[machineName].funNameToFunInfo[entityName].returnType;
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    if (it.Current == null)
                        return null;

                    if (it.Current.type.isSubtypeOf(returnType))
                    {
                        if (returnType is PNilType)
                        {
                            ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Return"))));
                            return new ZingTranslationInfo(MkZingReturn(MkZingIdentifier("entryCtxt")), new PNilType(), it.Current.isGhost);
                        }
                        else
                        {
                            var tmpRet = ctxt.getTmpVar(PType.Any, "retVar");
                            var res = MkZingSeq(
                                MkZingAssignOrCast(tmpRet, PType.Any, it.Current.node, it.Current.type),
                                MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "ReturnVal"), tmpRet)),
                                MkZingReturn(MkZingIdentifier("entryCtxt")));

                            return new ZingTranslationInfo(res, new PNilType(), it.Current.isGhost);
                        }
                    }
                    else
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Type mismatch in return expression of function {0}. Expected {1}, got {2}", entityName, returnType, it.Current.type), 0, CompilingProgram));
                        return null;
                    }
                }
            }
            else if (funName == PData.Con_Scall.Node.Name)
            {
                if (ctxt.translationContext == TranslationContext.Function)
                {
                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Call statement not allowed in function."), 0, CompilingProgram));
                    return null;
                }

                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    if (it.Current == null || it.Current.type != PType.State)
                        return null;

                    var stateName = it.Current.stateName;

                    if (allMachines[machineName].stateNameToStateInfo[stateName].nIncommingTransitions > 0)
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Have both transitions and calls into state {0}", stateName), 0, CompilingProgram));
                        return null;
                    }

                    var afterLabel = ctxt.getFreshLabel();
                    var res = MkZingSeq(
                        MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Call"), it.Current.node, MkCnst(ctxt.labelToId(afterLabel)))),
                        MkZingReturn(MkZingIdentifier("entryCtxt")),
                        MkZingLabeledStmt(afterLabel, ZingData.Cnst_Nil));
                    return new ZingTranslationInfo(res, new PNilType());
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
                        // if lhs is real or lhs is Id, isGhost(rhs) == isGhost(lhs)
                        if (!rhs.type.isSubtypeOf(lhs.type))
                        {
                            errors.Add(new Flag(SeverityKind.Error, n,
                                string.Format("Type of left-hand side ({0}) does not match type of right-hand side ({1}) in assignment.", lhs.type, rhs.type), 0, CompilingProgram));
                            return null;
                        }
                        if (!lhs.isGhost && rhs.isGhost)
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Cannot assign ghost expression to real variable."), 0, CompilingProgram));
                            return null;
                        }
                        if (lhs.type == PType.Id && lhs.isGhost != rhs.isGhost)
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Cannot assign real expression to a ghost variable of type id."), 0, CompilingProgram));
                            return null;
                        }

                        if (rhs.isKeys)
                        {
                            return new ZingTranslationInfo(MkZingAssignOrCast(lhs.node, lhs.type, MkZingCall(MkZingDot(rhs.node, "ToSeq")), rhs.type), new PNilType(), lhs.isGhost);
                        }
                        else if(rhs.isNew)
                        {
                            var zingSeq = MkZingSeq(
                                MkZingAssignOrCast(lhs.node, lhs.type, rhs.node, rhs.type),
                                MkZingReturn(MkZingIdentifier("entryCtxt")),
                                MkZingLabeledStmt(rhs.newAfterLabel, ZingData.Cnst_Nil)
                                );
                            return new ZingTranslationInfo(zingSeq, new PNilType(), lhs.isGhost);
                        }
                        else
                        {
                            return new ZingTranslationInfo(MkZingAssignOrCast(lhs.node, lhs.type, rhs.node, rhs.type), new PNilType(), lhs.isGhost);
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
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Conditional expression must be Boolean."), 0, CompilingProgram));
                        return null;
                    }
                    var inGhostContext =
                        (ctxt.translationContext == TranslationContext.Function) ?
                        allMachines[machineName].funNameToFunInfo[entityName].isForeign :
                        allMachines[machineName].isGhost;
                    if (!inGhostContext)
                    {
                        if (condExpr.node == null)
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Use of nondet in a real machine is not allowed."), 0, CompilingProgram));
                            return null;
                        }
                        if (condExpr.isGhost && !allMachines[machineName].isGhost)
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Use of ghost expression in a conditional in a real machine is not allowed."), 0, CompilingProgram));
                            return null;
                        }
                    }

                    // Order in which we emit side effets (else,then) is the reverse of the order in which the side effect stacks were pushed(then, else).
                    var ifName = getUnique(entityName + "_if");
                    var elseLabel = ifName + "_else";
                    var afterLabel = ifName + "_end";
                    var cookedElse = MkZingLabeledStmt(elseLabel, ctxt.emitZingSideEffects(zingWrapExprToStmts(elseStmt.node)));
                    var cookedThen = ctxt.emitZingSideEffects(zingWrapExprToStmts(thenStmt.node));
                    var res = MkZingSeq(MkZingIf(MkZingApply(ZingData.Cnst_Not, condExpr.node), AddArgs(ZingData.App_Goto, MkCnst(elseLabel))),
                        cookedThen,
                        AddArgs(ZingData.App_Goto, MkCnst(afterLabel)),
                        cookedElse,
                        MkZingLabeledStmt(afterLabel, ZingData.Cnst_Nil));
                        
                    return new ZingTranslationInfo(res, new PNilType(), condExpr.isGhost);
                }
            }
            else if (funName == PData.Con_Payload.Node.Name)
            {
                if (ctxt.translationContext == TranslationContext.Function)
                {
                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("ARG disallowed in body of function {0}.", entityName), 0, CompilingProgram));
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
                        possiblePayloads = allMachines[machineName].actionFunNameToActionFun[ctxt.entityName].argTypes;
                    }
                    else if (ctxt.translationContext == TranslationContext.Entry)
                    {
                        possiblePayloads = allMachines[machineName].stateNameToStateInfo[ctxt.entityName].argTypes;
                    }
                    else
                    {
                        Debug.Assert(ctxt.translationContext == TranslationContext.Exit);
                        possiblePayloads = Enumerable.Repeat<PType>(PType.Any, 1);
                    }

                    PType castType = (typeArg is Id && ((Id)typeArg).Name == PData.Cnst_Nil.Node.Name) ?
                        PType.computeLUB(possiblePayloads) : GetPType(typeArg);

                    // Statically check that our cast can be a relative of at least one of the possible payloads.
                    if (!possiblePayloads.Any(t => t.realtive(castType)))
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Cannot cast to {0} from any of the possible payload types in {1}: {2}", castType, ctxt.prettyName(),
                            possiblePayloads.Aggregate("", (str, t) => str + ", " + t)), 0, CompilingProgram));
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
                    if (allMachines[machineName].localVariableToVarInfo.ContainsKey(varName))
                    {
                        varInfo = allMachines[machineName].localVariableToVarInfo[varName];
                    }
                    else if (ctxt.translationContext == TranslationContext.Function && allMachines[machineName].funNameToFunInfo[entityName].parameterNameToInfo.ContainsKey(varName))
                    {
                        varInfo = allMachines[machineName].funNameToFunInfo[entityName].parameterNameToInfo[varName];
                    }
                    else
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Use of undeclared local variable {0}.", varName), 0, CompilingProgram));
                        return null;
                    }
                    return new ZingTranslationInfo(MkZingIdentifier(varName), varInfo.type, varInfo.isGhost);
                }
                else if (kind.Name == PData.Cnst_Event.Node.Name)
                {
                    var eventName = GetName(ft, 0);
                    // The next two lines are a temporary hack until the DSLParser is retired and all old examples have been ported
                    if (eventName == PData.Cnst_Default.Node.Name)
                        eventName = DefaultEvent;
                    if (!allEvents.ContainsKey(eventName))
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Use of undeclared event {0}.", eventName), 0, CompilingProgram));
                        return null;
                    }
                    return new ZingTranslationInfo(MkZingEvent(eventName), new PEventType(eventName), false);
                }
                else if (kind.Name == PData.Cnst_State.Node.Name)
                {
                    var stateName = GetName(ft, 0);
                    if (!allMachines[machineName].stateNameToStateInfo.ContainsKey(stateName))
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Use of undeclared state {0}.", stateName), 0, CompilingProgram));
                        return null;
                    }
                    return new ZingTranslationInfo(MkZingDot(string.Format("{0}_State", machineName), string.Format("_{0}", stateName)), new PStateType(), false, stateName);
                }
                else if (kind.Name == PData.Cnst_Field.Node.Name)
                {
                    var field = GetName(ft, 0);
                    // Fields by themselves really don't have a type
                    return new ZingTranslationInfo(Factory.Instance.MkCnst(field), new PNilType(), false);
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
                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Unexpected tuple in MainDecl statement!"), 0, CompilingProgram));
                    return null;
                }

                if (children.Any(child => child == null))
                    return null;

                var tupType = new PTupleType(children.Select(child => child.type));
                registerType(tupType);

                var node = MkZingCall(MkZingDot(pTypeToZingType(tupType), "Build"), children.Select(child => child.node));
                var tmpVar = ctxt.getTmpVar(tupType, "tmpTuple");
                ctxt.addSideEffect(MkZingAssign(tmpVar, node));

                return new ZingTranslationInfo(tmpVar, tupType, children.Any(child => child.isGhost));
            }
            else if (funName == PData.Con_NamedTuple.Node.Name)
            {
                if (ctxt.machineName == null) // We are in the main context. Shouldn't have tuples here!
                {
                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Unexpected named tuple in MainDecl statement!"), 0, CompilingProgram));
                    return null;
                }

                if (children.Any(child => child == null))
                    return null;

                var type = new PNamedTupleType(children.Select(child => new Tuple<string, PType>(
                    ((Cnst)GetArgByIndex((FuncTerm)child.node.Node, 0)).GetStringValue(), child.type)));
                var isGhost = children.Any(child => child.isGhost);
                registerType(type);

                var buildArgs = ConstructList(ZingData.App_Args,
                    children.OrderBy(child => ((Cnst)GetArgByIndex((FuncTerm)child.node.Node, 0)).GetStringValue()).Select(
                        child => Factory.Instance.ToAST(GetArgByIndex((FuncTerm)child.node.Node, 1))
                    ));

                var node = AddArgs(ZingData.App_Call, AddArgs(ZingData.App_Args, MkZingDot(pTypeToZingType(type), "Build"), buildArgs));
                var tmpVar = ctxt.getTmpVar(type, "tmpNamedTuple");
                ctxt.addSideEffect(MkZingAssign(tmpVar, node));
                return new ZingTranslationInfo(tmpVar, type, isGhost);
            }
            else if (funName == PData.Con_Apply.Node.Name)
            {
                var pOp = (Id)GetArgByIndex(ft, 0);
                int arity;
                var zingOp = PData.POpToZingOp(pOp, out arity);
                if ((arity == 1) != (pOp.Name == PData.Cnst_Neg.Node.Name || pOp.Name == PData.Cnst_Not.Node.Name || pOp.Name == PData.Cnst_Sizeof.Node.Name))
                {
                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Mismatched arity in expression."), 0, CompilingProgram));
                    return null;
                }
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var arg1 = it.Current;
                    if (arg1 == null)
                        return null;
                    var isGhost = arg1.isGhost;
                    if (arity == 1)
                    {
                        if (pOp.Name == PData.Cnst_Not.Node.Name && arg1.type == PType.Bool)
                        {   // TODO: In some places we return, in others we use outTerm. I think this is legacy could. Should refactor to always return
                            return new ZingTranslationInfo(MkZingApply(zingOp, arg1.node), new PBoolType(), isGhost);
                        }
                        else if (pOp.Name == PData.Cnst_Neg.Node.Name && arg1.type == PType.Int)
                        {
                            return new ZingTranslationInfo(MkZingApply(zingOp, arg1.node), new PIntType(), isGhost);
                        }
                        else if (pOp.Name == PData.Cnst_Sizeof.Node.Name)
                        {
                            if (!(arg1.type is PSeqType || arg1.type is PMapType))
                            {
                                errors.Add(new Flag(SeverityKind.Error, n, string.Format("sizeof() expects a sequence or map."), 0, CompilingProgram));
                                return null;
                            }
                            return new ZingTranslationInfo(MkZingDot(arg1.node, "size"), new PIntType(), isGhost);
                        }
                        else if (pOp.Name == PData.Cnst_Keys.Node.Name)
                        {
                            if (!(arg1.type is PMapType))
                            {
                                errors.Add(new Flag(SeverityKind.Error, n, string.Format("keys() expects a map."), 0, CompilingProgram));
                                return null;
                            }
                            return new ZingTranslationInfo(arg1.node, new PSeqType((arg1.type as PMapType).KeyT), isGhost, null, true);
                        }
                        else
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Mismatched types in expression."), 0, CompilingProgram));
                            return null;
                        }
                    }
                    else if (arity == 2)
                    {
                        it.MoveNext();
                        var arg2 = it.Current;
                        if (arg2 == null)
                            return null;
                        isGhost = isGhost || arg2.isGhost;
                        if (pOp.Name == PData.Cnst_In.Node.Name)
                        {
                            PMapType mapType = arg2.type as PMapType;
                            if (mapType == null)
                            {
                                errors.Add(new Flag(SeverityKind.Error, n, string.Format("Expected a map with the in operator"), 0, CompilingProgram));
                                return null;
                            }
                            PType keyType = arg1.type;
                            if (!(keyType.isSubtypeOf(mapType.KeyT))) 
                            {
                                errors.Add(new Flag(SeverityKind.Error, n, string.Format("Expected a subtype of {0} for the key when applying in operator to a map of type {1}", keyType, mapType), 0, CompilingProgram));
                                return null;
                            }
                            var tmpVar = ctxt.getTmpVar(PType.Bool, "tmpVal");
                            ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(arg2.node, "Contains"), arg1.node)));
                            return new ZingTranslationInfo(tmpVar, PType.Bool, arg1.isGhost || arg2.isGhost);
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
                                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Expected an int when indexing a tuple"), 0, CompilingProgram));
                                    return null;
                                }

                                if (!(indN is Cnst))
                                {
                                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Can only index a tuple with a constant"), 0, CompilingProgram));
                                    return null;
                                }

                                var elInd = (int)((Cnst)indN).GetNumericValue().Numerator;

                                if (elInd < 0 || elInd > baseType.elements.Count())
                                {
                                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Invalid index {0} into tuple of length {1}", elInd, baseType.elements.Count()), 0, CompilingProgram));
                                    return null;
                                }

                                return new ZingTranslationInfo(MkZingDot(arg1.node, getTupleField(elInd)), baseType.elements.ElementAt(elInd), arg1.isGhost);
                            }
                            else if (arg1.type is PSeqType)
                            {
                                var baseType = arg1.type as PSeqType;

                                if (indType != PType.Int)
                                {
                                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Expected an int when indexing a sequence"), 0, CompilingProgram));
                                    return null;
                                }
                                if (arg1.isKeys)
                                {
                                    var tmpVar = ctxt.getTmpVar(baseType.T, "tmpVal");
                                    ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(arg1.node, "LookupKeyAtIndex"), arg2.node)));
                                    return new ZingTranslationInfo(tmpVar, baseType.T, arg1.isGhost || arg2.isGhost);
                                }
                                else
                                {
                                    return new ZingTranslationInfo(MkZingIndex(MkZingDot(arg1.node, "arr"), arg2.node), baseType.T, arg1.isGhost || arg2.isGhost);
                                }
                            }
                            else if (arg1.type is PMapType)
                            {
                                var baseType = arg1.type as PMapType;

                                if (!indType.isSubtypeOf(baseType.KeyT))
                                {
                                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Expected subtype of type {0} when indexing a map of type {1}", baseType.KeyT, baseType), 0, CompilingProgram));
                                    return null;
                                }
                                var tmpVar = ctxt.getTmpVar(baseType.ValT, "tmpVal");
                                ctxt.addSideEffect(MkZingAssign(tmpVar, MkZingCall(MkZingDot(arg1.node, "Lookup"), arg2.node)));
                                return new ZingTranslationInfo(tmpVar, baseType.KeyT, arg1.isGhost || arg2.isGhost);
                            }
                            else
                            {
                                errors.Add(new Flag(SeverityKind.Error, n, string.Format("Unexpected expression of type {0} on the left side of indexing", arg1.type), 0, CompilingProgram));
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
                                errors.Add(new Flag(SeverityKind.Error, n, string.Format("Unexpected expression of type {0} on the left side of field lookup", baseType), 0, CompilingProgram));
                                return null;
                            }

                            // Verify that the second P Node is indeed a Use("f", FIELD) node
                            var useNode = GetArgByIndex((FuncTerm)GetArgByIndex((FuncTerm)GetArgByIndex(ft, 1), 1), 0) as FuncTerm;
                            if (useNode == null || ((Id)useNode.Function).Name != PData.Con_Use.Node.Name ||
                                !(GetArgByIndex(useNode, 1) is Id) || ((Id)GetArgByIndex(useNode, 1)).Name != PData.Cnst_Field.Node.Name)
                            {
                                errors.Add(new Flag(SeverityKind.Error, n, string.Format("Invalid Field expression in field lookup", baseType), 0, CompilingProgram));
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
                                errors.Add(new Flag(SeverityKind.Error, n, string.Format("Named Tuple {0} doesn't conatin a field {1}.", tupType, memberName), 0, CompilingProgram));
                                return null;
                            }

                            return new ZingTranslationInfo(MkZingDot(arg1.node, memberName), memberType, arg1.isGhost);
                        } if (pOp.Name == PData.Cnst_Eq.Node.Name || pOp.Name == PData.Cnst_NEq.Node.Name)
                        {

                            if (!arg1.type.isSubtypeOf(arg2.type) && !arg2.type.isSubtypeOf(arg1.type))
                            {
                                errors.Add(new Flag(SeverityKind.Error, n, string.Format("Cannot compare for equality items of types {0} and {1}", arg1.type, arg2.type), 0, CompilingProgram));
                                return null;
                            }

                            var outNode = pOp.Name == PData.Cnst_Eq.Node.Name ?
                                MkZingEq(ctxt, arg1.node, arg1.type, arg2.node, arg2.type) :
                                MkZingNeq(ctxt, arg1.node, arg1.type, arg2.node, arg2.type);
                            return new ZingTranslationInfo(outNode, new PBoolType(), isGhost);
                        }
                        else if (arg1.type == arg2.type)
                        {
                            if (pOp.Name == PData.Cnst_And.Node.Name || pOp.Name == PData.Cnst_Or.Node.Name)
                            {
                                if (arg1.type == PType.Bool)
                                {
                                    return new ZingTranslationInfo(MkZingApply(zingOp, arg1.node, arg2.node), new PBoolType(), isGhost);
                                }
                                else
                                {
                                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Mismatched types in expression."), 0, CompilingProgram));
                                    return null;
                                }
                            }
                            else if (arg1.type == PType.Int)
                            {
                                if (pOp.Name == PData.Cnst_Add.Node.Name || pOp.Name == PData.Cnst_Sub.Node.Name || pOp.Name == PData.Cnst_Mul.Node.Name || pOp.Name == PData.Cnst_IntDiv.Node.Name)
                                {
                                    return new ZingTranslationInfo(MkZingApply(zingOp, arg1.node, arg2.node), new PIntType(), isGhost);
                                }
                                else
                                {
                                    return new ZingTranslationInfo(MkZingApply(zingOp, arg1.node, arg2.node), new PBoolType(), isGhost);
                                }
                            }
                            else
                            {
                                errors.Add(new Flag(SeverityKind.Error, n, string.Format("Mismatched types in expression."), 0, CompilingProgram));
                                return null;
                            }
                        }
                        else
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Mismatched types in expression. Arguments have different types - {0} and {1}", arg1.type, arg2.type), 0, CompilingProgram));
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

                    return new ZingTranslationInfo(AddArgs(App_LabeledExpr, Factory.Instance.ToAST(GetArgByIndex(ft, 0)), it.Current.node), it.Current.type, it.Current.isGhost, it.Current.stateName, it.Current.isKeys);
                }
            }
            else if (funName == PData.Con_New.Node.Name)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();

                    var typeName = ((Cnst)GetArgByIndex((FuncTerm)it.Current.node.Node, 0)).GetStringValue();
                    if (!allMachines.ContainsKey(typeName))
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Machine type {0} has not been declared.", typeName), 0, CompilingProgram));
                        return null;
                    }

                    AST<Node> inits = ZingData.Cnst_Nil;

                    while (it.MoveNext())
                    {
                        if (it.Current == null)
                            return null;

                        if (it.Current.node.Node is Id)
                            break;

                        var argFt = (FuncTerm)it.Current.node.Node;

                        Debug.Assert(getFtName(argFt) == Con_LabeledExpr &&
                            GetArgByIndex(argFt, 0) is Cnst);
                        var varName = ((Cnst)GetArgByIndex(argFt, 0)).GetStringValue();
                        var rhs = GetArgByIndex(argFt, 1);
                        var rhsInfo = it.Current;

                        VariableInfo varInfo;
                        if (allMachines[typeName].localVariableToVarInfo.ContainsKey(varName))
                        {
                            varInfo = allMachines[typeName].localVariableToVarInfo[varName];
                        }
                        else
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Local variable {0} not declared in machine {1}.", varName, typeName), 0, CompilingProgram));
                            return null;
                        }

                        var lhs = new ZingTranslationInfo(MkZingIdentifier(varName), varInfo.type, varInfo.isGhost);

                        // type(lhs) == type(rhs)
                        // if lhs is real or lhs is Id, isGhost(rhs) == isGhost(lhs)
                        if (!rhsInfo.type.isSubtypeOf(lhs.type))
                        {
                            errors.Add(new Flag(SeverityKind.Error, n,
                                string.Format("Type of left-hand side ({0}) does not match type of right-hand ({1}) side in assignment.", lhs.type, rhsInfo.type), 0, CompilingProgram));
                            return null;
                        }
                        if (!lhs.isGhost && rhsInfo.isGhost)
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Cannot assign ghost expression to real variable."), 0, CompilingProgram));
                            return null;
                        }
                        if (lhs.type == PType.Id && lhs.isGhost != rhsInfo.isGhost)
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Cannot assign real expression to a ghost variable of type id."), 0, CompilingProgram));
                            return null;
                        }

                        if (lhs.type == rhsInfo.type)
                            inits = AddArgs(ZingData.App_Args, lhs.node, AddArgs(ZingData.App_Args, Factory.Instance.ToAST(rhs), inits));
                        else
                        {
                            var tmpCastVar = ctxt.getTmpVar(lhs.type, "tmpCastVar");
                            ctxt.addSideEffect(MkZingAssignOrCast(tmpCastVar, lhs.type, Factory.Instance.ToAST(rhs), rhsInfo.type));
                            inits = AddArgs(ZingData.App_Args, lhs.node, AddArgs(ZingData.App_Args, tmpCastVar, inits));
                        }
                    }
                    string afterLabel = null;
                    if (ctxt.entityName != null) // indicates its not the Main/God machine
                    {
                        afterLabel = ctxt.getFreshLabel();
                        ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "NewM"), MkCnst(ctxt.labelToId(afterLabel)))));
                    }
                    return new ZingTranslationInfo(MkZingCreateMachineCall(ctxt, typeName, inits.Node), new PIdType(), allMachines[typeName].isGhost, true, afterLabel);
                    
                }
            }
            else if (funName == PData.Con_Raise.Node.Name)
            {
                if (ctxt.translationContext != TranslationContext.Entry && ctxt.translationContext != TranslationContext.Action)
                {
                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Raise statement allowed only in entry functions or actions.", entityName), 0, CompilingProgram));
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
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("The argument to raise must be an event."), 0, CompilingProgram));
                        return null;
                    }

                    it.MoveNext();

                    var payloadExpr = it.Current;
                    if (payloadExpr == null)
                    {
                        return null;
                    }
                    if (payloadExpr.isGhost)
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("The payload of a raise must be real."), 0, CompilingProgram));
                        return null;
                    }
                    AST<Node> payload = payloadExpr.node != ZingData.Cnst_Nil ? payloadExpr.node : MkZingIdentifier("null");
                    PType payloadType = payloadExpr.type;


                    var assertStmt = MkZingSeq(Factory.Instance.AddArg(ZingData.App_Assert, MkZingApply(ZingData.Cnst_NEq, eventExpr.node, MkZingIdentifier("null"))),
                                               Factory.Instance.AddArg(ZingData.App_Assert, MkZingApply(ZingData.Cnst_NEq, eventExpr.node, MkZingEvent("default"))));
                    string traceString = string.Format("\"<RaiseLog> Machine {0}-{{0}} raised Event {{1}}\"", machineName);
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
                    var createRetCtxt = MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Raise"), tmpEv, tmpArg));

                    var outNode = MkZingSeq(assertStmt, traceStmt, eventPayloadCasts, payloadEventMatchAssert, createRetCtxt, 
                        MkZingReturn(MkZingIdentifier("entryCtxt")));
                    return new ZingTranslationInfo(outNode, new PNilType());
                }
            }
            else if (funName == PData.Con_Call.Node.Name)
            {
                var calleeName = GetName(ft, 0);
                if (!allMachines[machineName].funNameToFunInfo.ContainsKey(calleeName))
                {
                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Function {0} has not been declared.", calleeName), 0, CompilingProgram));
                    return null;
                }
                var calleeInfo = allMachines[machineName].funNameToFunInfo[calleeName];
                if (ctxt.translationContext == TranslationContext.Action)
                {
                    allMachines[machineName].actionFunNameToActionFun[ctxt.entityName].callees.Add(calleeName);
                }
                else if (ctxt.translationContext == TranslationContext.Function)
                {
                    allMachines[machineName].funNameToFunInfo[calleeName].callers.Add(ctxt.entityName);
                }
                else if (ctxt.translationContext == TranslationContext.Entry)
                {
                    allMachines[machineName].stateNameToStateInfo[ctxt.entityName].entryFunCallees.Add(calleeName);
                }
                else
                {
                    allMachines[machineName].stateNameToStateInfo[ctxt.entityName].exitFunCallees.Add(calleeName);
                }
                var isGhost = false;
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
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Fewer parameters than arguments."), 0, CompilingProgram));
                        return null;
                    }
                    var lhsInfo = calleeInfo.parameterNameToInfo[calleeInfo.parameterNames[count]];
                    var rhsInfo = child;
                    if (!rhsInfo.type.isSubtypeOf(lhsInfo.type))
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Mismatch in type of argument {0}.", count), 0, CompilingProgram));
                        return null;
                    }
                    if (!lhsInfo.isGhost && rhsInfo.isGhost)
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Cannot pass ghost expression to real parameter."), 0, CompilingProgram));
                        return null;
                    }
                    if (lhsInfo.type == PType.Id && lhsInfo.isGhost != rhsInfo.isGhost)
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Cannot assign real expression to a ghost parameter of type id."), 0, CompilingProgram));
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
                    isGhost = isGhost || child.isGhost;
                    args.Add(argActual);
                }
                if (count < calleeInfo.parameterNames.Count)
                {
                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Fewer arguments than parameters."), 0, CompilingProgram));
                    return null;
                }


                AST<Node> callExpr = MkZingCall(MkZingIdentifier(calleeName), args);
                AST<Node> processOutput;
                AST<Node> outExp;

                var beforeLabel = ctxt.getFreshLabel();
                if (calleeInfo.returnType is PNilType) {
                    processOutput = ZingData.Cnst_Nil;
                    outExp = ZingData.Cnst_Nil;
                } else {
                    var retVar = ctxt.getTmpVar(calleeInfo.returnType, "ret");
                    processOutput = MkZingAssignOrCast(retVar, calleeInfo.returnType, MkZingDot("entryCtxt", "payload"), PType.Any);
                    outExp = retVar;
                }


                AST<Node> callStmt = MkZingSeq(
                    MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "PushReturnTo"), MkCnst(0))),
                    MkZingLabeledStmt(beforeLabel, ctxt.emitZingSideEffects(MkZingAssign(MkZingIdentifier("entryCtxt"), callExpr))),
                    AddArgs(ZingData.App_ITE, MkZingEq(MkZingDot("entryCtxt", "reason"), MkZingDot("ContinuationReason", "ReturnVal")),
                        processOutput,
                        MkZingIf(MkZingNeq(MkZingDot("entryCtxt", "reason"), MkZingDot("ContinuationReason", "Return")),
                            MkZingSeq(
                                MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "PushReturnTo"), MkCnst(ctxt.labelToId(beforeLabel)))),
                                MkZingReturn(MkZingIdentifier("entryCtxt"))))));
                ctxt.addSideEffect(callStmt);
                return new ZingTranslationInfo(outExp, calleeInfo.returnType, isGhost);
            }
            else if (funName == PData.Con_Send.Node.Name)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    if (it.Current == null)
                    {
                        return null;
                    }
                    if (it.Current.type != PType.Id)
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("The target of a send must be of machine type."), 0, CompilingProgram));
                        return null;
                    }
                    var targetIsGhost = it.Current.isGhost;
                    var targetExpr = it.Current.node;
                    it.MoveNext();
                    if (it.Current == null)
                    {
                        return null;
                    }
                    if (it.Current.type != PType.Event)
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("The argument to send must be an event."), 0, CompilingProgram));
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

                    var argIsGhost = it.Current.isGhost;
                    if (it.Current.node != ZingData.Cnst_Nil)
                    {
                        if (it.Current.isGhost)
                        {
                            if (!targetIsGhost)
                            {
                                errors.Add(new Flag(SeverityKind.Error, n, string.Format("The payload of a send to a real machine must also be real."), 0, CompilingProgram));
                                return null;
                            }
                            if (payloadType == PType.Id)
                            {
                                errors.Add(new Flag(SeverityKind.Error, n, string.Format("Ghost payload of type machine id is not allowed."), 0, CompilingProgram));
                                return null;
                            }
                        }
                    }
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
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingApply(ZingData.Cnst_Dot, targetExpr, MkZingIdentifier("EnqueueEvent")), eventExpr, tmpVar, MkCnst("myHandle"))));
                    ctxt.addSideEffect(MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Send"),MkCnst(ctxt.labelToId(afterLabel)))));
                    // Actual Send statement
                    return new ZingTranslationInfo(MkZingSeq(MkZingReturn(MkZingIdentifier("entryCtxt")),
                        MkZingLabeledStmt(afterLabel, ZingData.Cnst_Nil)), new PNilType(), targetIsGhost || argIsGhost);
                }
            }
            else if (funName == PData.Con_While.Node.Name)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var condExpr = it.Current;
                    it.MoveNext();
                    var body = it.Current.node;

                    if (condExpr == null || it.Current == null)
                    {
                        return null;
                    }

                    if (condExpr.node != null && condExpr.type != PType.Bool)
                    {
                        errors.Add(new Flag(SeverityKind.Error, n, string.Format("Conditional expression must be Boolean not {0}", condExpr.type), 0, CompilingProgram));
                        return null;
                    }

                    if (!allMachines[machineName].isGhost)
                    {
                        if (condExpr == null)
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Use of nondet in a real machine is not allowed."), 0, CompilingProgram));
                            return null;
                        }
                        if (condExpr.isGhost && !allMachines[machineName].isGhost)
                        {
                            errors.Add(new Flag(SeverityKind.Error, n, string.Format("Use of ghost expression in a conditional in a real machine is not allowed."), 0, CompilingProgram));
                            return null;
                        }
                    }

                    var loopStart = getUnique(entityName + "_loop_start");
                    var loopEnd = getUnique(entityName + "_loop_end");

                    body = ctxt.emitZingSideEffects(zingWrapExprToStmts(body));

                    var res = MkZingLabeledStmt(loopStart, MkZingSeq(
                        ctxt.emitZingSideEffects(MkZingIf(MkZingApply(ZingData.Cnst_Not, condExpr.node), AddArgs(ZingData.App_Goto, MkCnst(loopEnd)))),
                        body,
                        AddArgs(ZingData.App_Goto, MkCnst(loopStart)),
                        MkZingLabeledStmt(loopEnd, ZingData.Cnst_Nil)));
                        
                    return new ZingTranslationInfo(res, new PNilType(), condExpr.isGhost);
                }
            }
            else if (funName == PData.Con_MachType.Node.Name)
            {
                return new ZingTranslationInfo(Factory.Instance.ToAST(n), new PNilType());
            }
            else if (funName == PData.Con_TypeTuple.Node.Name || funName == PData.Con_TypeNamedTuple.Node.Name)
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

        AST<Node> MkZingCreateMachineCall(ZingEntryFun_FoldContext ctxt, string typeName, Node initializers)
        {
            Dictionary<string, Node> localVarToInitializer = new Dictionary<string, Node>();
            while (true)
            {
                if (initializers.NodeKind == NodeKind.Id)
                    break;
                using (var localIter1 = ((FuncTerm)initializers).Args.GetEnumerator())
                {
                    localIter1.MoveNext();
                    var index = GetName((FuncTerm)localIter1.Current, 0);
                    localIter1.MoveNext();
                    initializers = localIter1.Current;
                    using (var localIter2 = ((FuncTerm)initializers).Args.GetEnumerator())
                    {
                        localIter2.MoveNext();
                        var value = localIter2.Current;
                        localVarToInitializer[index] = value;
                        localIter2.MoveNext();
                        initializers = localIter2.Current;
                    }
                }
            }
            List<AST<Node>> args = new List<AST<Node>>();
            foreach (var x in allMachines[typeName].localVariableToVarInfo)
            {
                var varName = x.Key;
                var varType = x.Value.type;
                if (localVarToInitializer.ContainsKey(varName))
                {
                    args.Add(Factory.Instance.ToAST(localVarToInitializer[varName]));
                }
                else
                {
                    // getDefault() should be a single call with no argument, so only 1 side
                    // effect generated here.
                    var argNode = getZingDefault(ctxt, varType);
                    if (varType is PCompoundType || varType is PAnyType)
                    {
                        var tmpId = ctxt.getTmpVar(varType, "tmpInitializer");
                        ctxt.addSideEffect(MkZingAssign(tmpId, argNode));
                        argNode = tmpId;
                    }
                    args.Add(argNode);
                }
            }
            
            return MkZingCall(MkZingDot("Main", string.Format("CreateMachine_{0}", typeName)), args);
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
                MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Leave"))),
                MkZingReturn(MkZingIdentifier("entryCtxt")));

            return MkZingMethodDecl(name, MkZingVarDecls(MkZingVarDecl("entryCtxt", MkCnst(getZingContinuationCtxtType(machine)))), MkCnst(getZingContinuationCtxtType(machine)),
                ctxt.emitLocals(), MkZingBlock("dummy", body));
        }

        private AST<Node> MkZingFunMethod(string funName, FunInfo funInfo)
        {
            var machineName = GetName(GetFuncTerm(GetArgByIndex(funInfo.funDecl, 1)), 0);
            AST<Node> parameters = LocalVariablesToVarDecls(funInfo.parameterNames, funInfo.parameterNameToInfo);
            parameters = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("entryCtxt", MkCnst(getZingContinuationCtxtType(machineName))), parameters);
            AST<Node> funBody;
            AST<Node> entry = Factory.Instance.ToAST(GetArgByIndex(funInfo.funDecl, 5));

            var ctxt = new ZingEntryFun_FoldContext(machineName, TranslationContext.Function, funName, this);
            var tuple = Factory.Instance.ToAST(entry.Node).Compute<ZingTranslationInfo>(
                x => ZingEntryFun_UnFold(ctxt, x),
                (x, ch) => ZingEntryFun_Fold(ctxt, x, ch));

            if (tuple == null) {
                funBody = ZingData.Cnst_Nil;
            } else {
                Debug.Assert(ctxt.sideEffectsStack.Count == 1);
                funBody = ctxt.emitZingSideEffects(zingWrapExprToStmts(tuple.node));
                ctxt.pushSideEffectStack();
            }

            funBody = MkZingSeq(
                ctxt.emitLabelPrelude(),
                MkZingLabeledStmt("start", funBody),
                MkZingCallStmt(MkZingCall(MkZingDot("entryCtxt", "Return"))),
                MkZingReturn(MkZingIdentifier("entryCtxt")));

            var retType = MkCnst(getZingContinuationCtxtType(machineName));
            return MkZingMethodDecl(funName, parameters, retType, ctxt.emitLocals(), MkZingBlock("dummy", funBody));
        }

        private AST<FuncTerm> MkZingPlus(AST<Node> n, int i)
        {
            return MkZingApply(ZingData.Cnst_Add, n, Factory.Instance.MkCnst(i));
        }

        private AST<FuncTerm> MkZingMinus(AST<Node> n, int i)
        {
            return MkZingApply(ZingData.Cnst_Sub, n, Factory.Instance.MkCnst(i));
        }

        private AST<Node> MkZingListIter(AST<Node> iterVar, AST<Node> start, AST<Node> end, AST<Node> body)
        {
            return MkZingSeq(
                MkZingAssign(iterVar, start),
                AddArgs(ZingData.App_While, MkZingApply(ZingData.Cnst_NEq, iterVar, end),
                    MkZingSeq(body, MkZingAssign(iterVar, MkZingDot(iterVar, "next")))));
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
                MkZingIf(MkZingNeq(MkZingDot("this", "size"), MkZingDot("other", "size")),
                    MkZingReturn(ZingData.Cnst_False)),
                    MkZingFor(ind, 0, MkZingDot("this", "size"),
                        ctxt.emitZingSideEffects(MkZingIf(MkZingNeq(ctxt, MkZingIndex(MkZingDot("this", "arr"), ind), meT.T, MkZingIndex(MkZingDot("other", "arr"), ind), otherT.T),
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
                    ctxt.emitZingSideEffects(MkZingIf(MkZingApply(ZingData.Cnst_Not, MkZingCanDownCastTo(ctxt, MkZingIndex(MkZingDot("this", "arr"), ind), fromT.T, toT.T)),
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

            var iter = ctxt.getTmpVar(MkCnst(pTypeToZingClassName(meT) + "_Entry"), "iter");
            var found = ctxt.getTmpVar(PType.Bool, "found");
            var val = ctxt.getTmpVar(pTypeToZingType(otherT.ValT), "val");
            var body = MkZingSeq(
                MkZingIf(MkZingNeq(MkZingDot("this", "size"), MkZingDot("other", "size")), MkZingReturn(ZingData.Cnst_False)),
                MkZingListIter(iter, MkZingDot("head", "next"), MkZingIdentifier("head"),
                               ctxt.emitZingSideEffects(
                                    MkZingSeq(MkZingAssign(found, MkZingCall(MkZingDot("other", "Contains"), MkZingDot(iter, "key"))),
                                              MkZingIf(MkZingApply(ZingData.Cnst_Not, found), MkZingReturn(ZingData.Cnst_False)),
                                              MkZingAssign(val, MkZingCall(MkZingDot("other", "Lookup"), MkZingDot(iter, "key"))),
                                              MkZingIf(MkZingApply(ZingData.Cnst_Not, MkZingEq(ctxt, val, otherT.ValT, MkZingDot(iter, "val"), meT.ValT)), MkZingReturn(ZingData.Cnst_False))
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
            var iter = ctxt.getTmpVar(MkCnst(pTypeToZingClassName(fromT) + "_Entry"), "iter");
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

            var iter = ctxt.getTmpVar(MkCnst(pTypeToZingClassName(fromT) + "_Entry"), "iter");
            var body = MkZingSeq(
                MkZingListIter(iter, MkZingDot("head", "next"), MkZingIdentifier("head"), 
                    ctxt.emitZingSideEffects(MkZingIf(MkZingApply(ZingData.Cnst_Not, 
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
            var iter = ctxt.getTmpVar(MkCnst(pTypeToZingClassName(t) + "_Entry"), "iter");
            var found = ctxt.getTmpVar(PType.Bool, "found");
            var containsBody = MkZingSeq(
                MkZingListIter(iter, MkZingDot("head", "next"), MkZingIdentifier("head"),
                               ctxt.emitZingSideEffects(MkZingIf(MkZingEq(ctxt, MkZingIdentifier("key"), t.KeyT, MkZingDot(iter, "key"), t.KeyT), MkZingReturn(ZingData.Cnst_True)))),
                MkZingReturn(ZingData.Cnst_False));
            ctxt.pushSideEffectStack();

            return MkZingMethodDecl("Contains", MkZingVarDecls(MkZingVarDecl("key", t.KeyT)),
                ZingData.Cnst_Bool, ctxt.emitLocals(), MkZingBlock("dummy", containsBody));
        }

        private AST<FuncTerm> MkZingMapLookup(PMapType t)
        {
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Lookup", this);
            var iter = ctxt.getTmpVar(MkCnst(pTypeToZingClassName(t) + "_Entry"), "iter");
            var found = ctxt.getTmpVar(PType.Bool, "found");
            var lookupBody = MkZingSeq(
                MkZingListIter(iter, MkZingDot("head", "next"), MkZingIdentifier("head"),
                               ctxt.emitZingSideEffects(MkZingIf(MkZingEq(ctxt, MkZingIdentifier("key"), t.KeyT, MkZingDot(iter, "key"), t.KeyT), MkZingReturn(MkZingDot(iter, "val"))))),
                MkZingAssert(ZingData.Cnst_False));
            ctxt.pushSideEffectStack();

            return MkZingMethodDecl("Lookup", MkZingVarDecls(MkZingVarDecl("key", t.KeyT)),
                pTypeToZingType(t.ValT), ctxt.emitLocals(), MkZingBlock("dummy", lookupBody));
        }

        private AST<FuncTerm> MkZingMapUpdate(PMapType t)
        {
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Update", this);
            var prev = ctxt.getTmpVar(MkCnst(pTypeToZingClassName(t) + "_Entry"), "prev");
            var iter = ctxt.getTmpVar(MkCnst(pTypeToZingClassName(t) + "_Entry"), "iter");
            var found = ctxt.getTmpVar(PType.Bool, "found");
            var insertBody = MkZingSeq(
                MkZingAssign(prev, MkZingIdentifier("head")),
                MkZingListIter(iter, MkZingDot("head", "next"), MkZingIdentifier("head"),
                               ctxt.emitZingSideEffects(
                                    MkZingSeq(MkZingIf(MkZingEq(ctxt, MkZingIdentifier("key"), t.KeyT, MkZingDot(iter, "key"), t.KeyT), MkZingSeq(MkZingAssign(MkZingDot(iter, "val"), MkZingIdentifier("val")), MkZingReturn(ZingData.Cnst_Nil))),
                                              MkZingAssign(prev, iter)))),
                MkZingAssign(MkZingDot("head", "key"), MkZingIdentifier("key")),
                MkZingAssign(MkZingDot("head", "val"), MkZingIdentifier("val")),
                MkZingAssign(MkZingIdentifier("head"), AddArgs(ZingData.App_New, MkCnst(pTypeToZingClassName(t) + "_Entry"), ZingData.Cnst_Nil)),
                MkZingAssign(MkZingDot("head", "next"), iter),
                MkZingAssign(MkZingDot(prev, "next"), MkZingIdentifier("head")),
                MkZingAssign(MkZingIdentifier("size"), MkZingApply(ZingData.Cnst_Add, MkZingIdentifier("size"), MkCnst(1)))
                );
            ctxt.pushSideEffectStack();

            return MkZingMethodDecl("Update", MkZingVarDecls(MkZingVarDecl("key", t.KeyT), MkZingVarDecl("val", t.ValT)),
                ZingData.Cnst_Void, ctxt.emitLocals(), MkZingBlock("dummy", insertBody));
        }

        private AST<FuncTerm> MkZingMapRemove(PMapType t)
        {
            ZingEntryFun_FoldContext ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Remove", this);
            var prev = ctxt.getTmpVar(MkCnst(pTypeToZingClassName(t) + "_Entry"), "prev");
            var iter = ctxt.getTmpVar(MkCnst(pTypeToZingClassName(t) + "_Entry"), "iter");
            var found = ctxt.getTmpVar(PType.Bool, "found");
            var removeBody = MkZingSeq(
                MkZingAssign(prev, MkZingIdentifier("head")),
                MkZingListIter(iter, MkZingDot("head", "next"), MkZingIdentifier("head"),
                               ctxt.emitZingSideEffects(
                                    MkZingSeq(MkZingIf(MkZingEq(ctxt, MkZingIdentifier("key"), t.KeyT, MkZingDot(iter, "key"), t.KeyT), MkZingSeq(
                                                                MkZingAssign(MkZingDot(prev, "next"), MkZingDot(iter, "next")), 
                                                                MkZingAssign(MkZingIdentifier("size"), MkZingApply(ZingData.Cnst_Sub, MkZingIdentifier("size"), MkCnst(1))),
                                                                MkZingAssign(MkZingIdentifier("iterCache"), MkZingDot("head", "next")),
                                                                MkZingAssign(MkZingIdentifier("iterCacheIndex"), MkCnst(0)),
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
            var iter = ctxt.getTmpVar(MkCnst(pTypeToZingClassName(fromT) + "_Entry"), "iter");
            var isUpcast = fromT.isSubtypeOf(toT) && toT != fromT;
            var from = isUpcast ? "obj" : "this"; // Only Upcasts are static

            var body = MkZingSeq(
                MkZingAssign(res, MkZingCall(MkZingDot(pTypeToZingClassName(toT), "BuildDefault"))),
                MkZingListIter(iter, MkZingDot("head", "next"), MkZingIdentifier("head"),
                               MkZingCallStmt(MkZingCall(MkZingDot(res, "Insert"), MkZingDot(iter, "key"), MkZingDot(iter, "val")))),
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

            var fields = ConstructList(ZingData.App_VarDecls, MkZingVarDecl("head", entryClassName), MkZingVarDecl("size", pTypeToZingType(PType.Int)), MkZingVarDecl("iterCache", MkCnst(pTypeToZingClassName(t) + "_Entry")), MkZingVarDecl("iterCacheIndex", PType.Int));

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

            // 
            methods.Add(MkZingMapContains(t));

            methods.Add(MkZingMapLookup(t));

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
                                    MkSeq(buildDefAssigns),
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
            methods.Add(MkZingMethodDecl("Clone", ZingData.Cnst_Nil, zType, cloneLocals,  cloneBody));

            // Create the "Equals" method.
            var equalsBody = new List<AST<Node>>();
            ZingEntryFun_FoldContext equalsCtxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Equals", this);
            foreach (var field in fieldDesc) {
                var check = MkZingNeq(equalsCtxt, MkZingDot("this", field.Item2), field.Item1, MkZingDot("other", field.Item2), field.Item1);
                equalsBody.Add(equalsCtxt.emitZingSideEffects(AddArgs(ZingData.App_ITE, check, AddArgs(ZingData.App_Return, ZingData.Cnst_False), ZingData.Cnst_Nil)));
                equalsCtxt.pushSideEffectStack();
            }

            equalsBody.Add(AddArgs(ZingData.App_Return, ZingData.Cnst_True));

            methods.Add(MkZingMethodDecl("Equals", AddArgs(ZingData.App_VarDecls, MkZingVarDecl("other", zType), ZingData.Cnst_Nil),
                ZingData.Cnst_Bool, equalsCtxt.emitLocals(), MkZingBlock("dummy", ConstructList(ZingData.App_Seq, equalsBody))));

            // For all subtypes, emit the DownCastTo and UpCastFrom methods
            foreach (var subT in subtypes[pType]) {
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

                    equalsBody.Add(ctxt.emitZingSideEffects(MkZingIf(MkZingNeq(ctxt, MkZingDot("this", supF.Item2), supF.Item1,
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
                        Debug.Assert(supF.Item1 is PAnyType || supF.Item1 is PEventType || supF.Item1 is PIdType);
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
                        checks.Add(ctxt.emitZingSideEffects(AddArgs(ZingData.App_ITE, cond, AddArgs(ZingData.App_Return, ZingData.Cnst_False), ZingData.Cnst_Nil)));
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

        public static AST<Cnst> MkCnst(string s)
        {
            return Factory.Instance.MkCnst(s);
        }

        public static AST<Cnst> MkCnst(int s)
        {
            return Factory.Instance.MkCnst(s);
        }


        private void MkZingClasses(List<AST<Node>> elements)
        {
            foreach (string machineName in allMachines.Keys)
            {
                AST<Node> fields = LocalVariablesToVarDecls(allMachines[machineName].localVariableToVarInfo.Keys, allMachines[machineName].localVariableToVarInfo);
                fields = AddArgs(ZingData.App_VarDecls, MkZingVarDecl("myHandle", ZingData.Cnst_SmHandle), fields);
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

                AST<FuncTerm> classDecl = AddArgs(ZingData.App_ClassDecl, Factory.Instance.MkCnst(machineName), fields, methods);
                elements.Add(classDecl);
            }
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

                AST<Node> methods = ZingData.Cnst_Nil;
                foreach (var machineName in allMachines.Keys)
                {
                    var createMachineMethod = MkCreateMachineMethod(machineName);
                    methods = AddArgs(ZingData.App_MethodDecls, createMachineMethod, methods);
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

                        runBody = MkZingSeq(runBody, ctxt.emitZingSideEffects(MkZingCallStmt(mainConstructor.node)));
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
                    var iteStmt = AddArgs(ZingData.App_ITE, iteExpr, ZingData.Cnst_Nil, assignStmt);
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
                                (t2 is PIdType && t1 is PNilType) ||
                                (t2 is PEventType && t1 is PNilType) ||
                                (t2 is PSeqType && t1 is PSeqType));
                        }
                    }
                }

                // Generate the IsSubtype static method. IsSybtype :: (Discriminator, Discriminator) -> Bool
                // Allows the runtime to check whether one statically declared type is a subtype of another.
                var isSubBody = new List<AST<Node>>();
                isSubBody.Add(AddArgs(ZingData.App_ITE, MkZingEq(MkZingIdentifier("b"), pTypeToZingDiscriminator(PType.Any)),
                        AddArgs(ZingData.App_Return, ZingData.Cnst_True), ZingData.Cnst_Nil));

                foreach (var t in allTypes)
                {
                    if (t is PAnyType)
                        continue;

                    var ifBody = new List<AST<Node>>();
                    ifBody.Add(AddArgs(ZingData.App_ITE, MkZingEq(MkZingIdentifier("a"), pTypeToZingDiscriminator(t)),
                            AddArgs(ZingData.App_Return, ZingData.Cnst_True), ZingData.Cnst_Nil));

                    foreach (var subT in subtypes[t])
                    {
                        ifBody.Add(AddArgs(ZingData.App_ITE, MkZingEq(MkZingIdentifier("a"), pTypeToZingDiscriminator(subT)),
                            AddArgs(ZingData.App_Return, ZingData.Cnst_True), ZingData.Cnst_Nil));
                    }
                    ifBody.Add(AddArgs(ZingData.App_Return, ZingData.Cnst_False));
                    isSubBody.Add(AddArgs(ZingData.App_ITE, MkZingEq(MkZingIdentifier("b"), pTypeToZingDiscriminator(t)),
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
                payloadOfBody.Add(MkZingIf(MkZingEq(MkZingIdentifier("e"), MkZingIdentifier("null")),
                    MkZingReturn(pTypeToZingDiscriminator(PType.Nil))));

                foreach (var evt in allEvents.Keys.Where(x => x != NullEvent))
                {
                    payloadOfBody.Add(AddArgs(ZingData.App_ITE, MkZingEq(MkZingDot("e", "name"), MkZingDot("Event", "_" + evt)),
                        AddArgs(ZingData.App_Return, pTypeToZingDiscriminator(allEvents[evt].payloadType)),
                        ZingData.Cnst_Nil));
                }

                payloadOfBody.Add(AddArgs(ZingData.App_Assert, ZingData.Cnst_False));
                AST<Node> payloadOfMethod = MkZingMethodDecl("PayloadOf",
                    MkZingVarDecls(MkZingVarDecl("e", Factory.Instance.MkCnst("SM_EVENT"))),
                    Factory.Instance.MkCnst("Discriminator"), ZingData.Cnst_Nil, MkZingBlock("dummy", MkZingSeq(payloadOfBody)), ZingData.Cnst_Static);
                methods = AddArgs(ZingData.App_MethodDecls, payloadOfMethod, methods);

                AST<FuncTerm> classDecl = AddArgs(ZingData.App_ClassDecl, Factory.Instance.MkCnst("Main"), fields, methods);
                elements.Add(classDecl);
            }

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

            // Emit the Continuation Context Classes
            foreach (var mName in allMachines.Keys)
            {

                var vars = new List<AST<Node>>();
                var methods = new List<AST<Node>>();
                var conT = MkCnst(getZingContinuationCtxtType(mName));

                vars.Add(MkZingVarDecl("returnTo", MkCnst("StackFrame")));
                vars.Add(MkZingVarDecl("payload", MkCnst(SM_ARG_UNION)));
                vars.Add(MkZingVarDecl("ev", MkCnst("SM_EVENT")));
                vars.Add(MkZingVarDecl("state", MkCnst(getZingStateEnumType(mName))));
                vars.Add(MkZingVarDecl("target", MkCnst("SM_HANDLE")));
                vars.Add(MkZingVarDecl("reason", MkCnst("ContinuationReason")));
                vars.Add(MkZingVarDecl("nondet", PType.Bool));

                { // Add the PopReturnTo method 
                    var ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "PopReturnTo", this);
                    var body = new List<AST<Node>>();

                    var tmpVar = ctxt.getTmpVar(PType.Int, "res");
                    
                    body.Add(MkZingAssign(tmpVar, MkZingDot("this", "returnTo", "pc")));
                    body.Add(MkZingAssign(MkZingDot("this", "returnTo"), MkZingDot("this", "returnTo", "next")));
                    body.Add(MkZingReturn(tmpVar));
                    methods.Add(MkZingMethodDecl("PopReturnTo", ZingData.Cnst_Nil,
                        pTypeToZingType(PType.Int), ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(body))));

                }

                { // Add the PushReturnTo method 
                    var ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "PushReturnTo", this);
                    var body = new List<AST<Node>>();

                    var tmpVar = ctxt.getTmpVar(MkCnst("StackFrame"), "tmp");

                    body.Add(MkZingAssign(tmpVar, AddArgs(ZingData.App_New, MkCnst("StackFrame"), ZingData.Cnst_Nil)));
                    body.Add(MkZingAssign(MkZingDot(tmpVar, "pc"), MkZingIdentifier("ret")));
                    body.Add(MkZingAssign(MkZingDot(tmpVar, "next"), MkZingDot("this", "returnTo")));
                    body.Add(MkZingAssign(MkZingDot("this", "returnTo"), tmpVar));
                    methods.Add(MkZingMethodDecl("PushReturnTo", MkZingVarDecls(MkZingVarDecl("ret", PType.Int)),
                        ZingData.Cnst_Void, ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(body))));

                }

                { // Add the Construct_Default method
                    var ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Construct_Default", this);
                    var body = new List<AST<Node>>();

                    var tmpVar = ctxt.getTmpVar(conT, "res");
                    body.Add(MkZingAssign(tmpVar, AddArgs(ZingData.App_New, conT, ZingData.Cnst_Nil)));
                    body.Add(MkZingAssign(MkZingDot(tmpVar, "returnTo"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot(tmpVar, "payload"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot(tmpVar, "ev"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot(tmpVar, "state"), MkZingDot(getZingStateEnumType(mName), "_" + allMachines[mName].stateNameToStateInfo.Keys.First())));
                    body.Add(MkZingAssign(MkZingDot(tmpVar, "target"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot(tmpVar, "reason"), MkZingDot("ContinuationReason", "Leave")));
                    body.Add(MkZingReturn(tmpVar));
                    methods.Add(MkZingMethodDecl("Construct_Default", MkZingVarDecls(),
                        conT, ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(body)), ZingData.Cnst_Static));
                }

                { // Add the Return method
                    var ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Return", this);
                    var body = new List<AST<Node>>();
                    body.Add(MkZingAssign(MkZingDot("this", "returnTo"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "payload"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "ev"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "state"), MkZingDot(getZingStateEnumType(mName), "_" + allMachines[mName].stateNameToStateInfo.Keys.First())));
                    body.Add(MkZingAssign(MkZingDot("this", "target"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "reason"), MkZingDot("ContinuationReason", "Return")));
                    methods.Add(MkZingMethodDecl("Return", MkZingVarDecls(), ZingData.Cnst_Void, ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(body))));
                }


                { // Add the ReturnVal method
                    var ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "ReturnVal", this);
                    var body = new List<AST<Node>>();
                    body.Add(MkZingAssign(MkZingDot("this", "returnTo"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "ev"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "state"), MkZingDot(getZingStateEnumType(mName), "_" + allMachines[mName].stateNameToStateInfo.Keys.First())));
                    body.Add(MkZingAssign(MkZingDot("this", "target"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "reason"), MkZingDot("ContinuationReason", "ReturnVal")));
                    body.Add(MkZingAssign(MkZingDot("this", "payload"), MkZingIdentifier("val")));
                    methods.Add(MkZingMethodDecl("ReturnVal", MkZingVarDecls(MkZingVarDecl("val", MkCnst(SM_ARG_UNION))),
                        ZingData.Cnst_Void, ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(body))));
                }

                { // Add the Leave method
                    var ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Leave", this);
                    var body = new List<AST<Node>>();
                    body.Add(MkZingAssign(MkZingDot("this", "returnTo"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "payload"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "ev"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "state"), MkZingDot(getZingStateEnumType(mName), "_" + allMachines[mName].stateNameToStateInfo.Keys.First())));
                    body.Add(MkZingAssign(MkZingDot("this", "target"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "reason"), MkZingDot("ContinuationReason", "Leave")));
                    methods.Add(MkZingMethodDecl("Leave", MkZingVarDecls(),
                        ZingData.Cnst_Void, ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(body))));
                }

                { // Add the Raise method
                    var ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Raise", this);
                    var body = new List<AST<Node>>();
                    body.Add(MkZingAssign(MkZingDot("this", "returnTo"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "state"), MkZingDot(getZingStateEnumType(mName), "_" + allMachines[mName].stateNameToStateInfo.Keys.First())));
                    body.Add(MkZingAssign(MkZingDot("this", "target"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "reason"), MkZingDot("ContinuationReason", "Raise")));
                    body.Add(MkZingAssign(MkZingDot("this", "ev"), MkZingIdentifier("ev")));
                    body.Add(MkZingAssign(MkZingDot("this", "payload"), MkZingIdentifier("payload")));
                    methods.Add(MkZingMethodDecl("Raise", MkZingVarDecls(MkZingVarDecl("ev", MkCnst("SM_EVENT")), MkZingVarDecl("payload", MkCnst(SM_ARG_UNION))),
                        ZingData.Cnst_Void, ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(body))));
                }

                { // Add the Send method
                    var ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Send", this);
                    var body = new List<AST<Node>>();
                    body.Add(MkZingAssign(MkZingDot("this", "returnTo"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "state"), MkZingDot(getZingStateEnumType(mName), "_" + allMachines[mName].stateNameToStateInfo.Keys.First())));
                    body.Add(MkZingAssign(MkZingDot("this", "reason"), MkZingDot("ContinuationReason", "Send")));
                    body.Add(MkZingAssign(MkZingDot("this", "target"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "ev"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "payload"), MkZingIdentifier("null")));
                    body.Add(MkZingCallStmt(MkZingCall(MkZingDot("this", "PushReturnTo"), MkZingIdentifier("ret"))));
                    methods.Add(MkZingMethodDecl("Send", MkZingVarDecls(MkZingVarDecl("ret", PType.Int)),
                        ZingData.Cnst_Void, ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(body))));
                }
                { // Add the New method
                    var ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "NewM", this);
                    var body = new List<AST<Node>>();
                    body.Add(MkZingAssign(MkZingDot("this", "returnTo"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "state"), MkZingDot(getZingStateEnumType(mName), "_" + allMachines[mName].stateNameToStateInfo.Keys.First())));
                    body.Add(MkZingAssign(MkZingDot("this", "reason"), MkZingDot("ContinuationReason", "NewM")));
                    body.Add(MkZingAssign(MkZingDot("this", "target"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "ev"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "payload"), MkZingIdentifier("null")));
                    body.Add(MkZingCallStmt(MkZingCall(MkZingDot("this", "PushReturnTo"), MkZingIdentifier("ret"))));
                    methods.Add(MkZingMethodDecl("NewM", MkZingVarDecls(MkZingVarDecl("ret", PType.Int)),
                        ZingData.Cnst_Void, ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(body))));
                }
                { // Add the Call method
                    var ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Call", this);
                    var body = new List<AST<Node>>();
                    body.Add(MkZingAssign(MkZingDot("this", "returnTo"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "payload"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "ev"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "target"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "reason"), MkZingDot("ContinuationReason", "Call")));
                    body.Add(MkZingAssign(MkZingDot("this", "state"), MkZingIdentifier("state")));
                    body.Add(MkZingCallStmt(MkZingCall(MkZingDot("this", "PushReturnTo"), MkZingIdentifier("ret"))));
                    methods.Add(MkZingMethodDecl("Call", MkZingVarDecls(MkZingVarDecl("state", MkCnst(getZingStateEnumType(mName))), MkZingVarDecl("ret", PType.Int)),
                        ZingData.Cnst_Void, ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(body))));
                }

                { // Add the Nondet method
                    var ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Nondet", this);
                    var body = new List<AST<Node>>();
                    
                    body.Add(MkZingAssign(MkZingDot("this", "returnTo"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "payload"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "ev"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "state"), MkZingDot(getZingStateEnumType(mName), "_" + allMachines[mName].stateNameToStateInfo.Keys.First())));
                    body.Add(MkZingAssign(MkZingDot("this", "target"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "reason"), MkZingDot("ContinuationReason", "Nondet")));
                    body.Add(MkZingCallStmt(MkZingCall(MkZingDot("this", "PushReturnTo"), MkZingIdentifier("ret"))));
                    methods.Add(MkZingMethodDecl("Nondet", MkZingVarDecls(MkZingVarDecl("ret", PType.Int)),
                        ZingData.Cnst_Void, ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(body))));
                }

                { // Add the Delete method
                    var ctxt = new ZingEntryFun_FoldContext(null, TranslationContext.Function, "Delete", this);
                    var body = new List<AST<Node>>();
                    
                    body.Add(MkZingAssign(MkZingDot("this", "returnTo"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "payload"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "ev"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "state"), MkZingDot(getZingStateEnumType(mName), "_" + allMachines[mName].stateNameToStateInfo.Keys.First())));
                    body.Add(MkZingAssign(MkZingDot("this", "target"), MkZingIdentifier("null")));
                    body.Add(MkZingAssign(MkZingDot("this", "reason"), MkZingDot("ContinuationReason", "Delete")));
                    methods.Add(MkZingMethodDecl("Delete", MkZingVarDecls(),
                    ZingData.Cnst_Void, ctxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(body))));
                }

                elements.Add(AddArgs(ZingData.App_ClassDecl, conT,
                    MkZingVarDecls(vars), ConstructList(ZingData.App_MethodDecls, methods)));

            }

            // Emit the SM_ARG_UNION class
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
                    body.Add(MkZingIf(MkZingEq(MkZingDot("result", "d"), pTypeToZingDiscriminator(t)), MkZingAssignOrCast(thisM, t, otherM, t)));
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
                        ifBody.Add(MkZingIf(MkZingEq(otherD, pTypeToZingDiscriminator(otherT)),
                            eqCtxt.emitZingSideEffects(MkZingReturn(MkZingEq(eqCtxt, thisM, t, MkZingDot("other", pTypeToZingUnionMember(otherT)), otherT)))));
                        eqCtxt.pushSideEffectStack();
                    }

                    equalsBody.Add(MkZingIf(MkZingEq(MkZingDot("this", "d"), pTypeToZingDiscriminator(t)), MkZingSeq(ifBody)));
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
                        equalsSubBody.Add(MkZingIf(MkZingEq(MkZingDot("this", "d"), pTypeToZingDiscriminator(otherT)),
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
                        downcastBody.Add(MkZingIf(MkZingEq(MkZingDot("this", "d"), pTypeToZingDiscriminator(midT)),
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
                        candowncastBody.Add(AddArgs(ZingData.App_ITE, MkZingEq(MkZingDot("this", "d"), pTypeToZingDiscriminator(midT)),
                            ctxt.emitZingSideEffects(AddArgs(ZingData.App_Return, MkZingCanDownCastTo(ctxt, MkZingDot("this", pTypeToZingUnionMember(midT)), midT, t))),
                            ZingData.Cnst_Nil));
                        ctxt.pushSideEffectStack();
                    }

                    foreach (var subT in subtypes[t])
                    {
                        candowncastBody.Add(AddArgs(ZingData.App_ITE, MkZingEq(MkZingDot("this", "d"), pTypeToZingDiscriminator(subT)),
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
                canCastBody.Add(MkZingIf(MkZingEq(MkZingIdentifier("toT"), MkZingDot("this", "d")), MkZingReturn(ZingData.Cnst_True)));
                // this.d is a subtype of toT
                canCastBody.Add(MkZingAssign(tmpEq, MkZingCall(MkZingDot("Main", "IsSubtype"), MkZingDot("this", "d"), MkZingIdentifier("toT"))));
                canCastBody.Add(MkZingIf(tmpEq, MkZingReturn(ZingData.Cnst_True)));
                // this.d is a supertype of toT, and we can downcast to it.
                foreach (var t in anyMemberTypes)
                {
                    canCastBody.Add(MkZingIf(MkZingEq(MkZingIdentifier("toT"), pTypeToZingDiscriminator(t)),
                        MkZingSeq(
                            MkZingAssign(tmpEq, MkZingCall(MkZingDot("this", getZingCanDownCastName(t)))),
                            MkZingReturn(tmpEq))));
                }

                canCastBody.Add(MkZingAssert(ZingData.Cnst_False)); // Sanity Check that we don't make it there.
                anyMethods.Add(MkZingMethodDecl("CanCastTo", MkZingVarDecls(MkZingVarDecl("toT", Factory.Instance.MkCnst("Discriminator"))),
                    pTypeToZingType(PType.Bool), canCastCtxt.emitLocals(), MkZingBlock("dummy", MkZingSeq(canCastBody))));

                elements.Add(AddArgs(ZingData.App_ClassDecl, Factory.Instance.MkCnst(SM_ARG_UNION),
                    MkZingVarDecls(anyVars), ConstructList(ZingData.App_MethodDecls, anyMethods)));
            }
        }

        AST<Node> MkZingIndex(AST<Node> baseExp, AST<Node> indExp)
        {
            return MkZingApply(ZingData.Cnst_Index, baseExp, indExp);
        }

        AST<Node> LocalVariablesToVarDecls(IEnumerable<string> varNames, Dictionary<string, VariableInfo> varNameToVarInfo)
        {
            return MkZingVarDecls(varNames.Select(name => MkZingVarDecl(name, pTypeToZingType(varNameToVarInfo[name].type))));
        }

        AST<Node> MkZingSeq(List<AST<Node>> stmts)
        {
            AST<Node> ret = ZingData.Cnst_Nil;
            foreach (var stmt in stmts.Reverse<AST<Node>>())
            {
                ret = AddArgs(ZingData.App_Seq, stmt, ret);
            }
            return ret;
        }


        AST<Node> MkZingSeq(params AST<Node>[] stmts)
        {
            return MkZingSeq(new List<AST<Node>>(stmts));
        }

        AST<Node> MkCreateMachineMethod(string machineName)
        {
            var objectName = string.Format("o_{0}", machineName);
            var parameters = LocalVariablesToVarDecls(allMachines[machineName].localVariableToVarInfo.Keys, allMachines[machineName].localVariableToVarInfo);
            var localVars = AddArgs(ZingData.App_VarDecls, MkZingVarDecl(objectName, Factory.Instance.MkCnst(machineName)), ZingData.Cnst_Nil);
            var machineInstance = MkZingIdentifier(string.Format("{0}_instance", machineName));
            var body = MkZingSeq(
                    MkZingAssign(MkZingIdentifier(objectName), AddArgs(ZingData.App_New, Factory.Instance.MkCnst(machineName), ZingData.Cnst_Nil)),
                    MkZingAssign(MkZingDot(objectName, "myHandle"), 
                                 MkZingCall(MkZingDot("SM_HANDLE", "Construct"), MkZingDot("Machine", string.Format("_{0}", machineName)), machineInstance, Factory.Instance.MkCnst(allMachines[machineName].maxQueueSize))),
                    MkZingAssign(machineInstance, MkZingApply(ZingData.Cnst_Add, machineInstance, Factory.Instance.MkCnst(1))),
                    MkZingAssign(MkZingDot(objectName, "stackDeferredSet"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("SM_EVENT_SET"), ZingData.Cnst_Nil)),
                    MkZingAssign(MkZingDot(objectName, "stackActionSet"), AddArgs(ZingData.App_New, Factory.Instance.MkCnst("SM_EVENT_SET"), ZingData.Cnst_Nil)),
                    MkZingAssign(MkZingDot(objectName, "localActions"), MkZingCall(MkZingDot("LocalActions", "Construct"), MkZingIdentifier("null")))
                    );
            foreach (var v in allMachines[machineName].localVariableToVarInfo.Keys)
            {
                body = MkZingSeq(body, MkZingAssign(MkZingDot(objectName, v), MkZingIdentifier(v)));
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

        private string getUnique(string prefix)
        {
            if (!uniqIDCounters.ContainsKey(prefix))
                uniqIDCounters[prefix] = 0;

            var ret = uniqIDCounters[prefix];
            uniqIDCounters[prefix]++;
            return prefix + '_' + ret;
        }

        private string GetMachineName(FuncTerm ft, int index)
        {
            FuncTerm machineDecl = GetFuncTerm(GetArgByIndex(ft, index));
            var machineName = GetName(machineDecl, 0);
            return machineName;
        }

        private void registerType(PType t)
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
                registerType((t as PMapType).KeyT);
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

        private PType GetPType(Node n)
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

        private IEnumerable<Node> Model_UnFold(GenericCtxt ctxt, Node n)
        {
            if (n.NodeKind == NodeKind.ModelFact)
            {
                ModelFact mf = (ModelFact)n;
                Node inner = mf.Match;

                bool unsetMachine = false, unsetState = false;

                if (isInstanceOf(inner, PData.Con_ActionDecl))
                {
                    ctxt.curMachine = LookupOwnerName(GetArgByIndex((FuncTerm)inner, 1));
                    unsetMachine = true;
                }
                else if (isInstanceOf(inner, PData.Con_StateDecl))
                {
                    ctxt.curMachine = LookupOwnerName(GetArgByIndex((FuncTerm)inner, 1));
                    ctxt.curState = ((Cnst)GetArgByIndex((FuncTerm)inner, 0)).GetStringValue();
                    unsetMachine = true;
                    unsetState = true;
                }
                else if (isInstanceOf(inner, PData.Con_ExitFun))
                {
                    var stateNode = GetFuncTerm(GetArgByIndex((FuncTerm)inner, 0));
                    ctxt.curMachine = LookupOwnerName(GetArgByIndex(stateNode, 1));
                    ctxt.curState = ((Cnst)GetArgByIndex(stateNode, 0)).GetStringValue();
                    unsetMachine = true;
                    unsetState = true;
                }
                else if (isInstanceOf(inner, PData.Con_VarDecl))
                {
                    ctxt.curMachine = LookupOwnerName(GetArgByIndex((FuncTerm)inner, 1));
                    unsetMachine = true;
                }
                else if (isInstanceOf(inner, PData.Con_TransDecl))
                {
                    var stateNode = GetFuncTerm(GetArgByIndex((FuncTerm)inner, 0));
                    ctxt.curMachine = LookupOwnerName(GetArgByIndex(stateNode, 1));
                    ctxt.curState = ((Cnst)GetArgByIndex(stateNode, 0)).GetStringValue();
                    unsetMachine = true;
                    unsetState = true;
                }
                else if (isInstanceOf(inner, PData.Con_FunDecl))
                {
                    ctxt.curMachine = LookupOwnerName(GetArgByIndex((FuncTerm)inner, 1));
                    unsetMachine = true;
                }
                else if (isInstanceOf(inner, PData.Con_StateSetDecl))
                {
                    ctxt.curMachine = LookupOwnerName(GetArgByIndex((FuncTerm)inner, 1));
                    unsetMachine = true;
                }
                else if (isInstanceOf(inner, PData.Con_EventSetDecl))
                {
                    ctxt.curMachine = LookupOwnerName(GetArgByIndex((FuncTerm)inner, 1));
                    unsetMachine = true;
                }
                else if (isInstanceOf(inner, PData.Con_Install))
                {
                    var stateNode = GetFuncTerm(GetArgByIndex((FuncTerm)inner, 0));
                    ctxt.curMachine = LookupOwnerName(GetArgByIndex(stateNode, 1));
                    ctxt.curState = ((Cnst)GetArgByIndex(stateNode, 0)).GetStringValue();
                    unsetMachine = true;
                    unsetState = true;                    
                }
                else if (isInstanceOf(inner, PData.Con_MachStart))
                {
                    ctxt.curMachine = LookupOwnerName(GetArgByIndex((FuncTerm)inner, 0));
                    unsetMachine = true;
                }
                else if (isInstanceOf(inner, PData.Con_InStateSet))
                {
                    var state = GetFuncTerm(GetArgByIndex((FuncTerm)inner, 1));
                    ctxt.curMachine = LookupOwnerName(GetArgByIndex(state, 1));
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

        internal class GenericCtxt
        {
            public string curMachine;
            public string curState;
        }

        private int ComputePayloadSets(GenericCtxt ctxt, Node n, IEnumerable<int> dummy)
        {
            if (isInstanceOf(n, PData.Con_Scall))
            {
                using (var it = ((FuncTerm) n).Args.GetEnumerator())
                {
                    // SCalls
                    it.MoveNext();
                    var target = it.Current;
                    if (!(target is FuncTerm) || getFtName((FuncTerm)target) != PData.Con_Use.Node.Name) // Error will becaught in type checking, so don't need to report it here
                         return 0;
                    
                    var targetUse = (FuncTerm)target;
                    var val  = ((Cnst)GetArgByIndex(targetUse, 0)).GetStringValue();
                    var kind = ((Id)GetArgByIndex(targetUse, 1)).Name;

                    if (!(kind == PData.Cnst_State.Node.Name)) // Error will becaught in type checking, so don't need to report it here
                        return 0;

                    allMachines[ctxt.curMachine].stateNameToStateInfo[val].argTypes.Add(PType.Nil);
                }
            } else if (isInstanceOf(n, PData.Con_TransDecl)) {
                // Normal transition
                var dst = ((Cnst)GetArgByIndex(GetFuncTerm(GetArgByIndex((FuncTerm)n, 2)), 0)).GetStringValue();
                PType eventArgTypeName;
                string eventName;
                GetEventInfo(GetArgByIndex((FuncTerm)n, 1), out eventName, out eventArgTypeName);
                allMachines[ctxt.curMachine].stateNameToStateInfo[dst].argTypes.Add(eventArgTypeName);
            } else if (isInstanceOf(n, PData.Con_MachStart)) {
                // Start state implicitly can take the Nil payloads
                string state = LookupOwnerName(GetArgByIndex((FuncTerm)n, 1));
                allMachines[ctxt.curMachine].stateNameToStateInfo[state].argTypes.Add(PType.Nil);
            }
            else if (isInstanceOf(n, PData.Con_Install))
            {
                PType eventArgTypeName;
                string eventName;
                GetEventInfo(GetArgByIndex((FuncTerm)n, 1), out eventName, out eventArgTypeName);
                string action = ((Cnst)GetArgByIndex(GetFuncTerm(GetArgByIndex((FuncTerm)n, 2)), 0)).GetStringValue();
                allMachines[ctxt.curMachine].actionFunNameToActionFun[action].argTypes.Add(eventArgTypeName);
            }

            return 0;
        }

        private int ComputeSubmachines(GenericCtxt ctxt, Node n, IEnumerable<int> dummy)
        {
            if (isInstanceOf(n, PData.Con_InStateSet))
            {
                var setName = LookupOwnerName(GetArgByIndex((FuncTerm)n, 0));
                var stateName = LookupOwnerName(GetArgByIndex((FuncTerm)n, 1));
                allMachines[ctxt.curMachine].stateNameToStateInfo[stateName].submachineName = setName;
            }
            return 0;
        }

        private int CheckSubmachineTransitions(GenericCtxt ctxt, Node n, IEnumerable<int> dummy)
        {
            if (isInstanceOf(n, PData.Con_TransDecl))
            {
                var from = LookupOwnerName(GetArgByIndex((FuncTerm)n, 0));
                var to = LookupOwnerName(GetArgByIndex((FuncTerm)n, 2));
                var fromInfo = allMachines[ctxt.curMachine].stateNameToStateInfo[from];
                var toInfo = allMachines[ctxt.curMachine].stateNameToStateInfo[to];
                var isPush = ((Id)GetArgByIndex((FuncTerm)n, 3)).Name == "TRUE";

                if (!isPush && fromInfo.submachineName != toInfo.submachineName)
                {
                    errors.Add(new Flag(SeverityKind.Error, n, string.Format("Ordinary transition from {0} to {1} escapes submachine {2}.", from, to, 
                        fromInfo.submachineName != null ? fromInfo.submachineName : toInfo.submachineName), 0, CompilingProgram));
                }
            }

            return 0;
        }

        const string NullEvent = "null";
        const string DefaultEvent = "default";
        const string DeleteEvent = "delete";

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
                            var maxInstances = (int)((Cnst)GetArgByIndex(ft, 0)).GetNumericValue().Numerator;
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
                            int maxQueueSize = (int) ((Cnst)it.Current).GetNumericValue().Numerator;
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
                                targetStateInfo.nIncommingTransitions++;
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

        void DeconstructEventSetDecl(FuncTerm term, out string eventSetName, out string machineName)
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

        AST<FuncTerm> MkZingFile(string filename, List<AST<Node>> elements)
        {
            return AddArgs(ZingData.App_File, Factory.Instance.MkCnst(filename), ConstructList(ZingData.App_Decls, elements));
        }
        #endregion

        private AST<Node> pTypeToZingType(PType t)
        {
            if (t is PNilType)
            {
                return Factory.Instance.MkCnst(SM_NULL);
            } else if (t is PPrimitiveType)
            {
                return ZingData.pTypeToZingType((t as PPrimitiveType).name);
            } else if (t is PTupleType || t is PNamedTupleType || t is PSeqType || t is PMapType)
            {
                return Factory.Instance.MkCnst(declaredTypes[t].zingType);
            }
            else if (t is PAnyType)
            {
                return Factory.Instance.MkCnst(SM_ARG_UNION);
            } else
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

        private string getZingContinuationCtxtType(string machine)
        {
            return machine + "_Continuation";
        }

        private string getZingStateEnumType(string machine)
        {
            return machine + "_State";
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
            else if (t is PIdType)
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