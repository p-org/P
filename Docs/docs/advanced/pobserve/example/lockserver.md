# Lock Server

## Overview

Consider a simple lock server that manages access to shared resources by granting or denying locks. The lock server grants locks if resources are available, denies locks if resources are locked, and releases locks when the client is done using resources.

To ensure the correctness of the lock server, we want to check if the following correctness properties hold in the presence of concurrent client requests:

* *The lock server grants a lock to only one client at a time (Mutual Exclusion)*
* *The lock server responds to a client only when requested (Response Only On Request)*

## Lock Server Package

The [LockServer](https://github.com/p-org/P/tree/main/Src/PObserve/Examples/LockServer) package will be used to demonstrate how to integrate PObserve directly into Java JUnit tests for real-time specification verification during test execution. This package contains:

1. **Lock Server Implementation**: Core lock server functionality with logging integration
2. **JUnit Test Classes**: Various test approaches including PObserve-enabled tests
3. **PObserve Integration**: Real-time specification monitoring during test execution
4. **Logging Configuration**: Log4j2 setup for capturing and analyzing events

## Lock Server PObserve Package

The [LockServerPObserve](https://github.com/p-org/P/tree/dev/pobserve/Src/PObserve/Examples/LockServerPObserve) package will be used to demonstrate how to use different modes of PObserve on the lock server example. This package contains three components:

1. **Parser**: The `LockServerParser` converts the service log lines to PObserve Events
2. **P Specification**: The `LockServerCorrect.p` implements two correctness specifications - `MutualExclusion` and `ResponseOnlyOnRequest`
3. **Logs**: The resources folder contains multiple sample lock server logs for you to play around