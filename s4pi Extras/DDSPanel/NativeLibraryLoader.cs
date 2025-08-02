using System;
using System.Runtime.InteropServices;

namespace S4PI.Extras.DDSPanel
{
    /// <summary>
    /// Cross-platform native library loader for DDS compression functionality
    /// </summary>
    public static class NativeLibraryLoader
    {
        private static bool _initialized = false;
        private static bool _libraryAvailable = false;

        /// <summary>
        /// Gets whether the native library is available on this platform
        /// </summary>
        public static bool IsLibraryAvailable
        {
            get
            {
                if (!_initialized)
                {
                    Initialize();
                }
                return _libraryAvailable;
            }
        }

        private static void Initialize()
        {
            try
            {
                // Check if we're on Windows and the appropriate DLLs exist
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string dllName = Environment.Is64BitProcess ? "squishinterface_x64.dll" : "squishinterface_Win32.dll";
                    
                    // Try to load the library
                    IntPtr handle = LoadLibrary(dllName);
                    _libraryAvailable = handle != IntPtr.Zero;
                    
                    if (handle != IntPtr.Zero)
                    {
                        FreeLibrary(handle);
                    }
                }
                else
                {
                    // On non-Windows platforms, the native library is not available
                    _libraryAvailable = false;
                }
            }
            catch
            {
                _libraryAvailable = false;
            }
            finally
            {
                _initialized = true;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);
    }
}
