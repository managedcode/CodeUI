# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Test Commands

```bash
# Build the solution
dotnet build CodeUI.slnx

# Run all tests  
dotnet test CodeUI.slnx

# Run with coverage
dotnet test CodeUI.slnx --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test CodeUI.Tests
dotnet test CodeUI.AspireTests

# Run specific test
dotnet test --filter FullyQualifiedName~CliExecutorTests

# Format code
dotnet format

# Run application with Aspire orchestration (recommended)
dotnet run --project CodeUI.AppHost

# Run web app directly
dotnet run --project CodeUI.Web
```

## High-Level Architecture

### Core Domain Model
The application follows a clean architecture pattern with clear separation of concerns:

**CodeUI.Core** - Business logic and domain models
- **Services**: Core application services with DI-friendly interfaces
  - `CliExecutor`: Manages AI CLI tool processes (Claude Code, Gemini, Codex)
  - `GitService`: Git operations using LibGit2Sharp
  - `FileSystemService`: File system operations with sandbox security
  - `DiffService`: File diff generation and comparison
- **Models**: Domain entities (`CliProcess`, `GitModels`, `DiffModels`, `FileSystemItem`)
- **Data**: EF Core DbContext and Identity models for authentication

**CodeUI.Web** - Blazor Server presentation layer
- **Components**: Reusable Blazor components organized by feature
  - `DiffViewer`: Monaco Editor-based diff visualization
  - `FileExplorer`: File system navigation component
  - Terminal and session management pages
- **Extensions**: Service registration helpers
- Uses MudBlazor component library for UI

**CodeUI.Orleans** - Distributed state management
- Orleans grains for session state and distributed computing
- Virtual Actor Model for scalable state management

**CodeUI.AppHost** - Aspire orchestration
- Configures and orchestrates the distributed application
- Provides service discovery and monitoring in development

### Service Architecture
All services follow interface-based design for testability:
- Interfaces defined alongside implementations in `CodeUI.Core/Services`
- Registered via DI in `ServiceCollectionExtensions`
- Async-first API design throughout

### Security Model
- `SandboxedFileSystemProvider`: Enforces file system access boundaries
- ASP.NET Core Identity: User authentication and authorization
- Input validation in `CliExecutor` for CLI command safety

### Data Flow
1. User interacts with Blazor components in browser
2. Server-side Blazor handles events and calls services
3. Services in CodeUI.Core execute business logic
4. Data persisted to SQLite via EF Core
5. Real-time updates pushed to client via SignalR

### Testing Strategy
- **Unit Tests** (`CodeUI.Tests`): Test individual services and components
- **Integration Tests** (`CodeUI.AspireTests`): Test full application stack with Aspire
- Uses `WebApplicationFactory` for web endpoint testing
- Mock external dependencies (processes, file system) for isolation