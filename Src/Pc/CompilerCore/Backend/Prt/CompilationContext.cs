using Plang.Compiler.TypeChecker.AST.Declarations;
using Plang.Compiler.TypeChecker.AST.States;
using Plang.Compiler.TypeChecker.Types;
using System.Collections.Generic;

namespace Plang.Compiler.Backend.Prt
{
    public class CompilationContext : CompilationContextBase
    {
        private readonly Dictionary<Interface, int> interfaceNumbering = new Dictionary<Interface, int>();
        private readonly Dictionary<Machine, int> machineNumbering = new Dictionary<Machine, int>();

        private readonly ValueInternmentManager<bool> registeredBools;
        private readonly ValueInternmentManager<double> registeredFloats;
        private readonly ValueInternmentManager<int> registeredInts;
        private readonly ValueInternmentManager<string> registeredStrings;

        private readonly Dictionary<Machine, Dictionary<State, int>> stateNumbering =
            new Dictionary<Machine, Dictionary<State, int>>();

        private readonly Dictionary<PEvent, int> userEventNumbering = new Dictionary<PEvent, int>();

        public CompilationContext(ICompilationJob job) : base(job)
        {
            Names = new PrtNameManager("P_");
            HeaderFileName = $"{job.ProjectName}.h";
            SourceFileName = $"{job.ProjectName}.c";
            registeredInts = new ValueInternmentManager<int>(Names);
            registeredFloats = new ValueInternmentManager<double>(Names);
            registeredBools = new ValueInternmentManager<bool>(Names);
            registeredStrings = new ValueInternmentManager<string>(Names);
        }

        public PrtNameManager Names { get; }

        public string HeaderFileName { get; }
        public string SourceFileName { get; }
        public IEnumerable<PLanguageType> UsedTypes => Names.UsedTypes;
        public HashSet<PLanguageType> WrittenTypes { get; } = new HashSet<PLanguageType>();

        public string RegisterLiteral(Function function, int value)
        {
            return registeredInts.RegisterValue(function, value);
        }

        public IEnumerable<KeyValuePair<int, string>> GetRegisteredIntLiterals(Function function)
        {
            return registeredInts.GetValues(function);
        }

        internal string RegisterLiteral(Function function, double value)
        {
            return registeredFloats.RegisterValue(function, value);
        }

        public IEnumerable<KeyValuePair<double, string>> GetRegisteredFloatLiterals(Function function)
        {
            return registeredFloats.GetValues(function);
        }

        public string RegisterLiteral(Function function, bool value)
        {
            return registeredBools.RegisterValue(function, value);
        }

        public string RegisterLiteral(Function function, string value)
        {
            return registeredStrings.RegisterValue(function, value);
        }

        public IEnumerable<KeyValuePair<string, string>> GetRegisteredStringLiterals(Function function)
        {
            return registeredStrings.GetValues(function);
        }

        public IEnumerable<KeyValuePair<bool, string>> GetRegisteredBoolLiterals(Function function)
        {
            return registeredBools.GetValues(function);
        }

        #region Numbering helpers

        private static int GetOrAddNumber<T>(IDictionary<T, int> dict, T declaration)
        {
            if (dict.TryGetValue(declaration, out int number))
            {
                return number;
            }

            number = dict.Count;
            dict.Add(declaration, number);
            return number;
        }

        public int GetDeclNumber(Interface pInterface)
        {
            return GetOrAddNumber(interfaceNumbering, pInterface);
        }

        public int GetDeclNumber(Machine machine)
        {
            return GetOrAddNumber(machineNumbering, machine);
        }

        public int GetDeclNumber(PEvent ev)
        {
            if (ev.IsNullEvent)
            {
                return 0;
            }

            if (ev.IsHaltEvent)
            {
                return 1;
            }

            // There are two built-in events, which have predetermined numbers.
            // User-defined events have a minimum id of 2, but GetOrAddNumber
            // assigns sequentially from 0.
            return 2 + GetOrAddNumber(userEventNumbering, ev);
        }

        public int GetDeclNumber(State state)
        {
            Machine machine = state.OwningMachine;
            if (!stateNumbering.TryGetValue(machine, out Dictionary<State, int> internalNumbering))
            {
                internalNumbering = new Dictionary<State, int>();
                stateNumbering.Add(machine, internalNumbering);
            }

            return GetOrAddNumber(internalNumbering, state);
        }

        #endregion Numbering helpers
    }
}