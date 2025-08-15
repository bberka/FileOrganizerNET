# File Organizer NET

A simple yet powerful command-line tool built with .NET and Cocona to automatically organize files in a specified directory (like your Downloads folder) into categorized subfolders based on their file extension.

## Key Features

- **Extensible Configuration**: Easily define which file extensions go into which folders using a simple `config.json` file.
- **Smart Folder Creation**: Category folders are created on-demand only when a file needs to be moved into them. No more empty folders.
- **Automatic Name Collision Handling**: If a file named `report.pdf` already exists in the destination, the new file will be automatically renamed to `report (1).pdf`.
- **Subfolder Organization**: Moves all existing subdirectories (except its own category folders) into a single, specified folder to declutter the main view.
- **Cross-Platform**: Built with .NET, it runs on Windows, macOS, and Linux.

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later.

## Installation & Setup

1. **Clone the repository:**

    ```bash
    git clone <your-repository-url>
    cd FileOrganizerCli
    ```

2. **Build the project:**
    This will restore dependencies and create an executable.

    ```bash
    dotnet build -c Release
    ```

## Usage

The tool is run from the command line, pointing it to the directory you want to organize.

### Command Syntax

```bash
dotnet run -- organize <TARGET_DIRECTORY> [OPTIONS]
```

### Arguments

- `<TARGET_DIRECTORY>` (Required): The full path to the directory you want to organize.

### Options

- `-c, --config <PATH>` (Optional): The path to a custom configuration file. If not provided, it defaults to `config.json` located in the same directory as the executable.

### Examples

**1. Organize your Downloads folder using the default `config.json`:**

```bash
# On Windows
dotnet run -- organize "C:\Users\YourUser\Downloads"

# On macOS / Linux
dotnet run -- organize "/Users/youruser/Downloads"
```

**2. Organize a different folder using a custom configuration file:**

```bash
dotnet run -- organize "D:\Unsorted\Projects" --config "C:\my-configs\project-rules.json"
```

**3. Get help and see all commands/options:**

```bash
dotnet run -- --help
```

## Configuration

The behavior of the organizer is controlled by a JSON file, by default named `config.json`. You can modify this file or create your own and point to it using the `--config` option.

### `config.json` Structure

```json
{
  "ExtensionMappings": {
    ".pdf": "Documents",
    ".docx": "Documents",
    ".jpg": "Photos",
    ".zip": "Archives",
    ".mp4": "Videos",
    ".cs": "Source Code",
    ".py": "Source Code",
    ".bat": "Scripts"
  },
  "OthersFolderName": "Others",
  "SubfoldersFolderName": "Folders"
}
```

### Configuration Sections

- **`ExtensionMappings`**: This is the core of the tool. It's a dictionary that maps a file extension (key) to a destination folder name (value).
  - The key **must** be a string starting with a dot (`.`) and should be lowercase.
  - The value is the name of the folder where matching files will be moved.

- **`OthersFolderName`**: Any file whose extension is not found in `ExtensionMappings` will be moved to a folder with this name.

- **`SubfoldersFolderName`**: After all files are processed, any remaining directories in the target folder (that are not category folders themselves) will be moved into a folder with this name.

## How It Works

The tool follows a simple, two-pass process:

1. **File Pass**:
    - It iterates through every **file** in the root of the target directory.
    - For each file, it looks up the extension in the `ExtensionMappings` from your config.
    - It determines the correct destination folder (e.g., "Photos", or "Others" if no match is found).
    - It creates the destination folder if it doesn't already exist.
    - It checks if a file with the same name exists at the destination. If so, it generates a new unique name (e.g., `image (1).jpg`).
    - It moves the file to its new home.

2. **Folder Pass**:
    - After all files are moved, it iterates through every **directory** in the root of the target directory.
    - It checks if the directory's name is one of the special category folders defined in the config (e.g., "Photos", "Documents", "Folders", etc.).
    - If it's **not** a special category folder, it is moved into the `SubfoldersFolderName`.

## Understanding the Releases

Each release provides several packages for different platforms and use cases. The file names follow a consistent pattern: `FileOrganizerNET-v<VERSION>-<SLUG>.zip`. Here’s how to choose the right one for you.

### Build Types

-   **Self-Contained (Recommended)**: These packages include the .NET runtime and all dependencies. They are larger but work out-of-the-box without any prerequisites. If you're unsure which to download, choose this one for your platform.
    -   Example slug: `win-x64`, `linux-x64`

-   **Framework-Dependent**: These are much smaller packages, but they require you to have the [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) installed on your system separately. This is a good option for users who already work with .NET.
    -   Example slug: `win-x64-framework`

-   **Single-File Executable (Windows)**: This is a special self-contained package that bundles the entire application into a single `.exe` file. It's the most portable option for Windows users.
    -   Example slug: `win-x64-single-file`

### Platform Naming

The "slug" in the file name tells you the target platform and architecture.

| Slug Suffix         | Meaning                               |
| ------------------- | ------------------------------------- |
| `-win-x64`          | Windows 64-bit                        |
| `-win-x86`          | Windows 32-bit                        |
| `-linux-x64`        | Linux 64-bit                          |
| `-osx-x64`          | macOS on Intel processors             |
| `-osx-arm64`        | macOS on Apple Silicon (M1/M2/M3)     |

## License

This project is licensed under the MIT License.
