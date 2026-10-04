# Tessdeck 랜딩페이지

이 폴더는 Tessdeck 공식 랜딩페이지의 정적 소스입니다. 별도 서버나 데이터베이스 없이 정적 호스팅에서 동작합니다.

## 구성

- `index.html`: 랜딩페이지 본문
- `styles.css`: 전체 디자인과 반응형 스타일
- `hero-tessdeck.webp`: 메인 히어로 이미지
- `thirds-tessdeck.webp`: 3분할 기능 예시 이미지
- `app.js`: 다운로드 버튼을 최신 GitHub Release의 설치 파일로 연결하고 상태를 표시
- `favicon.svg`: Tessdeck 파비콘
- `_headers`: 정적 호스팅용 보안 헤더

## 다운로드 파일

랜딩페이지는 일반 CI의 임시 Actions artifact를 직접 배포하지 않습니다.

모든 `윈도우용 다운로드` 링크는 GitHub 최신 공개 Release의 `Tessdeck-Setup.exe`를 직접 가리킵니다. 공개 Release는 `.github/workflows/release.yml`에서 태그/버전/`main` 포함 여부, 설치/제거 smoke test, SHA-256 검증을 통과한 결과물만 게시됩니다.

코드서명 신원이 연결되어 있으면 같은 Release 경로에서 Authenticode 서명과 검증까지 수행합니다. 아직 코드서명이 연결되지 않은 경우에는 Release 제목과 설명, 랜딩페이지에서 unsigned 상태와 Windows의 게시자/SmartScreen 경고 가능성을 명확하게 고지합니다.

따라서 SignPath 승인 전에도 검증된 unsigned GitHub Release를 공개 다운로드로 제공할 수 있고, 코드서명이 준비되면 이후 릴리스부터 signed 설치 파일로 전환합니다.

## 로컬 확인

정적 서버로 `website` 폴더를 열면 랜딩페이지 디자인을 확인할 수 있습니다. 다운로드 버튼은 로컬 파일이 아니라 GitHub의 최신 공개 Release를 가리킵니다.
