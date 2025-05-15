#!/usr/bin/env bash

NOCOLOR='\033[0m'
RED='\033[0;31m'
GREEN='\033[0;32m'
ORANGE='\033[0;33m'
BLUE='\033[0;34m'
BOLD='\033[1m'

# Default values
RUN_BUILD=false
RUN_TEST=false
SKIP_P_BUILD=false
TIMEOUT="15"
SCHEDULES="100"
MAX_STEPS="10000"

# Show usage
function show_usage {
    echo -e "${BOLD}Usage:${NOCOLOR} $0 [options]"
    echo -e "  ${BLUE}--build${NOCOLOR}        Build the PEx runtime"
    echo -e "  ${BLUE}--test${NOCOLOR}         Run the PEx tests"
    echo -e "  ${BLUE}--all${NOCOLOR}          Run both build and test (default if no options specified)"
    echo -e "  ${BLUE}--skip-p-build${NOCOLOR} Skip building the P project"
    echo -e "  ${BLUE}--timeout${NOCOLOR} N    Set test timeout (default: $TIMEOUT)"
    echo -e "  ${BLUE}--schedules${NOCOLOR} N  Set test schedules (default: $SCHEDULES)"
    echo -e "  ${BLUE}--max-steps${NOCOLOR} N  Set max steps (default: $MAX_STEPS)"
    echo -e "  ${BLUE}--help${NOCOLOR}         Show this help message"
}

# Parse arguments
if [ $# -eq 0 ]; then
    # If no arguments provided, run both build and test
    RUN_BUILD=true
    RUN_TEST=true
else
    while [ "$1" != "" ]; do
        case $1 in
            --build )        RUN_BUILD=true
                             ;;
            --test )         RUN_TEST=true
                             ;;
            --all )          RUN_BUILD=true
                             RUN_TEST=true
                             ;;
            --skip-p-build ) SKIP_P_BUILD=true
                             ;;
            --timeout )      shift
                             TIMEOUT=$1
                             ;;
            --schedules )    shift
                             SCHEDULES=$1
                             ;;
            --max-steps )    shift
                             MAX_STEPS=$1
                             ;;
            --help )         show_usage
                             exit 0
                             ;;
            * )              echo -e "${RED}Unknown option: $1${NOCOLOR}"
                             show_usage
                             exit 1
                             ;;
        esac
        shift
    done
fi

# Function to handle errors
function handle_error {
    echo -e "${RED}ERROR: $1${NOCOLOR}"
    exit 1
}

# Build function
function build_pex {
    echo -e "\n${ORANGE}${BOLD}========== Building PEx runtime ==========${NOCOLOR}"
    echo -e "${BLUE}Running: mvn clean initialize${NOCOLOR}"
    mvn clean initialize || handle_error "Maven initialize failed"
    
    echo -e "${BLUE}Running: mvn install -Dmaven.test.skip${NOCOLOR}"
    mvn install -Dmaven.test.skip || handle_error "Maven install failed"
    
    if [ "$SKIP_P_BUILD" = false ]; then
        echo -e "\n${ORANGE}${BOLD}========== Building P project ==========${NOCOLOR}"
        pushd . > /dev/null
        cd ../../../P/Bld/ || handle_error "Failed to change directory to ../../../P/Bld/"
        echo -e "${BLUE}Running: ./build.sh${NOCOLOR}"
        ./build.sh || handle_error "P build script failed"
        popd > /dev/null
    else
        echo -e "\n${ORANGE}${BOLD}========== Skipping P project build as requested ==========${NOCOLOR}"
    fi
    
    echo -e "${GREEN}${BOLD}========== Build completed successfully! ==========${NOCOLOR}"
}

# Test function
function run_tests {
    echo -e "\n${ORANGE}${BOLD}========== Running PEx Tests ==========${NOCOLOR}"
    echo -e "${BLUE}Timeout: $TIMEOUT, Schedules: $SCHEDULES, Max Steps: $MAX_STEPS${NOCOLOR}"
    
    # Run tests with Maven and the specified parameters
    mvn test -Dtimeout=$TIMEOUT -Dschedules=$SCHEDULES -Dmax.steps=$MAX_STEPS || handle_error "Tests failed"
    
    echo -e "${GREEN}${BOLD}========== Tests completed successfully! ==========${NOCOLOR}"
}

# Main execution
if [ "$RUN_BUILD" = true ]; then
    build_pex
fi

if [ "$RUN_TEST" = true ]; then
    run_tests
fi

echo -e "${GREEN}${BOLD}========== All requested operations completed successfully! ==========${NOCOLOR}"
