# Commercial Repository Bootstrap

Tessdeck v1.3.0 공개 MIT 코드와 v1.4+ 상용 코드를 분리하기 위한 1회성 bootstrap 절차입니다.

## 전제

GitHub에 **빈 Private 저장소**를 먼저 하나 생성합니다.

권장 이름:

```
Eben-Builds/Tessdeck-Commercial
```

초기화 옵션은 모두 끕니다.

- README 생성 안 함
- .gitignore 생성 안 함
- License 생성 안 함

## 실행

공개 Tessdeck 저장소를 최신 상태로 받은 뒤:

```powershell
git pull origin main

powershell -ExecutionPolicy Bypass -File .\tools\bootstrap-commercial-repo.ps1 `
  -PrivateRemoteUrl "https://github.com/Eben-Builds/Tessdeck-Commercial.git"
```

## 스크립트가 하는 일

1. 대상 Private 저장소가 접근 가능하고 비어 있는지 검사
2. 공개 Tessdeck 저장소를 별도 로컬 폴더로 clone
3. 상용 전환 감사 완료 커밋 `c48f906300087b16d8a6a8d35f921fc0b9b614f1`에 고정
4. 공개 remote를 `community`로 이름 변경
5. 새 Private 저장소를 `origin`으로 등록
6. root `LICENSE`를 proprietary notice로 교체
7. `docs/COMMERCIAL_BASELINE.md` 생성
8. README에 Private Commercial Source 경고 추가
9. 새 상용 기준 commit 생성
10. Private 저장소의 `main`만 push
11. 공개 v1.3.0 tag는 Private 저장소로 복제하지 않음

## 중요한 경계

공개 저장소:

```
Eben-Builds/Tessdeck-for-Windows
v1.3.0 / MIT / Community & historical edition
```

Private 저장소:

```
Eben-Builds/Tessdeck-Commercial
v1.4.0+ / proprietary commercial development
```

상용 Trial, License, Payment, entitlement 코드는 **Private 저장소에서만** 구현합니다.
