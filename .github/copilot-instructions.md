# CodeUI
CodeUI is a comprehensive web-based AI CLI tools management application built with .NET 9.0 and C# 13. It provides a unified interface for managing multiple AI coding CLI tools (Claude Code, Gemini, Codex) with integrated file system navigation, git operations, and terminal access through Blazor Server-side application.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Repository Status
This repository is currently upgraded to .NET 9.0 with proper Aspire orchestration and comprehensive testing infrastructure:
- .NET 9.0 with C# 13 language support (latest features)
- Aspire Hosting 9.4.1 for distributed application orchestration  
- Blazor Server-side web application
- Central Package Management for unified dependency management
- Modern .slnx solution format (XML-based)
- Comprehensive test coverage with unit and integration tests
- Automated CI/CD pipeline with GitHub Actions

The project provides a web application for managing AI CLI tools with features including terminal emulation, file management, git integration, and session management.

## Prerequisites and Environment Setup
**⚠️ Important: You must install .NET 9 before you start!**

- .NET 9.0 SDK is required and validated to work
- C# 13 language features supported (latest with .NET 9)
- Aspire workload for distributed application development
- All standard .NET development tools are available

## Working Effectively

### Project Structure
The solution follows a clean architecture pattern with modern .NET 9 features:
```
CodeUI/
├── CodeUI.slnx                   # Modern XML-based solution file (.NET 9+)
├── Directory.Packages.props      # Central Package Management configuration
├── global.json                   # .NET 9 SDK enforcement
├── CodeUI.AppHost/              # Aspire application host for orchestration
├── CodeUI.Web/                  # Blazor Server application
├── CodeUI.Core/                 # Core business logic and data models
├── CodeUI.Orleans/              # Orleans grains for state management
├── CodeUI.Tests/                # Unit tests using xUnit
├── CodeUI.AspireTests/          # Integration tests using Aspire testing framework
└── .github/workflows/           # CI/CD pipelines
```

### Building and Testing
- `dotnet restore CodeUI.slnx` - Restore NuGet packages using .slnx solution
- `dotnet build CodeUI.slnx` - Build the solution. TIMING: Takes 5-10 seconds. Set timeout to 180+ seconds.
- `dotnet test CodeUI.slnx` - Run all tests. TIMING: Takes 1-5 seconds for unit tests. Set timeout to 300+ seconds for integration tests.
- `dotnet run --project CodeUI.AppHost` - Run the Aspire orchestrated application
- `dotnet build CodeUI.slnx --configuration Release` - Build release version
- `dotnet test CodeUI.slnx --configuration Release --collect:"XPlat Code Coverage"` - Run tests with coverage

### Central Package Management
The solution uses .NET's Central Package Management feature:
- **Directory.Packages.props**: Centrally manages all NuGet package versions
- Individual project files reference packages without version attributes
- Ensures consistent package versions across all projects
- Simplifies dependency management and security updates

### Solution Format (.slnx)
- Uses modern .slnx XML-based solution format supported by .NET 9
- Simpler and more maintainable than legacy .sln format
- Better tooling support and version control friendly

### Aspire Application Development
The application uses .NET Aspire for orchestration:
- **CodeUI.AppHost**: Configures and orchestrates the distributed application
- **CodeUI.Web**: The main Blazor Server application
- Integration testing uses `DistributedApplicationTestingBuilder` for proper Aspire testing

Example Aspire host configuration:
```csharp
var builder = DistributedApplication.CreateBuilder(args);
var web = builder.AddProject("codeui-web", "../CodeUI.Web/CodeUI.Web.csproj");
builder.Build().Run();
```

### Testing Strategy
**Unit Tests (CodeUI.Tests)**:
- Uses WebApplicationFactory for testing web endpoints
- Tests individual components and services
- Fast execution, isolated from external dependencies

**Integration Tests (CodeUI.AspireTests)**:
- Uses Aspire testing framework with DistributedApplicationTestingBuilder
- Tests the full application stack including orchestration
- Validates end-to-end functionality

Example Aspire test:
```csharp
var appHost = await DistributedApplicationTestingBuilder.CreateAsync<CodeUI.AppHost.Program>();
await using var app = await appHost.BuildAsync();
await app.StartAsync();
var httpClient = app.CreateHttpClient("codeui-web");
var response = await httpClient.GetAsync("/");
```

### Code Quality and Formatting
- `dotnet format` - Format code according to .editorconfig settings
- `dotnet format --verify-no-changes` - Verify code is properly formatted (for CI)
- Test coverage is collected automatically and reported via Codecov

### Package Management
All packages are upgraded to .NET 9.0 compatible versions with Central Package Management:
- `Aspire.Hosting` 9.4.1 - Latest Aspire for application orchestration
- `Aspire.Hosting.Testing` 9.4.1 - For Aspire integration testing
- `Microsoft.AspNetCore.*` 9.0.8 - Latest web application framework
- `Microsoft.EntityFrameworkCore.*` 9.0.8 - Latest data access framework
- `Microsoft.Orleans.*` 9.2.1 - Latest distributed state management
- Package versions centrally managed in `Directory.Packages.props`
- `Microsoft.Orleans.*` 9.2.0 - For distributed state management

## CI/CD Pipeline

### Build and Test Workflow
- Builds solution with .NET 9.0 using CodeUI.slnx
- Runs unit and integration tests
- Collects test coverage with coverlet
- Creates deployable artifacts for Windows, Linux, and macOS
- Uses modern .slnx solution format throughout

### Aspire Integration Tests
- Uses proper Aspire testing framework (not dashboard)
- Tests distributed application functionality with Aspire 9.4.1
- Validates HTTP endpoints and application behavior

## Development Workflow

### Adding New Features
1. Create feature branch from main
2. Add/modify code using .NET 9.0 and C# 13 patterns
3. Write corresponding unit tests in CodeUI.Tests
4. Add integration tests in CodeUI.AspireTests if needed
5. Build and test locally with full coverage
6. Format code using `dotnet format`
7. Commit and push changes

### Testing Approach
- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test full application stack with Aspire orchestration
- **Coverage**: Aim for comprehensive coverage with both unit and integration tests

### Key Technologies and Packages
- **.NET 9.0 with C# 13** - Latest framework with newest language features
- **Aspire Hosting 9.4.1** - Latest distributed application orchestration
- **Blazor Server** - Interactive web UI with server-side rendering
- **Orleans** - Distributed session and state management
- **Entity Framework Core** - Data access and identity management
- **xUnit** - Unit and integration testing framework
- **Central Package Management** - Unified dependency management

## Timing Expectations and Timeouts

**Build Operations**:
- Solution Build: 2-5 seconds for incremental, 5-15 seconds for clean build
- Set timeout to 180+ seconds for build operations
- Test Execution: 1-3 seconds for unit tests, 5-15 seconds for integration tests
- Set timeout to 300+ seconds for comprehensive test suites

**Aspire Operations**:
- Application Startup: 5-10 seconds for full orchestration
- Integration Test Setup: 2-5 seconds per test
- HTTP Client Operations: 1-2 seconds for endpoint responses

## Troubleshooting

### Common Issues and Solutions

**Build Failures**:
- Ensure .NET 9.0 SDK is installed
- Run `dotnet restore CodeUI.slnx --force` to refresh packages
- Check for package version conflicts in Directory.Packages.props

**Test Failures**:
- Unit tests failing: Check WebApplicationFactory configuration
- Integration tests failing: Verify Aspire host configuration and project paths
- Coverage issues: Ensure coverlet packages are properly referenced

**Aspire Issues**:
- Project path errors: Verify relative paths in AppHost Program.cs
- Orchestration failures: Check service dependencies and configurations
- Testing framework issues: Ensure proper DistributedApplicationTestingBuilder usage

### Validation Commands
```bash
# Quick validation sequence
dotnet restore CodeUI.slnx
dotnet build CodeUI.slnx --configuration Release
dotnet test CodeUI.slnx --configuration Release --collect:"XPlat Code Coverage"
dotnet format --verify-no-changes

# Run specific test projects
dotnet test CodeUI.Tests --configuration Release
dotnet test CodeUI.AspireTests --configuration Release
```

## Best Practices

### Code Organization
- Follow clean architecture principles
- Separate concerns between Web, Core, and Orleans projects
- Use proper dependency injection and service registration
- Leverage Central Package Management for dependency consistency

### Testing Strategy
- Write unit tests for business logic in Core project
- Use integration tests for end-to-end scenarios
- Test both happy path and error conditions
- Maintain high test coverage with meaningful assertions

### Aspire Development
- Keep orchestration configuration simple and focused
- Use proper resource naming conventions
- Leverage Aspire testing framework for integration tests
- Monitor application health and performance through Aspire dashboard (development only)

### Modern .NET 9 Features
- Use C# 13 language features for cleaner code
- Leverage Central Package Management for consistency
- Use .slnx solution format for better tooling support
- Take advantage of .NET 9 performance improvements

This repository is production-ready with comprehensive testing, modern .NET 9.0 features, Central Package Management, and proper distributed application orchestration using latest Aspire.
