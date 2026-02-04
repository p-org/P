# PObserveJavaUnitTest

## Overview

PObserveBaseTest automatically checks the correctness of the implementation by checking unit test logs against the specified monitors in PSpec.

## Prerequisites

- JDK 17 or higher
- Maven 3.6.0 or higher

## Build and Run Instructions

### 1. Building the Project

To build the project, run:

```bash
mvn clean compile
```

This will compile the main source code in `src/main/java`.

### 2. Running Tests

To compile and run the tests:

```bash
mvn test
```

### 3. Building a JAR Package

To create a JAR package of the library:

```bash
mvn package
```

### 4. Complete Build

For a full build including tests, static analysis, and documentation:

```bash
mvn clean install
```
