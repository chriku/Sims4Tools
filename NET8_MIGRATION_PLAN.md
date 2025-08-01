# Sims4Tools .NET 8.0 Migration Plan

## Executive Summary

This document outlines a comprehensive plan to migrate Sims4Tools from .NET Framework 4.8.1 to .NET 8.0 with cross-platform compatibility where feasible. The migration is structured in phases to minimize risk and ensure functionality preservation.

## Current State Analysis

### Project Structure
- **116 C# projects** in solution
- **Target Framework**: .NET Framework 4.8.1
- **Project Format**: Legacy .csproj format
- **Main Applications**: 
  - `s4pe` - Windows Forms GUI application
  - `TS4MorphMaker` - Windows Forms tool
- **Core Libraries**: s4pi framework for Sims 4 package manipulation

### Critical Dependencies Identified

#### 1. Windows-Specific APIs 🔴
- **P/Invoke calls**: user32.dll, kernel32.dll for window management
- **Registry access**: Microsoft.Win32.Registry for game path detection
- **Native DLLs**: squishinterface_Win32.dll, squishinterface_x64.dll for DDS compression

#### 2. Windows Forms Applications 🟡
- Main GUI applications using Windows Forms
- Complex custom controls and designers
- Windows-specific UI behaviors

#### 3. Configuration System 🟡
- Heavy use of System.Configuration
- App.config files throughout solution
- Custom PortableSettingsProvider

#### 4. File System Operations 🟢
- Standard .NET file I/O operations
- Path manipulation (mostly cross-platform compatible)

## Migration Strategy

### Phase 1: Foundation and Planning (Weeks 1-2)

#### 1.1 Project Analysis and Cleanup
```powershell
# Create analysis branch
git checkout -b net8-migration-analysis

# Analyze current dependencies
dotnet list package --outdated --include-transitive
```

#### 1.2 Create New SDK-Style Project Files
- Convert all projects to SDK-style .csproj format
- Consolidate common properties in Directory.Build.props
- Update target framework to net8.0-windows for GUI projects

#### 1.3 Dependency Assessment
- Audit all NuGet packages for .NET 8.0 compatibility
- Identify platform-specific code sections
- Document breaking changes required

### Phase 2: Core Library Migration (Weeks 3-4)

#### 2.1 Start with Platform-Agnostic Libraries
**Migration Order**:
1. `CS System Classes` - Custom system utilities
2. `s4pi.Interfaces` - Core interfaces
3. `s4pi.Package` - Package handling
4. `s4pi Wrappers/*` - Resource wrappers

#### 2.2 Update Project Files Template
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.Win32.Registry" Version="5.0.0" />
    <PackageReference Include="System.Configuration.ConfigurationManager" Version="8.0.0" />
    <PackageReference Include="System.Drawing.Common" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### Phase 3: Platform Abstraction (Weeks 5-6)

#### 3.1 Create Platform Abstraction Layer
```csharp
// IPlatformServices.cs
public interface IPlatformServices
{
    string GetGameInstallPath();
    string GetUserDocumentsPath();
    void ShowInExplorer(string path);
    bool IsWindowsPlatform { get; }
}

// WindowsPlatformServices.cs
public class WindowsPlatformServices : IPlatformServices
{
    public string GetGameInstallPath()
    {
        // Registry-based detection for Windows
        return Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Maxis\\The Sims 4", "Install Dir", null) as string;
    }
    
    public string GetUserDocumentsPath()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
    
    public void ShowInExplorer(string path)
    {
        Process.Start("explorer.exe", $"/select,\"{path}\"");
    }
    
    public bool IsWindowsPlatform => true;
}

// CrossPlatformServices.cs
public class CrossPlatformServices : IPlatformServices
{
    public string GetGameInstallPath()
    {
        // Cross-platform detection using common paths
        var possiblePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EA Games", "The Sims 4"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "EA Games", "The Sims 4"),
            // Add Steam, Origin paths for Linux/Mac
        };
        
        return possiblePaths.FirstOrDefault(Directory.Exists);
    }
    
    public string GetUserDocumentsPath()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
    
    public void ShowInExplorer(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start("explorer.exe", $"/select,\"{path}\"");
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", $"-R \"{path}\"");
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Process.Start("xdg-open", Path.GetDirectoryName(path));
    }
    
    public bool IsWindowsPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
}
```

#### 3.2 Registry Abstraction
```csharp
// IRegistryService.cs
public interface IRegistryService
{
    string GetString(string keyPath, string valueName, string defaultValue = null);
    void SetString(string keyPath, string valueName, string value);
}

// WindowsRegistryService.cs - Windows implementation
// FileRegistryService.cs - Cross-platform file-based implementation
```

### Phase 4: Configuration System Migration (Week 7)

#### 4.1 Replace System.Configuration
```csharp
// New configuration system using Microsoft.Extensions.Configuration
public class AppConfiguration
{
    private readonly IConfiguration _configuration;
    
    public AppConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<AppConfiguration>();
        
        _configuration = builder.Build();
    }
    
    public string TS4Path 
    { 
        get => _configuration["GamePaths:TS4Path"];
        set => SetValue("GamePaths:TS4Path", value);
    }
    
    public string TS4UserPath 
    { 
        get => _configuration["GamePaths:TS4UserPath"];
        set => SetValue("GamePaths:TS4UserPath", value);
    }
}
```

#### 4.2 Convert App.config to appsettings.json
```json
{
  "GamePaths": {
    "TS4Path": "",
    "TS4UserPath": ""
  },
  "UI": {
    "MRUListSize": 4,
    "PersistentHeight": 600,
    "PersistentWidth": 800
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Phase 5: Native Dependency Handling (Week 8)

#### 5.1 DDS Compression Library Strategy
```csharp
// IDdsCompression.cs
public interface IDdsCompression
{
    byte[] CompressImage(byte[] rgba, int width, int height, DdsFormat format);
    bool IsSupported { get; }
}

// NativeDdsCompression.cs - Windows native implementation
// ManagedDdsCompression.cs - Pure C# fallback (or use existing library)
```

**Options for DDS Compression**:
1. **Wrapper approach**: Keep native DLLs for Windows, provide managed fallback
2. **Replace with managed library**: Use existing .NET DDS libraries
3. **Conditional compilation**: Windows-only features

#### 5.2 P/Invoke Window Management
```csharp
// IWindowManager.cs
public interface IWindowManager
{
    void BringToFront(IntPtr windowHandle);
    void SetForeground(IntPtr windowHandle);
}

// WindowsWindowManager.cs - P/Invoke implementation
// CrossPlatformWindowManager.cs - Limited cross-platform implementation
```

### Phase 6: GUI Application Migration (Weeks 9-10)

#### 6.1 Windows Forms Project Updates
- Update to `net8.0-windows` target framework
- Add required NuGet packages
- Test Windows Forms compatibility

#### 6.2 Consider Alternative UI Strategy
**Option A: Keep Windows Forms (Windows-only)**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <OutputType>WinExe</OutputType>
  </PropertyGroup>
</Project>
```

**Option B: Create Cross-Platform CLI Tools**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>
```

**Option C: Web-Based UI (Future)**
- Create ASP.NET Core web application
- Expose functionality via web interface
- Cross-platform by design

### Phase 7: Testing and Validation (Weeks 11-12)

#### 7.1 Automated Testing Strategy
```csharp
// Create comprehensive test suite
[Test]
public void TestPackageLoading()
{
    var package = Package.OpenPackage("test.package");
    Assert.IsNotNull(package);
}

[Test]
public void TestCrossPlatformPaths()
{
    var service = new CrossPlatformServices();
    var documentsPath = service.GetUserDocumentsPath();
    Assert.IsTrue(Directory.Exists(documentsPath));
}
```

#### 7.2 Platform Testing Matrix
| Feature | Windows | Linux | macOS |
|---------|---------|-------|-------|
| Package Reading | ✅ | ✅ | ✅ |
| Package Writing | ✅ | ✅ | ✅ |
| DDS Compression | ✅ | ⚠️ | ⚠️ |
| GUI Applications | ✅ | ❌ | ❌ |
| Game Path Detection | ✅ | ⚠️ | ⚠️ |

## Implementation Plan

### Directory Structure
```
Sims4Tools/
├── src/
│   ├── Core/                    # Platform-agnostic core libraries
│   │   ├── Sims4Tools.Core/
│   │   ├── Sims4Tools.Interfaces/
│   │   └── Sims4Tools.Package/
│   ├── Platform/                # Platform-specific implementations
│   │   ├── Sims4Tools.Platform.Abstractions/
│   │   ├── Sims4Tools.Platform.Windows/
│   │   └── Sims4Tools.Platform.CrossPlatform/
│   ├── Tools/                   # Command-line tools
│   │   ├── Sims4Tools.CLI/
│   │   └── Sims4Tools.PackageEditor/
│   └── Desktop/                 # Desktop applications
│       ├── Sims4Tools.Desktop.WinForms/
│       └── Sims4Tools.Desktop.Avalonia/  # Future cross-platform GUI
├── tests/
├── docs/
└── build/
```

### Migration Checklist

#### Pre-Migration
- [ ] Create feature branch: `net8-migration`
- [ ] Backup current working version
- [ ] Document current functionality
- [ ] Set up CI/CD for new target

#### Phase 1: Foundation
- [ ] Convert project files to SDK-style
- [ ] Create Directory.Build.props
- [ ] Update target framework references
- [ ] Add required NuGet packages

#### Phase 2: Core Libraries
- [ ] Migrate CS System Classes
- [ ] Migrate s4pi.Interfaces
- [ ] Migrate s4pi.Package
- [ ] Migrate all wrapper libraries
- [ ] Update inter-project references

#### Phase 3: Platform Abstraction
- [ ] Create IPlatformServices interface
- [ ] Implement WindowsPlatformServices
- [ ] Implement CrossPlatformServices
- [ ] Create IRegistryService abstraction
- [ ] Update all platform-specific code

#### Phase 4: Configuration
- [ ] Replace System.Configuration usage
- [ ] Convert App.config to appsettings.json
- [ ] Update settings management
- [ ] Test configuration persistence

#### Phase 5: Native Dependencies
- [ ] Abstract DDS compression
- [ ] Handle P/Invoke gracefully
- [ ] Create fallback implementations
- [ ] Test on target platforms

#### Phase 6: GUI Applications
- [ ] Update Windows Forms projects
- [ ] Test GUI functionality
- [ ] Consider cross-platform alternatives
- [ ] Document platform limitations

#### Phase 7: Testing
- [ ] Create comprehensive test suite
- [ ] Test on Windows 10/11
- [ ] Test on Linux (Ubuntu/RHEL)
- [ ] Test on macOS (if applicable)
- [ ] Performance testing
- [ ] User acceptance testing

### Risk Mitigation

#### High-Risk Items
1. **Native DLL dependencies** - Create managed fallbacks
2. **Windows-specific APIs** - Abstract behind interfaces
3. **Complex Windows Forms** - Thorough testing required

#### Rollback Strategy
- Maintain parallel branches during migration
- Keep .NET Framework version functional
- Provide migration path for users

### Success Criteria

#### Functional Requirements
- [ ] All core library functionality preserved
- [ ] Package reading/writing works cross-platform
- [ ] Windows GUI applications function correctly
- [ ] Game detection works on Windows
- [ ] Settings persistence works cross-platform

#### Non-Functional Requirements
- [ ] Performance equivalent or better
- [ ] Memory usage optimized
- [ ] Startup time improved
- [ ] Cross-platform compatibility (core libraries)
- [ ] Maintainable codebase

### Timeline Summary

| Phase | Duration | Key Deliverables |
|-------|----------|------------------|
| 1 | 2 weeks | Project structure modernization |
| 2 | 2 weeks | Core libraries migrated |
| 3 | 2 weeks | Platform abstraction complete |
| 4 | 1 week | Configuration system migrated |
| 5 | 1 week | Native dependencies handled |
| 6 | 2 weeks | GUI applications working |
| 7 | 2 weeks | Testing and validation complete |

**Total Estimated Duration: 12 weeks**

### Future Enhancements

#### Post-Migration Opportunities
1. **Cross-platform GUI** using Avalonia UI
2. **Web-based interface** using Blazor
3. **Package manager CLI** for automation
4. **Plugin system** for extensibility
5. **Performance optimizations** using .NET 8 features

#### Long-term Platform Strategy
- **Windows**: Full feature support with native optimizations
- **Linux**: Core functionality + CLI tools
- **macOS**: Core functionality + CLI tools
- **Web**: Browser-based package editor

This migration plan provides a structured approach to modernizing Sims4Tools while maintaining functionality and adding cross-platform capabilities where feasible.
