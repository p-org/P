# Example: Lock Server

This page demonstrates how to use PObserve CLI to verify system correctness using a lock server as an example.

## Lock Server Overview

Consider a simple lock server that manages access to shared resources by granting or denying locks. The lock server grants locks if resources are available, denies locks if resources are locked, and releases locks when the client is done using resources.

To ensure the correctness of the lock server, we want to check if the following correctness properties hold in the presence of concurrent client requests:

* *The lock server grants a lock to only one client at a time (Mutual Exclusion)*
* *The lock server responds to a client only when requested (Response Only On Request)*

## Lock Server PObserve Package

The [LockServerPObserve](https://github.com/p-org/P/tree/dev/pobserve/Src/PObserve/Examples/LockServerPObserve) package will be used to demonstrate how to use PObserve CLI on the lock server example. This package contains three components:

1. **Parser**: The `LockServerParser` converts the service log lines to PObserve Events
2. **P Specification**: The `LockServerCorrect.p` implements two correctness specifications - `MutualExclusion` and `ResponseOnlyOnRequest`
3. **Logs**: The resources folder contains multiple lock server logs. Feel free to experiment with PObserve CLI using these logs.

## Building PObserve and LockServerPObserve JARs

**1. Building PObserve JAR**

Follow the [Setup and Build PObserve Guide](./../setuppobservecli.md) to build the PObserve JAR.

**2. Building LockServerPObserve Uber JAR**

Clone the P repository from GitHub and build the LockServerPObserve package:

```bash
git clone https://github.com/p-org/P.git
cd P/Src/PObserve/Examples/LockServerPObserve/
gradle wrapper
./gradlew build
```

The LockServerPObserve uber JAR will be created at `build/libs/LockServerExample-1.0.0.jar` and contains all dependencies needed for PObserve.

## Running PObserve CLI with Lock Server Example

Once you have both the PObserve JAR and the LockServerPObserve uber JAR built, you can run PObserve CLI to monitor the lock server logs! :rocket:

**[Case 1] Happy Case**

```bash
java -jar PObserve-1.0.jar \
  --jars LockServerPObserve-1.0.jar \
  -p LockServerParser \
  --spec MutualExclusion \
  -l LockServerPObserve/src/test/resources/lock_server_log_10000_1_happy.txt
```

??? success "Expected Output"
    PObserve CLI successfully verifies that the `MutualExclusion` specification has been upheld in the log file:
    ```
    -------------------------------------
    Success! No bugs found.
    -------------------------------------

    Total time taken = 0.0030333332 min

    ---------------------------------------------------------------------------------------------------------
    Total Events Read        Total Verified Events    Total Verified Keys      Total Partition Keys   
    ---------------------------------------------------------------------------------------------------------
    10000                    10000                    1                        1                      


    ------------------------------------------------------------------------------------------------------
    Total Parser Errors      Total Spec Errors        Total Out of Order Errors     Total Unknown Errors   
    ------------------------------------------------------------------------------------------------------
    0                        0                        0      
    ```

**[Case 2] Error Case**

```bash
java -jar PObserve-1.0.jar \
  --jars LockServerPObserve-1.0.jar \
  -p LockServerParser \
  --spec MutualExclusion \
  -l LockServerPObserve/src/test/resources/lock_server_log_10000_1_error.txt
```

??? bug "Expected Output"
    PObserve CLI catches a specification violation and reports an error along with detailed output logs:
    ```
    -------------------------------------
    PObserve Spec Violation::
    Assertion failure: PSpec/LockServerCorrect.p:44:9 Lock 0 is already acquired, expects lock error but received lock success.
    .. writing error log into file /Users/mchadala/Desktop/P/Src/PObserve/PObserve/PObserve-19-08-2025_20:23:27/replayEvents_0.log
    -------------------------------------

    Total time taken = 0.0029333334 min

    ---------------------------------------------------------------------------------------------------------
    Total Events Read        Total Verified Events    Total Verified Keys      Total Partition Keys   
    ---------------------------------------------------------------------------------------------------------
    10003                    936                      0                        1                      


    ------------------------------------------------------------------------------------------------------
    Total Parser Errors      Total Spec Errors        Total Out of Order Errors     Total Unknown Errors   
    ------------------------------------------------------------------------------------------------------
    0                        1                        0                             0   
    ```

:confetti_ball: Congratulations! You have successfully run your first example with PObserve CLI.
