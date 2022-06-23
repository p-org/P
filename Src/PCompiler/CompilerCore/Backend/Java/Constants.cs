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

        #region Machine source generation

        private static readonly string[] JreDefaultImports =
        {
            "java.util.*",
        };

        /// <summary>
        /// Produce the fully-qualified import statements for a collection of classes.
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<string> ImportStatements()
        {
            List<string> classes = JreDefaultImports.ToList();
            classes.Sort();

            return classes.Select(pkg => $"import {pkg};");
        }


        public static readonly string MachineNamespaceName = "TopDeclarations";
        public static readonly string MachineDefnFileName = $"{MachineNamespaceName}.java";

        #endregion

        #region Event source generation

        public static readonly string EventNamespaceName = "PEvents";
        public static readonly string EventDefnFileName = $"{EventNamespaceName}.java";

        #endregion

        #region FFI generation

        internal static string FFIPackage = "PForeign";
        internal static string GlobalFFIPackage = $"{FFIPackage}.globals";

        #endregion

        #region Project build file generation

        internal static string DoNotEditWarning => $@"
/***************************************************************************
 * This file was auto-generated on {DateTime.Now.ToLongDateString()} at {DateTime.Now.ToLongTimeString()}.  
 * Please do not edit manually!
 **************************************************************************/";

        internal static string BuildFileName => "pom.xml";

        internal static string BuildFileTemplate(string projectName)
        {
            return String.Format(@"
<project xmlns=""http://maven.apache.org/POM/4.0.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd"">
    <modelVersion>4.0.0</modelVersion>

    <groupId>com.amazon.p</groupId>
    <version>1.0-SNAPSHOT</version>
    <artifactId>{0}</artifactId>

    <name>{0}</name>
    <properties>
        <project.build.sourceEncoding>UTF-8</project.build.sourceEncoding>
        <maven.compiler.source>11</maven.compiler.source>
        <maven.compiler.target>11</maven.compiler.target>
        <buildDirectory>${{project.basedir}}/POutput</buildDirectory>
    </properties>
    <packaging>jar</packaging>

    <dependencies>
        <dependency>
            <groupId>p.runtime</groupId>
            <artifactId>PJavaRuntime</artifactId>
            <version>1.0-SNAPSHOT</version>
        </dependency>
    </dependencies>

    <build>
        <directory>${{buildDirectory}}</directory>
        <sourceDirectory>.</sourceDirectory>
    </build>
</project>", projectName);
        }
        #endregion

        #region P runtime identifiers

        /// <summary>
        /// The fully-qualified name of the static `deepClone(PrtValue)` method exposed by
        /// the Java PRT runtime.
        /// </summary>
        public static readonly string PrtDeepCloneMethodName = "prt.values.Clone.deepClone";

        /// <summary>
        /// The fully-qualified name of the static `deepEquality(Object, Object)` method
        /// exposed by the Java PRT runtime.
        /// </summary>
        public static readonly string PrtDeepEqualsMethodName = "prt.values.Equality.deepEquals";

        /// <summary>
        /// The fully-qualified name of the static `compare(Comparable, Comparable)` method
        /// exposed by the Java PRT runtime.
        /// </summary>
        public static readonly string PrtCompareMethodName = "prt.values.Equality.compare";

        /// <summary>
        /// The fully-qualified name of the static `eleemntAt(LinkedHashSet, int)` method
        /// exposed by the Java PRT runtime.
        /// </summary>
        public static readonly string PrtSetElementAtMethodName = "prt.values.SetIndexing.elementAt";

        /// <summary>
        /// The fully-qualified class name of the Java P runtime's PValue class.
        /// </summary>
        public static readonly string PValueClass = "prt.values.PValue";

        /// <summary>
        /// The fully-qualified class name of the Java P runtime's PEvent class.
        /// </summary>
        public static readonly string PEventsClass = "prt.events.PEvent";

        #endregion
    }
}
