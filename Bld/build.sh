#!/usr/bin/env bash
# build.sh - Build script for the P Compiler
#
# Usage:
#   ./build.sh [options]
#
# Options:
#   -c, --config <config>  Build configuration (Debug|Release, default: Release)
#   -v, --verbose          Display detailed build output
#   -h, --help             Show this help message
#   --skip-submodules      Skip updating git submodules
#   --install              Install P as a global dotnet tool after building
#   --version <version>    Specify version when installing (default: 1.0.0-local)

# Terminal colors
NOCOLOR='\033[0m'
RED='\033[0;31m'
GREEN='\033[0;32m'
ORANGE='\033[0;33m'
BLUE='\033[0;34m'

# Default values
CONFIG="Release"
VERBOSE="q"
UPDATE_SUBMODULES=true
INSTALL_TOOL=false
TOOL_VERSION="1.0.0-local"

# Function to display usage information
usage() {
    echo -e "${BLUE}Build script for the P Compiler${NOCOLOR}"
    echo
    echo "Usage: $0 [options]"
    echo
    echo "Options:"
    echo "  -c, --config <config>  Build configuration (Debug|Release, default: Release)"
    echo "  -v, --verbose          Display detailed build output"
    echo "  -h, --help             Show this help message"
    echo "  --skip-submodules      Skip updating git submodules"
    echo "  --install              Install P as a global dotnet tool after building"
    echo "  --version <version>    Specify version when installing (default: 1.0.0-local)"
    echo
    exit 1
}

# Function to check if a command exists
command_exists() {
    command -v "$1" &> /dev/null
}

# Function to display error message and exit
error_exit() {
    echo -e "${RED}ERROR: $1${NOCOLOR}" >&2
    exit 1
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--config)
            CONFIG="$2"
            if [[ "$CONFIG" != "Debug" && "$CONFIG" != "Release" ]]; then
                echo -e "${RED}ERROR: Invalid configuration: $CONFIG. Must be Debug or Release.${NOCOLOR}" >&2
                exit 1
            fi
            shift 2
            ;;
        -v|--verbose)
            VERBOSE="n"
            shift
            ;;
        -h|--help)
            usage
            ;;
        --skip-submodules)
            UPDATE_SUBMODULES=false
            shift
            ;;
        --install)
            INSTALL_TOOL=true
            shift
            ;;
        --version)
            TOOL_VERSION="$2"
            shift 2
            ;;
        *)
            error_exit "Unknown option: $1"
            ;;
    esac
done

# Check prerequisites
if ! command_exists dotnet; then
    error_exit "dotnet is not installed. Please install .NET SDK."
fi

if ! command_exists git; then
    error_exit "git is not installed. Please install git."
fi

# Get script directory and navigate to project root
BLD_PATH=$(dirname "$(readlink -f "$0" 2>/dev/null || echo "$0")")
pushd "$BLD_PATH/.." > /dev/null || error_exit "Failed to navigate to project root directory"

# Set the binary path based on configuration
BINARY_PATH="${PWD}/Bld/Drops/${CONFIG}/Binaries/net8.0/p.dll"

# Initialize submodules if needed
if [ "$UPDATE_SUBMODULES" = true ]; then
    echo -e "${ORANGE} ---- Fetching git submodules ----${NOCOLOR}"
    git submodule update --init --recursive || error_exit "Failed to update git submodules"
else
    echo -e "${BLUE} ---- Skipping git submodules update ----${NOCOLOR}"
fi

echo -e "${ORANGE} ---- Building the PCompiler (${CONFIG} mode) ----${NOCOLOR}"

# Run the build with proper error handling
if [ "$VERBOSE" = "n" ]; then
    dotnet build -c "$CONFIG" -v n || error_exit "Build failed"
else
    dotnet build -c "$CONFIG" -v q || error_exit "Build failed"
fi

# Check if the binary exists
if [ -f "$BINARY_PATH" ]; then
    echo -e "${GREEN} ----------------------------------${NOCOLOR}"
    echo -e "${GREEN} P Compiler successfully built!${NOCOLOR}"
    echo -e "${GREEN} ----------------------------------${NOCOLOR}"
    echo -e "${GREEN} P Compiler located at:${NOCOLOR}"
    echo -e "${ORANGE} ${BINARY_PATH}${NOCOLOR}"
    echo -e "${GREEN} ----------------------------------${NOCOLOR}"
    echo -e "${GREEN} Recommended shortcuts:${NOCOLOR}"
    echo -e "${ORANGE} alias pl='dotnet ${BINARY_PATH}'${NOCOLOR}"
    echo -e "${GREEN} ----------------------------------${NOCOLOR}"
else
    error_exit "Build completed but binary not found at expected location: ${BINARY_PATH}"
fi

# Return to original directory
popd > /dev/null

# Install P as a global dotnet tool if requested
if [ "$INSTALL_TOOL" = true ]; then
    echo -e "${ORANGE} ---- Installing P as a global dotnet tool ----${NOCOLOR}"
    
    # Store original directory to return to it later
    ORIG_DIR=$(pwd)
    
    # Navigate to the PCommandLine directory where the project is located
    cd "$BLD_PATH/../Src/PCompiler/PCommandLine" || error_exit "Failed to navigate to PCommandLine directory"
    
    # Uninstall existing P tool if present
    echo -e "${BLUE} ---- Uninstalling existing P tool ----${NOCOLOR}"
    dotnet tool uninstall --global P 2>/dev/null || true
    
    # Create publish directory if it doesn't exist and ensure it's empty
    PUBLISH_DIR="$(pwd)/publish"
    rm -rf "$PUBLISH_DIR"
    mkdir -p "$PUBLISH_DIR"
    
    # Pack the tool
    echo -e "${BLUE} ---- Packing P tool (version: ${TOOL_VERSION}) ----${NOCOLOR}"
    dotnet pack PCommandLine.csproj \
        --configuration "$CONFIG" \
        --output "$PUBLISH_DIR" \
        -p:PackAsTool=true \
        -p:ToolCommandName=P \
        -p:Version="$TOOL_VERSION" \
        -p:GeneratePackageOnBuild=true || error_exit "Failed to pack P tool"
    
    # List the created packages
    echo -e "${BLUE} ---- Generated packages: ----${NOCOLOR}"
    ls -la "$PUBLISH_DIR"
    
    # Find the generated package (using version number as the reliable identifier)
    PACKAGE_PATH=$(find "$PUBLISH_DIR" -iname "*.${TOOL_VERSION}.nupkg" -type f)
    
    if [ -z "$PACKAGE_PATH" ]; then
        # Try a more flexible search if the specific version format isn't found
        PACKAGE_PATH=$(find "$PUBLISH_DIR" -iname "*.nupkg" -type f | grep -i "${TOOL_VERSION}")
    fi
    
    if [ -z "$PACKAGE_PATH" ]; then
        echo -e "${RED} ---- Available packages in $PUBLISH_DIR: ----${NOCOLOR}"
        ls -la "$PUBLISH_DIR"
        error_exit "Package file for version ${TOOL_VERSION} not found in $PUBLISH_DIR"
    fi
    
    echo -e "${BLUE} ---- Found package: $PACKAGE_PATH ----${NOCOLOR}"
    
    # Extract filename from path
    PACKAGE_FILENAME=$(basename "$PACKAGE_PATH")
    
    # Install the tool globally from the specific package
    echo -e "${BLUE} ---- Installing P tool globally ----${NOCOLOR}"
    echo -e "${BLUE} ---- Using package: $PACKAGE_FILENAME ----${NOCOLOR}"
    
    # First attempt: install using the standard approach
    if ! dotnet tool install P --global --add-source "$PUBLISH_DIR" --version "$TOOL_VERSION"; then
        echo -e "${ORANGE} ---- Standard installation failed, trying alternate method ----${NOCOLOR}"
        
        # Second attempt: install directly using the package path
        if ! dotnet tool install P --global --add-source "$PUBLISH_DIR"; then
            echo -e "${RED} ---- Both installation methods failed ----${NOCOLOR}"
            echo -e "${RED} ---- Debug information: ----${NOCOLOR}"
            echo -e "${RED} Package directory contents: ${NOCOLOR}"
            ls -la "$PUBLISH_DIR"
            echo -e "${RED} NuGet configuration: ${NOCOLOR}"
            dotnet nuget locals all --list
            error_exit "Failed to install P tool"
        fi
    fi
    
    # Verify installation by checking the version
    echo -e "${BLUE} ---- Verifying installation ----${NOCOLOR}"
    INSTALL_PATH=$(which P)
    echo "Installed at: $INSTALL_PATH"
    
    # Capture version information
    VERSION_OUTPUT=$(P --version || error_exit "Failed to verify P tool installation")
    echo "$VERSION_OUTPUT"
    
    # Check if reported version matches package version
    if ! echo "$VERSION_OUTPUT" | grep -q "$TOOL_VERSION"; then
        echo -e "${ORANGE} ---- NOTE: The package version (${TOOL_VERSION}) differs from the assembly version ----${NOCOLOR}"
        echo -e "${ORANGE} ---- This is expected if you haven't updated the assembly version information ----${NOCOLOR}"
        echo -e "${ORANGE} ---- The installed tool is using your local build with the assembly version shown above ----${NOCOLOR}"
        echo -e "${ORANGE} ---- To update the assembly version, modify the AssemblyInfo.cs file ----${NOCOLOR}"
    fi
    
    # Return to original directory
    cd "$ORIG_DIR" || true
    
    echo -e "${GREEN} ----------------------------------${NOCOLOR}"
    echo -e "${GREEN} P tool installed successfully!${NOCOLOR}"
    echo -e "${GREEN} ----------------------------------${NOCOLOR}"
    echo -e "${GREEN} Package version: ${TOOL_VERSION}${NOCOLOR}"
    echo -e "${GREEN} You can now use 'P' command globally${NOCOLOR}"
    echo -e "${GREEN} ----------------------------------${NOCOLOR}"
fi

echo -e "${GREEN}Build completed successfully!${NOCOLOR}"
exit 0
