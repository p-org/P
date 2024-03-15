// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using PChecker.IO.Debugging;
using PChecker.IO.Logging;

namespace PChecker.IO
{
    /// <summary>
    /// Reports errors and warnings to the user.
    /// </summary>
    internal sealed class ErrorReporter
    {
        /// <summary>
        /// CheckerConfiguration.
        /// </summary>
        private readonly CheckerConfiguration _checkerConfiguration;

        /// <summary>
        /// The installed logger.
        /// </summary>
        internal TextWriter Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorReporter"/> class.
        /// </summary>
        internal ErrorReporter(CheckerConfiguration checkerConfiguration, TextWriter logger)
        {
            _checkerConfiguration = checkerConfiguration;
            Logger = logger ?? new ConsoleLogger();
        }

        /// <summary>
        /// Reports an error, followed by the current line terminator.
        /// </summary>
        public void WriteErrorLine(string value)
        {
            Write("Error: ", ConsoleColor.Red);
            Write(value, ConsoleColor.Yellow);
            Logger.WriteLine(string.Empty);
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        private void Write(string value, ConsoleColor color)
        {
            if (_checkerConfiguration.EnableColoredConsoleOutput)
            {
                Error.Write(Logger, color, value);
            }
            else
            {
                Logger.Write(value);
            }
        }
    }
}