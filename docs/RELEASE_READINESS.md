# Tessdeck v1.1.0 공개 릴리스 상태

이 문서는 EbenTiler에서 Tessdeck으로의 v1.1.0 리브랜딩, 기존 공개 Release 이력, 코드서명 전후 전환 조건을 기록합니다.

## 0. v1.1.0 Tessdeck 리브랜딩

- `v1.0.1`: EbenTiler 이름으로 공개된 마지막 Release
- `v1.1.0`: Tessdeck 이름으로 공개된 첫 Release
- 저장소 이름: `Eben-Builds/Tessdeck-for-Windows`
- 실행 파일: `Tessdeck.exe`
- 설치 파일: `Tessdeck-Setup.exe`
- 설정 위치: `%APPDATA%\Tessdeck`
- 기존 `%APPDATA%\EbenTiler` 설정은 Tessdeck 첫 실행 시 자동 복사
- 기존 Windows 시작프로그램의 `EbenTiler` 값은 Tessdeck 등록 시 정리
- 설치 AppId는 기존 값 유지: 이전 설치를 별도 앱이 아니라 업그레이드로 인식
- 내부 C# namespace `EbenTilerWindows`는 호환성과 변경 범위 최소화를 위해 유지
- v1.1.0 준비 빌드에서 `Tessdeck.exe` / `Tessdeck-Setup.exe` 생성, SHA-256, 설치/제거, CLI, 시작프로그램 smoke test를 GitHub Actions에서 통과

2026-10-05 `v1.1.0` Release가 실제 게시되었으며, 랜딩페이지 다운로드 버튼은 최신 공개 Release의 `Tessdeck-Setup.exe`와 SHA-256 파일을 직접 가리킵니다.

## 1. GitHub Rulesets

2026-10-03 기준 아래 두 Ruleset이 저장소에 **Active** 상태로 적용되어 있습니다.

### Protect main

- 이름: `Protect main`
- Enforcement status: `Active`
- Target: `Default branch`
- `Restrict deletions`: 켬
- `Block force pushes` / `Non-fast-forward`: 켬

1인 개발 흐름을 유지하기 위해 처음부터 PR 강제까지 넣지는 않습니다. 외부 기여자가 생기거나 코드서명 공급자가 더 강한 검토 정책을 요구하면 그때 `Require a pull request before merging`을 추가합니다.

### Protect release tags

- 이름: `Protect release tags`
- Enforcement status: `Active`
- Target pattern: `v*`
- `Restrict updates`: 켬
- `Restrict deletions`: 켬
- `Block force pushes` / `Non-fast-forward`: 켬

`Restrict creations`는 현재 켜지 않습니다. 공개 릴리스 workflow 자체가 저장소 owner가 만든 `vMAJOR.MINOR.PATCH` 태그만 허용하고, 태그가 `main`의 커밋인지 다시 검증합니다.

## 2. GitHub Topics

저장소 About에 다음 Topics가 적용되어 있습니다.

`windows`, `window-manager`, `window-tiling`, `productivity`, `hotkeys`, `desktop-app`, `winforms`, `dotnet-framework`, `multi-monitor`, `open-source`

버전용 Git tag와 GitHub Topics는 목적이 다릅니다. 버전 태그는 공개 릴리스 신호이고, Topics는 검색과 프로젝트 발견성을 위한 메타데이터입니다.

## 3. 코드 서명 / 공개 배포 정책

현재 정책은 다음과 같습니다.

- 일반 `main` CI: unsigned, 코드서명 비밀정보 접근 금지
- 일반 CI artifact는 공개 다운로드 경로로 사용하지 않음
- 공개 `v*` Release: 태그/버전/`main` 포함 여부, 설치/제거 smoke test, SHA-256 검증 필수
- 코드서명 신원이 있으면 Authenticode 서명과 EKU 검증까지 추가
- 코드서명 신원이 없으면 unsigned 상태와 Windows 경고 가능성을 Release와 랜딩페이지에 명확히 고지한 뒤 공개 가능
- 랜딩페이지는 GitHub 최신 공개 Release의 설치 파일만 가리킴

2026-10-03 `v1.0.1` unsigned 공개 Release가 실제 게시되었습니다. `EbenTiler-Setup.exe`와 `EbenTiler-Setup.exe.sha256`이 함께 공개되어 있으며, Release 본문에는 Authenticode 미서명 상태와 SmartScreen/알 수 없는 게시자 경고 가능성을 명시합니다.

`v1.0.0` 태그는 첫 Release 게시 시도의 기록으로 남아 있으나 Release 게시 단계가 실패해 공개 Release는 생성되지 않았습니다. 보호된 태그를 덮어쓰지 않고 `v1.0.1`로 정상 공개했습니다.

상세 내용은 `docs/CODE_SIGNING.md`를 봅니다.

### SignPath Foundation 검토

Tessdeck은 공개 MIT OSS이고 소스/빌드 스크립트를 직접 관리합니다. 앱은 텔레메트리·광고·분석 SDK·사용자 데이터 전송 기능을 사용하지 않으며, 업데이트 확인 기능은 최대 24시간에 한 번 GitHub의 공개 Release 정보만 조회합니다.

2026-10-03 신청을 제출했으며 승인 여부는 SignPath Foundation이 결정합니다. 승인 전에는 SignPath가 현재 Tessdeck의 서명을 제공하는 것처럼 표시하지 않습니다.

참고:
- https://signpath.org/terms.html
- https://docs.signpath.io/trusted-build-systems/github

## 4. 개인정보 / 계정

- GitHub 계정 MFA 사용
- 향후 로컬 Git 커밋에는 GitHub `noreply` 이메일 사용
- 기존 공개 Git 히스토리는 파괴적인 rewrite 없이 유지
- `.env`, PFX/P12/PEM/KEY 등 민감 파일은 저장소에 커밋하지 않음
- 업데이트 확인은 GitHub 공개 Release 메타데이터만 조회하며 자동 다운로드/자동 설치하지 않음
- 업데이트 확인 시각과 마지막 알림 태그만 `%APPDATA%\Tessdeck\update-state.ini`에 로컬 저장
- 새 버전이 실제 설치되기 전까지 보일 트레이 `!` 배지 상태만 `%APPDATA%\Tessdeck\update-badge.ini`에 로컬 저장

## 5. UI / DPI 최종 확인

설정 > 일반에 `시작 가이드 다시 보기`가 있고, 수동으로 다시 연 가이드는 첫 실행 전용 `다시 표시하지 않기` 상태를 변경하지 않습니다.

실제 Windows 디스플레이 배율에서 다음을 각각 확인했습니다.

- [x] 100%
- [x] 125%
- [x] 150%

각 배율에서 설정 UI, 첫 실행 가이드, 트레이 메뉴의 크기와 잘림 여부를 실제 화면에서 확인했습니다. DPI 변경 시 설정 UI 오른쪽/하단 여백과 트레이 메뉴 축소 문제를 수정한 뒤 재검증했습니다.

필요하면 아래 스크립트로 첫 실행 가이드, 설정 > 일반, 설정에서 다시 연 가이드 PNG를 다시 캡처할 수 있습니다.

```powershell
powershell -ExecutionPolicy Bypass -File tools\capture-dpi-ui.ps1
```

## 6. 설치파일 수동 확인

실제 Windows에서 `EbenTiler-Setup.exe`를 직접 실행해 다음을 확인했습니다.

- [x] 설치 완료
- [x] 설치 후 EbenTiler 실행
- [x] Windows 시작 프로그램 옵션 동작
- [x] 제거 완료

자동 CI의 install/uninstall smoke test와 별도로 실제 사용자 흐름을 수동으로 한 번 더 확인한 상태입니다.

## 7. 업데이트 전환 확인

Tessdeck은 자동 설치 대신 다음 흐름을 사용합니다.

- 최대 24시간에 한 번 GitHub 최신 Release 정보 확인
- 새 버전이 있을 때만 Windows 알림 표시
- 새 버전이 감지되면 트레이 아이콘 오른쪽 위에 주황색 `!` 배지를 지속 표시
- 트레이 메뉴에 `업데이트 있음 · vX.Y.Z` 항목 표시
- 알림 또는 메뉴 클릭 시 `설정 > 정보`로 이동
- 사용자가 `업데이트 확인` 또는 Release 페이지 열기를 직접 선택
- 자동 다운로드 / 자동 설치는 하지 않음
- 새 버전 설치 후 현재 앱 버전이 해당 Release 이상이면 `!` 배지 상태를 자동 정리

실제 Release를 만들지 않고 트레이 업데이트 배지를 눈으로 확인하려면 다음 로컬 테스트 도구를 사용합니다.

```powershell
powershell -ExecutionPolicy Bypass -File tools\test-update-badge.ps1
```

테스트가 끝나면 이전 상태를 복원합니다.

```powershell
powershell -ExecutionPolicy Bypass -File tools\test-update-badge.ps1 -Restore
```

2026-10-03 실제 Windows에서 테스트용 `v9.9.9` 상태로 트레이 `!` 배지와 `업데이트 있음 · v9.9.9` 메뉴를 확인했고, 해당 메뉴를 누르면 `설정 > 정보`로 바로 이동하는 동작까지 확인했습니다.

첫 공개 다운로드 가능 unsigned Release는 EbenTiler `v1.0.1`입니다. Tessdeck 첫 공개 버전은 `v1.1.0`으로 준비하며, 이후 코드서명이 연결되면 같은 버전 파일을 교체하지 않고 `v1.1.1` 이상으로 올려 기존 사용자가 새 버전으로 인식하도록 합니다.

## 8. v1.0.1 unsigned 공개 Release 상태

- [x] `main` Ruleset 활성화
- [x] `v*` tag Ruleset 활성화
- [x] GitHub Topics 설정
- [x] 실제 Windows 100% / 125% / 150%에서 설정 UI, 첫 실행 가이드, 트레이 메뉴 최종 확인
- [x] 실제 `EbenTiler-Setup.exe` 설치 / 실행 / 시작 프로그램 / 제거 수동 확인
- [x] 로컬에서 트레이 업데이트 `!` 배지, `업데이트 있음 · vX.Y.Z` 메뉴, `설정 > 정보` 이동 확인
- [x] SignPath 승인 전 unsigned 공개 Release 허용 정책 확정
- [x] unsigned 상태와 Windows 경고 가능성을 Release/랜딩페이지에 명확히 고지
- [x] 공개 Release workflow가 SHA-256 및 설치/제거 검증 후 unsigned 게시를 허용하도록 변경
- [x] `v1.0.1` Release workflow 실제 실행 성공
- [x] GitHub Release에 `EbenTiler-Setup.exe`와 `.sha256` 게시
- [x] 랜딩페이지 소스가 `releases/latest/download/EbenTiler-Setup.exe`를 사용하고 최신 공개 Release가 `v1.0.1`인 것 확인
- [ ] 실제 브라우저에서 랜딩페이지 `윈도우용 다운로드` 클릭 후 `EbenTiler-Setup.exe` 다운로드 확인
- [ ] 실제 Windows 앱의 `설정 > 정보 > 업데이트 확인`에서 공개 Release 상태 확인

## 8. v1.1.0 Tessdeck 공개 Release 상태

- [x] `v1.1.0` 태그가 main의 검증된 커밋을 가리킴
- [x] Release workflow에서 버전 일치 확인
- [x] `Tessdeck-Setup.exe` 빌드 성공
- [x] 설치/제거 smoke test 성공
- [x] SHA-256 검증 성공
- [x] GitHub Release 게시 성공
- [x] `Tessdeck-Setup.exe` 및 `Tessdeck-Setup.exe.sha256` 첨부
- [x] 랜딩페이지 다운로드 경로를 Tessdeck 최신 Release 직링크로 전환

## 9. v1.1.0 이후 코드서명 승인 시

SignPath 또는 다른 공개 코드서명 수단이 연결되면 다음 Release에서:

- 앱 버전을 `1.1.1` 이상으로 올림
- Authenticode 서명 적용
- `tools/verify-release.ps1 -RequireCodeSigning` 통과
- signed 설치 파일 게시
- 랜딩페이지의 `코드 서명 준비 중` 안내 제거
- 기존 `v1.0.1` EbenTiler 및 `v1.1.0` Tessdeck 사용자에게 업데이트 알림이 정상 동작하는지 확인
