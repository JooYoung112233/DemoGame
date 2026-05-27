# Demo9 액션 아이콘

`src/data/actions.js`의 각 액션에 대응되는 PNG 아이콘 폴더.

## 파일명 규칙

**파일명 = action ID** (확장자 `.png`)

- 예: `warrior_atk1.png`, `mage_skill.png`, `enemy_runner_swipe.png`

코드가 자동으로 모든 action ID를 순회하며 `assets/actions/<id>.png` 를 로드 시도합니다. **파일이 없으면 이모지로 자동 폴백** — 일부만 채워 넣어도 됩니다.

## 권장 사양

- **64×64 PNG**, 투명 배경 (Retina 대비 128×128도 OK)
- 외곽 1~2px 패딩
- 중요 디테일은 **중앙 60%** 영역에 배치 (모서리는 쿨다운 게이지/숫자 라벨에 가려질 수 있음)
- 타입별 색 코딩:
  - `attack` — 붉/주황 외곽선
  - `skill` — 금/보라 글로우 + 두꺼운 외곽선
  - `support` — 녹/청 외곽선

상세 가이드: [`docs/systems/skill-icons.md`](../../docs/systems/skill-icons.md)

## 플레이어 액션 (24)

### 전사 (warrior)
- `warrior_atk1.png` — 정면 베기
- `warrior_atk2.png` — 회전 베기 (AoE 2)
- `warrior_atk3.png` — 방패 밀치기 (밀치기)
- `warrior_skill.png` — 방패의 맹세 (CD3, 도발+감쇄)

### 도적 (rogue)
- `rogue_atk1.png` — 단검 찌르기
- `rogue_atk2.png` — 투척 단검 (원거리)
- `rogue_atk3.png` — 측면 이동 (자신 후퇴)
- `rogue_skill.png` — 급소 찌르기 (CD3, 확정크리+출혈)

### 마법사 (mage)
- `mage_atk1.png` — 마력 화살
- `mage_atk2.png` — 화염구 (화상)
- `mage_atk3.png` — 냉기파 (둔화)
- `mage_skill.png` — 마력 폭발 (CD4, 적 전체)

### 궁수 (archer)
- `archer_atk1.png` — 활쏘기
- `archer_atk2.png` — 다중 사격 (3타깃)
- `archer_atk3.png` — 저격 (후열 한정)
- `archer_skill.png` — 관통 사격 (CD3, 일렬 관통)

### 사제 (priest)
- `priest_atk1.png` — 신성 빛
- `priest_atk2.png` — 정화 일격 (버프 해제)
- `priest_atk3.png` — 치유 (단일)
- `priest_skill.png` — 신성 치유 (CD3, 전체+디스펠)

### 연금술사 (alchemist)
- `alchemist_atk1.png` — 산성 투척 (DEF↓)
- `alchemist_atk2.png` — 폭탄 (AoE 2)
- `alchemist_atk3.png` — 강화 물약 (아군 ATK+)
- `alchemist_skill.png` — 화염병 (CD4, 적 전체+화상)

## 적 액션 (16)

### 일반
- `enemy_runner_swipe.png` / `enemy_runner_lunge.png`
- `enemy_bruiser_smash.png` / `enemy_bruiser_quake.png`
- `enemy_spitter_spit.png` / `enemy_spitter_spray.png`
- `enemy_summoner_bolt.png` / `enemy_summoner_curse.png`

### 정예
- `enemy_eliterunner_combo.png` / `enemy_eliterunner_eviscerate.png`
- `enemy_elitebruiser_slam.png` / `enemy_elitebruiser_shove.png`

### 보스
- `enemy_pitlord_cleave.png`
- `enemy_pitlord_quake.png`
- `enemy_pitlord_drag.png`

## 파일 추가 후

1. 위 폴더에 PNG 저장 (정확한 파일명)
2. 브라우저 강제 새로고침 (Ctrl+Shift+R)
3. 자동 적용 — 코드 수정 불필요
