# ModelingEvolution.Drawing Release Script for Windows
# Usage: .\release.ps1 [-Patch] [-Minor] [-Major] [-Version X.X.X.X]

param(
    [switch]$Patch = $false,
    [switch]$Minor = $false,
    [switch]$Major = $false,
    [string]$Version = ""
)

# Colors for output
function Write-Info { Write-Host "[INFO] $args" -ForegroundColor Green }
function Write-Warn { Write-Host "[WARN] $args" -ForegroundColor Yellow }
function Write-Error { Write-Host "[ERROR] $args" -ForegroundColor Red }

# Function to get the latest version from git tags
function Get-CurrentVersion {
    $latestTag = git tag -l "ModelingEvolution.Drawing/*" | Sort-Object -Version | Select-Object -Last 1
    
    if ($latestTag) {
        # Extract version from tag (format: ModelingEvolution.Drawing/1.0.57.35)
        return $latestTag -replace "ModelingEvolution.Drawing/", ""
    }
    else {
        # Fallback to csproj if no tags exist
        $csprojPath = "Sources\ModelingEvolution.Drawing\ModelingEvolution.Drawing.csproj"
        if (Test-Path $csprojPath) {
            $csproj = Get-Content $csprojPath -Raw
            if ($csproj -match '<AssemblyVersion>([^<]+)</AssemblyVersion>') {
                return $matches[1]
            }
        }
        return "1.0.57.35"  # Default based on known last version
    }
}

# Function to calculate next version
function Get-NextVersion {
    param(
        [string]$CurrentVersion,
        [string]$BumpType
    )
    
    $parts = $CurrentVersion.Split('.')
    $major = [int]$parts[0]
    $minor = if ($parts.Length -gt 1) { [int]$parts[1] } else { 0 }
    $patch = if ($parts.Length -gt 2) { [int]$parts[2] } else { 0 }
    $build = if ($parts.Length -gt 3) { [int]$parts[3] } else { 0 }
    
    switch ($BumpType) {
        "major" {
            $major++
            $minor = 0
            $patch = 0
            $build = 0
        }
        "minor" {
            $minor++
            $patch = 0
            $build++
        }
        "patch" {
            $patch++
            $build++
        }
        default {
            Write-Error "Unknown bump type: $BumpType"
            exit 1
        }
    }
    
    return "$major.$minor.$patch.$build"
}

# Function to update version in csproj
function Update-CsprojVersion {
    param([string]$Version)
    
    $csprojPath = "Sources\ModelingEvolution.Drawing\ModelingEvolution.Drawing.csproj"
    
    if (-not (Test-Path $csprojPath)) {
        Write-Error "Could not find $csprojPath"
        return $false
    }
    
    Write-Info "Updating version in $csprojPath to $Version"
    
    $content = Get-Content $csprojPath -Raw
    
    # Update AssemblyVersion
    $content = $content -replace '<AssemblyVersion>[^<]+</AssemblyVersion>', "<AssemblyVersion>$Version</AssemblyVersion>"
    
    # Update FileVersion
    $content = $content -replace '<FileVersion>[^<]+</FileVersion>', "<FileVersion>$Version</FileVersion>"
    
    # Update or add Version tag
    if ($content -match '<Version>') {
        $content = $content -replace '<Version>[^<]+</Version>', "<Version>$Version</Version>"
    }
    else {
        # Add Version after AssemblyVersion
        $content = $content -replace '(<AssemblyVersion>[^<]+</AssemblyVersion>)', "`$1`n    <Version>$Version</Version>"
    }
    
    Set-Content -Path $csprojPath -Value $content -NoNewline
    return $true
}

# Main script
function Main {
    # Determine bump type
    $bumpType = "patch"  # Default
    $customVersion = ""
    
    if ($Major) { $bumpType = "major" }
    elseif ($Minor) { $bumpType = "minor" }
    elseif ($Patch) { $bumpType = "patch" }
    elseif ($Version) { $customVersion = $Version }
    
    # Ensure we're in a git repository
    try {
        git rev-parse --git-dir | Out-Null
    }
    catch {
        Write-Error "Not in a git repository"
        exit 1
    }
    
    # Check for uncommitted changes
    $gitStatus = git status --porcelain
    if ($gitStatus) {
        Write-Warn "You have uncommitted changes. Continue anyway? (y/N)"
        $response = Read-Host
        if ($response -ne 'y' -and $response -ne 'Y') {
            Write-Info "Aborted"
            exit 0
        }
    }
    
    # Get current version
    $currentVersion = Get-CurrentVersion
    Write-Info "Current version: $currentVersion"
    
    # Calculate next version
    if ($customVersion) {
        $nextVersion = $customVersion
        Write-Info "Using custom version: $nextVersion"
    }
    else {
        $nextVersion = Get-NextVersion -CurrentVersion $currentVersion -BumpType $bumpType
        Write-Info "Next version ($bumpType bump): $nextVersion"
    }
    
    # Confirm with user
    Write-Host ""
    Write-Warn "This will:"
    Write-Host "  1. Update version in .csproj to $nextVersion"
    Write-Host "  2. Commit the changes"
    Write-Host "  3. Create tag: ModelingEvolution.Drawing/$nextVersion"
    Write-Host "  4. Push changes and tag to origin"
    Write-Host ""
    Write-Warn "Continue? (y/N)"
    $response = Read-Host
    
    if ($response -ne 'y' -and $response -ne 'Y') {
        Write-Info "Aborted"
        exit 0
    }
    
    # Update csproj
    if (-not (Update-CsprojVersion -Version $nextVersion)) {
        exit 1
    }
    
    # Commit changes
    Write-Info "Committing version update..."
    git add Sources/ModelingEvolution.Drawing/ModelingEvolution.Drawing.csproj
    git commit -m "Bump version to $nextVersion"
    
    # Create tag
    $tagName = "ModelingEvolution.Drawing/$nextVersion"
    Write-Info "Creating tag: $tagName"
    git tag -a $tagName -m "Release ModelingEvolution.Drawing v$nextVersion"
    
    # Push changes and tag
    Write-Info "Pushing to origin..."
    git push origin HEAD
    git push origin $tagName
    
    Write-Info "✅ Successfully created release $nextVersion"
    Write-Host ""
    Write-Host "Tag: $tagName"
    Write-Host "This will trigger the GitHub Actions workflow to:"
    Write-Host "  - Create a GitHub release"
    Write-Host "  - Publish to NuGet"
    Write-Host ""
    Write-Host "Monitor progress at: https://github.com/modelingevolution/drawing/actions"
}

# Run main function
Main