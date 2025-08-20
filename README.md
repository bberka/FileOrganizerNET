# FileOrganizerNET

A simple yet powerful command-line tool built with .NET 9 and Cocona to automatically organize files in a specified directory (like your Downloads folder) into categorized subfolders based on configurable rules.

## Key Features

-   **Extensible Configuration**: Define sophisticated file organization rules using a `config.json` file.
-   **Smart Folder Creation**: Category folders are created on-demand only when a file needs to be moved into them, avoiding empty directories.
-   **Recursive Processing**: An optional `--recursive` flag allows for cleaning up nested subdirectories in addition to the top-level directory.
-   **Safe Dry-Run Mode**: Preview all proposed file and folder operations with `--dry-run` before any changes are applied to the filesystem.
-   **Automatic Name Collision Handling**: If a file named `report.pdf` already exists in the destination, new files with the same name will be automatically renamed (e.g., `report (1).pdf`).
-   **Subfolder Organization**: Moves all existing subdirectories (except its own category folders) into a single, specified `Folders` directory to declutter the main view.
-   **Duplicate File Detection**: An optional `--check-duplicates` flag scans organized folders for identical files (using fast XXHash checksums) and deletes redundant copies, retaining only one instance.
-   **Cross-Platform**: Built with .NET 9, FileOrganizerNET runs seamlessly on Windows, macOS, and Linux.
-   **Automatic Logging**: All operations are logged to `organizer.log` located next to the executable, and also printed to the console.

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

The tool is run from the command line, offering several commands.

### Main Commands

-   **`organize`**: The primary command to organize files and folders.
-   **`init`**: Generates a default `config.json` file to get you started.
-   **`validate`**: Checks the syntax and structure of a `config.json` file.

### Command Syntax

```bash
# General syntax
dotnet run -- <COMMAND> [OPTIONS]

# Example for 'organize'
dotnet run -- organize <TARGET_DIRECTORY> [OPTIONS]

# Example for 'init'
dotnet run -- init [OPTIONS]

# Example for 'validate'
dotnet run -- validate [OPTIONS]
```

### `organize` Command Arguments & Options

| Option                | Alias | Description                                                              | Default       |
| :-------------------- | :---- | :----------------------------------------------------------------------- | :------------ |
| `<TARGET_DIRECTORY>`  |       | **(Required)** The full path to the directory to organize.               |               |
| `--config <PATH>`     | `-c`  | Path to a custom JSON config file.                                       | `config.json` |
| `--recursive`         | `-r`  | Process files in all subdirectories recursively.                         | `false`       |
| `--dry-run`           |       | Simulate the organization and print actions without modifying files.     | `false`       |
| `--check-duplicates`  |       | Scan organized folders for duplicate files (using XXHash) and delete extra copies. | `false`       |

### `init` Command Arguments & Options

| Option        | Alias | Description                                                         | Default       |
| :------------ | :---- | :------------------------------------------------------------------ | :------------ |
| `<OUTPUT_PATH>`|       | The path where the default `config.json` file will be created.    | `config.json` |
| `--force`     | `-f`  | Overwrite the existing file without a prompt if it already exists.  | `false`       |

### `validate` Command Arguments & Options

| Option        | Alias | Description                                                           | Default       |
| :------------ | :---- | :-------------------------------------------------------------------- | :------------ |
| `<CONFIG_PATH>`|       | The path to the `config.json` file to validate.                       | `config.json` |

### Examples

**1. Generate a default config file in the current directory:**

```bash
dotnet run -- init
```

**2. Generate and force overwrite a config file in a specific location:**

```bash
dotnet run -- init "C:\MyConfigs\my-custom-config.json" --force
```

**3. Validate an existing configuration file:**

```bash
dotnet run -- validate "C:\Users\YourUser\Downloads\config.json"
```

**4. Safely preview an organization of your Downloads folder (Dry Run):**

```bash
dotnet run -- organize "C:\Users\YourUser\Downloads" --dry-run
```

**5. Recursively organize a project folder and check for duplicates:**

```bash
dotnet run -- organize "D:\Projects" --recursive --check-duplicates
```

## Configuration

The behavior of the organizer is controlled by a JSON file (`config.json`). The configuration is based on an ordered list of rules, where the **first rule that a file matches is the one that gets applied**.

### `config.json` Structure

```json
{
  "Rules": [
    {
      "Action": "Delete",
      "Conditions": {
        "extensions": [".tmp", ".log"]
      }
    },
    {
      "Action": "Copy",
      "DestinationFolder": "Invoices/Backup",
      "Conditions": {
        "fileNameContains": ["invoice", "receipt"]
      }
    },
    {
      "Action": "Move",
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

-   **`Rules`**: An array of rule objects, processed from top to bottom.
    -   `Action`: (Optional) The action to perform when a rule matches. Can be `Move`, `Copy`, or `Delete`. **Defaults to `Move` if not specified.**
    -   `DestinationFolder`: The name of the folder for `Move` or `Copy` actions. This property is ignored for `Delete` actions.
    -   `Conditions`: An object specifying all conditions that a file must meet for the rule to apply. A file must satisfy **all** conditions within a single rule.

-   **`OthersFolderName`**: The name of the folder where any file that does not match any of the defined `Rules` will be moved. These files are always moved.

-   **`SubfoldersFolderName`**: The name of the folder where any remaining subdirectories (that are not themselves category folders defined in `Rules` or `OthersFolderName`) will be moved.

### Available Actions

| Action   | Description                                                              |
| :------- | :----------------------------------------------------------------------- |
| `Move`   | Moves the file to the `DestinationFolder`. This is the default action.   |
| `Copy`   | Copies the file to the `DestinationFolder`, leaving the original intact. |
| `Delete` | Permanently deletes the file. Use with caution.                          |

### Available Conditions

| Property           | Type             | Description                                                              |
| :----------------- | :--------------- | :----------------------------------------------------------------------- |
| `extensions`       | Array of strings | Matches if the file's extension is present in the list (case-insensitive). |
| `fileNameContains` | Array of strings | Matches if the file's name contains any of the keywords in the list (case-insensitive). |
| `olderThanDays`    | Number           | Matches if the file's last modified date is older than this many days.   |
| `minSizeMB`        | Number           | Matches if the file's size is greater than or equal to this many megabytes. |

## How It Works

The tool follows a multi-pass process to organize your files and folders:

1.  **File Organization Pass**:
    *   It iterates through every file in the target directory (and subdirectories if `--recursive` is enabled).
    *   It skips specific files like `config.json` and any files already residing within an established category folder.
    *   It evaluates the file against the `Rules` defined in `config.json` from top to bottom, applying the first rule whose `Conditions` are fully met.
    *   Based on the `Action` defined in the matched rule (Move, Copy, or Delete) or a default Move to the `OthersFolderName` if no rule matches, it performs the designated operation.
    *   During moves or copies, it creates destination folders as needed and intelligently handles name collisions by appending a counter (e.g., `image (1).jpg`).

2.  **Folder Organization Pass**:
    *   After all file actions are completed, it processes directories located in the **root** of the target directory.
    *   Any subdirectory that is not itself a special category folder (defined in your `config.json`) is moved into the `SubfoldersFolderName`.

3.  **Duplicate File Check Pass (Conditional)**:
    *   This pass executes **only if** the `--check-duplicates` command-line option is enabled.
    *   It scans all files within the *managed destination folders* (e.g., "Photos", "Documents", "Others", and any other folders defined in your rules).
    *   It calculates a fast [XXHash checksum](https://en.wikipedia.org/wiki/XXHash) for the content of each file.
    *   If multiple files share the same checksum, all but one copy (arbitrarily, the first one encountered) are identified as duplicates.
    *   These identified duplicate files are then deleted from the filesystem.

## Understanding the Releases

Each release provides several packages. The file names follow the pattern: `FileOrganizerNET-v<VERSION>-<SLUG>.zip`.

### Build Types

-   **Self-Contained (Recommended)**: These packages include the .NET runtime and all dependencies. They are larger but work out-of-the-box without any prerequisites. If you're unsure which to download, choose this one for your platform.
    -   Example slug: `win-x64`, `linux-x64`
-   **Framework-Dependent**: These are much smaller packages, but they require you to have the [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) installed on your system separately. This is a good option for users who already work with .NET.
    -   Example slug: `win-x64-framework`
-   **Single-File Executable (Windows)**: A self-contained package that consists of a single `.exe` file for maximum portability.
    -   Example slug: `win-x64-single-file`

### Platform Naming

The "slug" in the file name tells you the target platform and architecture.

| Slug Suffix         | Meaning                               |
| :------------------ | :------------------------------------ |
| `-win-x64`          | Windows 64-bit                        |
| `-win-x86`          | Windows 32-bit                        |
| `-linux-x64`        | Linux 64-bit                          |
| `-osx-x64`          | macOS on Intel processors             |
| `-osx-arm64`        | macOS on Apple Silicon (M1/M2/M3)     |

## License

This project is licensed under the MIT License.