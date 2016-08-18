namespace CParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Formula.API;
    using Microsoft.Formula.API.ASTQueries;
    using Microsoft.Formula.API.Nodes;
    using Microsoft.Formula.Common;

    using System.IO;

    internal static class CRenderer
    {
        private enum OpPlacementKind { Prefix, Infix, PostFix, Other };
        private enum LoopKind { While, Do };
        private enum StorageKind { None, Extern, Static, Auto, Register };
        private enum DataKind { Struct, Union };
        private enum PpIfKind { If, Ifdef, Ifndef };

        private static char[] NonzeroDigits = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        private static char[] LineBreakChars = new char[] { '\n' };
        private static Tuple<Node, PrintData>[] NoChildren = new Tuple<Node, PrintData>[0];
        private static Map<Id, SyntaxInfo> FrmToSyntaxInfo = new Map<Id, SyntaxInfo>(CompareId);

        private static Map<Id, OpInfo> FrmIdToUnOp = new Map<Id, OpInfo>(CompareId);
        private static Map<Id, OpInfo> FrmIdToBinOp = new Map<Id, OpInfo>(CompareId);
        private static Map<Id, OpInfo> FrmIdToTerOp = new Map<Id, OpInfo>(CompareId);

        //// The names of complex statements
        private static Set<Id> FrmStatements = new Set<Id>(CompareId);
        private static Set<Id> FrmDefinitions = new Set<Id>(CompareId);

        static CRenderer()
        {
            // Printing for each function symbol.
            FrmToSyntaxInfo[FormulaNodes.Section_Iden.Node] = new SyntaxInfo(StartSection, EndSection, ValidateSection);
            FrmToSyntaxInfo[FormulaNodes.PpEscape_Iden.Node] = new SyntaxInfo(StartPpEscape, EndPpEscape, ValidatePpEscape);
            FrmToSyntaxInfo[FormulaNodes.PpInclude_Iden.Node] = new SyntaxInfo(StartPpInclude, EndPpInclude, ValidatePpInclude);
            FrmToSyntaxInfo[FormulaNodes.PpDefine_Iden.Node] = new SyntaxInfo(StartPpDefine, EndPpDefine, ValidatePpDefine);
            FrmToSyntaxInfo[FormulaNodes.PpUndef_Iden.Node] = new SyntaxInfo(StartPpUndefine, EndPpUndefine, ValidatePpUndefine);
            FrmToSyntaxInfo[FormulaNodes.PpPragma_Iden.Node] = new SyntaxInfo(StartPpPragma, EndPpPragma, ValidatePpPragma);
            FrmToSyntaxInfo[FormulaNodes.PpITE_Iden.Node] = new SyntaxInfo(StartPpITE, EndPpITE, ValidatePpITE);
            FrmToSyntaxInfo[FormulaNodes.PpElIf_Iden.Node] = new SyntaxInfo(StartPpElIf, EndPpElIf, ValidatePpElIf);

            FrmToSyntaxInfo[FormulaNodes.VarDef_Iden.Node] = new SyntaxInfo(StartVarDef, EndVarDef, ValidateVarDef);
            FrmToSyntaxInfo[FormulaNodes.FunDef_Iden.Node] = new SyntaxInfo(StartFunDef, EndFunDef, ValidateFunDef);
            FrmToSyntaxInfo[FormulaNodes.TypDef_Iden.Node] = new SyntaxInfo(StartTypeDef, EndTypeDef, ValidateTypeDef);
            FrmToSyntaxInfo[FormulaNodes.EnmDef_Iden.Node] = new SyntaxInfo(StartEnumDef, EndEnumDef, ValidateEnumDef);
            FrmToSyntaxInfo[FormulaNodes.DatDef_Iden.Node] = new SyntaxInfo(StartDataDef, EndDataDef, ValidateDataDef);
            FrmToSyntaxInfo[FormulaNodes.Defs_Iden.Node] = new SyntaxInfo(StartDefs, EndDefs, ValidateDefs);
            FrmToSyntaxInfo[FormulaNodes.Elements_Iden.Node] = new SyntaxInfo(StartElements, EndElements, ValidateElements);
            FrmToSyntaxInfo[FormulaNodes.Element_Iden.Node] = new SyntaxInfo(StartElement, EndElement, ValidateElement);

            FrmToSyntaxInfo[FormulaNodes.Fields_Iden.Node] = new SyntaxInfo(StartFields, EndFields, ValidateFields);

            FrmToSyntaxInfo[FormulaNodes.BaseType_Iden.Node] = new SyntaxInfo(StartTypeExpr, EndTypeExpr, ValidateBaseType);
            FrmToSyntaxInfo[FormulaNodes.NmdType_Iden.Node] = new SyntaxInfo(StartTypeExpr, EndTypeExpr, ValidateNamedType);
            FrmToSyntaxInfo[FormulaNodes.PtrType_Iden.Node] = new SyntaxInfo(StartTypeExpr, EndTypeExpr, ValidatePtrType);
            FrmToSyntaxInfo[FormulaNodes.ArrType_Iden.Node] = new SyntaxInfo(StartTypeExpr, EndTypeExpr, ValidateArrType);
            FrmToSyntaxInfo[FormulaNodes.QualType_Iden.Node] = new SyntaxInfo(StartTypeExpr, EndTypeExpr, ValidateQualType);
            FrmToSyntaxInfo[FormulaNodes.FunType_Iden.Node] = new SyntaxInfo(StartTypeExpr, EndTypeExpr, ValidateFunType);
            FrmToSyntaxInfo[FormulaNodes.PrmTypes_Iden.Node] = new SyntaxInfo(StartPrmTypes, EndPrmTypes, ValidatePrmTypes);
            FrmToSyntaxInfo[FormulaNodes.Seq_Iden.Node] = new SyntaxInfo(StartSeq, EndSeq, ValidateSeq);
            FrmToSyntaxInfo[FormulaNodes.Block_Iden.Node] = new SyntaxInfo(StartBlock, EndBlock, ValidateBlock);
            FrmToSyntaxInfo[FormulaNodes.Label_Iden.Node] = new SyntaxInfo(StartLabel, EndLabel, ValidateLabel);
            FrmToSyntaxInfo[FormulaNodes.Goto_Iden.Node] = new SyntaxInfo(StartGoto, EndGoto, ValidateGoto);
            FrmToSyntaxInfo[FormulaNodes.Return_Iden.Node] = new SyntaxInfo(StartReturn, EndReturn, ValidateReturn);
            FrmToSyntaxInfo[FormulaNodes.ITE_Iden.Node] = new SyntaxInfo(StartITE, EndITE, ValidateITE);
            FrmToSyntaxInfo[FormulaNodes.Switch_Iden.Node] = new SyntaxInfo(StartSwitch, EndSwitch, ValidateSwitch);
            FrmToSyntaxInfo[FormulaNodes.StrJmp_Iden.Node] = new SyntaxInfo(StartStrJmp, EndStrJmp, ValidateStrJmp);
            FrmToSyntaxInfo[FormulaNodes.Loop_Iden.Node] = new SyntaxInfo(StartLoop, EndLoop, ValidateLoop);
            FrmToSyntaxInfo[FormulaNodes.For_Iden.Node] = new SyntaxInfo(StartFor, EndFor, ValidateFor);
            FrmToSyntaxInfo[FormulaNodes.Cast_Iden.Node] = new SyntaxInfo(StartCast, EndCast, ValidateCast);
            FrmToSyntaxInfo[FormulaNodes.SizeOf_Iden.Node] = new SyntaxInfo(StartSizeOf, EndSizeOf, ValidateSizeOf);
            FrmToSyntaxInfo[FormulaNodes.Paren_Iden.Node] = new SyntaxInfo(StartParen, EndParen, ValidateParen);
            FrmToSyntaxInfo[FormulaNodes.Cases_Iden.Node] = new SyntaxInfo(StartCases, EndCases, ValidateCases);
            FrmToSyntaxInfo[FormulaNodes.Cases_PpLine.Node] = new SyntaxInfo(StartPpLine, EndPpLine, ValidatePpLine);
            FrmToSyntaxInfo[FormulaNodes.UnApp_Iden.Node] = new SyntaxInfo(StartUnApp, EndUnApp, ValidateUnApp);
            FrmToSyntaxInfo[FormulaNodes.BinApp_Iden.Node] = new SyntaxInfo(StartBinApp, EndBinApp, ValidateBinApp);
            FrmToSyntaxInfo[FormulaNodes.TerApp_Iden.Node] = new SyntaxInfo(StartTerApp, EndTerApp, ValidateTerApp);
            FrmToSyntaxInfo[FormulaNodes.FunApp_Iden.Node] = new SyntaxInfo(StartFunApp, EndFuncApp, ValidateFunApp);
            FrmToSyntaxInfo[FormulaNodes.Init_Iden.Node] = new SyntaxInfo(StartInit, EndInit, ValidateInit);
            FrmToSyntaxInfo[FormulaNodes.Ident_Iden.Node] = new SyntaxInfo(StartIdent, EndIdent, ValidateIdent);
            FrmToSyntaxInfo[FormulaNodes.Args_Iden.Node] = new SyntaxInfo(StartArgs, EndArgs, ValidateArgs);
            FrmToSyntaxInfo[FormulaNodes.IntLit_Iden.Node] = new SyntaxInfo(StartIntLit, EndIntLit, ValidateIntLit);
            FrmToSyntaxInfo[FormulaNodes.StringLit_Iden.Node] = new SyntaxInfo(StartStringLit, EndStringLit, ValidateStringLit);
            FrmToSyntaxInfo[FormulaNodes.RealLit_Iden.Node] = new SyntaxInfo(StartRealLit, EndRealLit, ValidateRealLit);
            FrmToSyntaxInfo[FormulaNodes.Comment_Iden.Node] = new SyntaxInfo(StartComment, EndComment, ValidateComment);

            // Un Ops
            AddOpData(FormulaNodes.Paren_Iden.Node, 1, "()", 0, OpPlacementKind.Other);
            AddOpData(FormulaNodes.IncAfter_Iden.Node, 1, "++", 2, OpPlacementKind.PostFix);
            AddOpData(FormulaNodes.DecAfter_Iden.Node, 1, "--", 2, OpPlacementKind.PostFix);
            AddOpData(FormulaNodes.LNot_Iden.Node, 1, "!", 3, OpPlacementKind.Prefix);
            AddOpData(FormulaNodes.BNot_Iden.Node, 1, "~", 3, OpPlacementKind.Prefix);
            AddOpData(FormulaNodes.Neg_Iden.Node, 1, "-", 3, OpPlacementKind.Prefix);
            AddOpData(FormulaNodes.Pos_Iden.Node, 1, "+", 3, OpPlacementKind.Prefix);
            AddOpData(FormulaNodes.Addr_Iden.Node, 1, "&", 3, OpPlacementKind.Prefix);
            AddOpData(FormulaNodes.Drf_Iden.Node, 1, "*", 3, OpPlacementKind.Prefix);
            AddOpData(FormulaNodes.Inc_Iden.Node, 1, "++", 3, OpPlacementKind.Prefix);
            AddOpData(FormulaNodes.Dec_Iden.Node, 1, "--", 3, OpPlacementKind.Prefix);
            AddOpData(FormulaNodes.SizeOf_Iden.Node, 1, "sizeof", 3, OpPlacementKind.Other);

            // Bin Ops
            AddOpData(FormulaNodes.PtrFieldAccess_Iden.Node, 2, "->", 2, OpPlacementKind.Infix, false);
            AddOpData(FormulaNodes.FieldAccess_Iden.Node, 2, ".", 2, OpPlacementKind.Infix, false);
            AddOpData(FormulaNodes.ArrayAccess_Iden.Node, 2, "[]", 2, OpPlacementKind.Other);
            AddOpData(FormulaNodes.Cast_Iden.Node, 2, "()", 3, OpPlacementKind.Other);
            AddOpData(FormulaNodes.Mul_Iden.Node, 2, "*", 5, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Div_Iden.Node, 2, "/", 5, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Mod_Iden.Node, 2, "%", 5, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Add_Iden.Node, 2, "+", 6, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Sub_Iden.Node, 2, "-", 6, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Right_Iden.Node, 2, ">>", 7, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Left_Iden.Node, 2, "<<", 7, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Gt_Iden.Node, 2, ">", 8, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Lt_Iden.Node, 2, "<", 8, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.GtEq_Iden.Node, 2, ">=", 8, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.LtEq_Iden.Node, 2, "<=", 8, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.EqEq_Iden.Node, 2, "==", 9, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.NEq_Iden.Node, 2, "!=", 9, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.BAnd_Iden.Node, 2, "&", 10, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.BXOr_Iden.Node, 2, "^", 11, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.BOr_Iden.Node, 2, "|", 12, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.LAnd_Iden.Node, 2, "&&", 13, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.LOr_Iden.Node, 2, "||", 14, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Asn_Iden.Node, 2, "=", 16, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Asnadd_Iden.Node, 2, "+=", 16, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Asnsub_Iden.Node, 2, "-=", 16, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Asnmul_Iden.Node, 2, "*=", 16, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Asndiv_Iden.Node, 2, "/=", 16, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Asnand_Iden.Node, 2, "&=", 16, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Asnmod_Iden.Node, 2, "%=", 16, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Asnor_Iden.Node, 2, "|=", 16, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Asnxor_Iden.Node, 2, "^=", 16, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Asnrt_Iden.Node, 2, ">>=", 16, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Asnlft_Iden.Node, 2, "<<=", 16, OpPlacementKind.Infix);
            AddOpData(FormulaNodes.Comma_Iden.Node, 2, ",", 18, OpPlacementKind.Infix);

            // Ter ops
            AddOpData(FormulaNodes.Tcond_Iden.Node, 3, "?:", 15, OpPlacementKind.Other);

            //// The names of statements (excluding expression and nil)
            FrmStatements.Add(FormulaNodes.Seq_Iden.Node);
            FrmStatements.Add(FormulaNodes.Block_Iden.Node);
            FrmStatements.Add(FormulaNodes.Label_Iden.Node);
            FrmStatements.Add(FormulaNodes.Goto_Iden.Node);
            FrmStatements.Add(FormulaNodes.ITE_Iden.Node);
            FrmStatements.Add(FormulaNodes.Switch_Iden.Node);
            FrmStatements.Add(FormulaNodes.Loop_Iden.Node);
            FrmStatements.Add(FormulaNodes.Return_Iden.Node);
            FrmStatements.Add(FormulaNodes.For_Iden.Node);
            FrmStatements.Add(FormulaNodes.StrJmp_Iden.Node);

            //// The names of definitions
            FrmDefinitions.Add(FormulaNodes.TypDef_Iden.Node);
            FrmDefinitions.Add(FormulaNodes.VarDef_Iden.Node);
            FrmDefinitions.Add(FormulaNodes.DatDef_Iden.Node);
            FrmDefinitions.Add(FormulaNodes.EnmDef_Iden.Node);
            FrmDefinitions.Add(FormulaNodes.FunDef_Iden.Node);
        }

        internal static bool Render(AST<Node> root, CTextWriter wr, out List<Flag> renderFlags, System.Threading.CancellationToken cancel = default(System.Threading.CancellationToken))
        {
            Contract.Requires(root != null && root.Node.IsFuncOrAtom);
            Contract.Requires(wr != null);

            var pstack = new Stack<PrintData>();
            var success = new SuccessToken();
            var flags = new List<Flag>();
            root.Compute<PrintData, bool>(
                new PrintData(null, null, 0, false),
                (n, d) =>
                {
                    SyntaxInfo info;
                    FuncTerm f;
                    pstack.Push(d);
                    PrintPreamble(d, wr);
                    if (n.NodeKind != NodeKind.FuncTerm ||
                        !((f = (FuncTerm)n).Function is Id) ||
                        !FrmToSyntaxInfo.TryFindValue((Id)f.Function, out info))
                    {
                        switch (n.NodeKind)
                        {
                            case NodeKind.FuncTerm:
                                d.PrintEnd = EndForgnFunc;
                                return StartForgnFunc((FuncTerm)n, d, wr);
                            case NodeKind.Id:
                                d.PrintEnd = EndId;
                                return StartId((Id)n, d, wr);
                            case NodeKind.Cnst:
                                d.PrintEnd = EndCnst;
                                return StartCnst((Cnst)n, d, wr);
                            case NodeKind.Quote:
                                d.PrintEnd = EndQuote;
                                return StartQuote((Quote)n, d, wr);
                            case NodeKind.QuoteRun:
                                d.PrintEnd = EndQuoteRun;
                                return StartQuoteRun((QuoteRun)n, d, wr);
                            case NodeKind.Compr:
                                d.PrintEnd = EndCompr;
                                return StartCompr((Compr)n, d, wr);
                            case NodeKind.Body:
                                d.PrintEnd = EndBody;
                                return StartBody(n, d, wr);
                            case NodeKind.Find:
                                d.PrintEnd = EndFind;
                                return StartFind(n, d, wr);
                            case NodeKind.RelConstr:
                                d.PrintEnd = EndRelConstr;
                                return StartRelConstr(n, d, wr);
                            default:
                                throw new NotImplementedException();
                        }
                    }

                    if (!info.Validator(f, flags, success))
                    {
                        return NoChildren;
                    }

                    d.PrintEnd = info.PrinterEnd;
                    return info.PrinterStart(f, d, wr);
                },
                (n, v, ch) =>
                {
                    var pd = pstack.Peek();
                    if (pd.PrintEnd != null)
                    {
                        pd.PrintEnd(n, pd, wr);
                    }

                    PrintSuffix(pd, wr);
                    pstack.Pop();
                    return true;
                },
                cancel);

            renderFlags = flags;
            return success.Succeeded;
        }

        private static void PrintPreamble(
                                          PrintData data,
                                          CTextWriter wr)
        {
            if (data.Indentation > 0 && !data.SkipIndent)
            {
                wr.Write(data.GetIndentString());
            }

            if (data.Prefix != null)
            {
                wr.Write(data.Prefix);
            }
        }

        private static void PrintSuffix(PrintData data, CTextWriter wr)
        {
            if (data.Suffix != null)
            {
                wr.Write(data.Suffix);
            }
        }

        private static bool ValidateNotImp(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            throw new NotImplementedException();
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartNotImp(FuncTerm node, PrintData data, CTextWriter wr)
        {
            throw new NotImplementedException();
        }

        private static void EndNotImp(Node node, PrintData data, CTextWriter wr)
        {
            throw new NotImplementedException();
        }

        private static bool ValidateIdent(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 1)
            {
                flags.Add(MkBadArityFlag(node, 1));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartIdent(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg = it.Current;
            }

            Cnst strCnst = null;
            bool isPrintable = true;
            if ((strCnst = arg as Cnst) == null || 
                strCnst.CnstKind != CnstKind.String ||
                !IsCIdentifier(strCnst.GetStringValue()))
            {
                isPrintable = false;
            }
    
            //// This identifier is something we don't understand, so revert to escaped form.
            if (!isPrintable)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                yield return new Tuple<Node, PrintData>(arg, new PrintData(null, null, 0, false));
                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
                data.PrivateSuffix = "`";
            }

            wr.Write(strCnst.GetStringValue());
        }

        private static void EndIdent(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateStringLit(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartStringLit(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            Cnst strCnst = null;
            Id strFormat = null;
            bool isPrintable = true;
            if ((strCnst = arg1 as Cnst) == null || 
                strCnst.CnstKind != CnstKind.String)
            {
                isPrintable = false;
            }
            else if ((strFormat = arg2 as Id) == null ||
                     (strFormat.Name != FormulaNodes.Nil_Iden.Node.Name &&
                      strFormat.Name != FormulaNodes.L_Iden.Node.Name))
            {
                isPrintable = false;
            }

            //// This string lit is something we don't understand, so revert to escaped form.
            if (!isPrintable)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, ", ", 0, false));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, null, 0, false));
                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
                data.PrivateSuffix = "`";
            }

            if (wr.PrintLineDirective && data.Suffix != null && data.Suffix.Contains(";"))
            {
                wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());
            }

            wr.Write("{0}\"{1}\"",
                strFormat.Name == FormulaNodes.L_Iden.Node.Name ? FormulaNodes.L_Iden.Node.Name : string.Empty,
                strCnst.GetStringValue());
        }

        private static void EndStringLit(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateIntLit(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 3)
            {
                flags.Add(MkBadArityFlag(node, 3));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartIntLit(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null, arg3 = null; 
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
                it.MoveNext();
                arg3 = it.Current;
            }

            Cnst intCnst = null;
            Id intFormat = null;
            Id intMod = null;
            bool isPrintable = true;
            if ((intCnst = arg1 as Cnst) == null ||
                intCnst.CnstKind != CnstKind.Numeric ||
                !intCnst.GetNumericValue().IsInteger)
            {
                isPrintable = false;
            }
            else if ((intFormat = arg2 as Id) == null ||
                     (intFormat.Name != FormulaNodes.FormatDec_Iden.Node.Name &&
                      intFormat.Name != FormulaNodes.FormatChar_Iden.Node.Name &&    
                      intFormat.Name != FormulaNodes.FormatHex_Iden.Node.Name &&
                      intFormat.Name != FormulaNodes.FormatOct_Iden.Node.Name))
            {
                isPrintable = false;
            }
            else if ((intMod = arg3 as Id) == null ||
                     (intMod.Name != FormulaNodes.Nil_Iden.Node.Name &&
                      intMod.Name != FormulaNodes.L_Iden.Node.Name &&
                      intMod.Name != FormulaNodes.U_Iden.Node.Name &&
                      intMod.Name != FormulaNodes.UL_Iden.Node.Name))
            {
                isPrintable = false;
            }
            else if (intFormat.Name == FormulaNodes.FormatChar_Iden.Node.Name &&
                     intMod.Name == FormulaNodes.Nil_Iden.Node.Name &&
                     (intCnst.GetNumericValue().CompareTo(Rational.Zero) < 0 ||
                      intCnst.GetNumericValue().CompareTo(new Rational(255)) > 0))
            {
                isPrintable = false;
            }
            else if (intFormat.Name == FormulaNodes.FormatChar_Iden.Node.Name &&
                     intMod.Name == FormulaNodes.L_Iden.Node.Name &&
                     (intCnst.GetNumericValue().CompareTo(Rational.Zero) < 0 ||
                      intCnst.GetNumericValue().CompareTo(new Rational(65535)) > 0))
            {
                isPrintable = false;
            }
            else if (intFormat.Name == FormulaNodes.FormatChar_Iden.Node.Name &&
                     (intMod.Name == FormulaNodes.U_Iden.Node.Name ||
                      intMod.Name == FormulaNodes.UL_Iden.Node.Name))
            {
                isPrintable = false;
            }
          
            //// This string lit is something we don't understand, so revert to escaped form.
            if (!isPrintable)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, ", ", 0, false));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, ", ", 0, false));
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, null, 0, false));
                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
                data.PrivateSuffix = "`";
            }

            if (wr.PrintLineDirective && data.Suffix != null && data.Suffix.Contains(";"))
            {
                wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());
            }

            string modStr;
            if (intMod.Name == FormulaNodes.Nil_Iden.Node.Name)
            {
                modStr = "";
            }
            else if (intMod.Name == FormulaNodes.L_Iden.Node.Name)
            {
                modStr = "L";
            }
            else if (intMod.Name == FormulaNodes.U_Iden.Node.Name)
            {
                modStr = "U";
            }
            else if (intMod.Name == FormulaNodes.UL_Iden.Node.Name)
            {
                modStr = "UL";
            }
            else
            {
                throw new NotImplementedException();
            }

            if (intFormat.Name == FormulaNodes.FormatChar_Iden.Node.Name)
            {
                wr.Write("{0}{1}", modStr, ToCharLiteral((int)intCnst.GetNumericValue().Numerator));
            }
            else if (intFormat.Name == FormulaNodes.FormatDec_Iden.Node.Name) 
            {
                wr.Write("{0}{1}", intCnst.GetNumericValue().Numerator, modStr);
            }
            else if (intFormat.Name == FormulaNodes.FormatHex_Iden.Node.Name)
            {
                if (intCnst.GetNumericValue().Sign < 0)
                {
                    wr.Write("-0x{0}{1}", (-intCnst.GetNumericValue().Numerator).ToString("X"), modStr);
                }
                else
                {
                    wr.Write("0x{0}{1}", intCnst.GetNumericValue().Numerator.ToString("X"), modStr);
                }
            }
            else if (intFormat.Name == FormulaNodes.FormatOct_Iden.Node.Name)
            {
                var val = intCnst.GetNumericValue().Sign < 0 ?
                    System.Numerics.BigInteger.Negate(intCnst.GetNumericValue().Numerator) :
                    intCnst.GetNumericValue().Numerator;

                string octStr = "";
                if (val.Equals(System.Numerics.BigInteger.Zero))
                {
                    octStr = "0";
                }
                else 
                {
                    System.Numerics.BigInteger rem;
                    while (val > 0)
                    {
                        rem = val % 8;
                        octStr = rem.ToString() + octStr; 
                        val = (val - rem) / 8;
                    }
                }

                if (intCnst.GetNumericValue().Sign < 0)
                {
                    wr.Write("-0{0}{1}", octStr, modStr);
                }
                else
                {
                    wr.Write("0{0}{1}", octStr, modStr);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void EndIntLit(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateRealLit(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 4)
            {
                flags.Add(MkBadArityFlag(node, 4));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartRealLit(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null, arg3 = null, arg4 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
                it.MoveNext();
                arg3 = it.Current;
                it.MoveNext();
                arg4 = it.Current;
            }

            Cnst ratCnst = null;
            Cnst nSigFigs = null;
            Id ratFormat = null;
            Id ratMod = null;
            bool isPrintable = true;
            if ((ratCnst = arg1 as Cnst) == null || 
                ratCnst.CnstKind != CnstKind.Numeric)
            {
                isPrintable = false;
            }
            if ((nSigFigs = arg2 as Cnst) == null || 
                nSigFigs.CnstKind != CnstKind.Numeric ||
                !nSigFigs.GetNumericValue().IsInteger ||
                nSigFigs.GetNumericValue().Sign < 1)
            {
                isPrintable = false;
            }
            else if ((ratFormat = arg3 as Id) == null ||
                     (ratFormat.Name != FormulaNodes.FormatDec_Iden.Node.Name &&
                      ratFormat.Name != FormulaNodes.FormatExp_Iden.Node.Name))
            {
                isPrintable = false;
            }
            else if ((ratMod = arg4 as Id) == null ||
                     (ratMod.Name != FormulaNodes.Nil_Iden.Node.Name &&
                      ratMod.Name != FormulaNodes.F_Iden.Node.Name &&
                      ratMod.Name != FormulaNodes.L_Iden.Node.Name))
            {
                isPrintable = false;
            }

            //// This real lit is something we don't understand, so revert to escaped form.
            if (!isPrintable)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, ", ", 0, false));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, ", ", 0, false));
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, ", ", 0, false));
                yield return new Tuple<Node, PrintData>(arg4, new PrintData(null, null, 0, false));
                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
                data.PrivateSuffix = "`";
            }

            if (wr.PrintLineDirective && data.Suffix != null && data.Suffix.Contains(";"))
            {
                wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());
            }

            string modStr;
            if (ratMod.Name == FormulaNodes.Nil_Iden.Node.Name)
            {
                modStr = "";
            }
            else if (ratMod.Name == FormulaNodes.F_Iden.Node.Name)
            {
                modStr = "F";
            }
            else if (ratMod.Name == FormulaNodes.L_Iden.Node.Name)
            {
                modStr = "L";
            }
            else
            {
                throw new NotImplementedException();
            }

            var nSigs = (int)nSigFigs.GetNumericValue().Numerator;
            var rat = ratCnst.GetNumericValue();
            var whole = rat.Numerator / rat.Denominator;
            whole = whole.Sign < 0 ? -whole : whole;
            var wholeStr = whole.ToString();

            //// In this case, the whole part does not fit into the precision,
            //// so we immediately switch to special handling in exponential form.
            if (wholeStr.Length > nSigs)
            {
                if (wholeStr[nSigs] - '0' >= 5)
                {
                    ////In this case, need to round the whole string.
                    whole -= System.Numerics.BigInteger.Parse(wholeStr.Substring(nSigs));
                    whole += System.Numerics.BigInteger.Pow(new System.Numerics.BigInteger(10), wholeStr.Length - nSigs);
                    wholeStr = whole.ToString();
                }

                var mantissaStr = wholeStr.Substring(0, nSigs);
                if (rat.Sign < 0)
                {
                    wr.Write(
                        "-{0}.{1}E{2}{3}", 
                        mantissaStr[0], 
                        nSigs == 1 ? "0" : mantissaStr.Substring(1), 
                        wholeStr.Length - 1, 
                        modStr);
                }
                else
                {
                    wr.Write(
                        "{0}.{1}E{2}{3}",
                        mantissaStr[0],
                        nSigs == 1 ? "0" : mantissaStr.Substring(1),
                        wholeStr.Length - 1,
                        modStr);
                }

                yield break;
            }

            if (ratFormat.Name == FormulaNodes.FormatDec_Iden.Node.Name)
            {
                wr.Write("{0}{1}", rat.ToString(nSigs - wholeStr.Length), modStr);
            }
            else if (ratFormat.Name == FormulaNodes.FormatExp_Iden.Node.Name)
            {
                if (rat.IsZero)
                {
                    wr.Write("0E0{0}", modStr);
                    yield break;
                }

                var ratAbs = rat.Sign < 0 ? new Rational(-rat.Numerator, rat.Denominator) : rat;
                if (ratAbs.Numerator >= ratAbs.Denominator)
                {
                    var ratAbsStr = ratAbs.ToString(nSigs - wholeStr.Length);
                    var decIndex = ratAbsStr.IndexOf('.');
                    if (decIndex < 0)
                    {
                        decIndex = ratAbsStr.Length;
                        ratAbsStr = ratAbsStr.Insert(1, ".");
                        if (ratAbsStr.Length == 2)
                        {
                            ratAbsStr += "0";
                        }
                    }
                    else
                    {
                        ratAbsStr = ratAbsStr.Remove(decIndex, 1).Insert(1, ".");
                    }

                    wr.Write("{0}{1}E{2}{3}", rat.Sign < 0 ? "-" : "", ratAbsStr, decIndex - 1, modStr);
                }
                else
                {
                    //// Need to find the first place where the decimal representation is non-zero
                    //// These zeros represent extra significant digits
                    var tenth = new Rational(System.Numerics.BigInteger.One, new System.Numerics.BigInteger(10));
                    var place = tenth;
                    while (ratAbs.CompareTo(place) < 0)
                    {
                        nSigs++;
                        place = place * tenth;
                    }

                    var ratAbsStr = ratAbs.ToString(nSigs);
                    var sigDigit = ratAbsStr.IndexOfAny(NonzeroDigits);
                    ratAbsStr = ratAbsStr.Insert(sigDigit + 1, ".").Substring(sigDigit);
                    if (ratAbsStr[ratAbsStr.Length - 1] == '.')
                    {
                        ratAbsStr += "0";
                    }

                    wr.Write("{0}{1}E-{2}{3}", rat.Sign < 0 ? "-" : "", ratAbsStr, sigDigit - 1, modStr);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void EndRealLit(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }


        private static bool ValidateUnApp(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartUnApp(FuncTerm node, PrintData data, CTextWriter wr)
        {
            OpInfo myOp;

            //// This un op is something we don't understand, so revert to escaped form.
            if (!TryGetOpInfo(node, out myOp, FrmIdToUnOp))
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                var i = 0;
                foreach (var m in node.Args)
                {
                    yield return new Tuple<Node, PrintData>(
                        m,
                        new PrintData(null, i++ < node.Args.Count - 1 ? ", " : null, 0, false));
                }

                yield break;
            }

            Node arg1 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                it.MoveNext();
                arg1 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
                data.PrivateSuffix = "`";
            }

            if (wr.PrintLineDirective && data.Suffix != null && data.Suffix.Contains(";"))
            {
                wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());
            }

            bool isArgOp;
            OpInfo argOp;
            isArgOp = TryGetOpInfo(arg1, out argOp);
            var needsParens = isArgOp && argOp.Precedence > myOp.Precedence;
            if (myOp.Placement == OpPlacementKind.Prefix)
            {
                if (needsParens)
                {
                    yield return new Tuple<Node, PrintData>(arg1, new PrintData(myOp.PrintedForm + "(", ")", 0, true));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(arg1, new PrintData(myOp.PrintedForm, null, 0, true));
                }
            }
            else if (myOp.Placement == OpPlacementKind.PostFix)
            {
                if (needsParens)
                {
                    yield return new Tuple<Node, PrintData>(arg1, new PrintData("(", ")" + myOp.PrintedForm, 0, true));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, myOp.PrintedForm, 0, true));
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void EndUnApp(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateSizeOf(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 1)
            {
                flags.Add(MkBadArityFlag(node, 1));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartSizeOf(FuncTerm node, PrintData data, CTextWriter wr)
        {
            var myOp = FrmIdToUnOp[FormulaNodes.SizeOf_Iden.Node];
            Node arg1 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
                data.PrivateSuffix = "`";
            }

            if (wr.PrintLineDirective && data.Suffix != null && data.Suffix.Contains(";"))
            {
                wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());
            }

            yield return new Tuple<Node, PrintData>(arg1, new PrintData(myOp.PrintedForm + "(", ")", 0, true));
        }

        private static void EndSizeOf(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateCast(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartCast(FuncTerm node, PrintData data, CTextWriter wr)
        {
            var myOp = FrmIdToUnOp[FormulaNodes.SizeOf_Iden.Node];
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
                data.PrivateSuffix = "`";
            }

            if (wr.PrintLineDirective && data.Suffix != null && data.Suffix.Contains(";"))
            {
                wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());
            }

            yield return new Tuple<Node, PrintData>(arg1, new PrintData("(", ")", 0, true));
            yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, null, 0, true));
        }

        private static void EndCast(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateBaseType(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 1)
            {
                flags.Add(MkBadArityFlag(node, 1));
                success.Failed();
                return false;
            }

            return true;
        }

        private static bool ValidatePtrType(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 1)
            {
                flags.Add(MkBadArityFlag(node, 1));
                success.Failed();
                return false;
            }

            return true;
        }

        private static bool ValidateNamedType(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static bool ValidateArrType(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static bool ValidateQualType(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static bool ValidateFunType(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartTypeExpr(FuncTerm node, PrintData data, CTextWriter wr)
        {
            string expr;
            bool canRender = CTypeRenderer.Render(node, out expr, default(System.Threading.CancellationToken));

            //// This type expr is something we don't understand, so revert to escaped form.
            if (!canRender)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                var i = 0;
                foreach (var m in node.Args)
                {
                    yield return new Tuple<Node, PrintData>(
                        m,
                        new PrintData(null, i++ < node.Args.Count - 1 ? ", " : null, 0, false));
                }

                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
                data.PrivateSuffix = "`";
            }

            wr.Write(expr);
        }

        private static void EndTypeExpr(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidatePrmTypes(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartPrmTypes(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            //// PrmTypes will not be printed by themselves, since they must be in the context of a
            //// a function type.
            if (!data.InQuotation)
            {
                wr.Write("{0}(", ((Id)node.Function).Name);
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, ", ", 0, false));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, ")", 0, false));
                yield break;
            }

            yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, null, 0, true));
            if (!IsNil(arg2))
            {
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(", ", null, 0, true));
            }
        }

        private static void EndPrmTypes(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateVarDef(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 4)
            {
                flags.Add(MkBadArityFlag(node, 4));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartVarDef(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null, arg3 = null, arg4 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
                it.MoveNext();
                arg3 = it.Current;
                it.MoveNext();
                arg4 = it.Current;
            }

            string typeExpr = null;
            string varName;
            StorageKind storKind;
            var isPrintable = IsStorageKind(arg1, out storKind) &&
                              !IsFunType(arg2) &&
                              IsStringCnst(arg3, out varName) &&
                              IsCIdentifier(varName) &&
                              CTypeRenderer.Render(arg2, varName, out typeExpr, default(System.Threading.CancellationToken));

            if (!isPrintable)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                var i = 0;
                foreach (var m in node.Args)
                {
                    yield return new Tuple<Node, PrintData>(
                        m,
                        new PrintData(null, i++ < node.Args.Count - 1 ? ", " : null, 0, false));
                }

                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            wr.Write(GetStorageString(storKind));
            wr.Write(typeExpr);
            if (IsNil(arg4))
            {
                wr.Write(";\n");
            }
            else
            {
                yield return new Tuple<Node, PrintData>(arg4, new PrintData(" = ", ";\n", 0, true));
            }
        }

        private static void EndVarDef(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateTypeDef(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartTypeDef(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            string typeExpr = null;
            string typeName;
            var isPrintable = IsStringCnst(arg2, out typeName) &&
                              IsCIdentifier(typeName) &&
                              CTypeRenderer.Render(arg1, typeName, out typeExpr, default(System.Threading.CancellationToken));

            if (!isPrintable)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                var i = 0;
                foreach (var m in node.Args)
                {
                    yield return new Tuple<Node, PrintData>(
                        m,
                        new PrintData(null, i++ < node.Args.Count - 1 ? ", " : null, 0, false));
                }

                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            wr.Write("typedef {0};\n", typeExpr);
        }

        private static void EndTypeDef(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateEnumDef(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 3)
            {
                flags.Add(MkBadArityFlag(node, 3));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartEnumDef(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null, arg3 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
                it.MoveNext();
                arg3 = it.Current;
            }

            StorageKind storeKind;
            string enumName = null;
            var isPrintable = IsStorageKind(arg1, out storeKind) &&
                              IsStringCnst(arg2, out enumName) &&
                              IsCIdentifier(enumName);
                              
            if (!isPrintable)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                var i = 0;
                foreach (var m in node.Args)
                {
                    yield return new Tuple<Node, PrintData>(
                        m,
                        new PrintData(null, i++ < node.Args.Count - 1 ? ", " : null, 0, false));
                }

                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            if (IsNil(arg3))
            {
                wr.Write("{0}enum {1}\n{2}{{\n{2}}};\n\n", GetStorageString(storeKind), enumName, data.GetIndentString());
            }
            else if (IsUnknown(arg3))
            {
                wr.Write("{0}enum {1};\n\n", GetStorageString(storeKind), enumName);
            }
            else
            {
                wr.Write("{0}enum {1}\n{2}{{\n", GetStorageString(storeKind), enumName, data.GetIndentString());
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, string.Format("{0}}};\n\n", data.GetIndentString()), data.Indentation + 1, true));
            }
        }

        private static void EndEnumDef(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateElements(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static bool ValidateElement(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartElements(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            var isPrintable = IsElement(arg1);

            if (!isPrintable || !data.InQuotation)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                var i = 0;
                foreach (var m in node.Args)
                {
                    yield return new Tuple<Node, PrintData>(
                        m,
                        new PrintData(null, i++ < node.Args.Count - 1 ? ", " : null, 0, false));
                }

                yield break;
            }

            yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, IsNil(arg2) ? "\n" : ",\n", 0, true));
            if (!IsNil(arg2))
            {
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, null, data.Indentation, true));
            }
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartElement(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            string elementName = null;
            var isPrintable = IsStringCnst(arg2, out elementName) &&
                              IsCIdentifier(elementName);

            if (!isPrintable || !data.InQuotation)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                var i = 0;
                foreach (var m in node.Args)
                {
                    yield return new Tuple<Node, PrintData>(
                        m,
                        new PrintData(null, i++ < node.Args.Count - 1 ? ", " : null, 0, false));
                }

                yield break;
            }

            if (IsNil(arg1))
            {
                wr.Write(elementName);
            }
            else
            {
                wr.Write("{0} = ", elementName);
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, "", 0, true));
            }
        }

        private static void EndElements(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static void EndElement(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateDataDef(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 4)
            {
                flags.Add(MkBadArityFlag(node, 4));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartDataDef(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null, arg3 = null, arg4 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
                it.MoveNext();
                arg3 = it.Current;
                it.MoveNext();
                arg4 = it.Current;
            }

            StorageKind storeKind;
            DataKind dataKind = DataKind.Struct;
            string dataName = null;
            var isPrintable = IsStorageKind(arg1, out storeKind) &&
                              IsDataKind(arg2, out dataKind) &&
                              IsStringCnst(arg3, out dataName) &&
                              IsCIdentifier(dataName);

            if (!isPrintable)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                var i = 0;
                foreach (var m in node.Args)
                {
                    yield return new Tuple<Node, PrintData>(
                        m,
                        new PrintData(null, i++ < node.Args.Count - 1 ? ", " : null, 0, false));
                }

                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            if (IsNil(arg4))
            {
                wr.Write("{0}{1}{2}\n{3}{{\n{3}}};\n\n", GetStorageString(storeKind), GetDataString(dataKind), dataName, data.GetIndentString());
            }
            else if (IsUnknown(arg4))
            {
                wr.Write("{0}{1}{2};\n\n", GetStorageString(storeKind), GetDataString(dataKind), dataName, data.GetIndentString());
            }
            else
            {
                wr.Write("{0}{1}{2}\n{3}{{\n", GetStorageString(storeKind), GetDataString(dataKind), dataName, data.GetIndentString());
                yield return new Tuple<Node, PrintData>(arg4, new PrintData(null, string.Format("{0}}};\n\n", data.GetIndentString()), data.Indentation + 1, true));
            }
        }

        private static void EndDataDef(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateFields(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 4)
            {
                flags.Add(MkBadArityFlag(node, 4));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartFields(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null, arg3 = null, arg4 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
                it.MoveNext();
                arg3 = it.Current;
                it.MoveNext();
                arg4 = it.Current;
            }

            string typeExpr = null;
            string varName;
            var isPrintable = IsStringCnst(arg2, out varName) &&
                              IsCIdentifier(varName) &&
                              CTypeRenderer.Render(arg1, varName, out typeExpr, default(System.Threading.CancellationToken));

            if (!isPrintable)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                var i = 0;
                foreach (var m in node.Args)
                {
                    yield return new Tuple<Node, PrintData>(
                        m,
                        new PrintData(null, i++ < node.Args.Count - 1 ? ", " : null, 0, false));
                }

                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            if (IsNil(arg3))
            {
                wr.Write("{0};\n", typeExpr);
            }
            else
            {
                wr.Write("{0}: ", typeExpr);
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, ";\n", 0, true));
            }

            if (!IsNil(arg4))
            {
                yield return new Tuple<Node, PrintData>(arg4, new PrintData(null, null, data.Indentation, true));
            }
        }

        private static void EndFields(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateFunDef(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 5)
            {
                flags.Add(MkBadArityFlag(node, 5));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartFunDef(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null, arg3 = null, arg4 = null, arg5 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
                it.MoveNext();
                arg3 = it.Current;
                it.MoveNext();
                arg4 = it.Current;
                it.MoveNext();
                arg5 = it.Current;
            }

            string funName = string.Empty;
            StorageKind storKind;
            Node prmTypes = null, retType = null;
            var isPrintable = IsStorageKind(arg1, out storKind) &&
                              GetFunTypeData(arg2, out retType, out prmTypes) &&
                              IsStringCnst(arg3, out funName) &&
                              IsCIdentifier(funName);

            string sigString = string.Empty;
            string sigParamsStr = funName + "(";
            if (isPrintable)
            {
                bool isPair, isEllipse, isFirst = true;
                StorageKind prmKind;
                string name, paramStr;

                Node prms = arg4;
                Node type, prmTail, prmTypesTail;
                while ((isPair = GetFunParamInfo(prmTypes, prms, out prmKind, out type, out name, out isEllipse, out prmTypesTail, out prmTail)))
                {
                    if (!isFirst)
                    {
                        sigParamsStr += ", ";
                    }

                    if (type != null)
                    {
                        if (!CTypeRenderer.Render(type, name, out paramStr, default(System.Threading.CancellationToken)))
                        {
                            isPair = false;
                            break;
                        }
                        else
                        {
                            sigParamsStr += GetStorageString(prmKind) + paramStr;
                        }
                    }

                    if (prmTypesTail == null)
                    {
                        Contract.Assert(prmTail == null);
                        if (isEllipse)
                        {
                            sigParamsStr += ", ...";
                        }
                        else if (isFirst && type == null)
                        {
                            sigParamsStr += "void";
                        }

                        sigParamsStr += ")";
                        break;
                    }
                    else
                    {
                        prmTypes = prmTypesTail;
                        prms = prmTail;
                    }

                    isFirst = false;
                }


                if (!isPair ||
                    !CTypeRenderer.Render(retType, sigParamsStr, out sigString, default(System.Threading.CancellationToken)))
                {
                    isPrintable = false;
                }
                else
                {
                    sigString = GetStorageString(storKind) + sigString;
                }
            }

            if (!isPrintable)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                var i = 0;
                foreach (var m in node.Args)
                {
                    yield return new Tuple<Node, PrintData>(
                        m,
                        new PrintData(null, i++ < node.Args.Count - 1 ? ", " : null, 0, false));
                }

                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            bool needsBlock;
            if (IsNilStatement(arg5, out needsBlock))
            {
                wr.Write("{0}\n{1}{{\n{1}}}\n\n", sigString, data.GetIndentString());
            }
            else if (IsUnknown(arg5))
            {
                wr.Write("{0};\n\n", sigString);
            }
            else if (IsBlock(arg5))
            {
                wr.Write("{0}\n", sigString);
                yield return new Tuple<Node, PrintData>(arg5, new PrintData(null, "\n", data.Indentation, true));
            }
            else if (IsStatement(arg5))
            {
                wr.Write("{0}\n{1}{{\n", sigString, data.GetIndentString());
                yield return new Tuple<Node, PrintData>(arg5, new PrintData(null, string.Format("{0}}}\n\n", data.GetIndentString()), data.Indentation + 1, true));
            }
            else
            {
                wr.Write("{0}\n{1}{{\n", sigString, data.GetIndentString());
                yield return new Tuple<Node, PrintData>(arg5, new PrintData(null, string.Format(";\n{0}}}\n\n", data.GetIndentString()), data.Indentation + 1, true));
            }
        }

        private static void EndFunDef(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateParen(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 1)
            {
                flags.Add(MkBadArityFlag(node, 1));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartParen(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
                data.PrivateSuffix = "`";
            }

            if (wr.PrintLineDirective && data.Suffix != null && data.Suffix.Contains(";"))
            {
                wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());
            }

            yield return new Tuple<Node, PrintData>(arg1, new PrintData("(", ")", 0, true));
        }

        private static void EndParen(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateBinApp(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 3)
            {
                flags.Add(MkBadArityFlag(node, 3));
                success.Failed();
                return false;
            }
            
            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartBinApp(FuncTerm node, PrintData data, CTextWriter wr)
        {
            OpInfo myOp;

            //// This bin op is something we don't understand, so revert to escaped form.
            if (!TryGetOpInfo(node, out myOp, FrmIdToBinOp))
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                var i = 0;
                foreach (var m in node.Args)
                {
                    yield return new Tuple<Node, PrintData>(
                        m,
                        new PrintData(null, i++ < node.Args.Count - 1 ? ", " : null, 0, false));
                }

                yield break;
            }

            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
                data.PrivateSuffix = "`";
            }

            if (wr.PrintLineDirective && data.Suffix != null && data.Suffix.Contains(";"))
            {
                wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());
            }

            bool isArgOp;
            OpInfo argOp;

            isArgOp = TryGetOpInfo(arg1, out argOp);
            if (isArgOp && argOp.Precedence > myOp.Precedence)
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData("(", ")", 0, true));
            }
            else
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, null, 0, true));
            }

            //// Special handling for array access
            if (myOp.Name == FormulaNodes.ArrayAccess_Iden.Node)
            {
                yield return new Tuple<Node, PrintData>(arg2, new PrintData("[", "]", 0, true));
                yield break;
            }

            isArgOp = TryGetOpInfo(arg2, out argOp);
            if (isArgOp && argOp.Precedence > myOp.Precedence)
            {
                if (myOp.IsStandardSpacing)
                {
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(" " + myOp.PrintedForm + " (", ")", 0, true));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(myOp.PrintedForm + "(", ")", 0, true));
                }

            }
            else
            {
                if (myOp.IsStandardSpacing)
                {
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(" " + myOp.PrintedForm + " ", null, 0, true));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(myOp.PrintedForm, null, 0, true));
                }
            }
        }

        private static void EndBinApp(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateTerApp(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 4)
            {
                flags.Add(MkBadArityFlag(node, 4));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartTerApp(FuncTerm node, PrintData data, CTextWriter wr)
        {
            OpInfo myOp;

            //// This bin op is something we don't understand, so revert to escaped form.
            if (!TryGetOpInfo(node, out myOp, FrmIdToTerOp))
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                var i = 0;
                foreach (var m in node.Args)
                {
                    yield return new Tuple<Node, PrintData>(
                        m,
                        new PrintData(null, i++ < node.Args.Count - 1 ? ", " : null, 0, false));
                }

                yield break;
            }

            Node arg1 = null, arg2 = null, arg3 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
                it.MoveNext();
                arg3 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
                data.PrivateSuffix = "`";
            }

            if (myOp.Name != FormulaNodes.Tcond_Iden.Node)
            {
                throw new NotImplementedException();
            }

            if (wr.PrintLineDirective && data.Suffix != null && data.Suffix.Contains(";"))
            {
                wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());
            }

            bool isArgOp;
            OpInfo argOp;

            isArgOp = TryGetOpInfo(arg1, out argOp);
            if (isArgOp && argOp.Precedence >= myOp.Precedence)
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData("(", ") ? ", 0, true));
            }
            else
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, " ? ", 0, true));
            }

            isArgOp = TryGetOpInfo(arg2, out argOp);
            if (isArgOp && argOp.Precedence >= myOp.Precedence)
            {
                yield return new Tuple<Node, PrintData>(arg2, new PrintData("(", ") : ", 0, true));
            }
            else
            {
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, " : ", 0, true));
            }

            isArgOp = TryGetOpInfo(arg3, out argOp);
            if (isArgOp && argOp.Precedence >= myOp.Precedence)
            {
                yield return new Tuple<Node, PrintData>(arg3, new PrintData("(", ")", 0, true));
            }
            else
            {
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, null, 0, true));
            }
        }

        private static void EndTerApp(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateFunApp(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartFunApp(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
                data.PrivateSuffix = "`";
            }

            if (wr.PrintLineDirective && data.Suffix != null && data.Suffix.Contains(";"))
            {
                wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());
            }

            bool isArgOp;
            OpInfo argOp;
            isArgOp = TryGetOpInfo(arg1, out argOp);
            if (isArgOp && argOp.Precedence > 1)
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData("(", ")", 0, true));
            }
            else
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, null, 0, true));
            }

            if (IsNil(arg2))
            {
                data.PrivateSuffix = "()" + data.PrivateSuffix;
            }
            else
            {
                yield return new Tuple<Node, PrintData>(arg2, new PrintData("(", ")", 0, true));
            }
        }

        private static void EndFuncApp(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateArgs(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartArgs(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            //// Args will not be printed by themselves, since they must be in the context of a
            //// a function.
            if (!data.InQuotation)
            {
               wr.Write("{0}(", ((Id)node.Function).Name);
               yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, ", ", 0, false));
               yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, ")", 0, false));
               yield break;
            }

            if (data.Indentation == 0)
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, null, 0, true));
                if (!IsNil(arg2))
                {
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(", ", null, 0, true));
                }
            }
            else
            {
                if (!IsNil(arg2))
                {
                    yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, ",\n", data.Indentation, true));
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, null, data.Indentation, true));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, null, data.Indentation, true));
                }
            }
        }

        private static void EndArgs(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateDefs(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartDefs(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            //// Args will not be printed by themselves, since they must be in the context of
            //// something else.
            if (!data.InQuotation || !IsDefinition(arg1))
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                } 
                
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, ", ", 0, false));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, ")", 0, false));
                yield break;
            }

            yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, null, data.Indentation, true));
            if (!IsNil(arg2))
            {
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, null, data.Indentation, true, true));
            }
        }

        private static void EndDefs(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateInit(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 1)
            {
                flags.Add(MkBadArityFlag(node, 1));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartInit(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
            }

            //// Init will not be printed by themselves, since they must be in the context of a
            //// declaration.
            if (!data.InQuotation)
            {
                wr.Write("{0}(", ((Id)node.Function).Name);
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, ")", 0, false));
                yield break;
            }

            //// TODO: Because init may not see the indentation depth this doesn't generalize.
            //// Should also track the deepest indentation encountered on this path and use it here.
            wr.Write(string.Format("\n{0}{{\n", PrintData.GetIndentString(data.Indentation + 1)));
            var suffix = string.Format("\n{0}}}", PrintData.GetIndentString(data.Indentation + 1));
            yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, suffix, data.Indentation + 1, true));
        }

        private static void EndInit(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateITE(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 3)
            {
                flags.Add(MkBadArityFlag(node, 3));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartITE(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null, arg3 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
                it.MoveNext();
                arg3 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());

            var ifPrefix = "if (";
            bool needsBlock;
            if (IsNilStatement(arg2, out needsBlock))
            {
                var indent = data.GetIndentString();
                var block = string.Format(")\n{0}{{\n{0}}}\n", indent);
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(ifPrefix, block, 0, true));
            }
            else if (IsBlock(arg2))
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(ifPrefix, ")\n", 0, true));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, null, data.Indentation, true));
            }            
            else if (IsStatement(arg2))
            {
                var suffix1 = string.Format(")\n{0}{{\n", data.GetIndentString());
                var suffix2 = string.Format("{0}}}\n", data.GetIndentString());
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(ifPrefix, suffix1, 0, true));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, suffix2, data.Indentation + 1, true));
            }
            else
            {
                var suffix1 = string.Format(")\n{0}{{\n", data.GetIndentString());
                var suffix2 = string.Format(";\n{0}}}\n", data.GetIndentString());
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(ifPrefix, suffix1, 0, true));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, suffix2, data.Indentation + 1, true));
            }

            if (IsNilStatement(arg3, out needsBlock))
            {
                yield break;
            }
            else if (IsBlock(arg3))
            {
                var prefix = string.Format("else\n{0}", data.GetIndentString());
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(prefix, null, data.Indentation, true));
            }
            else if (IsITE(arg3))
            {
                yield return new Tuple<Node, PrintData>(arg3, new PrintData("else ", null, data.Indentation, true));
            }
            else if (IsStatement(arg3))
            {
                var prefix = string.Format("{0}else\n{0}{{\n{1}", data.GetIndentString(), PrintData.GetIndentString(data.Indentation + 1));
                var suffix = string.Format("{0}}}\n", data.GetIndentString());
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(prefix, suffix, data.Indentation + 1, true, true));
            }
            else
            {
                var prefix = string.Format("{0}else\n{0}{{\n{1}", data.GetIndentString(), PrintData.GetIndentString(data.Indentation + 1));
                var suffix = string.Format(";\n{0}}}\n", data.GetIndentString());
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(prefix, suffix, data.Indentation + 1, true, true));
            }
        }

        private static void EndITE(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidatePpITE(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 4)
            {
                flags.Add(MkBadArityFlag(node, 4));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartPpITE(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null, arg3 = null, arg4 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
                it.MoveNext();
                arg3 = it.Current;
                it.MoveNext();
                arg4 = it.Current;
            }

            PpIfKind kind;
            if (!IsPpIfKind(arg1, out kind))
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                var i = 0;
                foreach (var m in node.Args)
                {
                    yield return new Tuple<Node, PrintData>(
                        m,
                        new PrintData(null, i++ < node.Args.Count - 1 ? ", " : null, 0, false));
                }

                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            bool isNilT = IsNil(arg3);
            bool isNilF = IsNil(arg4);          
            if (isNilT && isNilF)
            {
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(GetPpIfString(kind), string.Format("\n{0}#endif\n", data.GetIndentString()), 0, true));
                yield break;
            }

            yield return new Tuple<Node, PrintData>(arg2, new PrintData(GetPpIfString(kind), "\n", 0, true));

            string suffix, prefix;
            if (!isNilT)
            {
                suffix = isNilF ? string.Format("{0}#endif\n", data.GetIndentString()) : null;
                if (IsCmp(arg3) || IsStatement(arg3))
                {
                    yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, suffix, data.Indentation + 1, true));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, ";\n" + suffix, data.Indentation + 1, true));
                }
            }

            if (IsPpElIf(arg4))
            {
                yield return new Tuple<Node, PrintData>(arg4, new PrintData(null, null, data.Indentation, true));
            }
            else if (!isNilF)
            {
                prefix = string.Format("{0}#else\n{1}", data.GetIndentString(), PrintData.GetIndentString(data.Indentation + 1));
                suffix = string.Format("{0}{1}#endif\n", IsCmp(arg4) || IsStatement(arg4) ? null : ";\n", data.GetIndentString());
                yield return new Tuple<Node, PrintData>(arg4, new PrintData(prefix, suffix, data.Indentation + 1, true, true));
            }
        }

        private static void EndPpITE(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidatePpElIf(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 3)
            {
                flags.Add(MkBadArityFlag(node, 3));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartPpElIf(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null, arg3 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
                it.MoveNext();
                arg3 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            bool isNilT = IsNil(arg2);
            bool isNilF = IsNil(arg3);
            if (isNilT && isNilF)
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData("#elif ", string.Format("\n{0}#endif\n", data.GetIndentString()), 0, true));
                yield break;
            }

            yield return new Tuple<Node, PrintData>(arg1, new PrintData("#elif ", "\n", 0, true));

            string suffix, prefix;
            if (!isNilT)
            {
                suffix = isNilF ? string.Format("{0}#endif\n", data.GetIndentString()) : null;
                if (IsCmp(arg2) || IsStatement(arg2))
                {
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, suffix, data.Indentation + 1, true));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, ";\n" + suffix, data.Indentation + 1, true));
                }
            }

            if (IsPpElIf(arg3))
            {
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, null, data.Indentation, true));
            }
            else if (!isNilF)
            {
                prefix = string.Format("{0}#else\n{1}", data.GetIndentString(), PrintData.GetIndentString(data.Indentation + 1));
                suffix = string.Format("{0}{1}#endif\n", IsCmp(arg3) || IsStatement(arg3) ? null : ";\n", data.GetIndentString());
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(prefix, suffix, data.Indentation + 1, true, true));
            }

            /*
            if (!isNilT)
            {
                if (isNilF)
                {
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, string.Format("{0}#endif\n", data.GetIndentString()), data.Indentation, true));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, null, data.Indentation, true));
                }
            }

            if (IsPpElIf(arg3))
            {
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, null, data.Indentation, true));
            }
            else if (!isNilF)
            {
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(string.Format("#else\n{0}\n", data.GetIndentString()), string.Format("{0}#endif\n", data.GetIndentString()), data.Indentation, true));
            }*/
        }

        private static void EndPpElIf(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidatePpLine(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartPpLine(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            data.Suffix = data.Suffix.Replace(";", "");
            if (arg2 is Cnst)
            {
                wr.Write("#line {0} \"{1}\"", ((Cnst)arg1).GetNumericValue().Numerator, MkSafeCLiteral(((Cnst)arg2).GetStringValue()));
            }
            else
            {
                wr.Write("#line {0}", ((Cnst)arg1).GetNumericValue().Numerator);
            }
            yield break;
        }

        private static string MkSafeCLiteral(string value)
        {
            string literal = value.Replace("\\", "\\\\").Replace("\n","\\n").Replace("\r","\\r").Replace("\"", "\\\"");
            return literal;
        }

        private static void EndPpLine(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateLoop(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 3)
            {
                flags.Add(MkBadArityFlag(node, 3));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartLoop(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null, arg3 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
                it.MoveNext();
                arg3 = it.Current;
            }

            LoopKind kind;
            if (!TryGetLoopKind(arg1, out kind))
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, ", ", 0, false));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, ", ", 0, false));
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, null, 0, false));
                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());

            bool needsBlock;
            if (kind == LoopKind.While)
            {
                if (IsNilStatement(arg3, out needsBlock))
                {
                    var indent = data.GetIndentString();
                    var block = string.Format(")\n{0}{{\n{0}}}\n", indent);
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData("while (", block, 0, true));
                }
                else if (IsBlock(arg3))
                {
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData("while (", ")\n", 0, true));
                    yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, null, data.Indentation, true));
                }
                else if (needsBlock)
                {
                    var suffix1 = string.Format(")\n{0}{{\n", data.GetIndentString());
                    var suffix2 = string.Format("{0}}}\n", data.GetIndentString());
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData("while (", suffix1, 0, true));
                    yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, suffix2, data.Indentation + 1, true));
                }
                else if (IsStatement(arg3))
                {
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData("while (", ")\n", 0, true));
                    yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, null, data.Indentation + 1, true));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData("while (", ")\n", 0, true));
                    yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, ";\n", data.Indentation + 1, true));
                }
            }
            else if (kind == LoopKind.Do)
            {
                var whilePrefix = string.Format("{0}while (", data.GetIndentString());
                if (IsNilStatement(arg3, out needsBlock))
                {
                    var indent = data.GetIndentString();
                    var block = string.Format("do\n{0}{{\n{0}}}\n{0}while(", indent);
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(block, ");\n", 0, true));
                }
                else if (IsBlock(arg3))
                {
                    var prefix = string.Format("do\n{0}", data.GetIndentString());
                    yield return new Tuple<Node, PrintData>(arg3, new PrintData(prefix, null, data.Indentation, true));
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(whilePrefix, ");\n", 0, true));
                }
                else if (needsBlock)
                {
                    var prefix = string.Format("do\n{0}{{\n{1}", data.GetIndentString(), PrintData.GetIndentString(data.Indentation + 1));
                    var suffix = string.Format("{0}}}\n", data.GetIndentString());
                    yield return new Tuple<Node, PrintData>(arg3, new PrintData(prefix, suffix, data.Indentation + 1, true, true));
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(whilePrefix, ");\n", 0, true));
                }
                else if (IsStatement(arg3))
                {
                    var prefix = string.Format("do\n{0}", PrintData.GetIndentString(data.Indentation + 1));
                    yield return new Tuple<Node, PrintData>(arg3, new PrintData(prefix, null, data.Indentation + 1, true, true));
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(whilePrefix, ");\n", 0, true));
                }
                else
                {
                    var prefix = string.Format("do\n{0}", PrintData.GetIndentString(data.Indentation + 1));


                    yield return new Tuple<Node, PrintData>(arg3, new PrintData(prefix, ";", data.Indentation + 1, true, true));
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(whilePrefix, ");\n", 0, true));
                }
            }
            else
            {
                throw new NotImplementedException();
            }         
        }

        private static void EndLoop(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateSwitch(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartSwitch(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());

            if (IsNil(arg2))
            {
                var suffix = string.Format(")\n{0}{{\n{0}}}\n", data.GetIndentString());
                yield return new Tuple<Node, PrintData>(arg1, new PrintData("switch (", suffix, 0, true));
            }
            else
            {
                var suffix1 = string.Format(")\n{0}{{\n", data.GetIndentString());
                var suffix2 = string.Format("{0}}}\n", data.GetIndentString());
                yield return new Tuple<Node, PrintData>(arg1, new PrintData("switch (", suffix1, 0, true));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, suffix2, data.Indentation, true));
            }
        }

        private static void EndSwitch(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateCases(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 3)
            {
                flags.Add(MkBadArityFlag(node, 3));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartCases(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null, arg3 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
                it.MoveNext();
                arg3 = it.Current;
            }

            //// Cases will not be printed by themselves, since they must be in the context of a
            //// a switch.
            if (!data.InQuotation)
            {
                wr.Write("{0}(", ((Id)node.Function).Name);
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, ", ", 0, false));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, ", ", 0, false));
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, ")", 0, false));
                yield break;
            }

            if (IsDefault(arg1))
            {
                if (IsNil(arg2))
                {
                    wr.Write("default:\n{0};\n", PrintData.GetIndentString(data.Indentation + 1));
                }
                else if (IsStatement(arg2))
                {
                    wr.Write("default:\n");
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, null, data.Indentation + 1, true));
                }
                else
                {
                    wr.Write("default:\n");
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, ";\n", 0, true));
                }
            }
            else
            {
                if (IsNil(arg2))
                {
                    var suffix = string.Format(":\n{0};\n", PrintData.GetIndentString(data.Indentation + 1));
                    yield return new Tuple<Node, PrintData>(arg1, new PrintData("case ", suffix, 0, true));
                }
                else if (IsStatement(arg2))
                {
                    yield return new Tuple<Node, PrintData>(arg1, new PrintData("case ", ":\n", 0, true));
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, null, data.Indentation + 1, true));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(arg1, new PrintData("case ", ":\n", 0, true));
                    yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, ";\n", data.Indentation + 1, true));
                }
            }

            if (!IsNil(arg3))
            {
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(null, null, data.Indentation, true));
            }
        }

        private static void EndCases(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateFor(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 4)
            {
                flags.Add(MkBadArityFlag(node, 4));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartFor(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null, arg3 = null, arg4 = null ;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
                it.MoveNext();
                arg3 = it.Current;
                it.MoveNext();
                arg4 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());

            bool needsBlock;
            var isNil1 = IsNil(arg1);
            var isNil2 = IsNil(arg2);
            var isNil3 = IsNil(arg3);
            var isNil4 = IsNilStatement(arg4, out needsBlock);

            if (isNil1 && isNil2 && isNil3 && isNil4)
            {
                wr.Write("for (;;)\n{0}{{\n{0}}}\n", data.GetIndentString());
                yield break;
            }

            bool printPrefixLater = isNil1 && isNil2 && isNil3;
            string postFix = isNil4 ? string.Format("{0}{{\n{0}}}\n", data.GetIndentString()) : string.Empty; 
            if (!isNil1 && isNil2 && isNil3)
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData("for (", ";;)\n" + postFix, 0, true));
            }
            else if (isNil1 && !isNil2 && isNil3)
            {
                yield return new Tuple<Node, PrintData>(arg2, new PrintData("for (; ", ";)\n" + postFix, 0, true));
            }
            else if (isNil1 && isNil2 && !isNil3)
            {
                yield return new Tuple<Node, PrintData>(arg3, new PrintData("for (;; ", ")\n" + postFix, 0, true));
            }
            else if (!isNil1 && !isNil2 && isNil3)
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData("for (", null, 0, true));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData("; ", ";)\n" + postFix, 0, true));
            }
            else if (!isNil1 && isNil2 && !isNil3)
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData("for (", null, 0, true));
                yield return new Tuple<Node, PrintData>(arg3, new PrintData(";; ", ")\n" + postFix, 0, true));
            }
            else if (isNil1 && !isNil2 && !isNil3)
            {
                yield return new Tuple<Node, PrintData>(arg2, new PrintData("for (; ", null, 0, true));
                yield return new Tuple<Node, PrintData>(arg3, new PrintData("; ", ")\n" + postFix, 0, true));
            }
            else if (!isNil1 && !isNil2 && !isNil3)
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData("for (", null, 0, true));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData("; ", null, 0, true));
                yield return new Tuple<Node, PrintData>(arg3, new PrintData("; ", ")\n" + postFix, 0, true));
            }
          
            if (isNil4)
            {
                yield break;
            }
            else if (IsBlock(arg4))
            {
                if (printPrefixLater)
                {
                    var prefix = string.Format("for (;;)\n{0}", data.GetIndentString());
                    yield return new Tuple<Node, PrintData>(arg4, new PrintData(prefix, null, data.Indentation, true, true));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(arg4, new PrintData(null, null, data.Indentation, true));
                }
            }
            else if (needsBlock)
            {
                string prefix;
                if (printPrefixLater)
                {
                    prefix = string.Format("for (;;)\n{0}{{\n{1}", data.GetIndentString(), PrintData.GetIndentString(data.Indentation + 1));
                }
                else
                {
                    prefix = string.Format("{0}{{\n{1}", data.GetIndentString(), PrintData.GetIndentString(data.Indentation + 1));
                }

                var suffix = string.Format("{0}}}\n", data.GetIndentString());
                yield return new Tuple<Node, PrintData>(arg4, new PrintData(prefix, suffix, data.Indentation + 1, true, true));
            }
            else if (IsStatement(arg4))
            {
                if (printPrefixLater)
                {
                    var prefix = string.Format("for (;;)\n{0}", PrintData.GetIndentString(data.Indentation + 1));
                    yield return new Tuple<Node, PrintData>(arg4, new PrintData(prefix, null, data.Indentation + 1, true, true));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(arg4, new PrintData(null, null, data.Indentation + 1, true));
                }
            }
            else
            {
                if (printPrefixLater)
                {
                    var prefix = string.Format("for (;;)\n{0}", PrintData.GetIndentString(data.Indentation + 1));
                    yield return new Tuple<Node, PrintData>(arg4, new PrintData(prefix, ";\n", data.Indentation + 1, true, true));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(arg4, new PrintData(null, ";\n", data.Indentation + 1, true));
                }
            }
        }

        private static void EndFor(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateSeq(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartSeq(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            bool needsBlock;
            var isNil1 = IsNilStatement(arg1, out needsBlock);
            var isNil2 = IsNilStatement(arg2, out needsBlock);
            if (!isNil1)
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, IsStatement(arg1) ? "" : ";\n", data.Indentation, true, true));
            }

            if (!isNil2)
            {
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, IsStatement(arg2) ? "" : ";\n", data.Indentation, true, isNil1));
            }

            if (isNil1 && isNil2)
            {
                wr.Write(";\n");
            }
        }

        private static void EndSeq(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateReturn(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 1)
            {
                flags.Add(MkBadArityFlag(node, 1));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartReturn(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());

            if (IsNil(arg1))
            {
                wr.Write("return;\n");
            }
            else
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData("return ", ";\n", 0, true));
            }
        }

        private static void EndReturn(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateStrJmp(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 1)
            {
                flags.Add(MkBadArityFlag(node, 1));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartStrJmp(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
            }

            bool isBreak = true, isPrintable = true;
            var jmpKind = arg1 as Id;
            if (jmpKind == null)
            {
                isPrintable = false;
            }
            else if (jmpKind.Name == FormulaNodes.break_Iden.Node.Name)
            {
                isBreak = true;
            }
            else if (jmpKind.Name == FormulaNodes.continue_Iden.Node.Name)
            {
                isBreak = false;
            }
            else
            {
                isPrintable = false;
            }

            if (!isPrintable)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, null, 0, false));
                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());

            if (isBreak)
            {
                wr.Write("break;\n");
            }
            else
            {
                wr.Write("continue;\n");
            }
        }

        private static void EndStrJmp(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateLabel(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartLabel(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            Cnst labelName = null;
            bool isPrintable = true;
            if ((labelName = arg1 as Cnst) == null ||
                labelName.CnstKind != CnstKind.String ||
                !IsCIdentifier(labelName.GetStringValue()))
            {
                isPrintable = false;
            }

            //// This identifier is something we don't understand, so revert to escaped form.
            if (!isPrintable)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, ", ", 0, false));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, null, 0, false));
                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());

            bool needsBlock;
            if (IsNilStatement(arg2, out needsBlock))
            {
                wr.Write("{0}: ;\n",  labelName.GetStringValue());
            }
            else if (IsStatement(arg2))
            {
                wr.Write("{0}:\n", labelName.GetStringValue());
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, null, data.Indentation, true));
            }
            else
            {
                wr.Write("{0}:\n", labelName.GetStringValue());
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, ";\n", data.Indentation, true));
            }
        }

        private static void EndLabel(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateComment(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartComment(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            string comment;
            bool isBlockStyle = false;
            var isPrintable = IsStringCnst(arg1, out comment) &&
                              IsBoolean(arg2, out isBlockStyle);

            //// This identifier is something we don't understand, so revert to escaped form.
            if (!isPrintable)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, ", ", 0, false));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, null, 0, false));
                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            var splits = comment.Split(LineBreakChars, StringSplitOptions.None);
            if (isBlockStyle)
            {
                if (splits.Length == 0)
                {
                    wr.Write("/* */\n");
                }
                else if (splits.Length == 1)
                {
                    wr.Write("/*{0}*/\n", splits[0]);
                }
                else
                {
                    wr.Write("/*{0}\n", splits[0]);
                    for (int i = 1; i < splits.Length; ++i)
                    {
                        if (i == splits.Length - 1)
                        {
                            wr.Write("{0}{1}*/\n", data.GetIndentString(), splits[i]);
                        }
                        else
                        {
                            wr.Write("{0}{1}\n", data.GetIndentString(), splits[i]);
                        }
                    }
                }
            }
            else
            {
                if (splits.Length == 0)
                {
                    wr.Write("//\n");
                }
                else
                {
                    wr.Write("//{0}\n", splits[0]); 
                }

                for (int i = 1; i < splits.Length; ++i)
                {
                    wr.Write("{0}//{1}\n", data.GetIndentString(), splits[i]); 
                }
            }
        }

        private static void EndComment(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidatePpInclude(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartPpInclude(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            string filename;
            bool isSystem = false;
            var isPrintable = IsStringCnst(arg1, out filename) &&
                              IsBoolean(arg2, out isSystem);

            //// This identifier is something we don't understand, so revert to escaped form.
            if (!isPrintable)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, ", ", 0, false));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, null, 0, false));
                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            if (isSystem)
            {
                wr.Write("#include <{0}>\n", filename);
            }
            else
            {
                wr.Write("#include \"{0}\"\n", filename);
            }
        }

        private static void EndPpInclude(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidatePpDefine(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartPpDefine(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            wr.Write("#define ");
            if (IsNil(arg2))
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, "\n", 0, true));
            }
            else
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, " ", 0, true));
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, "\n", 0, true));
            }
        }

        private static void EndPpDefine(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidatePpUndefine(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 1)
            {
                flags.Add(MkBadArityFlag(node, 1));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartPpUndefine(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            wr.Write("#undef ");
            yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, "\n", 0, true));
        }

        private static void EndPpUndefine(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidatePpEscape(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 1)
            {
                flags.Add(MkBadArityFlag(node, 1));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartPpEscape(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
            }

            yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, "\n", 0, true));
        }

        private static void EndPpEscape(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidatePpPragma(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 1)
            {
                flags.Add(MkBadArityFlag(node, 1));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartPpPragma(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            wr.Write("#pragma ");
            yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, "\n", 0, true));
        }

        private static void EndPpPragma(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateGoto(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 1)
            {
                flags.Add(MkBadArityFlag(node, 1));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartGoto(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
            }

            Cnst labelName = null;
            bool isPrintable = true;
            if ((labelName = arg1 as Cnst) == null ||
                labelName.CnstKind != CnstKind.String ||
                !IsCIdentifier(labelName.GetStringValue()))
            {
                isPrintable = false;
            }

            //// This identifier is something we don't understand, so revert to escaped form.
            if (!isPrintable)
            {
                if (data.InQuotation)
                {
                    wr.Write("${0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")$";
                }
                else
                {
                    wr.Write("{0}(", ((Id)node.Function).Name);
                    data.PrivateSuffix = ")";
                }

                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, null, 0, false));
                yield break;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());

            wr.Write("goto {0};\n", labelName.GetStringValue());
        }

        private static void EndGoto(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateBlock(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartBlock(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            wr.WriteLineDirective(node.Span.StartLine, data.GetIndentString());
            wr.Write("{\n");

            bool needsBlock;
            var isNil1 = IsNil(arg1);
            var isNil2 = IsNilStatement(arg2, out needsBlock);
            if (!isNil1)
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, null, data.Indentation + 1, true, true));
            }

            if (!isNil2)
            {
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, IsStatement(arg2) ? "" : ";\n", data.Indentation + 1, true));
            }
        }

        private static void EndBlock(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write("{0}}}\n", data.GetIndentString());
            wr.Write(data.PrivateSuffix);
        }

        private static bool ValidateSection(FuncTerm node, List<Flag> flags, SuccessToken success)
        {
            if (node.Args.Count != 2)
            {
                flags.Add(MkBadArityFlag(node, 2));
                success.Failed();
                return false;
            }

            return true;
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartSection(FuncTerm node, PrintData data, CTextWriter wr)
        {
            Node arg1 = null, arg2 = null;
            using (var it = node.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            if (!data.InQuotation)
            {
                wr.Write("`");
            }

            if (data.Indentation == 0)
            {
                data.Indentation = 1;
                wr.Write("\n{0}", data.GetIndentString());
            }

            if (!data.InQuotation)
            {
                data.PrivateSuffix = string.Format("{0}`", data.GetIndentString());
            }

            bool isNil1 = IsNil(arg1);
            if (!isNil1)
            {
                yield return new Tuple<Node, PrintData>(arg1, new PrintData(null, null, data.Indentation, true, true));
            }

            if (!IsNil(arg2))
            {
                yield return new Tuple<Node, PrintData>(arg2, new PrintData(null, null, data.Indentation, true, isNil1));
            }
        }

        private static void EndSection(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartForgnFunc(FuncTerm node, PrintData data, CTextWriter wr)
        {
            if (data.InQuotation)
            {
                wr.Write("$");
                data.PrivateSuffix = "$";
            }

            var fnc = (FuncTerm)node;
            string opStr;
            var style = OpStyleKind.Apply;
            if (fnc.Function is OpKind)
            {
                opStr = ASTSchema.Instance.ToString((OpKind)fnc.Function, out style);
            }
            else
            {
                opStr = ((Id)fnc.Function).Name;
            }

            if (fnc.Args.Count == 0)
            {
                wr.Write(string.Format("{0}()", opStr));
            }
            else if (fnc.Args.Count > 2 || style == OpStyleKind.Apply)
            {
                wr.Write(string.Format("{0}(", opStr));
                int i = 0;
                foreach (var a in fnc.Args)
                {
                    yield return new Tuple<Node, PrintData>(a, new PrintData(null, i < fnc.Args.Count - 1 ? ", " : ")", 0, false));
                    ++i;
                }
            }
            else if (fnc.Args.Count == 2)
            {
                Node arg1, arg2;
                using (var it = fnc.Args.GetEnumerator())
                {
                    it.MoveNext();
                    arg1 = it.Current;
                    it.MoveNext();
                    arg2 = it.Current;
                }

                if (arg1.NodeKind == NodeKind.FuncTerm &&
                    ((FuncTerm)arg1).Function is OpKind &&
                    ASTSchema.Instance.NeedsParen((OpKind)fnc.Function, (OpKind)((FuncTerm)arg1).Function))
                {
                    yield return new Tuple<Node, PrintData>(
                        arg1,
                        new PrintData("(", string.Format(") {0} ", opStr), 0, false));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(
                        arg1,
                        new PrintData(null, string.Format(" {0} ", opStr), 0, false));
                }

                if (arg2.NodeKind == NodeKind.FuncTerm &&
                    ((FuncTerm)arg2).Function is OpKind &&
                    ASTSchema.Instance.NeedsParen((OpKind)fnc.Function, (OpKind)((FuncTerm)arg2).Function))
                {
                    yield return new Tuple<Node, PrintData>(
                        arg2,
                        new PrintData("(", ")", 0, false));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(
                        arg2,
                        new PrintData(null, null, 0, false));
                }
            }
            else
            {
                Node arg1;
                using (var it = fnc.Args.GetEnumerator())
                {
                    it.MoveNext();
                    arg1 = it.Current;
                }

                if (arg1.NodeKind == NodeKind.FuncTerm &&
                    ((FuncTerm)arg1).Function is OpKind &&
                    ASTSchema.Instance.NeedsParen((OpKind)fnc.Function, (OpKind)((FuncTerm)arg1).Function))
                {
                    yield return new Tuple<Node, PrintData>(
                        arg1,
                        new PrintData(string.Format("{0}(", ")"), opStr, 0, false));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(
                        arg1,
                        new PrintData(opStr, null, 0, false));
                }
            }
        }

        private static void EndForgnFunc(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartId(Id node, PrintData data, CTextWriter wr)
        {
            if (data.InQuotation)
            {
                wr.Write("${0}", node.Name);
                data.PrivateSuffix = "$";
            }
            else
            {
                wr.Write(node.Name);
            }

            return NoChildren;
        }

        private static void EndId(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartCnst(Cnst node, PrintData data, CTextWriter wr)
        {
            if (data.InQuotation)
            {
                wr.Write("$");
                data.PrivateSuffix = "$";
            }

            switch (node.CnstKind)
            {
                case CnstKind.String:
                    wr.Write("\"{0}\"", node.GetStringValue());
                    break; 
                case CnstKind.Numeric:
                    wr.Write(node.GetNumericValue());
                    break;
                default:
                    throw new NotImplementedException();
            }

            return NoChildren;
        }

        private static void EndCnst(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartQuote(Quote node, PrintData data, CTextWriter wr)
        {
            //// To be on the safe side, we unquote nested quotations
            if (data.InQuotation)
            {
                wr.Write("$");
                data.PrivateSuffix = "$";
            }

            wr.Write("`");
            int i = 0;
            foreach (var q in node.Contents)
            {
                if (q.NodeKind == NodeKind.QuoteRun)
                {
                    yield return new Tuple<Node, PrintData>(q, new PrintData(null, i < node.Contents.Count - 1 ? null : "`", 0, false));
                }
                else
                {
                    yield return new Tuple<Node, PrintData>(q, new PrintData("$", i < node.Contents.Count - 1 ? "$" : "$`", 0, false));
                }

                ++i;
            }           
        }

        private static void EndQuote(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartQuoteRun(QuoteRun node, PrintData data, CTextWriter wr)
        {
            wr.Write(node.Text);
            yield break;
        }

        private static void EndQuoteRun(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(data.PrivateSuffix);
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartCompr(Compr cmpr, PrintData data, CTextWriter wr)
        {
            if (data.InQuotation)
            {
                wr.Write("$");
                data.PrivateSuffix = "$";
            }

            wr.Write("{ ");
            int i = 0;
            foreach (var h in cmpr.Heads)
            {
                yield return new Tuple<Node, PrintData>(
                    h,
                    new PrintData(null, i < cmpr.Heads.Count - 1 ? ", " : null, 0, false));
                ++i;
            }

            i = 0;
            foreach (var b in cmpr.Bodies)
            {
                yield return new Tuple<Node, PrintData>(
                    b,
                    new PrintData(i == 0 ? " | " : null, i < cmpr.Bodies.Count - 1 ? ", " : null, 0, false));
                ++i;
            }
        }

        private static void EndCompr(Node node, PrintData data, CTextWriter wr)
        {
            wr.Write(" }");
            wr.Write(data.PrivateSuffix);
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartBody(Node node, PrintData data, CTextWriter wr)
        {
            var body = (Body)node;
            int i = 0;
            foreach (var c in body.Constraints)
            {
                yield return new Tuple<Node, PrintData>(c, new PrintData(null, i < body.Constraints.Count - 1 ? ", " : null, 0, false));
                ++i;
            }
        }

        private static void EndBody(Node node, PrintData data, CTextWriter wr)
        {
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartFind(Node node, PrintData data, CTextWriter wr)
        {
            var pat = (Find)node;
            if (pat.Binding != null)
            {
                yield return new Tuple<Node, PrintData>(pat.Binding, new PrintData(null, " is ", 0, false));
            }

            yield return new Tuple<Node, PrintData>(pat.Match, new PrintData(null, null, 0, false));
        }

        private static void EndFind(Node node, PrintData data, CTextWriter wr)
        {
        }

        private static IEnumerable<Tuple<Node, PrintData>> StartRelConstr(Node node, PrintData data, CTextWriter wr)
        {
            var rc = (RelConstr)node;
            var op = ASTSchema.Instance.ToString(rc.Op);
            if (rc.Op == RelKind.No && rc.Arg1.NodeKind == NodeKind.Compr &&
                ((Compr)rc.Arg1).Heads.Count > 0 &&
                ((Compr)rc.Arg1).Bodies.Count == 1 &&
                ((Compr)rc.Arg1).Bodies.First<Body>().Constraints.Count == 1 &&
                ((Compr)rc.Arg1).Bodies.First<Body>().Constraints.First<Node>().NodeKind == NodeKind.Find)
            {
                wr.Write("{0} ", op);
                yield return new Tuple<Node, PrintData>(
                    ((Compr)rc.Arg1).Bodies.First<Body>().Constraints.First<Node>(),
                    new PrintData(null, null, 0, false));
            }
            else if (rc.Arg2 == null)
            {
                wr.Write("{0} ", op);
                yield return new Tuple<Node, PrintData>(rc.Arg1, new PrintData(null, null, 0, false));
            }
            else
            {
                yield return new Tuple<Node, PrintData>(rc.Arg1, new PrintData(null, string.Format(" {0} ", op), 0, false));
                yield return new Tuple<Node, PrintData>(rc.Arg2, new PrintData(null, null, 0, false));
            }
        }

        private static void EndRelConstr(Node node, PrintData data, CTextWriter wr)
        {
        }

        private static bool TryGetLoopKind(Node n, out LoopKind kind)
        {
            if (n.NodeKind != NodeKind.Id)
            {
                kind = LoopKind.Do;
                return false;
            }

            var id = (Id)n;
            if (id.Name == FormulaNodes.While_Iden.Node.Name)
            {
                kind = LoopKind.While;
                return true;
            }
            else if (id.Name == FormulaNodes.Do_Iden.Node.Name)
            {
                kind = LoopKind.Do;
                return true;
            }
            else
            {
                kind = LoopKind.Do;
                return false;
            }            
        }

        private static bool TryGetOpInfo(Node n, out OpInfo info, Map<Id, OpInfo> opIndex = null)
        {
            if (n.NodeKind == NodeKind.Id)
            {
                var id = (Id)n;
                if (opIndex != null)
                {
                    return opIndex.TryFindValue(id, out info);
                }

                if (FrmIdToUnOp.TryFindValue(id, out info))
                {
                    return true;
                }

                if (FrmIdToBinOp.TryFindValue(id, out info))
                {
                    return true;
                }

                if (FrmIdToTerOp.TryFindValue(id, out info))
                {
                    return true;
                }

                return false;
            }
            else if (n.NodeKind != NodeKind.FuncTerm)
            {
                info = default(OpInfo);
                return false;
            }

            var ft = (FuncTerm)n;
            if (ft.Args.Count == 0)
            {
                info = default(OpInfo);
                return false;
            }

            var ftId = ft.Function as Id;
            if (ftId == null)
            {
                info = default(OpInfo);
                return false;
            }

            if (ftId.Name == FormulaNodes.UnApp_Iden.Node.Name ||
                ftId.Name == FormulaNodes.BinApp_Iden.Node.Name ||
                ftId.Name == FormulaNodes.TerApp_Iden.Node.Name)
            {
                Node arg0 = null;
                using (var it = ft.Args.GetEnumerator())
                {
                    it.MoveNext();
                    arg0 = it.Current;
                }

                if (arg0.NodeKind == NodeKind.Id)
                {
                    return TryGetOpInfo(arg0, out info, opIndex);
                }                
            }

            info = default(OpInfo);
            return false;
        }

        private static string ToCharLiteral(int charVal)
        {
            if (charVal == (int)'\'')
            {
                return "'\\''";
            }
            else if (charVal == (int)'\"')
            {
                return "'\\\"'";
            }
            else if (charVal == (int)'\\')
            {
                return "'\\\\'";
            }
            else if (charVal == (int)'\0')
            {
                return "'\\0'";
            }
            else if (charVal == (int)'\a')
            {
                return "'\\a'";
            }
            else if (charVal == (int)'\b')
            {
                return "'\\b'";
            }
            else if (charVal == (int)'\f')
            {
                return "'\\f'";
            }
            else if (charVal == (int)'\n')
            {
                return "'\\n'";
            }
            else if (charVal == (int)'\r')
            {
                return "'\\r'";
            }
            else if (charVal == (int)'\t')
            {
                return "'\\t'";
            }
            else if (charVal == (int)'\v')
            {
                return "'\\v'";
            }
            else if (charVal >= 32 && charVal <= 126)
            {
                return string.Format("'{0}'", (char)charVal);
            }
            else if (charVal >= 0 && charVal <= 255)
            {
                return string.Format("'\\x{0:X2}'", charVal);     
            }
            else if (charVal > 255 && charVal < 65536)
            {
                return string.Format("'\\x{0:X4}'", charVal);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void AddOpData(Id name, int arity, string printedForm, int precedence, OpPlacementKind placement, bool isStandardSpacing = true)
        {
            switch (arity)
            {
                case 1:
                    FrmIdToUnOp.Add(name, new OpInfo(name, arity, printedForm, precedence, placement, isStandardSpacing));
                    break;
                case 2:
                    FrmIdToBinOp.Add(name, new OpInfo(name, arity, printedForm, precedence, placement, isStandardSpacing));
                    break;
                case 3:
                    FrmIdToTerOp.Add(name, new OpInfo(name, arity, printedForm, precedence, placement, isStandardSpacing));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static Flag MkBadArityFlag(FuncTerm func, int expectedArity)
        {
            return new Flag(
                    SeverityKind.Error,
                    func,
                    Constants.QuotationError.ToString(
                      string.Format("Expected {0} term to have {1} argument(s)", ((Id)func.Function).Name, expectedArity)),
                    Constants.QuotationError.Code);
        }

        private static Flag MkExpectedIdFlag(Node node)
        {
            return new Flag(
                    SeverityKind.Error,
                    node,
                    Constants.QuotationError.ToString(
                      string.Format("Expected an Id; instead got a(n) {0}", node.NodeKind)),
                    Constants.QuotationError.Code);
        }

        private static int CompareId(Id id1, Id id2)
        {
            return string.CompareOrdinal(id1.Name, id2.Name);
        }

        private static bool IsUnFunToId(Node n, Id funName, Id argName)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                return false;
            }

            var funTerm = (FuncTerm)n;
            if (funTerm.Args.Count != 1)
            {
                return false;
            }

            var funId = funTerm.Function as Id;
            if (funId == null || funId.Name != funName.Name)
            {
                return false;
            }

            Id arg = null;
            using (var it = funTerm.Args.GetEnumerator())
            {
                it.MoveNext();
                arg = it.Current as Id;
            }

            return arg != null && arg.Name == argName.Name;
        }

        private static bool IsNil(Node n)
        {
            if (n.NodeKind != NodeKind.Id)
            {
                return false;
            }

            return ((Id)n).Name == FormulaNodes.Nil_Iden.Node.Name;
        }

        private static bool IsBoolean(Node n, out bool value)
        {
            if (n.NodeKind != NodeKind.Id)
            {
                value = false;
                return false;
            }

            var id = (Id)n;
            if (id.Name == FormulaNodes.True_Iden.Node.Name)
            {
                value = true;
                return true;
            }
            else if (id.Name == FormulaNodes.False_Iden.Node.Name)
            {
                value = false;
                return true;
            }
            else
            {
                value = false;
                return false;
            }
        }

        private static bool IsUnknown(Node n)
        {
            if (n.NodeKind != NodeKind.Id)
            {
                return false;
            }

            return ((Id)n).Name == FormulaNodes.Unknown_Iden.Node.Name;
        }

        private static bool IsDefault(Node n)
        {
            if (n.NodeKind != NodeKind.Id)
            {
                return false;
            }

            return ((Id)n).Name == FormulaNodes.Default_Iden.Node.Name;
        }

        private static bool IsNilStatement(Node n, out bool needsBlock)
        {
            if (IsNil(n))
            {
                needsBlock = false;
                return true;
            }
            else if (!IsSeq(n))
            {
                needsBlock = IsITE(n);
                return false;
            }

            var seqFun = (FuncTerm)n;
            int nonNilCount = 0;
            foreach (var s in seqFun.Args)
            {
                if (nonNilCount >= 2)
                {
                    break;
                }

                var ast = Factory.Instance.ToAST(s);
                ast.Compute<bool>(
                    (m) =>
                    {
                        if (m.NodeKind != NodeKind.Id &&
                            m.NodeKind != NodeKind.FuncTerm)
                        {
                            nonNilCount++;
                            return null;
                        }

                        if (m.NodeKind == NodeKind.Id)
                        {
                            if (!IsNil(m))
                            {
                                nonNilCount++;
                            }

                            return null;
                        }

                        var func = ((FuncTerm)m).Function as Id;
                        if (func == null || func.Name != FormulaNodes.Seq_Iden.Node.Name)
                        {
                            if (func != null && func.Name == FormulaNodes.ITE_Iden.Node.Name)
                            {
                                nonNilCount = 2;
                            }
                            else
                            {
                                nonNilCount++;
                            }

                            return null;
                        }

                        return nonNilCount < 2 ? ((FuncTerm)m).Args : null;
                    },
                    (m, results) => true);
            }

            needsBlock = nonNilCount >= 1;
            return nonNilCount == 0;
        }

        private static bool IsBlock(Node n)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                return false;
            }

            var func = ((FuncTerm)n).Function as Id;     
            return func != null && func.Name == FormulaNodes.Block_Iden.Node.Name;
        }

        private static bool IsSeq(Node n)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                return false;
            }

            var func = ((FuncTerm)n).Function as Id;
            return func != null && func.Name == FormulaNodes.Seq_Iden.Node.Name;
        }

        private static bool IsITE(Node n)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                return false;
            }

            var func = ((FuncTerm)n).Function as Id;
            return func != null && func.Name == FormulaNodes.ITE_Iden.Node.Name;
        }

        private static bool IsCmp(Node n)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                return false;
            }

            var func = ((FuncTerm)n).Function as Id;
            if (func == null)
            {
                return false;
            }

            return func.Name == FormulaNodes.PpEscape_Iden.Node.Name || 
                   func.Name == FormulaNodes.PpInclude_Iden.Node.Name ||
                   func.Name == FormulaNodes.PpDefine_Iden.Node.Name ||
                   func.Name == FormulaNodes.PpUndef_Iden.Node.Name ||
                   func.Name == FormulaNodes.PpITE_Iden.Node.Name ||
                   func.Name == FormulaNodes.PpPragma_Iden.Node.Name ||
                   func.Name == FormulaNodes.Section_Iden.Node.Name ||
                   IsDefinition(n);
        }

        private static bool IsPpElIf(Node n)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                return false;
            }

            var func = ((FuncTerm)n).Function as Id;
            return func != null && func.Name == FormulaNodes.PpElIf_Iden.Node.Name;
        }

        private static bool IsStatement(Node n)
        {
            switch (n.NodeKind)
            {
                case NodeKind.FuncTerm:
                    {
                        var id = ((FuncTerm)n).Function as Id;
                        return id != null && FrmStatements.Contains(id);
                    }
                default:
                    return false;
            }
        }

        private static bool IsElement(Node n)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                return false;
            }

            var func = ((FuncTerm)n).Function as Id;
            return func != null && func.Name == FormulaNodes.Element_Iden.Node.Name;
        }

        private static bool IsDefinition(Node n)
        {
            switch (n.NodeKind)
            {
                case NodeKind.FuncTerm:
                    {
                        var id = ((FuncTerm)n).Function as Id;
                        return id != null && FrmDefinitions.Contains(id);
                    }
                default:
                    return false;
            }
        }

        private static bool IsFunType(Node n)
        {
            return n.NodeKind == NodeKind.FuncTerm &&
                   ((FuncTerm)n).Function is Id &&
                   ((Id)((FuncTerm)n).Function).Name == FormulaNodes.FunType_Iden.Node.Name;
        }

        private static bool IsStorageKind(Node n, out StorageKind kind)
        {
            var storId = n as Id;
            if (storId == null)
            {
                kind = StorageKind.None;
                return false;
            }

            if (storId.Name == FormulaNodes.Auto_Iden.Node.Name)
            {
                kind = StorageKind.Auto;
            }
            else if (storId.Name == FormulaNodes.Register_Iden.Node.Name)
            {
                kind = StorageKind.Register;
            }
            else if (storId.Name == FormulaNodes.static_Iden.Node.Name)
            {
                kind = StorageKind.Static;
            }
            else if (storId.Name == FormulaNodes.Extern_Iden.Node.Name)
            {
                kind = StorageKind.Extern;
            }
            else if (storId.Name == FormulaNodes.Nil_Iden.Node.Name)
            {
                kind = StorageKind.None;
            }
            else
            {
                kind = StorageKind.None;
                return false;
            }

            return true;
        }

        private static bool IsPpIfKind(Node n, out PpIfKind kind)
        {
            var ifId = n as Id;
            if (ifId == null)
            {
                kind = PpIfKind.If;
                return false;
            }

            if (ifId.Name == FormulaNodes.PpIf_Iden.Node.Name)
            {
                kind = PpIfKind.If;
            }
            else if (ifId.Name == FormulaNodes.PpIfdef_Iden.Node.Name)
            {
                kind = PpIfKind.Ifdef;
            }
            else if (ifId.Name == FormulaNodes.PpIfndef_Iden.Node.Name)
            {
                kind = PpIfKind.Ifndef;
            }
            else
            {
                kind = PpIfKind.If;
                return false;
            }

            return true;
        }

        private static bool IsDataKind(Node n, out DataKind kind)
        {
            var dataId = n as Id;
            if (dataId == null)
            {
                kind = DataKind.Struct;
                return false;
            }

            if (dataId.Name == FormulaNodes.struct_Iden.Node.Name)
            {
                kind = DataKind.Struct;
            }
            else if (dataId.Name == FormulaNodes.union_Iden.Node.Name)
            {
                kind = DataKind.Union;
            }
            else
            {
                kind = DataKind.Struct;
                return false;
            }

            return true;
        }

        private static string GetDataString(DataKind kind)
        {
            switch (kind)
            {
                case DataKind.Struct:
                    return "struct ";
                case DataKind.Union:
                    return "union ";
                default:
                    throw new NotImplementedException();
            }
        }

        private static string GetPpIfString(PpIfKind kind)
        {
            switch (kind)
            {
                case PpIfKind.If:
                    return "#if ";
                case PpIfKind.Ifdef:
                    return "#ifdef ";
                case PpIfKind.Ifndef:
                    return "#ifndef ";
                default:
                    throw new NotImplementedException();
            }
        }

        private static string GetStorageString(StorageKind kind)
        {
            switch (kind)
            {
                case StorageKind.Auto:
                    return "auto ";
                case StorageKind.Extern:
                    return "extern ";
                case StorageKind.Register:
                    return "register ";
                case StorageKind.Static:
                    return "static ";
                case StorageKind.None:
                    return string.Empty;
                default:
                    throw new NotImplementedException();
            }
        }

        private static bool GetFunTypeData(Node funType, out Node retType, out Node prmTypes)
        {
            prmTypes = retType = null;
            //// First make sure this is a proper prmTypes term
            if (funType.NodeKind != NodeKind.FuncTerm)
            {
                return false;
            }

            var funcTerm = (FuncTerm)funType;
            if (funcTerm.Args.Count != 2)
            {
                return false;
            }

            var func = funcTerm.Function as Id;
            if (func == null || func.Name != FormulaNodes.FunType_Iden.Node.Name)
            {
                return false;
            }

            using (var it = funcTerm.Args.GetEnumerator())
            {
                it.MoveNext();
                retType = it.Current;
                it.MoveNext();
                prmTypes = it.Current;
            }

            return true;
        }

        private static bool GetFunParamInfo(
                                    Node prmTypes, 
                                    Node prms, 
                                    out StorageKind kind,
                                    out Node type, 
                                    out string name, 
                                    out bool isEllipse, 
                                    out Node tailPrmTypes, 
                                    out Node tailPrms)
        {
            name = null;
            isEllipse = false;
            kind = StorageKind.None;
            type = tailPrms = tailPrmTypes = null;

            //// First make sure this is a proper prmTypes term
            if (prmTypes.NodeKind != NodeKind.FuncTerm)
            {
                return false;
            }

            var funcTerm = (FuncTerm)prmTypes;
            if (funcTerm.Args.Count != 2)
            {
                return false;
            }

            var func = funcTerm.Function as Id;
            if (func == null || func.Name != FormulaNodes.PrmTypes_Iden.Node.Name)
            {
                return false;
            }

            using (var it = funcTerm.Args.GetEnumerator())
            {
                it.MoveNext();
                type = it.Current;
                it.MoveNext();
                tailPrmTypes = it.Current;
            }

            if (tailPrmTypes.NodeKind == NodeKind.Id &&
                ((Id)tailPrmTypes).Name == FormulaNodes.Nil_Iden.Node.Name)
            {
                tailPrmTypes = null;
            }
            else if (tailPrmTypes.NodeKind == NodeKind.Id &&
                ((Id)tailPrmTypes).Name == FormulaNodes.Ellipse_Iden.Node.Name)
            {
                isEllipse = true;
                tailPrmTypes = null;
            }

            //// If the prms are empty then paramTypes must be (void, nil).
            if (IsNil(prms))
            {
                if (IsUnFunToId(type, FormulaNodes.BaseType_Iden.Node, FormulaNodes.void_Iden.Node) && !isEllipse)
                {
                    type = null;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            //// Second make sure that prms is a proper prms term
            if (prms.NodeKind != NodeKind.FuncTerm)
            {
                return false;
            }

            funcTerm = (FuncTerm)prms;
            if (funcTerm.Args.Count != 2)
            {
                return false;
            }

            func = funcTerm.Function as Id;
            if (func == null || func.Name != FormulaNodes.Params_Iden.Node.Name)
            {
                return false;
            }

            Node prm;
            using (var it = funcTerm.Args.GetEnumerator())
            {
                it.MoveNext();
                prm = it.Current;
                it.MoveNext();
                tailPrms = it.Current;
            }

            //// Third make sure that prm is a proper prm term
            if (prm.NodeKind != NodeKind.FuncTerm)
            {
                return false;
            }

            funcTerm = (FuncTerm)prm;
            if (funcTerm.Args.Count != 2)
            {
                return false;
            }

            func = funcTerm.Function as Id;
            if (func == null || func.Name != FormulaNodes.Param_Iden.Node.Name)
            {
                return false;
            }

            Node storId, nameCnst;
            using (var it = funcTerm.Args.GetEnumerator())
            {
                it.MoveNext();
                storId = it.Current;
                it.MoveNext();
                nameCnst = it.Current;
            }

            if (!IsStorageKind(storId, out kind) ||
                !IsStringCnst(nameCnst, out name) ||
                !IsCIdentifier(name))
            {
                return false;
            }

            if (IsNil(tailPrms))
            {
                tailPrms = null;
            }

            if (tailPrms == null && tailPrmTypes != null ||
                tailPrms != null && tailPrmTypes == null)
            {
                return false;
            }
            
            return true;         
        }

        internal static bool IsCIdentifier(string s)
        {
            if (s == null || s.Length == 0)
            {
                return false;
            }
            /* Shaz: 
             * I am commenting this code to allow printing arbitrary strings as identifiers.
             * Otherwise, we do not have a way to print pragma arguments.
            char c;
            for (int i = 0; i < s.Length; ++i)
            {
                c = s[i];
                if (char.IsDigit(c))
                {
                    if (i == 0)
                    {
                        return false;
                    }
                }
                else if (c != '_' && !char.IsLetter(c))
                {
                    return false;
                }
            }
             */
            return true;
        }

        internal static bool IsStringCnst(Node n, out string value)
        {
            var cnst = n as Cnst;
            if (cnst == null || cnst.CnstKind != CnstKind.String)
            {
                value = null;
                return false;
            }

            value = cnst.GetStringValue();
            return true;
        }

        private struct SyntaxInfo
        {
            private Func<FuncTerm, PrintData, CTextWriter, IEnumerable<Tuple<Node, PrintData>>> printerStart;
            private Action<Node, PrintData, CTextWriter> printerEnd;
            private Func<FuncTerm, List<Flag>, SuccessToken, bool> validator;

            public Func<FuncTerm, PrintData, CTextWriter, IEnumerable<Tuple<Node, PrintData>>> PrinterStart
            {
                get { return printerStart; }
            }

            public Action<Node, PrintData, CTextWriter> PrinterEnd
            {

                get { return printerEnd; }
            }

            public Func<FuncTerm, List<Flag>, SuccessToken, bool> Validator
            {
                get { return validator; }
            }

            public SyntaxInfo(Func<FuncTerm, PrintData, CTextWriter, IEnumerable<Tuple<Node, PrintData>>> printerStart,
                              Action<Node, PrintData, CTextWriter> printerEnd,
                              Func<FuncTerm, List<Flag>, SuccessToken, bool> validator)
            {
                this.printerStart = printerStart;
                this.printerEnd = printerEnd;
                this.validator = validator;
            }
        }

        private struct OpInfo
        {
            private Id name;
            private string printedForm;
            private int precedence;
            private int arity;
            private OpPlacementKind placement;
            private bool isStandardSpacing;

            public Id Name
            {
                get { return name; }
            }

            public string PrintedForm
            {
                get { return printedForm; }
            }

            public int Precedence
            {
                get { return precedence; }
            }

            public int Arity
            {
                get { return arity; }
            }

            public OpPlacementKind Placement
            {
                get { return placement; }
            }

            public bool IsStandardSpacing
            {
                get { return isStandardSpacing; }
            }

            public OpInfo(Id name, int arity, string printedForm, int precedence, OpPlacementKind placement, bool isStandardSpacing = true)
            {
                this.name = name;
                this.printedForm = printedForm;
                this.precedence = precedence;
                this.arity = arity;
                this.placement = placement;
                this.isStandardSpacing = isStandardSpacing;
            }
        }

        private class PrintData
        {
            private string prefix;
            private string suffix;
            private int indentation;
            private bool inQuotation;
            private Action<Node, PrintData, CTextWriter> printEnd;

            public string Prefix
            {
                get { return prefix; }
            }

            public string Suffix
            {
                get { return suffix; }
                set { suffix = value; }
            }

            public string PrivateSuffix
            {
                get;
                set;
            }

            public int Indentation
            {
                get { return indentation; }
                set { indentation = value; }
            }

            public bool InQuotation
            {
                get { return inQuotation; }
            }

            public bool SkipIndent
            {
                get;
                private set;
            }

            public Action<Node, PrintData, CTextWriter> PrintEnd
            {
                get { return printEnd; }
                set { printEnd = value; }
            }

            public static string GetIndentString(int n)
            {
                return n <= 0 ? string.Empty : new string(' ', 2 * n);
            }

            public string GetIndentString()
            {
                /* For debugging
                if (indentation % 2 == 0)
                {
                    return indentation <= 0 ? string.Empty : indentation.ToString() + new string('+', 2 * indentation);
                }
                else
                {
                    return indentation <= 0 ? string.Empty : indentation.ToString() + new string('-', 2 * indentation);
                }
                */
               
                return indentation <= 0 ? string.Empty : new string(' ', 2 * indentation);
            }

            public PrintData(string prefix, string suffix, int indentation, bool inQuotation, bool skipIndent = false)
            {
                this.prefix = prefix;
                this.suffix = suffix;
                this.indentation = indentation;
                this.inQuotation = inQuotation;
                this.printEnd = null;
                this.SkipIndent = skipIndent;
            }
        }
    }
}
