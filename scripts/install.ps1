# Installs the latest QueryCat (qcat) release from https://github.com/krasninja/querycat/releases/latest
#
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -Command "irm https://raw.githubusercontent.com/krasninja/querycat/main/scripts/install.ps1 | iex"
#
# - Supported: Windows x64 (Windows on ARM64 uses the x64 build via emulation). Linux/macOS: use scripts/install.sh.
# - Default install directory: %LOCALAPPDATA%\Programs\QueryCat; when elevated: %ProgramFiles%\QueryCat.
#   Warns if the install directory is not on PATH.
# - Installs qcat.exe and qcat-plugins-proxy.exe; re-running upgrades in place.
# - Environment: QCAT_VERSION (default: latest), QCAT_INSTALL_DIR (overrides target directory).
# - Requires: PowerShell 5.1 or newer.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

$Repo = 'krasninja/querycat'
$ReleasesUrl = "https://github.com/$Repo/releases"
$ApiLatestUrl = "https://api.github.com/repos/$Repo/releases/latest"
$ReleasesAtomUrl = "$ReleasesUrl.atom"

# Invoke-WebRequest is dramatically faster with the progress bar disabled (PowerShell 5.1).
$ProgressPreference = 'SilentlyContinue'

function Write-Step {
    param([Parameter(Mandatory)][string] $Message)
    Write-Host "==> $Message"
}

function Fail {
    param([Parameter(Mandatory)][string] $Message)
    throw $Message
}

# --- Platform detection -----------------------------------------------------

$IsWindowsOs = if ($PSVersionTable.PSVersion.Major -ge 6) { $IsWindows } else { $true }
if (-not $IsWindowsOs) {
    Fail 'This script supports Windows only. On Linux and macOS run scripts/install.sh instead.'
}

$ProcessorArchitecture = [Environment]::GetEnvironmentVariable('PROCESSOR_ARCHITEW6432')
if (-not $ProcessorArchitecture) {
    $ProcessorArchitecture = [Environment]::GetEnvironmentVariable('PROCESSOR_ARCHITECTURE')
}
if ($ProcessorArchitecture -ne 'AMD64' -and $ProcessorArchitecture -ne 'ARM64') {
    Fail "Unsupported CPU architecture: $ProcessorArchitecture. Only x64 builds are published."
}
# Only win-x64 binaries are published; Windows on ARM64 runs them through x64 emulation.
$Rid = 'win-x64'

$Identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$Principal = New-Object Security.Principal.WindowsPrincipal($Identity)
$IsAdmin = $Principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

# --- Version resolution -----------------------------------------------------

function Get-LatestVersion {
    # Primary source: GitHub API.
    try {
        $Release = Invoke-RestMethod -Uri $ApiLatestUrl -UseBasicParsing -Headers @{ 'User-Agent' = 'querycat-install' }
        if ($Release.tag_name) {
            return $Release.tag_name.TrimStart('v')
        }
    }
    catch {
        Write-Verbose "GitHub API request failed: $($_.Exception.Message)"
    }
    # Fallback: releases Atom feed (not affected by the API rate limit).
    try {
        $Feed = (Invoke-WebRequest -Uri $ReleasesAtomUrl -UseBasicParsing -Headers @{ 'User-Agent' = 'querycat-install' }).Content
        $Match = [regex]::Match($Feed, 'releases/tag/([^"<]+)')
        if ($Match.Success) {
            return $Match.Groups[1].Value.TrimStart('v')
        }
    }
    catch {
        Write-Verbose "Atom feed request failed: $($_.Exception.Message)"
    }
    Fail 'Could not determine the latest version. Set QCAT_VERSION explicitly, e.g. $env:QCAT_VERSION = "0.19.0".'
}

# --- Installation helpers ---------------------------------------------------

function Get-InstallDir {
    $Override = [Environment]::GetEnvironmentVariable('QCAT_INSTALL_DIR')
    if ($Override) {
        return $Override
    }
    if ($IsAdmin) {
        return (Join-Path $env:ProgramFiles 'QueryCat')
    }
    return (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\QueryCat')
}

function Test-DirOnPath {
    param([Parameter(Mandatory)][string] $Dir)
    $Paths = @(
        [Environment]::GetEnvironmentVariable('Path', 'Machine'),
        [Environment]::GetEnvironmentVariable('Path', 'User'),
        $env:Path
    )
    foreach ($Entry in ($Paths | Where-Object { $_ }) -split ';') {
        if ($Entry.TrimEnd('\') -ieq $Dir.TrimEnd('\')) {
            return $true
        }
    }
    return $false
}

function Install-Binary {
    param(
        [Parameter(Mandatory)][string] $Source,
        [Parameter(Mandatory)][string] $Destination
    )
    try {
        Copy-Item -Path $Source -Destination $Destination -Force
    }
    catch {
        Fail "Cannot write '$Destination': $($_.Exception.Message). Close any running qcat instances and try again."
    }
}

# --- Main -------------------------------------------------------------------

$Version = [Environment]::GetEnvironmentVariable('QCAT_VERSION')
if ($Version) {
    $Version = $Version.TrimStart('v')
    if ($Version -notmatch '^[A-Za-z0-9.+_-]+$') {
        Fail "Invalid QCAT_VERSION: $Version."
    }
}
else {
    Write-Step 'Resolving the latest release version...'
    $Version = Get-LatestVersion
}
$Tag = "v$Version"
Write-Step "Installing QueryCat $Version ($Rid)"

$InstallDir = Get-InstallDir
try {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}
catch {
    Fail "Cannot create directory ${InstallDir}: $($_.Exception.Message)"
}

$TempDir = Join-Path ([IO.Path]::GetTempPath()) ("qcat-install-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $TempDir -Force | Out-Null
try {
    foreach ($Package in @('qcat', 'qcat-plugins-proxy')) {
        $Archive = Join-Path $TempDir "$Package-$Version-$Rid.zip"
        $Url = "$ReleasesUrl/download/$Tag/$Package-$Version-$Rid.zip"
        Write-Step "Downloading $Url"
        try {
            Invoke-WebRequest -Uri $Url -OutFile $Archive -UseBasicParsing -Headers @{ 'User-Agent' = 'querycat-install' }
        }
        catch {
            Fail "Download failed: $Url. Check that release $Tag exists and provides a $Rid asset."
        }
        $ExtractDir = Join-Path $TempDir $Package
        Expand-Archive -Path $Archive -DestinationPath $ExtractDir -Force
        $Binary = Join-Path $ExtractDir "$Package.exe"
        if (-not (Test-Path -Path $Binary -PathType Leaf)) {
            Fail "Archive $Archive does not contain the $Package.exe binary."
        }
        Install-Binary -Source $Binary -Destination (Join-Path $InstallDir "$Package.exe")
        Write-Step "Installed $(Join-Path $InstallDir "$Package.exe")"
    }
}
finally {
    Remove-Item -Path $TempDir -Recurse -Force -ErrorAction SilentlyContinue
}

if (-not (Test-DirOnPath -Dir $InstallDir)) {
    $PathScope = if ($IsAdmin) { 'Machine' } else { 'User' }
    Write-Warning "$InstallDir is not in your PATH."
    Write-Warning "Add it with: [Environment]::SetEnvironmentVariable('Path', ""$InstallDir;"" + [Environment]::GetEnvironmentVariable('Path', '$PathScope'), '$PathScope')"
}

Write-Step "QueryCat $Version installed successfully."
& (Join-Path $InstallDir 'qcat.exe') --version
Write-Step 'Try it out: qcat "SELECT 1"'
