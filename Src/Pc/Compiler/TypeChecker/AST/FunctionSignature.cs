using System.Collections.Generic;
using System.Linq;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.TypeChecker.AST
{
    public class FunctionSignature
    {
        public List<Variable> Parameters { get; } = new List<Variable>();
        public IEnumerable<PLanguageType> ParameterTypes => Parameters.Select(ty => ty.Type);
        public PLanguageType ReturnType { get; set; } = PrimitiveType.Null;
    }
}
