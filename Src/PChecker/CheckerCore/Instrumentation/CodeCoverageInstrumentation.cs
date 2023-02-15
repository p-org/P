// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.CheckerConfiguration;
using System.Diagnostics;
#endif
using System.IO;
using PChecker.Utilities;
#if NETFRAMEWORK
using System.Linq;

using Microsoft.Coyote.IO;
using Microsoft.Coyote.SystematicTesting.Utilities;
#endif

namespace PChecker.Instrumentation
{
    /// <summary>
    /// Instruments a binary for code coverage.
    /// </summary>
    public static class CodeCoverageInstrumentation
    {
        internal static string OutputDirectory = string.Empty;

        /// <summary>
        /// Set the <see cref="OutputDirectory"/> to either the user-specified <see cref="CheckerConfiguration.OutputFilePath"/>
        /// or to a unique output directory name in the same directory as <see cref="CheckerConfiguration.AssemblyToBeAnalyzed"/>
        /// and starting with its name.
        /// </summary>
        public static void SetOutputDirectory(CheckerConfiguration checkerConfiguration, bool makeHistory)
        {
            if (OutputDirectory.Length > 0)
            {
                return;
            }

            // Do not create the output directory yet if we have to scroll back the history first.
            OutputDirectory = Reporter.GetOutputDirectory(checkerConfiguration.OutputFilePath, checkerConfiguration.AssemblyToBeAnalyzed,
                "POutput", createDir: !makeHistory);
            if (!makeHistory)
            {
                return;
            }

            // The MaxHistory previous results are kept under the directory name with a suffix scrolling back from 0 to 9 (oldest).
            const int MaxHistory = 10;
            string makeHistoryDirName(int history) => OutputDirectory.Substring(0, OutputDirectory.Length - 1) + history;
            var older = makeHistoryDirName(MaxHistory - 1);

            if (Directory.Exists(older))
            {
                Directory.Delete(older, true);
            }

            for (var history = MaxHistory - 2; history >= 0; --history)
            {
                var newer = makeHistoryDirName(history);
                if (Directory.Exists(newer))
                {
                    Directory.Move(newer, older);
                }

                older = newer;
            }

            if (Directory.Exists(OutputDirectory))
            {
                Directory.Move(OutputDirectory, older);
            }

            // Now create the new directory.
            Directory.CreateDirectory(OutputDirectory);
        }
    }
}
