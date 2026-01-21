# Getting Started with PObserve

## [Step 1] Create a PObserve Package
To get started with PObserve, you first need to set up an empty PObserve Gradle project structure. Follow the [Setting Up a PObserve Package with Gradle](./package-setup.md) guide to create the project structure and configure the build system.

Once you have the empty PObserve package set up, you'll need to implement two key components:

1. **[P specifications](../../../manual/monitors)** - P state machines that define the correctness properties to be checked
2. **[PObserve Log Parser](./logparser.md)** - A Java class that converts your system's logs into PObserve events

After implementing these components in your PObserve package, you can build the project to generate artifacts that can be used with PObserve.


## [Step 2] Checking specifications using PObserve
PObserve allows checking formal specifications in two different modes: (1) unit testing and (2) locally (as a commandline tool)

**[Mode 1] Check P specification during unit testing**

PObserveJUnit asserts P specifications during Java unit testing by directly passing the service logs generated during testing to PObserve.

!!!info ""
    ✔ Easily integrates with a service's existing JUnit tests

    ✔ Automatically checks specifications during JUnit test execution

Follow the [PObserve JUnit Integration Guide](./pobservejunit.md) to get started with unit test integration.



**[Mode 2] Check P specifications by running PObserve locally (as a commandline tool)**

!!!info ""
    ✔ Enables checking specifications against small service/test log files on local machine

Follow the [PObserve CLI Guide](./pobservecli.md) to start using the PObserve command line tool.
