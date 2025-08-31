#!/bin/bash

# CodeUI Self-Contained Deployment Script
# This script builds self-contained deployments for all supported platforms

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
PROJECT_NAME="CodeUI.Web"
OUTPUT_DIR="./publish"
CONFIGURATION="Release"

# Supported platforms
PLATFORMS=(
    "win-x64"
    "linux-x64"
    "osx-x64"
)

echo -e "${GREEN}CodeUI Self-Contained Deployment Builder${NC}"
echo "=========================================="

# Clean previous builds
if [ -d "$OUTPUT_DIR" ]; then
    echo -e "${YELLOW}Cleaning previous builds...${NC}"
    rm -rf "$OUTPUT_DIR"
fi

# Build for each platform
for platform in "${PLATFORMS[@]}"; do
    echo -e "${GREEN}Building for platform: $platform${NC}"
    
    # Determine executable name
    if [[ "$platform" == win-* ]]; then
        executable="CodeUI.Web.exe"
    else
        executable="CodeUI.Web"
    fi
    
    # Create platform directory
    platform_dir="$OUTPUT_DIR/$platform"
    mkdir -p "$platform_dir"
    
    # Build
    echo "Publishing $PROJECT_NAME for $platform..."
    dotnet publish "$PROJECT_NAME" \
        -c "$CONFIGURATION" \
        -r "$platform" \
        --self-contained \
        -p:PublishSingleFile=true \
        --output "$platform_dir"
    
    if [ $? -eq 0 ]; then
        # Get executable size
        if [ -f "$platform_dir/$executable" ]; then
            size=$(ls -lh "$platform_dir/$executable" | awk '{print $5}')
            echo -e "${GREEN}✓ Build successful for $platform (Size: $size)${NC}"
        else
            echo -e "${RED}✗ Executable not found for $platform${NC}"
        fi
    else
        echo -e "${RED}✗ Build failed for $platform${NC}"
        exit 1
    fi
    
    echo ""
done

# Create installation packages
echo -e "${GREEN}Creating installation packages...${NC}"

# Copy deployment scripts to each platform
for platform in "${PLATFORMS[@]}"; do
    platform_dir="$OUTPUT_DIR/$platform"
    
    if [[ "$platform" == win-* ]]; then
        # Copy Windows installation script
        cp deployment/scripts/install-windows.ps1 "$platform_dir/"
        cp deployment/services/codeui.service.xml "$platform_dir/"
    else
        # Copy Unix installation script
        cp deployment/scripts/install-unix.sh "$platform_dir/"
        chmod +x "$platform_dir/install-unix.sh"
        
        if [[ "$platform" == osx-* ]]; then
            cp deployment/services/com.codeui.app.plist "$platform_dir/"
        else
            cp deployment/services/codeui.service "$platform_dir/"
        fi
    fi
done

echo -e "${GREEN}✓ All builds completed successfully!${NC}"
echo ""
echo "Build outputs:"
echo "=============="
for platform in "${PLATFORMS[@]}"; do
    echo "  $platform: $OUTPUT_DIR/$platform/"
done

echo ""
echo "To install:"
echo "==========="
echo "Windows: Run PowerShell as Administrator and execute install-windows.ps1"
echo "Linux:   Run sudo ./install-unix.sh"
echo "macOS:   Run sudo ./install-unix.sh"