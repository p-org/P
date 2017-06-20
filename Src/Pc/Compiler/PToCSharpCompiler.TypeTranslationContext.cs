using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Formula.API;
using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc
{
    partial class PToCSharpCompiler
    {
        internal class TypeTranslationContext
        {
            private int typeCount;
            //This field is for emitting types; order is important
            public List<StatementSyntax> typeInitialization;
            public List<FieldDeclarationSyntax> typeDeclaration;
            private Dictionary<AST<Node>, ExpressionSyntax> pTypeToCSharpExpr;
            public Dictionary<AST<Node>, string> exportedTypes;
            public Dictionary<string, FuncTerm> duplicateExportedTypes;
            public Dictionary<AST<Node>, string> importedTypes;

            private PToCSharpCompiler pToCSharp;

            public TypeTranslationContext(PToCSharpCompiler pToCSharp)
            {
                this.pToCSharp = pToCSharp;
                typeCount = 0;
                typeDeclaration = new List<FieldDeclarationSyntax>();
                typeInitialization = new List<StatementSyntax>();
                pTypeToCSharpExpr = new Dictionary<AST<Node>, ExpressionSyntax>();
                exportedTypes = new Dictionary<AST<Node>, string>();
                importedTypes = new Dictionary<AST<Node>, string>();
                duplicateExportedTypes = new Dictionary<string, FuncTerm>();
            }

            public ExpressionSyntax GetTypeExpr(string typeName)
            {
                var typeClass = "Types";
                var retVal = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(typeClass), SyntaxFactory.IdentifierName(typeName));
                return retVal;
            }

            public string GetNextTypeName(string typeName = null)
            {
                
                typeName = typeName == null ?
                    String.Format("type_{0}_{1}", typeCount, Math.Abs(Path.GetFileNameWithoutExtension(pToCSharp.cSharpFileName).GetHashCode()).ToString())
                    : String.Format("type_{0}", typeName);
                typeCount++;
                return typeName;
            }

            public void AddTypeDeclaration(string typeName)
            {
                typeDeclaration.Add((FieldDeclarationSyntax)
                                    CSharpHelper.MkCSharpFieldDeclaration(SyntaxFactory.IdentifierName("PrtType"), typeName, SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)));
            }

            public void AddTypeInitialization(SyntaxNode lhs, SyntaxNode rhs)
            {
                typeInitialization.Add((StatementSyntax)(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(lhs, rhs)));
            }

            public ExpressionSyntax PTypeToCSharpExpr(FuncTerm pType)
            {
                
                var pTypeAST = Factory.Instance.ToAST(pType);
                if (!pTypeToCSharpExpr.ContainsKey(pTypeAST))
                {
                    pTypeToCSharpExpr[pTypeAST] = ConstructType(pType);
                }
                return pTypeToCSharpExpr[pTypeAST];
            }

            public void AddOriginalType(FuncTerm type, FuncTerm eType)
            {
                var typeAST = Factory.Instance.ToAST(type);
                var eTypeAST = Factory.Instance.ToAST(eType);
                if (pTypeToCSharpExpr.ContainsKey(eTypeAST) && !pTypeToCSharpExpr.ContainsKey(typeAST))
                {
                    pTypeToCSharpExpr[typeAST] = pTypeToCSharpExpr[eTypeAST];
                }
            }

            private ExpressionSyntax ConstructType(FuncTerm type)
            {
                string typeKind = ((Id)type.Function).Name;
                ExpressionSyntax typeExpr;
                string typeName;
                string originalName = "Interface";
                if (importedTypes.ContainsKey(Factory.Instance.ToAST(type)))
                {
                    originalName = importedTypes[Factory.Instance.ToAST(type)];
                    typeName = GetNextTypeName(importedTypes[Factory.Instance.ToAST(type)]);
                    return GetTypeExpr(typeName);
                }
                else
                {
                    if (exportedTypes.ContainsKey(Factory.Instance.ToAST(type)))
                    {
                        originalName = exportedTypes[Factory.Instance.ToAST(type)];
                        typeName = GetNextTypeName(exportedTypes[Factory.Instance.ToAST(type)]);
                        typeExpr = GetTypeExpr(typeName);
                    }
                    else
                    {
                        typeName = GetNextTypeName();
                        typeExpr = GetTypeExpr(typeName);
                    }

                    // add declaration and initialization
                    if (typeKind == "BaseType")
                    {
                        var primitiveType = ((Id)GetArgByIndex(type, 0)).Name;
                        if (primitiveType == "NULL")
                        {
                            AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtNullType")));
                            AddTypeDeclaration(typeName);
                        }
                        else if (primitiveType == "BOOL")
                        {
                            AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtBoolType")));
                            AddTypeDeclaration(typeName);
                        }
                        else if (primitiveType == "INT")
                        {
                            AddTypeDeclaration(typeName);
                            AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtIntType")));
                        }
                        else if (primitiveType == "EVENT")
                        {
                            AddTypeDeclaration(typeName);
                            AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtEventType")));
                        }
                        else if (primitiveType == "MACHINE")
                        {
                            AddTypeDeclaration(typeName);
                            AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtMachineType")));
                        }
                        else
                        {
                            Debug.Assert(primitiveType == "ANY", "Illegal BaseType");
                        }
                    }
                    else if (typeKind == "AnyType")
                    {
                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtAnyType")));
                    }
                    else if (typeKind == "NameType")
                    {
                        string enumTypeName = (GetArgByIndex(type, 0) as Cnst).GetStringValue();
                        List<ExpressionSyntax> args = new List<ExpressionSyntax>();
                        args.Add(CSharpHelper.MkCSharpStringLiteralExpression(enumTypeName));
                        foreach (var x in pToCSharp.allEnums[enumTypeName])
                        {
                            args.Add(CSharpHelper.MkCSharpStringLiteralExpression(x.Key));
                            args.Add(CSharpHelper.MkCSharpNumericLiteralExpression(x.Value));
                        }
                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtEnumType"), args.ToArray()));
                    }
                    else if (typeKind == "TupType")
                    {
                        List<SyntaxNode> memberTypes = new List<SyntaxNode>();
                        while (type != null)
                        {
                            memberTypes.Add(PTypeToCSharpExpr((FuncTerm)GetArgByIndex(type, 0)));
                            type = GetArgByIndex(type, 1) as FuncTerm;
                        }
                        //TODO(improve): create a generic method for inserting CommaToken into a generic list 
                        List<SyntaxNodeOrToken> initializer = new List<SyntaxNodeOrToken>();
                        foreach (var memberType in memberTypes)
                        {
                            initializer.Add(memberType);
                            initializer.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                        }
                        initializer.RemoveAt(initializer.Count() - 1);
                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtTupleType"), CSharpHelper.MkCSharpArrayCreationExpression("PrtType", initializer.ToArray())));

                    }
                    else if (typeKind == "NmdTupType")
                    {
                        List<SyntaxNode> memberNames = new List<SyntaxNode>();
                        List<SyntaxNode> memberTypes = new List<SyntaxNode>();

                        while (type != null)
                        {
                            var typeField = (FuncTerm)GetArgByIndex(type, 0);
                            string nameField = ((Cnst)GetArgByIndex(typeField, 0)).GetStringValue();
                            memberNames.Add(CSharpHelper.MkCSharpStringLiteralExpression(nameField));
                            memberTypes.Add(PTypeToCSharpExpr((FuncTerm)GetArgByIndex(typeField, 1)));
                            type = GetArgByIndex(type, 1) as FuncTerm;
                        }

                        List<SyntaxNodeOrToken> initializer = new List<SyntaxNodeOrToken>();
                        int ind = 0;
                        foreach (var memberName in memberNames)
                        {
                            initializer.Add(memberName);
                            initializer.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                            initializer.Add(memberTypes[ind++]);
                            initializer.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                        }
                        initializer.RemoveAt(initializer.Count() - 1);
                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtNamedTupleType"),
                                                                                                      CSharpHelper.MkCSharpArrayCreationExpression("object", initializer.ToArray())));
                    }
                    else if (typeKind == "SeqType")
                    {
                        SyntaxNode innerType = PTypeToCSharpExpr((FuncTerm)GetArgByIndex(type, 0));
                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtSeqType"), innerType));
                    }
                    else if (typeKind == "MapType")
                    {
                        SyntaxNode keyType = PTypeToCSharpExpr((FuncTerm)GetArgByIndex(type, 0));
                        SyntaxNode valType = PTypeToCSharpExpr((FuncTerm)GetArgByIndex(type, 1));
                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(typeExpr, CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtMapType"), keyType, valType));
                    }
                    else
                    {
                        // typekind == "InterfaceType"
                        var initializer = CSharpHelper.MkCSharpObjectCreationExpression(
                            SyntaxFactory.IdentifierName("PrtInterfaceType"), CSharpHelper.MkCSharpStringLiteralExpression(originalName));
                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(typeExpr, initializer);
                        
                    }
                }
                return typeExpr;
            }
        }
    }
}