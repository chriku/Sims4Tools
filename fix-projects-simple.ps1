# PowerShell script to fix all .csproj files in the Sims4Tools solution
Write-Host "Starting to fix all .csproj files in Sims4Tools solution..." -ForegroundColor Green

$baseDir = "c:\Users\nawgl\code\Sims4Tools"

# Clean all obj directories first
Write-Host "Cleaning obj directories..." -ForegroundColor Cyan
Get-ChildItem -Path $baseDir -Recurse -Directory -Name "obj" | ForEach-Object {
    $objPath = Join-Path $baseDir $_
    if (Test-Path $objPath) {
        Remove-Item $objPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Cleaned: $objPath" -ForegroundColor DarkGray
    }
}

# Function to fix a project file
function Update-ProjectFile {
    param($projectPath, $needsInterfacesRef = $false)
    
    Write-Host "Fixing: $projectPath" -ForegroundColor Yellow
    
    $content = Get-Content $projectPath -Raw
    
    # Check if it's SDK-style and needs fixes
    if ($content -match 'Project Sdk=') {
        # Add GenerateAssemblyInfo=false if not present
        if ($content -notmatch 'GenerateAssemblyInfo') {
            $content = $content -replace '(<TargetFramework>.*?</TargetFramework>)', '$1`r`n    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>'
        }
        
        # Add Deterministic=false if not present
        if ($content -notmatch 'Deterministic') {
            $content = $content -replace '(<GenerateAssemblyInfo>false</GenerateAssemblyInfo>)', '$1`r`n    <Deterministic>false</Deterministic>'
        }
        
        # Add Interfaces reference if needed and not present
        if ($needsInterfacesRef -and $content -notmatch 'Interfaces\.csproj') {
            if ($content -match '</PropertyGroup>') {
                $refBlock = "`r`n`r`n  <ItemGroup>`r`n    <ProjectReference Include=`"..\..\s4pi\Interfaces\Interfaces.csproj`" />`r`n  </ItemGroup>"
                $content = $content -replace '(</PropertyGroup>)', '$1' + $refBlock
            }
        }
        
        Set-Content $projectPath $content -Encoding UTF8
        Write-Host "  Fixed: $projectPath" -ForegroundColor Green
    }
}

# Fix wrapper projects
$wrapperProjects = @(
    "s4pi Wrappers\ComplateResource\ComplateResource.csproj",
    "s4pi Wrappers\WorldObjectDataResource\WorldObjectDataResource.csproj",
    "s4pi Wrappers\CASPartResource\CASPartResource.csproj",
    "s4pi Wrappers\ImageResource\ImageResource.csproj",
    "s4pi Wrappers\JazzResource\JazzResource.csproj",
    "s4pi Wrappers\MeshChunks\MeshChunks.csproj",
    "s4pi Wrappers\NGMPHashMapResource\NGMPHashMapResource.csproj",
    "s4pi Wrappers\RigResource\RigResource.csproj",
    "s4pi Wrappers\s4piRCOLChunks\s4piRCOLChunks.csproj",
    "s4pi Wrappers\GenericRCOLResource\GenericRCOLResource.csproj",
    "s4pi Wrappers\NameMapResource\NameMapResource.csproj",
    "s4pi Wrappers\CatalogResource\CatalogResource.csproj",
    "s4pi Wrappers\StblResource\StblResource.csproj",
    "s4pi Wrappers\ModularResource\ModularResource.csproj",
    "s4pi Wrappers\TxtcResource\TxtcResource.csproj",
    "s4pi Wrappers\TextResource\TextResource.csproj"
)

foreach ($project in $wrapperProjects) {
    $fullPath = Join-Path $baseDir $project
    if (Test-Path $fullPath) {
        Update-ProjectFile $fullPath $true
    }
}

# Fix helper projects
$helperProjects = @(
    "s4pe Helpers\DMAPImageHelper\DMAPImageHelper.csproj",
    "s4pe Helpers\RLESDDSHelper\RLESDDSHelper.csproj", 
    "s4pe Helpers\ThumbnailHelper\ThumbnailHelper.csproj"
)

foreach ($project in $helperProjects) {
    $fullPath = Join-Path $baseDir $project
    if (Test-Path $fullPath) {
        Update-ProjectFile $fullPath $true
    }
}

Write-Host "All project files have been fixed!" -ForegroundColor Green
