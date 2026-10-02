# RumiFlow for Windows

단축키로 창을 화면 절반·사분면·3분할에 순식간에 붙여 주는 Windows 상주 프로그램.
macOS의 [Rectangle](https://rectangleapp.com/)에서 익숙한 키보드 중심 창 배치 경험을 Windows에서 제공하는 독립적인 프로젝트다.

> RumiFlow는 Rectangle 또는 Rectangle Pro의 공식 Windows 버전이 아니다.

- 별도 런타임 설치 필요 없음 (Windows 11/10 에 기본 포함된 .NET Framework 4.8 사용)
- 실행 파일 하나, 약 40KB
- 알림 영역에 상주, 설정 창에서 단축키 자유롭게 변경

## 기본 단축키

| 단축키 | 하는 일 |
| --- | --- |
| `Ctrl + Alt + ←` | 왼쪽 절반 |
| `Ctrl + Alt + →` | 오른쪽 절반 |
| `Ctrl + Alt + ↑` | 위쪽 절반 |
| `Ctrl + Alt + ↓` | 아래쪽 절반 |
| `Ctrl + Alt + U` | 왼쪽 위 1/4 |
| `Ctrl + Alt + I` | 오른쪽 위 1/4 |
| `Ctrl + Alt + J` | 왼쪽 아래 1/4 |
| `Ctrl + Alt + K` | 오른쪽 아래 1/4 |
| `Ctrl + Alt + D` / `F` / `H` | 왼쪽 / 가운데 / 오른쪽 1/3 |
| `Ctrl + Alt + E` / `T` | 왼쪽 2/3 / 오른쪽 2/3 |
| `Ctrl + Alt + Enter` | 전체 화면(최대화) |
| `Ctrl + Alt + Shift + ↑` | 세로만 최대 (가로 폭 유지) |
| `Ctrl + Alt + C` | 화면 가운데로 |
| `Ctrl + Alt + =` / `-` | 크기 키우기 / 줄이기 |
| `Ctrl + Alt + Backspace` | 배치 전 원래 크기로 복원 |
| `Ctrl + Alt + Shift + →` / `←` | 다음 / 이전 모니터로 이동 |

`Ctrl + 방향키`를 쓰지 않는 이유: 거의 모든 텍스트 편집기와 브라우저에서 단어 단위 커서 이동에 쓰이는 조합이라,
전역 단축키로 뺏으면 타이핑이 망가진다. 설정 창에서 원하는 조합으로 바꿀 수 있다.

### 같은 키를 연달아 누르면 폭이 바뀐다

`Ctrl + Alt + ←` 를 세 번 연달아 누르면 왼쪽 **1/2 → 1/3 → 2/3** 순으로 폭이 바뀐다.
2초 안에 다시 누를 때만 순환하고, 그 뒤에는 다시 1/2 부터 시작한다.
설정 창에서 끌 수 있다.

## 빌드

.NET SDK 를 설치할 필요 없다. Windows 에 기본으로 들어 있는 .NET Framework 4.8 컴파일러로 바로 빌드한다.

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

결과물은 `build\RumiFlow.exe` 하나다. 원하는 곳에 두고 실행하면 된다.

## 설치

```powershell
powershell -ExecutionPolicy Bypass -File install.ps1
```

관리자 권한이 필요 없다. 현재 사용자 계정에만 설치된다.

- 프로그램: `%LOCALAPPDATA%\Programs\RumiFlow\RumiFlow.exe`
- 시작 메뉴 바로 가기 등록
- 윈도우 시작 시 자동 실행 등록 (`-NoStartup` 을 붙이면 등록하지 않는다)
- 단축키 충돌이 있으면 어떤 것이 겹치는지 알려 준다
- 설치 후 바로 실행된다

제거는 이렇게 한다. 설정까지 남기고 싶으면 `-KeepConfig` 를 붙인다.

```powershell
powershell -ExecutionPolicy Bypass -File install.ps1 -Uninstall
```

## 실행

설치했다면 이미 실행 중이다. 설치 없이 그냥 써 보려면:

```powershell
build\RumiFlow.exe
```

알림 영역(작업표시줄 오른쪽)에 아이콘이 생긴다. **아이콘이 안 보이면 `∧` 를 눌러 숨김 목록을 확인**하면 된다.
Windows 는 처음 보는 프로그램의 아이콘을 기본으로 숨김 처리한다.

아이콘을 **왼쪽으로 누르든 오른쪽으로 누르든** 메뉴가 뜬다.

- `단축키 설정...` : 설정 창
- `Windows 시작할 때 함께 실행` : 자동 시작 등록/해제. 현재 켜져 있으면 앞에 체크 표시가 붙는다.
- `종료`

메뉴를 열 때마다 실제 등록 상태를 다시 읽어 체크를 맞추므로, 설정을 다른 데서 바꿔도 표시가 어긋나지 않는다.

## 명령줄에서 쓰기

스크립트나 다른 도구에서 창 배치를 시킬 수도 있다.

```powershell
RumiFlow.exe --apply LeftHalf                 # 지금 활성 창을 왼쪽 절반에
RumiFlow.exe --apply TopRight --hwnd 0x3B078E # 창을 직접 지정
RumiFlow.exe --info                           # 활성 창 위치와 화면 작업 영역 확인
RumiFlow.exe --list                           # 쓸 수 있는 명령 목록
RumiFlow.exe --settings                       # 설정 창만 열기
RumiFlow.exe --check                          # 단축키가 다른 프로그램과 겹치는지 확인
RumiFlow.exe --startup on|off|status          # 윈도우 시작 시 자동 실행 등록/해제/확인
RumiFlow.exe --out result.txt --info          # 결과를 파일로도 저장
```

`--check` 는 이런 식으로 알려 준다. 단축키가 안 먹을 때 제일 먼저 확인하면 된다.

```text
total=21
assigned=21
unassigned=0
failed=1
conflict=오른쪽 1/3 (Ctrl + Alt + H)
```

`RumiFlow.exe` 는 창 프로그램이라 표준 출력이 파이프로 잡히지 않을 때가 있다.
스크립트에서 결과를 읽어야 하면 `--out <파일>` 을 함께 쓰면 된다.

## 설정 창

알림 영역 아이콘을 눌러 `단축키 설정...` 을 고르면 열린다.

- 왼쪽 목록에서 기능을 고르고, 아래 입력칸에 원하는 키 조합을 **실제로 눌러** 지정한다.
- 오른쪽 **미리보기**에 고른 기능이 창을 화면 어디에 놓는지 그림으로 나온다.
  크기 조절이나 모니터 이동처럼 자리만으로 설명이 안 되는 것은 점선(바뀌기 전)과 화살표로 함께 보여 준다.
- 같은 조합을 이미 다른 기능이 쓰고 있으면 물어보고 그쪽을 비운다.
- 보조키(Ctrl/Alt/Shift/Win) 없는 조합은 막는다. 그렇게 등록하면 다른 프로그램에서 그 키를 아예 못 쓰게 된다.

## 아이콘

`assets\app.ico` 를 빌드 때 실행 파일에 박는다. 16 / 24 / 32 / 48 / 64 / 128 / 256 픽셀이 한 파일에 들어 있고,
**크기마다 따로 그렸다.** 큰 그림 하나를 줄여 쓰면 알림 영역(16픽셀)에서 뭉개지기 때문이다.

모양은 창 두 장이 겹친 것이다. 창을 정리하기 전 모습을 그대로 아이콘으로 삼았다.
색은 파랑과 흰색만 쓴다. 검정 테두리는 어두운 작업표시줄에서 배경에 묻혀 사라지기 때문이다.

```powershell
powershell -ExecutionPolicy Bypass -File tools\make-appicon.ps1   # 아이콘 다시 만들기
```

## 설정 파일

`%APPDATA%\RumiFlow\config.ini` 에 저장된다. 직접 편집해도 된다.

```ini
[Hotkeys]
LeftHalf=Ctrl+Alt+Left
TopLeft=Ctrl+Alt+U
Maximize=Ctrl+Alt+Enter

[Options]
Gap=0            ; 창 사이와 화면 가장자리에 남길 여백(픽셀)
CycleHalves=true ; 같은 단축키 연타 시 1/2 -> 1/3 -> 2/3 순환
```

값을 비워 두면 그 기능의 단축키는 등록하지 않는다.

## 검증

실제 창을 띄워 놓고 자동으로 확인하는 스크립트가 들어 있다.

```powershell
# 배치 계산이 실제 창 위치와 맞는지 (13가지 배치 + 최대화/복원/크기조절/가운데정렬)
powershell -ExecutionPolicy Bypass -File tools\verify.ps1

# 전역 단축키를 실제 키 입력으로 눌러 보고 확인
powershell -ExecutionPolicy Bypass -File tools\verify-hotkeys.ps1

# 설정 파일 반영, 여백, 세로만 최대, 모니터 이동, 중복 실행 방지, 자동 실행 등록
powershell -ExecutionPolicy Bypass -File tools\verify-more.ps1

# 설정 창을 실제 마우스 클릭과 키 입력으로 조작해서 저장까지 확인
powershell -ExecutionPolicy Bypass -File tools\verify-settings.ps1

# 알림 영역 아이콘을 눌러 메뉴를 띄우고 체크 표시와 토글 동작 확인
powershell -ExecutionPolicy Bypass -File tools\verify-tray-menu.ps1

# 설정 창 미리보기가 기능마다 제대로 그려지는지 캡처
powershell -ExecutionPolicy Bypass -File tools\capture-preview.ps1

# 설정 창과 알림 영역 화면 캡처
powershell -ExecutionPolicy Bypass -File tools\capture-ui.ps1

# 창 두 개를 좌우로 붙인 화면 캡처
powershell -ExecutionPolicy Bypass -File tools\capture-demo.ps1
```

## 알아 둘 점

- **관리자 권한으로 실행 중인 창은 옮길 수 없다.** Windows 가 낮은 권한 프로그램이 높은 권한 창을 조작하는 것을 막기 때문이다.
  그런 창까지 배치하려면 `RumiFlow.exe` 도 관리자 권한으로 실행해야 한다.
- 다른 프로그램이 이미 선점한 단축키는 등록에 실패한다. 이때는 시작 직후 알림으로 어떤 것이 실패했는지 알려 주고,
  `RumiFlow.exe --check` 로 언제든 다시 확인할 수 있다. 설정 창에서 다른 조합으로 바꾸면 된다.
  게임 런처나 독(dock) 프로그램이 `Ctrl+Alt+숫자`, `Ctrl+Alt+G` 같은 조합을 자주 가져간다.
- 창 위치는 DWM 이 알려 주는 **실제로 보이는 테두리** 기준으로 맞춘다. Windows 10/11 창 바깥의 투명한 여백만큼 어긋나 보이는 문제가 없다.
- 모니터마다 배율이 다른 환경을 위해 per-monitor DPI 인식으로 동작한다.

## 구조

| 파일 | 하는 일 |
| --- | --- |
| `src/Native.cs` | Win32 API 선언, DPI 인식 설정 |
| `src/SnapAction.cs` | 배치 명령 목록, 한국어 이름, 기본 단축키 |
| `src/Hotkey.cs` | 단축키 문자열 해석과 표시 |
| `src/Config.cs` | 설정 파일 읽기/쓰기 |
| `src/WindowManager.cs` | 창 찾기, 위치 계산, 이동, 원래 크기 기억 |
| `src/HotkeyManager.cs` | 전역 단축키 등록과 수신 |
| `src/TrayApp.cs` | 알림 영역 상주, 메뉴 |
| `src/SettingsForm.cs` | 설정 창 |
| `src/Program.cs` | 진입점, 명령줄 모드 |
