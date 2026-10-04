# 사용자 지정 분할 비율 v0.1

## 목적

Tessdeck의 기존 강점인 키보드 중심 창 배치를 유지하면서, 같은 방향 단축키를 반복해서 눌렀을 때 사용하는 크기 비율을 사용자가 직접 정할 수 있게 한다.

현재 기본 동작:

```text
왼쪽/오른쪽/위/아래 절반 단축키 반복
50% → 33% → 67% → 50% ...
```

v0.1 이후에도 기본값은 그대로 유지한다.

```text
기본 순환 비율: 50 / 33 / 67
```

기존 사용자는 아무 설정도 바꾸지 않아도 지금과 똑같이 동작해야 한다.

---

## 왜 이 기능인가

복잡한 Zone 편집기를 새로 만드는 대신 Tessdeck의 기존 반복 배치 방식을 확장한다.

장점:

- 키보드 중심 사용 흐름을 유지한다.
- 별도 레이아웃 편집기나 오버레이를 만들 필요가 없다.
- 기존 `LeftHalf`, `RightHalf`, `TopHalf`, `BottomHalf` 동작을 그대로 재사용할 수 있다.
- 16:9, 울트라와이드, 세로 모니터 등 사용 환경에 맞춰 비율을 바꿀 수 있다.
- 설정 파일 몇 개의 숫자만 추가하면 되므로 앱의 가벼운 성격을 유지할 수 있다.

Microsoft PowerToys FancyZones도 사용자 정의 영역과 빠른 레이아웃 전환을 제공한다. Tessdeck은 같은 문제를 더 단순한 비율 기반 UX로 해결한다.

참고:
https://learn.microsoft.com/windows/powertoys/fancyzones

---

## UX

설정 > 레이아웃 > 반복 배치 영역을 확장한다.

현재:

```text
[✓] 같은 방향 단축키를 연달아 누르면
    1/2 → 1/3 → 2/3 으로 폭 바꾸기
```

변경:

```text
반복 배치

[✓] 같은 방향 단축키를 연달아 누르면 크기 순환

순환 비율
[ 50 ] %   [ 33 ] %   [ 67 ] %

예: Ctrl + Alt + ←
50% → 33% → 67%
```

### 입력 규칙

- 슬롯은 v0.1에서 3개로 고정한다.
- 허용 범위: 20% ~ 80%
- 정수만 허용한다.
- 중복 값은 허용하지 않는다.
- 저장 시 범위를 벗어나면 저장하지 않고 해당 입력을 강조한다.
- 토글이 꺼져 있으면 첫 번째 비율만 사용한다.
- 기본값은 `50 / 33 / 67`.

---

## 동작

### 왼쪽 / 오른쪽

`50 / 40 / 60`으로 설정한 경우:

```text
Ctrl + Alt + ←
50% → 40% → 60%

Ctrl + Alt + →
50% → 40% → 60%
```

왼쪽 명령은 왼쪽 가장자리에, 오른쪽 명령은 오른쪽 가장자리에 정렬한다.

### 위 / 아래

같은 비율을 높이에 적용한다.

```text
Ctrl + Alt + ↑
50% → 40% → 60%

Ctrl + Alt + ↓
50% → 40% → 60%
```

### 반복 판정

현재 규칙을 유지한다.

- 같은 창
- 같은 SnapAction
- 2초 이내 재입력

위 조건이 아니면 다시 첫 번째 비율부터 시작한다.

---

## 설정 파일

기존:

```ini
[Options]
Gap=0
CycleHalves=true
ShowWelcomeGuide=false
```

변경:

```ini
[Options]
Gap=0
CycleHalves=true
CycleRatio1=50
CycleRatio2=33
CycleRatio3=67
ShowWelcomeGuide=false
```

### 하위 호환

이전 설정 파일에 `CycleRatio1~3`이 없으면 자동으로:

```text
50 / 33 / 67
```

을 사용한다.

따라서 기존 사용자의 동작은 바뀌지 않는다.

---

## 코드 변경 범위

### src/Config.cs

추가:

```text
int CycleRatio1
int CycleRatio2
int CycleRatio3
```

기본값:

```text
50 / 33 / 67
```

Load / Save / CopyFrom / Clone에 포함한다.

### src/WindowManager.cs

현재 고정값:

```csharp
private static readonly double[] CycleFractions =
    new double[] { 0.5, 1.0 / 3.0, 2.0 / 3.0 };
```

을 제거하고 Config 값에서 계산한다.

예:

```text
50 → 0.50
33 → 0.33
67 → 0.67
```

`CycleHalves=false`이면 첫 번째 비율만 사용한다.

### src/SettingsForm.cs

기존 `반복 배치` 영역에 NumericUpDown 3개를 추가한다.

각 입력:

- Minimum: 20
- Maximum: 80
- DecimalPlaces: 0
- AccessibleName 제공

기존 레이아웃 화면을 새 페이지로 늘리지 않는다.

---

## 하지 않는 것

v0.1에서는 다음 기능을 넣지 않는다.

- 자유형 Zone 드래그 편집기
- 창 여러 개를 한 번에 자동 배치
- 앱별 레이아웃 규칙
- 모니터별 별도 비율
- 프로필 무제한 저장
- 클라우드 동기화

이 기능들은 Tessdeck의 현재 규모에 비해 복잡도가 크게 증가하므로 별도 기능으로 검토한다.

---

## 검증

### 설정 호환성

1. 기존 config.ini에서 실행
2. `CycleRatio*` 키가 없어도 정상 실행
3. 기존 기본 동작 `50 → 33 → 67` 유지

### 설정 저장

1. `50 / 40 / 60` 저장
2. 앱 재시작
3. 세 값 유지 확인

### 실제 창 배치

1920×1080 작업 영역 기준:

- 50% ≈ 960px
- 40% ≈ 768px
- 60% ≈ 1152px

DWM 프레임 보정 후 실제 보이는 창 영역을 기준으로 검증한다.

### 방향

- LeftHalf
- RightHalf
- TopHalf
- BottomHalf

모두 동일한 비율 순환을 사용해야 한다.

### 기존 기능 회귀

- 사분면
- 3분할
- 2/3
- 최대화
- 가운데 정렬
- 크기 조절
- 원래 크기 복원
- 모니터 이동

기존 동작에 변화가 없어야 한다.

---

## 릴리스 기준

이 기능은 기존 동작을 깨지 않는 minor feature로 취급한다.

v1.1.2 공개 이후 첫 기능 릴리스 후보는 `v1.2.0`으로 잡는다.

릴리스 전 필수:

- Build Windows Installer
- Visual Brand Check
- Upgrade Compatibility Check
- 실제 100% / 125% / 150% DPI 설정 UI 확인
- 기존 v1.1.2 설정 파일 업그레이드 확인
