#!/bin/bash

# CodeUI Unix Installation Script (Linux/macOS)
# Run with sudo

set -e

# Configuration
INSTALL_PATH="/opt/codeui"
SERVICE_NAME="codeui"
PORT=5000
USER="codeui"
GROUP="codeui"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED}Please run this script as root (use sudo)${NC}"
    exit 1
fi

# Detect OS
OS=""
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    OS="linux"
elif [[ "$OSTYPE" == "darwin"* ]]; then
    OS="macos"
else
    echo -e "${RED}Unsupported operating system: $OSTYPE${NC}"
    exit 1
fi

echo -e "${GREEN}CodeUI Unix Installation ($OS)${NC}"
echo -e "${GREEN}===============================${NC}"

# Create user and group
echo "Creating user and group..."
if ! id "$USER" &>/dev/null; then
    if [[ "$OS" == "linux" ]]; then
        useradd -r -s /bin/false -d "$INSTALL_PATH" "$USER"
    else
        # macOS
        # Find the next available UID
        MAX_UID=$(dscl . -list /Users uid | awk '{print $2}' | sort -n | tail -1)
        NEW_UID=$((MAX_UID + 1))
        
        dscl . -create /Users/$USER
        dscl . -create /Users/$USER UserShell /bin/false
        dscl . -create /Users/$USER RealName "CodeUI Service User"
        dscl . -create /Users/$USER UniqueID $NEW_UID
        dscl . -create /Users/$USER PrimaryGroupID 20
        dscl . -create /Users/$USER NFSHomeDirectory $INSTALL_PATH
    fi
fi

# Create installation directory
echo "Creating installation directory: $INSTALL_PATH"
mkdir -p "$INSTALL_PATH"
mkdir -p "$INSTALL_PATH/Data"
mkdir -p "$INSTALL_PATH/Logs"

# Copy files
echo "Copying application files..."
cp -r ./* "$INSTALL_PATH/" 2>/dev/null || :
rm -f "$INSTALL_PATH/install-unix.sh" 2>/dev/null || :

# Make executable
chmod +x "$INSTALL_PATH/CodeUI.Web"

# Update appsettings for production
echo "Configuring application settings..."
APPSETTINGS="$INSTALL_PATH/appsettings.json"

# Use jq if available, otherwise use sed
if command -v jq >/dev/null 2>&1; then
    # Update with jq
    tmp=$(mktemp)
    jq ".Urls = \"http://localhost:$PORT\" | 
        .ConnectionStrings.DefaultConnection = \"Data Source=$INSTALL_PATH/Data/codeui.db\" |
        .Logging.File.Path = \"$INSTALL_PATH/Logs/codeui-{Date}.log\"" "$APPSETTINGS" > "$tmp"
    mv "$tmp" "$APPSETTINGS"
else
    # Fallback: create a new appsettings.json
    cat > "$APPSETTINGS" << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    },
    "File": {
      "Path": "$INSTALL_PATH/Logs/codeui-{Date}.log",
      "MinLevel": "Information"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=$INSTALL_PATH/Data/codeui.db"
  },
  "DatabaseProvider": "Sqlite",
  "Urls": "http://localhost:$PORT",
  "Authentication": {
    "RequireConfirmedAccount": false,
    "Password": {
      "RequireDigit": true,
      "RequiredLength": 8,
      "RequireNonAlphanumeric": true,
      "RequireUppercase": true,
      "RequireLowercase": true
    }
  },
  "Application": {
    "Name": "CodeUI",
    "Environment": "Production"
  }
}
EOF
fi

# Set ownership
chown -R "$USER:$GROUP" "$INSTALL_PATH"

# Install and start service
if [[ "$OS" == "linux" ]]; then
    echo "Installing systemd service..."
    
    # Create systemd service file
    cat > /etc/systemd/system/$SERVICE_NAME.service << EOF
[Unit]
Description=CodeUI - AI CLI Tools Management Application
After=network.target

[Service]
Type=notify
ExecStart=$INSTALL_PATH/CodeUI.Web --urls http://localhost:$PORT
User=$USER
Group=$GROUP
WorkingDirectory=$INSTALL_PATH
Restart=always
RestartSec=5
SyslogIdentifier=$SERVICE_NAME
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
EOF

    # Enable and start service
    systemctl daemon-reload
    systemctl enable $SERVICE_NAME
    systemctl start $SERVICE_NAME
    
    # Check service status
    if systemctl is-active --quiet $SERVICE_NAME; then
        echo -e "${GREEN}Service started successfully!${NC}"
    else
        echo -e "${RED}Service failed to start. Check: sudo journalctl -u $SERVICE_NAME${NC}"
    fi
    
else
    # macOS - Create LaunchDaemon
    echo "Installing LaunchDaemon..."
    
    PLIST_PATH="/Library/LaunchDaemons/com.$SERVICE_NAME.app.plist"
    
    cat > "$PLIST_PATH" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.$SERVICE_NAME.app</string>
    <key>ProgramArguments</key>
    <array>
        <string>$INSTALL_PATH/CodeUI.Web</string>
        <string>--urls</string>
        <string>http://localhost:$PORT</string>
    </array>
    <key>UserName</key>
    <string>$USER</string>
    <key>WorkingDirectory</key>
    <string>$INSTALL_PATH</string>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>EnvironmentVariables</key>
    <dict>
        <key>ASPNETCORE_ENVIRONMENT</key>
        <string>Production</string>
    </dict>
</dict>
</plist>
EOF

    # Load and start
    launchctl load "$PLIST_PATH"
    launchctl start "com.$SERVICE_NAME.app"
    
    sleep 2
    if launchctl list | grep -q "com.$SERVICE_NAME.app"; then
        echo -e "${GREEN}Service started successfully!${NC}"
    else
        echo -e "${RED}Service failed to start. Check system logs.${NC}"
    fi
fi

echo ""
echo -e "${GREEN}Installation completed successfully!${NC}"
echo -e "${GREEN}===================================${NC}"
echo "Installation Path: $INSTALL_PATH"
echo "Application URL: http://localhost:$PORT"
echo "Service Name: $SERVICE_NAME"
echo ""
echo "Service Management Commands:"
if [[ "$OS" == "linux" ]]; then
    echo "  Start:   sudo systemctl start $SERVICE_NAME"
    echo "  Stop:    sudo systemctl stop $SERVICE_NAME"
    echo "  Status:  sudo systemctl status $SERVICE_NAME"
    echo "  Logs:    sudo journalctl -u $SERVICE_NAME -f"
else
    echo "  Start:   sudo launchctl start com.$SERVICE_NAME.app"
    echo "  Stop:    sudo launchctl stop com.$SERVICE_NAME.app"
    echo "  Status:  sudo launchctl list | grep $SERVICE_NAME"
    echo "  Logs:    tail -f /var/log/system.log | grep $SERVICE_NAME"
fi
echo ""
echo "To uninstall:"
if [[ "$OS" == "linux" ]]; then
    echo "  1. sudo systemctl stop $SERVICE_NAME"
    echo "  2. sudo systemctl disable $SERVICE_NAME"
    echo "  3. sudo rm /etc/systemd/system/$SERVICE_NAME.service"
    echo "  4. sudo systemctl daemon-reload"
else
    echo "  1. sudo launchctl stop com.$SERVICE_NAME.app"
    echo "  2. sudo launchctl unload /Library/LaunchDaemons/com.$SERVICE_NAME.app.plist"
    echo "  3. sudo rm /Library/LaunchDaemons/com.$SERVICE_NAME.app.plist"
fi
echo "  5. sudo rm -rf $INSTALL_PATH"
echo "  6. sudo userdel $USER" # Linux only, but harmless on macOS