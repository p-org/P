// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

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
            this._checkerConfiguration = checkerConfiguration;
            this.Logger = logger ?? new ConsoleLogger();
        }

        /// <summary>
        /// Reports an error, followed by the current line terminator.
        /// </summary>
        public void WriteErrorLine(string value)
        {
            this.Write("Error: ", ConsoleColor.Red);
            this.Write(value, ConsoleColor.Yellow);
            this.Logger.WriteLine(string.Empty);
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        private void Write(string value, ConsoleColor color)
        {
            if (this._checkerConfiguration.EnableColoredConsoleOutput)
            {
                Error.Write(this.Logger, color, value);
            }
            else
            {
                this.Logger.Write(value);
            }
        }
    }
}
