#define AppName "YouTube Downloader"
#define AppVersion "1.6.0b"
#define AppExeName "Youtube_donwloader.exe"
#define PublishDir "artifacts\publish\win-x64"
#define SetupIcon "Youtube donwloader\YoutubeDownloader.ico"

[Setup]
AppId={{B6D7A67C-C997-4D30-A91D-6910E4B9A4D3}
AppName={#AppName}
AppVersion={#AppVersion}
SetupIconFile={#SetupIcon}
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
OutputDir=artifacts
OutputBaseFilename=YoutubeDownloader-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent