// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// Reports errors and warnings to the user.
    /// </summary>
    internal sealed class ErrorReporter
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// The installed logger.
        /// </summary>
        internal TextWriter Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorReporter"/> class.
        /// </summary>
        internal ErrorReporter(Configuration configuration, TextWriter logger)
        {
            this.Configuration = configuration;
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
            if (this.Configuration.EnableColoredConsoleOutput)
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
