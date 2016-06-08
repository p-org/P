namespace ZingParser
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    using Microsoft.Formula.API;
    using Microsoft.Formula.API.Nodes;
    using Microsoft.Formula.API.Plugins;
    using Microsoft.Formula.Compiler;

    public class Parser : IQuoteParser
    {
        public string Description
        {
            get { return "A renderer for Zing"; }
        }

        public string UnquotePrefix
        {
            get { throw new NotImplementedException(); }
        }
        public AST<Domain> SuggestedDataModel
        {
            get { throw new NotImplementedException(); }
        }
        public IEnumerable<Tuple<string, CnstKind, string>> SuggestedSettings
        {
            get { yield break; }
        }
        public IQuoteParser CreateInstance(
                            AST<Node> module,
                            string collectionName,
                            string instanceName)
        {
            return new Parser();
        }

        public bool Parse(
                Configuration config,
                Stream quoteStream,
                SourcePositioner positioner,
                out AST<Node> results,
                out List<Flag> flags)
        {
            throw new NotImplementedException();
        }


        public bool Render(
                Configuration config,
                TextWriter writer,
                AST<Node> ast,
                out List<Flag> flags)
        {
            FuncTerm term = (FuncTerm)ast.Node;
            writer.Write("File(");
            using (var enumerator = term.Args.GetEnumerator())
            {
                enumerator.MoveNext();
                writer.Write(string.Format("\"{0}\", ", ((Cnst)enumerator.Current).GetStringValue()));
                enumerator.MoveNext();
                writer.Write("`");
                RenderDecls(enumerator.Current, writer);
                writer.Write("`");
            }
            writer.Write(")\n");
            flags = null;
            return true;
        }

        void RenderDecls(Node node, TextWriter writer)
        {
            while (true)
            {
                if (node.NodeKind == NodeKind.Id)
                    break;
                FuncTerm term = (FuncTerm)node;
                using (var enumerator = term.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    FuncTerm decl = (FuncTerm)enumerator.Current;
                    Id declId = (Id)decl.Function;
                    if (declId.Name == "ArrayDecl")
                    {
                        RenderArrayDecl(decl, writer);
                    }
                    else if (declId.Name == "SetDecl")
                    {
                        RenderSetDecl(decl, writer);
                    }
                    else if (declId.Name == "EnumDecl")
                    {
                        RenderEnumDecl(decl, writer);
                    }
                    else if (declId.Name == "ClassDecl")
                    {
                        RenderClassDecl(decl, writer);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                    enumerator.MoveNext();
                    node = enumerator.Current;
                }
            }
        }

        void RenderArrayDecl(FuncTerm term, TextWriter writer)
        {
            string arrName = null;
            Node innerT = null;
            using (var it = term.Args.GetEnumerator())
            {
                it.MoveNext();
                arrName = ((Cnst)it.Current).GetStringValue();
                it.MoveNext();
                innerT = it.Current;
                Debug.Assert(!it.MoveNext());
            }

            writer.Write("array " + arrName + "[] " + TypeName(innerT) + ";\n\n");
        }

        void RenderSetDecl(FuncTerm term, TextWriter writer)
        {
            string setName = null;
            Node innerT = null;
            using (var it = term.Args.GetEnumerator())
            {
                it.MoveNext();
                setName = ((Cnst)it.Current).GetStringValue();
                it.MoveNext();
                innerT = it.Current;
                Debug.Assert(!it.MoveNext());
            }

            writer.Write("set " + setName + " " + TypeName(innerT) + ";\n\n");
        }

        void RenderEnumDecl(FuncTerm term, TextWriter writer)
        {
            string enumDeclName;
            Node enumElems;
            using (var enumerator = term.Args.GetEnumerator())
            {
                enumerator.MoveNext();
                enumDeclName = ((Cnst)enumerator.Current).GetStringValue();
                enumerator.MoveNext();
                enumElems = enumerator.Current;
                Debug.Assert(!enumerator.MoveNext());
            }
            writer.Write("enum " + enumDeclName + " {");
            bool first = true;
            while (true)
            {
                Id nilConst = enumElems as Id;
                if (nilConst != null)
                {
                    Debug.Assert(nilConst.Name == "NIL");
                    break;
                }
                if (first)
                    first = false;
                else
                    writer.Write(", ");
                term = (FuncTerm)enumElems;
                using (var enumerator = term.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    string enumElemName = ((Cnst)enumerator.Current).GetStringValue();
                    writer.Write(enumElemName);
                    enumerator.MoveNext();
                    enumElems = enumerator.Current;
                }
            }
            writer.Write("};\n\n");
        }

        void RenderClassDecl(FuncTerm term, TextWriter writer)
        {
            string classDeclName;
            Node fieldDecls;
            Node methodDecls;
            using (var enumerator = term.Args.GetEnumerator())
            {
                enumerator.MoveNext();
                classDeclName = ((Cnst)enumerator.Current).GetStringValue();
                enumerator.MoveNext();
                fieldDecls = enumerator.Current;
                enumerator.MoveNext();
                methodDecls = enumerator.Current;
            }

            writer.Write("class " + classDeclName + " {\n");
            while (true) 
            {
                if (fieldDecls.NodeKind == NodeKind.Id)
                    break;
                FuncTerm ft =  (FuncTerm)fieldDecls;
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    RenderVarDecl((FuncTerm)enumerator.Current, writer);
                    writer.Write(";\n");
                    enumerator.MoveNext();
                    fieldDecls = enumerator.Current;
                }
            }
            while (true)
            {
                if (methodDecls.NodeKind == NodeKind.Id)
                    break;
                FuncTerm ft = (FuncTerm)methodDecls;
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    RenderMethodDecl((FuncTerm)enumerator.Current, writer);
                    enumerator.MoveNext();
                    methodDecls = enumerator.Current;
                }
            }
            writer.Write("};\n\n");
        }

        string RenderAttrs(Node n)
        {
            string ret = "";
            var iter = n;
            while (true)
            {
                if (iter.NodeKind == NodeKind.Id)
                    break;
                FuncTerm ft = (FuncTerm)iter;
                using (var it = ft.Args.GetEnumerator())
                {
                    it.MoveNext();
                    var attrName = ((Id)it.Current).Name;
                    if (attrName == "ASYNC")
                        ret = ret + "async ";
                    else if (attrName == "ACTIVATE")
                        ret = ret + "activate ";
                    else if (attrName == "STATIC")
                        ret = ret + "static ";
                    else
                        throw new NotImplementedException();
                    it.MoveNext();
                    iter = it.Current;
                }
            }
            return ret;
        }

        void RenderVarDecl(FuncTerm term, TextWriter writer)
        {
            using (var enumerator = term.Args.GetEnumerator())
            {
                enumerator.MoveNext();
                string varName = ((Cnst)enumerator.Current).GetStringValue();
                enumerator.MoveNext();
                Node typeDecl = enumerator.Current;
                enumerator.MoveNext();
                
                writer.Write(string.Format("{0}{1} {2}", RenderAttrs(enumerator.Current), TypeName(typeDecl), varName));
            }
        }

        string TypeName(Node typeDecl)
        {
            string typeName;
            if (typeDecl.NodeKind == NodeKind.Cnst)
                typeName = ((Cnst)typeDecl).GetStringValue();
            else
            {
                Id id = (Id)typeDecl;
                if (id.Name == "BOOL")
                    typeName = "bool";
                else if (id.Name == "INT")
                    typeName = "int";
                else if (id.Name == "VOID")
                    typeName = "void";
                else
                    throw new NotImplementedException();
            }
            return typeName;
        }

        void RenderMethodDecl(FuncTerm term, TextWriter writer)
        {
            string methodName;
            Node parameterDecls;
            Node returnType;
            Node localVarDecls;
            Node body;
            Node attrs;

            using (var enumerator = term.Args.GetEnumerator())
            {
                enumerator.MoveNext();
                methodName = ((Cnst)enumerator.Current).GetStringValue();
                enumerator.MoveNext();
                parameterDecls = enumerator.Current;
                enumerator.MoveNext();
                returnType = enumerator.Current;
                enumerator.MoveNext();
                localVarDecls = enumerator.Current;
                enumerator.MoveNext();
                body = enumerator.Current;
                enumerator.MoveNext();
                attrs = enumerator.Current;
            }

            writer.Write("{0}{1} {2}(", RenderAttrs(attrs), TypeName(returnType), methodName);
            bool first = true;
            while (true)
            {
                if (parameterDecls.NodeKind == NodeKind.Id)
                    break;
                if (first)
                    first = false;
                else
                    writer.Write(", ");
                FuncTerm ft = (FuncTerm)parameterDecls;
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    RenderVarDecl((FuncTerm)enumerator.Current, writer);
                    enumerator.MoveNext();
                    parameterDecls = enumerator.Current;
                }
            }
            writer.Write(") {\n");
            while (true)
            {
                if (localVarDecls.NodeKind == NodeKind.Id)
                    break;
                FuncTerm ft = (FuncTerm)localVarDecls;
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    RenderVarDecl((FuncTerm)enumerator.Current, writer);
                    writer.Write(";\n");
                    enumerator.MoveNext();
                    localVarDecls = enumerator.Current;
                }
            }
            RenderBody(body, writer);
            writer.Write("}\n\n");
        }

        void RenderBody(Node body, TextWriter writer)
        {
            if (body.NodeKind == NodeKind.Id)
                return;
            FuncTerm ft = (FuncTerm)body;
            while (true)
            {
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    RenderLabelStmt((FuncTerm)enumerator.Current, writer);
                    writer.Write("\n");
                    enumerator.MoveNext();
                    body = enumerator.Current;
                }
                if (body.NodeKind == NodeKind.Id)
                    break;
                ft = (FuncTerm)body;
            }
        }

        void RenderLabelStmt(FuncTerm ft, TextWriter writer)
        {
            using (var enumerator = ft.Args.GetEnumerator())
            {
                enumerator.MoveNext();
                writer.Write("\n{0}:\n", ((Cnst)enumerator.Current).GetStringValue());
                enumerator.MoveNext();
                writer.Write(RenderStmt(enumerator.Current));
                writer.Write("\n;\n");
            }
        }

        string RenderStmt(Node node)
        {
            if (node.NodeKind == NodeKind.Id)
            {
                Id id = (Id)node;
                if (id.Name == "YIELD")
                    return "yield;\n";
                else if (id.Name == "NIL")
                    return "";
                else
                    throw new NotImplementedException();
            }
            FuncTerm ft = (FuncTerm)node;
            string functionName = ((Id)ft.Function).Name;
            if (functionName == "Return")
            {
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    if (enumerator.Current.NodeKind == NodeKind.Id && ((Id)enumerator.Current).Name == "NIL")
                        return string.Format("return;\n");
                    else
                        return string.Format("return {0};\n", RenderExpr(enumerator.Current));
                }
            } 
            else if (functionName == "Assert")
            {
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    var expr = RenderExpr(enumerator.Current);
                    enumerator.MoveNext();
                    if (enumerator.Current.NodeKind == NodeKind.Cnst)
                    {
                        return string.Format("assert({0}, {1});\n", expr, ((Cnst)enumerator.Current).GetStringValue());
                    }
                    else
                    {
                        return string.Format("assert({0});\n", expr);
                    }
                }
            }
            else if (functionName == "Assume")
            {
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    return string.Format("assume({0});\n", RenderExpr(enumerator.Current));
                }
            }
            else if (functionName == "CallStmt")
            {
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    var callExpr = enumerator.Current;
                    enumerator.MoveNext();
                    var attrs = enumerator.Current;
                    return string.Format("{0}{1};\n", RenderAttrs(attrs), RenderExpr(callExpr));
                }
            }
            else if (functionName == "Assign")
            {
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    var lhs = RenderExpr(enumerator.Current);
                    enumerator.MoveNext();
                    var rhs = RenderExpr(enumerator.Current);
                    return string.Format("{0} = {1};\n", lhs, rhs);
                }
            }
            else if (functionName == "ITE")
            {
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    var condExpr = RenderExpr(enumerator.Current);
                    enumerator.MoveNext();
                    var thenStmt = RenderStmt(enumerator.Current);
                    enumerator.MoveNext();
                    var elseStmt = RenderStmt(enumerator.Current);
                    if (elseStmt != string.Empty)
                    {
                        return string.Format("if ({0}) {{\n{1}\n}} else {{\n{2}\n}}\n", condExpr, thenStmt, elseStmt);
                    }
                    else
                    {
                        return string.Format("if ({0}) {{\n{1}\n}}\n", condExpr, thenStmt);       
                    }

                }
            }
            else if (functionName == "While")
            {
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    var condExpr = RenderExpr(enumerator.Current);
                    enumerator.MoveNext();
                    var bodyStmt = RenderStmt(enumerator.Current);
                    return string.Format("while ({0}) {{\n{1}\n}}\n", condExpr, bodyStmt);
                }
            }
            else if (functionName == "Foreach")
            {
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    var typeExpr = enumerator.Current;
                    enumerator.MoveNext();
                    var iterExpr = ((Cnst)enumerator.Current).GetStringValue();
                    enumerator.MoveNext();
                    var setExpr = RenderExpr(enumerator.Current);
                    enumerator.MoveNext();
                    var bodyStmt = RenderStmt(enumerator.Current);
                    return string.Format("foreach ({0} {1} in {2}) {{\n{3}\n}}\n", TypeName(typeExpr), iterExpr, setExpr, bodyStmt);
                }
            }
            else if (functionName == "Seq")
            {
                string retval = "";
                Stack<Node> dfsStack = new Stack<Node>();
                dfsStack.Push(node);
                while (dfsStack.Count > 0)
                {
                    Node x = dfsStack.Pop();
                    FuncTerm f = x as FuncTerm;
                    if (f == null)
                    {
                        retval += RenderStmt(x);
                        continue;
                    }
                    if (((Id)f.Function).Name != "Seq")
                    {
                        retval += RenderStmt(x);
                        continue;
                    }
                    using (var enumerator = f.Args.GetEnumerator())
                    {
                        enumerator.MoveNext();
                        var first = enumerator.Current;
                        enumerator.MoveNext();
                        var second = enumerator.Current;
                        dfsStack.Push(second);
                        dfsStack.Push(first);
                    }
                }
                return retval;
            }
            else if (functionName == "Goto")
            {
                using (var enumerator = ft.Args.GetEnumerator())
                {
                    enumerator.MoveNext();
                    return string.Format("goto {0};", ((Cnst)enumerator.Current).GetStringValue());
                }
            }
            else if (functionName == "LabelStmt")
            {
                StringWriter wr = new StringWriter();
                RenderLabelStmt(ft, wr);
                return wr.ToString();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        string RenderExpr(Node n)
        {
            if (n.NodeKind == NodeKind.Cnst)
            {
                Cnst cnst = (Cnst)n;
                if (cnst.CnstKind == CnstKind.Numeric)
                    return cnst.GetNumericValue().ToString();
                else
                    return cnst.GetStringValue();
            }
            else if (n.NodeKind == NodeKind.Id)
            {
                Id id = (Id)n;
                if (id.Name == "TRUE")
                    return "true";
                else if (id.Name == "FALSE")
                    return "false";
                else
                    throw new NotImplementedException();
            } 
            else
            {
                FuncTerm ft = (FuncTerm)n;
                string functionName = ((Id)ft.Function).Name;
                if (functionName == "Identifier")
                {
                    using (var enumerator = ft.Args.GetEnumerator())
                    {
                        enumerator.MoveNext();
                        return ((Cnst)enumerator.Current).GetStringValue();
                    }
                }
                else if (functionName == "Call")
                {
                    List<string> args = new List<string>();
                    using (var enumerator = ft.Args.GetEnumerator())
                    {
                        enumerator.MoveNext();
                        RenderArgs((FuncTerm)enumerator.Current, args);
                    }
                    string ret = args[0] + "(";
                    bool first = true;
                    for (int i = 1; i < args.Count; i++)
                    {
                        if (first)
                            first = false;
                        else
                            ret = ret + ", ";
                        ret = ret + args[i];
                    }
                    return ret + ")";
                }
                else if (functionName == "New")
                {
                    using (var enumerator = ft.Args.GetEnumerator())
                    {
                        enumerator.MoveNext();
                        Cnst type = (Cnst)enumerator.Current;
                        enumerator.MoveNext();
                        Node sizeExpr = enumerator.Current;
                        if (sizeExpr.NodeKind == NodeKind.Id && ((Id)sizeExpr).Name == "NIL")
                        {
                            return string.Format("new {0}", type.GetStringValue());
                        }
                        else 
                        {
                            return string.Format("new {0}[{1}]", type.GetStringValue(), RenderExpr(sizeExpr));
                        }
                    }
                } 
                else
                {
                    using (var enumerator = ft.Args.GetEnumerator())
                    {
                        enumerator.MoveNext();
                        string opName = ((Id)enumerator.Current).Name;
                        List<string> args = new List<string>();
                        enumerator.MoveNext();
                        RenderArgs((FuncTerm)enumerator.Current, args);
                        switch (opName) {
                            case "NOT": return string.Format("!{0}", args[0]);
                            case "NEG": return string.Format("-{0}", args[0]);
                            case "ADD": return string.Format("({0} + {1})", args[0], args[1]);
                            case "SUB": return string.Format("({0} - {1})", args[0], args[1]);
                            case "MUL": return string.Format("({0} * {1})", args[0], args[1]);
                            case "INTDIV": return string.Format("({0} / {1})", args[0], args[1]);
                            case "AND": return string.Format("({0} && {1})", args[0], args[1]);
                            case "OR": return string.Format("({0} || {1})", args[0], args[1]);
                            case "EQ": return string.Format("({0} == {1})", args[0], args[1]);
                            case "NEQ": return string.Format("({0} != {1})", args[0], args[1]);
                            case "LT": return string.Format("({0} < {1})", args[0], args[1]);
                            case "LE": return string.Format("({0} <= {1})", args[0], args[1]);
                            case "GT": return string.Format("({0} > {1})", args[0], args[1]);
                            case "GE": return string.Format("({0} >= {1})", args[0], args[1]);
                            case "DOT": return string.Format("{0}.{1}", args[0], args[1]);
                            case "IN": return string.Format("({0} in {1})", args[0], args[1]);
                            case "INDEX": return string.Format("{0}[{1}]", args[0], args[1]);
                            default: throw new NotImplementedException();
                        }
                    }
                }
            }
        }

        void RenderArgs(FuncTerm ft, List<string> args)
        {
            using (var enumerator = ft.Args.GetEnumerator())
            {
                enumerator.MoveNext();
                args.Add(RenderExpr(enumerator.Current));
                enumerator.MoveNext();
                if (enumerator.Current.NodeKind == NodeKind.Id)
                    return;
                RenderArgs((FuncTerm)enumerator.Current, args);
            }
        }
    }
}
