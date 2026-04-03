#!/bin/bash
# Pre-commit hook: build all projects (warnings as errors) + tests
# Exit 2 = block the commit, stderr is fed back to Claude
cd "$CLAUDE_PROJECT_DIR"

# Build Shared + Companion with warnings as errors
echo "Building..." >&2
BUILD_OUTPUT=$(dotnet build LaunchDeck.Shared/LaunchDeck.Shared.csproj --nologo -v quiet -warnaserror 2>&1)
if [ $? -ne 0 ]; then
  echo "Shared build failed or has warnings:" >&2
  echo "$BUILD_OUTPUT" >&2
  exit 2
fi

BUILD_OUTPUT=$(dotnet build LaunchDeck.Companion/LaunchDeck.Companion.csproj --nologo -v quiet -warnaserror 2>&1)
if [ $? -ne 0 ]; then
  echo "Companion build failed or has warnings:" >&2
  echo "$BUILD_OUTPUT" >&2
  exit 2
fi

# Run tests
echo "Testing..." >&2
TEST_OUTPUT=$(dotnet test LaunchDeck.Tests/ --nologo --verbosity quiet 2>&1)
if [ $? -ne 0 ]; then
  echo "Tests failed:" >&2
  echo "$TEST_OUTPUT" >&2
  exit 2
fi

exit 0
