using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Sims4Tools.Platform.Abstractions;

namespace Sims4Tools.Platform.Windows
{
    /// <summary>
    /// Windows-specific implementation of platform services.
    /// </summary>
    public class WindowsPlatformServices : IPlatformServices
    {
        public string? GetGameInstallPath()
        {
            try
            {
                // Try the 64-bit registry first
                var path = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Maxis\The Sims 4", "Install Dir", null) as string;
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    return path;
                }

                // Try the 32-bit registry
                path = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Maxis\The Sims 4", "Install Dir", null) as string;
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    return path;
                }

                // Try common installation paths
                var commonPaths = new[]
                {
                    @"C:\Program Files (x86)\EA Games\The Sims 4",
                    @"C:\Program Files\EA Games\The Sims 4",
                    @"C:\Program Files (x86)\Origin Games\The Sims 4",
                    @"C:\Program Files\Origin Games\The Sims 4"
                };

                foreach (var commonPath in commonPaths)
                {
                    if (Directory.Exists(commonPath))
                    {
                        return commonPath;
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string GetUserDocumentsPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        public string? GetSims4UserPath()
        {
            try
            {
                var documentsPath = GetUserDocumentsPath();
                var sims4Path = Path.Combine(documentsPath, "Electronic Arts", "The Sims 4");
                
                if (Directory.Exists(sims4Path))
                {
                    return sims4Path;
                }

                return null;
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
                if (File.Exists(path))
                {
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
                else if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", $"\"{path}\"");
                }
            }
            catch (Exception)
            {
                // Silently fail - explorer might not be available
            }
        }

        public bool IsWindowsPlatform => true;

        public bool SupportsWindowsForms => true;

        public char PathSeparator => '\\';

        public string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            return Path.GetFullPath(path.Replace('/', '\\'));
        }
    }

    /// <summary>
    /// Windows registry service implementation.
    /// </summary>
    public class WindowsRegistryService : IRegistryService
    {
        public string? GetString(string keyPath, string valueName, string? defaultValue = null)
        {
            try
            {
                // Handle HKEY_LOCAL_MACHINE paths
                if (keyPath.StartsWith("HKEY_LOCAL_MACHINE\\"))
                {
                    var regPath = keyPath.Substring("HKEY_LOCAL_MACHINE\\".Length);
                    using var key = Registry.LocalMachine.OpenSubKey(regPath);
                    return key?.GetValue(valueName) as string ?? defaultValue;
                }

                // Handle HKEY_CURRENT_USER paths
                if (keyPath.StartsWith("HKEY_CURRENT_USER\\"))
                {
                    var regPath = keyPath.Substring("HKEY_CURRENT_USER\\".Length);
                    using var key = Registry.CurrentUser.OpenSubKey(regPath);
                    return key?.GetValue(valueName) as string ?? defaultValue;
                }

                // Use Registry.GetValue for full paths
                return Registry.GetValue(keyPath, valueName, defaultValue) as string;
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
                // Handle HKEY_CURRENT_USER paths (safer for user settings)
                if (keyPath.StartsWith("HKEY_CURRENT_USER\\"))
                {
                    var regPath = keyPath.Substring("HKEY_CURRENT_USER\\".Length);
                    using var key = Registry.CurrentUser.CreateSubKey(regPath);
                    key?.SetValue(valueName, value);
                }
                else
                {
                    Registry.SetValue(keyPath, valueName, value);
                }
            }
            catch (Exception)
            {
                // Silently fail - registry might not be writable
            }
        }

        public bool SupportsPersistentStorage => true;
    }

    /// <summary>
    /// Windows window manager using P/Invoke.
    /// </summary>
    public class WindowsWindowManager : IWindowManager
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        public bool BringToFront(IntPtr windowHandle)
        {
            try
            {
                if (windowHandle == IntPtr.Zero)
                    return false;

                if (IsIconic(windowHandle))
                {
                    ShowWindowAsync(windowHandle, SW_RESTORE);
                }

                return BringWindowToTop(windowHandle) && SetForegroundWindow(windowHandle);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool SetForeground(IntPtr windowHandle)
        {
            try
            {
                if (windowHandle == IntPtr.Zero)
                    return false;

                return SetForegroundWindow(windowHandle);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool IsSupported => true;
    }

    /// <summary>
    /// Windows native DDS compression service using native DLLs.
    /// </summary>
    public class NativeDdsCompressionService : IDdsCompressionService
    {
        private static readonly bool _is64Bit = Marshal.SizeOf(IntPtr.Zero) == 8;
        private readonly bool _isSupported;

        public NativeDdsCompressionService()
        {
            // Check if native DLLs are available
            try
            {
                var dllName = _is64Bit ? "squishinterface_x64.dll" : "squishinterface_Win32.dll";
                var dllPath = Path.Combine(AppContext.BaseDirectory, dllName);
                _isSupported = File.Exists(dllPath);
            }
            catch
            {
                _isSupported = false;
            }
        }

        public byte[] CompressImage(byte[] rgba, int width, int height, DdsCompressionFormat format)
        {
            if (!_isSupported)
                throw new NotSupportedException("Native DDS compression is not available on this system.");

            // Implementation would call into native DLL
            // This is a placeholder - actual implementation would use P/Invoke
            throw new NotImplementedException("Native DDS compression implementation pending.");
        }

        public bool IsSupported => _isSupported;

        public DdsCompressionFormat[] SupportedFormats => _isSupported 
            ? new[] { DdsCompressionFormat.Dxt1, DdsCompressionFormat.Dxt3, DdsCompressionFormat.Dxt5 }
            : new[] { DdsCompressionFormat.None };
    }
}
