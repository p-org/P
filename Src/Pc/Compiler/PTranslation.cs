using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Diagnostics;

using Microsoft.Formula.API;
using Microsoft.Formula.API.ASTQueries;
using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc
{
    internal class PTranslation
    {
        public const string NullEvent = "null";
        public const string HaltEvent = "halt";

        public static AST<FuncTerm> PTypeNull = AddArgs(Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")), Factory.Instance.MkId("NULL"));
        public static AST<FuncTerm> PTypeBool = AddArgs(Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")), Factory.Instance.MkId("BOOL"));
        public static AST<FuncTerm> PTypeInt = AddArgs(Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")), Factory.Instance.MkId("INT"));
        public static AST<FuncTerm> PTypeEvent = AddArgs(Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")), Factory.Instance.MkId("EVENT"));
        public static AST<FuncTerm> PTypeMachine = AddArgs(Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")), Factory.Instance.MkId("MACHINE"));
        public static AST<FuncTerm> PTypeAny = AddArgs(Factory.Instance.MkFuncTerm(Factory.Instance.MkId("BaseType")), Factory.Instance.MkId("ANY"));

        public string SpanToString(Span span, string msg)
        {
            Flag flag = new Flag(SeverityKind.Error, span, msg, 0, span.Program);
            return ErrorReporter.FormatError(flag, compiler.Options).Replace(@"\", @"\\");
        }

        public Span LookupSpan(FuncTerm ft)
        {
            string name = ((Id)ft.Function).Name;
            var id = ft.Args.Last();
            int integerId;
            string file;
            ErrorReporter.FindIdFromFuncTerm(id as FuncTerm, out file, out integerId);
            return idToSourceInfo[file][integerId].entrySpan;
        }

        public string GetOwnerName(FuncTerm ft, int ownerIndex)
        {
            var ownerArg = GetArgByIndex(ft, ownerIndex);
            switch (ownerArg.NodeKind)
            {
                case NodeKind.Id:
                    {
                        Debug.Assert(((Id)ownerArg).Name == "NIL");
                        return null;
                    }
                case NodeKind.Cnst:
                    return ((Cnst)ownerArg).GetStringValue();
                default:
                    throw new InvalidOperationException();
            }
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

        public Compiler compiler;
        public Dictionary<string, LinkedList<AST<FuncTerm>>> factBins;
        public Dictionary<AST<Node>, FuncTerm> aliasToTerm;
        public Dictionary<AST<FuncTerm>, Node> termToAlias;
        public Dictionary<AST<Node>, string> funToFileName;
        public Dictionary<string, EventInfo> allEvents;
        public Dictionary<string, Dictionary<string, int>> allEnums;
        public Dictionary<string, MachineInfo> allMachines;
        public HashSet<string> exportedEvents;
        public Dictionary<string, FunInfo> allGlobalFuns;
        public Dictionary<AST<Node>, string> anonFunToName;
        public Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo;

        public PTranslation(Compiler compiler, AST<Model> model, Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo)
        {
            this.compiler = compiler;
            this.idToSourceInfo = idToSourceInfo;
            this.factBins = new Dictionary<string, LinkedList<AST<FuncTerm>>>();
            this.aliasToTerm = new Dictionary<AST<Node>, FuncTerm>();
            this.termToAlias = new Dictionary<AST<FuncTerm>, Node>();
            model.FindAll(
                new NodePred[]
                {
                        NodePredFactory.Instance.Star,
                        NodePredFactory.Instance.MkPredicate(NodeKind.ModelFact)
                },
                (path, n) =>
                {
                    var mf = (ModelFact)n;
                    Id binding = mf.Binding;
                    FuncTerm match = (FuncTerm)mf.Match;
                    string matchName = ((Id)match.Function).Name;
                    var matchAst = (AST<FuncTerm>)Factory.Instance.ToAST(match);
                    if (binding != null)
                    {
                        var bindingAst = Factory.Instance.ToAST(binding);
                        aliasToTerm[bindingAst] = match;
                        termToAlias[matchAst] = binding;
                    }
                    GetBin(factBins, matchName).AddLast(matchAst);
                });
            GenerateProgramData(model);
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

        Dictionary<string, int> uniqIDCounters = new Dictionary<string, int>();
        public string GetUnique(string prefix)
        {
            if (!uniqIDCounters.ContainsKey(prefix))
                uniqIDCounters[prefix] = 0;

            var ret = uniqIDCounters[prefix];
            uniqIDCounters[prefix]++;
            return prefix + '_' + ret;
        }

        private void GenerateProgramData(AST<Model> model)
        {
            funToFileName = new Dictionary<AST<Node>, string>();
            allEvents = new Dictionary<string, EventInfo>();
            exportedEvents = new HashSet<string>();
            allEnums = new Dictionary<string, Dictionary<string, int>>();
            allEvents[HaltEvent] = new EventInfo(1, false, PTypeNull.Node);
            allEvents[NullEvent] = new EventInfo(1, false, PTypeNull.Node);
            allMachines = new Dictionary<string, MachineInfo>();
            allGlobalFuns = new Dictionary<string, FunInfo>();

            LinkedList<AST<FuncTerm>> terms;

            terms = GetBin(factBins, "FileInfo");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var fun = it.Current;
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

            terms = GetBin(factBins, "EnumTypeDef");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    FuncTerm strIter = it.Current as FuncTerm;
                    it.MoveNext();
                    FuncTerm valIter = it.Current as FuncTerm;
                    var constants = new Dictionary<string, int>();
                    if (valIter == null)
                    {
                        var val = 0;
                        while (strIter != null)
                        {
                            var constant = (GetArgByIndex(strIter, 0) as Cnst).GetStringValue();
                            constants[constant] = val;
                            strIter = GetArgByIndex(strIter, 1) as FuncTerm;
                            val++;
                        }
                    }
                    else
                    {
                        while (strIter != null)
                        {
                            var constant = (GetArgByIndex(strIter, 0) as Cnst).GetStringValue();
                            var val = (GetArgByIndex(valIter, 0) as Cnst).GetNumericValue();
                            constants[constant] = (int)val.Numerator;
                            strIter = GetArgByIndex(strIter, 1) as FuncTerm;
                            valIter = GetArgByIndex(valIter, 1) as FuncTerm;
                        }
                    }
                    allEnums[name] = constants;
                }
            }

            terms = GetBin(factBins, "MachineDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var machineName = ((Cnst)it.Current).GetStringValue();
                    allMachines[machineName] = new MachineInfo();
                }
            }

            terms = GetBin(factBins, "MachineKind");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var machineName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    allMachines[machineName].type = ((Id)it.Current).Name;
                }
            }

            terms = GetBin(factBins, "MachineCard");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var machineName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var bound = it.Current;
                    if (bound.NodeKind != NodeKind.Id)
                    {
                        var ft = (FuncTerm)bound;
                        allMachines[machineName].maxQueueSize = (int)((Cnst)GetArgByIndex(ft, 0)).GetNumericValue().Numerator;
                        allMachines[machineName].maxQueueSizeAssumed = ((Id)ft.Function).Name == "AssumeMaxInstances";
                    }
                }
            }

            terms = GetBin(factBins, "MachineStart");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var machineName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    allMachines[machineName].initStateName = GetNameFromQualifiedName(machineName, (FuncTerm)it.Current);
                }
            }

            terms = GetBin(factBins, "ObservesDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var machineName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    allMachines[machineName].observesEvents.Add(((Cnst)it.Current).GetStringValue());
                }
            }

            terms = GetBin(factBins, "MachineReceives");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var machineName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    string eventName;
                    if (it.Current.NodeKind == NodeKind.Id)
                    {
                        var name = ((Id)it.Current).Name;
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
                        eventName = ((Cnst)it.Current).GetStringValue();
                        allMachines[machineName].receiveSet.Add(eventName);
                    }
                    
                    
                }
            }

            terms = GetBin(factBins, "MachineSends");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var machineName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    string eventName;
                    if (it.Current.NodeKind == NodeKind.Id)
                    {
                        var name = ((Id)it.Current).Name;
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
                        eventName = ((Cnst)it.Current).GetStringValue();
                        allMachines[machineName].sendsSet.Add(eventName);
                    }
                }
            }

            terms = GetBin(factBins, "VarDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var varName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var machineName = ((Cnst)it.Current).GetStringValue();
                    var varTable = allMachines[machineName].localVariableToVarInfo;
                    it.MoveNext();
                    var type = (FuncTerm)it.Current;
                    varTable[varName] = new VariableInfo(type);
                }
            }

            Dictionary<AST<Node>, Node> translatedBody = new Dictionary<AST<Node>, Node>();
            terms = GetBin(factBins, "TranslatedBody");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var cntxt = Factory.Instance.ToAST(it.Current);
                    it.MoveNext();
                    var newStmt = it.Current;
                    translatedBody[cntxt] = newStmt;
                }
            }

            terms = GetBin(factBins, "FunDecl");
            foreach (var term in terms)
            {
                var termAlias = Factory.Instance.ToAST(termToAlias[term]);
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string funName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var owner = it.Current as Cnst;
                    it.MoveNext();
                    var isModel = ((Id)it.Current).Name == "MODEL";
                    it.MoveNext();
                    var parameters = it.Current as FuncTerm;
                    it.MoveNext();
                    var returnTypeName = it.Current is Id ? PTypeNull : (AST<FuncTerm>)Factory.Instance.ToAST(it.Current);
                    it.MoveNext();
                    var locals = it.Current as FuncTerm;
                    it.MoveNext();
                    var body = translatedBody[termAlias];
                    var funInfo = new FunInfo(false, parameters, returnTypeName, locals, body);
                    if (owner != null)
                    {
                        var machineName = owner.GetStringValue();
                        var machineInfo = allMachines[machineName];
                        machineInfo.funNameToFunInfo[funName] = funInfo;
                    }
                    else
                    {
                        allGlobalFuns[funName] = funInfo;
                    }
                }
            }

            terms = GetBin(factBins, "FunProtoDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    string funName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var parameters = it.Current as FuncTerm;
                    it.MoveNext();
                    var returnTypeName = it.Current is Id ? PTypeNull : (AST<FuncTerm>)Factory.Instance.ToAST(it.Current);
                    if (!allGlobalFuns.ContainsKey(funName))
                    {
                        allGlobalFuns.Add(funName, new FunInfo(parameters, returnTypeName));
                    }
                }
            }

            this.anonFunToName = new Dictionary<AST<Node>, string>();
            var anonFunCounter = new Dictionary<string, int>();
            int anonFunCounterStatic = 0;
            foreach (var x in allMachines.Keys)
            {
                anonFunCounter[x] = 0;
            }
            terms = GetBin(factBins, "AnonFunDecl");
            foreach (var term in terms)
            {
                var termAlias = Factory.Instance.ToAST(termToAlias[term]);
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var ownerMachineName = it.Current as Cnst;
                    it.MoveNext();
                    var ownerFunName = it.Current as Cnst;
                    it.MoveNext();
                    var locals = it.Current as FuncTerm;
                    it.MoveNext();
                    var body = translatedBody[termAlias];
                    it.MoveNext();
                    var envVars = it.Current as FuncTerm;
                    if (ownerMachineName == null)
                    {
                        var funName = "AnonFunStatic" + anonFunCounterStatic;
                        allGlobalFuns[funName] = new FunInfo(true, envVars, PToZing.PTypeNull, locals, body);
                        anonFunToName[termAlias] = funName;
                        anonFunCounterStatic++;
                    }
                    else
                    {
                        var machineName = ownerMachineName.GetStringValue();
                        var machineInfo = allMachines[machineName];
                        var funName = "AnonFun" + anonFunCounter[machineName];
                        machineInfo.funNameToFunInfo[funName] = new FunInfo(true, envVars, PToZing.PTypeNull, locals, body);
                        anonFunToName[termAlias] = funName;
                        anonFunCounter[machineName]++;
                    }
                }
            }

            terms = GetBin(factBins, "StateDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var qualifiedStateName = (FuncTerm)it.Current;
                    it.MoveNext();
                    var ownerName = ((Cnst)it.Current).GetStringValue();
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
                    var temperature = StateTemperature.WARM;
                    var t = ((Id)it.Current).Name;
                    if (t == "HOT")
                    {
                        temperature = StateTemperature.HOT;
                    }
                    else if (t == "COLD")
                    {
                        temperature = StateTemperature.COLD;
                    }
                    var stateTable = allMachines[ownerName].stateNameToStateInfo;
                    stateTable[stateName] = new StateInfo(ownerName, entryActionName, exitFunName, temperature, GetPrintedNameFromQualifiedName(qualifiedStateName));
                }
            }

            terms = GetBin(factBins, "TransDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var stateDecl = (FuncTerm)it.Current;
                    var qualifiedStateName = (FuncTerm)GetArgByIndex(stateDecl, 0);
                    var stateOwnerMachineName = GetName(stateDecl, 1);
                    var stateName = GetNameFromQualifiedName(stateOwnerMachineName, qualifiedStateName);
                    var stateTable = allMachines[stateOwnerMachineName].stateNameToStateInfo[stateName];
                    it.MoveNext();
                    string eventName;
                    if (it.Current.NodeKind == NodeKind.Id)
                    {
                        var name = ((Id)it.Current).Name;
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
                        eventName = ((Cnst)it.Current).GetStringValue();
                    }
                    it.MoveNext();
                    var targetStateName = GetNameFromQualifiedName(stateOwnerMachineName, (FuncTerm)it.Current);
                    it.MoveNext();
                    if (it.Current.NodeKind == NodeKind.Cnst)
                    {
                        var exitFunName = ((Cnst)it.Current).GetStringValue();
                        stateTable.transitions[eventName] = new TransitionInfo(targetStateName, exitFunName);
                    }
                    else if (it.Current.NodeKind == NodeKind.Id && (it.Current as Id).Name == "PUSH")
                    {
                        stateTable.transitions[eventName] = new TransitionInfo(targetStateName);
                    }
                    else
                    {
                        var exitFunName = anonFunToName[Factory.Instance.ToAST(it.Current)];
                        stateTable.transitions[eventName] = new TransitionInfo(targetStateName, exitFunName);
                    }
                }
            }

            terms = GetBin(factBins, "DoDecl");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var stateDecl = (FuncTerm)it.Current;
                    var qualifiedStateName = (FuncTerm)GetArgByIndex(stateDecl, 0);
                    var stateOwnerMachineName = GetName(stateDecl, 1);
                    var stateName = GetNameFromQualifiedName(stateOwnerMachineName, qualifiedStateName);
                    var stateTable = allMachines[stateOwnerMachineName].stateNameToStateInfo[stateName];
                    it.MoveNext();
                    string eventName;
                    if (it.Current.NodeKind == NodeKind.Id)
                    {
                        var name = ((Id)it.Current).Name;
                        if (name == "NULL")
                        {
                            eventName = NullEvent;
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
                    var action = it.Current;
                    if (action.NodeKind == NodeKind.Cnst)
                    {
                        stateTable.dos[eventName] = ((Cnst)action).GetStringValue();
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
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    FuncTerm annotationContext = 
                        it.Current.NodeKind == NodeKind.Id 
                        ? aliasToTerm[Factory.Instance.ToAST(it.Current)] 
                        : (FuncTerm)it.Current;
                    string annotationContextKind = ((Id)annotationContext.Function).Name;
                    if (annotationContextKind != "FunDecl") continue;
                    string ownerName = GetOwnerName(annotationContext, 1);
                    string funName = GetName(annotationContext, 0);
                    it.MoveNext();
                    string annotation = ((Cnst)it.Current).GetStringValue();
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
                        Cnst indexCnst = it.Current as Cnst;
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
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var eventName = ((Cnst)it.Current).GetStringValue();
                    exportedEvents.Add(eventName);
                }
            }

            terms = GetBin(factBins, "MaxNumLocals");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var typingContextAlias = Factory.Instance.ToAST(it.Current);
                    FuncTerm typingContext = aliasToTerm[typingContextAlias];
                    string typingContextKind = ((Id)typingContext.Function).Name;
                    it.MoveNext();
                    var maxNumLocals = (int)((Cnst)it.Current).GetNumericValue().Numerator;

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
                foreach (var machineName in allMachines.Keys)
                {
                    if (!allMachines[machineName].IsSpec) continue;
                    var machineInfo = allMachines[machineName];
                    List<string> initialSet = new List<string>();
                    foreach (var stateName in ComputeReachableStates(machineName, machineInfo, new string[] { machineInfo.initStateName }))
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
                    foreach (var stateName in ComputeReachableStates(machineName, machineInfo, initialSet))
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

        private HashSet<string> ComputeGotoTargets(string machineName, Node node)
        {
            HashSet<string> targets = new HashSet<string>();
            Stack<Node> searchStack = new Stack<Node>();
            searchStack.Push(node);
            while (searchStack.Count != 0)
            {
                var topOfStack = searchStack.Pop() as FuncTerm;
                if (topOfStack == null) continue;
                var name = ((Id)topOfStack.Function).Name;
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
                        var targetName = GetNameFromQualifiedName(machineName, GetArgByIndex(topOfStack, 0) as FuncTerm);
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

        private HashSet<string> ComputeGotoTargets(string machineName, MachineInfo machineInfo, StateInfo stateInfo)
        {
            HashSet<string> targets = new HashSet<string>();
            targets.UnionWith(ComputeGotoTargets(machineName, machineInfo.funNameToFunInfo[stateInfo.entryActionName].body));
            foreach (var actionName in stateInfo.dos.Values)
            {
                targets.UnionWith(ComputeGotoTargets(machineName, machineInfo.funNameToFunInfo[actionName].body));
            }
            return targets;
        }

        HashSet<string> ComputeReachableStates(string machineName, MachineInfo machineInfo, IEnumerable<string> initialSet)
        {
            Stack<string> dfsStack = new Stack<string>();
            HashSet<string> visitedStates = new HashSet<string>();
            foreach (var stateName in initialSet)
            {
                dfsStack.Push(stateName);
                visitedStates.Add(stateName);
            }
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
                foreach (var nextState in ComputeGotoTargets(machineName, machineInfo, curStateInfo))
                {
                    if (visitedStates.Contains(nextState)) continue;
                    visitedStates.Add(nextState);
                    dfsStack.Push(nextState);
                }
            }
            return visitedStates;
        }
    }
}
