# CodeUI
CodeUI is a .NET UI library project currently in its initial development phase. The repository contains basic infrastructure files but no implementation yet.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Repository Status
This repository is currently in its bootstrap phase with only:
- README.md (basic project name)
- LICENSE (MIT License)  
- .gitignore (Visual Studio/C# template)

The project is intended to become a .NET UI library based on the naming and .gitignore configuration.

## Prerequisites and Environment Setup
- .NET 8.0 SDK is available and validated to work
- No additional SDK downloads required - environment is pre-configured
- All standard .NET development tools are available

## Working Effectively

### Bootstrapping the Project
When the repository needs actual code, use these validated commands:

**Create a new solution:**
```bash
dotnet new sln -n CodeUI
```

**Create core library projects:**
```bash
dotnet new classlib -n CodeUI.Core
dotnet sln add CodeUI.Core/CodeUI.Core.csproj
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
Since this will be a UI library, after making any significant changes:

1. **Build Validation**: Always ensure the solution builds without errors
2. **Test Execution**: Run all tests to verify no regressions
3. **Package Creation**: For UI libraries, test that packages can be created with `dotnet pack`
4. **Consumer Testing**: When UI components exist, create a simple test application that references and uses the library

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
When adding code, follow these .NET conventions:
```
CodeUI/
├── src/
│   ├── CodeUI.Core/           # Core library functionality
│   ├── CodeUI.Controls/       # UI controls if applicable
│   └── CodeUI.Theming/        # Theming/styling if applicable
├── tests/
│   ├── CodeUI.Core.Tests/     # Unit tests for core
│   └── CodeUI.Integration.Tests/  # Integration tests
├── samples/
│   └── CodeUI.Sample.App/     # Sample application demonstrating usage
└── docs/                      # Documentation
```

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
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Logging
dotnet list package
dotnet list package --outdated
dotnet pack --configuration Release
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

### Expected Development Pattern
Based on the repository name "CodeUI" and .gitignore configuration, this project will likely:
- Provide .NET UI components or controls
- Target multiple .NET frameworks
- Include theming/styling capabilities
- Provide sample applications demonstrating usage
- Follow standard .NET library patterns and conventions

Always start with solution creation and core library setup when beginning actual development work.