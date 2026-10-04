#ifndef AppVersion
  #define AppVersion "1.2.0"
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
WizardImageFile=..\assets\installer-large.png
WizardSmallImageFile=..\assets\installer-small.png
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
CloseApplicationsFilter=Tessdeck.exe,EbenTiler.exe
RestartApplications=no
UsePreviousAppDir=not LegacyDefaultInstallPresent
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
Name: "startup"; Description: "Windows 시작 시 Tessdeck 자동 실행"; GroupDescription: "추가 옵션:"

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
Type: files; Name: "{localappdata}\Programs\EbenTiler\EbenTiler.exe"
Type: files; Name: "{localappdata}\Programs\EbenTiler\EbenTiler.exe.config"
Type: files; Name: "{localappdata}\Programs\EbenTiler\LICENSE"
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
var
  HadPreviousInstall: Boolean;
  HadStartup: Boolean;

function LegacyDefaultInstallPresent: Boolean;
begin
  Result := FileExists(ExpandConstant('{localappdata}\Programs\EbenTiler\EbenTiler.exe'));
end;

function StartupValueExists(const ValueName: String): Boolean;
var
  ValueData: String;
begin
  Result := RegQueryStringValue(
    HKCU,
    'Software\Microsoft\Windows\CurrentVersion\Run',
    ValueName,
    ValueData);
end;

function InitializeSetup(): Boolean;
begin
  HadPreviousInstall :=
    FileExists(ExpandConstant('{localappdata}\Programs\EbenTiler\EbenTiler.exe')) or
    FileExists(ExpandConstant('{localappdata}\Programs\Tessdeck\Tessdeck.exe'));

  HadStartup :=
    StartupValueExists('EbenTiler') or
    StartupValueExists('Tessdeck');

  Result := True;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if HadPreviousInstall and (CurPageID = wpSelectTasks) then
  begin
    if HadStartup then
      WizardSelectTasks('startup')
    else
      WizardSelectTasks('!startup');
  end;
end;

procedure CopyLegacyFileIfNeeded(const FileName: String);
var
  LegacyDir: String;
  NewDir: String;
  SourcePath: String;
  DestPath: String;
begin
  LegacyDir := ExpandConstant('{userappdata}\EbenTiler');
  NewDir := ExpandConstant('{userappdata}\Tessdeck');
  SourcePath := LegacyDir + '\' + FileName;
  DestPath := NewDir + '\' + FileName;

  if FileExists(SourcePath) and (not FileExists(DestPath)) then
  begin
    ForceDirectories(NewDir);
    if CopyFile(SourcePath, DestPath, True) then
      DeleteFile(SourcePath);
  end;
end;

procedure MigrateLegacyConfig();
var
  LegacyDir: String;
begin
  CopyLegacyFileIfNeeded('config.ini');
  CopyLegacyFileIfNeeded('update-state.ini');
  CopyLegacyFileIfNeeded('update-badge.ini');

  LegacyDir := ExpandConstant('{userappdata}\EbenTiler');
  RemoveDir(LegacyDir);
end;

procedure CleanupLegacyInstallDir();
var
  LegacyDir: String;
begin
  LegacyDir := ExpandConstant('{localappdata}\Programs\EbenTiler');

  if CompareText(LegacyDir, ExpandConstant('{app}')) <> 0 then
  begin
    DeleteFile(LegacyDir + '\unins000.exe');
    DeleteFile(LegacyDir + '\unins000.dat');
    RemoveDir(LegacyDir);
  end;
end;

procedure ApplyStartupPreference();
var
  EnableStartup: Boolean;
  RunValue: String;
begin
  if HadPreviousInstall and WizardSilent then
    EnableStartup := HadStartup
  else
    EnableStartup := WizardIsTaskSelected('startup');

  RunValue := '"' + ExpandConstant('{app}\{#AppExeName}') + '"';

  if EnableStartup then
    RegWriteStringValue(
      HKCU,
      'Software\Microsoft\Windows\CurrentVersion\Run',
      'Tessdeck',
      RunValue)
  else
    RegDeleteValue(
      HKCU,
      'Software\Microsoft\Windows\CurrentVersion\Run',
      'Tessdeck');

  RegDeleteValue(
    HKCU,
    'Software\Microsoft\Windows\CurrentVersion\Run',
    'EbenTiler');
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    MigrateLegacyConfig();
    ApplyStartupPreference();
  end
  else if CurStep = ssDone then
  begin
    CleanupLegacyInstallDir();
  end;
end;
