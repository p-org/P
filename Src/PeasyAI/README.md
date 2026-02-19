# PeasyAI

AI-powered code generation, compilation, and formal verification for the [P programming language](https://p-org.github.io/P/).

PeasyAI exposes an MCP (Model Context Protocol) server that works with **Cursor** and **Claude Code**, giving you 27 tools and 14 resources for generating P state machines from design documents, compiling them, and verifying correctness with PChecker.

## Features

- **Design Doc → Verified P Code** — Generate types, state machines, safety specs, and test drivers from a plain-text design document
- **Multi-Provider LLM Support** — Snowflake Cortex, AWS Bedrock, Direct Anthropic
- **Ensemble Generation** — Best-of-N candidate selection for higher quality code
- **Auto-Fix Pipeline** — Automatically fix compilation errors and PChecker failures
- **Human-in-the-Loop** — Falls back to user guidance when automated fixing fails
- **RAG-Enhanced** — 1 200+ indexed P code examples improve generation quality

---

## Installation

### Prerequisites

| Dependency | Install |
|------------|---------|
| **Python ≥ 3.10** | [python.org](https://www.python.org/downloads/) |
| **.NET SDK** | [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) |
| **P compiler** | `dotnet tool install -g P` |

### Install PeasyAI

```bash
# Clone the repository
git clone https://github.com/p-org/P.git
cd P/Src/PeasyAI

# Install the package
pip install .
```

### Create your configuration

```bash
peasyai-mcp init
```

This creates **`~/.peasyai/settings.json`** (similar to `~/.claude/settings.json`).
Open it and fill in the credentials for the LLM provider you want to use:

```json
{
  "llm": {
    "provider": "snowflake",
    "model": "claude-sonnet-4-5",
    "timeout": 600,
    "providers": {
      "snowflake": {
        "api_key": "your-snowflake-pat-token",
        "base_url": "https://your-account.snowflakecomputing.com/api/v2/cortex/openai"
      },
      "anthropic": {
        "api_key": "your-anthropic-key",
        "model": "claude-3-5-sonnet-20241022"
      },
      "bedrock": {
        "region": "us-west-2",
        "model_id": "anthropic.claude-3-5-sonnet-20241022-v2:0"
      }
    }
  },
  "generation": {
    "ensemble_size": 3,
    "output_dir": "./PGenerated"
  }
}
```

> **Only fill in the provider you use.** Set `"provider"` to `"snowflake"`, `"anthropic"`, or `"bedrock"`.

Verify everything is set up correctly:

```bash
peasyai-mcp config
```

---

## Add to Cursor

Edit **`~/.cursor/mcp.json`** (create the file if it doesn't exist):

```json
{
  "mcpServers": {
    "peasyai": {
      "command": "peasyai-mcp",
      "args": []
    }
  }
}
```

Restart Cursor — the PeasyAI tools will appear in the MCP panel.

## Add to Claude Code

```bash
claude mcp add peasyai -- peasyai-mcp
```

---

## Configuration Reference

All configuration lives in **`~/.peasyai/settings.json`**.

| Key | Example | Description |
|-----|---------|-------------|
| `llm.provider` | `"snowflake"` | Active provider: `snowflake`, `anthropic`, or `bedrock` |
| `llm.model` | `"claude-sonnet-4-5"` | Model name (uses provider default if omitted) |
| `llm.timeout` | `600` | Request timeout in seconds |
| `llm.providers.snowflake.api_key` | | Snowflake Programmatic Access Token |
| `llm.providers.snowflake.base_url` | | Snowflake Cortex endpoint URL |
| `llm.providers.anthropic.api_key` | | Anthropic API key |
| `llm.providers.bedrock.region` | `"us-west-2"` | AWS region |
| `llm.providers.bedrock.model_id` | | Bedrock model ID |
| `generation.ensemble_size` | `3` | Best-of-N candidates per file |
| `generation.output_dir` | `"./PGenerated"` | Default output directory |

**Precedence:** Environment variables > `~/.peasyai/settings.json` > built-in defaults.

---

## LLM Providers

### Snowflake Cortex

1. Log into your Snowflake account
2. Go to **Admin → Security → Programmatic Access Tokens**
3. Create a token with Cortex API permissions
4. Set `"provider": "snowflake"` and fill in `api_key` and `base_url`

### Anthropic (Direct API)

Set `"provider": "anthropic"` and fill in `api_key`.

### AWS Bedrock

Ensure `~/.aws/credentials` is configured, then set `"provider": "bedrock"`.

---

## MCP Tools (27)

| Category | Tool | Description |
|----------|------|-------------|
| **Generation** | `generate_project_structure` | Create P project skeleton (PSrc/, PSpec/, PTst/) |
| | `generate_types_events` | Generate types, enums, and events file |
| | `generate_machine` | Generate a single state machine (two-stage, ensemble) |
| | `generate_spec` | Generate safety specification / monitor |
| | `generate_test` | Generate test driver |
| | `generate_complete_project` | One-shot full project generation |
| | `save_p_file` | Save generated code to disk |
| **Compilation** | `p_compile` | Compile a P project |
| | `p_check` | Run PChecker model-checking verification |
| **Fixing** | `fix_compiler_error` | Fix a single compilation error |
| | `fix_checker_error` | Fix a PChecker error from trace analysis |
| | `fix_iteratively` | Iteratively fix all compilation errors |
| | `fix_buggy_program` | Auto-diagnose and fix PChecker failures |
| **Workflows** | `run_workflow` | Execute a multi-step workflow (compile_and_fix, full_verification, etc.) |
| | `resume_workflow` | Resume a paused workflow with user guidance |
| | `list_workflows` | List available and active workflows |
| **Query** | `syntax_help` | P language syntax help by topic |
| | `list_project_files` | List all .p files in a project |
| | `read_p_file` | Read contents of a P file |
| **RAG** | `search_p_examples` | Search the P program database |
| | `get_generation_context` | Get examples to improve generation quality |
| | `index_p_examples` | Index your own P files into the corpus |
| | `get_protocol_examples` | Get examples for common protocols (Paxos, Raft, …) |
| | `get_corpus_stats` | Get corpus statistics |
| **Trace** | `explore_trace` | Explore a PChecker execution trace |
| | `query_trace_state` | Query machine state at a point in the trace |
| **Environment** | `validate_environment` | Check P toolchain, LLM provider, and config |

## MCP Resources (14)

| Resource URI | Description |
|--------------|-------------|
| `p://guides/syntax` | Complete P syntax reference |
| `p://guides/basics` | P language fundamentals |
| `p://guides/machines` | State machine patterns |
| `p://guides/types` | Type system guide |
| `p://guides/events` | Event handling guide |
| `p://guides/enums` | Enum types guide |
| `p://guides/statements` | Statements and expressions guide |
| `p://guides/specs` | Specification monitors guide |
| `p://guides/tests` | Test cases guide |
| `p://guides/modules` | Module system guide |
| `p://guides/compiler` | Compiler usage guide |
| `p://guides/common_errors` | Common compilation errors and fixes |
| `p://examples/program` | Complete P program example |
| `p://about` | About the P language |

---

## Typical Workflow

The recommended step-by-step workflow for generating verified P code:

1. **Create project** — `generate_project_structure(design_doc, output_dir)`
2. **Generate types** — `generate_types_events(design_doc, project_path)` → review → `save_p_file`
3. **Generate machines** — `generate_machine(name, design_doc, project_path)` for each → review → `save_p_file`
4. **Generate spec** — `generate_spec("Safety", design_doc, project_path)` → review → `save_p_file`
5. **Generate test** — `generate_test("TestDriver", design_doc, project_path)` → review → `save_p_file`
6. **Compile** — `p_compile(project_path)`
7. **Fix errors** — `fix_iteratively(project_path)` if compilation fails
8. **Verify** — `p_check(project_path)` to run PChecker
9. **Fix bugs** — `fix_buggy_program(project_path)` if PChecker finds issues

Or use **`run_workflow("full_verification", project_path)`** to automate steps 6–9.

---

## Human-in-the-Loop Error Fixing

The `fix_compiler_error` and `fix_checker_error` tools try up to 3 automated fixes. If all fail, they return `needs_guidance: true` with diagnostic questions. Call the tool again with `user_guidance` containing the user's hint:

```
→ fix_compiler_error(...)             # attempt 1 — fails
→ fix_compiler_error(...)             # attempt 2 — fails
→ fix_compiler_error(...)             # attempt 3 — fails, returns needs_guidance=true
→ Ask user for guidance
→ fix_compiler_error(user_guidance="The type should be…")  # succeeds
```

---

## Running the MCP Server Standalone

```bash
# Installed
peasyai-mcp

# Development (from source)
cd Src/PeasyAI
.venv/bin/python -m ui.mcp.entry
```

## Streamlit Web App

```bash
cd Src/PeasyAI
pip install ".[streamlit]"
streamlit run src/app.py
```

## Running Tests

```bash
cd Src/PeasyAI
make test-contracts    # MCP contract tests
make regression        # full regression suite
```
