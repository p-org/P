using System.IO;
using Plang.Compiler.Backend.CSharp;

namespace Plang.Compiler.Backend.Symbolic
{
    public class SymbolicCodeCompiler
    {
        private static string pomTemplate = 
@"
<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0""
xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd"">
    <modelVersion>4.0.0</modelVersion>
    <groupId>psymbolic</groupId>
    <artifactId>projectName</artifactId>
    <version>1.0</version>
    <build>
        <sourceDirectory>.</sourceDirectory>
        <plugins>
            <plugin>
                <artifactId>maven-failsafe-plugin</artifactId>
                <version>2.22.2</version>
            </plugin>
            <plugin>
                <artifactId>maven-assembly-plugin</artifactId>
                <executions>
                    <execution>
                        <phase>package</phase>
                        <goals>
                            <goal>single</goal>
                        </goals>
                    </execution>
                </executions>
                <configuration>
                    <archive>
                        <manifest>
                            <addClasspath>true</addClasspath>
                            <mainClass>psymbolic.commandline.PSymbolic</mainClass>
                        </manifest>
                    </archive>
                    <descriptorRefs>
                        <descriptorRef>jar-with-dependencies</descriptorRef>
                    </descriptorRefs>
                </configuration>
            </plugin>
        </plugins>
    </build>
    <dependencies>
        <dependency>
            <groupId>psymbolic</groupId>
            <artifactId>SymbolicRuntime</artifactId>
            <version>1.0</version>
        </dependency>
    </dependencies>
    <properties>
        <project.build.sourceEncoding>UTF-8</project.build.sourceEncoding>
        <maven.compiler.source>1.8</maven.compiler.source>
        <maven.compiler.target>1.8</maven.compiler.target>
        <java.version>16</java.version>
    </properties>
</project>";
        
        public static void Compile(ICompilationJob job)
        {
            var pomPath = Path.Combine(job.ProjectRootPath.FullName, "pom.xml");
            string stdout = "";
            string stderr = "";
            // if the file does not exist then create the file
            if (!File.Exists(pomPath))
            {
                pomTemplate = pomTemplate.Replace("projectName",job.ProjectName);
                File.WriteAllText(pomPath, pomTemplate);
            }

            // compile the csproj file
            string[] args = new[] { "clean package"};

            int exitCode = CSharpCodeCompiler.RunWithOutput(job.ProjectRootPath.FullName, out stdout, out stderr, "mvn", args);
            if (exitCode != 0)
            {
                throw new TranslationException($"Compiling generated Symbolic Java code FAILED!\n" + $"{stdout}\n" + $"{stderr}\n");
            }

            job.Output.WriteInfo($"{stdout}");
        }

    }
}