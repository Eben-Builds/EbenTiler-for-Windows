# SignPath Foundation 신청 준비

이 문서는 EbenTiler를 SignPath Foundation 무료 OSS 코드서명에 신청할 때 확인하거나 복사해 사용할 수 있는 공개 정보만 정리합니다.

> 현재 상태: **신청/승인/연동 전**. 이 문서는 SignPath가 EbenTiler를 승인했다는 의미가 아닙니다.

## 프로젝트 정보

- Project: `EbenTiler for Windows`
- Repository: `https://github.com/Eben-Builds/EbenTiler-for-Windows`
- License: MIT
- Platform: Windows 10 / 11 x64
- Runtime: .NET Framework 4.8
- Installer: Inno Setup, per-user install, administrator privilege not required
- Maintainer: `Eben-Builds`

## 한 줄 설명

EbenTiler는 전역 단축키로 현재 Windows 창을 화면 절반, 사분면, 3분할, 2/3 및 여러 모니터에 빠르게 배치하는 경량 오픈소스 Windows 유틸리티입니다.

## 사용자 데이터 / 개인정보

- 계정/로그인 없음
- 텔레메트리 없음
- 광고/분석 SDK 없음
- 앱 자체 네트워크 요청 없음
- 사용자 파일/입력 내용/창 제목 수집 없음
- 로컬 설정만 `%APPDATA%\EbenTiler\config.ini`에 저장

Privacy policy: [`PRIVACY.md`](../PRIVACY.md)

필요한 공개 문구:

> This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it.

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
- 결과물은 GitHub Actions artifact로만 보관

### 웹사이트

`.github/workflows/build-website.yml`

- 정적 사이트만 배포
- unsigned installer를 사이트에 포함하지 않음
- 다운로드는 정식 GitHub Release 자산만 가리키도록 구성

### 정식 릴리스

`.github/workflows/release.yml`

현재는 Authenticode 서명 신원이 없으면 실패하도록 잠겨 있습니다.

- `vMAJOR.MINOR.PATCH`만 허용
- tag가 `main`의 commit인지 검증
- tag와 앱 버전 일치 검증
- 설치/제거 smoke test
- Authenticode 검증
- Code Signing EKU 확인
- SHA-256 검증
- 검증된 결과만 GitHub Release 게시

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

- Product name은 `EbenTiler for Windows`로 제한
- release 전체의 product version 일치
- EbenTiler가 직접 빌드한 `EbenTiler.exe`와 installer만 프로젝트 인증서로 서명
- 외부/제3자 바이너리를 EbenTiler 프로젝트 서명으로 다시 서명하지 않음
- release source는 해당 GitHub repository로 제한
- release branch/tag 정책을 명확히 제한

## 신청 전 외부 설정

GitHub 웹 UI에서 확인:

- GitHub 계정 MFA 활성화
- `main` branch Ruleset: force push 및 deletion 차단
- `v*` tag Ruleset: update/deletion 제한
- GitHub Topics 설정

현재 연결된 자동화 권한으로 repository administration Ruleset을 생성할 수 없으므로 저장소 owner가 GitHub UI에서 직접 설정해야 합니다.

## 중요: Released 조건

SignPath Foundation 조건에는 프로젝트가 **서명받고자 하는 형태로 이미 released 상태**여야 한다는 항목이 있습니다.

EbenTiler는 현재 안전을 위해 unsigned installer를 정식 사용자 다운로드로 공개하지 않도록 구성되어 있습니다. 따라서 신청 전에 SignPath에 현재 공개 저장소/빌드/사이트 상태로 신청 가능한지 확인합니다.

SignPath가 실제 downloadable pre-release를 요구한다고 확인한 경우에만, 정식 `v1.0.0`과 분리된 preview 배포 방식을 검토합니다. 확인 없이 unsigned production release를 만들지 않습니다.

## 승인 후 홈페이지에 추가할 문구

SignPath Foundation이 실제로 승인하고 해당 인증서를 사용할 수 있게 된 뒤에만 다음 문구를 README와 다운로드/릴리스 페이지에 표시합니다.

> Free code signing provided by SignPath.io, certificate by SignPath Foundation

승인 전에는 이 문구를 현재 제공 중인 서비스처럼 표시하지 않습니다.

## 공식 참고

- https://signpath.org/apply.html
- https://signpath.org/terms.html
- https://docs.signpath.io/trusted-build-systems/github
- https://docs.signpath.io/artifact-configuration/
