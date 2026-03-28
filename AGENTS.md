# Agent Instructions

## Issue Tracking

This project uses **bd (beads)** for issue tracking.
Run `bd prime` for workflow context, or install hooks (`bd hooks install`) for auto-injection.

**PATH setup** (run first):
```bash
export PATH="$PATH:/c/Users/lamti/AppData/Local/Programs/bd:/c/Program Files/dolt/bin"
```

**Workflow -- every code change MUST follow this:**
```bash
bd create --title="..." --type=bug|task|feature --priority=2
bd update <id> --claim    # Atomic -- fails if already claimed by another agent
# ... write code ...
bd close <id>
```

**Do NOT use** TodoWrite, TaskCreate, or markdown task lists.

**Quick reference:**
```bash
bd prime                  # Full dynamic workflow context
bd ready                  # Find unblocked work
bd show <id>              # View issue details
bd worktree create <name> # Isolated worktree for parallel work
```

## Parallel Agents

When multiple agents work simultaneously:
- Each agent MUST `bd update <id> --claim` before starting -- this is atomic and prevents conflicts
- Use `bd worktree create <name>` for filesystem isolation (shared beads DB)
- Use `--actor <name>` to identify yourself in the audit trail
- Use `bd gate` for cross-agent coordination (block until another agent's work completes)
- Use `--readonly` flag in worker sandboxes that should not mutate issue state

## Non-Interactive Shell Commands

Always use non-interactive flags to avoid hanging:
```bash
cp -f source dest        # NOT: cp source dest
mv -f source dest        # NOT: mv source dest
rm -rf directory         # NOT: rm -r directory
```
