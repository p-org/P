using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.Formula.API;
using Microsoft.Formula.API.Nodes;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Pc
{
    partial class PToCSharpCompiler : PTranslation
    {
        public PToCSharpCompiler(Compiler compiler, AST<Model> modelWithTypes, Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo, string cSharpFileName)
            : base(compiler, modelWithTypes, idToSourceInfo)
        {
            this.cSharpFileName = cSharpFileName;
            typeContext = new TypeTranslationContext(this);
            GenerateTypeInfo();
        }

        #region CSharpCompiler
        readonly string cSharpFileName;
        //for storing members of the Application class:
        private List<SyntaxNode> members = new List<SyntaxNode>();
        //final C# program:
        private SyntaxNode result;

        private SyntaxGenerator generator;

        #region Generate P Types
        private void GenerateTypeInfo()
        {
            var terms = GetBin(factBins, "TypeOf");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var typingContextAlias = Factory.Instance.ToAST(it.Current);
                    FuncTerm typingContext = aliasToTerm[typingContextAlias];
                    it.MoveNext();
                    var expr = Factory.Instance.ToAST(it.Current);
                    it.MoveNext();
                    var type = it.Current as FuncTerm;

                    string typingContextKind = ((Id)typingContext.Function).Name;
                    if (typingContextKind == "FunDecl")
                    {
                        string ownerName = GetOwnerName(typingContext, 1);
                        string funName = GetName(typingContext, 0);
                        if (ownerName == null)
                        {
                            allGlobalFuns[funName].typeInfo[expr] = type;
                        }
                        else
                        {
                            allMachines[ownerName].funNameToFunInfo[funName].typeInfo[expr] = type;
                        }
                    }
                    else
                    {
                        // typingContextKind == "AnonFunDecl"
                        string ownerName = GetOwnerName(typingContext, 0);
                        string funName = anonFunToName[typingContextAlias];
                        if (ownerName == null)
                        {
                            allGlobalFuns[funName].typeInfo[expr] = type;
                        }
                        else
                        {
                            allMachines[ownerName].funNameToFunInfo[funName].typeInfo[expr] = type;
                        }
                    }
                }
            }

            terms = GetBin(factBins, "ImportedType");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var typeName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    //ignore the duplicate imported types
                    if(!typeContext.ImportedTypes.ContainsKey(Factory.Instance.ToAST(it.Current)))
                    {
                        typeContext.ImportedTypes.Add(Factory.Instance.ToAST(it.Current), typeName);
                        typeContext.PTypeToCSharpExpr((FuncTerm)it.Current);
                    }
                    
                }
            }

            terms = GetBin(factBins, "ExportedType");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var typeName = ((Cnst)it.Current).GetStringValue();
                    it.MoveNext();
                    //if duplicate exported type then add it to duplicate and declare it separately in MkType()
                    if(typeContext.ExportedTypes.ContainsKey(Factory.Instance.ToAST(it.Current)))
                    {
                        typeContext.DuplicateExportedTypes.Add(typeName, it.Current as FuncTerm);
                    }
                    else
                    {
                        typeContext.ExportedTypes.Add(Factory.Instance.ToAST(it.Current), typeName);
                        typeContext.PTypeToCSharpExpr((FuncTerm)it.Current);
                    }
                    
                }
            }

            terms = GetBin(factBins, "TranslatedTypeExpr");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var eType = (FuncTerm)it.Current;
                    typeContext.PTypeToCSharpExpr(eType);
                }
            }

            terms = GetBin(factBins, "TypeExpansion");
            foreach (var term in terms)
            {
                using (var it = term.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var type = (FuncTerm)it.Current;
                    it.MoveNext();
                    var eType = (FuncTerm)it.Current;
                    typeContext.AddOriginalType(type, eType);
                }
            }
        }

        private readonly TypeTranslationContext typeContext;

        #endregion

        public static string EventName(string rawName)
        {
            switch (rawName) {
                case NullEvent:
                    return "@null";
                case HaltEvent:
                    return rawName;
                default:
                    return $"event_{rawName}";
            }
        }

        public static string StackFrameClassName(string rawName)
        {
            return $"{rawName}_StackFrame";
        }

        public static string VarName(string rawName)
        {
            return $"var_{rawName}";
        }

        public ExpressionSyntax GetEventVar(string eventName)
        {
            return MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Events"), IdentifierName(EventName(eventName)));
        }

        public void GenerateCSharp()
        {
            var workspace = new AdhocWorkspace();

            // Get the SyntaxGenerator for the specified language
            generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
            members = new List<SyntaxNode>();

            MkEvents();
            MkTypes();
            MkStaticFunctions();
            MkCreateMachineMethods();
            MkMachineClasses();
            MkCSharpOutput();
            EmitCSharpOutput();
        }

        private void MkEvents()
        {
            var evDeclarations = new List<SyntaxNode>();
            var eventInitializationStmts = new List<StatementSyntax>();
            const string eventsClassName = "Events";
            foreach (string evName in exportedEvents)
            {
                //add declaration
                evDeclarations.Add(
                    CSharpHelper.MkCSharpFieldDeclaration(
                        IdentifierName("PrtEventValue"),
                        EventName(evName),
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.StaticKeyword)));

                //add initialization
                SyntaxNode payloadType = typeContext.PTypeToCSharpExpr(allEvents[evName].payloadType);
                SyntaxNode maxInstances;
                if (allEvents[evName].maxInstances == -1)
                {
                    var s = "PrtEvent.DefaultMaxInstances";
                    maxInstances = IdentifierName(s);
                }
                else
                {
                    maxInstances = CSharpHelper.MkCSharpNumericLiteralExpression(allEvents[evName].maxInstances);
                }

                SyntaxNode doAssume = allEvents[evName].maxInstancesAssumed ? CSharpHelper.MkCSharpTrueLiteralExpression() : CSharpHelper.MkCSharpFalseLiteralExpression();
                SyntaxNode lhs = IdentifierName(EventName(evName));
                SyntaxNode rhs = CSharpHelper.MkCSharpObjectCreationExpression(
                    IdentifierName("PrtEventValue"),
                    CSharpHelper.MkCSharpObjectCreationExpression(
                        IdentifierName("PrtEvent"),
                        CSharpHelper.MkCSharpStringLiteralExpression(evName),
                        payloadType,
                        maxInstances,
                        doAssume));

                eventInitializationStmts.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(lhs, rhs));
            }

            string staticMethodName = "Events_" + Path.GetFileNameWithoutExtension(cSharpFileName.ToLower());
            MethodDeclarationSyntax staticInitializer =
                MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier(staticMethodName))
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                    .WithBody(Block(eventInitializationStmts)).NormalizeWhitespace();

            evDeclarations.Add(staticInitializer);

            SyntaxNode eventsClass = generator.ClassDeclaration(
                eventsClassName,
                null,
                Accessibility.Public,
                DeclarationModifiers.Partial,
                members: evDeclarations);

            members.Add(eventsClass);
        }

        private void MkTypes()
        {
            //add type declaration and initialization for duplicate exported types
            foreach (KeyValuePair<string, FuncTerm> dt in typeContext.DuplicateExportedTypes)
            {
                string typeName = typeContext.GetNextTypeName(dt.Key);
                ExpressionSyntax typeExpr = typeContext.GetTypeExpr(typeName);
                ExpressionSyntax declaredTypeExpr = typeContext.PTypeToCSharpExpr(dt.Value);
                typeContext.AddTypeDeclaration(typeName);
                typeContext.AddTypeInitialization(typeExpr, declaredTypeExpr);
            }

            var typeDeclarations = new List<SyntaxNode>();
            typeDeclarations.AddRange(typeContext.TypeDeclaration);

            string staticMethodName = "Types_" + Path.GetFileNameWithoutExtension(cSharpFileName.ToLower());
            MethodDeclarationSyntax staticInitializer =
                MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier(staticMethodName))
                    .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.PublicKeyword)))
                    .WithBody(Block(typeContext.TypeInitialization)).NormalizeWhitespace();

            typeDeclarations.Add(staticInitializer);

            SyntaxNode typesClass = generator.ClassDeclaration(
                "Types",
                null,
                Accessibility.Public,
                DeclarationModifiers.Partial,
                members: typeDeclarations);

            members.Add(typesClass);
        }

        private void MkStaticFunctions()
        {
            foreach (KeyValuePair<string, FunInfo> pair in allGlobalFuns)
            {
                if (!pair.Value.isFunProto)
                {
                    var funDecl = new MkFunctionDecl(pair.Key, pair.Value, null, this);
                    funDecl.AddFunClass();
                }
            }
        }

        private void MkCreateMachineMethods()
        {
            foreach (var machineName in allMachines.Keys)
            {
                if (allMachines[machineName].IsReal)
                {
                    MkCreateRealMachineMethod(machineName);
                }
                else
                {
                    MkCreateSpecMachineMethod(machineName);
                }
            }
        }

        private void MkCreateRealMachineMethod(string machineName)
        {
            var fields = new List<SyntaxNode>();

            //create machine
            MachineInfo machineInfo = allMachines[machineName];
            //There are three cases:
            //- default (no constraint on queue size): maxQueueSizeAssumed == false; maxQueueSize = default (10?) 
            //- assume <maxQueueSize>: maxQueueSize > 0, maxQueueSizeAssumed == true;
            //- assert <maxQueueSize>: maxQueueSize > 0, maxQueueSizeAssumed == false;   
            if (machineInfo.maxQueueSize > 0)
            {
                fields.Add(
                    generator.LocalDeclarationStatement(
                        generator.IdentifierName("var"),
                        "machine",
                        generator.ObjectCreationExpression(
                            generator.IdentifierName(machineName),
                            new List<SyntaxNode>
                            {
                                generator.IdentifierName("application"),
                                generator.LiteralExpression(machineInfo.maxQueueSize),
                                generator.LiteralExpression(machineInfo.maxQueueSizeAssumed)
                            })));
            }
            else
            {
                fields.Add(
                    generator.LocalDeclarationStatement(
                        generator.IdentifierName("var"),
                        "machine",
                        generator.ObjectCreationExpression(
                            generator.IdentifierName(machineName),
                            new List<SyntaxNode>
                            {
                                generator.IdentifierName("application"),
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("PrtImplMachine"),
                                    IdentifierName("DefaultMaxBufferSize")),
                                generator.LiteralExpression(machineInfo.maxQueueSizeAssumed)
                            })));
            }

            // log
            /*fields.Add(
                CSharpHelper.MkCSharpTrace(
                    $"<CreateLog> Created Machine {machineName}-{{0}}",
                    CSharpHelper.MkCSharpDot("machine", "instanceNumber")));*/

            //initialize the permission set for self
            if (allMachines[machineName].receiveSet == null)
            {
                fields.Add(
                    CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                        CSharpHelper.MkCSharpDot(IdentifierName("machine"), "self", "permissions"),
                        LiteralExpression(SyntaxKind.NullLiteralExpression)));
            }
            else
            {
                fields.AddRange(
                    allMachines[machineName].receiveSet.Select(
                        ev => CSharpHelper.MkCSharpInvocationExpression(
                            CSharpHelper.MkCSharpDot(IdentifierName("machine"), "self", "permissions", "Add"),
                            GetEventVar(ev))));
            }

            //initialize the send set
            if (allMachines[machineName].sendsSet == null)
            {
                fields.Add(
                    CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                        CSharpHelper.MkCSharpDot(IdentifierName("machine"), "sends"),
                        LiteralExpression(SyntaxKind.NullLiteralExpression)));
            }
            else
            {
                fields.AddRange(
                    allMachines[machineName].sendsSet.Select(
                        ev => CSharpHelper.MkCSharpInvocationExpression(
                            CSharpHelper.MkCSharpDot(IdentifierName("machine"), "sends", "Add"),
                            GetEventVar(ev))));
            }

            //machine.currentPayload = payload;
            fields.Add(
                CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                    CSharpHelper.MkCSharpDot("machine", "currentPayload"),
                    IdentifierName("payload")));

            //return machine;
            fields.Add(generator.ReturnStatement(generator.IdentifierName("machine")));

            var methodPars = new[]
            {
                generator.ParameterDeclaration("application", generator.IdentifierName("StateImpl")),
                generator.ParameterDeclaration("payload", generator.IdentifierName("PrtValue"))
            };
            SyntaxNode makeCreateMachineDecl = generator.MethodDeclaration(
                $"CreateMachine_{machineName}",
                methodPars,
                null, // type parameters
                generator.IdentifierName("PrtImplMachine"),
                Accessibility.Public,
                DeclarationModifiers.Static,
                fields);
            members.Add(makeCreateMachineDecl);
        }

        private void MkCreateSpecMachineMethod(string machineName)
        {
            var fields = new List<SyntaxNode>();

            //create machine
            fields.Add(
                generator.LocalDeclarationStatement(
                    generator.IdentifierName("var"),
                    "machine",
                    generator.ObjectCreationExpression(
                        generator.IdentifierName(machineName),
                        new List<SyntaxNode> {generator.IdentifierName("application")})));

            //log
            //fields.Add(CSharpHelper.MkCSharpTrace($"<CreateLog> Created spec Machine {machineName}"));

            fields.AddRange(allMachines[machineName].observesEvents.Select(x => CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("machine", "observes", "Add"), GetEventVar(x))));

            var prtvalueNull =
                (ExpressionSyntax) generator.MemberAccessExpression(IdentifierName("PrtEventValue"), IdentifierName("@null"));
            //execute the entry function of the machine
            fields.Add(
                CSharpHelper.MkCSharpInvocationExpression(
                    CSharpHelper.MkCSharpDot("machine", "PrtEnqueueEvent"),
                    prtvalueNull,
                    prtvalueNull,
                    LiteralExpression(SyntaxKind.NullLiteralExpression)));
            //return machine;
            fields.Add(generator.ReturnStatement(generator.IdentifierName("machine")));

            var methodPars = new[] {generator.ParameterDeclaration("application", generator.IdentifierName("StateImpl"))};
            SyntaxNode makeCreateSpecDecl = generator.MethodDeclaration(
                $"CreateSpecMachine_{machineName}",
                methodPars,
                null,
                IdentifierName("PrtSpecMachine"),
                Accessibility.Public,
                DeclarationModifiers.Static,
                fields);
            members.Add(makeCreateSpecDecl);
        }

        private void MkMachineClasses()
        {
            //Debug only:
            //Console.WriteLine("Number of machines: {0}", allMachines.Count());
            foreach (var pair in allMachines)
            {
                var mkMachine = new MkMachineClass(this, pair.Key, pair.Value);
                members.Add(mkMachine.MkClass());
            }
        }

        private void MkCSharpOutput()
        {
            SyntaxNode applicationcClassDeclaration = generator.ClassDeclaration(
                "Application",
                null,
                modifiers: DeclarationModifiers.Partial,
                accessibility: Accessibility.Public,
                baseType: generator.IdentifierName("StateImpl"),
                members: members);

            // Declare a namespace
            //Generate namespace name: <output file name, no ".cs"> 
            //int index = this.cSharpFileName.LastIndexOf(".");
            //string namespaceName = this.cSharpFileName.Substring(0, index);
            //namespace is "P.Program" for all P programs:
            SyntaxNode programNameSpaceDeclaration = generator.NamespaceDeclaration(
                IdentifierName("P.Program"),
                UsingDirective(IdentifierName("P.Runtime")),
                UsingDirective(IdentifierName("System")),
                UsingDirective(IdentifierName("System.Collections.Generic")),
                applicationcClassDeclaration);

            // Get a CompilationUnit (code file) for the generated code
            result = generator.CompilationUnit(programNameSpaceDeclaration).NormalizeWhitespace();
        }

        private void EmitCSharpOutput()
        {
            var outputDir = compiler.Options.outputDir;
            compiler.Log.WriteMessage($"Writing {cSharpFileName} ...", SeverityKind.Info);
            var file = new StreamWriter(Path.Combine(outputDir, cSharpFileName));
            // CS0162 = Unreachable code
            // CS0164 = Goto label declared, but unused
            // CS0168 = Variable declared, but unused
            // CS0649 = Field never assigned a value
            file.WriteLine("#pragma warning disable CS0162, CS0164, CS0168, CS0649");
            file.WriteLine(result);
            file.Close();
        }

        #endregion
    }
}

