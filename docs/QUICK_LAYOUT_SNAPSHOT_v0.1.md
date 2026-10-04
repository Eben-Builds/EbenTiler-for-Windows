# Quick Layout Snapshot v0.1

## 목표

현재 열려 있는 여러 앱 창의 위치와 크기를 한 번 저장하고, 나중에 현재 열려 있는 창들을 저장된 위치로 한 번에 복원한다.

Tessdeck v1.2.0까지는 활성 창 하나를 빠르게 배치하는 데 강점이 있다. v1.3.0에서는 이 강점을 유지하면서 "내가 지금 만들어 둔 전체 작업 배치"를 한 번에 되돌리는 기능을 추가한다.

기능명:

```text
Quick Layout Snapshot
빠른 창 배치 저장
```

---

## 왜 이 기능인가

2026년 Windows 창 관리 도구들은 단순한 한 창 배치를 넘어 "레이아웃을 저장하고 다시 불러오기"를 중요한 생산성 기능으로 제공한다.

- Microsoft PowerToys FancyZones는 사용자 정의 영역과 빠른 레이아웃 전환을 제공한다.
- Microsoft PowerToys Workspaces는 데스크톱 상태를 캡처하고 앱을 실행/재배치한다.
- DisplayFusion은 현재 열린 창의 위치와 크기를 Window Position Profile로 저장하고 다시 불러온다.

참고:
- https://learn.microsoft.com/windows/powertoys/fancyzones
- https://learn.microsoft.com/windows/powertoys/workspaces
- https://www.displayfusion.com/Features/WindowPositionProfiles/

Tessdeck은 이 기능들을 그대로 복제하지 않는다.

v0.1에서는 다음 한 가지 문제만 해결한다.

> "지금 열어 둔 창 배치를 저장해 두었다가, 흐트러졌을 때 바로 원래 위치로 돌리고 싶다."

---

## v0.1 범위

### 저장

트레이 메뉴:

```text
설정...
────────────
현재 창 배치 저장
저장된 창 배치 복원
────────────
Windows 시작할 때 함께 실행
...
```

`현재 창 배치 저장`을 누르면 현재 보이는 일반 앱 창들의 위치를 한 슬롯에 저장한다.

기존 스냅샷이 있으면 새 상태로 덮어쓴다.

### 복원

`저장된 창 배치 복원`을 누르면 현재 열려 있는 창 중 저장된 창과 일치하는 창을 찾아 저장 위치와 크기로 되돌린다.

복원 완료 알림 예:

```text
창 배치 복원 완료
6개 복원 · 1개 건너뜀
```

### CLI

자동화와 테스트를 위해 다음 명령을 제공한다.

```text
Tessdeck.exe --layout-save
Tessdeck.exe --layout-restore
Tessdeck.exe --layout-info
```

`--layout-info`는 저장된 창 수와 저장 시각 정도만 출력한다.

---

## 일부러 하지 않는 것

v0.1에서는 아래 기능을 넣지 않는다.

- 저장된 앱을 자동 실행
- 여러 개의 이름 있는 프로필
- 앱별 실행 인수
- 특정 프로젝트/파일 자동 열기
- 클라우드 동기화
- 계정
- 창 제목 저장
- 창 제목 기반 매칭
- 브라우저 탭 내용 저장
- 프로세스 전체 경로 저장
- 모니터 연결 감지 후 자동 복원

이 영역까지 들어가면 PowerToys Workspaces와 같은 별도 제품 수준의 복잡도가 된다.

v1.3.0은 Tessdeck답게 "이미 열린 창을 빠르게 되돌리는 기능"까지만 맡는다.

---

## 개인정보 원칙

Quick Layout Snapshot은 로컬 전용이다.

저장 금지:

```text
창 제목
문서 이름
브라우저 페이지 제목
URL
명령줄 인수
파일 경로
프로세스 전체 경로
창 내부 내용
```

저장 허용:

```text
프로세스 실행 파일 이름
Windows 창 클래스 이름
동일 앱 창의 순서 번호
모니터 장치 이름
정규화된 위치와 크기
최대화 여부
저장 시각
```

예:

```ini
[Window0]
Process=chrome
Class=Chrome_WidgetWin_1
Instance=0
Monitor=\\.\DISPLAY1
X=0
Y=0
Width=5000
Height=10000
Maximized=false
```

좌표는 0~10000 기준 상대값으로 저장한다.

즉 `Width=5000`은 해당 모니터 작업 영역의 50%다.

이 방식은 해상도나 DPI가 달라져도 배치를 비율로 복원할 수 있다.

---

## 창 선택 기준

저장 대상:

- 현재 보이는 top-level 창
- 일반 앱 창
- 최소 크기 이상
- DWM cloaked 상태가 아닌 창

제외:

- Tessdeck 자체 창
- Windows 작업표시줄
- Desktop / WorkerW
- 메뉴 / tooltip / shadow
- 시스템 전환 UI
- child window
- tool window
- 최소화된 창

현재 `WindowManager.GetTargetWindow()`의 필터 기준을 재사용할 수 있도록:

```text
IsManageableWindow(hwnd)
```

로 공통화한다.

---

## 창 식별

창 제목을 사용하지 않는다.

기본 키:

```text
ProcessName + WindowClass + InstanceIndex
```

예:

```text
chrome + Chrome_WidgetWin_1 + 0
chrome + Chrome_WidgetWin_1 + 1
Code + Chrome_WidgetWin_1 + 0
notepad + Notepad + 0
```

동일 프로세스/동일 클래스 창이 여러 개라면 현재 Z-order 기준 순서를 사용한다.

### 알려진 제한

같은 앱의 창 여러 개가 종료/재생성되어 순서가 바뀌면 완벽하게 같은 창을 구분하지 못할 수 있다.

v0.1에서는 민감할 수 있는 창 제목을 저장하지 않는 것을 우선한다.

---

## 모니터 처리

저장할 때:

- `Screen.FromHandle(hwnd).DeviceName`
- 해당 화면 `WorkingArea`
- 창의 visual rect

를 사용한다.

복원할 때 동일한 `DeviceName` 모니터가 있으면 그 모니터에 복원한다.

저장 당시 모니터가 현재 없으면 해당 창은 건너뛴다.

다른 모니터로 강제로 보내지 않는다.

이유:

노트북 단독/도킹/원격접속처럼 화면 구성이 달라졌을 때 사용자 창을 예상하지 못한 위치로 강제 이동시키지 않기 위함이다.

---

## 최대화 창

저장 당시 최대화 상태라면:

1. 대상 모니터를 찾는다.
2. normal rect를 먼저 복원 가능한 위치로 맞춘다.
3. `SW_SHOWMAXIMIZED`를 적용한다.

최소화 창은 v0.1 저장 대상에서 제외한다.

---

## 파일

새 파일:

```text
src/LayoutSnapshot.cs
```

역할:

- 현재 관리 가능한 top-level 창 열거
- 스냅샷 저장
- 스냅샷 읽기
- 현재 열린 창과 매칭
- 위치/크기 복원
- 결과 통계 반환

저장 위치:

```text
%APPDATA%\Tessdeck\quick-layout.ini
```

기존 `config.ini`와 분리한다.

설정을 초기화해도 snapshot을 지울지 여부는 별도로 결정한다.

---

## 필요한 Native API

`src/Native.cs`에 최소 추가:

```text
EnumWindows
GetWindowThreadProcessId
```

기존 API 재사용:

```text
IsWindow
IsWindowVisible
GetWindowLongSafe
GetClassName
DwmGetWindowAttribute
GetWindowRect
SetWindowPos
ShowWindow
```

---

## WindowManager 변경

현재 `GetTargetWindow()` 안에 있는 필터를 공통 메서드로 분리한다.

예:

```csharp
public static bool IsManageableWindow(IntPtr hwnd)
```

`GetTargetWindow()`는:

```text
GetForegroundWindow
→ IsManageableWindow
```

만 사용하도록 단순화한다.

`LayoutSnapshot`도 같은 필터를 사용한다.

창 이동은 새 구현을 만들지 않고 기존:

```text
WindowManager.GetVisualRect
WindowManager.MoveTo
```

를 사용한다.

---

## UI

v0.1에서는 새 설정 페이지를 만들지 않는다.

트레이 메뉴에 두 항목만 추가한다.

```text
현재 창 배치 저장
저장된 창 배치 복원
```

스냅샷이 없으면 복원 항목은 비활성화한다.

저장 성공:

```text
창 배치 저장 완료
7개의 창 위치를 저장했습니다.
```

복원 성공:

```text
창 배치 복원 완료
6개 복원 · 1개 건너뜀
```

---

## 기본 단축키

v0.1 첫 구현에서는 기본 전역 단축키를 추가하지 않는다.

이유:

- 기존 21개 단축키와 충돌 공간을 더 늘리지 않는다.
- 저장 동작은 실수로 눌러 기존 snapshot을 덮어쓰면 안 된다.
- 실제 사용 흐름을 먼저 검증한다.

CLI를 제공하므로 사용자는 PowerShell, AutoHotkey 또는 다른 자동화 도구에서 원하는 키에 연결할 수 있다.

실사용 검증 후 v1.4.0에서 별도 configurable hotkey를 검토한다.

---

## 안전장치

저장 시 0개 창이면 기존 snapshot을 덮어쓰지 않는다.

복원 시:

- snapshot 파일이 없으면 아무 것도 변경하지 않는다.
- 현재 존재하지 않는 앱은 건너뛴다.
- 저장 모니터가 없으면 건너뛴다.
- 이동 실패 창은 다른 창 복원을 중단시키지 않는다.
- 관리자 권한 창처럼 조작할 수 없는 창은 실패 건수에 포함한다.

전체 복원은 best-effort 방식이다.

---

## 테스트

### 단위/구조 검증

- snapshot 파일 파싱
- 상대 좌표 encode/decode
- 0~10000 clamp
- 동일 Process/Class instance 번호
- invalid/partial snapshot 안전 무시

### 실제 Windows 테스트

새 스크립트:

```text
tools/verify-layout-snapshot.ps1
```

시나리오:

1. 검증용 창 3개 생성
2. 각각 서로 다른 위치에 배치
3. `--layout-save`
4. 세 창 위치를 임의로 변경
5. `--layout-restore`
6. 원래 위치와 오차 ±2px 이내인지 확인

### 멀티모니터

CI runner는 보통 단일 모니터이므로 실제 PC에서 별도 검증한다.

- 2개 이상 모니터
- 서로 다른 DPI
- 저장 → 창 이동 → 복원
- 저장한 모니터 하나를 제거한 상태에서 restore
- 없는 모니터의 창은 안전하게 skip

---

## v1.3.0 완료 기준

- [ ] 현재 보이는 앱 창 snapshot 저장
- [ ] 창 제목/문서명/URL 저장하지 않음
- [ ] snapshot 로컬 파일 저장
- [ ] 현재 열린 창 복원
- [ ] resolution/DPI 변화에도 상대 좌표 복원
- [ ] 없는 창/없는 모니터 안전 skip
- [ ] 트레이 저장/복원 메뉴
- [ ] CLI save/restore/info
- [ ] 실제 Windows 3-window save/restore CI
- [ ] 기존 hotkey/layout 회귀 테스트 PASS
- [ ] installer smoke test PASS
- [ ] 실제 멀티모니터 PC 검증

---

## 이후 확장 후보

v0.1이 안정된 뒤에만 검토한다.

1. 이름 있는 여러 snapshot
2. snapshot별 단축키
3. 자동 저장
4. 모니터 연결/해제 감지
5. 앱 실행까지 포함하는 Workspace
6. 앱별 매칭 규칙

이 순서를 지키면 Tessdeck이 가벼운 window manager에서 갑자기 무거운 workspace orchestrator로 변하는 것을 막을 수 있다.
