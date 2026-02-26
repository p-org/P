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

# Configuration is loaded from ~/.peasyai/settings.json (no .env file needed)

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
- `~/.peasyai/settings.json`: PeasyAI configuration (LLM provider credentials, model selection, compiler paths)
- `Src/PeasyAI/requirements.txt`: Python dependencies for PeasyAI
- `Src/PeasyAI/resources/context_files/`: P language context and documentation for AI assistance

## Development Workflow

1. **Local Development**: Use `./Bld/build.sh` for full builds, or `dotnet build` for faster C#-only builds
2. **Testing**: Always run `dotnet test` before commits; tutorial tests validate end-to-end functionality
3. **Cross-platform Support**: The project supports Windows, macOS, and Ubuntu - check CI workflows for platform-specific considerations
4. **Java Components**: PEx components require JDK 11+ and Maven; use cached dependencies in CI
5. **PeasyAI Development**: PeasyAI requires Python 3.x and LLM provider credentials configured in `~/.peasyai/settings.json`. See `Src/PeasyAI/.peasyai-schema.json` for the config schema

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
- `peasy-ai-validate-env`: Validate P toolchain and LLM provider environment
- `peasy-ai-create-project`: Create new P project skeleton (Step 1)
- `peasy-ai-gen-types-events`: Generate Enums_Types_Events.p file (Step 2)
- `peasy-ai-gen-machine`: Generate a state machine (Step 3)
- `peasy-ai-gen-spec`: Generate specification monitor (Step 4)
- `peasy-ai-gen-test`: Generate test file (Step 5)
- `peasy-ai-save-file`: Save generated code to disk with compilation check
- `peasy-ai-compile`: Compile the P project
- `peasy-ai-check`: Run PChecker verification
- `peasy-ai-fix-compile-error`: Fix a single compilation error using AI
- `peasy-ai-fix-checker-error`: Fix PChecker verification errors using AI
- `peasy-ai-fix-all`: Iteratively compile and fix all errors
- `peasy-ai-fix-bug`: Auto-diagnose and fix PChecker failures from trace files
- `peasy-ai-syntax-help`: Get P language syntax help
- `peasy-ai-search-examples`: Search P program corpus for similar examples
- `peasy-ai-get-context`: Get contextual examples and hints for code generation
- `peasy-ai-index-examples`: Index P files into the examples database
- `peasy-ai-run-workflow`: Execute predefined workflows (compile_and_fix, full_verification, quick_check, full_generation)
- `peasy-ai-resume-workflow`: Resume a paused workflow with user guidance
- `peasy-ai-list-workflows`: List available and active workflows

### PeasyAI Code Review / Validation Pipeline

Every MCP generation tool (`peasy-ai-gen-types-events`, `peasy-ai-gen-machine`, `peasy-ai-gen-spec`, `peasy-ai-gen-test`) runs generated P code through a two-stage validation pipeline before returning it. The entry point is `_review_generated_code()` in `Src/PeasyAI/src/ui/mcp/tools/generation.py`, which delegates to `ValidationPipeline` in `Src/PeasyAI/src/core/validation/pipeline.py`.

#### Stage 1 — Deterministic Auto-Fixes (`PCodePostProcessor`)

Located in `Src/PeasyAI/src/core/compilation/p_post_processor.py`. Applies regex-based transformations that fix the most common LLM mistakes without any ambiguity:

- Variable declaration reordering (hoist `var` to top of function)
- Single-field tuple trailing comma insertion
- Named-field tuple construction → positional form
- Enum dot-access removal (`tEnum.VALUE` → `VALUE`)
- Entry function syntax (`entry Fn()` → `entry Fn;`)
- Bare `halt;` → `raise halt;`
- Forbidden keywords in spec monitors (detect + auto-remove `this as machine`)
- Missing semicolons after `return`
- Test declaration generation for PTst files missing `test` declarations

To add a new auto-fix, add a `_fix_*` method to `PCodePostProcessor` and call it from `process()`.

#### Stage 2 — Structured Validators

Located in `Src/PeasyAI/src/core/validation/validators.py`. Each validator is a class that extends `Validator` and implements `validate(code, context) -> ValidationResult`. The pipeline runs all validators in order and applies any auto-fixes they provide.

Current validators (registered in `pipeline.py::CORE_VALIDATORS`):

| Validator | Catches | Severity |
|---|---|---|
| `SyntaxValidator` | Unbalanced braces/parens, assignment in conditions | ERROR/WARN |
| `InlineInitValidator` | `var x: int = 0;` (auto-fixes by splitting) | ERROR |
| `VarDeclarationOrderValidator` | Vars after statements in fun/entry/handler blocks; vars inside while/foreach loops | ERROR |
| `CollectionOpsValidator` | `append()`, `receive()` (nonexistent), `seq = seq + (elem,)` (wrong concatenation) | ERROR/WARN |
| `TypeDeclarationValidator` | Types used but not declared in project | WARNING |
| `EventDeclarationValidator` | Events used in send/raise/on but not declared | WARNING |
| `MachineStructureValidator` | Missing start state, empty state bodies, goto to undefined states | ERROR/INFO |
| `SpecObservesConsistencyValidator` | Events handled but not in `observes` clause | ERROR |
| `DuplicateDeclarationValidator` | Same name declared in multiple files | ERROR |
| `SpecForbiddenKeywordValidator` | `this`/`new`/`send`/`announce`/`receive` in spec bodies | ERROR |
| `PayloadFieldValidator` | Field accesses using wrong field names in fun/entry/handler params | WARNING |
| `NamedTupleConstructionValidator` | Bare values instead of named tuples at `new`/`send` call sites; wrong/missing field names | ERROR/WARN |

Test-file-only validators (added when `is_test_file=True`):

| Validator | Catches | Severity |
|---|---|---|
| `TestFileValidator` | Missing test declarations, missing spec assertions | WARNING |

#### How to Add a New Validator

1. Create a new class in `Src/PeasyAI/src/core/validation/validators.py`:

```python
class MyNewValidator(Validator):
    name = "MyNewValidator"
    description = "What this validator checks"

    def validate(
        self, code: str, context: Optional[Dict[str, str]] = None
    ) -> ValidationResult:
        issues: List[ValidationIssue] = []

        # context is a dict of {relative_path: file_content} for other project files.
        # code is the file being validated.

        # Detect issues using regex or parsing:
        for m in re.finditer(r"some_bad_pattern", code):
            issues.append(ValidationIssue(
                severity=IssueSeverity.ERROR,  # or WARNING or INFO
                validator=self.name,
                message="Description of what's wrong",
                line_number=_line_of(code, m.start()),
                suggestion="How to fix it",
                # For auto-fixable issues:
                auto_fixable=True,
                fix_function=lambda c: c.replace("bad", "good"),
            ))

        return ValidationResult(
            is_valid=not any(i.severity == IssueSeverity.ERROR for i in issues),
            issues=issues,
            original_code=code,
        )
```

2. Register it in `Src/PeasyAI/src/core/validation/pipeline.py` by adding it to `CORE_VALIDATORS` (or `TEST_FILE_VALIDATORS` if it only applies to PTst files).

3. Export it from `Src/PeasyAI/src/core/validation/__init__.py`.

4. Add a test in `Src/PeasyAI/tests/test_validation.py`.

#### Key Design Rules

- **Severity**: Use `ERROR` for issues that will definitely cause compilation failure. Use `WARNING` for issues that might cause problems or PChecker failures. Use `INFO` for style suggestions.
- **Auto-fix**: Set `auto_fixable=True` and provide `fix_function` only when the fix is unambiguous. The `fix_function` takes the full code string and returns the fixed code string.
- **Context**: The `context` parameter contains other project files (relative path → content). Use it for cross-file checks (duplicate declarations, undefined types/events, spec assertions in tests). The file being validated is NOT in `context`.
- **Independence**: Each validator runs independently. If one crashes, the pipeline logs the error and continues with the remaining validators.
- **No LLM calls**: Validators are pure static analysis. They must be fast and deterministic. LLM-based fixing belongs in the fixer service (`Src/PeasyAI/src/core/services/fixer.py`), not in validators.
- **Code block iteration**: Use `iter_all_code_blocks(code)` from `p_code_utils.py` when you need to check all scopes where vars can be declared (fun bodies, entry blocks, on...do handlers). Use `iter_function_bodies(code)` if you only need `fun` definitions.

#### Stage 3 — LLM Wiring Review (test files only)

For test driver files, an additional LLM-based review step runs after Stages 1-2. This catches semantic issues that regex validators cannot detect:

- **Dependency ordering**: machines created before their dependencies exist
- **Circular dependency resolution**: uses init-event pattern to break cycles (e.g., Node needs FD, FD needs node set)
- **Empty collections**: `default(set[machine])` passed where populated collections are needed
- **Named tuple correctness**: field names match type definitions

Located in `GenerationService.review_test_wiring()` (`Src/PeasyAI/src/core/services/generation.py`), using the prompt `Src/PeasyAI/resources/instructions/review_test_wiring.txt`.

The review can fix multiple files at once (test driver, machine files, types/events) when resolving circular dependencies requires adding init events. Fixed files are returned in the `wiring_fixes` field of the MCP response.

#### Stage 4 — LLM Spec Correctness Review (spec files only)

For specification monitor files, an LLM-based review step runs after Stages 1-2. This catches semantic issues that regex validators cannot detect in spec monitors:

- **Observes clause completeness**: ensures the spec observes ALL events relevant to the safety property (e.g., a failure-detection spec must observe `eCrash`, `eNodeSuspected`, etc.)
- **Correct event-to-machine tracking**: verifies that event handlers use payload parameters to track specific machines, not just count events
- **Assertion logic correctness**: checks that `assert` conditions actually verify the stated safety property from the design document
- **Forbidden keywords**: catches `this`, `new`, `send`, `announce`, `receive` which are illegal in spec monitors
- **Payload type matching**: ensures handler parameter types match `Enums_Types_Events.p` declarations

Located in `GenerationService.review_spec_correctness()` (`Src/PeasyAI/src/core/services/generation.py`), using the prompt `Src/PeasyAI/resources/instructions/review_spec_correctness.txt`.

The review can fix the spec file and, rarely, other files (e.g., types/events). Fixed files are returned in the `spec_fixes` field of the MCP response.

#### Pipeline Data Flow

The `ValidationPipeline` is the **single place** where post-processing and validation happen. Both the MCP tool path and the workflow step path call it. `GenerationService._extract_p_code()` does NOT run any post-processing — it only extracts code from LLM responses.

```
LLM generates code
       │
       ▼
┌─────────────────────────────┐
│ _extract_p_code()           │  extraction only (XML tags / markdown blocks)
│  (services/generation.py)   │  no post-processing
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ Stage 1: PCodePostProcessor │  deterministic regex auto-fixes
│  (p_post_processor.py)      │  returns PostProcessResult with fixed code + fix list
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ Stage 2: Validator Chain    │  structured checks, each returns ValidationResult
│  (validators.py)            │  auto-fixable issues get applied to code
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ Stage 3: LLM Wiring Review  │  test files only — checks initialization order,
│  (services/generation.py)    │  circular deps, empty collections
│  review_test_wiring()        │  can fix test + machine + types files
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ Stage 4: LLM Spec Review    │  spec files only — checks observes completeness,
│  (services/generation.py)    │  assertion logic, payload usage, forbidden keywords
│  review_spec_correctness()   │  can fix spec + types files
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ PipelineResult + fixes       │  is_valid, fixed_code, issues[], fixes_applied[]
│  .to_review_dict()           │  + wiring_fixes / spec_fixes for cross-file changes
└─────────────────────────────┘
```

Two call sites invoke the pipeline:
- **MCP tools**: `_review_generated_code()` in `tools/generation.py` — returns `to_review_dict()` for the MCP response. For test files, `review_test_wiring()` runs as Stage 3. For spec files, `review_spec_correctness()` runs as Stage 4.
- **Workflow steps**: `_run_validation_pipeline()` in `workflow/p_steps.py` — returns the fixed code string

#### MCP Response Severity

Generation tool responses include top-level `is_valid`, `error_count`, and `warning_count` fields so the calling agent can decide whether to save the file or request regeneration. The `message` field changes based on severity:
- **No issues**: "Code generated for preview. Use peasy-ai-save-file to save to disk."
- **Warnings only**: "Code generated for preview with warnings. ..."
- **Errors**: "Code generated with N validation error(s) that will likely cause compilation failure. ..."

#### Running Tests

```bash
cd Src/PeasyAI
source venv/bin/activate
PYTHONPATH=src pytest tests/test_validation.py -v
```

### Common PeasyAI Tasks:
```bash
# Run PeasyAI tests
cd Src/PeasyAI && source venv/bin/activate
PYTHONPATH=src pytest tests/ -v

# Run just validation tests
PYTHONPATH=src pytest tests/test_validation.py -v

# Run MCP contract tests
PYTHONPATH=src pytest tests/test_mcp_contracts.py -v

# Run workflow persistence tests
PYTHONPATH=src pytest tests/test_workflow_persistence.py -v
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