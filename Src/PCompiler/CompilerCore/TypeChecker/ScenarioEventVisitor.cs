using Antlr4.Runtime.Tree;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;

namespace Plang.Compiler.TypeChecker;

public class ScenarioEventVisitor
{

        public static void PopulateEventTypes(Function function)
        {
                if (function.Role != FunctionRole.Scenario) return;
                for (var i = 0; i < function.Signature.Parameters.Count; i++)
                {
                        var v = function.Signature.Parameters[i];
                        var e = function.Signature.ParameterEvents[i];
                        v.Type = e.PayloadType;
                }
        }
}