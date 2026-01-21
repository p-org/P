# PEx: Exhaustive Model Checking

PEx is an exhaustive model checker for P programs that performs a systematic search to verify correctness properties for small (finite) instances of the system. It explores all possible behaviors of the system by analyzing finite inputs and finite processes. 

## Architecture

PEx uses a multi-threaded, stateful search optimized to scale to billions of protocol states. The core architecture includes:

- **State Tracking**: Maintains record of all unexplored state transitions
- **Dynamic Prioritization**: Optimizes exploration order during search
- **State Caching**: Prevents redundant work by caching visited states  
- **Parallel Execution**: Leverages thread-level parallelism for performance

### Prerequisites

PEx requires Java 17+ and Maven 3.7+ to be installed on your system.

=== "MacOS"
    ```shell
    # Install Java 17
    brew install openjdk@17
    
    # Install Maven
    brew install maven
    ```
    Don't have Homebrew? Download [Java](https://www.oracle.com/java/technologies/javase/jdk17-archive-downloads.html) and [Maven](https://maven.apache.org/install.html) directly.

=== "Ubuntu"
    ```shell
    # Install Java 17
    sudo apt install openjdk-17-jdk
    
    # Install Maven
    sudo apt install maven
    ```

=== "Windows"
    - Download and install [Java 17](https://www.oracle.com/java/technologies/javase/jdk17-archive-downloads.html)
    - Follow the [Maven installation guide](https://maven.apache.org/install.html)

??? hint "Verify installations"
    ```shell
    java -version   # Should show version 17 or higher
    mvn -version    # Should show version 3.7 or higher
    ```

### Building with PEx

To compile a P program for exhaustive model checking:

```shell
p compile --mode pex
```

### Running PEx

Basic usage to run PEx on a test case with a 60-second timeout on tutorial #1 Client Server:

```shell
p check --mode pex -tc tcSingleClient --timeout 60
```

??? example "Expected Output"
    ```
    .. Searching for a P compiled file locally in folder ./PGenerated/
    .. Found a P compiled file: ./PGenerated/PEx/target/ClientServer-jar-with-dependencies.jar
    .. Checking ./PGenerated/PEx/target/ClientServer-jar-with-dependencies.jar
    WARNING: sun.reflect.Reflection.getCallerClass is not supported. This will impact performance.
    .. Test case :: tcSingleClient
    ... Checker is using 'random' strategy with 1 threads (seed:1754700972405)
    --------------------
    Time     Memory    Tasks (run/fin/pen)    Schedules   Timelines     States   
    00:00:28   3.0 GB         0 / 1 / 0             10          1         90,000   

    --------------------
    ... Checking statistics:
    ..... Found 0 bugs.
    ... Search statistics:
    ..... Explored 90,000 distinct states over 1 timelines
    ..... Explored 10 distinct schedules
    ..... Finished 1 search tasks (0 pending)
    ..... Number of steps explored: 0 (min), 9,000 (avg), 10,001 (max).
    ... Elapsed 28 seconds and used 6.4 GB
    ..  Result: correct up to step 10,001 
    . Done
    ... Checker run finished.
    ~~ [PTool]: Thanks for using P! ~~
    ... Checker run terminated.
    ```

    Key Metrics:

    **Exploration Results**:
    Explored 90,000 distinct states in a single timeline; Examined 10 different schedules; Reached maximum step limit of 10,001 steps; Found no bugs in explored state space
    
    **Resource Usage**: Runtime: 28 seconds; Memory: 6.4 GB peak usage; Threads: Single thread with random strategy
    
    **Results**: Completed normally (hit step limit); Verified correct up to step 10,001; All properties satisfied in explored states

### Advanced Options

??? tip "Exploring PEx Runtime Options"
    View available runtime options:
    ```shell
    # Print basic help menu
    p check --mode pex --checker-args :--help

    # Print expert help menu with advanced options
    p check --mode pex --checker-args :--help-all
    ```

### Parallel Execution

PEx supports parallel execution to speed up the verification process. To run with multiple cores on tutorial #1 Client Server:

```shell
p check --mode pex -tc tcSingleClient --timeout 60 --checker-args :--nproc:32
```

This example uses 32 cores for parallel exploration of the state space.

!!! note "Performance Tips"
    - Start with a reasonable timeout value (e.g., 60 seconds) and adjust based on your system's complexity
    - For large state spaces, use parallel execution with multiple cores
    - Consider using abstractions to reduce the state space when possible
