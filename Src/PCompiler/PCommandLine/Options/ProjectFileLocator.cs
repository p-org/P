// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using PChecker.IO.Debugging;
using Plang.Parser;

namespace Plang.Options
{
    /// <summary>
    /// Shared helpers for locating P project files (*.pproj) on disk.
    /// Used by both <c>p compile</c> and <c>p check</c> argument parsers so the
    /// two stay in sync.
    /// </summary>
    internal static class ProjectFileLocator
    {
        /// <summary>
        /// If neither <c>pproj</c> nor <c>pfiles</c> has been supplied, searches the
        /// current directory for a <c>*.pproj</c> file and appends it to
        /// <paramref name="result"/> as a <c>pproj</c> argument.
        ///
        /// If exactly one project file is found it is selected. If more than one is
        /// found the selection is ambiguous, so this reports an error and exits
        /// rather than silently picking one (the previous behavior, which depended on
        /// filesystem ordering and could compile/check the wrong project). The user
        /// is asked to disambiguate with <c>--pproj</c>.
        /// </summary>
        public static void FindLocalPProject(List<CommandLineArgument> result)
        {
            if (!TryFindLocalPProject(result, Directory.GetCurrentDirectory(), out var error))
            {
                Error.ReportAndExit(error);
            }
        }

        /// <summary>
        /// Core resolution logic, factored out of <see cref="FindLocalPProject"/> so it
        /// can be unit tested without touching the process working directory or exiting.
        /// On success, mutates <paramref name="result"/> (adding a <c>pproj</c> argument
        /// when a single project file is found) and returns <c>true</c>. Returns
        /// <c>false</c> with <paramref name="errorMessage"/> set only for the ambiguous
        /// "multiple project files found" case.
        /// </summary>
        internal static bool TryFindLocalPProject(
            List<CommandLineArgument> result, string searchDirectory, out string errorMessage)
        {
            errorMessage = null;

            foreach (var arg in result)
            {
                if (arg.LongName.Equals("pproj") || arg.LongName.Equals("pfiles"))
                {
                    return true;
                }
            }

            CommandLineOutput.WriteInfo(".. Searching for a P project file *.pproj locally in the current folder");
            var files = (
                from file in Directory.GetFiles(searchDirectory, "*.pproj")
                let info = new FileInfo(file)
                where ((info.Attributes & FileAttributes.Hidden) == 0) & ((info.Attributes & FileAttributes.System) == 0)
                select file).OrderBy(file => file, System.StringComparer.Ordinal).ToArray();

            if (files.Length == 0)
            {
                CommandLineOutput.WriteInfo(
                    $".. No P project file *.pproj found in the current folder: {searchDirectory}");
                return true;
            }

            if (files.Length > 1)
            {
                errorMessage =
                    $"Found multiple P project files in {searchDirectory}: " +
                    $"{string.Join(", ", files.Select(Path.GetFileName))}. " +
                    "Please specify which one to use with the '--pproj' option.";
                return false;
            }

            var commandlineArg = new CommandLineArgument
            {
                Value = files[0],
                LongName = "pproj",
                ShortName = "pp",
            };
            CommandLineOutput.WriteInfo($".. Found P project file: {commandlineArg.Value}");
            result.Add(commandlineArg);
            return true;
        }
    }
}
