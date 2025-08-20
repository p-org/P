# Setting Up a PObserve Package with Gradle

This guide walks you through setting up a PObserve package using Gradle, assuming you already have your P specifications and Java parser implemented. You'll learn how to create an empty Gradle project, add your existing parser and spec files, and configure the necessary Gradle settings.

## [Step 1] Create Empty Gradle Project

First, create a new directory for your PObserve project and initialize it as a Gradle project:

```bash
mkdir YourPObserveProject
cd YourPObserveProject
gradle init --type java-library --dsl kotlin
```

This will create a basic Gradle project structure with Kotlin DSL.

## [Step 2] Set Up Project Structure

Assuming you have already implemented your P specifications and Java parser, you'll need to place them in the corresponding directories as follows:

**1. Add P Specification**

1. Copy your existing P specification files to `src/main/PSpec/`
2. Update the YourProject.pproj file to reference the new file paths in the Gradle project structure

**2. Add Java Parser**

1. Copy your existing Java parser to `src/main/java/your/package/parser/`
2. Update the P event imports in your parser to match the package paths where the generated P code will be placed

After adjusting the package names in the newly added files, your PObserve package should have a directory structure similar to this:

```
YourPObserveProject/
├── build.gradle.kts                   # Gradle build configuration
├── gradlew                            # Gradle wrapper (Unix)
├── gradlew.bat                        # Gradle wrapper (Windows)
├── gradle/
│   └── wrapper/
│       ├── gradle-wrapper.jar
│       └── gradle-wrapper.properties
└── src/
    ├── main/
    │   ├── java/
    │   │   └── your/package/
    │   │       └── parser/
    │   │           └── YourParser.java        # Your existing log parser
    │   ├── PSpec/
    │   │   └── YourSpec.p                     # Your existing P specification
    │   └── YourProject.pproj                  # P project file
    └── test/
        ├── java/
        │   └── your/package/
        │       └── YourPObserveTest.java      # JUnit tests
        └── resources/
            └── sample_logs.txt                # Test log files
```

## [Step 3] Configure Gradle Build File

**1. Add Required Plugins**

Add these plugins at the top of your build.gradle.kts:

```
plugins {
    id("java")
    id("java-library")
    // Required for creating an uber JAR
    id("com.github.johnrengelman.shadow") version "7.1.2"
}
```

**2. Configure Dependencies**

Add PObserve-specific dependencies:

```
dependencies {
    // PObserve core dependencies
    implementation("io.github.p-org:pobserve-commons:1.0.0")
    implementation("io.github.p-org:pobserve-java-unit-test:1.0.0")
}
```

**3. Configure P Compiler Integration**

The P compiler integration needs to be configured to generate Java code that PObserve can use. You can do this either through Gradle automation or manual compilation.

* [Option A] Automated Compilation via Gradle Task

    Add these tasks to automatically compile P specifications during the build process (assuming P >= v2.4 has already been installed):
    ```
    tasks.register<Exec>("compilePSpec") {
        commandLine("p", "compile", "--mode", "pobserve")
        workingDir = File("${project.rootDir}/src/main")
    }

    // Ensure P specs are compiled before Java compilation
    tasks.named("compileJava") {
        dependsOn("compilePSpec")
    }
    ```
    ??? note "Important: Make sure your P Project file is configured correctly when using automated compilation via Gradle"
        When using automated compilation, ensure your pproj file has the correct configuration:

        1. Set the correct output directory for generated Java code
        ```xml
        <OutputDir>java/your/package/spec</OutputDir>
        ```

        2. Specify the Java package name for generated code based on the output directory used in the `OutputDir` param
        ```xml
        <pobserve-package>your.package.spec</pobserve-package>
        ```
        Refer to [LockServerPObserve.pproj]() in the LockServerPObserve example package to see how the .pproj file was configured

* [Option B] Manual Compilation

    Alternatively, you can manually compile your P specifications by running `p compile --mode pobserve` in your original P project directory, then copy the generated files from `PGenerated/Java/` to `src/main/java/your/package/spec/` in your new PObserve Gradle project. Make sure to adjust the package declarations in the copied files to match your target package structure.


!!!tip "The automated approach (Option A) is recommended for consistent builds and better integration with your development workflow"


**4. Configure JAR Packaging**

Add tasks for creating an uber JAR that packages your classes and their dependencies into a single JAR file for PObserve to consume:

```
tasks.withType<ShadowJar> {
    archiveBaseName.set("YourProjectName")
    archiveClassifier.set("all")
    archiveVersion.set("1.0.0")
    mergeServiceFiles()
}

tasks.register<ShadowJar>("uberjar") {
    archiveBaseName.set("YourProjectName")
    archiveClassifier.set("uber")
    archiveVersion.set("1.0.0")
    from(sourceSets.main.get().output)
    configurations = listOf(project.configurations.runtimeClasspath.get())
    mergeServiceFiles()
    exclude("META-INF/*.SF", "META-INF/*.DSA", "META-INF/*.RSA")
}
```

## [Step 4] Build the Gradle Project

Build the PObserve project with the following command:

```bash
./gradlew build
```

This will:

1. Compile your P specifications to Java classes (if automated compilation is configured)
2. Compile your Java parser and spec code
3. Run tests (if any are present)
4. Create an uber JAR file in the `build/libs/` folder

The final uber JAR will be created at `build/libs/YourProject-1.0.0-uber.jar` and contains all your compiled code and dependencies needed for PObserve.

!!!info ""
    Refer to the [build.gradle.kts](https://github.com/p-org/P/blob/dev/pobserve/Src/PObserve/Examples/LockServerPObserve/build.gradle.kts) in the  LockServerPObserve example package for reference

!!!success ""
    Your PObserve package is now ready to be used with PObserve to monitor system logs against your formal specifications! 🚀✨
