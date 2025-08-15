# FileOrganizerNET

A simple yet powerful command-line tool built with .NET 9 and Cocona to automatically organize files in a specified directory (like your Downloads folder) into categorized subfolders based on their file extension.

## Key Features

-   **Extensible Configuration**: Easily define which file extensions go into which folders using a simple `config.json` file.
-   **Smart Folder Creation**: Category folders are created on-demand only when a file needs to be moved into them.
-   **Recursive Processing**: An optional `--recursive` flag allows you to clean up nested subdirectories as well.
-   **Safe Dry-Run Mode**: Preview all proposed changes with `--dry-run` before a single file is moved.
-   **Automatic Name Collision Handling**: If `report.pdf` already exists, a new file is automatically renamed to `report (1).pdf`.
-   **Subfolder Organization**: Moves all existing subdirectories into a single `Folders` directory to declutter the main view.
-   **Cross-Platform**: Built with .NET 9, it runs on Windows, macOS, and Linux.

## Prerequisites

-   [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (for building from source).

## Installation & Setup

1.  **Clone the repository:**

    ```bash
    git clone <your-repository-url>
    cd FileOrganizerNET
    ```

2.  **Build the project:**
    This will restore dependencies and create an executable.

    ```bash
    dotnet build -c Release
    ```

## Usage

The tool is run from the command line, pointing it to the directory you want to organize.

### Command Syntax

```bash
# Using dotnet run
dotnet run -- organize <TARGET_DIRECTORY> [OPTIONS]

# Using the published executable
FileOrganizerNET.exe organize <TARGET_DIRECTORY> [OPTIONS]
```

### Arguments

-   `<TARGET_DIRECTORY>` (Required): The full path to the directory you want to organize.

### Options

| Option              | Alias | Description                                                    | Default       |
| ------------------- | ----- | -------------------------------------------------------------- | ------------- |
| `--config <PATH>`   | `-c`  | The path to a custom configuration file.                       | `config.json` |
| `--recursive`       | `-r`  | Process files in all subdirectories recursively.               | `false`       |
| `--dry-run`         |       | Simulate the organization and print actions without moving files.| `false`       |
| `--log-file <PATH>` | `-l`  | Path to a file to write log output in addition to the console. | `null`        |

### Examples

**1. Safely preview an organization of your Downloads folder:**

```bash
dotnet run -- organize "C:\Users\YourUser\Downloads" --dry-run
```

**2. Recursively organize a project folder and log the output to a file:**

```bash
dotnet run -- organize "D:\Projects" --recursive --log-file "D:\logs\organizer.log"
```

## Configuration

The behavior of the organizer is controlled by a JSON file (`config.json`). The configuration is based on an ordered list of rules, where the **first rule that a file matches is the one that gets applied**.

### `config.json` Structure

```json
{
  "Rules": [
    {
      "DestinationFolder": "Invoices",
      "Conditions": {
        "fileNameContains": ["invoice", "receipt"],
        "extensions": [".pdf", ".docx"]
      }
    },
    {
      "DestinationFolder": "Archive",
      "Conditions": {
        "olderThanDays": 365
      }
    },
    {
      "DestinationFolder": "Photos",
      "Conditions": {
        "extensions": [".jpg", ".png"]
      }
    }
  ],
  "OthersFolderName": "Others",
  "SubfoldersFolderName": "Folders"
}
```

### Configuration Sections

-   **`Rules`**: An array of rule objects. They are processed from top to bottom.
    -   `DestinationFolder`: The name of the folder where matching files will be moved.
    -   `Conditions`: An object specifying all conditions that a file must meet for the rule to apply. A file must satisfy **all** conditions within a single rule.

-   **`OthersFolderName`**: The destination for any file that does not match any of the defined `Rules`.

-   **`SubfoldersFolderName`**: The destination for any subdirectory that is not a category folder itself.

### Available Conditions

You can use any combination of the following conditions within a rule's `Conditions` object:

| Property           | Type          | Description                                                              |
| ------------------ | ------------- | ------------------------------------------------------------------------ |
| `extensions`       | Array of strings | Matches if the file's extension is in the list (case-insensitive).       |
| `fileNameContains` | Array of strings | Matches if the file's name contains any of the keywords in the list.     |
| `olderThanDays`    | Number        | Matches if the file's last modified date is older than this many days.   |
| `minSizeMB`        | Number        | Matches if the file's size is greater than or equal to this many megabytes. |
## How It Works

The tool follows a simple, two-pass process:

1.  **File Pass**:
    -   It iterates through every file in the target directory. If `--recursive` is used, it includes all subdirectories.
    -   It skips any files that are already inside a managed category folder (e.g., a `.jpg` inside the `Photos` folder).
    -   It determines the correct destination folder (e.g., "Photos") based on the `config.json` mappings.
    -   It creates the destination folder if it doesn't already exist.
    -   It checks for name collisions and renames the file if necessary (e.g., `image (1).jpg`).
    -   It moves the file to its new home.

2.  **Folder Pass**:
    -   After all files are moved, it iterates through every directory in the **root** of the target directory.
    -   It moves any directory that is **not** a special category folder into the `SubfoldersFolderName`.

## Understanding the Releases

Each release provides several packages. The file names follow the pattern: `FileOrganizerNET-v<VERSION>-<SLUG>.zip`.

### Build Types

-   **Self-Contained (Recommended)**: These packages include the .NET runtime and all dependencies. They are larger but work out-of-the-box without any prerequisites. If you're unsure which to download, choose this one for your platform.
    -   Example slug: `win-x64`, `linux-x64`
-   **Framework-Dependent**: A much smaller download, but requires you to have the [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) installed on your system.
    -   Example slug: `win-x64-framework`
-   **Single-File Executable (Windows)**: A self-contained package that consists of a single `.exe` file for maximum portability.
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