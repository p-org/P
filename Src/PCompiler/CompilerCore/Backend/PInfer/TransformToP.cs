using System.Linq;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Expressions;

namespace Plang.Compiler.Backend.PInfer
{
    public class Transform
    {
        public string toP(IPExpr expr)
        {
            switch (expr)
            {
                case VariableAccessExpr varAccess: return $"{varAccess.Variable.Name}";
                case NamedTupleAccessExpr ntAccess:
                {
                    if (ntAccess.SubExpr is VariableAccessExpr && ntAccess.FieldName == "payload")
                    {
                        return toP(ntAccess.SubExpr);
                    }
                    else
                    {
                        return $"{toP(ntAccess.SubExpr)}.{ntAccess.FieldName}";
                    }
                }
                case TupleAccessExpr tAccess: return $"{toP(tAccess.SubExpr)}[{tAccess.FieldNo}]";
                case EnumElemRefExpr enumRef: return $"{enumRef.Value.Name}";
                case IntLiteralExpr intLit: return $"{intLit.Value}";
                case BoolLiteralExpr boolLit: return $"{boolLit.Value}";
                case FloatLiteralExpr floatLit: return $"{floatLit.Value}";
                case FunCallExpr funCall: return $"{funCall.Function.Name}({string.Join(", ", funCall.Arguments.Select(toP))})";
                case UnaryOpExpr unOpExpr:
                {
                    string op = unOpExpr.Operation switch
                    {
                        UnaryOpType.Negate => "-",
                        UnaryOpType.Not => "!",
                        _ => throw new System.Exception("Unknown unary operator")
                    };
                    return $"{op}({toP(unOpExpr.SubExpr)})";
                }
                case BinOpExpr binOpExpr:
                {
                    string op = binOpExpr.Operation switch
                    {
                        BinOpType.Add => "+",
                        BinOpType.Sub => "-",
                        BinOpType.Mul => "*",
                        BinOpType.Div => "/",
                        BinOpType.Mod => "%",
                        BinOpType.And => "&&",
                        BinOpType.Or => "||",
                        BinOpType.Eq => "==",
                        BinOpType.Neq => "!=",
                        BinOpType.Lt => "<",
                        BinOpType.Le => "<=",
                        BinOpType.Gt => ">",
                        BinOpType.Ge => ">=",
                        _ => throw new System.Exception("Unknown binary operator")
                    };
                    return $"({toP(binOpExpr.Lhs)} {op} {toP(binOpExpr.Rhs)})";
                }
                case SizeofExpr sizeofExpr: return $"sizeof({toP(sizeofExpr.Expr)})";
            }
            return null;
        }
    }
}