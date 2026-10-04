# SignPath Foundation 신청 준비

이 문서는 Tessdeck의 SignPath Foundation 무료 OSS 코드서명 신청과 현재 공개 배포 상태를 함께 정리합니다.

> 현재 상태: **2026-10-03 신청 제출, 승인 대기 중**. 이 문서는 SignPath가 Tessdeck를 승인했다는 의미가 아닙니다.

## 프로젝트 정보

- Project: `Tessdeck for Windows`
- Repository: `https://github.com/Eben-Builds/Tessdeck-for-Windows`
- License: MIT
- Platform: Windows 10 / 11 x64
- Runtime: .NET Framework 4.8
- Installer: Inno Setup, per-user install, administrator privilege not required
- Maintainer: `Eben-Builds`

## 한 줄 설명

Tessdeck는 전역 단축키로 현재 Windows 창을 화면 절반, 사분면, 3분할, 2/3 및 여러 모니터에 빠르게 배치하는 경량 오픈소스 Windows 유틸리티입니다.

## 사용자 데이터 / 개인정보

- 계정/로그인 없음
- 텔레메트리 없음
- 광고/분석 SDK 없음
- 사용자 파일/입력 내용/창 제목 수집 없음
- 최대 24시간에 한 번 GitHub 공개 Release API에서 최신 버전 정보만 조회
- 업데이트 파일 자동 다운로드/자동 설치 없음
- 앱 설정은 `%APPDATA%\Tessdeck\config.ini`에 로컬 저장
- 업데이트 확인 상태는 `%APPDATA%\Tessdeck\update-state.ini`에 로컬 저장
- 업데이트 배지 상태는 `%APPDATA%\Tessdeck\update-badge.ini`에 로컬 저장

Privacy policy: [`PRIVACY.md`](../PRIVACY.md)

## 코드서명 역할

현재 1인 유지보수 프로젝트입니다.

- Committer / Reviewer: `Eben-Builds`
- Release Approver: `Eben-Builds`
- 외부 Pull Request는 maintainer 검토 후 반영
- 모든 정식 signing request는 maintainer 수동 승인

## 현재 빌드 / 공급망 구조

### 일반 CI

`.github/workflows/build-installer.yml`

- GitHub-hosted Windows runner 사용
- unsigned installer 빌드
- SHA-256 검증
- 실제 설치/CLI/자동시작/제거 smoke test
- 코드서명 비밀정보에 접근하지 않음
- 결과물은 GitHub Actions artifact로 보관

### 웹사이트

`.github/workflows/build-website.yml`

- 정적 사이트만 배포
- 일반 CI artifact를 사이트에 포함하지 않음
- 다운로드는 최신 공개 GitHub Release 자산만 가리키도록 구성
- 코드서명 전 공개 릴리스 기간에는 Windows 경고 가능성을 고지

### 공개 릴리스

`.github/workflows/release.yml`

- `vMAJOR.MINOR.PATCH`만 허용
- tag가 `main`의 commit인지 검증
- tag와 앱 버전 일치 검증
- 설치/제거 smoke test
- SHA-256 검증
- 코드서명 신원이 연결되어 있으면 Authenticode와 Code Signing EKU까지 검증
- 코드서명 신원이 아직 없으면 Release 제목/설명에 unsigned 상태를 명확히 표시
- 검증된 결과만 GitHub Release 게시

현재 정책상 SignPath 승인 전에도 검증된 unsigned 공개 Release를 제공할 수 있습니다. 이는 일반 CI artifact를 그대로 공개하는 것이 아니라, 보호된 `v*` 태그와 별도 Release workflow를 통해 다시 검증한 결과물만 게시하는 방식입니다.

## SignPath 승인 후 목표 흐름

SignPath Open Source Code Signing이 승인되면 release flow를 다음으로 전환합니다.

1. GitHub-hosted runner에서 unsigned release artifact 빌드
2. GitHub Actions artifact로 먼저 업로드
3. SignPath GitHub trusted-build integration으로 signing request 제출
4. Maintainer가 release signing request 수동 승인
5. SignPath가 반환한 signed artifact 다운로드
6. Authenticode / 버전 / SHA-256 / 설치-제거 재검증
7. 검증된 signed artifact만 GitHub Release에 게시
8. 랜딩페이지 Download가 해당 Release asset을 가리키는지 확인

SignPath의 organization/project/signing-policy/artifact-configuration 식별자는 **승인 후 발급된 실제 값만** workflow에 넣습니다. 추정값이나 placeholder secret을 production workflow에 넣지 않습니다.

## Artifact Configuration에서 확인할 항목

- Product name은 `Tessdeck for Windows`로 제한
- release 전체의 product version 일치
- Tessdeck가 직접 빌드한 `Tessdeck.exe`와 installer만 프로젝트 인증서로 서명
- 외부/제3자 바이너리를 Tessdeck 프로젝트 서명으로 다시 서명하지 않음
- release source는 해당 GitHub repository로 제한
- release branch/tag 정책을 명확히 제한

## GitHub 외부 설정

- GitHub 계정 MFA 활성화
- `main` branch Ruleset: force push 및 deletion 차단
- `v*` tag Ruleset: update/deletion 제한
- GitHub Topics 설정

## Released 상태

Tessdeck는 SignPath 승인 여부와 독립적으로 공개 GitHub Release를 제공할 수 있도록 정책을 변경했습니다.

코드서명 전 Release는 다음을 지킵니다.

- unsigned 상태를 숨기지 않음
- Windows에서 `알 수 없는 게시자` 또는 SmartScreen 경고가 표시될 수 있음을 고지
- GitHub Actions에서 소스/버전/설치/제거/SHA-256 검증
- `.sha256` 파일 함께 게시
- 일반 CI artifact가 아닌 보호된 Release workflow 결과물만 공개

승인 후에는 기존 unsigned 버전을 같은 버전 번호로 교체하지 않고 더 높은 버전 번호의 signed Release를 새로 게시합니다.

## 승인 후 홈페이지에 추가할 문구

SignPath Foundation이 실제로 승인하고 해당 인증서를 사용할 수 있게 된 뒤에만 다음 문구를 README와 다운로드/릴리스 페이지에 표시합니다.

> Free code signing provided by SignPath.io, certificate by SignPath Foundation

승인 전에는 이 문구를 현재 제공 중인 서비스처럼 표시하지 않습니다.

## 공식 참고

- https://signpath.org/apply.html
- https://signpath.org/terms.html
- https://docs.signpath.io/trusted-build-systems/github
- https://docs.signpath.io/artifact-configuration/
