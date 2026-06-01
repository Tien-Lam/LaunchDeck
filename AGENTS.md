# Agent Instructions

## Non-Interactive Shell Commands

Always use non-interactive flags to avoid hanging:
```bash
cp -f source dest        # NOT: cp source dest
mv -f source dest        # NOT: mv source dest
rm -rf directory         # NOT: rm -r directory
```
