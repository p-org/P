using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Formula.API;
using Microsoft.Formula.API.ASTQueries;
using Microsoft.Formula.API.Nodes;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Pc
{
    //CSharpTranslationInfo is redundant:
    //internal class CSharpTranslationInfo
    //{
    //    public AST<Node> node = null;

    //    public CSharpTranslationInfo(AST<Node> n)
    //    {
    //        this.node = n;
    //    }
    //}

    class PToCSharp : PTranslation
    {
        //TODO(replace): these are Zing-related; used here for compilation only
        public static AST<Node> PrtMkDefaultValue = MkCSharpDot("PRT_VALUE", "PrtMkDefaultValue");
        public static AST<Node> PrtCloneValue = MkCSharpDot("PRT_VALUE", "PrtCloneValue");
        public PToCSharp(Compiler compiler, AST<Model> modelWithTypes, Dictionary<int, SourceInfo> idToSourceInfo, string csharpFileName)
            : base(compiler, modelWithTypes, idToSourceInfo)
        {
            this.csharpFileName = csharpFileName;
            //this.typeContext = new TypeTranslationContext(this);
            //GenerateTypeInfo(modelWithTypes);
        }

        #region CSharpCompiler
        string csharpFileName;
        //for storing members of the Application class:
        static List <SyntaxNode> members = new List<SyntaxNode>();
        //final C# program:
        SyntaxNode result = null;
        static SyntaxGenerator generator;

        #region Utility
        //utility methods:
        //public AST<Node> PTypeToZingExpr(FuncTerm pType)
        //{
        //    var pTypeAST = Factory.Instance.ToAST(pType);
        //    if (!pTypeToZingExpr.ContainsKey(pTypeAST))
        //    {
        //        pTypeToCSharpExpr[pTypeAST] = ConstructType(pType);
        //    }
        //    return pTypeToCSahrpExpr[pTypeAST];
        //}
        //This is based on PToZing.TypeTranslationContext.ConstructType
        //and will eventually be replaced with smth similar
        private string ConstructType(FuncTerm type)
        {
            string typeKind = ((Id)type.Function).Name;
            if (typeKind == "BaseType")
            {
                var primitiveType = ((Id)GetArgByIndex(type, 0)).Name;
                if (primitiveType == "NULL")
                {
                    return "null";
                }
                else if (primitiveType == "BOOL")
                {
                    return "bool";
                }
                else if (primitiveType == "INT")
                {
                    return "int";
                }
                else if (primitiveType == "EVENT")
                {
                    return "event";
                }
                else if (primitiveType == "MACHINE")
                {
                    return "machine";
                }
                else
                {
                    //TODO(question): how to enable Debug.Assert?
                    Debug.Assert(primitiveType == "ANY", "Illegal BaseType");
                    return "any";
                }
            }
            else if (typeKind == "NameType")
            {
                return "NameType: not implemented yet";
            }
            else if (typeKind == "TupType")
            {
                return "TupType: not implemented yet";
            }
            else if (typeKind == "NmdTupType")
            {
                return "NmdTupType: not implemented";
            }
            else if (typeKind == "SeqType")
            {
                return "SeqType: not implemented";
            }
            else
            {
                // typeKind == "MapType"
                return "MapType: not implemented";
            }
        }
        //TODO(expand): write this method: consider all possible types: null, bool, int, event, machine, any
        private string GetTypeCreationExpr(string type)
        {
            if (type == "null")
            {
                return "new PrtNullType()";
            }
            else if (type == "bool")
            {
                return "new PrtBoolType()";
            }
            else if (type == "int")
            {
                return "new PrtIntType()";
            }
            else if (type == "event")
            {
                return "new PrtEventType()";
            }
            else if (type == "machine")
            {
                return "new PrtMachineType()";
            }
            else if (type == "any")
            {
                return "new PrtAnyType()";
            }
            else
            {
                return "enum, tuple, seq or map type not implemented yet";
            }
        }
        public bool GenerateCSharp()
        {
            if (!allMachines.ContainsKey("Main"))
            {
                Console.WriteLine("Unable to generate CSharp code since Main machine is absent.");
                return false;
            }
            var workspace = new AdhocWorkspace();

            // Get the SyntaxGenerator for the specified language
            generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);


            // Create using/Imports directives
            var usingDirectives = generator.NamespaceImportDeclaration("System");


            List<AST<Node>> elements = new List<AST<Node>>();
            MkAppConstructors();
            MkEvents();
            //generated static anon function name is: 
            //var funName = "AnonFunStatic" + anonFunCounterStatic;
            //In PToZing.cs:
            //string funName = anonFunToName[Factory.Instance.ToAST(fun)];
            //, where fun is a Node.
            MkStaticFunctions();
            MkOtherAppFields();
            MkMachineClasses();
            //MkMonitorClasses(elements, workspace, generator);
            MkCSharpOutput();
            EmitCSharpOutput(csharpFileName);
            return true;
        }
        private void MkAppConstructors()
        {
            //parameterless constructor
            //TODO(expand): add inits for all fields
            var constructor_1 = generator.ConstructorDeclaration("Application", null, Accessibility.Public, baseConstructorArguments: new SyntaxNode[0]);
            members.Add(constructor_1);

            var constructorParameters = new SyntaxNode[] {
                generator.ParameterDeclaration("initialize",
                    generator.TypeExpression(SpecialType.System_Boolean)) };
            var constructorBody = generator.ExpressionStatement(generator.InvocationExpression(generator.IdentifierName("CreateMainMachine")));
            var constructor_2 = generator.ConstructorDeclaration("Application", constructorParameters, Accessibility.Public, baseConstructorArguments: new SyntaxNode[0], 
                                                                statements: new SyntaxNode[] { constructorBody });
            members.Add(constructor_2);
            //Generate "new Application();" 
            var makeSkeletonMethodBody = generator.ReturnStatement(generator.ObjectCreationExpression(generator.IdentifierName("Application")));
            var makeSkeletonMethodDecl = generator.MethodDeclaration("MakeSkeleton", null,
              null, generator.IdentifierName("StateImpl"),
              Accessibility.Public,
              DeclarationModifiers.Override,
              new SyntaxNode[] { makeSkeletonMethodBody });
            members.Add(makeSkeletonMethodDecl);
        }
        private void MkEvents()
        {
            foreach (var pair in allEvents)
            {
                SyntaxNode payloadType = generator.IdentifierName(GetTypeCreationExpr(ConstructType((pair.Value).payloadType)));
                SyntaxNode maxInstances;
                if ((pair.Value).maxInstances == -1)
                {
                    String s = "PrtEvent.DefaultMaxInstances";
                    maxInstances = generator.IdentifierName(s);
                }
                else
                {
                    maxInstances = generator.LiteralExpression((pair.Value).maxInstances);
                }
               
                SyntaxNode doAssume;
                if ((pair.Value).maxInstancesAssumed)
                {
                    doAssume = generator.LiteralExpression(true);
                }
                else
                {
                    doAssume = generator.LiteralExpression(false);
                }
                string quotedEventName = "\"" + pair.Key + "\"";
                var eventCreationPars = new SyntaxNode[] { generator.IdentifierName(quotedEventName), payloadType, maxInstances, doAssume };
                var eventCreationExpr = generator.ObjectCreationExpression(generator.IdentifierName("PrtEvent"), eventCreationPars);
                var initExpr_1 = generator.ObjectCreationExpression(generator.IdentifierName("PrtEventValue"), eventCreationExpr);

                //"@null" is written to he output as pair.Key for the null event; possibly, this is done to disambiguate null as C# keyword
                //output with "@null" compiles
                SyntaxNode eventField_1 = null;
                if (pair.Key == NullEvent)
                {
                    eventField_1 = generator.FieldDeclaration(@"null",
                        generator.IdentifierName("PrtEventValue"),
                        Accessibility.Public,
                         DeclarationModifiers.Static,
                        initExpr_1);
                }
                else
                {
                    eventField_1 = generator.FieldDeclaration(pair.Key,
                        generator.IdentifierName("PrtEventValue"),
                        Accessibility.Public,
                         DeclarationModifiers.Static,
                        initExpr_1);
                }
                

                members.Add(eventField_1);
                
            }
        }
        public static AST<FuncTerm> MkCSharpAssign(AST<Node> lhs, AST<Node> rhs)
        {
            throw new NotImplementedException();
        }
        public static AST<FuncTerm> MkCSharpCall(AST<Node> methodExpr, params AST<Node>[] args)
        {
            throw new NotImplementedException();
        }
        private static AST<FuncTerm> MkCSharpReturn(AST<Node> rVal)
        {
            throw new NotImplementedException();
        }
        public static AST<FuncTerm> MkCSharpCallStmt(AST<Node> callExpr, params AST<Node>[] attrs)
        {
            throw new NotImplementedException();
        }
        public static AST<FuncTerm> MkCSharpIdentifier(string name)
        {
            throw new NotImplementedException();
        }
        public static AST<FuncTerm> MkCSharpDot(AST<Node> n, string first, params string[] names)
        {
            throw new NotImplementedException();
        }
        public static AST<FuncTerm> MkCSharpVarDecl(string varName, AST<Node> varType, params AST<Node>[] attrs)
        {
            throw new NotImplementedException();
        }
        public static AST<FuncTerm> MkCSharpDot(string first, params string[] names)
        {
            return null;
        }
        public static AST<FuncTerm> MkCSharpIfThen(AST<Node> cond, AST<Node> then)
        {
            throw new NotImplementedException();
        }

        public static AST<FuncTerm> MkCSharpIfThenElse(AST<Node> cond, AST<Node> thenstmt, AST<Node> elsestmt)
        {
            throw new NotImplementedException();
        }
        public static AST<FuncTerm> MkCSharpGoto(string labelName)
        {
            throw new NotImplementedException();
        }
        public static AST<FuncTerm> MkCSharpBlock(string label, AST<Node> body)
        {
            throw new NotImplementedException();
        }

        public static AST<Node> MkCSharpBlocks(params AST<Node>[] blocks)
        {
            throw new NotImplementedException();
        }

        public static AST<Node> MkCSharpBlocks(List<AST<Node>> blocks)
        {
            throw new NotImplementedException();
        }
        private static AST<Node> MkCSharpNew(AST<Node> constructedType, AST<Node> optionalSize)
        {
            throw new NotImplementedException();
        }
        public static AST<FuncTerm> MkCSharpAssert(AST<Node> condition)
        {
            throw new NotImplementedException(); ;
        }

        private static AST<FuncTerm> MkCSharpAssert(AST<Node> condition, string msg)
        {
            throw new NotImplementedException();
        }
        public static AST<Node> MkCSharpEq(AST<Node> e1, AST<Node> e2)
        {
            throw new NotImplementedException();
        }
        public static AST<FuncTerm> MkCSharpAdd(AST<Node> a, AST<Node> b)
        {
            throw new NotImplementedException();
        }
        private static AST<Node> MkCSharpIndex(AST<Node> baseExp, AST<Node> indExp)
        {
            throw new NotImplementedException();
        }
        public static AST<Node> MkCSharpSeq(params AST<Node>[] stmts)
        {
            throw new NotImplementedException();
           
        }
        public static AST<Node> MkCSharpSeq(List<AST<Node>> stmts)
        {
            throw new NotImplementedException();
        }
        public static AST<Node> MkCSharpEvent(string eventName)
        {
            throw new NotImplementedException();
        }

        #endregion
        internal class MkFunctionDecl
        {
            //funName is former entityName
            public string funName;
            public string funType;
            //funInfo is former entityInfo
            public FunInfo funInfo;
            //TODO: might not be needed
            public bool isGlobalStatic;
            //null if global function:
            public MkMachineClass owner;
            //owner.machName is former machineName

            //These fields are copied from ZingFoldContext (not needed for C#)
            private PToCSharp pToCSharp;
            //public string machineName;
            //public string entityName;
            //public FunInfo entityInfo;
            //sideEffectsStack is used for non-atomic stmts: Seq, ITE, While in the translation of expressions.
            //Translation of a P expression should result in a zing expression so that translated expressions can be 
            //composed; but compilation of some P expressions results in a complicated piece of code containing statements as well.  
            //These statements are emitted to the side-effect stack and popped later.  
            //We should be able to do without this stack in C# code gen because C# method calls can be nested inside expressions
            public Stack<List<AST<Node>>> sideEffectsStack;
            //locals contain temp vars for storing intermediate results, for example, in translation of expressions
            public List<Tuple<AST<Node>, string>> locals;
            public Stack<bool> lhsStack;
            //labels are used for "continuations" in send, new, nondet, receive
            private Dictionary<string, int> labels;

            public int LabelToId(string l)
            {
                return labels[l];
            }

            public string GetFreshLabel()
            {
                var l = pToCSharp.GetUnique(funName);
                labels[l] = labels.Count + 1;
                return l;
            }

            public AST<Node> EmitLabelPrelude()
            {
                var prelude = new List<AST<Node>>();
                var tmpVar = GetTmpVar(Factory.Instance.MkCnst("StackFrame"), "retTo");

                prelude.Add(PToCSharp.MkCSharpAssign(tmpVar, PToCSharp.MkCSharpCall(MkCSharpDot("entryCtxt", "PopReturnTo"))));
                prelude.Add(PToCSharp.MkCSharpAssign(MkCSharpIdentifier("locals"), MkCSharpDot(tmpVar, "locals")));
                prelude.Add(PToCSharp.MkCSharpIfThen(PToCSharp.MkCSharpEq(MkCSharpDot(tmpVar, "pc"), Factory.Instance.MkCnst(0)), MkCSharpGoto("start")));

                foreach (var l in labels.Keys)
                {
                    prelude.Add(PToCSharp.MkCSharpIfThen(PToCSharp.MkCSharpEq(MkCSharpDot(tmpVar, "pc"), Factory.Instance.MkCnst(labels[l])), MkCSharpGoto(l)));
                }

                //TODO(Zing to CSharp): convert to CSharpData
                prelude.Add(MkCSharpAssert(ZingData.Cnst_False, "Internal error"));

                return PToCSharp.MkCSharpSeq(prelude);
            }

            public void AddSideEffect(AST<Node> stmt)
            {
                this.sideEffectsStack.Peek().Add(stmt);
            }

            public void PushSideEffectStack()
            {
                this.sideEffectsStack.Push(new List<AST<Node>>());
            }

            // type must be CSharp type
            public AST<Node> GetTmpVar(AST<Node> type, string baseName)
            {
                var tmpVarName = pToCSharp.GetUnique(baseName);
                var tmpVar = MkCSharpIdentifier(tmpVarName);
                this.locals.Add(new Tuple<AST<Node>, string>(type, tmpVarName));
                return tmpVar;
            }

            public AST<Node> EmitCSharpSideEffects(AST<Node> stmt)
            {
                Debug.Assert(sideEffectsStack.Count > 0);
                var sideEffects = sideEffectsStack.Pop();

                if (sideEffects.Count > 0)
                {
                    sideEffects.Add(stmt);
                    return PToCSharp.MkCSharpSeq(sideEffects);
                }
                else
                {
                    return stmt;
                }
            }

            public IEnumerable<AST<Node>> EmitLocals()
            {
                return locals.Select(loc => PToCSharp.MkCSharpVarDecl(loc.Item2, loc.Item1));
            }

            private IEnumerable<Node> ZingUnfold(Node n)
            {
                if (n.NodeKind != NodeKind.FuncTerm)
                {
                    yield break;
                }

                var ft = (FuncTerm)n;
                var funName = ((Id)ft.Function).Name;
                if (funName == PData.Con_New.Node.Name)
                {
                    Debug.Assert(false, "New expr in ZingUnfold");
                    yield break;
                }
                else if (funName == PData.Con_Print.Node.Name)
                {
                    foreach (var a in ZingUnfold(GetArgByIndex(ft, 2)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_Goto.Node.Name)
                {
                    foreach (var a in ZingUnfold(GetArgByIndex(ft, 1)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_Announce.Node.Name || funName == PData.Con_Raise.Node.Name)
                {
                    yield return GetArgByIndex(ft, 0);
                    foreach (var a in ZingUnfold(GetArgByIndex(ft, 1)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_Send.Node.Name)
                {
                    yield return GetArgByIndex(ft, 0);
                    yield return GetArgByIndex(ft, 1);
                    foreach (var a in ZingUnfold(GetArgByIndex(ft, 2)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_Receive.Node.Name)
                {
                    yield break;
                }
                else if (funName == PData.Con_FunApp.Node.Name)
                {
                    foreach (var a in ZingUnfold(GetArgByIndex(ft, 1)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_FunStmt.Node.Name || funName == PData.Con_NewStmt.Node.Name)
                {
                    foreach (var a in ZingUnfold(GetArgByIndex(ft, 1)))
                    {
                        yield return a;
                    }
                    var node = GetArgByIndex(ft, 2);
                    if (node.NodeKind != NodeKind.Id)
                    {
                        yield return node;
                    }
                }
                else if (funName == PData.Con_BinApp.Node.Name)
                {
                    var opName = ((Id)GetArgByIndex(ft, 0)).Name;
                    if (opName == PData.Cnst_Idx.Node.Name && lhsStack.Count > 0 && lhsStack.Peek())
                    {
                        lhsStack.Push(true);
                        yield return GetArgByIndex(ft, 1);
                        lhsStack.Pop();
                        lhsStack.Push(false);
                        yield return GetArgByIndex(ft, 2);
                        lhsStack.Pop();
                    }
                    else
                    {
                        yield return GetArgByIndex(ft, 1);
                        yield return GetArgByIndex(ft, 2);
                    }
                }
                else if (funName == PData.Con_Name.Node.Name ||
                         funName == PData.Con_NulApp.Node.Name ||
                         funName == PData.Con_UnApp.Node.Name ||
                         funName == PData.Con_Default.Node.Name ||
                         funName == PData.Con_NulStmt.Node.Name)
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
                else if (funName == PData.Con_Assert.Node.Name)
                {
                    yield return GetArgByIndex(ft, 0);
                }
                else if (funName == PData.Con_BinStmt.Node.Name)
                {
                    yield return GetArgByIndex(ft, 3);
                    var op = ((Id)GetArgByIndex(ft, 0)).Name;
                    if (op == PData.Cnst_Assign.Node.Name)
                    {
                        var lhs = (FuncTerm)GetArgByIndex(ft, 1);
                        var lhsName = ((Id)lhs.Function).Name;
                        if (lhsName == PData.Con_BinApp.Node.Name && ((Id)GetArgByIndex(lhs, 0)).Name == PData.Cnst_Idx.Node.Name)
                        {
                            lhsStack.Push(true);
                            yield return GetArgByIndex(lhs, 1);
                            lhsStack.Pop();
                            yield return GetArgByIndex(lhs, 2);
                        }
                        else if (lhsName == PData.Con_Field.Node.Name)
                        {
                            lhsStack.Push(true);
                            yield return GetArgByIndex(lhs, 0);
                            lhsStack.Pop();
                        }
                        else
                        {
                            lhsStack.Push(true);
                            yield return lhs;
                            lhsStack.Pop();
                        }
                    }
                    else
                    {
                        lhsStack.Push(true);
                        yield return GetArgByIndex(ft, 1);
                        lhsStack.Pop();
                    }
                }
                else if (funName == PData.Con_Field.Node.Name || funName == PData.Con_Cast.Node.Name)
                {
                    yield return ft.Args.First<Node>();
                }
                else if (funName == PData.Con_Tuple.Node.Name)
                {
                    foreach (var a in ZingUnfold(GetArgByIndex(ft, 0)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_NamedTuple.Node.Name)
                {
                    foreach (var a in ZingUnfold(GetArgByIndex(ft, 0)))
                    {
                        yield return a;
                    }
                }
                else if (funName == PData.Con_Exprs.Node.Name)
                {
                    do
                    {
                        yield return GetArgByIndex(ft, 1);
                        ft = GetArgByIndex(ft, 2) as FuncTerm;
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
                        PushSideEffectStack();
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
                        PushSideEffectStack();
                        yield return it.Current;
                        it.MoveNext();
                        PushSideEffectStack();
                        yield return it.Current;
                    }
                }
                else if (funName == PData.Con_While.Node.Name)
                {
                    using (var it = ft.Args.GetEnumerator())
                    {
                        it.MoveNext();
                        PushSideEffectStack();
                        yield return it.Current;
                        it.MoveNext();
                        PushSideEffectStack();
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
            //Eliminating ZingTranslationInfo: using AST<Node> instead
            private AST<Node> ZingFold(Node n, IEnumerable<AST<Node>> children)
            {
                if (n.NodeKind == NodeKind.Id || n.NodeKind == NodeKind.Cnst)
                    return ZingData.Cnst_Nil;

                var ft = (FuncTerm)n;
                var funName = ((Id)ft.Function).Name;

                if (funName == PData.Con_Name.Node.Name)
                {
                    return (AST<Node>)FoldName(ft, children);
                }
                else if (funName == PData.Con_Receive.Node.Name)
                {
                    return FoldReceive(ft, children);
                }
                else if (funName == PData.Con_FunApp.Node.Name)
                {
                    return FoldFunApp(ft, children);
                }
                else if (funName == PData.Con_NulApp.Node.Name)
                {
                    return FoldNulApp(ft, children);
                }
                else if (funName == PData.Con_UnApp.Node.Name)
                {
                    return FoldUnApp(ft, children);
                }
                else if (funName == PData.Con_BinApp.Node.Name)
                {
                    return FoldBinApp(ft, children);
                }
                else if (funName == PData.Con_Field.Node.Name)
                {
                    return FoldField(ft, children);
                }
                else if (funName == PData.Con_Default.Node.Name)
                {
                    return FoldDefault(ft, children);
                }
                else if (funName == PData.Con_Cast.Node.Name)
                {
                    return FoldCast(ft, children);
                }
                else if (funName == PData.Con_Tuple.Node.Name)
                {
                    return FoldTuple(ft, children);
                }
                else if (funName == PData.Con_NamedTuple.Node.Name)
                {
                    return FoldNamedTuple(ft, children);
                }
                else if (funName == PData.Con_NewStmt.Node.Name)
                {
                    return FoldNewStmt(ft, children);
                }
                else if (funName == PData.Con_Goto.Node.Name)
                {
                    return FoldGoto(ft, children);
                }
                else if (funName == PData.Con_Raise.Node.Name)
                {
                    return FoldRaise(ft, children);
                }
                else if (funName == PData.Con_Send.Node.Name)
                {
                    return FoldSend(ft, children);
                }
                else if (funName == PData.Con_Announce.Node.Name)
                {
                    return FoldAnnounce(ft, children);
                }
                else if (funName == PData.Con_FunStmt.Node.Name)
                {
                    return FoldFunStmt(ft, children);
                }
                else if (funName == PData.Con_NulStmt.Node.Name)
                {
                    return FoldNulStmt(ft, children);
                }
                else if (funName == PData.Con_Assert.Node.Name)
                {
                    return FoldAssert(ft, children);
                }
                else if (funName == PData.Con_Print.Node.Name)
                {
                    return FoldPrint(ft, children);
                }
                else if (funName == PData.Con_BinStmt.Node.Name)
                {
                    return FoldBinStmt(ft, children);
                }
                else if (funName == PData.Con_Return.Node.Name)
                {
                    return FoldReturn(ft, children);
                }
                else if (funName == PData.Con_While.Node.Name)
                {
                    return FoldWhile(ft, children);
                }
                else if (funName == PData.Con_Ite.Node.Name)
                {
                    return FoldIte(ft, children);
                }
                else if (funName == PData.Con_Seq.Node.Name)
                {
                    return FoldSeq(ft, children);
                }
                else if (funName == PData.Con_IdList.Node.Name)
                {
                    return ZingData.Cnst_Nil;
                }
                else
                {
                    Console.WriteLine("Unknown term name: " + funName);
                    throw new NotImplementedException();
                }
            }
            private List<AST<Node>> CaseFunCallHelper(List<string> eventNames, List<string> funNames, string afterAfterLabel)
            {
                List<AST<Node>> eventStmts = new List<AST<Node>>();
                List<AST<Node>> funStmts = new List<AST<Node>>();

                for (int i = 0; i < eventNames.Count; i++)
                {
                    var beforeLabel = GetFreshLabel();
                    var eventName = eventNames[i];
                    var funName = funNames[i];
                    var calleeInfo = pToCSharp.allStaticFuns.ContainsKey(funName) ? pToCSharp.allStaticFuns[funName] : pToCSharp.allMachines[owner.machName].funNameToFunInfo[funName];
                    Debug.Assert(calleeInfo.isAnonymous);
                    List<AST<Node>> ifStmts = new List<AST<Node>>();
                    ifStmts.Add(MkCSharpAssign(MkCSharpIndex(MkCSharpIdentifier("locals"), Factory.Instance.MkCnst(calleeInfo.localNameToInfo[calleeInfo.PayloadVarName].index)), MkCSharpCall(PrtCloneValue, MkCSharpDot("myHandle", "currentArg"))));
                    foreach (var calleeLocal in calleeInfo.localNames)
                    {
                        var calleeLocalInfo = calleeInfo.localNameToInfo[calleeLocal];
                        ifStmts.Add(MkCSharpAssign(MkCSharpIndex(MkCSharpIdentifier("locals"), Factory.Instance.MkCnst(calleeLocalInfo.index)), MkCSharpCall(PrtMkDefaultValue, typeContext.PTypeToZingExpr(calleeLocalInfo.type))));
                    }
                    ifStmts.Add(MkCSharpCallStmt(MkCSharpCall(MkCSharpDot("entryCtxt", "PushReturnTo"), Factory.Instance.MkCnst(0), MkCSharpIdentifier("locals"))));
                    ifStmts.Add(MkCSharpGoto(beforeLabel));
                    eventStmts.Add(MkCSharpIfThen(MkCSharpEq(MkCSharpDot("myHandle", "currentEvent"), MkCSharpEvent(eventName)), MkCSharpSeq(ifStmts)));
                    if (pToCSharp.allStaticFuns.ContainsKey(funName))
                    {
                        funStmts.Add(MkCSharpBlock(beforeLabel, MkCSharpCallStmt(MkCSharpCall(MkCSharpDot("Main", funName), MkCSharpIdentifier("myHandle"), MkCSharpIdentifier("entryCtxt")))));
                    }
                    else
                    {
                        funStmts.Add(MkCSharpBlock(beforeLabel, MkCSharpCallStmt(MkCSharpCall(MkCSharpIdentifier(funName), MkCSharpIdentifier("entryCtxt")))));
                    }
                    funStmts.Add(MkCSharpIfThenElse(
                                         MkCSharpEq(MkCSharpDot("entryCtxt", "reason"), MkCSharpDot("ContinuationReason", "Return")),
                                         MkCSharpGoto(afterAfterLabel),
                                         MkCSharpSeq(MkCSharpCallStmt(MkCSharpCall(MkCSharpDot("entryCtxt", "PushReturnTo"), Factory.Instance.MkCnst(LabelToId(beforeLabel)), MkCSharpIdentifier("locals"))),
                                                   MkCSharpReturn(ZingData.Cnst_Nil))));
                }
                List<AST<Node>> stmts = new List<AST<Node>>();
                stmts.AddRange(eventStmts);
                stmts.Add(MkCSharpAssert(ZingData.Cnst_False));
                stmts.AddRange(funStmts);
                return stmts;
            }
            AST<Node> FoldReceive(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                List<AST<Node>> stmts = new List<AST<Node>>();
                List<string> eventNames = new List<string>();
                List<string> funNames = new List<string>();
                var cases = GetArgByIndex(ft, 0) as FuncTerm;
                while (cases != null)
                {
                    Node evt = GetArgByIndex(cases, 0);
                    string eventName = null;
                    if (evt is Cnst)
                    {
                        eventName = (evt as Cnst).GetStringValue();
                    }
                    else if ((evt as Id).Name == "NULL")
                    {
                        eventName = NullEvent;
                    }
                    else
                    {
                        eventName = HaltEvent;
                    }
                    eventNames.Add(eventName);
                    stmts.Add(MkCSharpAssign(MkCSharpDot("myHandle", "receiveSet"), MkCSharpAdd(MkCSharpDot("myHandle", "receiveSet"), MkCSharpEvent(eventName))));
                    var fun = GetArgByIndex(cases, 1);
                    string funName = pToCSharp.anonFunToName[Factory.Instance.ToAST(fun)];
                    funNames.Add(funName);
                    cases = GetArgByIndex(cases, 2) as FuncTerm;
                }
                var afterLabel = GetFreshLabel();
                stmts.Add(MkCSharpCallStmt(MkCSharpCall(MkCSharpDot("entryCtxt", "Receive"), Factory.Instance.MkCnst(LabelToId(afterLabel)), MkCSharpIdentifier("locals"))));
                stmts.Add(MkCSharpReturn(ZingData.Cnst_Nil));
                stmts.Add(MkCSharpBlock(afterLabel, ZingData.Cnst_Nil));
                var afterAfterLabel = GetFreshLabel();
                stmts.AddRange(CaseFunCallHelper(eventNames, funNames, afterAfterLabel));
                stmts.Add(MkCSharpBlock(afterAfterLabel, ZingData.Cnst_Nil));
                AST<Node> node = MkCSharpSeq(stmts);
                return node;
            }
            AST<Node> FoldName(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldNewStmt(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldFunApp(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldNulApp(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldUnApp(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldBinApp(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldField(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldDefault(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldCast(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldTuple(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldNamedTuple(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldGoto(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldRaise(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldSend(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldAnnounce(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldNullStmt(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldAssert(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldFunStmt(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldNulStmt(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldPrint(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldBinStmt(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldReturn(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldWhile(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldIte(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            AST<Node> FoldSeq(FuncTerm ft, IEnumerable<AST<Node>> children)
            {
                throw new NotImplementedException();
            }
            //Possibly, all types-realted stuff should be moved to PToCSharp (similar to Zing compiler)
            //and  TypeTranslationContext to be instantiated in the PToCSharp constructor
            //After that, ConstructType in MkFunctionDecl can be replaced with TypeTranslationContext.ConstructType
            TypeTranslationContext typeContext;
            internal class TypeTranslationContext
            {
                private int fieldCount;
                private int typeCount;
                private List<AST<Node>> fieldNameInitialization;
                private List<AST<Node>> typeInitialization;
                private Dictionary<string, AST<FuncTerm>> fieldNameToZingExpr;
                private Dictionary<AST<Node>, AST<Node>> pTypeToZingExpr;
                private PToZing pToZing;

                public TypeTranslationContext(PToZing pToZing)
                {
                    this.pToZing = pToZing;
                    fieldCount = 0;
                    typeCount = 0;
                    fieldNameInitialization = new List<AST<Node>>();
                    typeInitialization = new List<AST<Node>>();
                    fieldNameToZingExpr = new Dictionary<string, AST<FuncTerm>>();
                    pTypeToZingExpr = new Dictionary<AST<Node>, AST<Node>>();
                }

                public AST<Node> InitializeFieldNamesAndTypes()
                {
                    return MkCSharpSeq(MkCSharpSeq(fieldNameInitialization), MkCSharpSeq(typeInitialization));
                }

                public IEnumerable<AST<Node>> MainVarDecls()
                {
                    List<AST<Node>> varDecls = new List<AST<Node>>();
                    for (int i = 0; i < fieldCount; i++)
                    {
                        varDecls.Add(MkCSharpVarDecl(string.Format("field_{0}_PRT_FIELD_NAME", i), Factory.Instance.MkCnst("PRT_FIELD_NAME"), ZingData.Cnst_Static));
                    }
                    for (int i = 0; i < typeCount; i++)
                    {
                        varDecls.Add(MkCSharpVarDecl(string.Format("type_{0}_PRT_TYPE", i), Factory.Instance.MkCnst("PRT_TYPE"), ZingData.Cnst_Static));
                    }
                    return varDecls;
                }

                private AST<FuncTerm> GetField(string fieldName)
                {
                    if (fieldNameToZingExpr.ContainsKey(fieldName))
                        return fieldNameToZingExpr[fieldName];
                    var retVal = MkCSharpDot("Main", string.Format("field_{0}_PRT_FIELD_NAME", fieldCount));
                    AddFieldNameInitialization(MkCSharpAssign(retVal, MkCSharpNew(Factory.Instance.MkCnst("PRT_FIELD_NAME"), ZingData.Cnst_Nil)));
                    fieldCount++;
                    fieldNameToZingExpr[fieldName] = retVal;
                    return retVal;
                }

                private new AST<FuncTerm> GetType()
                {
                    var retVal = MkCSharpDot("Main", string.Format("type_{0}_PRT_TYPE", typeCount));
                    typeCount++;
                    return retVal;
                }

                private void AddFieldNameInitialization(AST<Node> n)
                {
                    fieldNameInitialization.Add(n);
                }

                private void AddTypeInitialization(AST<Node> n)
                {
                    typeInitialization.Add(n);
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

                public void AddOriginalType(FuncTerm type, FuncTerm eType)
                {
                    var typeAST = Factory.Instance.ToAST(type);
                    var eTypeAST = Factory.Instance.ToAST(eType);
                    if (!pTypeToZingExpr.ContainsKey(typeAST))
                    {
                        pTypeToZingExpr[typeAST] = pTypeToZingExpr[eTypeAST];
                    }
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
                            AddTypeInitialization(MkCSharpAssign(tmpVar, MkCSharpCall(MkCSharpDot("PRT_TYPE", "PrtMkPrimitiveType"), MkCSharpDot("PRT_TYPE_KIND", "PRT_KIND_NULL"))));
                            return tmpVar;
                        }
                        else if (primitiveType == "BOOL")
                        {
                            var tmpVar = GetType();
                            AddTypeInitialization(MkCSharpAssign(tmpVar, MkCSharpCall(MkCSharpDot("PRT_TYPE", "PrtMkPrimitiveType"), MkCSharpDot("PRT_TYPE_KIND", "PRT_KIND_BOOL"))));
                            return tmpVar;
                        }
                        else if (primitiveType == "INT")
                        {
                            var tmpVar = GetType();
                            AddTypeInitialization(MkCSharpAssign(tmpVar, MkCSharpCall(MkCSharpDot("PRT_TYPE", "PrtMkPrimitiveType"), MkCSharpDot("PRT_TYPE_KIND", "PRT_KIND_INT"))));
                            return tmpVar;
                        }
                        else if (primitiveType == "EVENT")
                        {
                            var tmpVar = GetType();
                            AddTypeInitialization(MkCSharpAssign(tmpVar, MkCSharpCall(MkCSharpDot("PRT_TYPE", "PrtMkPrimitiveType"), MkCSharpDot("PRT_TYPE_KIND", "PRT_KIND_EVENT"))));
                            return tmpVar;
                        }
                        else if (primitiveType == "MACHINE")
                        {
                            var tmpVar = GetType();
                            AddTypeInitialization(MkCSharpAssign(tmpVar, MkCSharpCall(MkCSharpDot("PRT_TYPE", "PrtMkPrimitiveType"), MkCSharpDot("PRT_TYPE_KIND", "PRT_KIND_MACHINE"))));
                            return tmpVar;
                        }
                        else
                        {
                            Debug.Assert(primitiveType == "ANY", "Illegal BaseType");
                            var tmpVar = GetType();
                            AddTypeInitialization(MkCSharpAssign(tmpVar, MkCSharpCall(MkCSharpDot("PRT_TYPE", "PrtMkPrimitiveType"), MkCSharpDot("PRT_TYPE_KIND", "PRT_KIND_ANY"))));
                            return tmpVar;
                        }
                    }
                    else if (typeKind == "NameType")
                    {
                        var tmpVar = GetType();
                        AddTypeInitialization(MkCSharpAssign(tmpVar, MkCSharpCall(MkCSharpDot("PRT_TYPE", "PrtMkPrimitiveType"), MkCSharpDot("PRT_TYPE_KIND", "PRT_KIND_INT"))));
                        return tmpVar;
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
                        AddTypeInitialization(MkCSharpAssign(tupleType, MkCSharpCall(MkCSharpDot("PRT_TYPE", "PrtMkTupType"), Factory.Instance.MkCnst(memberTypes.Count))));
                        for (int i = 0; i < memberTypes.Count; i++)
                        {
                            AddTypeInitialization(MkCSharpCallStmt(MkCSharpCall(MkCSharpDot("PRT_TYPE", "PrtSetFieldType"), tupleType, Factory.Instance.MkCnst(i), memberTypes[i])));
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
                            memberNames.Add(GetField(((Cnst)GetArgByIndex(typeField, 0)).GetStringValue()));
                            memberTypes.Add(PTypeToZingExpr((FuncTerm)GetArgByIndex(typeField, 1)));
                            type = GetArgByIndex(type, 1) as FuncTerm;
                        }
                        var tupleType = GetType();
                        AddTypeInitialization(MkCSharpAssign(tupleType, MkCSharpCall(MkCSharpDot("PRT_TYPE", "PrtMkNmdTupType"), Factory.Instance.MkCnst(memberTypes.Count))));
                        for (int i = 0; i < memberTypes.Count; i++)
                        {
                            AddTypeInitialization(MkCSharpCallStmt(MkCSharpCall(MkCSharpDot("PRT_TYPE", "PrtSetFieldName"), tupleType, Factory.Instance.MkCnst(i), memberNames[i])));
                            AddTypeInitialization(MkCSharpCallStmt(MkCSharpCall(MkCSharpDot("PRT_TYPE", "PrtSetFieldType"), tupleType, Factory.Instance.MkCnst(i), memberTypes[i])));
                        }
                        return tupleType;
                    }
                    else if (typeKind == "SeqType")
                    {
                        var innerType = PTypeToZingExpr((FuncTerm)GetArgByIndex(type, 0));
                        var seqType = GetType();
                        AddTypeInitialization(MkCSharpAssign(seqType, MkCSharpCall(MkCSharpDot("PRT_TYPE", "PrtMkSeqType"), innerType)));
                        return seqType;
                    }
                    else
                    {
                        // typeKind == "MapType"
                        var domType = PTypeToZingExpr((FuncTerm)GetArgByIndex(type, 0));
                        var codType = PTypeToZingExpr((FuncTerm)GetArgByIndex(type, 1));
                        var mapType = GetType();
                        AddTypeInitialization(MkCSharpAssign(mapType, MkCSharpCall(MkCSharpDot("PRT_TYPE", "PrtMkMapType"), domType, codType)));
                        return mapType;
                    }
                }
            }
            //ZingTranslationInfo FoldName(FuncTerm ft, IEnumerable<CSharpTranslationInfo> children)
            
            public MkFunctionDecl(string funName, FunInfo funInfo, bool isGlobalStatic, MkMachineClass owner)
            {
                this.funName = funName;
                this.funType = funName + "_Class";
                this.funInfo = funInfo;
                this.isGlobalStatic = isGlobalStatic;
                this.owner = owner;
            }
            //TODO(expand): stopped here:
            public List<StatementSyntax> MkFunctionBody()
            {
                return new List<StatementSyntax>();
            }
            //TODO(fix): replace this code with general case: Execute method for any function
            //Line below is a template:
            public SyntaxNode MkExecuteMethod()
            {
                List<StatementSyntax> funStmts = new List<StatementSyntax>();
                //PrtFunStackFrame currFun = parent.PrtPopFunStackFrame();
                funStmts.Add(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName("PrtFunStackFrame"))
                        .WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(
                                    Identifier("currFun"))
                                .WithInitializer(
                                    EqualsValueClause(
                                        InvocationExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("parent"),
                                                IdentifierName("PrtPopFunStackFrame"))))))))
                    .NormalizeWhitespace());

                //TODO(expand): generate a "case" stmt (using EmitLabelPrelude) 
                //for possibly multiple labels "Loc_XX"
                //stored in "labels" of the former ZingFoldContext
                //if (currFun.returnTolocation == 0) goto Loc_0; else goto Ret;
                funStmts.Add(
                    IfStatement(
                        BinaryExpression(
                            SyntaxKind.EqualsExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("currFun"),
                                IdentifierName("returnTolocation")),
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(0))),
                        GotoStatement(
                            SyntaxKind.GotoStatement,
                            IdentifierName("Loc_0")))
                    .WithElse(
                        ElseClause(
                            GotoStatement(
                                SyntaxKind.GotoStatement,
                                IdentifierName("Ret"))))
                    .NormalizeWhitespace());

                //Loc_0:
                funStmts.Add(
                    LabeledStatement(
                        Identifier("Loc_0"),
                        ExpressionStatement(
                            IdentifierName(
                                MissingToken(SyntaxKind.IdentifierToken)))
                        .WithSemicolonToken(
                            MissingToken(SyntaxKind.SemicolonToken)))
                    .NormalizeWhitespace());

                //TODO(expand): insert compilation result for the general case of function body
                funStmts.AddRange(MkFunctionBody());

                //Ret: parent.PrtFunContReturn(null);
                funStmts.Add(
                    LabeledStatement(
                        Identifier("Ret"),
                        ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("parent"),
                                    IdentifierName("PrtFunContReturn")))
                            .WithArgumentList(
                                ArgumentList(
                                    SingletonSeparatedList<ArgumentSyntax>(
                                        Argument(
                                            LiteralExpression(
                                                SyntaxKind.NullLiteralExpression)))))))
                    .NormalizeWhitespace());

                var executeMethodDecl =
                    MethodDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.VoidKeyword)),
                        Identifier("Execute"))
                    .WithModifiers(
                        TokenList(
                            new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.OverrideKeyword)}))
                    .WithParameterList(
                        ParameterList(
                            SeparatedList<ParameterSyntax>(
                                new SyntaxNodeOrToken[]{
                                    Parameter(
                                        Identifier("application"))
                                    .WithType(
                                        IdentifierName("StateImpl")),
                                    Token(SyntaxKind.CommaToken),
                                    Parameter(
                                        Identifier("parent"))
                                    .WithType(
                                        IdentifierName("PrtMachine"))})))
                    .WithBody(
                        //Block(stmt1, stmt2, stmt3, stmt4))
                        Block(funStmts))
                    .NormalizeWhitespace();

                return executeMethodDecl;
            }
            //TODO(fix): replace this code with general case: CreateLocals method for any function
            public SyntaxNode MkCreateLocalsMethod()
            {
                //var locals = new List<PrtValue>();
                var stmt1 =
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName("var"))
                        .WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(
                                    Identifier("locals"))
                                .WithInitializer(
                                    EqualsValueClause(
                                        ObjectCreationExpression(
                                            GenericName(
                                                Identifier("List"))
                                            .WithTypeArgumentList(
                                                TypeArgumentList(
                                                    SingletonSeparatedList<TypeSyntax>(
                                                        IdentifierName("PrtValue")))))
                                        .WithArgumentList(
                                            ArgumentList()))))))
                    .NormalizeWhitespace();

                //foreach (var item in args)
                var stmt2 =
                    ForEachStatement(
                        IdentifierName("var"),
                        Identifier("item"),
                        IdentifierName("args"),
                        Block(
                            SingletonList<StatementSyntax>(
                                ExpressionStatement(
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("locals"),
                                            IdentifierName("Add")))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList<ArgumentSyntax>(
                                                Argument(
                                                    InvocationExpression(
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            IdentifierName("item"),
                                                            IdentifierName("Clone")))))))))))
                    .NormalizeWhitespace();

                ////no local variables hence nothing to add
                //return locals;
                var stmt3 =
                    ReturnStatement(
                        IdentifierName("locals"))
                    .WithReturnKeyword(
                        Token(
                            TriviaList(
                                Comment("//no local variables hence nothing to add")),
                            SyntaxKind.ReturnKeyword,
                            TriviaList()))
                    .NormalizeWhitespace();

                //public override List<PrtValue> CreateLocals(params PrtValue[] args) { ... }
                var createLocalsMethodDecl =
                    MethodDeclaration(
                        GenericName(
                            Identifier("List"))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList<TypeSyntax>(
                                    IdentifierName("PrtValue")))),
                        Identifier("CreateLocals"))
                    .WithModifiers(
                        TokenList(
                            new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.OverrideKeyword)}))
                    .WithParameterList(
                        ParameterList(
                            SingletonSeparatedList<ParameterSyntax>(
                                Parameter(
                                    Identifier("args"))
                                .WithModifiers(
                                    TokenList(
                                        Token(SyntaxKind.ParamsKeyword)))
                                .WithType(
                                    ArrayType(
                                        IdentifierName("PrtValue"))
                                    .WithRankSpecifiers(
                                        SingletonList<ArrayRankSpecifierSyntax>(
                                            ArrayRankSpecifier(
                                                SingletonSeparatedList<ExpressionSyntax>(
                                                    OmittedArraySizeExpression()))))))))
                    .WithBody(
                        Block(stmt1, stmt2, stmt3))
                    .NormalizeWhitespace();

                return createLocalsMethodDecl;
            }
            public SyntaxNode MkFuncClass()
            {
                SyntaxList<MemberDeclarationSyntax> funMembers = new SyntaxList<MemberDeclarationSyntax>();

                //IsAnonFun property for anon functions only (getter only):
                PropertyDeclarationSyntax isAnonProperty;
                if (funName.StartsWith("AnonFun"))
                {
                    isAnonProperty =
                        PropertyDeclaration(
                            PredefinedType(
                                Token(SyntaxKind.BoolKeyword)),
                            Identifier("IsAnonFun"))
                        .WithModifiers(
                            TokenList(
                                new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.OverrideKeyword)}))
                        .WithAccessorList(
                            AccessorList(
                                SingletonList<AccessorDeclarationSyntax>(
                                    AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration,
                                        Block(
                                            SingletonList<StatementSyntax>(
                                                ReturnStatement(
                                                    LiteralExpression(
                                                        SyntaxKind.TrueLiteralExpression))))))))
                        .NormalizeWhitespace();
                }
                else
                {
                    isAnonProperty =
                        PropertyDeclaration(
                            PredefinedType(
                                Token(SyntaxKind.BoolKeyword)),
                            Identifier("IsAnonFun"))
                        .WithModifiers(
                            TokenList(
                                new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.OverrideKeyword)}))
                        .WithAccessorList(
                            AccessorList(
                                SingletonList<AccessorDeclarationSyntax>(
                                    AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration,
                                        Block(
                                            SingletonList<StatementSyntax>(
                                                ReturnStatement(
                                                    LiteralExpression(
                                                        SyntaxKind.FalseLiteralExpression))))))))
                        .NormalizeWhitespace();
                }
                funMembers = funMembers.Add(isAnonProperty);

                funMembers = funMembers.Add((MemberDeclarationSyntax)MkExecuteMethod());
                funMembers = funMembers.Add((MemberDeclarationSyntax)MkCreateLocalsMethod());

                var funClassDecl =
                    ClassDeclaration(funType)
                    .WithModifiers(
                        TokenList(
                            Token(SyntaxKind.PublicKeyword)))
                    .WithBaseList(
                        BaseList(
                            SingletonSeparatedList<BaseTypeSyntax>(
                                SimpleBaseType(
                                    IdentifierName("PrtFun")))))
                    .WithMembers(funMembers)
                   .NormalizeWhitespace();

                return funClassDecl;
            }

            public void AddFunClass()
            {
                //Function class declaration should be generated in two cases:
                //1. For all global static functions
                //2. For other functions: if this function name was not already encountered
                if (owner == null || !(owner == null) && !owner.processedFuns.Contains(funName))
                {
                    //Class declaration:
                    List<SyntaxNode> whereToAdd;
                    whereToAdd = (this.isGlobalStatic) ? PToCSharp.members : owner.machineMembers;

                    whereToAdd.Add(MkFuncClass());
                    //Variable declaration:
                    whereToAdd.Add(
                        FieldDeclaration(
                            VariableDeclaration(
                                IdentifierName(funType))
                            .WithVariables(
                                SingletonSeparatedList<VariableDeclaratorSyntax>(
                                    VariableDeclarator(
                                        Identifier(funName)))))
                        .WithModifiers(
                            TokenList(
                                new[]{
                                        Token(SyntaxKind.PublicKeyword),
                                        Token(SyntaxKind.StaticKeyword)}))
                        .NormalizeWhitespace());

                    //Add function variable instantiation to:
                    //PToCsharp class for global static functions;
                    //Main constructor for other functions
                    if (this.isGlobalStatic)
                    {
                        PToCSharp.members.Add(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName(funName),
                                    ObjectCreationExpression(
                                        IdentifierName(funType))
                                    .WithArgumentList(
                                        ArgumentList())))
                            .NormalizeWhitespace());
                    }
                    else
                    {
                        owner.mainConstructorFields.Add(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName(funName),
                                    ObjectCreationExpression(
                                        IdentifierName(funType))
                                    .WithArgumentList(
                                        ArgumentList())))
                            .NormalizeWhitespace());
                    }
   
                    if (!(owner == null))
                    {
                        owner.processedFuns.Add(funName);
                    }   
                }
            }
        }
        private void MkStaticFunctions()
        {
            foreach (var pair in allStaticFuns)
            {
                var funName = pair.Key;
                var funType = funName + "_Class";
                MkFunctionDecl funDecl = new MkFunctionDecl(funName, pair.Value, true, null);
                funDecl.AddFunClass();
                //members.Add();
                //members.Add(AddStaticFunClass(funName,funType));
            }
        }
        private void MkOtherAppFields()
        {
            //CreateMainMachine method declaration:
            List<SyntaxNode> fields = new List<SyntaxNode>();
            //TODO(expand): replace "10" with Main machine's maxQueueSize;
            //check that maxQueueSizeAssumed is true
            //stmt1: var mainMachine = new Main(this, mainMachineMaxQueueSize);
            MachineInfo mainMachInfo;
            //TODO(question): do we need to have the two cases below? If not, what is maxQueueSizeAssumed for?    
            //TODO(bug fix) Implement both "assume" and "assert" for maxQueueSize
            //There are three cases:
            //- default (no constraint on queue size): maxQueueSizeAssumed == false; maxQueueSize = -1 
            // - replace by 10? (TODO(question))
            //- assume <maxQueueSize>: maxQueueSize > 0, 
            //- assert <maxQueueSize>: maxQueueSizeAssumed == true;   
            if (allMachines.TryGetValue("Main", out mainMachInfo))
            {
                if (mainMachInfo.maxQueueSizeAssumed)
                {
                    fields.Add(generator.LocalDeclarationStatement(generator.IdentifierName("var"), "mainMachine",
                                   generator.ObjectCreationExpression(generator.IdentifierName("Main"),
                                   new List<SyntaxNode>() { generator.ThisExpression(), generator.LiteralExpression(mainMachInfo.maxQueueSize),
                                                            generator.LiteralExpression(mainMachInfo.maxQueueSizeAssumed)})));
                }
                else
                {
                    //TODO(question): 10 is the default maxQueueSize for Main machine
                    fields.Add(generator.LocalDeclarationStatement(generator.IdentifierName("var"), "mainMachine",
                                   generator.ObjectCreationExpression(generator.IdentifierName("Main"),
                                   new List<SyntaxNode>() { generator.ThisExpression(), generator.LiteralExpression(10) })));
                }
            }
        
            //stmt2: AddImplMachineToStateImpl(mainMachine);
            fields.Add(generator.InvocationExpression(generator.IdentifierName("AddImplMachineToStateImpl"), 
                                 new List<SyntaxNode>() { generator.IdentifierName("mainMachine") }));
            //stmt3: return mainMachine;
            fields.Add(generator.ReturnStatement(generator.IdentifierName("mainMachine")));
            //public PrtImplMachine CreateMainMachine() {stmt1; stmt2; stmt3};
            var makeCreateMainMachineDecl = generator.MethodDeclaration("CreateMainMachine", null,
              null, generator.IdentifierName("PrtImplMachine"),
              Accessibility.Public,
              statements: fields);
            members.Add(makeCreateMainMachineDecl);
        }
        private void MkMachineClasses()
        {
            //TODO(expand) For now: only calls MkMainMachineClass  
            //Debug only:
            Console.WriteLine("Number of machines: {0}", allMachines.Count());
            foreach (var pair in allMachines)
            {
                MkMachineClass mkMachine;
                if ((pair.Value).IsReal)
                {
                    //Regular machine:
                    //Debug only:
                    Console.WriteLine("Next Real machine: name: {0}", pair.Key);
                    mkMachine = new MkMachineClass(this, pair.Key, pair.Value);
                    SyntaxNode node = mkMachine.MkRealMachineClass();
                    members.Add(node);
                }
                else
                {
                    //monitor machine
                    //Debug only:
                    Console.WriteLine("Next Spec machine: name: {0}", pair.Key);
                    mkMachine = new MkMachineClass(this, pair.Key, pair.Value);
                    SyntaxNode node = mkMachine.MkSpecMachineClass();
                    members.Add(node);
                }
            }
        }
        internal class MkMachineClass
        {
            public PToCSharp translator;
            public string machName;
            public MachineInfo machInfo;
            public List<SyntaxNode> machineMembers = new List<SyntaxNode>();
            public List<StatementSyntax> mainConstructorFields = new List<StatementSyntax>();
            //keeps track of already encountered function names:
            public HashSet<string> processedFuns = new HashSet<string>();
            private int transition_count = 1;
            public MkMachineClass(PToCSharp ptoCsharp, string name, MachineInfo info) {
                translator = ptoCsharp;
                machName = name;
                machInfo = info;
            }
            public StatementSyntax MkStateInstantiation(string stateName, string stateType, string entryFun, string exitFun, 
                                                    bool hasNullTransition, StateTemperature temperature)
            {
                if (hasNullTransition)
                {
                    if (temperature == StateTemperature.WARM)
                    {
                        return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(stateName),
                                ObjectCreationExpression(
                                    IdentifierName(stateType))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Argument(
                                                    IdentifierName("\"" + stateName + "\"")),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(entryFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(exitFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    LiteralExpression(
                                                        SyntaxKind.TrueLiteralExpression)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("StateTemperature"),
                                                        IdentifierName("Warm")))})))))
                        .NormalizeWhitespace();
                    }
                    else if (temperature == StateTemperature.HOT)
                    {
                        return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(stateName),
                                ObjectCreationExpression(
                                    IdentifierName(stateType))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Argument(
                                                    IdentifierName("\"" + stateName + "\"")),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(entryFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(exitFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    LiteralExpression(
                                                        SyntaxKind.TrueLiteralExpression)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("StateTemperature"),
                                                        IdentifierName("Hot")))})))))
                        .NormalizeWhitespace();
                    }
                    else
                    {
                        return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(stateName),
                                ObjectCreationExpression(
                                    IdentifierName(stateType))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Argument(
                                                    IdentifierName("\"" + stateName + "\"")),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(entryFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(exitFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    LiteralExpression(
                                                        SyntaxKind.TrueLiteralExpression)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("StateTemperature"),
                                                        IdentifierName("Cold")))})))))
                        .NormalizeWhitespace();
                    }
                }
                else
                {
                    if (temperature == StateTemperature.WARM)
                    {
                        return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(stateName),
                                ObjectCreationExpression(
                                    IdentifierName(stateType))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Argument(
                                                    IdentifierName("\"" + stateName + "\"")),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(entryFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(exitFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    LiteralExpression(
                                                        SyntaxKind.FalseLiteralExpression)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("StateTemperature"),
                                                        IdentifierName("Warm")))})))))
                        .NormalizeWhitespace();
                    }
                    else if (temperature == StateTemperature.HOT)
                    {
                        return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(stateName),
                                ObjectCreationExpression(
                                    IdentifierName(stateType))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Argument(
                                                    IdentifierName("\"" + stateName + "\"")),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(entryFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(exitFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    LiteralExpression(
                                                        SyntaxKind.FalseLiteralExpression)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("StateTemperature"),
                                                        IdentifierName("Hot")))})))))
                        .NormalizeWhitespace();
                    }
                    else
                    {
                        return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(stateName),
                                ObjectCreationExpression(
                                    IdentifierName(stateType))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Argument(
                                                    IdentifierName("\"" + stateName + "\"")),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(entryFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(exitFun)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    LiteralExpression(
                                                        SyntaxKind.FalseLiteralExpression)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName("StateTemperature"),
                                                        IdentifierName("Cold")))})))))
                        .NormalizeWhitespace();
                    }
                }
            }
            //stopped here: write code for this method 
            public FunInfo GetFunInfo(string funName)
            {
                return null;
            }
            public SyntaxNode MkRealMachineClass()
            {
                //StartState property (getter only, since there's no setter in the base class):
                string startState = machInfo.initStateName;
                var startStateProperty =
                    PropertyDeclaration(
                        IdentifierName("PrtState"),
                        Identifier("StartState"))
                    .WithModifiers(
                        TokenList(
                            new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.OverrideKeyword)}))
                    .WithAccessorList(
                        AccessorList(
                            SingletonList<AccessorDeclarationSyntax>(
                                AccessorDeclaration(
                                    SyntaxKind.GetAccessorDeclaration,
                                    Block(
                                        SingletonList<StatementSyntax>(
                                            ReturnStatement(
                                                //TODO(bug): replace with init state name from machineInfo, + "_State"
                                                IdentifierName(startState))))))))
                    .NormalizeWhitespace();
                machineMembers.Add(startStateProperty);

                
                var skeletonMethodBody = generator.ReturnStatement(generator.ObjectCreationExpression(generator.IdentifierName(machName)));
                var skeletonMethodDecl = generator.MethodDeclaration("MakeSkeleton", null,
                  null, generator.IdentifierName("PrtImplMachine"),
                  Accessibility.Public,
                  DeclarationModifiers.Override,
                  new SyntaxNode[] { skeletonMethodBody });
                machineMembers.Add(skeletonMethodDecl);

                //(this.GetType())
                var returnMethodPars = new SyntaxNode[]
                {
                generator.InvocationExpression(generator.MemberAccessExpression(generator.ThisExpression(), generator.IdentifierName("GetType")))
                };
                //return app.NextMachineInstanceNumber(this.GetType());
                var nextInstanceNumberMethodBody = generator.ReturnStatement(generator.InvocationExpression(generator.MemberAccessExpression(
                     generator.IdentifierName("app"), generator.IdentifierName("NextMachineInstanceNumber")), returnMethodPars));

                //(StateImpl app)
                var methodPars = new SyntaxNode[] { generator.ParameterDeclaration("app", generator.IdentifierName("StateImpl")) };
                var nextInstanceNumberMethodDecl = generator.MethodDeclaration("NextInstanceNumber", methodPars,
                  //null, generator.IdentifierName("int"),
                  null, generator.TypeExpression(SpecialType.System_Int32),
                  Accessibility.Public,
                  DeclarationModifiers.Override,
                  new SyntaxNode[] { nextInstanceNumberMethodBody });
                machineMembers.Add(nextInstanceNumberMethodDecl);
                
                //Name property (getter only):
                var nameProperty =
                    PropertyDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.StringKeyword)),
                        Identifier("Name"))
                    .WithModifiers(
                        TokenList(
                            new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.OverrideKeyword)}))
                    .WithAccessorList(
                        AccessorList(
                            SingletonList<AccessorDeclarationSyntax>(
                                AccessorDeclaration(
                                    SyntaxKind.GetAccessorDeclaration,
                                    Block(
                                        SingletonList<StatementSyntax>(
                                            ReturnStatement(
                                                IdentifierName("\"" + machName + "\""))))))))
                    .NormalizeWhitespace();
                machineMembers.Add(nameProperty);

                //constructor for cloning
                var constructor_1 = generator.ConstructorDeclaration(machName, null, Accessibility.Public, baseConstructorArguments: new SyntaxNode[0]);
                machineMembers.Add(constructor_1);

                //Machine class constructor
                //public PONG(StateImpl app, int maxB, bool assume): base (app, maxB, assume)
                //TODO(expand): add inits for all fields
                var constructorPars = new SyntaxNode[]
                {
                generator.ParameterDeclaration("app", generator.IdentifierName("StateImpl")),
                generator.ParameterDeclaration("maxB", generator.TypeExpression(SpecialType.System_Int32)),
                generator.ParameterDeclaration("assume", generator.TypeExpression(SpecialType.System_Boolean))
                };
                var baseConstructorArgs = new SyntaxNode[] { generator.IdentifierName("app"), generator.IdentifierName("maxB"), generator.IdentifierName("assume") };
                var constructor_2 = generator.ConstructorDeclaration(machName, constructorPars, Accessibility.Public,
                                    baseConstructorArguments: baseConstructorArgs);
                machineMembers.Add(constructor_2);

                //TODO(expand): getters and setters
                
                //#region Functions

                //TODO(expand): generate functions declared in the machine (not state-specific)

                //classes for functions for each state of the machine
                //and variable declarations for those functions:
                foreach (var pair in machInfo.stateNameToStateInfo)
                {
                    //entry function of the state:
                    var funName = pair.Value.entryActionName;
                    FunInfo funInfo = GetFunInfo(funName);
                    //TODO(remove)
                    Console.WriteLine("Entry func name for state {0}: {1}", pair.Key, funName);
                    MkFunctionDecl funDecl = new MkFunctionDecl(funName, funInfo, false, this);
                    funDecl.AddFunClass();

                    //exit function of the state: 
                    funName = pair.Value.exitFunName;
                    //TODO(remove)
                    Console.WriteLine("Exit func name for state {0}: {1}", pair.Key, funName);
                    funInfo = GetFunInfo(funName);
                    funDecl = new MkFunctionDecl(funName, funInfo, false, this);
                    funDecl.AddFunClass();

                    //Functions in transitions:
                    foreach (var transition in pair.Value.transitions)
                    {
                        funName = transition.Value.transFunName;
                        funInfo = GetFunInfo(funName);

                        if (!transition.Value.IsPush && !translator.allStaticFuns.ContainsKey(funName))
                        {
                            //TODO(remove)
                            Console.WriteLine("For goto transition: func name for state {0}: {1}", pair.Key, funName);
                            funDecl = new MkFunctionDecl(funName, funInfo, false, this);
                            funDecl.AddFunClass();
                        }
                    }

                    //Functions in dos: loop over StateInfo.actions (to be renamed into StateInfo.dos):
                    foreach (var doFun in pair.Value.actions)
                    {
                        funName = doFun.Value;
                        funInfo = GetFunInfo(funName);
                        //TODO(remove)
                        Console.WriteLine("For Do declaration: func name for state {0}: {1}", pair.Key, funName);
                        if (!translator.allStaticFuns.ContainsKey(funName))
                        {
                            funDecl = new MkFunctionDecl(funName, funInfo, false, this);
                            funDecl.AddFunClass();
                        } 
                    }

                }

                //State classes for all states of the machine:
                foreach (var pair in machInfo.stateNameToStateInfo)
                {
                    string stateName = pair.Key;
                    string stateType = stateName + "_Class";
                    //state class declaration:
                    var stateClassDecl =
                        ClassDeclaration(stateType)
                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.PublicKeyword)))
                            .WithBaseList(
                                BaseList(
                                    SingletonSeparatedList<BaseTypeSyntax>(
                                        SimpleBaseType(
                                            IdentifierName("PrtState")))))
                            .WithMembers(
                                SingletonList<MemberDeclarationSyntax>(
                                    ConstructorDeclaration(
                                        Identifier(stateType))
                                    .WithModifiers(
                                        TokenList(
                                            Token(SyntaxKind.PublicKeyword)))
                                    .WithParameterList(
                                        ParameterList(
                                            SeparatedList<ParameterSyntax>(
                                                new SyntaxNodeOrToken[]{
                                                    Parameter(
                                                        Identifier("name"))
                                                    .WithType(
                                                        PredefinedType(
                                                            Token(SyntaxKind.StringKeyword))),
                                                    Token(SyntaxKind.CommaToken),
                                                    Parameter(
                                                        Identifier("entryFun"))
                                                    .WithType(
                                                        IdentifierName("PrtFun")),
                                                    Token(SyntaxKind.CommaToken),
                                                    Parameter(
                                                        Identifier("exitFun"))
                                                    .WithType(
                                                        IdentifierName("PrtFun")),
                                                    Token(SyntaxKind.CommaToken),
                                                    Parameter(
                                                        Identifier("hasNullTransition"))
                                                    .WithType(
                                                        PredefinedType(
                                                            Token(SyntaxKind.BoolKeyword))),
                                                    Token(SyntaxKind.CommaToken),
                                                    Parameter(
                                                        Identifier("temperature"))
                                                    .WithType(
                                                        IdentifierName("StateTemperature"))})))
                                    .WithInitializer(
                                        ConstructorInitializer(
                                            SyntaxKind.BaseConstructorInitializer,
                                            ArgumentList(
                                                SeparatedList<ArgumentSyntax>(
                                                    new SyntaxNodeOrToken[]{
                                                        Argument(
                                                            IdentifierName("name")),
                                                        Token(SyntaxKind.CommaToken),
                                                        Argument(
                                                            IdentifierName("entryFun")),
                                                        Token(SyntaxKind.CommaToken),
                                                        Argument(
                                                            IdentifierName("exitFun")),
                                                        Token(SyntaxKind.CommaToken),
                                                        Argument(
                                                            IdentifierName("hasNullTransition")),
                                                        Token(SyntaxKind.CommaToken),
                                                        Argument(
                                                            IdentifierName("temperature"))}))))
                                    .WithBody(
                                        Block())))
                        .NormalizeWhitespace();
                    machineMembers.Add(stateClassDecl);

                    //state declaration: 
                    //Example: public static Init Init_State;
                    var stateDeclaration = 
                        FieldDeclaration(
                            VariableDeclaration(
                                IdentifierName(stateType))
                            .WithVariables(
                                SingletonSeparatedList<VariableDeclaratorSyntax>(
                                    VariableDeclarator(
                                        Identifier(stateName)))))
                        .WithModifiers(
                            TokenList(
                                new[]{
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.StaticKeyword)}))
                        .NormalizeWhitespace();
                    machineMembers.Add(stateDeclaration);

                    //state instantiation for the machine class constructor:
                    //Example: 
                    //PONG_Pong_WaitPing = new PONG_Pong_WaitPing_Class("PONG_Pong_WaitPing", AnonFun1, AnonFun0, false, StateTemperature.Warm);
                    //Consider 6 cases (write a mmethod): for each bool value of hasNullTransition, there are 3 possible "temperatures"
                    //TODO(remove):
                    Console.WriteLine("hasNullTransition for state {0} is {1}", stateName, pair.Value.hasNullTransition);
                    mainConstructorFields.Add(MkStateInstantiation(stateName, stateType, pair.Value.entryActionName, pair.Value.exitFunName,
                                              pair.Value.hasNullTransition, pair.Value.temperature));

                    //Add DoDecls to the StateInfo:
                    //Example: Main_Ping_SendPing.dos.Add(Pong, foo);
                    //Note: "ignore E" is considered a DeDecl with the default "ignore" function, and is present in StateInfo.actions
                    //TODO(question): do I have to use pre-defined 
                    //public class PrtIgnoreFun : PrtFun
                    //for "ignore" function?
                    foreach (var doFun in pair.Value.actions)
                    {
                        mainConstructorFields.Add(
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(stateName),
                                            IdentifierName("dos")),
                                        IdentifierName("Add")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Argument(
                                                    IdentifierName(doFun.Key)),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    IdentifierName(doFun.Value))}))))
                            .NormalizeWhitespace()
                            );
                    }

                    //Add deferred events (if any) to the state:
                    foreach (var deferredEvent in pair.Value.deferredEvents)
                    {
                        mainConstructorFields.Add(
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(stateName),
                                            IdentifierName("deferredSet")),
                                        IdentifierName("Add")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList<ArgumentSyntax>(
                                            Argument(
                                                IdentifierName(deferredEvent))))))
                            .NormalizeWhitespace()
                        );
                    }

                    //Transition instantiations for the state of the machine:

                    //"transition" + "_" + transition_count
                    foreach (var transition in pair.Value.transitions)
                    {
                        string trigger = transition.Key;
                        string transition_name = "transition" + "_" + transition_count;
                        //For push transition, transition.Value.transFunName name is null - 
                        //replacing wuth PrtCommonFunctions.SkipFun
                        if (transition.Value.IsPush)
                        {
                            //push transition:
                            mainConstructorFields.Add(
                            LocalDeclarationStatement(
                                VariableDeclaration(
                                    IdentifierName("PrtTransition"))
                                .WithVariables(
                                    SingletonSeparatedList<VariableDeclaratorSyntax>(
                                        VariableDeclarator(
                                            Identifier(transition_name))
                                        .WithInitializer(
                                            EqualsValueClause(
                                                ObjectCreationExpression(
                                                    IdentifierName("PrtTransition"))
                                                .WithArgumentList(
                                                    ArgumentList(
                                                        SeparatedList<ArgumentSyntax>(
                                                            new SyntaxNodeOrToken[]{
                                                                Argument(
                                                                    MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        IdentifierName("PrtCommonFunctions"),
                                                                        IdentifierName("SkipFun"))),
                                                                        Token(SyntaxKind.CommaToken),
                                                                Argument(
                                                                    IdentifierName(transition.Value.target)),
                                                                Token(SyntaxKind.CommaToken),
                                                                Argument(
                                                                    LiteralExpression(
                                                                        SyntaxKind.TrueLiteralExpression))}))))))))
                            .NormalizeWhitespace()); 
                        }
                        else
                        {
                            //goto transition:
                            mainConstructorFields.Add(
                            LocalDeclarationStatement(
                                VariableDeclaration(
                                    IdentifierName("PrtTransition"))
                                .WithVariables(
                                    SingletonSeparatedList<VariableDeclaratorSyntax>(
                                        VariableDeclarator(
                                            Identifier("transition" + "_" + transition_count))
                                        .WithInitializer(
                                            EqualsValueClause(
                                                ObjectCreationExpression(
                                                    IdentifierName("PrtTransition"))
                                                .WithArgumentList(
                                                    ArgumentList(
                                                        SeparatedList<ArgumentSyntax>(
                                                            new SyntaxNodeOrToken[]{
                                                                Argument(
                                                                    IdentifierName(transition.Value.transFunName)),
                                                                Token(SyntaxKind.CommaToken),
                                                                Argument(
                                                                    IdentifierName(transition.Value.target)),
                                                                Token(SyntaxKind.CommaToken),
                                                                Argument(
                                                                    LiteralExpression(
                                                                        SyntaxKind.FalseLiteralExpression))}))))))))
                            .NormalizeWhitespace());
                        }
                        //Add transition to the state:
                        mainConstructorFields.Add(
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(stateName),
                                            IdentifierName("transitions")),
                                        IdentifierName("Add")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                    Argument(
                                                        IdentifierName(trigger)),
                                                    Token(SyntaxKind.CommaToken),
                                                    Argument(
                                                        IdentifierName(transition_name))}))))
                            .NormalizeWhitespace()
                            );
                        transition_count += 1;
                    }
                }

                //Constructor for the machine class:
                //static Machine()
                var mainConstructor =
                    ConstructorDeclaration(
                        Identifier(machName))
                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.StaticKeyword)))
                            .WithBody(
                                Block(mainConstructorFields
                                    ))
                    .NormalizeWhitespace();

                //SyntaxNode mainConstructor = generator.ConstructorDeclaration(machName, modifiers: DeclarationModifiers.Static,
                //    statements: mainConstructorFields);

                machineMembers.Add(mainConstructor);

                var mainMachineClassDecl = generator.ClassDeclaration(
                  machName, typeParameters: null,
                  accessibility: Accessibility.Public,
                  baseType: generator.IdentifierName("PrtImplMachine"),
                  members: machineMembers);

                return mainMachineClassDecl;
            }
            
            public SyntaxNode MkSpecMachineClass()
            {
                //TODO(expand)
                return null;
            }
        }
        private void MkCSharpOutput()
        {
            var applicationcClassDeclaration = generator.ClassDeclaration(
              "Application", typeParameters: null,
              accessibility: Accessibility.Public,
              baseType: generator.IdentifierName("StateImpl"),
              members: members);

            // Declare a namespace
            //Generate namespace name: <output file name, no ".cs"> 
            int index = this.csharpFileName.LastIndexOf(".");
            string namespaceName = this.csharpFileName.Substring(0, index);
            var programNameSpaceDeclaration = generator.NamespaceDeclaration(namespaceName, applicationcClassDeclaration);

            List<SyntaxNode> usingDirectivesList = new List<SyntaxNode>();
            usingDirectivesList.Add(generator.NamespaceImportDeclaration("P.Runtime"));
            usingDirectivesList.Add(generator.NamespaceImportDeclaration("System.Collections.Generic"));

            // Get a CompilationUnit (code file) for the generated code
            //var newNode = generator.CompilationUnit(programNameSpaceDeclaration).
            // NormalizeWhitespace();

            // Get a CompilationUnit (code file) for the generated code
            result = generator.CompilationUnit(usingDirectivesList[0], usingDirectivesList[1], programNameSpaceDeclaration).
              NormalizeWhitespace();
        }

        private void EmitCSharpOutput(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            using (StringWriter writer = new StringWriter(sb))
            {
                result.WriteTo(writer);

                //Debug only:
                //Console.WriteLine(writer);
                
                System.IO.StreamWriter file = new System.IO.StreamWriter(fileName);
                file.WriteLine(result);

                file.Close();

            }
        }

        #endregion
    }
    
}
