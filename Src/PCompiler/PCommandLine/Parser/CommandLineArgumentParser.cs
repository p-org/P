// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace PChecker.Utilities
{
    /// <summary>
    /// Specifies a dependency between arguments.
    /// </summary>
    public class CommandLineArgumentDependency
    {
        /// <summary>
        /// Name of an argument.
        /// </summary>
        public string Name;

        /// <summary>
        /// Value of that argument.
        /// </summary>
        public string Value;
    }

    /// <summary>
    /// Exception raised from ParseArguments.
    /// </summary>
    public class CommandLineException : Exception
    {
        public CommandLineException(string msg, List<CommandLineArgument> result)
            : base(msg)
        {
            this.Result = result;
        }

        public List<CommandLineArgument> Result { get; set; }
    }

    /// <summary>
    /// A single command line argument.
    /// </summary>
    public class CommandLineArgument
    {
        /// <summary>
        /// The long name referenced using two dashes (e.g. "--max-steps").
        /// </summary>
        public string LongName;

        /// <summary>
        /// The short name referenced using single dash (e.g. "-ms").
        /// </summary>
        public string ShortName;

        /// <summary>
        /// Optional datatype (default string). Supported datatypes are primitive types
        /// only (e.g. int, uint, float, string, bool).
        /// </summary>
        public Type DataType;

        /// <summary>
        /// Help text for the command line option. You can use newlines to format the help content
        /// but each line will be auto-indented by the PrintHelp function.
        /// </summary>
        public string Description;

        /// <summary>
        /// Checks if the arugment is required.
        /// </summary>
        public bool IsRequired;

        /// <summary>
        /// Checks if the arugment is positional.
        /// </summary>
        public bool IsPositional;

        /// <summary>
        /// Optional name to a <see cref="CommandLineGroup"/>.
        /// </summary>
        public string Group;

        /// <summary>
        /// Hide this option from the printed help message.
        /// </summary>
        public bool IsHidden;

        /// <summary>
        /// Checks if the value of the argument is an array (e.g. "--coverage data activity").
        /// </summary>
        public bool IsMultiValue;

        /// <summary>
        /// The parsed command line value matching DataType.
        /// </summary>
        public object Value;

        /// <summary>
        /// This is the print help option.
        /// </summary>
        public bool PrintHelp;

        /// <summary>
        /// This argument depends on the specific value of another.
        /// </summary>
        public CommandLineArgumentDependency DependsOn;

        /// <summary>
        /// Defines a list of possible values.
        /// </summary>
        public List<string> AllowedValues = new List<string>();

        internal string LongSyntax
        {
            get
            {
                if (this.IsPositional)
                {
                    return this.LongName;
                }

                return "--" + this.LongName;
            }
        }

        internal string LongSyntaxAndDataType
        {
            get
            {
                string text = this.LongSyntax;
                if (this.DataType != typeof(bool))
                {
                    text += " " + this.DataTypeString;
                }

                return text;
            }
        }

        internal string ShortSyntax
        {
            get
            {
                return "-" + this.ShortName;
            }
        }

        internal string DataTypeString
        {
            get
            {
                string type = null;
                if (this.DataType == typeof(string))
                {
                    type = "string";
                }
                else if (this.DataType == typeof(int))
                {
                    type = "int";
                }
                else if (this.DataType == typeof(uint))
                {
                    type = "uint";
                }
                else if (this.DataType == typeof(double))
                {
                    type = "double";
                }
                else if (this.DataType == typeof(string))
                {
                    type = "bool";
                }
                else if (this.DataType != null)
                {
                    throw new Exception(string.Format("Unsupported data type: {0}", this.DataType.Name));
                }

                return type;
            }
        }

        internal object ParseValue(string value)
        {
            Type type = this.DataType;
            if (value == null)
            {
                if (type == typeof(bool))
                {
                    return true;  // Default for boolean options.
                }
                else
                {
                    if (!this.AllowedValues.Contains(string.Empty))
                    {
                        throw new Exception(string.Format("Argument: '{0}' missing a value", this.LongName));
                    }
                }
            }

            object result;
            if (type == null || type == typeof(string))
            {
                result = value;
            }
            else if (type == typeof(bool))
            {
                if (!bool.TryParse(value, out bool x))
                {
                    throw new Exception(string.Format("Argument: '{0}' value is not a valid bool", this.LongName));
                }

                result = x;
            }
            else if (type == typeof(int))
            {
                if (!int.TryParse(value, out int x))
                {
                    throw new Exception(string.Format("Argument: '{0}' value is not a valid integer", this.LongName));
                }

                result = x;
            }
            else if (type == typeof(uint))
            {
                if (!uint.TryParse(value, out uint x))
                {
                    throw new Exception(string.Format("Argument: '{0}' value is not a valid unsigned integer", this.LongName));
                }

                result = x;
            }
            else if (type == typeof(double))
            {
                if (!double.TryParse(value, out double x))
                {
                    throw new Exception(string.Format("Argument: '{0}' value is not a valid double", this.LongName));
                }

                result = x;
            }
            else
            {
                throw new Exception(string.Format("Argument: '{0}' type '{1}' is not supported, use bool, int, uint, double, string", this.LongName, type.Name));
            }

            if (this.AllowedValues.Count > 0)
            {
                if (result == null)
                {
                    result = string.Empty;
                }

                string s = result.ToString();
                if (!this.AllowedValues.Contains(s))
                {
                    throw new Exception(string.Format("Argument: '{0}' value '{1}' must be one of [{2}]", this.LongName, s, string.Join(", ", this.AllowedValues.ToArray())));
                }
            }

            return result;
        }

        internal CommandLineArgument Clone()
        {
            return new CommandLineArgument()
            {
                LongName = this.LongName,
                ShortName = this.ShortName,
                DataType = this.DataType,
                Description = this.Description,
                IsRequired = this.IsRequired,
                Group = this.Group,
                IsHidden = this.IsHidden,
                AllowedValues = this.AllowedValues,
                IsMultiValue = this.IsMultiValue,
                IsPositional = this.IsPositional,
                DependsOn = this.DependsOn
            };
        }

        internal void AddParsedValue(string arg)
        {
            if (!this.IsMultiValue)
            {
                if (this.Value != null)
                {
                    throw new Exception(string.Format("Argument: '--{0}' has too many values", this.LongName));
                }

                this.Value = this.ParseValue(arg);
            }
            else
            {
                var value = this.ParseValue(arg);
                if (this.DataType == typeof(string))
                {
                    this.Value = this.Append<string>(this.Value, value);
                }
                else if (this.DataType == typeof(int))
                {
                    this.Value = this.Append<int>(this.Value, value);
                }
                else if (this.DataType == typeof(uint))
                {
                    this.Value = this.Append<uint>(this.Value, value);
                }
                else if (this.DataType == typeof(double))
                {
                    this.Value = this.Append<double>(this.Value, value);
                }
                else if (this.DataType == typeof(string))
                {
                    this.Value = this.Append<bool>(this.Value, value);
                }
                else if (this.DataType != null)
                {
                    throw new Exception(string.Format("Unsupported data type: {0}", this.DataType.Name));
                }
                else
                {
                    this.Value = value;
                }
            }
        }

        private T[] Append<T>(object value1, object value2)
        {
            if (value1 == null)
            {
                return new T[1] { (T)value2 };
            }

            T[] existing = (T[])value1;
            T[] newList = new T[existing.Length + 1];
            int i = 0;
            while (i < existing.Length)
            {
                newList[i] = existing[i];
                i++;
            }

            newList[i] = (T)value2;
            return newList;
        }
    }

    /// <summary>
    /// Provides a way of grouping command line arguments in the help text.
    /// </summary>
    public class CommandLineGroup
    {
        private readonly CommandLineArgumentParser Parser;
        private readonly List<string> LongNames;

        internal CommandLineGroup(CommandLineArgumentParser parser, List<string> longNames)
        {
            this.Parser = parser;
            this.LongNames = longNames;
        }

        /// <summary>
        /// The unique name of the group.
        /// </summary>
        public string Name;

        /// <summary>
        /// Help text for the command line group. You can use newlines to format the help content
        /// but each line will be auto-indented by the PrintHelp function.
        /// </summary>
        public string Description;

        /// <summary>
        /// The whole group is hidden.
        /// </summary>
        public bool IsHidden;

        /// <summary>
        /// Specifies that this group is only required if the given dependency is true.
        /// </summary>
        public CommandLineArgumentDependency DependsOn;

        /// <summary>
        /// Add a positional argument. Positional arguments have no switch (--foo) and must be specified in the
        /// order that they are defined. Note that positional arguments must appear before any named arguments.
        /// </summary>
        /// <param name="name">The logical name of the argument.</param>
        /// <param name="description">Help text for the command line option. You can use newlines to format the
        /// help content but each line will be auto-indented by the PrintHelp function.</param>
        /// <param name="dataType">Optional datatype (default string). Supported datatypes are primitive types
        /// only (e.g. int, uint, float, string, bool).</param>
        /// <returns>The new option or throws <see cref="DuplicateNameException"/>.</returns>
        public CommandLineArgument AddPositionalArgument(string name, string description = null, Type dataType = null)
        {
            var argument = this.Parser.AddPositionalArgument(name, description, dataType);
            argument.IsHidden = this.IsHidden;
            argument.Group = this.Name;
            argument.DependsOn = this.DependsOn;
            return argument;
        }

        /// <summary>
        /// Add a new command line option to the group. The option names still need to be unique.
        /// </summary>
        /// <param name="longName">The long name referenced using two dashes (e.g. "--max-steps").</param>
        /// <param name="shortName">The short name referenced using single dash (e.g. "-ms").</param>
        /// <param name="description">Help text for the command line option. You can use newlines to format the
        /// help content but each line will be auto-indented by the PrintHelp function.</param>
        /// <param name="dataType">Optional datatype (default string). Supported datatypes are primitive types
        /// only (e.g. int, float, string, bool).</param>
        /// <param name="required">Whether the argument is required or not.</param>
        /// <returns>The new <see cref="CommandLineArgument"/> object.</returns>
        public CommandLineArgument AddArgument(string longName, string shortName, string description = null, Type dataType = null, bool required = false)
        {
            if (dataType == null)
            {
                dataType = typeof(string);
            }

            var argument = this.Parser.AddArgument(longName, shortName, description, dataType, required);
            argument.IsHidden = this.IsHidden;
            argument.Group = this.Name;
            return argument;
        }
    }

    /// <summary>
    /// A handy command line argument parser.
    /// </summary>
    public class CommandLineArgumentParser
    {
        private readonly string AppName;
        private readonly string AppDescription;

        /// <summary>
        /// To remember the oder in which they were added.
        /// </summary>
        private readonly List<string> GroupNames = new List<string>();

        /// <summary>
        /// To remember the order in which positional arguments were added.
        /// </summary>
        private readonly List<string> PositionalNames = new List<string>();

        /// <summary>
        /// To remember the order in which switch arguments were added.
        /// </summary>
        private readonly List<string> LongNames = new List<string>();

        /// <summary>
        /// The currnet list of command line groups.
        /// </summary>
        public Dictionary<string, CommandLineGroup> Groups = new Dictionary<string, CommandLineGroup>();

        /// <summary>
        /// The current set of possible command line arguments.
        /// </summary>
        public Dictionary<string, CommandLineArgument> Arguments = new Dictionary<string, CommandLineArgument>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgumentParser"/> class.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <param name="appDescription">The overview help text for the application..</param>
        public CommandLineArgumentParser(string appName, string appDescription)
        {
            this.AppName = appName;
            this.AppDescription = appDescription;
            this.AddArgument("?", "?", "Show this help menu", typeof(bool)).PrintHelp = true;
        }

        /// <summary>
        /// Add a new command line group or return the existing group if it already exists.
        /// </summary>
        /// <param name="name">The name of the group.</param>
        /// <param name="description">The help text for the group.</param>
        /// <returns>The new command line group.</returns>
        public CommandLineGroup GetOrCreateGroup(string name, string description)
        {
            if (this.Groups.TryGetValue(name, out CommandLineGroup group))
            {
                return group;
            }

            group = new CommandLineGroup(this, this.LongNames) { Name = name, Description = description };
            this.Groups.Add(name, group);
            this.GroupNames.Add(name);
            return group;
        }

        /// <summary>
        /// Add a positional argument. Positional arguments have no switch (--foo) and must be specified in the
        /// order that they are defined. Note that positional arguments must appear before any named arguments.
        /// </summary>
        /// <param name="name">The logical name of the argument.</param>
        /// <param name="description">Help text for the command line option. You can use newlines to format the
        /// help content but each line will be auto-indented by the PrintHelp function.</param>
        /// <param name="dataType">Optional datatype (default string). Supported datatypes are primitive types
        /// only (e.g. int, float, string, bool).</param>
        /// <returns>The new option or throws <see cref="DuplicateNameException"/>.</returns>
        public CommandLineArgument AddPositionalArgument(string name, string description = null, Type dataType = null)
        {
            if (this.Arguments.ContainsKey(name))
            {
                throw new DuplicateNameException(string.Format("Argument {0} already defined", name));
            }

            CommandLineArgument argument = new CommandLineArgument()
            {
                LongName = name,
                DataType = dataType,
                Description = description,
                IsRequired = true,
                IsPositional = true
            };

            this.Arguments[name] = argument;
            this.PositionalNames.Add(name);
            return argument;
        }

        /// <summary>
        /// Add a new command line option.
        /// </summary>
        /// <param name="longName">The long name referenced using two dashes (e.g. "--max-steps").</param>
        /// <param name="shortName">The short name referenced using single dash (e.g. "-ms").</param>
        /// <param name="description">Help text for the command line option. You can use newlines to format the
        /// help content but each line will be auto-indented by the PrintHelp function.</param>
        /// <param name="dataType">Optional datatype (default string). Supported datatypes are primitive types
        /// only (e.g. int, float, string, bool).</param>
        /// <param name="required">Whether argument is required.</param>
        /// <returns>The new option or throws <see cref="System.Data.DuplicateNameException"/>.</returns>
        public CommandLineArgument AddArgument(string longName, string shortName, string description = null, Type dataType = null, bool required = false)
        {
            if (this.Arguments.TryGetValue(longName, out CommandLineArgument argument))
            {
                throw new DuplicateNameException(string.Format("Argument {0} already defined", longName));
            }

            if (shortName != null)
            {
                var existing = (from a in this.Arguments.Values where a.ShortName == shortName select a).FirstOrDefault();
                if (existing != null)
                {
                    throw new DuplicateNameException(string.Format("Argument short name '{0}' is already being used by '{1}'", shortName, existing.LongName));
                }
            }

            argument = new CommandLineArgument()
            {
                LongName = longName,
                ShortName = shortName,
                DataType = dataType,
                Description = description,
                IsRequired = required
            };
            this.Arguments[longName] = argument;
            this.LongNames.Add(longName);
            return argument;
        }

        private class WordWrapper
        {
            private readonly TextWriter Output;
            private readonly int Indent;
            private readonly int LineLength;
            private int CurrentLineLength;
            private readonly string IndentText;

            internal WordWrapper(TextWriter output, int indent, int lineLength)
            {
                this.Output = output;
                this.Indent = indent;
                this.LineLength = lineLength;
                this.CurrentLineLength = indent;
                this.IndentText = new string(' ', this.Indent);
            }

            internal void Write(string text)
            {
                bool first = true;
                foreach (string line in text.Split('\n'))
                {
                    if (!first)
                    {
                        this.NewLine();
                    }

                    first = false;
                    foreach (string word in line.Split(' '))
                    {
                        this.WriteWord(word);
                    }
                }
            }

            internal void WriteWord(string word)
            {
                if (this.CurrentLineLength + word.Length > this.LineLength)
                {
                    this.NewLine();
                }

                this.Output.Write(word);
                this.Output.Write(" ");
                this.CurrentLineLength += word.Length + 1;
            }

            private void NewLine()
            {
                this.Output.WriteLine();
                this.CurrentLineLength = this.Indent;
                if (this.Indent > 0)
                {
                    this.Output.Write(this.IndentText);
                }
            }
        }

        /// <summary>
        /// Parse the command line using the options defined in this argument parser.
        /// </summary>
        /// <param name="args">The command line.</param>
        /// <returns>The parsed arguments.</returns>
        public List<CommandLineArgument> ParseArguments(string[] args)
        {
            List<CommandLineArgument> result = new List<CommandLineArgument>();
            int position = 0; // For positional arguments.
            CommandLineArgument current = null;

            for (int idx = 0; idx < args.Length; idx++)
            {
                string arg = args[idx];

                if (arg.StartsWith("-"))
                {
                    if (arg.StartsWith("--"))
                    {
                        var name = arg.Substring(2);
                        current = null;
                        this.Arguments.TryGetValue(name, out current);
                    }
                    else if (arg.StartsWith("-"))
                    {
                        current = null;
                        var name = arg.Substring(1);
                        // Note that "/" is not supported as an argument delimiter because it conflicts with unix file paths.
                        foreach (var s in this.Arguments.Values)
                        {
                            if (s.ShortName == name)
                            {
                                current = s;
                                break;
                            }
                        }

                        if (current == null)
                        {
                            // See if there's a matching long name with no short name defined.
                            foreach (var s in this.Arguments.Values)
                            {
                                if (s.LongName == name)
                                {
                                    current = s;
                                    break;
                                }
                            }
                        }
                    }

                    if (current == null)
                    {
                        throw new CommandLineException(string.Format("Unexpected argument: '{0}'", arg), result);
                    }

                    current = current.Clone();
                    result.Add(current);

                    if (current.PrintHelp)
                    {
                        this.PrintHelp(Console.Out);
                        Environment.Exit(1);
                    }
                }
                else if (current != null)
                {
                    // The value for the current switch argument.
                    current.AddParsedValue(arg);
                }
                else
                {
                    // Positional arguments.
                    do
                    {
                        if (position < this.PositionalNames.Count)
                        {
                            var name = this.PositionalNames[position++];
                            current = this.Arguments[name];
                        }
                        else
                        {
                            throw new CommandLineException(string.Format("Unexpected positional argument: '{0}'", arg), result);
                        }
                    }
                    while (!IsRequired(current, result));

                    // Positional arguments have no name so the arg is the value.
                    var temp = current.Clone();
                    temp.Value = current.ParseValue(arg);
                    result.Add(temp);
                    current = null; // This argument is done, cannot have any more values.
                }
            }

            foreach (var arg in this.Arguments.Values)
            {
                if (IsRequired(arg, result) && !(from r in result where r.LongName == arg.LongName select r).Any())
                {
                    if (arg.IsPositional)
                    {
                        throw new CommandLineException(string.Format("Missing required argument: '{0}'", arg.LongName), result);
                    }
                    else
                    {
                        throw new CommandLineException(string.Format("Missing required argument: '--{0}'", arg.LongName), result);
                    }
                }
            }

            foreach (var arg in result)
            {
                if (!arg.IsPositional && arg.Value == null && arg.DataType != typeof(bool) && !arg.AllowedValues.Contains(string.Empty))
                {
                    throw new CommandLineException(string.Format("Missing value for argument: '--{0}'", arg.LongName), result);
                }
            }

            return result;
        }

        private static bool IsRequired(CommandLineArgument argument, List<CommandLineArgument> result)
        {
            if (argument.IsRequired)
            {
                if (argument.DependsOn != null)
                {
                    var dependent = (from r in result where r.LongName == argument.DependsOn.Name select r).FirstOrDefault();
                    if (dependent != null && string.Compare(dependent.Value.ToString(), argument.DependsOn.Value, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Shows help.
        /// </summary>
        public void PrintHelp(TextWriter output)
        {
            const int ArgHelpLineLength = 100;
            const int ArgHelpIndent = 30;

            string prefix = string.Format("usage: {0} ", this.AppName);
            output.Write(prefix);
            int indent = prefix.Length;

            var wrapper = new WordWrapper(output, indent, ArgHelpLineLength);
            foreach (var name in this.PositionalNames)
            {
                var arg = this.Arguments[name];
                string text = arg.LongSyntax;
                if (arg.DependsOn != null)
                {
                    text = "[" + text + "]";
                }

                wrapper.WriteWord(text);
            }

            foreach (var name in this.LongNames)
            {
                var arg = this.Arguments[name];
                if (arg.IsHidden)
                {
                    continue;
                }

                string text = arg.LongSyntaxAndDataType;

                if (!arg.IsRequired)
                {
                    text = "[" + text + "]";
                }

                wrapper.WriteWord(text);
            }

            output.WriteLine();
            output.WriteLine();
            wrapper = new WordWrapper(output, 0, ArgHelpLineLength);
            wrapper.Write(this.AppDescription);
            output.WriteLine();
            output.WriteLine();
            var visitedOptions = new HashSet<string>();

            foreach (var name in this.GroupNames)
            {
                CommandLineGroup g = this.Groups[name];
                if (g.IsHidden)
                {
                    continue;
                }

                output.WriteLine(g.Description + ":");
                output.WriteLine(new string('-', g.Description.Length + 1));

                foreach (var option in this.PositionalNames.Concat(this.LongNames))
                {
                    var arg = this.Arguments[option];
                    if (arg.IsHidden)
                    {
                        continue;
                    }

                    if (arg.Group == name)
                    {
                        visitedOptions.Add(option);

                        string syntax = "  ";
                        if (!string.IsNullOrEmpty(arg.ShortName))
                        {
                            syntax += string.Format("{0}, ", arg.ShortSyntax);
                        }

                        syntax += string.Format("{0} ", arg.LongSyntaxAndDataType);

                        output.Write(syntax);
                        if (syntax.Length < ArgHelpIndent)
                        {
                            output.Write(new string(' ', ArgHelpIndent - syntax.Length));
                        }
                        else
                        {
                            output.WriteLine();
                            output.Write(new string(' ', ArgHelpIndent));
                        }

                        if (!string.IsNullOrEmpty(arg.Description))
                        {
                            output.Write(": ");
                            wrapper = new WordWrapper(output, ArgHelpIndent + 2, ArgHelpLineLength);
                            wrapper.Write(arg.Description);
                        }

                        output.WriteLine();
                    }
                }

                output.WriteLine();
            }

            bool optionalHeader = false;
            foreach (var option in this.PositionalNames.Concat(this.LongNames))
            {
                var arg = this.Arguments[option];
                if (arg.IsHidden)
                {
                    continue;
                }

                if (!visitedOptions.Contains(arg.LongName))
                {
                    if (!optionalHeader)
                    {
                        optionalHeader = true;
                        const string optionalBanner = "Optional Arguments:";
                        output.WriteLine(optionalBanner);
                        output.WriteLine(new string('-', optionalBanner.Length));
                    }

                    string syntax = "  ";
                    if (!string.IsNullOrEmpty(arg.ShortName))
                    {
                        syntax += string.Format("{0}, ", arg.ShortSyntax);
                    }

                    syntax += string.Format("{0} ", arg.LongSyntax);
                    output.Write(syntax);
                    if (syntax.Length < ArgHelpIndent)
                    {
                        output.Write(new string(' ', ArgHelpIndent - syntax.Length));
                    }
                    else
                    {
                        output.WriteLine();
                        output.Write(new string(' ', ArgHelpIndent));
                    }

                    if (!string.IsNullOrEmpty(arg.Description))
                    {
                        wrapper = new WordWrapper(output, ArgHelpIndent, ArgHelpLineLength);
                        wrapper.Write(arg.Description);
                    }

                    output.WriteLine();
                }
            }
        }
    }
}
