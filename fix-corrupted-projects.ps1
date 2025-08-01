# Clean up all corrupted project files and fix them properly
Write-Host "Fixing corrupted .csproj files..." -ForegroundColor Green

$corrupted_projects = @(
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
    "s4pi Wrappers\TextResource\TextResource.csproj",
    "s4pe Helpers\DMAPImageHelper\DMAPImageHelper.csproj",
    "s4pe Helpers\RLESDDSHelper\RLESDDSHelper.csproj",
    "s4pe Helpers\ThumbnailHelper\ThumbnailHelper.csproj"
)

foreach ($projectPath in $corrupted_projects) {
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
    }
}

Write-Host "All corrupted project files have been fixed!" -ForegroundColor Green
