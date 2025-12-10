# Zed Unity Integration

This Unity package integrates Zed as an external code editor for Unity, providing seamless file opening, bidirectional file sync, and automatic project file generation for Roslyn LSP support.

## Features

- **External Script Editor** - Double-click scripts in Unity to open them in Zed at the correct line
- **Bidirectional File Sync** - Changes made in Zed automatically refresh in Unity
- **Project File Generation** - Automatic `.csproj` and `.sln` generation compatible with Roslyn LSP
- **Cross-Platform** - Supports Linux, macOS, and Windows
## Installation

### Via Git URL (Recommended)

1. Open Unity Editor
2. Go to **Window > Package Manager**
3. Click **+** > **Add package from git URL**
4. Enter: `https://github.com/GameBayoumy/zed-unity.git?path=Unity/com.zed.unity`

### Local Installation

1. Clone the repository
2. In Unity Package Manager, click **+** > **Add package from disk**
3. Select `Unity/com.zed.unity/package.json`

## Setup

1. Go to **Edit > Preferences > External Tools**
2. Set **External Script Editor** to **Zed**
3. Configure settings as needed (most have sensible defaults)

## Settings

| Setting | Default | Description |
|---------|---------|-------------|
| Zed Path | Auto-detected | Path to Zed executable |
| Open in New Window | Off | Open files in new Zed window |
| Enable File Sync | On | Auto-refresh Unity on file changes |
| Sync Interval | 1.0s | File change detection interval |
| Generate .sln | On | Generate solution file |
| Generate .csproj | On | Generate project files |
| Include Packages | On | Include Unity packages in projects |
| Use Roslyn Analyzers | On | Include analyzers in projects |

## Menu Commands

**Tools > Zed**:
- **Open Project in Zed** - Open entire project
- **Regenerate Project Files** - Manually regenerate files
- **Force Sync** - Force full synchronization
- **Open Preferences** - Quick access to settings

## Zed CLI Reference

The package uses Zed's command-line interface:

```bash
# Open file at line:column
zed /path/to/file.cs:42:10

# Open project directory
zed /path/to/unity-project

# Open in new window
zed -n /path/to/file.cs
```

## Supported Platforms

| Platform | Zed Locations |
|----------|---------------|
| Linux | `/usr/bin/zed`, `~/.local/bin/zed`, flatpak, snap |
| macOS | `/Applications/Zed.app`, homebrew |
| Windows | Scoop, Chocolatey, WinGet (when available) |

## Troubleshooting

### Zed Not Detected

1. Click "Auto-Detect" in preferences
2. Or manually browse to the Zed executable
3. Common locations:
   - Linux: `/usr/bin/zed`, `~/.local/bin/zed`
   - macOS: `/Applications/Zed.app/Contents/MacOS/zed`

### Project Files Not Generating

1. Check Unity console for errors
2. Use **Tools > Zed > Regenerate Project Files**
3. Ensure you have write permissions to project folder

### File Sync Issues

1. Verify "Enable File Sync" is checked
2. Check sync interval (lower = faster, but more CPU)
3. Force sync via **Tools > Zed > Force Sync**

### LSP Not Working in Zed

1. Ensure the Zed Unity extension is installed in Zed
2. Check that `.sln` file exists in project root
3. Regenerate project files if needed

## License

MIT License - See LICENSE.md for details.
