import com.github.spotbugs.snom.SpotBugsTask

plugins {
    id("java")
    id("maven-publish")
    id("checkstyle")
    id("com.github.spotbugs") version "5.+"
    id("com.diffplug.spotless") version "6.+"
    id("java-library")
    id("signing")
}

repositories {
    mavenLocal()
    mavenCentral()
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

publishing {
    publications {
        create<MavenPublication>("mavenJava") {
            from(components["java"])

            groupId = "io.github.p-org"
            artifactId = "pobserve-commons"
            version = "1.0.0"
            
            pom {
                name.set("PObserveCommons")
                description.set("Common utilities for PObserve runtime monitoring")
                url.set("https://github.com/p-org/P")
                
                licenses {
                    license {
                        name.set("MIT License")
                        url.set("https://github.com/p-org/P/blob/master/LICENSE.txt")
                    }
                }
                
                scm {
                    connection.set("scm:git:git@github.com:p-org/P.git")
                    developerConnection.set("scm:git:ssh://github.com:p-org/P.git")
                    url.set("https://github.com/p-org/P/tree/master")
                }
            }
        }
    }
    
    repositories {
        maven {
            name = "OSSRH"
            val releasesRepoUrl = uri("https://s01.oss.sonatype.org/service/local/staging/deploy/maven2/")
            val snapshotsRepoUrl = uri("https://s01.oss.sonatype.org/content/repositories/snapshots/")
            url = if (version.toString().endsWith("SNAPSHOT")) snapshotsRepoUrl else releasesRepoUrl
            credentials {
                username = System.getenv("MAVEN_USERNAME")
                password = System.getenv("MAVEN_PASSWORD")
            }
        }
    }
}

// Signing configuration
signing {
    val signingKey = System.getenv("SIGNING_KEY")
    val signingPassword = System.getenv("SIGNING_PASSWORD")

    useInMemoryPgpKeys(signingKey, signingPassword)
    sign(publishing.publications["mavenJava"])
}