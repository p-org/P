#!/bin/bash

# Set the target directory relative to the script location
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
TARGET_DIR="$PROJECT_ROOT/PObserveRegressionTesting/src/main/java/pobserve/testing/spec"

check_dotnet() {
    if ! command -v dotnet >/dev/null 2>&1; then
        echo "Error: dotnet is not installed. Please install .NET SDK and try again."
        exit 1
    fi
    echo "✓ dotnet is installed"
}

check_p_tool() {
    if ! dotnet tool list -g | grep -q "p "; then
        echo "Installing p tool..."
        dotnet tool install --global p
        if [ $? -ne 0 ]; then
            echo "Error: Failed to install p tool."
            exit 1
        fi
        echo "✓ p tool installed successfully"
    else
        echo "✓ p tool is already installed"
    fi
}

run_p_compile() {
    # Save current directory
    ORIGINAL_DIR=$(pwd)

    echo "Changing to directory: $TARGET_DIR"
    if ! cd "$TARGET_DIR"; then
        echo "Error: Failed to change to directory $TARGET_DIR"
        return 1
    fi

    echo "Running p compile in $(pwd)..."
    p compile --mode pobserve
    RESULT=$?

    # Return to original directory
    cd "$ORIGINAL_DIR"

    if [ $RESULT -ne 0 ]; then
        echo "Error: p compile command failed."
        return 1
    fi
    echo "✓ p compile completed successfully"
}

main() {
    check_dotnet
    check_p_tool
    run_p_compile
    EXIT_CODE=$?
    echo "Current directory: $(pwd)"
    exit $EXIT_CODE
}

# Execute main function
main
