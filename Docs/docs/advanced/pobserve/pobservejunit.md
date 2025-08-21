# PObserve JUnit Integration

PObserveJUnit is a specialized component of the PObserve service designed to seamlessly integrate formal specification checking into your Java unit tests.

## Getting Started

**1. Import PObserve Package into Your System Implementation**

To use PObserveJUnit, you need to import your PObserve package (which contains your log parser and P specifications) into your system implementation project. 

If you haven't implemented your PObserve package yet, refer to [Setting Up a PObserve Package](./package-setup.md) to create the package structure and implement the necessary components.

!!! note "Publishing Requirement"
    Once you have your PObserve package ready, make sure it has been published to a Maven repository of your choice (local or custom) using `./gradlew publishToMavenLocal` or your preferred publishing method.

Add your PObserve package as a dependency in your system's `build.gradle.kts`:

```kotlin
dependencies {
    testImplementation("your.group.id:YourPObservePackage:1.0.0")
    testImplementation("io.github.p-org:pobserve-java-unit-test:1.0.0")
}
```

**2. Allow PObserveJUnit to fetch logs from unit tests**

There are three recommended ways in which PObserveJUnit can get access to the logs generated during execution. Pick the option that suits your application best.

## Three ways for PObserveJUnit to consume logs

!!! note ""

    Note that it does not matter how PObserveJUnit is given access to the logs generated during unit testing. These are the three options that we have used in the past (recommended):

**[Option 1] Extend the PObserve base test**

If you're using log4j as the logger and do not have a base test class yet, you can extend the [PObserveLog4JBaseTest](https://github.com/p-org/P/blob/main/Src/PObserve/PObserveJavaUnitTest/src/main/java/pobserve/junit/log4j/PObserveLog4JBaseTest.java) class. This class automatically creates a new PObserveLog4jAppender that fetches the same log lines as the log4j appender. Add the following annotation to your unit test class:

```java
@PObserveJUnitSpecConfig(parserClass, monitorSuppliersClass, log4jAppenderName)
```

* `parserClass` is the log parser class mentioned in the Getting Started section above (imported from your PObserve package)
* `monitorSuppliersClass` is the P specification class mentioned in the Getting Started section above (imported from your PObserve package)

**[Option 2] Manually install the PObserve log4j appender**

If your unit tests **already extend a base class or the application does not have a log appender setup**, you have to manually create a new [PObserve log appender](https://github.com/p-org/P/blob/main/Src/PObserve/PObserveJavaUnitTest/src/main/java/pobserve/junit/log4j/PObserveLog4JAppender.java) instance:

Call `PObserveLog4JAppenderHelper.installPObserveAppender(appenderLayoutFormat, parser, monitorSuppliers)` inside the `@BeforeEach` setUp method of your unit test class.

* `parser` is the log parser mentioned in the Getting Started section above (imported from your PObserve package)
* `monitorSuppliers` is the P specification mentioned in the Getting Started section above (imported from your PObserve package)

You also need to close and remove the PObserve log appender after each test by calling:
`PObserveLog4JAppenderHelper.teardownPObserveAppender(appender, testInfo)` inside the `@AfterEach` tearDown method.

**[Option 3] Application log appender extends the PObserve log appender**

If your application already has **in-memory logging functionality** that captures log messages and stores them in data structures such as lists, buffers, or queues in the application's memory space, simply extend [PObserveLogAppender](https://github.com/p-org/P/blob/main/Src/PObserve/PObserveJavaUnitTest/src/main/java/pobserve/junit/PObserveLogAppender.java) in your in-memory log appender.

Make sure to pass in the parser and monitor suppliers into the appender by adding `super(parser, monitorSuppliers)` in the appender constructor.

* `parser` is the log parser mentioned in the Getting Started section above (imported from your PObserve package)
* `monitorSuppliers` is the P specification mentioned in the Getting Started section above (imported from your PObserve package)

You'll also need to override the `append` function, adding `super.append()`, and override the `close` function adding `super.close()`.
