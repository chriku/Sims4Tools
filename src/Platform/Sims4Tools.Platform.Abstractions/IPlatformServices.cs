using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Sims4Tools.Platform.Abstractions
{
    /// <summary>
    /// Provides platform-specific services for game detection, file operations, and system integration.
    /// </summary>
    public interface IPlatformServices
    {
        /// <summary>
        /// Gets the installation path for The Sims 4 game.
        /// </summary>
        /// <returns>The game installation path, or null if not found.</returns>
        string? GetGameInstallPath();

        /// <summary>
        /// Gets the user's Documents folder path where Sims 4 saves are stored.
        /// </summary>
        /// <returns>The user documents path.</returns>
        string GetUserDocumentsPath();

        /// <summary>
        /// Gets the user's Sims 4 folder in Documents.
        /// </summary>
        /// <returns>The Sims 4 user folder path, or null if not found.</returns>
        string? GetSims4UserPath();

        /// <summary>
        /// Opens the file explorer to show the specified path.
        /// </summary>
        /// <param name="path">The path to show in the file explorer.</param>
        void ShowInExplorer(string path);

        /// <summary>
        /// Gets a value indicating whether the current platform is Windows.
        /// </summary>
        bool IsWindowsPlatform { get; }

        /// <summary>
        /// Gets a value indicating whether the current platform supports Windows Forms.
        /// </summary>
        bool SupportsWindowsForms { get; }

        /// <summary>
        /// Gets the platform-specific path separator.
        /// </summary>
        char PathSeparator { get; }

        /// <summary>
        /// Normalizes a file path for the current platform.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        string NormalizePath(string path);
    }

    /// <summary>
    /// Provides registry access abstraction for cross-platform compatibility.
    /// </summary>
    public interface IRegistryService
    {
        /// <summary>
        /// Gets a string value from the registry or configuration.
        /// </summary>
        /// <param name="keyPath">The registry key path or configuration key.</param>
        /// <param name="valueName">The value name.</param>
        /// <param name="defaultValue">The default value if not found.</param>
        /// <returns>The value or default if not found.</returns>
        string? GetString(string keyPath, string valueName, string? defaultValue = null);

        /// <summary>
        /// Sets a string value in the registry or configuration.
        /// </summary>
        /// <param name="keyPath">The registry key path or configuration key.</param>
        /// <param name="valueName">The value name.</param>
        /// <param name="value">The value to set.</param>
        void SetString(string keyPath, string valueName, string value);

        /// <summary>
        /// Gets a value indicating whether the service supports persistent storage.
        /// </summary>
        bool SupportsPersistentStorage { get; }
    }

    /// <summary>
    /// Provides DDS image compression services.
    /// </summary>
    public interface IDdsCompressionService
    {
        /// <summary>
        /// Compresses an image to DDS format.
        /// </summary>
        /// <param name="rgba">The RGBA image data.</param>
        /// <param name="width">The image width.</param>
        /// <param name="height">The image height.</param>
        /// <param name="format">The DDS compression format.</param>
        /// <returns>The compressed DDS data.</returns>
        byte[] CompressImage(byte[] rgba, int width, int height, DdsCompressionFormat format);

        /// <summary>
        /// Gets a value indicating whether DDS compression is supported on this platform.
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// Gets the available compression formats on this platform.
        /// </summary>
        DdsCompressionFormat[] SupportedFormats { get; }
    }

    /// <summary>
    /// DDS compression format options.
    /// </summary>
    public enum DdsCompressionFormat
    {
        /// <summary>No compression (fallback)</summary>
        None,
        /// <summary>DXT1 compression</summary>
        Dxt1,
        /// <summary>DXT3 compression</summary>
        Dxt3,
        /// <summary>DXT5 compression</summary>
        Dxt5
    }

    /// <summary>
    /// Provides window management services for GUI applications.
    /// </summary>
    public interface IWindowManager
    {
        /// <summary>
        /// Brings the specified window to the foreground.
        /// </summary>
        /// <param name="windowHandle">The window handle.</param>
        /// <returns>True if successful, false otherwise.</returns>
        bool BringToFront(IntPtr windowHandle);

        /// <summary>
        /// Sets the specified window as the foreground window.
        /// </summary>
        /// <param name="windowHandle">The window handle.</param>
        /// <returns>True if successful, false otherwise.</returns>
        bool SetForeground(IntPtr windowHandle);

        /// <summary>
        /// Gets a value indicating whether window management is supported on this platform.
        /// </summary>
        bool IsSupported { get; }
    }
}
