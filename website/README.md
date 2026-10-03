# EbenTiler 랜딩페이지

이 폴더는 EbenTiler 공식 랜딩페이지의 정적 소스입니다. 별도 서버나 데이터베이스 없이 정적 호스팅에서 동작합니다.

## 구성

- `index.html`: 랜딩페이지 본문
- `styles.css`: 전체 디자인과 반응형 스타일
- `hero-ebentiler.webp`: 메인 히어로 이미지
- `thirds-ebentiler.webp`: 3분할 기능 예시 이미지
- `app.js`: 다운로드 버튼을 정식 GitHub Release의 검증된 설치 파일로 연결하고 상태를 표시
- `favicon.svg`: EbenTiler 파비콘
- `_headers`: 정적 호스팅용 보안 헤더

## 다운로드 파일

랜딩페이지는 unsigned CI 산출물을 직접 배포하지 않습니다.

모든 `윈도우용 다운로드` 링크는 GitHub의 최신 정식 Release에 첨부된 `EbenTiler-Setup.exe`로 연결됩니다. 정식 Release는 `.github/workflows/release.yml`에서 Authenticode 서명, 설치/제거 테스트, SHA-256 검증을 모두 통과한 경우에만 게시됩니다.

따라서 코드 서명된 정식 Release가 아직 없다면 다운로드 링크도 정식 설치 파일을 제공하지 않습니다. 개발 중인 unsigned 설치 파일은 GitHub Actions artifact로만 유지하며 일반 사용자 배포에 사용하지 않습니다.

## 로컬 확인

정적 서버로 `website` 폴더를 열면 랜딩페이지 디자인을 확인할 수 있습니다. 다운로드 버튼은 로컬 파일이 아니라 GitHub의 최신 정식 Release를 가리킵니다.
