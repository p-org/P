# Getting Started with PObserve

## [Step 1] Extend your P formal model package with support for PObserve
As mentioned in the [PObserve overview page](./pobserve.md), PObserve requires two inputs from users:

1. A [PObserve Log Parser](./logparser.md) to convert logs into PObserve events
2. P specifications that should be checked on the logs

Once you have defined your parser and specifications, follow the [Setting Up a PObserve Package with Gradle](./package-setup.md) guide to learn how to package them into a JAR file that can be consumed by the PObserve CLI.

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
    ✔ Enables checking specifications on a local machine at a smaller scale

Follow the [PObserve CLI Guide](./pobservecli.md) to start using the PObserve command line tool.
