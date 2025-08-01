# PowerShell script to fix all .csproj files in the Sims4Tools solution
# This fixes assembly attribute duplication and missing references

Write-Host "Starting to fix all .csproj files in Sims4Tools solution..." -ForegroundColor Green

# Define the base directory
$baseDir = "c:\Users\nawgl\code\Sims4Tools"

# Function to fix a project file
function Fix-ProjectFile($projectPath, $needsInterfacesRef = $false, $needsHelpersRef = $false, $additionalRefs = @()) {
    Write-Host "Fixing: $($projectPath)" -ForegroundColor Yellow
    
    $content = Get-Content $projectPath -Raw
    
    # Check if it's already an SDK-style project
    if ($content -match '<Project Sdk="Microsoft\.NET\.Sdk">' -or $content -match '<Project Sdk="Microsoft\.NET\.Sdk\.WindowsDesktop">') {
        $lines = $content -split "\r?\n"
        $newLines = @()
        $inPropertyGroup = $false
        $hasGenerateAssemblyInfo = $false
        $hasDeterministic = $false
        $hasItemGroup = $false
        
        foreach ($line in $lines) {
            $newLines += $line
            
            if ($line.Trim() -eq "<PropertyGroup>") {
                $inPropertyGroup = $true
            }
            elseif ($inPropertyGroup -and $line.Trim() -eq "</PropertyGroup>") {
                # Add missing properties before closing PropertyGroup
                if (-not $hasGenerateAssemblyInfo) {
                    $newLines = $newLines[0..($newLines.Length-2)] + "    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>" + $newLines[-1]
                }
                if (-not $hasDeterministic) {
                    $newLines = $newLines[0..($newLines.Length-2)] + "    <Deterministic>false</Deterministic>" + $newLines[-1]
                }
                $inPropertyGroup = $false
            }
            elseif ($line.Trim().StartsWith("<GenerateAssemblyInfo>")) {
                $hasGenerateAssemblyInfo = $true
            }
            elseif ($line.Trim().StartsWith("<Deterministic>")) {
                $hasDeterministic = $true
            }
            elseif ($line.Trim().StartsWith("<ItemGroup>")) {
                $hasItemGroup = $true
            }
        }
        
        # Add ItemGroup for project references if needed
        if (($needsInterfacesRef -or $needsHelpersRef -or $additionalRefs.Count -gt 0) -and -not $hasItemGroup) {
            $insertIndex = $newLines.Length - 1  # Before </Project>
            $newLines = $newLines[0..($insertIndex-1)] + "" + "  <ItemGroup>" + $newLines[$insertIndex..$newLines.Length]
            
            if ($needsInterfacesRef) {
                $insertIndex = $newLines.Length - 1
                $newLines = $newLines[0..($insertIndex-1)] + '    <ProjectReference Include="..\..\s4pi\Interfaces\Interfaces.csproj" />' + $newLines[$insertIndex..$newLines.Length]
            }
            
            if ($needsHelpersRef) {
                $insertIndex = $newLines.Length - 1
                $newLines = $newLines[0..($insertIndex-1)] + '    <ProjectReference Include="..\..\s4pi Extras\Helpers\Helpers.csproj" />' + $newLines[$insertIndex..$newLines.Length]
            }
            
            foreach ($ref in $additionalRefs) {
                $insertIndex = $newLines.Length - 1
                $newLines = $newLines[0..($insertIndex-1)] + "    $ref" + $newLines[$insertIndex..$newLines.Length]
            }
            
            $insertIndex = $newLines.Length - 1
            $newLines = $newLines[0..($insertIndex-1)] + "  </ItemGroup>" + $newLines[$insertIndex..$newLines.Length]
        }
        
        $newContent = $newLines -join "`r`n"
        Set-Content $projectPath $newContent -Encoding UTF8
        Write-Host "  ✓ Fixed: $($projectPath)" -ForegroundColor Green
    }
    else {
        Write-Host "  ⚠ Skipped (not SDK-style): $($projectPath)" -ForegroundColor Orange
    }
}

# Clean all obj directories first to remove old generated files
Write-Host "`nCleaning obj directories..." -ForegroundColor Cyan
Get-ChildItem -Path $baseDir -Recurse -Directory -Name "obj" | ForEach-Object {
    $objPath = Join-Path $baseDir $_
    if (Test-Path $objPath) {
        Remove-Item $objPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Cleaned: $objPath" -ForegroundColor DarkGray
    }
}

# Fix s4pi Wrapper projects (need Interfaces reference)
Write-Host "`nFixing s4pi Wrapper projects..." -ForegroundColor Cyan
$wrapperProjects = @(
    "s4pi Wrappers\CASPartResource\CASPartResource.csproj",
    "s4pi Wrappers\CatalogResource\CatalogResource.csproj",
    "s4pi Wrappers\ComplateResource\ComplateResource.csproj",
    "s4pi Wrappers\DataResource\DataResource.csproj",
    "s4pi Wrappers\DWorldResource\DWorldResource.csproj",
    "s4pi Wrappers\GenericRCOLResource\GenericRCOLResource.csproj",
    "s4pi Wrappers\ImageResource\ImageResource.csproj",
    "s4pi Wrappers\JazzResource\JazzResource.csproj",
    "s4pi Wrappers\LotDescriptionResource\LotDescriptionResource.csproj",
    "s4pi Wrappers\MeshChunks\MeshChunks.csproj",
    "s4pi Wrappers\MiscellaneousResource\MiscellaneousResource.csproj",
    "s4pi Wrappers\ModularResource\ModularResource.csproj",
    "s4pi Wrappers\NameMapResource\NameMapResource.csproj",
    "s4pi Wrappers\NGMPHashMapResource\NGMPHashMapResource.csproj",
    "s4pi Wrappers\RegionDescriptionResource\RegionDescriptionResource.csproj",
    "s4pi Wrappers\RigResource\RigResource.csproj",
    "s4pi Wrappers\s4piRCOLChunks\s4piRCOLChunks.csproj",
    "s4pi Wrappers\StblResource\StblResource.csproj",
    "s4pi Wrappers\TerrainBlendMapResource\TerrainBlendMapResource.csproj",
    "s4pi Wrappers\TerrainMeshResource\TerrainMeshResource.csproj",
    "s4pi Wrappers\TextResource\TextResource.csproj",
    "s4pi Wrappers\ThumbnailCacheTableResource\ThumbnailCacheResource.csproj",
    "s4pi Wrappers\TxtcResource\TxtcResource.csproj",
    "s4pi Wrappers\WorldDescriptionResource\WorldDescriptionResource.csproj",
    "s4pi Wrappers\WorldObjectDataResource\WorldObjectDataResource.csproj"
)

foreach ($project in $wrapperProjects) {
    $fullPath = Join-Path $baseDir $project
    if (Test-Path $fullPath) {
        Fix-ProjectFile $fullPath $true $false
    }
}

# Fix s4pe Helper projects (need Interfaces and Helpers references)
Write-Host "`nFixing s4pe Helper projects..." -ForegroundColor Cyan
$helperProjects = @(
    "s4pe Helpers\DMAPImageHelper\DMAPImageHelper.csproj",
    "s4pe Helpers\RLEDDSHelper\RLEDDSHelper.csproj",
    "s4pe Helpers\RLESMaskHelper\RLESMaskHelper.csproj",
    "s4pe Helpers\RLESDDSHelper\RLESDDSHelper.csproj",
    "s4pe Helpers\ThumbnailHelper\ThumbnailHelper.csproj",
    "s4pe Helpers\ModelViewer\ModelViewer.csproj"
)

foreach ($project in $helperProjects) {
    $fullPath = Join-Path $baseDir $project
    if (Test-Path $fullPath) {
        Fix-ProjectFile $fullPath $true $true
    }
}

# Fix other projects that just need the assembly info fix
Write-Host "`nFixing other projects..." -ForegroundColor Cyan
$otherProjects = @(
    "s4pi Extras\CustomForms\CustomForms.csproj",
    "s4pi Extras\DDSPanel\DDSPanel.csproj",
    "s4pi Extras\Extensions\Extensions.csproj",
    "s4pi Extras\Filetable\Filetable.csproj",
    "s4pi Extras\s4piControls\s4piControls.csproj"
)

foreach ($project in $otherProjects) {
    $fullPath = Join-Path $baseDir $project
    if (Test-Path $fullPath) {
        Fix-ProjectFile $fullPath $false $false
    }
}

Write-Host "`nAll project files have been fixed!" -ForegroundColor Green
Write-Host "You can now run: dotnet build sims4tools.sln --configuration Debug" -ForegroundColor Cyan
