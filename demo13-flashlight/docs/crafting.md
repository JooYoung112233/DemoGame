# 제작 · 레시피 (RecipeData)

> **현 상태 = 진실.** 런타임은 `CraftingSystem` + `Resources/Data/Recipes/` 의 `RecipeData` SO.

## RecipeData 만들기 (Unity 에디터)

1. **`Assets/Resources/Data/Recipes/`** 폴더에서 우클릭
2. **Create → Dev Tools → Item → Recipe Data**
3. 아래 필드 설정 후 저장 (파일명은 자유, `recipeId`가 코드 기준 ID)

| 필드 | 설명 |
|------|------|
| **recipeId** | 고유 ID (`CraftingSystem.UnlockRecipe` / `IsUnlocked` 기준). 결과물 ID와 같아도 됨 |
| **displayName** | UI 표시명 |
| **description** | 짧은 설명 |
| **station** | `Workbench` / `MedicalBench` / `CookingBench` |
| **ingredients** | `itemId` + `count` (ItemDatabase ID) |
| **resultItemId** | 완성 아이템 ID |
| **resultCount** | 완성 수량 (기본 1) |
| **unlockedByDefault** | 체크 = 새 게임부터 조리/제작 가능 (**의료대 기본 레시피**) |
| **unlockRecipeItemId** | 레시피 **문서 아이템** `itemId`. 우클릭 사용 시 이 레시피 영구 해금 (비우면 문서 해금 없음) |

### 해금 규칙

| 유형 | unlockedByDefault | unlockRecipeItemId | 예 |
|------|:---------------:|-------------------|-----|
| 기본 제작 (의료대) | ✅ | (비움) | 붕대 = `cloth_rag`×2 |
| 문서 해금 (조리대·작업대) | ❌ | `recipe_stew` 등 | 맵에서 `recipe_*` 루팅 → 인벤 우클릭 |
| 이미 해금된 문서 재사용 | — | — | **소모 안 함** (NPC 판매용 중복만) |

레시피 문서 아이템: `Assets/Resources/Items/Key/`, `ItemData.isUsable = true` → `PlayerInventory` → `CraftingSystem.TryUnlockFromItem`.

---

## 구현된 레시피 목록

### 조리대 (`CookingBench`) — 5종

| recipeId | unlockRecipeItemId | 재료 | 결과 |
|----------|-------------------|------|------|
| `cooked_stew` | `recipe_stew` | 고기+채소+소금 | `cooked_stew` |
| `cooked_bread` | `recipe_bread` | 밀가루+설탕 | `cooked_bread` |
| `herbal_tea` | `recipe_tea` | 허브+물병 | `herbal_tea` |
| `energy_soup` | `recipe_soup` | 채소+소금+물병 | `energy_soup` |
| `special_meal` | `recipe_special` | 고기+허브+채소+소금 | `special_meal` |

### 의료대 (`MedicalBench`) — 5종, 전부 기본 해금

| recipeId | 재료 | 결과 |
|----------|------|------|
| `bandage` | `cloth_rag`×2 | `bandage` |
| `splint` | `wood_plank`×1 + `cloth_rag`×1 | `splint` |
| `gauze_roll` | `cloth_rag`×1 | `gauze_roll` |
| `painkiller` | `chemical_flask`×1 | `painkiller` |
| `disinfectant` | `chemical_flask`×1 + `water_bottle`×1 | `disinfectant` |

고급 의료(구급상자·수술 키트 등)는 **RecipeData 없음** — NPC 구매만 (`docs/items.md` §11-B).

---

## 루디 충전/가공대 (RudiBench) — 기획 개념 〔미구현 · 수치 TBD〕

> 루디 = 충전·방전·마모 활성 자원([→ gdd-core §5.3](gdd-core.md)). 거점에서 죽은/방전 루디를 **부분 충전**(현상 노드 대비 느림·안전)하고 **가공**하는 스테이션. [→ safehouse.md](safehouse.md)

- **충전**: 방전·죽은 루디 → 충전량 부분 회복(전부는 아님). 안전가옥 강화로 효율↑.
- **가공 레시피 개념**:
  - **소형 루디 합성**: `ruby_shard`/`ruby_dust` 여러 개 → `ruby_crystal` 소폭 보충(큰 루디 용량 채우기).
  - **안정화**: 반복 충전으로 줄어드는 최대 용량(마모)을 늦추는 강화.
- **확인 필요(구현 판단 대기)**: 신규 `station = RudiBench`(StationType enum 추가)로 둘지 작업대 탭으로 통합할지, 충전·가공을 RecipeData로 표현할지 별도 시스템으로 둘지 = 미정. 충전 속도·합성 비율·마모 지연율 = `GameTuning` TBD.

---

## 코드 · 데이터 경로

| 항목 | 경로 |
|------|------|
| SO 정의 | `Assets/Scripts/Crafting/RecipeData.cs` |
| 싱글톤 | `Assets/Scripts/Crafting/CraftingSystem.cs` — `Resources.LoadAll<RecipeData>("Data/Recipes")` |
| CSV 일괄 생성 (조리) | `tools/cooking_recipes.csv` → `tools/GenerateCookingRecipes.ps1` |
| CSV 일괄 생성 (의료) | `tools/medical_recipes.csv` → `tools/GenerateMedicalRecipes.ps1` |

| UI | `CraftingUI` — `UIManager.ShowCrafting(station)` / 시설 상호작용(E) |
| Safehouse 시설 | Interactable Creator로 `Workbench` / `MedicalBench` / `CookingBench` 수동 배치 |
| 디버그 | DebugTestUI(H) → 아이템 탭 → 의료대/조리대/작업대 버튼 |

`CraftingUI`: 좌측 해금 레시피 목록, 우측 재료·결과, **제작/조리** 버튼. 작업대만 **수리** 탭. ESC 닫기. Tab·이동 차단(`IsAnyUIOpen`).

---

## 변경 로그

| 날짜 | 내용 |
|------|------|
| 2026-06-18 | **제작/수리 재료를 가방+메인 창고(MainStash) 합산으로 변경** — CraftingSystem.CountItem/ConsumeItem이 가방(PlayerInventory.Grid)에 더해 MainStash.Instance.Grid도 함께 카운트, 소모는 창고 먼저→부족분 가방 순(HideoutModuleManager.ConsumeMaterial과 동일 규칙). CraftingUI 재료 보유량 표시도 합산값. MainStash null 가드. 시그니처/호출부 보존. |
| 2026-06-18 | **작업대 무기 레시피 3종(칼·방망이·도끼) 기본해금 전환** — WorkbenchKnife/Bat/Axe.asset `unlockedByDefault` 0→1. 이제 작업대 제작 목록에 파이프·나무몽둥이와 함께 5종 전부 즉시 노출(해금 문서 TBD였던 잠금 해제). 수리 탭은 이미 구현·연동 완료(CraftingSystem.GetRepairCost/CanRepair/Repair ↔ CraftingUI 수리 탭) — 변경 없음, 동작 확인만. |
| 2026-06-17 | **작업대(Workbench) 무기 제작 레시피 5종 신설** — craft_pipe(scrap_metal x2→pipe, 기본해금), craft_wood_club(wood_plank x2→wood_club, 기본해금), craft_knife(scrap_metal x1+tool_part x1→knife), craft_bat(wood_plank x1+screw x2→bat), craft_axe(scrap_metal x2+tool_part x1+wood_plank x1→axe). 재료·결과 itemId 모두 기존 아이템과 정합. 칼·방망이·도끼는 잠금(해금 수단 TBD). |
| 2026-06-16 | **루디 충전/가공대(RudiBench) 기획 개념 섹션 추가(gdd-core §5.3 캐논 정합).** 부분 충전 + 소형 루디 합성 + 안정화(마모 지연) 개념. 미구현 — station/RecipeData 표현 방식 확인 필요, 수치 TBD. 구현된 레시피 표(조리5·의료5)는 변경 없음. | [→ gdd-core §5.3](gdd-core.md), safehouse.md. |
| 2026-05-26 | RecipeData 에디터 워크플로 문서화. 조리 5 + 의료 5 SO. `unlockRecipeItemId` / `unlockedByDefault` 규칙 정리. |
| 2026-05-26 | `CraftingUI` 연동 완료. Safehouse 시설 부트스트랩. `InteractType` enum 순서 수정(MapBoard=8 유지). |
