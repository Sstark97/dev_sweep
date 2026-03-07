# Claude Code Setup

This project uses a multi-agent Claude Code configuration. The `.claude/` folder contains agents, commands, skills, and hooks that power the development workflow.

## What is gitignored

The following are intentionally local (not shared via git):

| Path | Reason |
|------|--------|
| `.claude/agents/` | Agent definitions — local workflow |
| `.claude/commands/` | Slash commands — local workflow |
| `.claude/settings.json` | Hooks config — local machine paths |
| `.claude/settings.local.json` | Permissions and MCP servers — personal |
| `.claude/workspace/` | Task coordination files — ephemeral |
| `.claude/backups/` | Config backups — local only |
| `.claude/memory/` | Agent memory — session-specific |

The `.claude/skills/` folder **is** committed — skills are referenced from `CLAUDE.md` and define coding standards.

## Replicating the setup

To replicate the full agent pipeline on a new machine:

### 1. Agents

Create `.claude/agents/` with these agents:

- `backend-planner.md` — analyzes tasks, creates PLAN files (model: opus)
- `backend-developer.md` — implements plans, writes C# (model: sonnet)
- `code-reviewer.md` — reviews code against DDD/ROP/clean-code standards (model: opus)
- `test-writer.md` — writes xUnit tests following AAEA pattern (model: sonnet)
- `config-auditor.md` — audits Claude Code configuration (model: opus)
- `config-implementer.md` — applies config improvements (model: sonnet)

Each agent file uses YAML frontmatter with `name`, `description`, `tools`, `model`, and `color`.

### 2. Commands

Create `.claude/commands/` with:

| Command | Purpose |
|---------|---------|
| `do-task.md` | Full pipeline: plan → implement → review |
| `plan.md` | Planning only |
| `build.md` | Run `dotnet build` |
| `test.md` | Run `dotnet test` with optional filter |
| `commit.md` | Smart microcommits with Conventional Commits |
| `review.md` | Quick code review |
| `refactor.md` | Refactor to clean-code standards |
| `progress.md` | Show workspace state |
| `audit.md` | Audit and improve Claude Code config |

### 3. Settings

Create `.claude/settings.json` with hooks:
- `PostToolUse` — auto-format `.cs` files on Edit/Write
- `Stop` — verify build passes before stopping
- `SubagentStop` — verify subagents complete their tasks

Create `.claude/settings.local.json` with:
- `permissions.allow` — scoped Bash permissions for dotnet, git, etc.
- `permissions.deny` — security denies for `~/.ssh`, `~/.aws`, `.env` files
- `enabledMcpjsonServers` — `["github", "context7"]`

### 4. Workspace

The `.claude/workspace/` directory is used for task coordination:

```
.claude/workspace/
  progress/          # PLAN-{task}.md and WIP-{task}.md files
  reviews/           # REVIEW-{task}.md files from code-reviewer
```

Run `/do-task <task description>` to start a new task through the full pipeline.
