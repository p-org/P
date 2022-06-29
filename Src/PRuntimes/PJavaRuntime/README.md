# Java PRT

This project is the Java runtime for executing monitors compiled by the P Java
backend.  It reimplements a subset of the Coyote state machine runtime.

## Building

```
$ mvn compile
```

## Testing

```
$ mvn test
```

## Installation

This builds and places the compiled JAR in your local Maven repository, which
by default is located at `~/.m2/`.

```
$ mvn install
...
[INFO] Installing target/PJavaRuntime-1.0-SNAPSHOT.jar to /Users/nathta/.m2/repository/p/runtime/PJavaRuntime/1.0-SNAPSHOT/PJavaRuntime-1.0-SNAPSHOT.jar
[INFO] Installing pom.xml to /Users/nathta/.m2/repository/p/runtime/PJavaRuntime/1.0-SNAPSHOT/PJavaRuntime-1.0-SNAPSHOT.pom
```
