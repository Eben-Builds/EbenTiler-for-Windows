# EbenTiler 코드 서명 정책

이 문서는 EbenTiler의 **정식 배포 파일을 어떻게 서명하고 검증하는지** 정리한다.
개발 중인 unsigned 빌드와 일반 사용자에게 공개하는 signed release를 분리하는 것이 목적이다.

## 현재 원칙

- 개발/CI 빌드는 코드 서명 인증서가 없어도 만들 수 있다.
- 일반 사용자에게 공개하는 GitHub Release는 **유효한 Authenticode 서명 없이는 게시하지 않는다.**
- `EbenTiler.exe`, 설치 프로그램, 제거 프로그램까지 서명한다.
- 설치 프로그램의 SHA-256 파일을 함께 만들고 릴리스 직전에 다시 검증한다.
- 인증서 개인키, API 키, 클라이언트 인증서 비밀번호 등은 저장소에 커밋하지 않는다.
- 서명 빌드는 SHA-256과 RFC 3161 타임스탬프를 사용한다.

## 2026년에 PFX만 기준으로 잡으면 안 되는 이유

공개 신뢰 코드서명 인증서는 2023년 6월 이후 개인키를 적절한 하드웨어 암호 모듈, 클라우드 HSM 또는 서명 서비스에서 생성·보관·사용하도록 요구된다.
따라서 새 인증서를 구매하면서 **"PFX 파일을 받아 GitHub Secret에 넣으면 된다"**는 전제를 두지 않는다.

CA/Browser Forum의 2026 Code Signing Baseline Requirements:
https://cabforum.org/working-groups/code-signing/requirements/

EbenTiler의 빌드 스크립트는 서명 공급자 자체를 알 필요가 없도록 구성한다.
서명 공급자가 Windows 인증서 저장소/KSP를 통해 인증서를 사용할 수 있게 하고 `EBENTILER_SIGNING_CERT_SHA1`에 인증서 thumbprint를 제공하면 `SignTool`이 실제 서명을 수행한다.

기존에 보유한 **export 가능한 PFX**가 있는 경우에만 호환 경로로 사용할 수 있다.
이 경로는 새 공개 인증서 구매의 기본 설계가 아니다.

## EbenTiler에 맞는 선택지

### 1. CA의 OV 코드서명 + HSM/클라우드 서명 서비스

현재처럼 **자체 랜딩페이지에서 EXE를 직접 배포**하고 저장소를 비공개로 유지하려면 가장 자연스러운 방식이다.
DigiCert, Sectigo, GlobalSign 등 공개 신뢰 CA의 코드서명 상품을 검토하되, 구매 전에 다음을 확인한다.

- GitHub Actions 같은 CI에서 사용할 수 있는가
- 개인키가 HSM/클라우드 서명 서비스에 보관되는가
- Windows `SignTool` 또는 호환 KSP/클라이언트를 제공하는가
- 개인 개발자 신원 검증이 가능한가
- 한국에서 발급과 결제가 가능한가
- 인증서 갱신 시 publisher identity를 안정적으로 유지할 수 있는가

새 인증서라면 EV를 SmartScreen 우회 목적으로 더 비싸게 살 이유는 없다. Microsoft는 2024년 이후 EV도 즉시 SmartScreen 신뢰를 부여하지 않으며 OV와 마찬가지로 평판이 쌓여야 한다고 안내한다.

Microsoft 참고:
https://learn.microsoft.com/windows/apps/package-and-deploy/code-signing-options
https://learn.microsoft.com/windows/apps/package-and-deploy/smartscreen-reputation

### 2. SignPath Foundation

프로젝트를 공개 OSS로 운영하고 SignPath Foundation의 조건을 충족한다면 무료 코드서명을 신청할 수 있다.
다만 소스/빌드 출처 검증, 유지보수 상태, OSS 라이선스, 릴리스 이력, 코드서명 정책, 승인 절차 등의 조건이 있다.

현재 저장소를 비공개로 유지하는 동안에는 이 경로를 기본 배포 방식으로 잡지 않는다.
공개 OSS 전환 시 다시 검토한다.

https://signpath.org/
https://signpath.org/terms.html

### 3. Microsoft Store + MSIX

MSIX로 Store에 배포하면 Microsoft가 패키지를 다시 서명하므로 별도 인증서 관리 부담을 줄일 수 있다.
하지만 현재 EbenTiler의 Inno Setup 기반 직접 배포 구조와 배포 경험을 바꾸는 선택이므로, 코드서명 문제 하나 때문에 지금 패키징 방식을 갈아엎지는 않는다.

https://learn.microsoft.com/windows/apps/package-and-deploy/code-signing-options

### 4. Azure Artifact Signing

Microsoft가 Store 밖 배포에 권장하는 서비스지만 지역/계정 유형별 사용 가능 범위가 있다.
2026년 Microsoft 문서 기준으로 개인 개발자는 미국/캐나다로 제한되어 있으므로, 실제 계정이 지원되는지 확인되지 않은 상태에서 EbenTiler의 기본 경로로 가정하지 않는다.

## SmartScreen에 대한 기대치

**서명했다고 첫날부터 SmartScreen 경고가 무조건 사라지는 것은 아니다.**
Microsoft는 새 바이너리와 새 publisher identity가 평판을 쌓는 동안 경고가 나타날 수 있다고 설명한다.

서명의 핵심 가치는 다음과 같다.

- 사용자가 확인 가능한 게시자 신원을 제공한다.
- 배포 후 파일이 변조되지 않았는지 확인할 수 있다.
- 같은 publisher identity를 꾸준히 사용하면 버전 간 평판 축적에 도움이 된다.
- 기업 환경에서 unsigned 파일보다 훨씬 정상적인 배포 경로를 제공한다.

## CI 통합 계약

`tools/prepare-code-signing.ps1`은 다음 순서로 서명 신원을 준비한다.

1. `EBENTILER_SIGNING_CERT_SHA1`이 있고 해당 인증서가 `Cert:\CurrentUser\My`에서 사용 가능하면 그대로 사용한다.
2. 기존 export 가능한 PFX가 있을 때만 `SIGNING_PFX_BASE64` + `SIGNING_PFX_PASSWORD` 호환 경로를 사용한다.
3. 정식 릴리스에서 둘 다 없으면 실패한다.

새 HSM/클라우드 공급자를 붙일 때는 `.github/workflows/release.yml`의 **Prepare code-signing identity** 단계보다 앞에서 공급자의 KSP/클라이언트를 구성하고, 최종 인증서 thumbprint를 `EBENTILER_SIGNING_CERT_SHA1`로 넘긴다.

## 릴리스 검증

정식 릴리스는 다음을 모두 통과해야 한다.

- 태그가 `vMAJOR.MINOR.PATCH` 형식이다.
- 태그 버전과 `AssemblyFileVersion`의 앞 세 자리가 같다.
- 설치/제거 스모크 테스트를 통과한다.
- `EbenTiler.exe` Authenticode 서명이 유효하다.
- `EbenTiler-Setup.exe` Authenticode 서명이 유효하다.
- 서명 인증서에 Code Signing EKU가 있다.
- 설치 파일 SHA-256이 manifest와 일치한다.

검증 스크립트:

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-release.ps1 -RequireCodeSigning
```

## 개인키와 비밀정보

- 공개 코드서명 개인키는 저장소에 넣지 않는다.
- PFX/P12를 커밋하지 않는다.
- GitHub Actions 로그에 비밀번호, API key, client certificate 내용을 출력하지 않는다.
- 가능하면 공급자 HSM에서 개인키를 non-exportable 상태로 유지한다.
- 서명 공급자 계정에는 MFA를 사용한다.
- 릴리스 게시 권한과 빌드/검증 권한을 분리한다.

## 현재 결정

EbenTiler는 기존 **Inno Setup + 직접 다운로드** 구조를 유지한다.
정식 공개 전에는 새 인증서 구매를 PFX 기준으로 서두르지 않고, **CI 사용 가능한 OV/HSM 또는 클라우드 서명 서비스**를 먼저 확정한다.
공개 OSS로 전환할 경우 SignPath Foundation을 다시 검토한다.
