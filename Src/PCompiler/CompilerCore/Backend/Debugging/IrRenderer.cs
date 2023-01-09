using System.Collections.Generic;
using System.Text;
using Plang.Compiler.TypeChecker;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Debugging
{
    public abstract class IrRenderer
    {
        private readonly StringBuilder writer = new StringBuilder();
        private int depth;

        protected string Padding { get; private set; }

        /*
        public static void InferCreates(Scope scope)
        {
            foreach(var machine in scope.Machines)
            {
                var createdMachines = new HashSet<Value>();
                foreach(var method in machine.Methods)
                {
                    createdMachines.UnionWith(GatherMachines(method.Body));
                }
                machine.Creates = createdMachines;
            }
        }
        */

        public string Render(Scope scope)
        {
            foreach (var decl in scope.AllDecls)
            {
                WriteDecl(decl);
            }

            return writer.ToString();
        }

        protected void Indent()
        {
            Padding = new string(' ', ++depth * 4);
        }

        protected void Dedent()
        {
            Padding = new string(' ', --depth * 4);
        }

        protected void WriteParts(string part)
        {
            writer.Append(part);
        }

        protected void WriteParts(params string[] parts)
        {
            foreach (var part in parts)
            {
                writer.Append(part);
            }
        }

        protected void WriteParts(params object[] parts)
        {
            foreach (var part in parts)
            {
                switch (part)
                {
                    case IPExpr expr:
                        WriteExpr(expr);
                        break;

                    case IEnumerable<IPExpr> exprs:
                        WriteExprList(exprs);
                        break;

                    case IEnumerable<string> strs:
                        WriteStringList(strs);
                        break;

                    case IPDecl decl:
                        WriteDeclRef(decl);
                        break;

                    case PLanguageType type:
                        WriteTypeRef(type);
                        break;

                    default:
                        writer.Append(part);
                        break;
                }
            }
        }

        protected abstract void WriteDecl(IPDecl decl);

        protected abstract void WriteExpr(IPExpr expr);

        protected abstract void WriteTypeRef(PLanguageType type);

        protected abstract void WriteDeclRef(IPDecl decl);

        protected abstract void WriteStringList(IEnumerable<string> strs);

        protected abstract void WriteExprList(IEnumerable<IPExpr> exprs);
    }
}