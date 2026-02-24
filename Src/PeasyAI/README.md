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
| **Generation** | `peasy-ai-create-project` | Create P project skeleton (PSrc/, PSpec/, PTst/) |
| | `peasy-ai-gen-types-events` | Generate types, enums, and events file |
| | `peasy-ai-gen-machine` | Generate a single state machine (two-stage, ensemble) |
| | `peasy-ai-gen-spec` | Generate safety specification / monitor |
| | `peasy-ai-gen-test` | Generate test driver |
| | `peasy-ai-gen-full-project` | One-shot full project generation |
| | `peasy-ai-save-file` | Save generated code to disk |
| **Compilation** | `peasy-ai-compile` | Compile a P project |
| | `peasy-ai-check` | Run PChecker model-checking verification |
| **Fixing** | `peasy-ai-fix-compile-error` | Fix a single compilation error |
| | `peasy-ai-fix-checker-error` | Fix a PChecker error from trace analysis |
| | `peasy-ai-fix-all` | Iteratively fix all compilation errors |
| | `peasy-ai-fix-bug` | Auto-diagnose and fix PChecker failures |
| **Workflows** | `peasy-ai-run-workflow` | Execute a multi-step workflow (compile_and_fix, full_verification, etc.) |
| | `peasy-ai-resume-workflow` | Resume a paused workflow with user guidance |
| | `peasy-ai-list-workflows` | List available and active workflows |
| **Query** | `peasy-ai-syntax-help` | P language syntax help by topic |
| | `peasy-ai-list-files` | List all .p files in a project |
| | `peasy-ai-read-file` | Read contents of a P file |
| **RAG** | `peasy-ai-search-examples` | Search the P program database |
| | `peasy-ai-get-context` | Get examples to improve generation quality |
| | `peasy-ai-index-examples` | Index your own P files into the corpus |
| | `peasy-ai-get-protocol-examples` | Get examples for common protocols (Paxos, Raft, …) |
| | `peasy-ai-corpus-stats` | Get corpus statistics |
| **Trace** | `peasy-ai-explore-trace` | Explore a PChecker execution trace |
| | `peasy-ai-query-trace` | Query machine state at a point in the trace |
| **Environment** | `peasy-ai-validate-env` | Check P toolchain, LLM provider, and config |

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

1. **Create project** — `peasy-ai-create-project(design_doc, output_dir)`
2. **Generate types** — `peasy-ai-gen-types-events(design_doc, project_path)` → review → `peasy-ai-save-file`
3. **Generate machines** — `peasy-ai-gen-machine(name, design_doc, project_path)` for each → review → `peasy-ai-save-file`
4. **Generate spec** — `peasy-ai-gen-spec("Safety", design_doc, project_path)` → review → `peasy-ai-save-file`
5. **Generate test** — `peasy-ai-gen-test("TestDriver", design_doc, project_path)` → review → `peasy-ai-save-file`
6. **Compile** — `peasy-ai-compile(project_path)`
7. **Fix errors** — `peasy-ai-fix-all(project_path)` if compilation fails
8. **Verify** — `peasy-ai-check(project_path)` to run PChecker
9. **Fix bugs** — `peasy-ai-fix-bug(project_path)` if PChecker finds issues

Or use **`peasy-ai-run-workflow("full_verification", project_path)`** to automate steps 6–9.

---

## Human-in-the-Loop Error Fixing

The `peasy-ai-fix-compile-error` and `peasy-ai-fix-checker-error` tools try up to 3 automated fixes. If all fail, they return `needs_guidance: true` with diagnostic questions. Call the tool again with `user_guidance` containing the user's hint:

```
→ peasy-ai-fix-compile-error(...)             # attempt 1 — fails
→ peasy-ai-fix-compile-error(...)             # attempt 2 — fails
→ peasy-ai-fix-compile-error(...)             # attempt 3 — fails, returns needs_guidance=true
→ Ask user for guidance
→ peasy-ai-fix-compile-error(user_guidance="The type should be…")  # succeeds
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
