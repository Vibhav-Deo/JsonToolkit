#!/bin/bash

# NuGet OIDC Trusted Publishing Validation Script
# This script helps validate the OIDC setup for NuGet trusted publishing

set -e

echo "🔍 NuGet OIDC Trusted Publishing Validation"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    local status=$1
    local message=$2
    case $status in
        "success")
            echo -e "${GREEN}✅ $message${NC}"
            ;;
        "warning")
            echo -e "${YELLOW}⚠️  $message${NC}"
            ;;
        "error")
            echo -e "${RED}❌ $message${NC}"
            ;;
        "info")
            echo -e "${BLUE}ℹ️  $message${NC}"
            ;;
    esac
}

# Check if we're running in GitHub Actions
if [ -n "$GITHUB_ACTIONS" ]; then
    print_status "info" "Running in GitHub Actions environment"
    
    # Check OIDC environment variables
    if [ -n "$ACTIONS_ID_TOKEN_REQUEST_TOKEN" ]; then
        print_status "success" "OIDC token request token is available"
    else
        print_status "error" "OIDC token request token is not available"
        print_status "info" "Ensure 'id-token: write' permission is set in workflow"
    fi
    
    if [ -n "$ACTIONS_ID_TOKEN_REQUEST_URL" ]; then
        print_status "success" "OIDC token request URL is available"
    else
        print_status "error" "OIDC token request URL is not available"
    fi
    
    # Display repository information
    echo ""
    print_status "info" "Repository Information:"
    echo "  Repository: ${GITHUB_REPOSITORY:-'Not set'}"
    echo "  Ref: ${GITHUB_REF:-'Not set'}"
    echo "  SHA: ${GITHUB_SHA:-'Not set'}"
    echo "  Workflow: ${GITHUB_WORKFLOW:-'Not set'}"
    echo "  Run ID: ${GITHUB_RUN_ID:-'Not set'}"
    
else
    print_status "warning" "Not running in GitHub Actions environment"
    print_status "info" "This script is designed to run within GitHub Actions workflows"
fi

echo ""

# Check for .NET CLI
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    print_status "success" ".NET CLI is available (version: $DOTNET_VERSION)"
else
    print_status "error" ".NET CLI is not available"
    exit 1
fi

echo ""

# Check workflow file exists
WORKFLOW_FILE=".github/workflows/release.yml"
if [ -f "$WORKFLOW_FILE" ]; then
    print_status "success" "Release workflow file exists: $WORKFLOW_FILE"
    
    # Check for OIDC permissions in workflow
    if grep -q "id-token: write" "$WORKFLOW_FILE"; then
        print_status "success" "OIDC permissions found in workflow file"
    else
        print_status "error" "OIDC permissions not found in workflow file"
        print_status "info" "Add 'id-token: write' to workflow permissions"
    fi
    
    # Check for environment configuration
    if grep -q "environment:" "$WORKFLOW_FILE"; then
        print_status "success" "Environment configuration found in workflow"
    else
        print_status "warning" "No environment configuration found (optional but recommended)"
    fi
    
else
    print_status "error" "Release workflow file not found: $WORKFLOW_FILE"
fi

echo ""

# Check project file for package metadata
PROJECT_FILE="src/JsonToolkit.STJ/JsonToolkit.STJ.csproj"
if [ -f "$PROJECT_FILE" ]; then
    print_status "success" "Project file exists: $PROJECT_FILE"
    
    # Check for package ID
    if grep -q "<PackageId>" "$PROJECT_FILE"; then
        PACKAGE_ID=$(grep "<PackageId>" "$PROJECT_FILE" | sed 's/.*<PackageId>\(.*\)<\/PackageId>.*/\1/' | tr -d ' ')
        print_status "success" "Package ID found: $PACKAGE_ID"
    else
        print_status "warning" "Package ID not explicitly set (will use assembly name)"
    fi
    
else
    print_status "warning" "Project file not found: $PROJECT_FILE"
fi

echo ""

# Validation summary
print_status "info" "Validation Summary"
echo "=================="
echo ""
echo "For NuGet Trusted Publishing to work, ensure:"
echo "1. ✅ Repository is public or has GitHub Pro/Enterprise"
echo "2. ✅ Workflow has 'id-token: write' permission"
echo "3. ✅ Trusted publisher is configured on NuGet.org with:"
echo "   - Repository owner: $(echo ${GITHUB_REPOSITORY:-'your-username/repo'} | cut -d'/' -f1)"
echo "   - Repository name: $(echo ${GITHUB_REPOSITORY:-'your-username/repo'} | cut -d'/' -f2)"
echo "   - Workflow name: release.yml"
echo "   - Package ID: JsonToolkit.STJ"
echo "4. ✅ You are an owner of the NuGet package"
echo ""

if [ -n "$GITHUB_ACTIONS" ]; then
    print_status "info" "Run this workflow on a version tag (v*.*.*) to test publishing"
else
    print_status "info" "Run this script in GitHub Actions to validate OIDC environment"
fi

echo ""
print_status "info" "For detailed setup instructions, see: docs/NUGET_TRUSTED_PUBLISHING.md"