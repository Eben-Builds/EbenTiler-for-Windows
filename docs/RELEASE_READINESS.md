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

EbenTiler는 공개 MIT OSS이고 소스/빌드 스크립트를 직접 관리하며, 앱 자체는 원격 API·텔레메트리·사용자 데이터 전송 기능이 없습니다. 구조상 SignPath Foundation의 무료 OSS 코드서명 후보로 검토할 수 있습니다.

2026-10-03 신청을 제출했으며 승인 여부는 SignPath Foundation이 결정합니다. 승인 전에는 SignPath가 현재 EbenTiler의 서명을 제공하는 것처럼 표시하지 않습니다.

참고:
- https://signpath.org/terms.html
- https://docs.signpath.io/trusted-build-systems/github

## 4. 개인정보 / 계정

- GitHub 계정 MFA 사용
- 향후 로컬 Git 커밋에는 GitHub `noreply` 이메일 사용
- 기존 공개 Git 히스토리는 파괴적인 rewrite 없이 유지
- `.env`, PFX/P12/PEM/KEY 등 민감 파일은 저장소에 커밋하지 않음

## 5. UI / DPI 최종 확인

설정 > 일반에 `시작 가이드 다시 보기`가 있고, 수동으로 다시 연 가이드는 첫 실행 전용 `다시 표시하지 않기` 상태를 변경하지 않습니다.

실제 Windows 디스플레이 배율에서 다음을 각각 확인합니다.

- 100%
- 125%
- 150%

각 배율에서 아래 스크립트를 실행하면 첫 실행 가이드, 설정 > 일반, 설정에서 다시 연 가이드를 실제 창으로 띄워 크기를 검증하고 PNG를 저장합니다.

```powershell
powershell -ExecutionPolicy Bypass -File tools\capture-dpi-ui.ps1
```

최종 판정에는 생성된 PNG에서 글자 잘림, 겹침, 버튼 잘림이 없는지 눈으로 확인하는 절차가 포함됩니다.

## 6. v1.0.0 생성 조건

다음이 모두 완료될 때만 `v1.0.0` 태그를 만듭니다.

- [x] `main` Ruleset 활성화
- [x] `v*` tag Ruleset 활성화
- [x] GitHub Topics 설정
- [ ] 코드서명 공급자/인증서 연결 또는 공개 배포 정책 최종 결정
- [ ] 실제 Windows 100% / 125% / 150%에서 설정 UI와 첫 실행 가이드 최종 확인
- [ ] 정식 Release workflow의 Authenticode 검증 통과
- [ ] GitHub Release에 `EbenTiler-Setup.exe`와 `.sha256` 게시
- [ ] 랜딩페이지 다운로드가 해당 signed Release를 가리키는지 확인

이 조건 전에는 `v1.0.0` 태그를 만들지 않습니다.
