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
    ignoreFailures.set(false)
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

tasks.withType<Checkstyle>().configureEach {
    exclude("**/PGenerated/**")
}

sourceSets {
    main {
        java {
            srcDirs("src/main/java")
            exclude("spec/PGenerated/CSharp/**")
        }
        resources {
            srcDir("src/resources/logs/")
            include("**/*.txt")
        }
    }
    test {
        java {
            srcDirs("src/test/java")
        }
    }
}

dependencies {
    implementation("io.github.p-org:pobserve-commons:1.0.0")
    testImplementation("org.junit.jupiter:junit-jupiter-api:5.11.3")
}

// TODO: Add windows support to run the setup script
tasks.register<Exec>("preBuildScript") {
    commandLine("bash", "setup.sh") // Or "./prebuild.sh" if executable
    workingDir = File("${project.rootDir}") // Set the working directory if needed
}

tasks.named("compileJava") {
    dependsOn("preBuildScript")
}


group = "io.github.p-org"
version = "1.0.0"

mavenPublishing {
    configure(JavaLibrary(
        sourcesJar = true, // Set to true to publish sources JAR
        javadocJar = JavadocJar.Javadoc(),
    ))

    coordinates(group.toString(), "pobserve-regression-testing", version.toString())
    publishToMavenCentral(automaticRelease = true)

    signAllPublications()

    pom {
        name.set("PObserveRegressionTesting")
        description.set("Regression testing module for PObserve")
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
                name.set("Aryaman Babber")
            }
        }
        scm {
            connection.set("scm:git:git@github.com:p-org/P.git")
            developerConnection.set("scm:git:ssh://github.com:p-org/P.git")
            url.set("https://github.com/p-org/P/tree/master")
        }
    }
}
