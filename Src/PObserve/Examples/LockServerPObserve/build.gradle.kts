import com.github.jengelman.gradle.plugins.shadow.tasks.ShadowJar

plugins {
    id("java")
    id("java-library")
    id("maven-publish")
    id("com.github.johnrengelman.shadow") version "7.1.2"
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

tasks.register<Exec>("compilePSpec") {
    commandLine("p", "compile", "--mode", "pobserve")
    workingDir = File("${project.rootDir}/src/main")
}

tasks.named("compileJava") {
    dependsOn("compilePSpec")
}

tasks.named<ProcessResources>("processTestResources") {
    duplicatesStrategy = DuplicatesStrategy.EXCLUDE
}

tasks.withType<ShadowJar> {
    archiveBaseName.set("LockServerPObserve")
    archiveClassifier.set("all")
    archiveVersion.set("1.0.0")
    mergeServiceFiles()
}

// Task for creating the uber jar
tasks.register<ShadowJar>("uberjar") {
    archiveBaseName.set("LockServerPObserve")
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

group = "io.github.p-org"
version = "1.0.0"

// Publish to maven local for pobserve java junit example
publishing {
    publications {
        create<MavenPublication>("mavenJava") {
            from(components["java"])
            
            groupId = group.toString()
            artifactId = "LockServerPObserve"
            version = version.toString()
        }
    }
    repositories {
        mavenLocal()
    }
}