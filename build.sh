#!/bin/bash
# Cross-platform shell build script for Sims4Tools

set -e

CONFIGURATION="Release"
PLATFORM="Any CPU"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --configuration|-c)
            CONFIGURATION="$2"
            shift 2
            ;;
        --platform|-p)
            PLATFORM="$2"
            shift 2
            ;;
        --clean)
            CLEAN=true
            shift
            ;;
        --help|-h)
            echo "Usage: $0 [options]"
            echo "Options:"
            echo "  -c, --configuration  Build configuration (Debug|Release) [default: Release]"
            echo "  -p, --platform       Platform (Any CPU) [default: Any CPU]"
            echo "      --clean          Clean before building"
            echo "  -h, --help           Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

SOLUTION_FILE="sims4tools.sln"

echo "Building Sims4Tools..."
echo "Configuration: $CONFIGURATION"
echo "Platform: $PLATFORM"

if [ "$CLEAN" = true ]; then
    echo "Cleaning solution..."
    dotnet clean "$SOLUTION_FILE"
fi

echo "Restoring NuGet packages..."
dotnet restore "$SOLUTION_FILE"

echo "Building solution..."
dotnet build "$SOLUTION_FILE" \
    --configuration "$CONFIGURATION" \
    --verbosity minimal

echo "Build completed successfully!"
