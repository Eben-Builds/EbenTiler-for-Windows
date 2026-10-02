# EbenTiler 설치

## 일반 사용자

`EbenTiler-Setup.exe`를 더블클릭하고 설치 안내에 따라 진행하면 됩니다.

- 관리자 권한이 필요하지 않습니다.
- `%LOCALAPPDATA%\Programs\EbenTiler`에 설치됩니다.
- 시작 메뉴에 `EbenTiler`이 등록됩니다.
- 설치 화면에서 Windows 시작 시 자동 실행 여부를 선택할 수 있습니다.
- 설치가 끝나면 바로 EbenTiler를 실행할 수 있습니다.
- 제거는 Windows **설정 > 앱 > 설치된 앱 > EbenTiler for Windows > 제거**에서 할 수 있습니다.

> 코드 서명 인증서로 서명하지 않은 배포본은 Windows SmartScreen에서 게시자를 확인할 수 없다는 경고가 표시될 수 있습니다.

## 인스톨러 만들기

Windows에서 처음 한 번만 Inno Setup 7을 설치합니다.

```powershell
winget install --id JRSoftware.InnoSetup.7 -e -s winget -i
```

그 다음 저장소 루트에서 실행합니다.

```powershell
powershell -ExecutionPolicy Bypass -File build-installer.ps1
```

Inno Setup이 설치되어 있지 않다면 다음 명령으로 설치와 빌드를 한 번에 진행할 수도 있습니다.

```powershell
powershell -ExecutionPolicy Bypass -File build-installer.ps1 -InstallTools
```

결과물:

```text
dist\EbenTiler-Setup.exe
dist\EbenTiler-Setup.exe.sha256
```

지인에게는 `EbenTiler-Setup.exe` 파일만 전달하면 됩니다.

## 코드 서명

코드 서명 인증서가 없으면 기존처럼 unsigned 인스톨러가 정상 생성됩니다.
인증서를 준비한 뒤에는 GitHub Actions가 `EbenTiler.exe`, 설치 프로그램, 제거 프로그램을 자동으로 Authenticode 서명합니다.

GitHub 저장소의 **Settings > Secrets and variables > Actions**에서 다음 Repository secrets 두 개를 등록합니다.

- `EBENTILER_SIGNING_PFX_BASE64`: PFX 인증서 파일을 Base64 문자열로 변환한 값
- `EBENTILER_SIGNING_PFX_PASSWORD`: PFX 비밀번호

PowerShell에서 PFX를 Base64로 변환하는 예:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\path\to\certificate.pfx")) | Set-Clipboard
```

PFX 파일 자체는 저장소에 커밋하지 마세요. `.gitignore`에서 `*.pfx`, `*.p12`를 차단합니다.

인증서 secrets가 설정되면 GitHub Actions 빌드에서 SHA-256 Authenticode 서명과 RFC 3161 타임스탬프를 적용하고 서명을 검증합니다.
