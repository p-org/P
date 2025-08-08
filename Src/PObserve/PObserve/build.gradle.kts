import com.github.spotbugs.snom.SpotBugsTask

plugins {
    application
    id("java")
    id("maven-publish")
    id("checkstyle")
    id("com.github.spotbugs") version "5.+"
    id("com.diffplug.spotless") version "6.+"
    id("java-library")
    id("jacoco")
    id("com.github.johnrengelman.shadow") version "7.1.2"
}

group = "io.github.p-org"
version = "1.0.0"

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

tasks.withType<Checkstyle>().configureEach {
    exclude("**/PGenerated/**")
}

tasks.jacocoTestReport {
  reports {
    xml.required.set(false)
    csv.required.set(false)
    html.outputLocation.set(layout.buildDirectory.dir("jacocoHtml"))
  }
}

tasks.jacocoTestCoverageVerification {
  violationRules {
    rule {
      isEnabled = true
      element = "CLASS"
      includes = listOf("pobserve.*")
      excludes = listOf(
              "pobserve.models.*",
              "pobserve.utils.*"
      )
      limit {
        counter = "LINE"
        value = "COVEREDRATIO"
        minimum = 0.toBigDecimal()
      }
    }
  }
}

application {
    mainClass.set("pobserve.PObserve")
    applicationDefaultJvmArgs = listOf("-Dlog4j.configurationFile=log4j2.properties")
}

// Fix for Gradle detected implicit dependency problems
tasks.named("distZip") {
    dependsOn("shadowJar")
}

tasks.named("distTar") {
    dependsOn("shadowJar")
}

tasks.named("startScripts") {
    dependsOn("shadowJar")
}

// Fix for startShadowScripts dependency issue
tasks.named("startShadowScripts") {
    dependsOn("jar")
}

tasks.check {
    dependsOn(tasks.jacocoTestCoverageVerification)
    dependsOn(tasks.jacocoTestReport)
}

tasks.build {
  dependsOn(tasks.named("shadowJar"), tasks.spotlessApply, tasks.jacocoTestReport)
}

tasks.test {
    useJUnitPlatform()
    testLogging {
        events("passed", "skipped", "failed")
    }

  // Print url for test report, even if tests pass
  doLast {
        if (state.failure == null) {
          val reportUrl = file("build/brazil-unit-tests/index.html").absolutePath
          println("\n\n\nAll tests passed. See the report at: file://$reportUrl\n\n")
        }
    }

}

tasks.named<com.github.jengelman.gradle.plugins.shadow.tasks.ShadowJar>("shadowJar") {
  archiveClassifier.set("super")
  archiveFileName.set("PObserve-${version}.jar")
  mergeServiceFiles()

  isZip64 = true
  exclude(
          "**/Log4j2Plugins.dat",
          "**/com/esotericsoftware/kryo/Kryo.class**",
          "**/com/esotericsoftware/kryo/Kryo\$1.class**",
          "**/com/esotericsoftware/kryo/Kryo\$2.class**")

  manifest {
    attributes(
            "Main-Class" to "pobserve.PObserve",
            "Build-Jdk" to System.getProperty("java.version")
    )
  }
}

sourceSets {
    main {
        java {
            srcDirs("src/main/java")
        }
        resources {
            srcDir("src/resources")
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
    implementation("io.github.p-org:pobserve-regression-testing:1.0.1")
    implementation("com.beust:jcommander:1.82")
    implementation("com.googlecode.json-simple:json-simple:1.1.1")
    implementation("com.google.code.findbugs:annotations:3.0.1")
    implementation("com.fasterxml.jackson.core:jackson-core:2.16.1")
    implementation("com.fasterxml.jackson.core:jackson-databind:2.16.1")
    implementation("de.ruedigermoeller:fst:2.50")
    implementation("org.apache.logging.log4j:log4j-slf4j-impl:2.20.0")

    compileOnly("org.projectlombok:lombok:1.18.30")
    annotationProcessor("org.projectlombok:lombok:1.18.30")

    testCompileOnly("org.projectlombok:lombok:1.18.30")
    testAnnotationProcessor("org.projectlombok:lombok:1.18.30")

    testImplementation("org.junit.jupiter:junit-jupiter-api:5.11.3")
    testImplementation("org.junit.jupiter:junit-jupiter-params:5.11.3")
    testImplementation("org.junit.jupiter:junit-jupiter-engine:5.11.3")
    testRuntimeOnly("org.junit.platform:junit-platform-launcher")
}
