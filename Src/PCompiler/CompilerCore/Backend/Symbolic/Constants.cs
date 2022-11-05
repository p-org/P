using System.IO;

namespace Plang.Compiler.Backend.Symbolic
{
    internal class Constants
    {
        internal static readonly string pomTemplate =
            @"<?xml version=""1.0"" encoding=""UTF-8""?>
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
                <artifactId>maven-jar-plugin</artifactId>
                <executions>
                    <execution>
                        <id>default-jar</id>
                        <phase>none</phase>
                    </execution>
                </executions>
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
                    <finalName>projectName</finalName>
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
            <groupId>io.github.p-org</groupId>
            <artifactId>psym</artifactId>
            <version>[0.5.5,)</version>
        </dependency>

        <!-- https://mvnrepository.com/artifact/org.projectlombok/lombok -->
        <dependency>
            <groupId>org.projectlombok</groupId>
            <artifactId>lombok</artifactId>
            <version>1.18.24</version>
            <scope>provided</scope>
        </dependency>
    </dependencies>

    <properties>
        <project.build.sourceEncoding>UTF-8</project.build.sourceEncoding>
        <maven.compiler.source>1.8</maven.compiler.source>
        <maven.compiler.target>1.8</maven.compiler.target>
        <java.version>16</java.version>
    </properties>
</project>
";
    }
}
