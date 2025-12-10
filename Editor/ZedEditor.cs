using System;
using System.Diagnostics;
using System.IO;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Zed.Unity.Editor
{
    /// <summary>
    /// Zed Code Editor integration for Unity.
    /// Implements IExternalCodeEditor to register Zed as an external script editor.
    /// </summary>
    [InitializeOnLoad]
    public class ZedEditor : IExternalCodeEditor
    {
        private static readonly string[] SupportedExtensions = { ".cs", ".shader", ".compute", ".hlsl", ".cginc", ".uss", ".uxml", ".json", ".xml", ".txt", ".md", ".asmdef" };
        
        private readonly ProjectGeneration _projectGeneration;
        private readonly FileSync _fileSync;

        static ZedEditor()
        {
            // Register this editor with Unity's CodeEditor system
            CodeEditor.Register(new ZedEditor());
        }

        public ZedEditor()
        {
            _projectGeneration = new ProjectGeneration();
            _fileSync = new FileSync();
        }

        /// <summary>
        /// Display name shown in Unity's External Tools preferences.
        /// </summary>
        public CodeEditor.Installation[] Installations => GetInstallations();

        /// <summary>
        /// Called when the user opens a file from Unity (double-click, error log, etc.)
        /// </summary>
        public bool OpenProject(string filePath, int line, int column)
        {
            string zedPath = ZedConfig.ZedPath;
            
            if (string.IsNullOrEmpty(zedPath) || !File.Exists(zedPath))
            {
                zedPath = ZedUtils.FindZedExecutable();
                if (string.IsNullOrEmpty(zedPath))
                {
                    Debug.LogError("[Zed Unity] Could not find Zed executable. Please set the path in Preferences > External Tools.");
                    return false;
                }
                ZedConfig.ZedPath = zedPath;
            }

            // Normalize the file path
            filePath = Path.GetFullPath(filePath);

            // Build the command line arguments
            // Zed uses file:line:column format for navigation
            string arguments = BuildArguments(filePath, line, column);

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = zedPath,
                    Arguments = arguments,
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                Process.Start(startInfo);
                
                if (ZedConfig.EnableLogging)
                {
                    Debug.Log($"[Zed Unity] Opening: {zedPath} {arguments}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Zed Unity] Failed to open Zed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Build command line arguments for Zed.
        /// </summary>
        private string BuildArguments(string filePath, int line, int column)
        {
            string projectPath = Directory.GetParent(Application.dataPath)?.FullName ?? "";
            
            // Escape spaces in paths
            string escapedFilePath = filePath.Contains(" ") ? $"\"{filePath}\"" : filePath;
            string escapedProjectPath = projectPath.Contains(" ") ? $"\"{projectPath}\"" : projectPath;

            // Zed supports file:line:column syntax
            string fileArg;
            if (line > 0)
            {
                fileArg = column > 0 
                    ? $"{escapedFilePath}:{line}:{column}" 
                    : $"{escapedFilePath}:{line}";
            }
            else
            {
                fileArg = escapedFilePath;
            }

            // Open project folder first, then the specific file
            string args = escapedProjectPath;
            
            if (!string.IsNullOrEmpty(filePath))
            {
                args += $" {fileArg}";
            }

            if (ZedConfig.OpenInNewWindow)
            {
                args = $"-n {args}";
            }

            return args;
        }

        /// <summary>
        /// Synchronize (regenerate) all project files.
        /// </summary>
        public void SyncAll()
        {
            _projectGeneration.GenerateAll();
            
            if (ZedConfig.EnableLogging)
            {
                Debug.Log("[Zed Unity] Project files synchronized.");
            }
        }

        /// <summary>
        /// Synchronize project files if needed.
        /// </summary>
        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            bool needsSync = false;
            
            // Check if any relevant files were changed
            foreach (string file in addedFiles)
            {
                if (IsSupportedFile(file))
                {
                    needsSync = true;
                    break;
                }
            }
            
            if (!needsSync)
            {
                foreach (string file in deletedFiles)
                {
                    if (IsSupportedFile(file))
                    {
                        needsSync = true;
                        break;
                    }
                }
            }
            
            if (!needsSync)
            {
                foreach (string file in movedFiles)
                {
                    if (IsSupportedFile(file))
                    {
                        needsSync = true;
                        break;
                    }
                }
            }

            if (needsSync)
            {
                _projectGeneration.GenerateAll();
                
                if (ZedConfig.EnableLogging)
                {
                    Debug.Log("[Zed Unity] Project files synchronized due to file changes.");
                }
            }
        }

        /// <summary>
        /// Initialize the code editor when it becomes the active editor.
        /// </summary>
        public void Initialize(string editorInstallationPath)
        {
            ZedConfig.ZedPath = editorInstallationPath;
            
            // Initialize file sync if enabled
            if (ZedConfig.EnableFileSync)
            {
                _fileSync.Initialize();
            }
        }

        /// <summary>
        /// Draw custom GUI in the External Tools preferences.
        /// </summary>
        public void OnGUI()
        {
            ZedPreferences.DrawPreferencesGUI();
        }

        /// <summary>
        /// Check if a given file path can be opened by this editor.
        /// </summary>
        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
        {
            if (ZedUtils.IsValidZedPath(editorPath))
            {
                installation = new CodeEditor.Installation
                {
                    Name = "Zed",
                    Path = editorPath
                };
                return true;
            }

            installation = default;
            return false;
        }

        /// <summary>
        /// Get all available Zed installations.
        /// </summary>
        private CodeEditor.Installation[] GetInstallations()
        {
            var installations = new System.Collections.Generic.List<CodeEditor.Installation>();

            // If a custom path is set and valid, add it first
            if (!string.IsNullOrEmpty(ZedConfig.ZedPath) && File.Exists(ZedConfig.ZedPath))
            {
                installations.Add(new CodeEditor.Installation
                {
                    Name = "Zed",
                    Path = ZedConfig.ZedPath
                });
            }

            // Try to find Zed in common locations
            string[] possiblePaths = ZedUtils.GetPossibleZedPaths();
            
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    // Check if already added
                    bool alreadyAdded = false;
                    foreach (var inst in installations)
                    {
                        if (inst.Path == path)
                        {
                            alreadyAdded = true;
                            break;
                        }
                    }
                    
                    if (!alreadyAdded)
                    {
                        installations.Add(new CodeEditor.Installation
                        {
                            Name = "Zed",
                            Path = path
                        });
                    }
                    break; // Only add the first found installation
                }
            }

            // Always provide at least one entry so Zed appears in the list
            // Users can then configure the path manually
            if (installations.Count == 0)
            {
                // Try to auto-detect one more time
                string detected = ZedUtils.FindZedExecutable();
                if (!string.IsNullOrEmpty(detected))
                {
                    installations.Add(new CodeEditor.Installation
                    {
                        Name = "Zed",
                        Path = detected
                    });
                }
                else
                {
                    // Add a placeholder entry - user needs to configure path
                    installations.Add(new CodeEditor.Installation
                    {
                        Name = "Zed",
                        Path = "/usr/bin/zed" // Default placeholder path
                    });
                }
            }

            return installations.ToArray();
        }

        /// <summary>
        /// Check if a file extension is supported.
        /// </summary>
        private bool IsSupportedFile(string filePath)
        {
            string extension = Path.GetExtension(filePath)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension)) return false;
            
            foreach (string supported in SupportedExtensions)
            {
                if (extension == supported) return true;
            }
            return false;
        }
    }
}
