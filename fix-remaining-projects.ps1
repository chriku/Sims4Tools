# Fix all remaining wrapper and helper projects that need configuration updates
Write-Host "Fixing remaining .csproj files..." -ForegroundColor Green

$remaining_projects = @(
    "s4pi Wrappers\AnimationResources\AnimationResources.csproj",
    "s4pi Wrappers\MiscellaneousResource\MiscellaneousResource.csproj",
    "s4pi Wrappers\DataResource\DataResource.csproj",
    "s4pi Wrappers\DWorldResource\DWorldResource.csproj",
    "s4pi Wrappers\WorldDescriptionResource\WorldDescriptionResource.csproj",
    "s4pi Wrappers\LotDescriptionResource\LotDescriptionResource.csproj",
    "s4pi Wrappers\RegionDescriptionResource\RegionDescriptionResource.csproj",
    "s4pi Wrappers\TerrainBlendMapResource\TerrainBlendMapResource.csproj",
    "s4pi Wrappers\TerrainMeshResource\TerrainMeshResource.csproj",
    "s4pi Wrappers\ThumbnailCacheTableResource\ThumbnailCacheResource.csproj",
    "s4pi Wrappers\UserCAStPresetResource\UserCAStPresetResource.csproj",
    "s4pi Wrappers\ObjKeyResource\ObjKeyResource.csproj",
    "s4pi Wrappers\ScriptResource\ScriptResource.csproj",
    "s4pe Helpers\DDSHelper\DDSHelper.csproj",
    "s4pe Helpers\LRLEPNGHelper\LRLEPNGHelper.csproj",
    "s4pe Helpers\ModelViewer\ModelViewer.csproj",
    "s4pe Helpers\RLEDDSHelper\RLEDDSHelper.csproj",
    "s4pe Helpers\RLESMaskHelper\RLESMaskHelper.csproj"
)

foreach ($projectPath in $remaining_projects) {
    $fullPath = "c:\Users\nawgl\code\Sims4Tools\$projectPath"
    if (Test-Path $fullPath) {
        Write-Host "Fixing: $projectPath" -ForegroundColor Yellow
        
        # Get the assembly name from the project path
        $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
        
        # Create proper project file content
        $content = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AssemblyName>$assemblyName</AssemblyName>
    <RootNamespace>$assemblyName</RootNamespace>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <Deterministic>false</Deterministic>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\s4pi\Interfaces\Interfaces.csproj" />
  </ItemGroup>

</Project>
"@
        
        Set-Content -Path $fullPath -Value $content -Encoding UTF8
        Write-Host "  Fixed: $projectPath" -ForegroundColor Green
    } else {
        Write-Host "  Not found: $projectPath" -ForegroundColor Red
    }
}

Write-Host "All remaining project files have been fixed!" -ForegroundColor Green
