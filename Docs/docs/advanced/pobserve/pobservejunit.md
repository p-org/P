# PObserve JUnit Integration

PObserveJUnit is a specialized component of the PObserve service designed to seamlessly integrate formal specification checking into your Java unit tests.

## Getting Started

P users can get started with PObserveJUnit using the following steps:

**1. Set up PObserve**

Make sure you have a log parser and P specification correctly written as specified in the [Getting Started guide](./gettingstarted.md).

**2. Allow PObserveJUnit to fetch logs from unit tests**

There are three recommended ways in which PObserveJUnit can get access to the logs generated during execution. Pick the option that suits your application best.

## Three ways for PObserveJUnit to consume logs

!!! note ""

    Note that it does not matter how PObserveJUnit is given access to the logs generated during unit testing. These are the three options that we have used in the past (recommended):

**[Option 1] Extend the PObserve base test**

If you're using log4j as the logger and do not have a base test class yet, you can extend the [PObserveLog4JBaseTest](https://github.com/p-org/P/blob/dev/pobserve/Src/PObserve/PObserveJavaUnitTest/src/main/java/pobserve/junit/log4j/PObserveLog4JBaseTest.java) class. This class automatically creates a new PObserveLog4jAppender that fetches the same log lines as the log4j appender. Add the following annotation to your unit test class:

```java
@PObserveJUnitSpecConfig(parserClass, monitorSuppliersClass, log4jAppenderName)
```

* `parserClass` is the log parser class mentioned in the Getting Started guide above
* `monitorSuppliersClass` is the P specification class mentioned in the Getting Started guide above

**[Option 2] Manually install the PObserve log4j appender**

If your unit tests **already extend a base class or the application does not have a log appender setup**, you have to manually create a new [PObserve log appender](https://github.com/p-org/P/blob/dev/pobserve/Src/PObserve/PObserveJavaUnitTest/src/main/java/pobserve/junit/log4j/PObserveLog4JAppender.java) instance:

Call `PObserveLog4JAppenderHelper.installPObserveAppender(appenderLayoutFormat, parser, monitorSuppliers)` inside the `@BeforeEach` setUp method of your unit test class.

* `parser` is the log parser mentioned in the Getting Started guide above
* `monitorSuppliers` is the P specification mentioned in the Getting Started guide above

You also need to close and remove the PObserve log appender after each test by calling:
`PObserveLog4JAppenderHelper.teardownPObserveAppender(appender, testInfo)` inside the `@AfterEach` tearDown method.

**[Option 3] Application log appender extends the PObserve log appender**

If your application already has **in-memory logging functionality** that captures log messages and stores them in data structures such as lists, buffers, or queues in the application's memory space, simply extend [PObserveLogAppender](https://github.com/p-org/P/blob/dev/pobserve/Src/PObserve/PObserveJavaUnitTest/src/main/java/pobserve/junit/PObserveLogAppender.java) in your in-memory log appender.

Make sure to pass in the parser and monitor suppliers into the appender by adding `super(parser, monitorSuppliers)` in the appender constructor.

* `parser` is the log parser mentioned in the Getting Started guide above
* `monitorSuppliers` is the P specification mentioned in the Getting Started guide above

You'll also need to override the `append` function, adding `super.append()`, and override the `close` function adding `super.close()`.
