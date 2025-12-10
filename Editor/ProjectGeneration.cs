using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Zed.Unity.Editor
{
    /// <summary>
    /// Generates .csproj and .sln files for Unity projects.
    /// Compatible with Roslyn LSP for IntelliSense in Zed.
    /// </summary>
    public class ProjectGeneration
    {
        private const string SlnTemplate = @"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
{0}Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
{1}	EndGlobalSection
EndGlobal
";

        private const string SlnProjectTemplate = @"Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{0}"", ""{1}"", ""{{{2}}}""
EndProject
";

        private const string CsprojTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <LangVersion>{0}</LangVersion>
    <_TargetFrameworkDirectories>non_empty_path_generated_by_unity</_TargetFrameworkDirectories>
    <_FullFrameworkReferenceAssemblyPaths>non_empty_path_generated_by_unity</_FullFrameworkReferenceAssemblyPaths>
    <DisableHandlePackageFileConflicts>true</DisableHandlePackageFileConflicts>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>10.0.20506</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{{{1}}}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>{2}</RootNamespace>
    <AssemblyName>{2}</AssemblyName>
    <TargetFrameworkVersion>{3}</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <BaseDirectory>{4}</BaseDirectory>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>Temp\bin\Debug\</OutputPath>
    <DefineConstants>{5}</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <NoWarn>0169;CS0649</NoWarn>
    <AllowUnsafeBlocks>{6}</AllowUnsafeBlocks>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>Temp\bin\Release\</OutputPath>
    <DefineConstants>{5}</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <NoWarn>0169;CS0649</NoWarn>
    <AllowUnsafeBlocks>{6}</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup>
{7}  </ItemGroup>
  <ItemGroup>
{8}  </ItemGroup>
  <ItemGroup>
{9}  </ItemGroup>
{10}  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>
";

        private string _projectPath;
        private string _projectName;
        private Dictionary<string, string> _assemblyGuidMap;

        public ProjectGeneration()
        {
            _projectPath = ZedUtils.GetProjectPath();
            _projectName = Path.GetFileName(_projectPath);
            _assemblyGuidMap = new Dictionary<string, string>();
        }

        /// <summary>
        /// Generate all project files (.sln and .csproj files).
        /// </summary>
        public void GenerateAll()
        {
            if (!ZedConfig.GenerateCsprojFiles && !ZedConfig.GenerateSlnFile)
                return;

            try
            {
                var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
                
                if (assemblies == null || assemblies.Length == 0)
                {
                    Debug.LogWarning("[Zed Unity] No assemblies found to generate project files.");
                    return;
                }

                // Generate GUIDs for assemblies
                foreach (var assembly in assemblies)
                {
                    if (!_assemblyGuidMap.ContainsKey(assembly.name))
                    {
                        _assemblyGuidMap[assembly.name] = GenerateGuid(assembly.name);
                    }
                }

                // Generate .csproj files
                if (ZedConfig.GenerateCsprojFiles)
                {
                    foreach (var assembly in assemblies)
                    {
                        GenerateCsproj(assembly);
                    }
                }

                // Generate .sln file
                if (ZedConfig.GenerateSlnFile)
                {
                    GenerateSln(assemblies);
                }

                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Zed Unity] Failed to generate project files: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Generate a .csproj file for an assembly.
        /// </summary>
        private void GenerateCsproj(Assembly assembly)
        {
            string csprojPath = Path.Combine(_projectPath, $"{assembly.name}.csproj");
            
            // Get source files
            StringBuilder sourceFiles = new StringBuilder();
            foreach (string sourceFile in assembly.sourceFiles)
            {
                string relativePath = GetRelativePath(sourceFile);
                sourceFiles.AppendLine($"    <Compile Include=\"{EscapeXml(relativePath)}\" />");
            }

            // Get references
            StringBuilder references = new StringBuilder();
            StringBuilder projectReferences = new StringBuilder();
            
            // Add assembly references
            foreach (string reference in assembly.compiledAssemblyReferences)
            {
                string referenceName = Path.GetFileNameWithoutExtension(reference);
                
                // Skip Unity assemblies that will be added as project references
                if (_assemblyGuidMap.ContainsKey(referenceName))
                    continue;
                    
                string hintPath = GetRelativePath(reference);
                references.AppendLine($"    <Reference Include=\"{EscapeXml(referenceName)}\">");
                references.AppendLine($"      <HintPath>{EscapeXml(hintPath)}</HintPath>");
                references.AppendLine($"    </Reference>");
            }

            // Add project references (other Unity assemblies)
            foreach (var assemblyRef in assembly.assemblyReferences)
            {
                if (_assemblyGuidMap.TryGetValue(assemblyRef.name, out string guid))
                {
                    projectReferences.AppendLine($"    <ProjectReference Include=\"{assemblyRef.name}.csproj\">");
                    projectReferences.AppendLine($"      <Project>{{{guid}}}</Project>");
                    projectReferences.AppendLine($"      <Name>{assemblyRef.name}</Name>");
                    projectReferences.AppendLine($"    </ProjectReference>");
                }
            }

            // Get define symbols
            string defines = string.Join(";", assembly.defines);

            // Get language version
            string langVersion = GetLanguageVersion();

            // Get target framework
            string targetFramework = GetTargetFrameworkVersion();

            // Check for unsafe code
            bool allowUnsafe = assembly.compilerOptions.AllowUnsafeCode;

            // Get assembly GUID
            string assemblyGuid = _assemblyGuidMap[assembly.name];

            // Build analyzers section
            string analyzersSection = "";
            if (ZedConfig.UseRoslynAnalyzers)
            {
                analyzersSection = GetAnalyzersSection();
            }

            // Generate the csproj content
            string content = string.Format(
                CsprojTemplate,
                langVersion,                          // {0} LangVersion
                assemblyGuid,                         // {1} ProjectGuid
                assembly.name,                        // {2} RootNamespace and AssemblyName
                targetFramework,                      // {3} TargetFrameworkVersion
                _projectPath,                         // {4} BaseDirectory
                defines,                              // {5} DefineConstants
                allowUnsafe.ToString().ToLower(),     // {6} AllowUnsafeBlocks
                sourceFiles.ToString(),               // {7} Compile items
                references.ToString(),                // {8} Reference items
                projectReferences.ToString(),         // {9} ProjectReference items
                analyzersSection                      // {10} Analyzers
            );

            File.WriteAllText(csprojPath, content, Encoding.UTF8);

            if (ZedConfig.EnableLogging)
            {
                Debug.Log($"[Zed Unity] Generated: {csprojPath}");
            }
        }

        /// <summary>
        /// Generate the .sln solution file.
        /// </summary>
        private void GenerateSln(Assembly[] assemblies)
        {
            string slnPath = Path.Combine(_projectPath, $"{_projectName}.sln");

            StringBuilder projects = new StringBuilder();
            StringBuilder projectConfigurations = new StringBuilder();

            foreach (var assembly in assemblies)
            {
                string guid = _assemblyGuidMap[assembly.name];
                string csprojFile = $"{assembly.name}.csproj";

                // Add project entry
                projects.AppendFormat(SlnProjectTemplate, assembly.name, csprojFile, guid.ToUpper());

                // Add project configurations
                string guidUpper = guid.ToUpper();
                projectConfigurations.AppendLine($"\t\t{{{guidUpper}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
                projectConfigurations.AppendLine($"\t\t{{{guidUpper}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
                projectConfigurations.AppendLine($"\t\t{{{guidUpper}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
                projectConfigurations.AppendLine($"\t\t{{{guidUpper}}}.Release|Any CPU.Build.0 = Release|Any CPU");
            }

            string content = string.Format(SlnTemplate, projects.ToString(), projectConfigurations.ToString());
            File.WriteAllText(slnPath, content, Encoding.UTF8);

            if (ZedConfig.EnableLogging)
            {
                Debug.Log($"[Zed Unity] Generated: {slnPath}");
            }
        }

        /// <summary>
        /// Get the Roslyn analyzers section for the csproj.
        /// </summary>
        private string GetAnalyzersSection()
        {
            StringBuilder analyzers = new StringBuilder();
            
            // Find Unity analyzers
            string[] analyzerPaths = Directory.GetFiles(_projectPath, "*.dll", SearchOption.AllDirectories)
                .Where(p => p.Contains("Analyzers") || p.Contains("analyzers"))
                .ToArray();

            if (analyzerPaths.Length > 0)
            {
                analyzers.AppendLine("  <ItemGroup>");
                foreach (string analyzerPath in analyzerPaths)
                {
                    string relativePath = GetRelativePath(analyzerPath);
                    analyzers.AppendLine($"    <Analyzer Include=\"{EscapeXml(relativePath)}\" />");
                }
                analyzers.AppendLine("  </ItemGroup>");
            }

            return analyzers.ToString();
        }

        /// <summary>
        /// Generate a deterministic GUID for an assembly name.
        /// </summary>
        private string GenerateGuid(string assemblyName)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(assemblyName));
                return new Guid(hash).ToString().ToUpper();
            }
        }

        /// <summary>
        /// Get the relative path from the project root.
        /// </summary>
        private string GetRelativePath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return fullPath;

            fullPath = Path.GetFullPath(fullPath);
            
            if (fullPath.StartsWith(_projectPath))
            {
                string relative = fullPath.Substring(_projectPath.Length);
                if (relative.StartsWith(Path.DirectorySeparatorChar.ToString()) ||
                    relative.StartsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    relative = relative.Substring(1);
                }
                return relative;
            }

            return fullPath;
        }

        /// <summary>
        /// Get the C# language version for the project.
        /// </summary>
        private string GetLanguageVersion()
        {
            // Unity 2021.2+ supports C# 9.0
            // Unity 2020.2+ supports C# 8.0
            // Earlier versions use C# 7.3
#if UNITY_2021_2_OR_NEWER
            return "9.0";
#elif UNITY_2020_2_OR_NEWER
            return "8.0";
#else
            return "7.3";
#endif
        }

        /// <summary>
        /// Get the target .NET framework version.
        /// </summary>
        private string GetTargetFrameworkVersion()
        {
            // Unity uses .NET Standard 2.1 or .NET Framework 4.7.1+
#if UNITY_2021_2_OR_NEWER
            return "v4.7.1";
#else
            return "v4.7.1";
#endif
        }

        /// <summary>
        /// Escape special XML characters.
        /// </summary>
        private string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        /// <summary>
        /// Sync project files when a specific file changes.
        /// </summary>
        public void SyncFile(string filePath, FileChangeType changeType)
        {
            if (!ZedConfig.GenerateCsprojFiles)
                return;

            // Find which assembly this file belongs to
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            
            foreach (var assembly in assemblies)
            {
                bool belongsToAssembly = assembly.sourceFiles.Any(sf => 
                    Path.GetFullPath(sf).Equals(Path.GetFullPath(filePath), StringComparison.OrdinalIgnoreCase));
                    
                if (belongsToAssembly || changeType == FileChangeType.Created || changeType == FileChangeType.Deleted)
                {
                    // Regenerate the affected csproj
                    if (!_assemblyGuidMap.ContainsKey(assembly.name))
                    {
                        _assemblyGuidMap[assembly.name] = GenerateGuid(assembly.name);
                    }
                    GenerateCsproj(assembly);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Type of file change for sync operations.
    /// </summary>
    public enum FileChangeType
    {
        Created,
        Modified,
        Deleted,
        Renamed
    }
}
