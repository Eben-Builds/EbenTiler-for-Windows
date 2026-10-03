# EbenTiler v1.0.0 출시 준비

이 문서는 공개 OSS 상태의 EbenTiler를 정식 배포하기 전에 확인할 항목만 정리합니다.

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

`Restrict creations`는 현재 켜지 않습니다. 정식 릴리스 workflow 자체가 저장소 owner가 만든 `vMAJOR.MINOR.PATCH` 태그만 허용하고, 태그가 `main`의 커밋인지 다시 검증합니다.

## 2. GitHub Topics

저장소 About에 다음 Topics가 적용되어 있습니다.

`windows`, `window-manager`, `window-tiling`, `productivity`, `hotkeys`, `desktop-app`, `winforms`, `dotnet-framework`, `multi-monitor`, `open-source`

버전용 Git tag와 GitHub Topics는 목적이 다릅니다. `v1.0.0`은 정식 릴리스 신호이고, Topics는 검색과 프로젝트 발견성을 위한 메타데이터입니다.

## 3. 코드 서명

현재 정책은 다음과 같습니다.

- 일반 `main` CI: unsigned, 코드서명 비밀정보 접근 금지
- 랜딩페이지: unsigned 설치파일 직접 호스팅 금지
- 정식 `v*` Release: Authenticode 서명 필수
- 서명 후 설치/제거, EKU, SHA-256까지 검증한 뒤에만 Release 게시

상세 내용은 `docs/CODE_SIGNING.md`를 봅니다.

### SignPath Foundation 검토

EbenTiler는 공개 MIT OSS이고 소스/빌드 스크립트를 직접 관리합니다. 앱은 텔레메트리·광고·분석 SDK·사용자 데이터 전송 기능을 사용하지 않으며, 업데이트 확인 기능은 최대 24시간에 한 번 GitHub의 공개 Release 정보만 조회합니다.

2026-10-03 신청을 제출했으며 승인 여부는 SignPath Foundation이 결정합니다. 승인 전에는 SignPath가 현재 EbenTiler의 서명을 제공하는 것처럼 표시하지 않습니다.

참고:
- https://signpath.org/terms.html
- https://docs.signpath.io/trusted-build-systems/github

## 4. 개인정보 / 계정

- GitHub 계정 MFA 사용
- 향후 로컬 Git 커밋에는 GitHub `noreply` 이메일 사용
- 기존 공개 Git 히스토리는 파괴적인 rewrite 없이 유지
- `.env`, PFX/P12/PEM/KEY 등 민감 파일은 저장소에 커밋하지 않음
- 업데이트 확인은 GitHub 공개 Release 메타데이터만 조회하며 자동 다운로드/자동 설치하지 않음
- 업데이트 확인 시각과 마지막 알림 태그만 `%APPDATA%\EbenTiler\update-state.ini`에 로컬 저장
- 새 버전이 실제 설치되기 전까지 보일 트레이 `!` 배지 상태만 `%APPDATA%\EbenTiler\update-badge.ini`에 로컬 저장

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

EbenTiler는 자동 설치 대신 다음 흐름을 사용합니다.

- 최대 24시간에 한 번 GitHub 최신 Release 정보 확인
- 새 버전이 있을 때만 Windows 알림 표시
- 새 버전이 감지되면 트레이 아이콘 오른쪽 위에 주황색 `!` 배지를 지속 표시
- 트레이 메뉴에 `업데이트 있음 · vX.Y.Z` 항목 표시
- 알림 또는 메뉴 클릭 시 `설정 > 정보`로 이동
- 사용자가 `업데이트 확인` 또는 Release 페이지 열기를 직접 선택
- 자동 다운로드 / 자동 설치는 하지 않음
- 새 버전 설치 후 현재 앱 버전이 해당 Release 이상이면 `!` 배지 상태를 자동 정리

현재 GitHub 정식 Release가 없으므로 `업데이트 확인`을 누르면 `아직 공개된 정식 릴리스가 없습니다.`가 정상입니다.

실제 Release를 만들지 않고 트레이 업데이트 배지를 눈으로 확인하려면 다음 로컬 테스트 도구를 사용합니다.

```powershell
powershell -ExecutionPolicy Bypass -File tools\test-update-badge.ps1
```

테스트가 끝나면 이전 상태를 복원합니다.

```powershell
powershell -ExecutionPolicy Bypass -File tools\test-update-badge.ps1 -Restore
```

2026-10-03 실제 Windows에서 테스트용 `v9.9.9` 상태로 트레이 `!` 배지와 `업데이트 있음 · v9.9.9` 메뉴를 확인했고, 해당 메뉴를 누르면 `설정 > 정보`로 바로 이동하는 동작까지 확인했습니다.

중요: 현재 unsigned 빌드의 제품 버전은 `1.0.0.0`입니다. 이 버전을 지인에게 이미 배포했다면 첫 signed Release를 같은 `v1.0.0`으로 내지 말고 `v1.0.1` 이상으로 올려야 기존 unsigned 사용자의 업데이트 확인이 새 버전을 감지합니다. unsigned 빌드를 외부에 배포하지 않았다면 첫 signed Release를 `v1.0.0`으로 유지할 수 있습니다.

## 8. v1.0.0 생성 조건

다음이 모두 완료될 때만 첫 정식 signed Release 태그를 만듭니다.

- [x] `main` Ruleset 활성화
- [x] `v*` tag Ruleset 활성화
- [x] GitHub Topics 설정
- [x] 실제 Windows 100% / 125% / 150%에서 설정 UI, 첫 실행 가이드, 트레이 메뉴 최종 확인
- [x] 실제 `EbenTiler-Setup.exe` 설치 / 실행 / 시작 프로그램 / 제거 수동 확인
- [ ] 로컬에서 `설정 > 정보 > 업데이트 확인` 동작 확인
- [x] 로컬에서 트레이 업데이트 `!` 배지, `업데이트 있음 · vX.Y.Z` 메뉴, `설정 > 정보` 이동 확인
- [ ] unsigned 빌드 외부 배포 여부에 따라 첫 signed 버전 번호 확정 (`v1.0.0` 또는 `v1.0.1+`)
- [ ] 코드서명 공급자/인증서 연결 또는 공개 배포 정책 최종 결정
- [ ] 정식 Release workflow의 Authenticode 검증 통과
- [ ] GitHub Release에 `EbenTiler-Setup.exe`와 `.sha256` 게시
- [ ] 랜딩페이지 다운로드가 해당 signed Release를 가리키는지 확인

이 조건 전에는 첫 정식 signed Release 태그를 만들지 않습니다.
