# CodeUI Windows Installation Script
# Run as Administrator

param(
    [string]$InstallPath = "C:\Program Files\CodeUI",
    [string]$ServiceName = "CodeUI",
    [int]$Port = 5000,
    [switch]$InstallService = $true
)

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator. Exiting..."
    exit 1
}

Write-Host "CodeUI Windows Installation" -ForegroundColor Green
Write-Host "===========================" -ForegroundColor Green

# Create installation directory
Write-Host "Creating installation directory: $InstallPath"
if (!(Test-Path $InstallPath)) {
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
}

# Copy files
Write-Host "Copying application files..."
Copy-Item -Path ".\*" -Destination $InstallPath -Recurse -Force -Exclude "install-windows.ps1"

# Update appsettings for production
Write-Host "Configuring application settings..."
$appsettingsPath = Join-Path $InstallPath "appsettings.json"
$appsettings = Get-Content $appsettingsPath | ConvertFrom-Json

# Update URLs for the specified port
$appsettings.Urls = "http://localhost:$Port;https://localhost:$($Port + 1)"

# Ensure data directory exists
$dataDir = Join-Path $InstallPath "Data"
if (!(Test-Path $dataDir)) {
    New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
}

# Update database path to use absolute path
$appsettings.ConnectionStrings.DefaultConnection = "Data Source=$dataDir\codeui.db"

# Save updated settings
$appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath

if ($InstallService) {
    # Install as Windows Service using sc.exe
    Write-Host "Installing Windows Service..."
    
    $servicePath = Join-Path $InstallPath "CodeUI.Web.exe"
    $serviceDescription = "CodeUI - AI CLI Tools Management Application"
    
    # Stop and remove existing service if it exists
    $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Host "Stopping existing service..."
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
        sc.exe delete $ServiceName
        Start-Sleep -Seconds 2
    }
    
    # Create the service
    sc.exe create $ServiceName binPath= "`"$servicePath`" --urls `"http://localhost:$Port`"" start= auto DisplayName= "CodeUI Application"
    sc.exe description $ServiceName $serviceDescription
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Service created successfully!" -ForegroundColor Green
        
        # Start the service
        Write-Host "Starting service..."
        Start-Service -Name $ServiceName
        
        if ((Get-Service -Name $ServiceName).Status -eq 'Running') {
            Write-Host "Service started successfully!" -ForegroundColor Green
        } else {
            Write-Warning "Service was created but failed to start. Check Windows Event Log for details."
        }
    } else {
        Write-Error "Failed to create service."
    }
}

# Create desktop shortcut
Write-Host "Creating desktop shortcut..."
$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\CodeUI.lnk")
$Shortcut.TargetPath = Join-Path $InstallPath "CodeUI.Web.exe"
$Shortcut.WorkingDirectory = $InstallPath
$Shortcut.Description = "CodeUI - AI CLI Tools Management"
$Shortcut.Save()

Write-Host ""
Write-Host "Installation completed successfully!" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Green
Write-Host "Installation Path: $InstallPath"
Write-Host "Application URL: http://localhost:$Port"
if ($InstallService) {
    Write-Host "Service Name: $ServiceName"
    Write-Host ""
    Write-Host "Service Management Commands:"
    Write-Host "  Start:   Start-Service -Name $ServiceName"
    Write-Host "  Stop:    Stop-Service -Name $ServiceName"
    Write-Host "  Status:  Get-Service -Name $ServiceName"
}
Write-Host ""
Write-Host "To uninstall:"
if ($InstallService) {
    Write-Host "  1. Stop-Service -Name $ServiceName"
    Write-Host "  2. sc.exe delete $ServiceName"
}
Write-Host "  3. Remove-Item -Path '$InstallPath' -Recurse -Force"