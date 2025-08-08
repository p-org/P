# LockServerPObserve

This project demonstrates the use of PObserve for monitoring a lock server implementation.

## Project Structure

- `src/main/java/` - Source code for the project
- `src/test/java/` - Test code
- `src/test/resources/` - Test resources including sample log files

## Building the Project

This project uses Gradle for build automation.

### Prerequisites

- Java Development Kit (JDK) 11 or later
- Gradle 7.0 or later (or use the Gradle Wrapper)
- P (>= 2.4.0)

### Commands
Generate gradle wrapper:

```bash
gradle wrapper
```

To build the project:

```bash
./gradlew build
```

To run tests:

```bash
./gradlew test
```

Building the project will create the project jar in the `build/libs/` directory with the name `LockServerPObserve-1.0.0-uber.jar`.
