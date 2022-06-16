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

        /// <summary>
        /// Produces the class name for the foreign function bridge class for a given machine.
        ///
        /// In the C# code generator, partial classes are used to automatically weave together
        /// the machine class and its foreign functions.  For instance, a machine called Client
        /// that relies on foreign functions would have the latter implemented in a partial class
        /// also called Client.  The C# compiler would then merge the two together into a final
        /// class definition.
        ///
        /// Java does not have this language feature, unfortunately, so we need a different approach:
        /// we need foreign function writers to implement foreign functions as static methods
        /// in a class whose name is derived from the monitor class.  The naming convention is
        /// specified by the return value of this function.
        ///
        /// <see url="https://en.wikipedia.org/wiki/Bridge_pattern"/>
        /// </summary>
        /// <param name="machineName"></param>
        /// <returns></returns>
        public string FFIBridgeForMachine(string machineName)
        {
            return $"{machineName}FFI";
        }
    }
}
