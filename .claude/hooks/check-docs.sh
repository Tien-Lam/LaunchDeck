#!/bin/bash
# PostToolUse hook: remind about doc updates when code files change
export PATH="$PATH:/mingw64/bin"
INPUT=$(cat)
FILE_PATH=$(echo "$INPUT" | grep -o '"file_path":"[^"]*"' | head -1 | sed 's/"file_path":"//;s/"$//')

# Only check code files, not docs/tests/scripts
case "$FILE_PATH" in
  *.cs|*.xaml)
    # Skip test files
    case "$FILE_PATH" in
      *Tests*) exit 0 ;;
    esac
    # Check if any doc was modified in unstaged/staged changes
    CHANGED_DOCS=$(cd "$CLAUDE_PROJECT_DIR" && git diff --name-only HEAD 2>/dev/null | grep -E '^(docs/|README\.md)' | head -5)
    if [ -z "$CHANGED_DOCS" ]; then
      echo '{"systemMessage":"Reminder: code changed but no docs updated yet. Check if docs/ or README.md need updates for this change."}'
    fi
    ;;
esac
exit 0
