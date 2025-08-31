# CodeUI Self-Contained Deployment

This directory contains scripts and configurations for deploying CodeUI as self-contained applications across Windows, Linux, and macOS platforms.

## Quick Start

### Build All Platforms
```bash
./deployment/scripts/build-all.sh
```

This will create self-contained executables for:
- Windows x64 (`./publish/win-x64/`)
- Linux x64 (`./publish/linux-x64/`)
- macOS x64 (`./publish/osx-x64/`)

### Manual Build Commands
```bash
# Windows
dotnet publish CodeUI.Web -c Release -r win-x64 --self-contained -p:PublishSingleFile=true --output ./publish/win-x64

# Linux
dotnet publish CodeUI.Web -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true --output ./publish/linux-x64

# macOS
dotnet publish CodeUI.Web -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true --output ./publish/osx-x64
```

## Installation

### Windows
1. Copy the contents of `./publish/win-x64/` to your target Windows machine
2. Run PowerShell as Administrator
3. Execute: `.\install-windows.ps1`

**Optional Parameters:**
```powershell
.\install-windows.ps1 -InstallPath "C:\MyApps\CodeUI" -Port 8080 -InstallService:$false
```

### Linux
1. Copy the contents of `./publish/linux-x64/` to your target Linux machine
2. Run: `sudo ./install-unix.sh`

### macOS
1. Copy the contents of `./publish/osx-x64/` to your target macOS machine
2. Run: `sudo ./install-unix.sh`

## Features

### ✅ Single Executable
- **Windows**: `CodeUI.Web.exe` (~151MB)
- **Linux**: `CodeUI.Web` (~147MB)
- **macOS**: `CodeUI.Web` (~146MB)

### ✅ Configuration via appsettings.json
- **Development**: Uses in-memory database
- **Production**: Uses SQLite database by default
- **Configurable**: Supports SQL Server via connection strings

### ✅ Auto-start Services

**Windows Service**
- Service name: `CodeUI`
- Automatic startup on boot
- Runs under LocalSystem account

**Linux systemd**
- Service file: `/etc/systemd/system/codeui.service`
- Runs under dedicated `codeui` user
- Automatic startup on boot

**macOS LaunchDaemon**
- Plist file: `/Library/LaunchDaemons/com.codeui.app.plist`
- Runs under dedicated `codeui` user
- Automatic startup on boot

### ✅ Installation Scripts
- **Windows**: PowerShell script with service registration
- **Unix**: Bash script for Linux and macOS
- Automatic user creation and permissions setup
- Service installation and startup

### ✅ No External Dependencies
- Completely self-contained
- All .NET runtime and libraries included
- SQLite database included for data persistence
- No Docker or additional runtimes required

## Service Management

### Windows
```powershell
# Service management
Start-Service -Name CodeUI
Stop-Service -Name CodeUI
Get-Service -Name CodeUI

# Uninstall
Stop-Service -Name CodeUI
sc.exe delete CodeUI
Remove-Item -Path "C:\Program Files\CodeUI" -Recurse -Force
```

### Linux
```bash
# Service management
sudo systemctl start codeui
sudo systemctl stop codeui
sudo systemctl status codeui
sudo journalctl -u codeui -f

# Uninstall
sudo systemctl stop codeui
sudo systemctl disable codeui
sudo rm /etc/systemd/system/codeui.service
sudo systemctl daemon-reload
sudo rm -rf /opt/codeui
sudo userdel codeui
```

### macOS
```bash
# Service management
sudo launchctl start com.codeui.app
sudo launchctl stop com.codeui.app
sudo launchctl list | grep codeui

# Uninstall
sudo launchctl stop com.codeui.app
sudo launchctl unload /Library/LaunchDaemons/com.codeui.app.plist
sudo rm /Library/LaunchDaemons/com.codeui.app.plist
sudo rm -rf /opt/codeui
sudo dscl . -delete /Users/codeui
```

## Configuration

### Database Providers
Edit `appsettings.json` to change database provider:

```json
{
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./Data/codeui.db",
    "SqlServerConnection": "Server=localhost;Database=CodeUI;Trusted_Connection=true;"
  }
}
```

Supported providers:
- `"Sqlite"` - Default, uses SQLite database
- `"SqlServer"` - Uses SQL Server
- `"InMemory"` - In-memory database (development only)

### Network Configuration
```json
{
  "Urls": "http://localhost:5000;https://localhost:5001"
}
```

### Authentication Settings
```json
{
  "Authentication": {
    "RequireConfirmedAccount": false,
    "Password": {
      "RequireDigit": true,
      "RequiredLength": 8,
      "RequireNonAlphanumeric": true,
      "RequireUppercase": true,
      "RequireLowercase": true
    }
  }
}
```

## Troubleshooting

### Check Application Status
- **URL**: http://localhost:5000 (default)
- **Logs**: 
  - Windows: `C:\Program Files\CodeUI\Logs\`
  - Unix: `/opt/codeui/Logs/`

### Common Issues

**Port conflicts**: Change port in appsettings.json or during installation
**Database issues**: Check SQLite file permissions in Data directory
**Service won't start**: Check logs and ensure proper permissions

### Performance
- Startup time: 3-5 seconds
- Memory usage: ~50-100MB
- Disk space: ~150MB per platform

## Security Notes

- SQLite database file should be secured with appropriate file permissions
- Change default authentication settings for production use
- Consider using HTTPS in production environments
- Service runs with minimal privileges on Unix systems

## Support

For issues and questions:
- Check application logs first
- Review service status
- Verify configuration files
- Create issues on the GitHub repository