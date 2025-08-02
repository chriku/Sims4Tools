#!/usr/bin/env pwsh
# Cross-platform PowerShell build script for Sims4Tools
# Note: Currently uses MSBuild for compatibility with existing project structure.
# For true cross-platform support, consider migrating to SDK-style projects.
param(
    [string]$Configuration = "Release",
    [string]$Platform = "Any CPU",
    [switch]$Clean,
    [switch]$Restore,
    [switch]$Build,
    [switch]$All
)

$ErrorActionPreference = "Stop"

# Set default actions if none specified
if (-not ($Clean -or $Restore -or $Build)) {
    $All = $true
}

if ($All) {
    $Clean = $true
    $Restore = $true
    $Build = $true
}

$SolutionFile = "sims4tools.sln"

Write-Host "Building Sims4Tools..." -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Platform: $Platform" -ForegroundColor Yellow

if ($Clean) {
    Write-Host "Cleaning solution..." -ForegroundColor Yellow
    msbuild $SolutionFile /t:Clean /p:Configuration=$Configuration /p:Platform="$Platform" /verbosity:minimal
}

if ($Restore) {
    Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
    msbuild $SolutionFile /t:Restore /p:Configuration=$Configuration /p:Platform="$Platform" /verbosity:minimal
}

if ($Build) {
    Write-Host "Building solution..." -ForegroundColor Yellow
    
    # First, ensure CreateAssemblyVersion utility is available in both Debug and Release
    Write-Host "Ensuring CreateAssemblyVersion utility is available..." -ForegroundColor Yellow
    
    # Build CreateAssemblyVersion for both Debug and Release configurations
    msbuild "s4pi\CreateAssemblyVersion\CreateAssemblyVersion.csproj" /p:Configuration=Debug /p:Platform="AnyCPU" /verbosity:minimal
    msbuild "s4pi\CreateAssemblyVersion\CreateAssemblyVersion.csproj" /p:Configuration=Release /p:Platform="AnyCPU" /verbosity:minimal
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build CreateAssemblyVersion utility"
        return
    }
    
    # Now build the full solution - project references will handle dependencies automatically
    Write-Host "Building full solution in $Configuration configuration..." -ForegroundColor Yellow
    msbuild $SolutionFile /p:Configuration=$Configuration /p:Platform="$Platform" /verbosity:minimal
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build completed successfully!" -ForegroundColor Green
    } else {
        Write-Error "Solution build failed with exit code $LASTEXITCODE"
    }
}
