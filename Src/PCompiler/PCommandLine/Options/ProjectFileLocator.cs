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
        /// If neither <c>pproj</c> nor <c>pfiles</c> has been supplied, searches
        /// the current directory for a single <c>*.pproj</c> file and, if one is
        /// found, appends it to <paramref name="result"/> as a <c>pproj</c> argument.
        /// </summary>
        public static void FindLocalPProject(List<CommandLineArgument> result)
        {
            foreach (var arg in result)
            {
                if (arg.LongName.Equals("pproj") || arg.LongName.Equals("pfiles"))
                {
                    return;
                }
            }

            CommandLineOutput.WriteInfo(".. Searching for a P project file *.pproj locally in the current folder");
            var filtered =
                from file in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.pproj")
                let info = new FileInfo(file)
                where ((info.Attributes & FileAttributes.Hidden) == 0) & ((info.Attributes & FileAttributes.System) == 0)
                select file;
            var files = filtered.ToArray();
            if (files.Length == 0)
            {
                CommandLineOutput.WriteInfo(
                    $".. No P project file *.pproj found in the current folder: {Directory.GetCurrentDirectory()}");
                return;
            }

            var commandlineArg = new CommandLineArgument
            {
                Value = files.First(),
                LongName = "pproj",
                ShortName = "pp",
            };
            CommandLineOutput.WriteInfo($".. Found P project file: {commandlineArg.Value}");
            result.Add(commandlineArg);
        }
    }
}
