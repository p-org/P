#!/bin/bash
# PeasyAI MCP Server launcher.
# Configuration is loaded from ~/.peasyai/settings.json
# Run  peasyai-mcp init  to create the config file.

cd /Users/adesai/workspace/public/P/Src/PeasyAI

# Add P compiler (dotnet tools) and dotnet SDK to PATH
export PATH="$HOME/.dotnet/tools:/usr/local/share/dotnet:$PATH"
export DOTNET_ROOT="/usr/local/share/dotnet"

exec /Users/adesai/workspace/public/P/Src/PeasyAI/.venv/bin/peasyai-mcp
