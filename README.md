# Recall 기능 정리

Unity 3D 프로젝트에서 “시간 되감기(Recall)”를 구현한 시스템입니다.
플레이어가 Recall 모드에 진입하면 Raycast로 RecallableObject를 탐지하고, 선택된 오브젝트의 이동 기록(Keyframe)을 기반으로 물리적으로 되감기를 실행합니다.

이 문서는 다음 3개의 스크립트를 중심으로 구조/동작을 정리합니다.
- `Assets/Scripts/RaycastController.cs`
- `Assets/Scripts/RecallManager.cs`
- `Assets/Scripts/RecallableObject.cs`

---

## 구조 요약

흐름
1) `RecallManager.Recall()`로 Recall 모드 진입
2) `RaycastController`가 시선 방향 Raycast로 후보 오브젝트 탐지
3) `RecallManager`가 `detectedRecallableObj`를 갱신 + Outline/경로 표시
4) 입력(Attack Cancel) 시 `RecallManager.SelectObject()` → 해당 오브젝트의 `startRewind()` 실행
5) `RecallableObject`가 기록된 Keyframe을 `FixedUpdate` 주기로 되감음

역할 분리
- `RecallManager`: 상태/입력/선택 관리 + 전역 흐름 제어
- `RaycastController`: 시선 기반 탐지 전용
- `RecallableObject`: 키프레임 기록 + 되감기 실행 + 경로/고스트 시각화

---

## 핵심 스크립트 설명

### 1) RecallManager.cs
Recall 시스템의 상태를 관리하고, 선택/되감기 플로우를 제어합니다.

핵심 변수
- `isRecallActivated` / `isSelectingRecall` / `isRecalling`: Recall 상태 플래그
- `recallTime`, `maxRecallFrames`: 되감기 시간과 저장 프레임 수
  `maxRecallFrames = recallTime * (1 / Time.fixedDeltaTime)`로 계산
- `detectedRecallableObj`: 현재 시선에 잡힌 오브젝트
- `recallToken`: 되감기 중복 호출/중단 방지용 토큰

주요 메서드
- `Recall()`
  - 처음 호출 시: 시간 정지, 흑백 시야 전환, 오브젝트 Outline 준비
  - 선택 중 다시 호출 시: Recall 모드 종료 및 상태 초기화
  - 되감기 중 호출 시: 되감기 중단
- `SelectObject()`
  - 선택 모드에서 클릭 취소 입력이 들어오면 되감기 시작
  - `recallToken`을 증가시켜 유효한 리콜만 진행
- `RecallObject()`
  - 대상이 유효하면 `obj.startRewind(token)`을 `yield return`으로 기다림
  - 중단/토큰 무효 시 빠르게 종료
- `SetDetectedRecallableObj()` / `ClearDetectedRecallableObj()`
  - 이전 대상의 Outline/경로 정리 후 새 대상 등록

---

### 2) RaycastController.cs
Recall 선택 상태에서만 동작하는 시선 기반 Raycast 컨트롤러입니다.

핵심 흐름
- `recallManager.isSelectingRecall`일 때만 Raycast 수행
- 맞은 오브젝트가 `OutlinePP`를 갖고 있으면 Recall 후보로 인정
- 대상이 바뀌면 `recallManager.SetDetectedRecallableObj()` 호출
- 클릭 중이면 Outline 색상을 `outlineSelected`로 변경

특징
- Raycast는 카메라 정면 방향 기준
- 선택 모드가 아닐 때는 즉시 return

---

### 3) RecallableObject.cs
실제 리콜 대상 오브젝트의 키프레임 기록 및 재생(되감기)을 담당합니다.

키프레임 기록
- `FixedUpdate()`에서 `AddKeyFrame()` 호출
  → `keyframes` 배열을 큐처럼 밀어 최신 프레임을 앞에 저장
  → `Time.timeScale != 0`일 때만 기록

되감기
- `startRewind(int token)`
  - 현재 키프레임을 `capturedKeyframes`로 복사(스냅샷)
  - `WaitForFixedUpdate`로 물리 프레임 단위 재생
  - `RecallManager.IsRecallTokenValid(token)` 검사로 중단/교차 호출 방지
  - 종료 후 고스트/경로 정리

경로/고스트 시각화
- `ShowRecallPath()`
  - `LineRenderer`로 이전 이동 경로 표시
  - 일정 간격으로 고스트 메쉬 생성(`ghostIntervalSeconds` 기준)
- `HideRecallPath()` / `ClearAllGhosts()`
  - 경로 및 고스트 제거

---

## 핵심 기능 요약

- 선택 모드 진입 시 시간 정지 + 시각 효과 + 아웃라인 준비
- Raycast로 실시간 대상 탐색
- 되감기 시점은 공격 취소(Click Canceled)
- Keyframe 기반 리와인드(FixedUpdate 기준)
- 되감기 중단/중복 요청은 토큰으로 방어
- 경로(라인) + 고스트로 시각적 피드백 제공

---

## 동작 순서 (간략)
1. 플레이어 입력 → `RecallManager.Recall()`
2. `RaycastController`가 대상 탐지
3. 대상 확정 시 `RecallManager.SelectObject()`
4. `RecallableObject.startRewind(token)` 실행
5. 토큰 유효 시 되감기 진행 → 종료/중단 처리

---

## 참고
- `RecallableObject`는 Rigidbody가 있는 오브젝트만 정상 동작하도록 설계되어 있음
- 키프레임 기록/되감기는 물리 프레임(`FixedUpdate`)에 맞춰 부드럽게 재생되도록 구성
