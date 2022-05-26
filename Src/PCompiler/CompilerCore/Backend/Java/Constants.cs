using System;
using System.Collections.Generic;
using System.Linq;
using Plang.Compiler.TypeChecker.Types;

namespace Plang.Compiler.Backend.Java
{
    /// <summary>
    /// Holds some string constants for the JavaCodeGenerator.
    /// </summary>
    internal static class Constants
    {
        private static string JavaRTPackageName = "com.amazon.PObserve.RT.temp.prefix.fixme";

        private static string[] defaultImport = {
            "Event",
            "Monitor",
            "State",
        };

        /// <summary>
        /// Produce the fully-qualified import statements for a collection of classes.
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<string> ImportStatements()
        {
            return defaultImport.Select(cname => $"{JavaRTPackageName}.{cname}");
        }

        /// <summary>
        /// The name for the top-level Java class, that will contain all implementation / monitor
        /// nested classes.
        /// </summary>
        internal static string TopLevelClassName = "PImplementation";
        
       
    }
}