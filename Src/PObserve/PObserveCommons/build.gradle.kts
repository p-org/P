import com.github.spotbugs.snom.SpotBugsTask
import com.vanniktech.maven.publish.JavaLibrary
import com.vanniktech.maven.publish.JavadocJar

plugins {
    id("java")
    id("checkstyle")
    id("com.github.spotbugs") version "5.+"
    id("com.diffplug.spotless") version "6.+"
    id("java-library")
    id("com.vanniktech.maven.publish") version "0.33.0"
}

repositories {
    mavenLocal()
    mavenCentral()
}

tasks.withType<Jar> {
    duplicatesStrategy = DuplicatesStrategy.EXCLUDE
}

tasks.javadoc {
    options.encoding = "UTF-8"
}


// spotbugs
spotbugs {
    toolVersion.set("4.7.3")
    ignoreFailures.set(true)
    excludeFilter.set(File("${project.rootDir}/config/spotbugs/excludeFilter.xml"))
}

tasks.withType<SpotBugsTask>() {
    reports {
        create("html") {
            enabled = true
            outputLocation.set(file(layout.buildDirectory.dir("reports/spotbugs/spotbugs.html")))
        }
    }
}

// checkstyle
checkstyle {
    sourceSets = listOf(the<SourceSetContainer>()["main"], the<SourceSetContainer>()["test"])
    configFile = File("${project.rootDir}/config/checkstyle/checkstyle.xml")
    setIgnoreFailures(false)
}

sourceSets {
    main {
        java {
            srcDirs("src/main/java")
        }
    }
    test {
        java {
            srcDirs("src/test/java")
        }
    }
}

dependencies {
    compileOnly("org.projectlombok:lombok:1.18.36")
    annotationProcessor("org.projectlombok:lombok:1.18.36")

    implementation("org.apache.logging.log4j:log4j-core:2.24.1")
    implementation("jakarta.validation:jakarta.validation-api:3.0.2")
    implementation("com.beust:jcommander:1.82")
    implementation("software.amazon.awssdk:regions:2.29.15")
    implementation("com.fasterxml.jackson.core:jackson-databind:2.18.1")
    implementation("com.github.javaparser:javaparser-core:3.24.0")
    implementation("javax.validation:validation-api:2.0.1.Final")
    implementation("org.junit.jupiter:junit-jupiter-api:5.10.1")
}

group = "io.github.p-org"
version = "1.0.0"

mavenPublishing {
    configure(JavaLibrary(
        sourcesJar = true, // Set to true to publish sources JAR
        javadocJar = JavadocJar.Javadoc(),
    ))

    coordinates(group.toString(), "pobserve-commons", version.toString())
    publishToMavenCentral(automaticRelease = true)

    signAllPublications()

    pom {
        name.set("PObserveCommons")
        description.set("Common utilities for PObserve runtime monitoring")
        inceptionYear.set("2024")
        url.set("https://github.com/p-org/P")
        licenses {
            license {
                name.set("MIT License")
                url.set("https://github.com/p-org/P/blob/master/LICENSE.txt")
            }
        }
        developers {
            developer {
                name.set("Ankush Desai")
            }
            developer {
                name.set("Mounika Chadalavada")
            }
        }
        scm {
            connection.set("scm:git:git@github.com:p-org/P.git")
            developerConnection.set("scm:git:ssh://github.com:p-org/P.git")
            url.set("https://github.com/p-org/P/tree/master")
        }
    }
}
