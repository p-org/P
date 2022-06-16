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
        private static string[] defaultImports = {
            "com.amazon.pobserve.todo.Event",
            "com.amazon.pobserve.todo.Monitor",
            "com.amazon.pobserve.todo.State",
        };

        private static string[] jreDefaultImports =
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
            return defaultImports
                .Concat(jreDefaultImports)
                .Select(pkg => $"import {pkg};");
        }

        internal static string GlobalForeignFunClassName => "GlobalFFI";

        internal static string DoNotEditWarning => $@"
/***************************************************************************
 * This file was auto-generated on {DateTime.Now.ToLongDateString()} at {DateTime.Now.ToLongTimeString()}.  
 * Please do not edit manually!
 **************************************************************************/";
    }
}