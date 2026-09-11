# 개발 핸드오프 (이어서 작업)

> 다른 PC·새 세션에서 이어서 작업할 때 **여기부터** 읽기.
> 최신 갱신: 2026-09-11

## 지금 위치 (2026-09-11)

- **게임**: 3D 루팅 RPG 슈터 — 오소 정면 탑다운 62° 카메라. 방향 = 낙원식 돈벌이·좋은 아이템 루팅 + 총격전. 요약은 [MASTER](MASTER.md) 「게임 정보」·「현재 개발 상태」.
- **3D 전환**: Stage 0~4 완료, Stage 5(2D 잔재 삭제) 남음 — [3d-migration.md](3d-migration.md).
- **병합 대기 PR — 쌓인 브랜치라 번호 순서대로 병합**: #16 총기 밴딧 → #17 플레이어 총격 → #18 배그식 사망 루팅 → #19 맨손 공격 폐기 → #20 셰이더 `BRB/GameLit` → #21 바라보는 방향·착용등·분위기 → #22 카메라 62° → 입구 문서 정리(`docs/entry-refresh`).
  ⚠️ 다른 세션의 마을(Town02)·은신처(Hideout02) 작업이 GameLit을 참조하므로 셰이더 PR(#20)이 먼저 들어가야 한다.
- **진행 중 = 시스템 정리 6단계** ([dev-roadmap.md §시스템 정리](dev-roadmap.md)):
  1 입구 문서 ✅ → 2 전투·총기 ✅(총·근접 둘 다 주력, 구르기 꺼 둠, 퀵슬롯 전환, 총 수치 WeaponData 통합) → **3 루팅·경제**(사용자 결정 필요: 돈 3종 통합 여부) → 4 거점(다른 세션 작업 종료 후) → 5 조명 잔재·레이드/실내 흐름 → 6 폴더·거대 파일.
- **테스트 규칙**: 항상 `Systems` 씬을 열고 Play(게임플레이 씬 단독 실행 금지). 에디터는 CLI로 몬다(아래 「에디터를 CLI로 몰기」).
- **공유 에디터 주의**: 여러 세션이 에디터를 같이 쓴다 — 다른 세션이 플레이를 멈추거나 씬을 열어 둘 수 있다. 커밋은 **내 파일만**(다른 세션의 미커밋 변경은 제외).

---

## (기록) 2026-09-07 밤 — Stage 0 완료, Stage 1 진행 중

브랜치 **`spike/3d-stage0`**(푸시됨). 베이스 = `chore/unity-6.6-upgrade`(PR #7).

### 확정된 카메라 (문서: 3d-migration.md)
**pitch 55° / yaw 0° / orthographicSize 7**(세로 14m) · 오클루전 = **화면 공간 컷어웨이**.
(yaw는 45°로 갔다가 사용자 재결정으로 0°. 대각선 폐기.)

### 된 것
- `Assets/Scenes/Sandbox3D.unity` — 스파이크 씬. 기존 2D를 안 건드리려고 **URP-2D 파이프라인에
  ForwardRenderer를 인덱스 1로 덧붙이고** 이 씬 카메라만 그것을 쓴다(되돌리기 = 목록 한 줄 제거).
- `Assets/Shaders/SpikeOccluder.shader` — 컷어웨이 완성형. 판정 = 화면 반경 ∩ 플레이어보다 앞.
  파라미터는 **전역 유니폼**이라 `Shader.SetGlobalVector` 한 번에 모든 건물이 동작한다.
  반경은 화면 높이 대비 비율(해상도 비의존). 그림자는 구멍과 무관하게 유지.
- `Assets/Scripts/Core/Plan3D.cs` — **평면(XZ) ↔ 월드 변환 헬퍼. Stage 1의 핵심 설계.**
  평면 로직은 계속 `Vector2`(x=월드X, y=월드Z)로 두고 물리·트랜스폼 경계에서만 변환한다.
  덕분에 `FacingDirection` 같은 공개 API가 안 바뀌어 소비자 86개 파일을 건드리지 않는다.
- `TopDownPlayer` 3D 전환 — Rigidbody+중력+CapsuleCollider, 마우스 조준을 **바닥 평면
  레이캐스트**로(오소에서 `ScreenToWorldPoint`는 의미 없음). 컴파일 통과 확인.

### ⚠️ 남은 일 (우선순위 순)
1. **PR #7에 깨진 프리팹이 들어 있다.** Spine 제거 때 쓴 perl 스크립트 버그로
   (`my (@out,$skip,...)=((),0,0,0,0);` — 배열이 뒤 값을 삼킴) `PlayerRig.prefab` 앞에
   `0000`이 찍혀 Unity가 못 읽었다. **`spike/3d-stage0`에서는 고쳤지만 `chore/unity-6.6-upgrade`
   (=PR #7 브랜치)에는 아직 반영 안 됨.** 교훈: **프리팹/씬 YAML을 직접 수정하지 말 것 —
   반드시 `PrefabUtility` 등 Unity API로.**
2. **`PlayerRig` 프리팹의 2D 물리 → 3D 교체 미완.** 런타임에선 `TopDownPlayer.Awake`가
   Rigidbody2D/Collider2D를 제거하고 3D를 붙여 자가보정하지만, 프리팹 자체는 아직 2D다.
   API 스크립트(`PrefabUtility.LoadPrefabContents`)가 NRE로 실패 — 디버깅 필요.
3. **캐릭터 가독성** — 컷어웨이로 뚫어도 캐릭터가 어둡다(올리브/검정 팔레트 + 구멍 안쪽이 그늘).
   권장: **아웃라인 + 접지 링**.
4. Stage 1 나머지 — `EnemyController`(1054) · `AttackPerformer` · `Hurtbox` · `Projectile` ·
   `PlayerGun` · `ThrowSystem` · `NavGrid`/`NavAgent` · `PlayerVision` LOS · `Breakable` ·
   `InteractableObject` · `DoorController`. 총 15파일 4,844 LOC 중 플레이어만 끝난 상태.

### 에디터를 CLI로 몰기 (이 세션에서 검증됨)
```bash
export PATH="$PATH:/c/Users/admin/AppData/Local/Unity/bin"
cd <프로젝트>            # ⚠️ --project-path 대신 cwd 기준으로 실행할 것
unity status             # state: ready 확인
unity command eval --code '<C#>'      # ⚠️ using 지시문 불가 — 전부 정규화 이름
unity command eval_file --file <.cs>
unity command screenshot --view game --output <png> --width 1280 --height 720
unity command recompile ; unity command recompile_status
unity command console --tail 30 --level error
```

---

## (기록) 2026-09-07 — 3D 전환 착수 준비 완료, Stage 0 직전

브랜치 **`chore/unity-6.6-upgrade`** (푸시됨). 커밋 6개: `fafd5e1` 계획 → `ffe9a21` 6.6 →
`511039c` Spine 제거 → `2ab7d2b`·`6b900cc` API 대응 → `c4c3232` CLI/MCP 연동.

### 이 PC에서 한 일
- **3D 쿼터뷰 전환 결정 4건 + Stage 0~5 계획** → SSOT는 [`3d-migration.md`](3d-migration.md).
  완전 3D(XZ+Rigidbody+URP 3D) / 고정 쿼터뷰 오소 / 단차·엄폐까지 / 아트 전부 3D 재제작.
- **Unity 6.3 → 6.6(6000.6.0f1) 업그레이드.** 사용자가 LTS 아니어도 무방 판단.
- **Spine 완전 제거**(689파일 + cha 에셋). 6.6에서 컴파일 불가 + 어차피 Stage 4 대상이라 앞당김.
  플레이어 표시는 3D 치비(`ChibiPlayerVisual`) → 스프라이트 → 그레이박스 순 폴백.
- **6.6 obsolete-as-error 대응**: `GetInstanceID()`→`GetEntityId()`(+컨테이너를 `EntityId`로),
  `SceneHandle`→int 폐지분은 `HashSet<Scene>`으로 회피. **컴파일 통과 확인됨**(에디터 ready).
- **Unity CLI + 공식 skills 6종 + MCP 연동.** 에디터를 CLI/MCP로 직접 제어 가능해짐.

### 🏠 집 PC에서 먼저 할 일 (순서대로)
1. **Unity 6000.6.0f1 설치** — 프로젝트가 이 버전이다. 다른 버전으로 열면 또 업그레이드가 일어난다.
2. `git checkout chore/unity-6.6-upgrade && git pull`
3. **Unity CLI 설치**(머신마다 필요):
   `$env:UNITY_CLI_CHANNEL='beta'; irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex`
   설치 위치 `%LOCALAPPDATA%NITYIN` — 새 셸을 열어야 PATH에 잡힌다.
4. **MCP 등록**: `unity mcp configure claude-code --project-path <프로젝트경로> --yes`
5. Unity로 프로젝트 열기 → `com.unity.pipeline`(manifest에 이미 있음)이 임포트되면
   프로젝트 폴더에서 `unity status` 가 `state: ready` 를 반환해야 한다.
   ⚠️ `--project-path`에 슬래시 경로를 주면 못 찾는 경우가 있다 — **프로젝트 폴더에서 cwd 기준으로 실행**할 것.
   ⚠️ 컴파일 에러가 있으면 Unity가 Safe Mode로 뜨고 Pipeline이 안 올라와 연결 자체가 안 된다.

### 다음 작업 = Stage 0 스파이크 (버리는 실험)
`Sandbox3D` 씬 하나에 URP-3D + 치비 프리팹 + 그레이박스 블록 + 오소 쿼터뷰 카메라.
**눈으로 확정할 것**: 카메라 pitch(≈50° 가안) / **yaw 0° vs 45°** / orthographicSize /
캐릭터 스케일 / 벽 높이 기준 / **오클루전 처리 방식**(디더 페이드·컷어웨이·지붕 숨김 중).
→ 씬 삭제로 되돌아가며 기존 코드는 안 건드린다. 오클루전 방식을 정하지 않고 Stage 3(맵)에
들어가면 맵을 두 번 만들게 되므로 여기서 반드시 답을 낼 것.

쓸 수 있는 에디터 명령(확인됨): `create_scene` `create_gameobject(s)` `add_component`
`set_component_properties` `set_serialized_field` `capture_scene_view` `capture_game_view`
`screenshot` `eval`(Roslyn C#) `open_scene` `save_scene` `bake_navmesh` 등.

### 알려진 구멍 (Stage 4 이월, `3d-migration.md`에 기록됨)
- **피격 연출이 플레이어 몸에 안 보임** — `HitFlash`·`InjuryVFX`가 SpriteRenderer 바디를 가정하는데
  3D 표시에선 그게 꺼져 있다. 화면 효과(비네트·셰이크)는 정상.
- **전투 모션 없음** — 치비는 idle/walk/run 3종뿐. 공격·피격·사망·앉기 미제작.
  구 Spine 구동부의 상태→모션 매핑(roll/attack/sit/sit_walk/walk/run/idle + 원샷 1회 보장)은
  `3d-migration.md` Stage 4에 옮겨 적어둠.
- **QA 봇 처분 미결** — 9파일 4,215 LOC 중 `QaBrain`·`QaBridge`·`QaReport`·`QaScenario`·`QaTelemetry`는
  2D 참조 0(3D에서도 재사용 가능), `QaBot`/`QaSteps`/`QaPerception`/`QaHeatmap`만 2D 결합.
  **Stage 1(XZ 전환)에서 깨질 때 포팅 vs 폐기를 판단**하기로 함.
- **`com.unity.pipeline` 0.6.0-exp.1은 experimental** — 문제 생기면 manifest에서 한 줄 제거.

### 미결 (사용자 판단 필요)
- 카메라 yaw 0° vs 45° · 오클루전 방식 → **Stage 0에서 눈으로**
- 2층·계단을 나중에 넣을지 (이번 범위는 단차·엄폐까지)
- 브랜치 전략 — `codex/chibi-survivor-3d`가 `origin/main` 기준 430 앞/1 뒤로 갈라져 있음

## (기록) 2026-06-29 — UI 프리팹화 풀 + 특성 효과 배선 1·2차 + TraitPanelUI 프리팹화

- **UI 프리팹화 전체(다른 PC 작업) 풀 완료**(b1c612e): 23개 패널 베이크 + UISkin 시안 스킨. 상세 `docs/ui-prefab-plan.md`. (옛 로컬 Systems.unity/PlayerRig.prefab는 stash@{0} 보관.)
- **아이템 아이콘·Spine 에셋 풀**(5379666, 다른 PC 아트 트랙): `Resources/Items/{Food,Medical,etc}` 아이콘 PNG 다수 + Spine. → 아이콘 ItemData 연결·적 Spine은 후속.
- **특성 효과 말단 배선(커밋됨 2e8bae6 = 1차 + TraitPanelUI 프리팹화)**:
  - 배치 1(8키): `TopDownPlayer`(stamina_max/regen·dodge_iframe/stamina_cost)·`PlayerInventory`(weight_max)·`SurvivalStats`(hunger_thirst_rate)·`Health`(damage_taken·low_hp_damage_taken, 플레이어 게이팅). `TraitManager` 정적 `Mod`/`AddVal`/`Flag`.
  - **TraitPanelUI 프리팹화**(누락분): K 토글 → 프리팹 영속+캔버스 토글. **사용자: `프리팹 베이크/TraitPanelUI` 후 K 검증.**
- **특성 효과 배선 2차(미커밋, 4키)**: `PlayerMedicalSystem`(pain_penalty)·`SleepUI`(sleep_recovery, HP만)·`ShopData`(buy_price/sell_price). 정적 감사 통과. 상세 `traits.md` 2026-06-29.
- **다음 코드 후보**: ⑤-3 **배선 3차**(전투 finesse: execute/groggy/durability/repair·잠행 detect_radius·경제 search/npc_affinity — 대상 시스템 존재하는 것부터) · ⑥ **FOV 어둠 오버레이** · ⑦ 총기 · **PP 획득 루트**(레이드/평판→GrantPP) · ⑧ 적 Spine(에셋 들어옴).
- **Unity 테스트 대기**: K 특성 해금 → 체감(노새=무게↑·폐활량=스태미너↑·허약한위장=허기↑·유리어깨=피해↑·통증내성=통증 스태미너 회복↑·빠른회복=수면 HP↑·감정가=판매가↑·단골=구매가↓).

## (기록) 2026-06-24 — 인벤토리 대규모 개편 완료, 전부 커밋·푸시됨

이번 세션은 **인벤토리/컨테이너 UX 전면 개편**. 모두 `feature/demo13-flashlight`에 **커밋·푸시 완료**(최신 `3800113`).

- **드래그 = 픽셀 잡기 오프셋**: 고스트가 잡은 지점 고정(자유 추적), 놓을 때만 가까운 칸 스냅. (`de629de`)
- **우클릭 메뉴 수정**: 좌클릭 다운에서 메뉴 닫혀 onClick 씹히던 버그 → 메뉴 밖 클릭일 때만 닫음. 착용 슬롯 우클릭=착용해제/자세히/제거, 좌클릭 해제는 Ctrl+클릭. 버리기/폐기/제거 **확인 팝업**.
- **아이템 사용 피드백**: `UseItem` 항상 성공/실패 사유 토스트. 소비템 useEffect 오태깅 교정(통조림/단백질/커피/에너지수프/스팀→Food).
- **🟢 보관함(컨테이너) 시스템**: `ItemData`(isContainer/internal격자/allowedCategories) + `ItemInstance.containerGrid`. `cont_*` 8종 SO(`Resources/Items/Container/`, 냉장고·무기케이스·금고 등), 카테고리 게이트, **세이브 재귀 직렬화**(`GridItemEntry.containerItems`), F1 보관함 탭.
- **가방·조끼도 컨테이너**: `containerWidth>0`이면 자동 `IsContainer`. 장착=내용물 휴대격자로 / 해제=가방으로 이동(짐 동행). 해제·반출은 **창고 우선→인벤**(레이드는 인벤, `StoreItemPreferStash`/`IsSafeArea`).
- **컨테이너 = 독립 이동 팝업**(`DraggableWindow`): 셀 크기 맞춤 창, 헤더 드래그, 정렬·닫기 버튼(가방은 정렬 제외). 드래그/우클릭/Ctrl/카테고리 게이트 통합. 팝업 항상 1개.
- **열지 않고 드롭-인**: 컨테이너 위에 드롭=안에 넣기, 꽉차면 스왑 대신 원위치(파랑 하이라이트). **가방 안 가방 방지**(`CanInsertIntoContainer`). Ctrl+클릭: 팝업 안=빼기 / 팝업 열림+창고아이템=넣기.
- **사용 시간(시전) 시스템**: `ItemData.useTimeSeconds`(0=즉시, >0=진행바 채널, 예 붕대 5초). `UseActionManager`(unscaled, ESC 취소, 단일). `UseItem`→`ApplyUseEffect` 분리. (`3800113`)
- **ESC 우선순위**: 컨텍스트메뉴 > 사용채널 > 컨테이너팝업 > 드래그 > 패널.

### ⚠️ 작업 재개 시 먼저 알 것
- **워킹트리에 대규모 Input System 마이그레이션이 진행 중**(미커밋): 약 40+개 파일 `Input.*`→`GameInput.*`, `cha.atlas`/`Monster.prefab` 삭제, `StatDB.asset`/`Zone1.unity`/`PlayerRig.prefab` 수정 등. **내(클로드) 작업 아님 — 손대지 않고 그대로 둠.** 재개하면 이 마이그레이션부터 마무리/커밋할 것.
- **규칙(CLAUDE.md 2026-06-24)**: `UnityEngine.Input.*` 직접 호출 금지 → `Scripts/Core/GameInput.cs` 사용(New-only면 Input.*가 런타임 throw). 코드로 EventSystem 만들 땐 `InputSystemUIInputModule`+`AssignDefaultActions()`. (현 EventSystem 생성처들은 이미 적용됨.)

### 이번 세션 Unity 테스트 대기
- 드래그 잡기 지점 고정 / 우클릭 메뉴 동작 / 확인 팝업 / 사용 토스트.
- F1 보관함 탭→냉장고 +창고→우클릭 열기(팝업)→카테고리 게이트→세이브 유지.
- 가방 열기/장착 시 짐 동행, 해제→창고 우선, 가방안가방 거부.
- 열지 않고 컨테이너 위 드롭-인(꽉차면 원위치), Ctrl 입출.
- 붕대 사용→진행바 5초→ESC 취소. ESC로 팝업만 닫힘.

### 특성 탭 UI 완료 (2026-06-24)
- **`TraitPanelUI`**(신규, 자가부팅 싱글톤, **K 토글**/ESC): 카테고리×티어 특성 목록 + PP 잔량 + **행 클릭 해금**(선행·비용·부정상한 준수) + 디버그 +10PP. uGUI 코드생성(VerticalLayoutGroup 스크롤). `UIManager.IsAnyUIOpen()` 등록(입력 차단). **세이브 훅**(`GameSaveData.traits` + Save/Load). unity-reviewer 통과.
- **⚠️ 미배선(다음)**: 특성 효과 **스탯 말단 read-site**(이속/스태미너/시야 등에 `GetModifier`/`HasFlag` 적용) — 아직 해금만 되고 효과는 미적용. PP 획득 루트(레이드/평판) 연결도 TODO(현재 디버그 버튼).
- **테스트**: 플레이 중 **K** → 특성 패널 → +10PP → T1 특성 행 클릭 해금(✔), 선행/PP부족/부정4개째 거부 토스트, 저장→이어하기 시 해금·PP 유지.

### 다음 코드 후보
④ ~~특성 탭 UI~~ ✅ · ⑤ **특성 효과 스탯 배선**(GetModifier read-site) · ⑥ **FOV 어둠 오버레이** · ⑦ 총기 무기(+파츠 실효과) · ⑧ 적 Spine 애니.

---

## (기록) 2026-06-19
**Phase 3(인벤토리·루팅) 코드 마무리 진행 중.** 6/18 핸드오프 이후 커밋들(대규모 업데이트·Spine 플레이어·중앙 인벤 재구성)에 더해, 이번에 **바닥 중첩 아이템 줍기 목록 UI**까지 코드 완료(정적 감사 통과, Unity 테스트 대기).

- **이번 세션(6/19) 코드 완료** (커밋 `ff346e1` 이후는 **미커밋**, 재컴파일만 필요):
  - **중앙 인벤 UI 재구성**: 단일 가방 격자 → 세로 스택(무기파츠 예약/가방/주머니4/보안3×3) + 좌측 특수창(Special) 슬롯. (커밋 `ff346e1`)
  - **바닥 중첩 줍기 목록 UI**(`GroundPickupUI`): E로 줍을 때 반경 1.6m WorldItem ≥2개면 목록(휠/↑↓ 선택, E 줍기, F 전부, Esc). `WorldItem.All`+`GatherNear`, `InteractableObject.HandlePickup` 분기, UIManager/GameBootstrap 등록.
  - **모션 스탯 일반화**(`MotionStat{anim,animSpeed,distance}` 리스트): 모든 모션 애니 속도+이동거리를 STAT으로. `PlayerStatData.motions`(idle/walk/run/crouch/crouch_walk/attack/roll) 배선(`TopDownPlayer`), `UnitStatData.motions`(idle/walk/attack/hit/death)는 **데이터만**(적 Spine 없음 → 적 애니 도입 시 EnemyController 와이어링). run.distance=최대 달리기 거리, roll.distance=구르기 거리 override. KoLabels 한글 라벨 추가.
  - **roll 애니 끝까지 1회 보장**: 원샷 애니 잠금(`_oneShotAnim`, Spine `IsComplete`) — Dodge 종료돼도 idle로 안 잘림.
  - **안전구역 스태미너 무한**: `RegionTimeManager.ActiveRegionId` 비어있음=안전구역 → 스태미너 가득·무소모·탈진 없음. 레이드 진입 시에만 소모.
  - **아이템 상세 팝업 + 우클릭 메뉴 개편**: "검사"→"자세히"(`ItemDetailUI` 팝업: 큰 아이콘+이름+설명+스탯), 사용템은 안전창고에서도 먹기/사용(`UseItem(placed, sourceGrid)`), 안전창고에 "폐기" 추가. UIManager Esc 우선+등록, 팝업 중 패널 입력 차단. 구 ShowItemInspect 제거.
  - **적 스포너**(`EnemySpawner`, 부팅 자가생성): 게임플레이 씬 로드 시 `SpawnZone`들 읽어 `round(enemyCount×GameTuning.enemySpawnCountMult)` 스폰(씬당1회/언로드시 해제, generatedPrefab 또는 런타임 그레이박스 적). `SpawnZone` 2D화(`GetRandomPoint2D`+기즈모). 고철시장 빌더에 밴딧 공터 SpawnZone(bandit_melee×3) 추가. `GameTuning.enemySpawnCountMult` 신설.
  - **하단 HUD(`QuickSlotBar`, 자가생성)**: 퀵슬롯 1~6(우클릭 "퀵슬롯" 등록→키/클릭 사용, `PlayerInventory.UseItemById`) + **스태미너 바**(`TopDownPlayer.StaminaPercent`, 색 단계). **세이브 영속화**(`GameSaveData.quickSlots`, GetSlotIds/LoadSlotIds). 모달 중 입력 차단, 플레이어 없으면 숨김.
  - **적 HP = unitStat.maxHp 적용**(`EnemyController.Start`). **적 처치 전리품**: `OnDeath`에서 지상 티어 region 루트 시신 주변 산포(`GameTuning.enemyDropChance` 게이트, 컨테이너보다 약함).
  - **무기 파츠(부착물) 1차** — 결정: 조준경/소염기/탄창/손잡이 4종, 무기 인스턴스 귀속(타르코프식), 시스템 먼저(총은 나중). `WeaponPartType` enum + `ItemData` 파츠필드 + `ItemInstance.attachments[4]` + `PlayerEquipment.slotInstances`/`GetSlotInstance`/`WeaponPart*Mult`. 중앙 weaponBox에 4슬롯 렌더(우클릭 "부착"/슬롯클릭 "분리"), `TopDownPlayer` 이속/스태미너에 partMult 곱연산. SO 4종(scope_basic/muzzle_basic/mag_extended/grip_tactical, `Resources/Items/Misc/WeaponPart/`, category=Misc). **⚠️ 미완: 부착물 세이브 영속화(GridItemEntry 확장), 드래그 부착, 총기+사거리/반동/장탄 실효과.** unity-reviewer 통과(인스턴스 추적·무게·null 안전 OK).
  - **FOV 시야콘 1차**(`PlayerVision` 자가생성): 매 프레임 `EnemyController.All`(신규 레지스트리) 가시성 판정(사거리·근접360°·콘각도·LOS Linecast) → `SetVisionVisible`로 시야 밖 적 몸체/HP바 숨김(AI·충돌 유지). `GameTuning.vision*`(enabled/fov150/range9/near2.2/los). **2026-06-05 '항상 은신+말풍선' 실험 정리**: `EnemySpeechBubble.Enabled` 기본 OFF(FOV가 가시성 권위, 강제표시 제거). **⚠️ 미완: 시각적 어둠 오버레이(콘 밖 어둑/포그 — 현재 적 몸체만 숨김), 지역/시간 가변, 랜턴 연동.** unity-reviewer 통과.
  - **적별 전용 드랍 테이블**: `UnitStatData.drops`(`EnemyDropEntry{itemId,chance,minQty,maxQty}`) + `EnemyController.DropLoot`가 테이블 우선/지역루트 폴백. **Control Panel ▸ StatDB ▸ Units ▸ 전리품 드랍**에서 유닛별 편집(제너릭 드로우 자동 노출).
  - **특성 "SO 0개 로드" 블로커 해결(2026-06-22)**: 진단 결과 `Resources TraitData=41`(에셋·로드 정상). 진범 = **에딧 모드에선 `AddComponent`가 Awake를 호출 안 함** → 자가검증의 TraitManager가 LoadDefinitions 미실행. **수정: `TraitManager.EnsureLoaded()` 추가 + 자가검증이 명시 호출**. 런타임(플레이)은 줄곧 정상.
  - **🔴 밸런스·컨트롤 단일 패널 통합(2026-06-22, 규칙 집행)**: 흩어진 밸런스/데이터 편집 메뉴를 `GameControlPanel`(밸런스·컨트롤)로 흡수. **삭제**: `StatDBEditor`(데이터▸스탯DB, 패널 컨트롤/몬스터 탭과 중복). **흡수**: 공격 에디터→**"전투" 탭**(AttackDataEditorWindow를 임베드 가능 클래스로 변환), 특성 SO 재생성·특성/평판 자가검증→**"도구·검증" 탭** 버튼. 4개 별도 `[MenuItem]` 제거. 몬스터 탭에 프리셋(근접/원거리/탱크/보스) 버튼 추가(bandit_melee 원클릭). **규칙: 밸런스/데이터 편집은 무조건 이 패널 탭으로 — 별도 EditorWindow·메뉴 금지.** [[balance-consolidation]]
  - ⚠️ **StatDB.asset 확인**: ①motions 빈 리스트 가능(폴백 안전) → 채워 저장. ②`bandit_melee` 미등록 → 그레이박스 폴백. Control Panel ▸ StatDB. ③파츠 SO 아이콘 미연결(이름 폴백).
  - **무기 파츠 마무리(2026-06-19)**: 부착물 **세이브 영속화**(`GridItemEntry.attachments` 인벤 무기 + `GameSaveData.equippedWeaponAttachments` 장착 무기, 라운드트립) + **드래그 부착**(`TryAttachDraggedToPartSlot` — weaponBox 슬롯에 드래그). 총기 실효과만 미완(총기 설계 필요). unity-reviewer 통과.
- **다음 코드 후보**: ① ~~FOV 시야콘 1차~~ ✅ · ② ~~적별 드랍 테이블~~ ✅ · ③ ~~무기 파츠 마무리(세이브·드래그)~~ ✅ · ④ **특성 탭 UI**(버그 검증 후) ⑤ **FOV 어둠 오버레이**(콘 밖 화면 어둑) ⑥ 총기 무기(+파츠 사거리/반동/장탄 실효과) ⑦ 적 Spine 애니.
- **사용자 Unity 테스트 대기**: 아래 신규 체크.

### 6/19 테스트 체크(신규)
- Tab → 중앙 패널이 무기파츠 예약/가방/주머니4/보안3×3 세로로, 좌측에 특수창 슬롯.
- 가방↔주머니↔보안↔창고 드래그·Ctrl이동·우클릭·Del 정상.
- 레이드에서 한자리에 2개+ 드롭 후 E → 목록 UI 뜨고 휠 선택·E 줍기·F 전부·Esc 닫힘.
- 구르기(Space) → roll 애니가 끝까지 재생 후 idle.
- 안전가옥/은신처에서 Shift 달리기·공격 무한(스태미너 안 닳음), 레이드선 닳음.
- Control Panel ▸ StatDB ▸ Player Stat ▸ motions에서 run/roll/attack 등 animSpeed·distance 조절 → 반영.
- 아이템 우클릭 → **자세히** 팝업(큰 아이콘+설명), 창고 음료 우클릭 → **사용/폐기**.
- **적 스포너**: `Tools ▸ TopDown ▸ 빌드 ▸ 지역1`(또는 고철시장 빌더) 재실행 → 레이드 진입 시 밴딧 공터에 적 3기 스폰·추격·전투. `GameTuning.enemySpawnCountMult`로 마릿수 조절.
- **퀵슬롯**: 소비템 우클릭 → "퀵슬롯" 등록 → 하단 바에 표시, 1~6 키/클릭으로 사용·개수 갱신. 저장→이어하기 시 슬롯 유지.
- **하단 스태미너 바**: 레이드서 달리기/공격 시 줄고 색 변함, 안전구역선 가득.
- **적 처치 전리품**: 적 죽이면 시신 주변에 루트 드랍(주워서 가방). `GameTuning.enemyDropChance`로 조절.

## (이전) 지금 위치 (2026-06-18 커밋 시점)
**하이드아웃 + 안전구역 전 기능을 그레이박스로 일괄 구현 완료 → 사용자 Unity 테스트 대기.**
이번 세션(6/18) 작업이 **이 커밋에 모두 포함**됨. 아래 "6/18 세션 (1)~(3)" 변경 섹션 + 맨 끝 변경들 참조.

- **완료(코드)**: 전체화면 UI 5종(라디오/파견/지역출전/의뢰통신/레이드지도) · 하이드아웃 시설 단일패널 + 발전기 전력 · 상점 위탁 · 작업대 레시피/수리 · 저장 체크포인트(스커밍 방지/크래시 복구) · 건물 트리거 전환(전당포 실내씬) · **이어하기 로드 버그 수정** · 인벤 드래그앤드롭+정렬 · 제작/건설 재료 가방+창고 합산 · 수면 페이드+완료 · 하이드아웃 복귀 스폰 보정.
- **⚠️ Unity 미실행(정적 감사만)**: 컴파일·플레이 검증은 사용자 차례.
- **다음 세션 할 일**: 아래 "## 사용자 확인 필요 (Unity)" 의 재빌드(안전구역/은신처) → "테스트 체크리스트" 순서대로 기능별 검증 → 버그 보고받아 수정.

**스코프**: 리소스(아트/스프라이트/실맵/사운드)만 제외, 그 외 **전 게임 시스템을 그레이박스로** 제작+테스트.

### 진행 중이던 워크플로 (커밋 시점)
UX 5트랙(인벤 드래그·정렬 / 제작 창고연동 / 수면 페이드 / 라디오 레이아웃 / 복귀 스폰) — 빌드 트랙은 **파일 작성 완료**(이 커밋에 포함), unity-reviewer 리뷰만 마무리 단계였음. 다음 세션에서 리뷰 결과/컴파일 에러 확인 권장.
- (6/18 추가) Spine idle 연동 + 캐릭터 좌우 반전 보정(flipInvert) + 밸런스에디터·컨트롤패널 통합(`GameControlPanel` 탭 2개, BalanceEditorWindow 삭제).

## 안전가옥·하이드아웃 남은 작업 (2026-06-18 기준)
**A. 검증/마무리 (먼저)**
- 이번 세션 전체 컴파일·플레이 검증, 안전구역/은신처 재빌드.
- 시설 시작 기본 레벨 결정(어떤 시설 미리 지어둘지 = 밸런스).
**B. 그레이박스 → 실연동 (중간)**
- 파견: roster 인력 슬롯 배정 + 성공률 공식 + 결과=인텔 월드 반영 + 장비 3슬롯 + 실시간 타이머 + 부상/손실 리스크.
- 라디오: 수신 → 실제 인텔/의뢰 생성 연동, 좌측 도움문서 실데이터, 사운드.
- 발전기: 연료 지속 소비(시간) + 꺼지면 라디오/파견 자동 off.
- 게시판: MapSelect 직결 → **의뢰 보드**로 분리 + 평판 게이트.
- NPC 신뢰도(전당포 LV 등) 데이터 연동(현재 전역 평판 임시), 위탁 정산 밸런스.
**C. 신규 콘텐츠/시설 (이후)**
- 인텔 시스템(`IntelManager`/`LandmarkIntelData`) 전체 — 파견/라디오 결과가 레이드 맵 상호작용 지점으로 (safehouse-intel Phase A~E).
- 루디 충전/가공대 시설(기획만 있음).
- 잠긴 건물 4개(수리점/블랙마켓/의료소/가구점) 해금 + 실내씬 + 기능(의료소=S-060).
- 평판 적립 연동 + HUD, 퀘스트 콘텐츠 풀(MQ/SQ/DQ), 보상→평판.
- PostRaidEvent 지역/밤 조건 필터(현재 무시), 아이템 검사 패널 UI(현재 콘솔 로그).
- (점검) StoryPlayer `auto_save`가 레이드 중 세이브 모델 우회하는지.

## Phase 1 이후 추가된 것 (6/16 세션 이후 커밋)
- **대화/상점/UI 시스템 일괄** (`5e4d6ae`): DialogueUI, ShopUI, NPC 상호작용 통합
- **하이드아웃 모듈 업그레이드** (`78ed0d0`): `HideoutModuleManager`, `HideoutUI`, 인벤 3열 레이아웃
- **허기·수분+수면 시스템** (`217c6e0`): `SurvivalStats`, `SleepUI`, `MainStash`(메인창고), 하이드아웃 카메라/ESC, 상점 UI 재구성

## Phase 2 준비 상태
| 항목 | 상태 |
|---|---|
| `SceneTransitionManager` (페이드+additive 전환) | ✅ 범용, 씬이름 동적 |
| `MapSelectUI` → Region1 `sceneName="ScrapMarket_GB"` | ✅ 연결됨 |
| `RaidManager` (타이머+사망+시간초과+정산) | ✅ 씬 배치만 하면 자동 시작 |
| `ScrapMarketGreyboxLayout` (에디터 빌더) | ✅ 준비 — 씬+RaidManager+스폰+출구+루트+적 배치 |
| `StoryTriggerManager` 레이드 씬 인식 | ✅ 수정됨 (제외식: Systems/Hideout가 아니면 레이드) |
| `DebugTestUI` 레이드 버튼 | ✅ `ScrapMarket_GB`로 갱신 |
| `SystemsScene.IsGameplayScene` | ✅ Systems/MapTool 아닌 모든 씬 = 게임플레이 |
| **ScrapMarket_GB.unity (레이드 씬)** | ❌ **미빌드 — 사용자가 에디터에서 실행해야 함** |
| Build Settings에 ScrapMarket_GB 등록 | ❌ 빌더 실행 시 자동 등록됨 |

## 다음 할 일 — 레이드 씬 빌드 + 루프 테스트 (사용자 차례, Unity)
1. **`Tools ▸ TopDown ▸ 맵 ▸ 고철시장 그레이박스`** 실행 → `ScrapMarket_GB.unity` 생성 + Build Settings 자동 등록
2. **Build Settings 확인**: Systems / Safehouse / Hideout / ScrapMarket_GB 4개 등록 확인
3. **Systems 씬만 열고 Play**:
   - 새 게임 → 안전가옥 스폰 확인
   - 지도판(E) → 폐상가(Region1) 선택 → 출전
   - **ScrapMarket_GB 로드 + 걸어다니기 + 20분 타이머 HUD** 확인
   - 맨홀(Manhole_Exit)에서 E → 5초 대기 → 안전가옥 귀환
   - **RaidResultUI 정산창** 뜨는지 확인
4. ⚠️ 확인 사항:
   - 씬 전환 시 페이드 끊김 없는지
   - 레이드 타이머 정상 동작(HUD 상단)
   - 루트 박스(gb_crate/gb_shelf) 상호작용(E)
   - 쪽지(gb_note) 읽기

## 테스트 방식 (고정)
- 정식 실행 = **Systems 씬만** 열고 Play (타이틀→새게임→Safehouse가 additive로 로드).
- 코드는 Claude가 작성+정적 감사 → **Unity 실행/테스트는 사용자** → 결과(특히 컴파일 에러) 받고 다음 단계.

## 6/17 세션 2차 변경사항
- **CharacterPanelUI 탭 제거 완료**: 인벤토리/의료/정보 탭 → 단일 "가방" 레이아웃 (Tarkov 스타일)
- **GameHUD 대폭 정리**: 스태미너 바·돈·배터리 바·HP 바 모두 제거, 부상 아이콘 + 하이드아웃 나가기 버튼만 남김
- **InteractionSystem 2D 거리 수정**: `diff.y=0` (구 이소메트릭) → `Vector2` 거리 (XY 평면)
- **전당포 NPC 스토리 비활성**: `StoryTriggerManager`에서 pawnshop S-002/S-017 체크 비활성 → 바로 일반 대화
- **ShopUI NullRef 수정**: `defaultFont` 제거 + Image+Text 같은 GO 충돌 해소 (모두 자식 GO로 분리)

## 6/17 세션 3차 변경사항
- **장비 슬롯 시스템 신설 (타르코프식)**: 7슬롯 (Head/Armor/Rig/Backpack/PrimaryWeapon/SecondaryWeapon/Melee)
  - `EquipSlot` enum + `ItemData.equipSlot/containerWidth/containerHeight` 추가
  - `PlayerEquipment` 전면 재작성: 멀티슬롯 Dictionary, 하위호환 세이브
  - `PlayerInventory` 가방 연동: 미장착=격자 0x0(인벤 없음), 장착 시 가방 크기로 동적 확장
  - `InventoryGrid.Resize()` 메서드 추가 (넘치는 아이템 → 창고/월드드롭)
  - 장비 무게 → CurrentWeight 합산
- **CharacterPanelUI 장비 UI**: 좌측 패널에 7개 장비 슬롯 시각화 (아이콘+라벨+클릭 해제)
  - 우클릭 컨텍스트 메뉴에 "장착" 옵션 추가
- **상점 우측 컬럼**: 가방 → 창고(MainStash, 10x14)로 변경
- **가방 미장착 시 인벤 숨김**: 중앙 패널에 "가방 미장착" 안내 표시, 격자·무게 텍스트 숨김. 아이템 줍기도 차단 + 토스트 "가방을 장착하세요"

## 6/17 세션 4차 변경사항
- **가방 미장착 시 인벤토리 숨김**: `PlayerInventory` pocketWidth/Height 기본=0, `HasBackpack` 프로퍼티. `CharacterPanelUI` 격자 대신 "가방 미장착" 플레이스홀더. `WorldItem`/`InteractableObject` 줍기 차단+토스트.
- **F1 DebugTestUI 전면 재작성**: 5탭(플레이어/경제·평판/아이템/하이드아웃/씬). 스크랩 충전(1k~1M), 평판 티어 세팅, NPC 호감도, 하이드아웃 모듈 레벨 조작, 서바이벌 스탯 조절.
- **RadioUI 신규**: 은신처 라디오 시설 UI. 4채널(소식/시장/구조/세계관), 레벨별 해금, 랜덤 메시지 수신+로그.
- **DispatchUI 신규**: 은신처 파견 보드 UI. 슬롯 2개(Lv2에서 해금), 목적지 4곳(레벨별 해금), 30초 그레이박스 타이머, 인텔 결과 풀.
- **HideoutController 버튼 추가**: 하단에 "라디오"·"파견 보드" 버튼 추가 (5개 버튼 레이아웃).
- **UIManager 통합**: `IsAnyUIOpen()`·`CloseAll()`에 RadioUI/DispatchUI 추가.
- **HideoutModuleManager.ForceSetLevel** 추가 (디버그용 비용 무시 레벨 설정).
- **NPCRelationshipManager** 디버그 메서드 추가 (`GetAllRelationships`, `ModifyRelationship`).
- **NPC 상점 확장**: veteran_scavenger + district_warden에 ShopData 연결 + 거래 대화 추가
  - `Shop_scavenger.asset`: 생존물자 8종(buyRate 1.2, sellRate 0.4)
  - `Shop_warden.asset`: 프리미엄 보급 6종(buyRate 0.9, sellRate 0.5), 진입 조건 호감도 30
- **평판 기반 상점 필터링**: ShopUI에서 아이템 희귀도↔평판 티어 매핑. Common/Uncommon=F등급, Rare=D등급, Epic=B등급, Legendary=A등급. 미달 아이템은 "잠김" 표시+필요 티어 안내.
- **ShopUI 상단바 평판 표시 개선**: "신뢰도 N" → "평판 N [호칭 등급]" 형식.

## 6/17 세션 5차 변경사항
- **하이드아웃에 라디오·파견 시설 타일 추가**: `HideoutGreyboxLayout`에 `라디오`(7,6.5)·`파견`(8,2.5) 시설 타일 배치. 다른 시설처럼 **클릭 = UI 열림**.
  - `InteractableObject.InteractType`에 `Radio`/`Dispatch` 추가(끝에 추가 — 직렬화 인덱스 보존), 클릭 시 `RadioUI.Show()`/`DispatchUI.Show()`.
  - `StashFacility` → 범용 `FacilityOverride(라벨/색/타입)`로 일반화(창고·라디오·파견 공용).
  - ⚠️ **은신처 그레이박스 재빌드 필요**(아래) — 타일은 씬에 구워지므로 빌더 재실행해야 보임.
- **에디터 메뉴 정리** (Tools ▸ TopDown):
  - **빌드** = `안전구역`/`은신처`/`지역1` **3개만** (각각 ContentBuildAll·Hideout·Zone1 빌더).
  - **맵** = 기능 툴만 남김(팔레트·데칼·조명 셋업·조명 쿠키·프롭 카탈로그/ID·낮밤 드라이버·이상현상 구역 생성). 씬 제작 항목 제거.
  - **개발**(신설) = 인프라/단독 빌더 이동(시스템 씬·맵 편집 씬·인게임/안전가옥 씬·전투 샌드박스·적 프리팹·이상현상 배치·안전가옥/고철시장 그레이박스 단독).

## 6/17 세션 6차 변경사항 — 하이드아웃 시설 UX 통합
- **시설 = 시설별 단일 패널**(타르코프식). 별도 "시설 관리" 목록 UI 폐지.
  - `HideoutUI` 재작성: 모듈 목록형 → `Show(module)` **단일 시설 패널**. Lv0=건설 비용+버튼 / Lv1~=업그레이드 비용+버튼 **+ 기능 버튼** / MAX=기능만. 재료 보유량 색표시.
  - `InteractableObject` 시설 타입(Bed/Workbench/MedicalBench/CookingBench/Stash/Radio/Dispatch) 클릭 → `HideoutUI.Show(moduleKey)` 라우팅(기존 직접 CraftingUI/SleepUI 호출 대체).
  - 기능 버튼: 작업대/의료대/조리대=제작, 창고=열기, 라디오=듣기, 파견=보내기, 침대=휴식. **건설(Lv≥1) 후에만 활성**.
  - `HideoutController` 하단바 정리: "시설 관리/라디오/파견 보드" 버튼 제거 → 나가기+인벤토리만.
  - ⚠️ 시설 전부 **Lv0 시작** → 클릭 시 건설 UI. 테스트는 F1→하이드아웃 탭에서 레벨 세팅 또는 스크랩+재료 지급 후 건설.

## 6/18 세션 — 전체화면 UI 5종 + 하이드아웃/안전구역 기능 완성
- **전체화면 UI 5종**(전부 한글 폰트 Malgun Gothic, 1920x1080, ESC 닫기):
  - `RadioUI` 재작성 = 레트로 무전 콘솔(상단 주파수/우측 수신/좌측 힌트), `DispatchUI` 재작성 = 위성맵+좌측 정보패널, `MapSelectUI` 재작성 = 일러스트 도시맵+노드 마커.
  - `QuestLogUI` 신규 = 메신저(좌측 발신자 목록/우측 스레드), `RaidMapUI` 신규 = 위성 전술맵(마커 현위치●·탈출◆·POI◇).
  - `UIManager.IsAnyUIOpen/CloseAll`에 QuestLogUI/RaidMapUI 등록. `DebugTestUI` 하이드아웃 탭에 "예시 UI 미리보기" 버튼 5개(ShowPreview). `QuestHUD` 우측 패널 클릭 → QuestLogUI.
  - 키: QuestLogUI=J, RaidMapUI=N (F1은 디버그 전용 — 충돌 제거). 둘 다 다른 UI 위엔 안 겹침. **최초 개방은 F1 미리보기 버튼/QuestHUD 클릭 후 키 토글 가능**.
- **발전기 전력 시스템**(그레이박스): 은신처에 발전기 타일. `HideoutModuleManager.GeneratorPowered`/`ToggleGeneratorPower`(ON 시 `fuel_can` 1 소비, 런타임 전용·세이브 안 함). 발전기 패널에서 ON/OFF. **라디오/파견은 전력 ON 전제**(미점등 시 토스트). `InteractType.Generator` 추가.
- **상점 위탁(consignment)**: ShopUI 상단 "거래/위탁" 탭. 위탁가=직접판매가×1.5, 슬롯 3개, 30초 후 정산(런타임 전용). 구매/판매 탭 그대로.
- **작업대**: 무기 레시피 5종 전부 기본 해금(파이프·나무몽둥이·칼·배트·도끼). 수리 탭 이미 완성·연동 확인(작업대 한정, 내구도<max 무기 → 비용 소비 30% 복구). 퀘스트 보고 흐름(NPC 대화→CompleteQuest) 정상 확인.
- **떠돌이 상인**: `wandering_merchant` NPCData + `Shop_merchant` ShopData(생필품 7종, buyRate 1.4/sellRate 0.45).

## 6/18 세션 (2) — 저장 체크포인트 + 건물 트리거 전환
- **저장 모델**(`docs/save.md`): `SaveCheckpoints` 신규. 디스크 커밋 = 레이드 시작/종료·안전맥락 이벤트(건물·의뢰)·침대 수면(수동). 레이드 중 이벤트(전투 시작/종료·건물·의뢰)는 **인메모리 Record만** → **크래시(LogType.Exception)** 시 1회 디스크 복구 커밋 / **강제종료·전원(OnApplicationQuit no-op)** 시 미커밋 → 다음 로드 = **레이드 시작 복귀**(세이브 스커밍 차단).
  - `SaveManager` 리팩터: `BuildSaveData()`/`ToJson()`/`WriteToDisk()`/`WriteJson()` 분리(기존 `Save()`/`AutoSave()` 호환).
  - `CombatStateTracker` 신규: 레이드 씬 0.5초 폴링, `EnemyController.IsEngagingPlayer` 기반 전투 시작 / 단절 30초 후 종료.
  - 훅: QuestManager(수주/완료), SleepUI(BedSleepSave), RaidManager(시작/종료). GameBootstrap 폴백 등록.
  - **리뷰 치명 수정 적용**: SafeHub 씬 = {Safehouse, Hideout, Pawnshop}. `ScrapMarket_GB`(레이드맵)를 안전허브에서 제거·`Pawnshop` 추가. `StoryTriggerManager`도 Pawnshop를 레이드에서 제외.
- **건물 트리거 전환**(`docs/safehouse.md`): `BuildingEntrance`(Collider2D 트리거) — 플레이어 진입 시 자동 `SceneTransitionManager.TransitionTo`(페이드+additive, 캐릭터·Systems 유지). E키 안 씀.
  - **전당포 실내 씬** `Pawnshop.unity` 신규(캐릭터 진입, 강무진+카운터, 출구 트리거→Safehouse/from_pawnshop).
  - 안전가옥: 전당포 방→건물 외관+입구 트리거(→Pawnshop), 은신처 입구 Exit(E)→트리거(→Hideout). 복귀 스폰 from_pawnshop 추가.
  - **하이드아웃만 예외**: 캐릭터 없음 + 퇴장은 UI 나가기/ESC만(트리거 퇴장 없음). 걸어나가는 출구 제거.

## 6/18 세션 (3) — 버그 2건 수정 + 라디오/의뢰/파견 기능
- **버그 수정**:
  - 건설 재료가 가방만 인정하던 문제 → **가방+창고 합산**(`HideoutModuleManager.CountMaterial/ConsumeMaterial`, 발전기 연료 포함, 창고 우선 소비). HideoutUI 보유표시도 합산.
  - 하이드아웃 퇴장 시 엉뚱한 위치 스폰 → `SceneTransitionManager`가 전환 중 옛 씬의 동명 스폰("default")을 잡던 문제. **목적지 씬 스폰만** 후보 한정 + 폴백(default→첫 스폰) + 경고 로그.
  - **이어하기 시 아이템/스크랩/창고 전부 사라지던 치명 버그** → 원인 ①`GameStartHandler`(유일하게 Load() 호출)가 그레이박스 재빌드된 안전가옥에 없어서 **Load()가 아예 안 됨** ②Load가 가방 아이템을 장비(가방)보다 **먼저** 넣어 0x0 격자로 유실. 수정: `GameStartHandler`를 **영속 자가 부트스트랩 + 세션 1회 가드**(씬 배치 불필요·재빌드에도 생존·레이드 귀환 시 재로드 안 함)로 재작성, `SaveManager.Load` 순서를 **장비→가방**으로 교정, `TitleScreen` 새게임/이어하기에서 `ResetSession()`. **이 수정은 재빌드 불필요(재컴파일만).**
- **라디오 주파수 다이얼**: 슬라이더(88~108MHz) 드래그 동조. 채널 틱(89.1/94.5/100.3/106.7), ±0.5 동조 시 수신 활성·밖이면 "치지직", 잠금채널 표시. 소리 TODO.
- **예시 의뢰 2개**: `ex_q01`(통조림 회수/회수꾼/수집), `ex_q02`(점포 정리/관리인/처치). 통신함(QuestLogUI)이 Resources/Data/Quests를 가용의뢰로 자동 로드 → 발신자별 스레드 표시(코드 수정 없음).
- **파견 고용**: DispatchUI "파견/고용" 탭. `DispatchRoster`(런타임) — 회수꾼 랜덤 풀 3명 + 계약(스크랩 견습500/숙련1500/베테랑4000) → 고정 파티 roster.

## 사용자 확인 필요 (Unity) — ⚠️ 테스트 전 준비
1. **안전구역 재빌드 필수**: `Tools ▸ TopDown ▸ 빌드 ▸ 안전구역` → Safehouse 갱신 + **`Pawnshop.unity` 자동 생성·빌드세팅 등록**(끝에서 PawnshopGreyboxLayout 호출). ⚠️ 이거 안 하면 전당포 진입 트리거가 빈 씬 로드 실패.
2. **은신처 재빌드 필수**: `Tools ▸ TopDown ▸ 빌드 ▸ 은신처` → 라디오/파견/**발전기** 타일 + 입구 트리거.
3. **MapSelectUI 갱신**: Systems 씬 구버전 MapSelect 직렬화면 인스펙터 Clear→Generate(또는 시스템 씬 재빌드).
4. **컴파일 확인**: unity-reviewer 정적감사 = 치명 이슈(저장 씬분류) 수정 완료. 실제 플레이 검증은 사용자.
5. 시설 전부 **Lv0 시작** → F1 디버그로 레벨/스크랩/재료 지급 후 건설·기능 테스트.
6. **저장 테스트**: 레이드 진입(저장됨) → 전투/루팅 → Alt+F4 강제종료 → 재실행 시 레이드 시작으로 복귀(루팅 사라짐) 확인. 안전가옥 의뢰 수주/건물 진입은 즉시 저장 확인.

→ 기능별 1개씩 테스트 순서는 채팅의 "테스트 체크리스트" 참조.

## 미해결·후속(이번 세션 발견)
- **StoryPlayer `auto_save` 노드**가 `SaveManager.AutoSave()` 직접 호출 → 레이드 씬 스토리에서 쓰이면 세이브 스커밍 모델 우회. 스토리 스크립트에서 레이드 중 auto_save 사용처 점검 필요(현재 확인 안 됨).

## 미해결·백로그
- **I32**: 타이틀↔`GameStartHandler` 프롤로그/세이브로드 배선(세이브/인트로 단계)
- 손전등 끝처리 쿠키(보류) — 손전등 개념 자체 삭제 예정
- 평판 적립 연동·HUD(Phase 7) — 코어 루프 후
- Phase 3 이후: 인벤토리·루팅 UI 바인딩 마무리, LootContainer 열기, WorldItem 아이콘
