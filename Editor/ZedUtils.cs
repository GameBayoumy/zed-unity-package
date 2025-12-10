using System;
using System.IO;
using UnityEngine;

namespace Zed.Unity.Editor
{
    /// <summary>
    /// Platform-specific utilities for finding and validating Zed editor installation.
    /// Supports Linux, macOS, and Windows.
    /// </summary>
    public static class ZedUtils
    {
        // Common Zed executable names
        private const string ZedExecutableLinux = "zed";
        private const string ZedExecutableMac = "zed";
        private const string ZedExecutableWindows = "zed.exe";

        /// <summary>
        /// Get the list of possible Zed installation paths based on the current platform.
        /// </summary>
        public static string[] GetPossibleZedPaths()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.LinuxEditor:
                    return GetLinuxPaths();
                    
                case RuntimePlatform.OSXEditor:
                    return GetMacPaths();
                    
                case RuntimePlatform.WindowsEditor:
                    return GetWindowsPaths();
                    
                default:
                    return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Find the Zed executable on the current system.
        /// </summary>
        public static string FindZedExecutable()
        {
            string[] possiblePaths = GetPossibleZedPaths();
            
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // Try to find via PATH environment variable
            // Check multiple possible executable names
            string[] execNames = { "zed", "zeditor", "zed-editor" };
            foreach (string execName in execNames)
            {
                string pathFromEnv = FindInPath(execName);
                if (!string.IsNullOrEmpty(pathFromEnv))
                {
                    return pathFromEnv;
                }
            }

            return null;
        }

        /// <summary>
        /// Check if a given path is a valid Zed executable.
        /// </summary>
        public static bool IsValidZedPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (!File.Exists(path))
                return false;

            string fileName = Path.GetFileName(path).ToLowerInvariant();
            return fileName == "zed" || fileName == "zed.exe" || fileName == "zed-editor" || fileName == "zeditor";
        }

        /// <summary>
        /// Get the platform-specific Zed executable name.
        /// </summary>
        public static string GetZedExecutableName()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    return ZedExecutableWindows;
                case RuntimePlatform.OSXEditor:
                    return ZedExecutableMac;
                case RuntimePlatform.LinuxEditor:
                default:
                    return ZedExecutableLinux;
            }
        }

        /// <summary>
        /// Get Linux-specific Zed installation paths.
        /// </summary>
        private static string[] GetLinuxPaths()
        {
            string home = Environment.GetEnvironmentVariable("HOME") ?? "";
            
            return new[]
            {
                // Standard locations
                "/usr/bin/zed",
                "/usr/bin/zeditor",
                "/usr/local/bin/zed",
                "/usr/local/bin/zeditor",
                Path.Combine(home, ".local/bin/zed"),
                Path.Combine(home, ".local/bin/zeditor"),
                
                // Cargo installation
                Path.Combine(home, ".cargo/bin/zed"),
                Path.Combine(home, ".cargo/bin/zeditor"),
                
                // Flatpak
                "/var/lib/flatpak/exports/bin/dev.zed.Zed",
                Path.Combine(home, ".local/share/flatpak/exports/bin/dev.zed.Zed"),
                
                // Snap
                "/snap/bin/zed",
                "/snap/bin/zeditor",
                "/snap/bin/zed-editor",
                
                // AppImage in common locations
                Path.Combine(home, "Applications/zed.appimage"),
                Path.Combine(home, "Applications/Zed.AppImage"),
                Path.Combine(home, ".local/bin/zed.appimage"),
                
                // AUR / Arch Linux
                "/usr/bin/zed-editor",
                
                // Nix
                Path.Combine(home, ".nix-profile/bin/zed"),
                "/run/current-system/sw/bin/zed"
            };
        }

        /// <summary>
        /// Get macOS-specific Zed installation paths.
        /// </summary>
        private static string[] GetMacPaths()
        {
            string home = Environment.GetEnvironmentVariable("HOME") ?? "";
            
            return new[]
            {
                // Application bundle
                "/Applications/Zed.app/Contents/MacOS/zed",
                Path.Combine(home, "Applications/Zed.app/Contents/MacOS/zed"),
                
                // Homebrew (Intel)
                "/usr/local/bin/zed",
                
                // Homebrew (Apple Silicon)
                "/opt/homebrew/bin/zed",
                
                // CLI symlink
                "/usr/local/bin/zed",
                Path.Combine(home, ".local/bin/zed"),
                
                // Cargo installation
                Path.Combine(home, ".cargo/bin/zed")
            };
        }

        /// <summary>
        /// Get Windows-specific Zed installation paths.
        /// </summary>
        private static string[] GetWindowsPaths()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            return new[]
            {
                // Standard installation paths (anticipating Windows release)
                Path.Combine(localAppData, "Programs", "Zed", "zed.exe"),
                Path.Combine(localAppData, "Zed", "zed.exe"),
                Path.Combine(programFiles, "Zed", "zed.exe"),
                Path.Combine(programFilesX86, "Zed", "zed.exe"),
                
                // Scoop
                Path.Combine(userProfile, "scoop", "apps", "zed", "current", "zed.exe"),
                Path.Combine(userProfile, "scoop", "shims", "zed.exe"),
                
                // Chocolatey
                @"C:\ProgramData\chocolatey\bin\zed.exe",
                
                // WinGet typical location
                Path.Combine(localAppData, "Microsoft", "WinGet", "Packages", "Zed.Zed_*", "zed.exe"),
                
                // Cargo installation
                Path.Combine(userProfile, ".cargo", "bin", "zed.exe"),
                
                // Portable / manual installation
                Path.Combine(userProfile, "Zed", "zed.exe"),
                @"C:\Zed\zed.exe"
            };
        }

        /// <summary>
        /// Search for an executable in the system PATH.
        /// </summary>
        private static string FindInPath(string executableName)
        {
            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv))
                return null;

            char separator = Application.platform == RuntimePlatform.WindowsEditor ? ';' : ':';
            string[] paths = pathEnv.Split(separator);

            foreach (string dir in paths)
            {
                if (string.IsNullOrEmpty(dir))
                    continue;

                string fullPath = Path.Combine(dir, executableName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        /// <summary>
        /// Get the project root path (parent of Assets folder).
        /// </summary>
        public static string GetProjectPath()
        {
            return Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        }

        /// <summary>
        /// Convert a Unity asset path to an absolute file system path.
        /// </summary>
        public static string AssetPathToFullPath(string assetPath)
        {
            string projectPath = GetProjectPath();
            return Path.GetFullPath(Path.Combine(projectPath, assetPath));
        }

        /// <summary>
        /// Convert an absolute path to a Unity asset path.
        /// </summary>
        public static string FullPathToAssetPath(string fullPath)
        {
            string projectPath = GetProjectPath();
            if (fullPath.StartsWith(projectPath))
            {
                string relativePath = fullPath.Substring(projectPath.Length);
                if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()) ||
                    relativePath.StartsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    relativePath = relativePath.Substring(1);
                }
                return relativePath.Replace('\\', '/');
            }
            return fullPath;
        }

        /// <summary>
        /// Normalize a file path for the current platform.
        /// </summary>
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            path = Path.GetFullPath(path);
            
            // Normalize directory separators
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                path = path.Replace('/', '\\');
            }
            else
            {
                path = path.Replace('\\', '/');
            }

            return path;
        }
    }
}
