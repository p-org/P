using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        public string TopLevelCName { get; }

        public NameManager(string topLevelCName, string namePrefix) : base(namePrefix)
        {
            TopLevelCName = topLevelCName;
        }

        /// <summary>
        /// Produces a Java enum value representing a particular State.  This can be
        /// registered with State Builders and used as arguments to `gotoState()`.
        /// </summary>
        /// <param name="s">The state.</param>
        /// <returns>The identifier.</returns>
        public string IdentForState(State s)
        {
            var name = GetNameForDecl(s);
            return Constants.StateEnumName + "." + name;
        }

        /// <summary>
        /// Produces a unique identifier for a NamedTupleType.
        /// </summary>
        /// <param name="t">The NamedTupleType.</param>
        /// <returns>A consistent but distinct identifier for `t`.</returns>
        internal string NameForNamedTuple(NamedTupleType t)
        {
            if (!_namedTupleJTypes.TryGetValue(t, out var val))
            {
                var names = t.Names;
                names = names.Select(AbbreviateTupleName);
                val = UniquifyName("PTuple_" + string.Join("_", names));
                _namedTupleJTypes.Add(t, val);
            }

            return val;
        }

        /// <summary>
        /// Attempts to simplify the given tuple name while still leaving it somewhat readable.
        /// </summary>
        /// <param name="name">The token to simplify</param>
        /// <returns>The simplified token.</returns>
        private static string AbbreviateTupleName(string name)
        {
            // Short names are already as succinct as we can reasonably expect.
            if (name.Length <= 5)
            {
                return name;
            }

            // Is it a superLongCamelCaseFieldName?  Abbreviate it that way.
            if (name.Count(char.IsUpper) >= 3)
            {
                return String.Concat(name.Where(char.IsUpper)).ToLower();
            }

            // Strip and simplify some common prefixes and suffixes.
            name = Regex.Replace(name, "^is([A-Z].*)", "$1");
            name = Regex.Replace(name, "(.*)Id$", "$1");
            name = Regex.Replace(name, "(.*)Val$", "$1");
            if (name.Length <= 5)
            {
                return name;
            }

            // If not, strip the vowels in the middle of a word out so it still seems pronounceable at a distance.
            name = name.Substring(0, 1) + Regex.Replace(name.Substring(1), "[aeiou]", "");
            if (name.Length <= 5)
            {
                return name.ToLower();
            }

            return name.Substring(0, 5).ToLower();
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

            // If the name matches a reserved word (either a string defined in the Constants class or the top-level
            // compilation job), pre-uniquify it so there is no collision during Java compilation.
            if (name == TopLevelCName || Constants.IsReserved(name))
            {
                //No need to use the fully-qualified typename, just grab the innermost class name for this.
                var tname = decl.GetType().ToString().Split(".").Last();
                name = $"{name}_{tname}";
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
            return $"{machineName}";
        }
    }
}