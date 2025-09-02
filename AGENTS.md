# Repository Guidelines

## Project Structure & Module Organization
- Core services: `CodeUI.Core` (Git, CLI, filesystem, diff models/services).
- Web app: `CodeUI.Web` (Blazor components under `Components`, static assets in `wwwroot`).
- App host: `CodeUI.AppHost` (.NET Aspire orchestrator for local dev).
- Optional Orleans: `CodeUI.Orleans` (grains/abstractions).
- Tests: `CodeUI.Tests` (unit/integration tests, xUnit).
- Deployment: `deployment/` (publish scripts and service files).

## Build, Test, and Development Commands
- Restore/build: `dotnet restore` then `dotnet build`.
- Run (recommended): `dotnet run --project CodeUI.AppHost` (starts Aspire host and `codeui-web`).
- Run web directly: `dotnet run --project CodeUI.Web`.
- Tests: `dotnet test` (xUnit; uses `Microsoft.AspNetCore.Mvc.Testing` for web tests).
- Coverage: `dotnet test /p:CollectCoverage=true` (coverlet collector).
- Publish (all platforms): `bash deployment/scripts/build-all.sh` (outputs to `publish/`).

## Coding Style & Naming Conventions
- Language: C# on .NET SDK `9.0` (see `global.json`).
- Indentation: 4 spaces; file-scoped namespaces; `PascalCase` for types/methods; `camelCase` for locals; private fields prefixed with `_`.
- Keep services in `CodeUI.Core/Services`, models in `CodeUI.Core/Models`, and UI in `CodeUI.Web/Components`.
- Favor async APIs; use DI-friendly interfaces (`I*Service`) colocated in `Services`.

## Testing Guidelines
- Framework: xUnit; tests live in `CodeUI.Tests` and end with `*Tests.cs`.
- Unit vs integration: pure logic in `Services/*Tests.cs`; web/Blazor uses `TestWebApplicationFactory` and `Integration/*`.
- Run focused tests: `dotnet test --filter FullyQualifiedName~CliExecutorTests`.
- Aim for coverage on new code; mock external processes and use temp dirs for filesystem tests.

## Commit & Pull Request Guidelines
- Commits: imperative, scoped summary (e.g., "Implement Diff Viewer component"), reference PR/issue when relevant.
- PRs: clear description, rationale, screenshots for UI changes, steps to test, and linked issues.
- Include tests or explain why not; keep changes constrained to one concern.

## Security & Configuration Tips
- Use `SandboxedFileSystemProvider` for any file access; never bypass sandbox boundaries.
- Store config in `appsettings.Development.json` locally; do not commit secrets.
- Validate and sanitize any CLI input passed through `CliExecutor`.

