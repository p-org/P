# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

P is a state machine based programming language for formally modeling and specifying complex distributed systems. The P framework allows programmers to model their system design as a collection of communicating state machines and provides automated reasoning backends (model checking, symbolic execution) to verify correctness specifications.

## Project Architecture

The P framework consists of four main components organized in `Src/`:

- **PCompiler** (`Src/PCompiler/`): The P language compiler that parses P programs and generates target code
  - `CompilerCore/`: Core compilation logic and AST handling
  - `PCommandLine/`: Command-line interface for the P compiler

- **PChecker** (`Src/PChecker/`): Model checker and systematic testing engine for P programs
  - `CheckerCore/`: Core model checking and systematic testing logic
  - `CoverageReportMerger/`: Tool for merging coverage reports

- **PEx** (`Src/PEx/`): Java-based execution engine that provides symbolic execution capabilities

- **PeasyAI** (`Src/PeasyAI/`): AI-powered system for P language development assistance
  - `src/core/`: Core logic including different interaction modes
  - `src/ui/`: User interface components (Streamlit web app, CLI, MCP server)
  - `src/utils/`: Utilities for code generation, compilation, and P language analysis
  - `resources/`: Context files, examples, and instruction templates for AI interactions

The project follows a multi-language approach with the compiler written in C# (.NET), the execution engine in Java with Maven build system, and the AI assistant system in Python.

## Common Development Commands

### Building the Project

```bash
# Build the entire project (recommended)
./Bld/build.sh

# Build with specific configuration
./Bld/build.sh --config Debug
./Bld/build.sh --config Release

# Build with verbose output
./Bld/build.sh --verbose

# Install P as a global dotnet tool after building
./Bld/build.sh --install
```

Alternative using dotnet CLI:
```bash
# Build all projects
dotnet build

# Build with specific configuration
dotnet build --configuration Release
dotnet build --configuration Debug
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with specific configuration
dotnet test --configuration Release

# Run specific test project
dotnet test Tst/UnitTests/UnitTests.csproj

# Run a single test by name
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Run tests in a specific category
dotnet test --filter "Category=Unit"

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Working with P Programs

```bash
# Compile a P program (requires P tool to be installed)
p compile

# Run model checking on a P program
p check

# Compile with PEx backend
p compile --mode pex

# Run model checking with PEx backend
p check --mode pex
```

### Working with PeasyAI

The PeasyAI system provides AI-assisted P language development through multiple interfaces:

```bash
# Set up PeasyAI environment
cd Src/PeasyAI
python3 -m venv venv
source venv/bin/activate
source .env

# Install dependencies
python3 -m pip install -r requirements.txt

# Run Streamlit web interface
streamlit run src/app.py

# Run CLI interface
python src/ui/cli/app.py

# Run MCP server for Claude Code integration
python src/ui/mcp/server.py
```

PeasyAI supports multiple interaction modes:
- **Design Document Input**: Generate P programs from high-level design documents
- **Interactive Mode**: Conversational assistance with P language development
- **PChecker Mode**: Automated error analysis and fixing for P programs

## Test Structure

Tests are organized in `Tst/` directory:

- `UnitTests/`: Core unit tests for the compiler and checker
- `RegressionTests/`: Regression test suite
- `PortfolioTests/`: Portfolio of P programs for testing
- `PrtTester/`: Runtime testing utilities
- `TestPCompiler/`: Compiler-specific tests

Tutorial examples in `Tutorial/` serve as integration tests and demonstrate P language features:
- `1_ClientServer/`: Basic client-server model
- `2_TwoPhaseCommit/`: Two-phase commit protocol
- `3_EspressoMachine/`: State machine modeling example
- `4_FailureDetector/`: Failure detection algorithm
- `5_Paxos/`: Paxos consensus protocol
- `6_Raft/`: Raft consensus protocol

## Key Configuration Files

- `P.sln`: Visual Studio solution file containing all projects
- `Directory.Build.props`: MSBuild properties shared across projects
- `Bld/build.sh`: Main build script with comprehensive options
- `Src/PEx/pom.xml`: Maven configuration for Java components
- `Src/PeasyAI/.env`: Environment configuration for PeasyAI (requires AWS Bedrock credentials)
- `Src/PeasyAI/requirements.txt`: Python dependencies for PeasyAI
- `Src/PeasyAI/resources/context_files/`: P language context and documentation for AI assistance

## Development Workflow

1. **Local Development**: Use `./Bld/build.sh` for full builds, or `dotnet build` for faster C#-only builds
2. **Testing**: Always run `dotnet test` before commits; tutorial tests validate end-to-end functionality
3. **Cross-platform Support**: The project supports Windows, macOS, and Ubuntu - check CI workflows for platform-specific considerations
4. **Java Components**: PEx components require JDK 11+ and Maven; use cached dependencies in CI
5. **PeasyAI Development**: PeasyAI requires Python 3.x, AWS Bedrock credentials, and specific Python dependencies. Set up `.env` file with AWS credentials before development

## Working with Tutorial Examples

Tutorial examples demonstrate P language usage and serve as integration tests:

```bash
cd Tutorial/1_ClientServer
p compile
p check -tc tcSingleClient
```

Each tutorial contains:
- `.p` files with P program source code
- `.pproj` project files
- Test configurations and specifications

## PeasyAI Architecture and Usage

PeasyAI is designed as a modular AI-assistance system with several key architectural patterns:

### Core Components:
- **Modes**: Different interaction patterns (`DesignDocInputMode`, `PCheckerMode`, `InteractiveMode`)
- **Pipelining**: Structured AI conversation flows with context management
- **RAG System**: Retrieval-Augmented Generation using P language documentation and examples
- **UI Interfaces**: Multiple front-ends (Streamlit, CLI, MCP server for Claude Code integration)

### Development Patterns:
- **Context Files**: Modular P language documentation in `resources/context_files/modular/`
- **Instruction Templates**: Reusable AI prompts in `resources/instructions/`
- **Examples and Benchmarks**: Test cases and reference implementations in `resources/p-model-benchmark/`
- **Utilities**: Helper functions for P compilation, analysis, and code generation

### PeasyAI MCP Tools for P Development:
When using the MCP server, these tools are available for P language development:
- `peasy-ai-create-project`: Create new P project skeleton
- `peasy-ai-gen-types-events`: Generate Enums_Types_Events.p file
- `peasy-ai-gen-machine`: Generate a state machine
- `peasy-ai-gen-spec`: Generate specification monitor
- `peasy-ai-gen-test`: Generate test file
- `peasy-ai-compile`: Compile the P project
- `peasy-ai-check`: Run PChecker verification
- `peasy-ai-fix-compile-error`: Automatically fix compilation errors
- `peasy-ai-fix-checker-error`: Fix PChecker verification errors
- `peasy-ai-syntax-help`: Get P language syntax help

### Common PeasyAI Tasks:
```bash
# Run evaluation metrics on AI performance
python compute_metrics.py

# Analyze P compiler errors and suggest fixes
python analyze-errors.py

# Analyze PChecker errors specifically
python analyze-checker-errors.py

# Evaluate PeasyAI responses using pass@k metrics
python evaluate_peasyai.py

# Visualize performance metrics
python visualize-pk-vs-tokens.py

# Test MCP integration with P examples
python test_mcp_paxos.py

# Run PeasyAI tests
pytest tests/

# Run pipeline tests specifically
pytest tests/pipeline/pipeline_tests.py
```

## P Language Syntax Guidelines

When working with P programs, be aware of these syntax patterns and common pitfalls:

### Reserved Keywords
Never use these as identifiers: `var`, `type`, `enum`, `event`, `on`, `do`, `goto`, `data`, `send`, `announce`, `receive`, `case`, `raise`, `machine`, `state`, `hot`, `cold`, `start`, `spec`, `module`, `test`, `main`, `fun`, `observes`, `entry`, `exit`, `with`, `union`, `foreach`, `else`, `while`, `return`, `break`, `continue`, `ignore`, `defer`, `assert`, `print`, `new`, `sizeof`, `keys`, `values`, `choose`, `format`, `if`, `halt`, `this`, `as`, `to`, `in`, `default`, `Interface`, `true`, `false`, `int`, `bool`, `float`, `string`, `seq`, `map`, `set`, `any`

### Common P Syntax Mistakes to Avoid

1. **Variable Initialization**: Separate declaration from assignment
   ```p
   // WRONG
   var x: int = 0;
   // CORRECT
   var x: int;
   x = 0;
   ```

2. **Sequence Operations**: Use index-value pairs
   ```p
   // WRONG
   mySeq += (value);
   // CORRECT
   mySeq += (sizeof(mySeq), value);
   ```

3. **Map Access**: Check existence before accessing
   ```p
   if (key in myMap) {
       var v = myMap[key];
   }
   ```

## CI/CD Integration

The project uses GitHub Actions with workflows for:
- `ubuntuci.yml`: Main CI on Ubuntu with .NET 8.0 and JDK 11
- `windowsci.yml`: Windows-specific testing
- `macosci.yml`: macOS-specific testing
- `pex.yml`: PEx-specific testing with published Maven packages
- `tutorials.yml`: Tutorial validation
- `maven-publish.yml`: Publishing PEx packages to Maven Central

All CI workflows use .NET 8.0 and maintain compatibility across platforms.