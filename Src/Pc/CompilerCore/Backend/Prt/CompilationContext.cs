using System.Collections.Generic;
using System.IO;
using Microsoft.Pc.TypeChecker.AST.Declarations;
using Microsoft.Pc.TypeChecker.AST.States;
using Microsoft.Pc.TypeChecker.Types;

namespace Microsoft.Pc.Backend.Prt
{
    internal class CompilationContext
    {
        private readonly Dictionary<Interface, int> interfaceNumbering = new Dictionary<Interface, int>();
        private readonly Dictionary<Machine, int> machineNumbering = new Dictionary<Machine, int>();
        private readonly ValueInternmentManager<bool> registeredBools;
        private readonly ValueInternmentManager<double> registeredFloats;

        private readonly ValueInternmentManager<int> registeredInts;
        private readonly Dictionary<Machine, Dictionary<State, int>> stateNumbering = new Dictionary<Machine, Dictionary<State, int>>();
        private bool lineHasBeenIndented;

        public CompilationContext(string projectName)
        {
            ProjectName = projectName;
            HeaderFileName = $"{projectName}.h";
            SourceFileName = $"{projectName}.c";
            Names = new NameManager($"P_{projectName.ToUpperInvariant()}_");
            registeredInts = new ValueInternmentManager<int>(Names);
            registeredFloats = new ValueInternmentManager<double>(Names);
            registeredBools = new ValueInternmentManager<bool>(Names);
        }

        public string ProjectName { get; }
        public string HeaderFileName { get; }
        public string SourceFileName { get; }
        public NameManager Names { get; }
        public IEnumerable<PLanguageType> UsedTypes => Names.UsedTypes;
        public HashSet<PLanguageType> WrittenTypes { get; } = new HashSet<PLanguageType>();
        private int IndentationLevel { get; set; }

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

        public void WriteLine(TextWriter output, string format = "")
        {
            // Unindent for every } at the beginning of the line, save the index 
            // of one past the last leading }.
            int i;
            for (i = 0; i < format.Length; i++)
            {
                if (format[i] == '}')
                {
                    IndentationLevel--;
                }
                else if (!char.IsWhiteSpace(format[i]))
                {
                    break;
                }
            }

            // Do not indent preprocessor lines.
            var indentation = new string(' ', 4 * IndentationLevel);
            if (format.StartsWith("#") || lineHasBeenIndented)
            {
                indentation = "";
            }

            output.WriteLine(indentation + format);
            lineHasBeenIndented = false;

            // Compute indentation for future lines starting from after last leading }.
            for (; i < format.Length; i++)
            {
                if (format[i] == '{')
                {
                    IndentationLevel++;
                }
                else if (format[i] == '}')
                {
                    IndentationLevel--;
                }
            }
        }

        public void Write(TextWriter output, string format)
        {
            // Unindent for every } at the beginning of the line, save the index 
            // of one past the last leading }.
            int i;
            for (i = 0; i < format.Length; i++)
            {
                if (format[i] == '}')
                {
                    IndentationLevel--;
                }
                else if (!char.IsWhiteSpace(format[i]))
                {
                    break;
                }
            }

            // Do not indent preprocessor lines.
            var indentation = new string(' ', 4 * IndentationLevel);
            if (format.StartsWith("#") || lineHasBeenIndented)
            {
                indentation = "";
            }

            output.Write(indentation + format);
            lineHasBeenIndented = true;

            // Compute indentation for future lines starting from after last leading }.
            for (; i < format.Length; i++)
            {
                if (format[i] == '{')
                {
                    IndentationLevel++;
                }
                else if (format[i] == '}')
                {
                    IndentationLevel--;
                }
            }
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
