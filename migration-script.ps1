#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Migrates Sims4Tools projects from .NET Framework 4.8.1 to .NET 8.0
.DESCRIPTION
    This script automates the conversion of project files from the old format to SDK-style projects.
    It handles the most common migration tasks and generates reports on what needs manual attention.
.PARAMETER ProjectPath
    Path to the solution or project directory
.PARAMETER DryRun
    Preview changes without making them
.PARAMETER BackupOriginal
    Create backup of original project files
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$ProjectPath = ".",
    
    [Parameter()]
    [switch]$DryRun,
    
    [Parameter()]
    [switch]$BackupOriginal = $true
)

$ErrorActionPreference = "Stop"

function Generate-NewProjectFile {
    param($Analysis)
    
    $targetFramework = if ($Analysis.IsWindowsForms -or $Analysis.IsWPF) { "net8.0-windows" } else { "net8.0" }
    
    $content = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>$targetFramework</TargetFramework>
    <AssemblyName>$($Analysis.Name)</AssemblyName>
    <RootNamespace>$($Analysis.Name -replace '[\s\-\.]', '')</RootNamespace>
"@

    if ($Analysis.IsExecutable) {
        $outputType = if ($Analysis.IsWindowsForms -or $Analysis.IsWPF) { "WinExe" } else { "Exe" }
        $content += "`n    <OutputType>$outputType</OutputType>"
    }
    
    if ($Analysis.IsWindowsForms) {
        $content += "`n    <UseWindowsForms>true</UseWindowsForms>"
    }
    
    if ($Analysis.IsWPF) {
        $content += "`n    <UseWPF>true</UseWPF>"
    }
    
    if ($Analysis.HasUnsafeCode) {
        $content += "`n    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>"
    }
    
    $content += "`n  </PropertyGroup>"
    
    # Add project references if any exist
    if ($Analysis.ProjectReferences.Count -gt 0) {
        $content += "`n`n  <ItemGroup>"
        foreach ($projRef in $Analysis.ProjectReferences) {
            $content += "`n    <ProjectReference Include=`"$projRef`" />"
        }
        $content += "`n  </ItemGroup>"
    }
    
    # Add additional package references for specific frameworks
    $needsPackageGroup = $false
    $packageRefsContent = ""
    
    # Add System.Drawing.Common for Windows Forms projects
    if ($Analysis.IsWindowsForms -and $Analysis.References -contains "System.Drawing") {
        if (-not $needsPackageGroup) { 
            $packageRefsContent += "`n`n  <ItemGroup>"
            $needsPackageGroup = $true
        }
        $packageRefsContent += "`n    <PackageReference Include=`"System.Drawing.Common`" Version=`"8.0.0`" />"
    }
    
    if ($needsPackageGroup) {
        $packageRefsContent += "`n  </ItemGroup>"
        $content += $packageRefsContent
    }
    
    $content += "`n`n</Project>"

    return $content
}

Write-Host "Sims4Tools .NET 8.0 Migration Script" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# Find all .csproj files
$projectFiles = Get-ChildItem -Path $ProjectPath -Filter "*.csproj" -Recurse

Write-Host "Found $($projectFiles.Count) project files to analyze" -ForegroundColor Yellow

$migrationReport = @()

foreach ($projectFile in $projectFiles) {
    Write-Host "Analyzing: $($projectFile.Name)" -ForegroundColor Cyan
    
    # Read the project file
    $content = Get-Content $projectFile.FullName -Raw
    
    # Determine if it's an old-style project
    $isOldStyle = $content -match '<Project\s+ToolsVersion' -or $content -match 'Microsoft\.CSharp\.targets'
    
    if (-not $isOldStyle) {
        Write-Host "  Already SDK-style, skipping" -ForegroundColor Green
        continue
    }
    
    # Analyze project characteristics
    $projectAnalysis = @{
        File = $projectFile.FullName
        Name = $projectFile.BaseName
        IsWindowsForms = $content -match 'System\.Windows\.Forms'
        IsWPF = $content -match 'PresentationFramework'
        HasUnsafeCode = $content -match '<AllowUnsafeBlocks>true</AllowUnsafeBlocks>'
        IsExecutable = $content -match '<OutputType>WinExe</OutputType>' -or $content -match '<OutputType>Exe</OutputType>'
        HasAppConfig = Test-Path (Join-Path $projectFile.Directory "App.config")
        References = @()
        ProjectReferences = @()
    }
    
    # Extract references
    $referenceMatches = [regex]::Matches($content, '<Reference Include="([^"]+)"')
    foreach ($match in $referenceMatches) {
        $projectAnalysis.References += $match.Groups[1].Value
    }
    
    # Extract project references  
    $projectRefMatches = [regex]::Matches($content, '<ProjectReference Include="([^"]+)"')
    foreach ($match in $projectRefMatches) {
        $projectAnalysis.ProjectReferences += $match.Groups[1].Value
    }
    
    $migrationReport += $projectAnalysis
    
    if ($DryRun) {
        Write-Host "  [DRY RUN] Would convert to SDK-style" -ForegroundColor Yellow
        continue
    }
    
    # Create backup if requested
    if ($BackupOriginal) {
        $backupPath = $projectFile.FullName + ".backup"
        Copy-Item $projectFile.FullName $backupPath
        Write-Host "  Created backup: $backupPath" -ForegroundColor Gray
    }
    
    # Generate new SDK-style project file
    $newProjectContent = Generate-NewProjectFile -Analysis $projectAnalysis
    
    # Write new project file
    Set-Content -Path $projectFile.FullName -Value $newProjectContent -Encoding UTF8
    Write-Host "  Converted to SDK-style" -ForegroundColor Green
}

# Generate migration report
Write-Host "`nMigration Report" -ForegroundColor Green
Write-Host "================" -ForegroundColor Green

$windowsFormsProjects = $migrationReport | Where-Object { $_.IsWindowsForms }
$executableProjects = $migrationReport | Where-Object { $_.IsExecutable }
$unsafeProjects = $migrationReport | Where-Object { $_.HasUnsafeCode }

Write-Host "Windows Forms projects: $($windowsFormsProjects.Count)" -ForegroundColor Yellow
foreach ($project in $windowsFormsProjects) {
    Write-Host "  - $($project.Name)" -ForegroundColor Gray
}

Write-Host "Executable projects: $($executableProjects.Count)" -ForegroundColor Yellow
foreach ($project in $executableProjects) {
    Write-Host "  - $($project.Name)" -ForegroundColor Gray
}

Write-Host "Projects with unsafe code: $($unsafeProjects.Count)" -ForegroundColor Yellow
foreach ($project in $unsafeProjects) {
    Write-Host "  - $($project.Name)" -ForegroundColor Gray
}

# Check for common references that need NuGet packages
$commonReferences = @{
    "System.Configuration" = "System.Configuration.ConfigurationManager"
    "System.Drawing" = "System.Drawing.Common"
    "Microsoft.Win32.Registry" = "Microsoft.Win32.Registry"
}

$packagesNeeded = @{}
foreach ($project in $migrationReport) {
    foreach ($reference in $project.References) {
        if ($commonReferences.ContainsKey($reference)) {
            if (-not $packagesNeeded.ContainsKey($commonReferences[$reference])) {
                $packagesNeeded[$commonReferences[$reference]] = @()
            }
            $packagesNeeded[$commonReferences[$reference]] += $project.Name
        }
    }
}

if ($packagesNeeded.Count -gt 0) {
    Write-Host "`nNuGet packages to install:" -ForegroundColor Yellow
    foreach ($package in $packagesNeeded.Keys) {
        Write-Host "  $package" -ForegroundColor Cyan
        Write-Host "    Used by: $($packagesNeeded[$package] -join ', ')" -ForegroundColor Gray
    }
}

Write-Host "`nNext Steps:" -ForegroundColor Green
Write-Host "1. Review and test converted projects" -ForegroundColor White
Write-Host "2. Install required NuGet packages" -ForegroundColor White
Write-Host "3. Update App.config files to appsettings.json" -ForegroundColor White
Write-Host "4. Test functionality on target platforms" -ForegroundColor White

Write-Host "`nMigration script completed!" -ForegroundColor Green
