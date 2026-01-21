# Running PObserve Java Unit Test on Lock Server Example

This page demonstrates how to use PObserve with Java JUnit tests to verify system correctness in real-time during test execution using a lock server as an example.

## Prerequisites: Building and Publishing LockServerPObserve Package

Before running the lock server package that has PObserve integrated JUnit tests, you need to build and publish the `LockServerPObserve` package to your local Maven repository. The JUnit tests depend on this package for the parser and specification classes.

**1. Clone P repository**
```bash
git clone https://github.com/p-org/P.git
```

**2. Navigate to the LockServerPObserve Directory**

```bash
cd P/Src/PObserve/Examples/LockServerPObserve/
```

**3. Build and Publish to Local Maven Repository**

```bash
# Initialize Gradle wrapper if not present
gradle wrapper

# Build the project
./gradlew build

# Publish to local Maven repository
./gradlew publishToMavenLocal
```

??? info "What happens during the build process"
    - **P Specification Compilation**: The `compilePSpec` task compiles the P specification files in `src/main/PSpec/` using the P compiler
    - **Java Compilation**: Compiles the Java parser and related classes
    - **Maven Publication**: Publishes the artifact `io.github.p-org:LockServerPObserve:1.0.0` to your local Maven repository (`~/.m2/repository/`)
    - **Dependencies Available**: The lock server package will now be able to resolve the dependency: `implementation("io.github.p-org:LockServerPObserve:1.0.0")`. This provides access to:

        * Parser class: `lockserver.pobserve.parser.LockServerParser`

        * MutualExclusion specification class: `lockserver.pobserve.spec.PMachines.MutualExclusion.Supplier`

        * ResponseOnlyOnRequest specification class: `lockserver.pobserve.spec.PMachines.ResponseOnlyOnRequest.Supplier`


## Running JUnit Tests in Lock Server Package

**1. Navigate to the Lock Server Package**

```bash
cd P/Src/PObserve/Examples/LockServer/
```

**2. Build the Project**

The PObserve integrated JUnit tests are executed during the build process:
```bash
gradle wrapper
./gradlew build
```

**2. Running Unit Tests**
```bash
./gradlew test
```

??? success "Expected Output"
    Running `./gradlew test` runs all the pobserve integrated unit tests in the LockServer package. The console output looks as follows:
    ```
    > Task :test

    LockServerExtendCustomBaseTest > basicTest() PASSED

    LockServerExtendCustomBaseTest > multipleRandomClients() PASSED

    LockServerExtendCustomBaseTest > randomClient() PASSED

    LockServerExtendCustomBaseTest > expectFail() PASSED

    LockServerExtendPObserveBaseTest > basicTest() PASSED

    LockServerExtendPObserveBaseTest > multipleRandomClients() PASSED

    LockServerExtendPObserveBaseTest > randomClient() PASSED

    LockServerExtendPObserveBaseTest > expectFail() PASSED

    LockServerTest > testReleaseLockFail() PASSED

    LockServerTest > testAcquireLockFail() PASSED

    LockServerTest > testAcquireAndReleaseLock() PASSED

    LockServerTest > testReleaseLockNotHeldByClient() PASSED

    LockTest > testReleaseLock() PASSED

    LockTest > testAcquireLock() PASSED

    LockTest > testAcquireLockAlreadyHeld() PASSED

    LockTest > testReleaseLockNotHeldByClient() PASSED

    LockTest > testReleaseLockWhenNotLocked() PASSED

    > Task :test

    StructuredLoggerTest > testAllValuesFilled() PASSED

    StructuredLoggerTest > testValuesNull() PASSED

    TransactionLoggerTest > testLogReleaseRequest() PASSED

    TransactionLoggerTest > testLogLockResp() PASSED

    TransactionLoggerTest > testLogReleaseResp() PASSED

    TransactionLoggerTest > testLogLockRequest() PASSED

    BUILD SUCCESSFUL in 6s
    4 actionable tasks: 4 executed
    ```

!!! success ""
    :confetti_ball: Congratulations! You have successfully set up and run PObserve with Java JUnit integration for real-time specification monitoring on a Lock Server Example!
