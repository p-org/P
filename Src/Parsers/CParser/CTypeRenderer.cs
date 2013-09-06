namespace CParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text;
    using System.Threading;

    using Microsoft.Formula.API;
    using Microsoft.Formula.API.ASTQueries;
    using Microsoft.Formula.API.Nodes;
    using Microsoft.Formula.Common;

    using System.IO;
    using System.Numerics;

    internal static class CTypeRenderer
    {
        private static Map<Id, string> FrmToBaseType;

        static CTypeRenderer()
        {
            //Types
            FrmToBaseType = new Map<Id, string>(CompareId);
            FrmToBaseType[FormulaNodes.int_Iden.Node] = "int";
            FrmToBaseType[FormulaNodes.uchar_Iden.Node] = "unsigned char";
            FrmToBaseType[FormulaNodes.schar_Iden.Node] = "signed char";
            FrmToBaseType[FormulaNodes.short_Iden.Node] = "short";
            FrmToBaseType[FormulaNodes.ushort_Iden.Node] = "unsigned short";
            FrmToBaseType[FormulaNodes.sshort_Iden.Node] = "signed short";
            FrmToBaseType[FormulaNodes.uint_Iden.Node] = "unsigned int";
            FrmToBaseType[FormulaNodes.sint_Iden.Node] = "signed int";
            FrmToBaseType[FormulaNodes.byte_Iden.Node] = "byte";
            FrmToBaseType[FormulaNodes.long_Iden.Node] = "long";
            FrmToBaseType[FormulaNodes.ulong_Iden.Node] = "unsigned long";
            FrmToBaseType[FormulaNodes.slong_Iden.Node] = "signed long";
            FrmToBaseType[FormulaNodes.double_Iden.Node] = "double";
            FrmToBaseType[FormulaNodes.void_Iden.Node] = "void";
            FrmToBaseType[FormulaNodes.float_Iden.Node] = "float";
            FrmToBaseType[FormulaNodes.ldouble_Iden.Node] = "long double";
        }

        public static bool Render(Node n, string identifier, out string expression, CancellationToken cancel)
        {
            var data = new PrintData();
            data.Builder.Append(identifier);
            var success = new SuccessToken();
            Factory.Instance.ToAST(n).Compute<PrintData, bool>(
                data,
                (m, d) => Unfold(m, d, success),
                (m, v, children) => true,
                cancel);
            expression = data.Builder.ToString().Trim();
            return success.Succeeded;
        }

        public static bool Render(Node n, out string expression, CancellationToken cancel)
        {
            var data = new PrintData();
            var success = new SuccessToken();
            Factory.Instance.ToAST(n).Compute<PrintData, bool>(
                data,
                (m, d) => Unfold(m, d, success),
                (m, v, children) => true,
                cancel);
            expression = data.Builder.ToString().Trim();
            return success.Succeeded;
        }

        private static IEnumerable<Tuple<Node, PrintData>> Unfold(Node n, PrintData d, SuccessToken success)
        {
            string typename, tag;
            Node type, prms;
            BigInteger dimen;
            bool isConst, isVolatile;

            if (IsBaseType(n, out typename))
            {
                d.Builder.Insert(0, d.ModifierString + typename + " ");
                d.ClearModifiers();
                yield break;
            }
            else if (IsNamedType(n, out tag, out typename))
            {
                d.Builder.Insert(0, d.ModifierString + tag + typename + " ");
                d.ClearModifiers();
                yield break;
            }
            else if (IsQualType(n, out type, out isConst, out isVolatile))
            {
                var dnext = new PrintData(d);
                dnext.IsConstModified |= isConst;
                dnext.IsVolatileModified |= isVolatile;
                yield return new Tuple<Node, PrintData>(type, dnext);
            }
            else if (IsPtrType(n, out type))
            {
                d.Builder.Insert(0, d.IsModified ? "* " + d.ModifierString : "*");
                d.ClearModifiers();
                var dnext = new PrintData(d);
                dnext.WasPtrExpression = true;
                yield return new Tuple<Node, PrintData>(type, dnext);
            }
            else if (IsArrayType(n, out type, out dimen))
            {
                if (d.IsModified)
                {
                    success.Failed();
                    yield break;
                }

                if (d.WasPtrExpression)
                {
                    if (d.Builder[d.Builder.Length - 1] == ' ')
                    {
                        d.Builder.Remove(d.Builder.Length - 1, 1);
                    }
                    
                    d.Builder.Insert(0, "(");
                    d.Builder.Append(")");
                }

                if (dimen.Sign < 0)
                {
                    d.Builder.Append("[]");
                }
                else
                {
                    d.Builder.Append("[" + dimen.ToString() + "]");
                }

                var dnext = new PrintData(d);
                dnext.WasPtrExpression = false;
                yield return new Tuple<Node, PrintData>(type, dnext);
            }
            else if (IsFuncType(n, out type, out prms))
            {
                if (d.IsModified)
                {
                    success.Failed();
                    yield break;
                }

                if (d.WasPtrExpression)
                {
                    if (d.Builder[d.Builder.Length - 1] == ' ')
                    {
                        d.Builder.Remove(d.Builder.Length - 1, 1);
                    }

                    d.Builder.Insert(0, "(");
                    d.Builder.Append(")");
                }

                d.Builder.Append("(");
                string prmRender;
                bool isPrms = false, isFirst = true, isEllipse = false;
                Node prmtype, tail;
                while ((isPrms = IsFunPrms(prms, out prmtype, out tail, out isEllipse)))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        d.Builder.Append(", ");
                    }

                    if (!Render(prmtype, out prmRender, default(CancellationToken)))
                    {
                        isPrms = false;
                        break;
                    }
                    else
                    {
                        d.Builder.Append(prmRender);
                    }

                    if (tail == null)
                    {
                        break;
                    }
                    else
                    {
                        prms = tail;
                    }
                }

                if (isEllipse)
                {
                    d.Builder.Append(", ...");
                }
                
                d.Builder.Append(")");
                if (!isPrms)
                {
                    success.Failed();
                    yield break;
                }

                var dnext = new PrintData(d);
                dnext.WasPtrExpression = false;
                yield return new Tuple<Node, PrintData>(type, dnext);
            }
            else
            {
                success.Failed();
                yield break;
            }
        }

        private static bool IsFunPrms(Node n, out Node type, out Node tail, out bool isEllipse)
        {
            isEllipse = false;
            type = tail = null;
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                return false;
            }

            var funcTerm = (FuncTerm)n;
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
                tail = it.Current;
            }

            if (tail.NodeKind == NodeKind.Id &&
                ((Id)tail).Name == FormulaNodes.Nil_Iden.Node.Name)
            {
                tail = null;
            }
            else if (tail.NodeKind == NodeKind.Id &&
                ((Id)tail).Name == FormulaNodes.Ellipse_Iden.Node.Name)
            {
                isEllipse = true;
                tail = null;
            }

            return true;
        }

        private static bool IsFuncType(Node n, out Node type, out Node prms)
        {
            type = prms = null;
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                return false;
            }

            var funcTerm = (FuncTerm)n;
            if (funcTerm.Args.Count != 2)
            {
                return false;
            }

            var func = funcTerm.Function as Id;
            if (func == null ||
                func.Name != FormulaNodes.FunType_Iden .Node.Name)
            {
                return false;
            }

            using (var it = funcTerm.Args.GetEnumerator())
            {
                it.MoveNext();
                type = it.Current;
                it.MoveNext();
                prms = it.Current;
            }

            return true;
        }

        private static bool IsQualType(Node n, out Node type, out bool isConst, out bool isVolatile)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                type = null;
                isConst = isVolatile = false;
                return false;
            }

            var funcTerm = (FuncTerm)n;
            if (funcTerm.Args.Count != 2)
            {
                type = null;
                isConst = isVolatile = false;
                return false;
            }

            var func = funcTerm.Function as Id;
            if (func == null ||
                func.Name != FormulaNodes.QualType_Iden.Node.Name)
            {
                type = null;
                isConst = isVolatile = false;
                return false;
            }

            Node arg1 = null, arg2 = null;
            using (var it = funcTerm.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            var modId = arg2 as Id;
            if (modId == null)
            {
                type = null;
                isConst = isVolatile = false;
                return false;
            }
            else if (modId.Name == FormulaNodes.Const_Iden.Node.Name)
            {
                type = arg1;
                isConst = true;
                isVolatile = false;
                return true;
            }
            else if (modId.Name == FormulaNodes.volatile_Iden.Node.Name)
            {
                type = arg1;
                isConst = false;
                isVolatile = true;
                return true;
            }
            else
            {
                type = null;
                isConst = isVolatile = false;
                return false;
            }
        }

        private static bool IsPtrType(Node n, out Node type)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                type = null;
                return false;
            }

            var funcTerm = (FuncTerm)n;
            if (funcTerm.Args.Count != 1)
            {
                type = null;
                return false;
            }

            var func = funcTerm.Function as Id;
            if (func == null ||
                func.Name != FormulaNodes.PtrType_Iden.Node.Name)
            {
                type = null;
                return false;
            }

            type = null;
            using (var it = funcTerm.Args.GetEnumerator())
            {
                it.MoveNext();
                type = it.Current;
            }

            return true;
        }

        private static bool IsArrayType(Node n, out Node type, out BigInteger dimen)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                type = null;
                dimen = -1;
                return false;
            }

            var funcTerm = (FuncTerm)n;
            if (funcTerm.Args.Count != 2)
            {
                type = null;
                dimen = -1;
                return false;
            }

            var func = funcTerm.Function as Id;
            if (func == null ||
                func.Name != FormulaNodes.ArrType_Iden.Node.Name)
            {
                type = null;
                dimen = -1;
                return false;
            }

            Node arg1 = null, arg2 = null;
            using (var it = funcTerm.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            if (arg2.NodeKind == NodeKind.Id)
            {
                if (((Id)arg2).Name == FormulaNodes.Nil_Iden.Node.Name)
                {
                    type = arg1;
                    dimen = -1;
                    return true;
                }
                else
                {
                    type = null;
                    dimen = -1;
                    return false;
                }
            }

            var dimenCnst = arg2 as Cnst;
            if (dimenCnst == null ||
                dimenCnst.CnstKind != CnstKind.Numeric ||
                !dimenCnst.GetNumericValue().IsInteger ||
                dimenCnst.GetNumericValue().Sign == -1)
            {
                type = null;
                dimen = -1;
                return false;
            }

            type = arg1;
            dimen = dimenCnst.GetNumericValue().Numerator;
            return true;
        }

        private static bool IsBaseType(Node n, out string typename)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                typename = null;
                return false;
            }

            var funcTerm = (FuncTerm)n;
            if (funcTerm.Args.Count != 1)
            {
                typename = null;
                return false;
            }

            var func = funcTerm.Function as Id;
            if (func == null ||
                func.Name != FormulaNodes.BaseType_Iden.Node.Name)
            {
                typename = null;
                return false;
            }

            Node arg1 = null;
            using (var it = funcTerm.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
            }

            if (arg1.NodeKind != NodeKind.Id)
            {
                typename = null;
                return false;
            }

            return FrmToBaseType.TryFindValue((Id)arg1, out typename);
        }

        private static bool IsNamedType(Node n, out string tag, out string typename)
        {
            if (n.NodeKind != NodeKind.FuncTerm)
            {
                tag = typename = null;
                return false;
            }

            var funcTerm = (FuncTerm)n;
            if (funcTerm.Args.Count != 2)
            {
                tag = typename = null;
                return false;
            }

            var func = funcTerm.Function as Id;
            if (func == null ||
                func.Name != FormulaNodes.NmdType_Iden.Node.Name)
            {
                tag = typename = null;
                return false;
            }

            Node arg1 = null, arg2 = null;
            using (var it = funcTerm.Args.GetEnumerator())
            {
                it.MoveNext();
                arg1 = it.Current;
                it.MoveNext();
                arg2 = it.Current;
            }

            var tagNode = arg1 as Id;
            if (tagNode == null)
            {
                tag = typename = null;
                return false;
            }
            else if (tagNode.Name == FormulaNodes.union_Iden.Node.Name)
            {
                tag = "union ";
            }
            else if (tagNode.Name == FormulaNodes.enum_Iden.Node.Name)
            {
                tag = "enum ";
            }
            else if (tagNode.Name == FormulaNodes.struct_Iden.Node.Name)
            {
                tag = "struct ";
            }
            else if (tagNode.Name == FormulaNodes.Nil_Iden.Node.Name)
            {
                tag = "";
            }
            else
            {
                tag = typename = null;
                return false;
            }

            var name = arg2 as Cnst;
            if (name == null ||
                name.CnstKind != CnstKind.String ||
                !CRenderer.IsCIdentifier(name.GetStringValue()))
            {
                tag = typename = null;
                return false;
            }

            typename = name.GetStringValue();
            return true;
        }

        private static int CompareId(Id id1, Id id2)
        {
            return string.CompareOrdinal(id1.Name, id2.Name);
        }

        private class PrintData
        {
            public StringBuilder Builder
            {
                get;
                private set;
            }

            public bool IsConstModified
            {
                get;
                set;
            }

            public bool IsVolatileModified
            {
                get;
                set;
            }

            public bool IsModified
            {
                get { return IsConstModified || IsVolatileModified; }
            }

            public string ModifierString
            {
                get
                {
                    if (IsConstModified && !IsVolatileModified)
                    {
                        return "const ";
                    }
                    else if (IsVolatileModified && !IsConstModified)
                    {
                        return "volatile ";
                    }
                    else if (IsConstModified && IsVolatileModified)
                    {
                        return "const volatile ";
                    }
                    else
                    {
                        return "";
                    }
                }
            }

            public bool WasPtrExpression
            {
                get;
                set;
            }

            public PrintData()
            {
                Builder = new StringBuilder();
            }

            public PrintData(PrintData data)
            {
                Builder = data.Builder;
                WasPtrExpression = data.WasPtrExpression;
                IsConstModified = data.IsConstModified;
                IsVolatileModified = data.IsVolatileModified;
            }

            public void ClearModifiers()
            {
                IsConstModified = IsVolatileModified = false;
            }
        }
    }
}
