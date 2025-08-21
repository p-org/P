# Setting Up a PObserve Package with Gradle

This guide walks you through setting up a PObserve package using Gradle. You'll learn how to create an empty Gradle project with the proper structure, configure the build for your intended usage (JUnit integration or PObserve CLI).

## [Step 1] Create Empty Gradle Project Structure

First, create a new directory for your PObserve project and initialize it as a Gradle project:

```bash
mkdir YourPObserveProject
cd YourPObserveProject
gradle init --type java-library --dsl kotlin
```

This creates a basic Gradle project structure with Kotlin DSL.

## [Step 2] Set Up PObserve Directory Structure

Create the necessary directories for your PObserve package:

```bash
# Create directories for P specifications
mkdir -p src/main/PSpec

# Create directories for Java parser
mkdir -p src/main/java/your/package/parser

# Create test directories if you intend to write unit tests
mkdir -p src/test/java/your/package
mkdir -p src/test/resources
```

Your project structure should now look like this:

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
    │   │       └── parser/            # Your parser will go here
    │   └── PSpec/                     # Your P specifications will go here
    └── test/
        ├── java/
        │   └── your/package/          # Your tests will go here
        └── resources/                 # Test log files will go here
```

## [Step 3] Configure Gradle Build

Configure your `build.gradle.kts` file with the base configuration and add the specific components you need to integrate with different pobserve modes as you need:

### Base Configuration

Start with this base configuration that's common to all PObserve packages:

```kotlin
plugins {
    id("java")
    id("java-library")
}

repositories {
    mavenLocal()
    mavenCentral()
}

dependencies {
    // PObserve dependencies
    implementation("io.github.p-org:pobserve-commons:1.0.0")
    implementation("io.github.p-org:pobserve-java-unit-test:1.0.0")
    
    // Testing dependencies
    testImplementation("org.junit.jupiter:junit-jupiter-api:5.10.1")
    testImplementation("org.junit.jupiter:junit-jupiter-engine:5.10.1")
    testRuntimeOnly("org.junit.platform:junit-platform-launcher")
}

sourceSets {
    main {
        java {
            srcDirs("src/main/java")
        }
        resources {
            srcDirs("src/main/resources")
        }
    }
    test {
        java {
            srcDirs("src/test/java")
        }
        resources {
            srcDirs("src/test/resources")
        }
    }
}

tasks.test {
    useJUnitPlatform()
    testLogging {
        events("passed", "skipped", "failed")
    }
}

// P Compiler integration
tasks.register<Exec>("compilePSpec") {
    commandLine("p", "compile", "--mode", "pobserve")
    workingDir = File("${project.rootDir}/src/main")
}

tasks.named("compileJava") {
    dependsOn("compilePSpec")
}

group = "your.group.id"
version = "1.0.0"
```

### Additional Configuration for PObserve JUnit Integration

If you want to use your package to run pobserve with unit tests, add these components to your `build.gradle.kts`:

**1. Add Maven Publish Plugin**
```kotlin
plugins {
    // ... existing plugins
    id("maven-publish")  // Add this for JUnit integration
}
```

**2. Add Maven Publication Configuration**
```kotlin
// Maven publication for JUnit integration
publishing { 
    publications {
        create<MavenPublication>("mavenJava") {
            from(components["java"])
            
            groupId = group.toString()
            artifactId = "YourProjectName"
            version = version.toString()
        }
    }
    repositories {
        mavenLocal()
    }
}
```

!!! info ""
    Publishing your PObserve package to Maven Local repository allows you to import the parser and spec components in your system implementation's unit tests. Alternatively, you can publish them to your custom Maven repository and import from there to run PObserve in your unit tests."

### Additional Configuration for PObserve CLI Usage

To use PObserve CLI, you need to provide both the parser and specification as an uber JAR. Add the following components to your `build.gradle.kts` to package your classes and their dependencies into a single uber JAR:

**1. Add Shadow Plugin**
```kotlin
import com.github.jengelman.gradle.plugins.shadow.tasks.ShadowJar

plugins {
    // ... existing plugins
    id("com.github.johnrengelman.shadow") version "7.1.2"  // Add this for CLI usage
}
```

**2. Add Uber JAR Tasks**
```kotlin
tasks.named<ProcessResources>("processTestResources") {
    duplicatesStrategy = DuplicatesStrategy.EXCLUDE
}

// Uber JAR configuration for PObserve CLI
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
    from(sourceSets.test.get().output)
    configurations = listOf(
        project.configurations.runtimeClasspath.get(),
        project.configurations.testRuntimeClasspath.get()
    )
    mergeServiceFiles()
    exclude("META-INF/*.SF", "META-INF/*.DSA", "META-INF/*.RSA")
}
```

!!! tip "Combining Both Configurations"
    You can add both JUnit and CLI configurations to the same `build.gradle.kts` file to support both use cases. Simply include all the plugins and configurations from both sections above.


## [Step 4] Create P Project Configuration

Create a P project file `src/main/YourProject.pproj` to configure P compilation:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <ProjectName>YourProject</ProjectName>
  <InputFiles>
    <PFile>PSpec/YourSpec.p</PFile>
  </InputFiles>
  <OutputDir>java/your/package/spec</OutputDir>
  <pobserve-package>your.package.spec</pobserve-package>
</Project>
```


## [Step 5] Build Your PObserve Package

**Build Commands**

```bash
# Build the project (always required)
./gradlew build
```

**For JUnit Integration (if configured)**

```bash
# Publish to local Maven repository
./gradlew publishToMavenLocal
```

**For PObserve CLI Usage (if configured)**

```bash
# Create the uber JAR
./gradlew uberjar
```

!!! success ""
    :tada: Your PObserve package setup is now complete!

    **What next?**

    Create your P specifications in `src/main/PSpec/` and implement your Java parser in `src/main/java/your/package/parser/`.

    For implementation guidance, see:

    - **[Writing P Specifications](../../../manual/monitors)** - How to write P specifications
    - **[Writing Log Parser](logparser.md)** - How to implement your Java log parser
