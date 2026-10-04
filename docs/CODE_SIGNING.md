# Tessdeck Code signing policy

이 문서는 Tessdeck의 **unsigned 공개 릴리스와 signed 공개 릴리스를 안전하게 구분**하는 기준을 정리합니다.

## 현재 상태

- Tessdeck은 공개 MIT OSS입니다.
- SignPath Foundation은 우선 검토 대상이지만 **아직 승인되거나 연결된 상태가 아닙니다.**
- 승인 전에는 SignPath가 Tessdeck의 서명을 제공하는 것처럼 표시하지 않습니다.
- 코드서명 신원이 연결되기 전에도 GitHub Release를 공개할 수 있습니다. 이 경우 설치 파일은 unsigned임을 Release와 랜딩페이지에 명확히 표시합니다.

## 핵심 원칙

- `main`의 일반 CI는 코드 서명 비밀정보에 접근하지 않습니다.
- 일반 CI의 unsigned 설치 파일은 GitHub Actions artifact에만 보관하고 공개 다운로드 경로로 사용하지 않습니다.
- 공개 GitHub Release는 태그/버전/소스 위치, 설치/제거 smoke test, SHA-256 검증을 통과해야 합니다.
- 코드서명 신원이 준비되어 있으면 같은 Release 경로에서 Authenticode 서명과 검증을 추가로 수행합니다.
- 코드서명 신원이 아직 없으면 Release 제목/설명과 랜딩페이지에 unsigned 상태와 Windows 경고 가능성을 명확히 고지합니다.
- 랜딩페이지는 자체 설치 파일을 호스팅하지 않고 GitHub의 최신 공개 Release 자산으로 연결합니다.
- 인증서 개인키, 비밀번호, API 키는 저장소나 릴리스 자산, 로그에 포함하지 않습니다.

## 프로젝트 역할

현재 Tessdeck은 1인 유지보수 프로젝트입니다.

- **Committer / Reviewer:** `Eben-Builds`
- **Release Approver:** `Eben-Builds`
- 외부 기여자가 생기면 외부 Pull Request는 maintainer 검토 후 반영합니다.
- 코드서명 요청은 maintainer가 소스, 빌드 결과와 릴리스 버전을 확인한 뒤 승인합니다.

저장소와 코드서명 서비스 계정에는 MFA를 사용합니다.

## 개인정보 정책

Tessdeck은 개인정보, 사용 기록, 창 제목, 입력 내용 또는 파일 내용을 수집하지 않습니다. 새 버전 확인을 위해 최대 24시간에 한 번 GitHub의 공개 Release API에서 최신 버전 정보만 조회하며, 업데이트 파일을 자동 다운로드하거나 자동 설치하지 않습니다.

상세 내용: [`PRIVACY.md`](../PRIVACY.md)

## 2026년 코드 서명 키 보관

새 공개 코드서명 인증서는 export 가능한 PFX 파일을 기본 전제로 설계하지 않습니다. 개인키는 하드웨어 토큰, HSM, 클라우드 HSM 또는 서명 서비스에서 보호하는 방식을 우선합니다.

Tessdeck 빌드는 공급자에 종속되지 않도록 구성합니다. 공급자가 Windows 인증서 저장소/KSP에서 인증서를 사용할 수 있게 하고 `EBENTILER_SIGNING_CERT_SHA1`에 thumbprint를 제공하면 SignTool이 실제 서명을 수행합니다.

기존에 보유한 export 가능한 PFX가 있는 경우에만 `EBENTILER_SIGNING_PFX_BASE64`와 `EBENTILER_SIGNING_PFX_PASSWORD`를 호환 경로로 사용할 수 있습니다.

참고: https://cabforum.org/working-groups/code-signing/requirements/

## 공개 OSS에서의 선택지

Tessdeck 저장소는 공개 OSS입니다. 따라서 다음 순서로 검토합니다.

1. **SignPath Foundation**: 프로젝트가 무료 OSS 코드서명 조건을 충족하는지 우선 검토
2. **OV 코드서명 + HSM/클라우드 서명 서비스**: SignPath가 맞지 않거나 별도 publisher identity가 필요한 경우 검토
3. **Microsoft Store/MSIX**: 향후 배포 채널을 Store 중심으로 바꿀 필요가 생길 때 검토

코드서명 문제 하나 때문에 현재의 가벼운 Inno Setup 구조를 무리하게 갈아엎지는 않습니다.

### SignPath Foundation 적용 시 추가되는 항목

SignPath Foundation 승인을 받은 경우에만 홈페이지/다운로드/릴리스 화면에 다음 고지를 추가합니다.

`Free code signing provided by SignPath.io, certificate by SignPath Foundation`

승인 후에는 SignPath 프로젝트의 Artifact Configuration에서 다음을 강제합니다.

- Product name: `Tessdeck for Windows`
- 모든 서명 대상의 제품 버전 일치
- Tessdeck 프로젝트가 직접 빌드한 바이너리만 서명
- GitHub-hosted runner에서 만들어진 GitHub Actions artifact만 서명 요청
- 정식 release signing request는 maintainer의 수동 승인 후 진행

## CI 분리

### 일반 CI

`.github/workflows/build-installer.yml`

- unsigned 설치 파일 빌드
- SHA-256 확인
- 설치/제거 스모크 테스트
- 코드 서명 Secret 접근 금지
- 결과물은 Actions artifact로만 보관

### 랜딩페이지

`.github/workflows/build-website.yml`

- 정적 웹 자산만 검증/게시
- 설치 파일을 빌드하거나 `site` 브랜치에 포함하지 않음
- 다운로드는 최신 공개 GitHub Release의 `Tessdeck-Setup.exe`로 연결
- unsigned 공개 릴리스 기간에는 Windows 경고 가능성을 사용자에게 고지

### 공개 릴리스

`.github/workflows/release.yml`

- `vMAJOR.MINOR.PATCH` 태그에서만 실행
- 저장소 owner가 만든 릴리스만 허용
- 태그가 `main`에 포함된 커밋을 가리키는지 확인
- 태그 버전과 `AssemblyFileVersion` 일치 확인
- 설치/제거 테스트
- SHA-256 검증
- 코드서명 신원이 있으면 EXE/Setup Authenticode 서명과 검증
- 코드서명 신원이 없으면 unsigned 상태를 Release 제목/설명에 명확히 표시
- 모든 검증 후에만 GitHub Release 게시

## 서명 신원 준비

`tools/prepare-code-signing.ps1`은 다음 순서로 동작합니다.

1. `EBENTILER_SIGNING_CERT_SHA1`이 있고 해당 인증서가 `Cert:\CurrentUser\My`에서 사용 가능하면 사용
2. 기존 export 가능한 PFX가 있는 경우에만 PFX 호환 경로 사용
3. 둘 다 없고 `-RequireSigning`을 지정한 경우 실패
4. 둘 다 없고 `-RequireSigning`이 없으면 unsigned 빌드로 계속 진행

HSM/클라우드 서명 공급자를 붙일 때는 Release workflow에서 공급자의 KSP/클라이언트를 준비한 뒤 최종 인증서 thumbprint를 `EBENTILER_SIGNING_CERT_SHA1`로 넘깁니다.

SignPath Foundation이 승인되면 위 로컬 인증서 경로를 억지로 사용하지 않고 SignPath의 GitHub trusted-build 흐름에 맞춰 `GitHub Actions artifact → signing request → signed artifact → 검증 → Release` 순서로 별도 연결합니다.

## 릴리스 검증

모든 공개 릴리스는 최소한 다음을 통과해야 합니다.

- `vMAJOR.MINOR.PATCH` 태그
- 태그가 `main`에 포함된 커밋을 가리킴
- 앱 버전과 태그 버전 일치
- 설치/제거 스모크 테스트 통과
- 설치 파일 SHA-256 일치

unsigned 공개 릴리스 수동 검증:

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-release.ps1
```

signed 공개 릴리스는 위 항목에 더해 다음을 확인합니다.

- `Tessdeck.exe` Authenticode 유효
- `Tessdeck-Setup.exe` Authenticode 유효
- Code Signing EKU 확인

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-release.ps1 -RequireCodeSigning
```

## unsigned → signed 전환

처음 공개한 unsigned 버전과 같은 버전 번호로 signed 파일을 다시 올리지 않습니다. 기존 사용자 앱이 새 버전으로 인식할 수 있도록 다음 signed 릴리스는 반드시 더 높은 버전 번호를 사용합니다.

예:

- `v1.0.0`: 최초 공개 unsigned 릴리스
- `v1.0.1`: 코드서명 연결 후 첫 signed 릴리스

이렇게 하면 기존 unsigned 사용자의 업데이트 알림이 signed 새 버전을 정상적으로 감지할 수 있습니다.

## 개인키와 비밀정보

- 공개 코드서명 개인키를 저장소에 넣지 않습니다.
- PFX/P12/PEM/KEY 파일을 커밋하지 않습니다.
- 로그에 비밀번호, API 키, 인증서 개인키를 출력하지 않습니다.
- 가능한 경우 개인키는 non-exportable 상태로 유지합니다.
- 서명 공급자 계정에는 MFA를 사용합니다.
- 릴리스 게시 권한과 일반 빌드 권한을 분리합니다.

## SmartScreen

코드서명 전 공개 릴리스는 Windows에서 `알 수 없는 게시자` 또는 SmartScreen 경고가 표시될 수 있습니다. 이것은 사용자에게 숨기지 않습니다.

코드서명은 게시자 신원과 파일 무결성을 제공하지만 새 publisher와 새 바이너리가 첫날부터 SmartScreen 경고를 항상 피한다는 의미는 아닙니다. 동일한 publisher identity를 안정적으로 유지하며 정상 배포 이력을 쌓는 것이 중요합니다.
