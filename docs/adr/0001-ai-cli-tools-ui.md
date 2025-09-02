# ADR 0001: AI CLI Tools Web UI Service

## Status
Accepted

## Context
We are building a Blazor Server web UI that provides a unified interface to multiple CLI-based AI tools with filesystem navigation, git operations, and a terminal. The project targets .NET 9 and uses an Aspire AppHost for local composition. Earlier drafts referenced PTY libraries that we will not use.

## Decision
- CLI execution: Use CliWrap for process orchestration, streaming stdout/stderr, cancellation, and stdin via anonymous pipes. No external PTY library.
- Terminal UX: XTerm.js in the browser with JS interop for input/output; preserve ANSI codes in output.
- Aspire alignment: Add a minimal `ServiceDefaults` library and expose `/health` for liveness checks.
- Filesystem: Use `SandboxedFileSystemProvider` + `FileSystemService` for safe access.
- Git: Use LibGit2Sharp for repository status, diffs, branches, and commits.

## Constraints (Explicit)
- Do not introduce Redis for session state or caching.
- Do not use PostgreSQL; persist local data with SQLite via EF Core.
- Do not use Seq or Elasticsearch; keep logging minimal with built-in providers (Console, optional simple file logs if added later).

## Consequences
- No OS-level PTY: Some TUI apps may not behave identically; we prioritize portability and simplicity. Signals (Ctrl+C, Ctrl+Z) are emulated via stdin/cancellation.
- Clear separation: Core services in `CodeUI.Core`; UI in `CodeUI.Web`; composition via `CodeUI.AppHost`.
- Health and readiness: `/health` endpoint is available; can be extended with real checks, tracing later.

## Implementation Notes
- CliWrap usage patterns:
  - Standard runs: `StartProcessAsync` / `ExecuteAsync`.
  - Interactive runs: `StartInteractiveProcessAsync` with PipeTarget delegates and stdin via anonymous pipes.
  - ANSI handling: preserved for now; can be parsed/converted in UI later.
- ServiceDefaults: lightweight extension methods `AddServiceDefaults()` and `MapDefaultEndpoints()` to mirror Aspire templates.

## Future Work
- Optional SignalR hub for multiplexed terminals and remote sessions.
- Optional OpenTelemetry wiring, limited to Console exporter only (no external vendors).
- Expand health checks (e.g., DB connectivity, workspace access).
