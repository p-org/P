plugins {
    id("java")
    id("java-library")
}

repositories {
    mavenLocal()
    mavenCentral()
}

tasks.withType<Copy> {
    duplicatesStrategy = DuplicatesStrategy.EXCLUDE
}

dependencies {
    // P PObserve package dependency
    testImplementation("io.github.p-org:LockServerPObserve:1.0.0")

    // PObserve dependencies
    implementation("io.github.p-org:pobserve-commons:1.0.0")
    testImplementation("io.github.p-org:pobserve-java-unit-test:1.0.0")

    // Log4j dependencies
    implementation("org.apache.logging.log4j:log4j-core:2.20.0")
    implementation("org.apache.logging.log4j:log4j-api:2.20.0")
    implementation("org.apache.logging.log4j:log4j-slf4j2-impl:2.20.0")
    implementation("org.slf4j:slf4j-api:2.0.7")

    compileOnly("org.projectlombok:lombok:1.18.30")
    annotationProcessor("org.projectlombok:lombok:1.18.30")
    compileOnly("com.github.spotbugs:spotbugs-annotations:4.8.3")

    // Testing dependencies
    testImplementation("org.mockito:mockito-core:5.11.0")
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
    systemProperty("log4j.configurationFile", "log4j2.xml")
}

group = "io.github.p-org"
version = "1.0.0"
