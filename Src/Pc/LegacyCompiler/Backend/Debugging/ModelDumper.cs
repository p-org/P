using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Pc.TypeChecker;
using Microsoft.Pc.TypeChecker.AST;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend.Debugging
{
    internal class ModelDumper : IrRenderer
    {
        private string Name(IPDecl decl)
        {
            return string.IsNullOrEmpty(decl.Name) ? "$anonymous" : decl.Name;
        }

        protected override void WriteDecl(IPDecl decl)
        {
            switch (decl)
            {
                case EnumElem enumElem:
                    WriteParts("{ \"type\": \"decl:enumelem\", \"name\": \"", Name(enumElem), "\", \"value\": ", enumElem.Value, " }");
                    break;
                case Function function:
                    WriteParts("{ \"type\": \"decl:function\", \"name\": \"", Name(function));
                    break;
                case Interface @interface:
                    break;
                case Machine machine:
                    break;
                case NamedEventSet namedEventSet:
                    break;
                case PEnum pEnum:
                    break;
                case PEvent pEvent:
                    break;
                case TypeDef typeDef:
                    break;
                case Variable variable:
                    break;
                case State state:
                    break;
                case StateGroup stateGroup:
                    break;
            }
        }

        protected override void WriteExpr(IPExpr expr) { throw new NotImplementedException(); }

        protected override void WriteTypeRef(PLanguageType type) { throw new NotImplementedException(); }

        protected override void WriteDeclRef(IPDecl decl) { throw new NotImplementedException(); }

        protected override void WriteStringList(IEnumerable<string> strs) { throw new NotImplementedException(); }

        protected override void WriteExprList(IEnumerable<IPExpr> exprs) { throw new NotImplementedException(); }
    }
}
