using UnityEditor;
using UnityEngine;

namespace Zed.Unity.Editor
{
    /// <summary>
    /// Configuration storage for Zed Unity integration.
    /// Uses EditorPrefs for persistent storage.
    /// </summary>
    public static class ZedConfig
    {
        // EditorPrefs keys
        private const string KeyZedPath = "ZedUnity_ZedPath";
        private const string KeyOpenInNewWindow = "ZedUnity_OpenInNewWindow";
        private const string KeyEnableFileSync = "ZedUnity_EnableFileSync";
        private const string KeyFileSyncInterval = "ZedUnity_FileSyncInterval";
        private const string KeyEnableLogging = "ZedUnity_EnableLogging";
        private const string KeyGenerateSlnFile = "ZedUnity_GenerateSlnFile";
        private const string KeyGenerateCsprojFiles = "ZedUnity_GenerateCsprojFiles";
        private const string KeyIncludePackages = "ZedUnity_IncludePackages";
        private const string KeyUseRoslynAnalyzers = "ZedUnity_UseRoslynAnalyzers";

        // Default values
        private const bool DefaultOpenInNewWindow = false;
        private const bool DefaultEnableFileSync = true;
        private const float DefaultFileSyncInterval = 1.0f;
        private const bool DefaultEnableLogging = false;
        private const bool DefaultGenerateSlnFile = true;
        private const bool DefaultGenerateCsprojFiles = true;
        private const bool DefaultIncludePackages = true;
        private const bool DefaultUseRoslynAnalyzers = true;

        /// <summary>
        /// Path to the Zed executable.
        /// </summary>
        public static string ZedPath
        {
            get => EditorPrefs.GetString(KeyZedPath, "");
            set => EditorPrefs.SetString(KeyZedPath, value);
        }

        /// <summary>
        /// Whether to open files in a new Zed window.
        /// </summary>
        public static bool OpenInNewWindow
        {
            get => EditorPrefs.GetBool(KeyOpenInNewWindow, DefaultOpenInNewWindow);
            set => EditorPrefs.SetBool(KeyOpenInNewWindow, value);
        }

        /// <summary>
        /// Whether to enable bidirectional file sync.
        /// </summary>
        public static bool EnableFileSync
        {
            get => EditorPrefs.GetBool(KeyEnableFileSync, DefaultEnableFileSync);
            set => EditorPrefs.SetBool(KeyEnableFileSync, value);
        }

        /// <summary>
        /// Interval in seconds for file sync polling.
        /// </summary>
        public static float FileSyncInterval
        {
            get => EditorPrefs.GetFloat(KeyFileSyncInterval, DefaultFileSyncInterval);
            set => EditorPrefs.SetFloat(KeyFileSyncInterval, Mathf.Max(0.1f, value));
        }

        /// <summary>
        /// Whether to enable debug logging.
        /// </summary>
        public static bool EnableLogging
        {
            get => EditorPrefs.GetBool(KeyEnableLogging, DefaultEnableLogging);
            set => EditorPrefs.SetBool(KeyEnableLogging, value);
        }

        /// <summary>
        /// Whether to generate the .sln solution file.
        /// </summary>
        public static bool GenerateSlnFile
        {
            get => EditorPrefs.GetBool(KeyGenerateSlnFile, DefaultGenerateSlnFile);
            set => EditorPrefs.SetBool(KeyGenerateSlnFile, value);
        }

        /// <summary>
        /// Whether to generate .csproj project files.
        /// </summary>
        public static bool GenerateCsprojFiles
        {
            get => EditorPrefs.GetBool(KeyGenerateCsprojFiles, DefaultGenerateCsprojFiles);
            set => EditorPrefs.SetBool(KeyGenerateCsprojFiles, value);
        }

        /// <summary>
        /// Whether to include Unity packages in project generation.
        /// </summary>
        public static bool IncludePackages
        {
            get => EditorPrefs.GetBool(KeyIncludePackages, DefaultIncludePackages);
            set => EditorPrefs.SetBool(KeyIncludePackages, value);
        }

        /// <summary>
        /// Whether to include Roslyn analyzers in project files.
        /// </summary>
        public static bool UseRoslynAnalyzers
        {
            get => EditorPrefs.GetBool(KeyUseRoslynAnalyzers, DefaultUseRoslynAnalyzers);
            set => EditorPrefs.SetBool(KeyUseRoslynAnalyzers, value);
        }

        /// <summary>
        /// Reset all settings to defaults.
        /// </summary>
        public static void ResetToDefaults()
        {
            EditorPrefs.DeleteKey(KeyZedPath);
            EditorPrefs.DeleteKey(KeyOpenInNewWindow);
            EditorPrefs.DeleteKey(KeyEnableFileSync);
            EditorPrefs.DeleteKey(KeyFileSyncInterval);
            EditorPrefs.DeleteKey(KeyEnableLogging);
            EditorPrefs.DeleteKey(KeyGenerateSlnFile);
            EditorPrefs.DeleteKey(KeyGenerateCsprojFiles);
            EditorPrefs.DeleteKey(KeyIncludePackages);
            EditorPrefs.DeleteKey(KeyUseRoslynAnalyzers);
        }
    }
}
