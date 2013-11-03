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

namespace PCompiler
{
    internal class CTranslationInfo
    {
        public AST<Node> node;
        public bool isKeys;

        public CTranslationInfo(AST<Node> n)
        {
            this.node = n;
            this.isKeys = false;
        }

        public CTranslationInfo(AST<Node> n, bool isKeys)
        {
            this.node = n;
            this.isKeys = isKeys;
        }
    }

    class PToC
    {
        public PToC(Compiler compiler)
        {
            this.compiler = compiler;
            this.PSMF_DRIVERDECL_TYPE = MkNmdType("PSMF_DRIVERDECL");
        }
        private AST<FuncTerm> PSMF_DRIVERDECL_TYPE;
        Compiler compiler;

        #region Static helpers
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

        private static AST<Node> MkIf(AST<Node> cond, AST<Node> then)
        {
            return Compiler.AddArgs(CData.App_ITE(), cond, then, CData.Cnst_Nil());
        }

        private static AST<Node> MkIdx(AST<Node> baseE, AST<Node> idxE)
        {
            return Compiler.AddArgs(CData.App_BinApp(), CData.Cnst_AAc(), baseE, idxE);
        }

        private static AST<Node> MkEq(AST<Node> e1, AST<Node> e2)
        {
            return Compiler.AddArgs(CData.App_BinApp(), CData.Cnst_Eq(), e1, e2);
        }

        private static AST<Node> MkCast(AST<Node> t, AST<Node> e)
        {
            return Compiler.AddArgs(CData.App_Cast(), t, e);
        }

        private static AST<FuncTerm> MkFunApp(AST<Node> funExp, Span span, List<AST<Node>> args)
        {
            return Compiler.AddArgs(CData.App_FunApp(span), funExp, Compiler.ConstructList(CData.App_Args(span), args, CData.Cnst_Nil(span)));
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

        private static AST<FuncTerm> MkComment(string comment, bool isBlockStyle)
        {
            return Factory.Instance.AddArg(
                    Factory.Instance.AddArg(CData.App_Comment(), Factory.Instance.MkCnst(comment)),
                    isBlockStyle ? CData.Cnst_True() : CData.Cnst_False());
        }

        private static AST<FuncTerm> MkId(string name, Span span = default(Span))
        {
            return Factory.Instance.AddArg(CData.App_Ident(span), Factory.Instance.MkCnst(name));
        }

        private static AST<FuncTerm> MkStringLiteral(string value, Span span = default(Span))
        {
            var strLit = Factory.Instance.AddArg(CData.App_StringLit(span), Factory.Instance.MkCnst(value));
            return Factory.Instance.AddArg(strLit, CData.Cnst_Long(span));
        }

        private static AST<FuncTerm> MkIntLiteral(int value, Span span = default(Span))
        {
            var intLit = Factory.Instance.AddArg(CData.App_IntLit(span), Factory.Instance.MkCnst(value));
            intLit = Factory.Instance.AddArg(intLit, CData.Cnst_Dec(span));
            return Factory.Instance.AddArg(intLit, CData.Cnst_Nil(span));
        }

        private static AST<FuncTerm> MkIntLiteral(int value, AST<Id> format, Span span = default(Span))
        {
            var intLit = Factory.Instance.AddArg(CData.App_IntLit(span), Factory.Instance.MkCnst(value));
            intLit = Factory.Instance.AddArg(intLit, format);
            return Factory.Instance.AddArg(intLit, CData.Cnst_Nil(span));
        }

        private static AST<FuncTerm> MkIntLiteral(uint value, AST<Id> format, Span span = default(Span))
        {
            var intLit = Factory.Instance.AddArg(CData.App_IntLit(span), Factory.Instance.MkCnst(value));
            intLit = Factory.Instance.AddArg(intLit, format);
            return Factory.Instance.AddArg(intLit, CData.Cnst_Nil(span));
        }

        private static AST<FuncTerm> MkArrType(string typeName)
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

        private static AST<FuncTerm> MkPtrType(string typeName)
        {
            var nmdType = Factory.Instance.AddArg(CData.App_NmdType(), CData.Cnst_Nil());
            nmdType = Factory.Instance.AddArg(nmdType, Factory.Instance.MkCnst(typeName));
            return MkPtrType(nmdType);
        }

        private static AST<FuncTerm> MkFunType(AST<Node> retType, IEnumerable<AST<Node>> argTypes)
        {
            return Compiler.AddArgs(CData.App_FunType(), retType, Compiler.ConstructCList(CData.App_PrmTypes(), argTypes));
        }

        private static AST<FuncTerm> MkFunType(AST<Node> retType, params AST<Node>[] argTypes)
        {
            return MkFunType(retType, new List<AST<Node>>(argTypes));
        }

        private static AST<Node> MkFunParams(bool withEllipsis = false, params string[] prmNames)
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

        private static AST<Node> MkBlock(AST<Node> body, params AST<Node>[] locals)
        {
            return Compiler.AddArgs(CData.App_Block(), Compiler.ConstructCList(CData.App_Defs(), locals), body);
        }

        private static AST<Node> MkSeq(IEnumerable<AST<Node>> stmts)
        {
            return Compiler.ConstructCList(CData.App_Seq(), stmts);
        }

        private static AST<Node> MkSeq(params AST<Node>[] stmts)
        {
            return Compiler.ConstructCList(CData.App_Seq(), stmts);
        }

        private static AST<Node> MkAssignment(AST<Node> lhs, AST<Node> rhs)
        {
            return Compiler.AddArgs(CData.App_BinApp(), CData.Cnst_Asn(), lhs, rhs);
        }

        private static AST<Node> MkDot(AST<Node> lhs, string member)
        {
            return Compiler.AddArgs(CData.App_BinApp(), CData.Cnst_Fld(), lhs, Compiler.AddArgs(CData.App_Ident(), Factory.Instance.MkCnst(member)));
        }

        private static AST<FuncTerm> MkBinApp(AST<Id> op, AST<Node> exp1, AST<Node> exp2)
        {
            return Compiler.AddArgs(CData.App_BinApp(), op, exp1, exp2);
        }

        private static AST<FuncTerm> MkUnop(AST<Id> op, AST<Node> exp)
        {
            return Compiler.AddArgs(CData.App_UnApp(), op, exp);
        }

        private static AST<FuncTerm> MkAddrOf(AST<Node> exp)
        {
            return MkUnop(CData.Cnst_Addr(), exp);
        }

        private static AST<FuncTerm> MkDrf(AST<Node> exp)
        {
            return MkUnop(CData.Cnst_Drf(), exp);
        }

        private static AST<Node> MkArrow(string baseE, string member)
        {
            return Compiler.AddArgs(CData.App_BinApp(), CData.Cnst_PFld(), MkId(baseE), MkId(member));
        }

        private static AST<Node> MkArrow(AST<Node> lhs, string member)
        {
            return Compiler.AddArgs(CData.App_BinApp(), CData.Cnst_PFld(), lhs, Compiler.AddArgs(CData.App_Ident(), Factory.Instance.MkCnst(member)));
        }

        private static AST<FuncTerm> MkNmdType(string typeName, AST<Node> kind)
        {
            var nmdType = Factory.Instance.AddArg(CData.App_NmdType(), kind);
            return Factory.Instance.AddArg(nmdType, Factory.Instance.MkCnst(typeName));
        }

        private static AST<FuncTerm> MkBaseType(AST<Id> type)
        {
            return Compiler.AddArgs(CData.App_BaseType(), type);
        }

        private static AST<FuncTerm> MkNmdType(string typeName)
        {
            return MkNmdType(typeName, CData.Cnst_Nil());
        }

        private static AST<FuncTerm> MkStructType(string typeName)
        {
            var nmdType = Factory.Instance.AddArg(CData.App_NmdType(), CData.Cnst_Struct());
            return Factory.Instance.AddArg(nmdType, Factory.Instance.MkCnst(typeName));
        }

        private static string dbgAst2Str(AST<Node> n)
        {
            System.IO.StringWriter sw = new System.IO.StringWriter();
            n.Print(sw);
            return sw.ToString();
        }

        private static string dbgAst2Str(Node n)
        {
            return dbgAst2Str(Factory.Instance.ToAST(n));
        }

        private static AST<FuncTerm> MkSimpleDefine(string identDefining, string identDefinition)
        {
            var ppDef = Factory.Instance.AddArg(CData.App_PpDefine(), MkId(identDefining));
            return Factory.Instance.AddArg(ppDef, MkId(identDefinition));
        }

        private static AST<FuncTerm> MkInit(params AST<Node>[] values)
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

        private static AST<Node> MkAssert(AST<Node> cond, Span s, string msg = null)
        {
            if (msg == null)
            {
                return MkFunApp("SMF_ASSERTMSG", s, MkStringLiteral(""), cond);
            }
            else
            {
                return MkFunApp("SMF_ASSERTMSG", s, MkStringLiteral(msg), cond);
            }
        }

        private static AST<Node> MkReturn(AST<Node> retV)
        {
            return Compiler.AddArgs(CData.App_Return(), retV);
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
            return zipped.Aggregate(ret, (aggr, el) => Compiler.AddArgs(constructor, el.Item1, el.Item2, aggr));
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

            return zipped.Aggregate(ret, (aggr, el) => Compiler.AddArgs(constructor, el.Item1, el.Item2, el.Item3, aggr));
        }

        private static AST<Node> MkSwitch(AST<Node> expr, IEnumerable<Tuple<AST<Node>, AST<Node>>> cases, AST<Node> def = null)
        {
            Debug.Assert(cases.Count() != 0 || def != null);
            AST<Node> defCase = def != null ? (AST<Node>)Compiler.AddArgs(CData.App_Cases(), CData.Cnst_Default(), def, CData.Cnst_Nil()) : (AST<Node>)CData.Cnst_Nil();

            if (cases.Count() == 0)
            {
                return def;
            }

            return Compiler.AddArgs(CData.App_Switch(), expr,
                ConstructList2(CData.App_Cases(), cases.Select(c => c.Item1),
                cases.Select(c => MkSeq(c.Item2, Compiler.AddArgs(CData.App_StrJmp(), CData.Cnst_Break()))), defCase));
        }

        private static AST<FuncTerm> MkCFile(string filename, params AST<Node>[] elements)
        {
            Contract.Requires(elements != null && elements.Length > 0);
            return Compiler.AddArgs(CData.App_File(), Factory.Instance.MkCnst(filename), Compiler.ConstructCList(CData.App_Section(), elements.Where(el => el != null)));
        }
        
        private static int getCIntConst(FuncTerm cExp)
        {
            if (Compiler.getFtName(cExp) == CData.Con_Cast().Node.Name)
            {
                return getCIntConst((FuncTerm)Compiler.GetArgByIndex(cExp, 1));
            }
            else if (Compiler.getFtName(cExp) == CData.Con_IntLit().Node.Name)
            {
                return (int)((Cnst)Compiler.GetArgByIndex(cExp, 0)).GetNumericValue().Numerator;
            }
            else
            {
                throw new Exception(string.Format("C expression '{0}' is not an integer constant", dbgAst2Str(cExp)));
            }
        }
        #endregion

        public void GenerateC(Env env, ref AST<Model> outModel)
        {
            Dictionary<string, int> typeIds = new Dictionary<string, int>();

            string pblEnmTypesCmt =
                string.Format(headerCmt,
                              "PublicEnumTypes.h",
                              "This file contains enumerated types for events, machines, and variables.",
                              compiler.target,
                              DateTime.Today.Date.ToShortDateString(),
                              compiler.kernelMode ? "Kernel mode" : "User mode");
            var pblEnmTypes = MkCFile(
                "PublicEnumTypes.h",
                compiler.emitHeaderComment ? MkComment(pblEnmTypesCmt, true) : null,
                CData.Trm_PragmaOnce(),
                MkEnum(
                    PData.Con_EventDecl.Node.Name,
                    "Events",
                    "Event_",
                    0,
                    compiler.modelEventIds,
                    Compiler.NullEvent),
                MkEnum(
                    PData.Con_MachineDecl.Node.Name,
                    "MachineTypes",
                    "MachineType_",
                    0,
                    compiler.modelMachineIds),
               MkEnums(
                    PData.Con_VarDecl.Node.Name,
                    PData.Con_MachineDecl.Node.Name,
                    "Vars",
                    "Var",
                    0,
                    1,
                    0,
                    compiler.modelVarIds),
               MkEnum(compiler.allTypes.Select(pType => pTypeToCEnum(pType)), "", "Types", typeIds));

            outModel = Compiler.Add(outModel, pblEnmTypes);

            var complexClassStructDefs = new List<AST<Node>>();
            var complexClassForwardDefs = new List<AST<Node>>();
            var complexClassMethods = new List<AST<Node>>();
            var complexClassTypeDecls = new List<AST<Node>>();

            foreach (PType t in compiler.allTypes)
            {
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
                    var cTypeName = compiler.declaredTypes[t].cType;

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
                        Compiler.AddArgs(CData.App_Cast(), MkPtrType(pTypeToCType(t)), MkId("dst")),
                        MkId(pTypeToCEnum((t as PSeqType).T)));
                    complexClassMethods.Add(MkBuildDefMethod(t, defBody));
                }
                else if (t is PMapType)
                {
                    // BuildDefault
                    complexClassForwardDefs.Add(MkBuildDefMethod(t));
                    var defBody = MkFunApp("BuildEmptyHashtable", default(Span),
                        Compiler.AddArgs(CData.App_Cast(), MkPtrType(pTypeToCType(t)), MkId("dst")),
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
                compiler.emitHeaderComment ? MkComment(pblEnmTypesCmt, true) : null,
                CData.Trm_PragmaOnce(),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfPublic.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfProtectedTypes.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfArrayList.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfHashtable.h"), PData.Cnst_False),
                Compiler.ConstructCList(CData.App_Section(), complexClassStructDefs),
                Compiler.ConstructCList(CData.App_Section(), complexClassForwardDefs));

            outModel = Compiler.Add(outModel, pblComplexTypesHeader);

            var prtComplexTypesFuns = MkCFile(
                "ComplexTypesMethods.c",
                compiler.emitHeaderComment ? MkComment(pblEnmTypesCmt, true) : null,
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("PublicComplexTypes.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("PublicEnumTypes.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfProtected.h"), PData.Cnst_False),
                Compiler.ConstructCList(CData.App_Section(), complexClassMethods));

            outModel = Compiler.Add(outModel, prtComplexTypesFuns);

            string prtEnmTypesCmt =
                string.Format(headerCmt,
                              "ProtectedEnumTypes.h",
                              "This file contains enumerated types for states and event sets.",
                              compiler.target,
                              DateTime.Today.Date.ToShortDateString(),
                              compiler.kernelMode ? "Kernel mode" : "User mode");
            var prtEnmTypes = MkCFile(
                "ProtectedEnumTypes.h",
                compiler.emitHeaderComment ? MkComment(prtEnmTypesCmt, true) : null,
                CData.Trm_PragmaOnce(),
               MkEnums(
                    PData.Con_StateDecl.Node.Name,
                    PData.Con_MachineDecl.Node.Name,
                    "States",
                    "State",
                    0,
                    1,
                    0,
                    compiler.modelStateIds),
               MkEnums(
                    PData.Con_EventSetDecl.Node.Name,
                    PData.Con_MachineDecl.Node.Name,
                    "EventSets",
                    "EventSet",
                    0,
                    1,
                    0,
                    compiler.modelEventSetIds));

            outModel = Compiler.Add(outModel, prtEnmTypes);

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
                              compiler.target,
                              DateTime.Today.Date.ToShortDateString(),
                              compiler.kernelMode ? "Kernel mode" : "User mode");
            var prtMachDecls = MkCFile(
                "ProtectedMachineDecls.h",
                compiler.emitHeaderComment ? MkComment(prtMachDeclsCmt, true) : null,
                CData.Trm_PragmaOnce(),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("PublicEnumTypes.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("ProtectedEnumTypes.h"), PData.Cnst_False),
                entriesH,
                exitsH,
                constructorsH,
                actionsH,
                MkEventSets(),
                MkTransTables(),
                MkActionTables(),
                MkStateTables(),
                MkVarTables());

            outModel = Compiler.Add(outModel, prtMachDecls);

            AST<FuncTerm> funDeclHeaders, funDeclBodies;
            MkFunDecls(out funDeclHeaders, out funDeclBodies);
            string functionPrototypesCmt =
                string.Format(headerCmt,
                              "FunctionPrototypes.h",
                              "This file contains headers for functions.",
                              compiler.target,
                              DateTime.Today.Date.ToShortDateString(),
                              compiler.kernelMode ? "Kernel mode" : "User mode");
            var functionPrototypes = MkCFile(
                "FunctionPrototypes.h",
                compiler.emitHeaderComment ? MkComment(functionPrototypesCmt, true) : null,
                CData.Trm_PragmaOnce(),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfPublic.h"), PData.Cnst_True),
                compiler.erase ? (AST<Node>)CData.Cnst_Nil() : (AST<Node>)Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfProtected.h"), PData.Cnst_True),
                funDeclHeaders);

            outModel = Compiler.Add(outModel, functionPrototypes);

            string entryAndExitFnsCmt =
                string.Format(headerCmt,
                              "EntryFunctions.c",
                              "This file contains definitions for entry functions, exit functions, action functions, and declared functions.",
                              compiler.target,
                              DateTime.Today.Date.ToShortDateString(),
                              compiler.kernelMode ? "Kernel mode" : "User mode");
            var entryAndExitFns = MkCFile(
                "EntryFunctions.c",
                compiler.emitHeaderComment ? MkComment(entryAndExitFnsCmt, true) : null,
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("FunctionPrototypes.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfPublic.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfProtected.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("PublicEnumTypes.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("ProtectedEnumTypes.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("ProtectedDriverDecl.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("PublicComplexTypes.h"), PData.Cnst_False),
                entriesB,
                exitsB,
                actionsB,
                funDeclBodies);

            outModel = Compiler.Add(outModel, entryAndExitFns);

            string driverDeclCmt =
                string.Format(headerCmt,
                              "ProtectedDriverDecl.h",
                              "This file contains tables for events, machines, and driver.",
                              compiler.target,
                              DateTime.Today.Date.ToShortDateString(),
                              compiler.kernelMode ? "Kernel mode" : "User mode");
            var driverDecl = MkCFile(
                "ProtectedDriverDecl.h",
                compiler.emitHeaderComment ? MkComment(driverDeclCmt, true) : null,
                CData.Trm_PragmaOnce(),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfPublic.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfProtected.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("PublicEnumTypes.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("ProtectedMachineDecls.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("ProtectedEnumTypes.h"), PData.Cnst_False),
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("PublicComplexTypes.h"), PData.Cnst_False),
                MkEventTable(),
                MkMachineTable(),
                MkTypeRelativesSets(typeIds),
                MkTypeTable(complexClassTypeDecls),
                MkDriverDecl());

            outModel = Compiler.Add(outModel, driverDecl);

            string driverName = string.Format("{0}.h", compiler.model.Node.Name);
            string driverCmt =
                string.Format(headerCmt,
                              driverName,
                              "This file contains the extern driver declaration.",
                              compiler.target,
                              DateTime.Today.Date.ToShortDateString(),
                              compiler.kernelMode ? "Kernel mode" : "User mode");
            var driver = MkCFile(
                driverName,
                compiler.emitHeaderComment ? MkComment(driverCmt, true) : null,
                Compiler.AddArgs(CData.App_PpInclude(), Factory.Instance.MkCnst("SmfPublicTypes.h"), PData.Cnst_False),
                MkVarDef(CData.Cnst_Extern(), MkNmdType("SMF_DRIVERDECL"), compiler.DriverDeclName(), CData.Cnst_Nil()));

            outModel = Compiler.Add(outModel, driver);
        }

        private string getCBuildName(PType t)
        {
            return "Build_" + compiler.declaredTypes[t].cType;
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
                return "Destroy_" + compiler.declaredTypes[t].cType;
        }
        
        private string getCBuildDefName(PType t)
        {
            if (t is PAnyType)
            {
                return "BuildDefault_PackedValue";
            }
            else
                return "BuildDefault_" + compiler.declaredTypes[t].cType;
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
                return "Clone_" + compiler.declaredTypes[t].cType;
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

        private AST<Node> MkEq(AST<Node> driver, AST<Node> e1, PType t1, AST<Node> e2, PType t2)
        {
            PType supT, subT;
            AST<Node> supE, subE;

            if (t1 == t2)
            {
                if (t1 is PNilType) // Since Nil is a singleton type, implicitly any two expressions of this type are equal.
                {
                    return MkId("TRUE");
                }
                else if (t1 is PPrimitiveType)
                {
                    return Compiler.AddArgs(CData.App_BinApp(), CData.Cnst_Eq(), e1, e2);
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
                var block = Compiler.AddArgs(CData.App_Block(), locals == null ? CData.Cnst_Nil() : locals, body);
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
            var res = new HashSet<PType>(compiler.subtypes[t]);
            res.Add(t);
            return res;
        }

        private IEnumerable<PType> supertypesAndMe(PType t)
        {
            var res = new HashSet<PType>(compiler.supertypes[t]);
            res.Add(t);
            return res;
        }

        private void MkUpCastMethods(List<AST<Node>> forwardDecls, List<AST<Node>> methodDefs)
        {
            foreach (var t in compiler.allTypes)
            {
                foreach (var subT in compiler.subtypes[t])
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
                        Compiler.AddArgs(CData.App_Block(), ctxt.emitCLocals(), ctxt.emitCSideEffects(MkSeq(body)))));
                }
            }
        }

        private AST<Node> MkIncFor(AST<Node> var, int start, AST<Node> end, AST<Node> body)
        {
            return Compiler.AddArgs(CData.App_For(),
                MkBinApp(CData.Cnst_Asn(), var, MkIntLiteral(start)),
                MkBinApp(CData.Cnst_Le(), var, end),
                MkBinApp(CData.Cnst_Asn(), var, MkBinApp(CData.Cnst_Add(), var, MkIntLiteral(1))),
                body);
        }

        private void MkDownCastMethods(List<AST<Node>> forwardDecls, List<AST<Node>> methodDefs)
        {
            foreach (var t in compiler.allTypes)
            {
                foreach (var subT in compiler.subtypes[t])
                {
                    PToCFoldContext ctxt = new PToCFoldContext(null, true, this);
                    var funType = MkFunType(MkBaseType(CData.Cnst_Void()), MkNmdType("PSMF_DRIVERDECL"),
                        MkPtrType(pTypeToCType(subT)), (t is PPrimitiveType) ? pTypeToCType(t) : MkPtrType(pTypeToCType(t)));
                    var name = getCDownCastName(t, subT);
                    var prms = MkFunParams(false, "Driver", "dst", "src");
                    var body = new List<AST<Node>>();
                    var errMsg = "Failed Downcasting From " + t + " to " + subT;

                    if (t is PAnyType)
                    {
                        var cases = new List<Tuple<AST<Node>, AST<Node>>>();

                        foreach (var midT in compiler.relatives(subT).Where(tp => !(tp is PAnyType)))
                        {
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
                        Compiler.AddArgs(CData.App_Block(), ctxt.emitCLocals(), ctxt.emitCSideEffects(MkSeq(body)))));

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
            foreach (var t in compiler.allTypes)
            {
                if (t is PEventType || t is PIdType)
                {
                    Debug.Assert(compiler.subtypes[t].Count == 1 && compiler.subtypes[t][0] == PType.Nil);
                    continue; // These cases are handled in MkCanCast
                }

                PToCFoldContext ctxt = new PToCFoldContext(null, true, this);
                var funType = MkFunType(pTypeToCType(PType.Bool),
                    (t is PPrimitiveType) ? pTypeToCType(t) : MkPtrType(pTypeToCType(t)), MkNmdType("SMF_TYPEDECL_INDEX"));
                var name = getCCanDownCastName(t);
                var prms = MkFunParams(false, "obj", "toT");
                var outerCases = new List<Tuple<AST<Node>, AST<Node>>>();

                foreach (var subT in compiler.subtypes[t])
                {
                    if (t is PAnyType)
                    {
                        var cases = new List<Tuple<AST<Node>, AST<Node>>>();

                        foreach (var midT in compiler.relatives(subT).Where(tp => !(tp is PAnyType)))
                        {
                            cases.Add(new Tuple<AST<Node>, AST<Node>>(MkId(pTypeToCEnum(midT)),
                                Compiler.AddArgs(CData.App_Return(), MkCanCast(ctxt.driver, getPackedValueMember(MkId("obj"), midT), midT, subT))));
                        }

                        outerCases.Add(new Tuple<AST<Node>, AST<Node>>(MkId(pTypeToCEnum(subT)),
                            MkSwitch(MkArrow("obj", "Type"), cases, Compiler.AddArgs(CData.App_Return(), MkId("FALSE")))));
                    }
                    else if (t is PTupleType || t is PNamedTupleType)
                    {
                        var fromFields = getFieldDesc(t).ToArray();
                        var toFields = getFieldDesc(subT).ToArray();
                        var fieldChecks = new List<AST<Node>>();

                        for (int i = 0; i < fromFields.Count(); i++)
                        {
                            var canCast = MkCanCast(ctxt.driver, MkArrow(MkId("obj"), fromFields[i].Item3), fromFields[i].Item1, toFields[i].Item1);

                            if (Compiler.isInstanceOf(canCast.Node, CData.App_Ident()) &&
                                ((Cnst)Compiler.GetArgByIndex((FuncTerm)canCast.Node, 0)).GetStringValue() == "TRUE")
                                continue; // Ignore Trivial Ifs

                            fieldChecks.Add(MkIf(MkUnop(CData.Cnst_LNot(), canCast),
                                Compiler.AddArgs(CData.App_Return(), MkId("FALSE"))));
                        }

                        fieldChecks.Add(Compiler.AddArgs(CData.App_Return(), MkId("TRUE")));
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
                methodDefs.Add(MkFunDef(funType, name, prms, Compiler.AddArgs(CData.App_Block(), ctxt.emitCLocals(),
                    ctxt.emitCSideEffects(MkSwitch(MkId("toT"), outerCases, Compiler.AddArgs(CData.App_Return(), MkId("FALSE")))))));
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
            body.Add(MkIf(MkEq(MkId("toT"), MkId(pTypeToCEnum(t))), Compiler.AddArgs(CData.App_Return(), MkId("TRUE"))));
            body.Add(Compiler.AddArgs(CData.App_Return(), MkFunApp(getCCanDownCastName(t), default(Span), MkId("obj"), MkId("toT"))));
            forwardDecls.Add(MkFunDef(funType, name, prms));
            methodDefs.Add(MkFunDef(funType, name, prms, Compiler.AddArgs(CData.App_Block(), ctxt.emitCLocals(),
                ctxt.emitCSideEffects(MkSeq(body)))));
        }

        private void MkEqualsMethods(List<AST<Node>> forwardDecls, List<AST<Node>> methodDefs)
        {
            foreach (var t in compiler.allTypes)
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
                    var arg2T = subT is PPrimitiveType ? pTypeToCType(subT) : MkPtrType(pTypeToCType(subT));
                    var funType = MkFunType(pTypeToCType(PType.Bool), PSMF_DRIVERDECL_TYPE, arg1T, arg2T);
                    var prms = MkFunParams(false, "Driver", "e1", "e2");
                    var body = new List<AST<Node>>();

                    if (t is PAnyType)
                    {
                        if (subT is PAnyType)
                        {   // Switch on e1's type, and in each case defer to comparing an "Any" value with a concrete type.
                            var cases = new List<Tuple<AST<Node>, AST<Node>>>();
                            foreach (var rightT in compiler.subtypes[t])
                            {
                                cases.Add(new Tuple<AST<Node>, AST<Node>>(MkId(pTypeToCEnum(rightT)),
                                        Compiler.AddArgs(CData.App_Return(), MkEq(ctxt.driver, MkDrf(MkId("e1")), t, getPackedValueMember(MkId("e2"), rightT), rightT))));
                            }
                            var sw = MkSwitch(MkArrow("e2", "Type"), cases,
                                MkSeq(MkFunApp("SMF_ASSERTMSG", default(Span), MkStringLiteral("Unknown Type Values in Packed Types"), MkId("FALSE")),
                                Compiler.AddArgs(CData.App_Return(), MkId("FALSE"))));
                            body.Add(sw);
                        }
                        else
                        {   // Comparing an Any type to a concrete type T. Check if Any holds any relative of T, or T itself, and if so, invoke
                            // the correct equality check.
                            var cases = new List<Tuple<AST<Node>, AST<Node>>>();
                            foreach (var midT in compiler.relatives(subT).Where(tp => !(tp is PAnyType)))
                            {
                                if (midT is PAnyType)
                                    continue;

                                var arg2 = (subT is PPrimitiveType) ? MkId("e2") : MkDrf(MkId("e2"));
                                cases.Add(new Tuple<AST<Node>, AST<Node>>(MkId(pTypeToCEnum(midT)),
                                    Compiler.AddArgs(CData.App_Return(), MkEq(ctxt.driver, getPackedValueMember(MkId("e1"), midT), midT, arg2, subT))));
                            }
                            var sw = MkSwitch(MkArrow("e1", "Type"), cases, Compiler.AddArgs(CData.App_Return(), MkId("FALSE")));
                            body.Add(sw);
                        }
                    }
                    else if (t is PTupleType || t is PNamedTupleType)
                    {
                        // Comparing a Tuple/Named tuple with a subtype. The subtype must also be tuple-like;
                        Debug.Assert((t is PTupleType && subT is PTupleType) || (t is PNamedTupleType && subT is PNamedTupleType));
                        var e1Fields = getFieldDesc(t);
                        var e2Fields = getFieldDesc(subT);

                        var fieldsEqualityTerms = e1Fields.Zip(e2Fields, (supField, subField) =>
                            MkEq(ctxt.driver, MkArrow("e1", supField.Item3), supField.Item1, MkArrow("e2", subField.Item3), subField.Item1)).ToArray<AST<Node>>();

                        AST<Node> equalsExp = fieldsEqualityTerms[0];

                        for (int i = 1; i < fieldsEqualityTerms.Length; i++)
                            equalsExp = Compiler.AddArgs(CData.App_BinApp(), CData.Cnst_LAnd(), fieldsEqualityTerms[i], equalsExp);

                        body.Add(Compiler.AddArgs(CData.App_Return(), equalsExp));
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
                        Compiler.AddArgs(CData.App_Block(), ctxt.emitCLocals(), ctxt.emitCSideEffects(MkSeq(body)))));
                }
            }
        }

        private AST<Node> MkCastFromULONGPTR(AST<Node> e, PType to)
        {
            return to is PPrimitiveType ? MkCast(to, e) : MkDrf(MkPtrCast(to, e));
        }

        private AST<Node> MkCastToULONGPTR(AST<Node> e, PType from)
        {
            return from is PPrimitiveType ? Compiler.AddArgs(CData.App_Cast(), MkNmdType("ULONG_PTR"), e) :
                Compiler.AddArgs(CData.App_Cast(), MkNmdType("ULONG_PTR"), MkAddrOf(e));
        }

        private void MkHashCodeMethods(List<AST<Node>> forwardDecls, List<AST<Node>> methodDefs)
        {
            foreach (var t in compiler.allTypes)
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
                        body.Add(MkReturn(Factory.Instance.MkCnst(0)));
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
                    Compiler.AddArgs(CData.App_Block(), ctxt.emitCLocals(), ctxt.emitCSideEffects(MkSeq(body)))));
            }
        }

        private void MkDestroyMethods(List<AST<Node>> forwardDecls, List<AST<Node>> methodDefs)
        {
            foreach (var t in compiler.allTypes)
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
                    Compiler.AddArgs(CData.App_Block(), ctxt.emitCLocals(), ctxt.emitCSideEffects(MkSeq(body)))));

            }
        }

        private void MkCloneMethods(List<AST<Node>> forwardDecls, List<AST<Node>> methodDefs)
        {
            foreach (var t in compiler.allTypes)
            {
                if (!typeNeedsClone(t))
                    continue;

                if (t is PAnyType) // This is handled by Clone_PackedValue in SmfRuntime.c
                    continue;

                PToCFoldContext ctxt = new PToCFoldContext(null, true, this);
                var funType = MkFunType(MkBaseType(CData.Cnst_Void()), MkNmdType("PSMF_DRIVERDECL"), MkPtrType(pTypeToCType(t)), MkPtrType(pTypeToCType(t)));
                var name = getCCloneName(t);
                var prms = MkFunParams(false, "Driver", "dst", "src");
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
                    Compiler.AddArgs(CData.App_Block(), ctxt.emitCLocals(), ctxt.emitCSideEffects(MkSeq(body)))));

            }
        }

        private IEnumerable<Tuple<PType, AST<Node>, string, string>> getFieldDesc(PType t)
        {
            if (t is PTupleType)
            {
                var tupT = t as PTupleType;
                return tupT.elements.Zip(System.Linq.Enumerable.Range(0, tupT.elements.Count()),
                    (type, ind) => new Tuple<PType, AST<Node>, string, string>(type, pTypeToCType(type), Compiler.getTupleField(ind), Compiler.getFuncArg(ind)));

            }
            else if (t is PNamedTupleType)
            {
                return ((PNamedTupleType)t).elements.Select(field => new Tuple<PType, AST<Node>, string, string>(field.Item2, pTypeToCType(field.Item2), field.Item1, "arg_" + field.Item1));
            }
            else
                throw new Exception("Cannot generate field descriptions for non-tuple like type " + t);
        }

        private AST<Node> getPackedValueMember(AST<Node> value, PType memT)
        {
            if (memT is PNilType)
                return MkIntLiteral(0);
            if (memT is PPrimitiveType)
                return Compiler.AddArgs(CData.App_Cast(), pTypeToCType(memT), MkArrow(value, "Value"));
            else
                return MkDrf(Compiler.AddArgs(CData.App_Cast(), MkPtrType(pTypeToCType(memT)), MkArrow(value, "Value")));
        }

        private static AST<FuncTerm> MkEntryFun(string name, AST<Node> locals, AST<Node> body)
        {
            var retTypes = Factory.Instance.AddArg(CData.App_PrmTypes(), MkNmdType("PSMF_SMCONTEXT"));
            retTypes = Factory.Instance.AddArg(retTypes, CData.Cnst_Nil());
            var funType = Factory.Instance.AddArg(CData.App_FunType(), MkNmdType("VOID"));
            funType = Factory.Instance.AddArg(funType, retTypes);

            var prms = Compiler.AddArgs(CData.App_Params(), CData.Cnst_Nil(), Factory.Instance.MkCnst("Context"), CData.Cnst_Nil());

            var funDef = Factory.Instance.AddArg(CData.App_FunDef(), CData.Cnst_Nil());
            funDef = Factory.Instance.AddArg(funDef, funType);
            funDef = Factory.Instance.AddArg(funDef, Factory.Instance.MkCnst(name));
            funDef = Factory.Instance.AddArg(funDef, prms);
            return Factory.Instance.AddArg(funDef, Compiler.AddArgs(CData.App_Block(), locals, body));
        }

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
            var bin = compiler.GetBin(binName);
            var ownerBin = compiler.GetBin(ownerBinName);
            string name;
            LinkedList<AST<FuncTerm>> memList;

            foreach (var e in ownerBin)
            {
                var ownerName = compiler.GetName(e.Node, ownerNameIndex);
                enumMap[ownerName] = new LinkedList<AST<FuncTerm>>();
            }

            foreach (var e in bin)
            {
                name = compiler.GetOwnerName(e.Node, ownerIndex, ownerNameIndex);
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
                    name = string.Format("{0}_{1}_{2}", elemPrefix, enm.Key, compiler.GetName(e.Node, nameIndex));
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

        private static AST<FuncTerm> MkEnums(
            string enumName,
            string prefix,
            IEnumerable<string> names)
        {
            var elements = new List<AST<Node>>(names.Select(name => Factory.Instance.MkCnst(prefix + name)));
            elements.Add(Factory.Instance.MkCnst("_n" + enumName));
            var indices = Enumerable.Range(0, names.Count() + 1).Select(num => MkIntLiteral(num));
            return Compiler.AddArgs(CData.App_EnmDef(), CData.Cnst_Nil(), Factory.Instance.MkCnst(enumName),
                ConstructList2(CData.App_Elements(), elements, indices));
        }

        private static AST<FuncTerm> MkEnum(IEnumerable<string> names, string prefix, string enumName, Dictionary<string, int> idMap = null)
        {
            if (idMap != null)
            {   // Record the index for each name. Note we do this WITHOUT the prifx.
                foreach (var nmId in names.Zip(Enumerable.Range(0, names.Count()), (nm, id) => new Tuple<string, int>(nm, id)))
                    if (!idMap.ContainsKey(nmId.Item1)) idMap[nmId.Item1] = nmId.Item2;
            }

            List<string> allNames = new List<string>(names.Select(nm => prefix + nm));  // Add the prefix
            allNames.Add("_n" + enumName);  // Add the "count" enum member

            return Compiler.AddArgs(CData.App_EnmDef(), CData.Cnst_Nil(), Factory.Instance.MkCnst(enumName),
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
            var bin = compiler.GetBin(binName);
            string name;
            List<string> names = new List<string>();

            if (zeroth != null)
                names.Add(zeroth);

            foreach (var e in bin)
            {
                name = compiler.GetName(e.Node, nameIndex);
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
            var bin = compiler.GetBin(PData.Con_EventSetDecl.Node.Name);

            //// Allocate space for event set declarations.
            Dictionary<string, AST<FuncTerm>[]> machESDecls =
                new Dictionary<string, AST<FuncTerm>[]>();

            foreach (var m in compiler.modelMachineIds.Keys)
            {
                machESDecls.Add(m, new AST<FuncTerm>[compiler.modelEventSetIds["_nEventSets_" + m]]);
            }

            foreach (var e in bin)
            {
                MkEventSet(e, out setName, out machName, out varDefName, out packedElements);
                packedDef = MkVarDef(MkArrType("ULONG32"), string.Format("{0}_Packed", varDefName), packedElements);
                section = Factory.Instance.AddArg(CData.App_Section(), packedDef);
                sections = Factory.Instance.AddArg(section, sections == null ? (AST<Node>)CData.Cnst_Nil() : sections);

                var declId = compiler.modelEventSetIds[string.Format("EventSet_{0}_{1}", machName, setName)];
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
            var bin = compiler.GetBin(PData.Con_MachineDecl.Node.Name);
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
            var eventsTable = new AST<FuncTerm>[compiler.modelEventIds.Count];
            foreach (var ev in compiler.allEvents)
            {
                var eventName = ev.Key;
                var maxInstances = ev.Value.maxInstances;
                var payloadType = ev.Value.payloadType;

                if (eventName == Compiler.DefaultEvent || eventName == Compiler.DeleteEvent)
                    continue;

                var data = MkInit(
                    MkId(string.Format("Event_{0}", eventName)),
                    MkStringLiteral(eventName),
                maxInstances == -1 ? MkId("UINT16_MAX") : MkIntLiteral(maxInstances),
                MkId(pTypeToCEnum(payloadType)));

                eventsTable[compiler.modelEventIds[eventName]] = data;

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
            var bin = compiler.GetBin(PData.Con_MachineDecl.Node.Name);
            var binInits = compiler.GetBin(PData.Con_MachStart.Node.Name);
            var machTable = new AST<FuncTerm>[compiler.modelMachineIds.Count];
            string machName, initName;
            foreach (var e in bin)
            {
                machName = compiler.GetName(e.Node, 0);
                initName = null;
                foreach (var init in binInits)
                {
                    if (compiler.GetOwnerName(init.Node, 0, 0) == machName)
                    {
                        initName = compiler.GetOwnerName(init.Node, 1, 0);
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
                    compiler.allMachines[machName].maxQueueSize == -1 ? MkId("UINT8_MAX") : MkIntLiteral(compiler.allMachines[machName].maxQueueSize),
                    MkId(string.Format("_nEventSets_{0}", machName)),
                    MkId(string.Format("EventSetTable_{0}", machName)),
                    MkId(string.Format("State_{0}_{1}", machName, initName)),
                    MkFunApp("MAKE_OPAQUE_CONSTRUCTOR", default(Span), MkId(string.Format("Constructor_{0}", machName))));
                machTable[compiler.modelMachineIds[machName]] = data;
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
            foreach (var t in compiler.allTypes)
            {
                typeSets.Add(MkVarDef(MkArrType("ULONG32"), pTypeToCEnum(t) + "SuperTypes",
                        MkPackedSet(compiler.supertypes[t].Select(superT => typeIds[pTypeToCEnum(superT)]), compiler.allTypes.Count)));
                typeSets.Add(MkVarDef(MkArrType("ULONG32"), pTypeToCEnum(t) + "SubTypes",
                    MkPackedSet(compiler.subtypes[t].Select(subT => typeIds[pTypeToCEnum(subT)]), compiler.allTypes.Count)));
            }
            return (AST<FuncTerm>)Compiler.ConstructCList(CData.App_Section(), typeSets);
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
                    compiler.DriverDeclName(),
                    data);
        }

        private AST<FuncTerm> MkVarTables()
        {
            var bin = compiler.GetBin(PData.Con_MachineDecl.Node.Name);
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
            var bin = compiler.GetBin(PData.Con_VarDecl.Node.Name);
            var machName = compiler.GetName(machine.Node, 0);
            Node typeNode;
            string varName, varMach;
            var varTable = new AST<FuncTerm>[compiler.modelVarIds[string.Format("_nVars_{0}", machName)]];
            foreach (var s in bin)
            {
                varName = compiler.GetName(s.Node, 0);
                varMach = compiler.GetOwnerName(s.Node, 1, 0);
                if (varMach != machName)
                {
                    continue;
                }

                typeNode = Compiler.GetArgByIndex(s.Node, 2);
                var pType = compiler.GetPType(typeNode);

                var data = MkInit(
                    MkId(string.Format("Var_{0}_{1}", machName, varName)),
                    MkId(string.Format("MachineType_{0}", machName)),
                    MkStringLiteral(varName),
                    MkId(pTypeToCEnum(pType)));
                varTable[compiler.modelVarIds[string.Format("Var_{0}_{1}", machName, varName)]] = data;
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
            var bin = compiler.GetBin(PData.Con_StateDecl.Node.Name);
            var machName = compiler.GetName(machine.Node, 0);
            string stateName, stateMach;
            AST<FuncTerm> section, sections = null;
            var stateTable = new AST<FuncTerm>[compiler.modelStateIds[string.Format("_nStates_{0}", machName)]];
            foreach (var s in bin)
            {
                stateName = compiler.GetName(s.Node, 0);
                stateMach = compiler.GetOwnerName(s.Node, 1, 0);
                if (stateMach != machName)
                {
                    continue;
                }

                var stateInfo = compiler.allMachines[machName].stateNameToStateInfo[stateName];

                var packedTransitionDefName = string.Format("Transitions_{0}_{1}_Packed", machName, stateName);
                var packedTransitionEvents = PackedEvents(stateInfo.transitions.Keys.Where(x => x != Compiler.DefaultEvent));
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
                        passiveFlag = Compiler.AddArgs(CData.App_BinApp(), CData.Cnst_Bor(), MkId("SmfEntryFunPassiveLevel"), MkId("SmfExitFunPassiveLevel"));
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
                var defersSet = Compiler.GetArgByIndex(s.Node, 3);
                var data = MkInit(
                    MkId(string.Format("State_{0}_{1}", machName, stateName)),
                    MkId(string.Format("MachineType_{0}", machName)),
                    MkStringLiteral(stateName),
                    passiveFlag,
                    MkFunApp("MAKE_OPAQUE", default(Span), MkId(string.Format("EntryFun_{0}_{1}", machName, stateName))),
                    stateInfo.exitFun != null ? MkFunApp("MAKE_OPAQUE", default(Span), MkId(string.Format("ExitFun_{0}_{1}", machName, stateName))) : MkId("NULL"),
                    defersSet.NodeKind == NodeKind.Id && ((Id)defersSet).Name == "NIL" ? MkId(string.Format("EventSet_{0}_None", machName)) : MkId(string.Format("EventSet_{0}_{1}", machName, compiler.GetName(compiler.GetFuncTerm(defersSet), 0))),
                    MkIntLiteral(compiler.modelTransSizes[string.Format("State_{0}_{1}", machName, stateName)]),
                    MkId(string.Format("TransTable_{0}_{1}", machName, stateName)),
                    MkId(packedTransitionDefName),
                    MkIntLiteral(stateInfo.actions.Count),
                    MkId(string.Format("ActionTable_{0}_{1}", machName, stateName)),
                    MkId(packedActionDefName),
                    MkIntLiteral(compiler.allMachines[machName].stateNameToStateInfo[stateName].hasDefaultTransition ? 1 : 0));
                stateTable[compiler.modelStateIds[string.Format("State_{0}_{1}", machName, stateName)]] = data;
            }

            AST<FuncTerm> dclSection;
            if (stateTable.Length == 0)
            {
                dclSection = MkSimpleDefine(string.Format("StateTable_{0}", machName), "NULL");
            }
            else
            {
                dclSection = MkVarDef(
                    MkArrType("SMF_STATEDECL"),
                    string.Format("StateTable_{0}", machName),
                    MkInit(stateTable));
            }

            return Compiler.AddArgs(CData.App_Section(), sections, dclSection);
        }

        private AST<FuncTerm> MkTransTables()
        {
            var bin = compiler.GetBin(PData.Con_StateDecl.Node.Name);
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
            var stateName = compiler.GetName(state.Node, 0);
            var machName = compiler.GetOwnerName(state.Node, 1, 0);
            var bin = compiler.GetBin(PData.Con_TransDecl.Node.Name);

            string tState, tMachine;
            int index = 0;
            var transTable = new List<AST<FuncTerm>>();
            foreach (var t in bin)
            {
                tState = compiler.GetOwnerName(t.Node, 0, 0);
                tMachine = compiler.GetOwnerOwnerName(t.Node, 0, 1, 0);
                var isCall = ((Id)Compiler.GetArgByIndex(t.Node, 3)).Name == "TRUE";
                if (tState == stateName && tMachine == machName)
                {
                    var transitionEvent = (Id)Compiler.GetArgByIndex(t.Node, 1);
                    transTable.Add(
                        MkInit(
                            MkIntLiteral(index++),
                            MkId(string.Format("State_{0}_{1}", machName, stateName)),
                            MkId(string.Format("MachineType_{0}", machName)),
                            transitionEvent.Name != PData.Cnst_Default.Node.Name ? MkId(string.Format("Event_{0}", compiler.GetOwnerName(t.Node, 1, 0))) : MkId("SmfDefaultEvent"),
                            MkId(string.Format("State_{0}_{1}", machName, compiler.GetOwnerName(t.Node, 2, 0))),
                            MkId(isCall ? "TRUE" : "FALSE")));
                }
            }

            compiler.modelTransSizes.Add(string.Format("State_{0}_{1}", machName, stateName), transTable.Count);
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
            var bin = compiler.GetBin(PData.Con_StateDecl.Node.Name);
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
            var stateName = compiler.GetName(state.Node, 0);
            var machName = compiler.GetOwnerName(state.Node, 1, 0);
            var bin = compiler.GetBin(PData.Con_Install.Node.Name);

            string tState, tMachine;
            int index = 0;
            var actionTable = new List<AST<FuncTerm>>();
            foreach (var t in bin)
            {
                tState = compiler.GetOwnerName(t.Node, 0, 0);
                tMachine = compiler.GetOwnerOwnerName(t.Node, 0, 1, 0);
                if (tState == stateName && tMachine == machName)
                {
                    var eventName = compiler.GetOwnerName(t.Node, 1, 0);
                    var actionFunName = compiler.GetOwnerName(t.Node, 2, 0);
                    actionTable.Add(
                        MkInit(
                            MkIntLiteral(index++),
                            MkId(string.Format("State_{0}_{1}", machName, stateName)),
                            MkId(string.Format("MachineType_{0}", machName)),
                            MkStringLiteral(actionFunName),
                            MkId(string.Format("Event_{0}", compiler.GetOwnerName(t.Node, 1, 0))),
                            MkFunApp("MAKE_OPAQUE", default(Span), MkId(string.Format("ActionFun_{0}_{1}", machName, actionFunName))),
                            compiler.allMachines[machName].actionFunNameToActionFun[actionFunName].atPassive ? MkId("TRUE") : MkId("FALSE")));
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
            setName = compiler.GetName(eventSet.Node, 0);
            ownerName = compiler.GetOwnerName(eventSet.Node, 1, 0);
            var bin = compiler.GetBin(PData.Con_InEventSet.Node.Name);
            varDefName = string.Format("EventSetMbrs_{0}_{1}", ownerName, setName);

            HashSet<string> eventNames = new HashSet<string>();
            foreach (var e in bin)
            {
                if (compiler.GetOwnerName(e.Node, 0, 0) != setName)
                {
                    continue;
                }
                var eventName = compiler.GetOwnerName(e.Node, 1, 0);
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
                args = Compiler.AddArgs(CData.App_Args(), MkIntLiteral(pack, CData.Cnst_Hex()), args);
            }

            return Factory.Instance.AddArg(CData.App_Init(), args);
        }

        private AST<FuncTerm> PackedEvents(IEnumerable<string> eventNames)
        {
            return MkPackedSet(eventNames.Select(eventName => compiler.modelEventIds[eventName]), compiler.modelEventIds.Count);
        }

        private void MkFunDecls(out AST<FuncTerm> headers, out AST<FuncTerm> bodies)
        {
            var bin = compiler.GetBin(PData.Con_FunDecl.Node.Name);
            headers = compiler.erase ? null : MkFunDef(MkFunType(MkNmdType("BOOLEAN"), Factory.Instance.AddArg(CData.App_BaseType(), Factory.Instance.MkId("VOID"))), "NONDET", MkFunParams(false));
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
                    headers = Compiler.AddArgs(CData.App_Section(), header, headers);
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
                return compiler.declaredTypes[pType].cType;
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
                    return MkStructType(compiler.declaredTypes[pType].cType);
                else if (pType is PNamedTupleType)
                    return MkStructType(compiler.declaredTypes[pType].cType);
                else if (pType is PSeqType)
                    return MkNmdType(Compiler.SMF_ARRAYLIST);
                else if (pType is PMapType)
                    return MkNmdType(Compiler.SMF_HASHTABLE);
            }
            else if (pType is PAnyType)
            {
                return MkNmdType(Compiler.SMF_PACKED_VALUE);
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
                return Compiler.AddArgs(CData.App_Sizeof(), MkNmdType("ULONG_PTR"));
            }
            else if (t is PAnyType || t is PSeqType || t is PMapType)
            {
                return Compiler.AddArgs(CData.App_Sizeof(), pTypeToCType(t));
            }
            else
            {
                var addOp = Compiler.AddArgs(CData.App_BinApp(), CData.Cnst_Add());
                if (t is PTupleType)
                    return (t as PTupleType).elements.Aggregate(MkIntLiteral(0), (acc, el) => Compiler.AddArgs(addOp, GetCTypeSize(el), acc));

                if (t is PNamedTupleType)
                    return (t as PNamedTupleType).elements.Aggregate(MkIntLiteral(0), (acc, el) => Compiler.AddArgs(addOp, GetCTypeSize(el.Item2), acc));

                throw new NotImplementedException("Unknown complex type: " + t);
            }
        }

        private AST<FuncTerm> MkForwardFunDecl(AST<Node> storageClass, AST<Node> funType, string funName, IEnumerable<string> paramNames)
        {
            return Compiler.AddArgs(CData.App_FunDef(), storageClass, funType, Factory.Instance.MkCnst(funName), MkFunParams(false, paramNames.ToArray()), CData.Cnst_Unknown());
        }

        private AST<FuncTerm> MkForwardFunDecl(AST<Node> storageClass, AST<Node> funType, string funName, params string[] paramNames)
        {
            return MkForwardFunDecl(storageClass, funType, funName, new List<string>(paramNames));
        }


        private AST<FuncTerm> MkFunDef(AST<FuncTerm> funType, string funName, AST<Node> parameters)
        {
            return Compiler.AddArgs(CData.App_FunDef(), CData.Cnst_Nil(), funType, Factory.Instance.MkCnst(funName), parameters, CData.Cnst_Unknown());
        }

        private AST<FuncTerm> MkFunDef(AST<FuncTerm> funType, string funName, AST<Node> parameters, AST<Node> body)
        {
            return Compiler.AddArgs(CData.App_FunDef(), CData.Cnst_Nil(), funType, Factory.Instance.MkCnst(funName), parameters, body);
        }

        private void MkFunDecl(AST<FuncTerm> fun, out AST<FuncTerm> funHeader, out AST<FuncTerm> funBody)
        {
            var funName = compiler.GetName(fun.Node, 0);
            var ownerName = compiler.GetOwnerName(fun.Node, 1, 0);
            var qualifiedFunName = compiler.allMachines[ownerName].funNameToFunInfo[funName].isForeign ? funName : string.Format("{0}_{1}", ownerName, funName);
            var parameters = Compiler.GetArgByIndex(fun.Node, 2);
            var pReturnType = compiler.GetPType(Compiler.GetArgByIndex(fun.Node, 3));
            AST<FuncTerm> cReturnType;
            var cParameterNames = new List<string>();
            var cParameterTypes = new List<AST<Node>>();
            if (compiler.erase)
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
                    var pParameterType = compiler.GetPType(it.Current);
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
            if (compiler.erase)
                return;

            var ctxt = new PToCFoldContext(ownerName, true, this, MkArrow("Context", "Driver"), funName);
            var outTerm = Factory.Instance.ToAST(Compiler.GetArgByIndex(fun.Node, 5)).Compute<CTranslationInfo>(
                x => EntryFun_UnFold(ctxt, x),
                (x, ch) => EntryFun_Fold(ctxt, x, ch));

            // Add temporary local variables, accumulated as a result of side effects
            funBody = Compiler.AddArgs(CData.App_Block(), ctxt.emitCLocals(), ctxt.emitCSideEffects(outTerm.node));
            Debug.Assert(ctxt.sideEffectsStack.Count == 0);

            funBody = Compiler.AddArgs(CData.App_Seq(), MkFunApp("DUMMYREFERENCE", default(Span), MkId("Context")), funBody);
            funBody = MkFunDef(MkFunType(cReturnType, cParameterTypes.ToArray()),
                                 qualifiedFunName,
                                 MkFunParams(false, cParameterNames.ToArray()),
                                 funBody);
        }

        private void MkEntryFuns(out AST<FuncTerm> headers, out AST<FuncTerm> bodies)
        {
            var bin = compiler.GetBin(PData.Con_StateDecl.Node.Name);
            headers = null;
            bodies = null;

            AST<FuncTerm> header, body;
            foreach (var s in bin)
            {
                var name = compiler.GetName(s.Node, 0);
                var ownerName = compiler.GetOwnerName(s.Node, 1, 0);
                var entryFunName = string.Format("EntryFun_{0}_{1}", ownerName, name);
                var entryFun = Factory.Instance.ToAST(Compiler.GetArgByIndex(s.Node, 2));
                header = Compiler.AddArgs(CData.App_VarDef(), CData.Cnst_Nil(), MkNmdType("SMF_ENTRYFUN"), Factory.Instance.MkCnst(entryFunName), CData.Cnst_Nil());
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
            var bin = compiler.GetBin(PData.Con_ActionDecl.Node.Name);
            headers = null;
            bodies = null;

            AST<FuncTerm> header, body;
            foreach (var s in bin)
            {
                var actionFunName = compiler.GetName(s.Node, 0);
                var ownerName = compiler.GetOwnerName(s.Node, 1, 0);
                var actionFun = Factory.Instance.ToAST(Compiler.GetArgByIndex(s.Node, 2));
                var uniqueActionFunName = string.Format("ActionFun_{0}_{1}", ownerName, actionFunName);
                header = Compiler.AddArgs(CData.App_VarDef(), CData.Cnst_Nil(), MkNmdType("SMF_ENTRYFUN"), Factory.Instance.MkCnst(uniqueActionFunName), CData.Cnst_Nil());
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
            var bin = compiler.GetBin("ExitFun");
            headers = null;
            bodies = null;

            AST<FuncTerm> header, body;
            foreach (var s in bin)
            {
                using (var it = s.Node.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var stateDecl = compiler.GetFuncTerm(it.Current);
                    var stateName = compiler.GetName(stateDecl, 0);
                    var ownerName = compiler.GetMachineName(stateDecl, 1);
                    var exitFunName = string.Format("ExitFun_{0}_{1}", ownerName, stateName);
                    it.MoveNext();
                    var exitFun = Factory.Instance.ToAST(it.Current);
                    header = Compiler.AddArgs(CData.App_VarDef(), CData.Cnst_Nil(), MkNmdType("SMF_EXITFUN"), Factory.Instance.MkCnst(exitFunName), CData.Cnst_Nil());
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
            var bin = compiler.GetBin(PData.Con_MachineDecl.Node.Name);
            consHeaders = null;
            foreach (var m in bin)
            {
                var machineName = compiler.GetName(m.Node, 0);
                var constructorName = string.Format("Constructor_{0}", machineName);
                AST<FuncTerm> consHeader = Compiler.AddArgs(CData.App_VarDef(), CData.Cnst_Nil(), MkNmdType("SMF_CONSTRUCTORFUN"), Factory.Instance.MkCnst(constructorName), CData.Cnst_Nil());

                if (consHeaders == null)
                {
                    consHeaders = consHeader;
                }
                else
                {
                    consHeaders = Compiler.AddArgs(CData.App_Section(), consHeader, consHeaders);
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
            foreach (var i in children)
            {
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
                var app = Compiler.AddArgs(CData.App_BinApp(), CData.Cnst_Eq(), Compiler.AddArgs(CData.App_BinApp(), CData.Cnst_PFld(), MkId("Context"), MkId("ReturnTo")), MkIntLiteral(i));
                var gotoStmt = Compiler.AddArgs(CData.App_Goto(), Factory.Instance.MkCnst(string.Format("L{0}", i)));
                trampoline = Compiler.AddArgs(CData.App_ITE(), app, gotoStmt, trampoline);
            }

            var ctxt = new PToCFoldContext(ownerName, false, this, MkArrow("Context", "Driver"));
            var outTerm = entryFun.Compute<CTranslationInfo>(
                x => EntryFun_UnFold(ctxt, x),
                (x, ch) => EntryFun_Fold(ctxt, x, ch));

            AST<Node> rawBody = ctxt.emitCSideEffects(outTerm.node);
            Debug.Assert(ctxt.sideEffectsStack.Count == 0);

            if (trampoline != CData.Cnst_Nil())
            {
                rawBody = Compiler.AddArgs(CData.App_Seq(), trampoline, rawBody);
            }
            rawBody = Compiler.AddArgs(CData.App_Seq(), MkFunApp("DUMMYREFERENCE", default(Span), MkId("Context")), rawBody);
            funBody = MkEntryFun(entryFunName, ctxt.emitCLocals(), rawBody);
        }

        private AST<FuncTerm> MkClone(PToCFoldContext ctxt, PType t, Span span, AST<Node> dst, AST<Node> src)
        {
            Debug.Assert(t is PCompoundType || t is PAnyType);
            return MkFunApp(getCCloneName(t), span, ctxt.driver, dst, src);
        }

        private AST<Node> MkDataDef(string name, PType t)
        {
            var fieldDesc = getFieldDesc(t);
            var cType = pTypeToCType(t);

            var fields = ConstructList3(CData.App_Fields(),
                fieldDesc.Select(fd => pTypeToCType(fd.Item1)),
                fieldDesc.Select(fd => Factory.Instance.MkCnst(fd.Item3)),
                fieldDesc.Select(fd => CData.Cnst_Nil()));

            return Compiler.AddArgs(CData.App_DataDef(), CData.Cnst_Nil(), CData.Cnst_Struct(), Factory.Instance.MkCnst(name), fields);
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
                if (type is PTupleType || type is PNamedTupleType)
                {
                    var structType = Compiler.AddArgs(CData.App_NmdType(), CData.Cnst_Struct(), Factory.Instance.MkCnst(compiler.declaredTypes[type].cType));
                    var pStructType = Compiler.AddArgs(CData.App_PtrType(), structType);
                    var casted = Compiler.AddArgs(CData.App_Cast(), pStructType, arr);
                    var derefed = MkUnop(CData.Cnst_Drf(), casted);
                    return derefed;
                }
                else if (type is PAnyType)
                {
                    var casted = Compiler.AddArgs(CData.App_Cast(), MkNmdType("PSMF_PACKED_VALUE"), arr);
                    var derefed = MkUnop(CData.Cnst_Drf(), casted);
                    return derefed;
                }
                else if (type is PSeqType)
                {
                    var casted = Compiler.AddArgs(CData.App_Cast(), MkNmdType("PSMF_ARRAYLIST"), arr);
                    var derefed = MkUnop(CData.Cnst_Drf(), casted);
                    return derefed;
                }
                else if (type is PMapType)
                {
                    var casted = Compiler.AddArgs(CData.App_Cast(), MkNmdType("PSMF_HASHTABLE"), arr);
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
                return MkFunApp("PackValue", default(Span), ctxt.driver, MkAddrOf(lhs), Compiler.AddArgs(CData.App_Cast(default(Span)), MkNmdType("ULONG_PTR"), rhs), MkId(pTypeToCEnum(rhsType)));
            }
            else
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
                    Compiler.AddArgs(CData.App_Cast(), MkPtrType(pTypeToCType(lhsType)), MkAddrOf(lhs)),
                    (rhsType is PPrimitiveType) ? consumedRhs : MkAddrOf(consumedRhs));
            }
            else
                throw new Exception(string.Format("Can't assign ({0})::{1} to ({2})::{3}.", dbgAst2Str(rhs), rhsType, dbgAst2Str(lhs), lhsType));
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
            private PToC pToC;
            private AST<Node> driverRef;

            public PToCFoldContext(string ownerName, bool isFunction, PToC comp, AST<Node> driver = null, string funName = null)
            {
                this.ownerName = ownerName;
                this.callStatementCounter = 0;
                this.isFunction = isFunction;
                this.sideEffectsStack = new Stack<List<AST<Node>>>();
                this.freeVars = new HashSet<string>();
                this.busyVars = new HashSet<string>();
                this.tmpUseCount = new Dictionary<string, int>();
                this.destroyStack = new Stack<List<Tuple<PType, bool, string>>>();
                this.pToC = comp;
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

                foreach (PType t in tmpVars.Keys)
                {
                    foreach (Tuple<string, bool> var in tmpVars[t])
                    {
                        if (tmpUseCount[var.Item1] == 0)
                            continue;

                        varDefs.Add(MkVarDef(var.Item2 ? MkPtrType(pToC.pTypeToCType(t)) : pToC.pTypeToCType(t), var.Item1));
                    }
                }
                return Compiler.ConstructCList(CData.App_Defs(), varDefs);
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

                    res.Add(MkFunApp(pToC.getCDestroyName(v.Item1), default(Span), driver, MkAddrOf(MkId(v.Item3))));

                }

                if (res.Count > 1)
                    return PToC.MkSeq(res);
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

                        res.Add(MkFunApp(pToC.getCDestroyName(v.Item1), default(Span), driver, MkAddrOf(MkId(v.Item3))));
                    }
                }

                res.Add(stmt);

                if (res.Count > 1)
                    return PToC.MkSeq(res);
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
                if (hasFreeVar(pType, isPtr))
                {
                    var tmpVar = tmpVars[pType].First(var => var.Item2 == isPtr && freeVars.Contains(var.Item1));
                    freeVars.Remove(tmpVar.Item1);
                    tmpVarName = tmpVar.Item1;
                }
                else
                {
                    var cType = pToC.pTypeToCType(pType);
                    tmpVarName = pToC.compiler.getUnique("tmp");
                    tmpUseCount[tmpVarName] = 0;

                    if (!tmpVars.ContainsKey(pType))
                        tmpVars[pType] = new HashSet<Tuple<string, bool>>();

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
                    var name = ((Cnst)Compiler.GetArgByIndex((FuncTerm)n.Node, 0)).GetStringValue();
                    Debug.Assert(busyVars.Contains(name));
                    tmpUseCount[name]++;
                    busyVars.Remove(name);
                    freeVars.Add(name);
                }

                return n;
            }

            public bool isTmpVar(Node n)
            {
                if (!Compiler.isInstanceOf(n, CData.App_Ident()))
                    return false;

                var name = ((Cnst)Compiler.GetArgByIndex((FuncTerm)n, 0)).GetStringValue();
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
                Debug.Assert(Compiler.isInstanceOf(var.Node, CData.App_Ident()));
                var name = ((Cnst)Compiler.GetArgByIndex((FuncTerm)var.Node, 0)).GetStringValue();

                var topSideEffectStack = this.sideEffectsStack.Pop();
                var newSideEffectStack = topSideEffectStack.Select(term => pToC.ReplaceVar(term, name, expr));
                this.sideEffectsStack.Push(new List<AST<Node>>(newSideEffectStack));
            }
        }

        private Dictionary<Node, Node> cnodeToPNode = new Dictionary<Node, Node>();

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
                if (Compiler.getFtName((FuncTerm)n) == CData.Con_Ident().Node.Name)
                {
                    var varName = ((Cnst)Compiler.GetArgByIndex((FuncTerm)n, 0)).GetStringValue();
                    return varName == ctxt.var ? ctxt.expr : MkId(varName);
                }
                else
                {
                    var ftId = (AST<Id>)Factory.Instance.ToAST((Id)((FuncTerm)n).Function);
                    var res = Factory.Instance.MkFuncTerm(ftId, n.Span);
                    return Compiler.AddArgs(res, children);
                }
            }
            else
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
    
        private PType getComputedType(Node n)
        {
            return compiler.computedType[n].type;
        }

        private CTranslationInfo EntryFun_Fold(PToCFoldContext ctxt, Node n, IEnumerable<CTranslationInfo> children)
        {
            var ret = EntryFun_Fold_Impl(ctxt, n, children);

            if (n != null && ret != null)
                cnodeToPNode[ret.node.Node] = n;

            return ret;
        }

        private bool shouldErase(Node pNode) { return compiler.erase && compiler.computedType[pNode].isGhost; }

        private CTranslationInfo EntryFun_Fold_Impl(PToCFoldContext ctxt, Node n, IEnumerable<CTranslationInfo> children)
        {
            string ownerName = ctxt.ownerName;
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                if (n.NodeKind == NodeKind.Cnst)
                {
                    var cnst = (Cnst)n;
                    if (cnst.CnstKind == CnstKind.Numeric)
                    {
                        return new CTranslationInfo(MkIntLiteral((int)cnst.GetNumericValue().Numerator, n.Span));
                    }
                    else
                    {
                        return new CTranslationInfo(Factory.Instance.ToAST(n));
                    }
                }
                else if (n.NodeKind == NodeKind.Id)
                {
                    var id = (Id)n;
                    if (id.Name == PData.Cnst_This.Node.Name)
                    {
                        return new CTranslationInfo(CData.Trm_This(n.Span));
                    }
                    else if (id.Name == PData.Cnst_Trigger.Node.Name)
                    {
                        return new CTranslationInfo(CData.Trm_Trigger(n.Span));
                    }
                    else if (id.Name == PData.Cnst_Nondet.Node.Name)
                    {
                        if (shouldErase(n))
                            return new CTranslationInfo((CData.Cnst_Nil(n.Span)));

                        return new CTranslationInfo(MkFunApp("NONDET", n.Span));
                    }
                    else if (id.Name == PData.Cnst_Nil.Node.Name)
                    {
                        return new CTranslationInfo(CData.Cnst_Nil(n.Span));
                    }
                    else if (id.Name == PData.Cnst_True.Node.Name)
                    {
                        return new CTranslationInfo(MkId("TRUE", n.Span));
                    }
                    else if (id.Name == PData.Cnst_False.Node.Name)
                    {
                        return new CTranslationInfo(MkId("FALSE", n.Span));
                    }
                    else if (id.Name == PData.Cnst_Leave.Node.Name)
                    {
                        return new CTranslationInfo(Factory.Instance.AddArg(CData.App_Return(n.Span), CData.Cnst_Nil(n.Span)));
                    }
                    else if (id.Name == PData.Cnst_Delete.Node.Name)
                    {
                        return new CTranslationInfo(Compiler.AddArgs(CData.App_Seq(n.Span), MkFunApp("SmfDelete", n.Span, MkId("Context")),
                            ctxt.emitAllCleanup(Compiler.AddArgs(CData.App_Return(n.Span), CData.Cnst_Nil(n.Span)))));
                    }
                    else if (id.Name == PData.Cnst_Null.Node.Name)
                    {
                        return new CTranslationInfo(MkIntLiteral(0));
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
                    return new CTranslationInfo(CData.Cnst_Nil(n.Span));

                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    return new CTranslationInfo(MkAssert(ctxt.consumeExp(it.Current.node), n.Span));
                }
            }
            else if (funName == PData.Con_Return.Node.Name)
            {
                if (shouldErase(n))
                    return new CTranslationInfo(CData.Cnst_Nil(n.Span));

                if (ctxt.isFunction)
                {
                    using (var it = children.GetEnumerator())
                    {
                        it.MoveNext();
                        var formalRetType = compiler.allMachines[ownerName].funNameToFunInfo[ctxt.funName].returnType;
                        var actualRetType = getComputedType(cnodeToPNode[it.Current.node.Node]);

                        if (formalRetType is PNilType)
                        {
                            Debug.Assert(actualRetType is PNilType);
                            return new CTranslationInfo(ctxt.emitAllCleanup(Compiler.AddArgs(CData.App_Return(n.Span), CData.Cnst_Nil(n.Span))));
                        }
                        else if (formalRetType is PPrimitiveType)
                        {   // For primitive types, just return the expected value
                            if (formalRetType != actualRetType)
                            {   // Need an implicit up cast from the actual return value to the formal return type.
                                var tmpVar = ctxt.getTmpVar(formalRetType, false);
                                ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, tmpVar, formalRetType, ctxt.consumeExp(it.Current.node), actualRetType));
                                return new CTranslationInfo(ctxt.emitAllCleanup(Compiler.AddArgs(CData.App_Return(n.Span), tmpVar)));
                            }
                            else
                            {
                                return new CTranslationInfo(ctxt.emitAllCleanup(Compiler.AddArgs(CData.App_Return(n.Span), ctxt.consumeExp(it.Current.node))));
                            }
                        }
                        else
                        {   // For compound return types, assign them to the "dst" parameter and return void.
                            Debug.Assert(formalRetType is PAnyType || formalRetType is PCompoundType); // Just a reminder to revisit this when adding new types.
                            return new CTranslationInfo(MkSeq(MkAssignOrCast(ctxt, n.Span, MkDrf(MkId("dst")), formalRetType, ctxt.consumeExp(it.Current.node), actualRetType),
                                ctxt.emitAllCleanup(Compiler.AddArgs(CData.App_Return(n.Span), CData.Cnst_Nil(n.Span)))));
                        }
                    }
                }
                else
                {
                    return new CTranslationInfo(Compiler.AddArgs(CData.App_Seq(n.Span), MkFunApp("SmfPop", n.Span, MkId("Context")),
                        ctxt.emitAllCleanup(Compiler.AddArgs(CData.App_Return(n.Span), CData.Cnst_Nil(n.Span)))));
                }
            }
            else if (funName == PData.Con_DataOp.Node.Name)
            {
                if (shouldErase(n))
                    return new CTranslationInfo(CData.Cnst_Nil(n.Span));
                var op = ((Id)Compiler.GetArgByIndex(ft, 0)).Name;

                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var mutatedVar = it.Current.node;
                    var mutatedVarT = getComputedType(cnodeToPNode[mutatedVar.Node]);
                    if (mutatedVarT is PSeqType)
                    {
                        var innerT = (mutatedVarT as PSeqType).T;
                        it.MoveNext();
                        var ind = it.Current.node;

                        if (op == PData.Cnst_Remove.Node.Name)
                        {
                            return new CTranslationInfo(MkFunApp("SmfArrayListRemove", n.Span, ctxt.driver, MkAddrOf(mutatedVar), ind));
                        }

                        it.MoveNext();
                        var val = it.Current.node;
                        var valT = getComputedType(cnodeToPNode[val.Node]);

                        var tmpUp = ctxt.getTmpVar(innerT, false);
                        ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, ctxt.consumeExp(tmpUp), innerT, val, valT));
                        if (op == PData.Cnst_Insert.Node.Name)
                        {
                            return new CTranslationInfo(MkFunApp("SmfArrayListInsert", n.Span, ctxt.driver, MkAddrOf(mutatedVar), ind, MkCastToULONGPTR(tmpUp, innerT)));
                        }
                        else
                        {
                            return new CTranslationInfo(MkAssignment(MkIdx(MkArrow(MkAddrOf(mutatedVar), "Values"), ind), MkCastToULONGPTR(tmpUp, innerT)));
                        }
                    }
                    else
                    {
                        Debug.Assert(mutatedVarT is PMapType);
                        var mutatedVarKeyT = (mutatedVarT as PMapType).KeyT;
                        var mutatedVarValT = (mutatedVarT as PMapType).ValT;
                        it.MoveNext();
                        var key = it.Current.node;
                        var keyT = getComputedType(cnodeToPNode[key.Node]);

                        var tmpKey = ctxt.getTmpVar(mutatedVarKeyT, false);
                        ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, tmpKey, mutatedVarKeyT, key, keyT));
                        if (op == PData.Cnst_Remove.Node.Name)
                        {
                            return new CTranslationInfo(MkFunApp("SmfHashtableRemove", n.Span, ctxt.driver, MkAddrOf(mutatedVar), MkCastToULONGPTR(ctxt.consumeExp(tmpKey), keyT)));
                        }

                        it.MoveNext();
                        var val = it.Current.node;
                        var valT = getComputedType(cnodeToPNode[val.Node]);

                        var tmpVal = ctxt.getTmpVar(mutatedVarValT, false);
                        ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, tmpVal, mutatedVarValT, val, valT));
                        return new CTranslationInfo(MkFunApp("SmfHashtableUpdate", n.Span, ctxt.driver, MkAddrOf(mutatedVar), MkCastToULONGPTR(ctxt.consumeExp(tmpKey), keyT), MkCastToULONGPTR(ctxt.consumeExp(tmpVal), mutatedVarValT)));
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
                    result = Compiler.AddArgs(CData.App_Seq(n.Span), result, Compiler.AddArgs(CData.App_BinApp(n.Span), CData.Cnst_Asn(n.Span), Compiler.AddArgs(CData.App_BinApp(n.Span), CData.Cnst_PFld(n.Span), MkId("Context"), MkId("ReturnTo")), MkIntLiteral(callLabel)));
                    result = Compiler.AddArgs(CData.App_Seq(n.Span), result, MkFunApp("SmfCall", n.Span, MkId("Context"), ctxt.consumeExp(it.Current.node)));
                    result = Compiler.AddArgs(CData.App_Seq(n.Span), result, ctxt.emitAllCleanup(Factory.Instance.AddArg(CData.App_Return(n.Span), CData.Cnst_Nil(n.Span))));
                    result = Compiler.AddArgs(CData.App_Seq(n.Span), result, Compiler.AddArgs(CData.App_Lbl(n.Span), Factory.Instance.MkCnst(string.Format("L{0}", callLabel)), CData.Cnst_Nil(n.Span)));
                    return new CTranslationInfo(result);
                }
            }
            else if (funName == PData.Con_Ecall.Node.Name)
            {
                return new CTranslationInfo(CData.Cnst_Nil(n.Span));
            }
            else if (funName == PData.Con_Seq.Node.Name)
            {
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var outTerm = Factory.Instance.AddArg(CData.App_Seq(n.Span), ctxt.emitCSideEffects(it.Current.node));
                    it.MoveNext();
                    return new CTranslationInfo(Factory.Instance.AddArg(outTerm, it.Current.node));
                }
            }
            else if (funName == PData.Con_Assign.Node.Name)
            {
                if (shouldErase(n))
                    return new CTranslationInfo(CData.Cnst_Nil(n.Span));

                var lhsType = getComputedType(Compiler.GetArgByIndex(ft, 0));
                var rhsType = getComputedType(Compiler.GetArgByIndex(ft, 1));

                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var lhs = it.Current.node;
                    it.MoveNext();
                    var rhs = it.Current.node;
                    var rhsIsKeys = it.Current.isKeys;

                    if (rhsIsKeys)
                    {
                        return new CTranslationInfo(MkAssignOrCast(ctxt, n.Span, lhs, lhsType, MkFunApp("SmfHashtableConvertToArrayList", n.Span, ctxt.driver, MkAddrOf(rhs)), rhsType));
                    }
                    else
                    {
                        return new CTranslationInfo(MkAssignOrCast(ctxt, n.Span, lhs, lhsType, rhs, rhsType));
                    }
                }
            }
            else if (funName == PData.Con_ITE.Node.Name)
            {
                if (shouldErase(n))
                    return new CTranslationInfo(CData.Cnst_Nil(n.Span));

                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var cond = ctxt.consumeExp(it.Current.node);
                    it.MoveNext();
                    var thenBody = it.Current.node;
                    it.MoveNext();
                    var elseBody = it.Current.node;

                    var elseStmt = ctxt.emitCSideEffects(elseBody);
                    var thenStmt = ctxt.emitCSideEffects(thenBody);

                    return new CTranslationInfo(Compiler.AddArgs(CData.App_ITE(n.Span), cond, thenStmt, elseStmt));
                }
            }
            else if (funName == PData.Con_Payload.Node.Name)
            {
                PType argT = getComputedType(ft);

                if (argT is PAnyType)
                {
                    return new CTranslationInfo(CData.Trm_Arg(n.Span));
                }
                else
                {   // Downcast
                    var tmpVar = ctxt.getTmpVar(argT, false);
                    ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, tmpVar, argT, CData.Trm_Arg(n.Span), PType.Any));
                    return new CTranslationInfo(tmpVar);
                }
            }
            else if (funName == PData.Con_Use.Node.Name)
            {
                var kind = (Id)Compiler.GetArgByIndex(ft, 1);
                if (kind.Name == PData.Cnst_Var.Node.Name)
                {
                    var varName = compiler.GetName(ft, 0);
                    Debug.Assert((compiler.erase && compiler.allMachines[ownerName].localVariableToVarInfo.ContainsKey(varName) && compiler.allMachines[ownerName].localVariableToVarInfo[varName].isGhost) ==
                        shouldErase(n));

                    if (shouldErase(n))
                        return new CTranslationInfo(CData.Cnst_Nil(n.Span));
                    PType type;
                    AST<Node> varExp;

                    if (ctxt.isFunction && compiler.allMachines[ownerName].funNameToFunInfo[ctxt.funName].parameterNameToInfo.ContainsKey(varName))
                    {   // This is a function parameter. (And we are in /doNotErase mode)
                        type = compiler.allMachines[ownerName].funNameToFunInfo[ctxt.funName].parameterNameToInfo[varName].type;
                        varExp = type is PPrimitiveType ? MkId(varName) : MkDrf(MkId(varName));
                    }
                    else
                    {
                        // This is a machine local variable.
                        type = compiler.allMachines[ownerName].localVariableToVarInfo[varName].type;
                        varExp = MkVar(varName, ownerName, type);
                    }

                    return new CTranslationInfo(varExp);
                }
                else if (kind.Name == PData.Cnst_Event.Node.Name)
                {
                    var eventName = compiler.GetName(ft, 0);
                    if (eventName == PData.Cnst_Default.Node.Name)
                        return new CTranslationInfo(MkId("SmfDefaultEvent", n.Span));
                    return new CTranslationInfo(MkId(string.Format("Event_{0}", eventName), n.Span));
                }
                else if (kind.Name == PData.Cnst_State.Node.Name)
                {
                    return new CTranslationInfo(MkId(string.Format("State_{0}_{1}", ownerName, compiler.GetName(ft, 0)), n.Span));
                }
                else if (kind.Name == PData.Cnst_Field.Node.Name)
                {
                    var field = compiler.GetName(ft, 0);
                    return new CTranslationInfo(Factory.Instance.MkCnst(field));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if (funName == PData.Con_Call.Node.Name)
            {
                if (shouldErase(n))
                    return new CTranslationInfo(CData.Cnst_Nil(n.Span));

                var calleeName = compiler.GetName(ft, 0);
                FunInfo funInfo = compiler.allMachines[ctxt.ownerName].funNameToFunInfo[calleeName];
                var argList = new List<AST<Node>>();
                AST<Node> tmpRetVar = null;

                if (compiler.erase)
                {
                    argList.Add(Compiler.AddArgs(CData.App_BinApp(n.Span), CData.Cnst_PFld(n.Span), MkId("Context"), MkId("ExtContext")));
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
                        var actual = it.Current.node;

                        if (actual.Node.NodeKind == NodeKind.Id && ((Id)actual.Node).Name == "NIL")
                            break;

                        var actualType = compiler.computedType[cnodeToPNode[actual.Node]].type;
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
                    return new CTranslationInfo(MkFunApp(calleeName, n.Span, argList.ToArray()));
                else
                {
                    ctxt.addSideEffect(MkFunApp(calleeName, n.Span, argList.ToArray()));
                    return new CTranslationInfo(ctxt.consumeExp(tmpRetVar));
                }
            }
            else if (funName == PData.Con_Apply.Node.Name)
            {
                if (shouldErase(n))
                    return new CTranslationInfo(CData.Cnst_Nil(n.Span));

                using (var it = children.GetEnumerator())
                {

                    int arity;
                    var cOp = PData.POpToCOp((Id)Compiler.GetArgByIndex(ft, 0), out arity);
                    var pOp = ((Id)Compiler.GetArgByIndex(ft, 0)).Name;
                    it.MoveNext();
                    it.MoveNext();

                    var arg1 = ctxt.consumeExp(it.Current.node);
                    var arg1IsKeys = it.Current.isKeys;

                    if (arg1 == null)
                        return null;

                    if (arity == 1)
                    {
                        if (pOp == PData.Cnst_Sizeof.Node.Name)
                        {
                            return new CTranslationInfo(MkDot(arg1, "Size"));
                        }
                        else if (pOp == PData.Cnst_Keys.Node.Name)
                        {
                            return new CTranslationInfo(arg1, true);
                        }
                        else
                        {
                            return new CTranslationInfo(Compiler.AddArgs(CData.App_UnApp(n.Span), cOp, arg1));
                        }
                    }
                    else if (arity == 2)
                    {
                        it.MoveNext();
                        var arg2 = ctxt.consumeExp(it.Current.node);

                        if (arg2 == null)
                            return null;

                        var arg1Type = getComputedType(Compiler.GetArgByIndex((FuncTerm)Compiler.GetArgByIndex(ft, 1), 0));
                        var arg2Type = getComputedType(Compiler.GetArgByIndex((FuncTerm)Compiler.GetArgByIndex((FuncTerm)Compiler.GetArgByIndex(ft, 1), 1), 0));

                        if (pOp == PData.Cnst_In.Node.Name)
                        {
                            return new CTranslationInfo(MkFunApp("SmfHashtableContains", n.Span, ctxt.driver, MkAddrOf(arg2), arg1));
                        }
                        else if (pOp == PData.Cnst_Idx.Node.Name)
                        {
                            var baseType = getComputedType(cnodeToPNode[arg1.Node]);
                            if (baseType is PTupleType)
                            {
                                return new CTranslationInfo(MkDot(arg1, Compiler.getTupleField(getCIntConst((FuncTerm)arg2.Node))));
                            }
                            else if (baseType is PSeqType)
                            {
                                if (arg1IsKeys)
                                {
                                    return new CTranslationInfo(MkCastFromULONGPTR(MkFunApp("SmfHashtableLookupKeyAtIndex", n.Span, ctxt.driver, MkAddrOf(arg1), arg2), (baseType as PSeqType).T));
                                }
                                else
                                {
                                    return new CTranslationInfo(MkCastFromULONGPTR(MkIdx(MkDot(arg1, "Values"), arg2), (baseType as PSeqType).T));
                                }
                            }
                            else
                            {
                                Debug.Assert(baseType is PMapType);
                                return new CTranslationInfo(MkCastFromULONGPTR(MkFunApp("SmfHashtableLookup", n.Span, ctxt.driver, MkAddrOf(arg1), arg2), (baseType as PMapType).ValT));
                            }
                        }
                        else if (pOp == PData.Cnst_Fld.Node.Name)
                        {
                            return new CTranslationInfo(MkDot(arg1, ((Cnst)arg2.Node).GetStringValue()));
                        }
                        else if (cOp.Node.Name == CData.Cnst_Eq().Node.Name || cOp.Node.Name == CData.Cnst_NEq().Node.Name)
                        {
                            var app = MkEq(ctxt.driver, arg1, arg1Type, arg2, arg2Type);
                            return new CTranslationInfo(cOp.Node.Name == CData.Cnst_Eq().Node.Name ?
                                app : Compiler.AddArgs(CData.App_UnApp(n.Span), CData.Cnst_LNot(n.Span), app));
                        }
                        else
                        {
                            if (arg1Type is PIntType)
                            {
                                Debug.Assert(arg2Type is PIntType);
                                arg1 = arg1Type is PPrimitiveType ? MkCast(arg1Type, arg1) : arg1; // TODO: Ternary looks unnecessary here. Fix this
                                arg2 = arg2Type is PPrimitiveType ? MkCast(arg1Type, arg2) : arg2;
                            }

                            return new CTranslationInfo(Compiler.AddArgs(CData.App_BinApp(n.Span), cOp, arg1, arg2));
                        }
                    }
                    throw new NotImplementedException();

                }
            }
            else if (funName == Compiler.Con_LabeledExpr)
            {
                if (shouldErase(Compiler.GetArgByIndex(ft, 1)))
                    return new CTranslationInfo(CData.Cnst_Nil(n.Span));

                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    if (it.Current == null)
                        return null;

                    return new CTranslationInfo(Compiler.AddArgs(Compiler.App_LabeledExpr, Factory.Instance.MkCnst(((Cnst)Compiler.GetArgByIndex(ft, 0)).GetStringValue()),
                        it.Current.node));
                }
            }
            else if (funName == PData.Con_New.Node.Name)
            {
                if (shouldErase(n))
                    return new CTranslationInfo(CData.Cnst_Nil(n.Span));

                var machTypeName = compiler.GetOwnerName(ft, 0, 0);
                List<AST<Node>> argList = new List<AST<Node>>();
                argList.Add(Compiler.AddArgs(CData.App_UnApp(n.Span), CData.Cnst_Addr(n.Span), MkId(compiler.DriverDeclName())));
                argList.Add(MkId("Context"));
                argList.Add(MkId(string.Format("MachineType_{0}", machTypeName)));

                if (children.Any(child => child == null))
                    return null;

                IEnumerable<Tuple<string, Node>> inits =
                    children.Select(child => new Tuple<string, Node>(
                        ((Cnst)Compiler.GetArgByIndex((FuncTerm)child.node.Node, 0)).GetStringValue(),
                        Compiler.GetArgByIndex((FuncTerm)child.node.Node, 1)));

                // Remove ghosts if neccessary
                inits = inits.Where(init =>
                    !(compiler.erase && compiler.allMachines[machTypeName].localVariableToVarInfo.ContainsKey(init.Item1) &&
                    compiler.allMachines[machTypeName].localVariableToVarInfo[init.Item1].isGhost));

                foreach (var init in inits)
                {
                    var varName = init.Item1;
                    argList.Add(MkId(string.Format("Var_{0}_{1}", machTypeName, varName)));
                    var actualType = getComputedType(cnodeToPNode[init.Item2]);
                    var varInfo = compiler.allMachines[machTypeName].localVariableToVarInfo[varName];
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

                return new CTranslationInfo(MkFunApp("SmfNew", n.Span, argList.ToArray()));
            }
            else if (funName == PData.Con_Raise.Node.Name)
            {
                // Raise's arguments are apparently always real, so don't need to check for erasure here.
                var args = new AST<Node>[3];
                args[0] = MkId("Context");
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    args[1] = ctxt.consumeExp(it.Current.node);
                    it.MoveNext();
                    if (it.Current.node.Node.NodeKind == NodeKind.Id &&
                        ((Id)it.Current.node.Node).Name == PData.Cnst_Nil.Node.Name)
                    {
                        args[2] = Compiler.AddArgs(CData.App_Cast(n.Span), MkNmdType("PSMF_PACKED_VALUE"), MkAddrOf(MkId("g_SmfNullPayload", n.Span)));
                    }
                    else
                    {
                        var argType = getComputedType(Compiler.GetArgByIndex(ft, 1));
                        var tmpVar = ctxt.getTmpVar(PType.Any, false, false);

                        ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, tmpVar, PType.Any, ctxt.consumeExp(it.Current.node), argType));
                        args[2] = MkAddrOf(ctxt.consumeExp(tmpVar));
                    }
                    // Emit an assert that the payload can be cast to the event's expected type.
                    var eventPayloadType = MkDot(MkIdx(MkArrow(ctxt.driver, "Events"), args[1]), "Type");
                    ctxt.addSideEffect(MkAssert(MkFunApp(getCCanCastName(PType.Any), default(Span), ctxt.driver, args[2], eventPayloadType),
                        n.Span, "Payload not Cast-able to expected event payload on Send"));
                    return new CTranslationInfo(Compiler.AddArgs(CData.App_Seq(), MkFunApp("SmfRaise", n.Span, args),
                        ctxt.emitAllCleanup(Factory.Instance.AddArg(CData.App_Return(n.Span), CData.Cnst_Nil(n.Span)))));
                }
            }
            else if (funName == PData.Con_Send.Node.Name)
            {
                if (shouldErase(n))
                    return new CTranslationInfo(CData.Cnst_Nil(n.Span));

                var args = new AST<Node>[4];
                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    args[0] = ctxt.consumeExp(it.Current.node); // Target ID
                    if (it.Current.node.Node.NodeKind == NodeKind.Id && ((Id)it.Current.node.Node).Name == CData.Cnst_Nil().Node.Name)
                        return new CTranslationInfo(CData.Cnst_Nil(n.Span));
                    it.MoveNext();
                    args[1] = ctxt.consumeExp(it.Current.node); // Event ID
                    it.MoveNext();
                    if (it.Current.node.Node.NodeKind == NodeKind.Id &&
                        ((Id)it.Current.node.Node).Name == PData.Cnst_Nil.Node.Name)
                    {   // No Payload Case
                        args[2] = Compiler.AddArgs(CData.App_Cast(n.Span), MkNmdType("PSMF_PACKED_VALUE"), MkAddrOf(MkId("g_SmfNullPayload", n.Span)));
                    }
                    else
                    {   // We have a payload - upcast it to Any. We always send Any values. (SMF_PACKED_VALUE in C).
                        var argType = getComputedType(Compiler.GetArgByIndex(ft, 2));
                        var tmpVar = ctxt.getTmpVar(PType.Any, false, false);
                        ctxt.addSideEffect(MkAssignOrCast(ctxt, n.Span, tmpVar, PType.Any, ctxt.consumeExp(it.Current.node), argType));
                        args[2] = MkAddrOf(ctxt.consumeExp(tmpVar));
                    }
                    args[3] = MkId("FALSE");

                    // Emit an assert that the payload can be cast to the event's expected type.
                    var eventPayloadType = MkDot(MkIdx(MkArrow(ctxt.driver, "Events"), args[1]), "Type");
                    ctxt.addSideEffect(MkAssert(MkFunApp(getCCanCastName(PType.Any), default(Span), ctxt.driver, args[2], eventPayloadType), n.Span, "Payload not Cast-able to expected event payload on Send"));

                    return new CTranslationInfo(MkFunApp("SmfEnqueueEvent", n.Span, args));
                }
            }
            else if (funName == PData.Con_While.Node.Name)
            {
                if (shouldErase(n))
                    return new CTranslationInfo(CData.Cnst_Nil(n.Span));

                using (var it = children.GetEnumerator())
                {
                    it.MoveNext();
                    var cond = it.Current.node;
                    it.MoveNext();
                    var body = it.Current.node;

                    body = ctxt.emitCSideEffects(body);

                    var loop_start = compiler.getUnique("loop_start");
                    var loop_end = compiler.getUnique("loop_end");
                    var loop = MkSeq(Compiler.AddArgs(CData.App_Lbl(n.Span), Factory.Instance.MkCnst(loop_start),
                        ctxt.emitCSideEffects(MkIf(MkUnop(CData.Cnst_LNot(n.Span), cond),
                            Compiler.AddArgs(CData.App_Goto(n.Span), Factory.Instance.MkCnst(loop_end))))),
                            body,
                            Compiler.AddArgs(CData.App_Goto(n.Span), Factory.Instance.MkCnst(loop_start)),
                            Compiler.AddArgs(CData.App_Lbl(n.Span), Factory.Instance.MkCnst(loop_end), CData.Cnst_Nil(n.Span)));

                    return new CTranslationInfo(loop);
                }
            }
            else if (funName == PData.Con_MachType.Node.Name)
            {
                return new CTranslationInfo(Factory.Instance.ToAST(n));
            }
            else if (funName == PData.Con_Tuple.Node.Name)
            {
                if (shouldErase(n))
                    return new CTranslationInfo(CData.Cnst_Nil(n.Span));

                var type = (PTupleType)getComputedType(ft);

                var tmpVar = ctxt.getTmpVar(type, false);
                var fieldDesc = getFieldDesc(type);

                var sideEffects = fieldDesc.Zip(children, (field, val) => MkAssignOrCast(ctxt, n.Span, MkDot(tmpVar, field.Item3), field.Item1,
                    val.node, getComputedType(cnodeToPNode[val.node.Node])));

                foreach (var sideEff in sideEffects) ctxt.addSideEffect(sideEff);
                return new CTranslationInfo(tmpVar);
            }
            else if (funName == PData.Con_NamedTuple.Node.Name)
            {
                if (shouldErase(n))
                    return new CTranslationInfo(CData.Cnst_Nil(n.Span));

                var type = (PNamedTupleType)getComputedType(ft);
                var tmpVar = ctxt.getTmpVar(type, false);
                var fieldDesc = getFieldDesc(type);

                var sortedRawArgs = children.OrderBy(labeledExp => ((Cnst)Compiler.GetArgByIndex((FuncTerm)labeledExp.node.Node, 0)).GetStringValue()).Select(
                    labeledExp => Factory.Instance.ToAST(Compiler.GetArgByIndex((FuncTerm)labeledExp.node.Node, 1)));

                var sideEffects = fieldDesc.Zip(sortedRawArgs, (field, val) => MkAssignOrCast(ctxt, n.Span, MkDot(tmpVar, field.Item3), field.Item1,
                    val, getComputedType(cnodeToPNode[val.Node])));

                foreach (var sideEff in sideEffects) ctxt.addSideEffect(sideEff);
                return new CTranslationInfo(tmpVar);
            }
            else if (funName == PData.Con_TypeTuple.Node.Name ||
                funName == PData.Con_TypeNamedTuple.Node.Name ||
                funName == PData.Con_TypeField.Node.Name)
            {
                return new CTranslationInfo(CData.Cnst_Nil(n.Span));
            }
            else
            {
                throw new NotImplementedException("Unknown function term name: " + funName);
            }
        }

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
                    ft = Compiler.GetArgByIndex(ft, 1) as FuncTerm;
                }
                while (ft != null);
            }
            else if (funName == PData.Con_NamedExprs.Node.Name)
            {
                do
                {
                    yield return Compiler.AddArgs(Compiler.App_LabeledExpr, Factory.Instance.ToAST(Compiler.GetArgByIndex(ft, 0)), Factory.Instance.ToAST(Compiler.GetArgByIndex(ft, 1))).Node;
                    ft = Compiler.GetArgByIndex(ft, 2) as FuncTerm;
                }
                while (ft != null);
            }
            else if (funName == Compiler.Con_LabeledExpr)
            {
                yield return Compiler.GetArgByIndex(ft, 1);
            }
            else if (funName == PData.Con_Tuple.Node.Name)
            {
                foreach (var a in EntryFun_UnFold(ctxt, Compiler.GetArgByIndex(ft, 0)))
                {
                    yield return a;
                }
            }
            else if (funName == PData.Con_NamedTuple.Node.Name)
            {
                foreach (var a in EntryFun_UnFold(ctxt, Compiler.GetArgByIndex(ft, 0)))
                {
                    yield return a;
                }
            }
            else if (funName == PData.Con_DataOp.Node.Name)
            {
                foreach (var a in EntryFun_UnFold(ctxt, Compiler.GetArgByIndex(ft, 1)))
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
                foreach (var a in EntryFun_UnFold(ctxt, Compiler.GetArgByIndex(ft, 1)))
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
    }
}
