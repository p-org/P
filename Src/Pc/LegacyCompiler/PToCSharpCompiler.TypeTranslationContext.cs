using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Formula.API;
using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc
{
    internal partial class PToCSharpCompiler
    {
        internal class TypeTranslationContext
        {
            public readonly Dictionary<string, FuncTerm> DuplicateExportedTypes = new Dictionary<string, FuncTerm>();
            public Dictionary<AST<Node>, string> ExportedTypes = new Dictionary<AST<Node>, string>();
            public Dictionary<AST<Node>, string> ImportedTypes = new Dictionary<AST<Node>, string>();

            private readonly PToCSharpCompiler pToCSharp;
            private readonly Dictionary<AST<Node>, ExpressionSyntax> pTypeToCSharpExpr = new Dictionary<AST<Node>, ExpressionSyntax>();
            private int typeCount;

            public List<FieldDeclarationSyntax> TypeDeclaration = new List<FieldDeclarationSyntax>();

            //This field is for emitting types; order is important
            public List<StatementSyntax> TypeInitialization = new List<StatementSyntax>();

            public TypeTranslationContext(PToCSharpCompiler pToCSharp)
            {
                this.pToCSharp = pToCSharp;
            }

            public ExpressionSyntax GetTypeExpr(string typeName)
            {
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Types"),
                    SyntaxFactory.IdentifierName(typeName));
            }

            public string GetNextTypeName(string typeName = null)
            {
                typeName = typeName == null
                    ? $"type_{typeCount}_{Math.Abs(Path.GetFileNameWithoutExtension(pToCSharp.cSharpFileName).GetHashCode())}"
                    : $"type_{typeName}";
                typeCount++;
                return typeName;
            }

            public void AddTypeDeclaration(string typeName)
            {
                TypeDeclaration.Add(
                    (FieldDeclarationSyntax) CSharpHelper.MkCSharpFieldDeclaration(
                        SyntaxFactory.IdentifierName("PrtType"),
                        typeName,
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword)));
            }

            public void AddTypeInitialization(SyntaxNode lhs, SyntaxNode rhs)
            {
                TypeInitialization.Add(CSharpHelper.MkCSharpSimpleAssignmentExpressionStatement(lhs, rhs));
            }

            public ExpressionSyntax PTypeToCSharpExpr(FuncTerm pType)
            {
                AST<Node> pTypeAST = Factory.Instance.ToAST(pType);
                if (!pTypeToCSharpExpr.ContainsKey(pTypeAST))
                {
                    pTypeToCSharpExpr[pTypeAST] = ConstructType(pType);
                }
                return pTypeToCSharpExpr[pTypeAST];
            }

            public void AddOriginalType(FuncTerm type, FuncTerm eType)
            {
                AST<Node> typeAST = Factory.Instance.ToAST(type);
                AST<Node> eTypeAST = Factory.Instance.ToAST(eType);
                if (pTypeToCSharpExpr.ContainsKey(eTypeAST) && !pTypeToCSharpExpr.ContainsKey(typeAST))
                {
                    pTypeToCSharpExpr[typeAST] = pTypeToCSharpExpr[eTypeAST];
                }
            }

            private ExpressionSyntax ConstructType(FuncTerm type)
            {
                string typeKind = ((Id) type.Function).Name;
                ExpressionSyntax typeExpr;
                string typeName;
                
                if (ImportedTypes.ContainsKey(Factory.Instance.ToAST(type)))
                {
                    typeName = GetNextTypeName(ImportedTypes[Factory.Instance.ToAST(type)]);
                    return GetTypeExpr(typeName);
                }

                var originalName = "Interface";
                if (ExportedTypes.ContainsKey(Factory.Instance.ToAST(type)))
                {
                    originalName = ExportedTypes[Factory.Instance.ToAST(type)];
                    typeName = GetNextTypeName(ExportedTypes[Factory.Instance.ToAST(type)]);
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
                    string primitiveType = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(((Id) GetArgByIndex(type, 0)).Name.ToLowerInvariant());

                    Debug.Assert(
                        primitiveType == "Any" || primitiveType == "Null" || primitiveType == "Bool" || primitiveType == "Int" || primitiveType == "Float"
                        || primitiveType == "Event" || primitiveType == "Value",
                        $"Illegal BaseType: {primitiveType}");

                    if (primitiveType != "Any")
                    {
                        AddTypeInitialization(
                            typeExpr,
                            CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName($"Prt{primitiveType}Type")));
                        AddTypeDeclaration(typeName);
                    }
                }
                else if (typeKind == "AnyType")
                {
                    AddTypeDeclaration(typeName);
                    AddTypeInitialization(
                        typeExpr,
                        CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtAnyType")));
                }
                else if (typeKind == "NameType")
                {
                    string enumTypeName = (GetArgByIndex(type, 0) as Cnst).GetStringValue();
                    if(pToCSharp.allEnums.ContainsKey(enumTypeName))
                    {
                        var args = new List<ExpressionSyntax> { CSharpHelper.MkCSharpStringLiteralExpression(enumTypeName) };
                        foreach (KeyValuePair<string, int> x in pToCSharp.allEnums[enumTypeName])
                        {
                            args.Add(CSharpHelper.MkCSharpStringLiteralExpression(x.Key));
                            args.Add(CSharpHelper.MkCSharpNumericLiteralExpression(x.Value));
                        }

                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(
                            typeExpr,
                            CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtEnumType"), args.ToArray()));
                    }
                    else
                    {
                        var args = new List<ExpressionSyntax>();
                        AddTypeDeclaration(typeName);
                        AddTypeInitialization(
                            typeExpr,
                            CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName($"Foreign_{enumTypeName}"), args.ToArray()));
                    }
                    
                }
                else if (typeKind == "TupType")
                {
                    var memberTypes = new List<SyntaxNodeOrToken>();
                    while (type != null)
                    {
                        memberTypes.Add(PTypeToCSharpExpr((FuncTerm) GetArgByIndex(type, 0)));
                        type = GetArgByIndex(type, 1) as FuncTerm;
                    }

                    var initializer = CSharpHelper.Intersperse(memberTypes, SyntaxFactory.Token(SyntaxKind.CommaToken));

                    AddTypeDeclaration(typeName);
                    AddTypeInitialization(
                        typeExpr,
                        CSharpHelper.MkCSharpObjectCreationExpression(
                            SyntaxFactory.IdentifierName("PrtTupleType"),
                            CSharpHelper.MkCSharpArrayCreationExpression("PrtType", initializer.ToArray())));
                }
                else if (typeKind == "NmdTupType")
                {
                    var memberNames = new List<SyntaxNode>();
                    var memberTypes = new List<SyntaxNode>();

                    while (type != null)
                    {
                        var typeField = (FuncTerm) GetArgByIndex(type, 0);
                        string nameField = ((Cnst) GetArgByIndex(typeField, 0)).GetStringValue();
                        memberNames.Add(CSharpHelper.MkCSharpStringLiteralExpression(nameField));
                        memberTypes.Add(PTypeToCSharpExpr((FuncTerm) GetArgByIndex(typeField, 1)));
                        type = GetArgByIndex(type, 1) as FuncTerm;
                    }

                    var initializer = new List<SyntaxNodeOrToken>();
                    var ind = 0;
                    foreach (SyntaxNode memberName in memberNames)
                    {
                        initializer.Add(memberName);
                        initializer.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                        initializer.Add(memberTypes[ind++]);
                        initializer.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                    }

                    initializer.RemoveAt(initializer.Count - 1);
                    AddTypeDeclaration(typeName);
                    AddTypeInitialization(
                        typeExpr,
                        CSharpHelper.MkCSharpObjectCreationExpression(
                            SyntaxFactory.IdentifierName("PrtNamedTupleType"),
                            CSharpHelper.MkCSharpArrayCreationExpression("object", initializer.ToArray())));
                }
                else if (typeKind == "SeqType")
                {
                    SyntaxNode innerType = PTypeToCSharpExpr((FuncTerm) GetArgByIndex(type, 0));
                    AddTypeDeclaration(typeName);
                    AddTypeInitialization(
                        typeExpr,
                        CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtSeqType"), innerType));
                }
                else if (typeKind == "MapType")
                {
                    SyntaxNode keyType = PTypeToCSharpExpr((FuncTerm) GetArgByIndex(type, 0));
                    SyntaxNode valType = PTypeToCSharpExpr((FuncTerm) GetArgByIndex(type, 1));
                    AddTypeDeclaration(typeName);
                    AddTypeInitialization(
                        typeExpr,
                        CSharpHelper.MkCSharpObjectCreationExpression(SyntaxFactory.IdentifierName("PrtMapType"), keyType, valType));
                }
                else
                {
                    // typekind == "InterfaceType"
                    ObjectCreationExpressionSyntax initializer = CSharpHelper.MkCSharpObjectCreationExpression(
                        SyntaxFactory.IdentifierName("PrtInterfaceType"),
                        CSharpHelper.MkCSharpStringLiteralExpression(originalName));
                    AddTypeDeclaration(typeName);
                    AddTypeInitialization(typeExpr, initializer);
                }

                return typeExpr;
            }
        }
    }
}