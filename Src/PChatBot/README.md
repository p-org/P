# P-Chatbot

AI-powered code generation for the P programming language.

## Features

- **Multi-Provider LLM Support** - Snowflake Cortex, AWS Bedrock, Direct Anthropic
- **Streamlit Web App** - Interactive chatbot UI for design doc → P code
- **MCP Server for Cursor** - IDE integration with 13 tools and 14 resources
- **Human-in-the-Loop** - Automatic error fixing with fallback to user guidance

## Interfaces

| Interface | Use Case |
|-----------|----------|
| **Streamlit UI** | Interactive code generation from design documents |
| **MCP Server** | Cursor IDE integration for step-by-step generation |
| **Services API** | Programmatic access from Python code |

## Quick Start

### 1. Set up Python Environment

```bash
cd Src/PChatBot
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
```

### 2. Configure Snowflake Cortex

Create a `.env` file in the `Src/PChatBot` directory:

```bash
# .env file for Snowflake Cortex
export OPENAI_API_KEY=<your-snowflake-programmatic-access-token>
export OPENAI_BASE_URL=https://<your-account>.snowflakecomputing.com/api/v2/cortex/inference:chat
export OPENAI_MODEL_NAME=claude-3-5-sonnet
```

**Available Models (Snowflake Cortex):**
- `claude-3-5-sonnet` - Claude Sonnet 4.5 (recommended)
- `claude-3-5-haiku` - Claude Opus 4.5

### 3. Run the Chatbot

```bash
cd Src/PChatBot
source venv/bin/activate
source .env
streamlit run src/app.py
```

The chatbot will open in your default browser.

---

## Environment Variables Reference

| Variable | Required | Description |
|----------|----------|-------------|
| `OPENAI_API_KEY` | Yes | Snowflake Programmatic Access Token (PAT) |
| `OPENAI_BASE_URL` | Yes | Snowflake Cortex endpoint URL |
| `OPENAI_MODEL_NAME` | No | Model name (default: `claude-3-5-sonnet`) |

---

## Getting a Snowflake Programmatic Access Token

1. Log into your Snowflake account
2. Go to **Admin** → **Security** → **Programmatic Access Tokens**
3. Create a new token with appropriate permissions for Cortex
4. Copy the token and use it as `OPENAI_API_KEY`

---

## Using AWS Bedrock (Alternative)

If you prefer to use AWS Bedrock instead:

1. Set up AWS credentials:
```bash
vim ~/.aws/credentials
```

Add your Bedrock credentials:
```
[default]
aws_access_key_id=<access key>
aws_secret_access_key=<secret access key>
```

2. Set the provider in your `.env`:
```bash
export LLM_PROVIDER=bedrock
```

---

## Troubleshooting

### Common Issues

1. **"401 Unauthorized" or "Programmatic access token is invalid"**
   - Verify your token is valid and not expired
   - Ensure the token has permissions for Cortex API
   - Check the base URL is correct for your Snowflake account

2. **Connection errors**
   - Verify your `OPENAI_BASE_URL` is correct
   - Ensure network connectivity to Snowflake

3. **Model not found errors**
   - Check that `OPENAI_MODEL_NAME` is set to a valid model
   - Verify the model is available in your Snowflake region

---

## MCP Server for Cursor IDE

The P ChatBot can be run as an MCP (Model Context Protocol) server, enabling AI-assisted P code generation directly within Cursor IDE.

### Setup MCP Server

1. **Install dependencies**:
```bash
cd Src/PChatBot
source venv/bin/activate
pip install fastmcp
```

2. **Configure Cursor**:

Add the MCP server to your Cursor settings. Edit `~/.cursor/mcp.json` (or create it):

```json
{
  "mcpServers": {
    "p-chatbot": {
      "command": "python",
      "args": ["-m", "src.ui.mcp.server"],
      "cwd": "/absolute/path/to/P/Src/PChatBot",
      "env": {
        "OPENAI_API_KEY": "your-snowflake-pat-token",
        "OPENAI_BASE_URL": "https://your-account.snowflakecomputing.com/api/v2/cortex/openai",
        "OPENAI_MODEL_NAME": "claude-3-5-sonnet"
      }
    }
  }
}
```

3. **Restart Cursor** to load the MCP server.

### Available MCP Tools (13 tools)

| Category | Tool | Description |
|----------|------|-------------|
| **Generation** | `generate_project_structure` | Create P project skeleton (PSrc/, PSpec/, PTst/) |
| | `generate_types_events` | Generate types, enums, and events file |
| | `generate_machine` | Generate a single P state machine (two-stage) |
| | `generate_spec` | Generate specification/monitor file |
| | `generate_test` | Generate test file |
| **Compilation** | `p_compile` | Compile P project |
| | `p_check` | Run PChecker verification |
| **Fixing** | `fix_compiler_error` | Fix compilation errors (with auto-retry) |
| | `fix_checker_error` | Fix PChecker errors (with auto-retry) |
| | `fix_iteratively` | Iteratively fix all compilation errors |
| **Query** | `syntax_help` | Get P language syntax help |
| | `list_project_files` | List all P files in a project |
| | `read_p_file` | Read contents of a P file |

### Available MCP Resources

| Resource URI | Description |
|--------------|-------------|
| `p://guides/syntax` | Complete P syntax reference |
| `p://guides/basics` | P language fundamentals |
| `p://guides/machines` | State machine patterns |
| `p://guides/types` | Type system guide |
| `p://guides/events` | Event handling guide |
| `p://guides/specs` | Specification monitors guide |
| `p://guides/tests` | Test cases guide |
| `p://examples/fewshot` | Few-shot code examples |
| `p://about` | About P language |

### Human-in-the-Loop Error Fixing

The `fix_compiler_error` and `fix_checker_error` tools implement automatic retry with human fallback:

1. **Automatic attempts** - The tool tries up to 3 automated fixes
2. **Request guidance** - If all attempts fail, returns `needs_guidance: true` with:
   - `error_summary` - Brief description of the issue
   - `attempted_fixes` - What was tried
   - `suggested_questions` - Questions to ask the user
3. **Guided fix** - Call the tool again with `user_guidance` parameter containing the user's hint

Example workflow:
```
→ fix_compiler_error(attempt_number=1) # Fails
→ fix_compiler_error(attempt_number=2) # Fails  
→ fix_compiler_error(attempt_number=3) # Fails, returns needs_guidance=true
→ Ask user for guidance
→ fix_compiler_error(user_guidance="The type should be...") # Success!
```

### Typical P Code Generation Workflow

1. **Create project**: `generate_project_structure(design_doc, output_dir)`
2. **Generate types**: `generate_types_events(design_doc, project_path)`
3. **Generate machines**: `generate_machine(machine_name, design_doc, project_path)` (repeat for each)
4. **Compile**: `p_compile(project_path)`
5. **Fix errors**: `fix_compiler_error(...)` if needed
6. **Generate specs**: `generate_spec(spec_name, design_doc, project_path)`
7. **Generate tests**: `generate_test(test_name, design_doc, project_path)`
8. **Verify**: `p_check(project_path)`
9. **Fix checker errors**: `fix_checker_error(...)` if needed

### Running MCP Server Standalone

For testing, you can run the MCP server directly:

```bash
cd Src/PChatBot
source venv/bin/activate
source .env
python -m src.ui.mcp.server
```

---

## .cursorrules

A `.cursorrules` file is included with P language essentials for Cursor's AI assistant. It contains:
- P syntax patterns
- Common mistakes to avoid
- Available MCP tools reference
- Human-in-the-loop guidance instructions
