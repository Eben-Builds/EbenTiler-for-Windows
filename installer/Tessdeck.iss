#ifndef AppVersion
  #define AppVersion "1.0.1"
#endif

#define AppName "Tessdeck for Windows"
#define AppExeName "Tessdeck.exe"
#define AppPublisher "Eben-Builds"

[Setup]
AppId={{4FB0F3B1-5D64-4B6B-B1C4-7F1D05FD5102}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppCopyright=Copyright (c) 2026 Eben-Builds
DefaultDirName={localappdata}\Programs\Tessdeck
DefaultGroupName=Tessdeck
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.0
OutputDir=..\dist
OutputBaseFilename=Tessdeck-Setup
SetupIconFile=..\assets\app.ico
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
CloseApplicationsFilter=Tessdeck.exe,EbenTiler.exe
RestartApplications=no
UsePreviousAppDir=yes
UsePreviousTasks=yes
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Setup
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
#ifdef EnableSigning
SignTool=ebentiler
SignedUninstaller=yes
#endif

[Tasks]
Name: "startup"; Description: "Windows 시작 시 Tessdeck 자동 실행"; GroupDescription: "추가 옵션:"; Flags: checkedonce

[Files]
Source: "..\build\Tessdeck.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\Tessdeck.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\Tessdeck"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Tessdeck"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Tessdeck 실행"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\{#AppExeName}"; Parameters: "--startup off"; Flags: runhidden waituntilterminated; RunOnceId: "DisableTessdeckStartup"

[InstallDelete]
Type: files; Name: "{app}\EbenTiler.exe"
Type: files; Name: "{app}\EbenTiler.exe.config"
Type: files; Name: "{userprograms}\EbenTiler.lnk"
Type: files; Name: "{userprograms}\EbenTiler for Windows.lnk"

[UninstallDelete]
Type: files; Name: "{userappdata}\Tessdeck\config.ini"
Type: files; Name: "{userappdata}\Tessdeck\update-state.ini"
Type: files; Name: "{userappdata}\Tessdeck\update-badge.ini"
Type: dirifempty; Name: "{userappdata}\Tessdeck"
Type: files; Name: "{userappdata}\EbenTiler\config.ini"
Type: files; Name: "{userappdata}\EbenTiler\update-state.ini"
Type: files; Name: "{userappdata}\EbenTiler\update-badge.ini"
Type: dirifempty; Name: "{userappdata}\EbenTiler"

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if not WizardIsTaskSelected('startup') then
      RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'Tessdeck');
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'EbenTiler');
  end;
end;
