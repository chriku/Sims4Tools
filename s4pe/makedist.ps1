#!/usr/bin/env pwsh
# Cross-platform distribution script for s4pe
param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "./dist"
)

$ErrorActionPreference = "Stop"

$TargetName = "s4pe"
$DateTime = Get-Date -Format "yy-MMdd-HHmm"
$DistName = "${TargetName}_${DateTime}"

Write-Host "Creating distribution for $TargetName..." -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Output Path: $OutputPath" -ForegroundColor Yellow

# Create output directory
$FullOutputPath = Join-Path $OutputPath $DistName
New-Item -ItemType Directory -Path $FullOutputPath -Force | Out-Null

# Build the project first
Write-Host "Building project..." -ForegroundColor Yellow
& ./build.ps1 -Configuration $Configuration -Build

# Copy built files
$BinPath = Join-Path "s4pe" "bin" $Configuration
if (Test-Path $BinPath) {
    Write-Host "Copying files from $BinPath..." -ForegroundColor Yellow
    Copy-Item -Path "$BinPath/*" -Destination $FullOutputPath -Recurse -Force
    
    # Create version file
    $DateTime | Out-File -FilePath (Join-Path $FullOutputPath "${TargetName}-Version.txt") -Encoding UTF8
    
    Write-Host "Distribution created at: $FullOutputPath" -ForegroundColor Green
} else {
    Write-Error "Build output not found at $BinPath"
}
