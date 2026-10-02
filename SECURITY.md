# Security

EbenTiler는 현재 계정, 로그인, 원격 API, 원격 코드 실행 기능을 사용하지 않는 로컬 Windows 유틸리티입니다.
현재 앱 본체는 네트워크 요청을 하지 않으며, 랜딩페이지도 정적 파일만 제공합니다.

## 공개 배포 전 확인

- `assets/app.ico`가 실행 파일, 설치 프로그램, 설정 UI, 트레이 아이콘에서 동일하게 사용되는지 확인합니다.
- GitHub Actions에서 설치 파일을 새로 빌드한 뒤 `EbenTiler-Setup.exe.sha256`과 실제 SHA-256이 일치해야 합니다.
- 공개 릴리스는 Authenticode 코드 서명 인증서를 연결한 뒤 `EbenTiler.exe`와 `EbenTiler-Setup.exe`의 서명이 모두 유효해야 합니다.
- 설치 → 실행 → 단축키 설정 → 트레이 메뉴 → 자동 시작 → 제거 흐름을 깨끗한 Windows 10/11 환경에서 확인합니다.
- 랜딩페이지는 HTTPS에서만 제공하고 `_headers`의 CSP/HSTS/프레임 차단 정책이 실제 응답에 적용되는지 확인합니다.
- 빌드 워크플로의 외부 GitHub Action과 설치 도구 버전은 고정된 값으로 유지합니다.

## 현재 공격 표면

앱이 직접 다루는 외부 입력은 전역 단축키, 로컬 설정 파일, CLI 인수, Windows 창 핸들입니다.
설정 파일은 현재 사용자 `%APPDATA%\EbenTiler`에 저장되며, 자동 시작은 현재 사용자 `HKCU` 영역만 사용합니다.
관리자 권한 설치를 요구하지 않습니다.

## 릴리스 보안 원칙

공개 파일은 GitHub Actions에서 생성한 설치 파일만 사용합니다.
수동으로 교체한 실행 파일이나 출처가 확인되지 않은 바이너리는 랜딩페이지에 배포하지 않습니다.

정식 GitHub Release는 `vMAJOR.MINOR.PATCH` 태그로만 시작하며, 태그 버전과 `AssemblyFileVersion`이 일치해야 합니다.
`.github/workflows/release.yml`은 코드 서명 Secret이 없으면 즉시 실패하고, `tools/verify-release.ps1`에서 SHA-256과 Authenticode 서명을 다시 검증한 뒤에만 릴리스를 게시합니다.
서명 타임스탬프는 HTTPS RFC 3161 엔드포인트를 사용합니다.

필요한 Repository secrets:

- `EBENTILER_SIGNING_PFX_BASE64`: 코드 서명 PFX 파일의 Base64 값
- `EBENTILER_SIGNING_PFX_PASSWORD`: 해당 PFX 비밀번호

코드 서명 인증서 자체와 비밀번호는 저장소 파일, 릴리스 자산, 로그에 포함하지 않습니다.
