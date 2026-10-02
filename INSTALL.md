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
