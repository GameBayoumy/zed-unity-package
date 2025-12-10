using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Zed.Unity.Editor
{
    /// <summary>
    /// Bidirectional file synchronization between Zed editor and Unity.
    /// Monitors file changes made outside Unity and triggers asset database refresh.
    /// Also updates .csproj files when C# files are added, removed, or renamed.
    /// </summary>
    [InitializeOnLoad]
    public class FileSync
    {
        private static FileSync _instance;
        private static double _lastSyncTime;
        
        private readonly Dictionary<string, FileInfo> _trackedFiles;
        private readonly Dictionary<string, string> _fileHashes;
        private readonly ProjectGeneration _projectGeneration;
        private readonly HashSet<string> _pendingChanges;
        
        private bool _isInitialized;
        private string _projectPath;
        private FileSystemWatcher _watcher;

        // File extensions to track
        private static readonly string[] TrackedExtensions = 
        {
            ".cs", ".shader", ".compute", ".hlsl", ".cginc",
            ".uss", ".uxml", ".json", ".xml", ".txt", ".md",
            ".asmdef", ".asmref"
        };

        static FileSync()
        {
            // Initialize on editor load
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.quitting += OnEditorQuitting;
            
            _instance = new FileSync();
            
            if (ZedConfig.EnableFileSync)
            {
                _instance.Initialize();
            }
        }

        public FileSync()
        {
            _trackedFiles = new Dictionary<string, FileInfo>();
            _fileHashes = new Dictionary<string, string>();
            _pendingChanges = new HashSet<string>();
            _projectGeneration = new ProjectGeneration();
            _projectPath = ZedUtils.GetProjectPath();
        }

        /// <summary>
        /// Initialize the file sync system.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            try
            {
                // Set up FileSystemWatcher for real-time monitoring
                SetupFileWatcher();
                
                // Initial scan of tracked files
                ScanTrackedFiles();
                
                _isInitialized = true;
                
                if (ZedConfig.EnableLogging)
                {
                    Debug.Log("[Zed Unity] File sync initialized.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Zed Unity] Failed to initialize file sync: {ex.Message}");
            }
        }

        /// <summary>
        /// Shutdown the file sync system.
        /// </summary>
        public void Shutdown()
        {
            if (!_isInitialized)
                return;

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }

            _trackedFiles.Clear();
            _fileHashes.Clear();
            _pendingChanges.Clear();
            _isInitialized = false;

            if (ZedConfig.EnableLogging)
            {
                Debug.Log("[Zed Unity] File sync shutdown.");
            }
        }

        /// <summary>
        /// Set up the FileSystemWatcher for monitoring file changes.
        /// </summary>
        private void SetupFileWatcher()
        {
            if (_watcher != null)
            {
                _watcher.Dispose();
            }

            string assetsPath = Path.Combine(_projectPath, "Assets");
            
            if (!Directory.Exists(assetsPath))
            {
                Debug.LogWarning("[Zed Unity] Assets folder not found. File watching disabled.");
                return;
            }

            _watcher = new FileSystemWatcher(assetsPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                EnableRaisingEvents = true
            };

            // Set up filters for all tracked extensions
            _watcher.Filter = "*.*"; // Watch all files, filter in handlers

            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileCreated;
            _watcher.Deleted += OnFileDeleted;
            _watcher.Renamed += OnFileRenamed;
            _watcher.Error += OnWatcherError;
        }

        /// <summary>
        /// Initial scan of all tracked files in the project.
        /// </summary>
        private void ScanTrackedFiles()
        {
            _trackedFiles.Clear();
            _fileHashes.Clear();

            string assetsPath = Path.Combine(_projectPath, "Assets");
            
            if (!Directory.Exists(assetsPath))
                return;

            var files = Directory.GetFiles(assetsPath, "*.*", SearchOption.AllDirectories)
                .Where(f => IsTrackedExtension(f));

            foreach (string file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    _trackedFiles[file] = fileInfo;
                    _fileHashes[file] = ComputeFileHash(file);
                }
                catch (Exception ex)
                {
                    if (ZedConfig.EnableLogging)
                    {
                        Debug.LogWarning($"[Zed Unity] Failed to track file {file}: {ex.Message}");
                    }
                }
            }

            if (ZedConfig.EnableLogging)
            {
                Debug.Log($"[Zed Unity] Tracking {_trackedFiles.Count} files for sync.");
            }
        }

        /// <summary>
        /// Check if a file extension should be tracked.
        /// </summary>
        private bool IsTrackedExtension(string filePath)
        {
            string extension = Path.GetExtension(filePath)?.ToLowerInvariant();
            return TrackedExtensions.Contains(extension);
        }

        /// <summary>
        /// Handle file changed event.
        /// </summary>
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!IsTrackedExtension(e.FullPath))
                return;

            lock (_pendingChanges)
            {
                _pendingChanges.Add(e.FullPath);
            }

            if (ZedConfig.EnableLogging)
            {
                Debug.Log($"[Zed Unity] File changed: {e.FullPath}");
            }
        }

        /// <summary>
        /// Handle file created event.
        /// </summary>
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (!IsTrackedExtension(e.FullPath))
                return;

            lock (_pendingChanges)
            {
                _pendingChanges.Add(e.FullPath);
            }

            // Update tracked files
            try
            {
                var fileInfo = new FileInfo(e.FullPath);
                _trackedFiles[e.FullPath] = fileInfo;
                _fileHashes[e.FullPath] = ComputeFileHash(e.FullPath);
            }
            catch { }

            // Trigger project regeneration for .cs files
            if (Path.GetExtension(e.FullPath)?.ToLowerInvariant() == ".cs")
            {
                EditorApplication.delayCall += () =>
                {
                    _projectGeneration.SyncFile(e.FullPath, FileChangeType.Created);
                };
            }

            if (ZedConfig.EnableLogging)
            {
                Debug.Log($"[Zed Unity] File created: {e.FullPath}");
            }
        }

        /// <summary>
        /// Handle file deleted event.
        /// </summary>
        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            if (!IsTrackedExtension(e.FullPath))
                return;

            lock (_pendingChanges)
            {
                _pendingChanges.Add(e.FullPath);
            }

            // Remove from tracked files
            _trackedFiles.Remove(e.FullPath);
            _fileHashes.Remove(e.FullPath);

            // Trigger project regeneration for .cs files
            if (Path.GetExtension(e.FullPath)?.ToLowerInvariant() == ".cs")
            {
                EditorApplication.delayCall += () =>
                {
                    _projectGeneration.SyncFile(e.FullPath, FileChangeType.Deleted);
                };
            }

            if (ZedConfig.EnableLogging)
            {
                Debug.Log($"[Zed Unity] File deleted: {e.FullPath}");
            }
        }

        /// <summary>
        /// Handle file renamed event.
        /// </summary>
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            bool wasTracked = IsTrackedExtension(e.OldFullPath);
            bool isTracked = IsTrackedExtension(e.FullPath);

            // Remove old tracking
            if (wasTracked)
            {
                _trackedFiles.Remove(e.OldFullPath);
                _fileHashes.Remove(e.OldFullPath);
            }

            // Add new tracking
            if (isTracked)
            {
                try
                {
                    var fileInfo = new FileInfo(e.FullPath);
                    _trackedFiles[e.FullPath] = fileInfo;
                    _fileHashes[e.FullPath] = ComputeFileHash(e.FullPath);
                }
                catch { }

                lock (_pendingChanges)
                {
                    _pendingChanges.Add(e.FullPath);
                }
            }

            // Trigger project regeneration for .cs files
            if ((wasTracked || isTracked) && 
                (Path.GetExtension(e.OldFullPath)?.ToLowerInvariant() == ".cs" ||
                 Path.GetExtension(e.FullPath)?.ToLowerInvariant() == ".cs"))
            {
                EditorApplication.delayCall += () =>
                {
                    _projectGeneration.SyncFile(e.FullPath, FileChangeType.Renamed);
                };
            }

            if (ZedConfig.EnableLogging)
            {
                Debug.Log($"[Zed Unity] File renamed: {e.OldFullPath} -> {e.FullPath}");
            }
        }

        /// <summary>
        /// Handle watcher error.
        /// </summary>
        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            Debug.LogWarning($"[Zed Unity] File watcher error: {e.GetException()?.Message}");
            
            // Try to restart the watcher
            EditorApplication.delayCall += () =>
            {
                SetupFileWatcher();
            };
        }

        /// <summary>
        /// Compute MD5 hash of a file for change detection.
        /// </summary>
        private string ComputeFileHash(string filePath)
        {
            try
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Process pending changes and refresh asset database.
        /// </summary>
        private void ProcessPendingChanges()
        {
            HashSet<string> changes;
            
            lock (_pendingChanges)
            {
                if (_pendingChanges.Count == 0)
                    return;

                changes = new HashSet<string>(_pendingChanges);
                _pendingChanges.Clear();
            }

            if (changes.Count > 0)
            {
                if (ZedConfig.EnableLogging)
                {
                    Debug.Log($"[Zed Unity] Processing {changes.Count} file changes...");
                }

                // Refresh the asset database to pick up changes
                AssetDatabase.Refresh(ImportAssetOptions.Default);
            }
        }

        /// <summary>
        /// Called on editor update.
        /// </summary>
        private static void OnEditorUpdate()
        {
            if (_instance == null || !ZedConfig.EnableFileSync)
                return;

            // Check at configured interval
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - _lastSyncTime >= ZedConfig.FileSyncInterval)
            {
                _lastSyncTime = currentTime;
                _instance.ProcessPendingChanges();
            }
        }

        /// <summary>
        /// Called when editor is quitting.
        /// </summary>
        private static void OnEditorQuitting()
        {
            _instance?.Shutdown();
        }

        /// <summary>
        /// Force a full sync.
        /// </summary>
        public void ForceSync()
        {
            ScanTrackedFiles();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            _projectGeneration.GenerateAll();
        }

        /// <summary>
        /// Static method to force sync from menu.
        /// </summary>
        [MenuItem("Tools/Zed/Force Sync")]
        public static void ForceFullSync()
        {
            _instance?.ForceSync();
            Debug.Log("[Zed Unity] Force sync completed.");
        }
    }

    /// <summary>
    /// Asset post-processor to handle asset imports and keep project files in sync.
    /// </summary>
    public class ZedAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!ZedConfig.EnableFileSync)
                return;

            bool needsProjectSync = false;

            // Check if any C# files were affected
            foreach (string asset in importedAssets.Concat(deletedAssets).Concat(movedAssets))
            {
                if (asset.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    needsProjectSync = true;
                    break;
                }
            }

            if (needsProjectSync && ZedConfig.GenerateCsprojFiles)
            {
                EditorApplication.delayCall += () =>
                {
                    var projectGen = new ProjectGeneration();
                    projectGen.GenerateAll();
                    
                    if (ZedConfig.EnableLogging)
                    {
                        Debug.Log("[Zed Unity] Project files updated due to asset changes.");
                    }
                };
            }
        }
    }
}
