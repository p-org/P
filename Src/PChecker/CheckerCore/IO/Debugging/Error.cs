// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;

namespace PChecker.IO.Debugging
{
    /// <summary>
    /// Static class implementing error reporting methods.
    /// </summary>
    public static class Error
    {
        /// <summary>
        /// If you play with Console.ForegroundColor then you should grab this lock in order
        /// to avoid color leakage (wrong color becomes set permanently).
        /// </summary>
        public static readonly object ColorLock = new object();

        /// <summary>
        /// Reports a generic error to the user.
        /// </summary>
        public static void Report(string format, params object[] args)
        {
            var message = string.Format(CultureInfo.InvariantCulture, format, args);
            Write(ConsoleColor.Red, "Error: ");
            Write(ConsoleColor.Yellow, message);
            Console.Error.WriteLine(string.Empty);
        }

        /// <summary>
        /// Reports a generic error to the user and exits.
        /// </summary>
        public static void ReportAndExit(string value)
        {
            Write(ConsoleColor.Red, "Error: ");
            Write(ConsoleColor.Yellow, value);
            Console.Error.WriteLine(string.Empty);
            Environment.Exit(1);
        }

        /// <summary>
        /// Reports a compiler error to the user and exits.
        /// </summary>
        public static void CompilerReportAndExit(string value)
        {
            ReportAndExit(value + " Run `p compile --help` for usage help.");
        }

        /// <summary>
        /// Reports a checker error to the user and exits.
        /// </summary>
        public static void CheckerReportAndExit(string value)
        {
            ReportAndExit(value + " Run `p check --help` for usage help.");
        }

        /// <summary>
        /// Reports a generic error to the user and exits.
        /// </summary>
        public static void ReportAndExit(string format, params object[] args)
        {
            var message = string.Format(CultureInfo.InvariantCulture, format, args);
            Write(ConsoleColor.Red, "Error: ");
            Write(ConsoleColor.Yellow, message);
            Console.Error.WriteLine(string.Empty);
            Environment.Exit(1);
        }

        /// <summary>
        ///  Writes the specified string value to the output stream.
        /// </summary>
        private static void Write(ConsoleColor color, string value)
        {
            Write(Console.Error, color, value);
        }

        /// <summary>
        /// Writes with console color to the specified TextWriter.
        /// </summary>
        internal static void Write(TextWriter logger, ConsoleColor color, string value)
        {
            lock (ColorLock)
            {
                try
                {
                    Console.ForegroundColor = color;
                    logger.Write(value);
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }
    }
}