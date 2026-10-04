# Tessdeck 설치

## 일반 사용자\n\n> 기존 EbenTiler 설치 사용자는 Tessdeck v1.1.0 설치 시 설정이 자동으로 이전됩니다. 기존 설치와 같은 AppId를 유지해 업그레이드 경로가 이어집니다.

`Tessdeck-Setup.exe`를 더블클릭하고 설치 안내에 따라 진행하면 됩니다.

- 관리자 권한이 필요하지 않습니다.
- `%LOCALAPPDATA%\Programs\Tessdeck`에 설치됩니다.
- 시작 메뉴에 `Tessdeck`이 등록됩니다.
- 설치 화면에서 Windows 시작 시 자동 실행 여부를 선택할 수 있습니다.
- 설치가 끝나면 바로 Tessdeck를 실행할 수 있습니다.
- 제거는 Windows **설정 > 앱 > 설치된 앱 > Tessdeck for Windows > 제거**에서 할 수 있습니다.

> 코드 서명 인증서로 서명하지 않은 개발/테스트 빌드는 Windows SmartScreen에서 게시자를 확인할 수 없다는 경고가 표시될 수 있습니다. 정식 공개 릴리스는 유효한 Authenticode 서명을 요구합니다.

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
dist\Tessdeck-Setup.exe
dist\Tessdeck-Setup.exe.sha256
```

지인에게 테스트용으로 전달하려면 `Tessdeck-Setup.exe`를 사용할 수 있습니다. 공개 배포는 코드 서명이 완료된 정식 Release를 사용합니다.

## 코드 서명

2026년의 공개 신뢰 코드서명 인증서는 새로 발급받을 때 **PFX 파일을 전제로 잡지 않습니다.**
공개 신뢰 인증서의 개인키는 일반적으로 하드웨어 토큰, HSM, 클라우드 HSM 또는 서명 서비스에서 보호됩니다.

Tessdeck는 서명 공급자와 빌드 로직을 분리합니다.
서명 공급자가 Windows 인증서 저장소/KSP를 통해 인증서를 사용할 수 있게 한 뒤 아래 환경변수에 thumbprint를 제공하면 됩니다.

```text
EBENTILER_SIGNING_CERT_SHA1
```

정식 릴리스에서는 `.github/workflows/release.yml`이 `tools\prepare-code-signing.ps1 -RequireSigning`을 실행합니다.
유효한 서명 신원이 준비되지 않으면 릴리스는 게시되지 않습니다.

기존에 보유한 export 가능한 PFX가 있을 경우에만 호환 경로로 다음 GitHub Repository secrets를 사용할 수 있습니다.

- `EBENTILER_SIGNING_PFX_BASE64`
- `EBENTILER_SIGNING_PFX_PASSWORD`

이 PFX 방식은 **새 공개 인증서 구매의 권장 기준이 아닙니다.**
PFX/P12 파일 자체는 저장소에 커밋하지 마세요. `.gitignore`에서 `*.pfx`, `*.p12`를 차단합니다.

코드 서명 공급자 선택, SmartScreen 동작, OV/EV 차이, SignPath 및 Store 경로는 다음 문서를 먼저 확인하세요.

```text
docs\CODE_SIGNING.md
```

정식 릴리스 검증:

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-release.ps1 -RequireCodeSigning
```

정식 릴리스는 `Tessdeck.exe`, 설치 프로그램과 제거 프로그램을 Authenticode 서명하고, 설치 파일 SHA-256까지 다시 확인한 뒤 게시합니다.
