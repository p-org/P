using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plang.Compiler.Backend.Java
{
    /// <summary>
    /// Holds some string constants for the JavaCodeGenerator.  Any public strings
    /// defined in this class are additionally implicitly Java backend-specific
    /// keywords and will be renamed if a P programmer gives an identifier a name
    /// matching one of them.  See the `Reserved words` region for details.
    /// </summary>
    internal static class Constants
    {
        #region P Java runtime constants

        public static readonly string PRTNamespaceName = "prt";

        public static readonly string TryAssertMethodName = "tryAssert";
        public static readonly string TryRaiseEventMethodName = "tryRaiseEvent";

        #endregion

        #region Machine source generation

        private static readonly string[] JreDefaultImports =
        {
            "java.io.Serializable",
            "java.util.*",
            "java.util.logging.*"
        };

        /// <summary>
        /// Produce the fully-qualified import statements for a collection of classes.
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<string> ImportStatements()
        {
            var classes = JreDefaultImports.ToList();
            classes.Sort();

            return classes.Select(pkg => $"import {pkg};");
        }

        public static readonly string MachineNamespaceName = "PMachines";
        public static readonly string MachineDefnFileName = $"{MachineNamespaceName}.java";

        public static readonly string StateEnumName = "PrtStates";

        #endregion

        #region Event source generation

        public static readonly string EventNamespaceName = "PEvents";
        public static readonly string EventDefnFileName = $"{EventNamespaceName}.java";

        #endregion

        #region Types source generation

        public static readonly string TypesNamespaceName = "PTypes";
        public static readonly string TypesDefnFileName = $"{TypesNamespaceName}.java";

        public static readonly string UnnamedTupleFieldPrefix = "arg_";

        #endregion

        #region FFI generation

        private static readonly string rawFFIBanner = $@"
P <-> Java Foreign Function Interface Stubs

This file was auto-generated on {DateTime.Now.ToLongDateString()} at {DateTime.Now.ToLongTimeString()}.

Please separate each generated class into its own .java file (detailed throughout the file), filling
in the body of each function definition as necessary for your project's business logic.
";

        public static readonly string FFIStubFileName = "FFIStubs.txt";

        public static readonly string FFIPackage = "PForeign";
        public static readonly string FFIGlobalScopeCname = "PObserveGlobal";
        public static readonly string FFILocalScopeSuffix = "Foreign";


        // Something that is clearly not valid Java.
        private static readonly string FFICommentToken = "%";

        internal static readonly string FFICommentDivider = new StringBuilder(72).Insert(0, FFICommentToken, 72).ToString();

        internal static string AsFFIComment(string line)
        {
            return FFICommentToken + " " + line;
        }

        internal static string[] FfiBanner => rawFFIBanner
            .Split("\n")
            .Select(AsFFIComment)
            .ToArray();

        #endregion

        #region Project build file generation

        internal static string DoNotEditWarning => $@"
/***************************************************************************
 * This file was auto-generated on {DateTime.Now.ToLongDateString()} at {DateTime.Now.ToLongTimeString()}.
 * Please do not edit manually!
 **************************************************************************/";

        internal static string BuildFileName => "pom.xml";

        internal static readonly string pomTemplate =
            @"
<project xmlns=""http://maven.apache.org/POM/4.0.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd"">
    <modelVersion>4.0.0</modelVersion>

    <groupId>com.amazon.p</groupId>
    <artifactId>-package-name-</artifactId>
    <version>1.0-SNAPSHOT</version>

    <name>-package-name-</name>
    <properties>
        <project.build.sourceEncoding>UTF-8</project.build.sourceEncoding>
        <maven.compiler.source>11</maven.compiler.source>
        <maven.compiler.target>11</maven.compiler.target>
        <buildDirectory>${{project.basedir}}/PObserve</buildDirectory>
    </properties>
    <packaging>jar</packaging>

    <dependencies>
        <dependency>
            <groupId>p.runtime</groupId>
            <artifactId>PJavaRuntime</artifactId>
            <version>1.0-SNAPSHOT</version>

            <!-- Do not transitively bundle log4j as whoever uses this jar will also depend on it. -->
            <exclusions>
                <exclusion>
                    <groupId>org.apache.logging.log4j</groupId>
                    <artifactId>*</artifactId>
                </exclusion>
            </exclusions>
        </dependency>
    </dependencies>

    <build>
        <plugins>
            -foreign-include-
            <plugin>
                <artifactId>maven-assembly-plugin</artifactId>
                <version>3.3.0</version>
                <configuration>
                    <descriptorRefs>
                        <descriptorRef>jar-with-dependencies</descriptorRef>
                    </descriptorRefs>
                </configuration>
                <executions>
                    <execution>
                        <id>make-assembly</id> <!-- this is used for inheritance merges -->
                        <phase>package</phase> <!-- bind to the packaging phase -->
                        <goals>
                            <goal>single</goal>
                        </goals>
                    </execution>
                </executions>
            </plugin>
        </plugins>
        <sourceDirectory>.</sourceDirectory>
    </build>
</project>";

        internal static readonly string pomForeignTemplate =
            @"
            <plugin>
                <groupId>org.codehaus.mojo</groupId>
                <artifactId>build-helper-maven-plugin</artifactId>
                <version>3.2.0</version>
                <executions>
                    <execution>
                        <id>add-source</id>
                        <phase>generate-sources</phase>
                        <goals>
                            <goal>add-source</goal>
                        </goals>
                        <configuration>
                            <sources>
-foreign-source-include-                            </sources>
                        </configuration>
                    </execution>
                </executions>
            </plugin>
";
        #endregion

        #region P runtime identifiers

        /// <summary>
        /// The fully-qualified name of the static `deepClone(PrtValue)` method exposed by
        /// the Java PRT runtime.
        /// </summary>
        internal static readonly string PrtDeepCloneMethodName = "prt.values.Clone.deepClone";

        /// <summary>
        /// The fully-qualified name of the static `deepEquality(Object, Object)` method
        /// exposed by the Java PRT runtime.
        /// </summary>
        internal static readonly string PrtDeepEqualsMethodName = "prt.values.Equality.deepEquals";

        /// <summary>
        /// The fully-qualified name of the static `compare(Comparable, Comparable)` method
        /// exposed by the Java PRT runtime.
        /// </summary>
        internal static readonly string PrtCompareMethodName = "prt.values.Equality.compare";

        /// <summary>
        /// The fully-qualified name of the static `elementAt(LinkedHashSet, long)` method
        /// exposed by the Java PRT runtime.
        /// </summary>
        internal static readonly string PrtSetElementAtMethodName = "prt.values.SetIndexing.elementAt";

        /// <summary>
        /// The fully-qualified class name of the Java P runtime's PValue class.
        /// </summary>
        internal static readonly string PValueClass = "prt.values.PValue";

        /// <summary>
        /// The fully-qualified class name of the Java P runtime's PEvent class.
        /// </summary>
        internal static readonly string PEventsClass = "prt.events.PEvent";

        #endregion


        #region Reserved words

        // https://docs.oracle.com/javase/tutorial/java/nutsandbolts/_keywords.html
        // keywords that match P reserved words are commented out.
        private static IEnumerable<string> _javaKeywords = new[]
        {
            "abstract",
            /*"assert",*/
            /*"boolean",*/
            /*"break",*/
            "byte",
            /*"case",*/
            "catch",
            "char",
            "class",
            "const",
            "continue",
            "default",
            /*"do",*/
            /*"double",*/
            "else",
            "enum",
            "extends",
            "final",
            "finally",
            /*"float",*/
            /*"for",*/
            /*"goto",*/
            /*"if",*/
            "implements",
            "import",
            "instanceof",
            /*"int",*/
            "interface",
            "java", /* not strictly a keyword but it's the top level language import */
            "long",
            "native",
            /*"new",*/
            "package",
            "private",
            "protected",
            "public",
            /*"return",*/
            "short",
            "static",
            "strictfp",
            "super",
            "switch",
            "synchronized",
            /*"this",*/
            "throw",
            "throws",
            "transient",
            "try",
            /*"void",*/
            "volatile",
            /*"while",*/
        };

        private static HashSet<string> _reservedWords = null;

        /// <summary>
        /// Reflects out all the string fields defined in this class.
        /// </summary>
        private static HashSet<string> ExtractReservedWords()
        {
            var self = typeof(Constants);
            return self.GetFields()
                .Where(f => f.IsStatic && f.FieldType == typeof(string))
                .Select(f => (string)f.GetValue(null) /* passed null b/c all our fields are static */ )
                .ToHashSet();
        }

        /// <summary>
        /// Checks whether the given string should be considered a reserved word for the Java backend.  For our
        /// purposes, if the string matches a static string defined in this class, then we consider it a reserved
        /// word (since that string will be used actively in code generation)
        /// </summary>
        public static bool IsReserved(string token)
        {
            _reservedWords ??= ExtractReservedWords()
                .Concat(_javaKeywords)
                .ToHashSet();
            return _reservedWords.Contains(token);
        }

        #endregion
    }
}