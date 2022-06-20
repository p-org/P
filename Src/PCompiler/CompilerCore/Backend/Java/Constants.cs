using System;
using System.Collections.Generic;
using System.Linq;

namespace Plang.Compiler.Backend.Java
{
    /// <summary>
    /// Holds some string constants for the JavaCodeGenerator.
    /// </summary>
    internal static class Constants
    {
        private static readonly string[] PrtImports = {
            "events.PObserveEvent",
            "prt.State",
            "prt.Monitor",
            "prt.Values"
        };

        private static readonly string[] JreDefaultImports =
        {
            "java.text.MessageFormat",
            "java.util.*",
            "java.util.stream.Stream"
        };

        /// <summary>
        /// Produce the fully-qualified import statements for a collection of classes.
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<string> ImportStatements()
        {
            List<string> classes = PrtImports
                .Concat(JreDefaultImports)
                .ToList();
            classes.Sort();

            return classes.Select(pkg => $"import {pkg};");
        }

        internal static string GlobalForeignFunClassName => "GlobalFFI";

        internal static string DoNotEditWarning => $@"
/***************************************************************************
 * This file was auto-generated on {DateTime.Now.ToLongDateString()} at {DateTime.Now.ToLongTimeString()}.  
 * Please do not edit manually!
 **************************************************************************/";
    }
}
