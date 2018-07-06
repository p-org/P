using System.Collections.Generic;
using Microsoft.Pc.TypeChecker;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend.Prt
{

    public class CompilationContext : CompilationContextBase
    {
        private readonly Dictionary<Interface, int> interfaceNumbering = new Dictionary<Interface, int>();
        private readonly Dictionary<Machine, int> machineNumbering = new Dictionary<Machine, int>();
        private readonly Dictionary<Machine, Dictionary<State, int>> stateNumbering = new Dictionary<Machine, Dictionary<State, int>>();

        private readonly ValueInternmentManager<bool> registeredBools;
        private readonly ValueInternmentManager<double> registeredFloats;
        private readonly ValueInternmentManager<int> registeredInts;

        public CompilationContext(ICompilationJob job) : base(job)
        {
            string projectName = job.ProjectName;
            Names = new PrtNameManager($"P_{projectName.ToUpperInvariant()}_");
            HeaderFileName = $"{projectName}.h";
            SourceFileName = $"{projectName}.c";
            registeredInts = new ValueInternmentManager<int>(Names);
            registeredFloats = new ValueInternmentManager<double>(Names);
            registeredBools = new ValueInternmentManager<bool>(Names);
        }

        public PrtNameManager Names { get; }

        public string HeaderFileName { get; }
        public string SourceFileName { get; }
        public IEnumerable<PLanguageType> UsedTypes => Names.UsedTypes;
        public HashSet<PLanguageType> WrittenTypes { get; } = new HashSet<PLanguageType>();

        public int GetNumberForInterface(Interface pInterface)
        {
            if (interfaceNumbering.TryGetValue(pInterface, out int name))
            {
                return name;
            }

            name = interfaceNumbering.Count;
            interfaceNumbering.Add(pInterface, name);
            return name;
        }

        public int GetNumberForMachine(Machine machine)
        {
            if (machineNumbering.TryGetValue(machine, out int name))
            {
                return name;
            }

            name = machineNumbering.Count;
            machineNumbering.Add(machine, name);
            return name;
        }

        public int GetNumberForState(State state)
        {
            Machine machine = state.OwningMachine;
            if (!stateNumbering.TryGetValue(machine, out var internalNumbering))
            {
                internalNumbering = new Dictionary<State, int>();
                stateNumbering.Add(machine, internalNumbering);
            }

            if (internalNumbering.TryGetValue(state, out int name))
            {
                return name;
            }

            name = internalNumbering.Count;
            internalNumbering.Add(state, name);
            return name;
        }

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

        public IEnumerable<KeyValuePair<bool, string>> GetRegisteredBoolLiterals(Function function)
        {
            return registeredBools.GetValues(function);
        }
    }
}
