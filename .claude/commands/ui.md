---
description: UI 프리팹화(시안 스타일 + Resources/UI 프리팹) 작업을 한 단계 이어서 진행
argument-hint: "[선택: 패널/단계 — 예: ItemDetail, Shop, 키트, next]"
---

`demo13-flashlight` **UI를 코드 절차 생성 → 프리팹 기반(시안 스타일)으로 전환**하는 작업을 한 단계 이어서 진행한다. (요약 짧게 — 파일 덤프 금지.)

## 1. 상태 로드 (먼저, 병렬로)
- `demo13-flashlight/docs/ui-prefab-plan.md` — **계획·이미지 매핑(§3)·단계(§4)·열린 질문(§6)·변경로그. 여기가 SSOT.**
- `demo13-flashlight/Assets/Resources/UI/` — 이미 만든 프리팹(있으면) + `Image/` 스프라이트(box/btn/btnb/name/title/storage*/item*/itembox, `시안.png`=목업).
- `git fetch` 후 원격이 앞서면(다른 PC 병행) 먼저 pull(미커밋이면 stash→pull→pop). `git log --oneline -5` + `git status -s`.
- 전환 대상 현 코드: `demo13-flashlight/Assets/Scripts/UI/` (ItemDetailUI / CharacterPanelUI / ShopUI / UITheme / GridDragView …).

## 2. 요약 후 진행
- 어디까지 됐고 **다음 한 단계가 뭔지 2~3줄** 요약(계획서 §4 기준 다음 미완 단계).
- 계획서 **§6 열린 질문 4가지**(TMP 여부 / 창고 카테고리 탭 / 셀 프리팹 vs GridPanel / RESOURCES HUD 포함)가 미확정이면 **AskUserQuestion으로 먼저 확정** 후 진행(설계 임의결정 금지).
- `$ARGUMENTS` 있으면 그 패널/단계를, 없으면 §4 다음 미완 단계. **권장 시작 = 자산 9-slice 셋업 → 공용 키트 → PoC `ItemDetail`.**

## 3. 아키텍처 원칙 (고정)
- **프리팹 = `Assets/Resources/UI/*.prefab`** + 패널 스크립트의 **`[SerializeField]` 직렬화 바인딩**(find-by-name 금지). 부트스트랩 = **프리팹 Instantiate**(게임 시작 시 코드 절차 생성 금지).
- **동적 콘텐츠(격자 셀/리스트)만 절차 유지** — 직렬화된 슬롯 루트에 Instantiate. 격자 셀 방식은 §6 결정 따름.
- 이미지는 매핑표(§3)대로 **자연스러운 곳만**. **시맨틱 색**(희귀도/HP/스태미나/내구도/토스트)은 `UITheme` 유지 — 억지 적용 금지.
- 큰 패널(CharacterPanelUI·ShopUI)은 **패널 단위 점진 전환**(한 패널 완결 후 다음, 이중 상태 회피).

## 4. 프로젝트 규약
- 입력은 `GameInput`(UnityEngine.Input.* 금지). 코드 생성 EventSystem은 `InputSystemUIInputModule` + `AssignDefaultActions()`.
- **기획/UX 결정은 즉시** 해당 `docs/` md에 기록(날짜/질문/결정). 진행분은 `ui-prefab-plan.md` 단계 체크·변경로그 갱신.
- 코드는 작성 후 **정적 컴파일 감사**(브레이스 균형·시그니처/타입 실재·using 정합). Unity 실행/테스트는 사용자.
- `main` 직접 push 금지(브랜치→PR/푸시). **커밋·푸시는 사용자가 요청할 때만.** 메시지 끝: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

## 5. 마무리
- 단계 끝나면 `ui-prefab-plan.md`(§4 단계 체크 / 변경로그)를 갱신해 다음 핸드오프를 남긴다.
