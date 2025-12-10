# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2024-12-10

### Added

- Initial release
- `IExternalCodeEditor` implementation for Unity integration
- Automatic Zed executable detection (Linux, macOS, Windows)
- File opening with line:column navigation support
- Bidirectional file synchronization using FileSystemWatcher
- Automatic `.csproj` and `.sln` generation for Roslyn LSP
- Unity preferences UI for configuration
- Menu commands under Tools > Zed
- Support for multiple file types (.cs, .shader, .uss, .uxml, etc.)
- EditorPrefs-based persistent configuration
