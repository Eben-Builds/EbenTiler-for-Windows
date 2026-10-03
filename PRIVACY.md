# Privacy Policy

EbenTiler for Windows는 로컬에서 동작하는 창 배치 유틸리티입니다.

## 수집하는 정보

EbenTiler 앱은 사용자의 개인정보, 사용 통계, 창 제목, 입력 내용, 파일 내용 또는 계정 정보를 수집하지 않습니다.

## 네트워크 통신

EbenTiler 앱 본체는 네트워크 요청을 하지 않으며 텔레메트리, 광고, 분석 SDK, 원격 API를 사용하지 않습니다.

**This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it.**

사용자가 직접 GitHub 링크나 웹사이트 링크를 열거나 정식 릴리스 설치 파일을 내려받는 경우에는 해당 브라우저와 외부 서비스의 개인정보 처리방침이 적용됩니다. EbenTiler 앱이 백그라운드에서 이 동작을 수행하지는 않습니다.

## 로컬에 저장되는 정보

앱 설정은 현재 Windows 사용자 계정의 다음 위치에 저장됩니다.

`%APPDATA%\EbenTiler\config.ini`

저장되는 값은 단축키, 창 사이 여백, 반복 배치 옵션, 시작 가이드 표시 여부와 같은 로컬 설정뿐입니다.

Windows 시작 시 자동 실행을 켜면 현재 사용자 영역의 다음 레지스트리 값만 사용합니다.

`HKCU\Software\Microsoft\Windows\CurrentVersion\Run\EbenTiler`

관리자 권한이 필요하지 않으며 다른 사용자 계정의 설정에는 접근하지 않습니다.

## 제거

정식 설치 프로그램으로 제거하면 EbenTiler가 만든 `config.ini`와 자동 시작 값을 제거합니다. 같은 설정 폴더에 사용자가 직접 넣은 다른 파일은 삭제하지 않습니다.

## 랜딩페이지

EbenTiler 랜딩페이지는 정적 사이트이며 자체 분석 스크립트, 광고 추적기 또는 사용자 계정을 사용하지 않습니다. 다운로드 버튼은 검증된 정식 GitHub Release 자산을 가리키도록 구성합니다.

## 변경

향후 네트워크 기능이나 데이터 수집 기능이 추가된다면 배포 전에 이 문서를 먼저 갱신하고, 사용자에게 영향을 주는 동작은 명확하게 고지합니다.
