# System Instructions (DevSweep Architecture)

This workspace operates under the agent architecture defined in `.claude/`.
It simulates native Claude Code behavior using 4.6 models.

## 1. Sources of Truth
- **Configuration:** Always read `CLAUDE.md` for conventions.
- **Skills:** Before coding, consult `.claude/skills/` (e.g., `naming.md`, `testing.md`).

## 2. Workflow (Pipeline)
1. **Planning:** Create files in `.claude/workspace/planning/`.
2. **Implementation:** Only work on tasks that are in `.claude/workspace/progress/`.
3. **Review:** Move to `.claude/workspace/review/` for auditing.

## 3. Roles
Strictly adopt the role of the invoked agent.