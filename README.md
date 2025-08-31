# CodeUI

A modern .NET 8 application with Aspire orchestration and Blazor Server-side rendering.

## 🏗️ Project Structure

```
CodeUI/
├── CodeUI.sln                    # Main solution file
├── CodeUI.AppHost/               # Aspire orchestration project
├── CodeUI.Web/                   # Blazor Server application
├── CodeUI.Core/                  # Core business logic and data models
├── CodeUI.Orleans/               # Orleans grain definitions
└── README.md                     # This file
```

## 🚀 Technology Stack

- **.NET 8.0** - Latest LTS version of .NET
- **C# 12** - Latest C# language features
- **Aspire** - .NET Aspire for cloud-native orchestration
- **Blazor Server** - Server-side rendering with real-time updates
- **ASP.NET Core Identity** - Built-in authentication and authorization
- **Entity Framework Core** - Data access with In-Memory database for development
- **Orleans** - Virtual Actor Model for distributed applications

## 📋 Prerequisites

Before running this application, ensure you have:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Docker](https://docs.docker.com/get-docker/) (for Aspire orchestration)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/) (recommended)

## 🛠️ Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/managedcode/CodeUI.git
cd CodeUI
```

### 2. Install .NET Aspire Workload

```bash
dotnet workload update
dotnet workload install aspire
```

### 3. Restore Dependencies

```bash
dotnet restore
```

### 4. Build the Solution

```bash
dotnet build
```

## 🏃‍♂️ Running the Application

### Option 1: Run with Aspire Orchestration (Recommended)

```bash
dotnet run --project CodeUI.AppHost
```

This will:
- Start the Aspire dashboard at `http://localhost:18888`
- Launch the Blazor Server app
- Provide monitoring and observability features

### Option 2: Run Blazor Server App Directly

```bash
dotnet run --project CodeUI.Web
```

The application will be available at `http://localhost:5225` (or the port shown in the console).

## 🔐 Authentication

The application includes basic ASP.NET Core Identity authentication with:

- **Registration** - Create new user accounts
- **Login/Logout** - User authentication
- **In-Memory Database** - For development (switch to SQL Server/PostgreSQL for production)
- **Relaxed Password Policy** - For development convenience

### Default Settings:
- Minimum password length: 6 characters
- No special character requirements
- No email confirmation required

## 🏗️ Project Details

### CodeUI.AppHost
- **Purpose**: Aspire orchestration host
- **Features**: Service discovery, monitoring, distributed tracing
- **Dependencies**: Aspire.Hosting

### CodeUI.Web
- **Purpose**: Blazor Server application
- **Features**: Interactive server-side rendering, authentication, responsive UI
- **Dependencies**: ASP.NET Core Identity, Entity Framework Core

### CodeUI.Core
- **Purpose**: Shared business logic and data models
- **Features**: Entity Framework DbContext, Identity models
- **Dependencies**: Entity Framework Core, ASP.NET Core Identity

### CodeUI.Orleans
- **Purpose**: Orleans grain definitions for distributed computing
- **Features**: Virtual Actor Model interfaces
- **Dependencies**: Microsoft.Orleans.Abstractions

## 🧪 Development

### Building

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build CodeUI.Web
```

### Testing

```bash
# Run tests (when added)
dotnet test
```

### Adding Migrations (when using a real database)

```bash
# Add migration
dotnet ef migrations add InitialCreate --project CodeUI.Core --startup-project CodeUI.Web

# Update database
dotnet ef database update --project CodeUI.Core --startup-project CodeUI.Web
```

## 📁 Key Files

- `CodeUI.sln` - Solution file containing all projects
- `CodeUI.AppHost/Program.cs` - Aspire orchestration configuration
- `CodeUI.Web/Program.cs` - Web application startup and services configuration
- `CodeUI.Core/Data/ApplicationDbContext.cs` - Entity Framework database context
- `CodeUI.Core/Data/ApplicationUser.cs` - Identity user model
- `CodeUI.Orleans/Grains/IHelloGrain.cs` - Sample Orleans grain interface

## 🚀 Deployment

### Self-Contained Deployment (Recommended)

CodeUI supports self-contained deployment for production environments without requiring Docker or external dependencies.

#### Quick Start
```bash
# Build for all platforms
./deployment/scripts/build-all.sh

# Manual build commands
dotnet publish CodeUI.Web -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
dotnet publish CodeUI.Web -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
dotnet publish CodeUI.Web -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
```

#### Platform Installation
- **Windows**: Run `install-windows.ps1` as Administrator
- **Linux**: Run `sudo ./install-unix.sh`
- **macOS**: Run `sudo ./install-unix.sh`

#### Features
- ✅ Single executable (~150MB per platform)
- ✅ SQLite database for data persistence
- ✅ Windows Service / systemd / LaunchDaemon auto-start
- ✅ No external dependencies required
- ✅ Production-ready configuration

See [deployment/README.md](deployment/README.md) for detailed instructions.

### Traditional Deployment

For development or containerized environments:

1. **Update Database Provider**: Change from In-Memory to SQL Server/PostgreSQL in `Program.cs`
2. **Configure Connection Strings**: Update `appsettings.json` with production database
3. **Security Settings**: Review and harden authentication settings
4. **Environment Variables**: Configure for production environment

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Commit your changes: `git commit -am 'Add some feature'`
4. Push to the branch: `git push origin feature/your-feature-name`
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support

For questions and support:
- Create an issue in the GitHub repository
- Check existing documentation and README
- Review the [.NET Aspire documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)

---

**Happy Coding! 🎉**