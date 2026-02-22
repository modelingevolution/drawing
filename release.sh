#!/bin/bash

# ModelingEvolution.Drawing Release Script
# Usage: ./release.sh [--patch|--minor|--major|--version X.X.X.X]
#
# Creates a git tag that triggers GitHub Actions to build, inject version via sed, and publish to NuGet.
# The csproj stays at 1.0.0 placeholder — CI/CD handles version injection at build time.

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
print_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Function to get the latest version from git tags
get_current_version() {
    local latest_tag=$(git tag -l "ModelingEvolution.Drawing/*" | sort -V | tail -1)

    if [ -n "$latest_tag" ]; then
        echo "${latest_tag#ModelingEvolution.Drawing/}"
    else
        echo "1.0.0.0"
    fi
}

# Function to calculate next version
calculate_next_version() {
    local current_version=$1
    local bump_type=$2

    IFS='.' read -ra VERSION_PARTS <<< "$current_version"
    local major="${VERSION_PARTS[0]:-1}"
    local minor="${VERSION_PARTS[1]:-0}"
    local patch="${VERSION_PARTS[2]:-0}"
    local build="${VERSION_PARTS[3]:-0}"

    case "$bump_type" in
        major)
            major=$((major + 1))
            minor=0
            patch=0
            build=0
            ;;
        minor)
            minor=$((minor + 1))
            patch=0
            build=$((build + 1))
            ;;
        patch)
            patch=$((patch + 1))
            build=$((build + 1))
            ;;
        *)
            print_error "Unknown bump type: $bump_type"
            exit 1
            ;;
    esac

    echo "$major.$minor.$patch.$build"
}

# Main script
main() {
    local bump_type="patch"
    local custom_version=""

    while [[ $# -gt 0 ]]; do
        case $1 in
            --patch)
                bump_type="patch"
                shift
                ;;
            --minor)
                bump_type="minor"
                shift
                ;;
            --major)
                bump_type="major"
                shift
                ;;
            --version)
                custom_version="$2"
                shift 2
                ;;
            -h|--help)
                echo "Usage: $0 [--patch|--minor|--major|--version X.X.X.X]"
                echo ""
                echo "Creates a git tag that triggers GitHub Actions to publish to NuGet."
                echo "The csproj version stays at 1.0.0 — CI/CD injects the real version at build time."
                echo ""
                echo "Options:"
                echo "  --patch    Increment patch version (default)"
                echo "  --minor    Increment minor version"
                echo "  --major    Increment major version"
                echo "  --version  Set a specific version"
                echo "  -h, --help Show this help message"
                exit 0
                ;;
            *)
                print_error "Unknown option: $1"
                echo "Use --help for usage information"
                exit 1
                ;;
        esac
    done

    if ! git rev-parse --git-dir > /dev/null 2>&1; then
        print_error "Not in a git repository"
        exit 1
    fi

    # Check for uncommitted changes
    if ! git diff-index --quiet HEAD --; then
        print_warn "You have uncommitted changes. Continue anyway? (y/N)"
        read -r response
        if [[ ! "$response" =~ ^[Yy]$ ]]; then
            print_info "Aborted"
            exit 0
        fi
    fi

    # Get current version
    local current_version=$(get_current_version)
    print_info "Current version: $current_version"

    # Calculate next version
    local next_version
    if [ -n "$custom_version" ]; then
        next_version="$custom_version"
        print_info "Using custom version: $next_version"
    else
        next_version=$(calculate_next_version "$current_version" "$bump_type")
        print_info "Next version ($bump_type bump): $next_version"
    fi

    # Confirm with user
    local tag_name="ModelingEvolution.Drawing/$next_version"
    echo ""
    print_warn "This will:"
    echo "  1. Create tag: $tag_name"
    echo "  2. Push tag to origin (triggers GitHub Actions NuGet publish)"
    echo ""
    print_warn "Continue? (y/N)"
    read -r response

    if [[ ! "$response" =~ ^[Yy]$ ]]; then
        print_info "Aborted"
        exit 0
    fi

    # Create tag
    print_info "Creating tag: $tag_name"
    git tag -a "$tag_name" -m "Release ModelingEvolution.Drawing v$next_version"

    # Push tag
    print_info "Pushing tag to origin..."
    git push origin "$tag_name"

    print_info "Successfully created release $next_version"
    echo ""
    echo "Tag: $tag_name"
    echo "GitHub Actions will now:"
    echo "  - Inject version $next_version into csproj via sed"
    echo "  - Build and test"
    echo "  - Publish to NuGet"
    echo ""
    echo "Monitor progress at: https://github.com/modelingevolution/drawing/actions"
}

main "$@"
