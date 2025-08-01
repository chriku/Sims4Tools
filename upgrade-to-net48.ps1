# Phase 1: Upgrade Sims4Tools from .NET Framework 4.0 to 4.8
# This script updates all .csproj files to target .NET Framework 4.8

Write-Host "Starting Phase 1: .NET Framework 4.0 -> 4.8 Upgrade" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

# Get all .csproj files in the solution
$projectFiles = Get-ChildItem -Recurse -Filter "*.csproj" | Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" }

Write-Host "Found $($projectFiles.Count) project files to update:" -ForegroundColor Yellow
$projectFiles | ForEach-Object { Write-Host "  - $($_.FullName)" }

$updatedCount = 0
$failedFiles = @()

foreach ($projectFile in $projectFiles) {
    try {
        Write-Host "`nProcessing: $($projectFile.Name)" -ForegroundColor Cyan
        
        # Read the content
        $content = Get-Content $projectFile.FullName -Raw
        $originalContent = $content
        
        # Update TargetFrameworkVersion from v4.0 to v4.8
        $content = $content -replace '<TargetFrameworkVersion>v4\.0</TargetFrameworkVersion>', '<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>'
        
        # Check if any changes were made
        if ($content -ne $originalContent) {
            # Backup original file
            $backupPath = "$($projectFile.FullName).bak"
            Copy-Item $projectFile.FullName $backupPath
            
            # Write updated content
            Set-Content -Path $projectFile.FullName -Value $content -NoNewline
            
            Write-Host "  Updated successfully (backup saved as .bak)" -ForegroundColor Green
            $updatedCount++
        } else {
            Write-Host "  - No changes needed" -ForegroundColor Gray
        }
    }
    catch {
        Write-Host "  Error updating file: $($_.Exception.Message)" -ForegroundColor Red
        $failedFiles += $projectFile.FullName
    }
}

Write-Host "`n=================================================" -ForegroundColor Green
Write-Host "Phase 1 Update Summary:" -ForegroundColor Green
Write-Host "  Total files processed: $($projectFiles.Count)"
Write-Host "  Files updated: $updatedCount" -ForegroundColor Green

if ($failedFiles.Count -gt 0) {
    Write-Host "  Files failed: $($failedFiles.Count)" -ForegroundColor Red
    Write-Host "  Failed files:" -ForegroundColor Red
    $failedFiles | ForEach-Object { Write-Host "    - $_" -ForegroundColor Red }
}

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Update App.config files to reference .NET Framework 4.8"
Write-Host "2. Build and test the solution"
Write-Host "3. Run regression tests"
