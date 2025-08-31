# CodeUI
CodeUI is a comprehensive web-based AI CLI tools management application built with .NET 8 and C# 12. It provides a unified interface for managing multiple AI coding CLI tools (Claude Code, Gemini, Codex) with integrated file system navigation, git operations, and terminal access through Blazor Server-side application.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Repository Status
This repository is currently in its bootstrap phase with only:
- README.md (basic project name)
- LICENSE (MIT License)  
- .gitignore (Visual Studio/C# template)

The project is intended to become a web application providing a UI service for AI CLI tools with features including terminal emulation, file management, git integration, and session management.

## Prerequisites and Environment Setup
- .NET 8.0 SDK is available and validated to work
- C# 12 language features supported (latest with .NET 8)
- Ready for upgrade to .NET 9 and C# 13 when available in environment
- No additional SDK downloads required - environment is pre-configured
- All standard .NET development tools are available

## Working Effectively

### Bootstrapping the Project
When the repository needs actual code, use these validated commands based on the project requirements:

**Create the initial solution and Blazor Server structure:**
```bash
dotnet new sln -n CodeUI
dotnet new blazor -n CodeUI.Web --interactivity Server
dotnet new classlib -n CodeUI.Core
dotnet new classlib -n CodeUI.Orleans
```

**Add projects to solution:**
```bash
dotnet sln add CodeUI.Web/CodeUI.Web.csproj
dotnet sln add CodeUI.Core/CodeUI.Core.csproj
dotnet sln add CodeUI.Orleans/CodeUI.Orleans.csproj
```

**Set up project references:**
```bash
dotnet add CodeUI.Web/CodeUI.Web.csproj reference CodeUI.Core/CodeUI.Core.csproj
dotnet add CodeUI.Web/CodeUI.Web.csproj reference CodeUI.Orleans/CodeUI.Orleans.csproj
```

**Create test projects:**
```bash
dotnet new xunit -n CodeUI.Tests
dotnet sln add CodeUI.Tests/CodeUI.Tests.csproj
dotnet add CodeUI.Tests/CodeUI.Tests.csproj reference CodeUI.Core/CodeUI.Core.csproj
```

### Building and Testing
- `dotnet restore` - Restore NuGet packages (included in build/test commands)
- `dotnet build` - Build the solution. TIMING: Takes 5-10 seconds for typical projects. NEVER CANCEL - Set timeout to 180+ seconds.
- `dotnet test` - Run all tests. TIMING: Takes 1-5 seconds for basic test suites. NEVER CANCEL - Set timeout to 300+ seconds for comprehensive test suites.
- `dotnet run --project CodeUI.Web` - Run the Blazor Server application
- `dotnet build --configuration Release` - Build release version
- `dotnet test --configuration Release` - Run tests against release build

### Code Quality and Formatting
- `dotnet format` - Format code according to .editorconfig settings
- `dotnet format --verify-no-changes` - Verify code is properly formatted (for CI)
- `dotnet format style` - Apply code style fixes
- `dotnet format analyzers` - Apply analyzer-based fixes

### Package Management
- `dotnet add package <PackageName>` - Add NuGet package reference
- `dotnet remove package <PackageName>` - Remove package reference
- `dotnet list package` - List installed packages
- `dotnet restore` - Restore packages after manual .csproj changes

## Validation Requirements

### Manual Testing Scenarios
Since this is a web application for AI CLI tools management, after making any significant changes:

1. **Build Validation**: Always ensure the solution builds without errors
2. **Test Execution**: Run all tests to verify no regressions
3. **Application Startup**: Verify Blazor Server app starts and serves pages correctly
4. **CLI Integration Testing**: Test that CLI tools can be executed and output is captured
5. **Terminal Functionality**: Verify XTerm.js integration works for interactive sessions
6. **File System Access**: Test file navigation and git operations work correctly

### Pre-commit Validation
Always run these commands before considering work complete:
- `dotnet format --verify-no-changes` - Ensure code formatting is correct
- `dotnet build` - Verify builds successfully  
- `dotnet test` - Verify all tests pass

## Development Workflow

### Adding New Features
1. Create feature branch from main
2. Add/modify code using standard .NET patterns
3. Write corresponding unit tests
4. Build and test locally
5. Format code using `dotnet format`
6. Commit and push changes

### Project Structure Recommendations
When adding code, follow this structure based on the project issues and requirements:
```
CodeUI/
├── CodeUI.Web/                  # Blazor Server application
│   ├── Components/              # Blazor components (Terminal, FileExplorer, etc.)
│   ├── Pages/                   # Blazor pages
│   ├── wwwroot/                 # Static files, XTerm.js assets
│   └── Services/                # Application services
├── CodeUI.Core/                 # Core business logic
│   ├── Services/                # CLI execution, git operations
│   ├── Models/                  # Domain models
│   └── Interfaces/              # Service contracts
├── CodeUI.Orleans/              # Orleans grains for state management
│   ├── Grains/                  # Session and state management grains
│   └── Interfaces/              # Grain interfaces
├── tests/
│   ├── CodeUI.Tests/            # Unit tests
│   └── CodeUI.E2E.Tests/        # Playwright E2E tests
└── docs/                        # Documentation
```

### Key Technologies and Packages
Based on the project issues, these packages will be needed:
- **CliWrap** - CLI process management
- **Microsoft.Orleans.Server** - Distributed session management
- **LibGit2Sharp** - Git operations
- **Pty.Net** - Pseudo-terminal support for interactive CLI
- **Microsoft.SemanticKernel** - AI orchestration
- **Serilog.AspNetCore** - Structured logging
- **XTerm.js** - Terminal emulation in browser (via JavaScript interop)

## Timing Expectations and Timeouts

**CRITICAL - NEVER CANCEL these operations:**

- **Solution Creation**: 1-2 seconds
- **Project Creation**: 2-5 seconds (includes restore)
- **Clean Build**: 2-5 seconds for small projects, 5-15 seconds for medium projects. Set timeout to 180+ seconds.
- **Incremental Build**: 1-3 seconds. Set timeout to 60+ seconds.
- **Test Execution**: 3-5 seconds for basic unit tests. Set timeout to 300+ seconds.
- **Package Installation**: 5-15 seconds for common packages. Set timeout to 180+ seconds.
- **Package Restore**: 5-30 seconds depending on package count. Set timeout to 180+ seconds.
- **Code Formatting**: 1-3 seconds. Set timeout to 60+ seconds.
- **Package Creation**: 2-5 seconds for basic libraries. Set timeout to 120+ seconds.

For larger solutions with multiple projects:
- **Full Build**: 30-60 seconds. Set timeout to 300+ seconds.
- **Full Test Suite**: 30-120 seconds. Set timeout to 600+ seconds.

## Troubleshooting

### Common Issues and Solutions

**Build Failures:**
- Run `dotnet clean` then `dotnet build`
- Check for missing package references
- Verify target framework compatibility

**Test Failures:**
- Run `dotnet test --logger console --verbosity normal` for detailed output
- Check for missing test dependencies
- Verify test project references

**Package Issues:**
- Run `dotnet restore --force` to force package re-download
- Check NuGet.config if using private feeds
- Clear package cache: `dotnet nuget locals all --clear`

**Formatting Issues:**
- Create .editorconfig file for consistent formatting rules
- Run `dotnet format` to fix formatting issues
- Use `dotnet format --verbosity diagnostic` for detailed formatting output

## Common Commands Reference

### Quick validation sequence:
```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

### Package operations:
```bash
dotnet add package CliWrap
dotnet add package Microsoft.Orleans.Server
dotnet add package LibGit2Sharp
dotnet add package Pty.Net
dotnet add package Microsoft.SemanticKernel
dotnet add package Serilog.AspNetCore
dotnet list package
dotnet list package --outdated
```

### Solution management:
```bash
dotnet sln list
dotnet sln add src/NewProject/NewProject.csproj
dotnet sln remove src/OldProject/OldProject.csproj
```

## Repository Information

### Current State
- Repository initialized with basic files only
- .NET 8.0 environment ready
- No actual implementation exists yet
- Ready for project bootstrapping

## Repository Information

### Current State
- Repository initialized with basic files only
- .NET 8.0 environment ready with C# 12 support
- No actual implementation exists yet
- Ready for Blazor Server project bootstrapping

### Expected Development Pattern
Based on the repository issues and project description, this application will:
- Provide a web UI for managing multiple AI CLI tools (Claude Code, Gemini, Codex)
- Include terminal emulation using XTerm.js for interactive CLI sessions
- Integrate file system navigation with security boundaries
- Provide git operations and diff viewing capabilities
- Support distributed session management using Orleans
- Include AI orchestration with Semantic Kernel
- Deploy as self-contained single-file application for multiple platforms
- Support mobile-responsive design for tablet/phone access

Always start with the solution creation and Blazor Server setup when beginning actual development work.