# Security

Tessdeck은 계정, 로그인, 원격 코드 실행 기능을 사용하지 않는 로컬 Windows 유틸리티입니다.
랜딩페이지는 정적 파일만 제공하며, 앱은 새 버전 확인을 위해 최대 24시간에 한 번 GitHub의 공개 Release API에 버전 정보만 요청합니다.

## 현재 공격 표면

앱이 직접 다루는 외부 입력은 전역 단축키, 로컬 설정 파일, CLI 인수, Windows 창 핸들, GitHub 공개 Release 메타데이터입니다.

- 설정 파일: 현재 사용자 `%APPDATA%\Tessdeck\config.ini`
- 업데이트 확인 상태: `%APPDATA%\Tessdeck\update-state.ini`
- 업데이트 배지 상태: `%APPDATA%\Tessdeck\update-badge.ini`
- Quick Layout 스냅샷: `%APPDATA%\Tessdeck\quick-layout.ini` (창 제목/URL/파일 경로/명령줄/창 내용 저장 안 함)
- 자동 시작: 현재 사용자 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- 설치 위치: 현재 사용자 `%LOCALAPPDATA%\Programs\Tessdeck`
- 관리자 권한 설치 요구 없음
- 계정/로그인/텔레메트리 없음
- 업데이트 파일 자동 다운로드/자동 설치 없음

## CI와 코드 서명 격리

일반 `main` CI는 코드 서명 인증서, 개인키, PFX 비밀번호 같은 릴리스 자격증명을 읽지 않습니다.

- `.github/workflows/build-installer.yml`: unsigned 개발/검증 빌드만 생성
- `.github/workflows/build-website.yml`: 정적 랜딩페이지만 검증/게시하며 설치 파일을 만들거나 포함하지 않음
- `.github/workflows/release.yml`: `vMAJOR.MINOR.PATCH` 공개 릴리스에서만 게시 권한과 선택적 코드서명 자격증명 사용

공개 Release는 코드서명 유무와 관계없이 태그/버전/`main` 포함 여부, 설치/제거 smoke test, SHA-256 검증을 통과해야 합니다.
코드서명 신원이 연결된 경우에는 `tools/verify-release.ps1 -RequireCodeSigning`으로 Authenticode와 Code Signing EKU까지 추가 검증합니다.

## 다운로드 공급망

일반 사용자는 랜딩페이지에서 일반 CI의 임시 Actions artifact를 받지 않습니다.
랜딩페이지의 다운로드 버튼은 GitHub의 최신 공개 Release에 첨부된 `Tessdeck-Setup.exe`만 가리킵니다.

모든 공개 Release 게시 조건:

- 태그 형식이 `vMAJOR.MINOR.PATCH`
- 태그가 `main`에 포함된 커밋을 가리킴
- 태그 버전과 `AssemblyFileVersion` 일치
- 설치/제거 스모크 테스트 통과
- 설치 파일 SHA-256 검증 통과

코드서명 신원이 있는 Release는 추가로 다음을 통과해야 합니다.

- `Tessdeck.exe` Authenticode 서명 유효
- `Tessdeck-Setup.exe` Authenticode 서명 유효
- Code Signing EKU 확인

코드서명 신원이 아직 없는 초기 공개 Release는 unsigned로 게시할 수 있지만, 다음을 반드시 지킵니다.

- Release 제목/설명에 unsigned 상태를 명확히 표시
- Windows에서 `알 수 없는 게시자` 또는 SmartScreen 경고가 표시될 수 있음을 고지
- `.sha256` 파일을 같은 Release 자산으로 게시
- 랜딩페이지에서도 코드 서명 전 상태를 숨기지 않음

일반 CI에서 생성한 unsigned Actions artifact를 공개 사용자 다운로드로 직접 연결하지 않습니다.

## 랜딩페이지 보안

`website/_headers`에서 다음 정책을 사용합니다.

- CSP: 외부 스크립트/객체/프레임/네트워크 연결 차단
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: no-referrer`
- Permissions Policy로 불필요한 브라우저 권한 차단
- HSTS

랜딩페이지는 HTTPS에서만 공개합니다.

## 민감정보와 개인정보

다음 파일은 저장소에 커밋하지 않습니다.

- `.env`, `.env.*`
- `*.pfx`, `*.p12`
- `*.pem`, `*.key`
- 코드 서명 개인키/비밀번호/API 키

업데이트 확인 요청은 GitHub 공개 Release 메타데이터 조회에만 사용하며 개인정보, 사용 기록, 창 제목, 입력 내용, 파일 내용을 전송하지 않습니다.

Git 저장소의 커밋 author 이메일은 공개 저장소에서 누구나 볼 수 있습니다. 앞으로 개인 이메일 노출을 원하지 않는 경우 GitHub의 noreply 이메일을 Git author 이메일로 사용합니다. 기존 공개 Git 히스토리는 별도 결정 없이 강제로 rewrite하지 않습니다.

## 공개 배포 전 확인

- Windows 10/11 깨끗한 환경에서 설치 → 실행 → 설정 → 트레이 → 자동 시작 → 제거 확인
- GitHub Actions의 외부 Action은 commit SHA로 고정
- Inno Setup 등 빌드 도구 버전 고정
- 모든 공개 Release에서 SHA-256 검증
- signed Release에서는 Authenticode 검증 추가
- unsigned Release에서는 코드서명 전 상태와 Windows 경고 가능성 명시
- `main` force push/삭제 방지 및 `v*` 릴리스 태그 보호 규칙 적용
- HTTPS 랜딩페이지에서 실제 응답 보안 헤더 확인

## 보안 취약점 제보

공개 이슈에 비밀번호, 인증서, 개인키, API 키, 개인 데이터 같은 민감정보를 올리지 마세요. 민감한 취약점 제보 채널을 별도로 제공하게 되면 이 문서에 안내합니다.
