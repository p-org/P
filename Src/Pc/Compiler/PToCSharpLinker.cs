using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.Formula.API;
using Microsoft.Formula.API.ASTQueries;
using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc
{
    class PToCSharpLinker
    {
        private SyntaxGenerator generator;
        private List<string> inputFiles;
        private ICompilerOutput Log { get; set; }
        internal class TestCaseInfo
        {
            public Dictionary<string, Dictionary<string, string>> linkMap;
            public Dictionary<string, string> machineDefMap;
            public Dictionary<string, bool> isSafeMap;
            public Dictionary<string, List<string>> specMachineMap;
            public Dictionary<string, List<string>> interfaceMap;
            public HashSet<string> sendActions;
            public HashSet<string> createActions;
            public string mainInterface;
            public TestCaseInfo()
            {
                linkMap = new Dictionary<string, Dictionary<string, string>>();
                machineDefMap = new Dictionary<string, string>();
                isSafeMap = new Dictionary<string, bool>();
                specMachineMap = new Dictionary<string, List<string>>();
                interfaceMap = new Dictionary<string, System.Collections.Generic.List<string>>();
                sendActions = new HashSet<string>();
                createActions = new HashSet<string>();
                mainInterface = "Main";
            }
        }

        private Dictionary<string, TestCaseInfo> allTests;
        private Dictionary<string, List<string>> dependsOn;
        public PToCSharpLinker(ICompilerOutput log, AST<Program> linkerModel, List<string> inputFilesNames)
        {
            this.inputFiles = inputFilesNames.ToList();
            Log = log;
            allTests = new Dictionary<string, TestCaseInfo>();
            dependsOn = new Dictionary<string, List<string>>();
            GenerateLinkerInfo(linkerModel);
        }

        void GenerateLinkerInfo(AST<Program> model)
        {
            
            var linkerModel = model.FindAny(
                new NodePred[] { NodePredFactory.Instance.MkPredicate(NodeKind.Program), NodePredFactory.Instance.MkPredicate(NodeKind.Model) });

            var factBins = new Dictionary<string, LinkedList<AST<FuncTerm>>>();
            linkerModel.FindAll(
                new NodePred[]
                {
                    NodePredFactory.Instance.Star,
                    NodePredFactory.Instance.MkPredicate(NodeKind.ModelFact)
                },

                (path, n) =>
                {
                    var mf = (ModelFact)n;
                    FuncTerm ft = (FuncTerm)mf.Match;
                    GetBin(factBins, ft).AddLast((AST<FuncTerm>)Factory.Instance.ToAST(ft));
                });
            LinkedList<AST<FuncTerm>> terms;

            
            terms = GetBin(factBins, "CSharpLinkMap");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var currMachineName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var IorMName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var impMachineName = ((Cnst)it.Current).GetStringValue();

                    if (allTests.ContainsKey(name))
                    {
                        if (allTests[name].linkMap.ContainsKey(currMachineName))
                        {
                            allTests[name].linkMap[currMachineName].Add(IorMName, impMachineName);
                        }
                        else
                        {
                            allTests[name].linkMap[currMachineName] = new Dictionary<string, string>();
                            allTests[name].linkMap[currMachineName].Add(IorMName, impMachineName);
                        }
                    }
                    else
                    {
                        allTests[name] = new TestCaseInfo();
                        allTests[name].linkMap[currMachineName] = new Dictionary<string, string>();
                        allTests[name].linkMap[currMachineName].Add(IorMName, impMachineName);
                    }
                }
            }

            terms = GetBin(factBins, "CSharpRenameMap");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var renamedMachineName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var impName = ((Cnst)it.Current).GetStringValue();
                   
                    if (allTests.ContainsKey(name))
                    {
                        if (allTests[name].machineDefMap.ContainsKey(renamedMachineName))
                        {
                            Console.WriteLine("Internal Error");
                            Environment.Exit(-1);
                        }
                        else
                        {
                            allTests[name].machineDefMap.Add(renamedMachineName, impName);
                        }
                    }
                    else
                    {
                        allTests[name] = new TestCaseInfo();
                        allTests[name].machineDefMap.Add(renamedMachineName, impName);
                    }
                }
            }

            terms = GetBin(factBins, "CSharpDependsOn");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var currFileName = ((Cnst)it.Current).GetStringValue();
                    currFileName = Path.GetFileNameWithoutExtension(currFileName);
                    it.MoveNext();
                    
                    if (it.Current.NodeKind == NodeKind.Id)
                    {
                        var name = ((Id)it.Current).Name;
                        if (name == "NIL")
                        {
                            if (!dependsOn.ContainsKey(currFileName))
                            { 
                                dependsOn[currFileName] = new List<string>();
                            }
                        }
                    }
                    else
                    {
                        var dOn = ((Cnst)it.Current).GetStringValue();
                        dOn = Path.GetFileNameWithoutExtension(dOn);

                        if (dependsOn.ContainsKey(currFileName))
                        {
                            dependsOn[currFileName].Add(dOn);
                        }
                        else
                        {
                            dependsOn[currFileName] = new List<string>();
                            dependsOn[currFileName].Add(dOn);
                        }
                    }
                    
                }
            }

            terms = GetBin(factBins, "CSharpSafeMap");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var renamedMachineName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var isSafe = (it.Current as Id).Name == PData.Cnst_True.Node.Name;

                    if (allTests.ContainsKey(name))
                    {
                        if (allTests[name].isSafeMap.ContainsKey(renamedMachineName))
                        {
                            Console.WriteLine("Internal Error");
                            Environment.Exit(-1);
                        }
                        else
                        {
                            allTests[name].isSafeMap.Add(renamedMachineName, isSafe);
                        }
                    }
                    else
                    {
                        allTests[name] = new TestCaseInfo();
                        allTests[name].isSafeMap.Add(renamedMachineName, isSafe);
                    }
                }
            }

            terms = GetBin(factBins, "CSharpMonitorMap");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var newSpecMachineName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var impMachine = ((Cnst)it.Current).GetStringValue();

                    if (allTests.ContainsKey(name))
                    {
                        if (allTests[name].specMachineMap.ContainsKey(newSpecMachineName))
                        {
                            allTests[name].specMachineMap[newSpecMachineName].Add(impMachine);
                        }
                        else
                        {
                            allTests[name].specMachineMap[newSpecMachineName] = new List<string>();
                            allTests[name].specMachineMap[newSpecMachineName].Add(impMachine);
                        }
                    }
                    else
                    {
                        allTests[name] = new TestCaseInfo();
                        allTests[name].specMachineMap[newSpecMachineName] = new List<string>();
                        allTests[name].specMachineMap[newSpecMachineName].Add(impMachine);
                    }
                }
            }

            terms = GetBin(factBins, "CSharpInterfaceMap");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var iname = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var evname = it.Current is Cnst ? ((Cnst)it.Current).GetStringValue() : "halt";


                    var testInfo = new TestCaseInfo();
                    if (allTests.ContainsKey(name))
                    {
                        if (allTests[name].interfaceMap.ContainsKey(iname))
                        {
                            allTests[name].interfaceMap[iname].Add(evname);
                        }
                        else
                        {
                            allTests[name].interfaceMap[iname] = new List<string>();
                            allTests[name].interfaceMap[iname].Add(evname);
                        }
                    }
                    else
                    {
                        allTests[name] = new TestCaseInfo();
                        allTests[name].interfaceMap[iname] = new List<string>();
                        allTests[name].interfaceMap[iname].Add(evname);
                    }
                }
            }

            terms = GetBin(factBins, "CSharpMainInterfaceName");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var imain = ((Cnst)it.Current).GetStringValue();
                    var testInfo = new TestCaseInfo();
                    if (allTests.ContainsKey(name))
                    {
                        allTests[name].mainInterface = imain;
                    }
                    else
                    {
                        allTests[name] = new TestCaseInfo();
                        allTests[name].mainInterface = imain;
                    }
                }
            }

            terms = GetBin(factBins, "CSharpEventActionsRefinementTest");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var eventName = ((Cnst)it.Current).GetStringValue();

                    var lhsName = string.Format("{0}LHS", name);
                    var rhsName = string.Format("{0}RHS", name);
                    allTests[lhsName].sendActions.Add(eventName);
                    allTests[rhsName].sendActions.Add(eventName);
                }
            }

            terms = GetBin(factBins, "CSharpInterfaceActionsRefinementTest");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var name = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    var eventName = ((Cnst)it.Current).GetStringValue();

                    var lhsName = string.Format("{0}LHS", name);
                    var rhsName = string.Format("{0}RHS", name);
                    allTests[lhsName].createActions.Add(eventName);
                    allTests[rhsName].createActions.Add(eventName);
                }
            }
        }

        public LinkedList<AST<FuncTerm>> GetBin(Dictionary<string, LinkedList<AST<FuncTerm>>> factBins, FuncTerm ft)
        {
            var fun = (Id)ft.Function;
            return GetBin(factBins, fun.Name);
        }

        public LinkedList<AST<FuncTerm>> GetBin(Dictionary<string, LinkedList<AST<FuncTerm>>> factBins, string name)
        {
            LinkedList<AST<FuncTerm>> bin;
            if (!factBins.TryGetValue(name, out bin))
            {
                bin = new LinkedList<AST<FuncTerm>>();
                factBins.Add(name, bin);
            }
            return bin;
        }

        private SyntaxNode MkEvents()
        {
            List<SyntaxNode> evDeclarations = new List<SyntaxNode>();
            List<StatementSyntax> eventInitializationStmts = new List<StatementSyntax>();
            string eventsClassName = "Events";

            //add halt event
            evDeclarations.Add(
                CSharpHelper.MkCSharpFieldDeclarationWithInit(SyntaxFactory.IdentifierName("PrtEventValue"),
                                                              "halt",
                                                              SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                                              SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                                              SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("PrtValue"), SyntaxFactory.IdentifierName("halt"))
                )
            );

            //add null event
            evDeclarations.Add(
                CSharpHelper.MkCSharpFieldDeclarationWithInit(SyntaxFactory.IdentifierName("PrtEventValue"),
                                                              "@null",
                                                              SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                                              SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                                                              SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("PrtValue"), SyntaxFactory.IdentifierName("@null"))
                )
            );

            var eventsClass = generator.ClassDeclaration(
                eventsClassName, typeParameters: null,
                accessibility: Accessibility.Public,
                modifiers: DeclarationModifiers.Partial,
                members: evDeclarations);

            return eventsClass;
        }
        private List<SyntaxNode> MkAppConstructors(string testName)
        {

            //parameterless constructor
            List<SyntaxNode> constructorList = new List<SyntaxNode>();
            var constructor_1 = generator.ConstructorDeclaration("Application", null, Accessibility.Public, baseConstructorArguments: new SyntaxNode[0]);
            constructorList.Add(constructor_1);

            //constructor with the initialize paramenter which is called by Ptester at the start.
            var constructorParameters = new SyntaxNode[] {
                generator.ParameterDeclaration("initialize",
                                               generator.TypeExpression(SpecialType.System_Boolean)) };
            List<SyntaxNode> stmtList = new List<SyntaxNode>();


            foreach (var monName in allTests[testName].specMachineMap.Keys)
            {
                stmtList.Add(SyntaxFactory.ExpressionStatement(
                                 CSharpHelper.MkCSharpInvocationExpression(SyntaxFactory.IdentifierName("CreateSpecMachine"), CSharpHelper.MkCSharpStringLiteralExpression(monName))));
            }
            

            var constructorBody = generator.ExpressionStatement(
                generator.InvocationExpression(
                    generator.IdentifierName("CreateMainMachine"), generator.LiteralExpression(allTests[testName].mainInterface)));
            stmtList.Add(constructorBody);
            
            var constructor_2 = generator.ConstructorDeclaration("Application", constructorParameters, Accessibility.Public, baseConstructorArguments: new SyntaxNode[0],
                                                                 statements: stmtList.ToArray());
            constructorList.Add(constructor_2);

            //skeleton constructor used by the clone function.
            var makeSkeletonMethodBody = generator.ReturnStatement(generator.ObjectCreationExpression(generator.IdentifierName("Application")));
            var makeSkeletonMethodDecl = generator.MethodDeclaration("MakeSkeleton", null,
                                                                     null, generator.IdentifierName("StateImpl"),
                                                                     Accessibility.Public,
                                                                     DeclarationModifiers.Override,
                                                                     new SyntaxNode[] { makeSkeletonMethodBody });
            constructorList.Add(makeSkeletonMethodDecl);

            return constructorList;
        }


        private SyntaxNode MkStaticAppConstructor(string testName)
        {

            
            var stmtList = new List<StatementSyntax>();

            //Initialize types and events
            var nodes = dependsOn.Keys.Select(s => s.ToLower()).ToList();
            foreach(var files in dependsOn.Values)
            {
                foreach(var f in files)
                {
                    if(!nodes.Contains(f))
                    {
                        nodes.Add(f);
                    }
                }
            }
            var edges = new List<Tuple<string, string>>();
            foreach (var file in dependsOn)
            {
                foreach (var dep in file.Value)
                {
                    if (file.Key != dep)
                    {
                        edges.Add(new Tuple<string, string>(dep.ToLower(), file.Key.ToLower()));
                    }
                }
            }
            List<string> topoOrder = TopologicalSortFiles<string>(nodes, edges);
            //make invocations to the initialization of all types
            foreach (var fName in topoOrder)
            {
                stmtList.Add(
                    SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("Types"), SyntaxFactory.IdentifierName("Types_" + fName))
                                        )));
            }

            //make invocation to the initialization of all events
            foreach (var fName in topoOrder)
            {
                stmtList.Add(
                    SyntaxFactory.ExpressionStatement(CSharpHelper.MkCSharpInvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("Events"), SyntaxFactory.IdentifierName("Events_" + fName))
                                        )));
            }

            //Initialize all the maps

            //safe map
            foreach (var sm in allTests[testName].isSafeMap)
            {
                var safeadd = CSharpHelper.MkCSharpInvocationExpression(
                    CSharpHelper.MkCSharpDot("isSafeMap", "Add"),
                    CSharpHelper.MkCSharpStringLiteralExpression(sm.Key),
                    sm.Value ? CSharpHelper.MkCSharpTrueLiteralExpression() : CSharpHelper.MkCSharpFalseLiteralExpression()
                );
                stmtList.Add(SyntaxFactory.ExpressionStatement(safeadd));
            }

            //rename map
            foreach (var rename in allTests[testName].machineDefMap)
            {
                var renameadd = CSharpHelper.MkCSharpInvocationExpression(
                    CSharpHelper.MkCSharpDot("machineDefMap", "Add"),
                    CSharpHelper.MkCSharpStringLiteralExpression(rename.Key),
                    CSharpHelper.MkCSharpStringLiteralExpression(rename.Value)
                );
                stmtList.Add(SyntaxFactory.ExpressionStatement(renameadd));
            }
            //create map
            var listOfSpecMachineNames = allTests[testName].machineDefMap.Where(item => allTests[testName].specMachineMap.ContainsKey(item.Key)).Select(item => item.Value).Distinct();
            var listOfMachineNames = allTests[testName].machineDefMap.Where(item => !allTests[testName].specMachineMap.ContainsKey(item.Key)).Select(item => item.Value).Distinct();
            foreach (var specMachine in listOfSpecMachineNames)
            {
                var createadd = CSharpHelper.MkCSharpInvocationExpression(
                    CSharpHelper.MkCSharpDot("createSpecMap", "Add"),
                    CSharpHelper.MkCSharpStringLiteralExpression(specMachine),
                    SyntaxFactory.IdentifierName(string.Format("CreateSpecMachine_{0}", specMachine)));
                stmtList.Add(SyntaxFactory.ExpressionStatement(createadd));
            }
            foreach (var machine in listOfMachineNames)
            {
                //its a machine
                var createadd = CSharpHelper.MkCSharpInvocationExpression(
                    CSharpHelper.MkCSharpDot("createMachineMap", "Add"),
                    CSharpHelper.MkCSharpStringLiteralExpression(machine),
                    SyntaxFactory.IdentifierName(string.Format("CreateMachine_{0}", machine))
                );
                stmtList.Add(SyntaxFactory.ExpressionStatement(createadd));
 
            }
            //interface map
            foreach (var it in allTests[testName].interfaceMap)
            {
                var eventsParams = new SeparatedSyntaxList<ExpressionSyntax>();
                foreach (var ev in it.Value)
                {
                    eventsParams = eventsParams.Add(GetEventVar(ev));
                }
                var initStmt = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                    SyntaxFactory.ElementAccessExpression(
                        SyntaxFactory.IdentifierName("interfaceMap"),
                        SyntaxFactory.BracketedArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(CSharpHelper.MkCSharpStringLiteralExpression(it.Key))))
                    ),
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.GenericName(SyntaxFactory.Identifier("List"), SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(SyntaxFactory.IdentifierName("PrtEventValue")))),
                        SyntaxFactory.ArgumentList(),
                        SyntaxFactory.InitializerExpression(SyntaxKind.CollectionInitializerExpression, eventsParams))
                );
                stmtList.Add(initStmt);
            }

            //send actions
            {
                var eventsParams = new SeparatedSyntaxList<ExpressionSyntax>();
                foreach (var ev in allTests[testName].sendActions)
                {
                    eventsParams = eventsParams.Add(CSharpHelper.MkCSharpStringLiteralExpression(ev));
                }
                var initStmt = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                    SyntaxFactory.IdentifierName("visibleEvents"),
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.GenericName(SyntaxFactory.Identifier("List"), SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(SyntaxFactory.IdentifierName("string")))),
                        SyntaxFactory.ArgumentList(),
                        SyntaxFactory.InitializerExpression(SyntaxKind.CollectionInitializerExpression, eventsParams))
                );
                stmtList.Add(initStmt);
            }

            //interface actions
            {
                var interfaceParams = new SeparatedSyntaxList<ExpressionSyntax>();
                foreach (var it in allTests[testName].createActions)
                {
                    interfaceParams = interfaceParams.Add(CSharpHelper.MkCSharpStringLiteralExpression(it));
                }
                var initStmt = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                    SyntaxFactory.IdentifierName("visibleInterfaces"),
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.GenericName(SyntaxFactory.Identifier("List"), SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(SyntaxFactory.IdentifierName("string")))),
                        SyntaxFactory.ArgumentList(),
                        SyntaxFactory.InitializerExpression(SyntaxKind.CollectionInitializerExpression, interfaceParams))
                );
                stmtList.Add(initStmt);
            }

            //initialize the interface types
            foreach (var it in allTests[testName].interfaceMap)
            {
                var initStmt = CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParenthesizedExpression(
                                               CSharpHelper.MkCSharpCastExpression("PrtInterfaceType",
                                                                                   SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("Types"), SyntaxFactory.IdentifierName(String.Format("type_{0}", it.Key))))),
                                           SyntaxFactory.IdentifierName("permissions")),
                    SyntaxFactory.ElementAccessExpression(
                        SyntaxFactory.IdentifierName("interfaceMap"),
                        SyntaxFactory.BracketedArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(CSharpHelper.MkCSharpStringLiteralExpression(it.Key))))
                    )
                );
                stmtList.Add(initStmt);
            }

            //spec machine map
            foreach (var specMachine in allTests[testName].specMachineMap)
            {
                SeparatedSyntaxList<ExpressionSyntax> machines = new SeparatedSyntaxList<ExpressionSyntax>();
                machines = machines.AddRange(specMachine.Value.Select(x => CSharpHelper.MkCSharpStringLiteralExpression(x)));

                var addstmt =
                    CSharpHelper.MkCSharpInvocationExpression(
                        CSharpHelper.MkCSharpDot("specMachineMap", "Add"),
                        CSharpHelper.MkCSharpStringLiteralExpression(specMachine.Key),
                        SyntaxFactory.ObjectCreationExpression(
                            CSharpHelper.MkCSharpGenericListType(SyntaxFactory.IdentifierName("string")),
                            SyntaxFactory.ArgumentList(),
                            SyntaxFactory.InitializerExpression(SyntaxKind.CollectionInitializerExpression, machines))
                    );
                stmtList.Add(SyntaxFactory.ExpressionStatement(addstmt));
            }
            //link map
            //declare temp variable 
            var dictStringString = 
                SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("Dictionary"),
                    SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList<TypeSyntax>(
                                         new SyntaxNodeOrToken[]{
                                             SyntaxFactory.PredefinedType(
                                                 SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                                             SyntaxFactory.Token(SyntaxKind.CommaToken),
                                             SyntaxFactory.PredefinedType(
                                                 SyntaxFactory.Token(SyntaxKind.StringKeyword))}))
                );
            stmtList.Add(
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        dictStringString,
                        SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier("_temp"))
                        )  
                    )
                )
            );
            foreach(var creatorM in allTests[testName].linkMap)
            {
                var tempInit = 
                    CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                        SyntaxFactory.IdentifierName("_temp"),
                        CSharpHelper.MkCSharpObjectCreationExpression(dictStringString)
                    );
                stmtList.Add(tempInit);
                foreach(var linkmapitem in creatorM.Value)
                {
                    stmtList.Add(
                        SyntaxFactory.ExpressionStatement(
                            CSharpHelper.MkCSharpInvocationExpression(
                                CSharpHelper.MkCSharpDot("_temp", "Add"),
                                CSharpHelper.MkCSharpStringLiteralExpression(linkmapitem.Key),
                                CSharpHelper.MkCSharpStringLiteralExpression(linkmapitem.Value)
                            ))
                    );
                }

                var addLinkItem = SyntaxFactory.ExpressionStatement(
                    CSharpHelper.MkCSharpInvocationExpression(
                        CSharpHelper.MkCSharpDot("linkMap", "Add"),
                        CSharpHelper.MkCSharpStringLiteralExpression(creatorM.Key),
                        SyntaxFactory.IdentifierName("_temp"))
                );
                stmtList.Add(addLinkItem);
            }



            

            var staticConstrutor = SyntaxFactory.ConstructorDeclaration(
                    SyntaxFactory.Identifier("Application"))
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithBody(SyntaxFactory.Block(stmtList))
                .NormalizeWhitespace();
            return staticConstrutor;
        }

        #region Topological Sorting Dependencies
        /// <summary>
        /// Topological Sorting (Kahn's algorithm) 
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Topological_sorting</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="nodes">All nodes of directed acyclic graph.</param>
        /// <param name="edges">All edges of directed acyclic graph.</param>
        /// <returns>Sorted node in topological order.</returns>
        static List<T> TopologicalSortFiles<T>(List<T> nodes, List<Tuple<T, T>> edges) where T : IEquatable<T>
        {
            // Empty list that will contain the sorted elements
            var L = new List<T>();

            // Set of all nodes with no incoming edges
            var S = new HashSet<T>(nodes.Where(n => edges.All(e => e.Item2.Equals(n) == false)));

            // while S is non-empty do
            while (S.Any())
            {

                //  remove a node n from S
                var n = S.First();
                S.Remove(n);

                // add n to tail of L
                L.Add(n);

                // for each node m with an edge e from n to m do
                foreach (var e in edges.Where(e => e.Item1.Equals(n)).ToList())
                {
                    var m = e.Item2;

                    // remove edge e from the graph
                    edges.Remove(e);

                    // if m has no other incoming edges then
                    if (edges.All(me => me.Item2.Equals(m) == false))
                    {
                        // insert m into S
                        S.Add(m);
                    }
                }
            }

            // if graph has edges then
            if (edges.Any())
            {
                // return error (graph has at least one cycle)
                return null;
            }
            else
            {
                // return L (a topologically sorted order)
                return L;
            }
        }
        #endregion

        public static string EventName(string rawName)
        {
            if (rawName == "null")
            {
                return "@null";
            }
            else if (rawName == "halt")
            {
                return rawName;
            }
            else
            {
                return String.Format("event_{0}", rawName);
            }
        }

        public ExpressionSyntax GetEventVar(string eventName)
        {
            var eventClass = "Events";
            var retVal = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(eventClass), SyntaxFactory.IdentifierName(EventName(eventName)));
            return retVal;
        }

        public bool GenerateCSharpLinkerOutput(string outputDir)
        {
            bool success = true;
            foreach(var testCase in allTests)
            {
                //make sure test case has a main file
                if(!testCase.Value.machineDefMap.ContainsKey(testCase.Value.mainInterface))
                {
                    Log.WriteMessage(string.Format($"No Main {testCase.Value.mainInterface}, cannot generate {0}.dll", testCase.Key), SeverityKind.Error);
                    return false;
                }
                SyntaxNode finalOutput = null;

                var workspace = new AdhocWorkspace();
                // Get the SyntaxGenerator for the specified language
                generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);

                var members = new List<SyntaxNode>();
                // Create using/Imports directives
                var usingDirectives = generator.NamespaceImportDeclaration("System");

                members.Add(MkEvents());
                members.AddRange(MkAppConstructors(testCase.Key));
                members.Add(MkStaticAppConstructor(testCase.Key));

                //create the class
                var applicationcClassDeclaration = generator.ClassDeclaration(
                    "Application", typeParameters: null,
                    modifiers: DeclarationModifiers.Partial,
                    accessibility: Accessibility.Public,
                    baseType: generator.IdentifierName("StateImpl"),
                    members: members);
                var programNameSpaceDeclaration = generator.NamespaceDeclaration(
                    SyntaxFactory.IdentifierName("P.Program"),
                    SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("P.Runtime")),
                    SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System")),
                    SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System.Collections.Generic")), 
                    applicationcClassDeclaration);

                // Get a CompilationUnit (code file) for the generated code
                finalOutput = generator.CompilationUnit(
                                            generator.NamespaceImportDeclaration("P.Runtime"),
                                            generator.NamespaceImportDeclaration("System"),
                                            generator.NamespaceImportDeclaration("System.Collections.Generic"),
                                            programNameSpaceDeclaration).
                                        NormalizeWhitespace();
                var outputFile = Path.Combine(outputDir, testCase.Key + ".cs");
                EmitLinkerCS(finalOutput, outputFile);
                Log.WriteMessage(string.Format("Writing {0}.cs ...", testCase.Key), SeverityKind.Info);
                success = success & EmitCSDll(outputDir, testCase.Key);
            }
            return success;
        }

        private bool EmitCSDll(string outputDir, string testCaseName)
        {
            List<string> allCSFiles = new List<string>();

            allCSFiles.Add(Path.Combine(outputDir, testCaseName + ".cs"));
            allCSFiles.AddRange(inputFiles.Select(fileName => Path.ChangeExtension(fileName,".cs")).ToList());
            
            string cs_code = "";
            foreach(var file in allCSFiles)
            {
                if (!File.Exists(file))
                {
                    Log.WriteMessage(string.Format("{0} not found, recompile the corresponding P file", file), SeverityKind.Warning);
                    return false;
                }
                using (var sr = new StreamReader(file))
                {
                    cs_code = string.Concat(cs_code, sr.ReadToEnd());
                }
            }

            Log.WriteMessage(string.Format("Writing {0}.dll ...", testCaseName), SeverityKind.Info);
            var tree = CSharpSyntaxTree.ParseText(cs_code);

            var pruntime = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\prt.dll";
            if(!File.Exists(pruntime))
            {
                Log.WriteMessage(string.Format("could not find file {0}", pruntime), SeverityKind.Error);
                return false;
            }
            CSharpCompilation compilation = CSharpCompilation.Create(
                testCaseName,
                new[] { tree },
                new [] {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(pruntime),
                    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


            var outputDll = Path.Combine(outputDir, testCaseName + ".dll");
            var outputPdb = Path.Combine(outputDir, testCaseName + ".pdb");
            var emitResult = compilation.Emit(outputDll, outputPdb);

            //If our compilation failed, we can discover exactly why.
            if (!emitResult.Success)
            {
                Log.WriteMessage(string.Format("C# file corresponding to dll generated : {0}_dllerror.cs", testCaseName), SeverityKind.Error);
                using (var sw = new StreamWriter(Path.Combine(outputDir, testCaseName + "_dllerror.cs")))
                {
                    sw.WriteLine(tree);
                }
                foreach (var diagnostic in emitResult.Diagnostics)
                {
                    Log.WriteMessage(diagnostic.ToString(), SeverityKind.Error);
                }
                return false;
            }

            return true;
        }
        private void EmitLinkerCS(SyntaxNode finalOutput, string fileName)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(fileName);
            file.WriteLine("#pragma warning disable CS0162, CS0164, CS0168");
            file.WriteLine(finalOutput);
            file.Close();
        }
    }
}