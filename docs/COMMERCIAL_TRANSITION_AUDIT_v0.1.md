# Tessdeck 상용 전환 사전 감사 v0.1

기준일: 2026-10-05

목적: 공개 MIT 프로젝트인 Tessdeck v1.3.0을 마지막 공개 버전으로 유지하고, 이후 상용 버전을 별도 Private 저장소에서 개발할 수 있는지 코드/라이선스/브랜딩 관점에서 사전 점검한다.

## 결론

현재 저장소는 **상용 전환 가능한 상태**로 판단한다.

다만 아래 항목은 상용 판매 시작 전에 반드시 정리한다.

1. 공개 저장소의 v1.3.0은 기존 MIT 라이선스를 그대로 유지한다.
2. 상용 Private 저장소에서는 MIT `LICENSE`를 그대로 배포하지 않고 별도 상용 EULA/라이선스로 교체한다.
3. 설치 프로그램의 `LICENSE` 동봉 로직도 상용 라이선스 파일을 가리키도록 변경한다.
4. 홈페이지의 Windows 그래픽 로고 형태는 Tessdeck 자체 아이콘 또는 일반 텍스트로 교체한다.
5. 상용 버전의 업데이트 채널은 공개 GitHub `releases/latest`와 분리한다.
6. Inno Setup의 상업적 사용 정책을 출시 시점에 다시 확인하고 필요한 경우 Commercial License를 구매한다.
7. 판매용 이미지/스크린샷의 제작 출처를 내부 기록으로 남긴다.

## 1. 저장소 / 저작권 상태

- 저장소: `Eben-Builds/Tessdeck-for-Windows`
- 현재 공개 상태: Public
- 현재 라이선스: MIT
- 저작권 표시: `Copyright (c) 2026 Eben-Builds`
- 현재 공개 v1.3.0 및 과거 공개 코드에 부여된 MIT 권리는 이후 상용 전환과 별개로 유지된다.

상용 버전은 같은 저작권자가 자신의 코드에 별도 라이선스를 적용하는 구조로 분리한다.

## 2. 외부 기여 이력

GitHub 커밋 이력 487개를 확인했다.

- `Eben-Builds`: 486 commits
- `github-actions[bot]`: 1 commit
- 사람 외부 기여자: 확인되지 않음
- 확인된 Pull Request는 GitHub Actions dependency bump 성격이며 사람의 제품 코드 기여는 확인되지 않음

현재 기록상 외부 사람의 저작권 동의가 필요한 코드 기여 때문에 상용 전환이 막히는 징후는 없다.

## 3. 런타임 외부 라이브러리

현재 저장소에는 다음 항목이 없다.

- `*.csproj` PackageReference
- `packages.config`
- NuGet runtime dependency manifest
- Newtonsoft.Json
- RestSharp
- MahApps
- MaterialDesign
- FontAwesome
- WebView2
- SQLite
- YamlDotNet
- NLog / Serilog
- Polly
- Autofac / Dapper
- AngleSharp / SkiaSharp

C# 빌드는 Windows에 포함된 .NET Framework compiler와 아래 framework assembly를 직접 참조한다.

- `System.dll`
- `System.Core.dll`
- `System.Drawing.dll`
- `System.Windows.Forms.dll`

네이티브 호출은 `user32.dll`, `dwmapi.dll`, `kernel32.dll` 등 Windows API를 사용한다.

따라서 현재 앱 런타임에는 별도 제3자 NuGet 라이브러리의 재배포 라이선스 의무가 보이지 않는다.

## 4. 웹사이트 외부 자산

현재 웹사이트는 로컬 HTML/CSS/JS와 저장소 내부 이미지/아이콘을 사용한다.

검색 결과 다음 외부 프런트엔드 자산 사용 흔적은 확인되지 않았다.

- Google Fonts
- jsDelivr
- unpkg
- 외부 CDN CSS/JS
- 외부 icon/font package

앱 아이콘과 installer brand image는 `tools/make-appicon.ps1`에서 직접 생성한다.

다만 `hero-tessdeck.webp`, `thirds-tessdeck.webp` 등 판매용 비주얼은 상용 배포 전에 제작 출처를 별도 기록해 둔다. 외부 스톡/제3자 이미지가 아니라 실제 Tessdeck UI 캡처 또는 자체 제작 자산으로 유지하는 것을 원칙으로 한다.

## 5. Microsoft 브랜드 사용 주의

`website/app.js`는 다운로드 버튼에 네 칸 형태의 Windows 로고 그래픽을 직접 삽입한다.

상용 판매 및 Microsoft Store 제출 전에는 해당 그래픽을 제거한다.

권장 대체:

- Tessdeck 자체 아이콘
- 단순한 `Windows용 다운로드` 텍스트
- Microsoft의 별도 사용 허가를 받은 공식 자산이 있는 경우에만 해당 자산 사용

앱 이름의 `Tessdeck for Windows` 같은 호환성 설명은 로고 사용과 별개로 취급한다.

## 6. Inno Setup

현재 installer build는 Inno Setup을 사용한다.

- 앱 런타임에 Inno Setup compiler를 포함하지 않음
- CI/개발 환경에서 installer compiler를 사용
- 생성된 Setup EXE만 사용자에게 배포

Inno Setup 공식 정책은 상업적 사용자를 대상으로 Commercial License 구매를 요청한다. 현재 안내상 강제 조건이라고 명시하지는 않지만, 실제 유료 판매가 시작되면 최신 정책을 다시 확인하고 상업용 라이선스 구매 여부를 결정한다.

## 7. 상용 버전에서 MIT LICENSE 혼입 방지

현재 `installer/Tessdeck.iss`는 다음 파일을 설치 폴더에 포함한다.

```
Source: "..\LICENSE"; DestDir: "{app}"
```

현재 공개판에는 올바른 동작이지만 상용 Private 버전에서 이 상태를 그대로 사용하면 **상용 제품에 MIT LICENSE를 잘못 동봉하는 문제**가 생긴다.

상용 저장소에서는 예를 들어 아래처럼 분리한다.

```
LICENSE-COMMERCIAL.txt
EULA.txt
THIRD-PARTY-NOTICES.txt
```

그리고 installer는 상용 EULA/라이선스만 배포하도록 변경한다.

## 8. 업데이트 채널 분리 필요

현재 `src/UpdateChecker.cs`는 아래 공개 저장소를 직접 확인한다.

```
https://api.github.com/repos/Eben-Builds/Tessdeck-for-Windows/releases/latest
```

따라서 v1.4+ Commercial Edition을 Private으로 운영하면 이 경로를 그대로 사용할 수 없다.

상용 버전에서는 다음 중 하나로 변경한다.

- `tessdeck.com`의 signed update manifest
- 판매/배포 전용 공개 metadata endpoint
- Microsoft Store update channel

상용 설치 파일 자체를 Private GitHub Release URL에 직접 의존시키지 않는다.

## 9. 제품명 사전 확인

2026-10-05 간단한 웹/KIPRIS/USPTO 검색에서 `Tessdeck`의 명확한 동일 소프트웨어/상표 충돌은 확인되지 않았다.

단, 이는 정식 상표 법률 검토가 아니다. 실제 매출이 커지거나 상표 출원을 진행하기 전에는 한국 KIPRIS 및 주요 판매 국가 상표 데이터베이스에서 별도 clearance를 수행한다.

## 10. 권장 저장소 구조

### Public

`Eben-Builds/Tessdeck-for-Windows`

- 마지막 공개 코드: v1.3.0
- MIT
- Community / historical open-source edition
- 보안 또는 치명적 버그 수정만 필요 시 제한적으로 반영
- 상용 Trial/License 구현은 추가하지 않음

### Private

예: `Eben-Builds/Tessdeck-Commercial`

- v1.4.0+
- 3-Day Full Trial
- One-time License
- Commercial EULA
- License activation / device management
- 판매용 update channel
- 결제 연동
- 상용 installer / Microsoft Store packaging

## 11. 상용 전환 Gate

상용 개발 시작 전에:

- [x] 외부 사람 코드 기여 이력 점검
- [x] NuGet/runtime dependency 점검
- [x] 웹 외부 CDN/font dependency 점검
- [x] 공개 MIT 범위 확인
- [x] installer의 MIT LICENSE 동봉 지점 확인
- [x] 공개 GitHub update endpoint 의존 확인
- [ ] Windows 그래픽 로고 제거
- [ ] 판매용 이미지 출처 기록
- [ ] Private Commercial 저장소 생성
- [ ] Commercial EULA 초안
- [ ] Third-Party Notices 초안
- [ ] 상용 update channel 설계
- [ ] Trial / License 구조 설계
- [ ] 결제 공급자 최종 확정
- [ ] 코드서명 / MSIX 배포 전략 확정

