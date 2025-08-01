# Sims4Tools .NET 8.0 Migration - Quick Start Guide

## Prerequisites

1. **.NET 8.0 SDK** installed
2. **Visual Studio 2022** (17.8 or later) or **Visual Studio Code**
3. **PowerShell 7** (for migration scripts)
4. **Git** (for version control during migration)

## Quick Start (30 minutes)

### Step 1: Backup and Branch (5 minutes)
```bash
cd c:\Users\nawgl\code\Sims4Tools
git checkout -b net8-migration
git add . && git commit -m "Pre-migration backup"
```

### Step 2: Run Migration Script (10 minutes)
```powershell
# Dry run first to see what will change
.\migration-script.ps1 -DryRun

# Run the actual migration
.\migration-script.ps1 -BackupOriginal
```

### Step 3: Add Platform Services (10 minutes)
The platform abstraction layer is already created in:
- `src/Platform/Sims4Tools.Platform.Abstractions/`
- `src/Platform/Sims4Tools.Platform.Windows/`
- `src/Platform/Sims4Tools.Platform.CrossPlatform/`

### Step 4: Test Build (5 minutes)
```bash
dotnet build
dotnet test  # if tests exist
```

## Detailed Implementation

### Phase 1: Convert Core Libraries First

**Recommended Order:**
1. `CS System Classes` → `src/Core/Sims4Tools.Core/`
2. `s4pi/Interfaces` → `src/Core/Sims4Tools.Interfaces/`
3. `s4pi/Package` → `src/Core/Sims4Tools.Package/`

**Example conversion for CS System Classes:**

**Old project file (CS System Classes.csproj):**
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.8.1</TargetFrameworkVersion>
    <!-- ... lots of boilerplate ... -->
  </PropertyGroup>
  <!-- ... -->
</Project>
```

**New SDK-style project file:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AssemblyName>Sims4Tools.Core</AssemblyName>
    <RootNamespace>System</RootNamespace>
  </PropertyGroup>
</Project>
```

### Phase 2: Update Main Applications

**For s4pe (Windows Forms app):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <OutputType>WinExe</OutputType>
    <UseWindowsForms>true</UseWindowsForms>
    <AssemblyName>s4pe</AssemblyName>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\Platform\Sims4Tools.Platform.Windows\Sims4Tools.Platform.Windows.csproj" />
    <ProjectReference Include="..\Core\Sims4Tools.Core\Sims4Tools.Core.csproj" />
  </ItemGroup>
</Project>
```

### Phase 3: Replace Windows-Specific Code

**Before (Registry access):**
```csharp
string gamePath = (string)Microsoft.Win32.Registry.GetValue(
    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Maxis\\The Sims 4", 
    "Install Dir", null);
```

**After (Platform abstracted):**
```csharp
var platformServices = PlatformServiceFactory.CreatePlatformServices();
string gamePath = platformServices.GetGameInstallPath();
```

**Before (Window management):**
```csharp
[DllImport("user32.dll")]
private static extern bool SetForegroundWindow(IntPtr hWnd);

SetForegroundWindow(windowHandle);
```

**After (Platform abstracted):**
```csharp
var windowManager = PlatformServiceFactory.CreateWindowManager();
windowManager.SetForeground(windowHandle);
```

### Phase 4: Configuration Migration

**Before (App.config):**
```xml
<configuration>
  <appSettings>
    <add key="TS4Path" value="" />
    <add key="TS4UserPath" value="" />
  </appSettings>
</configuration>
```

**After (appsettings.json + code):**
```json
{
  "GamePaths": {
    "TS4Path": "",
    "TS4UserPath": ""
  }
}
```

```csharp
public class GameConfiguration
{
    private readonly IConfiguration _config;
    
    public GameConfiguration()
    {
        _config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
    }
    
    public string TS4Path 
    { 
        get => _config["GamePaths:TS4Path"];
        set => SetConfigValue("GamePaths:TS4Path", value);
    }
}
```

## Testing Strategy

### Unit Tests Template
```csharp
[Test]
public void PlatformServices_GetGamePath_Windows_ReturnsValidPath()
{
    // Arrange
    var services = new WindowsPlatformServices();
    
    // Act
    var gamePath = services.GetGameInstallPath();
    
    // Assert
    if (gamePath != null)
    {
        Assert.IsTrue(Directory.Exists(gamePath));
    }
}

[Test]
public void PackageReading_LoadValidPackage_Success()
{
    // Test that package reading still works after migration
    var package = Package.OpenPackage("test.package");
    Assert.IsNotNull(package);
}
```

### Manual Testing Checklist
- [ ] s4pe launches and shows UI
- [ ] Can open .package files
- [ ] Can save/export resources
- [ ] Game path detection works
- [ ] Settings persist between sessions
- [ ] All resource wrappers load correctly

## Troubleshooting Common Issues

### Issue: "Could not load file or assembly"
**Solution:** Check that all project references are updated to new locations.

### Issue: "System.Configuration not found"
**Solution:** Add NuGet package:
```bash
dotnet add package System.Configuration.ConfigurationManager
```

### Issue: Windows Forms designer issues
**Solution:** Use `net8.0-windows` target framework and add:
```xml
<UseWindowsForms>true</UseWindowsForms>
```

### Issue: P/Invoke DLL not found
**Solution:** Copy native DLLs to output directory:
```xml
<ItemGroup>
  <None Include="squishinterface_x64.dll">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## Performance Expectations

**Expected improvements with .NET 8.0:**
- **Startup time**: 20-30% faster
- **Memory usage**: 10-20% reduction
- **Package loading**: 15-25% faster
- **File I/O**: Improved on all platforms

## Cross-Platform Support Matrix

| Feature | Windows | Linux | macOS | Notes |
|---------|---------|-------|-------|--------|
| Core Libraries | ✅ Full | ✅ Full | ✅ Full | All package manipulation |
| GUI Applications | ✅ Full | ❌ None | ❌ None | Windows Forms only |
| Game Detection | ✅ Registry | ⚠️ Limited | ⚠️ Limited | Common paths only |
| DDS Compression | ✅ Native | ⚠️ Fallback | ⚠️ Fallback | Managed implementation |
| File Explorer | ✅ Full | ✅ Basic | ✅ Basic | Platform file managers |

## Next Steps After Migration

1. **Create CLI Tools** for cross-platform usage
2. **Web Interface** using ASP.NET Core
3. **Performance Optimization** with .NET 8 features
4. **Enhanced Testing** with cross-platform CI/CD

## Success Metrics

- [ ] All existing functionality works on Windows
- [ ] Core libraries work on Linux/macOS
- [ ] Build time improved
- [ ] Runtime performance improved
- [ ] Codebase is more maintainable
- [ ] Platform abstraction allows future enhancements
