# 파괴 가능 오브젝트 (Destructible / Breakable)

상자·항아리·판자벽 등을 때리면 **단계별로 부서지고 마지막 단계에 파괴**되는 시스템.
손상 비주얼은 "**모든 셰이더와 같이 쓸 수 있게**" 베이스 위에 덧씌우는 오버레이로 처리한다.

> **관련**: 부서짐과 별개로 "**상시 낡음/녹/폐허**"는 `Weathered` 컴포넌트(같은 `BRB/DamageOverlay` 재사용, 이미지 0장). 카탈로그 "Aged · 낡음/녹/폐허" 섹션. 둘을 같이 켜면 "원래 낡았는데 부서짐". 셰이더 상세는 [`rendering.md`](rendering.md).

## 핵심 결정 (2026-06-03)
- **질문**: "모든 셰이더에 같이 쓸 수 있게 부서지거나 폐허 느낌 셰이더를 넣을 수 있는 구조. 상자를 때리면 1→2→3 단계로 부서지고 마지막엔 파괴되는 느낌."
- **결정**: **오버레이 방식** + 드라이버 스크립트.
  - 손상 비주얼 = 베이스 스프라이트 위에 그리는 **자식 오버레이**(`BRB/DamageOverlay`). 베이스 셰이더(Wall/Prop/Floor/Decal/스프라이트-Lit 등)를 **전혀 건드리지 않음** → 어떤 셰이더든 호환.
  - 단계/파괴 로직 = `Breakable.cs`(MonoBehaviour).
- **근거**:
  - 각 셰이더에 손상 코드를 넣으면 셰이더마다 수정·머티리얼마다 배선이 필요 → 오버레이 1개로 통일.
  - 균열·그을음은 **절차적**(노이즈)이라 단계별 아트 없이도 동작. (원하면 단계별 스프라이트 교체도 옵션)

## 구조
```
Breakable (베이스 GameObject, SpriteRenderer 옆)
├─ 자식 "DamageOverlay" (SpriteRenderer, BRB/DamageOverlay, HideFlags.DontSave)
│    └ 베이스 스프라이트를 복제해 실루엣 마스크로, _Damage(0~1) 단계별 상승
├─ (옵션) 단계별 베이스 스프라이트 교체
├─ (옵션) 베이스 _Brightness 어둡게 (MPB, BRB 공통)
└─ 파괴 시: 파편(절차적) 또는 VFX 프리팹 + 제거/잔해
```

### `BRB/DamageOverlay` (셰이더)
- 베이스 위 Transparent 오버레이. `_Damage` 0~1로 **균열(veins) + 그을음/때(grime)** 를 절차적으로 생성.
- 베이스 스프라이트 **알파로 마스킹** → 실루엣 밖으로 안 새어나감.
- Light2D 반응(`_LIT_ON` 토글), 픽셀화로 도트 일관.
- 색: `_CrackColor`(균열, 어두움) / `_GrimeColor`(그을음). 모양: `_CrackScale`/`_CrackSharp`/`_GrimeScale`/`_GrimeStrength`.

### `Breakable.cs` (드라이버)
| 그룹 | 필드 | 의미 |
|------|------|------|
| Stages | `stageCount`(기본 3) | 손상 단계 수. 마지막 = 파괴(1→2→3) |
| | `maxHp`(기본 24) | 총 내구도(데미지 합). `Hit()` 1회 = maxHp/stageCount. 전투 시 타수 ≈ maxHp / 공격력(약공 ~8 → 24면 ~3대) |
| | `syncWithHealth` | Health 있으면 그걸로 단계 구동(전투 데미지 자동 연동) |
| Overlay | `useOverlay`, `overlayIntensity`, `crackColor`, `grimeColor` | 오버레이 on/세기/색 |
| Sprite Swap | `stageSprites[]` | (옵션) 단계별 베이스 스프라이트 교체. 런타임만 |
| Darken | `darkenBase`, `darkenAmount` | (옵션) 단계별 베이스 어둡게(`_Brightness`) |
| Feedback | `shakeOnHit`/`shakeAmount`, `flashOnHit` | 피격 흔들림 + 흰 플래시(HitFlash 있으면) |
| Break | `destroyOnBreak` | true=제거 / false=잔해로 남김(콜라이더 끔) |
| | `rubbleSprite` | 잔해 스프라이트(옵션) |
| | `debrisCount`/`debrisLifetime` | 절차적 파편 개수/수명 |
| | `breakVfxPrefab` | (옵션) 있으면 절차적 파편 대신 스폰 |
| Loot | `dropItem`/`dropCount` | (옵션) 파괴 시 바닥에 고정 아이템 드랍(`WorldItem.Drop`) |
| | `dropRegionLoot` | (옵션) 지역 루트로도 드랍 — 임시 `ItemSpawnPoint(Ground)` 스폰 |
| Preview | `previewStage` | 에디터에서 단계 미리보기(오버레이만) |

**이벤트**: `OnStageChanged(int)`, `OnBroken`.

## 데미지 입력 (어떻게 "때리나")
타격 파이프라인: `AttackPerformer`(OverlapBox/Circle + `targetMask`=`TopDownPlayer.enemyMask`) → `Hurtbox.ReceiveHit` → `Health.TakeDamage`.

1. **카탈로그에서 "때려서 부수기" ON (권장, 코드 0줄)**: `Prop2DBuilder`가 베이크 시 **`Health`(내구도=`breakHp`) + 자식 `Hurtbox`(트리거, `Destructible` 레이어)** 를 자동 부착하고 `Breakable.syncWithHealth=true`. 플레이어 근접 공격이 닿으면 Health 감소 → Breakable이 `OnDamaged`→단계, `OnDeath`→파괴. **전투 코드 수정 불필요.**
   - **레이어**: `Destructible`은 `GameLayers.EnsureDestructible()`가 자동 생성(없으면). 플레이어 공격이 이 레이어를 때리려면 `TopDownPlayerBuilder`가 `enemyMask`에 `Destructible`을 포함해야 함 → **PlayerRig를 다시 빌드**. (`enemyMask`는 공격 전용이라 자동조준 등엔 영향 없음.)
   - 막힘용 솔리드 콜라이더와 별개로, Hurtbox는 자식의 `isTrigger` 콜라이더(스프라이트 크기).
2. **직접 호출** (트랩·스크립트 이벤트 등 비-전투, "때려서 부수기" OFF): Health 없이 자체 내구도.
   - `breakable.Hit()` — 한 단계 분량(maxHp/stageCount) 데미지. (상자 3대 = 기본값)
   - `breakable.Hit(2)` / `breakable.ApplyDamage(1.5f)` — 임의량.

## 카탈로그(프롭) 연동
에디터: **Tools▸TopDown▸Map▸Prop Catalog**의 "Breakable · 파괴" 섹션(`Prop2DDefinition` 필드). ON이면 `Prop2DBuilder`가 베이크 시 자동 배선:
| 필드 | 의미 |
|------|------|
| `breakable` | 파괴 가능 ON → `Breakable` 부착 |
| `breakStages` / `breakHp` | 단계 수 / 내구도 |
| `breakDestroy` (+`breakRubbleSprite`) | 파괴 시 제거 / 끄면 잔해(콜라이더 끔, 잔해 스프라이트) |
| `breakHittable` | **때려서 부수기** — Health+Hurtbox(Destructible) 자동 부착 |
| `breakOverlayIntensity` / `breakCrackColor` / `breakGrimeColor` | 오버레이 세기·색 |
| `breakDrop` (+`breakDropItemId`/`breakDropCount`/`breakDropRegionLoot`) | 부수면 드랍(고정 아이템 / 지역 루트) |

## 사용 절차 (요약)
1. 카탈로그에서 프롭 `breakable` ON → 필요 옵션(때려서 부수기/드랍/색) 설정 → **베이크**.
2. (최초 1회) **PlayerRig 다시 빌드** → 공격 마스크에 `Destructible` 포함(때려서 부수기 쓸 때).
3. 인스펙터 `previewStage`를 0→N으로 끌어 단계 비주얼 확인.
4. (옵션) 단계별 아트가 있으면 `Breakable.stageSprites`에 넣기(없으면 절차적 오버레이만).

## 한계 / TODO
- 균열은 **절차적**이라 손그림 균열만큼 정교하진 않음 → 정교함이 필요하면 `stageSprites`(단계별 아트) 사용.
- 오버레이 UV는 베이스 스프라이트 UV를 따름 → **아틀라스 스프라이트**면 노이즈가 아틀라스 부분영역으로 잡힘(상자 등 비-아틀라스는 0~1 정상). 필요 시 별도 크랙 텍스처 옵션 추후.
- 파편은 베이스 스프라이트 축소본(절차적) — 전용 파편 스프라이트/파티클은 `breakVfxPrefab`로.
- ⚠️ **레이어 셋업**: "때려서 부수기"는 `Destructible` 레이어 필요(자동 생성) + 플레이어 `enemyMask`에 포함(**PlayerRig 재빌드**). 안 하면 안 맞음.
- ⚠️ **빌드**: `Breakable`이 `Shader.Find("BRB/DamageOverlay")`로 로드 → 머티리얼 에셋이 이 셰이더를 참조하지 않으면 빌드에서 누락될 수 있음. **Project Settings ▸ Graphics ▸ Always Included Shaders**에 `BRB/DamageOverlay` 등록 권장(SpriteFlash와 동일). 못 찾으면 경고 후 오버레이만 생략(파괴 로직은 동작).

## 변경 로그
| 날짜 | 결정 | 근거 |
|---|---|---|
| 2026-06-03 | 파괴 시스템 신설: `Breakable.cs`(단계/파괴/파편/피드백) + `BRB/DamageOverlay`(절차적 균열·그을음 오버레이). 오버레이 방식으로 모든 베이스 셰이더 호환. Health 자동 연동 + `Hit()`/`ApplyDamage()`. 카탈로그(`Prop2DDefinition.breakable`)에서 프롭별 ON. | 사용자 요청. 셰이더별 수정 없이 "전 셰이더 공용 부서짐/폐허". 절차적이라 아트 없이도 동작, 단계별 스프라이트는 옵션. |
| 2026-06-03 | 카탈로그 노출 + 기능 확장(사용자 결정): ① **때려서 부수기**(`breakHittable`) — `Destructible` 레이어 신설(`GameLayers.EnsureDestructible`)+`TopDownPlayerBuilder`가 플레이어 `enemyMask`에 추가, 베이크 시 Health+Hurtbox 자동 부착 → 근접 공격으로 직접 파괴. ② **부수면 드랍**(`breakDrop`: 고정 아이템/지역 루트, `OnBroken`→`WorldItem`/`ItemSpawnPoint`). ③ **오버레이 색·세기** 카탈로그 노출. ④ **파괴 후 잔해**(`breakDestroy=false`+`breakRubbleSprite`). | "맵툴 카탈로그에서 노출 + 기능 추가". 타격 연결은 전용 Destructible 레이어로(Enemy 재사용 안 함 → 박스가 적 취급 안 됨). 단계별 스프라이트 슬롯은 이번 범위 제외. |
