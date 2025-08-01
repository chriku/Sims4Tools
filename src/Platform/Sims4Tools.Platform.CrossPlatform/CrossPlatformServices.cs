using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Sims4Tools.Platform.Abstractions;

namespace Sims4Tools.Platform.CrossPlatform
{
    /// <summary>
    /// Cross-platform implementation of platform services.
    /// </summary>
    public class CrossPlatformServices : IPlatformServices
    {
        public string? GetGameInstallPath()
        {
            // Common installation paths across platforms
            var possiblePaths = new[]
            {
                // Windows paths
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EA Games", "The Sims 4"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "EA Games", "The Sims 4"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Origin Games", "The Sims 4"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Origin Games", "The Sims 4"),
                
                // Linux paths (Steam, native)
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam", "steamapps", "common", "The Sims 4"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Steam", "steamapps", "common", "The Sims 4"),
                "/opt/EA Games/The Sims 4",
                
                // macOS paths
                "/Applications/The Sims 4.app",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Steam", "steamapps", "common", "The Sims 4")
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        return path;
                    }
                }
                catch (Exception)
                {
                    // Continue checking other paths
                }
            }

            return null;
        }

        public string GetUserDocumentsPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents");
            }
            else
            {
                // Linux and others
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents");
            }
        }

        public string? GetSims4UserPath()
        {
            try
            {
                var documentsPath = GetUserDocumentsPath();
                string sims4Path;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    sims4Path = Path.Combine(documentsPath, "Electronic Arts", "The Sims 4");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    sims4Path = Path.Combine(documentsPath, "Electronic Arts", "The Sims 4");
                }
                else
                {
                    // Linux - may vary by distribution and Wine setup
                    var possiblePaths = new[]
                    {
                        Path.Combine(documentsPath, "Electronic Arts", "The Sims 4"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wine", "drive_c", "users", Environment.UserName, "Documents", "Electronic Arts", "The Sims 4"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "The Sims 4")
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (Directory.Exists(path))
                        {
                            return path;
                        }
                    }
                    return null;
                }

                return Directory.Exists(sims4Path) ? sims4Path : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void ShowInExplorer(string path)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (File.Exists(path))
                        Process.Start("explorer.exe", $"/select,\"{path}\"");
                    else if (Directory.Exists(path))
                        Process.Start("explorer.exe", $"\"{path}\"");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    if (File.Exists(path))
                        Process.Start("open", $"-R \"{path}\"");
                    else if (Directory.Exists(path))
                        Process.Start("open", $"\"{path}\"");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var directoryPath = File.Exists(path) ? Path.GetDirectoryName(path) : path;
                    if (Directory.Exists(directoryPath))
                    {
                        // Try common Linux file managers
                        var fileManagers = new[] { "nautilus", "dolphin", "thunar", "pcmanfm", "caja" };
                        foreach (var fm in fileManagers)
                        {
                            try
                            {
                                Process.Start(fm, $"\"{directoryPath}\"");
                                return;
                            }
                            catch
                            {
                                // Try next file manager
                            }
                        }
                        
                        // Fallback to xdg-open
                        Process.Start("xdg-open", $"\"{directoryPath}\"");
                    }
                }
            }
            catch (Exception)
            {
                // Silently fail
            }
        }

        public bool IsWindowsPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public bool SupportsWindowsForms => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public char PathSeparator => Path.DirectorySeparatorChar;

        public string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return path;
            }
        }
    }

    /// <summary>
    /// File-based registry service for cross-platform settings storage.
    /// </summary>
    public class FileRegistryService : IRegistryService
    {
        private readonly string _configPath;
        private readonly IConfiguration _configuration;

        public FileRegistryService()
        {
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Sims4Tools",
                "settings.json");

            var builder = new ConfigurationBuilder()
                .AddJsonFile(_configPath, optional: true, reloadOnChange: true);
            
            _configuration = builder.Build();
            
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
        }

        public string? GetString(string keyPath, string valueName, string? defaultValue = null)
        {
            try
            {
                var configKey = ConvertRegistryKeyToConfigKey(keyPath, valueName);
                return _configuration[configKey] ?? defaultValue;
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public void SetString(string keyPath, string valueName, string value)
        {
            try
            {
                // For simplicity, we'll write to a simple JSON structure
                // In a real implementation, you might want to use a more sophisticated approach
                var configKey = ConvertRegistryKeyToConfigKey(keyPath, valueName);
                
                // This is a simplified implementation
                // A full implementation would properly update the JSON file
                File.WriteAllText(_configPath, $"{{\"{configKey}\": \"{value}\"}}");
            }
            catch (Exception)
            {
                // Silently fail
            }
        }

        public bool SupportsPersistentStorage => true;

        private static string ConvertRegistryKeyToConfigKey(string keyPath, string valueName)
        {
            // Convert Windows registry path to a configuration key
            var cleanPath = keyPath
                .Replace("HKEY_LOCAL_MACHINE\\", "")
                .Replace("HKEY_CURRENT_USER\\", "")
                .Replace("\\", ":");
            
            return $"{cleanPath}:{valueName}";
        }
    }

    /// <summary>
    /// Cross-platform window manager with limited functionality.
    /// </summary>
    public class CrossPlatformWindowManager : IWindowManager
    {
        public bool BringToFront(IntPtr windowHandle)
        {
            // Limited cross-platform window management
            // Most window management requires platform-specific APIs
            return false;
        }

        public bool SetForeground(IntPtr windowHandle)
        {
            // Limited cross-platform window management
            return false;
        }

        public bool IsSupported => false;
    }

    /// <summary>
    /// Managed DDS compression service using pure C# implementation.
    /// </summary>
    public class ManagedDdsCompressionService : IDdsCompressionService
    {
        public byte[] CompressImage(byte[] rgba, int width, int height, DdsCompressionFormat format)
        {
            // This would implement DDS compression in pure C#
            // For now, return uncompressed data as a fallback
            if (format == DdsCompressionFormat.None)
            {
                return rgba;
            }

            // TODO: Implement managed DDS compression
            // Could use existing libraries like BCnEncoder.Net or similar
            throw new NotImplementedException("Managed DDS compression implementation pending. Consider using BCnEncoder.Net or similar library.");
        }

        public bool IsSupported => true;

        public DdsCompressionFormat[] SupportedFormats => new[] { DdsCompressionFormat.None };
    }
}
