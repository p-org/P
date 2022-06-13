using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.AST;
using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Java
{
    public class NameManager : NameManagerBase
    {
        // Maps the NamedTuple to some generated Java class name;
        private readonly Dictionary<NamedTupleType, string> _namedTupleJTypes = new Dictionary<NamedTupleType, string>();

        public NameManager(string namePrefix) : base(namePrefix)
        {
        }

        /// <summary>
        /// Produces a Java String value representing a particular State.  This can be
        /// registered with State Builders and used as arguments to `gotoState()`.
        /// </summary>
        /// <param name="s">The state.</param>
        /// <returns>The identifier.</returns>
        public string IdentForState(State s)
        {
            return $"{s.Name.ToUpper()}_STATE";
        }

        /// <summary>
        /// Produces a unique identifier for a NamedTupleType.
        /// </summary>
        /// <param name="t">The NamedTupleType.</param>
        /// <returns>A consistent but distinct identifier for `t`.</returns>
        internal string NameForNamedTuple(NamedTupleType t)
        {
            string val;
            if (!_namedTupleJTypes.TryGetValue(t, out val))
            {
                IEnumerable<string> names = t.Names;
                if (names.Count() > 5)
                {
                    names = names.Select(f => f.Substring(0, 3)).ToArray();
                }
                val = UniquifyName("PTuple_" + string.Join("_", names));
                _namedTupleJTypes.Add(t, val);
            }

            return val;
        }

        protected override string ComputeNameForDecl(IPDecl decl)
        {
            string name;

            switch (decl)
            {
                case PEvent { IsNullEvent: true }:
                    return "DefaultEvent";
                case PEvent { IsHaltEvent: true }:
                    return "PHalt";
                case Interface i:
                    name = "I_" + i.Name;
                    break;
                default:
                    name = decl.Name;
                    break;
            }

            if (string.IsNullOrEmpty(name))
            {
                name = "Anon";
            }

            if (name.StartsWith("$"))
            {
                name = "TMP_" + name.Substring(1);
            }

            return UniquifyName(name);
        }
    }
}
