# Zed Unity Package

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Unity package that integrates [Zed](https://zed.dev) as an external code editor with bidirectional file sync and Roslyn LSP support.

## Features

- **External Script Editor** - Register Zed as Unity's external code editor
- **File Navigation** - Double-click scripts to open at the correct line/column
- **Bidirectional File Sync** - Changes in Zed automatically refresh in Unity
- **Project File Generation** - Automatic `.csproj` and `.sln` generation for Roslyn LSP
- **Cross-Platform** - Supports Linux, macOS, and Windows

## Installation

### Via Git URL (Recommended)

1. Open Unity Editor
2. Go to **Window > Package Manager**
3. Click **+** > **Add package from git URL**
4. Enter: `https://github.com/GameBayoumy/zed-unity-package.git`
5. Click **Add**

### Manual Installation

1. Clone this repository
2. In Unity, go to **Window > Package Manager**
3. Click **+** > **Add package from disk**
4. Navigate to `package.json`

## Setup

1. Go to **Edit > Preferences > External Tools**
2. Set **External Script Editor** to **Zed**
3. Configure settings as needed

## Settings

| Setting | Default | Description |
|---------|---------|-------------|
| Zed Path | Auto-detected | Path to Zed executable |
| Open in New Window | Off | Open files in new Zed window |
| Enable File Sync | On | Auto-refresh Unity on file changes |
| Sync Interval | 1.0s | File change detection interval |
| Generate .sln | On | Generate solution file |
| Generate .csproj | On | Generate project files |
| Use Roslyn Analyzers | On | Include analyzers in projects |

## Menu Commands

**Tools > Zed**:
- **Open Project in Zed** - Open entire project in Zed
- **Regenerate Project Files** - Manually regenerate .sln/.csproj files
- **Force Sync** - Force full file synchronization
- **Open Preferences** - Quick access to settings

## Zed Extension

For full C# IntelliSense and USS language support in Zed, install the companion Zed extension:

- [zed-unity](https://github.com/GameBayoumy/zed-unity) - Zed extension with Roslyn LSP and USS language server

## Requirements

- Unity 2021.3 or later
- Zed Editor

## Supported Platforms

| Platform | Zed Executable Names |
|----------|---------------------|
| Linux | `zed`, `zeditor`, `zed-editor` |
| macOS | `zed` |
| Windows | `zed.exe` |

## License

MIT License - See [LICENSE.md](LICENSE.md) for details.

## Related Projects

- [zed-unity](https://github.com/GameBayoumy/zed-unity) - Zed extension for Unity development
- [Zed Editor](https://zed.dev) - High-performance code editor
