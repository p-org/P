#!/bin/bash
cd /Users/adesai/workspace/public/P/Src/PChatBot
source .env
export PYTHONPATH="/Users/adesai/workspace/public/P/Src/PChatBot/src:$PYTHONPATH"
# Add P compiler (dotnet tools) and dotnet SDK to PATH
export PATH="$HOME/.dotnet/tools:/usr/local/share/dotnet:$PATH"
export DOTNET_ROOT="/usr/local/share/dotnet"
exec /Users/adesai/workspace/public/P/Src/PChatBot/venv/bin/python -m src.ui.mcp.server
