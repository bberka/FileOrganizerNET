# build.ps1
#
# Automates building and packaging for multiple platforms.
# Each build target is individually zipped into a release-ready package.

# --- Configuration ---
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop" # Exit script on any error

# --- Path and Version Setup ---
$scriptDir = $PSScriptRoot
$projectRoot = Resolve-Path "$scriptDir\.."
$projectName = "FileOrganizerNET"

# Read the version from the VERSION file
$versionFile = "$projectRoot\VERSION"
if (-not (Test-Path $versionFile)) {
    Write-Host "ERROR: Version file not found at '$versionFile'" -ForegroundColor Red
    exit 1
}
$version = (Get-Content $versionFile).Trim()
if ([string]::IsNullOrWhiteSpace($version)) {
    Write-Host "ERROR: VERSION file is empty." -ForegroundColor Red
    exit 1
}

Write-Host "--- Starting build for $projectName version $version ---" -ForegroundColor Cyan

# --- Output Directories ---
$releaseDir = "$projectRoot\release"
# A temporary directory for each individual build before it's zipped
$stagingDir = "$projectRoot\build_temp"

# --- 1. Clean previous builds ---
Write-Host "`n--- Cleaning previous build artifacts ---" -ForegroundColor Yellow
if (Test-Path $releaseDir) {
    Remove-Item -Path $releaseDir -Recurse -Force
}
if (Test-Path $stagingDir) {
    Remove-Item -Path $stagingDir -Recurse -Force
}
New-Item -Path $releaseDir -ItemType Directory | Out-Null
New-Item -Path $stagingDir -ItemType Directory | Out-Null

# --- 2. Define Build Targets ---
# Each target now includes a 'ZipSlug' for unique archive naming.
$buildTargets = @(
    # --- Windows Builds ---
    @{ Name = "Windows-x64-SelfContained";      Args = "-r win-x64 --self-contained true";  ZipSlug = "win-x64" },
    @{ Name = "Windows-x86-SelfContained";      Args = "-r win-x86 --self-contained true";  ZipSlug = "win-x86" },
    @{ Name = "Windows-x64-FrameworkDependent"; Args = "-r win-x64 --self-contained false"; ZipSlug = "win-x64-framework" },
    @{ Name = "Windows-x86-FrameworkDependent"; Args = "-r win-x86 --self-contained false"; ZipSlug = "win-x86-framework" },
    @{ Name = "Windows-x64-SingleFile";         Args = "-r win-x64 --self-contained true /p:PublishSingleFile=true"; ZipSlug = "win-x64-single-file" },
    @{ Name = "Windows-x86-SingleFile";         Args = "-r win-x86 --self-contained true /p:PublishSingleFile=true"; ZipSlug = "win-x86-single-file" },

    # --- Linux Builds ---
    @{ Name = "Linux-x64-SelfContained";      Args = "-r linux-x64 --self-contained true";  ZipSlug = "linux-x64" },
    @{ Name = "Linux-x64-FrameworkDependent"; Args = "-r linux-x64 --self-contained false"; ZipSlug = "linux-x64-framework" },

    # --- macOS Builds ---
    @{ Name = "macOS-x64-SelfContained";      Args = "-r osx-x64 --self-contained true";  ZipSlug = "osx-x64" },
    @{ Name = "macOS-arm64-SelfContained";    Args = "-r osx-arm64 --self-contained true";  ZipSlug = "osx-arm64" },
    @{ Name = "macOS-x64-FrameworkDependent"; Args = "-r osx-x64 --self-contained false"; ZipSlug = "osx-x64-framework" },
    @{ Name = "macOS-arm64-FrameworkDependent"; Args = "-r osx-arm64 --self-contained false"; ZipSlug = "osx-arm64-framework" }
)

# --- 3. Run Publish and Zip Commands ---
Write-Host "`n--- Publishing and packaging for all targets ---" -ForegroundColor Yellow

# Correctly locate the .csproj file, assuming a standard solution structure
$projectFile = Get-ChildItem -Path $projectRoot -Filter "$projectName.csproj" -Recurse | Select-Object -First 1
if (-not $projectFile) {
    Write-Host "ERROR: Could not find '$projectName.csproj' in '$projectRoot' or its subdirectories." -ForegroundColor Red
    exit 1
}

foreach ($target in $buildTargets) {
    $targetName = $target.Name
    $publishArgs = $target.Args
    $zipSlug = $target.ZipSlug
    
    # Use a unique output directory for each build inside the staging area
    $targetOutputDir = "$stagingDir\$targetName"
    
    Write-Host "`nBuilding for: $targetName" -ForegroundColor Green
    
    # Construct and execute the dotnet publish command
    $command = "dotnet publish `"$($projectFile.FullName)`" -c Release -o `"$targetOutputDir`" $publishArgs"
    Invoke-Expression $command

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Build failed for $targetName" -ForegroundColor Red
        exit 1
    }

    # --- Create Individual Zip Package ---
    $zipFileName = "$projectName-v$version-$zipSlug.zip"
    $zipFilePath = "$releaseDir\$zipFileName"
    
    Write-Host "Packaging into: $zipFileName"
    Compress-Archive -Path "$targetOutputDir\*" -DestinationPath $zipFilePath -Force
}

# --- 4. Final Cleanup ---
Write-Host "`n--- Cleaning up temporary build directory ---" -ForegroundColor Yellow
Remove-Item -Path $stagingDir -Recurse -Force

# --- Completion ---
Write-Host "`nBuild process complete!" -ForegroundColor Cyan
Write-Host "Release packages are available in: $releaseDir"