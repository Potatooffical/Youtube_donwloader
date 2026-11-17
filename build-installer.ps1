$ErrorActionPreference = 'Stop'

# Determine the repository root - use script path if available, otherwise use current directory
if ($MyInvocation.MyCommand.Path) {
    $repositoryRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
} else {
    $repositoryRoot = Get-Location
}

$projectPath = Join-Path $repositoryRoot 'Youtube donwloader\Youtube donwloader.csproj'
$projectDirectory = Split-Path -Parent $projectPath
$publishDirectory = Join-Path $repositoryRoot 'artifacts\publish\win-x64'
$installerScript = Join-Path $repositoryRoot 'installer.iss'
$compiler = (Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue).Source

if (-not $compiler) {
    $defaultCompiler = Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'
    if (Test-Path $defaultCompiler) {
        $compiler = $defaultCompiler
    }
}

if (-not $compiler) {
    $userCompiler = Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'
    if (Test-Path $userCompiler) {
        $compiler = $userCompiler
    }
}

if (-not $compiler) {
    throw 'Inno Setup 6 is required to create the installer. Install it from https://jrsoftware.org/isinfo.php, then run this script again.'
}

& dotnet publish $projectPath --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output $publishDirectory
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$installedAppIcon = Join-Path $projectDirectory 'InstalledApp.ico'
if (-not (Test-Path $installedAppIcon)) {
    throw "Installed app icon was not found: $installedAppIcon"
}
Copy-Item $installedAppIcon $publishDirectory -Force

$toolDirectories = @(
    (Join-Path $projectDirectory 'bin\Release\net8.0-windows'),
    (Join-Path $projectDirectory 'bin\Debug\net8.0-windows')
)
$toolDirectory = $toolDirectories | Where-Object {
    (Test-Path (Join-Path $_ 'yt-dlp.exe')) -and (Test-Path (Join-Path $_ 'ffmpeg.exe'))
} | Select-Object -First 1

if (-not $toolDirectory) {
    throw 'yt-dlp.exe and ffmpeg.exe were not found in the project build output. Add both tools and run this script again.'
}

Copy-Item (Join-Path $toolDirectory 'yt-dlp.exe') $publishDirectory -Force
Copy-Item (Join-Path $toolDirectory 'ffmpeg.exe') $publishDirectory -Force

# Check for Deno executable
$denoExe = Join-Path $toolDirectory 'deno.exe'
if (Test-Path $denoExe) {
    Copy-Item $denoExe $publishDirectory -Force
}

$ffmpegLicense = Join-Path $toolDirectory 'ffmpeg\LICENSE.md'
$ffmpegCopying = Join-Path $toolDirectory 'ffmpeg\COPYING.GPLv3'
if (Test-Path $ffmpegLicense) {
    Copy-Item $ffmpegLicense (Join-Path $publishDirectory 'FFmpeg-LICENSE.md') -Force
}
if (Test-Path $ffmpegCopying) {
    Copy-Item $ffmpegCopying (Join-Path $publishDirectory 'FFmpeg-COPYING-GPLv3.txt') -Force
}

Push-Location $repositoryRoot
try {
    & $compiler $installerScript
    $installerExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($installerExitCode -ne 0) {
    throw "Inno Setup failed with exit code $installerExitCode."
}

Write-Host "Installer created: $(Join-Path $repositoryRoot 'artifacts\YoutubeDownloader-Setup.exe')"
