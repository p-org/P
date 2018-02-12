using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Microsoft.Formula.API;
using Microsoft.Formula.API.ASTQueries;
using Microsoft.Formula.API.Nodes;
using Microsoft.Formula.Common;

namespace Microsoft.Pc
{
    internal class PTranslation
    {
        public const string NullEvent = "null";
        public const string HaltEvent = "halt";

        public static AST<FuncTerm> PTypeNull = AddArgs(
            Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")),
            Factory.Instance.MkId("NULL"));

        public static AST<FuncTerm> PTypeBool = AddArgs(
            Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")),
            Factory.Instance.MkId("BOOL"));

        public static AST<FuncTerm> PTypeInt = AddArgs(
            Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")),
            Factory.Instance.MkId("INT"));

        public static AST<FuncTerm> PTypeEvent = AddArgs(
            Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")),
            Factory.Instance.MkId("EVENT"));

        public static AST<FuncTerm> PTypeMachine = AddArgs(
            Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")),
            Factory.Instance.MkId("MACHINE"));

        public static AST<FuncTerm> PTypeAny = AddArgs(
            Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")),
            Factory.Instance.MkId("ANY"));

        public Dictionary<AST<Node>, FuncTerm> aliasToTerm = new Dictionary<AST<Node>, FuncTerm>();
        public Dictionary<string, Dictionary<string, int>> allEnums = new Dictionary<string, Dictionary<string, int>>();
        public Dictionary<string, EventInfo> allEvents = new Dictionary<string, EventInfo>
        {
            [HaltEvent] = new EventInfo(1, false, PTypeNull.Node),
            [NullEvent] = new EventInfo(1, false, PTypeNull.Node)
        };
        public Dictionary<string, FunInfo> allGlobalFuns = new Dictionary<string, FunInfo>();
        public Dictionary<string, MachineInfo> allMachines = new Dictionary<string, MachineInfo>();
        public Dictionary<AST<Node>, string> anonFunToName = new Dictionary<AST<Node>, string>();

        public Compiler compiler;
        public HashSet<string> exportedEvents = new HashSet<string>();
        public Dictionary<string, LinkedList<AST<FuncTerm>>> factBins = new Dictionary<string, LinkedList<AST<FuncTerm>>>();
        public Dictionary<AST<Node>, string> funToFileName = new Dictionary<AST<Node>, string>();
        public Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo;
        public Dictionary<AST<FuncTerm>, Node> termToAlias = new Dictionary<AST<FuncTerm>, Node>();

        private readonly Dictionary<string, int> uniqIDCounters = new Dictionary<string, int>();

        public PTranslation(Compiler compiler, AST<Model> model, Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo)
        {
            this.compiler = compiler;
            this.idToSourceInfo = idToSourceInfo;

            model.FindAll(
                new NodePred[] {NodePredFactory.Instance.Star, NodePredFactory.Instance.MkPredicate(NodeKind.ModelFact)},
                (path, n) =>
                {
                    var mf = (ModelFact) n;
                    Id binding = mf.Binding;
                    var match = (FuncTerm) mf.Match;
                    string matchName = ((Id) match.Function).Name;
                    var matchAst = (AST<FuncTerm>) Factory.Instance.ToAST(match);
                    if (binding != null)
                    {
                        AST<Node> bindingAst = Factory.Instance.ToAST(binding);
                        aliasToTerm[bindingAst] = match;
                        termToAlias[matchAst] = binding;
                    }
                    GetBin(factBins, matchName).AddLast(matchAst);
                });

            GenerateProgramData();
        }

        public string SpanToString(Span span, string msg)
        {
            var flag = new Flag(SeverityKind.Error, span, msg, 0, span.Program);
            return ErrorReporter.FormatError(flag, compiler.Options).Replace(@"\", @"\\");
        }

        public Span LookupSpan(FuncTerm ft)
        {
            Node id = ft.Args.Last();
            int integerId;
            string file;
            ErrorReporter.FindIdFromFuncTerm(id as FuncTerm, out file, out integerId);
            return idToSourceInfo[file][integerId].entrySpan;
        }

        public string GetOwnerName(FuncTerm ft, int ownerIndex)
        {
            Node ownerArg = GetArgByIndex(ft, ownerIndex);
            switch (ownerArg.NodeKind)
            {
                case NodeKind.Id:
                {
                    Debug.Assert(((Id) ownerArg).Name == "NIL");
                    return null;
                }
                case NodeKind.Cnst: return ((Cnst) ownerArg).GetStringValue();
                default: throw new InvalidOperationException();
            }
        }

        public static string GetNameFromQualifiedName(string machineName, FuncTerm qualifiedName)
        {
            string stateName = machineName;
            while (qualifiedName != null)
            {
                stateName = stateName + "_" + GetName(qualifiedName, 0);
                qualifiedName = GetArgByIndex(qualifiedName, 1) as FuncTerm;
            }

            return stateName;
        }

        public static AST<FuncTerm> AddArgs(AST<FuncTerm> ft, params AST<Node>[] args)
        {
            return args.Aggregate(ft, (current, v) => Factory.Instance.AddArg(current, v));
        }

        public static AST<FuncTerm> AddArgs(AST<FuncTerm> ft, IEnumerable<AST<Node>> args)
        {
            return args.Aggregate(ft, (current, v) => Factory.Instance.AddArg(current, v));
        }

        public static Node GetArgByIndex(FuncTerm ft, int index)
        {
            Contract.Requires(index >= 0 && index < ft.Args.Count);

            var i = 0;
            foreach (Node a in ft.Args)
            {
                if (i == index)
                {
                    return a;
                }

                ++i;
            }

            throw new InvalidOperationException();
        }

        public static string GetName(FuncTerm ft, int nameIndex)
        {
            return ((Cnst) GetArgByIndex(ft, nameIndex)).GetStringValue();
        }

        public static string GetPrintedNameFromQualifiedName(FuncTerm qualifiedName)
        {
            string stateName = GetName(qualifiedName, 0);
            while (true)
            {
                qualifiedName = GetArgByIndex(qualifiedName, 1) as FuncTerm;
                if (qualifiedName == null)
                {
                    break;
                }

                stateName = stateName + "." + GetName(qualifiedName, 0);
            }

            return stateName;
        }

        public static LinkedList<AST<FuncTerm>> GetBin(Dictionary<string, LinkedList<AST<FuncTerm>>> factBins, string name)
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

        public string GetUnique(string prefix)
        {
            if (!uniqIDCounters.ContainsKey(prefix))
            {
                uniqIDCounters[prefix] = 0;
            }

            int ret = uniqIDCounters[prefix];
            uniqIDCounters[prefix]++;
            return prefix + '_' + ret;
        }

        private void GenerateProgramData()
        {
            LinkedList<AST<FuncTerm>> terms = GetBin(factBins, "FileInfo");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    Node fun = it.Current;
                    it.MoveNext();
                    var fileInfo = it.Current as Cnst;
                    string fileName = null;
                    if (fileInfo != null)
                    {
                        fileName = fileInfo.GetStringValue();
                        if (compiler.Options.shortFileNames)
                        {
                            fileName = Path.GetFileName(fileName);
                        }
                    }
                    funToFileName[Factory.Instance.ToAST(fun)] = fileName;
                }
            }

            terms = GetBin(factBins, "EventDecl");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string name = ((Cnst) it.Current).GetStringValue();
                    it.MoveNext();
                    Node bound = it.Current;
                    it.MoveNext();
                    var payloadType = (FuncTerm) (it.Current.NodeKind == NodeKind.Id ? PTypeNull.Node : it.Current);
                    if (bound.NodeKind == NodeKind.Id)
                    {
                        allEvents[name] = new EventInfo(payloadType);
                    }
                    else
                    {
                        var ft = (FuncTerm) bound;
                        var maxInstances = (int) ((Cnst) GetArgByIndex(ft, 0)).GetNumericValue().Numerator;
                        bool maxInstancesAssumed = ((Id) ft.Function).Name == "AssumeMaxInstances";
                        allEvents[name] = new EventInfo(maxInstances, maxInstancesAssumed, payloadType);
                    }
                }
            }

            terms = GetBin(factBins, "EnumTypeDef");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string name = ((Cnst) it.Current).GetStringValue();
                    it.MoveNext();
                    var strIter = it.Current as FuncTerm;
                    it.MoveNext();
                    var valIter = it.Current as FuncTerm;
                    var constants = new Dictionary<string, int>();
                    if (valIter == null)
                    {
                        var val = 0;
                        while (strIter != null)
                        {
                            string constant = (GetArgByIndex(strIter, 0) as Cnst).GetStringValue();
                            constants[constant] = val;
                            strIter = GetArgByIndex(strIter, 1) as FuncTerm;
                            val++;
                        }
                    }
                    else
                    {
                        while (strIter != null)
                        {
                            string constant = (GetArgByIndex(strIter, 0) as Cnst).GetStringValue();
                            Rational val = (GetArgByIndex(valIter, 0) as Cnst).GetNumericValue();
                            constants[constant] = (int) val.Numerator;
                            strIter = GetArgByIndex(strIter, 1) as FuncTerm;
                            valIter = GetArgByIndex(valIter, 1) as FuncTerm;
                        }
                    }

                    allEnums[name] = constants;
                }
            }

            terms = GetBin(factBins, "MachineDecl");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string machineName = ((Cnst) it.Current).GetStringValue();
                    allMachines[machineName] = new MachineInfo();
                }
            }

            terms = GetBin(factBins, "MachineKind");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string machineName = ((Cnst) it.Current).GetStringValue();
                    it.MoveNext();
                    allMachines[machineName].type = ((Id) it.Current).Name;
                }
            }

            terms = GetBin(factBins, "MachineCard");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string machineName = ((Cnst) it.Current).GetStringValue();
                    it.MoveNext();
                    Node bound = it.Current;
                    if (bound.NodeKind != NodeKind.Id)
                    {
                        var ft = (FuncTerm) bound;
                        allMachines[machineName].maxQueueSize = (int) ((Cnst) GetArgByIndex(ft, 0)).GetNumericValue().Numerator;
                        allMachines[machineName].maxQueueSizeAssumed = ((Id) ft.Function).Name == "AssumeMaxInstances";
                    }
                }
            }

            terms = GetBin(factBins, "MachineStart");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string machineName = ((Cnst) it.Current).GetStringValue();
                    it.MoveNext();
                    allMachines[machineName].initStateName = GetNameFromQualifiedName(machineName, (FuncTerm) it.Current);
                }
            }

            terms = GetBin(factBins, "ObservesDecl");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string machineName = ((Cnst) it.Current).GetStringValue();
                    it.MoveNext();
                    allMachines[machineName].observesEvents.Add(((Cnst) it.Current).GetStringValue());
                }
            }

            terms = GetBin(factBins, "MachineReceives");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string machineName = ((Cnst) it.Current).GetStringValue();
                    it.MoveNext();
                    string eventName;
                    if (it.Current.NodeKind == NodeKind.Id)
                    {
                        string name = ((Id) it.Current).Name;
                        if (name == "ALL")
                        {
                            allMachines[machineName].receiveSet = null;
                        }
                        else
                        {
                            eventName = HaltEvent;
                            allMachines[machineName].receiveSet.Add(eventName);
                        }
                    }
                    else
                    {
                        eventName = ((Cnst) it.Current).GetStringValue();
                        allMachines[machineName].receiveSet.Add(eventName);
                    }
                }
            }

            terms = GetBin(factBins, "MachineSends");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string machineName = ((Cnst) it.Current).GetStringValue();
                    it.MoveNext();
                    string eventName;
                    if (it.Current.NodeKind == NodeKind.Id)
                    {
                        string name = ((Id) it.Current).Name;
                        if (name == "ALL")
                        {
                            allMachines[machineName].sendsSet = null;
                        }
                        else
                        {
                            eventName = HaltEvent;
                            allMachines[machineName].sendsSet.Add(eventName);
                        }
                    }
                    else
                    {
                        eventName = ((Cnst) it.Current).GetStringValue();
                        allMachines[machineName].sendsSet.Add(eventName);
                    }
                }
            }

            terms = GetBin(factBins, "VarDecl");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string varName = ((Cnst) it.Current).GetStringValue();
                    it.MoveNext();
                    string machineName = ((Cnst) it.Current).GetStringValue();
                    Dictionary<string, VariableInfo> varTable = allMachines[machineName].localVariableToVarInfo;
                    it.MoveNext();
                    var type = (FuncTerm) it.Current;
                    varTable[varName] = new VariableInfo(type);
                }
            }

            var translatedBody = new Dictionary<AST<Node>, Node>();
            terms = GetBin(factBins, "TranslatedBody");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    AST<Node> cntxt = Factory.Instance.ToAST(it.Current);
                    it.MoveNext();
                    Node newStmt = it.Current;
                    translatedBody[cntxt] = newStmt;
                }
            }

            terms = GetBin(factBins, "FunDecl");
            foreach (AST<FuncTerm> term in terms)
            {
                AST<Node> termAlias = Factory.Instance.ToAST(termToAlias[term]);
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string funName = ((Cnst) it.Current).GetStringValue();
                    it.MoveNext();
                    var owner = it.Current as Cnst;
                    it.MoveNext();
                    var parameters = it.Current as FuncTerm;
                    it.MoveNext();
                    AST<FuncTerm> returnTypeName = it.Current is Id ? PTypeNull : (AST<FuncTerm>) Factory.Instance.ToAST(it.Current);
                    it.MoveNext();
                    var locals = it.Current as FuncTerm;
                    it.MoveNext();
                    Node body = null; //body is empty
                    if (translatedBody.ContainsKey(termAlias))
                    {
                        body = translatedBody[termAlias];
                    }
                    
                    var funInfo = new FunInfo(false, parameters, returnTypeName, locals, body);
                    if (owner != null)
                    {
                        string machineName = owner.GetStringValue();
                        MachineInfo machineInfo = allMachines[machineName];
                        machineInfo.funNameToFunInfo[funName] = funInfo;
                    }
                    else
                    {
                        allGlobalFuns[funName] = funInfo;
                    }
                }
            }

            terms = GetBin(factBins, "FunProtoDecl");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string funName = ((Cnst) it.Current).GetStringValue();
                    it.MoveNext();
                    var parameters = it.Current as FuncTerm;
                    it.MoveNext();
                    AST<FuncTerm> returnTypeName = it.Current is Id ? PTypeNull : (AST<FuncTerm>) Factory.Instance.ToAST(it.Current);
                    if (!allGlobalFuns.ContainsKey(funName))
                    {
                        allGlobalFuns.Add(funName, new FunInfo(parameters, returnTypeName));
                    }
                }
            }

            var anonFunCounter = new Dictionary<string, int>();
            var anonFunCounterStatic = 0;
            foreach (string x in allMachines.Keys)
            {
                anonFunCounter[x] = 0;
            }

            terms = GetBin(factBins, "AnonFunDecl");
            foreach (AST<FuncTerm> term in terms)
            {
                AST<Node> termAlias = Factory.Instance.ToAST(termToAlias[term]);
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var ownerMachineName = it.Current as Cnst;
                    it.MoveNext();
                    var ownerFunName = it.Current as Cnst;
                    it.MoveNext();
                    var locals = it.Current as FuncTerm;
                    it.MoveNext();
                    Node body = translatedBody[termAlias];
                    it.MoveNext();
                    var envVars = it.Current as FuncTerm;
                    if (ownerMachineName == null)
                    {
                        string funName = "AnonFunStatic" + anonFunCounterStatic;
                        allGlobalFuns[funName] = new FunInfo(true, envVars, PTypeNull, locals, body);
                        anonFunToName[termAlias] = funName;
                        anonFunCounterStatic++;
                    }
                    else
                    {
                        string machineName = ownerMachineName.GetStringValue();
                        MachineInfo machineInfo = allMachines[machineName];
                        string funName = "AnonFun" + anonFunCounter[machineName];
                        machineInfo.funNameToFunInfo[funName] = new FunInfo(true, envVars, PTypeNull, locals, body);
                        anonFunToName[termAlias] = funName;
                        anonFunCounter[machineName]++;
                    }
                }
            }

            terms = GetBin(factBins, "StateDecl");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var qualifiedStateName = (FuncTerm) it.Current;
                    it.MoveNext();
                    string ownerName = ((Cnst) it.Current).GetStringValue();
                    string stateName = GetNameFromQualifiedName(ownerName, qualifiedStateName);
                    it.MoveNext();
                    string entryActionName = it.Current.NodeKind == NodeKind.Cnst
                        ? ((Cnst) it.Current).GetStringValue()
                        : anonFunToName[Factory.Instance.ToAST(it.Current)];
                    it.MoveNext();
                    string exitFunName = it.Current.NodeKind == NodeKind.Cnst
                        ? ((Cnst) it.Current).GetStringValue()
                        : anonFunToName[Factory.Instance.ToAST(it.Current)];
                    it.MoveNext();
                    var temperature = StateTemperature.WARM;
                    string t = ((Id) it.Current).Name;
                    if (t == "HOT")
                    {
                        temperature = StateTemperature.HOT;
                    }
                    else if (t == "COLD")
                    {
                        temperature = StateTemperature.COLD;
                    }
                    Dictionary<string, StateInfo> stateTable = allMachines[ownerName].stateNameToStateInfo;
                    stateTable[stateName] = new StateInfo(
                        ownerName,
                        entryActionName,
                        exitFunName,
                        temperature,
                        GetPrintedNameFromQualifiedName(qualifiedStateName));
                }
            }

            terms = GetBin(factBins, "TransDecl");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var stateDecl = (FuncTerm) it.Current;
                    var qualifiedStateName = (FuncTerm) GetArgByIndex(stateDecl, 0);
                    string stateOwnerMachineName = GetName(stateDecl, 1);
                    string stateName = GetNameFromQualifiedName(stateOwnerMachineName, qualifiedStateName);
                    StateInfo stateTable = allMachines[stateOwnerMachineName].stateNameToStateInfo[stateName];
                    it.MoveNext();
                    string eventName;
                    if (it.Current.NodeKind == NodeKind.Id)
                    {
                        string name = ((Id) it.Current).Name;
                        if (name == "NULL")
                        {
                            eventName = NullEvent;
                            stateTable.hasNullTransition = true;
                        }
                        else
                        {
                            // name == "HALT"
                            eventName = HaltEvent;
                        }
                    }
                    else
                    {
                        eventName = ((Cnst) it.Current).GetStringValue();
                    }
                    it.MoveNext();
                    string targetStateName = GetNameFromQualifiedName(stateOwnerMachineName, (FuncTerm) it.Current);
                    it.MoveNext();
                    if (it.Current.NodeKind == NodeKind.Cnst)
                    {
                        string exitFunName = ((Cnst) it.Current).GetStringValue();
                        stateTable.transitions[eventName] = new TransitionInfo(targetStateName, exitFunName);
                    }
                    else if (it.Current.NodeKind == NodeKind.Id && (it.Current as Id).Name == "PUSH")
                    {
                        stateTable.transitions[eventName] = new TransitionInfo(targetStateName);
                    }
                    else
                    {
                        string exitFunName = anonFunToName[Factory.Instance.ToAST(it.Current)];
                        stateTable.transitions[eventName] = new TransitionInfo(targetStateName, exitFunName);
                    }
                }
            }

            terms = GetBin(factBins, "DoDecl");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var stateDecl = (FuncTerm) it.Current;
                    var qualifiedStateName = (FuncTerm) GetArgByIndex(stateDecl, 0);
                    string stateOwnerMachineName = GetName(stateDecl, 1);
                    string stateName = GetNameFromQualifiedName(stateOwnerMachineName, qualifiedStateName);
                    StateInfo stateTable = allMachines[stateOwnerMachineName].stateNameToStateInfo[stateName];
                    it.MoveNext();
                    string eventName;
                    if (it.Current.NodeKind == NodeKind.Id)
                    {
                        string name = ((Id) it.Current).Name;
                        eventName = name == "NULL" ? NullEvent : HaltEvent;
                    }
                    else
                    {
                        eventName = ((Cnst) it.Current).GetStringValue();
                    }
                    it.MoveNext();
                    Node action = it.Current;
                    if (action.NodeKind == NodeKind.Cnst)
                    {
                        stateTable.dos[eventName] = ((Cnst) action).GetStringValue();
                    }
                    else if (action.NodeKind == NodeKind.Id && (action as Id).Name == "DEFER")
                    {
                        stateTable.deferredEvents.Add(eventName);
                    }
                    else if (action.NodeKind == NodeKind.Id && (action as Id).Name == "IGNORE")
                    {
                        stateTable.dos[eventName] = "ignore";
                    }
                    else
                    {
                        stateTable.dos[eventName] = anonFunToName[Factory.Instance.ToAST(action)];
                    }
                }
            }

            terms = GetBin(factBins, "Annotation");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    FuncTerm annotationContext = it.Current.NodeKind == NodeKind.Id
                        ? aliasToTerm[Factory.Instance.ToAST(it.Current)]
                        : (FuncTerm) it.Current;
                    string annotationContextKind = ((Id) annotationContext.Function).Name;
                    if (annotationContextKind != "FunDecl")
                    {
                        continue;
                    }

                    string ownerName = GetOwnerName(annotationContext, 1);
                    string funName = GetName(annotationContext, 0);
                    it.MoveNext();
                    string annotation = ((Cnst) it.Current).GetStringValue();
                    it.MoveNext();
                    if (annotation == "invokescheduler")
                    {
                        if (ownerName == null)
                        {
                            allGlobalFuns[funName].invokeSchedulerFuns.Add(it.Current);
                        }
                        else
                        {
                            allMachines[ownerName].funNameToFunInfo[funName].invokeSchedulerFuns.Add(it.Current);
                        }
                    }
                    else if (annotation == "printvalue")
                    {
                        var indexCnst = it.Current as Cnst;
                        if (indexCnst != null)
                        {
                            string arg = indexCnst.GetStringValue();
                            if (ownerName == null)
                            {
                                allGlobalFuns[funName].printArgs.Add(arg);
                            }
                            else
                            {
                                allMachines[ownerName].funNameToFunInfo[funName].printArgs.Add(arg);
                            }
                        }
                    }
                    else if (annotation == "invokeplugin")
                    {
                        if (ownerName == null)
                        {
                            allGlobalFuns[funName].invokePluginFuns.Add(it.Current);
                        }
                        else
                        {
                            allMachines[ownerName].funNameToFunInfo[funName].invokePluginFuns.Add(it.Current);
                        }
                    }
                }
            }

            terms = GetBin(factBins, "ExportedEvent");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string eventName = ((Cnst) it.Current).GetStringValue();
                    exportedEvents.Add(eventName);
                }
            }

            terms = GetBin(factBins, "MaxNumLocals");
            foreach (AST<FuncTerm> term in terms)
            {
                using (IEnumerator<Node> it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    AST<Node> typingContextAlias = Factory.Instance.ToAST(it.Current);
                    FuncTerm typingContext = aliasToTerm[typingContextAlias];
                    string typingContextKind = ((Id) typingContext.Function).Name;
                    it.MoveNext();
                    var maxNumLocals = (int) ((Cnst) it.Current).GetNumericValue().Numerator;

                    if (typingContextKind == "FunDecl")
                    {
                        string ownerName = GetOwnerName(typingContext, 1);
                        string funName = GetName(typingContext, 0);
                        if (ownerName == null)
                        {
                            allGlobalFuns[funName].maxNumLocals = maxNumLocals;
                        }
                        else
                        {
                            allMachines[ownerName].funNameToFunInfo[funName].maxNumLocals = maxNumLocals;
                        }
                    }
                    else
                    {
                        // typingContextKind == "AnonFunDecl"
                        string ownerName = GetOwnerName(typingContext, 0);
                        string funName = anonFunToName[typingContextAlias];
                        if (ownerName == null)
                        {
                            allGlobalFuns[funName].maxNumLocals = maxNumLocals;
                        }
                        else
                        {
                            allMachines[ownerName].funNameToFunInfo[funName].maxNumLocals = maxNumLocals;
                        }
                    }
                }
            }

            if (compiler.Options.liveness != LivenessOption.None)
            {
                foreach (string machineName in allMachines.Keys)
                {
                    if (!allMachines[machineName].IsSpec)
                    {
                        continue;
                    }

                    MachineInfo machineInfo = allMachines[machineName];
                    var initialSet = new List<string>();
                    foreach (string stateName in ComputeReachableStates(machineName, machineInfo, new[] {machineInfo.initStateName}))
                    {
                        if (machineInfo.stateNameToStateInfo[stateName].IsWarm)
                        {
                            continue;
                        }

                        if (machineInfo.stateNameToStateInfo[stateName].IsHot)
                        {
                            machineInfo.specType = SpecType.FINALLY;
                            continue;
                        }

                        initialSet.Add(stateName);
                    }
                    foreach (string stateName in ComputeReachableStates(machineName, machineInfo, initialSet))
                    {
                        if (machineInfo.stateNameToStateInfo[stateName].IsHot)
                        {
                            machineInfo.specType = SpecType.REPEATEDLY;
                            break;
                        }
                    }
                }

                if (allMachines.Values.All(x => !x.IsSpec || x.specType == SpecType.SAFETY))
                {
                    compiler.Options.liveness = LivenessOption.None;
                }
            }
        }

        private static IEnumerable<string> ComputeGotoTargets(string machineName, Node node)
        {
            var targets = new HashSet<string>();
            var searchStack = new Stack<Node>();
            searchStack.Push(node);
            while (searchStack.Count != 0)
            {
                var topOfStack = searchStack.Pop() as FuncTerm;
                if (topOfStack == null)
                {
                    continue;
                }

                string name = ((Id) topOfStack.Function).Name;
                switch (name)
                {
                    case "NewStmt":
                    case "Raise":
                    case "Send":
                    case "Announce":
                    case "FunStmt":
                    case "NulStmt":
                    case "BinStmt":
                    case "Return":
                    case "Assert":
                    case "Print":
                    case "Receive": // receive is not allowed in spec machines
                        continue;
                    case "Goto":
                        string targetName = GetNameFromQualifiedName(machineName, GetArgByIndex(topOfStack, 0) as FuncTerm);
                        targets.Add(targetName);
                        continue;
                    case "While":
                        searchStack.Push(GetArgByIndex(topOfStack, 1));
                        continue;
                    case "Ite":
                        searchStack.Push(GetArgByIndex(topOfStack, 1));
                        searchStack.Push(GetArgByIndex(topOfStack, 2));
                        continue;
                    case "Seq":
                        searchStack.Push(GetArgByIndex(topOfStack, 0));
                        searchStack.Push(GetArgByIndex(topOfStack, 1));
                        continue;
                }
            }

            return targets;
        }

        private static IEnumerable<string> ComputeGotoTargets(string machineName, MachineInfo machineInfo, StateInfo stateInfo)
        {
            var targets = new HashSet<string>();
            targets.UnionWith(ComputeGotoTargets(machineName, machineInfo.funNameToFunInfo[stateInfo.entryActionName].body));
            foreach (string actionName in stateInfo.dos.Values)
            {
                targets.UnionWith(ComputeGotoTargets(machineName, machineInfo.funNameToFunInfo[actionName].body));
            }

            return targets;
        }

        private static IEnumerable<string> ComputeReachableStates(string machineName, MachineInfo machineInfo, IEnumerable<string> initialSet)
        {
            var dfsStack = new Stack<string>();
            var visitedStates = new HashSet<string>();
            foreach (string stateName in initialSet)
            {
                dfsStack.Push(stateName);
                visitedStates.Add(stateName);
            }
            while (dfsStack.Count > 0)
            {
                string curState = dfsStack.Pop();
                StateInfo curStateInfo = machineInfo.stateNameToStateInfo[curState];
                foreach (string e in curStateInfo.transitions.Keys)
                {
                    string nextState = curStateInfo.transitions[e].target;
                    if (visitedStates.Contains(nextState))
                    {
                        continue;
                    }

                    visitedStates.Add(nextState);
                    dfsStack.Push(nextState);
                }
                foreach (string nextState in ComputeGotoTargets(machineName, machineInfo, curStateInfo))
                {
                    if (visitedStates.Contains(nextState))
                    {
                        continue;
                    }

                    visitedStates.Add(nextState);
                    dfsStack.Push(nextState);
                }
            }

            return visitedStates;
        }
    }
}