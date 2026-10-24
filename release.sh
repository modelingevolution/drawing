#!/bin/bash
#
# Release script: computes the next version from git tags, creates the tag and pushes it.
# Publishing is done by GitHub Actions only (publish-nuget.yml); nothing is ever published from a machine.
#
# Usage: ./release.sh [--patch|--minor|--major|--version X.Y.Z.B] [--preview] [--ci] [--dry-run]
#
#   --patch            bump patch, build+1 (default)
#   --minor            bump minor, patch=0, build+1
#   --major            bump major, minor=0, patch=0, build+1 (build is never reset)
#   --version X.Y.Z.B  use this exact version
#   --preview          tag {next}-preview.{N}; allowed from any branch
#   --ci               no prompts; refuses a dirty tree (untracked files count)
#   --dry-run          print the computed tag and exit 0 without tagging or pushing
#
# Tags are fetched from origin first. A non-preview release runs on master only. Nothing is tagged (previews
# included) unless the newest release tag is an ancestor of HEAD. Inside GitHub Actions ($GITHUB_OUTPUT set) the script appends
# version=, tag= and previous= to $GITHUB_OUTPUT.
#
# This script is copied unchanged between repositories: only PACKAGE and INITIAL_VERSION differ.

set -euo pipefail

PACKAGE="ModelingEvolution.Drawing"
INITIAL_VERSION="1.0.0.0"
RELEASE_BRANCH="master"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Messages go to stderr; stdout carries only the --dry-run result.
print_info() { echo -e "${GREEN}[INFO]${NC} $1" >&2; }
print_warn() { echo -e "${YELLOW}[WARN]${NC} $1" >&2; }
print_error() { echo -e "${RED}[ERROR]${NC} $1" >&2; }

usage() {
    sed -n '6,14p' "$0" | sed 's/^# \{0,1\}//'
}

# Version of the newest release tag, empty when there is none; -preview (any pre-release suffix) tags are ignored.
get_latest_release() {
    git tag -l "$PACKAGE/*" | sed "s#^$PACKAGE/##" | grep -v -- '-' | sort -V | tail -1 || true
}

get_current_version() {
    local latest
    latest=$(get_latest_release)
    if [ -n "$latest" ]; then
        echo "$latest"
    else
        echo "$INITIAL_VERSION"
    fi
}

calculate_next_version() {
    local current_version=$1
    local bump_type=$2
    local major minor patch build

    IFS='.' read -r major minor patch build <<< "$current_version"
    major="${major:-1}"
    minor="${minor:-0}"
    patch="${patch:-0}"
    build="${build:-0}"

    case "$bump_type" in
        major) major=$((major + 1)); minor=0; patch=0 ;;
        minor) minor=$((minor + 1)); patch=0 ;;
        patch) patch=$((patch + 1)) ;;
        *) print_error "Unknown bump type: $bump_type"; exit 1 ;;
    esac
    build=$((build + 1))

    echo "$major.$minor.$patch.$build"
}

# 1 + the highest existing {version}-preview.N
next_preview_number() {
    local version=$1
    local highest
    highest=$(git tag -l "$PACKAGE/$version-preview.*" | sed "s#^$PACKAGE/$version-preview\.##" \
        | grep -E '^[0-9]+$' | sort -n | tail -1 || true)
    echo $(( ${highest:-0} + 1 ))
}

confirm() {
    print_warn "$1 (y/N)"
    local response
    read -r response
    [[ "$response" =~ ^[Yy]$ ]]
}

monitor_url() {
    local url
    url=$(git remote get-url origin 2>/dev/null || true)
    url="${url%.git}"
    url="${url/#git@github.com:/https://github.com/}"
    [ -n "$url" ] && echo "$url/actions"
}

main() {
    local bump_type="patch"
    local custom_version=""
    local version_given=false
    local preview=false
    local ci=false
    local dry_run=false

    while [[ $# -gt 0 ]]; do
        case $1 in
            --patch) bump_type="patch"; shift ;;
            --minor) bump_type="minor"; shift ;;
            --major) bump_type="major"; shift ;;
            --version)
                if [[ $# -lt 2 ]]; then print_error "--version needs a value"; exit 1; fi
                custom_version="$2"; version_given=true; shift 2 ;;
            --preview) preview=true; shift ;;
            --ci) ci=true; shift ;;
            --dry-run) dry_run=true; shift ;;
            -h|--help) usage; exit 0 ;;
            *) print_error "Unknown option: $1"; usage >&2; exit 1 ;;
        esac
    done

    if ! git rev-parse --git-dir > /dev/null 2>&1; then
        print_error "Not in a git repository"
        exit 1
    fi

    if [ "$version_given" = true ] && ! [[ "$custom_version" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        print_error "--version must be X.Y.Z.B, got: $custom_version"
        exit 1
    fi

    local branch
    branch=$(git rev-parse --abbrev-ref HEAD)
    if [ "$preview" = false ] && [ "$branch" != "$RELEASE_BRANCH" ]; then
        print_error "A release runs on $RELEASE_BRANCH only (current branch: $branch). Use --preview on a branch."
        exit 1
    fi

    if [ -n "$(git status --porcelain)" ]; then
        if [ "$ci" = true ]; then
            print_error "Uncommitted changes; --ci refuses a dirty tree"
            exit 1
        elif [ "$dry_run" = true ]; then
            print_warn "Uncommitted changes (ignored by --dry-run)"
        elif ! confirm "You have uncommitted changes. Continue anyway?"; then
            print_info "Aborted"
            exit 0
        fi
    fi

    # Stale local tags would give a stale version: fetch them first.
    if git remote get-url origin > /dev/null 2>&1; then
        if ! git fetch -q --tags origin; then
            if [ "$dry_run" = true ] && [ "$ci" = false ]; then
                print_warn "Could not fetch tags (local tag differs from origin or origin unreachable); ignored by --dry-run"
            else
                print_error "Could not fetch tags (local tag differs from origin or origin unreachable)"
                exit 1
            fi
        fi
    fi

    # The newest release must be in HEAD's history, or an older commit would get a higher version.
    local latest_release
    latest_release=$(get_latest_release)
    if [ -n "$latest_release" ] && ! git merge-base --is-ancestor "refs/tags/$PACKAGE/$latest_release" HEAD; then
        print_error "The newest release $PACKAGE/$latest_release is not an ancestor of HEAD; refusing to tag an older commit. Merge $RELEASE_BRANCH first."
        exit 1
    fi

    local current_version next_version version tag_name
    current_version=$(get_current_version)
    print_info "Current version: $current_version"

    if [ "$version_given" = true ]; then
        next_version="$custom_version"
        print_info "Using custom version: $next_version"
    else
        next_version=$(calculate_next_version "$current_version" "$bump_type")
        print_info "Next version ($bump_type bump): $next_version"
    fi

    version="$next_version"
    if [ "$preview" = true ]; then
        version="$next_version-preview.$(next_preview_number "$next_version")"
        print_info "Preview version: $version"
    fi
    tag_name="$PACKAGE/$version"

    if git rev-parse -q --verify "refs/tags/$tag_name" > /dev/null; then
        print_error "Tag already exists: $tag_name"
        exit 1
    fi

    if [ "$dry_run" = true ]; then
        print_info "Dry run: tag $tag_name not created"
        echo "$tag_name"
        exit 0
    fi

    if [ "$ci" = false ]; then
        echo "" >&2
        print_warn "This will:"
        echo "  1. Create tag: $tag_name" >&2
        echo "  2. Push tag to origin (triggers the GitHub Actions NuGet publish)" >&2
        echo "" >&2
        if ! confirm "Continue?"; then
            print_info "Aborted"
            exit 0
        fi
    fi

    print_info "Creating tag: $tag_name"
    git tag -a "$tag_name" -m "Release $PACKAGE v$version"

    print_info "Pushing tag to origin..."
    if ! git push origin "refs/tags/$tag_name"; then
        # Drop the local tag so a retry is not refused with "Tag already exists".
        git tag -d "$tag_name" > /dev/null
        print_error "Push of $tag_name failed; local tag removed"
        exit 1
    fi

    if [ -n "${GITHUB_OUTPUT:-}" ]; then
        {
            echo "version=$version"
            echo "tag=$tag_name"
            echo "previous=$current_version"
        } >> "$GITHUB_OUTPUT"
    fi

    print_info "Created $tag_name"
    local url
    url=$(monitor_url || true)
    [ -n "$url" ] && print_info "Monitor at: $url"
    exit 0
}

main "$@"
