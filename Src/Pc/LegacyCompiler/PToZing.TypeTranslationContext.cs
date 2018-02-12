using System;
using System.Collections.Generic;
using Microsoft.Formula.API;
using Microsoft.Formula.API.Nodes;

namespace Microsoft.Pc
{
    partial class PToZing
    {
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
                return MkZingSeq(MkZingSeq(fieldNameInitialization), MkZingSeq(typeInitialization));
            }

            public IEnumerable<AST<Node>> MainVarDecls()
            {
                List<AST<Node>> varDecls = new List<AST<Node>>();
                for (int i = 0; i < fieldCount; i++)
                {
                    varDecls.Add(MkZingVarDecl(string.Format("field_{0}_PRT_FIELD_NAME", i), Factory.Instance.MkCnst("PRT_FIELD_NAME"), ZingData.Cnst_Static));
                }
                for (int i = 0; i < typeCount; i++)
                {
                    varDecls.Add(MkZingVarDecl(string.Format("type_{0}_PRT_TYPE", i), Factory.Instance.MkCnst("PRT_TYPE"), ZingData.Cnst_Static));
                }
                return varDecls;
            }

            private AST<FuncTerm> GetField(string fieldName)
            {
                if (fieldNameToZingExpr.ContainsKey(fieldName))
                    return fieldNameToZingExpr[fieldName];
                var retVal = MkZingDot("Main", string.Format("field_{0}_PRT_FIELD_NAME", fieldCount));
                AddFieldNameInitialization(MkZingAssign(retVal, MkZingNew(Factory.Instance.MkCnst("PRT_FIELD_NAME"), ZingData.Cnst_Nil)));
                fieldCount++;
                fieldNameToZingExpr[fieldName] = retVal;
                return retVal;
            }

            private new AST<FuncTerm> GetType()
            {
                var retVal = MkZingDot("Main", string.Format("type_{0}_PRT_TYPE", typeCount));
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
                if (pTypeToZingExpr.ContainsKey(eTypeAST) && !pTypeToZingExpr.ContainsKey(typeAST))
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
                        AddTypeInitialization(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_NULL"))));
                        return tmpVar;
                    }
                    else if (primitiveType == "BOOL")
                    {
                        var tmpVar = GetType();
                        AddTypeInitialization(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_BOOL"))));
                        return tmpVar;
                    }
                    else if (primitiveType == "INT")
                    {
                        var tmpVar = GetType();
                        AddTypeInitialization(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_INT"))));
                        return tmpVar;
                    }
                    else if (primitiveType == "EVENT")
                    {
                        var tmpVar = GetType();
                        AddTypeInitialization(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_EVENT"))));
                        return tmpVar;
                    }
                    else if (primitiveType == "MACHINE")
                    {
                        var tmpVar = GetType();
                        AddTypeInitialization(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_MACHINE"))));
                        return tmpVar;
                    }
                    else
                    {
                        throw new NotSupportedException("Internal Error: Please report to P Developers");
                    }
                }
                else if(typeKind == "AnyType")
                {
                    var tmpVar = GetType();
                    AddTypeInitialization(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_ANY"))));
                    return tmpVar;
                }
                else if (typeKind == "NameType")
                {
                    var tmpVar = GetType();
                    AddTypeInitialization(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_INT"))));
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
                    AddTypeInitialization(MkZingAssign(tupleType, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkTupType"), Factory.Instance.MkCnst(memberTypes.Count))));
                    for (int i = 0; i < memberTypes.Count; i++)
                    {
                        AddTypeInitialization(MkZingCallStmt(MkZingCall(MkZingDot("PRT_TYPE", "PrtSetFieldType"), tupleType, Factory.Instance.MkCnst(i), memberTypes[i])));
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
                    AddTypeInitialization(MkZingAssign(tupleType, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkNmdTupType"), Factory.Instance.MkCnst(memberTypes.Count))));
                    for (int i = 0; i < memberTypes.Count; i++)
                    {
                        AddTypeInitialization(MkZingCallStmt(MkZingCall(MkZingDot("PRT_TYPE", "PrtSetFieldName"), tupleType, Factory.Instance.MkCnst(i), memberNames[i])));
                        AddTypeInitialization(MkZingCallStmt(MkZingCall(MkZingDot("PRT_TYPE", "PrtSetFieldType"), tupleType, Factory.Instance.MkCnst(i), memberTypes[i])));
                    }
                    return tupleType;
                }
                else if (typeKind == "SeqType")
                {
                    var innerType = PTypeToZingExpr((FuncTerm)GetArgByIndex(type, 0));
                    var seqType = GetType();
                    AddTypeInitialization(MkZingAssign(seqType, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkSeqType"), innerType)));
                    return seqType;
                }
                else if(typeKind == "MapType")
                {
                    // typeKind == "MapType"
                    var domType = PTypeToZingExpr((FuncTerm)GetArgByIndex(type, 0));
                    var codType = PTypeToZingExpr((FuncTerm)GetArgByIndex(type, 1));
                    var mapType = GetType();
                    AddTypeInitialization(MkZingAssign(mapType, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkMapType"), domType, codType)));
                    return mapType;
                }
                else
                {
                    // its InterfaceType so consider it as machine type
                    var tmpVar = GetType();
                    AddTypeInitialization(MkZingAssign(tmpVar, MkZingCall(MkZingDot("PRT_TYPE", "PrtMkPrimitiveType"), MkZingDot("PRT_TYPE_KIND", "PRT_KIND_MACHINE"))));
                    return tmpVar;
                }
            }
        }
    }
}