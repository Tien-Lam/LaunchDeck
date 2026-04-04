#!/bin/bash
# Pre-commit hook: build all projects (warnings as errors) + tests
# Exit 2 = block the commit, stderr is fed back to Claude
DOTNET_DIR=$(cygpath "${PROGRAMFILES:-C:\\Program Files}" 2>/dev/null || echo "/c/Program Files")/dotnet
export PATH="$PATH:$DOTNET_DIR:/mingw64/bin"
export TEMP="${TEMP:-/tmp}"
export TMP="${TMP:-/tmp}"
cd "$CLAUDE_PROJECT_DIR"

# Build all dotnet projects with warnings as errors
for proj in LaunchDeck.Shared/LaunchDeck.Shared.csproj LaunchDeck.Companion/LaunchDeck.Companion.csproj LaunchDeck.Tests/LaunchDeck.Tests.csproj; do
  echo "Building $proj..." >&2
  BUILD_OUTPUT=$(dotnet build "$proj" --nologo -v quiet -warnaserror 2>&1)
  if [ $? -ne 0 ]; then
    echo "$proj build failed or has warnings:" >&2
    echo "$BUILD_OUTPUT" >&2
    exit 2
  fi
done

# Run tests
echo "Testing..." >&2
TEST_OUTPUT=$(dotnet test LaunchDeck.Tests/ --nologo --verbosity quiet 2>&1)
if [ $? -ne 0 ]; then
  echo "Tests failed:" >&2
  echo "$TEST_OUTPUT" >&2
  exit 2
fi

exit 0
