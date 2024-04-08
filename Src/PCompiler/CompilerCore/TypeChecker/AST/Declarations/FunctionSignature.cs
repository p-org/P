using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class FunctionSignature
    {
        public List<Variable> Parameters { get; } = new List<Variable>();
        // This is only used by scenarios.
        public List<PEvent> ParameterEvents { get; } = new();
        public IEnumerable<PLanguageType> ParameterTypes => Parameters.Select(ty => ty.Type);
        public PLanguageType ReturnType { get; set; } = PrimitiveType.Null;
    }
}