using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zed.Unity.Editor
{
    /// <summary>
    /// Unity preferences UI for Zed Editor integration.
    /// Displayed in Edit > Preferences > External Tools when Zed is selected.
    /// </summary>
    public static class ZedPreferences
    {
        private static GUIStyle _headerStyle;
        private static GUIStyle _boxStyle;
        private static bool _showAdvanced = false;

        /// <summary>
        /// Draw the preferences GUI in the External Tools panel.
        /// </summary>
        public static void DrawPreferencesGUI()
        {
            InitStyles();

            EditorGUILayout.Space(10);

            // Header
            EditorGUILayout.LabelField("Zed Editor Settings", _headerStyle);
            EditorGUILayout.Space(5);

            // Zed Path Section
            DrawZedPathSection();
            
            EditorGUILayout.Space(10);

            // General Settings Section
            DrawGeneralSettingsSection();
            
            EditorGUILayout.Space(10);

            // Project Generation Section
            DrawProjectGenerationSection();
            
            EditorGUILayout.Space(10);

            // File Sync Section
            DrawFileSyncSection();
            
            EditorGUILayout.Space(10);

            // Advanced Section
            DrawAdvancedSection();
            
            EditorGUILayout.Space(10);

            // Actions Section
            DrawActionsSection();
        }

        private static void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14
                };
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle("HelpBox")
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
        }

        private static void DrawZedPathSection()
        {
            EditorGUILayout.LabelField("Zed Executable", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(_boxStyle);
            
            // Current path display
            EditorGUILayout.BeginHorizontal();
            
            string currentPath = ZedConfig.ZedPath;
            if (string.IsNullOrEmpty(currentPath))
            {
                currentPath = ZedUtils.FindZedExecutable() ?? "(Not found - please browse)";
            }
            
            EditorGUI.BeginChangeCheck();
            string newPath = EditorGUILayout.TextField("Path", currentPath);
            if (EditorGUI.EndChangeCheck() && newPath != currentPath)
            {
                ZedConfig.ZedPath = newPath;
            }

            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string selectedPath = EditorUtility.OpenFilePanel(
                    "Select Zed Executable",
                    Path.GetDirectoryName(currentPath) ?? "/usr/bin",
                    Application.platform == RuntimePlatform.WindowsEditor ? "exe" : ""
                );
                
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    ZedConfig.ZedPath = selectedPath;
                }
            }

            if (GUILayout.Button("Auto-Detect", GUILayout.Width(80)))
            {
                string detected = ZedUtils.FindZedExecutable();
                if (!string.IsNullOrEmpty(detected))
                {
                    ZedConfig.ZedPath = detected;
                    Debug.Log($"[Zed Unity] Found Zed at: {detected}");
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Zed Not Found",
                        "Could not automatically detect Zed installation. Please browse to select the executable manually.",
                        "OK"
                    );
                }
            }
            
            EditorGUILayout.EndHorizontal();

            // Validation
            if (!string.IsNullOrEmpty(ZedConfig.ZedPath) && !File.Exists(ZedConfig.ZedPath))
            {
                EditorGUILayout.HelpBox("The specified Zed executable does not exist.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawGeneralSettingsSection()
        {
            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(_boxStyle);

            // Open in new window
            EditorGUI.BeginChangeCheck();
            bool openInNewWindow = EditorGUILayout.Toggle(
                new GUIContent("Open in New Window", "Always open files in a new Zed window instead of reusing existing one"),
                ZedConfig.OpenInNewWindow
            );
            if (EditorGUI.EndChangeCheck())
            {
                ZedConfig.OpenInNewWindow = openInNewWindow;
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawProjectGenerationSection()
        {
            EditorGUILayout.LabelField("Project Generation", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(_boxStyle);

            // Generate .sln file
            EditorGUI.BeginChangeCheck();
            bool generateSln = EditorGUILayout.Toggle(
                new GUIContent("Generate .sln File", "Generate Visual Studio solution file for the project"),
                ZedConfig.GenerateSlnFile
            );
            if (EditorGUI.EndChangeCheck())
            {
                ZedConfig.GenerateSlnFile = generateSln;
            }

            // Generate .csproj files
            EditorGUI.BeginChangeCheck();
            bool generateCsproj = EditorGUILayout.Toggle(
                new GUIContent("Generate .csproj Files", "Generate C# project files for each assembly"),
                ZedConfig.GenerateCsprojFiles
            );
            if (EditorGUI.EndChangeCheck())
            {
                ZedConfig.GenerateCsprojFiles = generateCsproj;
            }

            // Include packages
            EditorGUI.BeginChangeCheck();
            bool includePackages = EditorGUILayout.Toggle(
                new GUIContent("Include Packages", "Include Unity packages in project generation"),
                ZedConfig.IncludePackages
            );
            if (EditorGUI.EndChangeCheck())
            {
                ZedConfig.IncludePackages = includePackages;
            }

            // Use Roslyn Analyzers
            EditorGUI.BeginChangeCheck();
            bool useAnalyzers = EditorGUILayout.Toggle(
                new GUIContent("Use Roslyn Analyzers", "Include Roslyn analyzers in project files for enhanced code analysis"),
                ZedConfig.UseRoslynAnalyzers
            );
            if (EditorGUI.EndChangeCheck())
            {
                ZedConfig.UseRoslynAnalyzers = useAnalyzers;
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawFileSyncSection()
        {
            EditorGUILayout.LabelField("File Synchronization", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(_boxStyle);

            // Enable file sync
            EditorGUI.BeginChangeCheck();
            bool enableFileSync = EditorGUILayout.Toggle(
                new GUIContent("Enable File Sync", "Automatically sync file changes between Zed and Unity"),
                ZedConfig.EnableFileSync
            );
            if (EditorGUI.EndChangeCheck())
            {
                ZedConfig.EnableFileSync = enableFileSync;
            }

            // File sync interval
            using (new EditorGUI.DisabledGroupScope(!ZedConfig.EnableFileSync))
            {
                EditorGUI.BeginChangeCheck();
                float syncInterval = EditorGUILayout.Slider(
                    new GUIContent("Sync Interval (seconds)", "How often to check for file changes"),
                    ZedConfig.FileSyncInterval,
                    0.5f,
                    10f
                );
                if (EditorGUI.EndChangeCheck())
                {
                    ZedConfig.FileSyncInterval = syncInterval;
                }
            }

            EditorGUILayout.HelpBox(
                "File sync monitors changes made in Zed and automatically refreshes Unity's asset database.",
                MessageType.Info
            );

            EditorGUILayout.EndVertical();
        }

        private static void DrawAdvancedSection()
        {
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true);
            
            if (_showAdvanced)
            {
                EditorGUILayout.BeginVertical(_boxStyle);

                // Enable logging
                EditorGUI.BeginChangeCheck();
                bool enableLogging = EditorGUILayout.Toggle(
                    new GUIContent("Enable Logging", "Log debug information to the Unity console"),
                    ZedConfig.EnableLogging
                );
                if (EditorGUI.EndChangeCheck())
                {
                    ZedConfig.EnableLogging = enableLogging;
                }

                EditorGUILayout.EndVertical();
            }
        }

        private static void DrawActionsSection()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(_boxStyle);
            
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Regenerate Project Files"))
            {
                var projectGen = new ProjectGeneration();
                projectGen.GenerateAll();
                EditorUtility.DisplayDialog(
                    "Project Files Generated",
                    "Project files (.sln and .csproj) have been regenerated.",
                    "OK"
                );
            }

            if (GUILayout.Button("Open Project in Zed"))
            {
                string zedPath = ZedConfig.ZedPath;
                if (string.IsNullOrEmpty(zedPath))
                {
                    zedPath = ZedUtils.FindZedExecutable();
                }

                if (!string.IsNullOrEmpty(zedPath) && File.Exists(zedPath))
                {
                    string projectPath = ZedUtils.GetProjectPath();
                    System.Diagnostics.Process.Start(zedPath, $"\"{projectPath}\"");
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Zed Not Found",
                        "Could not find Zed executable. Please set the path in the settings above.",
                        "OK"
                    );
                }
            }

            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);

            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog(
                    "Reset Settings",
                    "Are you sure you want to reset all Zed Unity settings to their defaults?",
                    "Reset",
                    "Cancel"))
                {
                    ZedConfig.ResetToDefaults();
                }
            }

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// Menu items for Zed Unity integration.
    /// </summary>
    public static class ZedMenu
    {
        [MenuItem("Tools/Zed/Open Project in Zed")]
        public static void OpenProjectInZed()
        {
            string zedPath = ZedConfig.ZedPath;
            if (string.IsNullOrEmpty(zedPath))
            {
                zedPath = ZedUtils.FindZedExecutable();
            }

            if (!string.IsNullOrEmpty(zedPath) && File.Exists(zedPath))
            {
                string projectPath = ZedUtils.GetProjectPath();
                System.Diagnostics.Process.Start(zedPath, $"\"{projectPath}\"");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Zed Not Found",
                    "Could not find Zed executable. Please configure the path in Edit > Preferences > External Tools.",
                    "OK"
                );
            }
        }

        [MenuItem("Tools/Zed/Regenerate Project Files")]
        public static void RegenerateProjectFiles()
        {
            var projectGen = new ProjectGeneration();
            projectGen.GenerateAll();
            Debug.Log("[Zed Unity] Project files regenerated.");
        }

        [MenuItem("Tools/Zed/Open Preferences")]
        public static void OpenPreferences()
        {
            SettingsService.OpenUserPreferences("Preferences/External Tools");
        }
    }
}
