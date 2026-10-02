#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#define AppName "EbenTiler for Windows"
#define AppExeName "EbenTiler.exe"
#define AppPublisher "Eben-Builds"

[Setup]
AppId={{4FB0F3B1-5D64-4B6B-B1C4-7F1D05FD5102}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppCopyright=Copyright (c) 2026 Eben-Builds
DefaultDirName={localappdata}\Programs\EbenTiler
DefaultGroupName=EbenTiler
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.0
OutputDir=..\dist
OutputBaseFilename=EbenTiler-Setup
SetupIconFile=..\assets\app.ico
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
CloseApplicationsFilter=EbenTiler.exe
RestartApplications=no
UsePreviousAppDir=yes
UsePreviousTasks=yes
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Setup
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

[Tasks]
Name: "startup"; Description: "Windows 시작 시 EbenTiler 자동 실행"; GroupDescription: "추가 옵션:"; Flags: checkedonce

[Files]
Source: "..\build\EbenTiler.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\EbenTiler"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "EbenTiler"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#AppExeName}"; Description: "EbenTiler 실행"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\{#AppExeName}"; Parameters: "--startup off"; Flags: runhidden waituntilterminated; RunOnceId: "DisableEbenTilerStartup"

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\EbenTiler"

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if not WizardIsTaskSelected('startup') then
      RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'EbenTiler');
  end;
end;
