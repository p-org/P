using Plang.Compiler.TypeChecker.Types;
using System.Collections.Generic;
using System.Linq;

namespace Plang.Compiler.TypeChecker.AST.Declarations
{
    public class FunctionSignature
    {
        public List<Variable> Parameters { get; } = new List<Variable>();
        public IEnumerable<PLanguageType> ParameterTypes => Parameters.Select(ty => ty.Type);
        public PLanguageType ReturnType { get; set; } = PrimitiveType.Null;
    }
}