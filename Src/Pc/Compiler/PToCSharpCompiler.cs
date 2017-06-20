using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.IO;

using Microsoft.Formula.API;
using Microsoft.Formula.API.Nodes;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Pc
{
    partial class PToCSharpCompiler : PTranslation
    {
        public PToCSharpCompiler(Compiler compiler, AST<Model> modelWithTypes, Dictionary<string, Dictionary<int, SourceInfo>> idToSourceInfo, string csharpFileName)
            : base(compiler, modelWithTypes, idToSourceInfo)
        {
            this.cSharpFileName = csharpFileName;
            this.typeContext = new TypeTranslationContext(this);
            GenerateTypeInfo(modelWithTypes);
        }

        #region CSharpCompiler
        string cSharpFileName;
        //for storing members of the Application class:
        List<SyntaxNode> members = new List<SyntaxNode>();
        //final C# program:
        SyntaxNode result = null;
        SyntaxGenerator generator;

        #region Generate P Types
        void GenerateTypeInfo(AST<Model> model)
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
                    if(!typeContext.importedTypes.ContainsKey(Factory.Instance.ToAST(it.Current)))
                    {
                        typeContext.importedTypes.Add(Factory.Instance.ToAST(it.Current), typeName);
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
                    if(typeContext.exportedTypes.ContainsKey(Factory.Instance.ToAST(it.Current)))
                    {
                        typeContext.duplicateExportedTypes.Add(typeName, it.Current as FuncTerm);
                    }
                    else
                    {
                        typeContext.exportedTypes.Add(Factory.Instance.ToAST(it.Current), typeName);
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

        TypeTranslationContext typeContext;

        #endregion

        public static string EventName(string rawName)
        {
            if (rawName == NullEvent)
            {
                return "@null";
            }
            else if (rawName == HaltEvent)
            {
                return rawName;
            }
            else
            {
                return String.Format("event_{0}", rawName);
            }
        }

        public static string StackFrameClassName(string rawName)
        {
            return String.Format("{0}_StackFrame", rawName);
        }

        public static string VarName(string rawName)
        {
            return String.Format("var_{0}", rawName);
        }

        public ExpressionSyntax GetEventVar(string eventName)
        {
            var eventClass = "Events";
            var retVal = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(eventClass), IdentifierName(EventName(eventName)));
            return retVal;
        }

        public void GenerateCSharp()
        {
            var workspace = new AdhocWorkspace();

            // Get the SyntaxGenerator for the specified language
            generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
            members = new List<SyntaxNode>();

            // Create using/Imports directives
            var usingDirectives = generator.NamespaceImportDeclaration("System");

            List<AST<Node>> elements = new List<AST<Node>>();
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
            List<SyntaxNode> evDeclarations = new List<SyntaxNode>();
            List<StatementSyntax> eventInitializationStmts = new List<StatementSyntax>();
            string eventsClassName = "Events";
            foreach (var evName in exportedEvents)
            {
                //add declaration
                evDeclarations.Add(
                CSharpHelper.MkCSharpFieldDeclaration(IdentifierName("PrtEventValue"),
                    EventName(evName),
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword)
                    )
                );

                //add initialization
                SyntaxNode payloadType = typeContext.PTypeToCSharpExpr(allEvents[evName].payloadType);
                SyntaxNode maxInstances;
                if (allEvents[evName].maxInstances == -1)
                {
                    String s = "PrtEvent.DefaultMaxInstances";
                    maxInstances = IdentifierName(s);
                }
                else
                {
                    maxInstances = CSharpHelper.MkCSharpNumericLiteralExpression(allEvents[evName].maxInstances);
                }

                SyntaxNode doAssume;
                if (allEvents[evName].maxInstancesAssumed)
                {
                    doAssume = CSharpHelper.MkCSharpTrueLiteralExpression();
                }
                else
                {
                    doAssume = CSharpHelper.MkCSharpFalseLiteralExpression();
                }

                SyntaxNode lhs = IdentifierName(EventName(evName));
                SyntaxNode rhs = CSharpHelper.MkCSharpObjectCreationExpression(
                    IdentifierName("PrtEventValue"),
                    CSharpHelper.MkCSharpObjectCreationExpression(
                        IdentifierName("PrtEvent"),
                        CSharpHelper.MkCSharpStringLiteralExpression(evName),
                        payloadType,
                        maxInstances,
                        doAssume
                    ));
                eventInitializationStmts.Add((StatementSyntax)CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(lhs, rhs));
            }

            var staticMethodName = "Events_" + Path.GetFileNameWithoutExtension(cSharpFileName.ToLower());
            var staticInitializer =
                    MethodDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.VoidKeyword)),
                        Identifier(staticMethodName))
                            .WithModifiers(
                                TokenList(new[] { Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)}))
                            .WithBody(
                                Block(eventInitializationStmts
                                    ))
                    .NormalizeWhitespace();

            evDeclarations.Add(staticInitializer);

            var eventsClass = generator.ClassDeclaration(
              eventsClassName, typeParameters: null,
              accessibility: Accessibility.Public,
              modifiers: DeclarationModifiers.Partial,
              members: evDeclarations);

            members.Add(eventsClass);
        }


        private void MkTypes()
        {
            //add type declaration and initialization for duplicate exported types
            foreach(var dt in typeContext.duplicateExportedTypes)
            {
                var typeName = typeContext.GetNextTypeName(dt.Key);
                var typeExpr = typeContext.GetTypeExpr(typeName);
                var declaredTypeExpr = typeContext.PTypeToCSharpExpr(dt.Value);
                typeContext.AddTypeDeclaration(typeName);
                typeContext.AddTypeInitialization(typeExpr, declaredTypeExpr);
            }

            string typesClassName = "Types";
            List<SyntaxNode> typeDeclarations = new List<SyntaxNode>();
            typeDeclarations.AddRange(typeContext.typeDeclaration);


            var staticMethodName = "Types_" + Path.GetFileNameWithoutExtension(cSharpFileName.ToLower());
            var staticInitializer =
                    MethodDeclaration(
                        PredefinedType(
                            Token(SyntaxKind.VoidKeyword)),
                        Identifier(staticMethodName))
                            .WithModifiers(
                                TokenList(new[] { Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.PublicKeyword) }))
                            .WithBody(
                                Block(
                                    typeContext.typeInitialization
                                    ))
                    .NormalizeWhitespace();

            typeDeclarations.Add(staticInitializer);

            var typesClass = generator.ClassDeclaration(
              typesClassName, typeParameters: null,
              accessibility: Accessibility.Public,
              modifiers: DeclarationModifiers.Partial,
              members: typeDeclarations);

            members.Add(typesClass);
        }

        private void MkStaticFunctions()
        {
            foreach (var pair in allGlobalFuns)
            {
                if(!pair.Value.isFunProto)
                {
                    MkFunctionDecl funDecl = new MkFunctionDecl(pair.Key, pair.Value, null, this);
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
            List<SyntaxNode> fields = new List<SyntaxNode>();

            //create machine
            MachineInfo machineInfo = allMachines[machineName];
            //There are three cases:
            //- default (no constraint on queue size): maxQueueSizeAssumed == false; maxQueueSize = default (10?) 
            //- assume <maxQueueSize>: maxQueueSize > 0, maxQueueSizeAssumed == true;
            //- assert <maxQueueSize>: maxQueueSize > 0, maxQueueSizeAssumed == false;   
            if (machineInfo.maxQueueSize > 0)
            {
                fields.Add(generator.LocalDeclarationStatement(generator.IdentifierName("var"), "machine",
                               generator.ObjectCreationExpression(generator.IdentifierName(machineName),
                               new List<SyntaxNode>() { generator.IdentifierName("application"), generator.LiteralExpression(machineInfo.maxQueueSize),
                                                            generator.LiteralExpression(machineInfo.maxQueueSizeAssumed)})));
            }
            else
            {
                fields.Add(generator.LocalDeclarationStatement(generator.IdentifierName("var"), "machine",
                               generator.ObjectCreationExpression(generator.IdentifierName(machineName),
                               new List<SyntaxNode>() {
                                   generator.IdentifierName("application"),
                                   MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("PrtImplMachine"), IdentifierName("DefaultMaxBufferSize")),
                                   generator.LiteralExpression(machineInfo.maxQueueSizeAssumed) })));
            }

            

            // log
            fields.Add(
                CSharpHelper.MkCSharpTrace(
                    string.Format("<CreateLog> Created Machine {0}-{{0}}", machineName),
                    CSharpHelper.MkCSharpDot("machine", "instanceNumber")));

            //initialize the permission set for self
            if(allMachines[machineName].receiveSet == null)
            {
                fields.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot(IdentifierName("machine"), "self", "permissions"), LiteralExpression(SyntaxKind.NullLiteralExpression)));
            }
            else
            {
                foreach (var ev in allMachines[machineName].receiveSet)
                {
                    fields.Add(
                        CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(IdentifierName("machine"), "self", "permissions", "Add"), GetEventVar(ev))
                        );
                }
            }

            //initialize the send set
            if (allMachines[machineName].sendsSet ==  null)
            {
                fields.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(CSharpHelper.MkCSharpDot(IdentifierName("machine"), "sends"), LiteralExpression(SyntaxKind.NullLiteralExpression)));
            }
            else
            {
                foreach (var ev in allMachines[machineName].sendsSet)
                {
                    fields.Add(
                        CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot(IdentifierName("machine"), "sends", "Add"), GetEventVar(ev))
                        );
                }
            }

            //machine.currentPayload = payload;
            fields.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(
                CSharpHelper.MkCSharpDot("machine", "currentPayload"),
                IdentifierName("payload")));

            //return machine;
            fields.Add(generator.ReturnStatement(generator.IdentifierName("machine")));

            var methodPars = new SyntaxNode[] {
                    generator.ParameterDeclaration("application", generator.IdentifierName("StateImpl")),
                    generator.ParameterDeclaration("payload", generator.IdentifierName("PrtValue")) };
            var makeCreateMachineDecl = generator.MethodDeclaration(string.Format("CreateMachine_{0}", machineName), methodPars,
              null, generator.IdentifierName("PrtImplMachine"),
              Accessibility.Public, DeclarationModifiers.Static,
              statements: fields);
            members.Add(makeCreateMachineDecl);
        }

        private void MkCreateSpecMachineMethod(string machineName)
        {
            List<SyntaxNode> fields = new List<SyntaxNode>();
            
            //create machine
            fields.Add(generator.LocalDeclarationStatement(generator.IdentifierName("var"), "machine",
                           generator.ObjectCreationExpression(generator.IdentifierName(machineName),
                           new List<SyntaxNode>() { generator.IdentifierName("application") })));

            //log
            fields.Add(CSharpHelper.MkCSharpTrace(string.Format("<CreateLog> Created spec Machine {0}", machineName)));

            foreach (var x in allMachines[machineName].observesEvents)
            {
                fields.Add(CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("machine", "observes", "Add"), GetEventVar(x)));
            }

            var prtvalueNull = (ExpressionSyntax)generator.MemberAccessExpression(IdentifierName("PrtEventValue"), IdentifierName("@null"));
            //execute the entry function of the machine
            fields.Add(
                    CSharpHelper.MkCSharpInvocationExpression(CSharpHelper.MkCSharpDot("machine", "PrtEnqueueEvent"), prtvalueNull, prtvalueNull, LiteralExpression(SyntaxKind.NullLiteralExpression))
                );
            //return machine;
            fields.Add(generator.ReturnStatement(generator.IdentifierName("machine")));

            var methodPars = new SyntaxNode[] {
                    generator.ParameterDeclaration("application", generator.IdentifierName("StateImpl")) };
            var makeCreateSpecDecl = generator.MethodDeclaration(string.Format("CreateSpecMachine_{0}", machineName), methodPars,
              null, IdentifierName("PrtSpecMachine"),    
              Accessibility.Public, DeclarationModifiers.Static,
              statements: fields);
            members.Add(makeCreateSpecDecl);
        }

        private void MkMachineClasses()
        {
            //Debug only:
            //Console.WriteLine("Number of machines: {0}", allMachines.Count());
            foreach (var pair in allMachines)
            {
                MkMachineClass mkMachine;
                mkMachine = new MkMachineClass(this, pair.Key, pair.Value);
                SyntaxNode node = mkMachine.MkClass();
                members.Add(node);
            }
        }

        private void MkCSharpOutput()
        {
            var applicationcClassDeclaration = generator.ClassDeclaration(
              "Application", typeParameters: null,
              modifiers: DeclarationModifiers.Partial,
              accessibility: Accessibility.Public,
              baseType: generator.IdentifierName("StateImpl"),
              members: members);

            // Declare a namespace
            //Generate namespace name: <output file name, no ".cs"> 
            //int index = this.csharpFileName.LastIndexOf(".");
            //string namespaceName = this.csharpFileName.Substring(0, index);
            //namespace is "P.Program" for all P programs:
            var programNameSpaceDeclaration = generator.NamespaceDeclaration(
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
            compiler.Log.WriteMessage(string.Format("Writing {0} ...", cSharpFileName), SeverityKind.Info);
            System.IO.StreamWriter file = new System.IO.StreamWriter(Path.Combine(outputDir, cSharpFileName));
            file.WriteLine("#pragma warning disable CS0162, CS0164, CS0168, CS0649");
            file.WriteLine(result);
            file.Close();
        }

        #endregion
    }
}

