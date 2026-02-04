# Build and Test Scripts

This directory contains scripts for building and testing the PEx project.

## Java Requirements

The PEx project requires Java 17 to build and run. If you have multiple Java versions installed on your system, make sure to use Java 17 when building the project.

To use Java 17 for building the project:

```bash
# If using Homebrew Java 17 on macOS
export JAVA_HOME="/opt/homebrew/Cellar/openjdk@17/17.0.15/libexec/openjdk.jdk/Contents/Home"
export PATH="$JAVA_HOME/bin:$PATH"

# Then run the build script
scripts/build_and_test.sh [options]
```

## Available Scripts

### build.sh
The original build script that only builds the PEx runtime.

### build_and_test.sh
An improved script that can both build and test the project with various options.

## Usage

```bash
./build_and_test.sh [options]
```

### Options

- `--build`: Build the PEx runtime
- `--test`: Run the PEx tests
- `--all`: Run both build and test (default if no options specified)
- `--skip-p-build`: Skip building the P project (useful for faster builds when only PEx changed)
- `--timeout N`: Set test timeout (default: 15)
- `--schedules N`: Set test schedules (default: 100)
- `--max-steps N`: Set max steps (default: 10000)
- `--help`: Show the help message

### Examples

Build and test with default settings:
```bash
./build_and_test.sh
```

Only build (no tests):
```bash
./build_and_test.sh --build
```

Only run tests (no build):
```bash
./build_and_test.sh --test
```

Build without building the P project, then run tests with custom settings:
```bash
./build_and_test.sh --build --skip-p-build --test --timeout 30 --schedules 200 --max-steps 20000
